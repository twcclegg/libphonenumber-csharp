# AGENTS.md

Guidance for coding agents (Claude Code, claude.ai/code, and others) working in this repository.
Keep this file short: it is loaded into every session. Task-specific detail lives in the skills
under `.claude/skills/` (see the index at the end), which are loaded only when relevant.

## What this repo is

C# port of Google's [libphonenumber](https://github.com/google/libphonenumber). The code was
rewritten from the Java source mostly unchanged — **when in doubt about behaviour, the Java
upstream is the source of truth.** `resources/` is a verbatim copy of upstream's metadata, synced
automatically every ~two weeks; the library compiles it to binaries at build time and embeds them.

## Repository layout

- `csharp/Directory.Build.props` — settings shared by every project (`LangVersion`,
  `TreatWarningsAsErrors`, `NoWarn` baseline, NuGet audit, Source Link, `.snupkg`). Set things
  here, not per csproj. `global.json` pins the SDK (`10.0.100`, `latestFeature` roll-forward).
- `csharp/Directory.Packages.props` — Central Package Management; every package version lives here.
- `csharp/PhoneNumbers/` — main library (NuGet `libphonenumber-csharp`), `netstandard2.0;net8.0;net10.0`.
- `csharp/PhoneNumbers.Test/` — xUnit tests ported from Java, `net8.0;net10.0`.
- `csharp/PhoneNumbers.Extensions/` (+ `.Test/`) — NuGet `libphonenumber-csharp.extensions`:
  C#-idiomatic helpers with no Java counterpart.
- `csharp/PhoneNumbers.MetadataBuilder/` — build-time tool that turns `resources/` into per-region
  binaries; source-links a few library files so it cannot cycle with the main project.
- `csharp/PhoneNumbers.PerformanceTest/` — BenchmarkDotNet harness.
  `csharp/PhoneNumbers.BenchmarkTools/` — CI-only comparison tool (not in the solution).
- `csharp/PhoneNumbers.Demo/` (+ `.Tests/`) — Blazor WASM demo on GitHub Pages; also proves the
  library works trimmed. Has its own `AGENTS.md`.
- `csharp/PhoneNumbers.Fuzz/` — SharpFuzz/libFuzzer target, run weekly (not in the solution).
- `resources/` — upstream XML metadata plus `geocoding/`, `carrier/`, `timezones/`, & protos;
  `resources/locale/country_names.txt` is generated here by `lib/DumpLocale.java`.
- `.gitattributes` — nothing but `linguist-generated` markings on the data in that tree; the
  protos and READMEs are exempt. See the hard rules below.
- `lib/` — bash automation for the metadata sync, changelog and release. The sync runs daily and
  opens a `metadata-update/*` PR with auto-merge off for a maintainer to review and merge; a later
  run that finds it still open regenerates the branch and arms auto-merge as a backstop.
- `docfx/` — DocFX config for the generated API reference site, deployed alongside the demo under
  `/docs/` in the same `deploy-demo.yml` run. Build it with `docfx/build.sh`, never `docfx`
  directly — the script copies `docs/*.md` in as articles and rewrites their repo-relative links.
  The two sites share one sidebar: `docfx/template/layout/_master.tmpl` (DocFX 2.78.5's master
  layout, vendored — re-diff it when bumping docfx) renders the demo's brand and its
  Overview/Features links, `docfx/toc.yml` holds the Reference links, and
  `docfx/template/public/main.css` ports the demo's design tokens and sidebar styling. A new demo
  page goes in both the demo's `MainLayout` page table and that template (`build.sh` fails when
  they differ). Both sidebars collapse to an icon-only rail on desktop; the two sites share
  localStorage (same origin), so the theme (DocFX's `theme` key) and the collapsed state
  (`sidebar`) carry across, and the docs apply both before first paint. `docs_preview.yml` uploads the
  rendered site as a PR artifact.
- `assets/brand/` — the logomark and favicon SVGs, shared by both sites: docfx maps them to
  `images/` (`docfx/docfx.json`) and the demo links them into its `wwwroot/` (a `Content` item in
  `PhoneNumbers.Demo.csproj`). Kept at the repo root rather than inside one site so the two can't
  drift. Changing them rebuilds both — see the `assets/brand/**` path filters in the workflows.
- `docs/api-differences-from-java.md` — the deliberate API-shape divergences from Java.

## Common commands

Run from the repository root.

```bash
dotnet restore csharp
dotnet build csharp --no-restore
dotnet test csharp/PhoneNumbers.slnx -p:TargetFrameworks=net10.0   # what the PR check runs
dotnet test csharp/PhoneNumbers.slnx                                # every TFM
dotnet test csharp/PhoneNumbers.Test --filter "FullyQualifiedName~TestPhoneNumberUtil.TestParseNationalNumber"
```

`dotnet build` runs the metadata pipeline itself; there is no separate generation step.

## Hard rules

- **`resources/` is 16 MB of generated upstream data: don't hand-edit it, and don't read it.**
  Fixes go upstream — anything changed here is overwritten by the next sync, including the generated
  `locale/country_names.txt`. The only reason to open the tree is the schema, which is the two
  `.proto` files (22 KB); the XML and the prefix tables are off limits. Why a number validates, types
  or formats as it does is **not** a reason — the XML is a build input compiled into the embedded
  binary metadata, so it only restates what a test tells you in seconds, and an upstream report is
  settled on the numbering authority's published plan, never on what the XML says (see the
  `diagnosing-number-behaviour` skill). If you do need one rule, extract it:
  `sed -n '/<territory id="GB"/,/<\/territory>/p' resources/PhoneNumberMetadata.xml` is ~500 lines
  of 32,000, against 957 KB whole or up to 3.8 MB for a geocoding table.
- **The two metadata-derived tables.** `ShortNumbersRegionCodeSet.cs` is off limits to hand edits
  like the data it comes from. `CountryCodeToRegionCodeMap.cs` reads and is named like a generated
  file, but nothing regenerates it — its header still says "todo make this file automatically
  generated" and `lib/github-actions-metadata-update.sh` treats a change to it as hand-written
  content, so edit it by hand when you need to. Neither is marked `linguist-generated` (#478); the
  data in `resources/` is, which is a display decision and not a reason to read a collapsed diff as
  an empty one.
- **Adding a public member to `csharp/PhoneNumbers/` needs explicit sign-off from the user, as its
  own decision.** Package validation only catches breaks against the published baseline — never
  additions, so nothing automated will object. "It matches an existing pattern" is not permission —
  `IMetadataLoader`/`SetMetadataLoader` and `PrewarmRegionsAsync` were added on exactly that
  reasoning and both were regretted. Ask, every time. `PhoneNumbers.Extensions` is exempt and
  exists to grow.
- **Never build a metadata-derived regex with `RegexOptions.Compiled`.** It shipped as a regression
  three times; `TestPhoneRegex.MetadataPatternsAreNeverCompiled` guards it. Go through
  `RegexCache` / `PhoneRegex` rather than constructing `Regex` on a call path.
- **Warnings are errors**, including the trim/AOT analyzers (`IsAotCompatible` on the modern TFMs):
  no reflection or dynamic code reachable from the public API.
- **Package versions go in `Directory.Packages.props` only** — an inline `Version` on a
  `PackageReference` fails restore (`NU1008`). `nuget.config` pins nuget.org as the only source.
  There are deliberately no `packages.lock.json` files: every version is exact already, and a lock
  file would only couple the build to the SDK's implicit package versions. Don't add one.
- **The build must be reproducible.** CI packs twice and fails if any assembly differs by hash, so
  a build property that embeds a timestamp, an absolute path or a random seed breaks CI with nothing
  to point at. See the `changing-ci-workflows` skill.
- **No JavaScript in `lib/`.** CI/build tooling is bash (+`jq`) or a small C# console tool.
- **A PR's title and body describe its final state, never its editing history.** Commit subjects use
  conventional prefixes (`feat:`, `fix:`, `perf:`, `ci:`, `test:`, `docs:`, `refactor:`).

## Architecture in brief

- `PhoneNumberUtil.GetInstance()` is the entry point. Region metadata loads lazily through
  `MetadataSource` + `IMetadataLoader` (`EmbeddedResourceMetadataLoader` reads the embedded
  binaries under `PhoneNumbers.metadata.*`). `BuildMetadataFromXml.cs` survives for build time and
  the legacy `PhoneNumberUtil(Stream)` constructor only. `LocaleNames` reads country display names
  one country at a time; `LocaleData` exposes the whole table only for callers outside the library.
- `PhoneNumberUtil` is a partial class split by TFM: `PhoneNumberUtil.net.cs` (modern BCL) and
  `PhoneNumberUtil.netstandard.cs` (fallbacks). Every public signature must exist on all three TFMs.
- Subsystems mirror Java types of the same name: `AsYouTypeFormatter`, `PhoneNumberMatcher`,
  `ShortNumberInfo`, `PhoneNumberOfflineGeocoder` / `PhoneNumberToCarrierMapper` /
  `PhoneNumberToTimeZonesMapper` (backed by `AreaCodeMap` prefix maps, stored via
  `AreaCodeMapStorageStrategy` — `DefaultMapStorage` or `FlyweightMapStorage`).
- Parsing and formatting are allocation-light on purpose: spans and slices over substrings and
  `Match` objects; lookup tables built once into frozen collections. Measure hot-path changes with
  the benchmark harness rather than reasoning about them.
- Nullable reference types are on everywhere except `netstandard2.0`; annotate new code regardless.
- CI is GitHub Actions. The default runner is `ubuntu-24.04-arm`; a handful of jobs run x64 on
  `ubuntu-latest`; there are no Windows or macOS runners. PRs run
  `build_and_run_unit_tests_linux.yml` (net10.0, plus the reproducibility check),
  `run_all_tests_and_upload_code_coverage.yml` (every TFM, Codecov) and `codeql.yml`. Releases are
  tag-driven (`vX.Y.Z` → `publish_nuget.yml`, OIDC trusted publishing, no API key).

## Skills

Task-specific procedures live in `.claude/skills/<name>/SKILL.md`. Claude Code loads them on
demand; other agents should read the matching file before starting one of these tasks.

| Task | Skill |
| --- | --- |
| "Number X in region Y returns Z" — is it metadata or a port bug? | `diagnosing-number-behaviour` |
| Port a fix, feature or test from the Java upstream | `porting-upstream-changes` |
| Add, change or remove public API; package-validation or TFM-parity failures | `changing-public-api` |
| Decide where a test goes and how to write it (synthetic vs real metadata, FsCheck, fuzz) | `writing-tests` |
| Parse/format/match performance, benchmarks, PR benchmark comments | `tuning-hot-paths` |
| How `resources/` becomes embedded binaries; stale metadata; CS2012 / obj races | `building-embedded-metadata` |
| The upstream metadata sync, `metadata-update/*` PRs, releases and the changelog | `syncing-upstream-metadata` |
| Anything under `.github/workflows/`, Dependabot, Scorecard, CI tooling | `changing-ci-workflows` |
| The Blazor demo: styling, accessibility, bUnit tests, browser verification | `changing-demo-ui` |
