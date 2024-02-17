using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace PhoneNumbers.Extensions.Test
{
    public class TestConversion
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        [Theory]
        [InlineData("+16194002404", "+16194002404", null)]
        [InlineData("+16194002404", "6194002404", "US")]
        [InlineData("+448443351801", "+448443351801", null)]
        [InlineData("+448443351801", "0844 335 1801", "GB")]
        [InlineData("+448443351801", "844 335 1801", "GB")]
        [InlineData("+380445004973", "+380445004973", null)]
        [InlineData("+380445004973", "0445004973", "UA")]
        [InlineData("+50022215", "+50022215", null)]
        [InlineData("+50022215", "22215", "FK")]
#if NET6_0_OR_GREATER
        public void TestSerialization(string expected, string input, string? region)
#else
        public void TestSerialization(string expected, string input, string region)
#endif
        {
            var number = Util.Parse(input, region);
            var json = JsonSerializer.Serialize(new TestPhoneNumber(number));
#if NET6_0_OR_GREATER
            var str = JsonSerializer.Deserialize<TestString>(json)!.PhoneNumber;
#else
            var str = JsonSerializer.Deserialize<TestString>(json).PhoneNumber;
#endif
            Assert.Equal(expected, str);
        }

        [Theory]
        [InlineData("+16194002404")]
        [InlineData("+448443351801")]
        [InlineData("+380445004973")]
        [InlineData("+50022215")]
        public void TestDeserialization(string value)
        {
            var json = $@"{{""PhoneNumber"": ""{value}""}}";
#if NET6_0_OR_GREATER
            var number = JsonSerializer.Deserialize<TestPhoneNumber>(json)!.PhoneNumber;
#else
            var number = JsonSerializer.Deserialize<TestPhoneNumber>(json).PhoneNumber;
#endif
            Assert.Equal(value, Util.Format(number, PhoneNumberFormat.E164));
        }
    }

#if NET6_0_OR_GREATER
    public record TestPhoneNumber(
        [property: JsonConverter(typeof(PhoneNumberConverter))]
        PhoneNumbers.PhoneNumber PhoneNumber);

    public record TestString(string PhoneNumber);
#else
    public class TestPhoneNumber
    {
        public TestPhoneNumber(PhoneNumbers.PhoneNumber phoneNumber)
        {
            PhoneNumber = phoneNumber;
        }

        [JsonConverter(typeof(PhoneNumberConverter))]
        public PhoneNumbers.PhoneNumber PhoneNumber { get; set; }
    }

    public class TestString
    {
        public TestString(string phoneNumber)
        {
            PhoneNumber = phoneNumber;
        }

        public string PhoneNumber { get; set; }
    }
#endif
}
