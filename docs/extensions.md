# The Extensions package

[`libphonenumber-csharp.extensions`](https://www.nuget.org/packages/libphonenumber-csharp.extensions)
is the C#-shaped layer over this library. The core package mirrors Google's Java API so that every
upstream fix ports cleanly and Google's documentation applies unchanged; the conveniences Java has
no counterpart for live here instead:

- a non-throwing, nullable-annotated `TryParse`;
- `Format`, `IsValidNumber`, `GetNumberType` and `GetRegionCodeForNumber` on the number itself;
- a System.Text.Json converter that works in any DTO, trimmed and under Native AOT;
- a DataAnnotations validation attribute.

```bash
dotnet add package libphonenumber-csharp.extensions
```

It targets `netstandard2.0`, `net8.0` and `net10.0`, is trim- and AOT-compatible on the modern
targets, and depends on the core package of the same version.

| Namespace | What |
| --- | --- |
| `PhoneNumbers` | `PhoneNumber.TryParse` / `TryParseValid` (`PhoneNumberParser`), `number.Format(...)` and friends (`PhoneNumberFormatting`) |
| `PhoneNumbers.Text.Json` | `PhoneNumberJsonConverter`, `options.AddPhoneNumberConverter()` |
| `PhoneNumbers.ComponentModel.DataAnnotations` | `[ValidPhoneNumber]` |

Everything you need for parsing, validating and formatting comes with `using PhoneNumbers;`, the
same import as the core package. **Do not import `PhoneNumbers.Extensions`**: that namespace holds
the obsolete first version of this package (see [Diagnostics](diagnostics.md) and the
[migration guide](extensions-migration.md)).

## Parse, validate, format

```csharp
using PhoneNumbers;

if (PhoneNumber.TryParse(input, "GB", out PhoneNumber? number) && number.IsValidNumber())
{
    string e164 = number.Format(PhoneNumberFormat.E164);       // "+442070313000"
    string display = number.Format(PhoneNumberFormat.NATIONAL); // "020 7031 3000"
    PhoneNumberType type = number.GetNumberType();              // FIXED_LINE
    string? region = number.GetRegionCodeForNumber();           // "GB"
}
```

- `TryParse(s, out number)` accepts international input only: E.164 (`"+442070313000"`), a
  formatted international number, or an RFC 3966 URI (`"tel:+44-20-7031-3000"`).
- `TryParse(s, defaultRegion, out number)` also accepts national input, interpreted in
  `defaultRegion` (a two-letter CLDR code such as `"GB"`).
- `TryParseValid` does both steps at once: `true` only for a valid number, and `number` is `null`
  otherwise.
- After a `true` return, `number` is known not to be null; no `!` is needed.

Parsing is exactly `PhoneNumberUtil.Parse`, and returns `false` where `Parse` would throw. Input
that cannot possibly parse (empty, no digits, a national number with no region) is rejected without
throwing anything, so a failed `TryParse` costs nanoseconds, not microseconds.

### C# versions and Visual Basic

`PhoneNumber.TryParse` is a C# 14 *extension member* (the .NET 10 SDK and later). The compiler also
emits each member as an ordinary static method on `PhoneNumberParser`, which is what earlier
versions and other languages call. The formatting helpers are ordinary extension methods.

| You write | C# 14 | C# 7.3–13 | Visual Basic |
| --- | --- | --- | --- |
| `PhoneNumber.TryParse(s, "GB", out PhoneNumber? n)` | ✓ | – | – |
| `PhoneNumberParser.TryParse(s, "GB", out PhoneNumber? n)` | ✓ | ✓ | `PhoneNumberParser.TryParse(s, "GB", n)` |
| `n.Format(PhoneNumberFormat.E164)`, `n.IsValidNumber()` | ✓ | ✓ | ✓ |

## ASP.NET Core

### Route and query parameters

A `PhoneNumber` parameter binds from the route or query string with no setup, in minimal APIs and
in MVC, because the core type implements `IParsable<PhoneNumber>` on .NET 8 and later:

```csharp
app.MapGet("/call/{number}", (PhoneNumber number) => number.Format(PhoneNumberFormat.E164));
// GET /call/+442070313000 -> 200; GET /call/junk -> 400
```

Binding has no region to fall back on, so the value must be in international format
(`%2B442070313000` once URL-encoded). To accept national numbers, bind a `string` and parse it
yourself with the region you know:

```csharp
app.MapGet("/call/{country}/{number}", (string country, string number) =>
    PhoneNumber.TryParse(number, country, out PhoneNumber? parsed)
        ? Results.Ok(parsed.Format(PhoneNumberFormat.E164))
        : Results.BadRequest());
```

Because the type converts from a string, ASP.NET Core treats `PhoneNumber` as a simple value: an
MVC action or minimal API parameter of that type comes from the route or query, not the request
body. Mark it `[FromBody]` if it really is the whole body.

### JSON request and response bodies

Register the converter once and every `PhoneNumber` in every DTO is an E.164 string:

```csharp
using PhoneNumbers.Text.Json;

// Minimal APIs
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.AddPhoneNumberConverter());

// MVC controllers
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.AddPhoneNumberConverter());
```

An invalid value fails model binding with a 400 that names the JSON path. For Native AOT see
[System.Text.Json](#systemtextjson) below.

### Validation

Put `[ValidPhoneNumber]` on the DTO (see [DataAnnotations](#dataannotations)). MVC controllers with
`[ApiController]` validate automatically; minimal APIs on .NET 10 validate once
`builder.Services.AddValidation()` is called.

## Configuration and `IOptions<T>`

A `PhoneNumber` property binds from configuration with no registration, through the core type's
built-in `TypeConverter`:

```csharp
public sealed class SmsOptions
{
    public PhoneNumber? Sender { get; set; }
}

builder.Services.Configure<SmsOptions>(builder.Configuration.GetSection("Sms"));
```

```json
{ "Sms": { "Sender": "+442070313000" } }
```

- The value must be in international format; an invalid one fails when the options are first read.
- A blank value leaves the property `null` rather than failing startup.
- If you registered the obsolete `PhoneNumberTypeConverter` with `TypeDescriptor.AddAttributes`,
  delete that line ([PNX0005](diagnostics.md#PNX0005)).
- **Native AOT:** the configuration-binding source generator cannot bind a `PhoneNumber` property
  (it reports SYSLIB1100). Bind a `string` and parse it with `PhoneNumber.TryParse`.

## System.Text.Json

```csharp
using System.Text.Json;
using PhoneNumbers.Text.Json;

var options = new JsonSerializerOptions().AddPhoneNumberConverter();

public record Contact(string Name, PhoneNumber Phone, PhoneNumber? Mobile);

string json = JsonSerializer.Serialize(contact, options);
// {"Name":"Ada","Phone":"+442070313000","Mobile":null}
```

- Works at any depth: properties, collections, and dictionary keys.
- Writes E.164. System.Text.Json's default encoder escapes `+` as `+`; it is the same string
  to any JSON reader.
- Reads international format only, with the same parse as `TryParse(s, out ...)`. A number read
  from JSON is equal to the same text parsed by `PhoneNumberUtil.Parse`, configuration binding or
  model binding.
- An invalid value throws `JsonException`, with the JSON path and without the value itself.
- E.164 has no room for an extension, so an extension is dropped on write. If you need to keep one,
  store `number.Format(PhoneNumberFormat.RFC3966)` in a `string` property.

Without a converter, System.Text.Json would serialize `PhoneNumber`'s properties one by one, which
does not terminate. Always register it.

### Trimming and Native AOT

List the converter on your own source-generated context. There is no shared context or options
instance to combine with:

```csharp
[JsonSourceGenerationOptions(Converters = new[] { typeof(PhoneNumberJsonConverter) })]
[JsonSerializable(typeof(Contact))]
internal partial class AppJsonContext : JsonSerializerContext { }

string json = JsonSerializer.Serialize(contact, AppJsonContext.Default.Contact);

// ASP.NET Core minimal APIs under Native AOT
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
```

`[JsonConverter(typeof(PhoneNumberJsonConverter))]` on an individual property also works. The
repository publishes exactly this setup with Native AOT in CI and checks the round trip.

## DataAnnotations

```csharp
using PhoneNumbers.ComponentModel.DataAnnotations;

public record SignUp(
    [property: Required, ValidPhoneNumber(DefaultRegion = "GB")] string Phone,
    [property: ValidPhoneNumber] PhoneNumber? Backup);
```

- A `string` is parsed with `DefaultRegion` (international format only when it is not set) and must
  be a valid number. A `PhoneNumber` must be valid; `DefaultRegion` plays no part, since it was
  already parsed.
- `null` is valid; combine with `[Required]` to require a value. An empty string is invalid.
- The default message is "The {0} field is not a valid phone number."; set `ErrorMessage` to change
  it.

## Recipes

Integrations that would add a dependency to the package are documented here instead of shipped.

### Newtonsoft.Json

Often nothing is required: the core type's built-in `TypeConverter` already makes Newtonsoft.Json
read and write a `PhoneNumber` as an E.164 string, dictionary keys included. Add a converter if you want
invalid input to fail with Newtonsoft's own `JsonSerializationException` and a clear message, or to
change how numbers are written. This one is compiled and tested in the repository:

```csharp
using System;
using Newtonsoft.Json;
using PhoneNumbers;

public sealed class NewtonsoftPhoneNumberConverter : JsonConverter<PhoneNumber>
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
```

Register it with `new JsonSerializerSettings { Converters = { new NewtonsoftPhoneNumberConverter() } }`.
Newtonsoft.Json does not escape `+`, so its output reads `"+442070313000"`.

### Entity Framework Core

Store the E.164 string in a column of at most 16 characters (`+` and up to 15 digits). For one
property:

```csharp
modelBuilder.Entity<Contact>()
    .Property(c => c.Phone)
    .HasConversion(
        number => number.Format(PhoneNumberFormat.E164),
        text => PhoneNumberUtil.GetInstance().Parse(text, null))
    .HasMaxLength(16);
```

For every `PhoneNumber` property in the model:

```csharp
public sealed class PhoneNumberToE164Converter : ValueConverter<PhoneNumber, string>
{
    public PhoneNumberToE164Converter()
        : base(number => number.Format(PhoneNumberFormat.E164),
               text => PhoneNumberUtil.GetInstance().Parse(text, null))
    {
    }
}

protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    => configurationBuilder.Properties<PhoneNumber>()
        .HaveConversion<PhoneNumberToE164Converter>()
        .HaveMaxLength(16);
```

Equality queries such as `Where(c => c.Phone == number)` compare the stored strings. As with JSON,
E.164 drops an extension. This recipe is not compiled in the repository, which does not reference
EF Core.
