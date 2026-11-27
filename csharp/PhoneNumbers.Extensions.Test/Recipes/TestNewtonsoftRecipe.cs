using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace PhoneNumbers.Test.Extensions.Recipes
{
    public class TestNewtonsoftRecipe
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        private static readonly JsonSerializerSettings Settings = new()
        {
            Converters = { new NewtonsoftPhoneNumberConverter() },
        };

        [Fact]
        public void Recipe_RoundTripsADto()
        {
            var dto = new Contact { Name = "Ada", Phone = Util.Parse("020 7031 3000", "GB") };

            var json = JsonConvert.SerializeObject(dto, Settings);
            var back = JsonConvert.DeserializeObject<Contact>(json, Settings)!;

            // Newtonsoft does not escape '+', unlike System.Text.Json's default encoder.
            Assert.Equal("{\"Name\":\"Ada\",\"Phone\":\"+442070313000\",\"Mobile\":null}", json);
            Assert.Equal(dto.Phone, back.Phone);
            Assert.Null(back.Mobile);
        }

        [Fact]
        public void Recipe_RejectsInvalidInput()
        {
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<Contact>("{\"Phone\":\"junk\"}", Settings));
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<Contact>("{\"Phone\":44}", Settings));
        }

        [Fact]
        public void WithoutTheRecipe_TheBuiltInTypeConverterAlreadyGivesE164()
        {
            // What the guide says before offering the recipe: the core TypeConverter makes
            // Newtonsoft treat PhoneNumber as a string, dictionary keys included.
            var dto = new Contact { Name = "Ada", Phone = Util.Parse("+442070313000", null) };
            var byNumber = new Dictionary<PhoneNumber, string> { [dto.Phone] = "office" };

            var json = JsonConvert.SerializeObject(dto);
            var back = JsonConvert.DeserializeObject<Contact>(json)!;
            var dictJson = JsonConvert.SerializeObject(byNumber);

            Assert.Contains("\"Phone\":\"+442070313000\"", json, System.StringComparison.Ordinal);
            Assert.Equal(dto.Phone, back.Phone);
            Assert.Equal("{\"+442070313000\":\"office\"}", dictJson);
        }

        private sealed class Contact
        {
            public string? Name { get; set; }

            public PhoneNumber Phone { get; set; } = null!;

            public PhoneNumber? Mobile { get; set; }
        }
    }
}
