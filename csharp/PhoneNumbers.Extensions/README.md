# libphonenumber-csharp.extensions

C#-idiomatic companion to [`libphonenumber-csharp`](https://www.nuget.org/packages/libphonenumber-csharp),
the C# port of Google's [libphonenumber](https://github.com/google/libphonenumber).

The core package mirrors the Java API, so upstream fixes port cleanly and Google's documentation
applies unchanged. This package adds what C# developers expect and Java doesn't have.

```csharp
using PhoneNumbers;

if (PhoneNumber.TryParse(input, "GB", out PhoneNumber? number) && number.IsValidNumber())
{
    Console.WriteLine(number.Format(PhoneNumberFormat.E164));   // +442070313000
    Console.WriteLine(number.GetNumberType());                   // FIXED_LINE
}
```

- **Parse without exceptions:** `PhoneNumber.TryParse` / `TryParseValid` on C# 14, or
  `PhoneNumberParser.TryParse` / `TryParseValid` on earlier versions and VB. Nullable-annotated,
  and a hopeless input is rejected in nanoseconds instead of a thrown exception.
- **Helpers on the number:** `Format`, `IsValidNumber`, `GetNumberType`, `GetRegionCodeForNumber`.
- **System.Text.Json:** `using PhoneNumbers.Text.Json;` then
  `new JsonSerializerOptions().AddPhoneNumberConverter()`, for any DTO. Native AOT: list
  `PhoneNumberJsonConverter` on your own `JsonSerializerContext`.
- **Validation:** `[ValidPhoneNumber(DefaultRegion = "GB")]` from
  `PhoneNumbers.ComponentModel.DataAnnotations`.

Only `using PhoneNumbers;` is needed for parsing and formatting. Don't import
`PhoneNumbers.Extensions`: that namespace is the obsolete first version of this package
(warnings PNX0001–PNX0005).

Targets `netstandard2.0`, `net8.0` and `net10.0`, and is trim- and Native AOT-compatible on the
modern targets.

- [Extensions guide](https://twcclegg.github.io/libphonenumber-csharp/docs/articles/extensions.html)
- [Migrating from `PhoneNumbers.Extensions`](https://twcclegg.github.io/libphonenumber-csharp/docs/articles/extensions-migration.html)
- [API reference](https://twcclegg.github.io/libphonenumber-csharp/docs/)
- [Source and issues](https://github.com/twcclegg/libphonenumber-csharp)
