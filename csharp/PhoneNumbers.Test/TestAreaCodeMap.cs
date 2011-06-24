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
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PhoneNumbers.Test
{
    /**
 * Unittests for AreaCodeMap.java
 *
 * @author Shaopeng Jia
 */
    [TestFixture]
    class TestAreaCodeMap
    {
        private AreaCodeMap areaCodeMap;

        [TestFixtureSetUp]
        public void SetupFixture()
        {
            PhoneNumberUtil.ResetInstance();
            PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance(
                TestPhoneNumberUtil.TEST_META_DATA_FILE_PREFIX,
                CountryCodeToRegionCodeMapForTesting.GetCountryCodeToRegionCodeMap());
            areaCodeMap = new AreaCodeMap(phoneUtil);

            SortedDictionary<int, String> sortedMap = new SortedDictionary<int, String>();
            sortedMap[1212] = "New York";
            sortedMap[1480] = "Arizona";
            sortedMap[1650] = "California";
            sortedMap[1907] = "Alaska";
            sortedMap[1201664] = "Westwood, NJ";
            sortedMap[1480893] = "Phoenix, AZ";
            sortedMap[1501372] = "Little Rock, AR";
            sortedMap[1626308] = "Alhambra, CA";
            sortedMap[1650345] = "San Mateo, CA";
            sortedMap[1867993] = "Dawson, YT";
            sortedMap[1972480] = "Richardson, TX";
            areaCodeMap.ReadAreaCodeMap(sortedMap);
        }

        [Test]
        public void TestLookupInvalidNumber_US()
        {
            // central office code cannot start with 1.
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2121234567L).Build();
            Assert.AreEqual("New York", areaCodeMap.Lookup(number));
        }

        [Test]
        public void TestLookupNumber_NJ()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2016641234L).Build();
            Assert.AreEqual("Westwood, NJ", areaCodeMap.Lookup(number));
        }

        [Test]
        public void TestLookupNumber_NY()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2126641234L).Build();
            Assert.AreEqual("New York", areaCodeMap.Lookup(number));
        }

        [Test]
        public void TestLookupNumber_CA_1()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6503451234L).Build();
            Assert.AreEqual("San Mateo, CA", areaCodeMap.Lookup(number));
        }

        [Test]
        public void TestLookupNumber_CA_2()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502531234L).Build();
            Assert.AreEqual("California", areaCodeMap.Lookup(number));
        }

        [Test]
        public void TestLookupNumberFound_TX()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(9724801234L).Build();
            Assert.AreEqual("Richardson, TX", areaCodeMap.Lookup(number));
        }

        [Test]
        public void TestLookupNumberNotFound_TX()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(9724811234L).Build();
            Assert.AreEqual("", areaCodeMap.Lookup(number));
        }

        [Test]
        public void TestLookupNumber_CH()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(446681300L).Build();
            Assert.AreEqual("", areaCodeMap.Lookup(number));
        }
    }
}
