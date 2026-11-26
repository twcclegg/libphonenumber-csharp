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
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetItalianLeadingZero(true).Build();

            var numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.False(numberA.Equals(numberB));
            Assert.False(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [Fact]
        public void TestEqualWithItalianLeadingZeroSetToDefault()
        {
            var numberA = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).SetItalianLeadingZero(false).Build();

            var numberB = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            // These should still be equal, since the default value for this field is false.
            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestLeadingZeroFieldsHaveJavaDefaults()
        {
            // Java's PhoneNumber: italian_leading_zero defaults to false, number_of_leading_zeros to
            // 1, and each has its own has-bit.
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();
            Assert.False(number.HasItalianLeadingZero);
            Assert.False(number.ItalianLeadingZero);
            Assert.False(number.HasNumberOfLeadingZeros);
            Assert.Equal(1, number.NumberOfLeadingZeros);
        }

        [Fact]
        public void TestItalianLeadingZeroAndNumberOfLeadingZerosAreSeparateFields()
        {
            var builder = new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(236618300L);
            builder.ItalianLeadingZero = true;
            Assert.True(builder.HasItalianLeadingZero);
            Assert.False(builder.HasNumberOfLeadingZeros);
            Assert.Equal(1, builder.NumberOfLeadingZeros);

            builder.NumberOfLeadingZeros = 2;
            Assert.True(builder.ItalianLeadingZero);
            Assert.True(builder.HasNumberOfLeadingZeros);

            builder.ClearNumberOfLeadingZeros();
            Assert.False(builder.HasNumberOfLeadingZeros);
            Assert.Equal(1, builder.NumberOfLeadingZeros);
            Assert.True(builder.ItalianLeadingZero);

            builder.ClearItalianLeadingZero();
            Assert.False(builder.HasItalianLeadingZero);
            Assert.False(builder.ItalianLeadingZero);
        }

        [Fact]
        public void TestMergeFromCopiesBothLeadingZeroFields()
        {
            var source = new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(1L)
                .SetItalianLeadingZero(true).SetNumberOfLeadingZeros(3).Build();
            var copy = new PhoneNumber.Builder().MergeFrom(source).Build();
            Assert.True(copy.HasItalianLeadingZero);
            Assert.True(copy.ItalianLeadingZero);
            Assert.True(copy.HasNumberOfLeadingZeros);
            Assert.Equal(3, copy.NumberOfLeadingZeros);
            Assert.Equal(source, copy);
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
