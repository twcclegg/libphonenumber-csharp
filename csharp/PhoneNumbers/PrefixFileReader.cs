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
using System.Linq;
using System.Reflection;

namespace PhoneNumbers
{
    /// <summary>
    /// A helper class that handles file loading and prefix-based lookup of phone number mappings.
    /// </summary>
    internal class PrefixFileReader
    {
        private static readonly char[] s_pathSeparators = { '/', '\\' };

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
        private readonly string phoneDataZipFile;
        private readonly Assembly assembly;
        // Normalized entry name -> original FullName, built during construction for O(1) zip lookup.
        private readonly Dictionary<string, string> _zipEntryIndex;

        internal PrefixFileReader(string phonePrefixDataDirectory, Assembly asm = null)
        {
            SortedDictionary<int, HashSet<string>> files;
            asm ??= typeof(PrefixFileReader).Assembly;
            var prefix = asm.GetName().Name + "." + phonePrefixDataDirectory;

            var zipFile = prefix + "zip";
            var zipStream = asm.GetManifestResourceStream(zipFile);

            if (zipStream != null)
            {
                using (zipStream)
                {
                    files = LoadFileNamesFromZip(zipStream, out var entryIndex);
                    _zipEntryIndex = entryIndex;
                }
                phoneDataZipFile = zipFile;
            }
            else
            {
                files = LoadFileNamesFromManifestResources(asm, prefix);
            }

            mappingFileProvider = new MappingFileProvider();
            mappingFileProvider.ReadFileConfigs(files);
            assembly = asm;
            this.phonePrefixDataDirectory = prefix;
            _fileNameFactory = k => mappingFileProvider.GetFileName(k.Item1, k.Item2, k.Item3, k.Item4);
            _areaCodeMapFactory = key => new Lazy<AreaCodeMap>(() => LoadAreaCodeMapFromFile(key));
        }

        // For zipped data: entries follow the pattern "lang/cc.txt".
        private static SortedDictionary<int, HashSet<string>> LoadFileNamesFromZip(
            Stream zipStream, out Dictionary<string, string> entryIndex)
        {
            var files = new SortedDictionary<int, HashSet<string>>();
            entryIndex = new Dictionary<string, string>(StringComparer.Ordinal);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Name))
                    continue;

                entryIndex[entry.FullName.Replace("/", ".").Replace("\\", ".")] = entry.FullName;

                var pathParts = entry.FullName.Split(s_pathSeparators, StringSplitOptions.RemoveEmptyEntries);
                if (pathParts.Length < 2)
                    continue;

                var language = pathParts[0];
                var ccPart = pathParts[pathParts.Length - 1].Split('.')[0];
                if (!int.TryParse(ccPart, out var country))
                    continue;

                if (!files.TryGetValue(country, out var languages))
                    files[country] = languages = new HashSet<string>();
                languages.Add(language);
            }
            return files;
        }

        // For unzipped data: resources are "{AssemblyName}.{prefix}{lang}.{cc}.txt".
        private static SortedDictionary<int, HashSet<string>> LoadFileNamesFromManifestResources(
            Assembly asm, string prefix)
        {
            var files = new SortedDictionary<int, HashSet<string>>();
            var names = asm.GetManifestResourceNames()
                .Where(n => n.StartsWith(prefix, StringComparison.Ordinal));
            foreach (var n in names)
            {
                // filePart e.g. "en.44.txt" or "zh_Hant.852.txt"
                var filePart = n.Substring(prefix.Length);
                var parts = filePart.Split('.');
                // Minimum: [lang, cc, "txt"] => length 3
                if (parts.Length < 3)
                    continue;

                // Last segment is "txt", second-to-last is the country calling code.
                // Everything before is the language (joined with '_' to reconstruct "zh_Hant" etc.)
                var ccIdx = parts.Length - 2;
                if (!int.TryParse(parts[ccIdx], out var country))
                    continue;

                var lang = string.Join("_", parts.Take(ccIdx));
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
            // As the NANPA data is split into multiple files covering 3-digit areas, use a phone number
            // prefix of 4 digits for NANPA instead, e.g. 1650.
            var phonePrefix = countryCallingCode != 1
                ? countryCallingCode
                : (int)(1000 + number.NationalNumber / 10000000);
            var phonePrefixDescriptions = GetPhonePrefixDescriptions(phonePrefix, lang, script, region);
            var description = phonePrefixDescriptions?.Lookup(number);
            if (string.IsNullOrEmpty(description) && MayFallBackToEnglish(lang))
            {
                var defaultMap = GetPhonePrefixDescriptions(phonePrefix, "en", "", "");
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
            var fp = phoneDataZipFile != null
                ? GetManifestZipFileStream(assembly, phoneDataZipFile, fileName, _zipEntryIndex)
                : GetManifestFileStream(assembly, phonePrefixDataDirectory, fileName);

            using (fp)
            {
                return AreaCodeParser.ParseAreaCodeMap(fp);
            }
        }

        private static Stream GetManifestFileStream(Assembly asm, string phonePrefixDataDirectory, string fileName)
        {
            return asm.GetManifestResourceStream(phonePrefixDataDirectory + fileName);
        }

        private static Stream GetManifestZipFileStream(Assembly asm, string phoneDataZipFile, string fileName,
            Dictionary<string, string> entryIndex)
        {
            using var zipStream = asm.GetManifestResourceStream(phoneDataZipFile);
            if (zipStream == null)
                throw new InvalidOperationException("Manifest zip file stream was null.");

            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var entry = entryIndex.TryGetValue(fileName, out var originalName)
                ? archive.GetEntry(originalName)
                : archive.Entries.First(p => p.FullName.Replace("/", ".").Replace("\\", ".") == fileName);
            if (entry == null)
                throw new InvalidOperationException($"Entry '{fileName}' not found in zip.");
            using var entryStream = entry.Open();
            var fileStream = new MemoryStream();
            entryStream.CopyTo(fileStream);
            fileStream.Position = 0;
            return fileStream;
        }
    }
}
