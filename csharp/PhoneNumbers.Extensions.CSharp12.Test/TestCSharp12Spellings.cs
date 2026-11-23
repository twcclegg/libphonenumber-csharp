using Xunit;

namespace PhoneNumbers.Test.CSharp12
{
    /// <summary>
    /// The pre-C# 14 spellings of the Extensions API. What matters most is that this file compiles
    /// under LangVersion 12; the assertions only confirm each call reaches the real implementation.
    /// </summary>
    public class TestCSharp12Spellings
    {
        [Fact]
        public void PhoneNumberParser_TryParse()
        {
            Assert.True(PhoneNumberParser.TryParse("+442070313000", out PhoneNumber? international));
            Assert.Equal(44, international.CountryCode);

            Assert.True(PhoneNumberParser.TryParse("020 7031 3000", "GB", out PhoneNumber? national));
            Assert.Equal(international, national);

            Assert.False(PhoneNumberParser.TryParse("junk", "GB", out PhoneNumber? none));
            Assert.Null(none);
        }

        [Fact]
        public void PhoneNumberParser_TryParseValid()
        {
            Assert.True(PhoneNumberParser.TryParseValid("+16502530000", out PhoneNumber? valid));
            Assert.Equal(1, valid.CountryCode);

            Assert.True(PhoneNumberParser.TryParseValid("6502530000", "US", out valid));
            Assert.Equal(6502530000UL, valid.NationalNumber);

            Assert.False(PhoneNumberParser.TryParseValid("1235557704", "US", out PhoneNumber? invalid));
            Assert.Null(invalid);
        }
    }
}
