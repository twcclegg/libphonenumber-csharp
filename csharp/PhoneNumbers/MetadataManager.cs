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
using System.Reflection;

namespace PhoneNumbers
{
    /**
    * Class encapsulating loading of PhoneNumber Metadata information. Currently this is used only for
    * additional data files such as PhoneNumberAlternateFormats, but in the future it is envisaged it
    * would handle the main metadata file (PhoneNumberMetaData.xml) as well.
    *
    * @author Lara Rennie
    */
    public class MetadataManager
    {
        internal const string AlternateFormatsFilePrefix = "PhoneNumberAlternateFormats.xml";

        internal const string ShortNumberMetadataFilePrefix = "ShortNumberMetadata.xml";

        private static readonly Dictionary<int?, PhoneMetadata> CallingCodeToAlternateFormatsMap =
            new Dictionary<int?, PhoneMetadata>();

        // A set of which country calling codes there are alternate format data for. If the set has an
        // entry for a code, then there should be data for that code linked into the resources.
        private static readonly Dictionary<int?, List<string>> CountryCodeSet =
            BuildMetadataFromXml.GetCountryCodeToRegionCodeMap(AlternateFormatsFilePrefix);


        // A mapping from a region code to the short number metadata for that region code.
        private static readonly Dictionary<string, PhoneMetadata> ShortNumberMetadataMap =
            new Dictionary<string, PhoneMetadata>();

        // The set of region codes for which there are short number metadata. For every region code in
        // this set there should be metadata linked into the resources.
        private static readonly HashSet<string> ShortNumberMetadataRegionCodes =
            ShortNumbersRegionCodeSet.RegionCodeSet;

        private MetadataManager()
        {
        }

        private static void LoadAlternateFormatsMedataFromFile(string filePrefix)
        {
#if (NET35 || NET40)
            var asm = Assembly.GetExecutingAssembly();
#else
            var asm = typeof(MetadataManager).GetTypeInfo().Assembly;
#endif
            var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(filePrefix)) ?? "missing";
            var meta = BuildMetadataFromXml.BuildPhoneMetadataCollection(name, false, false, false, true); // todo lite/special build
            foreach (var m in meta.Metadata)
            {
                CallingCodeToAlternateFormatsMap[m.CountryCode] = m;
            }
        }
        private static void LoadShortNumberMedataFromFile(string filePrefix)
        {
#if (NET35 || NET40)
            var asm = Assembly.GetExecutingAssembly();
#else
            var asm = typeof(MetadataManager).GetTypeInfo().Assembly;
#endif
            var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(filePrefix)) ?? "missing";
            var meta = BuildMetadataFromXml.BuildPhoneMetadataCollection(name, false, false, true, false); // todo lite/special build
            foreach (var m in meta.Metadata)
            {
                ShortNumberMetadataMap[m.Id] = m;
            }
        }

        public static PhoneMetadata GetAlternateFormatsForCountry(int? countryCallingCode)
        {
            lock(CallingCodeToAlternateFormatsMap)
            {
                if(!CountryCodeSet.ContainsKey(countryCallingCode))
                    return null;
                if(!CallingCodeToAlternateFormatsMap.ContainsKey(countryCallingCode))
                    LoadAlternateFormatsMedataFromFile(AlternateFormatsFilePrefix);
                return CallingCodeToAlternateFormatsMap.ContainsKey(countryCallingCode)
                    ? CallingCodeToAlternateFormatsMap[countryCallingCode]
                    : null;
            }
        }

        internal static PhoneMetadata GetShortNumberMetadataForRegion(string regionCode)
        {
            lock (ShortNumberMetadataMap)
            {

                if (!ShortNumberMetadataRegionCodes.Contains(regionCode))
                    return null;

                if (!ShortNumberMetadataMap.ContainsKey(regionCode))
                    LoadShortNumberMedataFromFile(ShortNumberMetadataFilePrefix);

                return ShortNumberMetadataMap.ContainsKey(regionCode)
                    ? ShortNumberMetadataMap[regionCode]
                    : null;
            }
        }

        internal static HashSet<string> GetSupportedShortNumberRegions()
        {
            return ShortNumberMetadataRegionCodes;
        }


    }
}
