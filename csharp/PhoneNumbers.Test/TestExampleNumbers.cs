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
using Xunit;

namespace PhoneNumbers.Test
{
    public class TestExampleNumbers
    {
        private readonly PhoneNumberUtil phoneNumberUtil;
        private readonly List<PhoneNumber> invalidCases = new List<PhoneNumber>();
        private readonly List<PhoneNumber> wrongTypeCases = new List<PhoneNumber>();

        public TestExampleNumbers()
        {
            invalidCases.Clear();
            wrongTypeCases.Clear();
            PhoneNumberUtil.ResetInstance();
            phoneNumberUtil = PhoneNumberUtil.GetInstance();
        }

        /**
        * @param exampleNumberRequestedType  type we are requesting an example number for
        * @param possibleExpectedTypes       acceptable types that this number should match, such as
        *     FIXED_LINE and FIXED_LINE_OR_MOBILE for a fixed line example number.
        */
        private void CheckNumbersValidAndCorrectType(PhoneNumberType exampleNumberRequestedType,
            HashSet<PhoneNumberType> possibleExpectedTypes)
        {
            foreach (var regionCode in phoneNumberUtil.GetSupportedRegions())
            {
                PhoneNumber exampleNumber =
                phoneNumberUtil.GetExampleNumberForType(regionCode, exampleNumberRequestedType);
                if (exampleNumber != null)
                {
                    if (!phoneNumberUtil.IsValidNumber(exampleNumber))
                    {
                        invalidCases.Add(exampleNumber);
                        //LOGGER.log(Level.SEVERE, "Failed validation for " + exampleNumber.toString());
                    }
                    else
                    {
                        // We know the number is valid, now we check the type.
                        PhoneNumberType exampleNumberType = phoneNumberUtil.GetNumberType(exampleNumber);
                        if (!possibleExpectedTypes.Contains(exampleNumberType))
                        {
                            wrongTypeCases.Add(exampleNumber);
                            //LOGGER.log(Level.SEVERE, "Wrong type for " + exampleNumber.toString() + ": got " + exampleNumberType);
                            //LOGGER.log(Level.WARNING, "Expected types: ");
                            //for (PhoneNumberType type : possibleExpectedTypes) {
                            //LOGGER.log(Level.WARNING, type.toString());
                        }
                    }
                }
            }
        }

        private HashSet<PhoneNumberType> MakeSet(PhoneNumberType t1, PhoneNumberType t2)
        {
            return new HashSet<PhoneNumberType>(new[] { t1, t2 });
        }

        private HashSet<PhoneNumberType> MakeSet(PhoneNumberType t1)
        {
            return MakeSet(t1, t1);
        }

        [Fact]
        public void TestFixedLine()
        {
            HashSet<PhoneNumberType> fixedLineTypes = MakeSet(PhoneNumberType.FIXED_LINE,
                                            PhoneNumberType.FIXED_LINE_OR_MOBILE);
            CheckNumbersValidAndCorrectType(PhoneNumberType.FIXED_LINE, fixedLineTypes);
            Assert.Equal(0, invalidCases.Count);
            Assert.Equal(0, wrongTypeCases.Count);
        }

        [Fact]
        public void TestMobile()
        {
            HashSet<PhoneNumberType> mobileTypes = MakeSet(PhoneNumberType.MOBILE,
                                                          PhoneNumberType.FIXED_LINE_OR_MOBILE);
            CheckNumbersValidAndCorrectType(PhoneNumberType.MOBILE, mobileTypes);
            Assert.Equal(0, invalidCases.Count);
            Assert.Equal(0, wrongTypeCases.Count);
        }

        [Fact]
        public void TestTollFree()
        {

            HashSet<PhoneNumberType> tollFreeTypes = MakeSet(PhoneNumberType.TOLL_FREE);
            CheckNumbersValidAndCorrectType(PhoneNumberType.TOLL_FREE, tollFreeTypes);
            Assert.Equal(0, invalidCases.Count);
            Assert.Equal(0, wrongTypeCases.Count);
        }

        [Fact]
        public void TestPremiumRate()
        {
            HashSet<PhoneNumberType> premiumRateTypes = MakeSet(PhoneNumberType.PREMIUM_RATE);
            CheckNumbersValidAndCorrectType(PhoneNumberType.PREMIUM_RATE, premiumRateTypes);
            Assert.Equal(0, invalidCases.Count);
            Assert.Equal(0, wrongTypeCases.Count);
        }

        [Fact]
        public void TestVoip()
        {
            HashSet<PhoneNumberType> voipTypes = MakeSet(PhoneNumberType.VOIP);
            CheckNumbersValidAndCorrectType(PhoneNumberType.VOIP, voipTypes);
            Assert.Equal(0, invalidCases.Count);
            Assert.Equal(0, wrongTypeCases.Count);
        }

        [Fact]
        public void TestPager()
        {
            HashSet<PhoneNumberType> pagerTypes = MakeSet(PhoneNumberType.PAGER);
            CheckNumbersValidAndCorrectType(PhoneNumberType.PAGER, pagerTypes);
            Assert.Equal(0, invalidCases.Count);
            Assert.Equal(0, wrongTypeCases.Count);
        }

        [Fact]
        public void TestUan()
        {
            HashSet<PhoneNumberType> uanTypes = MakeSet(PhoneNumberType.UAN);
            CheckNumbersValidAndCorrectType(PhoneNumberType.UAN, uanTypes);
            Assert.Equal(0, invalidCases.Count);
            Assert.Equal(0, wrongTypeCases.Count);
        }

        [Fact]
        public void TestVoicemail()
        {
            HashSet<PhoneNumberType> voicemailTypes = MakeSet(PhoneNumberType.VOICEMAIL);
            CheckNumbersValidAndCorrectType(PhoneNumberType.VOICEMAIL, voicemailTypes);
            Assert.Equal(0, invalidCases.Count);
            Assert.Equal(0, wrongTypeCases.Count);
        }

        [Fact]
        public void TestSharedCost()
        {
            HashSet<PhoneNumberType> sharedCostTypes = MakeSet(PhoneNumberType.SHARED_COST);
            CheckNumbersValidAndCorrectType(PhoneNumberType.SHARED_COST, sharedCostTypes);
            Assert.Equal(0, invalidCases.Count);
            Assert.Equal(0, wrongTypeCases.Count);
        }

        [Fact]
        public void TestCanBeInternationallyDialled()
        {
            foreach (var regionCode in phoneNumberUtil.GetSupportedRegions())
            {
                PhoneNumber exampleNumber = null;
                PhoneNumberDesc desc =
                    phoneNumberUtil.GetMetadataForRegion(regionCode).NoInternationalDialling;
                try
                {
                    if (desc.HasExampleNumber)
                    {
                        exampleNumber = phoneNumberUtil.Parse(desc.ExampleNumber, regionCode);
                    }
                }
                catch (NumberParseException)
                {
                }
                if (exampleNumber != null && phoneNumberUtil.CanBeInternationallyDialled(exampleNumber))
                {
                    wrongTypeCases.Add(exampleNumber);
                    // LOGGER.log(Level.SEVERE, "Number " + exampleNumber.toString()
                    //   + " should not be internationally diallable");
                }
            }
            Assert.Equal(0, wrongTypeCases.Count);
        }

        // TODO: Update this to use connectsToEmergencyNumber or similar once that is
        // implemented.
        [Fact]
        public void TestEmergency()
        {
            ShortNumberUtil shortUtil = new ShortNumberUtil(phoneNumberUtil);
            int wrongTypeCounter = 0;
            foreach(var regionCode in phoneNumberUtil.GetSupportedRegions())
            {
                PhoneNumberDesc desc =
                    phoneNumberUtil.GetMetadataForRegion(regionCode).Emergency;
                if (desc.HasExampleNumber)
                {
                    String exampleNumber = desc.ExampleNumber;
                    if (!new PhoneRegex(desc.PossibleNumberPattern).MatchAll(exampleNumber).Success ||
                        !shortUtil.IsEmergencyNumber(exampleNumber, regionCode))
                    {
                        wrongTypeCounter++;
                    // LOGGER.log(Level.SEVERE, "Emergency example number test failed for " + regionCode);
                    }
                }
            }
            Assert.Equal(0, wrongTypeCounter);
        }

        [Fact]
        public void TestGlobalNetworkNumbers()
        {
            foreach(var callingCode in phoneNumberUtil.GetSupportedGlobalNetworkCallingCodes())
            {
                PhoneNumber exampleNumber =
                    phoneNumberUtil.GetExampleNumberForNonGeoEntity(callingCode);
                Assert.NotNull(exampleNumber);
                if (!phoneNumberUtil.IsValidNumber(exampleNumber))
                {
                    invalidCases.Add(exampleNumber);
                    // LOGGER.log(Level.SEVERE, "Failed validation for " + exampleNumber.toString());
                }
            }
        }

        [Fact]
        public void TestEveryRegionHasAnExampleNumber()
        {
            foreach (var regionCode in phoneNumberUtil.GetSupportedRegions())
            {
                PhoneNumber exampleNumber = phoneNumberUtil.GetExampleNumber(regionCode);
                Assert.NotNull(exampleNumber);
            }
        }
    }
}
