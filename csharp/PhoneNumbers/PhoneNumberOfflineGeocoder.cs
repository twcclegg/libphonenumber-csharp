#nullable disable
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
using System.Globalization;
using System.Reflection;

namespace PhoneNumbers
{
    public class Locale
    {
        public static readonly Locale English = new("en", "GB");
        public static readonly Locale French = new("fr", "FR");
        public static readonly Locale German = new("de", "DE");
        public static readonly Locale Italian = new("it", "IT");
        public static readonly Locale Korean = new("ko", "KR");
        public static readonly Locale SimplifiedChinese = new("zh", "CN");

        public readonly string Language;
        public readonly string Country;

        public Locale(string language, string countryCode)
        {
            Language = language;
            Country = countryCode;
        }

        public string GetDisplayCountry(string language)
        {
            if (string.IsNullOrEmpty(Country))
                return "";
            var name = GetCountryName(Country, language);
            if (name != null)
                return name;
            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            if (lang != language)
            {
                name = GetCountryName(Country, lang);
                if (name != null)
                    return name;
            }

            if (language != "en" && lang != "en")
            {
                name = GetCountryName(Country, "en");
                if (name != null)
                    return name;
            }

            name = GetCountryName(Country, Language);
            return name ?? "";
        }

        private static string GetCountryName(string country, string language)
        {
            var names = LocaleData.Data[country];
            if (!names.TryGetValue(language, out var name))
                return null;
            if (name.Length > 0 && name[0] == '*')
                return names[name.Substring(1)];
            return name;
        }
    }

    /// <summary>
    /// An offline geocoder which provides geographical information related to a phone number.
    /// </summary>
    /// <remarks>
    /// Author: Shaopeng Jia. <para/>
    /// Loads phone-prefix → location-string mappings from per-(language, country-code) binary
    /// files generated at build time by <c>PhoneNumbers.MetadataBuilder</c>, via
    /// <see cref="PrefixFileReader"/>.
    /// </remarks>
    public class PhoneNumberOfflineGeocoder
    {
        private static PhoneNumberOfflineGeocoder instance;
        private const string MAPPING_DATA_DIRECTORY = "geocoding.";
        private static readonly object ThisLock = new object();

        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
        private readonly PrefixFileReader prefixFileReader;

        // @VisibleForTesting
        internal PhoneNumberOfflineGeocoder(string phonePrefixDataDirectory, Assembly asm = null)
        {
            prefixFileReader = new PrefixFileReader(phonePrefixDataDirectory, asm);
        }

        /// <summary>
        /// Gets a <see cref="PhoneNumberOfflineGeocoder"/> instance to carry out international phone number
        /// geocoding.
        /// <para>The <see cref="PhoneNumberOfflineGeocoder"/> is implemented as a singleton. Therefore, calling
        /// this method multiple times will only result in one instance being created.</para>
        /// </summary>
        /// <returns>a <see cref="PhoneNumberOfflineGeocoder"/> instance</returns>
        public static PhoneNumberOfflineGeocoder GetInstance()
        {
            lock (ThisLock)
            {
                return instance ?? (instance = new PhoneNumberOfflineGeocoder(MAPPING_DATA_DIRECTORY));
            }
        }

        /// <summary>
        /// Preload the data file for the given language and country calling code, so that a future lookup
        /// for this language and country calling code will not incur any file loading.
        /// </summary>
        /// <param name="locale">specifies the language of the data file to load</param>
        /// <param name="countryCallingCode">specifies the country calling code of phone numbers that are contained by the file to be loaded</param>
        public void LoadDataFile(Locale locale, int countryCallingCode)
        {
            prefixFileReader.GetDescriptionForNumber(
                new PhoneNumber.Builder().SetCountryCode(countryCallingCode).Build(),
                locale.Language, "", locale.Country);
        }

        /// <summary>
        /// Returns the customary display name in the given language for the given territory the phone
        /// number is from. If it could be from many territories, nothing is returned.
        /// </summary>
        private string GetCountryNameForNumber(PhoneNumber number, Locale language)
        {
            var regionCodes = phoneUtil.GetRegionCodesForCountryCode(number.CountryCode);
            if (regionCodes.Count == 1)
            {
                return GetRegionDisplayName(regionCodes[0], language);
            }
            var regionWhereNumberIsValid = "ZZ";
            foreach (var regionCode in regionCodes)
            {
                if (phoneUtil.IsValidNumberForRegion(number, regionCode))
                {
                    // If the number has already been found valid for one region, then we don't know
                    // which region it belongs to so we return nothing.
                    if (!regionWhereNumberIsValid.Equals("ZZ"))
                        return "";
                    regionWhereNumberIsValid = regionCode;
                }
            }
            return GetRegionDisplayName(regionWhereNumberIsValid, language);
        }

        /// <summary>
        /// Returns the customary display name in the given language for the given region.
        /// </summary>
        private static string GetRegionDisplayName(string regionCode, Locale language)
        {
            return regionCode == null || regionCode.Equals("ZZ") ||
                   regionCode.Equals(PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY)
                ? ""
                : new Locale("", regionCode).GetDisplayCountry(language.Language);
        }

        /// <summary>
        /// Returns a text description for the given phone number, in the language provided. The
        /// description might consist of the name of the country where the phone number is from, or the
        /// name of the geographical area the phone number is from if more detailed information is
        /// available.
        /// <para>This method assumes the validity of the number passed in has already been checked, and that
        /// the number is suitable for geocoding. We consider fixed-line and mobile numbers possible
        /// candidates for geocoding.</para>
        /// </summary>
        /// <param name="number">a valid phone number for which we want to get a text description</param>
        /// <param name="languageCode">the language code for which the description should be written</param>
        /// <returns>a text description for the given language code for the given phone number, or an
        /// empty string if the number could come from multiple countries, or the country code is in fact invalid</returns>
        public string GetDescriptionForValidNumber(PhoneNumber number, Locale languageCode)
        {
            var langStr = languageCode.Language;
            var scriptStr = ""; // No script is specified
            var regionStr = languageCode.Country;

            string areaDescription;
            var mobileToken = PhoneNumberUtil.GetCountryMobileToken(number.CountryCode);
            var nationalNumber = phoneUtil.GetNationalSignificantNumber(number);
            if (mobileToken.Length != 0 && nationalNumber.StartsWith(mobileToken, StringComparison.Ordinal))
            {
                // In some countries, e.g. Argentina, mobile numbers have a mobile token before the
                // national destination code; this should be removed before geocoding.
                nationalNumber = nationalNumber.Substring(mobileToken.Length);
                var region = phoneUtil.GetRegionCodeForCountryCode(number.CountryCode);
                PhoneNumber copiedNumber;
                try
                {
                    copiedNumber = phoneUtil.Parse(nationalNumber, region);
                }
                catch (NumberParseException)
                {
                    // If this happens, just reuse what we had.
                    copiedNumber = number;
                }
                areaDescription = prefixFileReader.GetDescriptionForNumber(
                    copiedNumber, langStr, scriptStr, regionStr);
            }
            else
            {
                areaDescription = prefixFileReader.GetDescriptionForNumber(
                    number, langStr, scriptStr, regionStr);
            }
            return areaDescription.Length > 0
                ? areaDescription
                : GetCountryNameForNumber(number, languageCode);
        }

        /// <summary>
        /// As per <see cref="GetDescriptionForValidNumber(PhoneNumber, Locale)"/> but also considers the
        /// region of the user. If the phone number is from the same region as the user, only a lower-level
        /// description will be returned, if one exists. Otherwise, the phone number's region will be
        /// returned, with optionally some more detailed information.
        ///
        /// <para>For example, for a user from the region "US" (United States), we would show "Mountain View,
        /// CA" for a particular number, omitting the United States from the description. For a user from
        /// the United Kingdom (region "GB"), for the same number we may show "Mountain View, CA, United
        /// States" or even just "United States".</para>
        ///
        /// This method assumes the validity of the number passed in has already been checked.
        /// </summary>
        /// <param name="number">the phone number for which we want to get a text description</param>
        /// <param name="languageCode">the language code for which the description should be written</param>
        /// <param name="userRegion">the region code for a given user. This region will be omitted from the description if the phone number comes from this region. It should be a two-letter upper-case CLDR region code.</param>
        /// <returns>a text description for the given language code for the given phone number, or an
        /// empty string if the number could come from multiple countries, or the country code is in fact invalid</returns>
        public string GetDescriptionForValidNumber(PhoneNumber number, Locale languageCode,
            string userRegion)
        {
            // If the user region matches the number's region, then we just show the lower-level
            // description, if one exists - if no description exists, we will show the region(country) name
            // for the number.
            var regionCode = phoneUtil.GetRegionCodeForNumber(number);
            if (userRegion.Equals(regionCode))
            {
                return GetDescriptionForValidNumber(number, languageCode);
            }

            // Otherwise, we just show the region(country) name for now.
            return GetRegionDisplayName(regionCode, languageCode);
            // TODO: Concatenate the lower-level and country-name information in an appropriate
            // way for each language.
        }

        /// <summary>
        /// As per <see cref="GetDescriptionForValidNumber(PhoneNumber, Locale)"/> but explicitly checks
        /// the validity of the number passed in.
        /// </summary>
        /// <param name="number">the phone number for which we want to get a text description</param>
        /// <param name="languageCode">the language code for which the description should be written</param>
        /// <returns>a text description for the given language code for the given phone number, or empty
        /// string if the number passed in is invalid or could belong to multiple countries</returns>
        public string GetDescriptionForNumber(PhoneNumber number, Locale languageCode)
        {
            var numberType = phoneUtil.GetNumberType(number);
            if (numberType == PhoneNumberType.UNKNOWN)
                return "";
            if (!phoneUtil.IsNumberGeographical(numberType, number.CountryCode))
                return GetCountryNameForNumber(number, languageCode);
            return GetDescriptionForValidNumber(number, languageCode);
        }

        /// <summary>
        /// As per <see cref="GetDescriptionForValidNumber(PhoneNumber, Locale, string)"/> but
        /// explicitly checks the validity of the number passed in.
        /// </summary>
        /// <param name="number">the phone number for which we want to get a text description</param>
        /// <param name="languageCode">the language code for which the description should be written</param>
        /// <param name="userRegion">the region code for a given user. This region will be omitted from the description if the phone number comes from this region. It should be a two-letter upper-case CLDR region code.</param>
        /// <returns>a text description for the given language code for the given phone number, or empty
        /// string if the number passed in is invalid or could belong to multiple countries</returns>
        public string GetDescriptionForNumber(PhoneNumber number, Locale languageCode,
            string userRegion)
        {
            var numberType = phoneUtil.GetNumberType(number);
            if (numberType == PhoneNumberType.UNKNOWN)
                return "";
            if (!phoneUtil.IsNumberGeographical(numberType, number.CountryCode))
                return GetCountryNameForNumber(number, languageCode);
            return GetDescriptionForValidNumber(number, languageCode, userRegion);
        }
    }
}
