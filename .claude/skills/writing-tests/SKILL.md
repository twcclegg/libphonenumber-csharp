---
name: writing-tests
description: Decide where a test belongs and how to write it for this repository — ported xUnit tests against synthetic metadata, regression cases against the real shipped metadata, hostile-input and FsCheck property tests, reproducing a fuzzer crash, and running a single test. Use when adding or fixing any test under csharp/, when a test fails after a metadata sync, or when asked "where should this test go". Demo (bUnit) tests are covered by the changing-demo-ui skill instead.
---

# Writing tests

The suite runs against **two different metadata sets**, and putting a test in the wrong one makes it
prove nothing. Decide that first.

## Which metadata does the test need?

| You are… | Metadata | Where it goes |
| --- | --- | --- |
| Porting a Java test | Synthetic `PhoneNumberMetadataForTesting.xml` | The class mirroring the Java test file (`TestPhoneNumberUtil.cs`, `TestAsYouTypeFormatter.cs`, `TestPhoneNumberMatcher.cs`, `TestShortNumberInfo.cs`, …) |
| Guarding a real number / region / crash | Real shipped metadata via `GetInstance()` | `TestPublicApiRobustness.cs` (curated inputs) or `TestPhoneNumberProperties.cs` (FsCheck) |
| Asserting every region's example numbers still parse | Real | `TestExampleNumbers.cs` |
| Testing a reader/writer of the binary format | Real | `TestBuildMetadataFromBin.cs`, `TestBuildPrefixMapFromBin.cs` |
| Testing the `ResourcePack` container itself | None | `TestResourcePack.cs` |
| Testing the trimmed-data opt-out or its build-time couplings | Real | `TestTrimmedDataDiagnostics.cs` |

**Synthetic metadata** classes carry `[Collection("TestMetadataTestCase")]` and take
`TestMetadataTestCase.PhoneUtil` — hand-built fake regions whose rules exist to exercise code
paths. A real-world number parked there tells you nothing about how the library behaves for users.

**Real metadata** classes call `PhoneNumberUtil.GetInstance()` (and the geocoder / carrier / time
zone `GetInstance()`s). This is what ships and where the awkward regions live; an issue reported by a
user is reproduced and guarded here.

## Regression and hostile-input cases

`TestPublicApiRobustness.cs` drives each public entry point over an array of hostile strings inside
a `[Fact]`, deliberately not `[Theory]`/`[MemberData]`: xunit serialises theory arguments for
discovery and some inputs (lone surrogates, NUL) do not survive that. Add a new hostile input to the
existing arrays, or a new `[Fact]` for a new entry point, asserting that the only exception that
escapes is the documented one (`NumberParseException` for parsing). This pair of files has already
caught an unbounded `stackalloc` and a `KeyNotFoundException` the well-formed-input suite missed.

`TestPhoneNumberProperties.cs` is the FsCheck counterpart: `[Property(MaxTest = …)]` methods over
generated input asserting invariants (`ValidNumbersRoundTripThroughE164`,
`ValidNumbersAreAlsoPossible`, `NormalizeIsIdempotent`, `IsNumberMatchIsSymmetric`, …). **Keep the
`using FsCheck;` / `using FsCheck.Xunit;` lines** even if a refactor stops needing them — the OSSF
Scorecard fuzzing check greps for them and the score drops silently without.

A crash from the weekly fuzzer (`fuzz.yml`, `csharp/PhoneNumbers.Fuzz/`) reproduces with
`dotnet fuzz-out/PhoneNumbers.Fuzz.dll <crash-file>`; its README has the full recipe. Turn every
finding into a `TestPublicApiRobustness` case so it stays fixed.

## After a metadata sync breaks a test

A test that starts failing on a `metadata-update/*` PR is almost always right to fail: a region's
example number or format changed upstream and the ported assertion is stale. Update the *test* to
the new behaviour; never edit `resources/` to make it pass. If the failing test uses the synthetic
metadata it cannot be the sync — look for a code change.

## Running

```bash
dotnet test csharp/PhoneNumbers.Test --filter "FullyQualifiedName~TestPhoneNumberUtil.TestParseNationalNumber"
dotnet test csharp/PhoneNumbers.Test --filter "FullyQualifiedName~TestPublicApiRobustness"
dotnet test csharp/PhoneNumbers.slnx -p:TargetFrameworks=net10.0     # the PR check
dotnet test csharp/PhoneNumbers.slnx                                  # adds net8.0
```

Tests never run on `netstandard2.0`; a broken fallback in `PhoneNumberUtil.netstandard.cs` only
shows up in `dotnet build csharp`, so run that too when a change touches it.

`csharp/coverlet.runsettings` excludes the generated `CountryCodeToRegionCodeMap` and the
protobuf-style `*Builder` nested types from coverage; don't write tests whose only purpose is to
lift the percentage on those.

## Conventions

- Ported tests keep the Java test's name and order so the next port can diff against upstream.
- New non-ported tests are named for the behaviour (`ParseFailsOnlyWithNumberParseException`), one
  concept per test, no snapshots.
- Use the `RegionCode` constants in `csharp/PhoneNumbers.Test/RegionCode.cs` rather than typing
  region strings by hand.
