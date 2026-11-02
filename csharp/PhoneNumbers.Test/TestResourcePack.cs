using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        /// <summary>Drains an entry's view, so the assertions below stay byte comparisons.</summary>
        private static byte[]? ReadEntry(ResourcePack pack, string name)
        {
            using var entry = pack.OpenEntry(name);
            if (entry is null) return null;
            using var buffer = new MemoryStream();
            entry.CopyTo(buffer);
            return buffer.ToArray();
        }

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
                Assert.Equal(entry.Value, ReadEntry(pack, entry.Key));
        }

        [Fact]
        public void Read_ReturnsNullForAnEntryThePackDoesNotHave()
        {
            var pack = Open(Pack(Entry("en.1", 1)));
            Assert.Null(ReadEntry(pack, "fr.33"));
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
            Assert.Empty(ReadEntry(pack, "empty")!);
            Assert.Equal(new byte[] { 9 }, ReadEntry(pack, "after"));
        }

        /// <summary>
        /// Every entry must be readable more than once: the pack keeps only offsets and reopens the
        /// stream per read, so a reader that consumed or disposed shared state would fail here.
        /// </summary>
        [Fact]
        public void Read_IsRepeatable()
        {
            var pack = Open(Pack(Entry("en.1", 1, 2, 3), Entry("de.49", 4)));
            Assert.Equal(new byte[] { 1, 2, 3 }, ReadEntry(pack, "en.1"));
            Assert.Equal(new byte[] { 1, 2, 3 }, ReadEntry(pack, "en.1"));
            Assert.Equal(new byte[] { 4 }, ReadEntry(pack, "de.49"));
            Assert.Equal(new byte[] { 1, 2, 3 }, ReadEntry(pack, "en.1"));
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
        /// Truncating the directory itself, rather than the payload, is how a real pack fails: the
        /// entry-count guard only catches a stream too short to hold the directory it declares, and
        /// a shipped pack's directory is kilobytes into a megabyte file. BinaryReader reports that
        /// as EndOfStreamException, which is an IOException and so not catchable alongside
        /// InvalidDataException — hence the wrap.
        /// </summary>
        [Fact]
        public void Open_ReportsATruncatedDirectoryAsMalformedData()
        {
            var entries = Enumerable.Range(0, 50)
                .Select(i => Entry($"en.{i:D2}", (byte)i))
                .ToArray();
            var full = Pack(entries);

            // Past the count guard (50 entries need 450 bytes minimum) but short of the real
            // directory, so the read runs off the end partway through.
            var truncated = full.Take(9 + 500).ToArray();

            var ex = Assert.Throws<InvalidDataException>(() => Open(truncated));
            Assert.Contains("malformed or truncated", ex.Message, StringComparison.Ordinal);
            Assert.NotNull(ex.InnerException);
        }

        /// <summary>
        /// The entry view is deliberately read-only and forward-only. Pinned because
        /// <c>GZipStream</c> is the only consumer today and exercises none of this, so a later
        /// change could quietly make the view seekable and no other test would notice.
        /// </summary>
        [Fact]
        public void OpenEntry_ReturnsAReadOnlyForwardOnlyView()
        {
            var pack = Open(Pack(Entry("en.1", 1, 2, 3, 4)));
            using var entry = pack.OpenEntry("en.1");
            Assert.NotNull(entry);

            Assert.True(entry!.CanRead);
            Assert.False(entry.CanSeek);
            Assert.False(entry.CanWrite);
            Assert.Equal(4, entry.Length);
            Assert.Equal(0, entry.Position);

            entry.Flush(); // no-op, but must not throw
            Assert.Throws<NotSupportedException>(() => entry.Position = 2);
            Assert.Throws<NotSupportedException>(() => entry.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => entry.SetLength(2));
            Assert.Throws<NotSupportedException>(() => entry.Write(new byte[1], 0, 1));

            Assert.Equal(1, entry.ReadByte());
            Assert.Equal(1, entry.Position);
        }

        /// <summary>
        /// A stream is opened per entry, so a failure between opening it and handing it back must
        /// not leak the handle.
        /// </summary>
        [Fact]
        public void OpenEntry_DisposesTheStreamIfPositioningFails()
        {
            var bytes = Pack(Entry("en.1", 1, 2, 3, 4));
            var opened = new List<ThrowOnSeekStream>();

            var pack = ResourcePack.Open(() =>
            {
                var stream = new ThrowOnSeekStream(bytes, failOnPositionSet: opened.Count > 0);
                opened.Add(stream);
                return stream;
            });
            Assert.NotNull(pack);

            Assert.Throws<NotSupportedException>(() => pack!.OpenEntry("en.1"));
            Assert.True(opened[^1].Disposed, "the per-entry stream was not disposed after the failure");
        }

        private sealed class ThrowOnSeekStream : MemoryStream
        {
            private readonly bool failOnPositionSet;

            internal ThrowOnSeekStream(byte[] bytes, bool failOnPositionSet)
                : base(bytes, writable: false) => this.failOnPositionSet = failOnPositionSet;

            internal bool Disposed { get; private set; }

            public override long Position
            {
                get => base.Position;
                set
                {
                    if (failOnPositionSet) throw new NotSupportedException("positioning refused");
                    base.Position = value;
                }
            }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        // ---- malformed packs must be reported as malformed, not decoded into junk ----------

        private static byte[] PackHeader(int magic, byte version, int count)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(magic);
                writer.Write(version);
                writer.Write(count);
            }
            return stream.ToArray();
        }

        [Fact]
        public void Open_RejectsAnUnsupportedFormatVersion()
        {
            var bytes = PackHeader(ResourcePack.FormatMagic, (byte)(ResourcePack.FormatVersion + 1), 0);

            var ex = Assert.Throws<InvalidDataException>(
                () => ResourcePack.Open(() => new MemoryStream(bytes, writable: false)));
            Assert.Contains("version", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Open_RejectsANegativeOffsetOrLength()
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(ResourcePack.FormatMagic);
                writer.Write(ResourcePack.FormatVersion);
                writer.Write(1);
                writer.Write("en.1");
                writer.Write(0);
                writer.Write(-1);
                writer.Write(new byte[] { 1, 2, 3, 4 });
            }
            var bytes = stream.ToArray();

            var ex = Assert.Throws<InvalidDataException>(
                () => ResourcePack.Open(() => new MemoryStream(bytes, writable: false)));
            Assert.Contains("negative", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// The writer rejects duplicates too, not just the reader. Catching it at build time beats
        /// shipping an assembly whose pack fails to open, and two equal names would also sort
        /// unpredictably through an unstable introsort, breaking reproducibility.
        /// </summary>
        [Fact]
        public void Write_RejectsDuplicateEntryNames()
        {
            using var stream = new MemoryStream();

            var ex = Assert.Throws<ArgumentException>(() => ResourcePack.Write(stream, new[]
            {
                new KeyValuePair<string, byte[]>("en.1", new byte[] { 1 }),
                new KeyValuePair<string, byte[]>("en.1", new byte[] { 2 }),
            }));
            Assert.Contains("Duplicate", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// A pack reads its directory once and then reopens the stream for each entry, so it relies
        /// on every stream the factory hands back being the same. This drives the defence for a
        /// factory that breaks that contract; without it a short read would return a zero-padded
        /// buffer that fails later as a GZip error nowhere near the cause.
        /// </summary>
        [Fact]
        public void OpenEntry_RejectsAStreamThatShrinksAfterTheDirectoryWasRead()
        {
            using var full = new MemoryStream();
            ResourcePack.Write(full, new[]
            {
                new KeyValuePair<string, byte[]>("en.1", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }),
            });
            var bytes = full.ToArray();

            var opened = 0;
            var pack = ResourcePack.Open(() =>
            {
                // First call reads the directory and must see the whole pack; later calls are the
                // per-entry reads and get a truncated view.
                var take = opened++ == 0 ? bytes.Length : bytes.Length - 4;
                return new MemoryStream(bytes, 0, take, writable: false);
            });

            Assert.NotNull(pack);
            // The view is handed out fine; the short read surfaces when the caller drains it.
            using var entry = pack!.OpenEntry("en.1");
            Assert.NotNull(entry);
            using var sink = new MemoryStream();
            var ex = Assert.Throws<InvalidDataException>(() => entry!.CopyTo(sink));
            Assert.Contains("truncated", ex.Message, StringComparison.Ordinal);
        }
    }
}
