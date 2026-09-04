[![Build status](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/run_all_tests_and_upload_code_coverage.yml/badge.svg?branch=main)](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/run_all_tests_and_upload_code_coverage.yml)
[![codecov](https://codecov.io/gh/twcclegg/libphonenumber-csharp/branch/main/graph/badge.svg)](https://codecov.io/gh/twcclegg/libphonenumber-csharp)
[![NuGet](https://img.shields.io/nuget/dt/libphonenumber-csharp.svg)](https://www.nuget.org/packages/libphonenumber-csharp/)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/twcclegg/libphonenumber-csharp/badge)](https://scorecard.dev/viewer/?uri=github.com/twcclegg/libphonenumber-csharp)

C# port of Google's [libphonenumber library](https://github.com/google/libphonenumber).

The code was rewritten from the Java source mostly unchanged, please refer to the original documentation for sample code and API documentation.

The original Apache License 2.0 was preserved.

> [!TIP]
> **[Try the interactive demo →](https://twcclegg.github.io/libphonenumber-csharp/)** — parse, format, validate, and find phone numbers in your browser. No install required; runs entirely via WebAssembly.

See [this](csharp/README.md) for details about the port.

Phone number metadata is updated in the Google repo approximately every two weeks. This library is automatically updated by a [scheduled github action](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/create_new_release_on_new_metadata_update.yml) to include the latest metadata, usually within a day. See [Metadata updates](#metadata-updates) for how that works and how to run it manually.

See [CHANGELOG.md](CHANGELOG.md) for release history.

## Installation

Run the following command to add this library to your project

```
dotnet add package libphonenumber-csharp
```

Available on NuGet as package [`libphonenumber-csharp`](https://www.nuget.org/packages/libphonenumber-csharp).

Targets `netstandard2.0`, `net8.0` and `net10.0`.

[`libphonenumber-csharp.extensions`](https://www.nuget.org/packages/libphonenumber-csharp.extensions) is an optional companion package with helpers that suit C# better than the ported Java API — `PhoneNumber.TryParse` and `PhoneNumber.TryParseValid` return a `bool` instead of throwing, and `PhoneNumberConverter` is a `System.Text.Json` converter for `PhoneNumber`.

### Trimming and Native AOT

The library is annotated as trim- and AOT-compatible, and the trim/AOT analyzers run as part of its own build. All metadata — including the geocoding, carrier and time zone prefix maps — is compiled to a binary form at build time and embedded in the assembly as compressed resources, so no XML is parsed and no file is read from disk at run time. The [interactive demo](https://twcclegg.github.io/libphonenumber-csharp/) is a Blazor WebAssembly app that runs this library trimmed, in the browser.

### Regex compilation and startup cost

Validation and formatting are driven by regexes built from the bundled metadata. There are thousands of them, and each is built the first time some caller touches that region.

Those metadata regexes are deliberately **not** built with `RegexOptions.Compiled`. Compiling one costs around 1.5 ms of IL-emit and saves roughly 0.044 µs per match, so a pattern has to be matched on the order of 30,000 times before compiling it breaks even — which metadata patterns rarely are, because a workload spread across regions matches each one comparatively few times. Measured end-to-end on net8.0 (total wall time including startup, parse + validate + format), compiling them is 34x slower for 1,000 operations across 245 regions, 3.7x slower for 100,000, and still 1.7x slower at 1,000,000. It wins only for a process concentrating very high volume on one or two regions, and then by 6–20%.

The library's own fixed regexes are a different case and are still compiled: there is a small, fixed number of them, each built once per process and hot for its whole life.

There is no per-call knob for this. If you have measured a workload where compiling the metadata patterns wins — a long-running process concentrating very high volume on one or two regions is the shape where it can — please open an issue with the numbers rather than reaching for internal types.

### Debugging and symbols

Symbols are published to the NuGet.org symbol server as a `.snupkg` alongside each release, with [Source Link](https://learn.microsoft.com/dotnet/standard/library-guidance/sourcelink) wired up — enable symbol server support in your debugger to step into the library.

## Examples

### Parsing a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var e164PhoneNumber = "+44 117 496 0123";
var nationalPhoneNumber = "2024561111";
var smsShortNumber = "83835";
var phoneNumber = phoneNumberUtil.Parse(e164PhoneNumber, null);
phoneNumber = phoneNumberUtil.Parse(nationalPhoneNumber, "US");
phoneNumber = phoneNumberUtil.Parse(smsShortNumber, "US");
```

### Formatting a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+14156667777", "US");
var formattedPhoneNumber = phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.INTERNATIONAL);
var formattedPhoneNumberNational = phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.NATIONAL);

Console.WriteLine(formattedPhoneNumber.ToString()); // +1 415-666-7777
Console.WriteLine(formattedPhoneNumberNational.ToString()); // (415) 666-7777
```

### Check if a phone number is valid
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+14156667777", "US");
var isValid = phoneNumberUtil.IsValidNumber(phoneNumber);

Console.WriteLine(isValid); // true
```

### Get the type of a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+14156667777", "US");
var numberType = phoneNumberUtil.GetNumberType(phoneNumber);

Console.WriteLine(numberType); // PhoneNumberType.FIXED_LINE_OR_MOBILE
```

See [PhoneNumberType.cs](csharp/PhoneNumbers/PhoneNumberType.cs) for the various possible types of phone numbers

### Get the region code for a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+14156667777", null);
var regionCode = phoneNumberUtil.GetRegionCodeForNumber(phoneNumber);

Console.WriteLine(regionCode); // US
```

### Get the location of a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var geocoder = PhoneNumberOfflineGeocoder.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+12128120000", null);
var description = geocoder.GetDescriptionForNumber(phoneNumber, Locale.English);

Console.WriteLine(description); // New York, NY
```

The lookup is entirely offline. Detail varies by region — some yield a city, others only a state or the country name — and non-geographic or invalid numbers return the country name or an empty string. Pass a user region to omit it from the description for local numbers, or use `GetDescriptionForValidNumber` to skip the internal validity check.

### Get the time zones for a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var timeZonesMapper = PhoneNumberToTimeZonesMapper.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+12128120000", null);
var timeZones = timeZonesMapper.GetTimeZonesForNumber(phoneNumber);

Console.WriteLine(string.Join(", ", timeZones)); // America/New_York
```

Returns a `List<string>` of [IANA time zone identifiers](https://www.iana.org/time-zones). For numbers that span multiple time zones (e.g. a country-level lookup), the list will contain more than one entry. Returns `["Etc/Unknown"]` for invalid or unrecognised numbers.

Use `GetTimeZonesForGeographicalNumber` instead if you have already validated the number and want to skip the internal type check.

### Get the carrier name for a phone number
```csharp
using PhoneNumbers;

var phoneNumberUtil = PhoneNumberUtil.GetInstance();
var carrierMapper = PhoneNumberToCarrierMapper.GetInstance();
var phoneNumber = phoneNumberUtil.Parse("+917503397672", null);
var carrierName = carrierMapper.GetNameForNumber(phoneNumber, Locale.English);

Console.WriteLine(carrierName); // Aircel
```

> **Note:** Carrier data reflects the original network allocation. If the country supports mobile number portability, the number may have since moved to a different carrier. Use `GetSafeDisplayName` to return an empty string in those regions.

## Features

* Parsing/formatting/validating phone numbers for all countries/regions of the world.
* GetNumberType - gets the type of the number based on the number itself; able to distinguish Fixed-line, Mobile, Toll-free, Premium Rate, Shared Cost, VoIP and Personal Numbers (whenever feasible).
* IsNumberMatch - gets a confidence level on whether two numbers could be the same.
* GetExampleNumber/GetExampleNumberByType - provides valid example numbers for 218 countries/regions, with the option of specifying which type of example phone number is needed.
* IsPossibleNumber - quickly guessing whether a number is a possible phone number by using only the length information, much faster than a full validation.
* AsYouTypeFormatter - formats phone numbers on-the-fly when users enter each digit.
* FindNumbers - finds numbers in text input
* PhoneNumberToCarrierMapper - looks up the carrier name originally assigned to a mobile or pager number, with locale-aware output and a safe-display mode for regions with mobile number portability.
* PhoneNumberOfflineGeocoder - describes where a number is from, in a requested language, without a network call.
* PhoneNumberToTimeZonesMapper - maps a number to its IANA time zone identifiers.

See [PhoneNumberUtil.cs](csharp/PhoneNumbers/PhoneNumberUtil.cs) for the various methods and properties available.

## Why keep libphonenumber-csharp up to date?
A lot of the functionality depends on updated metadata that is published by the google repository, see example [here](https://github.com/google/libphonenumber/releases/tag/v8.13.55).

This means that if you don't keep the package up to date, methods like `IsValidNumber` will return false for newer numbers that rely on the updated metadata

Therefore, we recommend you keep this nuget package as up to date as possible using automated means (such as dependabot) as metadata changes published by the google repository is frequent, usually a few times a month.

For more information on metadata usage, please refer to the [main repository faq](https://github.com/google/libphonenumber/blob/master/FAQ.md#metadata)

## Running tests locally

```bash
# Every project, every target framework.
dotnet test csharp/PhoneNumbers.slnx

# Faster, and what the pull request check runs.
dotnet test csharp/PhoneNumbers.slnx -p:TargetFrameworks=net10.0
```

The binary metadata the library reads at run time is generated during the build, so a plain
`dotnet build` is all that is needed first — there is no separate generation step.
See [CONTRIBUTING.md](CONTRIBUTING.md) for the build settings that will fail CI if missed.

## Metadata updates

The [`create_new_release_on_new_metadata_update`](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/create_new_release_on_new_metadata_update.yml) workflow runs daily and drives [`lib/github-actions-metadata-update.sh`](lib/github-actions-metadata-update.sh). When the latest `google/libphonenumber` release is newer than the published NuGet package, it copies the upstream `resources/`, regenerates `resources/locale/country_names.txt`, adds a [CHANGELOG.md](CHANGELOG.md) entry for the release, then commits, pushes to a `metadata-update/*` branch and opens a PR against `main` with auto-merge enabled. The push and PR authenticate as the dedicated `libphonenumber-csharp-bot` account (via the `BOT_ACCESS_TOKEN` secret) rather than the default `GITHUB_TOKEN`, since GitHub requires a maintainer to manually approve workflow runs on PRs opened with `GITHUB_TOKEN`. Once that PR's required checks pass and it merges, [`finalize_metadata_release`](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/finalize_metadata_release.yml) tags the merge commit, creates a matching GitHub release, and dispatches the NuGet publish. The changelog entry is written in the same PR rather than afterwards: `main`'s branch-protection ruleset requires every push to go through a PR with no bypass for any actor (including this automation's own bot account), so `finalize_metadata_release` — which only tags an existing commit and calls the Releases API — has no way to push a follow-up commit of its own. The version number is already known at PR-open time (it's copied straight from the upstream tag), so there's nothing to guess.

Before doing any of that it inspects the upstream diff and stops if it contains `.java` or `.proto` files, because changes to the Java sources may need porting by hand and an unattended metadata bump would silently skip them.

### Skipping the java (or proto) check

If you have reviewed the upstream diff and the Java changes don't need porting (for example test-only or build-file changes), you can run the update anyway:

* **From the Actions UI** — open [create_new_release_on_new_metadata_update](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/create_new_release_on_new_metadata_update.yml), click **Run workflow**, and tick **skip_java_check** (and/or **skip_proto_check**). Scheduled runs always leave both unticked.
* **Locally** — pass the flag or set the environment variable:

  ```bash
  bash lib/github-actions-metadata-update.sh --skip-java-check "$GITHUB_TOKEN"
  # or
  SKIP_JAVA_CHECK=true bash lib/github-actions-metadata-update.sh "$GITHUB_TOKEN"
  ```

  `--skip-proto-check` / `SKIP_PROTO_CHECK` work the same way, and `--help` lists every option.

Skipping a check means the release ships upstream metadata from a version whose Java-side changes were not ported, so read the upstream diff first — the script prints the offending filenames before it stops.

### Dry runs

`--dry-run` (or the **dry_run** workflow input) runs every read-only step — version lookups, repository checks, the upstream diff gates, the upstream clone — reports what a real run would do, and stops before the first change to the working tree. Nothing is copied, generated, committed, pushed or released, no token is required, and the clean-`main` requirement is relaxed to a warning so it works from a feature branch:

```bash
# what would tonight's scheduled run do?
bash lib/github-actions-metadata-update.sh --dry-run
```

`UPSTREAM_TAG` and `DEPLOYED_VERSION` override the two version lookups, which lets you replay any historical release pair — useful for seeing how a given release trips the gates:

```bash
UPSTREAM_TAG=v9.0.33 DEPLOYED_VERSION=9.0.32 \
  bash lib/github-actions-metadata-update.sh --dry-run
```

### Running it against a fork

Nothing about the target repository is hard-coded. The script commits and pushes through whatever checkout it runs in, and takes the repository to release from `GITHUB_REPOSITORY` — set automatically by GitHub Actions, and otherwise derived from the `origin` remote. So a fork releases to itself, and the dry-run summary names the repository it would publish to. `UPSTREAM_REPOSITORY` (default `google/libphonenumber`) and `NUGET_PACKAGE_ID` (default `libphonenumber-csharp`) are overridable the same way.

### Automated triage of metadata issues

A large share of the issues filed here turn out to be reports about Google's phone number metadata itself (an unrecognized prefix, an outdated numbering plan) rather than a bug in this port's code — see the checklist in [`bug_report.md`](.github/ISSUE_TEMPLATE/bug_report.md). The [`triage_metadata_issues`](.github/workflows/triage_metadata_issues.yml) workflow uses the GitHub Copilot CLI, grounded in [`.github/triage/metadata_examples.md`](.github/triage/metadata_examples.md), to spot these on issue creation, closes them with a comment pointing to [google/libphonenumber](https://github.com/google/libphonenumber), and labels them `metadata`. Every closure appends to that examples file, so the classifier keeps learning from real outcomes instead of drifting from a fixed prompt.

## Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md)

## Donations

[![Buy me a beer](https://raw.githubusercontent.com/twcclegg/libphonenumber-csharp/main/bmacButton.png)](https://www.buymeacoffee.com/tclegg)
