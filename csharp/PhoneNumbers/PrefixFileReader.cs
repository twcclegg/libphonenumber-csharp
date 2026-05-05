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
using System.Linq;
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
        private readonly string phonePrefixDataDirectory;
        private readonly Assembly assembly;

        internal PrefixFileReader(string phonePrefixDataDirectory, Assembly asm = null)
        {
            asm ??= typeof(PrefixFileReader).Assembly;
            var prefix = asm.GetName().Name + "." + phonePrefixDataDirectory;
            var files = LoadFileNamesFromManifestResources(asm, prefix);
            mappingFileProvider = new MappingFileProvider();
            mappingFileProvider.ReadFileConfigs(files);
            assembly = asm;
            this.phonePrefixDataDirectory = prefix;
            _fileNameFactory = k => mappingFileProvider.GetFileName(k.Item1, k.Item2, k.Item3, k.Item4);
            _areaCodeMapFactory = key => new Lazy<AreaCodeMap>(() => LoadAreaCodeMapFromFile(key));
        }

        // Resources follow the pattern "{AssemblyName}.{prefix}{lang}.{cc}"
        // e.g. "PhoneNumbers.carrier.en.1" or "PhoneNumbers.Test.carrier.zh_Hant.852".
        private static SortedDictionary<int, HashSet<string>> LoadFileNamesFromManifestResources(
            Assembly asm, string prefix)
        {
            var files = new SortedDictionary<int, HashSet<string>>();
            var names = asm.GetManifestResourceNames()
                .Where(n => n.StartsWith(prefix, StringComparison.Ordinal));
            foreach (var n in names)
            {
                // filePart e.g. "en.44" or "zh_Hant.852"
                var filePart = n.Substring(prefix.Length);
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
            !lang.Equals("zh") && !lang.Equals("ja") && !lang.Equals("ko");

        private AreaCodeMap GetPhonePrefixDescriptions(int prefixMapKey, string language, string script, string region)
        {
            var fileName = _fileNameCache.GetOrAdd((prefixMapKey, language, script, region), _fileNameFactory);
            if (fileName.Length == 0)
                return null;

            return availablePhonePrefixMaps.GetOrAdd(fileName, _areaCodeMapFactory).Value;
        }

        private AreaCodeMap LoadAreaCodeMapFromFile(string fileName)
        {
            var resName = phonePrefixDataDirectory + fileName;
            using var fp = assembly.GetManifestResourceStream(resName)
                ?? throw new MissingMetadataException(
                    $"Carrier resource '{resName}' not found on assembly '{assembly.GetName().Name}'.");

            var sortedMap = BuildPrefixMapFromBin.ReadAreaCodeMap(fp);
            var areaCodeMap = new AreaCodeMap();
            areaCodeMap.ReadAreaCodeMap(sortedMap);
            return areaCodeMap;
        }
    }
}
