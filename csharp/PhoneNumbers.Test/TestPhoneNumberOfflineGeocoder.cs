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
    /**
    * Unit tests for PhoneNumberOfflineGeocoder.java
    *
    * @author Shaopeng Jia
    */
    [Collection("TestMetadataTestCase")]
    public class TestPhoneNumberOfflineGeocoder
    {
        private PhoneNumberOfflineGeocoder geocoder;
        const string TEST_MAPPING_DATA_DIRECTORY = "res.test_";

        // Set up some test numbers to re-use.
        private static readonly PhoneNumber KONumber1 =
            new PhoneNumber {CountryCode = 82, NationalNumber = 22123456L };
        private static readonly PhoneNumber KONumber2 =
            new PhoneNumber {CountryCode = 82, NationalNumber = 322123456L };
        private static readonly PhoneNumber KONumber3 =
            new PhoneNumber {CountryCode = 82, NationalNumber = 6421234567L };
        private static readonly PhoneNumber KOInvalidNumber =
           new PhoneNumber {CountryCode = 82, NationalNumber = 1234L };
        private static readonly PhoneNumber USNumber1 =
            new PhoneNumber {CountryCode =1, NationalNumber = 6502530000L };
        private static readonly PhoneNumber USNumber2 =
            new PhoneNumber {CountryCode =1, NationalNumber = 6509600000L };
        private static readonly PhoneNumber USNumber3 =
            new PhoneNumber {CountryCode =1, NationalNumber = 2128120000L };
        private static readonly PhoneNumber USNumber4 =
            new PhoneNumber {CountryCode =1, NationalNumber = 6174240000L };
        private static readonly PhoneNumber USInvalidNumber =
            new PhoneNumber {CountryCode =1, NationalNumber = 123456789L };
        private static readonly PhoneNumber BSNumber1 =
            new PhoneNumber {CountryCode =1, NationalNumber = 2423651234L };
        private static readonly PhoneNumber AUNumber =
            new PhoneNumber {CountryCode = 61, NationalNumber = 236618300L };
        private static readonly PhoneNumber NumberWithInvalidCountryCode =
            new PhoneNumber {CountryCode = 999, NationalNumber = 2423651234L };
        private static readonly PhoneNumber InternationalTollFree =
            new PhoneNumber {CountryCode = 800, NationalNumber = 12345678L };

        public TestPhoneNumberOfflineGeocoder()
        {
            PhoneNumberUtil.ResetInstance();
            geocoder = new PhoneNumberOfflineGeocoder(TEST_MAPPING_DATA_DIRECTORY);
        }

        [Fact]
        public void TestInstantiationWithRegularData()
        {
            PhoneNumberUtil.ResetInstance();
            geocoder = PhoneNumberOfflineGeocoder.GetInstance();
        }

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
        public void TestGetDescritionForInvaildNumber()
        {
            Assert.Equal("", geocoder.GetDescriptionForNumber(KOInvalidNumber, Locale.English));
            Assert.Equal("", geocoder.GetDescriptionForNumber(USInvalidNumber, Locale.English));
        }
    }
}