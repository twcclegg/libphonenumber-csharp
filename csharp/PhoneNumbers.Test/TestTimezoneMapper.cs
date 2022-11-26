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
using System.Data.SqlTypes;
using System.Xml;
using Xunit;
///using static System.Net.Mime.MediaTypeNames;

namespace PhoneNumbers.Test
{
    [Collection("TestMetadataTestCase")]
    public class TestTimezoneMapper
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
            new long[] { 82, 22123456L },   // south korea
            new long[] { 82, 322123456L },
            new long[] { 82, 6421234567L },
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

        public TestTimezoneMapper()
        {
            foreach (var n in numbers)
            {
                testNumbers.Add(new PhoneNumber.Builder().SetCountryCode((int)n[0]).SetNationalNumber((ulong)n[1]).Build());
            }
        }

        [Fact]
        public void TestReader()
        {
            var ianaTZListDelimiter = new char[] { '&' };
            Assert.Empty(TimezoneReader.ToList<XmlNode>(null));
            Assert.Empty(TimezoneReader.ToList<XmlNode>(ianaTZListDelimiter));
            Assert.Empty(TimezoneReader.GetPrefixMap(new System.IO.MemoryStream(Array.Empty<byte>()), ianaTZListDelimiter));
            Assert.Empty(TimezoneReader.GetPrefixMap(null, ianaTZListDelimiter));
            Assert.Empty(TimezoneReader.GetIanaWindowsMap(new System.IO.MemoryStream(Array.Empty<byte>())));
            Assert.Empty(TimezoneReader.GetIanaWindowsMap(null));

            byte[] ssBytes = System.Text.Encoding.UTF8.GetBytes(MapTestData);
            using var ms = new System.IO.MemoryStream(ssBytes);
            var map = TimezoneReader.GetPrefixMap(ms, ianaTZListDelimiter);
            Assert.NotNull(map);
            Assert.Equal(11, map.Count);
            Assert.True(map.ContainsKey(1));
            Assert.True(1 < map[1].Length);
            ssBytes = System.Text.Encoding.UTF8.GetBytes(XMLTestData);
            using var xms = new System.IO.MemoryStream(ssBytes);
            var ianaNetMap = TimezoneReader.GetIanaWindowsMap(xms);
            Assert.NotNull(ianaNetMap);
            Assert.Equal(15, ianaNetMap.Count);
            Assert.True(ianaNetMap.ContainsKey("Africa/Casablanca"));
            Assert.Equal(2, ianaNetMap["Africa/Casablanca"].Count);
        }

        [Fact]
        public void TestMapper()
        {
            var mapper = TimezoneMapper.GetInstance();
            foreach (var pn in testNumbers)
            {
                var res1 = mapper.GetTimezones(pn);
                Assert.NotNull(res1);
                var res2 = mapper.TryGetTimeZoneInfo(pn, out var tzinfo);
                Assert.True(res2 == (null != tzinfo));
                var res3 = mapper.GetOffsetsFromUtc(pn);
                Assert.NotNull(res3);
            }
        }

        [Fact]
        public void TestMapperWithNoData()
        {
            var emptyMapper = new TimezoneMapper(new Dictionary<int, string[]>(), new Dictionary<string, List<string[]>>(), new List<TimeZoneInfo>());
            foreach (var pn in testNumbers)
            {
                var res1 = emptyMapper.GetTimezones(pn);
                Assert.NotNull(res1);
                var res2 = emptyMapper.TryGetTimeZoneInfo(pn, out var tzinfo);
                Assert.True(res2 == (null != tzinfo));
                var res3 = emptyMapper.GetOffsetsFromUtc(pn);
                Assert.NotNull(res3);
            }
        }

        [Fact]
        public void TestMapperWithBadData()
        {
            var ssBytes = System.Text.Encoding.UTF8.GetBytes(MapTestData);
            using var ms = new System.IO.MemoryStream(ssBytes);
            var map = TimezoneReader.GetPrefixMap(ms, new char[] { '&' });
            Assert.NotNull(map);
            Assert.Equal(11, map.Count);
            Assert.True(map.ContainsKey(1));
            Assert.True(1 < map[1].Length);
            ssBytes = System.Text.Encoding.UTF8.GetBytes(XMLTestData);
            using var xms = new System.IO.MemoryStream(ssBytes);
            var ianaNetMap = TimezoneReader.GetIanaWindowsMap(xms);
            Assert.NotNull(ianaNetMap);
            Assert.Equal(15, ianaNetMap.Count);
            Assert.True(ianaNetMap.ContainsKey("Africa/Casablanca"));
            Assert.Equal(2, ianaNetMap["Africa/Casablanca"].Count);

            var wrongMapper = new TimezoneMapper(map, ianaNetMap, new List<TimeZoneInfo>());
            foreach (var pn in testNumbers)
            {
                var res1 = wrongMapper.GetTimezones(pn);
                Assert.NotNull(res1);
                var res2 = wrongMapper.TryGetTimeZoneInfo(pn, out var tzinfo);
                Assert.True(res2 == (null != tzinfo));
                var res3 = wrongMapper.GetOffsetsFromUtc(pn);
                Assert.NotNull(res3);
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
        private static string XMLTestData = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<!DOCTYPE supplementalData SYSTEM ""../../common/dtd/ldmlSupplemental.dtd"">

<supplementalData>
<version number=""$Revision$""/>
<windowsZones>
<mapTimezones otherVersion=""7e11800"" typeVersion=""2021a"">

<!-- (UTC-12:00) International Date Line West -->
<mapZone other=""Dateline Standard Time"" territory=""001"" type=""Etc/GMT+12""/>
<mapZone other=""Dateline Standard Time"" territory=""ZZ"" type=""Etc/GMT+12""/>

<!-- (UTC-11:00) Coordinated Universal Time-11 -->
<mapZone other=""UTC-11"" territory=""001"" type=""Etc/GMT+11""/>
<mapZone other=""UTC-11"" territory=""AS"" type=""Pacific/Pago_Pago""/>
<mapZone other=""UTC-11"" territory=""NU"" type=""Pacific/Niue""/>
<mapZone other=""UTC-11"" territory=""UM"" type=""Pacific/Midway""/>
<mapZone other=""UTC-11"" territory=""ZZ"" type=""Etc/GMT+11""/>

<!-- (UTC-10:00) Aleutian Islands -->
<mapZone other=""Aleutian Standard Time"" territory=""001"" type=""America/Adak""/>
<mapZone other=""Aleutian Standard Time"" territory=""US"" type=""America/Adak""/>

<!-- (UTC-08:00) Coordinated Universal Time-08 -->
<mapZone other=""UTC-08"" territory=""001"" type=""Etc/GMT+8""/>
<mapZone other=""UTC-08"" territory=""PN"" type=""Pacific/Pitcairn""/>
<mapZone other=""UTC-08"" territory=""ZZ"" type=""Etc/GMT+8""/>

<!-- (UTC-08:00) Pacific Time (US & Canada) -->
<mapZone other=""Pacific Standard Time"" territory=""001"" type=""America/Los_Angeles""/>
<mapZone other=""Pacific Standard Time"" territory=""CA"" type=""America/Vancouver""/>
<mapZone other=""Pacific Standard Time"" territory=""US"" type=""America/Los_Angeles""/>
<mapZone other=""Pacific Standard Time"" territory=""ZZ"" type=""PST8PDT""/>

<!-- (UTC+00:00) Sao Tome -->
<mapZone other=""Sao Tome Standard Time"" territory=""001"" type=""Africa/Sao_Tome""/>
<mapZone other=""Sao Tome Standard Time"" territory=""ST"" type=""Africa/Sao_Tome""/>

<!-- (UTC+01:00) Casablanca -->
<mapZone other=""Morocco Standard Time"" territory=""001"" type=""Africa/Casablanca""/>
<mapZone other=""Morocco Standard Time"" territory=""EH"" type=""Africa/El_Aaiun""/>
<mapZone other=""Morocco Standard Time"" territory=""MA"" type=""Africa/Casablanca""/>

<!-- (UTC+08:00) Perth -->
<mapZone other=""W. Australia Standard Time"" territory=""001"" type=""Australia/Perth""/>
<!-- entry is intentionally different and incorrect, for test cases (dotnet name not found) -->
<mapZone other=""W. Australia"" territory=""AU"" type=""Australia/Perth""/>

<!-- for test cases (no attributes) -->
<mapZone/>
<mapZone/>

</mapTimezones>
</windowsZones>
</supplementalData>";

    }
}
