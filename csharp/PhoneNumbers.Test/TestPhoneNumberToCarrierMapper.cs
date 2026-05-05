/*
 * Copyright (C) 2013 The Libphonenumber Authors
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

using System.Reflection;
using Xunit;

namespace PhoneNumbers.Test
{
    [Collection("TestMetadataTestCase")]
    public class TestPhoneNumberToCarrierMapper
    {
        private readonly PhoneNumberToCarrierMapper carrierMapper = PhoneNumberToCarrierMapper.GetInstance();

        // Test-data mapper backed by resources/test/carrier/
        private static readonly PhoneNumberToCarrierMapper testCarrierMapper =
            new PhoneNumberToCarrierMapper("carrier.", Assembly.GetExecutingAssembly());

        // ── Production-data numbers (used with carrierMapper / GetInstance()) ─────────────────

        // UK mobile: 447106 -> O2, 447306 -> Virgin Mobile (en/44.txt)
        private static readonly PhoneNumber UK_MOBILE1 =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7106123456L).Build();
        private static readonly PhoneNumber UK_MOBILE2 =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7306123456L).Build();
        private static readonly PhoneNumber UK_FIXED1 =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(2071234567L).Build();
        // Too short to be valid — UNKNOWN type
        private static readonly PhoneNumber UK_INVALID_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7301234L).Build();

        // Angola mobile: 24491 -> Movicel, 24492 -> UNITEL (en/244.txt)
        private static readonly PhoneNumber AO_MOBILE1 =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(912345678L).Build();
        private static readonly PhoneNumber AO_MOBILE2 =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(923456789L).Build();
        private static readonly PhoneNumber AO_FIXED1 =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(222333444L).Build();
        // Too short to be valid — UNKNOWN type
        private static readonly PhoneNumber AO_INVALID_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(123456L).Build();
        // Prefix 24498 is not present in en/244.txt
        private static readonly PhoneNumber AO_NUMBER_WITH_MISSING_PREFIX =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(985000000L).Build();

        // US FIXED_LINE_OR_MOBILE — no carrier data for this prefix in en/1.txt
        private static readonly PhoneNumber US_FIXED_OR_MOBILE =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();

        // No carrier data files exist for these country codes
        private static readonly PhoneNumber NUMBER_WITH_INVALID_COUNTRY_CODE =
            new PhoneNumber.Builder().SetCountryCode(999).SetNationalNumber(2423651234L).Build();
        private static readonly PhoneNumber INTERNATIONAL_TOLL_FREE =
            new PhoneNumber.Builder().SetCountryCode(800).SetNationalNumber(12345678L).Build();

        // ── Test-data numbers (used with testCarrierMapper, match prefixes in resources/test/carrier/) ─

        // en/44.txt: 4411 -> "British fixed line carrier", 4473 -> "British carrier", 44760 -> "British pager"
        // sv/44.txt: 4473 -> "Brittisk operatör"
        private static readonly PhoneNumber UK_MOBILE1_TEST =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7387654321L).Build();
        private static readonly PhoneNumber UK_MOBILE2_TEST =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7487654321L).Build();
        private static readonly PhoneNumber UK_FIXED1_TEST =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(1123456789L).Build();
        private static readonly PhoneNumber UK_FIXED2_TEST =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(2987654321L).Build();
        private static readonly PhoneNumber UK_PAGER =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7601234567L).Build();
        private static readonly PhoneNumber UK_INVALID_TEST =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7301234L).Build();

        // en/244.txt: 244917 -> "Angolan carrier", 244262 -> "Angolan fixed line carrier"
        private static readonly PhoneNumber AO_MOBILE1_TEST =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(917654321L).Build();
        private static readonly PhoneNumber AO_MOBILE2_TEST =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(927654321L).Build();
        private static readonly PhoneNumber AO_FIXED1_TEST =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(22254321L).Build();
        private static readonly PhoneNumber AO_FIXED2_TEST =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(26254321L).Build();
        private static readonly PhoneNumber AO_INVALID_TEST =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(101234L).Build();

        // en/1.txt: 1650212 -> "US carrier", 1650213 -> "US carrier2"
        private static readonly PhoneNumber US_FIXED_OR_MOBILE_TEST =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502123456L).Build();
        // Area code 212 (New York) — no entry in the test data, so lookup returns "".
        private static readonly PhoneNumber s_usNanpaNoDataTest =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2128120000L).Build();

        [Fact]
        public void TestGetNameForMobilePortableRegion()
        {
            // UK supports mobile number portability: GetNameForNumber still returns the carrier.
            Assert.Equal("O2", carrierMapper.GetNameForNumber(UK_MOBILE1, Locale.English));
            // No French carrier data for UK — falls back to English.
            Assert.Equal("O2", carrierMapper.GetNameForNumber(UK_MOBILE1, Locale.French));
            // GetSafeDisplayName returns empty because UK has MNP.
            Assert.Equal("", carrierMapper.GetSafeDisplayName(UK_MOBILE1, Locale.English));
        }

        [Fact]
        public void TestGetNameForNonMobilePortableRegion()
        {
            // Angola does not support MNP: both methods return the carrier.
            Assert.Equal("Movicel", carrierMapper.GetNameForNumber(AO_MOBILE1, Locale.English));
            Assert.Equal("Movicel", carrierMapper.GetSafeDisplayName(AO_MOBILE1, Locale.English));
        }

        [Fact]
        public void TestGetNameForFixedLineNumber()
        {
            // Fixed-line numbers are not mobile type: GetNameForNumber returns "".
            Assert.Equal("", carrierMapper.GetNameForNumber(AO_FIXED1, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForNumber(UK_FIXED1, Locale.English));
            // GetNameForValidNumber skips the type check but there is no carrier data for fixed lines.
            Assert.Equal("", carrierMapper.GetNameForValidNumber(AO_FIXED1, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForValidNumber(UK_FIXED1, Locale.English));
        }

        [Fact]
        public void TestGetNameForFixedOrMobileNumber()
        {
            // FIXED_LINE_OR_MOBILE is treated as mobile by GetNameForNumber.
            // No carrier data exists for US prefix 1-6502..., so "" is returned.
            Assert.Equal("", carrierMapper.GetNameForNumber(US_FIXED_OR_MOBILE, Locale.English));
        }

        [Fact]
        public void TestGetNameForNumberWithNoDataFile()
        {
            // No carrier data file for country code 999 or 800.
            Assert.Equal("", carrierMapper.GetNameForNumber(NUMBER_WITH_INVALID_COUNTRY_CODE, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForNumber(INTERNATIONAL_TOLL_FREE, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForValidNumber(NUMBER_WITH_INVALID_COUNTRY_CODE, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForValidNumber(INTERNATIONAL_TOLL_FREE, Locale.English));
        }

        [Fact]
        public void TestGetNameForNumberWithMissingPrefix()
        {
            // Prefix 24498 is absent from en/244.txt — returns "" regardless of number type.
            Assert.Equal("", carrierMapper.GetNameForNumber(AO_NUMBER_WITH_MISSING_PREFIX, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForValidNumber(AO_NUMBER_WITH_MISSING_PREFIX, Locale.English));
        }

        [Fact]
        public void TestGetNameForInvalidNumber()
        {
            // UNKNOWN-type numbers are not mobile, so GetNameForNumber returns "".
            Assert.Equal("", carrierMapper.GetNameForNumber(UK_INVALID_NUMBER, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForNumber(AO_INVALID_NUMBER, Locale.English));
        }

        [Fact]
        public void TestGetNameForValidNumber()
        {
            // GetNameForValidNumber skips type checking and returns the carrier directly.
            Assert.Equal("O2", carrierMapper.GetNameForValidNumber(UK_MOBILE1, Locale.English));
            Assert.Equal("Virgin Mobile", carrierMapper.GetNameForValidNumber(UK_MOBILE2, Locale.English));
            Assert.Equal("Movicel", carrierMapper.GetNameForValidNumber(AO_MOBILE1, Locale.English));
            Assert.Equal("UNITEL", carrierMapper.GetNameForValidNumber(AO_MOBILE2, Locale.English));
        }

        [Fact]
        public void TestGetSafeDisplayName()
        {
            // UK supports MNP — always returns "".
            Assert.Equal("", carrierMapper.GetSafeDisplayName(UK_MOBILE1, Locale.English));
            // Angola does not support MNP — returns the carrier name.
            Assert.Equal("Movicel", carrierMapper.GetSafeDisplayName(AO_MOBILE1, Locale.English));
            // Fixed-line: GetSafeDisplayName calls GetNameForNumber which returns "" for non-mobile type.
            Assert.Equal("", carrierMapper.GetSafeDisplayName(UK_FIXED1, Locale.English));
        }

        [Fact]
        public void TestGetNameFallbackToEnglish()
        {
            // French has no carrier data for UK, so the result falls back to English.
            Assert.Equal("O2", carrierMapper.GetNameForValidNumber(UK_MOBILE1, Locale.French));
            // Korean never falls back to English (zh, ja, ko are excluded).
            Assert.Equal("", carrierMapper.GetNameForValidNumber(UK_MOBILE1, Locale.Korean));
        }

        // ── Tests using synthetic test carrier data ─────────────────────────────────────────────

        [Fact]
        public void TestGetNameForMobilePortableRegion_WithTestData()
        {
            // UK has MNP: GetNameForNumber still resolves the original carrier.
            Assert.Equal("British carrier", testCarrierMapper.GetNameForNumber(UK_MOBILE1_TEST, Locale.English));
            // French has no test data for UK — falls back to English.
            Assert.Equal("British carrier", testCarrierMapper.GetNameForNumber(UK_MOBILE1_TEST, Locale.French));
            // GetSafeDisplayName returns "" because UK supports MNP.
            Assert.Equal("", testCarrierMapper.GetSafeDisplayName(UK_MOBILE1_TEST, Locale.English));
        }

        [Fact]
        public void TestGetNameForNonMobilePortableRegion_WithTestData()
        {
            // Angola has no MNP: both methods return the carrier.
            Assert.Equal("Angolan carrier", testCarrierMapper.GetNameForNumber(AO_MOBILE1_TEST, Locale.English));
            Assert.Equal("Angolan carrier", testCarrierMapper.GetSafeDisplayName(AO_MOBILE1_TEST, Locale.English));
        }

        [Fact]
        public void TestGetNameForPagerNumber()
        {
            // PAGER is treated as mobile by GetNameForNumber — carrier data is returned.
            Assert.Equal("British pager", testCarrierMapper.GetNameForNumber(UK_PAGER, Locale.English));
        }

        [Fact]
        public void TestGetNameForFixedOrMobileNumber_WithCarrierData()
        {
            // FIXED_LINE_OR_MOBILE is treated as mobile, so carrier data is returned.
            Assert.Equal("US carrier", testCarrierMapper.GetNameForNumber(US_FIXED_OR_MOBILE_TEST, Locale.English));
        }

        [Fact]
        public void TestGetNameForFixedLineNumber_WithTestData()
        {
            // Fixed-line type: GetNameForNumber skips non-mobile types — returns "".
            Assert.Equal("", testCarrierMapper.GetNameForNumber(AO_FIXED1_TEST, Locale.English));
            Assert.Equal("", testCarrierMapper.GetNameForNumber(UK_FIXED1_TEST, Locale.English));
            // GetNameForValidNumber bypasses the type check — returns the carrier from the data file.
            Assert.Equal("Angolan fixed line carrier", testCarrierMapper.GetNameForValidNumber(AO_FIXED2_TEST, Locale.English));
            Assert.Equal("British fixed line carrier", testCarrierMapper.GetNameForValidNumber(UK_FIXED1_TEST, Locale.English));
            // No carrier data for this UK fixed-line prefix — returns "" even without type check.
            Assert.Equal("", testCarrierMapper.GetNameForValidNumber(UK_FIXED2_TEST, Locale.English));
        }

        [Fact]
        public void TestGetNameForNumberWithMissingPrefix_WithTestData()
        {
            // Prefixes not in the test data return "" regardless of number type.
            Assert.Equal("", testCarrierMapper.GetNameForNumber(UK_MOBILE2_TEST, Locale.English));
            Assert.Equal("", testCarrierMapper.GetNameForNumber(AO_MOBILE2_TEST, Locale.English));
        }

        [Fact]
        public void TestGetNameForInvalidNumber_WithTestData()
        {
            Assert.Equal("", testCarrierMapper.GetNameForNumber(UK_INVALID_TEST, Locale.English));
            Assert.Equal("", testCarrierMapper.GetNameForNumber(AO_INVALID_TEST, Locale.English));
        }

        [Fact]
        public void TestGetNameForNumberWithSwedishLocale()
        {
            // Swedish test data (sv/44.txt) has an entry for prefix 4473.
            Assert.Equal("Brittisk operatör",
                testCarrierMapper.GetNameForNumber(UK_MOBILE1_TEST, new Locale("sv", "SE")));
        }

        // ── NANPA lookup tests ──────────────────────────────────────────────────────────────────
        // NANPA carrier data lives in a single en/1.txt file. AreaCodeMap.Lookup does
        // longest-prefix matching against the full E.164 number, so en/1.txt entries like
        // "1650212" correctly resolve numbers in the 650-212x range.

        [Fact]
        public void TestGetNameForNanpaNumber()
        {
            // 6502123456 → longest-prefix match on 1650212 in en/1.txt → "US carrier"
            Assert.Equal("US carrier",
                testCarrierMapper.GetNameForNumber(US_FIXED_OR_MOBILE_TEST, Locale.English));
            // 6502133456 → longest-prefix match on 1650213 in en/1.txt → "US carrier2"
            Assert.Equal("US carrier2",
                testCarrierMapper.GetNameForValidNumber(
                    new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502133456L).Build(),
                    Locale.English));
        }

        [Fact]
        public void TestGetNameForNanpaNumberWithMissingPrefix()
        {
            // Area code 212 has no entry in the test en/1.txt — returns "".
            Assert.Equal("", testCarrierMapper.GetNameForValidNumber(s_usNanpaNoDataTest, Locale.English));
        }

        // Bahamas +1 242 357 xxxx — entry "1242357|BaTelCo" is present in the shipped en/1.txt.
        private static readonly PhoneNumber BAHAMAS_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2423571234L).Build();

        [Fact]
        public void TestGetNameForNanpaNumberFromProductionData()
        {
            Assert.Equal("BaTelCo", carrierMapper.GetNameForValidNumber(BAHAMAS_NUMBER, Locale.English));
        }
    }
}
