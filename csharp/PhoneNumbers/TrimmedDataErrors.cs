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

namespace PhoneNumbers
{
    /// <summary>
    /// What to throw when a data set the build opted out of is used anyway.
    /// <para>
    /// The opt-out itself lives entirely in the build: <c>buildTransitive/</c> maps an MSBuild
    /// property to an <see cref="System.AppContext"/> switch, and <c>ILLink.Substitutions.xml</c>
    /// gates the matching resource removal on that switch. Nothing here reads either.
    /// </para>
    /// <para>
    /// These are returned rather than thrown so the caller writes <c>throw ...</c> and the compiler
    /// sees the path terminate. Callers throw instead of returning an empty result: the geocoder
    /// otherwise answers every query with <c>""</c> and exits cleanly when its data has been
    /// trimmed away, which is a silently wrong answer rather than a missing one.
    /// </para>
    /// <para>
    /// <see cref="MissingMetadataException"/> rather than a bare
    /// <see cref="System.InvalidOperationException"/>: it derives from one, so nothing a caller
    /// catches today stops working, and it is the type <see cref="PhoneNumberToTimeZonesMapper"/>
    /// already threw when this resource was absent.
    /// </para>
    /// </summary>
    internal static class TrimmedDataErrors
    {
        internal static MissingMetadataException GeocodingDataTrimmed() =>
            new MissingMetadataException(
                "The geocoding, carrier and time zone data is not present in this build. If the " +
                "PhoneNumbersIncludeGeocodingData MSBuild property is set to false, remove it or set " +
                "it to true to use PhoneNumberOfflineGeocoder, PhoneNumberToCarrierMapper or " +
                "PhoneNumberToTimeZonesMapper; otherwise the embedded data resources were removed " +
                "by something other than that property.");

        internal static MissingMetadataException LocaleDataTrimmed() =>
            new MissingMetadataException(
                "The country-name data is not present in this build. If the " +
                "PhoneNumbersIncludeLocaleNameData MSBuild property is set to false, remove it or " +
                "set it to true to use Locale.GetDisplayCountry or LocaleData; otherwise the " +
                "embedded data resource was removed by something other than that property.");
    }
}
