using System.Text;
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
        public void TestSerialization(string expected, string input, string region)
        {
            var number = Util.Parse(input, region);
            var json = JsonSerializer.Serialize(new TestPhoneNumber(number));
            var str = JsonSerializer.Deserialize<TestString>(json)!.PhoneNumber;
            Assert.Equal(expected, str);
        }

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
        public void TestSerialization_OldVersion(string expected, string input, string region)
        {
            var number = Util.Parse(input, region);
            var json = JsonSerializer.Serialize(new TestPhoneNumber_Old(number));
            var str = JsonSerializer.Deserialize<TestString>(json)!.PhoneNumber;
            Assert.Equal(expected, str);
        }

        [Theory]
        [InlineData("+16194002404")]
        [InlineData("+448443351801")]
        [InlineData("+380445004973")]
        [InlineData("+50022215")]
        public void TestDeserialization(string value)
        {
            var json = $$"""{"PhoneNumber": "{{value}}"}""";
            var number = JsonSerializer.Deserialize<TestPhoneNumber>(json)!.PhoneNumber;
            Assert.Equal(value, Util.Format(number, PhoneNumberFormat.E164));
        }

        [Theory]
        [InlineData("+16194002404")]
        [InlineData("+448443351801")]
        [InlineData("+380445004973")]
        [InlineData("+50022215")]
        public void TestDeserialization_OldVersion(string value)
        {
            var sb = new StringBuilder(20);
            var json = $$"""{"PhoneNumber": "{{value}}"}""";
            var number = JsonSerializer.Deserialize<TestPhoneNumber>(json)!.PhoneNumber;
            Util.Format(number, PhoneNumberFormat.E164, sb);
            Assert.Equal(value, sb.ToString());
        }
    }

    public record TestPhoneNumber(
        [property: JsonConverter(typeof(PhoneNumberConverter))] PhoneNumbers.PhoneNumber PhoneNumber);
    public record TestPhoneNumber_Old(
        [property: JsonConverter(typeof(PhoneNumberConverter_Old))] PhoneNumbers.PhoneNumber PhoneNumber);
    public record TestString(string PhoneNumber);
}
