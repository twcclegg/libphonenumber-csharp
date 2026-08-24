using Xunit;

namespace PhoneNumbers.Extensions.Test
{
    public class TestPhoneNumberTypeConverter
    {
        private static readonly PhoneNumberTypeConverter Converter = new();

        [Fact]
        public void ConvertFrom_ParsesString()
        {
            var result = Converter.ConvertFrom("+16192987704");

            var number = Assert.IsType<PhoneNumbers.PhoneNumber>(result);
            Assert.Equal(6192987704UL, number.NationalNumber);
        }

        [Fact]
        public void ConvertFrom_MatchesTryParse_ForEqualityAndHashing()
        {
            // Both entry points must produce equal/interchangeable PhoneNumber instances for the same
            // input; RawInput divergence would otherwise break Equals()/GetHashCode() consistency.
            var converted = Assert.IsType<PhoneNumbers.PhoneNumber>(Converter.ConvertFrom("+16192987704"));
            Assert.True(PhoneNumber.TryParse("+16192987704", out var parsed));

            Assert.Equal(parsed, converted);
            Assert.Equal(parsed!.GetHashCode(), converted.GetHashCode());
        }

        [Fact]
        public void ConvertTo_FormatsE164()
        {
            var util = PhoneNumberUtil.GetInstance();
            var number = util.Parse("+16192987704", null);

            var result = Converter.ConvertTo(number, typeof(string));

            Assert.Equal("+16192987704", result);
        }

        [Fact]
        public void CanConvertFrom_String_ReturnsTrue()
        {
            Assert.True(Converter.CanConvertFrom(typeof(string)));
        }

        [Fact]
        public void CanConvertTo_String_ReturnsTrue()
        {
            Assert.True(Converter.CanConvertTo(typeof(string)));
        }
    }
}
