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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhoneNumbers
{
    public class CountryCodeToRegionCodeMap
    {
        // A mapping from a country code to the region codes which denote the
        // country/region represented by that country code. In the case of multiple
        // countries sharing a calling code, such as the NANPA countries, the one
        // indicated with "isMainCountryForCode" in the metadata should be first.
        public static Dictionary<int, List<String>> GetCountryCodeToRegionCodeMap()
        {
            // The capacity is set to 273 as there are 205 different country codes,
            // and this offers a load factor of roughly 0.75.
            var countryCodeToRegionCodeMap = new Dictionary<int, List<String>>(273);

            List<String> listWithRegionCode = new List<String>(24);
            listWithRegionCode.Add("US");
            listWithRegionCode.Add("AG");
            listWithRegionCode.Add("AI");
            listWithRegionCode.Add("AS");
            listWithRegionCode.Add("BB");
            listWithRegionCode.Add("BM");
            listWithRegionCode.Add("BS");
            listWithRegionCode.Add("CA");
            listWithRegionCode.Add("DM");
            listWithRegionCode.Add("DO");
            listWithRegionCode.Add("GD");
            listWithRegionCode.Add("GU");
            listWithRegionCode.Add("JM");
            listWithRegionCode.Add("KN");
            listWithRegionCode.Add("KY");
            listWithRegionCode.Add("LC");
            listWithRegionCode.Add("MP");
            listWithRegionCode.Add("MS");
            listWithRegionCode.Add("PR");
            listWithRegionCode.Add("TC");
            listWithRegionCode.Add("TT");
            listWithRegionCode.Add("VC");
            listWithRegionCode.Add("VG");
            listWithRegionCode.Add("VI");
            countryCodeToRegionCodeMap[1] = listWithRegionCode;

            listWithRegionCode = new List<String>(2);
            listWithRegionCode.Add("RU");
            listWithRegionCode.Add("KZ");
            countryCodeToRegionCodeMap[7] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("EG");
            countryCodeToRegionCodeMap[20] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ZA");
            countryCodeToRegionCodeMap[27] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GR");
            countryCodeToRegionCodeMap[30] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NL");
            countryCodeToRegionCodeMap[31] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BE");
            countryCodeToRegionCodeMap[32] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("FR");
            countryCodeToRegionCodeMap[33] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ES");
            countryCodeToRegionCodeMap[34] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("HU");
            countryCodeToRegionCodeMap[36] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("IT");
            countryCodeToRegionCodeMap[39] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("RO");
            countryCodeToRegionCodeMap[40] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CH");
            countryCodeToRegionCodeMap[41] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AT");
            countryCodeToRegionCodeMap[43] = listWithRegionCode;

            listWithRegionCode = new List<String>(4);
            listWithRegionCode.Add("GB");
            listWithRegionCode.Add("GG");
            listWithRegionCode.Add("IM");
            listWithRegionCode.Add("JE");
            countryCodeToRegionCodeMap[44] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("DK");
            countryCodeToRegionCodeMap[45] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SE");
            countryCodeToRegionCodeMap[46] = listWithRegionCode;

            listWithRegionCode = new List<String>(2);
            listWithRegionCode.Add("NO");
            listWithRegionCode.Add("SJ");
            countryCodeToRegionCodeMap[47] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PL");
            countryCodeToRegionCodeMap[48] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("DE");
            countryCodeToRegionCodeMap[49] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PE");
            countryCodeToRegionCodeMap[51] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MX");
            countryCodeToRegionCodeMap[52] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CU");
            countryCodeToRegionCodeMap[53] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AR");
            countryCodeToRegionCodeMap[54] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BR");
            countryCodeToRegionCodeMap[55] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CL");
            countryCodeToRegionCodeMap[56] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CO");
            countryCodeToRegionCodeMap[57] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("VE");
            countryCodeToRegionCodeMap[58] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MY");
            countryCodeToRegionCodeMap[60] = listWithRegionCode;

            listWithRegionCode = new List<String>(3);
            listWithRegionCode.Add("AU");
            listWithRegionCode.Add("CC");
            listWithRegionCode.Add("CX");
            countryCodeToRegionCodeMap[61] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ID");
            countryCodeToRegionCodeMap[62] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PH");
            countryCodeToRegionCodeMap[63] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NZ");
            countryCodeToRegionCodeMap[64] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SG");
            countryCodeToRegionCodeMap[65] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TH");
            countryCodeToRegionCodeMap[66] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("JP");
            countryCodeToRegionCodeMap[81] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("KR");
            countryCodeToRegionCodeMap[82] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("VN");
            countryCodeToRegionCodeMap[84] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CN");
            countryCodeToRegionCodeMap[86] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TR");
            countryCodeToRegionCodeMap[90] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("IN");
            countryCodeToRegionCodeMap[91] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PK");
            countryCodeToRegionCodeMap[92] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AF");
            countryCodeToRegionCodeMap[93] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LK");
            countryCodeToRegionCodeMap[94] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MM");
            countryCodeToRegionCodeMap[95] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("IR");
            countryCodeToRegionCodeMap[98] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MA");
            countryCodeToRegionCodeMap[212] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("DZ");
            countryCodeToRegionCodeMap[213] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TN");
            countryCodeToRegionCodeMap[216] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LY");
            countryCodeToRegionCodeMap[218] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GM");
            countryCodeToRegionCodeMap[220] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SN");
            countryCodeToRegionCodeMap[221] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MR");
            countryCodeToRegionCodeMap[222] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ML");
            countryCodeToRegionCodeMap[223] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GN");
            countryCodeToRegionCodeMap[224] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CI");
            countryCodeToRegionCodeMap[225] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BF");
            countryCodeToRegionCodeMap[226] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NE");
            countryCodeToRegionCodeMap[227] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TG");
            countryCodeToRegionCodeMap[228] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BJ");
            countryCodeToRegionCodeMap[229] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MU");
            countryCodeToRegionCodeMap[230] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LR");
            countryCodeToRegionCodeMap[231] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SL");
            countryCodeToRegionCodeMap[232] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GH");
            countryCodeToRegionCodeMap[233] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NG");
            countryCodeToRegionCodeMap[234] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TD");
            countryCodeToRegionCodeMap[235] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CF");
            countryCodeToRegionCodeMap[236] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CM");
            countryCodeToRegionCodeMap[237] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CV");
            countryCodeToRegionCodeMap[238] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ST");
            countryCodeToRegionCodeMap[239] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GQ");
            countryCodeToRegionCodeMap[240] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GA");
            countryCodeToRegionCodeMap[241] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CG");
            countryCodeToRegionCodeMap[242] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CD");
            countryCodeToRegionCodeMap[243] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AO");
            countryCodeToRegionCodeMap[244] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GW");
            countryCodeToRegionCodeMap[245] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("IO");
            countryCodeToRegionCodeMap[246] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AC");
            countryCodeToRegionCodeMap[247] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SC");
            countryCodeToRegionCodeMap[248] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SD");
            countryCodeToRegionCodeMap[249] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("RW");
            countryCodeToRegionCodeMap[250] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ET");
            countryCodeToRegionCodeMap[251] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SO");
            countryCodeToRegionCodeMap[252] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("DJ");
            countryCodeToRegionCodeMap[253] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("KE");
            countryCodeToRegionCodeMap[254] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TZ");
            countryCodeToRegionCodeMap[255] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("UG");
            countryCodeToRegionCodeMap[256] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BI");
            countryCodeToRegionCodeMap[257] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MZ");
            countryCodeToRegionCodeMap[258] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ZM");
            countryCodeToRegionCodeMap[260] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MG");
            countryCodeToRegionCodeMap[261] = listWithRegionCode;

            listWithRegionCode = new List<String>(2);
            listWithRegionCode.Add("RE");
            listWithRegionCode.Add("YT");
            countryCodeToRegionCodeMap[262] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ZW");
            countryCodeToRegionCodeMap[263] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NA");
            countryCodeToRegionCodeMap[264] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MW");
            countryCodeToRegionCodeMap[265] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LS");
            countryCodeToRegionCodeMap[266] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BW");
            countryCodeToRegionCodeMap[267] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SZ");
            countryCodeToRegionCodeMap[268] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("KM");
            countryCodeToRegionCodeMap[269] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SH");
            countryCodeToRegionCodeMap[290] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ER");
            countryCodeToRegionCodeMap[291] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AW");
            countryCodeToRegionCodeMap[297] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("FO");
            countryCodeToRegionCodeMap[298] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GL");
            countryCodeToRegionCodeMap[299] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GI");
            countryCodeToRegionCodeMap[350] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PT");
            countryCodeToRegionCodeMap[351] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LU");
            countryCodeToRegionCodeMap[352] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("IE");
            countryCodeToRegionCodeMap[353] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("IS");
            countryCodeToRegionCodeMap[354] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AL");
            countryCodeToRegionCodeMap[355] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MT");
            countryCodeToRegionCodeMap[356] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CY");
            countryCodeToRegionCodeMap[357] = listWithRegionCode;

            listWithRegionCode = new List<String>(2);
            listWithRegionCode.Add("FI");
            listWithRegionCode.Add("AX");
            countryCodeToRegionCodeMap[358] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BG");
            countryCodeToRegionCodeMap[359] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LT");
            countryCodeToRegionCodeMap[370] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LV");
            countryCodeToRegionCodeMap[371] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("EE");
            countryCodeToRegionCodeMap[372] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MD");
            countryCodeToRegionCodeMap[373] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AM");
            countryCodeToRegionCodeMap[374] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BY");
            countryCodeToRegionCodeMap[375] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AD");
            countryCodeToRegionCodeMap[376] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MC");
            countryCodeToRegionCodeMap[377] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SM");
            countryCodeToRegionCodeMap[378] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("VA");
            countryCodeToRegionCodeMap[379] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("UA");
            countryCodeToRegionCodeMap[380] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("RS");
            countryCodeToRegionCodeMap[381] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("ME");
            countryCodeToRegionCodeMap[382] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("HR");
            countryCodeToRegionCodeMap[385] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SI");
            countryCodeToRegionCodeMap[386] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BA");
            countryCodeToRegionCodeMap[387] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MK");
            countryCodeToRegionCodeMap[389] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CZ");
            countryCodeToRegionCodeMap[420] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SK");
            countryCodeToRegionCodeMap[421] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LI");
            countryCodeToRegionCodeMap[423] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("FK");
            countryCodeToRegionCodeMap[500] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BZ");
            countryCodeToRegionCodeMap[501] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GT");
            countryCodeToRegionCodeMap[502] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SV");
            countryCodeToRegionCodeMap[503] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("HN");
            countryCodeToRegionCodeMap[504] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NI");
            countryCodeToRegionCodeMap[505] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CR");
            countryCodeToRegionCodeMap[506] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PA");
            countryCodeToRegionCodeMap[507] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PM");
            countryCodeToRegionCodeMap[508] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("HT");
            countryCodeToRegionCodeMap[509] = listWithRegionCode;

            listWithRegionCode = new List<String>(3);
            listWithRegionCode.Add("GP");
            listWithRegionCode.Add("BL");
            listWithRegionCode.Add("MF");
            countryCodeToRegionCodeMap[590] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BO");
            countryCodeToRegionCodeMap[591] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GY");
            countryCodeToRegionCodeMap[592] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("EC");
            countryCodeToRegionCodeMap[593] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GF");
            countryCodeToRegionCodeMap[594] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PY");
            countryCodeToRegionCodeMap[595] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MQ");
            countryCodeToRegionCodeMap[596] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SR");
            countryCodeToRegionCodeMap[597] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("UY");
            countryCodeToRegionCodeMap[598] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AN");
            countryCodeToRegionCodeMap[599] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TL");
            countryCodeToRegionCodeMap[670] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NF");
            countryCodeToRegionCodeMap[672] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BN");
            countryCodeToRegionCodeMap[673] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NR");
            countryCodeToRegionCodeMap[674] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PG");
            countryCodeToRegionCodeMap[675] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TO");
            countryCodeToRegionCodeMap[676] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SB");
            countryCodeToRegionCodeMap[677] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("VU");
            countryCodeToRegionCodeMap[678] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("FJ");
            countryCodeToRegionCodeMap[679] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PW");
            countryCodeToRegionCodeMap[680] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("WF");
            countryCodeToRegionCodeMap[681] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("CK");
            countryCodeToRegionCodeMap[682] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NU");
            countryCodeToRegionCodeMap[683] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("WS");
            countryCodeToRegionCodeMap[685] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("KI");
            countryCodeToRegionCodeMap[686] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NC");
            countryCodeToRegionCodeMap[687] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TV");
            countryCodeToRegionCodeMap[688] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PF");
            countryCodeToRegionCodeMap[689] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TK");
            countryCodeToRegionCodeMap[690] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("FM");
            countryCodeToRegionCodeMap[691] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MH");
            countryCodeToRegionCodeMap[692] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("KP");
            countryCodeToRegionCodeMap[850] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("HK");
            countryCodeToRegionCodeMap[852] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MO");
            countryCodeToRegionCodeMap[853] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("KH");
            countryCodeToRegionCodeMap[855] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LA");
            countryCodeToRegionCodeMap[856] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BD");
            countryCodeToRegionCodeMap[880] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TW");
            countryCodeToRegionCodeMap[886] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MV");
            countryCodeToRegionCodeMap[960] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("LB");
            countryCodeToRegionCodeMap[961] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("JO");
            countryCodeToRegionCodeMap[962] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SY");
            countryCodeToRegionCodeMap[963] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("IQ");
            countryCodeToRegionCodeMap[964] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("KW");
            countryCodeToRegionCodeMap[965] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("SA");
            countryCodeToRegionCodeMap[966] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("YE");
            countryCodeToRegionCodeMap[967] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("OM");
            countryCodeToRegionCodeMap[968] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("PS");
            countryCodeToRegionCodeMap[970] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AE");
            countryCodeToRegionCodeMap[971] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("IL");
            countryCodeToRegionCodeMap[972] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BH");
            countryCodeToRegionCodeMap[973] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("QA");
            countryCodeToRegionCodeMap[974] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("BT");
            countryCodeToRegionCodeMap[975] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("MN");
            countryCodeToRegionCodeMap[976] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("NP");
            countryCodeToRegionCodeMap[977] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TJ");
            countryCodeToRegionCodeMap[992] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("TM");
            countryCodeToRegionCodeMap[993] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("AZ");
            countryCodeToRegionCodeMap[994] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("GE");
            countryCodeToRegionCodeMap[995] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("KG");
            countryCodeToRegionCodeMap[996] = listWithRegionCode;

            listWithRegionCode = new List<String>(1);
            listWithRegionCode.Add("UZ");
            countryCodeToRegionCodeMap[998] = listWithRegionCode;

            return countryCodeToRegionCodeMap;
        }
    }
}
