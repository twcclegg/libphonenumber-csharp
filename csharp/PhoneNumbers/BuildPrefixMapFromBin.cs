#nullable disable
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
using System.Text;

namespace PhoneNumbers
{
    /// <summary>
    /// Binary serializer/deserializer for the prefix maps that back
    /// <see cref="PhoneNumberOfflineGeocoder"/> (phone-prefix → location-string) and
    /// <see cref="PhoneNumberToTimeZonesMapper"/> (phone-prefix → list of IANA tz names). Replaces
    /// the runtime text/zip parsing path with a direct read of pre-serialized data.
    /// </summary>
    /// <remarks>
    /// Two distinct file formats — one for area-code maps (int prefix, string description) and one
    /// for timezone maps (long prefix, string[] descriptions). Each gets its own magic so the
    /// reader can fail loudly if a caller passes the wrong stream.
    /// </remarks>
    public static class BuildPrefixMapFromBin
    {
        // Magic + version layout matches BuildMetadataFromBin so future tooling can sniff a stream.
        internal const int AreaCodeMapMagic = 0x504E4143; // 'P','N','A','C'
        internal const int TimezoneMapMagic = 0x504E5454; // 'P','N','T','T'
        internal const int LocaleNamesMagic = 0x504E4C4E; // 'P','N','L','N'
        internal const int RegexPatternListMagic = 0x504E5250; // 'P','N','R','P'
        internal const byte FormatVersion = 1;

        // --- Area code map (int → string) ---------------------------------------------

        public static void WriteAreaCodeMap(Stream stream, SortedDictionary<int, string> map)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(AreaCodeMapMagic);
            writer.Write(FormatVersion);
            writer.Write(map.Count);
            foreach (var entry in map)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value ?? "");
            }
        }

        public static SortedDictionary<int, string> ReadAreaCodeMap(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var magic = reader.ReadInt32();
            if (magic != AreaCodeMapMagic)
                throw new InvalidDataException(
                    $"Unexpected area-code-map magic 0x{magic:X8} (expected 0x{AreaCodeMapMagic:X8}).");
            var version = reader.ReadByte();
            if (version != FormatVersion)
                throw new InvalidDataException(
                    $"Unsupported area-code-map version {version} (expected {FormatVersion}).");

            var count = reader.ReadInt32();
            var map = new SortedDictionary<int, string>();
            for (var i = 0; i < count; i++)
            {
                var prefix = reader.ReadInt32();
                var description = reader.ReadString();
                map[prefix] = description;
            }
            return map;
        }

        // --- Timezone map (long → string[]) -------------------------------------------

        public static void WriteTimezoneMap(Stream stream, SortedDictionary<long, string[]> map)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(TimezoneMapMagic);
            writer.Write(FormatVersion);
            writer.Write(map.Count);
            foreach (var entry in map)
            {
                writer.Write(entry.Key);
                var zones = entry.Value ?? [];
                writer.Write(zones.Length);
                for (var i = 0; i < zones.Length; i++)
                    writer.Write(zones[i] ?? "");
            }
        }

        public static SortedDictionary<long, string[]> ReadTimezoneMap(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var magic = reader.ReadInt32();
            if (magic != TimezoneMapMagic)
                throw new InvalidDataException(
                    $"Unexpected timezone-map magic 0x{magic:X8} (expected 0x{TimezoneMapMagic:X8}).");
            var version = reader.ReadByte();
            if (version != FormatVersion)
                throw new InvalidDataException(
                    $"Unsupported timezone-map version {version} (expected {FormatVersion}).");

            var count = reader.ReadInt32();
            var map = new SortedDictionary<long, string[]>();
            for (var i = 0; i < count; i++)
            {
                var prefix = reader.ReadInt64();
                var zoneCount = reader.ReadInt32();
                var zones = new string[zoneCount];
                for (var j = 0; j < zoneCount; j++)
                    zones[j] = reader.ReadString();
                map[prefix] = zones;
            }
            return map;
        }

        // --- Locale country names (language → name), one file per country ----------------

        public static void WriteLocaleNames(Stream stream, SortedDictionary<string, string> names)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(LocaleNamesMagic);
            writer.Write(FormatVersion);
            writer.Write(names.Count);
            foreach (var entry in names)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value ?? "");
            }
        }

        public static Dictionary<string, string> ReadLocaleNames(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var magic = reader.ReadInt32();
            if (magic != LocaleNamesMagic)
                throw new InvalidDataException(
                    $"Unexpected locale-names magic 0x{magic:X8} (expected 0x{LocaleNamesMagic:X8}).");
            var version = reader.ReadByte();
            if (version != FormatVersion)
                throw new InvalidDataException(
                    $"Unsupported locale-names version {version} (expected {FormatVersion}).");

            var count = reader.ReadInt32();
            // Ordinal comparison to match the lookups this feeds, which pass language codes
            // through unchanged from the caller's Locale.
            var names = new Dictionary<string, string>(count, StringComparer.Ordinal);
            for (var i = 0; i < count; i++)
            {
                var language = reader.ReadString();
                names[language] = reader.ReadString();
            }
            return names;
        }

        // --- Regex pattern list (flat set of distinct metadata-derived pattern strings) --

        /// <summary>
        /// Writes the flat, order-independent set of distinct regex pattern strings enumerable at
        /// build time from the shipped metadata -- see RegexPatternCollector in
        /// PhoneNumbers.MetadataBuilder. Consumed at runtime by <see cref="PhoneRegex"/> to
        /// pre-populate its pattern cache with known keys (see <see cref="ReadRegexPatternList"/>);
        /// not itself a regex compile or match -- purely a list of strings.
        /// </summary>
        public static void WriteRegexPatternList(Stream stream, IReadOnlyCollection<string> patterns)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(RegexPatternListMagic);
            writer.Write(FormatVersion);
            writer.Write(patterns.Count);
            foreach (var pattern in patterns)
                writer.Write(pattern);
        }

        public static string[] ReadRegexPatternList(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var magic = reader.ReadInt32();
            if (magic != RegexPatternListMagic)
                throw new InvalidDataException(
                    $"Unexpected regex-pattern-list magic 0x{magic:X8} (expected 0x{RegexPatternListMagic:X8}).");
            var version = reader.ReadByte();
            if (version != FormatVersion)
                throw new InvalidDataException(
                    $"Unsupported regex-pattern-list version {version} (expected {FormatVersion}).");

            var count = reader.ReadInt32();
            var patterns = new string[count];
            for (var i = 0; i < count; i++)
                patterns[i] = reader.ReadString();
            return patterns;
        }
    }
}
