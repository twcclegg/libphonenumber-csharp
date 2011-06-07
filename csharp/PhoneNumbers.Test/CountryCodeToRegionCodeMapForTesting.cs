/*
 * Copyright (C) 2009 Google Inc.
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

namespace PhoneNumbers.Test
{
    class CountryCodeToRegionCodeMapForTesting
    {
        // A mapping from a country code to the region codes which denote the
        // country/region represented by that country code. In the case of multiple
        // countries sharing a calling code, such as the NANPA countries, the one
        // indicated with "isMainCountryForCode" in the metadata should be first.
        internal static Dictionary<int, List<String>> GetCountryCodeToRegionCodeMap()
        {
            // The capacity is set to 20 as there are 15 different country codes,
            // and this offers a load factor of roughly 0.75.
            var countryCodeToRegionCodeMap = new Dictionary<int, List<String>>(20);

            List<String> listWithRegionCode;
            listWithRegionCode = new List<String>(2);
            listWithRegionCode.Add("US");
            listWithRegionCode.Add("BS");
            countryCodeToRegionCodeMap[1] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("IT");
            countryCodeToRegionCodeMap[39] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GB");
            countryCodeToRegionCodeMap[44] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PL");
            countryCodeToRegionCodeMap[48] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("DE");
            countryCodeToRegionCodeMap[49] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MX");
            countryCodeToRegionCodeMap[52] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AR");
            countryCodeToRegionCodeMap[54] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AU");
            countryCodeToRegionCodeMap[61] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NZ");
            countryCodeToRegionCodeMap[64] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SG");
            countryCodeToRegionCodeMap[65] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("JP");
            countryCodeToRegionCodeMap[81] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("KR");
            countryCodeToRegionCodeMap[82] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AO");
            countryCodeToRegionCodeMap[244] = listWithRegionCode;

            listWithRegionCode = new List<String>(2);
            listWithRegionCode.Add("RE");
            listWithRegionCode.Add("YT");
            countryCodeToRegionCodeMap[262] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AD");
            countryCodeToRegionCodeMap[376] = listWithRegionCode;

            return countryCodeToRegionCodeMap;
        }
    }
}
