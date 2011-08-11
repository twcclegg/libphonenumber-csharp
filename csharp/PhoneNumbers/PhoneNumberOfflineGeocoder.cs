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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PhoneNumbers
{

    public class Locale
    {
        public static readonly Locale ENGLISH = new Locale("en", "GB");
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
            int countryCallingCode, String language, String script, String region)
        {
            String fileName = mappingFileProvider.GetFileName(countryCallingCode, language, script, region);
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
            return (regionCode == null || regionCode.Equals("ZZ"))
                ? "" : new RegionInfo(regionCode).EnglishName;
        }

        /**
        * Returns a text description for the given language code for the given phone number. The
        * description might consist of the name of the country where the phone number is from and/or the
        * name of the geographical area the phone number is from. This method assumes the validity of the
        * number passed in has already been checked.
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
        * Returns a text description for the given language code for the given phone number. The
        * description might consist of the name of the country where the phone number is from and/or the
        * name of the geographical area the phone number is from. This method explictly checkes the
        * validity of the number passed in.
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
         * Returns an area-level text description in the given language for the given phone number.
         *
         * @param number  the phone number for which we want to get a text description
         * @param lang  two-letter lowercase ISO language codes as defined by ISO 639-1
         * @param script  four-letter titlecase (the first letter is uppercase and the rest of the letters
         *     are lowercase) ISO script codes as defined in ISO 15924
         * @param region  two-letter uppercase ISO country codes as defined by ISO 3166-1
         * @return  an area-level text description in the given language for the given phone number, or an
         *     empty string if such a description is not available
         */
        private String GetAreaDescriptionForNumber(
            PhoneNumber number, String lang, String script, String region)
        {
            AreaCodeMap phonePrefixDescriptions =
                GetPhonePrefixDescriptions(number.CountryCode, lang, script, region);
            return (phonePrefixDescriptions != null) ? phonePrefixDescriptions.Lookup(number) : "";
        }
    }
}