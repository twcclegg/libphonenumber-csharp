/*
 * Copyright (C) 2009 Google Inc.
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
    [TestFixture]
    class TestPhoneNumberOfflineGeocoder
    {
        private PhoneNumberOfflineGeocoder geocoder;
        const String TEST_META_DATA_FILE_PREFIX = "PhoneNumberMetaDataForTesting.xml";

        // Set up some test numbers to re-use.
        private static readonly PhoneNumber US_NUMBER1 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();
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
            PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance(
                TEST_META_DATA_FILE_PREFIX,
                CountryCodeToRegionCodeMapForTesting.GetCountryCodeToRegionCodeMap());
            geocoder = new PhoneNumberOfflineGeocoder(phoneUtil);
        }

        [Test]
        public void TestGetCompactDescriptionForNumber()
        {
            Assert.AreEqual("United States",
                geocoder.GetDescriptionForNumber(US_NUMBER1));
            // XXX: No RegionInfo for Bahamas, skipping for now...
            //Assert.AreEqual("Bahamas",
            //    geocoder.GetDescriptionForNumber(BS_NUMBER1));
            Assert.AreEqual("Australia",
                geocoder.GetDescriptionForNumber(AU_NUMBER));
            Assert.AreEqual("", geocoder.GetDescriptionForNumber(NUMBER_WITH_INVALID_COUNTRY_CODE));
        }
    }
}
