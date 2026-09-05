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
            var numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            var numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestEqualWithCountryCodeSourceSet()
        {
            var numberA = new PhoneNumber.Builder()
                .SetRawInput("+1 650 253 00 00").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).BuildPartial();
            var numberB = new PhoneNumber.Builder()
                .SetRawInput("+1 650 253 00 00").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).BuildPartial();
            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestNonEqualWithItalianLeadingZeroSetToTrue()
        {
            var numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetNumberOfLeadingZeros(1).Build();

            var numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.False(numberA.Equals(numberB));
            Assert.False(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Fact]
        public void TestItalianLeadingZeroSetterWritesNumberOfLeadingZeros()
        {
            // Exercises the obsolete ItalianLeadingZero property setter itself, not
            // SetNumberOfLeadingZeros directly, since that setter is what delegates to
            // SetNumberOfLeadingZeros(value ? 1 : 0) under the hood.
#pragma warning disable CS0618 // intentionally exercising the obsolete setter
            var builder = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L);
            builder.ItalianLeadingZero = true;
            Assert.Equal(1, builder.NumberOfLeadingZeros);

            builder.ItalianLeadingZero = false;
            Assert.Equal(0, builder.NumberOfLeadingZeros);
#pragma warning restore CS0618
        }

        [Fact]
        public void TestNonEqualWithDifferingRawInput()
        {
            var numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetRawInput("+1 650 253 00 00")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();

            var numberB = new PhoneNumber.Builder()
            // Although these numbers would pass an isNumberMatch test, they are not considered "equal" as
            // objects, since their raw input is different.
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetRawInput("+1-650-253-00-00").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();

            Assert.False(numberA.Equals(numberB));
            Assert.False(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Fact]
        public void TestNonEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            var numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetPreferredDomesticCarrierCode("").Build();

            var numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.False(numberA.Equals(numberB));
            Assert.False(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Fact]
        public void TestEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            var numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetPreferredDomesticCarrierCode("").Build();

            var numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetPreferredDomesticCarrierCode("").Build();

            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }
    }
}
