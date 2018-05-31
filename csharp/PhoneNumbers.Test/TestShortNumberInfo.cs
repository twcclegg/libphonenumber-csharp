/*
 * Copyright (C) 2011 The Libphonenumber Authors
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
using Xunit;

namespace PhoneNumbers.Test
{
    /**
     * Unit tests for ShortNumberUtil.java
     *
     * @author Shaopeng Jia
     */
    [Collection("TestMetadataTestCase")]
    public class TestShortNumberInfo : IClassFixture<TestMetadataTestCase>
    {
        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
        private readonly ShortNumberInfo shortInfo = ShortNumberInfo.GetInstance();

        [Fact]
        public void TestIsPossibleShortNumber()
        {
            var possibleNumber = new PhoneNumber.Builder();
            possibleNumber.SetCountryCode(33).SetNationalNumber(123456L);
            Assert.True(shortInfo.IsPossibleShortNumber(possibleNumber.Build()));
            Assert.True(
                shortInfo.IsPossibleShortNumberForRegion(Parse("123456", RegionCode.FR), RegionCode.FR));

            var impossibleNumber = new PhoneNumber.Builder();
            impossibleNumber.SetCountryCode(33).SetNationalNumber(9L);
            Assert.False(shortInfo.IsPossibleShortNumber(impossibleNumber.Build()));

            // Note that GB and GG share the country calling code 44, and that this number is possible but
            // not valid.
            Assert.True(shortInfo.IsPossibleShortNumber(
                new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(11001L).Build()));
        }


        [Fact]
        public void TestIsValidShortNumber()
        {
            Assert.True(shortInfo.IsValidShortNumber(
                new PhoneNumber.Builder().SetCountryCode(33).SetNationalNumber(1010L).Build()));
            Assert.True(shortInfo.IsValidShortNumberForRegion(Parse("1010", RegionCode.FR), RegionCode.FR));
            Assert.False(shortInfo.IsValidShortNumber(
                new PhoneNumber.Builder().SetCountryCode(33).SetNationalNumber(123456L).Build()));
            Assert.False(
                shortInfo.IsValidShortNumberForRegion(Parse("123456", RegionCode.FR), RegionCode.FR));

            // Note that GB and GG share the country calling code 44.
            Assert.True(shortInfo.IsValidShortNumber(
                new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(18001L).Build()));
        }

        [Fact]
        public void TestIsCarrierSpecific()
        {
            var carrierSpecificNumber = new PhoneNumber.Builder();
            carrierSpecificNumber.SetCountryCode(1).SetNationalNumber(33669L);
            Assert.True(shortInfo.IsCarrierSpecific(carrierSpecificNumber.Build()));
            Assert.True(
                shortInfo.IsCarrierSpecificForRegion(Parse("33669", RegionCode.US), RegionCode.US));

            var notCarrierSpecificNumber = new PhoneNumber.Builder();
            notCarrierSpecificNumber.SetCountryCode(1).SetNationalNumber(911L);
            Assert.False(shortInfo.IsCarrierSpecific(notCarrierSpecificNumber.Build()));
            Assert.False(
                shortInfo.IsCarrierSpecificForRegion(Parse("911", RegionCode.US), RegionCode.US));

            var carrierSpecificNumberForSomeRegion = new PhoneNumber.Builder();
            carrierSpecificNumberForSomeRegion.SetCountryCode(1).SetNationalNumber(211L);
            Assert.True(shortInfo.IsCarrierSpecific(carrierSpecificNumberForSomeRegion.Build()));
            Assert.True(
                shortInfo.IsCarrierSpecificForRegion(carrierSpecificNumberForSomeRegion.Build(), RegionCode.US));
            Assert.False(
                shortInfo.IsCarrierSpecificForRegion(carrierSpecificNumberForSomeRegion.Build(), RegionCode.BB));
        }

        [Fact]
        public void TestIsSmsService()
        {
            var smsServiceNumberForSomeRegion = new PhoneNumber.Builder();
            smsServiceNumberForSomeRegion.SetCountryCode(1).SetNationalNumber(21234L);
            Assert.True(shortInfo.IsSmsServiceForRegion(smsServiceNumberForSomeRegion.Build(), RegionCode.US));
            Assert.False(shortInfo.IsSmsServiceForRegion(smsServiceNumberForSomeRegion.Build(), RegionCode.BB));
        }

        [Fact]
        public void TestGetExpectedCost()
        {
            var premiumRateExample = shortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.PREMIUM_RATE);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE, shortInfo.GetExpectedCostForRegion(
                Parse(premiumRateExample, RegionCode.FR), RegionCode.FR));
            var premiumRateNumber = new PhoneNumber.Builder();
            premiumRateNumber.SetCountryCode(33).SetNationalNumber(ulong.Parse(premiumRateExample));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE,
                shortInfo.GetExpectedCost(premiumRateNumber.Build()));

            var standardRateExample = shortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.STANDARD_RATE);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.STANDARD_RATE, shortInfo.GetExpectedCostForRegion(
                Parse(standardRateExample, RegionCode.FR), RegionCode.FR));
            var standardRateNumber = new PhoneNumber.Builder();
            standardRateNumber.SetCountryCode(33).SetNationalNumber(ulong.Parse(standardRateExample));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.STANDARD_RATE,
                shortInfo.GetExpectedCost(standardRateNumber.Build()));

            var tollFreeExample = shortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.TOLL_FREE);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.GetExpectedCostForRegion(Parse(tollFreeExample, RegionCode.FR), RegionCode.FR));
            var tollFreeNumber = new PhoneNumber.Builder();
            tollFreeNumber.SetCountryCode(33).SetNationalNumber(ulong.Parse(tollFreeExample));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.GetExpectedCost(tollFreeNumber.Build()));

            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.GetExpectedCostForRegion(Parse("12345", RegionCode.FR), RegionCode.FR));
            var unknownCostNumber = new PhoneNumber.Builder();
            unknownCostNumber.SetCountryCode(33).SetNationalNumber(12345L);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.GetExpectedCost(unknownCostNumber.Build()));

            // Test that an invalid number may nevertheless have a cost other than UNKNOWN_COST.
            Assert.False(
                shortInfo.IsValidShortNumberForRegion(Parse("116123", RegionCode.FR), RegionCode.FR));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.GetExpectedCostForRegion(Parse("116123", RegionCode.FR), RegionCode.FR));
            var invalidNumber = new PhoneNumber.Builder();
            invalidNumber.SetCountryCode(33).SetNationalNumber(116123L);
            Assert.False(shortInfo.IsValidShortNumber(invalidNumber.Build()));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.GetExpectedCost(invalidNumber.Build()));

            // Test a nonexistent country code.
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.GetExpectedCostForRegion(Parse("911", RegionCode.US), RegionCode.ZZ));
            unknownCostNumber.Clear();
            unknownCostNumber.SetCountryCode(123).SetNationalNumber(911L);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.GetExpectedCost(unknownCostNumber.Build()));
        }

        [Fact]
        public void TestGetExpectedCostForSharedCountryCallingCode()
        {
            // Test some numbers which have different costs in countries sharing the same country calling
            // code. In Australia, 1234 is premium-rate, 1194 is standard-rate, and 733 is toll-free. These
            // are not known to be valid numbers in the Christmas Islands.
            var ambiguousPremiumRateString = "1234";
            var ambiguousPremiumRateNumber =
                new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(1234L);
            var ambiguousStandardRateString = "1194";
            var ambiguousStandardRateNumber =
                new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(1194L);
            var ambiguousTollFreeString = "733";
            var ambiguousTollFreeNumber =
                new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(733L);

            Assert.True(shortInfo.IsValidShortNumber(ambiguousPremiumRateNumber.Build()));
            Assert.True(shortInfo.IsValidShortNumber(ambiguousStandardRateNumber.Build()));
            Assert.True(shortInfo.IsValidShortNumber(ambiguousTollFreeNumber.Build()));

            Assert.True(shortInfo.IsValidShortNumberForRegion(
                Parse(ambiguousPremiumRateString, RegionCode.AU), RegionCode.AU));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE, shortInfo.GetExpectedCostForRegion(
                Parse(ambiguousPremiumRateString, RegionCode.AU), RegionCode.AU));
            Assert.False(shortInfo.IsValidShortNumberForRegion(
                Parse(ambiguousPremiumRateString, RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST, shortInfo.GetExpectedCostForRegion(
                Parse(ambiguousPremiumRateString, RegionCode.CX), RegionCode.CX));
            // PREMIUM_RATE takes precedence over UNKNOWN_COST.
            Assert.Equal(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE,
                shortInfo.GetExpectedCost(ambiguousPremiumRateNumber.Build()));

            Assert.True(shortInfo.IsValidShortNumberForRegion(
                Parse(ambiguousStandardRateString, RegionCode.AU), RegionCode.AU));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.STANDARD_RATE, shortInfo.GetExpectedCostForRegion(
                Parse(ambiguousStandardRateString, RegionCode.AU), RegionCode.AU));
            Assert.False(shortInfo.IsValidShortNumberForRegion(
                Parse(ambiguousStandardRateString, RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST, shortInfo.GetExpectedCostForRegion(
                Parse(ambiguousStandardRateString, RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.GetExpectedCost(ambiguousStandardRateNumber.Build()));

            Assert.True(shortInfo.IsValidShortNumberForRegion(Parse(ambiguousTollFreeString, RegionCode.AU),
                RegionCode.AU));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE, shortInfo.GetExpectedCostForRegion(
                Parse(ambiguousTollFreeString, RegionCode.AU), RegionCode.AU));
            Assert.False(shortInfo.IsValidShortNumberForRegion(Parse(ambiguousTollFreeString, RegionCode.CX),
                RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST, shortInfo.GetExpectedCostForRegion(
                Parse(ambiguousTollFreeString, RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.GetExpectedCost(ambiguousTollFreeNumber.Build()));
        }

        [Fact]
        public void TestGetExampleShortNumber()
        {
            Assert.Equal("8711", shortInfo.GetExampleShortNumber(RegionCode.AM));
            Assert.Equal("1010", shortInfo.GetExampleShortNumber(RegionCode.FR));
            Assert.Equal("", shortInfo.GetExampleShortNumber(RegionCode.UN001));
            Assert.Equal("", shortInfo.GetExampleShortNumber(null));
        }

        [Fact]
        public void TestGetExampleShortNumberForCost()
        {
            Assert.Equal("3010", shortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.TOLL_FREE));
            Assert.Equal("1023", shortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.STANDARD_RATE));
            Assert.Equal("42000", shortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.PREMIUM_RATE));
            Assert.Equal("", shortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.UNKNOWN_COST));
        }


        [Fact]
        public void TestConnectsToEmergencyNumber_US()
        {
            Assert.True(shortInfo.ConnectsToEmergencyNumber("911", RegionCode.US));
            Assert.True(shortInfo.ConnectsToEmergencyNumber("119", RegionCode.US));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("999", RegionCode.US));
        }

        [Fact]
        public void TestConnectsToEmergencyNumberLongNumber_US()
        {
            Assert.True(shortInfo.ConnectsToEmergencyNumber("9116666666", RegionCode.US));
            Assert.True(shortInfo.ConnectsToEmergencyNumber("1196666666", RegionCode.US));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("9996666666", RegionCode.US));
        }

        [Fact]
        public void TestConnectsToEmergencyNumberWithFormatting_US()
        {
            Assert.True(shortInfo.ConnectsToEmergencyNumber("9-1-1", RegionCode.US));
            Assert.True(shortInfo.ConnectsToEmergencyNumber("1-1-9", RegionCode.US));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("9-9-9", RegionCode.US));
        }

        [Fact]
        public void TestConnectsToEmergencyNumberWithPlusSign_US()
        {
            Assert.False(shortInfo.ConnectsToEmergencyNumber("+911", RegionCode.US));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("\uFF0B911", RegionCode.US));
            Assert.False(shortInfo.ConnectsToEmergencyNumber(" +911", RegionCode.US));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("+119", RegionCode.US));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("+999", RegionCode.US));
        }

        [Fact]
        public void TestConnectsToEmergencyNumber_BR()
        {
            Assert.True(shortInfo.ConnectsToEmergencyNumber("911", RegionCode.BR));
            Assert.True(shortInfo.ConnectsToEmergencyNumber("190", RegionCode.BR));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("999", RegionCode.BR));
        }

        [Fact]
        public void TestConnectsToEmergencyNumberLongNumber_BR()
        {
            // Brazilian emergency numbers don't work when additional digits are appended.
            Assert.False(shortInfo.ConnectsToEmergencyNumber("9111", RegionCode.BR));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("1900", RegionCode.BR));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("9996", RegionCode.BR));
        }

        [Fact]
        public void TestConnectsToEmergencyNumber_AO()
        {
            // Angola doesn't have any metadata for emergency numbers in the test metadata.
            Assert.False(shortInfo.ConnectsToEmergencyNumber("911", RegionCode.AO));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("222123456", RegionCode.AO));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("923123456", RegionCode.AO));
        }

        [Fact]
        public void TestConnectsToEmergencyNumber_ZW()
        {
            // Zimbabwe doesn't have any metadata in the test metadata.
            Assert.False(shortInfo.ConnectsToEmergencyNumber("911", RegionCode.ZW));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("01312345", RegionCode.ZW));
            Assert.False(shortInfo.ConnectsToEmergencyNumber("0711234567", RegionCode.ZW));
        }

        [Fact(Skip = "todo fix short numbers")]
        public void TestIsEmergencyNumber_US()
        {
            Assert.True(shortInfo.IsEmergencyNumber("911", RegionCode.US));
            Assert.True(shortInfo.IsEmergencyNumber("119", RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber("999", RegionCode.US));
        }

        [Fact]
        public void TestIsEmergencyNumberLongNumber_US()
        {
            Assert.False(shortInfo.IsEmergencyNumber("9116666666", RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber("1196666666", RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber("9996666666", RegionCode.US));
        }

        [Fact]
        public void TestIsEmergencyNumberWithFormatting_US()
        {
            Assert.True(shortInfo.IsEmergencyNumber("9-1-1", RegionCode.US));
            Assert.True(shortInfo.IsEmergencyNumber("*911", RegionCode.US));
            Assert.True(shortInfo.IsEmergencyNumber("1-1-9", RegionCode.US));
            Assert.True(shortInfo.IsEmergencyNumber("*119", RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber("9-9-9", RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber("*999", RegionCode.US));
        }

        [Fact]
        public void TestIsEmergencyNumberWithPlusSign_US()
        {
            Assert.False(shortInfo.IsEmergencyNumber("+911", RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber("\uFF0B911", RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber(" +911", RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber("+119", RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber("+999", RegionCode.US));
        }

        [Fact]
        public void TestIsEmergencyNumber_BR()
        {
            Assert.True(shortInfo.IsEmergencyNumber("911", RegionCode.BR));
            Assert.True(shortInfo.IsEmergencyNumber("190", RegionCode.BR));
            Assert.False(shortInfo.IsEmergencyNumber("999", RegionCode.BR));
        }

        [Fact]
        public void TestIsEmergencyNumberLongNumber_BR()
        {
            Assert.False(shortInfo.IsEmergencyNumber("9111", RegionCode.BR));
            Assert.False(shortInfo.IsEmergencyNumber("1900", RegionCode.BR));
            Assert.False(shortInfo.IsEmergencyNumber("9996", RegionCode.BR));
        }

        [Fact]
        public void TestIsEmergencyNumber_AO()
        {
            // Angola doesn't have any metadata for emergency numbers in the test metadata.
            Assert.False(shortInfo.IsEmergencyNumber("911", RegionCode.AO));
            Assert.False(shortInfo.IsEmergencyNumber("222123456", RegionCode.AO));
            Assert.False(shortInfo.IsEmergencyNumber("923123456", RegionCode.AO));
        }

        [Fact]
        public void TestIsEmergencyNumber_ZW()
        {
            // Zimbabwe doesn't have any metadata in the test metadata.
            Assert.False(shortInfo.IsEmergencyNumber("911", RegionCode.ZW));
            Assert.False(shortInfo.IsEmergencyNumber("01312345", RegionCode.ZW));
            Assert.False(shortInfo.IsEmergencyNumber("0711234567", RegionCode.ZW));
        }


        [Fact]
        public void TestEmergencyNumberForSharedCountryCallingCode()
        {
            // Test the emergency number 112, which is valid in both Australia and the Christmas Islands.
            Assert.True(shortInfo.IsEmergencyNumber("112", RegionCode.AU));
            Assert.True(shortInfo.IsValidShortNumberForRegion(Parse("112", RegionCode.AU), RegionCode.AU));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.GetExpectedCostForRegion(Parse("112", RegionCode.AU), RegionCode.AU));
            Assert.True(shortInfo.IsEmergencyNumber("112", RegionCode.CX));
            Assert.True(shortInfo.IsValidShortNumberForRegion(Parse("112", RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.GetExpectedCostForRegion(Parse("112", RegionCode.CX), RegionCode.CX));
            var sharedEmergencyNumber =
                new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(112L).Build();
            Assert.True(shortInfo.IsValidShortNumber(sharedEmergencyNumber));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.GetExpectedCost(sharedEmergencyNumber));
        }

        [Fact]
        public void TestOverlappingNANPANumber()
        {
            // 211 is an emergency number in Barbados, while it is a toll-free information line in Canada
            // and the USA.
            Assert.True(shortInfo.IsEmergencyNumber("211", RegionCode.BB));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.GetExpectedCostForRegion(Parse("211", RegionCode.BB), RegionCode.BB));
            Assert.False(shortInfo.IsEmergencyNumber("211", RegionCode.US));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.GetExpectedCostForRegion(Parse("211", RegionCode.US), RegionCode.US));
            Assert.False(shortInfo.IsEmergencyNumber("211", RegionCode.CA));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.GetExpectedCostForRegion(Parse("211", RegionCode.CA), RegionCode.CA));
        }

        [Fact]
        public void TestCountryCallingCodeIsNotIgnored()
        {
            // +46 is the country calling code for Sweden (SE), and 40404 is a valid short number in the US.
            Assert.False(shortInfo.IsPossibleShortNumberForRegion(
                Parse("+4640404", RegionCode.SE), RegionCode.US));
            Assert.False(shortInfo.IsValidShortNumberForRegion(
                Parse("+4640404", RegionCode.SE), RegionCode.US));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.GetExpectedCostForRegion(
                    Parse("+4640404", RegionCode.SE), RegionCode.US));
        }

        private PhoneNumber Parse(string number, string regionCode)
        {
            try
            {
                return phoneUtil.Parse(number, regionCode);
            }
            catch (NumberParseException e)
            {
                throw new Exception(
                    "Test input data should always Parse correctly: " + number + " (" + regionCode + ")", e);
            }
        }
    }
}
