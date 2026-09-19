#nullable disable
/*
 * Copyright (C) 2026 The Libphonenumber Authors
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
using System.Linq;

namespace PhoneNumbers
{
    /// <summary>
    /// Country names by language, read one country at a time from the per-country binary files
    /// generated at build time by <c>PhoneNumbers.MetadataBuilder</c> from
    /// <c>resources/locale/country_names.txt</c>.
    /// </summary>
    /// <remarks>
    /// Callers want one country's names, so only that country is read and cached. The whole table
    /// is roughly 250 countries by 190 languages, which is why <see cref="LocaleData"/> - which
    /// exposes all of it at once - is not on this path.
    /// </remarks>
    internal static class LocaleNames
    {
        internal const string PackResourceName = "PhoneNumbers.locale.pack";

        private static readonly Assembly Assembly = typeof(LocaleNames).GetTypeInfo().Assembly;

        /// <summary>
        /// The whole data set, one embedded resource, read on first use. Null when the build opted
        /// out of the locale data and the trimmer removed it.
        /// </summary>
        private static readonly Lazy<ResourcePack> Pack =
            new Lazy<ResourcePack>(() => ResourcePack.FromAssembly(Assembly, PackResourceName), true);

        /// <summary>
        /// Countries with no entry cache a null, so a miss costs one dictionary lookup rather than
        /// a manifest probe every time. Not every phone-metadata region is an ISO country - AC and
        /// XK both reach here - so misses are expected rather than exceptional.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Cache =
            new ConcurrentDictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        private static readonly Func<string, Dictionary<string, string>> LoadFactory = Load;

        /// <summary>
        /// Returns the language-to-name map for a country, or null when the country has no entry.
        /// Names beginning with '*' point at another language in the same map; resolving that is
        /// the caller's job, as it was when this data was a single generated dictionary.
        /// </summary>
        internal static Dictionary<string, string> ForCountry(string country) =>
            country is null ? null : Cache.GetOrAdd(country, LoadFactory);

        private static Dictionary<string, string> Load(string country)
        {
            var pack = Pack.Value ?? throw PhoneNumbersFeatures.LocaleDataTrimmed();
            var entry = pack.Read(country);
            if (entry is null)
                return null;

            using var raw = new MemoryStream(entry, writable: false);
            using var gz = new GZipStream(raw, CompressionMode.Decompress);
            return BuildPrefixMapFromBin.ReadLocaleNames(gz);
        }

        /// <summary>
        /// Every country that has an entry. Only <see cref="LocaleData"/> needs this; the lookup
        /// path never enumerates.
        /// </summary>
        internal static IEnumerable<string> SupportedCountries()
        {
            var pack = Pack.Value ?? throw PhoneNumbersFeatures.LocaleDataTrimmed();
            foreach (var name in pack.Names)
            {
                yield return name;
            }
        }
    }
}
