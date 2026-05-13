using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace PhoneNumbers.Test
{
    public class TestBuildPrefixMapFromBin
    {
        [Fact]
        public void TestAreaCodeMap_WriteAndRead_VerifiesIntegrity()
        {
            var testMap = new SortedDictionary<int, string>
            {
                { 1, "US" },
                { 41, "Switzerland" },
                { 412, "Unknown" }
            };

            using var memoryStream = new MemoryStream();
            BuildPrefixMapFromBin.WriteAreaCodeMap(memoryStream, testMap);

            memoryStream.Position = 0;

            var deserializedMap = BuildPrefixMapFromBin.ReadAreaCodeMap(memoryStream);

            Assert.Equal(testMap.Count, deserializedMap.Count);
            foreach (var kvp in testMap)
            {
                Assert.True(deserializedMap.ContainsKey(kvp.Key));
                Assert.Equal(kvp.Value, deserializedMap[kvp.Key]);
            }
        }

        [Fact]
        public void TestAreaCodeMap_EmptyMap_RoundTrips()
        {
            var testMap = new SortedDictionary<int, string>();

            using var memoryStream = new MemoryStream();
            BuildPrefixMapFromBin.WriteAreaCodeMap(memoryStream, testMap);
            memoryStream.Position = 0;

            var deserializedMap = BuildPrefixMapFromBin.ReadAreaCodeMap(memoryStream);
            Assert.Empty(deserializedMap);
        }

        [Fact]
        public void TestAreaCodeMap_InvalidMagic_ThrowsInvalidDataException()
        {
            using var memoryStream = new MemoryStream();
            using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(0x12345678);
            }

            memoryStream.Position = 0;
            var ex = Assert.Throws<InvalidDataException>(() => BuildPrefixMapFromBin.ReadAreaCodeMap(memoryStream));
            Assert.Contains("Unexpected area-code-map magic", ex.Message);
        }

        [Fact]
        public void TestAreaCodeMap_WrongVersion_ThrowsInvalidDataException()
        {
            using var memoryStream = new MemoryStream();
            using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(BuildPrefixMapFromBin.AreaCodeMapMagic);
                writer.Write((byte)(BuildPrefixMapFromBin.FormatVersion + 1));
            }

            memoryStream.Position = 0;
            var ex = Assert.Throws<InvalidDataException>(() => BuildPrefixMapFromBin.ReadAreaCodeMap(memoryStream));
            Assert.Contains("Unsupported area-code-map version", ex.Message);
        }

        [Fact]
        public void TestTimezoneMap_WriteAndRead_VerifiesIntegrity()
        {
            var testMap = new SortedDictionary<long, string[]>
            {
                { 1212, new[] { "America/New_York" } },
                { 41, new[] { "Europe/Zurich", "Europe/Vaduz" } }
            };

            using var memoryStream = new MemoryStream();
            BuildPrefixMapFromBin.WriteTimezoneMap(memoryStream, testMap);

            memoryStream.Position = 0;

            var deserializedMap = BuildPrefixMapFromBin.ReadTimezoneMap(memoryStream);

            Assert.Equal(testMap.Count, deserializedMap.Count);
            foreach (var kvp in testMap)
            {
                Assert.True(deserializedMap.ContainsKey(kvp.Key));
                Assert.Equal(kvp.Value, deserializedMap[kvp.Key]);
            }
        }

        [Fact]
        public void TestTimezoneMap_EmptyMap_RoundTrips()
        {
            var testMap = new SortedDictionary<long, string[]>();

            using var memoryStream = new MemoryStream();
            BuildPrefixMapFromBin.WriteTimezoneMap(memoryStream, testMap);
            memoryStream.Position = 0;

            var deserializedMap = BuildPrefixMapFromBin.ReadTimezoneMap(memoryStream);
            Assert.Empty(deserializedMap);
        }

        [Fact]
        public void TestTimezoneMap_InvalidMagic_ThrowsInvalidDataException()
        {
            using var memoryStream = new MemoryStream();
            using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(0x12345678);
            }

            memoryStream.Position = 0;
            var ex = Assert.Throws<InvalidDataException>(() => BuildPrefixMapFromBin.ReadTimezoneMap(memoryStream));
            Assert.Contains("Unexpected timezone-map magic", ex.Message);
        }

        [Fact]
        public void TestTimezoneMap_WrongVersion_ThrowsInvalidDataException()
        {
            using var memoryStream = new MemoryStream();
            using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(BuildPrefixMapFromBin.TimezoneMapMagic);
                writer.Write((byte)(BuildPrefixMapFromBin.FormatVersion + 1));
            }

            memoryStream.Position = 0;
            var ex = Assert.Throws<InvalidDataException>(() => BuildPrefixMapFromBin.ReadTimezoneMap(memoryStream));
            Assert.Contains("Unsupported timezone-map version", ex.Message);
        }
    }
}
