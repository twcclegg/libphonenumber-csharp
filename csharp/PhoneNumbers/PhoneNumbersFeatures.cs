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
#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace PhoneNumbers
{
    /// <summary>
    /// Build-time opt-outs for the bundled data sets that are not needed to parse, validate or
    /// format a number.
    /// <para>
    /// Deliberately <c>internal</c>. This port mirrors Java's public API and does not grow a surface
    /// of its own because the port has a packaging problem Java does not: the opt-out is an MSBuild
    /// property, set once in a csproj, never a runtime call. <c>buildTransitive/</c> maps each
    /// property to the <see cref="AppContext"/> switch read here, and
    /// <c>ILLink.Substitutions.xml</c> gates the matching resource removal on the same switch.
    /// </para>
    /// <para>
    /// The switches only remove anything in a trimmed build. An untrimmed consumer that sets them
    /// keeps every data set, because nothing removes embedded resources in that case, and the
    /// library carries on working normally.
    /// </para>
    /// </summary>
    internal static class PhoneNumbersFeatures
    {
        internal const string GeocodingSwitch = "PhoneNumbers.IncludeGeocodingData";
        internal const string LocaleNamesSwitch = "PhoneNumbers.IncludeLocaleNameData";

        /// <summary>
        /// Backs <see cref="PhoneNumberOfflineGeocoder"/>, <see cref="PhoneNumberToCarrierMapper"/>
        /// and <see cref="PhoneNumberToTimeZonesMapper"/>. Defaults to included, so a consumer who
        /// sets nothing gets exactly what they get today.
        /// </summary>
#if NET9_0_OR_GREATER
        [FeatureSwitchDefinition(GeocodingSwitch)]
#endif
        internal static bool IncludeGeocodingData =>
            !AppContext.TryGetSwitch(GeocodingSwitch, out var included) || included;

        /// <summary>
        /// Backs the country-name table behind <c>Locale.GetDisplayCountry</c> and
        /// <see cref="LocaleData"/>. Separate from <see cref="IncludeGeocodingData"/> because it
        /// backs different public API: a caller can want country names without the area
        /// descriptions, which are five times the size.
        /// </summary>
#if NET9_0_OR_GREATER
        [FeatureSwitchDefinition(LocaleNamesSwitch)]
#endif
        internal static bool IncludeLocaleNameData =>
            !AppContext.TryGetSwitch(LocaleNamesSwitch, out var included) || included;

        /// <summary>
        /// Returned rather than thrown so the caller writes <c>throw ...</c> and the compiler sees
        /// the path terminate. Callers throw instead of returning an empty result: the geocoder
        /// otherwise answers every query with <c>""</c> and exits cleanly when its data has been
        /// trimmed away, which is a silently wrong answer rather than a missing one.
        /// <para>
        /// <see cref="MissingMetadataException"/> rather than a bare
        /// <see cref="InvalidOperationException"/>: it derives from one, so nothing a caller
        /// catches today stops working, and it is the type
        /// <see cref="PhoneNumberToTimeZonesMapper"/> already threw when this resource was absent.
        /// </para>
        /// <para>
        /// The message branches on the switch because "the resource is absent" and "you asked for
        /// it to be absent" are different problems. The second is a one-line fix in a csproj; the
        /// first means something removed the resource that was not this feature, and saying "you
        /// set the property to false" to someone who did not would send them looking in the wrong
        /// place.
        /// </para>
        /// </summary>
        internal static MissingMetadataException GeocodingDataTrimmed() =>
            new MissingMetadataException(IncludeGeocodingData
                ? "The geocoding, carrier and timezone data is missing from this build, but the " +
                  "PhoneNumbersIncludeGeocodingData property was not set to false. The embedded " +
                  "resources PhoneNumbers.geocoding.pack, PhoneNumbers.carrier.pack and " +
                  "PhoneNumbers.timezones.map_data.bin appear to have been removed by something else."
                : "The geocoding, carrier and timezone data is not present in this build because the " +
                  "PhoneNumbersIncludeGeocodingData MSBuild property was set to false. Remove that " +
                  "property, or set it to true, to use PhoneNumberOfflineGeocoder, " +
                  "PhoneNumberToCarrierMapper or PhoneNumberToTimeZonesMapper.");

        internal static MissingMetadataException LocaleDataTrimmed() =>
            new MissingMetadataException(IncludeLocaleNameData
                ? "The country-name data is missing from this build, but the " +
                  "PhoneNumbersIncludeLocaleNameData property was not set to false. The embedded " +
                  "resource PhoneNumbers.locale.pack appears to have been removed by something else."
                : "The country-name data is not present in this build because the " +
                  "PhoneNumbersIncludeLocaleNameData MSBuild property was set to false. Remove that " +
                  "property, or set it to true, to use Locale.GetDisplayCountry or LocaleData.");
    }
}
