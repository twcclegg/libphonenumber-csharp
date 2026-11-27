using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PhoneNumbers.ComponentModel.DataAnnotations;
using Xunit;

namespace PhoneNumbers.Test.Extensions
{
    public class TestValidPhoneNumberAttributeValidation
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        [Theory]
        [InlineData("+442070313000", null)]
        [InlineData("+44 20 7031 3000", "US")]
        [InlineData("020 7031 3000", "GB")]
        [InlineData("tel:+1-650-253-0000", null)]
        public void ValidString(string value, string? region)
        {
            Assert.True(new ValidPhoneNumberAttribute { DefaultRegion = region }.IsValid(value));
        }

        [Theory]
        [InlineData("020 7031 3000", null)]
        [InlineData("1235557704", "US")]
        [InlineData("junk", "GB")]
        [InlineData("", "GB")]
        [InlineData("+3809", null)]
        public void InvalidString(string value, string? region)
        {
            Assert.False(new ValidPhoneNumberAttribute { DefaultRegion = region }.IsValid(value));
        }

        [Fact]
        public void Null_IsValid()
        {
            Assert.True(new ValidPhoneNumberAttribute().IsValid(null));
        }

        [Fact]
        public void ParsedNumber_ValidatedAsIs()
        {
            var attribute = new ValidPhoneNumberAttribute { DefaultRegion = "GB" };

            Assert.True(attribute.IsValid(Util.Parse("+16502530000", null)));
            Assert.False(attribute.IsValid(Util.Parse("1235557704", "US")));
        }

        [Fact]
        public void OtherType_IsInvalid()
        {
            Assert.False(new ValidPhoneNumberAttribute().IsValid(4420703130));
        }

        [Fact]
        public void Validator_OnRecordProperty_ReportsTheMemberAndMessage()
        {
            var ok = new Contact("Ada", "020 7031 3000", Util.Parse("+447912345678", null));
            var bad = new Contact("Bob", "020 7031", null);

            Assert.True(Validate(ok, out _));
            Assert.False(Validate(bad, out var results));

            var result = Assert.Single(results);
            Assert.Equal(new[] { nameof(Contact.Phone) }, result.MemberNames);
            Assert.Equal("The Phone field is not a valid phone number.", result.ErrorMessage);
        }

        [Fact]
        public void CustomErrorMessage_IsUsed()
        {
            var attribute = new ValidPhoneNumberAttribute { ErrorMessage = "{0} must be a phone number" };

            Assert.Equal("Mobile must be a phone number", attribute.FormatErrorMessage("Mobile"));
        }

        private static bool Validate(object instance, out List<ValidationResult> results)
        {
            results = new List<ValidationResult>();
            return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        }

        private sealed record Contact(
            string Name,
            [property: ValidPhoneNumber(DefaultRegion = "GB")] string Phone,
            [property: ValidPhoneNumber] PhoneNumber? Mobile);
    }
}
