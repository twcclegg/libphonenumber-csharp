#nullable disable
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
using System.Linq;

namespace PhoneNumbers
{
    /// <summary>
    /// Flyweight area code map storage strategy that uses a table to store unique strings and shorts to
    /// store the prefix and description indexes when possible. It is particularly space-efficient when
    /// the provided area code map contains a lot of description redundant descriptions.
    /// </summary>
    /// <remarks>Author: Philippe Liard</remarks>
    public class FlyweightMapStorage : AreaCodeMapStorageStrategy
    {
        // Size of short and integer types in bytes.
        private const int ShortNumBytes = sizeof(short);

        private static readonly int IntNumBytes = sizeof(int);

        // The number of bytes used to store a description index. It is computed from the size of the
        // description pool containing all the strings.
        private int descIndexSizeInBytes;

        private ByteBuffer descriptionIndexes;

        // Sorted string array of unique description strings.
        private string[] descriptionPool;

        private ByteBuffer phoneNumberPrefixes;

        // The number of bytes used to store a phone number prefix.
        private int prefixSizeInBytes;

        public override int GetPrefix(int index)
        {
            return ReadWordFromBuffer(phoneNumberPrefixes, prefixSizeInBytes, index);
        }

        public override int GetStorageSize()
        {
            return phoneNumberPrefixes.GetCapacity() + descriptionIndexes.GetCapacity()
                   + descriptionPool.Sum(d => d.Length);
        }

        /// <summary>
        /// This implementation returns the same string (same identity) when called for multiple indexes
        /// corresponding to prefixes that have the same description.
        /// </summary>
        public override string GetDescription(int index)
        {
            var indexInDescriptionPool =
                ReadWordFromBuffer(descriptionIndexes, descIndexSizeInBytes, index);
            return descriptionPool[indexInDescriptionPool];
        }

        public override void ReadFromSortedMap(SortedDictionary<int, string> areaCodeMap)
        {
            var descriptionsSet = new HashSet<string>();
            NumOfEntries = areaCodeMap.Count;
            prefixSizeInBytes = GetOptimalNumberOfBytesForValue(areaCodeMap.Keys.Last());
            phoneNumberPrefixes = new ByteBuffer(NumOfEntries * prefixSizeInBytes);

            // Fill the phone number prefixes byte buffer, the set of possible lengths of prefixes and the
            // description set.
            var index = 0;
            var possibleLengthsSet = new HashSet<int>();
            foreach (var entry in areaCodeMap)
            {
                var prefix = entry.Key;
                StoreWordInBuffer(phoneNumberPrefixes, prefixSizeInBytes, index, prefix);
                var lengthOfPrefixRef = (int) Math.Log10(prefix) + 1;
                possibleLengthsSet.Add(lengthOfPrefixRef);
                descriptionsSet.Add(entry.Value);
                index++;
            }
            PossibleLengths.Clear();
            PossibleLengths.AddRange(possibleLengthsSet);
            PossibleLengths.Sort();
            CreateDescriptionPool(descriptionsSet, areaCodeMap);
        }

        /// <summary>
        /// Creates the description pool from the provided set of string descriptions and area code map.
        /// </summary>
        private void CreateDescriptionPool(HashSet<string> descriptionsSet, SortedDictionary<int, string> areaCodeMap)
        {
            // Create the description pool.
            descIndexSizeInBytes = GetOptimalNumberOfBytesForValue(descriptionsSet.Count - 1);
            descriptionIndexes = new ByteBuffer(NumOfEntries * descIndexSizeInBytes);
            descriptionPool = descriptionsSet.ToArray();
            Array.Sort(descriptionPool);

            // Map the phone number prefixes to the descriptions.
            var index = 0;
            for (var i = 0; i < NumOfEntries; i++)
            {
                var prefix = ReadWordFromBuffer(phoneNumberPrefixes, prefixSizeInBytes, i);
                var description = areaCodeMap[prefix];
                var positionInDescriptionPool = Array.BinarySearch(descriptionPool, description);
                StoreWordInBuffer(descriptionIndexes, descIndexSizeInBytes, index,
                    positionInDescriptionPool);
                index++;
            }
        }

        /// <summary>
        /// Gets the minimum number of bytes that can be used to store the provided <c>value</c>.
        /// </summary>
        private static int GetOptimalNumberOfBytesForValue(int value)
        {
            return value <= short.MaxValue ? ShortNumBytes : IntNumBytes;
        }

        /// <summary>
        /// Stores the provided <c>value</c> to the provided byte <c>buffer</c> at the specified <c>index</c> using the provided <c>wordSize</c> in bytes. Note that only integer and short sizes are
        /// supported.
        /// </summary>
        /// <param name="buffer">the byte buffer to which the value is stored</param>
        /// <param name="wordSize">the number of bytes used to store the provided value</param>
        /// <param name="index">the index to which the value is stored</param>
        /// <param name="value">the value that is stored assuming it does not require more than the specified number of bytes.</param>
        private static void StoreWordInBuffer(ByteBuffer buffer, int wordSize, int index, int value)
        {
            index *= wordSize;
            if (wordSize == ShortNumBytes)
                buffer.PutShort(index, (short) value);
            else
                buffer.PutInt(index, value);
        }

        /// <summary>
        /// Reads the <c>value</c> at the specified <c>index</c> from the provided byte <c>buffer</c>.
        /// Note that only integer and short sizes are supported.
        /// </summary>
        /// <param name="buffer">the byte buffer from which the value is read</param>
        /// <param name="wordSize">the number of bytes used to store the value</param>
        /// <param name="index">the index where the value is read from</param>
        /// <returns>the value read from the buffer</returns>
        private static int ReadWordFromBuffer(ByteBuffer buffer, int wordSize, int index)
        {
            index *= wordSize;
            return wordSize == ShortNumBytes ? buffer.GetShort(index) : buffer.GetInt(index);
        }

        /// <summary>
        /// Fixed-size scratch buffer of 16- and 32-bit words. Reads land on the binary-search path in
        /// <see cref="AreaCodeMap.Lookup"/>, so each one is a pair of array reads rather than a stream
        /// seek through a BinaryReader. Byte order is little-endian, matching what the BinaryWriter
        /// this replaced produced; the buffer never leaves the process, so only self-consistency
        /// actually matters.
        /// </summary>
        private sealed class ByteBuffer
        {
            private readonly byte[] bytes;

            public ByteBuffer(int size)
            {
                bytes = new byte[size];
            }

            public void PutShort(int offset, short value)
            {
                bytes[offset] = (byte)value;
                bytes[offset + 1] = (byte)(value >> 8);
            }

            public void PutInt(int offset, int value)
            {
                bytes[offset] = (byte)value;
                bytes[offset + 1] = (byte)(value >> 8);
                bytes[offset + 2] = (byte)(value >> 16);
                bytes[offset + 3] = (byte)(value >> 24);
            }

            public short GetShort(int offset)
            {
                return (short)(bytes[offset] | (bytes[offset + 1] << 8));
            }

            public int GetInt(int offset)
            {
                return bytes[offset]
                    | (bytes[offset + 1] << 8)
                    | (bytes[offset + 2] << 16)
                    | (bytes[offset + 3] << 24);
            }

            public int GetCapacity()
            {
                return bytes.Length;
            }
        }
    }
}
