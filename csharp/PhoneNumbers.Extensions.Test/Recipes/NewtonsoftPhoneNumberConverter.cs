using System;
using Newtonsoft.Json;

namespace PhoneNumbers.Test.Extensions.Recipes
{
    // The Newtonsoft.Json recipe from docs/extensions.md, verbatim apart from this comment, the
    // namespace and "internal" (the docs say public). Keep the two in step: this copy is what
    // proves the documented one compiles and works.
    internal sealed class NewtonsoftPhoneNumberConverter : JsonConverter<PhoneNumber>
    {
        public override PhoneNumber? ReadJson(JsonReader reader, Type objectType, PhoneNumber? existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            if (reader.TokenType == JsonToken.String
                && PhoneNumberParser.TryParse((string?)reader.Value, out PhoneNumber? number))
                return number;
            throw new JsonSerializationException("Expected a phone number in international format, such as \"+442070313000\".");
        }

        public override void WriteJson(JsonWriter writer, PhoneNumber? value, JsonSerializer serializer)
        {
            if (value is null)
                writer.WriteNull();
            else
                writer.WriteValue(value.Format(PhoneNumberFormat.E164));
        }
    }
}
