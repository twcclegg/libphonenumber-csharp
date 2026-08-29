// Smoke test for the *published* NuGet packages, not the in-repo source: this project restores
// libphonenumber-csharp / libphonenumber-csharp.extensions from nuget.org via a PackageReference
// (see the .csproj), so a run here proves what a real consumer gets after `dotnet add package`.
//
// It runs unmodified under two modes (see .github/workflows/package_smoke_test.yml):
//   1. `dotnet run`                                  - normal managed execution.
//   2. `dotnet publish -p:PublishAot=true` + execute - Native AOT, exercising the trim/AOT
//      annotations the library ships (IsAotCompatible) end to end, not just the static analyzer
//      warnings that PhoneNumbers.csproj's own build already checks.
//
// Every check funnels through Check()/CheckEqual(), which print PASS/FAIL and tally failures;
// Main returns 1 if anything failed, so CI fails loudly instead of on a swallowed exception.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhoneNumbers;
// Both namespaces define a type named "PhoneNumber" (the data type here, the static
// TryParse/TryParseValid helper in Extensions) - having both `using`s active is what forces every
// reference to either below to be fully qualified (CS0104 otherwise), which is deliberate: it is
// the exact ambiguity a real consumer hits combining these two packages.
using PhoneNumbers.Extensions;

var failures = 0;

void Check(bool condition, string description)
{
    if (condition)
    {
        Console.WriteLine($"PASS: {description}");
    }
    else
    {
        Console.WriteLine($"FAIL: {description}");
        failures++;
    }
}

void CheckEqual<T>(T expected, T actual, string description)
{
    Check(Equals(expected, actual), $"{description} (expected '{expected}', got '{actual}')");
}

// --- Version check -----------------------------------------------------------------------------
// Confirms restore actually resolved the version this run was asked to test, rather than a stale
// or cached one - the "expected" here is not what the library reports about itself, it's what CI
// (or a human dispatching the workflow) requested via the environment/argument.

var expectedVersion = Environment.GetEnvironmentVariable("PACKAGE_UNDER_TEST_VERSION")
    ?? (args.Length > 0 ? args[0] : null);

var informationalVersion = typeof(PhoneNumberUtil).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion;
Console.WriteLine($"PhoneNumbers.dll AssemblyInformationalVersion: {informationalVersion}");

if (string.IsNullOrEmpty(expectedVersion))
{
    Console.WriteLine("WARN: no expected version supplied (PACKAGE_UNDER_TEST_VERSION env var or " +
                       "argv[0]) - skipping the version-match check.");
}
else
{
    // The informational version can carry a build-metadata suffix (e.g. "9.0.38+abcdef123"); only
    // the part before '+' is the package version.
    var reportedVersion = informationalVersion?.Split('+')[0];
    CheckEqual(expectedVersion, reportedVersion, "restored package version matches the requested one");
}

// --- Parse / format / validate across a few regions ---------------------------------------------

var phoneUtil = PhoneNumberUtil.GetInstance();

void CheckNumber(string raw, string region, string expectedE164, string expectedNational)
{
    var number = phoneUtil.Parse(raw, region);
    Check(phoneUtil.IsValidNumber(number), $"{raw} ({region}) parses as valid");
    CheckEqual(expectedE164, phoneUtil.Format(number, PhoneNumberFormat.E164),
        $"{raw} ({region}) formats as E164");
    CheckEqual(expectedNational, phoneUtil.Format(number, PhoneNumberFormat.NATIONAL),
        $"{raw} ({region}) formats as NATIONAL");
    Check(phoneUtil.Format(number, PhoneNumberFormat.INTERNATIONAL).StartsWith('+'),
        $"{raw} ({region}) formats as INTERNATIONAL starting with '+'");
}

CheckNumber("(650) 253-0000", "US", "+16502530000", "(650) 253-0000");
CheckNumber("020 7031 3000", "GB", "+442070313000", "020 7031 3000");
CheckNumber("030 901820", "DE", "+4930901820", "030 901820");

// --- AsYouTypeFormatter --------------------------------------------------------------------------

var formatter = phoneUtil.GetAsYouTypeFormatter("US");
var lastFormatted = "";
// No leading '+': this exercises the NANP trunk-prefix path (a bare "1" read as the country code
// digit), not the international-prefix path - typing a leading '+' takes a different branch and
// keeps it in the output instead.
foreach (var c in "16502530000")
{
    lastFormatted = formatter.InputDigit(c);
}
CheckEqual("1 (650) 253-0000", lastFormatted, "AsYouTypeFormatter formats a US number as it is typed");

// --- ShortNumberInfo (separate metadata file) ---------------------------------------------------

var shortNumberInfo = ShortNumberInfo.GetInstance();
var shortNumber = phoneUtil.Parse("911", "US");
Check(shortNumberInfo.IsValidShortNumber(shortNumber), "911 (US) is a valid short number");

// --- Offline geocoder / carrier / timezone mappers (embedded binary metadata) --------------------
// These are the point of this test: they read the gzip-compressed binary resources embedded in the
// published assembly, which nothing in the library's own build-time AOT/trim analysis exercises.

var geocoder = PhoneNumberOfflineGeocoder.GetInstance();
var geocodedNumber = phoneUtil.Parse("+16502530000", "US");
var description = geocoder.GetDescriptionForNumber(geocodedNumber, Locale.English);
Check(!string.IsNullOrEmpty(description), $"offline geocoder returns a description ('{description}')");

var carrierMapper = PhoneNumberToCarrierMapper.GetInstance();
// Carrier data is sparse by design (most numbers are ported/MVNO and have no known carrier), so
// this only checks that the lookup runs against the embedded data without throwing - not that it
// returns a non-empty name.
var carrierName = carrierMapper.GetNameForNumber(geocodedNumber, Locale.English);
Console.WriteLine($"PASS: carrier mapper ran without throwing (name: '{carrierName}')");

var timeZoneMapper = PhoneNumberToTimeZonesMapper.GetInstance();
var timeZones = timeZoneMapper.GetTimeZonesForNumber(geocodedNumber);
Check(timeZones.Count > 0, $"timezone mapper returns at least one zone ({string.Join(", ", timeZones)})");

// --- FindNumbers on free text ---------------------------------------------------------------------

var freeText = "Call us on +1 650-253-0000 or +44 20 7031 3000 for support.";
var matches = phoneUtil.FindNumbers(freeText, "US").ToList();
CheckEqual(2, matches.Count, "FindNumbers finds both numbers in free text");

// --- Extensions package: TryParse / TryParseValid -------------------------------------------------
// PhoneNumbers.Extensions.PhoneNumber (static helper) and PhoneNumbers.PhoneNumber (the data type)
// share a simple name, so with both namespaces `using`'d every reference below must be fully
// qualified - that ambiguity (CS0104) is deliberately exercised here, not avoided.

Check(PhoneNumbers.Extensions.PhoneNumber.TryParse("+16502530000", out PhoneNumbers.PhoneNumber? parsed) && parsed is not null,
    "Extensions.PhoneNumber.TryParse succeeds on a well-formed E164 number");
Check(!PhoneNumbers.Extensions.PhoneNumber.TryParse("not a number", "US", out _),
    "Extensions.PhoneNumber.TryParse fails on garbage input");
Check(PhoneNumbers.Extensions.PhoneNumber.TryParseValid("+16502530000", out _),
    "Extensions.PhoneNumber.TryParseValid succeeds on a valid number");
Check(!PhoneNumbers.Extensions.PhoneNumber.TryParseValid("+1 555-000-0000", out _),
    "Extensions.PhoneNumber.TryParseValid rejects a syntactically-parseable but invalid number");

// --- Extensions package: PhoneNumberConverter under System.Text.Json, AOT-safe -------------------
// Under PublishAot, JsonSerializer.Serialize/Deserialize<T>(value, options) without a
// TypeInfoResolver hits IL2026/IL3050 (reflection-based serialization is disabled) and throws at
// run time. The fix is a source-generated JsonSerializerContext wired in through
// options.TypeInfoResolver - NOT JsonSerializer.Serialize(value, SomeContext.Default.PhoneNumber)
// directly, which bypasses options.Converters (PhoneNumberConverter never runs) and recurses into
// the source-generated serializer instead, overflowing the stack. Routing both calls through
// `options` keeps the custom converter in charge.

var jsonOptions = new JsonSerializerOptions
{
    TypeInfoResolver = SmokeTestJsonContext.Default,
};
jsonOptions.Converters.Add(new PhoneNumbers.Extensions.PhoneNumberConverter());

var numberToSerialize = phoneUtil.Parse("+16502530000", "US");
var json = SerializeNumber(numberToSerialize, jsonOptions);
// Compare the decoded string value, not the raw JSON text: System.Text.Json's default encoder
// escapes '+' (and other ASCII punctuation) with a \uXXXX sequence for HTML-embedding safety, so
// the literal JSON text is not "+16502530000" even though the value it represents is.
CheckEqual("+16502530000", JsonDocument.Parse(json).RootElement.GetString(),
    "PhoneNumberConverter serializes to an E164 JSON string");

var deserialized = DeserializeNumber(json, jsonOptions);
Check(deserialized is not null && phoneUtil.IsValidNumber(deserialized),
    "PhoneNumberConverter deserializes back into a valid PhoneNumber");

// The generic/Type-based JsonSerializer overloads are annotated RequiresUnreferencedCode /
// RequiresDynamicCode unconditionally - the trim/AOT analyzer has no way to know, just from the
// call site, that options.TypeInfoResolver plus a matching entry in options.Converters means the
// actual value work happens in PhoneNumberConverter, not via reflection. That is exactly what was
// verified by hand against the published package before wiring this project into CI, so the
// suppression below is deliberate and narrow, not a blanket opt-out: it only holds because
// PhoneNumberConverter (registered by the caller) takes precedence over the source-generated
// contract for PhoneNumbers.PhoneNumber specifically. A `#pragma warning disable` at the call site
// is not enough here - ilc (the Native AOT compiler) re-runs this same analysis over the published
// IL independently of the Roslyn compiler, and only respects UnconditionalSuppressMessageAttribute
// metadata, not source-level pragmas.
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
    "Serialization goes through the PhoneNumberConverter registered in options.Converters, not reflection.")]
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AotAnalysis", "IL3050", Justification =
    "Serialization goes through the PhoneNumberConverter registered in options.Converters, not reflection.")]
static string SerializeNumber(PhoneNumbers.PhoneNumber number, JsonSerializerOptions options)
    => JsonSerializer.Serialize(number, options);

[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
    "Deserialization goes through the PhoneNumberConverter registered in options.Converters, not reflection.")]
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AotAnalysis", "IL3050", Justification =
    "Deserialization goes through the PhoneNumberConverter registered in options.Converters, not reflection.")]
static PhoneNumbers.PhoneNumber? DeserializeNumber(string json, JsonSerializerOptions options)
    => (PhoneNumbers.PhoneNumber?)JsonSerializer.Deserialize(json, typeof(PhoneNumbers.PhoneNumber), options);

// --- Summary --------------------------------------------------------------------------------------

Console.WriteLine();
Console.WriteLine(failures == 0
    ? "All checks passed."
    : $"{failures} check(s) FAILED.");

return failures == 0 ? 0 : 1;

// JsonSerializerContext must be a partial class at the top level (source-generated); PhoneNumber is
// PhoneNumbers.PhoneNumber, disambiguated from PhoneNumbers.Extensions.PhoneNumber by full name.
[JsonSerializable(typeof(PhoneNumbers.PhoneNumber))]
internal partial class SmokeTestJsonContext : JsonSerializerContext
{
}
