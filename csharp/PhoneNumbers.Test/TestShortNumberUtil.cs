/*
 * Copyright (C) 2011 The Libphonenumber Authors
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
     * @author Shaopeng Jia
     */
    [TestFixture]
    public class ShortNumberUtilTest
    {
        private ShortNumberUtil shortUtil;
        public const String TEST_META_DATA_FILE_PREFIX = "PhoneNumberMetaDataForTesting.xml";

        public ShortNumberUtilTest()
        {
            PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance(
                TEST_META_DATA_FILE_PREFIX,
                CountryCodeToRegionCodeMapForTesting.GetCountryCodeToRegionCodeMap());
            shortUtil = new ShortNumberUtil(phoneUtil);
        }

        [Test]
        public void testConnectsToEmergencyNumber_US()
        {
            Assert.True(shortUtil.ConnectsToEmergencyNumber("911", RegionCode.US));
            Assert.True(shortUtil.ConnectsToEmergencyNumber("119", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("999", RegionCode.US));
        }

        [Test]
        public void testConnectsToEmergencyNumberLongNumber_US()
        {
            Assert.True(shortUtil.ConnectsToEmergencyNumber("9116666666", RegionCode.US));
            Assert.True(shortUtil.ConnectsToEmergencyNumber("1196666666", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("9996666666", RegionCode.US));
        }

        [Test]
        public void testConnectsToEmergencyNumberWithFormatting_US()
        {
            Assert.True(shortUtil.ConnectsToEmergencyNumber("9-1-1", RegionCode.US));
            Assert.True(shortUtil.ConnectsToEmergencyNumber("1-1-9", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("9-9-9", RegionCode.US));
        }

        [Test]
        public void testConnectsToEmergencyNumberWithPlusSign_US()
        {
            Assert.False(shortUtil.ConnectsToEmergencyNumber("+911", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("\uFF0B911", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber(" +911", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("+119", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("+999", RegionCode.US));
        }

        [Test]
        public void testConnectsToEmergencyNumber_BR()
        {
            Assert.True(shortUtil.ConnectsToEmergencyNumber("911", RegionCode.BR));
            Assert.True(shortUtil.ConnectsToEmergencyNumber("190", RegionCode.BR));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("999", RegionCode.BR));
        }

        [Test]
        public void testConnectsToEmergencyNumberLongNumber_BR()
        {
            // Brazilian emergency numbers don't work when additional digits are appended.
            Assert.False(shortUtil.ConnectsToEmergencyNumber("9111", RegionCode.BR));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("1900", RegionCode.BR));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("9996", RegionCode.BR));
        }
    }
}
