/*
 * Copyright (C) 2010 Google Inc.
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

namespace PhoneNumbers
{
    public class CountryCodeToRegionCodeMap
    {
        // A mapping from a country code to the region codes which denote the
        // country/region represented by that country code. In the case of multiple
        // countries sharing a calling code, such as the NANPA countries, the one
        // indicated with "isMainCountryForCode" in the metadata should be first.
        public static Dictionary<int, List<string>> GetCountryCodeToRegionCodeMap()
        {
            return BuildMetadataFromXml.GetCountryCodeToRegionCodeMap(
                PhoneNumberUtil.META_DATA_FILE_PREFIX);
        }
    }
}
