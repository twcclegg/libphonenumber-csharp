#nullable enable
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PhoneNumbers
{
    /// <summary>
    /// A name-to-bytes archive holding one data set (all the geocoding maps, say) in a single
    /// embedded resource instead of one resource per file.
    /// <para>
    /// The point is the trimmer. An <c>ILLink.Substitutions.xml</c> <c>&lt;resource&gt;</c> element
    /// matches a resource by its exact name and supports no wildcard: an entry of
    /// <c>PhoneNumbers.geocoding.*</c> is reported as not found (IL2040) rather than expanded. With
    /// one resource per file that meant enumerating ~690 names, regenerated on every metadata sync;
    /// packing each data set into one resource makes the removable set four fixed names that never
    /// change. For these three data sets it also collapses ~690 manifest entries into three and
    /// lets the build's incremental check name its real output; the per-region phone metadata is
    /// untouched and still one resource each, on a sentinel output.
    /// </para>
    /// <para>
    /// <c>ZipArchive</c> would give all of this for free -- named entries, per-entry deflate,
    /// <c>GetEntry(name).Open()</c> as a bounded sub-stream -- and was measured rather than assumed.
    /// It loses on the one number this whole feature exists to shrink: the data is 31 KB larger as
    /// zip entries, and referencing <c>ZipArchive</c> adds ~66 KB of trimmed IL
    /// (<c>System.IO.Compression</c> goes 34 KB to 100 KB). That is ~97 KB on an opted-out
    /// <c>PhoneNumbers.dll</c> of ~259 KB, charged to every consumer including those who never opt
    /// out. Hence the bespoke format.
    /// </para>
    /// <para>
    /// Only the directory is held in memory. Entry bytes are read from the resource stream on
    /// demand and not retained, because an embedded resource stream is a view over the
    /// already-mapped assembly image: materialising every entry up front moves the whole data set
    /// onto the GC heap, which measured as 1.9 MB of permanently retained memory for a process that
    /// geocodes (1511 KB to 3430 KB). Entries also stay individually compressed, so a caller
    /// decompresses only the one it asked for.
    /// </para>
    /// </summary>
    internal sealed class ResourcePack
    {
        // Spells 'PNPK' as an int, but BinaryWriter writes little-endian so the first four
        // bytes on disk are 4B 50 4E 50 ("KPNP"). Bumped only if the layout below changes
        // incompatibly.
        internal const int FormatMagic = 0x504E504B;
        internal const byte FormatVersion = 1;

        private readonly Func<Stream?> _openStream;
        private readonly Dictionary<string, (int Offset, int Length)> _directory;
        private readonly long _payloadStart;

        private ResourcePack(
            Func<Stream?> openStream, Dictionary<string, (int Offset, int Length)> directory, long payloadStart)
        {
            _openStream = openStream;
            _directory = directory;
            _payloadStart = payloadStart;
        }

        /// <summary>Entry names, in no particular order. Read-only after construction.</summary>
        internal Dictionary<string, (int Offset, int Length)>.KeyCollection Names => _directory.Keys;

        /// <summary>
        /// Loads the directory of a pack held in an embedded resource, or returns null when the
        /// resource is not there.
        /// <para>
        /// Absent is a supported state, not a failure: a build that opted out of a data set has had
        /// the trimmer remove exactly this resource, and the caller turns the null into the
        /// documented "this data was trimmed out" error rather than a missing-manifest-resource
        /// exception that would not say what to do about it.
        /// </para>
        /// </summary>
        internal static ResourcePack? FromAssembly(Assembly assembly, string resourceName)
            => Open(() => assembly.GetManifestResourceStream(resourceName));

        /// <summary>
        /// Reads a pack's directory. <paramref name="openStream"/> is called once here and again for
        /// every entry read, so it must return a fresh, seekable stream each time.
        /// </summary>
        internal static ResourcePack? Open(Func<Stream?> openStream)
        {
            if (openStream is null) throw new ArgumentNullException(nameof(openStream));

            using var stream = openStream();
            if (stream is null)
                return null;

            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var magic = reader.ReadInt32();
            if (magic != FormatMagic)
                throw new InvalidDataException(
                    $"Unexpected resource pack magic 0x{magic:X8}, expected 0x{FormatMagic:X8}.");
            var version = reader.ReadByte();
            if (version != FormatVersion)
                throw new InvalidDataException(
                    $"Unsupported resource pack version {version} (expected {FormatVersion}).");

            var count = reader.ReadInt32();
            // A corrupt header must not be trusted into an allocation. The smallest a directory
            // entry can be is nine bytes -- a one-byte 7-bit length prefix for an empty name, plus
            // two Int32s -- so a count needing more than the remaining bytes is nonsense, and
            // without this check a garbage value sizes the dictionary before anything is read.
            const int MinimumDirectoryEntryBytes = 9;
            var remaining = stream.Length - stream.Position;
            if (count < 0 || count > remaining / MinimumDirectoryEntryBytes)
                throw new InvalidDataException(
                    $"Resource pack declares {count} entries, which does not fit the remaining " +
                    $"{remaining} bytes.");

            var directory = new Dictionary<string, (int Offset, int Length)>(count, StringComparer.Ordinal);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var name = reader.ReadString();
                    var offset = reader.ReadInt32();
                    var length = reader.ReadInt32();
                    if (offset < 0 || length < 0)
                        throw new InvalidDataException($"Resource pack entry '{name}' has a negative offset or length.");
                    // Duplicate names would silently drop one entry's data and then fail far away, as a
                    // missing prefix map rather than as a malformed pack.
                    if (directory.ContainsKey(name))
                        throw new InvalidDataException($"Resource pack contains duplicate entry '{name}'.");
                    directory[name] = (offset, length);
                }
            }
            // The count guard above only catches a truncation short enough to shrink the whole
            // stream below the declared directory size; a real pack's directory is kilobytes into a
            // megabyte file, so running off the end here is the normal way a truncated pack fails.
            // BinaryReader reports that as EndOfStreamException (an IOException) and a corrupt
            // 7-bit length prefix as FormatException, neither of which a caller catching
            // InvalidDataException would see.
            catch (Exception e) when (e is EndOfStreamException or FormatException)
            {
                throw new InvalidDataException("Resource pack directory is malformed or truncated.", e);
            }

            var payloadStart = stream.Position;
            var available = stream.Length - payloadStart;
            foreach (var pair in directory)
            {
                // Bounds-checked here, once, rather than per read: an entry claiming a huge length
                // would otherwise allocate that buffer before the short-read check could fire.
                if (pair.Value.Offset + (long)pair.Value.Length > available)
                    throw new InvalidDataException(
                        $"Resource pack entry '{pair.Key}' runs past the end of the pack " +
                        $"(offset {pair.Value.Offset}, length {pair.Value.Length}, {available} bytes of payload).");
            }

            return new ResourcePack(openStream, directory, payloadStart);
        }

        /// <summary>
        /// A read-only view over one entry's bytes, or null when the pack has no such entry. The
        /// caller owns the returned stream.
        /// <para>
        /// A view rather than a <c>byte[]</c> so nothing is copied onto the managed heap. An
        /// embedded resource stream is already a window onto the mapped assembly image, and the
        /// largest entries are well over the 85,000-byte large-object threshold -- in the shipped
        /// geocoding pack <c>zh.86</c> is 502 KB and <c>en.1</c>, the map a US lookup needs, is
        /// 175 KB -- so copying would put those straight on the LOH on first use of each region.
        /// Both callers hand the result to a <see cref="System.IO.Compression.GZipStream"/> and
        /// never want an array.
        /// </para>
        /// </summary>
        internal Stream? OpenEntry(string name)
        {
            if (!_directory.TryGetValue(name, out var entry))
                return null;

            var stream = _openStream()
                ?? throw new InvalidDataException(
                    $"Resource pack stream for entry '{name}' could not be reopened.");
            try
            {
                stream.Position = _payloadStart + entry.Offset;
                return new EntryStream(stream, entry.Length, name);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Bounds reads to one entry. Necessary rather than tidy: <c>GZipStream</c> supports
        /// concatenated members, so handed an unbounded stream it would run past this entry's
        /// trailer and start decoding the next entry as more of the same member.
        /// </summary>
        private sealed class EntryStream : Stream
        {
            private readonly Stream _inner;
            private readonly string _name;
            private readonly long _length;
            private long _position;

            internal EntryStream(Stream inner, long length, string name)
            {
                _inner = inner;
                _length = length;
                _name = name;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _length;

            public override long Position
            {
                get => _position;
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var remaining = _length - _position;
                if (remaining <= 0)
                    return 0;
                if (count > remaining)
                    count = (int)remaining;

                var read = _inner.Read(buffer, offset, count);
                if (read <= 0)
                    // A truncated pack must fail loudly rather than decompress a short buffer into
                    // junk a long way from the cause.
                    throw new InvalidDataException(
                        $"Resource pack entry '{_name}' is truncated: expected {_length} bytes, got {_position}.");

                _position += read;
                return read;
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _inner.Dispose();
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Writes entries as a single pack. Entries are sorted by ordinal name, and nothing derived
        /// from the clock, the filesystem or the host is written, so the same inputs always produce
        /// byte-identical output. CI packs twice and fails the build if any assembly differs by
        /// hash, which a non-deterministic resource would break with nothing obvious to point at.
        /// </summary>
        internal static void Write(Stream stream, IReadOnlyList<KeyValuePair<string, byte[]>> entries)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            if (entries is null) throw new ArgumentNullException(nameof(entries));

            var ordered = new List<KeyValuePair<string, byte[]>>(entries);
            ordered.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));

            // Reject here as well as in Open. Writing a duplicate would otherwise fail at run time
            // in the shipped assembly rather than at build time, and because List<T>.Sort is an
            // unstable introsort two equal names would also order unpredictably, breaking the
            // byte-for-byte reproducibility CI checks by packing twice.
            for (var i = 1; i < ordered.Count; i++)
            {
                if (string.Equals(ordered[i - 1].Key, ordered[i].Key, StringComparison.Ordinal))
                    throw new ArgumentException(
                        $"Duplicate resource pack entry '{ordered[i].Key}'.", nameof(entries));
            }

            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(FormatMagic);
            writer.Write(FormatVersion);
            writer.Write(ordered.Count);

            // Offsets are relative to the start of the payload block rather than to the start of
            // the stream, so the directory can be written before its own size is known.
            var offset = 0;
            foreach (var entry in ordered)
            {
                writer.Write(entry.Key);
                writer.Write(offset);
                writer.Write(entry.Value.Length);
                offset += entry.Value.Length;
            }

            foreach (var entry in ordered)
                writer.Write(entry.Value);
        }
    }
}
