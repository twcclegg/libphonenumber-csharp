using System;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// Obsolete: use <see cref="PhoneNumbers.Text.Json.PhoneNumberJsonConverter"/>.
    /// </summary>
    /// <remarks>
    /// Kept as it was: it reads with <see cref="PhoneNumberUtil.ParseAndKeepRawInput(string, string)"/>, so its
    /// numbers carry <see cref="PhoneNumbers.PhoneNumber.RawInput"/> and do not compare equal to
    /// ones from <see cref="PhoneNumberUtil.Parse(string, string)"/>, and it throws
    /// <see cref="NumberParseException"/> rather than <see cref="JsonException"/> for invalid input.
    /// The replacement fixes both.
    /// </remarks>
    [Obsolete(Obsoletions.JsonMessage, DiagnosticId = Obsoletions.JsonDiagId, UrlFormat = Obsoletions.UrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PhoneNumberConverter : JsonConverter<PhoneNumbers.PhoneNumber>
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        public override PhoneNumbers.PhoneNumber? Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
            => Util.ParseAndKeepRawInput(reader.GetString(), null);

        public override void Write(Utf8JsonWriter writer, PhoneNumbers.PhoneNumber value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Format(PhoneNumberFormat.E164));
    }
}
