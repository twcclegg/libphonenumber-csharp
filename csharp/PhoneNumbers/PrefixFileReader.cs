#nullable disable
/*
 * Copyright (C) 2013 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace PhoneNumbers
{
    /// <summary>
    /// A helper class that handles file loading and prefix-based lookup of phone number mappings.
    /// </summary>
    internal class PrefixFileReader
    {
        private readonly MappingFileProvider mappingFileProvider;
        private readonly ConcurrentDictionary<string, Lazy<AreaCodeMap>> availablePhonePrefixMaps =
            new ConcurrentDictionary<string, Lazy<AreaCodeMap>>();
        // Caches GetFileName results to avoid StringBuilder allocations on every lookup.
        private readonly ConcurrentDictionary<(int, string, string, string), string> _fileNameCache =
            new ConcurrentDictionary<(int, string, string, string), string>();
        // Pre-allocated delegates to avoid closure re-allocation on every GetOrAdd call.
        private readonly Func<(int, string, string, string), string> _fileNameFactory;
        private readonly Func<string, Lazy<AreaCodeMap>> _areaCodeMapFactory;
        private readonly ResourcePack pack;
        private readonly string packResourceName;

        internal PrefixFileReader(string phonePrefixDataDirectory, Assembly asm = null)
        {
            asm ??= typeof(PrefixFileReader).Assembly;
            // One resource per data set: "PhoneNumbers.geocoding.pack", and for the test
            // assembly's own fixtures "PhoneNumbers.Test.carrier.pack".
            packResourceName = asm.GetName().Name + "." + phonePrefixDataDirectory + "pack";
            pack = ResourcePack.FromAssembly(asm, packResourceName);
            var files = LoadFileNamesFromPack(pack);
            mappingFileProvider = new MappingFileProvider();
            mappingFileProvider.ReadFileConfigs(files);
            _fileNameFactory = k => mappingFileProvider.GetFileName(k.Item1, k.Item2, k.Item3, k.Item4);
            _areaCodeMapFactory = key => new Lazy<AreaCodeMap>(() => LoadAreaCodeMapFromFile(key));
        }

        /// <summary>
        /// True when the data set this reader was constructed for is not in the assembly, i.e. the
        /// build opted out of it and the trimmer removed the resource. Callers check this instead
        /// of discovering it as an empty lookup result.
        /// </summary>
        internal bool IsDataTrimmed => pack is null;

        // Pack entries are named "{lang}.{cc}", e.g. "en.44" or "zh_Hant.852".
        private static SortedDictionary<int, HashSet<string>> LoadFileNamesFromPack(ResourcePack pack)
        {
            var files = new SortedDictionary<int, HashSet<string>>();
            if (pack is null)
                return files;
            foreach (var filePart in pack.Names)
            {
                var parts = filePart.Split('.');
                // Minimum: [lang, cc] => length 2
                if (parts.Length < 2)
                    continue;

                // Last segment is the country calling code; everything before is the language.
                var ccIdx = parts.Length - 1;
                if (!int.TryParse(parts[ccIdx], out var country))
                    continue;

                var lang = string.Join(".", parts, 0, ccIdx);
                if (lang.Length == 0)
                    continue;

                if (!files.TryGetValue(country, out var languages))
                    files[country] = languages = new HashSet<string>();
                languages.Add(lang);
            }
            return files;
        }

        /// <summary>
        /// Returns a text description in the given language for the given phone number.
        /// Falls back to English when no mapping exists for the requested language,
        /// except for Chinese, Japanese, and Korean.
        /// </summary>
        internal string GetDescriptionForNumber(PhoneNumber number, string lang, string script, string region)
        {
            var countryCallingCode = number.CountryCode;
            var phonePrefixDescriptions = GetPhonePrefixDescriptions(countryCallingCode, lang, script, region);
            var description = phonePrefixDescriptions?.Lookup(number);
            if (string.IsNullOrEmpty(description) && MayFallBackToEnglish(lang))
            {
                var defaultMap = GetPhonePrefixDescriptions(countryCallingCode, "en", "", "");
                if (defaultMap == null)
                    return "";
                description = defaultMap.Lookup(number);
            }
            return description ?? "";
        }

        private static bool MayFallBackToEnglish(string lang) =>
            lang is not "zh" and not "ja" and not "ko";

        private AreaCodeMap GetPhonePrefixDescriptions(int prefixMapKey, string language, string script, string region)
        {
            var fileName = _fileNameCache.GetOrAdd((prefixMapKey, language, script, region), _fileNameFactory);
            if (fileName.Length == 0)
                return null;

            return availablePhonePrefixMaps.GetOrAdd(fileName, _areaCodeMapFactory).Value;
        }

        private AreaCodeMap LoadAreaCodeMapFromFile(string fileName)
        {
            var entry = pack?.Read(fileName)
                ?? throw new MissingMetadataException(
                    $"Prefix map entry '{fileName}' not found in resource pack '{packResourceName}'.");
            using var raw = new MemoryStream(entry, writable: false);
            using var fp = new GZipStream(raw, CompressionMode.Decompress);

            var sortedMap = BuildPrefixMapFromBin.ReadAreaCodeMap(fp);
            var areaCodeMap = new AreaCodeMap();
            areaCodeMap.ReadAreaCodeMap(sortedMap);
            return areaCodeMap;
        }
    }
}
