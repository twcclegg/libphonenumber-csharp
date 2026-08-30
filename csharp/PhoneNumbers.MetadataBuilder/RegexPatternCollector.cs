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
using System.IO;
using System.IO.Compression;

namespace PhoneNumbers.MetadataBuilder;

/// <summary>
/// Build-time: walks every region in <c>PhoneNumberMetadata.xml</c>, <c>ShortNumberMetadata.xml</c>
/// and <c>PhoneNumberAlternateFormats.xml</c> and collects every distinct regex pattern string the
/// runtime could hand to <c>PhoneRegex.Get(...)</c> from a metadata-driven call site, then writes
/// that flat set (via <see cref="BuildPrefixMapFromBin.WriteRegexPatternList"/>) to a single
/// gzip-compressed binary file that <c>PhoneRegex</c> reads at static-init to pre-populate its
/// pattern cache with known keys -- see PhoneRegex.cs's <c>KnownPatterns</c>/<c>FrozenDictionary</c>
/// remarks. This only enumerates strings; it never builds a <see cref="System.Text.RegularExpressions.Regex"/>
/// or emits generated code -- each pattern's actual <c>Regex</c> is still built lazily, on first real
/// use, exactly as for any pattern outside the known set. That's the entire difference from the
/// abandoned <c>perf/coldstart-sourcegen-regex</c> branch this enumeration logic was ported from: that
/// branch went on to emit one <c>[GeneratedRegex]</c> method per pattern per anchoring variant
/// (8,766 generated methods, a 152s build and 5.4x assembly size); this only reuses the enumeration
/// step, not what it fed into.
/// <para>
/// Coverage is NOT exhaustive of every string that could ever reach <c>PhoneRegex.Get</c>: the legacy
/// public <c>RegexCache.GetPatternForRegex</c> / <c>PhoneRegex(string)</c> surface accepts arbitrary
/// caller-supplied text that cannot be known at build time, and <c>PhoneNumberMetadataForTesting.xml</c>
/// (test-only metadata, never shipped in the published assembly) is deliberately excluded -- both fall
/// back to the ordinary, unmodified dynamic-cache path in <c>PhoneRegex</c>. Everything actually
/// reachable from metadata-driven internal call sites (PhoneNumberDesc national-number patterns,
/// NumberFormat pattern + leading-digits patterns, NationalPrefixForParsing, LeadingDigits,
/// InternationalPrefix in both its raw form and the "\+|" + InternationalPrefix form
/// AsYouTypeFormatter.AttemptToExtractIdd builds) is covered.
/// </para>
/// </summary>
internal static class RegexPatternCollector
{
    // AsYouTypeFormatter.AttemptToExtractIdd() always builds
    //   PhoneRegex.Get("\\" + PhoneNumberUtil.PLUS_SIGN + "|" + currentMetadata.InternationalPrefix)
    // PhoneNumberUtil.PLUS_SIGN is the constant char '+'; PhoneNumberUtil.cs isn't source-linked
    // into this project (see the csproj comment), so the composed prefix is reproduced literally
    // here rather than referencing that constant.
    private const string IddPrefixPrefix = "\\+|";

    internal static int BuildRegexPatternList(string resourcesDir, string outputFile)
    {
        var phoneXml = Path.Combine(resourcesDir, "PhoneNumberMetadata.xml");
        var shortXml = Path.Combine(resourcesDir, "ShortNumberMetadata.xml");
        var altXml = Path.Combine(resourcesDir, "PhoneNumberAlternateFormats.xml");

        foreach (var f in new[] { phoneXml, shortXml, altXml })
            if (!File.Exists(f))
                throw new FileNotFoundException($"Input file not found: {f}", f);

        if (IsUpToDate(outputFile, phoneXml, shortXml, altXml))
            return 0;

        var patterns = new SortedSet<string>(StringComparer.Ordinal);
        CollectPatterns(phoneXml, isShortNumberMetadata: false, isAlternateFormatsMetadata: false, patterns);
        CollectPatterns(shortXml, isShortNumberMetadata: true, isAlternateFormatsMetadata: false, patterns);
        CollectPatterns(altXml, isShortNumberMetadata: false, isAlternateFormatsMetadata: true, patterns);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile))!);
        using (var gz = new GZipStream(File.Create(outputFile), CompressionLevel.SmallestSize))
            BuildPrefixMapFromBin.WriteRegexPatternList(gz, patterns);

        Console.Out.WriteLine(
            $"PhoneNumbers.MetadataBuilder: wrote {patterns.Count} known regex pattern(s) to {outputFile}");
        return 0;
    }

    private static bool IsUpToDate(string outputFile, params string[] inputs)
    {
        if (!File.Exists(outputFile)) return false;
        var outputMTime = File.GetLastWriteTimeUtc(outputFile);
        foreach (var input in inputs)
            if (File.GetLastWriteTimeUtc(input) > outputMTime)
                return false;
        return true;
    }

    private static void CollectPatterns(
        string xmlPath, bool isShortNumberMetadata, bool isAlternateFormatsMetadata, ISet<string> patterns)
    {
        using var input = File.OpenRead(xmlPath);
        var metadataList = BuildMetadataFromXml.BuildPhoneMetadataFromStream(
            input,
            liteBuild: false,
            specialBuild: false,
            isShortNumberMetadata: isShortNumberMetadata,
            isAlternateFormatsMetadata: isAlternateFormatsMetadata);

        foreach (var metadata in metadataList)
            CollectPatternsFromMetadata(metadata, patterns);
    }

    private static void CollectPatternsFromMetadata(PhoneMetadata metadata, ISet<string> patterns)
    {
        AddDesc(metadata.GeneralDesc, patterns);
        AddDesc(metadata.FixedLine, patterns);
        AddDesc(metadata.Mobile, patterns);
        AddDesc(metadata.TollFree, patterns);
        AddDesc(metadata.PremiumRate, patterns);
        AddDesc(metadata.SharedCost, patterns);
        AddDesc(metadata.PersonalNumber, patterns);
        AddDesc(metadata.Voip, patterns);
        AddDesc(metadata.Pager, patterns);
        AddDesc(metadata.Uan, patterns);
        AddDesc(metadata.Emergency, patterns);
        AddDesc(metadata.Voicemail, patterns);
        AddDesc(metadata.ShortCode, patterns);
        AddDesc(metadata.StandardRate, patterns);
        AddDesc(metadata.CarrierSpecific, patterns);
        AddDesc(metadata.SmsServices, patterns);
        AddDesc(metadata.NoInternationalDialling, patterns);

        // Phonemetadata.cs: MatchNationalPrefixForParsing / MatchNationalPrefixLengthForParsing.
        if (metadata.HasNationalPrefixForParsing)
            patterns.Add(metadata.NationalPrefixForParsing);

        // Phonemetadata.cs: IsMatchLeadingDigits.
        if (metadata.HasLeadingDigits)
            patterns.Add(metadata.LeadingDigits);

        // PhoneNumberUtil.MaybeExtractCountryCode -> MaybeStripInternationalPrefixAndNormalize
        // passes defaultRegionMetadata.InternationalPrefix verbatim.
        if (metadata.HasInternationalPrefix)
            patterns.Add(metadata.InternationalPrefix);

        // AsYouTypeFormatter.AttemptToExtractIdd() always composes this pattern (InternationalPrefix
        // defaults to "" when absent), regardless of HasInternationalPrefix.
        patterns.Add(IddPrefixPrefix + metadata.InternationalPrefix);

        foreach (var nf in metadata.NumberFormatList) AddNumberFormat(nf, patterns);
        foreach (var nf in metadata.IntlNumberFormatList) AddNumberFormat(nf, patterns);
    }

    private static void AddDesc(PhoneNumberDesc desc, ISet<string> patterns)
    {
        if (desc is not null && desc.HasNationalNumberPattern)
            patterns.Add(desc.NationalNumberPattern);
    }

    private static void AddNumberFormat(NumberFormat nf, ISet<string> patterns)
    {
        if (nf.HasPattern)
            patterns.Add(nf.Pattern);
        for (var i = 0; i < nf.LeadingDigitsPatternCount; i++)
            patterns.Add(nf.GetLeadingDigitsPattern(i));
    }
}
