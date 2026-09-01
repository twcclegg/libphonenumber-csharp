# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

C# port of Google's [libphonenumber](https://github.com/google/libphonenumber). Code was rewritten from the Java source mostly unchanged — when in doubt about behavior, the Java upstream is the source of truth.

The library tracks upstream metadata releases (~every two weeks) via the `create_new_release_on_new_metadata_update.yml` GitHub Action; see commits like "feat: automatic upgrade to vX.Y.Z" for what those changes look like. The action stops when the upstream diff touches `.java` or `.proto` files, since those may need porting by hand; README.md ("Metadata updates") documents the dry-run and check-override options.

## Repository layout

- `csharp/Directory.Build.props` — build settings shared by every project: `LangVersion`, `TreatWarningsAsErrors` (so warnings break the build), the repo-wide `NoWarn` baseline, repository metadata, the NuGet audit settings, symbol packaging (`.snupkg`), and the reproducible-build/Source Link switches. Set things here rather than per-csproj.
- `csharp/Directory.Packages.props` — Central Package Management. Every package version lives here; a `PackageReference` carrying its own `Version` is an error (`NU1008`).
- `csharp/PhoneNumbers/` — main library (NuGet `libphonenumber-csharp`). Multi-targets `netstandard2.0;net8.0;net10.0`.
- `csharp/PhoneNumbers.Test/` — xUnit tests, ported from the Java tests. Multi-targets `net8.0;net10.0`.
- `csharp/PhoneNumbers.Extensions/` — separate NuGet (`libphonenumber-csharp.extensions`) with C#-idiomatic helpers that don't exist in the Java library.
- `csharp/PhoneNumbers.Extensions.Test/` — xUnit tests for the Extensions package.
- `csharp/PhoneNumbers.PerformanceTest/` — BenchmarkDotNet harness.
- `csharp/PhoneNumbers.BenchmarkTools/` — CI-only console tool that compares two `PhoneNumbers.PerformanceTest` JSON result sets (Welch's t-test via MathNet.Numerics) and writes the significant differences for `run_performance_tests.yml`/`post_performance_test_comment.yml`; paired with `lib/fail-on-benchmark-regression.sh`. Not in the solution; run directly via `dotnet run --project`.
- `csharp/PhoneNumbers.MetadataBuilder/` — build-time tool that converts XML metadata + geocoding/timezone text files into per-region binary files. Source-links a small set of files from `PhoneNumbers/` so it doesn't depend on (and can't cycle with) the main library at build time.
- `csharp/PhoneNumbers.Demo/` — Blazor WebAssembly demo, deployed to GitHub Pages by `deploy-demo.yml`. Doubles as proof the library works trimmed under WASM.
- `csharp/PhoneNumbers.Demo.Tests/` — bUnit tests for the demo.
- `csharp/coverlet.runsettings` — keeps the generated data tables out of coverage instrumentation; passed by the coverage workflow.
- `resources/` — XML metadata (`PhoneNumberMetadata.xml`, `ShortNumberMetadata.xml`, `PhoneNumberAlternateFormats.xml`, `PhoneNumberMetadataForTesting.xml`), plus `geocoding/`, `carrier/`, `timezones/`. **These are copied verbatim from upstream** (`locale/` is the exception: it is generated from the local jdk by `DumpLocale.java`) — do not hand-edit. The library no longer reads them at runtime: the build pipeline emits binary equivalents under `obj/metadata/`, `obj/geocoding/`, `obj/timezones/` which are embedded into the published assembly.
- `lib/github-actions-metadata-update.sh` + `lib/DumpLocale.java` — automation that pulls upstream resources and regenerates `resources/locale/country_names.txt`.
- `csharp/PhoneNumbers.Fuzz/` — SharpFuzz/libFuzzer target for the parsing surface, run weekly by `fuzz.yml`. Not in the solution; see its README and the note in its csproj.

## Common commands

All commands below run from the repository root, which is what the `csharp/…` paths in them assume.

Metadata is built from XML/text into per-region binary files at build time by
`csharp/PhoneNumbers.MetadataBuilder/` (see the `BuildBinaryMetadata`,
`BuildGeocodingBins`, and `BuildTimezoneBin` MSBuild targets in `PhoneNumbers.csproj`).
You don't need to run anything by hand — `dotnet build` invokes the tool. At run time those
binaries are read straight out of the assembly's embedded resources (gzip-compressed) via
`IMetadataLoader` / `BuildPrefixMapFromBin` — no XML or text resource is parsed, and no zip
archive or file on disk is involved.

Build / test:

```bash
dotnet restore csharp
dotnet build csharp --no-restore
# Full test matrix:
dotnet test csharp/PhoneNumbers.slnx
# Faster: net10.0 only (matches the Linux PR check):
dotnet test csharp/PhoneNumbers.slnx -p:TargetFrameworks=net10.0
```

Run a single test (xUnit filter syntax):

```bash
dotnet test csharp/PhoneNumbers.Test --filter "FullyQualifiedName~TestPhoneNumberUtil.TestParseNationalNumber"
dotnet test csharp/PhoneNumbers.Test --filter "FullyQualifiedName~TestPhoneNumberUtil"   # whole class
```

Pack the NuGet packages (mirrors `publish_nuget.yml`; the workflow adds `-p:VersionPrefix=<tag minus "v">`):

```bash
dotnet pack -c Release csharp/PhoneNumbers
dotnet pack -c Release csharp/PhoneNumbers.Extensions
```

Benchmarks:

```bash
cd csharp/PhoneNumbers.PerformanceTest
dotnet run -c Release --framework net10.0 -- --filter "*"
dotnet run -c Release --framework net10.0 -- --filter "*PhoneNumberWorkflowBenchmark*"
```

## Architecture notes that span files

- **Singleton + metadata loading.** `PhoneNumberUtil.GetInstance()` is the entry point. Region/country metadata is lazily loaded via `MetadataSource` + `IMetadataLoader` (default impl: `EmbeddedResourceMetadataLoader`, which reads per-region binary files generated at build time by `PhoneNumbers.MetadataBuilder` and embedded under `PhoneNumbers.metadata.<prefix>_<region-or-cc>`). The XML parser (`BuildMetadataFromXml.cs`) is still used at build time and by the legacy `PhoneNumberUtil(Stream)` constructor for consumers loading custom XML, but is no longer on the default load path.
- **Generated files.** `CountryCodeToRegionCodeMap.cs` is generated; don't hand-edit it. `resources/locale/country_names.txt` is generated too, by `javac DumpLocale.java && java DumpLocale > resources/locale/country_names.txt` (see `lib/github-actions-metadata-update.sh`); the build turns it into per-country binaries that `LocaleNames` reads one country at a time, and `LocaleData` exposes the whole table only for callers outside the library.
- **Partial-class TFM split.** `PhoneNumberUtil.cs` is a `partial class` with framework-specific halves: `PhoneNumberUtil.net.cs` (modern .NET) and `PhoneNumberUtil.netstandard.cs` (netstandard2.0 fallbacks). When adding APIs that use newer BCL features, put the polyfill on the netstandard side.
- **Subsystems and their entry types** (each ports a Java counterpart of the same name):
  - `PhoneNumberUtil` — parse / format / validate.
  - `AsYouTypeFormatter` — incremental formatting.
  - `PhoneNumberMatcher` / `PhoneNumberMatch` — find numbers in free text.
  - `ShortNumberInfo` — short codes / SMS shortcodes (separate metadata file).
  - `PhoneNumberOfflineGeocoder`, `PhoneNumberToCarrierMapper`, `PhoneNumberToTimeZonesMapper` — geo / carrier / tz lookups, backed by the binary prefix maps embedded at build time.
  - `AreaCodeMap` + `AreaCodeMapStorageStrategy` / `DefaultMapStorage` / `FlyweightMapStorage` — prefix → string lookup used by geocoder/carrier/timezone mappers.
- **Regex caching.** Use `RegexCache` / `PhoneRegex` rather than constructing `Regex` ad hoc on hot paths — phone parsing is regex-heavy and the cache matters for throughput.
- **Nullable reference types** are enabled on every target except `netstandard2.0` (see csproj `Condition`). New code should still annotate.
- **Trim/AOT clean.** `IsAotCompatible` is set on the modern TFMs, so the trim, single-file and AOT analyzers run during the build and their warnings are errors. Keep reflection and dynamic code off any path reachable from the public API — the Blazor WASM demo depends on this too.
- **Hot-path allocation.** Parsing and formatting are deliberately allocation-light: match against spans and slices instead of materialising substrings or `Match` objects, and build lookup tables once into frozen collections. Measure a hot-path change with `PhoneNumbers.PerformanceTest` rather than reasoning about it — `run_performance_tests.yml` benchmarks the base commit on the same runner and posts a comparison to the PR.

## Working with this port vs. upstream Java

- When fixing parsing/validation bugs, first check the upstream Java equivalent (`java/` in `google/libphonenumber`) — fixes that already exist upstream should be ported faithfully rather than reinvented. File and method names match closely (`PhoneNumberUtil.java` ↔ `PhoneNumberUtil.cs`, `BuildMetadataFromXml.java` ↔ `BuildMetadataFromXml.cs`, etc.).
- **Don't change `resources/*.xml` to fix metadata bugs.** Those changes belong upstream; here they will be overwritten on the next automated metadata sync.
- The XML-vs-protobuf and `CharSequence` divergences are documented in `csharp/README.md` ("Known Issues") — be aware they exist if you see API shape differences from Java.
- **Adding a new public member to `PhoneNumbers` (the main library) is treated as seriously as removing one — never do it silently.** `EnablePackageValidation` only catches breaking removals/signature changes against the baseline; it has nothing to say about new members, so a new public type/method/property can ship with zero automated pushback. Before adding one, stop and get explicit sign-off from the user in that conversation, as its own decision — a task like "fix this perf issue" does not by itself authorize a new public member as a side effect, no matter how well it matches an existing pattern. `IMetadataLoader`/`MetadataManager.SetMetadataLoader` and `PhoneNumberUtil.PrewarmRegionsAsync` were both added exactly this way — justified in the moment by "matches an existing precedent" reasoning — and both were later regretted. That reasoning is not itself permission; ask anyway, break-glass style. `PhoneNumbers.Extensions` is exempt from this — it exists specifically to grow with C#-idiomatic helpers beyond Java's API, so add to it freely.

## CI and release

- CI is GitHub Actions only, on `ubuntu-24.04-arm`. There are no Windows runners.
- PRs trigger `build_and_run_unit_tests_linux.yml` (net10.0 only), `run_all_tests_and_upload_code_coverage.yml` (whole solution, every TFM, uploads to Codecov), and `codeql.yml`. Two more are path-filtered: `run_performance_tests.yml` (library or benchmark changes, with `post_performance_test_comment.yml` posting the result) and `build_and_run_demo_tests.yml` (demo changes). `scorecard.yml` runs on `main` and on branch-protection changes.
- **Restore is not locked.** There are no `packages.lock.json` files: every version is exact in `Directory.Packages.props`, so restore already resolves the same graph, and a lock file would only couple the build to the SDK's implicit package versions. `NuGetAudit` covers advisories; `nuget.config` pins the single source.
- `global.json` pins the SDK to 10.0.100 with `latestFeature` roll-forward, and CI verifies the build is reproducible. `EnablePackageValidation` is on for both packable projects, so a change that breaks the public surface — or that makes it inconsistent across TFMs — fails the build rather than shipping.
- Releases are tag-driven: a `vX.Y.Z` tag fires `publish_nuget.yml`, which packs both projects at the tag's version and pushes them, each with its `.snupkg`, to nuget.org via trusted publishing (GitHub OIDC, `NuGet/login`) — there is no API key secret. Metadata-bump tags are created by `finalize_metadata_release.yml` once the PR opened by `create_new_release_on_new_metadata_update.yml` merges.
- **No JavaScript in `lib/`.** JavaScript belongs only in `csharp/PhoneNumbers.Demo/` (the Blazor WASM demo's own web assets, if any) — a handful of `lib/*.js` CI helper scripts were added as an incidental implementation detail of unrelated work and later ported to bash or C# (see `csharp/PhoneNumbers.BenchmarkTools/`, `lib/fail-on-benchmark-regression.sh`, `lib/update-changelog.sh`) once that was noticed. Write new CI/build tooling in bash (simple text/JSON-via-`jq` logic) or a small C# console tool (anything needing real data structures, math, or a library) instead.
- **A PR's title and description describe its current, final state — never its own editing history.** A short-lived PR doesn't need "originally did X, then on reflection switched to Y"; by the time it merges, only what it actually does matters, and self-narrated history just makes the description harder to read for no benefit. This is unrelated to `CHANGELOG.md`, which does need to describe how `main` changed release over release — that's about the codebase's history, not one PR's own drafting process. If a PR's approach changed after review or discussion, force-push the branch (or amend, on a branch nobody else is building on) so the diff and description both reflect only the final approach, and update the title/body to match rather than layering a "revised" section on top.
