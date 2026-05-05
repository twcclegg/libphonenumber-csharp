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

    private static int Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine(
                "Usage: PhoneNumbers.MetadataBuilder <kind> <input> <output-dir-or-file>");
            Console.Error.WriteLine(
                "  kind: phone | short | alternate | test         (XML metadata file -> per-region bins)");
            Console.Error.WriteLine(
                "        geocoding                                (geocoding/ tree -> per-(lang,cc) bins)");
            Console.Error.WriteLine(
                "        timezones                                (timezones/map_data.txt -> single bin)");
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
            "geocoding" => BuildGeocoding(input, output),
            "timezones" => BuildTimezones(input, output),
            _ => UnknownKind(kind),
        };
    }

    /// <summary>
    /// Walks an input directory tree shaped <c>&lt;inputDir&gt;/&lt;lang&gt;/&lt;countryCode&gt;.txt</c>
    /// (the layout used by libphonenumber's geocoding/ and carrier/ trees) and emits one binary
    /// file per (lang, countryCode) pair as <c>&lt;outputDir&gt;/&lt;lang&gt;.&lt;countryCode&gt;</c>.
    /// </summary>
    private static int BuildGeocoding(string inputDir, string outputDir)
    {
        if (!Directory.Exists(inputDir))
            throw new DirectoryNotFoundException($"Input directory not found: {inputDir}");
        if (IsGeocodingOutputUpToDate(inputDir, outputDir))
            return 0;
        Directory.CreateDirectory(outputDir);

        var written = 0;
        foreach (var langDir in Directory.EnumerateDirectories(inputDir))
        {
            var lang = Path.GetFileName(langDir);
            foreach (var txtPath in Directory.EnumerateFiles(langDir, "*.txt"))
            {
                var countryCode = Path.GetFileNameWithoutExtension(txtPath);
                var map = ParseAreaCodeText(txtPath);
                var outPath = Path.Combine(outputDir, $"{lang}.{countryCode}");
                using var fs = File.Create(outPath);
                BuildPrefixMapFromBin.WriteAreaCodeMap(fs, map);
                written++;
            }
        }
        Console.Out.WriteLine($"PhoneNumbers.MetadataBuilder: wrote {written} geocoding bin file(s) to {outputDir}");
        return 0;
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
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

        var map = ParseTimezoneText(inputFile, splitter: '&');
        using var fs = File.Create(outputFile);
        BuildPrefixMapFromBin.WriteTimezoneMap(fs, map);
        Console.Out.WriteLine($"PhoneNumbers.MetadataBuilder: wrote {map.Count} timezone entries to {outputFile}");
        return 0;
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
        foreach (var file in existing)
        {
            if (File.GetLastWriteTimeUtc(file) < inputMTime) return false;
        }
        return true;
    }

    /// <summary>
    /// Geocoding analog: every existing bin under <paramref name="outputDir"/> must be at least
    /// as new as the newest .txt under <paramref name="inputDir"/>'s tree.
    /// </summary>
    private static bool IsGeocodingOutputUpToDate(string inputDir, string outputDir)
    {
        if (!Directory.Exists(outputDir)) return false;
        var existing = Directory.GetFiles(outputDir);
        if (existing.Length == 0) return false;
        var newestInput = DateTime.MinValue;
        foreach (var f in Directory.EnumerateFiles(inputDir, "*.txt", SearchOption.AllDirectories))
        {
            var t = File.GetLastWriteTimeUtc(f);
            if (t > newestInput) newestInput = t;
        }
        foreach (var file in existing)
        {
            if (File.GetLastWriteTimeUtc(file) < newestInput) return false;
        }
        return true;
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
        Console.Error.WriteLine($"Unknown kind '{kind}'. Expected one of: phone, short, alternate, test.");
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
            var path = Path.Combine(outputDir, $"{filePrefix}_{key}");
            using var fs = File.Create(path);
            BuildMetadataFromBin.WriteMetadata(fs, metadata);
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
