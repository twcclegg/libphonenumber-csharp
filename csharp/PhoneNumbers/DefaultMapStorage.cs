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
    /**
    * Default area code map storage strategy that is used for data not containing description
    * duplications. It is mainly intended to avoid the overhead of the string table management when it
    * is actually unnecessary (i.e no string duplication).
    *
    * @author Shaopeng Jia
    */
    public class DefaultMapStorage : AreaCodeMapStorageStrategy
    {
        private int[] phoneNumberPrefixes;
        private string[] descriptions;

        public override int GetPrefix(int index)
        {
            return phoneNumberPrefixes[index];
        }

        public override int GetStorageSize()
        {
            return phoneNumberPrefixes.Length * sizeof(int)
                + descriptions.Sum(d => d.Length);
        }

        public override string GetDescription(int index)
        {
            return descriptions[index];
        }

        public override void ReadFromSortedMap(SortedDictionary<int, string> sortedAreaCodeMap)
        {
            NumOfEntries = sortedAreaCodeMap.Count;
            phoneNumberPrefixes = new int[NumOfEntries];
            descriptions = new string[NumOfEntries];
            int index = 0;
            var possibleLengthsSet = new HashSet<int>();
            foreach (int prefix in sortedAreaCodeMap.Keys)
            {
                phoneNumberPrefixes[index] = prefix;
                descriptions[index] = sortedAreaCodeMap[prefix];
                index++;
                var lengthOfPrefix = (int)Math.Log10(prefix) + 1;
                possibleLengthsSet.Add(lengthOfPrefix);
            }
            PossibleLengths.Clear();
            PossibleLengths.AddRange(possibleLengthsSet);
            PossibleLengths.Sort();
        }
    }
}
