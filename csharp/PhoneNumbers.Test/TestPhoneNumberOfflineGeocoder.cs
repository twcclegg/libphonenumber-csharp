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

using Xunit;

namespace PhoneNumbers.Test
{
    [Collection("TestLocaleTestCase")]
    public class TestLocale
    {
        [Fact]
        public void GetDisplayCountry_WhenPassedWithAnValidLanguageCode_ShouldReturnTheCorrectDisplayCountry()
        {
            Assert.Equal("United States", new Locale("en", "US").GetDisplayCountry("US"));
        }

        [Fact]
        public void GetDisplayCountry_WhenPassedWithAnEmptyCountryCode_ShouldReturnAnEmptyDisplayCountry()
        {
            Assert.Equal(string.Empty, new Locale("en", string.Empty).GetDisplayCountry("abc"));
        }

        [Theory]
        // Phone-metadata regions that are not ISO countries. Expected values are what
        // java.util.Locale.getDisplayCountry returns on JDK 25.0.5 (Temurin), the JDK that generated
        // resources/locale/country_names.txt. For a language with no name (kl here) Java returns the
        // region code itself, and so must we.
        [InlineData("AC", "en", "Ascension Island")]
        [InlineData("AC", "de", "Ascension")]
        [InlineData("AC", "zh", "\u963F\u68EE\u677E\u5C9B")]
        [InlineData("AC", "kl", "AC")]
        [InlineData("TA", "en", "Tristan da Cunha")]
        [InlineData("TA", "zh", "\u7279\u91CC\u65AF\u5766-\u8FBE\u5E93\u5C3C\u4E9A\u7FA4\u5C9B")]
        [InlineData("TA", "kl", "TA")]
        [InlineData("XK", "en", "Kosovo")]
        [InlineData("XK", "zh", "\u79D1\u7D22\u6C83")]
        [InlineData("XK", "kl", "XK")]
        public void GetDisplayCountry_WhenRegionIsNotAnIsoCountry_ShouldReturnTheJavaLocaleName(
            string regionCode, string language, string expected)
        {
            Assert.Equal(expected, new Locale(language, regionCode).GetDisplayCountry(language));
        }

        [Fact]
        public void GetDisplayCountry_WhenUnknownLanguage_ShouldReturnADefaultDisplayCountry()
        {
            Assert.Equal("United States", new Locale("unknown-language", "US").GetDisplayCountry("unknown-language"));
        }
    }

    /// <summary>
    /// GetDescriptionForNumber with the real geocoding data, for numbers in regions that are not ISO
    /// 3166-1 countries. These have no prefix data in most languages, so the description is the
    /// region name. Expected values come from the Java libphonenumber 9.0.44 jars (libphonenumber,
    /// geocoder 3.44, prefixmapper 3.44) on JDK 25.0.5 (Temurin):
    /// <c>PhoneNumberOfflineGeocoder.getInstance().getDescriptionForNumber(n, new Locale(lang))</c>.
    /// </summary>
    public class TestGeocoderNonIsoRegions
    {
        [Theory]
        [InlineData("+247 542011", "en", "Ascension Island")]
        [InlineData("+247 542011", "de", "Ascension")]
        [InlineData("+247 542011", "zh", "\u963F\u68EE\u677E\u5C9B")]
        [InlineData("+247 542011", "kl", "AC")]
        [InlineData("+383 43 201 234", "en", "Kosovo")]
        [InlineData("+383 43 201 234", "de", "Kosovo")]
        [InlineData("+383 43 201 234", "zh", "\u79D1\u7D22\u6C83")]
        [InlineData("+383 43 201 234", "kl", "XK")]
        [InlineData("+290 8999", "en", "Tristan da Cunha")]
        [InlineData("+290 8999", "zh", "\u7279\u91CC\u65AF\u5766-\u8FBE\u5E93\u5C3C\u4E9A\u7FA4\u5C9B")]
        public void GetDescriptionForNumber_WhenRegionIsNotAnIsoCountry_ShouldMatchJava(
            string number, string language, string expected)
        {
            var phoneNumber = PhoneNumberUtil.GetInstance().Parse(number, "ZZ");

            Assert.Equal(expected,
                PhoneNumberOfflineGeocoder.GetInstance().GetDescriptionForNumber(phoneNumber, new Locale(language, "")));
        }
    }

    /// <summary>
    /// Unit tests for the geocoder, using the binary test geocoding data generated at build time
    /// from <c>resources/test/geocoding/</c> and embedded under <c>PhoneNumbers.Test.geocoding.*</c>.
    /// </summary>
    /// <remarks>
    /// The previously-separate "zipped" variant (TestZippedPhoneNumberOfflineGeocoder) was deleted
    /// when zip-based loading was removed in favor of per-(lang, country-code) binary files —
    /// there's only one geocoding load path now, so one test class is enough.
    /// </remarks>
    [Collection("TestMetadataTestCase")]
    public class TestPhoneNumberOfflineGeocoder : TestPhoneNumberOfflineGeocoderBase
    {
        private static readonly PhoneNumberOfflineGeocoder geocoderInstance = new PhoneNumberOfflineGeocoder("geocoding.", typeof(TestPhoneNumberOfflineGeocoder).Assembly);

        protected override PhoneNumberOfflineGeocoder geocoder => geocoderInstance;

        [Fact]
        public void TestInstantiationWithRegularData()
        {
            Assert.NotNull(PhoneNumberOfflineGeocoder.GetInstance());
        }
    }

    /// <summary>
    /// Base Unit tests for PhoneNumberOfflineGeocoder.java
    /// </summary>
    /// <remarks>Author: Shaopeng Jia</remarks>
    public abstract class TestPhoneNumberOfflineGeocoderBase
    {
        protected abstract PhoneNumberOfflineGeocoder geocoder { get; }

        // Set up some test numbers to re-use.
        private static readonly PhoneNumber KONumber1 =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(22123456L).Build();
        private static readonly PhoneNumber KONumber2 =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(322123456L).Build();
        private static readonly PhoneNumber KONumber3 =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(6421234567L).Build();
        private static readonly PhoneNumber KOInvalidNumber =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(1234L).Build();
        private static readonly PhoneNumber USNumber1 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();
        private static readonly PhoneNumber USNumber2 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6509600000L).Build();
        private static readonly PhoneNumber USNumber3 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2128120000L).Build();
        private static readonly PhoneNumber USNumber4 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6174240000L).Build();
        private static readonly PhoneNumber USInvalidNumber =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(123456789L).Build();
        private static readonly PhoneNumber NANPATollFree =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(8002431234L).Build();
        private static readonly PhoneNumber BSNumber1 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2423651234L).Build();
        private static readonly PhoneNumber AUNumber =
            new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(236618300L).Build();
        private static readonly PhoneNumber ARMobileNumber =
            new PhoneNumber.Builder().SetCountryCode(54).SetNationalNumber(92214000000L).Build();
        private static readonly PhoneNumber KOMobile =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(101234567L).Build();
        private static readonly PhoneNumber NumberWithInvalidCountryCode =
            new PhoneNumber.Builder().SetCountryCode(999).SetNationalNumber(2423651234L).Build();
        private static readonly PhoneNumber InternationalTollFree =
            new PhoneNumber.Builder().SetCountryCode(800).SetNationalNumber(12345678L).Build();

        [Fact]
        public void TestGetDescriptionForNumberWithNoDataFile()
        {
            // No data file containing mappings for US numbers is available in Chinese for the unittests. As
            // a result, the country name of United States in simplified Chinese is returned.
            Assert.Equal("\u7F8E\u56FD",
                geocoder.GetDescriptionForNumber(USNumber1, Locale.SimplifiedChinese));
            Assert.Equal("Bahamas",
                geocoder.GetDescriptionForNumber(BSNumber1, new Locale("en", "US")));
            Assert.Equal("Australia",
                geocoder.GetDescriptionForNumber(AUNumber, new Locale("en", "US")));
            Assert.Equal("", geocoder.GetDescriptionForNumber(NumberWithInvalidCountryCode,
                new Locale("en", "US")));
            Assert.Equal("", geocoder.GetDescriptionForNumber(InternationalTollFree,
                new Locale("en", "US")));
        }

        [Fact]
        public void TestGetDescriptionForNumberWithMissingPrefix()
        {
            // Test that the name of the country is returned when the number passed in is valid but not
            // covered by the geocoding data file.
            Assert.Equal("United States",
                geocoder.GetDescriptionForNumber(USNumber4, new Locale("en", "US")));
        }

        [Fact]
        public void TestGetDescriptionForNumberBelongingToMultipleCountriesIsEmpty()
        {
            // Test that nothing is returned when the number passed in is valid but not covered by the
            // geocoding data file and belongs to multiple countries.
            Assert.Equal("",
                geocoder.GetDescriptionForNumber(NANPATollFree, new Locale("en", "US")));
        }

        [Fact]
        public void TestGetDescriptionForArgentinianMobileNumber()
        {
            Assert.Equal("La Plata",
                geocoder.GetDescriptionForNumber(ARMobileNumber, Locale.English));
        }

        [Fact]
        public void TestGetDescriptionForNonGeographicalNumberWithGeocodingPrefix()
        {
            // We have a geocoding prefix, but we shouldn't use it since this is not geographical.
            Assert.Equal("South Korea",
                geocoder.GetDescriptionForNumber(KOMobile, Locale.English));
        }

        [Fact]
        public void testGetDescriptionForNumber_en_US()
        {
            Assert.Equal("CA",
                geocoder.GetDescriptionForNumber(USNumber1, new Locale("en", "US")));
            Assert.Equal("Mountain View, CA", geocoder.GetDescriptionForNumber(USNumber2, new Locale("en", "US")));
            Assert.Equal("New York, NY", geocoder.GetDescriptionForNumber(USNumber3, new Locale("en", "US")));
        }

        [Fact]
        public void TestGetDescriptionForKoreanNumber()
        {
            Assert.Equal("Seoul",
                geocoder.GetDescriptionForNumber(KONumber1, Locale.English));
            Assert.Equal("Incheon",
                geocoder.GetDescriptionForNumber(KONumber2, Locale.English));
            Assert.Equal("Jeju",
                geocoder.GetDescriptionForNumber(KONumber3, Locale.English));
            Assert.Equal("\uC11C\uC6B8",
                geocoder.GetDescriptionForNumber(KONumber1, Locale.Korean));
            Assert.Equal("\uC778\uCC9C",
                geocoder.GetDescriptionForNumber(KONumber2, Locale.Korean));
        }

        [Fact]
        public void TestGetDescriptionForFallBack()
        {
            // No fallback, as the location name for the given phone number is available in the requested
            // language.
            Assert.Equal("Kalifornien",
                geocoder.GetDescriptionForNumber(USNumber1, Locale.German));
            // German falls back to English.
            Assert.Equal("New York, NY",
                geocoder.GetDescriptionForNumber(USNumber3, Locale.German));
            // Italian falls back to English.
            Assert.Equal("CA",
                geocoder.GetDescriptionForNumber(USNumber1, Locale.Italian));
            // Korean doesn't fall back to English.
            Assert.Equal("\uB300\uD55C\uBBFC\uAD6D",
                geocoder.GetDescriptionForNumber(KONumber3, Locale.Korean));
        }

        [Fact]
        public void TestGetDescriptionForNumberWithUserRegion()
        {
            // User in Italy, American number. We should just show United States, in Spanish, and not more
            // detailed information.
            Assert.Equal("Estados Unidos",
                geocoder.GetDescriptionForNumber(USNumber1, new Locale("es", "ES"), "IT"));
            // Unknown region - should just show country name.
            Assert.Equal("Estados Unidos",
                geocoder.GetDescriptionForNumber(USNumber1, new Locale("es", "ES"), "ZZ"));
            // User in the States, language German, should show detailed data.
            Assert.Equal("Kalifornien",
                geocoder.GetDescriptionForNumber(USNumber1, Locale.German, "US"));
            // User in the States, language French, no data for French, so we fallback to English detailed
            // data.
            Assert.Equal("CA",
                geocoder.GetDescriptionForNumber(USNumber1, Locale.French, "US"));
            // Invalid number - return an empty string.
            Assert.Equal("", geocoder.GetDescriptionForNumber(USInvalidNumber, Locale.English,
                "US"));
        }

        [Fact]
        public void TestGetDescriptionForInvalidNumber()
        {
            Assert.Equal("", geocoder.GetDescriptionForNumber(KOInvalidNumber, Locale.English));
            Assert.Equal("", geocoder.GetDescriptionForNumber(USInvalidNumber, Locale.English));
        }
    }
}