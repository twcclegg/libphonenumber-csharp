using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace PhoneNumbers.Test
{
    public class TestResourcePack
    {
        private static KeyValuePair<string, byte[]> Entry(string name, params byte[] payload) =>
            new KeyValuePair<string, byte[]>(name, payload);

        private static byte[] Pack(params KeyValuePair<string, byte[]>[] entries)
        {
            using var stream = new MemoryStream();
            ResourcePack.Write(stream, entries);
            return stream.ToArray();
        }

        /// <summary>
        /// Opens over a byte array the way the library opens over an embedded resource: a fresh
        /// seekable stream per call, since entries are read on demand rather than held.
        /// </summary>
        private static ResourcePack Open(byte[] bytes) =>
            ResourcePack.Open(() => new MemoryStream(bytes, writable: false))!;

        [Fact]
        public void WriteAndRead_RoundTripsEveryEntry()
        {
            var entries = new[]
            {
                Entry("en.1", 1, 2, 3),
                Entry("de.49", 4, 5),
                Entry("zh_Hant.852", 6),
            };

            var pack = Open(Pack(entries));

            Assert.Equal(3, pack.Names.Count());
            foreach (var entry in entries)
                Assert.Equal(entry.Value, pack.Read(entry.Key));
        }

        [Fact]
        public void Read_ReturnsNullForAnEntryThePackDoesNotHave()
        {
            var pack = Open(Pack(Entry("en.1", 1)));
            Assert.Null(pack.Read("fr.33"));
        }

        [Fact]
        public void WriteAndRead_EmptyPackRoundTrips()
        {
            var pack = Open(Pack());
            Assert.Empty(pack.Names);
        }

        [Fact]
        public void WriteAndRead_ZeroLengthEntryRoundTrips()
        {
            var pack = Open(Pack(Entry("empty"), Entry("after", 9)));
            Assert.Empty(pack.Read("empty")!);
            Assert.Equal(new byte[] { 9 }, pack.Read("after"));
        }

        /// <summary>
        /// Every entry must be readable more than once: the pack keeps only offsets and reopens the
        /// stream per read, so a reader that consumed or disposed shared state would fail here.
        /// </summary>
        [Fact]
        public void Read_IsRepeatable()
        {
            var pack = Open(Pack(Entry("en.1", 1, 2, 3), Entry("de.49", 4)));
            Assert.Equal(new byte[] { 1, 2, 3 }, pack.Read("en.1"));
            Assert.Equal(new byte[] { 1, 2, 3 }, pack.Read("en.1"));
            Assert.Equal(new byte[] { 4 }, pack.Read("de.49"));
            Assert.Equal(new byte[] { 1, 2, 3 }, pack.Read("en.1"));
        }

        [Fact]
        public void FromAssembly_ReturnsNullWhenTheResourceIsAbsent()
        {
            Assert.Null(ResourcePack.FromAssembly(typeof(PhoneNumberUtil).Assembly, "PhoneNumbers.no.such.pack"));
        }

        /// <summary>
        /// The pack is an embedded resource, and CI packs twice and fails the build if any assembly
        /// differs by hash. Input order must therefore not reach the output.
        /// </summary>
        [Fact]
        public void Write_IsDeterministicRegardlessOfInputOrder()
        {
            var forwards = Pack(Entry("a.1", 1), Entry("b.2", 2), Entry("c.3", 3));
            var backwards = Pack(Entry("c.3", 3), Entry("b.2", 2), Entry("a.1", 1));
            Assert.Equal(forwards, backwards);
        }

        [Fact]
        public void Open_RejectsForeignContent()
        {
            var bytes = Encoding.UTF8.GetBytes("this is not a resource pack at all, not even close");
            Assert.Throws<InvalidDataException>(() => Open(bytes));
        }

        /// <summary>
        /// A truncated pack must fail rather than hand back a zero-padded buffer, which would reach
        /// the caller as a GZip or prefix-map parse error a long way from the real cause.
        /// </summary>
        [Fact]
        public void Open_RejectsTruncatedPayload()
        {
            var full = Pack(Entry("en.1", 1, 2, 3, 4, 5, 6, 7, 8));
            var truncated = full.Take(full.Length - 4).ToArray();

            var ex = Assert.Throws<InvalidDataException>(() => Open(truncated));
            Assert.Contains("runs past the end", ex.Message);
        }

        /// <summary>
        /// A header claiming more entries than the stream can hold must be rejected before the
        /// directory is sized from it.
        /// </summary>
        [Fact]
        public void Open_RejectsImplausibleEntryCount()
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(ResourcePack.FormatMagic);
                writer.Write(ResourcePack.FormatVersion);
                writer.Write(int.MaxValue);
            }

            Assert.Throws<InvalidDataException>(() => Open(stream.ToArray()));
        }

        /// <summary>
        /// Duplicate names would otherwise drop one entry silently and surface much later as a
        /// missing prefix map.
        /// </summary>
        [Fact]
        public void Open_RejectsDuplicateEntryNames()
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(ResourcePack.FormatMagic);
                writer.Write(ResourcePack.FormatVersion);
                writer.Write(2);
                writer.Write("en.1");
                writer.Write(0);
                writer.Write(1);
                writer.Write("en.1");
                writer.Write(1);
                writer.Write(1);
                writer.Write(new byte[] { 7, 8 });
            }

            var ex = Assert.Throws<InvalidDataException>(() => Open(stream.ToArray()));
            Assert.Contains("duplicate entry", ex.Message);
        }

        /// <summary>
        /// Guards the coupling between <c>ILLink.Substitutions.xml</c> and the resources it names.
        /// <para>
        /// The substitutions file removes resources by exact name and supports no wildcard, so a
        /// renamed pack would not fail anything: the entry would simply stop matching and a trimmed
        /// build would quietly keep shipping the data the property asked it to drop. The library's
        /// own build does not trim, so nothing else in CI would notice.
        /// </para>
        /// </summary>
        [Fact]
        public void SubstitutionsFileNamesOnlyResourcesThatExist()
        {
            var assembly = typeof(PhoneNumberUtil).Assembly;

            using var stream = assembly.GetManifestResourceStream("ILLink.Substitutions.xml");
            Assert.NotNull(stream);

            var elements = XDocument.Load(stream!).Descendants("resource").ToList();
            Assert.NotEmpty(elements);

            var actual = new HashSet<string>(assembly.GetManifestResourceNames(), StringComparer.Ordinal);
            foreach (var element in elements)
            {
                var name = element.Attribute("name")?.Value;
                Assert.False(string.IsNullOrEmpty(name),
                    "every <resource> in ILLink.Substitutions.xml needs a name attribute");
                Assert.True(actual.Contains(name!),
                    $"ILLink.Substitutions.xml removes '{name}', which is not an embedded resource of " +
                    "PhoneNumbers. Either the resource was renamed and the substitutions file was not " +
                    "updated, or the entry is stale; a trimmed build would silently keep shipping the data.");
            }
        }

        /// <summary>
        /// The other half of the same coupling: every data set that is meant to be removable has to
        /// be a single resource, because one substitution entry can only name one. A data set that
        /// regressed to a resource per file would still work, and would still be untrimmable.
        /// </summary>
        [Fact]
        public void RemovableDataSetsAreEachASingleResource()
        {
            var assembly = typeof(PhoneNumberUtil).Assembly;
            var names = assembly.GetManifestResourceNames();

            foreach (var prefix in new[] { "PhoneNumbers.geocoding.", "PhoneNumbers.carrier.", "PhoneNumbers.locale." })
            {
                var matching = names.Where(n => n.StartsWith(prefix, StringComparison.Ordinal)).ToList();
                Assert.True(matching.Count == 1,
                    $"Expected exactly one embedded resource under '{prefix}', found {matching.Count}: " +
                    string.Join(", ", matching.Take(5)) + ". Each removable data set must be packed into " +
                    "one resource so ILLink.Substitutions.xml can drop it with a single entry.");
            }
        }
    }
}
