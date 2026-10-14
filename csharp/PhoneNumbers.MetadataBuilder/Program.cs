/*
 * Copyright (C) 2026 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace PhoneNumbers.MetadataBuilder;

/// <summary>
/// Build-time tool that converts the XML metadata files in <c>resources/</c> into per-region
/// binary files consumed at runtime. Mirrors the Java upstream's
/// <c>BuildMetadataProtoFromXml</c>: one file per region (or per country-calling-code for
/// non-geographical entities and alternate formats), serialized via
/// <see cref="BuildMetadataFromBin"/>.
/// </summary>
internal static class Program
{
    private const string PhoneMetadataPrefix = "PhoneNumberMetadata";
    private const string ShortMetadataPrefix = "ShortNumberMetadata";
    private const string AlternateFormatsPrefix = "PhoneNumberAlternateFormats";
    private const string TestMetadataPrefix = "PhoneNumberMetadataForTesting";

    private const string NonGeoEntityRegionCode = "001";

    public static int Main(string[] args)
    {
        try
        {
            // Serialize concurrent invocations across MSBuild parents (PhoneNumbers and
            // PhoneNumbers.Test both call us during a parallel `dotnet build` of the sln). Without
            // this, two processes can race writing the same file under obj/geocoding/ — the
            // failure mode that broke CI in the previous attempt at this PR. Mutex name keys off
            // the output dir so different output trees don't cross-block, but a single writer
            // owns each tree at a time.
            var mutexName = ComputeMutexName(args);
            using var mutex = new Mutex(initiallyOwned: false, name: mutexName);
            // Mutex.WaitOne returns true once acquired; abandoned mutex (prior process crashed
            // mid-write) throws AbandonedMutexException — we catch and proceed since on retry the
            // new process will overwrite the partial files cleanly.
            try { mutex.WaitOne(); }
            catch (AbandonedMutexException) { /* prior writer crashed; safe to proceed. */ }

            try { return Run(args); }
            finally { mutex.ReleaseMutex(); }
        }
        // Deliberately broad: this is the process-wide handler for a build-time tool, and the
        // work it guards reaches XML parsing, file I/O and named-mutex code that between them
        // can throw a dozen unrelated types. Nothing is swallowed — the full exception goes to
        // stderr and the non-zero exit code fails the MSBuild target that invoked us — so
        // narrowing this would only trade a readable one-line diagnostic for an unhandled-
        // exception crash dump, with no change to whether the build fails.
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PhoneNumbers.MetadataBuilder failed: {ex.Message}");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    /// <summary>
    /// Builds a stable, OS-friendly mutex name from the output path so concurrent invocations
    /// targeting the SAME output dir contend, but invocations targeting different dirs (e.g.
    /// PhoneNumbers/obj/geocoding vs PhoneNumbers.Test/obj/test-geocoding) run in parallel.
    /// Hashed to dodge the named-mutex character restrictions (no path separators on Windows;
    /// 250-char limit on macOS/Linux IIRC).
    /// </summary>
    private static string ComputeMutexName(string[] args)
    {
        // args[2] is the output dir/file for every supported subcommand.
        var key = args.Length >= 3 ? Path.GetFullPath(args[2]) : "global";
        using var sha = SHA1.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        return "Global\\PhoneNumbers.MetadataBuilder." + Convert.ToHexString(hash);
    }

    /// <summary>
    /// Creates the directory a file is about to be written into. One helper because the three
    /// call sites had three spellings, one of which (GetDirectoryName without GetFullPath) throws
    /// for a bare relative filename -- fine from MSBuild, which always passes a joined path, and a
    /// crash for anyone running the tool by hand.
    /// </summary>
    private static void EnsureDirectoryFor(string outputFile) =>
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile))!);

    /// <summary>
    /// The one list of supported kinds. Both callers print this rather than restating it: the
    /// unknown-kind message used to carry its own list, which had already drifted to name four of
    /// the nine kinds.
    /// </summary>
    private static void PrintUsage()
    {
        Console.Error.WriteLine(
            "Usage: PhoneNumbers.MetadataBuilder <kind> <input> <output-dir-or-file>");
        Console.Error.WriteLine(
            "  kind: all                                      (resources/ -> every output below)");
        Console.Error.WriteLine(
            "        phone | short | alternate | test         (XML metadata file -> per-region bins)");
        Console.Error.WriteLine(
            "        geocoding | carrier                      (<type>/ tree -> one .pack)");
        Console.Error.WriteLine(
            "        timezones                                (timezones/map_data.txt -> single bin)");
        Console.Error.WriteLine(
            "        locale                                   (locale/country_names.txt -> one .pack)");
    }

    private static int Run(string[] args)
    {
        if (args.Length < 3)
        {
            PrintUsage();
            return 2;
        }

        var kind = args[0];
        var input = args[1];
        var output = args[2];

        return kind switch
        {
            "phone" => BuildPerRegion(input, output, PhoneMetadataPrefix,
                isShortNumberMetadata: false, isAlternateFormatsMetadata: false),
            "short" => BuildPerRegion(input, output, ShortMetadataPrefix,
                isShortNumberMetadata: true, isAlternateFormatsMetadata: false),
            "alternate" => BuildPerRegion(input, output, AlternateFormatsPrefix,
                isShortNumberMetadata: false, isAlternateFormatsMetadata: true),
            "test" => BuildPerRegion(input, output, TestMetadataPrefix,
                isShortNumberMetadata: false, isAlternateFormatsMetadata: false),
            "all" => BuildAll(input, output),
            "geocoding" or "carrier" => BuildGeocoding(input, output),
            "timezones" => BuildTimezones(input, output),
            "locale" => BuildLocaleNames(input, output),
            _ => UnknownKind(kind),
        };
    }

    /// <summary>
    /// Walks an input directory tree shaped <c>&lt;inputDir&gt;/&lt;lang&gt;/&lt;countryCode&gt;.txt</c>
    /// (the layout used by libphonenumber's geocoding/ and carrier/ trees) and emits a single
    /// <see cref="ResourcePack"/> whose entries are named <c>&lt;lang&gt;.&lt;countryCode&gt;</c>.
    /// <para>
    /// One file rather than ~220 so the assembly carries one embedded resource per data set. That
    /// is what lets the trimmer drop the whole set from a fixed four-line substitutions file --
    /// resource removal matches exact names and has no wildcard -- and it lets the MSBuild target
    /// above declare its real output instead of a sentinel.
    /// </para>
    /// </summary>
    private static int BuildGeocoding(string inputDir, string outputFile)
    {
        if (!Directory.Exists(inputDir))
            throw new DirectoryNotFoundException($"Input directory not found: {inputDir}");
        if (IsPackUpToDate(inputDir, outputFile))
            return 0;
        EnsureDirectoryFor(outputFile);

        var entries = new List<KeyValuePair<string, byte[]>>();
        foreach (var langDir in Directory.EnumerateDirectories(inputDir))
        {
            var lang = Path.GetFileName(langDir);
            foreach (var txtPath in Directory.EnumerateFiles(langDir, "*.txt"))
            {
                var countryCode = Path.GetFileNameWithoutExtension(txtPath);
                var map = ParseAreaCodeText(txtPath);
                // Entries stay individually gzipped: the runtime decompresses only the one
                // (language, country) map it was asked for, exactly as before packing.
                using var buffer = new MemoryStream();
                using (var gz = new GZipStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
                    BuildPrefixMapFromBin.WriteAreaCodeMap(gz, map);
                entries.Add(new KeyValuePair<string, byte[]>($"{lang}.{countryCode}", buffer.ToArray()));
            }
        }

        WritePack(outputFile, entries);
        Console.Out.WriteLine(
            $"PhoneNumbers.MetadataBuilder: packed {entries.Count} prefix map(s) into {outputFile}");
        return 0;
    }

    /// <summary>
    /// Writes a pack to a temporary file and renames it into place, so a reader cannot observe a
    /// half-written pack. The mutex in <see cref="Main"/> is what actually serialises writers; this
    /// is cheap insurance on top of it, and cheaper here than for the per-region bins because a
    /// pack is one file rather than several hundred.
    /// </summary>
    private static void WritePack(string outputFile, List<KeyValuePair<string, byte[]>> entries)
    {
        var temp = outputFile + ".tmp";
        using (var stream = File.Create(temp))
            ResourcePack.Write(stream, entries);
        File.Move(temp, outputFile, overwrite: true);
    }

    /// <summary>
    /// A pack is up to date when it exists and is at least as new as the newest input under the
    /// tree it was built from.
    /// </summary>
    private static bool IsPackUpToDate(string inputDir, string outputFile)
    {
        if (!File.Exists(outputFile)) return false;
        var newestInput = Directory.EnumerateFiles(inputDir, "*.txt", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max();
        return File.GetLastWriteTimeUtc(outputFile) >= newestInput;
    }

    /// <summary>
    /// Converts <c>resources/timezones/map_data.txt</c> into a single binary file at the supplied
    /// output path. The text format pairs a phone-number prefix with one or more IANA tz names
    /// joined by '&amp;'; we split here and store the array directly so the runtime mapper doesn't
    /// have to.
    /// </summary>
    private static int BuildTimezones(string inputFile, string outputFile)
    {
        if (!File.Exists(inputFile))
            throw new FileNotFoundException($"Input file not found: {inputFile}", inputFile);
        if (File.Exists(outputFile)
            && File.GetLastWriteTimeUtc(outputFile) >= File.GetLastWriteTimeUtc(inputFile))
            return 0;
        EnsureDirectoryFor(outputFile);

        var map = ParseTimezoneText(inputFile, splitter: '&');
        using var gz = new GZipStream(File.Create(outputFile), CompressionLevel.SmallestSize);
        BuildPrefixMapFromBin.WriteTimezoneMap(gz, map);
        Console.Out.WriteLine($"PhoneNumbers.MetadataBuilder: wrote {map.Count} timezone entries to {outputFile}");
        return 0;
    }

    /// <summary>
    /// Converts <c>resources/locale/country_names.txt</c> (lines of
    /// <c>country|language|name</c>) into a single <see cref="ResourcePack"/> with one entry per
    /// country, so the runtime can still decompress a single country's names on demand.
    /// </summary>
    private static int BuildLocaleNames(string inputFile, string outputFile)
    {
        if (!File.Exists(inputFile))
            throw new FileNotFoundException($"Input file not found: {inputFile}", inputFile);
        if (File.Exists(outputFile)
            && File.GetLastWriteTimeUtc(outputFile) >= File.GetLastWriteTimeUtc(inputFile))
            return 0;
        EnsureDirectoryFor(outputFile);

        var byCountry = ParseLocaleText(inputFile);
        var entries = new List<KeyValuePair<string, byte[]>>(byCountry.Count);
        foreach (var country in byCountry)
        {
            using var buffer = new MemoryStream();
            using (var gz = new GZipStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
                BuildPrefixMapFromBin.WriteLocaleNames(gz, country.Value);
            entries.Add(new KeyValuePair<string, byte[]>(country.Key, buffer.ToArray()));
        }

        WritePack(outputFile, entries);
        Console.Out.WriteLine(
            $"PhoneNumbers.MetadataBuilder: packed {entries.Count} locale name set(s) into {outputFile}");
        return 0;
    }

    /// <summary>
    /// Every output in one invocation. The seven separate MSBuild <c>Exec</c>s this replaces each
    /// paid a process launch and a turn through the cross-process mutex, and each needed its own
    /// Inputs/Outputs gate with a sentinel file standing in for a directory of outputs. The
    /// per-kind up-to-date checks below still short-circuit, so a no-op rebuild stays a no-op.
    /// </summary>
    private static int BuildAll(string resourcesDir, string objDir)
    {
        if (!Directory.Exists(resourcesDir))
            throw new DirectoryNotFoundException($"Resources directory not found: {resourcesDir}");

        // Straight-line, with the return values ignored: every Build* below returns 0 on every
        // path and signals failure by throwing, which Main turns into a non-zero exit. Threading an
        // rc through would imply a convention none of them follows.
        var metadataDir = Path.Join(objDir, "metadata");
        BuildPerRegion(Path.Join(resourcesDir, "PhoneNumberMetadata.xml"), metadataDir,
            PhoneMetadataPrefix, isShortNumberMetadata: false, isAlternateFormatsMetadata: false);
        BuildPerRegion(Path.Join(resourcesDir, "ShortNumberMetadata.xml"), metadataDir,
            ShortMetadataPrefix, isShortNumberMetadata: true, isAlternateFormatsMetadata: false);
        BuildPerRegion(Path.Join(resourcesDir, "PhoneNumberAlternateFormats.xml"), metadataDir,
            AlternateFormatsPrefix, isShortNumberMetadata: false, isAlternateFormatsMetadata: true);
        BuildGeocoding(Path.Join(resourcesDir, "geocoding"), Path.Join(objDir, "geocoding", "geocoding.pack"));
        BuildGeocoding(Path.Join(resourcesDir, "carrier"), Path.Join(objDir, "carrier", "carrier.pack"));
        BuildLocaleNames(Path.Join(resourcesDir, "locale", "country_names.txt"),
            Path.Join(objDir, "locale", "locale.pack"));
        BuildTimezones(Path.Join(resourcesDir, "timezones", "map_data.txt"),
            Path.Join(objDir, "timezones", "map_data.bin"));
        return 0;
    }

    private static SortedDictionary<string, SortedDictionary<string, string>> ParseLocaleText(string path)
    {
        var byCountry = new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.Ordinal);
        using var reader = new StreamReader(path, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var first = line.IndexOf('|');
            if (first < 0) continue;
            var second = line.IndexOf('|', first + 1);
            if (second < 0) continue;

            var country = line.Substring(0, first);
            var language = line.Substring(first + 1, second - first - 1);
            // Names never contain '|' (asserted when this file was first generated), so the
            // remainder of the line is the name even though it is free text.
            var name = line.Substring(second + 1);

            if (!byCountry.TryGetValue(country, out var names))
                byCountry[country] = names = new SortedDictionary<string, string>(StringComparer.Ordinal);
            names[language] = name;
        }
        return byCountry;
    }

    /// <summary>
    /// Returns true when every per-region bin under <paramref name="outputDir"/> matching the
    /// supplied prefix is at least as new as <paramref name="inputXml"/>. Used inside the mutex
    /// to short-circuit redundant work when a sibling MSBuild inner build already generated the
    /// bins.
    /// </summary>
    private static bool IsOutputUpToDate(string inputXml, string outputDir, string filePrefix)
    {
        if (!Directory.Exists(outputDir)) return false;
        var existing = Directory.GetFiles(outputDir, filePrefix + "_*");
        if (existing.Length == 0) return false;
        var inputMTime = File.GetLastWriteTimeUtc(inputXml);
        return !existing.Any(file => File.GetLastWriteTimeUtc(file) < inputMTime);
    }

    private static SortedDictionary<int, string> ParseAreaCodeText(string path)
    {
        var map = new SortedDictionary<int, string>();
        using var reader = new StreamReader(path, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var pipe = line.IndexOf('|');
            if (pipe < 0) continue;
            var prefix = int.Parse(line.AsSpan(0, pipe), CultureInfo.InvariantCulture);
            map[prefix] = line.Substring(pipe + 1);
        }
        return map;
    }

    private static SortedDictionary<long, string[]> ParseTimezoneText(string path, char splitter)
    {
        var map = new SortedDictionary<long, string[]>();
        using var reader = new StreamReader(path, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var pipe = line.IndexOf('|');
            if (pipe < 0) continue;
            var prefix = long.Parse(line.AsSpan(0, pipe), CultureInfo.InvariantCulture);
            map[prefix] = line.Substring(pipe + 1).Split(splitter, StringSplitOptions.RemoveEmptyEntries);
        }
        return map;
    }

    private static int UnknownKind(string kind)
    {
        Console.Error.WriteLine($"Unknown kind '{kind}'.");
        PrintUsage();
        return 2;
    }

    private static int BuildPerRegion(
        string inputXml,
        string outputDir,
        string filePrefix,
        bool isShortNumberMetadata,
        bool isAlternateFormatsMetadata)
    {
        // Double-checked skip: even though MSBuild's Inputs/Outputs gating skips this target
        // when outputs are up-to-date, three concurrent inner per-TFM builds can all pass that
        // gate on a fresh build (no outputs yet, all see "rebuild needed") and queue behind the
        // Mutex acquired in Main(). The first invocation does the work; subsequent ones must
        // re-check here and skip, otherwise their concurrent re-writes race with the C#
        // compiler reading already-embedded resources from a sibling inner build.
        if (IsOutputUpToDate(inputXml, outputDir, filePrefix))
            return 0;

        // The tool creates its own output directory rather than relying on a <MakeDir> in the
        // calling target: the single `all` invocation writes into four of them, and a caller that
        // forgets one fails only on a clean build, where obj/ does not already exist.
        Directory.CreateDirectory(outputDir);

        using var input = File.OpenRead(inputXml);
        var metadataList = BuildMetadataFromXml.BuildPhoneMetadataFromStream(
            input,
            liteBuild: false,
            specialBuild: false,
            isShortNumberMetadata: isShortNumberMetadata,
            isAlternateFormatsMetadata: isAlternateFormatsMetadata);

        var written = 0;
        foreach (var metadata in metadataList)
        {
            var key = MakeFileNameKey(metadata, isAlternateFormatsMetadata);
            var path = Path.Join(outputDir, Path.GetFileName($"{filePrefix}_{key}"));
            using var gz = new GZipStream(File.Create(path), CompressionLevel.SmallestSize);
            BuildMetadataFromBin.WriteMetadata(gz, metadata);
            written++;
        }

        Console.Out.WriteLine($"PhoneNumbers.MetadataBuilder: wrote {written} {filePrefix}_* file(s) to {outputDir}");
        return 0;
    }

    /// <summary>
    /// Builds the per-file suffix Java's <c>MultiFileModeFileNameProvider</c> would: region code
    /// for geographical entries, country-calling-code for non-geographical / alternate-format
    /// entries (which don't have a meaningful region code).
    /// </summary>
    private static string MakeFileNameKey(PhoneMetadata metadata, bool isAlternateFormatsMetadata)
    {
        if (isAlternateFormatsMetadata)
            return metadata.CountryCode.ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(metadata.Id) || metadata.Id == NonGeoEntityRegionCode)
            return metadata.CountryCode.ToString(CultureInfo.InvariantCulture);
        return metadata.Id;
    }
}
