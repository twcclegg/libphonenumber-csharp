using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhoneNumbers.Text.Json;
using Xunit;

namespace PhoneNumbers.Test.Extensions
{
    public class TestPhoneNumberJsonConverter
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions().AddPhoneNumberConverter();

        [Fact]
        public void Dto_RoundTrips()
        {
            var contact = new Contact("Ada", Util.Parse("020 7031 3000", "GB"), null);

            var json = JsonSerializer.Serialize(contact, Options);
            var back = JsonSerializer.Deserialize<Contact>(json, Options)!;

            using var doc = JsonDocument.Parse(json);
            Assert.Equal("+442070313000", doc.RootElement.GetProperty("Phone").GetString());
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Mobile").ValueKind);
            Assert.Equal(contact.Phone, back.Phone);
            Assert.Null(back.Mobile);
        }

        [Fact]
        public void CollectionsAndDictionaryKeys_RoundTrip()
        {
            var numbers = new List<PhoneNumber> { Util.Parse("+442070313000", null), Util.Parse("+16502530000", null) };
            var byNumber = new Dictionary<PhoneNumber, string> { [numbers[0]] = "office", [numbers[1]] = "hq" };

            var listBack = JsonSerializer.Deserialize<List<PhoneNumber>>(JsonSerializer.Serialize(numbers, Options), Options)!;
            var dictJson = JsonSerializer.Serialize(byNumber, Options);
            var dictBack = JsonSerializer.Deserialize<Dictionary<PhoneNumber, string>>(dictJson, Options)!;

            Assert.Equal(numbers, listBack);
            Assert.Contains("\"\\u002B442070313000\":\"office\"", dictJson, StringComparison.Ordinal);
            Assert.Equal("hq", dictBack[numbers[1]]);
        }

        [Fact]
        public void Read_ProducesTheSameInstanceAsEveryOtherEntryPoint()
        {
            // One canonical parse. The obsolete PhoneNumberConverter used ParseAndKeepRawInput, so
            // its numbers never equalled ones from Parse, TryParse or binding (Equals compares
            // RawInput and CountryCodeSource).
            const string Text = "+44 20 7031 3000";
            var fromJson = JsonSerializer.Deserialize<PhoneNumber>("\"" + Text + "\"", Options);
            var fromConverter = (PhoneNumber?)TypeDescriptor.GetConverter(typeof(PhoneNumber)).ConvertFromInvariantString(Text);
            Assert.True(PhoneNumber.TryParse(Text, out PhoneNumber? fromTryParse));

            Assert.Equal(Util.Parse(Text, null), fromJson);
            Assert.Equal(fromTryParse, fromJson);
            Assert.Equal(fromConverter, fromJson);
        }

        [Theory]
        [InlineData("\"020 7031 3000\"")]
        [InlineData("\"junk\"")]
        [InlineData("\"\"")]
        [InlineData("44")]
        [InlineData("{}")]
        [InlineData("true")]
        public void Read_InvalidValue_ThrowsJsonExceptionWithPath(string value)
        {
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Contact>(
                "{\"Name\":\"x\",\"Phone\":" + value + "}", Options));

            Assert.Equal("$.Phone", ex.Path);
        }

        [Fact]
        public void Read_InvalidValue_DoesNotEchoTheInput()
        {
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PhoneNumber>("\"0123 secret\"", Options));

            Assert.DoesNotContain("secret", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Write_DropsTheExtension()
        {
            var number = Util.Parse("+44 20 7031 3000 ext. 123", null);

            Assert.Equal("\"\\u002B442070313000\"", JsonSerializer.Serialize(number, Options));
        }

        [Fact]
        public void AddPhoneNumberConverter_ReturnsTheSameOptions()
        {
            var options = new JsonSerializerOptions();

            Assert.Same(options, options.AddPhoneNumberConverter());
            Assert.IsType<PhoneNumberJsonConverter>(Assert.Single(options.Converters));
        }

        [Fact]
        public void AddPhoneNumberConverter_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ((JsonSerializerOptions)null!).AddPhoneNumberConverter());
        }

        [Fact]
        public void SourceGeneratedContext_WithConverterOnTheContext_RoundTrips()
        {
            // The trimmed / Native AOT shape: no reflection, the converter registered on the
            // consumer's own context. PhoneNumbers.Extensions.AotTest runs the same thing published
            // with PublishAot.
            var contact = new Contact("Ada", Util.Parse("+442070313000", null), Util.Parse("+447912345678", null));

            var json = JsonSerializer.Serialize(contact, ContactJsonContext.Default.Contact);
            var back = JsonSerializer.Deserialize(json, ContactJsonContext.Default.Contact)!;

            Assert.Equal(contact, back);
        }

        [Fact]
        public void PropertyAttribute_RoundTrips()
        {
            var dto = new AttributedDto { Phone = Util.Parse("+16502530000", null) };

            var json = JsonSerializer.Serialize(dto);
            var back = JsonSerializer.Deserialize<AttributedDto>(json)!;

            Assert.Equal(dto.Phone, back.Phone);
        }
    }

    internal sealed record Contact(string Name, PhoneNumber Phone, PhoneNumber? Mobile);

    internal sealed class AttributedDto
    {
        [JsonConverter(typeof(PhoneNumberJsonConverter))]
        public PhoneNumber? Phone { get; set; }
    }

    [JsonSourceGenerationOptions(Converters = new[] { typeof(PhoneNumberJsonConverter) })]
    [JsonSerializable(typeof(Contact))]
    internal sealed partial class ContactJsonContext : JsonSerializerContext
    {
    }
}
