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
    class TestPhonenumber
    {
        [Test]
        public void TestEqualSimpleNumber()
        {
            PhoneNumber numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            PhoneNumber numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.AreEqual(numberA, numberB);
            Assert.AreEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Test]
        public void TestEqualWithItalianLeadingZeroSetToDefault()
        {
            PhoneNumber numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetItalianLeadingZero(false).Build();

            PhoneNumber numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            // These should still be equal, since the default value for this field is false.
            Assert.AreEqual(numberA, numberB);
            Assert.AreEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Test]
        public void TestEqualWithCountryCodeSourceSet()
        {
            PhoneNumber numberA = new PhoneNumber.Builder()
                .SetRawInput("+1 650 253 00 00").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).BuildPartial();
            PhoneNumber numberB = new PhoneNumber.Builder()
                .SetRawInput("+1 650 253 00 00").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).BuildPartial();
            Assert.AreEqual(numberA, numberB);
            Assert.AreEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Test]
        public void TestNonEqualWithItalianLeadingZeroSetToTrue()
        {
            PhoneNumber numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetItalianLeadingZero(true).Build();

            PhoneNumber numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.IsFalse(numberA.Equals(numberB));
            Assert.IsFalse(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Test]
        public void TestNonEqualWithDifferingRawInput()
        {
            PhoneNumber numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetRawInput("+1 650 253 00 00")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();

            PhoneNumber numberB = new PhoneNumber.Builder()
            // Although these numbers would pass an isNumberMatch test, they are not considered "equal" as
            // objects, since their raw input is different.
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetRawInput("+1-650-253-00-00").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();

            Assert.IsFalse(numberA.Equals(numberB));
            Assert.IsFalse(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Test]
        public void TestNonEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            PhoneNumber numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetPreferredDomesticCarrierCode("").Build();

            PhoneNumber numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.IsFalse(numberA.Equals(numberB));
            Assert.IsFalse(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Test]
        public void TestEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            PhoneNumber numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetPreferredDomesticCarrierCode("").Build();

            PhoneNumber numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetPreferredDomesticCarrierCode("").Build();

            Assert.AreEqual(numberA, numberB);
            Assert.AreEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }
    }
}
