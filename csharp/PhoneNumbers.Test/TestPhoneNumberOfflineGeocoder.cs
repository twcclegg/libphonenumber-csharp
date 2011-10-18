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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PhoneNumbers.Test
{
    /**
    * Unit tests for PhoneNumberOfflineGeocoder.java
    *
    * @author Shaopeng Jia
    */
    [TestFixture]
    class TestPhoneNumberOfflineGeocoder
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

        [TestFixtureSetUp]
        public void SetupFixture()
        {
            PhoneNumberUtil.ResetInstance();
            geocoder = new PhoneNumberOfflineGeocoder(TEST_MAPPING_DATA_DIRECTORY);
        }

        /* This test is disabled as we do not have localized country names by
         * default on .NET. Also, Bahamas RegionInfo does not exist.
        [Test]
        public void testGetDescriptionForNumberWithNoDataFile()
        {
            // No data file containing mappings for US numbers is available in Chinese for the unittests. As
            // a result, the country name of United States in simplified Chinese is returned.
            Assert.AreEqual("\u7F8E\u56FD",
                geocoder.GetDescriptionForNumber(US_NUMBER1, Locale.SIMPLIFIED_CHINESE));
            Assert.AreEqual("Bahamas",
                geocoder.GetDescriptionForNumber(BS_NUMBER1, new Locale("en", "US")));
            Assert.AreEqual("Australia",
                geocoder.GetDescriptionForNumber(AU_NUMBER, new Locale("en", "US")));
            Assert.AreEqual("", geocoder.GetDescriptionForNumber(NUMBER_WITH_INVALID_COUNTRY_CODE,
                                                              new Locale("en", "US")));
        }
        */

        [Test]
        public void testGetDescriptionForNumberWithMissingPrefix()
        {
            // Test that the name of the country is returned when the number passed in is valid but not
            // covered by the geocoding data file.
            Assert.AreEqual("United States",
                geocoder.GetDescriptionForNumber(US_NUMBER4, new Locale("en", "US")));
        }

        [Test]
        public void testGetDescriptionForNumber_en_US()
        {
            Assert.AreEqual("CA",
                geocoder.GetDescriptionForNumber(US_NUMBER1, new Locale("en", "US")));
            Assert.AreEqual("Mountain View, CA",
                geocoder.GetDescriptionForNumber(US_NUMBER2, new Locale("en", "US")));
            Assert.AreEqual("New York, NY",
                geocoder.GetDescriptionForNumber(US_NUMBER3, new Locale("en", "US")));
        }

        [Test]
        public void testGetDescriptionForKoreanNumber()
        {
            Assert.AreEqual("Seoul",
                geocoder.GetDescriptionForNumber(KO_NUMBER1, Locale.ENGLISH));
            Assert.AreEqual("Incheon",
                geocoder.GetDescriptionForNumber(KO_NUMBER2, Locale.ENGLISH));
            Assert.AreEqual("Jeju",
                geocoder.GetDescriptionForNumber(KO_NUMBER3, Locale.ENGLISH));
            Assert.AreEqual("\uC11C\uC6B8",
                geocoder.GetDescriptionForNumber(KO_NUMBER1, Locale.KOREAN));
            Assert.AreEqual("\uC778\uCC9C",
                geocoder.GetDescriptionForNumber(KO_NUMBER2, Locale.KOREAN));
        }

        [Test]
        public void TestGetDescriptionForFallBack()
        {
            // No fallback, as the location name for the given phone number is available in the requested
            // language.
            Assert.AreEqual("Kalifornien",
                geocoder.GetDescriptionForNumber(US_NUMBER1, Locale.GERMAN));
            // German falls back to English.
            Assert.AreEqual("New York, NY",
                geocoder.GetDescriptionForNumber(US_NUMBER3, Locale.GERMAN));
            // Italian falls back to English.
            Assert.AreEqual("CA",
                geocoder.GetDescriptionForNumber(US_NUMBER1, Locale.ITALIAN));
            // Korean doesn't fall back to English.
            // C#: changed from Java because we lack korean locale information
            Assert.AreEqual("Korea",
                geocoder.GetDescriptionForNumber(KO_NUMBER3, Locale.KOREAN));
        }

        [Test]
        public void TestGetDescritionForInvaildNumber()
        {
            Assert.AreEqual("", geocoder.GetDescriptionForNumber(KO_INVALID_NUMBER, Locale.ENGLISH));
            Assert.AreEqual("", geocoder.GetDescriptionForNumber(US_INVALID_NUMBER, Locale.ENGLISH));
        }
    }
}