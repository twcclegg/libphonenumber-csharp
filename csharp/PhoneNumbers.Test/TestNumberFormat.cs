using Xunit;

namespace PhoneNumbers.Test
{
    /**
     * Tests derived from https://github.com/google/libphonenumber/blob/master/java/libphonenumber/test/com/google/i18n/phonenumbers/PhonenumberTest.java
     */
    public class TestNumberFormat
    {
        [Fact]
        public void TestEqualSimpleNumber()
        {
            var numberA = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();
            var numberB = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestEqualWithSetNumberOfLeadingZerosSetToDefault()
        {
            var numberA = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L)
                .SetNumberOfLeadingZeros(0).Build();
            var numberB = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestEqualWithCountryCodeSourceSet()
        {
            var numberA = new PhoneNumber.Builder().SetRawInput("+1 650 253 00 00")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();
            var numberB = new PhoneNumber.Builder().SetRawInput("+1 650 253 00 00")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();

            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestEqualWithSetNumberOfLeadingZerosSetTo1()
        {
            var numberA = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L)
                .SetNumberOfLeadingZeros(1).Build();
            var numberB = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.NotEqual(numberA, numberB);
            Assert.NotEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestNonEqualWithDifferingRawInput()
        {
            var numberA = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L)
                .SetRawInput("+1 650 253 00 00")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();

            var numberB = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L)
                .SetRawInput("+1-650-253-00-00")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();
            // Although these numbers would pass an isNumberMatch test, they are not considered "equal" as
            // objects, since their raw input is different.
            Assert.NotEqual(numberA, numberB);
            Assert.NotEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestNonEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            var numberA = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L)
                .SetPreferredDomesticCarrierCode("").Build();
            var numberB = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();

            Assert.NotEqual(numberA, numberB);
            Assert.NotEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [Fact]
        public void TestEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            var numberA = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L)
                .SetPreferredDomesticCarrierCode("").Build();
            var numberB = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L)
                .SetPreferredDomesticCarrierCode("").Build();

            Assert.Equal(numberA, numberB);
            Assert.Equal(numberA.GetHashCode(), numberB.GetHashCode());
        }
    }
}