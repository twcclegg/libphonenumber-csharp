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
    /// change. It also cuts the manifest from ~690 entries to a handful and lets the build's
    /// incremental checks name their real output instead of a sentinel file.
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
        // 'P','N','P','K'. Bumped only if the layout below changes incompatibly.
        internal const int FormatMagic = 0x504E504B;
        internal const byte FormatVersion = 1;

        private readonly Func<Stream?> openStream;
        private readonly Dictionary<string, Entry> directory;
        private readonly long payloadStart;

        private readonly struct Entry
        {
            internal Entry(int offset, int length) { Offset = offset; Length = length; }
            internal int Offset { get; }
            internal int Length { get; }
        }

        private ResourcePack(Func<Stream?> openStream, Dictionary<string, Entry> directory, long payloadStart)
        {
            this.openStream = openStream;
            this.directory = directory;
            this.payloadStart = payloadStart;
        }

        /// <summary>
        /// Entry names, in no particular order. Typed as <see cref="IEnumerable{T}"/> rather than the
        /// dictionary's own key collection because <c>Entry</c> is private; enumerated once per
        /// reader, so the boxed enumerator does not matter.
        /// </summary>
        internal IEnumerable<string> Names => directory.Keys;

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
            // A corrupt header must not be trusted into an allocation. Every entry costs at least
            // one byte of directory, so a count past the remaining length is nonsense, and without
            // this check a garbage value sizes the dictionary before anything is read.
            if (count < 0 || count > stream.Length - stream.Position)
                throw new InvalidDataException(
                    $"Resource pack declares {count} entries, which does not fit the remaining " +
                    $"{stream.Length - stream.Position} bytes.");

            var directory = new Dictionary<string, Entry>(count, StringComparer.Ordinal);
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
                directory[name] = new Entry(offset, length);
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
        /// The bytes of one entry, or null when the pack has no such entry. Deliberately not cached:
        /// every caller decompresses the result into a structure it caches itself.
        /// </summary>
        internal byte[]? Read(string name)
        {
            if (!directory.TryGetValue(name, out var entry))
                return null;

            using var stream = openStream()
                ?? throw new InvalidDataException(
                    $"Resource pack stream for entry '{name}' could not be reopened.");
            stream.Position = payloadStart + entry.Offset;

            var buffer = new byte[entry.Length];
            var read = 0;
            while (read < entry.Length)
            {
                // Stream.Read is allowed to return a short count; a truncated pack must fail loudly
                // rather than hand back a zero-padded buffer that decompresses to junk.
                var n = stream.Read(buffer, read, entry.Length - read);
                if (n <= 0)
                    throw new InvalidDataException(
                        $"Resource pack entry '{name}' is truncated: expected {entry.Length} bytes, got {read}.");
                read += n;
            }
            return buffer;
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
