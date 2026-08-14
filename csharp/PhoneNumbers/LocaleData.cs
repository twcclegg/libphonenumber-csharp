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

using System.Collections.Immutable;

namespace PhoneNumbers
{
    /// <summary>
    /// Holds a map from ISO 3166-1 country code (e.g. GB) to a dict. Each dict maps from an
    /// ISO 639-1 language code (e.g. ja) to the country's name in that language.
    /// </summary>
    /// <remarks>
    /// The data lives in per-country binary resources built from
    /// <c>resources/locale/country_names.txt</c>. Reading <see cref="Data"/> materialises every
    /// country at once, so nothing inside this library uses it: the country-name lookup goes
    /// through <see cref="LocaleNames"/>, which reads one country. This type stays for callers
    /// outside the library that were using it before the data moved.
    /// </remarks>
    public class LocaleData
    {
        public static readonly ImmutableDictionary<string, ImmutableDictionary<string, string>> Data = BuildAll();

        private static ImmutableDictionary<string, ImmutableDictionary<string, string>> BuildAll()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, string>>();
            foreach (var country in LocaleNames.SupportedCountries())
            {
                var names = LocaleNames.ForCountry(country);
                if (names != null)
                    builder[country] = names.ToImmutableDictionary();
            }
            return builder.ToImmutable();
        }
    }
}
