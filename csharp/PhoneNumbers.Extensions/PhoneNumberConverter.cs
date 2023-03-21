using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoneNumbers.Extensions
{
    public class PhoneNumberConverter : JsonConverter<PhoneNumbers.PhoneNumber>
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        public override PhoneNumbers.PhoneNumber Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
            => Util.ParseAndKeepRawInput(reader.GetString(), null);

        public override void Write(Utf8JsonWriter writer, PhoneNumbers.PhoneNumber value, JsonSerializerOptions options)
            => writer.WriteStringValue(Util.Format(value, PhoneNumberFormat.E164));
    }
}
