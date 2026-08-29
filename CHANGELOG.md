# Changelog

All notable changes to `libphonenumber-csharp` and its companion `libphonenumber-csharp.extensions`
package are documented here.

**This project's release cadence is unusual for a changelog.** Nearly every release exists purely to
track a new upstream [google/libphonenumber](https://github.com/google/libphonenumber) metadata
release (about every two weeks), cut automatically by
[`create_new_release_on_new_metadata_update.yml`](.github/workflows/create_new_release_on_new_metadata_update.yml).
Those releases get a single mechanical entry here, added by
[`lib/github-actions-metadata-update.sh`](lib/github-actions-metadata-update.sh) in the very same
commit that syncs the metadata — nothing here is guessed ahead of time, since the version number is
already known: it's copied straight from the upstream tag being synced. Long runs of consecutive
metadata-only releases are condensed into a single ranged entry so this file stays readable; every
release still has a matching entry, so no version number is skipped.

Entries that describe an actual code change — a bug fix, a new API, a breaking change — were written
by hand at review time. For the exhaustive per-PR detail behind any release (including the routine
ones), see its [GitHub Release](https://github.com/twcclegg/libphonenumber-csharp/releases), whose
notes are auto-generated from merged PR titles.

Format loosely follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning tracks
whatever upstream `google/libphonenumber` release this port is synced to, not strict
[SemVer](https://semver.org/) — see
["Why keep libphonenumber-csharp up to date?"](README.md#why-keep-libphonenumber-csharp-up-to-date)
in the README for why staying current matters even for patch-looking bumps.

<!-- next-entry -->

## [v9.0.38](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.37...v9.0.38) - 2026-08-28

A larger-than-usual release: the metadata-sync automation itself was reworked, alongside a batch of
Extensions API additions and hardening fixes.

- Added (Extensions): `PhoneNumber.TryParse`/`TryParseValid` 2-argument overloads matching the
  conventional .NET `TryParse` shape; `PhoneNumberTypeConverter` for `TypeConverter`-based binding
  (config binding, `PropertyGrid`, etc.); `PhoneNumberAttribute`, a `DataAnnotations`
  `ValidationAttribute` backed by real validation (`PhoneNumberUtil.IsValidNumber`) instead of the
  framework's loose `[Phone]` regex. (#405)
- Security: fixed a path-traversal issue in `BuildLocaleNames` and an uncaught parse exception in
  `PhoneNumberTypeConverter`. (#428)
- Fixed: `NumberParseException` (rather than `ArgumentOutOfRangeException`) on malformed RFC3966
  `phone-context` ordering (#406); three `PhoneNumberMatcher`/`AsYouTypeFormatter` parity gaps
  against the upstream Java source (#408); unbounded growth in `AsYouTypeFormatter` and missing
  null-guards in `PhoneNumberUtil` (#402).
- Changed: metadata-sync automation now opens a PR (authenticated as the `libphonenumber-csharp-bot`
  account) and auto-merges it, instead of pushing straight to `main` — see the "Metadata updates"
  design note this same change added to CHANGELOG.md automation, below. Also: added a fuzzing target
  for the parsing surface (FsCheck properties + SharpFuzz, #392); migrated the solution to `.slnx`
  and picked up a transitive `AngleSharp` advisory fix (#388); added build-provenance attestation for
  published packages (#400).
- A large batch of purely internal idiomatic-C# cleanups and documentation refreshes with no
  behavior change is omitted here — see the release notes for the full PR list.
- Metadata update to upstream [libphonenumber v9.0.38](https://github.com/google/libphonenumber/releases/tag/v9.0.38).

## [v9.0.32 – v9.0.37](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.31...v9.0.37) - 2026-06-05 – 2026-08-14

6 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v9.0.31](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.30...v9.0.31) - 2026-05-23

- Added: cross-platform embedded debug symbols for both packages. (#340)
- Metadata update to upstream v9.0.31.

## [v9.0.30](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.29...v9.0.30) - 2026-05-07

- Added: `net10.0` target framework.
- Changed: geocoding and time-zone prefix maps are now generated to per-region binary files at build
  time (`PhoneNumbers.MetadataBuilder`) and loaded from embedded resources at runtime, replacing the
  bundled `geocoding.zip` and runtime text parsing. This completes the same "binary metadata" move
  already made for the phone/short-number/alternate-format XML.
- Metadata update to upstream v9.0.30.

## [v9.0.2 – v9.0.29](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.1...v9.0.29) - 2025-03-28 – 2026-04-25

28 releases, almost entirely metadata syncs and routine CI/dependency maintenance (Dependabot
bumps, `net9.0` support added and later dropped again as it went out of support). One test-only fix
kept CI green after a `System.Collections.Immutable` behavior change (#297). No user-facing source
changes.

## [v9.0.1](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.0...v9.0.1) - 2025-03-23

- Reverted the assembly strong-name signing added one release earlier in v9.0.0 (below), after it
  caused problems for consumers. (#291)

## [v9.0.0](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.55...v9.0.0) - 2025-03-11

- **Breaking:** both packages' assemblies are strong-name signed. (#288) Bumped the major version
  because it changes assembly identity — reverted the very next release, v9.0.1 above, after it
  turned out to break more consumers than it helped.

## [v8.13.51 – v8.13.55](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.50...v8.13.55) - 2024-12-02 – 2025-02-14

5 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.13.50](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.49...v8.13.50) - 2024-11-16

- Added: `net9.0` target framework (dropped again once out of support — see the v9.0.2–v9.0.29 entry
  above). (#273)

## [v8.13.45 – v8.13.49](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.44...v8.13.49) - 2024-09-06 – 2024-11-04

5 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.13.44](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.43...v8.13.44) - 2024-08-26

- Added: `net8.0` target framework; dropped `net7.0`. (#264)

## [v8.13.41 – v8.13.43](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.40...v8.13.43) - 2024-07-25 – 2024-08-09

3 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.13.40](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.39...v8.13.40) - 2024-07-03

- Fixed: area-code information was lost for all Mexican (MX) numbers.

## [v8.13.28 – v8.13.39](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.27...v8.13.39) - 2024-01-25 – 2024-06-15

12 releases, mostly metadata syncs. Code coverage now runs and uploads on every PR instead of only
on `main` (#245, #250). No other source changes.

## [v8.13.27](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.26...v8.13.27) - 2023-12-21

- Fixed: a null-reference exception during number normalization when given a null input. (#207)

## [v8.13.26](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.25...v8.13.26) - 2023-11-29

- Changed: metadata updates are now proposed and published automatically instead of by hand — the
  first `feat: automatic upgrade to vX.Y.Z` release, and the origin of most of the entries in this
  file. (#192)
- Added: Dependabot configuration for NuGet package updates. (#185)

## [v8.13.25](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.24...v8.13.25) - 2023-11-20

- Fixed: a leading `+` was incorrectly stripped/ignored when parsing. (#184)

## [v8.13.19 – v8.13.24](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.18...v8.13.24) - 2023-08-22 – 2023-10-31

6 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.13.18](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.17...v8.13.18) - 2023-08-03

- Changed: reduced allocations on the parsing/formatting hot path by skipping internal
  protobuf-style builder objects. (#180)

## [v8.13.17](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.16...v8.13.17) - 2023-07-26

- Changed: reduced unnecessary regex construction and usage on hot paths. (#178)

## [v8.13.12 – v8.13.16](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.11...v8.13.16) - 2023-06-15 – 2023-07-11

5 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.13.11](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.10...v8.13.11) - 2023-04-27

Metadata-only release tracking upstream libphonenumber. No source changes.

## [v8.13.10](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.9...v8.13.10) - 2023-04-20

- Added: `net6.0` target framework.
- Changed: `Format()` uses stack-allocated `Span<char>` instead of heap allocations on the
  formatting hot path. (#166)

## [v8.13.9](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.8...v8.13.9) - 2023-04-10

Metadata-only release tracking upstream libphonenumber. No source changes.

## [v8.13.8](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.7...v8.13.8) - 2023-03-27

- Added (Extensions): a `System.Text.Json` converter for `PhoneNumber`.

## [v8.13.3 – v8.13.7](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.2...v8.13.7) - 2022-12-22 – 2023-03-03

5 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.13.2](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.1...v8.13.2) - 2022-12-08

- Added: `PhoneNumberToTimeZonesMapper`, mapping a phone number to its candidate IANA time zones.
  (#163)

## [v8.13.1](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.13.0...v8.13.1) - 2022-11-28

- Added: the `libphonenumber-csharp.extensions` NuGet package, for C#-idiomatic helpers that have no
  Java-upstream equivalent. (#164)

## [v8.12.44 – v8.13.0](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.12.43...v8.13.0) - 2022-02-24 – 2022-11-08

16 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.12.39 – v8.12.43](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.12.38...v8.12.43) - 2022-02-14

- Removed: the `net35` target framework and other end-of-life targets. (#146)
- 5 metadata-only patch releases shipped the same day as this change.

## [v8.12.35 – v8.12.38](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.12.34...v8.12.38) - 2021-12-15

- Added: a way to refresh a `PhoneNumberUtil` instance's metadata from a stream at runtime, without
  restarting the process. (#142)
- 4 metadata-only patch releases shipped the same day as this change.

## [v8.11.4 – v8.12.34](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.11.3...v8.12.34) - 2020-02-13 – 2021-10-07

35 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.11.3](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.11.2...v8.11.3) - 2020-02-03

- Fixed: numbers with multiple leading zeroes were parsed incorrectly. (#111)

## [v8.10.1 – v8.11.2](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.10.0...v8.11.2) - 2018-11-28 – 2020-01-15

26 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.10.0](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.9.16...v8.10.0) - 2018-11-28

- Upstream libphonenumber 8.10.0 changed how `AsYouTypeFormatter` chooses between international and
  national-dialing rules, correctly formatting some numbers that use a national prefix but aren't
  internationally diallable. (#89)

## [v8.9.9 – v8.9.16](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.9.8...v8.9.16) - 2018-06-29 – 2018-10-19

8 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.9.8](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.9.7...v8.9.8) - 2018-06-15

- Added: `ShortNumberUtil`, providing information about short codes — the predecessor of today's
  `ShortNumberInfo`. (#64)

## [v8.9.4.1 – v8.9.7](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.9.3...v8.9.7) - 2018-04-17 – 2018-05-30

4 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.9.3](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.9.2...v8.9.3) - 2018-04-05

- Added: a `net35` target framework, using embedded XML resources instead of the standard resource
  pipeline. (#58) (Dropped again in v8.12.39–v8.12.43, 2022-02-14, above.)

## [v8.8.0 – v8.9.2](https://github.com/twcclegg/libphonenumber-csharp/compare/v8.7.1...v8.9.2) - 2017-08-22 – 2018-03-19

16 metadata-only releases tracking upstream libphonenumber. No source changes.

## [v8.7.1](https://github.com/twcclegg/libphonenumber-csharp/releases/tag/v8.7.1) - 2017-08-03

Earliest release with a recoverable history and the baseline for this changelog. See
[GitHub Releases](https://github.com/twcclegg/libphonenumber-csharp/releases) for anything published
before it.
