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
using System.IO;
using System.Linq;
using System.Text;

namespace PhoneNumbers
{
    /**
    * Flyweight area code map storage strategy that uses a table to store unique strings and shorts to
    * store the prefix and description indexes when possible. It is particularly space-efficient when
    * the provided area code map contains a lot of description redundant descriptions.
    *
    * @author Philippe Liard
    */
    class FlyweightMapStorage : AreaCodeMapStorageStrategy
    {
        class ByteBuffer
        {
            private MemoryStream stream;
            private BinaryReader reader;
            private BinaryWriter writer;

            public ByteBuffer(int size)
            {
                stream = new MemoryStream(new byte[size]);
                reader = new BinaryReader(stream);
                writer = new BinaryWriter(stream);
            }

            public void putShort(int offset, short value)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                writer.Write(value);
            }

            public void putInt(int offset, int value)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                writer.Write(value);
            }

            public short getShort(int offset)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                return reader.ReadInt16();
            }

            public int getInt(int offset)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                return reader.ReadInt32();
            }

            public int getCapacity()
            {
                return stream.Capacity;
            }
        }

        // Size of short and integer types in bytes.
        private static readonly int SHORT_SIZE = sizeof(short);
        private static readonly int INT_SIZE = sizeof(int);

        // The number of bytes used to store a phone number prefix.
        private int prefixSizeInBytes;
        // The number of bytes used to store a description index. It is computed from the size of the
        // description pool containing all the strings.
        private int descIndexSizeInBytes;

        private ByteBuffer phoneNumberPrefixes;
        private ByteBuffer descriptionIndexes;

        // Sorted string array of unique description strings.
        private String[] descriptionPool;

        public FlyweightMapStorage()
        {
        }

        public override bool isFlyweight()
        {
            return true;
        }

        /**
         * Gets the minimum number of bytes that can be used to store the provided {@code value}.
         */
        private static int getOptimalNumberOfBytesForValue(int value)
        {
            return value <= short.MaxValue ? SHORT_SIZE : INT_SIZE;
        }

        /**
         * Stores the provided {@code value} to the provided byte {@code buffer} at the specified {@code
         * index} using the provided {@code wordSize} in bytes. Note that only integer and short sizes are
         * supported.
         *
         * @param buffer  the byte buffer to which the value is stored
         * @param wordSize  the number of bytes used to store the provided value
         * @param index  the index to which the value is stored
         * @param value  the value that is stored assuming it does not require more than the specified
         *    number of bytes.
         */
        private static void storeWordInBuffer(ByteBuffer buffer, int wordSize, int index, int value)
        {
            index *= wordSize;
            if (wordSize == SHORT_SIZE)
            {
                buffer.putShort(index, (short)value);
            }
            else
            {
                buffer.putInt(index, value);
            }
        }

        /**
         * Reads the {@code value} at the specified {@code index} from the provided byte {@code buffer}.
         * Note that only integer and short sizes are supported.
         *
         * @param buffer  the byte buffer from which the value is read
         * @param wordSize  the number of bytes used to store the value
         * @param index  the index where the value is read from
         *
         * @return  the value read from the buffer
         */
        private static int readWordFromBuffer(ByteBuffer buffer, int wordSize, int index)
        {
            index *= wordSize;
            return wordSize == SHORT_SIZE ? buffer.getShort(index) : buffer.getInt(index);
        }

        public override int getPrefix(int index)
        {
            return readWordFromBuffer(phoneNumberPrefixes, prefixSizeInBytes, index);
        }

        public override int getStorageSize()
        {
            return phoneNumberPrefixes.getCapacity() + descriptionIndexes.getCapacity()
                + descriptionPool.Sum(d => d.Length);
        }

        public override String getDescription(int index)
        {
            return descriptionPool[readWordFromBuffer(descriptionIndexes, descIndexSizeInBytes, index)];
        }

        public override void readFromSortedMap(SortedDictionary<int, String> sortedAreaCodeMap)
        {
            var descriptionsSet = new HashSet<String>();
            var descriptionsList = new List<String>();
            numOfEntries = sortedAreaCodeMap.Count;
            prefixSizeInBytes = getOptimalNumberOfBytesForValue(sortedAreaCodeMap.Keys.Last());
            phoneNumberPrefixes = new ByteBuffer(numOfEntries * prefixSizeInBytes);

            // Fill the phone number prefixes byte buffer, the set of possible lengths of prefixes and the
            // description set.
            int index = 0;
            var possibleLengthsSet = new HashSet<int>();
            foreach (var entry in sortedAreaCodeMap)
            {
                int prefix = entry.Key;
                storeWordInBuffer(phoneNumberPrefixes, prefixSizeInBytes, index++, prefix);
                var lengthOfPrefixRef = (int)Math.Log10(prefix) + 1;
                if (!possibleLengthsSet.Contains(lengthOfPrefixRef))
                {
                    possibleLengthsSet.Add(lengthOfPrefixRef);
                    possibleLengths.Add(lengthOfPrefixRef);
                }
                if (!descriptionsSet.Contains(entry.Value))
                {
                    descriptionsSet.Add(entry.Value);
                    descriptionsList.Add(entry.Value);
                }
            }

            // Create the description pool.
            descIndexSizeInBytes = getOptimalNumberOfBytesForValue(descriptionsList.Count - 1);
            descriptionIndexes = new ByteBuffer(numOfEntries * descIndexSizeInBytes);
            descriptionPool = descriptionsList.ToArray();

            // Map the phone number prefixes to the descriptions.
            index = 0;
            for (int i = 0; i < numOfEntries; i++)
            {
                int prefix = readWordFromBuffer(phoneNumberPrefixes, prefixSizeInBytes, i);
                String description = sortedAreaCodeMap[prefix];
                int positionInDescriptionPool = Array.BinarySearch(descriptionPool, description);
                storeWordInBuffer(descriptionIndexes, descIndexSizeInBytes, index++,
                                  positionInDescriptionPool);
            }
        }
    }
}