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

using System.Collections.Generic;
using System.Linq;

namespace PhoneNumbers
{
    /**
    * Class encapsulating loading of PhoneNumber Metadata information. Currently this is used only for
    * additional data files such as PhoneNumberAlternateFormats, but in the future it is envisaged it
    * would handle the main metadata file (PhoneNumberMetadata.xml) as well.
    *
    * @author Lara Rennie
    */
    public static class MetadataManager
    {
        private static class AlternateFormats
        {
            public static readonly Dictionary<int, PhoneMetadata> Map =
                BuildMetadataFromXml.BuildPhoneMetadata("PhoneNumberAlternateFormats.xml", isAlternateFormatsMetadata: true).ToDictionary(m => m.CountryCode);
        }

        private static class ShortNumber
        {
            // A mapping from a region code to the short number metadata for that region code.
            public static readonly Dictionary<string, PhoneMetadata> MetadataMap =
                BuildMetadataFromXml.BuildPhoneMetadata("ShortNumberMetadata.xml", isShortNumberMetadata: true).ToDictionary(m => m.Id);
        }

#if NET6_0_OR_GREATER
        public static PhoneMetadata? GetAlternateFormatsForCountry(int countryCallingCode)
#else
        public static PhoneMetadata GetAlternateFormatsForCountry(int countryCallingCode)
#endif
        {
            return AlternateFormats.Map.TryGetValue(countryCallingCode, out var metadata) ? metadata : null;
        }

#if NET6_0_OR_GREATER
        internal static PhoneMetadata? GetShortNumberMetadataForRegion(string regionCode)
#else
        internal static PhoneMetadata GetShortNumberMetadataForRegion(string regionCode)
#endif
        {
            if (!ShortNumbersRegionCodeSet.RegionCodeSet.Contains(regionCode))
                return null;
            return ShortNumber.MetadataMap.TryGetValue(regionCode, out var metadata) ? metadata : null;
        }
    }
}
