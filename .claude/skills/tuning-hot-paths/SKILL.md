---
name: tuning-hot-paths
description: Measure and tune the parse, format, match, geocode or cold-start hot paths with the BenchmarkDotNet harness in csharp/PhoneNumbers.PerformanceTest. Use before and after any change under csharp/PhoneNumbers/ that touches parsing, formatting, matching, regex handling or allocation, when a PR's benchmark comparison comment reports a regression, or when someone proposes RegexOptions.Compiled.
---

# Tuning a hot path

Parsing and formatting are the library's whole job and callers run them in tight loops, so this repo
treats allocation and throughput as correctness-adjacent. **Measure a hot-path change; do not reason
about it** — the JIT, the regex cache and the frozen lookup tables routinely invert intuition.

## Conventions to keep

- Match against spans and slices rather than materialising substrings or `Match` objects.
- Go through `RegexCache` / `PhoneRegex`; never construct a `Regex` on a call path.
- Build lookup tables once, into frozen collections, rather than per call.
- Nothing on a public path may need reflection or dynamic code — `IsAotCompatible` is on and the
  trim/AOT analyzers are errors. The Blazor WASM demo runs the library trimmed and depends on this.

## Never compile metadata regexes

Do not build a metadata-derived pattern with `RegexOptions.Compiled`. This shipped as a regression
three times (8.8.0, 8.13.0, 9.0.30), each time on "compiled should be faster" reasoning. Measured
end to end, compiling the metadata patterns is 34x slower for 1,000 operations across 245 regions,
3.7x slower for 100,000, and still 1.7x slower at 1,000,000 — the break-even math is in `README.md`
under "Regex compilation and startup cost" and in the harness README under "Why metadata regexes
are not compiled". `TestPhoneRegex.MetadataPatternsAreNeverCompiled` asserts on the options the
metadata regexes are actually built with; the library's own small fixed set of regexes is a
different case and stays compiled.

## Measuring locally

```bash
cd csharp/PhoneNumbers.PerformanceTest
dotnet run -c Release --framework net10.0 -- --filter "*PhoneNumberWorkflowBenchmark*"
```

`--filter "*"` runs everything (a few minutes). Results land in `BenchmarkDotNet.Artifacts/results`.
Pick the benchmark that isolates your change:

| Filter | Covers |
| --- | --- |
| `*PhoneNumberWorkflowBenchmark*` | Widest slice — end-to-end, plus `ParseOnly` / `ValidateOnly` / `FormatOnly` over the same data so a cost can be attributed to a phase |
| `*ParsingHelpersBenchmark*` | `ExtractPossibleNumber`, split by clean input vs input that forces the strip path |
| `*AsYouTypeFormatterBenchmark*` | Per-keystroke `InputDigit` cost |
| `*PhoneNumberMatcherBenchmark*` | `FindNumbers` over a synthetic text body |
| `*PhoneNumberOfflineGeocoderBenchmark*` | `GetDescriptionForNumber`, plus `Locale.GetDisplayCountry` alone |
| `*ColdStartBenchmark*` | First-use cost: construction, lazy metadata load, first region lookup |
| `*ProcessStartBenchmark*` | Whole-process startup to first parse |

Benchmark the same filter on the unmodified base commit and compare — a single run of the changed
code proves nothing on its own. Watch the **Allocated** column as closely as **Mean**; allocation
regressions are the ones that hurt callers in a loop.

**Allocated does not measure retained memory.** `MemoryDiagnoser` runs after every static
initializer has completed, so state the library keeps forever is invisible to it — a 3.4 MB
retained-state regression once moved `ProcessStartBenchmark`'s Allocated by 15 KB. Retained memory
is audited by a separate mode that CI also runs:

```bash
dotnet run -c Release --framework net10.0 -- --retained-memory   # exits non-zero over budget
```

`csharp/PhoneNumbers.PerformanceTest/README.md` explains what each benchmark exercises and why.

## The PR comparison, and its blind spot

Changes under `csharp/PhoneNumbers/`, the benchmark project, `resources/`, or the shared build files
trigger `run_performance_tests.yml`, which runs the base commit and the branch on the same runner
and posts a comparison comment via `post_performance_test_comment.yml`. Read that comment rather
than guessing.

It flags a case only when Welch's t-test clears p < 0.001 **and** the mean moved by at least **20%**.
That floor is deliberate — base and branch each get one process launch, and launch-to-launch drift
alone measured a 17% spread on identical code. The consequence: **a real 5–15% regression will pass
CI silently.** If you are tuning something that matters at that scale, measure it locally with
repeated runs; a green benchmark check is not evidence of no regression.

BenchmarkDotNet exits 0 even when a benchmark throws, reporting the row as `NA`. The workflow has a
separate step for that — if you see `NA` in results, the benchmark failed, it did not measure zero.

## Adding a benchmark

New benchmarks go in `csharp/PhoneNumbers.PerformanceTest/Benchmarks/`, target `net10.0` only, and
use `[MemoryDiagnoser]` + `[SimpleJob(RuntimeMoniker.Net10_0)]` like the existing ones. Reuse
`PhoneNumberBenchmarkData` for input so cases stay comparable across benchmarks.
