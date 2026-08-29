# API differences from upstream Java

This port follows the upstream Java `libphonenumber` API closely — file names, class
names, and method names generally match one-to-one (`PhoneNumberUtil.java` ↔
`PhoneNumberUtil.cs`, `isValidNumber` ↔ `IsValidNumber`, etc.), and where behavior is in
question the Java source is the source of truth. This page documents the places where the
*shape* of the C# API is not what a straight Java-to-C# reading would suggest, or not what
a C# developer would instinctively reach for. Every entry below was verified against the
current source in this repository (file paths are given so you can check them yourself).

Use this page two ways: as a contributor porting a new upstream Java change, to know where
the port intentionally departs from a literal translation; and as a consumer, to avoid the
specific mistakes documented here.

## Contents

- [`Locale` is a small type local to this library, not `System.Globalization.CultureInfo`](#locale-is-a-small-type-local-to-this-library-not-systemglobalizationcultureinfo)
- [`PhoneNumbers.Extensions.PhoneNumber` and `PhoneNumbers.PhoneNumber` share a simple name](#phonenumbersextensionsphonenumber-and-phonenumbersphonenumber-share-a-simple-name)
- [Enum members keep Java's `SCREAMING_SNAKE_CASE`, not .NET `PascalCase`](#enum-members-keep-javas-screaming_snake_case-not-net-pascalcase)
- [`PhoneNumber` looks like a plain settable class but is a protobuf-style immutable message](#phonenumber-looks-like-a-plain-settable-class-but-is-a-protobuf-style-immutable-message)
- [`CharSequence` parameters become `string`, not a comparable abstraction](#charsequence-parameters-become-string-not-a-comparable-abstraction)
- [Metadata and prefix maps are a custom binary format, not protocol buffers](#metadata-and-prefix-maps-are-a-custom-binary-format-not-protocol-buffers)

---

## `Locale` is a small type local to this library, not `System.Globalization.CultureInfo`

**Java:** `PhoneNumberOfflineGeocoder.getDescriptionForNumber` and
`PhoneNumberToCarrierMapper.getNameForNumber` take a `java.util.Locale`.

**C# (this port):** the equivalent parameter is `PhoneNumbers.Locale` — a small class
defined in [`csharp/PhoneNumbers/PhoneNumberOfflineGeocoder.cs`](../csharp/PhoneNumbers/PhoneNumberOfflineGeocoder.cs)
(despite the file name, it is *not* nested inside `PhoneNumberOfflineGeocoder`; it's a
top-level type in the `PhoneNumbers` namespace). It is constructed directly:

```csharp
public class Locale
{
    public static readonly Locale English = new("en", "GB");
    public static readonly Locale French = new("fr", "FR");
    public static readonly Locale German = new("de", "DE");
    public static readonly Locale Italian = new("it", "IT");
    public static readonly Locale Korean = new("ko", "KR");
    public static readonly Locale SimplifiedChinese = new("zh", "CN");

    public readonly string Language;
    public readonly string Country;

    public Locale(string language, string countryCode) { ... }
}
```

`GetDescriptionForNumber(PhoneNumber, Locale)` and `GetNameForNumber(PhoneNumber, Locale)`
both take this type — not `System.Globalization.CultureInfo`, which is what a C# developer
would instinctively reach for given a parameter named `languageCode` next to a phone
number API. Passing a `CultureInfo` will not compile; use one of the static presets
(`Locale.English`, `Locale.French`, ...) or `new Locale("en", "US")`.

**Why:** `java.util.Locale` has no direct BCL equivalent with the same two-part
language/country shape, so the port ported the small pieces of `java.util.Locale` actually
used (language + country, and `getDisplayCountry`) into a dedicated type instead of
reshaping the API around `CultureInfo`. One known gap: `java.util.Locale.getScript()` has
no equivalent here, so callers cannot express a script subtag — see the comment in
[`PhoneNumberToCarrierMapper.cs`](../csharp/PhoneNumbers/PhoneNumberToCarrierMapper.cs)
(`GetNameForValidNumber`), which always passes an empty script to the underlying prefix
reader.

---

## `PhoneNumbers.Extensions.PhoneNumber` and `PhoneNumbers.PhoneNumber` share a simple name

**Java:** no equivalent — upstream Java ships a single artifact, so this situation cannot
arise there.

**C# (this port):** the main package's data type is `PhoneNumbers.PhoneNumber` (the
protobuf-derived phone number value — see
[`csharp/PhoneNumbers/Phonenumber.cs`](../csharp/PhoneNumbers/Phonenumber.cs)). The
separate `libphonenumber-csharp.extensions` package adds a *static helper class* with the
same simple name, `PhoneNumbers.Extensions.PhoneNumber`, exposing `TryParse` /
`TryParseValid` (see
[`csharp/PhoneNumbers.Extensions/PhoneNumber.cs`](../csharp/PhoneNumbers.Extensions/PhoneNumber.cs)).

If a file has both `using PhoneNumbers;` and `using PhoneNumbers.Extensions;`, an unqualified
reference to `PhoneNumber` is genuinely ambiguous and fails to compile:

```
error CS0104: 'PhoneNumber' is an ambiguous reference between
'PhoneNumbers.PhoneNumber' and 'PhoneNumbers.Extensions.PhoneNumber'
```

Work around it with a fully-qualified call, e.g.:

```csharp
if (PhoneNumbers.Extensions.PhoneNumber.TryParse(input, out PhoneNumbers.PhoneNumber number))
{
    ...
}
```

or a `using` alias (`using PhoneNumberHelper = PhoneNumbers.Extensions.PhoneNumber;`) if
the ambiguity comes up often in one file.

**Why:** this is a naming collision incidental to how the Extensions package is organized
— `PhoneNumber` reads naturally as "the static helper for working with a `PhoneNumber`",
mirroring the type it wraps, but that convenience is exactly what collides. It is not a
Java/C# behavioral difference, just something worth knowing before it surprises you with a
compiler error in code that otherwise looks correct.

---

## Enum members keep Java's `SCREAMING_SNAKE_CASE`, not .NET `PascalCase`

**Java:** enum constants use Java's constant-naming convention, e.g.
`PhoneNumberType.FIXED_LINE`, `PhoneNumberFormat.E164`, `ErrorType.INVALID_COUNTRY_CODE`.

**C# (this port):** the same spelling is kept as-is instead of being translated to the
.NET-idiomatic `PascalCase` a C# developer would expect (`PhoneNumberType.FixedLine`, for
example, does not exist — it's `PhoneNumberType.FIXED_LINE`). This is consistent across
every public enum in the library, not just one:

- [`PhoneNumberType`](../csharp/PhoneNumbers/PhoneNumberType.cs) — `FIXED_LINE`, `MOBILE`, `TOLL_FREE`, `UNKNOWN`, ...
- [`PhoneNumberFormat`](../csharp/PhoneNumbers/PhoneNumberFormat.cs) — `E164`, `INTERNATIONAL`, `NATIONAL`, `RFC3966`
- [`NumberParseException.ErrorType`](../csharp/PhoneNumbers/NumberParseException.cs) — `INVALID_COUNTRY_CODE`, `NOT_A_NUMBER`, ...
- `PhoneNumberUtil.MatchType`, `PhoneNumberUtil.ValidationResult`, `PhoneNumberUtil.Leniency` (all in [`PhoneNumberUtil.cs`](../csharp/PhoneNumbers/PhoneNumberUtil.cs))
- [`ShortNumberInfo.ShortNumberCost`](../csharp/PhoneNumbers/ShortNumberInfo.cs)
- `PhoneNumber.Types.CountryCodeSource` (in [`Phonenumber.cs`](../csharp/PhoneNumbers/Phonenumber.cs))

**Why:** a straight rename to `PascalCase` would be a purely cosmetic diff against every
Java release this port tracks, for no behavioral gain, and it's easy to keep matching
Java exactly (`grep`-able) at the cost of a naming-convention violation IDEs and analyzers
will flag. Worth knowing going in so `PhoneNumberType.FixedLine` doesn't cost you a
"did you mean" compiler round-trip.

---

## `PhoneNumber` looks like a plain settable class but is a protobuf-style immutable message

**Java:** `com.google.i18n.phonenumbers.Phonenumber.PhoneNumber` is a generated protobuf
message: private fields, `getXxx()`/`hasXxx()` accessors, and mutation only through a
nested `Builder` (`PhoneNumber.newBuilder().setCountryCode(1)...build()`).

**C# (this port):** [`csharp/PhoneNumbers/Phonenumber.cs`](../csharp/PhoneNumbers/Phonenumber.cs)
mirrors that same protobuf shape rather than turning it into a plain C# POCO. `CountryCode`,
`NationalNumber`, `Extension`, etc. are real public properties (so they read like an
ordinary mutable class), but every setter is `internal`:

```csharp
public int CountryCode { get; internal set; }
public bool HasCountryCode => CountryCode != 0;
```

That means an object initializer that looks perfectly reasonable to a C# developer —

```csharp
var number = new PhoneNumber { CountryCode = 1, NationalNumber = 4155551234 }; // CS0200
```

— fails to compile (the setters aren't accessible outside the assembly). The only ways to
get a `PhoneNumber` are `PhoneNumberUtil.Parse(...)` or the protobuf-style builder:

```csharp
var number = PhoneNumber.CreateBuilder()
    .SetCountryCode(1)
    .SetNationalNumber(4155551234)
    .Build();
```

**Why:** this one *does* match Java faithfully — Java's protobuf messages are equally
immutable outside their builder, so a straight port preserves that. The trap is purely
that C#'s auto-property syntax makes `PhoneNumber` *look* like an ordinary settable class
at a glance (Java has no equivalent syntax to be misled by), so the internal setters come
as a surprise the first time you reach for an object initializer instead of `Parse` or
`CreateBuilder()`.

---

## `CharSequence` parameters become `string`, not a comparable abstraction

**Java:** several entry points accept `CharSequence` (e.g. `PhoneNumberMatcher`'s
constructor, `isViablePhoneNumber`), letting callers pass a `String`, `StringBuilder`, or
`StringBuffer` without copying.

**C# (this port):** the equivalent spots take `string` (`PhoneNumberMatcher(PhoneNumberUtil, string, ...)`,
`PhoneNumberUtil.IsViablePhoneNumber(string)`) rather than a comparable abstraction such as
`ReadOnlySpan<char>` or a generic `IEnumerable<char>`, so a caller building a number up in
a `StringBuilder` has to call `.ToString()` first.

**Why:** `CharSequence` has no single natural .NET analogue that preserves both the
zero-copy behavior and the API ergonomics — `ReadOnlySpan<char>` can't be a field or used
across `async`/iterator boundaries the way this code is structured, and a generic
interface would add an abstraction layer for a case (`StringBuilder` input) that is rare
in practice. `string` was chosen as the pragmatic equivalent.

---

## Metadata and prefix maps are a custom binary format, not protocol buffers

**Java:** metadata (`PhoneNumberMetadata.xml`, etc.) is compiled to protocol buffers and
read from those at run time.

**C# (this port):** the XML source files in `resources/` are converted to a custom
per-region binary format at build time by `PhoneNumbers.MetadataBuilder`; the published
assembly embeds those binaries (gzip-compressed) and never reads XML or protocol buffers
at run time. Geocoding, timezone, and carrier prefix maps go through the same
build-time-binary-and-embed pipeline — no zip files or text files are needed to run the
library or its tests. See `IMetadataLoader` / `EmbeddedResourceMetadataLoader` in
`csharp/PhoneNumbers/` for the read side.

**Why:** avoids taking a runtime dependency on Google's protobuf C# library purely to
deserialize static, build-time-known data, and keeps the library trim/AOT-friendly (no
protobuf reflection at run time). This does not change any public method's behavior — it's
an internal storage detail — but it does mean `PhoneNumber`'s protobuf-*shaped* API (see
above) is not backed by actual protobuf serialization in this port, which can be confusing
if you come from the Java side expecting `.proto`-generated wire compatibility.
