/*
 * Copyright (C) 2012 The Libphonenumber Authors
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

namespace PhoneNumbers
{
    /// <summary>
    /// Class encapsulating loading of PhoneNumber Metadata information for the supplementary data
    /// files: <c>PhoneNumberAlternateFormats</c> (per country calling code) and
    /// <c>ShortNumberMetadata</c> (per region).
    /// </summary>
    /// <remarks>
    /// Switched from eager XML parsing to lazy binary loading via <see cref="MetadataSource"/>:
    /// callers only pay the cost of a region's metadata when they ask for it, and metadata is
    /// served from the build-time-generated binary files embedded in the assembly rather than the
    /// XML files. <para/>
    /// Author: Lara Rennie
    /// </remarks>
    public static class MetadataManager
    {
        private const string AlternateFormatsPrefix = "PhoneNumberAlternateFormats";
        private const string ShortNumberMetadataPrefix = "ShortNumberMetadata";

        private static MetadataSource alternateFormatsSource = CreateDefault(AlternateFormatsPrefix);
        private static MetadataSource shortNumberSource = CreateDefault(ShortNumberMetadataPrefix);

        // The obsolete-and-public EmbeddedResourceMetadataLoader is still this library's own
        // production default; the Obsolete attribute is aimed at external callers, not this.
#pragma warning disable CS0618
        private static MetadataSource CreateDefault(string filePrefix)
            => new MetadataSource(new EmbeddedResourceMetadataLoader(), filePrefix);
#pragma warning restore CS0618

        /// <summary>
        /// Replaces the <see cref="IMetadataLoader"/> used to fetch the supplementary metadata
        /// files (<c>PhoneNumberAlternateFormats</c> and <c>ShortNumberMetadata</c>). Mirrors the
        /// equivalent injection point in Java's <c>DefaultMetadataDependenciesProvider</c> and
        /// pairs with the loader argument accepted by the internal <see cref="PhoneNumberUtil"/>
        /// constructor.
        /// </summary>
        /// <remarks>
        /// Not intended for external use — see the note on <see cref="IMetadataLoader"/>. Exercised
        /// directly by this project's own tests.
        /// </remarks>
        /// <param name="loader">Loader to use for both supplementary metadata file types.</param>
        [Obsolete("Not intended for external use and will become internal in a future release.")]
        public static void SetMetadataLoader(IMetadataLoader loader)
        {
            if (loader is null) throw new ArgumentNullException(nameof(loader));
            alternateFormatsSource = new(loader, AlternateFormatsPrefix);
            shortNumberSource = new(loader, ShortNumberMetadataPrefix);
        }

#if !NETSTANDARD2_0
        public static PhoneMetadata? GetAlternateFormatsForCountry(int countryCallingCode)
#else
        public static PhoneMetadata GetAlternateFormatsForCountry(int countryCallingCode)
#endif
            => alternateFormatsSource.GetMetadataForNonGeographicalRegion(countryCallingCode);

#if !NETSTANDARD2_0
        internal static PhoneMetadata? GetShortNumberMetadataForRegion(string regionCode)
#else
        internal static PhoneMetadata GetShortNumberMetadataForRegion(string regionCode)
#endif
        {
            if (!ShortNumbersRegionCodeSet.RegionCodeSet.Contains(regionCode))
                return null;
            return shortNumberSource.GetMetadataForRegion(regionCode);
        }
    }
}
