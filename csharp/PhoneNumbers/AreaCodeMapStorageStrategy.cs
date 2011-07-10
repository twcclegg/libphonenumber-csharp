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
    * Abstracts the way area code data is stored into memory and serialized to a stream.
    *
    * @author Philippe Liard
    */
    public abstract class AreaCodeMapStorageStrategy
    {
        protected readonly int countryCallingCode;
        protected readonly bool isLeadingZeroPossible;
        protected int numOfEntries = 0;
        protected readonly List<int> possibleLengths = new List<int>();

        /**
        * Constructs a new area code map storage strategy from the provided country calling code and
        * boolean parameter.
        *
        * @param countryCallingCode  the country calling code of the number prefixes contained in the map
        * @param isLeadingZeroPossible  whether the phone number prefixes belong to a region which
        *    {@link PhoneNumberUtil#isLeadingZeroPossible isLeadingZeroPossible}
        */
        public AreaCodeMapStorageStrategy(int countryCallingCode, bool isLeadingZeroPossible)
        {
            this.countryCallingCode = countryCallingCode;
            this.isLeadingZeroPossible = isLeadingZeroPossible;
        }

        /**
         * Returns whether the underlying implementation of this abstract class is flyweight.
         * It is expected to be flyweight if it implements the {@code FlyweightMapStorage} class.
         *
         * @return  whether the underlying implementation of this abstract class is flyweight
         */
        public abstract bool isFlyweight();

        /**
         * @return  the number of entries contained in the area code map
         */
        public int getNumOfEntries()
        {
            return numOfEntries;
        }

        /**
         * @return  the set containing the possible lengths of prefixes
         */
        public List<int> getPossibleLengths()
        {
            return possibleLengths;
        }

        /**
         * Gets the phone number prefix located at the provided {@code index}.
         *
         * @param index  the index of the prefix that needs to be returned
         * @return  the phone number prefix at the provided index
         */
        public abstract int getPrefix(int index);

        public abstract int getStorageSize();

        /**
         * Gets the description corresponding to the phone number prefix located at the provided {@code
         * index}.
         *
         * @param index  the index of the phone number prefix that needs to be returned
         * @return  the description corresponding to the phone number prefix at the provided index
         */
        public abstract String getDescription(int index);

        /**
         * Sets the internal state of the underlying storage implementation from the provided {@code
         * sortedAreaCodeMap} that maps phone number prefixes to description strings.
         *
         * @param sortedAreaCodeMap  a sorted map that maps phone number prefixes including country
         *    calling code to description strings
         */
        public abstract void readFromSortedMap(SortedDictionary<int, String> sortedAreaCodeMap);

        /**
         * Utility class used to pass arguments by "reference".
         */
        protected class Reference<T>
        {
            public T data;
        }

        /**
         * Removes the country calling code from the provided {@code prefix} if the country can't have any
         * leading zero; otherwise it is left as it is. Sets the provided {@code lengthOfPrefixRef}
         * parameter to the length of the resulting prefix.
         *
         * @param prefix  a phone number prefix containing a leading country calling code
         * @param lengthOfPrefixRef  a "reference" to an integer set to the length of the resulting
         *    prefix. This parameter is ignored when set to null.
         * @return  the resulting prefix which may have been stripped
         */
        protected int stripPrefix(int prefix, Reference<int> lengthOfPrefixRef)
        {
            int lengthOfCountryCode = (int)Math.Log10(countryCallingCode) + 1;
            int lengthOfPrefix = (int)Math.Log10(prefix) + 1;
            if (!isLeadingZeroPossible)
            {
                lengthOfPrefix -= lengthOfCountryCode;
                prefix -= countryCallingCode * (int)Math.Pow(10, lengthOfPrefix);
            }
            if (lengthOfPrefixRef != null)
            {
                lengthOfPrefixRef.data = lengthOfPrefix;
            }
            return prefix;
        }

        /**
         * Removes the country calling code from the provided {@code prefix} if the country can't have any
         * leading zero; otherwise it is left as it is.
         *
         * @param prefix  a phone number prefix containing a leading country calling code
         * @return  the resulting prefix which may have been stripped
         */
        protected int stripPrefix(int prefix)
        {
            return stripPrefix(prefix, null);
        }
    }
}
