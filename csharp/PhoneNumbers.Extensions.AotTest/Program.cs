using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhoneNumbers;
using PhoneNumbers.Text.Json;

namespace PhoneNumbers.AotTest
{
    internal sealed record Contact(string Name, PhoneNumber Phone, PhoneNumber? Mobile, List<PhoneNumber> Others);

    [JsonSourceGenerationOptions(Converters = new[] { typeof(PhoneNumberJsonConverter) })]
    [JsonSerializable(typeof(Contact))]
    [JsonSerializable(typeof(Dictionary<PhoneNumber, string>))]
    internal sealed partial class AotJsonContext : JsonSerializerContext
    {
    }

    internal static class Program
    {
        private static int failures;

        private static int Main()
        {
            Check(PhoneNumber.TryParse("020 7031 3000", "GB", out PhoneNumber? phone), "TryParse national");
            Check(PhoneNumberParser.TryParse("+447912345678", out PhoneNumber? mobile), "TryParse international");
            Check(!PhoneNumber.TryParse("junk", "GB", out _), "TryParse rejects junk");
            if (phone is null || mobile is null)
            {
                return Report();
            }

            Check(phone.IsValidNumber() && phone.GetNumberType() == PhoneNumberType.FIXED_LINE, "validity and type");
            Check(phone.Format(PhoneNumberFormat.E164) == "+442070313000", "Format");

            var contact = new Contact("Ada", phone, null, new List<PhoneNumber> { mobile });
            var json = JsonSerializer.Serialize(contact, AotJsonContext.Default.Contact);
            var back = JsonSerializer.Deserialize(json, AotJsonContext.Default.Contact);
            Check(back is not null && back.Phone.Equals(phone) && back.Mobile is null && back.Others[0].Equals(mobile),
                "DTO round trip: " + json);

            var byNumber = new Dictionary<PhoneNumber, string> { [phone] = "office" };
            var dictJson = JsonSerializer.Serialize(byNumber, AotJsonContext.Default.DictionaryPhoneNumberString);
            var dictBack = JsonSerializer.Deserialize(dictJson, AotJsonContext.Default.DictionaryPhoneNumberString);
            Check(dictBack is not null && dictBack[phone] == "office", "dictionary key round trip: " + dictJson);

            try
            {
                JsonSerializer.Deserialize("{\"Name\":\"x\",\"Phone\":\"junk\",\"Others\":[]}", AotJsonContext.Default.Contact);
                Check(false, "invalid phone number rejected");
            }
            catch (JsonException)
            {
            }

            // Options built from the context, with the converter added by the extension method
            // rather than by the attribute: the other supported registration.
            var options = new JsonSerializerOptions { TypeInfoResolver = AotJsonContext.Default }.AddPhoneNumberConverter();
            var viaOptions = JsonSerializer.Serialize(contact, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<Contact>)options.GetTypeInfo(typeof(Contact)));
            Check(viaOptions == json, "options-based serialization matches");

            return Report();
        }

        private static void Check(bool condition, string what)
        {
            Console.WriteLine((condition ? "ok   " : "FAIL ") + what);
            if (!condition)
            {
                failures++;
            }
        }

        private static int Report()
        {
            Console.WriteLine(failures == 0 ? "Native AOT round trip passed." : failures + " check(s) failed.");
            return failures == 0 ? 0 : 1;
        }
    }
}
