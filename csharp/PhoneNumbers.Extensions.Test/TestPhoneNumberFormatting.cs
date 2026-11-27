using System;
using Xunit;

namespace PhoneNumbers.Test.Extensions
{
    public class TestPhoneNumberFormatting
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        [Theory]
        [InlineData(PhoneNumberFormat.E164, "+442070313000")]
        [InlineData(PhoneNumberFormat.INTERNATIONAL, "+44 20 7031 3000")]
        [InlineData(PhoneNumberFormat.NATIONAL, "020 7031 3000")]
        [InlineData(PhoneNumberFormat.RFC3966, "tel:+44-20-7031-3000")]
        public void Format_MatchesPhoneNumberUtil(PhoneNumberFormat format, string expected)
        {
            var number = Util.Parse("+442070313000", null);

            Assert.Equal(expected, number.Format(format));
            Assert.Equal(Util.Format(number, format), number.Format(format));
        }

        [Fact]
        public void IsValidNumber_MatchesPhoneNumberUtil()
        {
            Assert.True(Util.Parse("+16502530000", null).IsValidNumber());
            Assert.False(Util.Parse("1235557704", "US").IsValidNumber());
        }

        [Fact]
        public void GetNumberType_MatchesPhoneNumberUtil()
        {
            Assert.Equal(PhoneNumberType.MOBILE, Util.Parse("+447912345678", null).GetNumberType());
            Assert.Equal(PhoneNumberType.FIXED_LINE, Util.Parse("+442070313000", null).GetNumberType());
            Assert.Equal(PhoneNumberType.UNKNOWN, Util.Parse("1235557704", "US").GetNumberType());
        }

        [Fact]
        public void GetRegionCodeForNumber_MatchesPhoneNumberUtil()
        {
            Assert.Equal("GB", Util.Parse("+442070313000", null).GetRegionCodeForNumber());
            Assert.Equal("CA", Util.Parse("+16135550123", null).GetRegionCodeForNumber());
            Assert.Equal("001", Util.Parse("+80012345678", null).GetRegionCodeForNumber());
        }

        [Fact]
        public void NullReceiver_ThrowsArgumentNullException()
        {
            PhoneNumber number = null!;

            Assert.Equal("number", Assert.Throws<ArgumentNullException>(() => number.Format(PhoneNumberFormat.E164)).ParamName);
            Assert.Throws<ArgumentNullException>(() => number.IsValidNumber());
            Assert.Throws<ArgumentNullException>(() => number.GetNumberType());
            Assert.Throws<ArgumentNullException>(() => number.GetRegionCodeForNumber());
        }

        [Fact]
        public void TryParseThenFormat_ReadsAsOneStatement()
        {
            // The headline shape from the Extensions guide, kept compiling.
            if (PhoneNumber.TryParse("020 7031 3000", "GB", out PhoneNumber? n) && n.IsValidNumber())
            {
                Assert.Equal("+442070313000", n.Format(PhoneNumberFormat.E164));
                Assert.Equal(PhoneNumberType.FIXED_LINE, n.GetNumberType());
                Assert.Equal("GB", n.GetRegionCodeForNumber());
            }
            else
            {
                Assert.Fail("expected a valid number");
            }
        }
    }
}
