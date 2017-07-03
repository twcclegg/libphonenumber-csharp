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
        const String TEST_MAPPING_DATA_DIRECTORY = "res.test_";

        // Set up some test numbers to re-use.
        private static readonly PhoneNumber KO_NUMBER1 =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(22123456L).Build();
        private static readonly PhoneNumber KO_NUMBER2 =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(322123456L).Build();
        private static readonly PhoneNumber KO_NUMBER3 =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(6421234567L).Build();
        private static readonly PhoneNumber KO_INVALID_NUMBER =
           new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(1234L).Build();
        private static readonly PhoneNumber US_NUMBER1 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();
        private static readonly PhoneNumber US_NUMBER2 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6509600000L).Build();
        private static readonly PhoneNumber US_NUMBER3 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2128120000L).Build();
        private static readonly PhoneNumber US_NUMBER4 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6174240000L).Build();
        private static readonly PhoneNumber US_INVALID_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(123456789L).Build();
        private static readonly PhoneNumber BS_NUMBER1 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2423651234L).Build();
        private static readonly PhoneNumber AU_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(236618300L).Build();
        private static readonly PhoneNumber NUMBER_WITH_INVALID_COUNTRY_CODE =
            new PhoneNumber.Builder().SetCountryCode(999).SetNationalNumber(2423651234L).Build();
        private static readonly PhoneNumber INTERNATIONAL_TOLL_FREE =
            new PhoneNumber.Builder().SetCountryCode(800).SetNationalNumber(12345678L).Build();

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
                geocoder.GetDescriptionForNumber(US_NUMBER1, Locale.SIMPLIFIED_CHINESE));
            Assert.Equal("Bahamas",
                geocoder.GetDescriptionForNumber(BS_NUMBER1, new Locale("en", "US")));
            Assert.Equal("Australia",
                geocoder.GetDescriptionForNumber(AU_NUMBER, new Locale("en", "US")));
            Assert.Equal("", geocoder.GetDescriptionForNumber(NUMBER_WITH_INVALID_COUNTRY_CODE,
                                                              new Locale("en", "US")));
            Assert.Equal("", geocoder.GetDescriptionForNumber(INTERNATIONAL_TOLL_FREE,
                                                            new Locale("en", "US")));
        }

        [Fact]
        public void TestGetDescriptionForNumberWithMissingPrefix()
        {
            // Test that the name of the country is returned when the number passed in is valid but not
            // covered by the geocoding data file.
            Assert.Equal("United States",
                geocoder.GetDescriptionForNumber(US_NUMBER4, new Locale("en", "US")));
        }

        [Fact]
        public void testGetDescriptionForNumber_en_US()
        {
            Assert.Equal("CA",
                geocoder.GetDescriptionForNumber(US_NUMBER1, new Locale("en", "US")));
            Assert.Equal("Mountain View, CA", geocoder.GetDescriptionForNumber(US_NUMBER2, new Locale("en", "US")));
            Assert.Equal("New York, NY", geocoder.GetDescriptionForNumber(US_NUMBER3, new Locale("en", "US")));
        }

        [Fact]
        public void TestGetDescriptionForKoreanNumber()
        {
            Assert.Equal("Seoul",
                geocoder.GetDescriptionForNumber(KO_NUMBER1, Locale.ENGLISH));
            Assert.Equal("Incheon",
                geocoder.GetDescriptionForNumber(KO_NUMBER2, Locale.ENGLISH));
            Assert.Equal("Jeju",
                geocoder.GetDescriptionForNumber(KO_NUMBER3, Locale.ENGLISH));
            Assert.Equal("\uC11C\uC6B8",
                geocoder.GetDescriptionForNumber(KO_NUMBER1, Locale.KOREAN));
            Assert.Equal("\uC778\uCC9C",
                geocoder.GetDescriptionForNumber(KO_NUMBER2, Locale.KOREAN));
        }

        [Fact]
        public void TestGetDescriptionForFallBack()
        {
            // No fallback, as the location name for the given phone number is available in the requested
            // language.
            Assert.Equal("Kalifornien",
                geocoder.GetDescriptionForNumber(US_NUMBER1, Locale.GERMAN));
            // German falls back to English.
            Assert.Equal("New York, NY",
                geocoder.GetDescriptionForNumber(US_NUMBER3, Locale.GERMAN));
            // Italian falls back to English.
            Assert.Equal("CA",
                geocoder.GetDescriptionForNumber(US_NUMBER1, Locale.ITALIAN));
            // Korean doesn't fall back to English.
            Assert.Equal("\uB300\uD55C\uBBFC\uAD6D",
                geocoder.GetDescriptionForNumber(KO_NUMBER3, Locale.KOREAN));
        }

        [Fact]
        public void TestGetDescriptionForNumberWithUserRegion()
        {
            // User in Italy, American number. We should just show United States, in Spanish, and not more
            // detailed information.
            Assert.Equal("Estados Unidos",
                geocoder.GetDescriptionForNumber(US_NUMBER1, new Locale("es", "ES"), "IT"));
            // Unknown region - should just show country name.
            Assert.Equal("Estados Unidos",
                geocoder.GetDescriptionForNumber(US_NUMBER1, new Locale("es", "ES"), "ZZ"));
            // User in the States, language German, should show detailed data.
            Assert.Equal("Kalifornien",
                geocoder.GetDescriptionForNumber(US_NUMBER1, Locale.GERMAN, "US"));
            // User in the States, language French, no data for French, so we fallback to English detailed
            // data.
            Assert.Equal("CA",
                geocoder.GetDescriptionForNumber(US_NUMBER1, Locale.FRENCH, "US"));
            // Invalid number - return an empty string.
            Assert.Equal("", geocoder.GetDescriptionForNumber(US_INVALID_NUMBER, Locale.ENGLISH,
                "US"));
        }

        [Fact]
        public void TestGetDescritionForInvaildNumber()
        {
            Assert.Equal("", geocoder.GetDescriptionForNumber(KO_INVALID_NUMBER, Locale.ENGLISH));
            Assert.Equal("", geocoder.GetDescriptionForNumber(US_INVALID_NUMBER, Locale.ENGLISH));
        }
    }
}