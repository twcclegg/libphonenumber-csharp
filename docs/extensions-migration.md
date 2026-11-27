# Migrating from `PhoneNumbers.Extensions`

The first version of the `libphonenumber-csharp.extensions` package put its helpers in namespace
`PhoneNumbers.Extensions`, including a static class named `PhoneNumber` that collides with the core
data type. Those types are now obsolete and each reports a warning, PNX0001 to PNX0005, naming its
replacement. Nothing has been removed: code compiled against them keeps running.

One date matters. In the **first release on or after 2027-03-01**, the colliding class
`PhoneNumbers.Extensions.PhoneNumber` disappears from compilation (it stays in the runtime assembly,
so existing binaries are unaffected). After that, C# 13-or-earlier code that still names it stops
compiling. Migrate before then; the compiler warnings list every call site.

## Steps

1. Update both packages to the same version (the Extensions package pins its core dependency).
2. Build. Every use of the old API now has a `PNX000n` warning.
3. Replace each one, using the table below or the details on the [Diagnostics](diagnostics.md) page.
4. Remove `using PhoneNumbers.Extensions;` from every file, and any `global using` of it.
5. Build again, with the warnings visible, until none of PNX0001–PNX0005 remain.

| Before | After |
| --- | --- |
| `using PhoneNumbers.Extensions;` | `using PhoneNumbers;` (plus the namespaces below where needed) |
| `PhoneNumber.TryParse(s, out var n)` | `PhoneNumber.TryParse(s, out PhoneNumber? n)` on C# 14, `PhoneNumberParser.TryParse(s, out PhoneNumber? n)` earlier |
| `PhoneNumber.TryParseValid(s, "GB", out var n)` | `PhoneNumber.TryParseValid(s, "GB", out PhoneNumber? n)` (`n` is now `null` when the number is invalid) |
| `n.ToE164()` / `n.ToNationalFormat()` / `n.ToInternationalFormat()` | `n.Format(PhoneNumberFormat.E164)` / `NATIONAL` / `INTERNATIONAL` |
| `n.IsValid()` | `n.IsValidNumber()` |
| `new PhoneNumberConverter()`, `PhoneNumberJsonOptions.Default` / `.Create()` | `using PhoneNumbers.Text.Json;` and `options.AddPhoneNumberConverter()` |
| `PhoneNumberJsonOptions.Serialize(n)` / `.Deserialize(json)` | `JsonSerializer.Serialize(n, options)` / `JsonSerializer.Deserialize<PhoneNumber>(json, options)`, or your own source-generated context |
| `PhoneNumberJsonContext` | `[JsonSourceGenerationOptions(Converters = new[] { typeof(PhoneNumberJsonConverter) })]` on your own context |
| `[PhoneNumber(Region = "GB")]` | `using PhoneNumbers.ComponentModel.DataAnnotations;` and `[ValidPhoneNumber(DefaultRegion = "GB")]` |
| `TypeDescriptor.AddAttributes(typeof(PhoneNumber), new TypeConverterAttribute(typeof(PhoneNumberTypeConverter)))` | Delete it: the type has a built-in converter |

### Behaviour that changes when you switch

- **`TryParseValid`** sets its `out` value to `null` when the input parses but is not valid. The old
  one returned the parsed number with `false`.
- **JSON reading** parses with `Parse`, not `ParseAndKeepRawInput`, so deserialized numbers no
  longer carry `RawInput` or `CountryCodeSource`, and now compare equal to numbers from `Parse`,
  configuration and model binding. Invalid input throws `JsonException` instead of
  `NumberParseException`.
- **JSON options** are your own: there is no shared `Default` instance to mutate by accident.

### Files that import both namespaces

While you migrate, a file may need both `using PhoneNumbers;` and `using PhoneNumbers.Extensions;`.
The simple name `PhoneNumber` is ambiguous there (CS0104). The new formatting helpers deliberately
use different names from the old ones, so `n.ToE164()` and `n.Format(...)` can both be called in such
a file; spell the type `var` or `PhoneNumbers.PhoneNumber` until the old import is gone.

### Suppressing while you migrate

Each id can be suppressed separately, on every target framework, with
`<NoWarn>$(NoWarn);PNX0003</NoWarn>` or `#pragma warning disable PNX0003`. Avoid suppressing PNX0001
project-wide: it is the warning that announces the 2027-03-01 change.

## Migrating with an AI assistant

Paste this into your coding assistant, in the repository you want to migrate:

> Migrate this project from the obsolete `PhoneNumbers.Extensions` namespace of the
> `libphonenumber-csharp.extensions` NuGet package to its replacement API in the same package.
> Remove every `using PhoneNumbers.Extensions;` (including `global using`) and make sure
> `using PhoneNumbers;` is present. Replace `PhoneNumbers.Extensions.PhoneNumber.TryParse(...)` and
> `TryParseValid(...)` with `PhoneNumber.TryParse(...)` / `PhoneNumber.TryParseValid(...)` if the
> project's C# language version is 14 or later, or `PhoneNumberParser.TryParse(...)` /
> `PhoneNumberParser.TryParseValid(...)` otherwise, and declare the out variable as
> `PhoneNumber?` rather than `var`; note that the new `TryParseValid` sets the out value to null
> when the number is invalid. Replace `ToE164()`, `ToNationalFormat()` and
> `ToInternationalFormat()` with `Format(PhoneNumberFormat.E164)`, `Format(PhoneNumberFormat.NATIONAL)`
> and `Format(PhoneNumberFormat.INTERNATIONAL)`, and `IsValid()` with `IsValidNumber()`. Replace
> `PhoneNumberConverter`, `PhoneNumberJsonOptions` and `PhoneNumberJsonContext` with
> `using PhoneNumbers.Text.Json;` and `options.AddPhoneNumberConverter()`, or for a source-generated
> `JsonSerializerContext` add
> `[JsonSourceGenerationOptions(Converters = new[] { typeof(PhoneNumberJsonConverter) })]`; where
> code caught `NumberParseException` around JSON deserialization, catch `JsonException`. Replace
> `[PhoneNumber(Region = "X")]` with `[ValidPhoneNumber(DefaultRegion = "X")]` from
> `using PhoneNumbers.ComponentModel.DataAnnotations;`. Delete any
> `TypeDescriptor.AddAttributes(... PhoneNumberTypeConverter ...)` call. Do not suppress the
> warnings. Build with warnings PNX0001 to PNX0005 visible and repeat until none remain.

The same guidance, in a form you can keep in your own `AGENTS.md` or
`.github/copilot-instructions.md`, is in this site's
[llms.txt](https://twcclegg.github.io/libphonenumber-csharp/docs/llms.txt).
