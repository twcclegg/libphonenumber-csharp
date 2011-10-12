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
using NUnit.Framework;

namespace PhoneNumbers.Test
{
    [TestFixture]
    class TestExampleNumbers
    {
        private PhoneNumberUtil phoneNumberUtil;
        private List<PhoneNumber> invalidCases = new List<PhoneNumber>();
        private List<PhoneNumber> wrongTypeCases = new List<PhoneNumber>();

        [TestFixtureSetUp]
        public void SetupFixture()
        {
            PhoneNumberUtil.ResetInstance();
            phoneNumberUtil = PhoneNumberUtil.GetInstance();
        }

        [SetUp]
        protected void SetUp()
        {
            invalidCases.Clear();
            wrongTypeCases.Clear();
        }

        /**
        * @param exampleNumberRequestedType  type we are requesting an example number for
        * @param possibleExpectedTypes       acceptable types that this number should match, such as
        *     FIXED_LINE and FIXED_LINE_OR_MOBILE for a fixed line example number.
        */
        private void checkNumbersValidAndCorrectType(PhoneNumberType exampleNumberRequestedType,
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
            return new HashSet<PhoneNumberType>(new PhoneNumberType[] { t1, t2 });
        }

        private HashSet<PhoneNumberType> MakeSet(PhoneNumberType t1)
        {
            return MakeSet(t1, t1);
        }

        [Test]
        public void TestFixedLine()
        {
            HashSet<PhoneNumberType> fixedLineTypes = MakeSet(PhoneNumberType.FIXED_LINE,
                                            PhoneNumberType.FIXED_LINE_OR_MOBILE);
            checkNumbersValidAndCorrectType(PhoneNumberType.FIXED_LINE, fixedLineTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [Test]
        public void TestMobile()
        {
            HashSet<PhoneNumberType> mobileTypes = MakeSet(PhoneNumberType.MOBILE,
                                                          PhoneNumberType.FIXED_LINE_OR_MOBILE);
            checkNumbersValidAndCorrectType(PhoneNumberType.MOBILE, mobileTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [Test]
        public void TestTollFree()
        {

            HashSet<PhoneNumberType> tollFreeTypes = MakeSet(PhoneNumberType.TOLL_FREE);
            checkNumbersValidAndCorrectType(PhoneNumberType.TOLL_FREE, tollFreeTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [Test]
        public void TestPremiumRate()
        {
            HashSet<PhoneNumberType> premiumRateTypes = MakeSet(PhoneNumberType.PREMIUM_RATE);
            checkNumbersValidAndCorrectType(PhoneNumberType.PREMIUM_RATE, premiumRateTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [Test]
        public void TestVoip()
        {
            HashSet<PhoneNumberType> voipTypes = MakeSet(PhoneNumberType.VOIP);
            checkNumbersValidAndCorrectType(PhoneNumberType.VOIP, voipTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [Test]
        public void TestPager()
        {
            HashSet<PhoneNumberType> pagerTypes = MakeSet(PhoneNumberType.PAGER);
            checkNumbersValidAndCorrectType(PhoneNumberType.PAGER, pagerTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [Test]
        public void TestUan()
        {
            HashSet<PhoneNumberType> uanTypes = MakeSet(PhoneNumberType.UAN);
            checkNumbersValidAndCorrectType(PhoneNumberType.UAN, uanTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [Test]
        public void TestSharedCost()
        {
            HashSet<PhoneNumberType> sharedCostTypes = MakeSet(PhoneNumberType.SHARED_COST);
            checkNumbersValidAndCorrectType(PhoneNumberType.SHARED_COST, sharedCostTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [Test]
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
                }
            }
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [Test]
        public void TestEveryRegionHasAnExampleNumber()
        {
            foreach(var regionCode in phoneNumberUtil.GetSupportedRegions())
            {
                PhoneNumber exampleNumber = phoneNumberUtil.GetExampleNumber(regionCode);
                Assert.IsNotNull(exampleNumber, "None found for region " + regionCode);
            }
        }
    }
}
