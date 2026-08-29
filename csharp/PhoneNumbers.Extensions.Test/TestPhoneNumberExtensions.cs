using Xunit;

namespace PhoneNumbers.Extensions.Test
{
    public class TestPhoneNumberExtensions
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        [Fact]
        public void ToE164_FormatsAsE164()
        {
            var number = Util.Parse("6194002404", "US");

            Assert.Equal("+16194002404", number.ToE164());
        }

        [Fact]
        public void ToNationalFormat_FormatsNationally()
        {
            var number = Util.Parse("+16194002404", null);

            Assert.Equal("(619) 400-2404", number.ToNationalFormat());
        }

        [Fact]
        public void ToInternationalFormat_FormatsInternationally()
        {
            var number = Util.Parse("+16194002404", null);

            Assert.Equal("+1 619-400-2404", number.ToInternationalFormat());
        }

        [Fact]
        public void IsValid_ValidNumber_ReturnsTrue()
        {
            var number = Util.Parse("+16194002404", null);

            Assert.True(number.IsValid());
        }

        [Fact]
        public void IsValid_InvalidNumber_ReturnsFalse()
        {
            var number = Util.Parse("1235557704", "US");

            Assert.False(number.IsValid());
        }
    }
}
