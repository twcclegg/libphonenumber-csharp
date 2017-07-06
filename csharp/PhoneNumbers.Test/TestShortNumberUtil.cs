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
using Xunit;

namespace PhoneNumbers.Test
{
    /**
     * Unit tests for ShortNumberUtil.java
     *
     * @author Shaopeng Jia
     */
    [Collection("TestMetadataTestCase")]
    public class TestShortNumberUtil : IClassFixture<TestMetadataTestCase>
    {
        private readonly ShortNumberUtil shortUtil;

        public TestShortNumberUtil(TestMetadataTestCase metadata)
        {
            shortUtil = new ShortNumberUtil(metadata.phoneUtil);
        }

        [Fact(Skip = "todo fix short numbers")]
        public void testConnectsToEmergencyNumber_US()
        {
            Assert.True(shortUtil.ConnectsToEmergencyNumber("911", RegionCode.US));
            Assert.True(shortUtil.ConnectsToEmergencyNumber("119", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("999", RegionCode.US));
        }

        [Fact(Skip = "todo fix short numbers")]
        public void testConnectsToEmergencyNumberLongNumber_US()
        {
            Assert.True(shortUtil.ConnectsToEmergencyNumber("9116666666", RegionCode.US));
            Assert.True(shortUtil.ConnectsToEmergencyNumber("1196666666", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("9996666666", RegionCode.US));
        }

        [Fact(Skip = "todo fix short numbers")]
        public void testConnectsToEmergencyNumberWithFormatting_US()
        {
            Assert.True(shortUtil.ConnectsToEmergencyNumber("9-1-1", RegionCode.US));
            Assert.True(shortUtil.ConnectsToEmergencyNumber("1-1-9", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("9-9-9", RegionCode.US));
        }

        [Fact]
        public void testConnectsToEmergencyNumberWithPlusSign_US()
        {
            Assert.False(shortUtil.ConnectsToEmergencyNumber("+911", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("\uFF0B911", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber(" +911", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("+119", RegionCode.US));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("+999", RegionCode.US));
        }

        [Fact(Skip = "todo fix short numbers")]
        public void testConnectsToEmergencyNumber_BR()
        {
            Assert.True(shortUtil.ConnectsToEmergencyNumber("911", RegionCode.BR));
            Assert.True(shortUtil.ConnectsToEmergencyNumber("190", RegionCode.BR));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("999", RegionCode.BR));
        }

        [Fact]
        public void testConnectsToEmergencyNumberLongNumber_BR()
        {
            // Brazilian emergency numbers don't work when additional digits are appended.
            Assert.False(shortUtil.ConnectsToEmergencyNumber("9111", RegionCode.BR));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("1900", RegionCode.BR));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("9996", RegionCode.BR));
        }
    
        [Fact]
        public void testConnectsToEmergencyNumber_AO()
        {
            // Angola doesn't have any metadata for emergency numbers in the test metadata.
            Assert.False(shortUtil.ConnectsToEmergencyNumber("911", RegionCode.AO));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("222123456", RegionCode.AO));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("923123456", RegionCode.AO));
        }

        [Fact]
        public void testConnectsToEmergencyNumber_ZW()
        {
            // Zimbabwe doesn't have any metadata in the test metadata.
            Assert.False(shortUtil.ConnectsToEmergencyNumber("911", RegionCode.ZW));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("01312345", RegionCode.ZW));
            Assert.False(shortUtil.ConnectsToEmergencyNumber("0711234567", RegionCode.ZW));
        }

        [Fact(Skip = "todo fix short numbers")]
        public void testIsEmergencyNumber_US()
        {
            Assert.True(shortUtil.IsEmergencyNumber("911", RegionCode.US));
            Assert.True(shortUtil.IsEmergencyNumber("119", RegionCode.US));
            Assert.False(shortUtil.IsEmergencyNumber("999", RegionCode.US));
        }

        [Fact]
        public void testIsEmergencyNumberLongNumber_US()
        {
            Assert.False(shortUtil.IsEmergencyNumber("9116666666", RegionCode.US));
            Assert.False(shortUtil.IsEmergencyNumber("1196666666", RegionCode.US));
            Assert.False(shortUtil.IsEmergencyNumber("9996666666", RegionCode.US));
        }

        [Fact(Skip = "todo fix short numbers")]
        public void testIsEmergencyNumberWithFormatting_US()
        {
            Assert.True(shortUtil.IsEmergencyNumber("9-1-1", RegionCode.US));
            Assert.True(shortUtil.IsEmergencyNumber("*911", RegionCode.US));
            Assert.True(shortUtil.IsEmergencyNumber("1-1-9", RegionCode.US));
            Assert.True(shortUtil.IsEmergencyNumber("*119", RegionCode.US));
            Assert.False(shortUtil.IsEmergencyNumber("9-9-9", RegionCode.US));
            Assert.False(shortUtil.IsEmergencyNumber("*999", RegionCode.US));
        }

        [Fact]
        public void testIsEmergencyNumberWithPlusSign_US()
        {
            Assert.False(shortUtil.IsEmergencyNumber("+911", RegionCode.US));
            Assert.False(shortUtil.IsEmergencyNumber("\uFF0B911", RegionCode.US));
            Assert.False(shortUtil.IsEmergencyNumber(" +911", RegionCode.US));
            Assert.False(shortUtil.IsEmergencyNumber("+119", RegionCode.US));
            Assert.False(shortUtil.IsEmergencyNumber("+999", RegionCode.US));
        }

        [Fact(Skip = "todo fix short numbers")]
        public void testIsEmergencyNumber_BR()
        {
            Assert.True(shortUtil.IsEmergencyNumber("911", RegionCode.BR));
            Assert.True(shortUtil.IsEmergencyNumber("190", RegionCode.BR));
            Assert.False(shortUtil.IsEmergencyNumber("999", RegionCode.BR));
        }

        [Fact]
        public void testIsEmergencyNumberLongNumber_BR()
        {
            Assert.False(shortUtil.IsEmergencyNumber("9111", RegionCode.BR));
            Assert.False(shortUtil.IsEmergencyNumber("1900", RegionCode.BR));
            Assert.False(shortUtil.IsEmergencyNumber("9996", RegionCode.BR));
        }

        [Fact]
        public void testIsEmergencyNumber_AO()
        {
            // Angola doesn't have any metadata for emergency numbers in the test metadata.
            Assert.False(shortUtil.IsEmergencyNumber("911", RegionCode.AO));
            Assert.False(shortUtil.IsEmergencyNumber("222123456", RegionCode.AO));
            Assert.False(shortUtil.IsEmergencyNumber("923123456", RegionCode.AO));
        }

        [Fact]
        public void testIsEmergencyNumber_ZW()
        {
            // Zimbabwe doesn't have any metadata in the test metadata.
            Assert.False(shortUtil.IsEmergencyNumber("911", RegionCode.ZW));
            Assert.False(shortUtil.IsEmergencyNumber("01312345", RegionCode.ZW));
            Assert.False(shortUtil.IsEmergencyNumber("0711234567", RegionCode.ZW));
        }
    }
}
