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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Web;
using NUnit.Framework;

namespace PhoneNumbers.Test
{
    [TestFixture]
    public class TestPhoneNumberUtil
    {
        private PhoneNumberUtil phoneUtil;
        // This is used by BuildMetadataProtoFromXml.
        public const String TEST_META_DATA_FILE_PREFIX = "PhoneNumberMetaDataForTesting.xml";

        // Set up some test numbers to re-use.
        private static readonly PhoneNumber ALPHA_NUMERIC_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(80074935247L).Build();
        private static readonly PhoneNumber AR_MOBILE =
            new PhoneNumber.Builder().SetCountryCode(54).SetNationalNumber(91187654321L).Build();
        private static readonly PhoneNumber AR_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(54).SetNationalNumber(1187654321).Build();
        private static readonly PhoneNumber AU_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(236618300L).Build();
        private static readonly PhoneNumber BS_MOBILE =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2423570000L).Build();
        private static readonly PhoneNumber BS_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2423651234L).Build();
        // Note that this is the same as the example number for DE in the metadata.
        private static readonly PhoneNumber DE_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(30123456L).Build();
        private static readonly PhoneNumber DE_SHORT_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(1234L).Build();
        private static readonly PhoneNumber GB_MOBILE =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7912345678L).Build();
        private static readonly PhoneNumber GB_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(2070313000L).Build();
        private static readonly PhoneNumber IT_MOBILE =
            new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(345678901L).Build();
        private static readonly PhoneNumber IT_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(236618300L).
            SetItalianLeadingZero(true).Build();
        // Numbers to test the formatting rules from Mexico.
        private static readonly PhoneNumber MX_MOBILE1 =
            new PhoneNumber.Builder().SetCountryCode(52).SetNationalNumber(12345678900L).Build();
        private static readonly PhoneNumber MX_MOBILE2 =
            new PhoneNumber.Builder().SetCountryCode(52).SetNationalNumber(15512345678L).Build();
        private static readonly PhoneNumber MX_NUMBER1 =
            new PhoneNumber.Builder().SetCountryCode(52).SetNationalNumber(3312345678L).Build();
        private static readonly PhoneNumber MX_NUMBER2 =
            new PhoneNumber.Builder().SetCountryCode(52).SetNationalNumber(8211234567L).Build();
        private static readonly PhoneNumber NZ_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(64).SetNationalNumber(33316005L).Build();
        private static readonly PhoneNumber SG_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(65).SetNationalNumber(65218000L).Build();
        // A too-long and hence invalid US number.
        private static readonly PhoneNumber US_LONG_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(65025300001L).Build();
        private static readonly PhoneNumber US_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();
        private static readonly PhoneNumber US_PREMIUM =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(9002530000L).Build();
        // Too short, but still possible US numbers.
        private static readonly PhoneNumber US_LOCAL_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2530000L).Build();
        private static readonly PhoneNumber US_SHORT_BY_ONE_NUMBER =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(650253000L).Build();
        private static readonly PhoneNumber US_TOLLFREE =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(8002530000L).Build();
        private static readonly PhoneNumber US_SPOOF =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(0L).Build();
        private static readonly PhoneNumber US_SPOOF_WITH_RAW_INPUT =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(0L)
                .SetRawInput("000-000-0000").Build();

        private static PhoneNumber.Builder Update(PhoneNumber p)
        {
            return new PhoneNumber.Builder().MergeFrom(p);
        }

        private static NumberFormat.Builder Update(NumberFormat p)
        {
            return new NumberFormat.Builder().MergeFrom(p);
        }

        private static PhoneMetadata.Builder Update(PhoneMetadata p)
        {
            return new PhoneMetadata.Builder().MergeFrom(p);
        }

        private static void AreEqual(PhoneNumber.Builder p1, PhoneNumber.Builder p2)
        {
            Assert.AreEqual(p1.Clone().Build(), p2.Clone().Build());
        }

        private static void AreEqual(PhoneNumber p1, PhoneNumber.Builder p2)
        {
            Assert.AreEqual(p1, p2.Clone().Build());
        }

        [TestFixtureSetUp]
        public void SetupFixture()
        {
            phoneUtil = InitializePhoneUtilForTesting();
        }

        public static PhoneNumberUtil InitializePhoneUtilForTesting()
        {
            PhoneNumberUtil.ResetInstance();
            return PhoneNumberUtil.GetInstance(
                TEST_META_DATA_FILE_PREFIX,
                CountryCodeToRegionCodeMapForTesting.GetCountryCodeToRegionCodeMap());
        }

        [Test]
        public void TestGetSupportedRegions()
        {
            Assert.That(phoneUtil.GetSupportedRegions().Count > 0);
        }

        [Test]
        public void TestGetInstanceLoadUSMetadata()
        {
            PhoneMetadata metadata = phoneUtil.GetMetadataForRegion(RegionCode.US);
            Assert.AreEqual("US", metadata.Id);
            Assert.AreEqual(1, metadata.CountryCode);
            Assert.AreEqual("011", metadata.InternationalPrefix);
            Assert.That(metadata.HasNationalPrefix);
            Assert.AreEqual(2, metadata.NumberFormatCount);
            Assert.AreEqual("(\\d{3})(\\d{3})(\\d{4})",
                metadata.NumberFormatList[1].Pattern);
            Assert.AreEqual("$1 $2 $3", metadata.NumberFormatList[1].Format);
            Assert.AreEqual("[13-689]\\d{9}|2[0-35-9]\\d{8}",
                metadata.GeneralDesc.NationalNumberPattern);
            Assert.AreEqual("\\d{7}(?:\\d{3})?", metadata.GeneralDesc.PossibleNumberPattern);
            Assert.That(metadata.GeneralDesc.Equals(metadata.FixedLine));
            Assert.AreEqual("\\d{10}", metadata.TollFree.PossibleNumberPattern);
            Assert.AreEqual("900\\d{7}", metadata.PremiumRate.NationalNumberPattern);
            // No shared-cost data is available, so it should be initialised to "NA".
            Assert.AreEqual("NA", metadata.SharedCost.NationalNumberPattern);
            Assert.AreEqual("NA", metadata.SharedCost.PossibleNumberPattern);
        }

        [Test]
        public void TestGetInstanceLoadDEMetadata()
        {
            PhoneMetadata metadata = phoneUtil.GetMetadataForRegion(RegionCode.DE);
            Assert.AreEqual("DE", metadata.Id);
            Assert.AreEqual(49, metadata.CountryCode);
            Assert.AreEqual("00", metadata.InternationalPrefix);
            Assert.AreEqual("0", metadata.NationalPrefix);
            Assert.AreEqual(6, metadata.NumberFormatCount);
            Assert.AreEqual(1, metadata.NumberFormatList[5].LeadingDigitsPatternCount);
            Assert.AreEqual("900", metadata.NumberFormatList[5].LeadingDigitsPatternList[0]);
            Assert.AreEqual("(\\d{3})(\\d{3,4})(\\d{4})",
                     metadata.NumberFormatList[5].Pattern);
            Assert.AreEqual("$1 $2 $3", metadata.NumberFormatList[5].Format);
            Assert.AreEqual("(?:[24-6]\\d{2}|3[03-9]\\d|[789](?:[1-9]\\d|0[2-9]))\\d{1,8}",
                     metadata.FixedLine.NationalNumberPattern);
            Assert.AreEqual("\\d{2,14}", metadata.FixedLine.PossibleNumberPattern);
            Assert.AreEqual("30123456", metadata.FixedLine.ExampleNumber);
            Assert.AreEqual("\\d{10}", metadata.TollFree.PossibleNumberPattern);
            Assert.AreEqual("900([135]\\d{6}|9\\d{7})", metadata.PremiumRate.NationalNumberPattern);
        }

        [Test]
        public void TestGetInstanceLoadARMetadata()
        {
            PhoneMetadata metadata = phoneUtil.GetMetadataForRegion(RegionCode.AR);
            Assert.AreEqual("AR", metadata.Id);
            Assert.AreEqual(54, metadata.CountryCode);
            Assert.AreEqual("00", metadata.InternationalPrefix);
            Assert.AreEqual("0", metadata.NationalPrefix);
            Assert.AreEqual("0(?:(11|343|3715)15)?", metadata.NationalPrefixForParsing);
            Assert.AreEqual("9$1", metadata.NationalPrefixTransformRule);
            Assert.AreEqual("$2 15 $3-$4", metadata.NumberFormatList[2].Format);
            Assert.AreEqual("(9)(\\d{4})(\\d{2})(\\d{4})",
                     metadata.NumberFormatList[3].Pattern);
            Assert.AreEqual("(9)(\\d{4})(\\d{2})(\\d{4})",
                     metadata.IntlNumberFormatList[3].Pattern);
            Assert.AreEqual("$1 $2 $3 $4", metadata.IntlNumberFormatList[3].Format);
        }

        [Test]
        public void TestIsLeadingZeroPossible()
        {
            Assert.That(phoneUtil.IsLeadingZeroPossible(39));   // Italy
            Assert.False(phoneUtil.IsLeadingZeroPossible(1));   // USA
            Assert.False(phoneUtil.IsLeadingZeroPossible(800)); // Not in metadata file, just default to
            // false.
        }

        [Test]
        public void TestGetLengthOfGeographicalAreaCode()
        {
            // Google MTV, which has area code "650".
            Assert.AreEqual(3, phoneUtil.GetLengthOfGeographicalAreaCode(US_NUMBER));

            // A North America toll-free number, which has no area code.
            Assert.AreEqual(0, phoneUtil.GetLengthOfGeographicalAreaCode(US_TOLLFREE));

            // Google London, which has area code "20".
            Assert.AreEqual(2, phoneUtil.GetLengthOfGeographicalAreaCode(GB_NUMBER));

            // A UK mobile phone, which has no area code.
            Assert.AreEqual(0, phoneUtil.GetLengthOfGeographicalAreaCode(GB_MOBILE));

            // Google Buenos Aires, which has area code "11".
            Assert.AreEqual(2, phoneUtil.GetLengthOfGeographicalAreaCode(AR_NUMBER));

            // Google Sydney, which has area code "2".
            Assert.AreEqual(1, phoneUtil.GetLengthOfGeographicalAreaCode(AU_NUMBER));

            // Google Singapore. Singapore has no area code and no national prefix.
            Assert.AreEqual(0, phoneUtil.GetLengthOfGeographicalAreaCode(SG_NUMBER));

            // An invalid US number (1 digit shorter), which has no area code.
            Assert.AreEqual(0, phoneUtil.GetLengthOfGeographicalAreaCode(US_SHORT_BY_ONE_NUMBER));
        }

        [Test]
        public void TestGetLengthOfNationalDestinationCode()
        {
            // Google MTV, which has national destination code (NDC) "650".
            Assert.AreEqual(3, phoneUtil.GetLengthOfNationalDestinationCode(US_NUMBER));

            // A North America toll-free number, which has NDC "800".
            Assert.AreEqual(3, phoneUtil.GetLengthOfNationalDestinationCode(US_TOLLFREE));

            // Google London, which has NDC "20".
            Assert.AreEqual(2, phoneUtil.GetLengthOfNationalDestinationCode(GB_NUMBER));

            // A UK mobile phone, which has NDC "7912".
            Assert.AreEqual(4, phoneUtil.GetLengthOfNationalDestinationCode(GB_MOBILE));

            // Google Buenos Aires, which has NDC "11".
            Assert.AreEqual(2, phoneUtil.GetLengthOfNationalDestinationCode(AR_NUMBER));

            // An Argentinian mobile which has NDC "911".
            Assert.AreEqual(3, phoneUtil.GetLengthOfNationalDestinationCode(AR_MOBILE));

            // Google Sydney, which has NDC "2".
            Assert.AreEqual(1, phoneUtil.GetLengthOfNationalDestinationCode(AU_NUMBER));

            // Google Singapore, which has NDC "6521".
            Assert.AreEqual(4, phoneUtil.GetLengthOfNationalDestinationCode(SG_NUMBER));

            // An invalid US number (1 digit shorter), which has no NDC.
            Assert.AreEqual(0, phoneUtil.GetLengthOfNationalDestinationCode(US_SHORT_BY_ONE_NUMBER));

            // A number containing an invalid country calling code, which shouldn't have any NDC.
            PhoneNumber number = new PhoneNumber.Builder().SetCountryCode(123).SetNationalNumber(6502530000L).Build();
            Assert.AreEqual(0, phoneUtil.GetLengthOfNationalDestinationCode(number));
        }

        [Test]
        public void TestGetNationalSignificantNumber()
        {
            Assert.AreEqual("6502530000", phoneUtil.GetNationalSignificantNumber(US_NUMBER));

            // An Italian mobile number.
            Assert.AreEqual("345678901", phoneUtil.GetNationalSignificantNumber(IT_MOBILE));

            // An Italian fixed line number.
            Assert.AreEqual("0236618300", phoneUtil.GetNationalSignificantNumber(IT_NUMBER));
        }

        [Test]
        public void TestGetExampleNumber()
        {
            Assert.AreEqual(DE_NUMBER, phoneUtil.GetExampleNumber(RegionCode.DE));

            Assert.AreEqual(DE_NUMBER, phoneUtil.GetExampleNumberForType(RegionCode.DE,
                PhoneNumberType.FIXED_LINE));
            Assert.AreEqual(null, phoneUtil.GetExampleNumberForType(RegionCode.DE,
                PhoneNumberType.MOBILE));
            // For the US, the example number is placed under general description, and hence should be used
            // for both fixed line and mobile, so neither of these should return null.
            Assert.IsNotNull(phoneUtil.GetExampleNumberForType(RegionCode.US,
                PhoneNumberType.FIXED_LINE));
            Assert.IsNotNull(phoneUtil.GetExampleNumberForType(RegionCode.US,
                PhoneNumberType.MOBILE));
            // CS is an invalid region, so we have no data for it.
            Assert.IsNull(phoneUtil.GetExampleNumberForType(RegionCode.CS,
                PhoneNumberType.MOBILE));
        }

        [Test]
        public void TestConvertAlphaCharactersInNumber()
        {
            String input = "1800-ABC-DEF";
            // Alpha chars are converted to digits; everything else is left untouched.
            String expectedOutput = "1800-222-333";
            Assert.AreEqual(expectedOutput, PhoneNumberUtil.ConvertAlphaCharactersInNumber(input));
        }


        [Test]
        public void TestNormaliseRemovePunctuation()
        {
            String inputNumber = "034-56&+#234";
            String expectedOutput = "03456234";
            Assert.AreEqual(expectedOutput,
                PhoneNumberUtil.Normalize(inputNumber),
                "Conversion did not correctly remove punctuation");
        }

        [Test]
        public void TestNormaliseReplaceAlphaCharacters()
        {
            String inputNumber = "034-I-am-HUNGRY";
            String expectedOutput = "034426486479";
            Assert.AreEqual(expectedOutput,
                PhoneNumberUtil.Normalize(inputNumber),
                "Conversion did not correctly replace alpha characters");
        }

        [Test]
        public void TestNormaliseOtherDigits()
        {
            String inputNumber = "\uFF125\u0665";
            String expectedOutput = "255";
            Assert.AreEqual(expectedOutput,
                PhoneNumberUtil.Normalize(inputNumber),
                "Conversion did not correctly replace non-latin digits");
            // Eastern-Arabic digits.
            inputNumber = "\u06F52\u06F0";
            expectedOutput = "520";
            Assert.AreEqual(expectedOutput,
                PhoneNumberUtil.Normalize(inputNumber),
                "Conversion did not correctly replace non-latin digits");
        }

        [Test]
        public void TestNormaliseStripAlphaCharacters()
        {
            String inputNumber = "034-56&+a#234";
            String expectedOutput = "03456234";
            Assert.AreEqual(expectedOutput,
                PhoneNumberUtil.NormalizeDigitsOnly(inputNumber),
                "Conversion did not correctly remove alpha character");
        }

        [Test]
        public void TestFormatUSNumber()
        {
            Assert.AreEqual("650 253 0000", phoneUtil.Format(US_NUMBER, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+1 650 253 0000", phoneUtil.Format(US_NUMBER, PhoneNumberFormat.INTERNATIONAL));

            Assert.AreEqual("800 253 0000", phoneUtil.Format(US_TOLLFREE, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+1 800 253 0000", phoneUtil.Format(US_TOLLFREE, PhoneNumberFormat.INTERNATIONAL));

            Assert.AreEqual("900 253 0000", phoneUtil.Format(US_PREMIUM, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+1 900 253 0000", phoneUtil.Format(US_PREMIUM, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+1-900-253-0000", phoneUtil.Format(US_PREMIUM, PhoneNumberFormat.RFC3966));

            // Numbers with all zeros in the national number part will be formatted by using the raw_input
            // if that is available no matter which format is specified.
            Assert.AreEqual("000-000-0000",
                phoneUtil.Format(US_SPOOF_WITH_RAW_INPUT, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("0", phoneUtil.Format(US_SPOOF, PhoneNumberFormat.NATIONAL));
        }

        [Test]
        public void TestFormatBSNumber()
        {
            Assert.AreEqual("242 365 1234", phoneUtil.Format(BS_NUMBER, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+1 242 365 1234", phoneUtil.Format(BS_NUMBER, PhoneNumberFormat.INTERNATIONAL));
        }

        [Test]
        public void TestFormatGBNumber()
        {
            Assert.AreEqual("(020) 7031 3000", phoneUtil.Format(GB_NUMBER, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+44 20 7031 3000", phoneUtil.Format(GB_NUMBER, PhoneNumberFormat.INTERNATIONAL));

            Assert.AreEqual("(07912) 345 678", phoneUtil.Format(GB_MOBILE, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+44 7912 345 678", phoneUtil.Format(GB_MOBILE, PhoneNumberFormat.INTERNATIONAL));
        }

        [Test]
        public void TestFormatDENumber()
        {
            var deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(301234L).Build();
            Assert.AreEqual("030/1234", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+49 30/1234", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+49-30-1234", phoneUtil.Format(deNumber, PhoneNumberFormat.RFC3966));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(291123L).Build();
            Assert.AreEqual("0291 123", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+49 291 123", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(29112345678L).Build();
            Assert.AreEqual("0291 12345678", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+49 291 12345678", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(912312345L).Build();
            Assert.AreEqual("09123 12345", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+49 9123 12345", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(80212345L).Build();
            Assert.AreEqual("08021 2345", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+49 8021 2345", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));
            // Note this number is correctly formatted without national prefix. Most of the numbers that
            // are treated as invalid numbers by the library are short numbers, and they are usually not
            // dialed with national prefix.
            Assert.AreEqual("1234", phoneUtil.Format(DE_SHORT_NUMBER, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+49 1234", phoneUtil.Format(DE_SHORT_NUMBER, PhoneNumberFormat.INTERNATIONAL));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(41341234).Build();
            Assert.AreEqual("04134 1234", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
        }

        [Test]
        public void TestFormatITNumber()
        {
            Assert.AreEqual("02 3661 8300", phoneUtil.Format(IT_NUMBER, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+39 02 3661 8300", phoneUtil.Format(IT_NUMBER, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+390236618300", phoneUtil.Format(IT_NUMBER, PhoneNumberFormat.E164));

            Assert.AreEqual("345 678 901", phoneUtil.Format(IT_MOBILE, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+39 345 678 901", phoneUtil.Format(IT_MOBILE, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+39345678901", phoneUtil.Format(IT_MOBILE, PhoneNumberFormat.E164));
        }

        [Test]
        public void TestFormatAUNumber()
        {
            Assert.AreEqual("02 3661 8300", phoneUtil.Format(AU_NUMBER, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+61 2 3661 8300", phoneUtil.Format(AU_NUMBER, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+61236618300", phoneUtil.Format(AU_NUMBER, PhoneNumberFormat.E164));

            PhoneNumber auNumber = new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(1800123456L).Build();
            Assert.AreEqual("1800 123 456", phoneUtil.Format(auNumber, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+61 1800 123 456", phoneUtil.Format(auNumber, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+611800123456", phoneUtil.Format(auNumber, PhoneNumberFormat.E164));
        }

        [Test]
        public void TestFormatARNumber()
        {
            Assert.AreEqual("011 8765-4321", phoneUtil.Format(AR_NUMBER, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+54 11 8765-4321", phoneUtil.Format(AR_NUMBER, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+541187654321", phoneUtil.Format(AR_NUMBER, PhoneNumberFormat.E164));

            Assert.AreEqual("011 15 8765-4321", phoneUtil.Format(AR_MOBILE, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+54 9 11 8765 4321", phoneUtil.Format(AR_MOBILE,
                                                            PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+5491187654321", phoneUtil.Format(AR_MOBILE, PhoneNumberFormat.E164));
        }

        [Test]
        public void TestFormatMXNumber()
        {
            Assert.AreEqual("045 234 567 8900", phoneUtil.Format(MX_MOBILE1, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+52 1 234 567 8900", phoneUtil.Format(
                MX_MOBILE1, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+5212345678900", phoneUtil.Format(MX_MOBILE1, PhoneNumberFormat.E164));

            Assert.AreEqual("045 55 1234 5678", phoneUtil.Format(MX_MOBILE2, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+52 1 55 1234 5678", phoneUtil.Format(
                MX_MOBILE2, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+5215512345678", phoneUtil.Format(MX_MOBILE2, PhoneNumberFormat.E164));

            Assert.AreEqual("01 33 1234 5678", phoneUtil.Format(MX_NUMBER1, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+52 33 1234 5678", phoneUtil.Format(MX_NUMBER1, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+523312345678", phoneUtil.Format(MX_NUMBER1, PhoneNumberFormat.E164));

            Assert.AreEqual("01 821 123 4567", phoneUtil.Format(MX_NUMBER2, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("+52 821 123 4567", phoneUtil.Format(MX_NUMBER2, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+528211234567", phoneUtil.Format(MX_NUMBER2, PhoneNumberFormat.E164));
        }

        [Test]
        public void testFormatOutOfCountryCallingNumber()
        {
            Assert.AreEqual("00 1 900 253 0000",
            phoneUtil.FormatOutOfCountryCallingNumber(US_PREMIUM, RegionCode.DE));

            Assert.AreEqual("1 650 253 0000",
            phoneUtil.FormatOutOfCountryCallingNumber(US_NUMBER, RegionCode.BS));

            Assert.AreEqual("00 1 650 253 0000",
            phoneUtil.FormatOutOfCountryCallingNumber(US_NUMBER, RegionCode.PL));

            Assert.AreEqual("011 44 7912 345 678",
            phoneUtil.FormatOutOfCountryCallingNumber(GB_MOBILE, RegionCode.US));

            Assert.AreEqual("00 49 1234",
            phoneUtil.FormatOutOfCountryCallingNumber(DE_SHORT_NUMBER, RegionCode.GB));
            // Note this number is correctly formatted without national prefix. Most of the numbers that
            // are treated as invalid numbers by the library are short numbers, and they are usually not
            // dialed with national prefix.
            Assert.AreEqual("1234", phoneUtil.FormatOutOfCountryCallingNumber(DE_SHORT_NUMBER, RegionCode.DE));

            Assert.AreEqual("011 39 02 3661 8300",
            phoneUtil.FormatOutOfCountryCallingNumber(IT_NUMBER, RegionCode.US));
            Assert.AreEqual("02 3661 8300",
            phoneUtil.FormatOutOfCountryCallingNumber(IT_NUMBER, RegionCode.IT));
            Assert.AreEqual("+39 02 3661 8300",
            phoneUtil.FormatOutOfCountryCallingNumber(IT_NUMBER, RegionCode.SG));

            Assert.AreEqual("6521 8000",
            phoneUtil.FormatOutOfCountryCallingNumber(SG_NUMBER, RegionCode.SG));

            Assert.AreEqual("011 54 9 11 8765 4321",
            phoneUtil.FormatOutOfCountryCallingNumber(AR_MOBILE, RegionCode.US));

            PhoneNumber arNumberWithExtn = new PhoneNumber.Builder().MergeFrom(AR_MOBILE).SetExtension("1234").Build();
            Assert.AreEqual("011 54 9 11 8765 4321 ext. 1234",
            phoneUtil.FormatOutOfCountryCallingNumber(arNumberWithExtn, RegionCode.US));
            Assert.AreEqual("0011 54 9 11 8765 4321 ext. 1234",
            phoneUtil.FormatOutOfCountryCallingNumber(arNumberWithExtn, RegionCode.AU));
            Assert.AreEqual("011 15 8765-4321 ext. 1234",
            phoneUtil.FormatOutOfCountryCallingNumber(arNumberWithExtn, RegionCode.AR));
        }

        [Test]
        public void TestFormatOutOfCountryWithInvalidRegion()
        {
            // AQ/Antarctica isn't a valid region code for phone number formatting,
            // so this falls back to intl formatting.
            Assert.AreEqual("+1 650 253 0000",
                phoneUtil.FormatOutOfCountryCallingNumber(US_NUMBER, "AQ"));
        }

        [Test]
        public void TestFormatOutOfCountryWithPreferredIntlPrefix()
        {
            // This should use 0011, since that is the preferred international prefix (both 0011 and 0012
            // are accepted as possible international prefixes in our test metadta.)
            Assert.AreEqual("0011 39 02 3661 8300",
                phoneUtil.FormatOutOfCountryCallingNumber(IT_NUMBER, RegionCode.AU));
        }

        [Test]
        public void TestFormatOutOfCountryKeepingAlphaChars()
        {
            var alphaNumericNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(8007493524L)
                .SetRawInput("1800 six-flag")
                .Build();
            Assert.AreEqual("0011 1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetRawInput("1-800-SIX-flag")
                .Build();
            Assert.AreEqual("0011 1 800-SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetRawInput("Call us from UK: 00 1 800 SIX-flag")
                .Build();
            Assert.AreEqual("0011 1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetRawInput("800 SIX-flag")
                .Build();
            Assert.AreEqual("0011 1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            // Formatting from within the NANPA region.
            Assert.AreEqual("1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.US));

            Assert.AreEqual("1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.BS));

            // Testing that if the raw input doesn't exist, it is formatted using
            // FormatOutOfCountryCallingNumber.
            alphaNumericNumber = Update(alphaNumericNumber)
                .ClearRawInput()
                .Build();
            Assert.AreEqual("00 1 800 749 3524",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.DE));

            // Testing AU alpha number formatted from Australia.
            alphaNumericNumber = Update(alphaNumericNumber)
                .SetCountryCode(61).SetNationalNumber(827493524L)
                .SetRawInput("+61 82749-FLAG").Build();
            alphaNumericNumber = Update(alphaNumericNumber).Build();
            // This number should have the national prefix fixed.
            Assert.AreEqual("082749-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetRawInput("082749-FLAG").Build();
            Assert.AreEqual("082749-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetNationalNumber(18007493524L).SetRawInput("1-800-SIX-flag").Build();
            // This number should not have the national prefix prefixed, in accordance with the override for
            // this specific formatting rule.
            Assert.AreEqual("1-800-SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            // The metadata should not be permanently changed, since we copied it before modifying patterns.
            // Here we check this.
            alphaNumericNumber = Update(alphaNumericNumber)
                .SetNationalNumber(1800749352L).Build();
            Assert.AreEqual("1800 749 352",
                phoneUtil.FormatOutOfCountryCallingNumber(alphaNumericNumber, RegionCode.AU));

            // Testing a region with multiple international prefixes.
            Assert.AreEqual("+61 1-800-SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.SG));

            // Testing the case with an invalid country calling code.
            alphaNumericNumber = Update(alphaNumericNumber)
                .SetCountryCode(0).SetNationalNumber(18007493524L).SetRawInput("1-800-SIX-flag").Build();
            // Uses the raw input only.
            Assert.AreEqual("1-800-SIX-flag",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.DE));

            // Testing the case of an invalid alpha number.
            alphaNumericNumber = Update(alphaNumericNumber)
                .SetCountryCode(1).SetNationalNumber(80749L).SetRawInput("180-SIX").Build();
            // No country-code stripping can be done.
            Assert.AreEqual("00 1 180-SIX",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.DE));
        }

        [Test]
        public void TestFormatWithCarrierCode()
        {
            // We only support this for AR in our test metadata, and only for mobile numbers starting with
            // certain values.
            PhoneNumber arMobile = new PhoneNumber.Builder().SetCountryCode(54).SetNationalNumber(92234654321L).Build();
            Assert.AreEqual("02234 65-4321", phoneUtil.Format(arMobile, PhoneNumberFormat.NATIONAL));
            // Here we force 14 as the carrier code.
            Assert.AreEqual("02234 14 65-4321",
                phoneUtil.FormatNationalNumberWithCarrierCode(arMobile, "14"));
            // Here we force the number to be shown with no carrier code.
            Assert.AreEqual("02234 65-4321",
                phoneUtil.FormatNationalNumberWithCarrierCode(arMobile, ""));
            // Here the international rule is used, so no carrier code should be present.
            Assert.AreEqual("+5492234654321", phoneUtil.Format(arMobile, PhoneNumberFormat.E164));
            // We don't support this for the US so there should be no change.
            Assert.AreEqual("650 253 0000", phoneUtil.FormatNationalNumberWithCarrierCode(US_NUMBER, "15"));
        }

        [Test]
        public void TestFormatWithPreferredCarrierCode()
        {
            // We only support this for AR in our test metadata.
            PhoneNumber arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(91234125678L).Build();
            // Test formatting with no preferred carrier code stored in the number itself.
            Assert.AreEqual("01234 15 12-5678",
                phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, "15"));
            Assert.AreEqual("01234 12-5678",
            phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, ""));
            // Test formatting with preferred carrier code present.
            arNumber = Update(arNumber).SetPreferredDomesticCarrierCode("19").Build();
            Assert.AreEqual("01234 12-5678", phoneUtil.Format(arNumber, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("01234 19 12-5678",
                phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, "15"));
            Assert.AreEqual("01234 19 12-5678",
                phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, ""));
            // When the preferred_domestic_carrier_code is present (even when it contains an empty string),
            // use it instead of the default carrier code passed in.
            arNumber = Update(arNumber).SetPreferredDomesticCarrierCode("").Build();
            Assert.AreEqual("01234 12-5678",
                phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, "15"));
            // We don't support this for the US so there should be no change.
            PhoneNumber usNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(4241231234L).SetPreferredDomesticCarrierCode("99")
                .Build();
            Assert.AreEqual("424 123 1234", phoneUtil.Format(usNumber, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual("424 123 1234",
            phoneUtil.FormatNationalNumberWithPreferredCarrierCode(usNumber, "15"));
        }

        [Test]
        public void TestFormatNumberForMobileDialing()
        {
            // US toll free numbers are marked as noInternationalDialling in the test metadata for testing
            // purposes.
            Assert.AreEqual("800 253 0000",
                phoneUtil.FormatNumberForMobileDialing(US_TOLLFREE, RegionCode.US, true));
            Assert.AreEqual("", phoneUtil.FormatNumberForMobileDialing(US_TOLLFREE, RegionCode.CN, true));
            Assert.AreEqual("+1 650 253 0000",
                phoneUtil.FormatNumberForMobileDialing(US_NUMBER, RegionCode.US, true));
            PhoneNumber usNumberWithExtn = new PhoneNumber.Builder().MergeFrom(US_NUMBER).SetExtension("1234").Build();
            Assert.AreEqual("+1 650 253 0000",
                phoneUtil.FormatNumberForMobileDialing(usNumberWithExtn, RegionCode.US, true));

            Assert.AreEqual("8002530000",
                phoneUtil.FormatNumberForMobileDialing(US_TOLLFREE, RegionCode.US, false));
            Assert.AreEqual("", phoneUtil.FormatNumberForMobileDialing(US_TOLLFREE, RegionCode.CN, false));
            Assert.AreEqual("+16502530000",
                phoneUtil.FormatNumberForMobileDialing(US_NUMBER, RegionCode.US, false));
            Assert.AreEqual("+16502530000",
                phoneUtil.FormatNumberForMobileDialing(usNumberWithExtn, RegionCode.US, false));
        }

        [Test]
        public void TestFormatByPattern()
        {
            NumberFormat newNumFormat = new NumberFormat.Builder()
                .SetPattern("(\\d{3})(\\d{3})(\\d{4})")
                .SetFormat("($1) $2-$3").Build();
            List<NumberFormat> newNumberFormats = new List<NumberFormat>();
            newNumberFormats.Add(newNumFormat);

            Assert.AreEqual("(650) 253-0000", phoneUtil.FormatByPattern(US_NUMBER, PhoneNumberFormat.NATIONAL,
                newNumberFormats));
            Assert.AreEqual("+1 (650) 253-0000", phoneUtil.FormatByPattern(US_NUMBER,
                PhoneNumberFormat.INTERNATIONAL,
                newNumberFormats));

            // $NP is set to '1' for the US. Here we check that for other NANPA countries the US rules are
            // followed.
            newNumberFormats[0] = Update(newNumberFormats[0])
                .SetNationalPrefixFormattingRule("$NP ($FG)")
                .SetFormat("$1 $2-$3").Build();
            Assert.AreEqual("1 (242) 365-1234",
                phoneUtil.FormatByPattern(BS_NUMBER, PhoneNumberFormat.NATIONAL,
                newNumberFormats));
            Assert.AreEqual("+1 242 365-1234",
                phoneUtil.FormatByPattern(BS_NUMBER, PhoneNumberFormat.INTERNATIONAL,
                newNumberFormats));

            newNumberFormats[0] = Update(newNumberFormats[0])
                .SetPattern("(\\d{2})(\\d{5})(\\d{3})")
                .SetFormat("$1-$2 $3").Build();

            Assert.AreEqual("02-36618 300",
                phoneUtil.FormatByPattern(IT_NUMBER, PhoneNumberFormat.NATIONAL,
                newNumberFormats));
            Assert.AreEqual("+39 02-36618 300",
                phoneUtil.FormatByPattern(IT_NUMBER, PhoneNumberFormat.INTERNATIONAL,
                newNumberFormats));

            newNumberFormats[0] = Update(newNumberFormats[0]).SetNationalPrefixFormattingRule("$NP$FG")
                .SetPattern("(\\d{2})(\\d{4})(\\d{4})")
                .SetFormat("$1 $2 $3").Build();
            Assert.AreEqual("020 7031 3000",
                phoneUtil.FormatByPattern(GB_NUMBER, PhoneNumberFormat.NATIONAL,
                newNumberFormats));

            newNumberFormats[0] = Update(newNumberFormats[0]).SetNationalPrefixFormattingRule("($NP$FG)").Build();
            Assert.AreEqual("(020) 7031 3000",
                phoneUtil.FormatByPattern(GB_NUMBER, PhoneNumberFormat.NATIONAL,
                newNumberFormats));

            newNumberFormats[0] = Update(newNumberFormats[0]).SetNationalPrefixFormattingRule("").Build();
            Assert.AreEqual("20 7031 3000",
                phoneUtil.FormatByPattern(GB_NUMBER, PhoneNumberFormat.NATIONAL,
                newNumberFormats));

            Assert.AreEqual("+44 20 7031 3000",
                phoneUtil.FormatByPattern(GB_NUMBER, PhoneNumberFormat.INTERNATIONAL,
                newNumberFormats));
        }

        [Test]
        public void TestFormatE164Number()
        {
            Assert.AreEqual("+16502530000", phoneUtil.Format(US_NUMBER, PhoneNumberFormat.E164));
            Assert.AreEqual("+4930123456", phoneUtil.Format(DE_NUMBER, PhoneNumberFormat.E164));
        }

        [Test]
        public void TestFormatNumberWithExtension()
        {
            PhoneNumber nzNumber = new PhoneNumber.Builder().MergeFrom(NZ_NUMBER).SetExtension("1234").Build();
            // Uses default extension prefix:
            Assert.AreEqual("03-331 6005 ext. 1234", phoneUtil.Format(nzNumber, PhoneNumberFormat.NATIONAL));
            // Uses RFC 3966 syntax.
            Assert.AreEqual("+64-3-331-6005;ext=1234", phoneUtil.Format(nzNumber, PhoneNumberFormat.RFC3966));
            // Extension prefix overridden in the territory information for the US:
            PhoneNumber usNumberWithExtension = new PhoneNumber.Builder().MergeFrom(US_NUMBER)
                .SetExtension("4567").Build();
            Assert.AreEqual("650 253 0000 extn. 4567", phoneUtil.Format(usNumberWithExtension,
                PhoneNumberFormat.NATIONAL));
        }

        [Test]
        public void TestFormatUsingOriginalNumberFormat()
        {
            PhoneNumber number1 = phoneUtil.ParseAndKeepRawInput("+442087654321", RegionCode.GB);
            Assert.AreEqual("+44 20 8765 4321", phoneUtil.FormatInOriginalFormat(number1, RegionCode.GB));

            PhoneNumber number2 = phoneUtil.ParseAndKeepRawInput("02087654321", RegionCode.GB);
            Assert.AreEqual("(020) 8765 4321", phoneUtil.FormatInOriginalFormat(number2, RegionCode.GB));

            PhoneNumber number3 = phoneUtil.ParseAndKeepRawInput("011442087654321", RegionCode.US);
            Assert.AreEqual("011 44 20 8765 4321", phoneUtil.FormatInOriginalFormat(number3, RegionCode.US));

            PhoneNumber number4 = phoneUtil.ParseAndKeepRawInput("442087654321", RegionCode.GB);
            Assert.AreEqual("44 20 8765 4321", phoneUtil.FormatInOriginalFormat(number4, RegionCode.GB));

            PhoneNumber number5 = phoneUtil.Parse("+442087654321", RegionCode.GB);
            Assert.AreEqual("(020) 8765 4321", phoneUtil.FormatInOriginalFormat(number5, RegionCode.GB));

            // Invalid numbers should be formatted using its raw input when that is available. Note area
            // codes starting with 7 are intentionally excluded in the test metadata for testing purposes.
            PhoneNumber number6 = phoneUtil.ParseAndKeepRawInput("7345678901", RegionCode.US);
            Assert.AreEqual("7345678901", phoneUtil.FormatInOriginalFormat(number6, RegionCode.US));

            // When the raw input is unavailable, format as usual.
            PhoneNumber number7 = phoneUtil.Parse("7345678901", RegionCode.US);
            Assert.AreEqual("734 567 8901", phoneUtil.FormatInOriginalFormat(number7, RegionCode.US));
        }

        [Test]
        public void TestIsPremiumRate()
        {
            Assert.AreEqual(PhoneNumberType.PREMIUM_RATE, phoneUtil.GetNumberType(US_PREMIUM));

            PhoneNumber premiumRateNumber = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(892123L).Build();
            Assert.AreEqual(PhoneNumberType.PREMIUM_RATE,
                phoneUtil.GetNumberType(premiumRateNumber));

            premiumRateNumber = Update(premiumRateNumber).SetCountryCode(44).SetNationalNumber(9187654321L).Build();
            Assert.AreEqual(PhoneNumberType.PREMIUM_RATE,
                phoneUtil.GetNumberType(premiumRateNumber));

            premiumRateNumber = Update(premiumRateNumber).SetCountryCode(49).SetNationalNumber(9001654321L).Build();
            Assert.AreEqual(PhoneNumberType.PREMIUM_RATE,
                phoneUtil.GetNumberType(premiumRateNumber));

            premiumRateNumber = Update(premiumRateNumber).SetCountryCode(49).SetNationalNumber(90091234567L).Build();
            Assert.AreEqual(PhoneNumberType.PREMIUM_RATE,
                phoneUtil.GetNumberType(premiumRateNumber));
        }

        [Test]
        public void TestIsTollFree()
        {
            PhoneNumber tollFreeNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(8881234567L).Build();
            Assert.AreEqual(PhoneNumberType.TOLL_FREE,
                phoneUtil.GetNumberType(tollFreeNumber));

            tollFreeNumber = Update(tollFreeNumber).SetCountryCode(39).SetNationalNumber(803123L).Build();
            Assert.AreEqual(PhoneNumberType.TOLL_FREE,
                phoneUtil.GetNumberType(tollFreeNumber));

            tollFreeNumber = Update(tollFreeNumber).SetCountryCode(44).SetNationalNumber(8012345678L).Build();
            Assert.AreEqual(PhoneNumberType.TOLL_FREE,
                phoneUtil.GetNumberType(tollFreeNumber));

            tollFreeNumber = Update(tollFreeNumber).SetCountryCode(49).SetNationalNumber(8001234567L).Build();
            Assert.AreEqual(PhoneNumberType.TOLL_FREE,
                phoneUtil.GetNumberType(tollFreeNumber));
        }

        [Test]
        public void TestIsMobile()
        {
            Assert.AreEqual(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(BS_MOBILE));
            Assert.AreEqual(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(GB_MOBILE));
            Assert.AreEqual(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(IT_MOBILE));
            Assert.AreEqual(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(AR_MOBILE));

            PhoneNumber mobileNumber = new PhoneNumber.Builder()
                .SetCountryCode(49).SetNationalNumber(15123456789L).Build();
            Assert.AreEqual(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(mobileNumber));
        }

        [Test]
        public void TestIsFixedLine()
        {
            Assert.AreEqual(PhoneNumberType.FIXED_LINE, phoneUtil.GetNumberType(BS_NUMBER));
            Assert.AreEqual(PhoneNumberType.FIXED_LINE, phoneUtil.GetNumberType(IT_NUMBER));
            Assert.AreEqual(PhoneNumberType.FIXED_LINE, phoneUtil.GetNumberType(GB_NUMBER));
            Assert.AreEqual(PhoneNumberType.FIXED_LINE, phoneUtil.GetNumberType(DE_NUMBER));
        }

        [Test]
        public void TestIsFixedLineAndMobile()
        {
            Assert.AreEqual(PhoneNumberType.FIXED_LINE_OR_MOBILE,
                 phoneUtil.GetNumberType(US_NUMBER));

            PhoneNumber fixedLineAndMobileNumber = new PhoneNumber.Builder().
                SetCountryCode(54).SetNationalNumber(1987654321L).Build();
            Assert.AreEqual(PhoneNumberType.FIXED_LINE_OR_MOBILE,
                 phoneUtil.GetNumberType(fixedLineAndMobileNumber));
        }

        [Test]
        public void TestIsSharedCost()
        {
            PhoneNumber gbNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(8431231234L).Build();
            Assert.AreEqual(PhoneNumberType.SHARED_COST, phoneUtil.GetNumberType(gbNumber));
        }

        [Test]
        public void TestIsVoip()
        {
            PhoneNumber gbNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(5631231234L).Build();
            Assert.AreEqual(PhoneNumberType.VOIP, phoneUtil.GetNumberType(gbNumber));
        }

        [Test]
        public void TestIsPersonalNumber()
        {
            PhoneNumber gbNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(7031231234L).Build();
            Assert.AreEqual(PhoneNumberType.PERSONAL_NUMBER,
                phoneUtil.GetNumberType(gbNumber));
        }

        [Test]
        public void TestIsUnknown()
        {
            // Invalid numbers should be of type UNKNOWN.
            Assert.AreEqual(PhoneNumberType.UNKNOWN, phoneUtil.GetNumberType(US_LOCAL_NUMBER));
        }

        [Test]
        public void TestIsValidNumber()
        {
            Assert.That(phoneUtil.IsValidNumber(US_NUMBER));
            Assert.That(phoneUtil.IsValidNumber(IT_NUMBER));
            Assert.That(phoneUtil.IsValidNumber(GB_MOBILE));

            PhoneNumber nzNumber = new PhoneNumber.Builder().SetCountryCode(64).SetNationalNumber(21387835L).Build();
            Assert.That(phoneUtil.IsValidNumber(nzNumber));
        }

        [Test]
        public void TestIsValidForRegion()
        {
            // This number is valid for the Bahamas, but is not a valid US number.
            Assert.That(phoneUtil.IsValidNumber(BS_NUMBER));
            Assert.That(phoneUtil.IsValidNumberForRegion(BS_NUMBER, RegionCode.BS));
            Assert.False(phoneUtil.IsValidNumberForRegion(BS_NUMBER, RegionCode.US));
            PhoneNumber bsInvalidNumber =
                new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2421232345L).Build();
            // This number is no longer valid.
            Assert.False(phoneUtil.IsValidNumber(bsInvalidNumber));

            // La Mayotte and Reunion use 'leadingDigits' to differentiate them.
            PhoneNumber reNumber = new PhoneNumber.Builder()
                .SetCountryCode(262).SetNationalNumber(262123456L).Build();
            Assert.That(phoneUtil.IsValidNumber(reNumber));
            Assert.That(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.RE));
            Assert.False(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.YT));
            // Now change the number to be a number for La Mayotte.
            reNumber = Update(reNumber).SetNationalNumber(269601234L).Build();
            Assert.That(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.YT));
            Assert.False(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.RE));
            // This number is no longer valid for La Reunion.
            reNumber = Update(reNumber).SetNationalNumber(269123456L).Build();
            Assert.False(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.YT));
            Assert.False(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.RE));
            Assert.False(phoneUtil.IsValidNumber(reNumber));
            // However, it should be recognised as from La Mayotte, since it is valid for this region.
            Assert.AreEqual(RegionCode.YT, phoneUtil.GetRegionCodeForNumber(reNumber));
            // This number is valid in both places.
            reNumber = Update(reNumber).SetNationalNumber(800123456L).Build();
            Assert.That(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.YT));
            Assert.That(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.RE));
        }

        [Test]
        public void TestIsNotValidNumber()
        {
            Assert.False(phoneUtil.IsValidNumber(US_LOCAL_NUMBER));

            PhoneNumber invalidNumber = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(23661830000L).SetItalianLeadingZero(true).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));

            invalidNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(791234567L).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));

            invalidNumber = new PhoneNumber.Builder()
                .SetCountryCode(49).SetNationalNumber(1234L).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));

            invalidNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(3316005L).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));
        }

        [Test]
        public void TestGetRegionCodeForCountryCode()
        {
            Assert.AreEqual(RegionCode.US, phoneUtil.GetRegionCodeForCountryCode(1));
            Assert.AreEqual(RegionCode.GB, phoneUtil.GetRegionCodeForCountryCode(44));
            Assert.AreEqual(RegionCode.DE, phoneUtil.GetRegionCodeForCountryCode(49));
        }

        [Test]
        public void TestGetRegionCodeForNumber()
        {
            Assert.AreEqual(RegionCode.BS, phoneUtil.GetRegionCodeForNumber(BS_NUMBER));
            Assert.AreEqual(RegionCode.US, phoneUtil.GetRegionCodeForNumber(US_NUMBER));
            Assert.AreEqual(RegionCode.GB, phoneUtil.GetRegionCodeForNumber(GB_MOBILE));
        }

        [Test]
        public void TestGetCountryCodeForRegion()
        {
            Assert.AreEqual(1, phoneUtil.GetCountryCodeForRegion(RegionCode.US));
            Assert.AreEqual(64, phoneUtil.GetCountryCodeForRegion(RegionCode.NZ));
            Assert.AreEqual(0, phoneUtil.GetCountryCodeForRegion(null));
            Assert.AreEqual(0, phoneUtil.GetCountryCodeForRegion(RegionCode.ZZ));
            // CS is already deprecated so the library doesn't support it.
            Assert.AreEqual(0, phoneUtil.GetCountryCodeForRegion(RegionCode.CS));
        }

        [Test]
        public void TestGetNationalDiallingPrefixForRegion()
        {
            Assert.AreEqual("1", phoneUtil.GetNddPrefixForRegion(RegionCode.US, false));
            // Test non-main country to see it gets the national dialling prefix for the main country with
            // that country calling code.
            Assert.AreEqual("1", phoneUtil.GetNddPrefixForRegion(RegionCode.BS, false));
            Assert.AreEqual("0", phoneUtil.GetNddPrefixForRegion(RegionCode.NZ, false));
            // Test case with non digit in the national prefix.
            Assert.AreEqual("0~0", phoneUtil.GetNddPrefixForRegion(RegionCode.AO, false));
            Assert.AreEqual("00", phoneUtil.GetNddPrefixForRegion(RegionCode.AO, true));
            // Test cases with invalid regions.
            Assert.AreEqual(null, phoneUtil.GetNddPrefixForRegion(null, false));
            Assert.AreEqual(null, phoneUtil.GetNddPrefixForRegion(RegionCode.ZZ, false));
            // CS is already deprecated so the library doesn't support it.
            Assert.AreEqual(null, phoneUtil.GetNddPrefixForRegion(RegionCode.CS, false));
        }

        [Test]
        public void TestIsNANPACountry()
        {
            Assert.That(phoneUtil.IsNANPACountry(RegionCode.US));
            Assert.That(phoneUtil.IsNANPACountry(RegionCode.BS));
            Assert.False(phoneUtil.IsNANPACountry(RegionCode.DE));
            Assert.False(phoneUtil.IsNANPACountry(RegionCode.ZZ));
            Assert.False(phoneUtil.IsNANPACountry(null));
        }

        [Test]
        public void TestIsPossibleNumber()
        {
            Assert.That(phoneUtil.IsPossibleNumber(US_NUMBER));
            Assert.That(phoneUtil.IsPossibleNumber(US_LOCAL_NUMBER));
            Assert.That(phoneUtil.IsPossibleNumber(GB_NUMBER));

            Assert.That(phoneUtil.IsPossibleNumber("+1 650 253 0000", RegionCode.US));
            Assert.That(phoneUtil.IsPossibleNumber("+1 650 GOO OGLE", RegionCode.US));
            Assert.That(phoneUtil.IsPossibleNumber("(650) 253-0000", RegionCode.US));
            Assert.That(phoneUtil.IsPossibleNumber("253-0000", RegionCode.US));
            Assert.That(phoneUtil.IsPossibleNumber("+1 650 253 0000", RegionCode.GB));
            Assert.That(phoneUtil.IsPossibleNumber("+44 20 7031 3000", RegionCode.GB));
            Assert.That(phoneUtil.IsPossibleNumber("(020) 7031 3000", RegionCode.GB));
            Assert.That(phoneUtil.IsPossibleNumber("7031 3000", RegionCode.GB));
            Assert.That(phoneUtil.IsPossibleNumber("3331 6005", RegionCode.NZ));
        }

        [Test]
        public void TestIsPossibleNumberWithReason()
        {
            // National numbers for country calling code +1 that are within 7 to 10 digits are possible.
            Assert.AreEqual(PhoneNumberUtil.ValidationResult.IS_POSSIBLE,
            phoneUtil.IsPossibleNumberWithReason(US_NUMBER));

            Assert.AreEqual(PhoneNumberUtil.ValidationResult.IS_POSSIBLE,
            phoneUtil.IsPossibleNumberWithReason(US_LOCAL_NUMBER));

            Assert.AreEqual(PhoneNumberUtil.ValidationResult.TOO_LONG,
            phoneUtil.IsPossibleNumberWithReason(US_LONG_NUMBER));

            PhoneNumber number = new PhoneNumber.Builder().SetCountryCode(0).SetNationalNumber(2530000L).Build();
            Assert.AreEqual(PhoneNumberUtil.ValidationResult.INVALID_COUNTRY_CODE,
            phoneUtil.IsPossibleNumberWithReason(number));

            number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(253000L).Build();
            Assert.AreEqual(PhoneNumberUtil.ValidationResult.TOO_SHORT,
            phoneUtil.IsPossibleNumberWithReason(number));

            number = new PhoneNumber.Builder().SetCountryCode(65).SetNationalNumber(1234567890L).Build();
            Assert.AreEqual(PhoneNumberUtil.ValidationResult.IS_POSSIBLE,
            phoneUtil.IsPossibleNumberWithReason(number));

            // Try with number that we don't have metadata for.
            var adNumber = new PhoneNumber.Builder().SetCountryCode(376).SetNationalNumber(12345L).Build();
            Assert.AreEqual(PhoneNumberUtil.ValidationResult.IS_POSSIBLE,
            phoneUtil.IsPossibleNumberWithReason(adNumber));
            adNumber = Update(adNumber).SetCountryCode(376).SetNationalNumber(13L).Build();
            Assert.AreEqual(PhoneNumberUtil.ValidationResult.TOO_SHORT,
            phoneUtil.IsPossibleNumberWithReason(adNumber));
            adNumber = Update(adNumber).SetCountryCode(376).SetNationalNumber(12345678901234567L).Build();
            Assert.AreEqual(PhoneNumberUtil.ValidationResult.TOO_LONG,
            phoneUtil.IsPossibleNumberWithReason(adNumber));
        }

        [Test]
        public void TestIsNotPossibleNumber()
        {
            Assert.False(phoneUtil.IsPossibleNumber(US_LONG_NUMBER));

            PhoneNumber number = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(253000L).Build();
            Assert.False(phoneUtil.IsPossibleNumber(number));

            number = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(300L).Build();
            Assert.False(phoneUtil.IsPossibleNumber(number));

            Assert.False(phoneUtil.IsPossibleNumber("+1 650 253 00000", RegionCode.US));
            Assert.False(phoneUtil.IsPossibleNumber("(650) 253-00000", RegionCode.US));
            Assert.False(phoneUtil.IsPossibleNumber("I want a Pizza", RegionCode.US));
            Assert.False(phoneUtil.IsPossibleNumber("253-000", RegionCode.US));
            Assert.False(phoneUtil.IsPossibleNumber("1 3000", RegionCode.GB));
            Assert.False(phoneUtil.IsPossibleNumber("+44 300", RegionCode.GB));
        }

        [Test]
        public void TestTruncateTooLongNumber()
        {
            // US number 650-253-0000, but entered with one additional digit at the end.
            var usNumber = new PhoneNumber.Builder().MergeFrom(US_LONG_NUMBER);
            Assert.That(phoneUtil.TruncateTooLongNumber(usNumber));
            AreEqual(US_NUMBER, usNumber);

            // GB number 080 1234 5678, but entered with 4 extra digits at the end.
            var tooLongNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(80123456780123L);
            var validNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(8012345678L);
            Assert.That(phoneUtil.TruncateTooLongNumber(tooLongNumber));
            AreEqual(validNumber, tooLongNumber);

            // IT number 022 3456 7890, but entered with 3 extra digits at the end.

            tooLongNumber = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(2234567890123L).SetItalianLeadingZero(true);
            validNumber = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(2234567890L).SetItalianLeadingZero(true);
            Assert.That(phoneUtil.TruncateTooLongNumber(tooLongNumber));
            AreEqual(validNumber, tooLongNumber);

            // Tests what happens when a valid number is passed in.
            var validNumberCopy = validNumber.Clone();
            Assert.That(phoneUtil.TruncateTooLongNumber(validNumber));
            // Tests the number is not modified.
            AreEqual(validNumberCopy, validNumber);

            // Tests what happens when a number with invalid prefix is passed in.
            // The test metadata says US numbers cannot have prefix 240.
            var numberWithInvalidPrefix = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(2401234567L);
            var invalidNumberCopy = numberWithInvalidPrefix.Clone();
            Assert.False(phoneUtil.TruncateTooLongNumber(numberWithInvalidPrefix));
            // Tests the number is not modified.
            AreEqual(invalidNumberCopy, numberWithInvalidPrefix);

            // Tests what happens when a too short number is passed in.
            var tooShortNumber = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(1234L);
            var tooShortNumberCopy = tooShortNumber.Clone();
            Assert.False(phoneUtil.TruncateTooLongNumber(tooShortNumber));
            // Tests the number is not modified.
            AreEqual(tooShortNumberCopy, tooShortNumber);
        }

        [Test]
        public void TestIsViablePhoneNumber()
        {
            // Only one or two digits before strange non-possible punctuation.
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("12. March"));
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("1+1+1"));
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("80+0"));
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("00"));
            // Three digits is viable.
            Assert.That(PhoneNumberUtil.IsViablePhoneNumber("111"));
            // Alpha numbers.
            Assert.That(PhoneNumberUtil.IsViablePhoneNumber("0800-4-pizza"));
            Assert.That(PhoneNumberUtil.IsViablePhoneNumber("0800-4-PIZZA"));
        }

        [Test]
        public void TestIsViablePhoneNumberNonAscii()
        {
            // Only one or two digits before possible punctuation followed by more digits.
            Assert.That(PhoneNumberUtil.IsViablePhoneNumber("1\u300034"));
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("1\u30003+4"));
            // Unicode variants of possible starting character and other allowed punctuation/digits.
            Assert.That(PhoneNumberUtil.IsViablePhoneNumber("\uFF081\uFF09\u30003456789"));
            // Testing a leading + is okay.
            Assert.That(PhoneNumberUtil.IsViablePhoneNumber("+1\uFF09\u30003456789"));
        }

        [Test]
        public void TestExtractPossibleNumber()
        {
            // Removes preceding funky punctuation and letters but leaves the rest untouched.
            Assert.AreEqual("0800-345-600", PhoneNumberUtil.ExtractPossibleNumber("Tel:0800-345-600"));
            Assert.AreEqual("0800 FOR PIZZA", PhoneNumberUtil.ExtractPossibleNumber("Tel:0800 FOR PIZZA"));
            // Should not remove plus sign
            Assert.AreEqual("+800-345-600", PhoneNumberUtil.ExtractPossibleNumber("Tel:+800-345-600"));
            // Should recognise wide digits as possible start values.
            Assert.AreEqual("\uFF10\uFF12\uFF13",
            PhoneNumberUtil.ExtractPossibleNumber("\uFF10\uFF12\uFF13"));
            // Dashes are not possible start values and should be removed.
            Assert.AreEqual("\uFF11\uFF12\uFF13",
            PhoneNumberUtil.ExtractPossibleNumber("Num-\uFF11\uFF12\uFF13"));
            // If not possible number present, return empty string.
            Assert.AreEqual("", PhoneNumberUtil.ExtractPossibleNumber("Num-...."));
            // Leading brackets are stripped - these are not used when parsing.
            Assert.AreEqual("650) 253-0000", PhoneNumberUtil.ExtractPossibleNumber("(650) 253-0000"));

            // Trailing non-alpha-numeric characters should be removed.
            Assert.AreEqual("650) 253-0000", PhoneNumberUtil.ExtractPossibleNumber("(650) 253-0000..- .."));
            Assert.AreEqual("650) 253-0000", PhoneNumberUtil.ExtractPossibleNumber("(650) 253-0000."));
            // This case has a trailing RTL char.
            Assert.AreEqual("650) 253-0000", PhoneNumberUtil.ExtractPossibleNumber("(650) 253-0000\u200F"));
        }

        [Test]
        public void TestMaybeStripNationalPrefix()
        {
            PhoneMetadata metadata = new PhoneMetadata.Builder()
                .SetNationalPrefixForParsing("34")
                .SetGeneralDesc(new PhoneNumberDesc.Builder().SetNationalNumberPattern("\\d{4,8}").Build())
                .BuildPartial();
            StringBuilder numberToStrip = new StringBuilder("34356778");
            String strippedNumber = "356778";
            Assert.True(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.AreEqual(strippedNumber, numberToStrip.ToString(),
                "Should have had national prefix stripped.");
            // Retry stripping - now the number should not start with the national prefix, so no more
            // stripping should occur.
            Assert.False(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.AreEqual(strippedNumber, numberToStrip.ToString(),
                "Should have had no change - no national prefix present.");
            // Some countries have no national prefix. Repeat test with none specified.
            metadata = Update(metadata).SetNationalPrefixForParsing("").BuildPartial();
            Assert.False(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.AreEqual(strippedNumber, numberToStrip.ToString(),
                "Should not strip anything with empty national prefix.");
            // If the resultant number doesn't match the national rule, it shouldn't be stripped.
            metadata = Update(metadata).SetNationalPrefixForParsing("3").BuildPartial();
            numberToStrip = new StringBuilder("3123");
            strippedNumber = "3123";
            Assert.False(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.AreEqual(strippedNumber, numberToStrip.ToString(),
                "Should have had no change - after stripping, it wouldn't have matched " +
                "the national rule.");
            // Test extracting carrier selection code.
            metadata = Update(metadata).SetNationalPrefixForParsing("0(81)?").BuildPartial();
            numberToStrip = new StringBuilder("08122123456");
            strippedNumber = "22123456";
            StringBuilder carrierCode = new StringBuilder();
            Assert.True(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(
                numberToStrip, metadata, carrierCode));
            Assert.AreEqual("81", carrierCode.ToString());
            Assert.AreEqual(strippedNumber, numberToStrip.ToString(),
                "Should have had national prefix and carrier code stripped.");
            // If there was a transform rule, check it was applied.
            // Note that a capturing group is present here.
            metadata = Update(metadata).SetNationalPrefixTransformRule("5${1}5")
                .SetNationalPrefixForParsing("0(\\d{2})").BuildPartial();
            numberToStrip = new StringBuilder("031123");
            String transformedNumber = "5315123";
            Assert.True(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.AreEqual(transformedNumber, numberToStrip.ToString(),
                "Should transform the 031 to a 5315.");
        }

        [Test]
        public void TestMaybeStripInternationalPrefix()
        {
            String internationalPrefix = "00[39]";
            StringBuilder numberToStrip = new StringBuilder("0034567700-3898003");
            // Note the dash is removed as part of the normalization.
            StringBuilder strippedNumber = new StringBuilder("45677003898003");
            Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));
            Assert.AreEqual(strippedNumber.ToString(), numberToStrip.ToString(),
                "The number supplied was not stripped of its international prefix.");
            // Now the number no longer starts with an IDD prefix, so it should now report
            // FROM_DEFAULT_COUNTRY.
            Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));

            numberToStrip = new StringBuilder("00945677003898003");
            Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));
            Assert.AreEqual(strippedNumber.ToString(), numberToStrip.ToString(),
                "The number supplied was not stripped of its international prefix.");
            // Test it works when the international prefix is broken up by spaces.
            numberToStrip = new StringBuilder("00 9 45677003898003");
            Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));
            Assert.AreEqual(strippedNumber.ToString(), numberToStrip.ToString(),
                "The number supplied was not stripped of its international prefix.");
            // Now the number no longer starts with an IDD prefix, so it should now report
            // FROM_DEFAULT_COUNTRY.
            Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));

            // Test the + symbol is also recognised and stripped.
            numberToStrip = new StringBuilder("+45677003898003");
            strippedNumber = new StringBuilder("45677003898003");
            Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN,
            phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                                                         internationalPrefix));
            Assert.AreEqual(strippedNumber.ToString(), numberToStrip.ToString(),
                "The number supplied was not stripped of the plus symbol.");

            // If the number afterwards is a zero, we should not strip this - no country calling code begins
            // with 0.
            numberToStrip = new StringBuilder("0090112-3123");
            strippedNumber = new StringBuilder("00901123123");
            Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY,
            phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                                                         internationalPrefix));
            Assert.AreEqual(strippedNumber.ToString(), numberToStrip.ToString(),
                "The number supplied had a 0 after the match so shouldn't be stripped.");
            // Here the 0 is separated by a space from the IDD.
            numberToStrip = new StringBuilder("009 0-112-3123");
            Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY,
            phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                                                         internationalPrefix));
        }

        [Test]
        public void TestMaybeExtractCountryCode()
        {
            var number = new PhoneNumber.Builder();
            PhoneMetadata metadata = phoneUtil.GetMetadataForRegion(RegionCode.US);
            // Note that for the US, the IDD is 011.
            try
            {
                String phoneNumber = "011112-3456789";
                String strippedNumber = "123456789";
                int countryCallingCode = 1;
                StringBuilder numberToFill = new StringBuilder();
                Assert.AreEqual(countryCallingCode,
                    phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true, number),
                    "Did not extract country calling code " + countryCallingCode + " correctly.");
                Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD, number.CountryCodeSource,
                    "Did not figure out CountryCodeSource correctly");
                // Should strip and normalize national significant number.
                Assert.AreEqual(strippedNumber,
                    numberToFill.ToString(),
                    "Did not strip off the country calling code correctly.");
            }
            catch (NumberParseException e)
            {
                Assert.Fail("Should not have thrown an exception: " + e.ToString());
            }
            number = new PhoneNumber.Builder();
            try
            {
                String phoneNumber = "+6423456789";
                int countryCallingCode = 64;
                StringBuilder numberToFill = new StringBuilder();
                Assert.AreEqual(countryCallingCode,
                    phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true, number),
                    "Did not extract country calling code " + countryCallingCode + " correctly.");
                Assert.AreEqual(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN, number.CountryCodeSource,
                    "Did not figure out CountryCodeSource correctly");
            }
            catch (NumberParseException e)
            {
                Assert.Fail("Should not have thrown an exception: " + e.ToString());
            }
            number = new PhoneNumber.Builder();
            try
            {
                String phoneNumber = "2345-6789";
                StringBuilder numberToFill = new StringBuilder();
                Assert.AreEqual(
                0,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true, number),
                "Should not have extracted a country calling code - no international prefix present.");
                Assert.AreEqual(
                PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY, number.CountryCodeSource,
                "Did not figure out CountryCodeSource correctly");
            }
            catch (NumberParseException e)
            {
                Assert.Fail("Should not have thrown an exception: " + e.ToString());
            }
            number = new PhoneNumber.Builder();
            try
            {
                String phoneNumber = "0119991123456789";
                StringBuilder numberToFill = new StringBuilder();
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true, number);
                Assert.Fail("Should have thrown an exception, no valid country calling code present.");
            }
            catch (NumberParseException e)
            {
                // Expected.
                Assert.AreEqual(
                ErrorType.INVALID_COUNTRY_CODE,
                e.ErrorType,
                "Wrong error type stored in exception.");
            }
            number = new PhoneNumber.Builder();
            try
            {
                String phoneNumber = "(1 610) 619 4466";
                int countryCallingCode = 1;
                StringBuilder numberToFill = new StringBuilder();
                Assert.AreEqual(
                countryCallingCode,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true,
                number),
                "Should have extracted the country calling code of the region passed in");
                Assert.AreEqual(
                PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN,
                number.CountryCodeSource,
                "Did not figure out CountryCodeSource correctly");
            }
            catch (NumberParseException e)
            {
                Assert.Fail("Should not have thrown an exception: " + e.ToString());
            }
            number = new PhoneNumber.Builder();
            try
            {
                String phoneNumber = "(1 610) 619 4466";
                int countryCallingCode = 1;
                StringBuilder numberToFill = new StringBuilder();
                Assert.AreEqual(
                countryCallingCode,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, false,
                number),
                "Should have extracted the country calling code of the region passed in");
                Assert.False(number.HasCountryCodeSource, "Should not contain CountryCodeSource.");
            }
            catch (NumberParseException e)
            {
                Assert.Fail("Should not have thrown an exception: " + e.ToString());
            }
            number = new PhoneNumber.Builder();
            try
            {
                String phoneNumber = "(1 610) 619 446";
                StringBuilder numberToFill = new StringBuilder();
                Assert.AreEqual(
                0,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, false,
                number),
                "Should not have extracted a country calling code - invalid number after " +
                "extraction of uncertain country calling code.");
                Assert.False(number.HasCountryCodeSource, "Should not contain CountryCodeSource.");
            }
            catch (NumberParseException e)
            {
                Assert.Fail("Should not have thrown an exception: " + e.ToString());
            }
            number = new PhoneNumber.Builder();
            try
            {
                String phoneNumber = "(1 610) 619";
                StringBuilder numberToFill = new StringBuilder();
                Assert.AreEqual(
                0,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true,
                number),
                "Should not have extracted a country calling code - too short number both " +
                "before and after extraction of uncertain country calling code.");
                Assert.AreEqual(
                PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY, number.CountryCodeSource,
                "Did not figure out CountryCodeSource correctly");
            }
            catch (NumberParseException e)
            {
                Assert.Fail("Should not have thrown an exception: " + e.ToString());
            }
        }

        [Test]
        public void TestParseNationalNumber()
        {
            // National prefix attached.
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("033316005", RegionCode.NZ));
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("33316005", RegionCode.NZ));
            // National prefix attached and some formatting present.
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("03-331 6005", RegionCode.NZ));
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("03 331 6005", RegionCode.NZ));

            // Testing international prefixes.
            // Should strip country calling code.
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("0064 3 331 6005", RegionCode.NZ));
            // Try again, but this time we have an international number with Region Code US. It should
            // recognise the country calling code and parse accordingly.
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("01164 3 331 6005", RegionCode.US));
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("+64 3 331 6005", RegionCode.US));

            // We should ignore the leading plus here, since it is not followed by a valid country code but
            // instead is followed by the IDD for the US.
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("+01164 3 331 6005", RegionCode.US));
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("+0064 3 331 6005", RegionCode.NZ));
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("+ 00 64 3 331 6005", RegionCode.NZ));

            PhoneNumber nzNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(64123456L).Build();
            Assert.AreEqual(nzNumber, phoneUtil.Parse("64(0)64123456", RegionCode.NZ));
            // Check that using a "/" is fine in a phone number.
            Assert.AreEqual(DE_NUMBER, phoneUtil.Parse("301/23456", RegionCode.DE));

            // Check it doesn't use the '1' as a country calling code when parsing if the phone number was
            // already possible.
            PhoneNumber usNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(1234567890L).Build();
            Assert.AreEqual(usNumber, phoneUtil.Parse("123-456-7890", RegionCode.US));
        }

        [Test]
        public void TestParseNumberWithAlphaCharacters()
        {
            // Test case with alpha characters.
            PhoneNumber tollfreeNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(800332005L).Build();
            Assert.AreEqual(tollfreeNumber, phoneUtil.Parse("0800 DDA 005", RegionCode.NZ));
            PhoneNumber premiumNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(9003326005L).Build();
            Assert.AreEqual(premiumNumber, phoneUtil.Parse("0900 DDA 6005", RegionCode.NZ));
            // Not enough alpha characters for them to be considered intentional, so they are stripped.
            Assert.AreEqual(premiumNumber, phoneUtil.Parse("0900 332 6005a", RegionCode.NZ));
            Assert.AreEqual(premiumNumber, phoneUtil.Parse("0900 332 600a5", RegionCode.NZ));
            Assert.AreEqual(premiumNumber, phoneUtil.Parse("0900 332 600A5", RegionCode.NZ));
            Assert.AreEqual(premiumNumber, phoneUtil.Parse("0900 a332 600A5", RegionCode.NZ));
        }



        [Test]
        public void TestParseWithInternationalPrefixes()
        {
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("+1 (650) 253-0000", RegionCode.NZ));
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("1-650-253-0000", RegionCode.US));
            // Calling the US number from Singapore by using different service providers
            // 1st test: calling using SingTel IDD service (IDD is 001)
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("0011-650-253-0000", RegionCode.SG));
            // 2nd test: calling using StarHub IDD service (IDD is 008)
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("0081-650-253-0000", RegionCode.SG));
            // 3rd test: calling using SingTel V019 service (IDD is 019)
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("0191-650-253-0000", RegionCode.SG));
            // Calling the US number from Poland
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("0~01-650-253-0000", RegionCode.PL));
            // Using "++" at the start.
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("++1 (650) 253-0000", RegionCode.PL));
            // Using a very strange decimal digit range (Mongolian digits).
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("\u1811 \u1816\u1815\u1810 " +
                "\u1812\u1815\u1813 \u1810\u1810\u1810\u1810",
                RegionCode.US));
        }

        [Test]
        public void TestParseNonAscii()
        {
            // Using a full-width plus sign.
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("\uFF0B1 (650) 253-0000", RegionCode.SG));
            // The whole number, including punctuation, is here represented in full-width form.
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("\uFF0B\uFF11\u3000\uFF08\uFF16\uFF15\uFF10\uFF09" +
                                                "\u3000\uFF12\uFF15\uFF13\uFF0D\uFF10\uFF10\uFF10" +
                                                "\uFF10",
                                                RegionCode.SG));
            // Using U+30FC dash instead.
            Assert.AreEqual(US_NUMBER, phoneUtil.Parse("\uFF0B\uFF11\u3000\uFF08\uFF16\uFF15\uFF10\uFF09" +
                                                "\u3000\uFF12\uFF15\uFF13\u30FC\uFF10\uFF10\uFF10" +
                                                "\uFF10",
                                                RegionCode.SG));
        }

        [Test]
        public void TestParseWithLeadingZero()
        {
            Assert.AreEqual(IT_NUMBER, phoneUtil.Parse("+39 02-36618 300", RegionCode.NZ));
            Assert.AreEqual(IT_NUMBER, phoneUtil.Parse("02-36618 300", RegionCode.IT));

            Assert.AreEqual(IT_MOBILE, phoneUtil.Parse("345 678 901", RegionCode.IT));
        }

        [Test]
        public void TestParseNationalNumberArgentina()
        {
            // Test parsing mobile numbers of Argentina.
            var arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(93435551212L).Build();
            Assert.AreEqual(arNumber, phoneUtil.Parse("+54 9 343 555 1212", RegionCode.AR));
            Assert.AreEqual(arNumber, phoneUtil.Parse("0343 15 555 1212", RegionCode.AR));

            arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(93715654320L).Build();
            Assert.AreEqual(arNumber, phoneUtil.Parse("+54 9 3715 65 4320", RegionCode.AR));
            Assert.AreEqual(arNumber, phoneUtil.Parse("03715 15 65 4320", RegionCode.AR));
            Assert.AreEqual(AR_MOBILE, phoneUtil.Parse("911 876 54321", RegionCode.AR));

            // Test parsing fixed-line numbers of Argentina.
            Assert.AreEqual(AR_NUMBER, phoneUtil.Parse("+54 11 8765 4321", RegionCode.AR));
            Assert.AreEqual(AR_NUMBER, phoneUtil.Parse("011 8765 4321", RegionCode.AR));

            arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(3715654321L).Build();
            Assert.AreEqual(arNumber, phoneUtil.Parse("+54 3715 65 4321", RegionCode.AR));
            Assert.AreEqual(arNumber, phoneUtil.Parse("03715 65 4321", RegionCode.AR));

            arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(2312340000L).Build();
            Assert.AreEqual(arNumber, phoneUtil.Parse("+54 23 1234 0000", RegionCode.AR));
            Assert.AreEqual(arNumber, phoneUtil.Parse("023 1234 0000", RegionCode.AR));
        }

        [Test]
        public void TestParseWithXInNumber()
        {
            // Test that having an 'x' in the phone number at the start is ok and that it just gets removed.
            Assert.AreEqual(AR_NUMBER, phoneUtil.Parse("01187654321", RegionCode.AR));
            Assert.AreEqual(AR_NUMBER, phoneUtil.Parse("(0) 1187654321", RegionCode.AR));
            Assert.AreEqual(AR_NUMBER, phoneUtil.Parse("0 1187654321", RegionCode.AR));
            Assert.AreEqual(AR_NUMBER, phoneUtil.Parse("(0xx) 1187654321", RegionCode.AR));
            var arFromUs = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(81429712L).Build();
            // This test is intentionally constructed such that the number of digit after xx is larger than
            // 7, so that the number won't be mistakenly treated as an extension, as we allow extensions up
            // to 7 digits. This assumption is okay for now as all the countries where a carrier selection
            // code is written in the form of xx have a national significant number of length larger than 7.
            Assert.AreEqual(arFromUs, phoneUtil.Parse("011xx5481429712", RegionCode.US));
        }

        [Test]
        public void TestParseNumbersMexico()
        {
            // Test parsing fixed-line numbers of Mexico.
            PhoneNumber mxNumber = new PhoneNumber.Builder()
                .SetCountryCode(52).SetNationalNumber(4499780001L).Build();
            Assert.AreEqual(mxNumber, phoneUtil.Parse("+52 (449)978-0001", RegionCode.MX));
            Assert.AreEqual(mxNumber, phoneUtil.Parse("01 (449)978-0001", RegionCode.MX));
            Assert.AreEqual(mxNumber, phoneUtil.Parse("(449)978-0001", RegionCode.MX));

            // Test parsing mobile numbers of Mexico.
            mxNumber = new PhoneNumber.Builder()
                .SetCountryCode(52).SetNationalNumber(13312345678L).Build();
            Assert.AreEqual(mxNumber, phoneUtil.Parse("+52 1 33 1234-5678", RegionCode.MX));
            Assert.AreEqual(mxNumber, phoneUtil.Parse("044 (33) 1234-5678", RegionCode.MX));
            Assert.AreEqual(mxNumber, phoneUtil.Parse("045 33 1234-5678", RegionCode.MX));
        }

        [Test]
        public void TestFailedParseOnInvalidNumbers()
        {
            try
            {
                String sentencePhoneNumber = "This is not a phone number";
                phoneUtil.Parse(sentencePhoneNumber, RegionCode.NZ);
                Assert.Fail("This should not parse without throwing an exception " + sentencePhoneNumber);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(ErrorType.NOT_A_NUMBER,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String tooLongPhoneNumber = "01495 72553301873 810104";
                phoneUtil.Parse(tooLongPhoneNumber, RegionCode.GB);
                Assert.Fail("This should not parse without throwing an exception " + tooLongPhoneNumber);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.TOO_LONG,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String plusMinusPhoneNumber = "+---";
                phoneUtil.Parse(plusMinusPhoneNumber, RegionCode.DE);
                Assert.Fail("This should not parse without throwing an exception " + plusMinusPhoneNumber);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.NOT_A_NUMBER,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String tooShortPhoneNumber = "+49 0";
                phoneUtil.Parse(tooShortPhoneNumber, RegionCode.DE);
                Assert.Fail("This should not parse without throwing an exception " + tooShortPhoneNumber);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.TOO_SHORT_NSN,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String invalidCountryCode = "+210 3456 56789";
                phoneUtil.Parse(invalidCountryCode, RegionCode.NZ);
                Assert.Fail("This is not a recognised region code: should fail: " + invalidCountryCode);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                    ErrorType.INVALID_COUNTRY_CODE,
                   e.ErrorType,
                   "Wrong error type stored in exception.");
            }
            try
            {
                String plusAndIddAndInvalidCountryCode = "+ 00 210 3 331 6005";
                phoneUtil.Parse(plusAndIddAndInvalidCountryCode, RegionCode.NZ);
                Assert.Fail("This should not parse without throwing an exception.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception. 00 is a correct IDD, but 210 is not a valid country code.
                Assert.AreEqual(
                             ErrorType.INVALID_COUNTRY_CODE,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String someNumber = "123 456 7890";
                phoneUtil.Parse(someNumber, RegionCode.ZZ);
                Assert.Fail("'Unknown' region code not allowed: should fail.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.INVALID_COUNTRY_CODE,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String someNumber = "123 456 7890";
                phoneUtil.Parse(someNumber, RegionCode.CS);
                Assert.Fail("Deprecated region code not allowed: should fail.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.INVALID_COUNTRY_CODE,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String someNumber = "123 456 7890";
                phoneUtil.Parse(someNumber, null);
                Assert.Fail("Null region code not allowed: should fail.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.INVALID_COUNTRY_CODE,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String someNumber = "0044------";
                phoneUtil.Parse(someNumber, RegionCode.GB);
                Assert.Fail("No number provided, only region code: should fail");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.TOO_SHORT_AFTER_IDD,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String someNumber = "0044";
                phoneUtil.Parse(someNumber, RegionCode.GB);
                Assert.Fail("No number provided, only region code: should fail");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.TOO_SHORT_AFTER_IDD,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String someNumber = "011";
                phoneUtil.Parse(someNumber, RegionCode.US);
                Assert.Fail("Only IDD provided - should fail.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.TOO_SHORT_AFTER_IDD,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String someNumber = "0119";
                phoneUtil.Parse(someNumber, RegionCode.US);
                Assert.Fail("Only IDD provided and then 9 - should fail.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.TOO_SHORT_AFTER_IDD,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String emptyNumber = "";
                // Invalid region.
                phoneUtil.Parse(emptyNumber, RegionCode.ZZ);
                Assert.Fail("Empty string - should fail.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.NOT_A_NUMBER,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            try
            {
                String nullNumber = null;
                // Invalid region.
                phoneUtil.Parse(nullNumber, RegionCode.ZZ);
                Assert.Fail("Null string - should fail.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.NOT_A_NUMBER,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            catch (ArgumentNullException)
            {
                Assert.Fail("Null string - but should not throw a null pointer exception.");
            }
            try
            {
                String nullNumber = null;
                phoneUtil.Parse(nullNumber, RegionCode.US);
                Assert.Fail("Null string - should fail.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                             ErrorType.NOT_A_NUMBER,
                             e.ErrorType,
                             "Wrong error type stored in exception.");
            }
            catch (ArgumentNullException)
            {
                Assert.Fail("Null string - but should not throw a null pointer exception.");
            }
        }

        [Test]
        public void TestParseNumbersWithPlusWithNoRegion()
        {
            // RegionCode.ZZ is allowed only if the number starts with a '+' - then the country calling code
            // can be calculated.
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("+64 3 331 6005", RegionCode.ZZ));
            // Test with full-width plus.
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("\uFF0B64 3 331 6005", RegionCode.ZZ));
            // Test with normal plus but leading characters that need to be stripped.
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("Tel: +64 3 331 6005", RegionCode.ZZ));
            Assert.AreEqual(NZ_NUMBER, phoneUtil.Parse("+64 3 331 6005", null));

            // It is important that we set the carrier code to an empty string, since we used
            // ParseAndKeepRawInput and no carrier code was found.
            PhoneNumber nzNumberWithRawInput = new PhoneNumber.Builder().MergeFrom(NZ_NUMBER)
                .SetRawInput("+64 3 331 6005")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN)
                .SetPreferredDomesticCarrierCode("").Build();
            Assert.AreEqual(nzNumberWithRawInput, phoneUtil.ParseAndKeepRawInput("+64 3 331 6005",
                                                                      RegionCode.ZZ));
            // Null is also allowed for the region code in these cases.
            Assert.AreEqual(nzNumberWithRawInput, phoneUtil.ParseAndKeepRawInput("+64 3 331 6005", null));
        }

        [Test]
        public void TestParseExtensions()
        {
            PhoneNumber nzNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(33316005L).SetExtension("3456").Build();
            Assert.AreEqual(nzNumber, phoneUtil.Parse("03 331 6005 ext 3456", RegionCode.NZ));
            Assert.AreEqual(nzNumber, phoneUtil.Parse("03-3316005x3456", RegionCode.NZ));
            Assert.AreEqual(nzNumber, phoneUtil.Parse("03-3316005 int.3456", RegionCode.NZ));
            Assert.AreEqual(nzNumber, phoneUtil.Parse("03 3316005 #3456", RegionCode.NZ));
            // Test the following do not extract extensions:
            Assert.AreEqual(ALPHA_NUMERIC_NUMBER, phoneUtil.Parse("1800 six-flags", RegionCode.US));
            Assert.AreEqual(ALPHA_NUMERIC_NUMBER, phoneUtil.Parse("1800 SIX FLAGS", RegionCode.US));
            Assert.AreEqual(ALPHA_NUMERIC_NUMBER, phoneUtil.Parse("0~0 1800 7493 5247", RegionCode.PL));
            Assert.AreEqual(ALPHA_NUMERIC_NUMBER, phoneUtil.Parse("(1800) 7493.5247", RegionCode.US));
            // Check that the last instance of an extension token is matched.
            PhoneNumber extnNumber = new PhoneNumber.Builder().MergeFrom(ALPHA_NUMERIC_NUMBER).SetExtension("1234").Build();
            Assert.AreEqual(extnNumber, phoneUtil.Parse("0~0 1800 7493 5247 ~1234", RegionCode.PL));
            // Verifying bug-fix where the last digit of a number was previously omitted if it was a 0 when
            // extracting the extension. Also verifying a few different cases of extensions.
            PhoneNumber ukNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(2034567890L).SetExtension("456").Build();
            Assert.AreEqual(ukNumber, phoneUtil.Parse("+44 2034567890x456", RegionCode.NZ));
            Assert.AreEqual(ukNumber, phoneUtil.Parse("+44 2034567890x456", RegionCode.GB));
            Assert.AreEqual(ukNumber, phoneUtil.Parse("+44 2034567890 x456", RegionCode.GB));
            Assert.AreEqual(ukNumber, phoneUtil.Parse("+44 2034567890 X456", RegionCode.GB));
            Assert.AreEqual(ukNumber, phoneUtil.Parse("+44 2034567890 X 456", RegionCode.GB));
            Assert.AreEqual(ukNumber, phoneUtil.Parse("+44 2034567890 X  456", RegionCode.GB));
            Assert.AreEqual(ukNumber, phoneUtil.Parse("+44 2034567890 x 456  ", RegionCode.GB));
            Assert.AreEqual(ukNumber, phoneUtil.Parse("+44 2034567890  X 456", RegionCode.GB));
            Assert.AreEqual(ukNumber, phoneUtil.Parse("+44-2034567890;ext=456", RegionCode.GB));

            PhoneNumber usWithExtension = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(8009013355L).SetExtension("7246433").Build();
            Assert.AreEqual(usWithExtension, phoneUtil.Parse("(800) 901-3355 x 7246433", RegionCode.US));
            Assert.AreEqual(usWithExtension, phoneUtil.Parse("(800) 901-3355 , ext 7246433", RegionCode.US));
            Assert.AreEqual(usWithExtension,
                     phoneUtil.Parse("(800) 901-3355 ,extension 7246433", RegionCode.US));
            Assert.AreEqual(usWithExtension,
                     phoneUtil.Parse("(800) 901-3355 ,extensi\u00F3n 7246433", RegionCode.US));
            // Repeat with the small letter o with acute accent created by combining characters.
            Assert.AreEqual(usWithExtension,
                     phoneUtil.Parse("(800) 901-3355 ,extensio\u0301n 7246433", RegionCode.US));
            Assert.AreEqual(usWithExtension, phoneUtil.Parse("(800) 901-3355 , 7246433", RegionCode.US));
            Assert.AreEqual(usWithExtension, phoneUtil.Parse("(800) 901-3355 ext: 7246433", RegionCode.US));

            // Test that if a number has two extensions specified, we ignore the second.
            PhoneNumber usWithTwoExtensionsNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(2121231234L).SetExtension("508").Build();
            Assert.AreEqual(usWithTwoExtensionsNumber, phoneUtil.Parse("(212)123-1234 x508/x1234",
                                                                RegionCode.US));
            Assert.AreEqual(usWithTwoExtensionsNumber, phoneUtil.Parse("(212)123-1234 x508/ x1234",
                                                                RegionCode.US));
            Assert.AreEqual(usWithTwoExtensionsNumber, phoneUtil.Parse("(212)123-1234 x508\\x1234",
                                                                RegionCode.US));

            // Test parsing numbers in the form (645) 123-1234-910# works, where the last 3 digits before
            // the # are an extension.
            usWithExtension = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6451231234L).SetExtension("910").Build();
            Assert.AreEqual(usWithExtension, phoneUtil.Parse("+1 (645) 123 1234-910#", RegionCode.US));
            // Retry with the same number in a slightly different format.
            Assert.AreEqual(usWithExtension, phoneUtil.Parse("+1 (645) 123 1234 ext. 910#", RegionCode.US));
        }

        [Test]
        public void TestParseAndKeepRaw()
        {
            PhoneNumber alphaNumericNumber = new PhoneNumber.Builder().MergeFrom(ALPHA_NUMERIC_NUMBER)
                .SetRawInput("800 six-flags")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY)
                .SetPreferredDomesticCarrierCode("").Build();
            Assert.AreEqual(alphaNumericNumber,
                phoneUtil.ParseAndKeepRawInput("800 six-flags", RegionCode.US));

            PhoneNumber shorterAlphaNumber = new PhoneNumber.Builder()
                .SetCountryCode(1)
                .SetNationalNumber(8007493524L)
                .SetRawInput("1800 six-flag")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN)
                .SetPreferredDomesticCarrierCode("").Build();
            Assert.AreEqual(shorterAlphaNumber,
                phoneUtil.ParseAndKeepRawInput("1800 six-flag", RegionCode.US));

            shorterAlphaNumber = Update(shorterAlphaNumber).SetRawInput("+1800 six-flag").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();
            Assert.AreEqual(shorterAlphaNumber,
                phoneUtil.ParseAndKeepRawInput("+1800 six-flag", RegionCode.NZ));

            shorterAlphaNumber = Update(shorterAlphaNumber).SetRawInput("001800 six-flag").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD).Build();
            Assert.AreEqual(shorterAlphaNumber,
                phoneUtil.ParseAndKeepRawInput("001800 six-flag", RegionCode.NZ));

            // Invalid region code supplied.
            try
            {
                phoneUtil.ParseAndKeepRawInput("123 456 7890", RegionCode.CS);
                Assert.Fail("Deprecated region code not allowed: should fail.");
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.AreEqual(
                    ErrorType.INVALID_COUNTRY_CODE,
                    e.ErrorType,
                    "Wrong error type stored in exception.");
            }

            PhoneNumber koreanNumber = new PhoneNumber.Builder()
                .SetCountryCode(82).SetNationalNumber(22123456).SetRawInput("08122123456").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY).
                SetPreferredDomesticCarrierCode("81").Build();
            Assert.AreEqual(koreanNumber, phoneUtil.ParseAndKeepRawInput("08122123456", RegionCode.KR));
        }

        [Test]
        public void TestCountryWithNoNumberDesc()
        {
            // Andorra is a country where we don't have PhoneNumberDesc info in the metadata.
            PhoneNumber adNumber = new PhoneNumber.Builder()
                .SetCountryCode(376).SetNationalNumber(12345L).Build();
            Assert.AreEqual("+376 12345", phoneUtil.Format(adNumber, PhoneNumberFormat.INTERNATIONAL));
            Assert.AreEqual("+37612345", phoneUtil.Format(adNumber, PhoneNumberFormat.E164));
            Assert.AreEqual("12345", phoneUtil.Format(adNumber, PhoneNumberFormat.NATIONAL));
            Assert.AreEqual(PhoneNumberType.UNKNOWN, phoneUtil.GetNumberType(adNumber));
            Assert.That(phoneUtil.IsValidNumber(adNumber));

            // Test dialing a US number from within Andorra.
            Assert.AreEqual("00 1 650 253 0000",
            phoneUtil.FormatOutOfCountryCallingNumber(US_NUMBER, RegionCode.AD));
        }

        [Test]
        public void TestUnknownCountryCallingCodeForValidation()
        {
            PhoneNumber invalidNumber = new PhoneNumber.Builder()
                .SetCountryCode(0).SetNationalNumber(1234L).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));
        }

        [Test]
        public void TestIsNumberMatchMatches()
        {
            // Test simple matches where formatting is different, or leading zeroes, or country calling code
            // has been specified.
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331 6005", "+64 03 331 6005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+64 03 331-6005", "+64 03331 6005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+643 331-6005", "+64033316005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+643 331-6005", "+6433316005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "+6433316005"));
            // Test alpha numbers.
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+1800 siX-Flags", "+1 800 7493 5247"));
            // Test numbers with extensions.
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005 extn 1234", "+6433316005#1234"));
            // Test proto buffers.
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch(NZ_NUMBER, "+6403 331 6005"));

            PhoneNumber nzNumber = new PhoneNumber.Builder().MergeFrom(NZ_NUMBER).SetExtension("3456").Build();
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch(nzNumber, "+643 331 6005 ext 3456"));
            // Check empty extensions are ignored.
            nzNumber = Update(nzNumber).SetExtension("").Build();
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch(nzNumber, "+6403 331 6005"));
            // Check variant with two proto buffers.
            Assert.AreEqual(
                PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch(nzNumber, NZ_NUMBER),
                "Number " + nzNumber.ToString() + " did not match " + NZ_NUMBER.ToString());

            // Check raw_input, country_code_source and preferred_domestic_carrier_code are ignored.
            PhoneNumber brNumberOne = new PhoneNumber.Builder()
                .SetCountryCode(55).SetNationalNumber(3121286979L)
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN)
                .SetPreferredDomesticCarrierCode("12").SetRawInput("012 3121286979").Build();
            PhoneNumber brNumberTwo = new PhoneNumber.Builder()
                .SetCountryCode(55).SetNationalNumber(3121286979L)
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY)
                .SetPreferredDomesticCarrierCode("14").SetRawInput("143121286979").Build();
            Assert.AreEqual(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch(brNumberOne, brNumberTwo));
        }

        [Test]
        public void TestIsNumberMatchNonMatches()
        {
            // Non-matches.
            Assert.AreEqual(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("03 331 6005", "03 331 6006"));
            // Different country calling code, partial number match.
            Assert.AreEqual(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "+16433316005"));
            // Different country calling code, same number.
            Assert.AreEqual(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "+6133316005"));
            // Extension different, all else the same.
            Assert.AreEqual(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005 extn 1234", "0116433316005#1235"));
            // NSN matches, but extension is different - not the same number.
            Assert.AreEqual(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005 ext.1235", "3 331 6005#1234"));

            // Invalid numbers that can't be parsed.
            Assert.AreEqual(PhoneNumberUtil.MatchType.NOT_A_NUMBER,
                phoneUtil.IsNumberMatch("43", "3 331 6043"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.NOT_A_NUMBER,
                phoneUtil.IsNumberMatch("+43", "+64 3 331 6005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.NOT_A_NUMBER,
                phoneUtil.IsNumberMatch("+43", "64 3 331 6005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.NOT_A_NUMBER,
                phoneUtil.IsNumberMatch("Dog", "64 3 331 6005"));
        }

        [Test]
        public void TestIsNumberMatchNsnMatches()
        {
            // NSN matches.
            Assert.AreEqual(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "03 331 6005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch(NZ_NUMBER, "03 331 6005"));
            // Here the second number possibly starts with the country calling code for New Zealand,
            // although we are unsure.
            PhoneNumber unchangedNzNumber = new PhoneNumber.Builder().MergeFrom(NZ_NUMBER).Build();
            Assert.AreEqual(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch(unchangedNzNumber, "(64-3) 331 6005"));
            // Check the phone number proto was not edited during the method call.
            Assert.AreEqual(NZ_NUMBER, unchangedNzNumber);

            // Here, the 1 might be a national prefix, if we compare it to the US number, so the resultant
            // match is an NSN match.
            Assert.AreEqual(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch(US_NUMBER, "1-650-253-0000"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch(US_NUMBER, "6502530000"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch("+1 650-253 0000", "1 650 253 0000"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch("1 650-253 0000", "1 650 253 0000"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch("1 650-253 0000", "+1 650 253 0000"));
            // For this case, the match will be a short NSN match, because we cannot assume that the 1 might
            // be a national prefix, so don't remove it when parsing.
            PhoneNumber randomNumber = new PhoneNumber.Builder()
                .SetCountryCode(41).SetNationalNumber(6502530000L).Build();
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch(randomNumber, "1-650-253-0000"));
        }


        [Test]
        public void TestIsNumberMatchShortNsnMatches()
        {
            // Short NSN matches with the country not specified for either one or both numbers.
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "331 6005"));
            // We did not know that the "0" was a national prefix since neither number has a country code,
            // so this is considered a SHORT_NSN_MATCH.
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("3 331-6005", "03 331 6005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("3 331-6005", "331 6005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("3 331-6005", "+64 331 6005"));
            // Short NSN match with the country specified.
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("03 331-6005", "331 6005"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("1 234 345 6789", "345 6789"));
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("+1 (234) 345 6789", "345 6789"));
            // NSN matches, country calling code omitted for one number, extension missing for one.
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "3 331 6005#1234"));
            // One has Italian leading zero, one does not.
            PhoneNumber italianNumberOne = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(1234L).SetItalianLeadingZero(true).Build();
            PhoneNumber italianNumberTwo = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(1234L).Build();
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch(italianNumberOne, italianNumberTwo));
            // One has an extension, the other has an extension of "".
            italianNumberOne = Update(italianNumberOne).SetExtension("1234").ClearItalianLeadingZero().Build();
            italianNumberTwo = Update(italianNumberTwo).SetExtension("").Build();
            Assert.AreEqual(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch(italianNumberOne, italianNumberTwo));
        }

        [Test]
        public void TestCanBeInternationallyDialled()
        {
            // We have no-international-dialling rules for the US in our test metadata that say that
            // toll-free numbers cannot be dialled internationally.
            Assert.False(phoneUtil.CanBeInternationallyDialled(US_TOLLFREE));

            // Normal US numbers can be internationally dialled.
            Assert.That(phoneUtil.CanBeInternationallyDialled(US_NUMBER));

            // Invalid number.
            Assert.That(phoneUtil.CanBeInternationallyDialled(US_LOCAL_NUMBER));

            // We have no data for NZ - should return true.
            Assert.That(phoneUtil.CanBeInternationallyDialled(NZ_NUMBER));
        }

        [Test]
        public void TestIsAlphaNumber()
        {
            Assert.That(phoneUtil.IsAlphaNumber("1800 six-flags"));
            Assert.That(phoneUtil.IsAlphaNumber("1800 six-flags ext. 1234"));
            Assert.False(phoneUtil.IsAlphaNumber("1800 123-1234"));
            Assert.False(phoneUtil.IsAlphaNumber("1800 123-1234 extension: 1234"));
        }
    }
}
/*




}
*/