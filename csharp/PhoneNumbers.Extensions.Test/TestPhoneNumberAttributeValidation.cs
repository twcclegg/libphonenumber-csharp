using Xunit;

namespace PhoneNumbers.Extensions.Test
{
    public class TestPhoneNumberAttributeValidation
    {
        [Fact]
        public void IsValid_Null_ReturnsTrue()
        {
            var attribute = new PhoneNumberAttribute();

            Assert.True(attribute.IsValid(null));
        }

        [Fact]
        public void IsValid_ValidE164String_ReturnsTrue()
        {
            var attribute = new PhoneNumberAttribute();

            Assert.True(attribute.IsValid("+16192987704"));
        }

        [Fact]
        public void IsValid_ValidNationalStringWithRegion_ReturnsTrue()
        {
            var attribute = new PhoneNumberAttribute { Region = "US" };

            Assert.True(attribute.IsValid("6192987704"));
        }

        [Fact]
        public void IsValid_NationalStringWithoutRegion_ReturnsFalse()
        {
            var attribute = new PhoneNumberAttribute();

            Assert.False(attribute.IsValid("6192987704"));
        }

        [Fact]
        public void IsValid_InvalidString_ReturnsFalse()
        {
            var attribute = new PhoneNumberAttribute { Region = "US" };

            Assert.False(attribute.IsValid("1235557704"));
        }

        [Fact]
        public void IsValid_ValidParsedPhoneNumber_ReturnsTrue()
        {
            var util = PhoneNumberUtil.GetInstance();
            var number = util.Parse("+16192987704", null);
            var attribute = new PhoneNumberAttribute();

            Assert.True(attribute.IsValid(number));
        }

        [Fact]
        public void IsValid_UnsupportedType_ReturnsFalse()
        {
            var attribute = new PhoneNumberAttribute();

            Assert.False(attribute.IsValid(42));
        }
    }
}
