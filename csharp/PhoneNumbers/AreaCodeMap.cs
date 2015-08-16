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
        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();

        private AreaCodeMapStorageStrategy areaCodeMapStorage;

        public AreaCodeMapStorageStrategy getAreaCodeMapStorage()
        {
            return areaCodeMapStorage;
        }

        /**
         * Creates an empty {@link AreaCodeMap}. The default constructor is necessary for implementing
         * {@link Externalizable}. The empty map could later be populated by
         * {@link #readAreaCodeMap(java.util.SortedMap)} or {@link #readExternal(java.io.ObjectInput)}.
         */
        public AreaCodeMap() {}

        /**
         * Gets the size of the provided area code map storage. The map storage passed-in will be filled
         * as a result.
         */
        private static int getSizeOfAreaCodeMapStorage(AreaCodeMapStorageStrategy mapStorage,
            SortedDictionary<int, String> areaCodeMap)
        {
            mapStorage.readFromSortedMap(areaCodeMap);
            return mapStorage.getStorageSize();
        }

        private AreaCodeMapStorageStrategy createDefaultMapStorage()
        {
            return new DefaultMapStorage();
        }

        private AreaCodeMapStorageStrategy createFlyweightMapStorage()
        {
            return new FlyweightMapStorage();
        }

        /**
         * Gets the smaller area code map storage strategy according to the provided area code map. It
         * actually uses (outputs the data to a stream) both strategies and retains the best one which
         * make this method quite expensive.
         */
        // @VisibleForTesting
        public AreaCodeMapStorageStrategy getSmallerMapStorage(SortedDictionary<int, String> areaCodeMap)
        {
            AreaCodeMapStorageStrategy flyweightMapStorage = createFlyweightMapStorage();
            int sizeOfFlyweightMapStorage = getSizeOfAreaCodeMapStorage(flyweightMapStorage, areaCodeMap);

            AreaCodeMapStorageStrategy defaultMapStorage = createDefaultMapStorage();
            int sizeOfDefaultMapStorage = getSizeOfAreaCodeMapStorage(defaultMapStorage, areaCodeMap);

            return sizeOfFlyweightMapStorage < sizeOfDefaultMapStorage
                ? flyweightMapStorage : defaultMapStorage;
        }

        /**
         * Creates an {@link AreaCodeMap} initialized with {@code sortedAreaCodeMap}.  Note that the
         * underlying implementation of this method is expensive thus should not be called by
         * time-critical applications.
         *
         * @param sortedAreaCodeMap  a map from phone number prefixes to descriptions of corresponding
         *     geographical areas, sorted in ascending order of the phone number prefixes as integers.
         */
        public void readAreaCodeMap(SortedDictionary<int, String> sortedAreaCodeMap)
        {
            areaCodeMapStorage = getSmallerMapStorage(sortedAreaCodeMap);
        }

        /**
         * Returns the description of the geographical area the {@code number} corresponds to. This method
         * distinguishes the case of an invalid prefix and a prefix for which the name is not available in
         * the current language. If the description is not available in the current language an empty
         * string is returned. If no description was found for the provided number, null is returned.
         *
         * @param number  the phone number to look up
         * @return  the description of the geographical area
         */
        public String Lookup(PhoneNumber number)
        {
            int numOfEntries = areaCodeMapStorage.getNumOfEntries();
            if (numOfEntries == 0)
            {
                return null;
            }
            long phonePrefix =
                long.Parse(number.CountryCode + phoneUtil.GetNationalSignificantNumber(number));
            int currentIndex = numOfEntries - 1;
            List<int> currentSetOfLengths = areaCodeMapStorage.getPossibleLengths();
            var length = currentSetOfLengths.Count;
            while (length > 0)
            {
                int possibleLength = currentSetOfLengths[length - 1];
                String phonePrefixStr = phonePrefix.ToString();
                if (phonePrefixStr.Length > possibleLength)
                {
                    phonePrefix = long.Parse(phonePrefixStr.Substring(0, possibleLength));
                }
                currentIndex = binarySearch(0, currentIndex, phonePrefix);
                if (currentIndex < 0)
                {
                    return null;
                }
                int currentPrefix = areaCodeMapStorage.getPrefix(currentIndex);
                if (phonePrefix == currentPrefix)
                {
                    return areaCodeMapStorage.getDescription(currentIndex);
                }
                while (length > 0 && currentSetOfLengths[length - 1] >= possibleLength)
                    length--;
            }
            return null;
        }

        /**
         * Does a binary search for {@code value} in the provided array from {@code start} to {@code end}
         * (inclusive). Returns the position if {@code value} is found; otherwise, returns the
         * position which has the largest value that is less than {@code value}. This means if
         * {@code value} is the smallest, -1 will be returned.
         */
        private int binarySearch(int start, int end, long value)
        {
            int current = 0;
            while (start <= end)
            {
                current = (start + end) / 2;
                int currentValue = areaCodeMapStorage.getPrefix(current);
                if (currentValue == value)
                {
                    return current;
                }
                else if (currentValue > value)
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
            return areaCodeMapStorage.ToString();
        }
    }
}