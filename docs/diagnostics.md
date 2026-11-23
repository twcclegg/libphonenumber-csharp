# Diagnostics PNX0001–PNX0005

The original API of the `libphonenumber-csharp.extensions` package, everything in namespace
`PhoneNumbers.Extensions`, is obsolete. Each type reports its own warning id when you use it, so
you can find, migrate and (temporarily) suppress them one kind at a time. The replacements live in
namespace `PhoneNumbers` and in namespaces that mirror the .NET ones they extend; see the
[Extensions guide](extensions.md) for how to use them and the
[migration guide](extensions-migration.md) for a step-by-step walk-through.

Code compiled against the old types keeps running: nothing has been removed from the package.

| Id | Obsolete | Replacement |
| --- | --- | --- |
| [PNX0001](#PNX0001) | `PhoneNumbers.Extensions.PhoneNumber` | `PhoneNumber.TryParse` / `TryParseValid` (C# 14), `PhoneNumberParser.TryParse` / `TryParseValid` |
| [PNX0002](#PNX0002) | `PhoneNumbers.Extensions.PhoneNumberExtensions` | `number.Format(...)`, `number.IsValidNumber()` |
| [PNX0003](#PNX0003) | `PhoneNumberConverter`, `PhoneNumberJsonContext`, `PhoneNumberJsonOptions` | `PhoneNumbers.Text.Json.PhoneNumberJsonConverter`, `options.AddPhoneNumberConverter()` |
| [PNX0004](#PNX0004) | `PhoneNumberAttribute` | `PhoneNumbers.ComponentModel.DataAnnotations.ValidPhoneNumberAttribute` |
| [PNX0005](#PNX0005) | `PhoneNumberTypeConverter` | Nothing: `PhoneNumber` has a built-in `TypeConverter` |

**Suppressing.** Each id can be suppressed on its own, on every target framework (including
.NET Framework), with `<NoWarn>$(NoWarn);PNX0003</NoWarn>` or `#pragma warning disable PNX0003`.
Please don't suppress PNX0001 across a project: it is the warning that tells you about the
scheduled change below.

<a id="PNX0001"></a>

## PNX0001: `PhoneNumbers.Extensions.PhoneNumber` is obsolete

```text
warning PNX0001: 'PhoneNumber' is obsolete: 'PhoneNumbers.Extensions.PhoneNumber is obsolete and
will be hidden from compilation in the first release on or after 2027-03-01 ...'
```

**Why.** The static helper class shares its simple name with the core data type
`PhoneNumbers.PhoneNumber`. A file that imports both namespaces cannot name either (CS0104), and
the helper's `out` parameter could only be written as `out var` (CS0723).

**Scheduled change.** In the **first release on or after 2027-03-01**, this one class is removed
from the package's reference assembly and kept in its runtime assembly. That is a source change
only:

- Code already compiled against it keeps running, with no rebuild.
- On C# 14, a file that still imports both namespaces starts compiling unchanged, because
  `PhoneNumber.TryParse` then resolves to the new member.
- Only a call site written on C# 13 or earlier that still names the old class gets a compile
  error, which is why the warning comes first.

**Fix.**

```csharp
// Before
using PhoneNumbers.Extensions;
if (PhoneNumber.TryParse(input, "GB", out var number)) { ... }

// After, C# 14 (.NET 10 SDK)
using PhoneNumbers;
if (PhoneNumber.TryParse(input, "GB", out PhoneNumber? number)) { ... }

// After, C# 13 and earlier, or Visual Basic
using PhoneNumbers;
if (PhoneNumberParser.TryParse(input, "GB", out PhoneNumber? number)) { ... }
```

One behaviour differs: the new `TryParseValid` sets the `out` value to `null` when the input
parses but is not a valid number. The old one handed back the parsed number in that case.

<a id="PNX0002"></a>

## PNX0002: `PhoneNumberExtensions` is obsolete

**Why.** The old extension methods (`ToE164`, `ToNationalFormat`, `ToInternationalFormat`,
`IsValid`) needed `using PhoneNumbers.Extensions;`, which brings PNX0001's name collision with it.

**Fix.** Use the methods of `PhoneNumberFormatting` in namespace `PhoneNumbers`, which keep the
names of the `PhoneNumberUtil` methods they call:

| Old | New |
| --- | --- |
| `number.ToE164()` | `number.Format(PhoneNumberFormat.E164)` |
| `number.ToNationalFormat()` | `number.Format(PhoneNumberFormat.NATIONAL)` |
| `number.ToInternationalFormat()` | `number.Format(PhoneNumberFormat.INTERNATIONAL)` |
| `number.IsValid()` | `number.IsValidNumber()` |

The names differ on purpose, so both sets can be called from the same file while you migrate.

<a id="PNX0003"></a>

## PNX0003: the `PhoneNumbers.Extensions` JSON types are obsolete

Covers `PhoneNumberConverter`, `PhoneNumberJsonContext` and `PhoneNumberJsonOptions`.

**Why.** `PhoneNumberJsonOptions.Default` serializes a bare `PhoneNumber` only and throws
`NotSupportedException` for any other type, including a DTO with a `PhoneNumber` property.
`PhoneNumberConverter` reads with `ParseAndKeepRawInput`, so its numbers never compare equal to
ones from `Parse`, configuration or model binding, and it throws `NumberParseException` rather
than `JsonException`.

**Fix.**

```csharp
using PhoneNumbers.Text.Json;

var options = new JsonSerializerOptions().AddPhoneNumberConverter();
var json = JsonSerializer.Serialize(contact, options);   // any DTO
```

For trimmed or Native AOT apps, list the converter on your own source-generated context and
delete any use of `PhoneNumberJsonContext`:

```csharp
[JsonSourceGenerationOptions(Converters = new[] { typeof(PhoneNumberJsonConverter) })]
[JsonSerializable(typeof(Contact))]
partial class AppJsonContext : JsonSerializerContext { }
```

The new converter reads invalid input as a `JsonException` (with the JSON path), not a
`NumberParseException`. If you caught the latter around deserialization, catch the former.

<a id="PNX0004"></a>

## PNX0004: `PhoneNumberAttribute` is obsolete

**Why.** Its name is close to the core type's, and `Region` did not say what it is for.

**Fix.** Replace `[PhoneNumber(Region = "GB")]` with
`[ValidPhoneNumber(DefaultRegion = "GB")]` from namespace
`PhoneNumbers.ComponentModel.DataAnnotations`. Validation is the same.

<a id="PNX0005"></a>

## PNX0005: `PhoneNumberTypeConverter` is obsolete

**Why.** `PhoneNumber` now carries a built-in `TypeConverter` from the core package, which
configuration binding (`IOptions<T>`), `TypeDescriptor` and other converter-based frameworks find
without any registration.

**Fix.** Delete the registration:

```csharp
// Delete this line. It replaces the built-in converter with the obsolete one.
TypeDescriptor.AddAttributes(typeof(PhoneNumber), new TypeConverterAttribute(typeof(PhoneNumberTypeConverter)));
```
