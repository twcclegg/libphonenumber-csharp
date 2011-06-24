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
using System.Text;

namespace PhoneNumbers
{
    /**
    * A utility that maps phone number prefixes to a string describing the geographical area the prefix
    * covers.
    *
    * @author Shaopeng Jia
    */
    public class AreaCodeMap
    {
        private int numOfEntries = 0;
        private List<int> possibleLengths = new List<int>();
        private int[] phoneNumberPrefixes;
        private String[] descriptions;
        private PhoneNumberUtil phoneUtil;

        /**
         * Creates an empty {@link AreaCodeMap}. The default constructor is necessary for implementing
         * {@link Externalizable}. The empty map could later populated by
         * {@link #readAreaCodeMap(java.util.SortedMap)} or {@link #readExternal(java.io.ObjectInput)}.
         */
        public AreaCodeMap()
        {
            phoneUtil = PhoneNumberUtil.GetInstance();
        }

        // @VisibleForTesting
        public AreaCodeMap(PhoneNumberUtil phoneUtil)
        {
            this.phoneUtil = phoneUtil;
        }

        /**
         * Creates an {@link AreaCodeMap} initialized with {@code sortedAreaCodeMap}.
         *
         * @param sortedAreaCodeMap  a map from phone number prefixes to descriptions of corresponding
         *     geographical areas, sorted in ascending order of the phone number prefixes as integers.
         */
        public void ReadAreaCodeMap(SortedDictionary<int, String> sortedAreaCodeMap)
        {
            numOfEntries = sortedAreaCodeMap.Count;
            phoneNumberPrefixes = new int[numOfEntries];
            descriptions = new String[numOfEntries];
            int index = 0;
            foreach (int prefix in sortedAreaCodeMap.Keys)
            {
                phoneNumberPrefixes[index++] = prefix;
                possibleLengths.Add((int)Math.Log10(prefix) + 1);
            }
            possibleLengths = possibleLengths.Distinct().OrderBy(a => a).ToList();

            sortedAreaCodeMap.Values.CopyTo(descriptions, 0);
        }

        /**
         * Returns the description of the geographical area the {@code number} corresponds to.
         *
         * @param number  the phone number to look up
         * @return  the description of the geographical area
         */
        public String Lookup(PhoneNumber number)
        {
            if (numOfEntries == 0)
            {
                return "";
            }
            long phonePrefix =
                long.Parse(number.CountryCode + phoneUtil.GetNationalSignificantNumber(number));
            int currentIndex = numOfEntries - 1;
            List<int> currentSetOfLengths = possibleLengths;
            int maxIndex = currentSetOfLengths.Count();

            while (maxIndex > 0)
            {
                var possibleLength = currentSetOfLengths[maxIndex - 1];
                String phonePrefixStr = phonePrefix.ToString();
                if (phonePrefixStr.Length > possibleLength)
                {
                    phonePrefix = long.Parse(phonePrefixStr.Substring(0, possibleLength));
                }
                currentIndex = BinarySearch(0, currentIndex, phonePrefix);
                if (currentIndex < 0)
                {
                    return "";
                }
                if (phonePrefix == phoneNumberPrefixes[currentIndex])
                {
                    return descriptions[currentIndex];
                }
                while (maxIndex > 0 && currentSetOfLengths[maxIndex - 1] >= possibleLength)
                {
                    maxIndex--;
                }
            }
            return "";
        }

        /**
         * Does a binary search for {@code value} in the phoneNumberPrefixes array from {@code start} to
         * {@code end} (inclusive). Returns the position if {@code value} is found; otherwise, returns the
         * position which has the largest value that is less than {@code value}. This means if
         * {@code value} is the smallest, -1 will be returned.
         */
        private int BinarySearch(int start, int end, long value)
        {
            int current = 0;
            while (start <= end)
            {
                current = (start + end) / 2;
                if (phoneNumberPrefixes[current] == value)
                {
                    return current;
                }
                else if (phoneNumberPrefixes[current] > value)
                {
                    current--;
                    end = current;
                }
                else
                {
                    start = current + 1;
                }
            }
            return current;
        }

        /**
         * Dumps the mappings contained in the area code map.
         */
        public override String ToString()
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < numOfEntries; i++)
            {
                output.Append(phoneNumberPrefixes[i]);
                output.Append("|");
                output.Append(descriptions[i]);
                output.Append("\n");
            }
            return output.ToString();
        }
    }
}
