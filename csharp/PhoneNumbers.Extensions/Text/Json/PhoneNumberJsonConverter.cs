using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoneNumbers.Text.Json
{
    /// <summary>
    /// Reads and writes a <see cref="PhoneNumber"/> as a JSON string in E.164 format,
    /// e.g. <c>"+442070313000"</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Register it once with <see cref="PhoneNumberJsonSerializerOptionsExtensions.AddPhoneNumberConverter"/>
    /// and every <see cref="PhoneNumber"/> in any DTO, collection or dictionary key serializes
    /// through it. For a source-generated (trimmed or Native AOT) context, list it on the context
    /// instead: <c>[JsonSourceGenerationOptions(Converters = new[] { typeof(PhoneNumberJsonConverter) })]</c>.
    /// Without a converter, <see cref="PhoneNumber"/>'s public properties would be serialized
    /// member by member, which does not terminate.
    /// </para>
    /// <para>
    /// Reading parses like <see cref="PhoneNumberUtil.Parse(string, string)"/> with no default
    /// region, so the string must carry its country calling code (E.164, a formatted international
    /// number, or an RFC 3966 <c>tel:</c> URI). The result equals what <c>Parse</c>, <c>TryParse</c>,
    /// configuration binding and ASP.NET Core model binding produce for the same text. Anything
    /// else throws <see cref="JsonException"/>, which System.Text.Json reports with the JSON path.
    /// A JSON <c>null</c> reads as <see langword="null"/>.
    /// </para>
    /// <para>
    /// E.164 holds only the country calling code and national number, so an extension does not
    /// survive a round trip. To keep one, store <c>number.Format(PhoneNumberFormat.RFC3966)</c> in a
    /// string property instead.
    /// </para>
    /// </remarks>
    public sealed class PhoneNumberJsonConverter : JsonConverter<PhoneNumber>
    {
        /// <inheritdoc />
        public override PhoneNumber? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected a phone number as a JSON string, found " + reader.TokenType + ".");
            }

            return Parse(reader.GetString());
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, PhoneNumber value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Format(PhoneNumberFormat.E164));

        /// <inheritdoc />
        public override PhoneNumber ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Parse(reader.GetString());

        /// <inheritdoc />
        public override void WriteAsPropertyName(Utf8JsonWriter writer, PhoneNumber value, JsonSerializerOptions options)
            => writer.WritePropertyName(value.Format(PhoneNumberFormat.E164));

        // The input is deliberately left out of the message: it is personal data, and
        // System.Text.Json already adds the JSON path to the exception it surfaces.
        private static PhoneNumber Parse(string? s)
            => PhoneNumberParser.TryParse(s, null, out var number)
                ? number
                : throw new JsonException("The JSON value is not a phone number in international format, such as \"+442070313000\".");
    }
}
