/*
 * Copyright (C) 2011 Google Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Text;

namespace PhoneNumbers
{

    public class Locale
    {
        public static readonly Locale ENGLISH = new Locale("en", "GB");
        public static readonly Locale FRENCH = new Locale("fr", "FR");
        public static readonly Locale GERMAN = new Locale("de", "DE");
        public static readonly Locale ITALIAN = new Locale("it", "IT");
        public static readonly Locale KOREAN = new Locale("ko", "KR");
        public static readonly Locale SIMPLIFIED_CHINESE = new Locale("zh", "CN");

        public readonly String Language;
        public readonly String Country;

        public Locale(String language, String countryCode)
        {
            Language = language;
            Country = countryCode;
        }

        public String GetDisplayCountry(String language)
        {
            if(String.IsNullOrEmpty(Country))
                return "";
            var name = GetCountryName(Country, language);
            if(name != null)
                return name;
            var lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
            if(lang != language)
            {
                name = GetCountryName(Country, lang);
                if(name != null)
                    return name;
            }
            if(language != "en" && lang != "en")
            {
                name = GetCountryName(Country, "en");
                if(name != null)
                    return name;
            }
            name = GetCountryName(Country, Language);
            return name ?? "";
        }

        private String GetCountryName(String country, String language)
        {
            var names = LocaleData.Data[Country];
            String name;
            if(!names.TryGetValue(language, out name))
                return null;
            if(name.Length > 0 && name[0] == '*')
                return names[name.Substring(1)];
            return name;
        }
    }
    /**
     * An offline geocoder which provides geographical information related to a phone number.
     *
     * @author Shaopeng Jia
     */
    public class PhoneNumberOfflineGeocoder
    {
        private static PhoneNumberOfflineGeocoder instance = null;
        private const String MAPPING_DATA_DIRECTORY = "res.";
        private static Object thisLock = new Object();

        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
        private readonly String phonePrefixDataDirectory;

        // The mappingFileProvider knows for which combination of countryCallingCode and language a phone
        // prefix mapping file is available in the file system, so that a file can be loaded when needed.
        private MappingFileProvider mappingFileProvider = new MappingFileProvider();

        // A mapping from countryCallingCode_lang to the corresponding phone prefix map that has been
        // loaded.
        private Dictionary<String, AreaCodeMap> availablePhonePrefixMaps = new Dictionary<String, AreaCodeMap>();

        // @VisibleForTesting
        public PhoneNumberOfflineGeocoder(String phonePrefixDataDirectory)
        {
            this.phonePrefixDataDirectory = phonePrefixDataDirectory;
            LoadMappingFileProvider();
        }

        private void LoadMappingFileProvider()
        {
            var files = new SortedDictionary<int, HashSet<String>>();
            var asm = Assembly.GetExecutingAssembly();
            var allNames = asm.GetManifestResourceNames();
            var prefix = asm.GetName().Name + "." + phonePrefixDataDirectory;
            var names = allNames.Where(n => n.StartsWith(prefix));
            foreach (var n in names)
            {
                var name = n.Substring(prefix.Length);
                var pos = name.IndexOf("_");
                var country = int.Parse(name.Substring(0, pos));
                var language = name.Substring(pos + 1);
                if (!files.ContainsKey(country))
                    files[country] = new HashSet<String>();
                files[country].Add(language);
            }
            mappingFileProvider = new MappingFileProvider();
            mappingFileProvider.ReadFileConfigs(files);
        }

        private AreaCodeMap GetPhonePrefixDescriptions(
            int prefixMapKey, String language, String script, String region)
        {
            String fileName = mappingFileProvider.GetFileName(prefixMapKey, language, script, region);
            if (fileName.Length == 0)
            {
                return null;
            }
            if (!availablePhonePrefixMaps.ContainsKey(fileName))
            {
                LoadAreaCodeMapFromFile(fileName);
            }
            AreaCodeMap map;
            return availablePhonePrefixMaps.TryGetValue(fileName, out map) ? map : null;
        }

        private void LoadAreaCodeMapFromFile(String fileName)
        {
            var asm = Assembly.GetExecutingAssembly();
            var prefix = asm.GetName().Name + "." + phonePrefixDataDirectory;
            var resName = prefix + fileName;
            using (var fp = asm.GetManifestResourceStream(resName))
            {
                var areaCodeMap = AreaCodeParser.ParseAreaCodeMap(fp);
                availablePhonePrefixMaps[fileName] = areaCodeMap;
            }
        }

        /**
         * Gets a {@link PhoneNumberOfflineGeocoder} instance to carry out international phone number
         * geocoding.
         *
         * <p> The {@link PhoneNumberOfflineGeocoder} is implemented as a singleton. Therefore, calling
         * this method multiple times will only result in one instance being created.
         *
         * @return  a {@link PhoneNumberOfflineGeocoder} instance
         */
        public static PhoneNumberOfflineGeocoder GetInstance()
        {
            lock (thisLock)
            {
                if (instance == null)
                {
                    instance = new PhoneNumberOfflineGeocoder(MAPPING_DATA_DIRECTORY);
                }
                return instance;
            }
        }

        /**
         * Preload the data file for the given language and country calling code, so that a future lookup
         * for this language and country calling code will not incur any file loading.
         *
         * @param locale  specifies the language of the data file to load
         * @param countryCallingCode   specifies the country calling code of phone numbers that are
         *     contained by the file to be loaded
         */
        public void LoadDataFile(Locale locale, int countryCallingCode)
        {
            instance.GetPhonePrefixDescriptions(countryCallingCode, locale.Language, "",
                locale.Country);
        }

        /**
         * Returns the customary display name in the given language for the given territory the phone
         * number is from.
         */
        private String GetCountryNameForNumber(PhoneNumber number, Locale language)
        {
            String regionCode = phoneUtil.GetRegionCodeForNumber(number);
            return GetRegionDisplayName(regionCode, language);
        }

        /**
        * Returns the customary display name in the given language for the given region.
        */
        private String GetRegionDisplayName(String regionCode, Locale language)
        {
            return (regionCode == null || regionCode.Equals("ZZ"))
                ? "" : new Locale("", regionCode).GetDisplayCountry(language.Language);
        }

        /**
        * Returns a text description for the given phone number, in the language provided. The
        * description might consist of the name of the country where the phone number is from, or the
        * name of the geographical area the phone number is from if more detailed information is
        * available.
        *
        * <p>This method assumes the validity of the number passed in has already been checked.
        *
        * @param number  a valid phone number for which we want to get a text description
        * @param languageCode  the language code for which the description should be written
        * @return  a text description for the given language code for the given phone number
        */
        public String GetDescriptionForValidNumber(PhoneNumber number, Locale languageCode)
        {
            String langStr = languageCode.Language;
            String scriptStr = "";  // No script is specified
            String regionStr = languageCode.Country;

            String areaDescription =
                GetAreaDescriptionForNumber(number, langStr, scriptStr, regionStr);
            return (areaDescription.Length > 0)
                ? areaDescription : GetCountryNameForNumber(number, languageCode);
        }

        /**
        * As per {@link #getDescriptionForValidNumber(PhoneNumber, Locale)} but also considers the
        * region of the user. If the phone number is from the same region as the user, only a lower-level
        * description will be returned, if one exists. Otherwise, the phone number's region will be
        * returned, with optionally some more detailed information.
        *
        * <p>For example, for a user from the region "US" (United States), we would show "Mountain View,
        * CA" for a particular number, omitting the United States from the description. For a user from
        * the United Kingdom (region "GB"), for the same number we may show "Mountain View, CA, United
        * States" or even just "United States".
        *
        * <p>This method assumes the validity of the number passed in has already been checked.
        *
        * @param number  the phone number for which we want to get a text description
        * @param languageCode  the language code for which the description should be written
        * @param userRegion  the region code for a given user. This region will be omitted from the
        *     description if the phone number comes from this region. It is a two-letter uppercase ISO
        *     country code as defined by ISO 3166-1.
        * @return  a text description for the given language code for the given phone number, or empty
        *     string if the number passed in is invalid
        */
        public String GetDescriptionForValidNumber(PhoneNumber number, Locale languageCode,
            String userRegion)
        {
            // If the user region matches the number's region, then we just show the lower-level
            // description, if one exists - if no description exists, we will show the region(country) name
            // for the number.
            String regionCode = phoneUtil.GetRegionCodeForNumber(number);
            if (userRegion.Equals(regionCode))
            {
                return GetDescriptionForValidNumber(number, languageCode);
            }
            // Otherwise, we just show the region(country) name for now.
            return GetRegionDisplayName(regionCode, languageCode);
            // TODO: Concatenate the lower-level and country-name information in an appropriate
            // way for each language.
        }

        /**
        * As per {@link #getDescriptionForValidNumber(PhoneNumber, Locale)} but explicitly checks
        * the validity of the number passed in.
        *
        * @param number  the phone number for which we want to get a text description
        * @param languageCode  the language code for which the description should be written
        * @return  a text description for the given language code for the given phone number, or empty
        *     string if the number passed in is invalid
        */
        public String GetDescriptionForNumber(PhoneNumber number, Locale languageCode)
        {
            if (!phoneUtil.IsValidNumber(number))
                return "";
            return GetDescriptionForValidNumber(number, languageCode);
        }

        /**
        * As per {@link #getDescriptionForValidNumber(PhoneNumber, Locale, String)} but
        * explicitly checks the validity of the number passed in.
        *
        * @param number  the phone number for which we want to get a text description
        * @param languageCode  the language code for which the description should be written
        * @param userRegion  the region code for a given user. This region will be omitted from the
        *     description if the phone number comes from this region. It is a two-letter uppercase ISO
        *     country code as defined by ISO 3166-1.
        * @return  a text description for the given language code for the given phone number, or empty
        *     string if the number passed in is invalid
        */
        public String GetDescriptionForNumber(PhoneNumber number, Locale languageCode,
            String userRegion)
        {
            if (!phoneUtil.IsValidNumber(number))
            {
                return "";
            }
            return GetDescriptionForValidNumber(number, languageCode, userRegion);
        }

        private String GetAreaDescriptionForNumber(
            PhoneNumber number, String lang, String script, String region)
        {
            int countryCallingCode = number.CountryCode;
            // As the NANPA data is split into multiple files covering 3-digit areas, use a phone number
            // prefix of 4 digits for NANPA instead, e.g. 1650.
            //int phonePrefix = (countryCallingCode != 1) ?
            //    countryCallingCode : (1000 + (int) (number.NationalNumber / 10000000));
            int phonePrefix = countryCallingCode;

            AreaCodeMap phonePrefixDescriptions =
                GetPhonePrefixDescriptions(phonePrefix, lang, script, region);
            String description = (phonePrefixDescriptions != null)
                ? phonePrefixDescriptions.Lookup(number)
                : null;
            // When a location is not available in the requested language, fall back to English.
            if ((description == null || description.Length == 0) && MayFallBackToEnglish(lang))
            {
                AreaCodeMap defaultMap = GetPhonePrefixDescriptions(phonePrefix, "en", "", "");
                if (defaultMap == null)
                {
                    return "";
                }
                description = defaultMap.Lookup(number);
            }
            return description != null ? description : "";
        }

        private bool MayFallBackToEnglish(String lang)
        {
            // Don't fall back to English if the requested language is among the following:
            // - Chinese
            // - Japanese
            // - Korean
            return !lang.Equals("zh") && !lang.Equals("ja") && !lang.Equals("ko");
        }

    }
}