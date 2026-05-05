/*
 * Copyright (C) 2026 The Libphonenumber Authors
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

using System.IO;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// Roundtrip tests for the binary metadata serializer. Exercises the full XML→bin→PhoneMetadata
    /// pipeline against real production metadata to catch any field we forgot to serialize.
    /// </summary>
    public class TestBuildMetadataFromBin
    {
        [Fact]
        public void RoundtripPreservesAllRegions()
        {
            // Parse the production metadata once via the existing XML loader, then for every region
            // check that bin -> Read -> Equals(original). PhoneMetadata.Equals walks every field
            // (including nested descs and number formats), so this is a real diff, not just a smoke
            // test that the bytes survive.
            var fromXml = BuildMetadataFromXml.BuildPhoneMetadata(
                "PhoneNumberMetadata.xml", typeof(TestBuildMetadataFromBin).Assembly);

            Assert.True(fromXml.Count > 200, "expected >200 regions");

            foreach (var original in fromXml)
            {
                using var ms = new MemoryStream();
                BuildMetadataFromBin.WriteMetadata(ms, original);
                ms.Position = 0;
                var roundtripped = BuildMetadataFromBin.ReadMetadata(ms);

                Assert.True(
                    original.Equals(roundtripped),
                    $"PhoneMetadata roundtrip mismatch for region '{original.Id}' (countryCode={original.CountryCode}).");
            }
        }

        [Fact]
        public void RoundtripPreservesShortNumberMetadata()
        {
            var fromXml = BuildMetadataFromXml.BuildPhoneMetadata(
                "ShortNumberMetadata.xml",
                typeof(TestBuildMetadataFromBin).Assembly,
                isShortNumberMetadata: true);

            Assert.NotEmpty(fromXml);

            foreach (var original in fromXml)
            {
                using var ms = new MemoryStream();
                BuildMetadataFromBin.WriteMetadata(ms, original);
                ms.Position = 0;
                var roundtripped = BuildMetadataFromBin.ReadMetadata(ms);

                Assert.True(
                    original.Equals(roundtripped),
                    $"ShortNumber metadata roundtrip mismatch for region '{original.Id}'.");
            }
        }

        [Fact]
        public void RoundtripPreservesAlternateFormats()
        {
            var fromXml = BuildMetadataFromXml.BuildPhoneMetadata(
                "PhoneNumberAlternateFormats.xml",
                typeof(TestBuildMetadataFromBin).Assembly,
                isAlternateFormatsMetadata: true);

            Assert.NotEmpty(fromXml);

            foreach (var original in fromXml)
            {
                using var ms = new MemoryStream();
                BuildMetadataFromBin.WriteMetadata(ms, original);
                ms.Position = 0;
                var roundtripped = BuildMetadataFromBin.ReadMetadata(ms);

                Assert.True(
                    original.Equals(roundtripped),
                    $"AlternateFormats metadata roundtrip mismatch for countryCode={original.CountryCode}.");
            }
        }

        [Fact]
        public void EmptyMetadataRoundtripsCleanly()
        {
            var empty = new PhoneMetadata();
            using var ms = new MemoryStream();
            BuildMetadataFromBin.WriteMetadata(ms, empty);
            ms.Position = 0;
            var read = BuildMetadataFromBin.ReadMetadata(ms);
            Assert.True(empty.Equals(read));
        }

        [Fact]
        public void RejectsBadMagic()
        {
            using var ms = new MemoryStream(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01 });
            Assert.Throws<InvalidDataException>(() => BuildMetadataFromBin.ReadMetadata(ms));
        }

        [Fact]
        public void RejectsBadVersion()
        {
            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                bw.Write(BuildMetadataFromBin.FormatMagic);
                bw.Write((byte)99);
            }
            ms.Position = 0;
            Assert.Throws<InvalidDataException>(() => BuildMetadataFromBin.ReadMetadata(ms));
        }
    }
}
