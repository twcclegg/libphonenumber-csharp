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

using Xunit;

namespace PhoneNumbers.Test
{
    public class TestPhonenumber
    {
        [Fact]
        public void TestEqualSimpleNumber()
        {
            var numberA = new PhoneNumber {CountryCode = 1, NationalNumber = 6502530000L};
            var numberB = new PhoneNumber {CountryCode = 1, NationalNumber = 6502530000L};
            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestEqualWithCountryCodeSourceSet()
        {
            var numberA = new PhoneNumber
            {
                RawInput = "+1 650 253 00 00",
                CountryCodeSource = PhoneNumber.Types.CountryCodeSource.FromNumberWithPlusSign
            };
            var numberB = new PhoneNumber
            {
                RawInput = "+1 650 253 00 00",
                CountryCodeSource = PhoneNumber.Types.CountryCodeSource.FromNumberWithPlusSign
            };
            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestNonEqualWithItalianLeadingZeroSetToTrue()
        {
            var numberA = new PhoneNumber {CountryCode = 1, NationalNumber = 6502530000L, ItalianLeadingZero = true};
            var numberB = new PhoneNumber {CountryCode = 1, NationalNumber = 6502530000L};
            Assert.False(numberA.Equals(numberB));
            Assert.False(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Fact]
        public void TestNonEqualWithDifferingRawInput()
        {
            var numberA = new PhoneNumber
            {
                CountryCode = 1,
                NationalNumber = 6502530000L,
                RawInput = "+1 650 253 00 00",
                CountryCodeSource = PhoneNumber.Types.CountryCodeSource.FromNumberWithPlusSign
            };
            // Although these numbers would pass an isNumberMatch test, they are not considered "equal" as
            // objects, since their raw input is different.
            var numberB = new PhoneNumber
            {
                CountryCode = 1,
                NationalNumber = 6502530000L,
                RawInput = "+1-650-253-00-00",
                CountryCodeSource = PhoneNumber.Types.CountryCodeSource.FromNumberWithPlusSign
            };
            Assert.False(numberA.Equals(numberB));
            Assert.False(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Fact]
        public void TestNonEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            var numberA = new PhoneNumber
            {
                CountryCode = 1,
                NationalNumber = 6502530000L,
                PreferredDomesticCarrierCode = ""
            };
            var numberB = new PhoneNumber {CountryCode = 1, NationalNumber = 6502530000L};
            Assert.False(numberA.Equals(numberB));
            Assert.False(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Fact]
        public void TestEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            var numberA = new PhoneNumber
            {
                CountryCode = 1,
                NationalNumber = 6502530000L,
                PreferredDomesticCarrierCode = ""
            };
            var numberB = new PhoneNumber
            {
                CountryCode = 1,
                NationalNumber = 6502530000L,
                PreferredDomesticCarrierCode = ""
            };
            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }
    }
}
