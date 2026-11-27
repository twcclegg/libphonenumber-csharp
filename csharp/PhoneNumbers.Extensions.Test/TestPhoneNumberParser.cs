using Xunit;

// Deliberately not nested under PhoneNumbers.Extensions: inside that namespace the simple name
// PhoneNumber binds to the obsolete PhoneNumbers.Extensions.PhoneNumber class, which is exactly
// the trap the new API exists to avoid. Consumer code lives outside it, and so do these tests.
namespace PhoneNumbers.Test.Extensions
{
    public class TestPhoneNumberParser
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        [Fact]
        public void TryParse_International_NoRegion()
        {
            Assert.True(PhoneNumber.TryParse("+44 20 7031 3000", out PhoneNumber? number));
            Assert.Equal("+442070313000", Util.Format(number, PhoneNumberFormat.E164));
        }

        [Fact]
        public void TryParse_National_WithRegion()
        {
            Assert.True(PhoneNumber.TryParse("020 7031 3000", "GB", out PhoneNumber? number));
            Assert.Equal(44, number.CountryCode);
            Assert.Equal(2070313000UL, number.NationalNumber);
        }

        [Fact]
        public void TryParse_National_WithoutRegion_Fails()
        {
            Assert.False(PhoneNumber.TryParse("020 7031 3000", out PhoneNumber? number));
            Assert.Null(number);
        }

        [Fact]
        public void TryParse_ExplicitTypeOutVariable_Compiles()
        {
            // The obsolete static class only allowed "out var" (CS0723 otherwise). The new member
            // takes a declared PhoneNumber? and needs no '!' afterwards.
            PhoneNumber? number;
            var parsed = PhoneNumber.TryParse("+16502530000", "US", out number);

            Assert.True(parsed);
            Assert.Equal(1, number!.CountryCode);
        }

        [Fact]
        public void TryParse_NotNullWhenTrue_NoSuppressionNeeded()
        {
            if (PhoneNumber.TryParse("+16502530000", out PhoneNumber? number))
            {
                // No '!' here: [NotNullWhen(true)] tells the compiler number is not null. With
                // warnings as errors this line would not compile otherwise.
                Assert.Equal(6502530000UL, number.NationalNumber);
            }
            else
            {
                Assert.Fail("expected a parse");
            }
        }

        [Fact]
        public void TryParse_ProducesTheSameInstanceAsParse()
        {
            // One canonical parse: equal to what PhoneNumberUtil.Parse returns, RawInput included,
            // so a number from TryParse compares equal to one from JSON, configuration or binding.
            Assert.True(PhoneNumber.TryParse("+44 20 7031 3000 ext. 123", out PhoneNumber? number));

            Assert.Equal(Util.Parse("+44 20 7031 3000 ext. 123", null), number);
        }

        [Fact]
        public void TryParse_FallbackSpelling_IsTheSameMember()
        {
            Assert.True(PhoneNumberParser.TryParse("0412 345 678", "AU", out PhoneNumber? viaClass));
            Assert.True(PhoneNumber.TryParse("0412 345 678", "AU", out PhoneNumber? viaType));

            Assert.Equal(viaClass, viaType);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("", "US")]
        [InlineData("junk", "US")]
        [InlineData("+", null)]
        [InlineData("1", "US")]
        [InlineData("1235557704", "ZX")]
        [InlineData("6502530000", null)]
        [InlineData("6502530000", "ZZ")]
        [InlineData("+3809", null)]
        [InlineData("tel:+1-650-253-0000;phone-context=", null)]
        public void TryParse_Unparseable_ReturnsFalseAndNull(string? input, string? region)
        {
            Assert.False(PhoneNumber.TryParse(input, region, out PhoneNumber? number));
            Assert.Null(number);
        }

        [Fact]
        public void TryParse_TooLong_ReturnsFalse()
        {
            Assert.False(PhoneNumber.TryParse(new string('1', 251), "US", out PhoneNumber? number));
            Assert.Null(number);
        }

        [Fact]
        public void TryParseValid_Valid()
        {
            Assert.True(PhoneNumber.TryParseValid("6502530000", "US", out PhoneNumber? number));
            Assert.Equal("+16502530000", Util.Format(number, PhoneNumberFormat.E164));

            Assert.True(PhoneNumber.TryParseValid("+16502530000", out number));
            Assert.Equal("+16502530000", Util.Format(number, PhoneNumberFormat.E164));
        }

        [Fact]
        public void TryParseValid_ParsesButInvalid_ReturnsFalseAndNull()
        {
            // "123 555 7704" parses for US but is not a valid number. Unlike the obsolete
            // PhoneNumbers.Extensions.PhoneNumber.TryParseValid, the out value is null on false.
            Assert.True(PhoneNumber.TryParse("1235557704", "US", out _));

            Assert.False(PhoneNumber.TryParseValid("1235557704", "US", out PhoneNumber? number));
            Assert.Null(number);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("+3809")]
        [InlineData("+380123456789012345678")]
        public void TryParseValid_Invalid_ReturnsFalse(string? input)
        {
            Assert.False(PhoneNumber.TryParseValid(input, null, out PhoneNumber? number));
            Assert.Null(number);
        }
    }
}
