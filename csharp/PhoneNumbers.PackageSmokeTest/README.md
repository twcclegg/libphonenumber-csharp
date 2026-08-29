# Package smoke test

Restores `libphonenumber-csharp` and `libphonenumber-csharp.extensions` from nuget.org as ordinary
`PackageReference`s (never the in-repo source - see the comment in the `.csproj`) and runs a small
battery of checks against them: parse/format/validate across a few regions, `AsYouTypeFormatter`,
`ShortNumberInfo`, the offline geocoder/carrier/timezone mappers (the embedded binary metadata is
the actual point - nothing else in this repo restores the packages as a consumer would and reads
that data back out of them), `PhoneNumberUtil.FindNumbers`, and the extensions package's
`TryParse`/`TryParseValid`/`PhoneNumberConverter`.

`PhoneNumbers.csproj` sets `IsAotCompatible`, which catches static trim/AOT analyzer warnings at
library-build time. It never restores the packed `.nupkg` as a consumer would or runs a published
executable, so it cannot catch a problem in the packed output itself, or one that only appears once
a real app's own code (not the library's) is compiled or AOT-analyzed against the library's public
surface - the JSON serialization pattern below is exactly such a case.

This project is not in `PhoneNumbers.slnx` - see the comment in the `.csproj`.

## Running it locally

The version to test is a required parameter, not something pinned in the project - pass it with
`-p:PackageUnderTestVersion=X.Y.Z` or the `PACKAGE_UNDER_TEST_VERSION` environment variable. It
must already be published on nuget.org.

```bash
# Normal managed run.
dotnet run --project csharp/PhoneNumbers.PackageSmokeTest -c Release \
  -p:PackageUnderTestVersion=9.0.38 -- 9.0.38

# Native AOT: publish, then execute the native binary directly.
dotnet publish csharp/PhoneNumbers.PackageSmokeTest -c Release -p:PublishAot=true \
  -p:PackageUnderTestVersion=9.0.38 -o aot-out
PACKAGE_UNDER_TEST_VERSION=9.0.38 ./aot-out/PhoneNumbers.PackageSmokeTest
```

Either mode exits non-zero if any check fails, and prints `PASS`/`FAIL` per check plus a summary
line.

## Why the JSON checks look the way they do

Under `PublishAot`, `JsonSerializer.Serialize`/`Deserialize` without a `JsonSerializerContext` throw
`InvalidOperationException: Reflection-based serialization has been disabled`. The fix is a
`[JsonSerializable(typeof(PhoneNumbers.PhoneNumber))] partial class : JsonSerializerContext` wired
in via `options.TypeInfoResolver`, with calls routed through the `options`-taking overloads
(`JsonSerializer.Serialize(value, options)`, `JsonSerializer.Deserialize(json, typeof(T), options)`)
rather than `JsonSerializer.Serialize(value, SomeContext.Default.PhoneNumber)` directly - the latter
bypasses `options.Converters` entirely, so `PhoneNumberConverter` never runs and the source-generated
serializer recurses into `PhoneNumber`'s own fields instead, overflowing the stack.

Those `options`-taking overloads are still annotated `RequiresUnreferencedCode`/`RequiresDynamicCode`
unconditionally (the analyzer cannot see that the registered converter is what actually runs), so
`Program.cs` isolates them behind two local functions carrying a narrow, justified
`UnconditionalSuppressMessageAttribute` - a `#pragma warning disable` at the call site is not enough
here, because `ilc` (the Native AOT compiler) re-runs the same trim/AOT analysis over the published
IL independently of the Roslyn compiler and only respects the attribute, not source-level pragmas.

## In CI

`.github/workflows/package_smoke_test.yml` runs both modes after a real `publish_nuget` run, and can
also be dispatched on demand against any already-published version.
