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
using Xunit;

namespace PhoneNumbers.Test
{
    [Collection("TestMetadataTestCase")]
    public class TestPhoneNumberToTimeZonesMapper
    {
        private static long[][] numbers =
        {
            new long[] { 45, 35353535L }, // denmark
            new long[] { 45, 53831292L },
            new long[] { 1, 5106761618L }, // usa
            new long[] { 1, 4155192079L },
            new long[] { 1, 8002156195L },
            new long[] { 91, 8826466567L }, // india
            new long[] { 91, 7065185423L },
            new long[] { 44, 7562397981L }, // uk
            new long[] { 48, 535019729L },  // poland
            new long[] { 82, 22123456L },   // south korea 8
            new long[] { 82, 322123456L },  // south korea 9
            new long[] { 82, 6421234567L }, // south korea 10
            new long[] { 82, 1234L },
            new long[] { 1, 6502530000L }, // usa
            new long[] { 1, 6509600000L },
            new long[] { 1, 2128120000L },
            new long[] { 1, 6174240000L },
            new long[] { 1, 123456789L },
            new long[] { 1, 2423651234L },
            new long[] { 61, 236618300L }, // australia sydney
            new long[] { 61, 851087300L }, // australia perth
            new long[] { 999, 2423651234L }, // nowhere, invalid country code
            new long[] { 800, 12345678L }, // international toll free
            new long[] { 0, 0L }, // not a number, has no country code, no region
        };

        private static readonly List<PhoneNumber> testNumbers = new List<PhoneNumber>();

        public TestPhoneNumberToTimeZonesMapper()
        {
            foreach (var n in numbers)
            {
                testNumbers.Add(new PhoneNumber.Builder().SetCountryCode((int)n[0]).SetNationalNumber((ulong)n[1]).Build());
            }
        }

        [Fact]
        public void TestMapDataReader()
        {
            var ianaTZListDelimiter = new char[] { '&' };
            Assert.Empty(TimezoneMapDataReader.GetPrefixMap(new System.IO.MemoryStream(Array.Empty<byte>()), ianaTZListDelimiter));
            Assert.Empty(TimezoneMapDataReader.GetPrefixMap(null, ianaTZListDelimiter));

            byte[] ssBytes = System.Text.Encoding.UTF8.GetBytes(MapTestData);
            using (var ms = new System.IO.MemoryStream(ssBytes))
            {
                var map = TimezoneMapDataReader.GetPrefixMap(ms, ianaTZListDelimiter);
                Assert.NotNull(map);
                Assert.Equal(11, map.Count);
                Assert.True(map.ContainsKey(1));
                Assert.True(1 < map[1].Length);
            }
        }

        [Fact]
        public void TestMapper()
        {
            var mapper = PhoneNumberToTimeZonesMapper.GetInstance();
            Assert.Same(mapper, PhoneNumberToTimeZonesMapper.GetInstance());
            Assert.Equal("Etc/Unknown", mapper.GetUnknownTimeZone());
            foreach (var pn in testNumbers)
            {
                var res0 = mapper.GetTimeZonesForNumber(pn);
                Assert.NotEmpty(res0);
            }
        }

        [Theory]
        [InlineData(0, 0, false, "")]
        [InlineData(1, 2127735151, true, "America/New_York")]
        [InlineData(1, 2125151, false, "")]
        [InlineData(800, 2156195, false, "")]
        [InlineData(800, 12345678L, false, "")]
        [InlineData(1, 8002156195, true, "America/Adak")]
        [InlineData(1, 2, false, "")]
        [InlineData(45, 35353535L, true, "Europe/Copenhagen")]
        [InlineData(1, 4155192079L, true, "America/Los_Angeles")]
        [InlineData(91, 8826466567L, true, "Asia/Calcutta")]
        [InlineData(91, 7065185423L, true, "Asia/Calcutta")]
        [InlineData(82, 22123456L, true, "Asia/Seoul")]
        [InlineData(61, 236618300L, true, "Australia/Sydney")]
        [InlineData(61, 851087300L, true, "Australia/Perth")]
        [InlineData(999, 2423651234L, false, "")]
        [InlineData(1, 123456789L, false, "")]
        [InlineData(44, 7562397981L, true, "Europe/Guernsey")]
        [InlineData(48, 535019729L, true, "Europe/Warsaw")]
        public void TestMapperOutcomes(int countryCode, long nationalNumber, bool hasTimezones, string tzName)
        {
            var phoneNumber = new PhoneNumber.Builder().SetCountryCode(countryCode).SetNationalNumber((ulong)nationalNumber).Build();
            var mapper = PhoneNumberToTimeZonesMapper.GetInstance();
            var list = mapper.GetTimeZonesForNumber(phoneNumber);
            Assert.NotNull(list);
            Assert.NotEmpty(list);
            Assert.Equal(hasTimezones, !list[0].Equals(mapper.GetUnknownTimeZone()));
            if (hasTimezones)
            {
                Assert.Equal(tzName, list[0]);
            }
            else
            {
                Assert.Single(list);
                Assert.Equal("Etc/Unknown", list[0]);
            }
        }

        [Fact]
        public void TestMapperWithNoData()
        {
            var emptyMapper = new PhoneNumberToTimeZonesMapper(new Dictionary<long, string[]>());
            foreach (var pn in testNumbers)
            {
                var list = emptyMapper.GetTimeZonesForNumber(pn);
                Assert.NotNull(list);
                Assert.Single(list);
                Assert.Equal("Etc/Unknown", list[0]);
            }
        }

        [Fact]
        public void TestMapperWithBadData()
        {
            var ssBytes = System.Text.Encoding.UTF8.GetBytes(MapTestData);
            using (var ms = new System.IO.MemoryStream(ssBytes))
            {
                var map = TimezoneMapDataReader.GetPrefixMap(ms, new char[] { '&' });
                Assert.NotNull(map);
                Assert.Equal(11, map.Count);
                Assert.True(map.ContainsKey(1));
                Assert.True(1 < map[1].Length);

                var wrongMapper = new PhoneNumberToTimeZonesMapper(map);
                foreach (var pn in testNumbers)
                {
                    var list = wrongMapper.GetTimeZonesForNumber(pn);
                    Assert.NotNull(list);
                    Assert.NotEmpty(list);
                }
            }
        }

        private static string MapTestData = @"# Copyright (C) 2012 The Libphonenumber Authors

# Licensed under the Apache License, Version 2.0 (the ""License"");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at

 _________ _________ ___________ this is for tests - do not remove

1|America/New_York&America/Chicago&America/Winnipeg&America/Los_Angeles
1201|America/New_York
1212812|America/New_York
1234|America/New_York
1604|America/Winnipeg
1617423|America/Chicago
1650960|America/Los_Angeles
1989|Ameriac/Los_Angeles
612|Australia/Sydney
61851087|Australia/Perth
82|Asia/Seoul
";
    }
}
