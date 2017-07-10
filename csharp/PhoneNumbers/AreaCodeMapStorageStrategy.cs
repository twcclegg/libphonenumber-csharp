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

using System.Collections.Generic;
using System.Text;

namespace PhoneNumbers
{
    /**
    * Abstracts the way area code data is stored into memory and serialized to a stream. It is used by
    * {@link AreaCodeMap} to support the most space-efficient storage strategy according to the
    * provided data.
    *
    * @author Philippe Liard
    */
    public abstract class AreaCodeMapStorageStrategy
    {
        protected int NumOfEntries = 0;
        protected readonly List<int> PossibleLengths = new List<int>();

        /**
         * Gets the phone number prefix located at the provided {@code index}.
         *
         * @param index  the index of the prefix that needs to be returned
         * @return  the phone number prefix at the provided index
         */
        public abstract int GetPrefix(int index);

        public abstract int GetStorageSize();

        /**
         * Gets the description corresponding to the phone number prefix located at the provided {@code
         * index}. If the description is not available in the current language an empty string is
         * returned.
         *
         * @param index  the index of the phone number prefix that needs to be returned
         * @return  the description corresponding to the phone number prefix at the provided index
         */
        public abstract string GetDescription(int index);

        /**
         * Sets the internal state of the underlying storage implementation from the provided {@code
         * sortedAreaCodeMap} that maps phone number prefixes to description strings.
         *
         * @param sortedAreaCodeMap  a sorted map that maps phone number prefixes including country
         *    calling code to description strings
         */
        public abstract void ReadFromSortedMap(SortedDictionary<int, string> sortedAreaCodeMap);

        /**
         * @return  the number of entries contained in the area code map
         */
        public int GetNumOfEntries()
        {
            return NumOfEntries;
        }

        /**
         * @return  the set containing the possible lengths of prefixes
         */
        public List<int> GetPossibleLengths()
        {
            return PossibleLengths;
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < GetNumOfEntries(); i++)
            {
                output.Append(GetPrefix(i))
                    .Append("|")
                    .Append(GetDescription(i))
                    .Append("\n");
            }
            return output.ToString();
        }

    }
}