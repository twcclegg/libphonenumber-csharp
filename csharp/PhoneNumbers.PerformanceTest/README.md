## Performance testing history

See [Github Actions](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/run_performance_tests.yml) for a history of previous runs, in the logs, you can see the performance results for each method being tested

## Running locally

Install the .NET 10 SDK. The benchmark project targets `net10.0` only, and every benchmark
is configured with a single `net10.0` job (`[SimpleJob(RuntimeMoniker.Net10_0)]`), so .NET 10
is the only runtime required.

```powershell
cd csharp/PhoneNumbers.PerformanceTest
dotnet run -c Release --framework net10.0 -- --filter "*"
```

BenchmarkDotNet writes detailed reports to `BenchmarkDotNet.Artifacts/results`.

To run only the phone number workflow benchmark, pass a filter after `--`:

```powershell
dotnet run -c Release --framework net10.0 -- --filter "*PhoneNumberWorkflowBenchmark*"
```

The `PhoneNumberWorkflowBenchmark` exercises the widest slice of the library; the full suite
completes in a few minutes on a single runtime. Alongside the end-to-end
`ParseValidateAndFormatPhoneNumbers` it measures `ParseOnly`, `ValidateOnly` and `FormatOnly`
against the same data, so a cost — allocation especially — can be attributed to a phase instead of
only being visible as a total. `ParseNationalFormat` and `ParseWithExtension` re-render those same
numbers the way callers usually supply them: the seed data is E164, which is the cheapest input
`Parse` can take, since the national-prefix and extension patterns both fail and cost nothing.

Other available benchmarks:

- `*AsYouTypeFormatterBenchmark*` — per-keystroke cost of `AsYouTypeFormatter.InputDigit` over
  a representative set of regional numbers.
- `*PhoneNumberMatcherBenchmark*` — `PhoneNumberUtil.FindNumbers` over a synthetic text body
  with phone numbers embedded between filler sentences.
- `*ParsingHelpersBenchmark*` — `PhoneNumberUtil.ExtractPossibleNumber` measured separately
  for clean inputs (no leading junk) and inputs that force the strip path.
- `*PhoneNumberOfflineGeocoderBenchmark*` — `PhoneNumberOfflineGeocoder.GetDescriptionForNumber`
  end-to-end, plus `Locale.GetDisplayCountry` on its own to isolate the country-name table that
  backs the fallback path when a number has no finer-grained area description.
- `*ColdStartBenchmark*` — cost a consumer pays the first time they touch the library: bare
  `PhoneNumberUtil` construction, construction plus lazy-load of every region's metadata, an
  isolated first-region metadata lookup, and four `FirstUse*` benchmarks
  (`FirstUseValidateAndFormat`, `FirstUseAsYouType`, `FirstUseFindNumbers`, `FirstUseGeocode`)
  that each pair a region-keyed subsystem — Parse/IsValidNumber/Format, `AsYouTypeFormatter`,
  `PhoneNumberUtil.FindNumbers`, and `PhoneNumberOfflineGeocoder` respectively — with a region no
  earlier iteration, in any benchmark in this process, has touched. Uses BDN's
  `RunStrategy.ColdStart` with `invocationCount: 1` so each measurement is a genuine first-use,
  not a steady-state loop. See "Why there's a first-use-per-region benchmark" below before
  changing or removing any `FirstUse*` benchmark — they exist specifically to catch a class of
  regression the other benchmarks here structurally cannot see.

The benchmark data is generated from valid example numbers in the bundled metadata and expanded
deterministically to the configured `PhoneNumberCount` value. Each benchmark
iteration parses, validates, and formats every number in that data set.

Below you can see a sample of what the results might look like

| Method                              | PhoneNumberCount | Job                | Runtime            | Mean     | Error     | StdDev    | Gen0    | Allocated |
|------------------------------------ |-----------------:|------------------- |------------------- |---------:|----------:|----------:|--------:|----------:|
| ParseValidateAndFormatPhoneNumbers  |             1000 | .NET 10.0          | .NET 10.0          | 1.25 ms  | 0.018 ms  | 0.017 ms  | 31.2500 |   512 KB  |

## Why there's a first-use-per-region benchmark

This library has shipped the same class of performance regression three times without this
benchmark suite catching any of them — twice as an accidental side effect, once as a deliberate,
benchmarked, tested tradeoff that just wasn't tested against the right shape of workload:

- **2017** (`8.8.0`): adding a `netstandard2.0` build target flipped a `#if NETSTANDARD1_1` switch
  in `InternalRegexOptions.cs` from `RegexOptions.None` to `RegexOptions.Compiled`. Combined with
  `RegexCache`'s 100-entry LRU cap (already too small for a diverse-region workload, but harmless
  under interpreted regex), throughput dropped ~115x for any caller touching more than ~100
  distinct region/format patterns. Reported independently as
  [#38](https://github.com/twcclegg/libphonenumber-csharp/issues/38) and
  [#136](https://github.com/twcclegg/libphonenumber-csharp/issues/136); [#136] led to
  [PR #161](https://github.com/twcclegg/libphonenumber-csharp/pull/161) (Nov 2022, `8.13.1`),
  which removed the LRU cap entirely (unbounded `ConcurrentDictionary`) — fixing throughput ~3,800x
  for the same diverse workload. A second, independent report of the same underlying problem,
  [#154](https://github.com/twcclegg/libphonenumber-csharp/issues/154), was closed as stale eleven
  days after #161 had already shipped the fix — nobody connected the two.
- **2026** (`9.0.30`): [PR #325](https://github.com/twcclegg/libphonenumber-csharp/pull/325)
  deliberately switched `PhoneRegex`'s per-region patterns from interpreted to
  `RegexOptions.Compiled`, benchmarked and merged on real numbers — `PhoneNumberWorkflowBenchmark`
  showed an honest 11-14% steady-state improvement. What that benchmark's `GlobalSetup` couldn't
  see: it builds its seed data by calling `GetExampleNumberForType`/`IsValidNumber`/`Format`
  against every supported region *before* the timed run starts, which pre-compiles (and — see
  `PhoneRegex.cs` — permanently caches, process-wide) every pattern the timed loop will touch. A
  consumer's *first* Parse/IsValidNumber/Format against a region their process has never seen pays
  a JIT-compile cost of ~30ms instead of ~0.3ms; cold start across a diverse set of new regions
  went from ~100ms to ~620-700ms. Not a bug — a real, measured tradeoff nobody's benchmark
  methodology was shaped to price correctly.

Both regressions share one signature: **they only appear when many *distinct* regions are
touched, and only on that first touch.** Every other benchmark in this project either exercises
one region repeatedly (cheap after the first call, so a cache/compile regression is invisible) or
warms its whole diverse dataset in `GlobalSetup` before the timed run starts (so the timed loop
never sees a cold pattern either). The four `ColdStartBenchmark.FirstUse*` benchmarks are built to
have neither property: fresh instances per iteration (or, for `FirstUseAsYouType`, a fresh
formatter off the shared warm `PhoneNumberUtil` — matching how `GetInstance()` is actually used),
a fixed list of regions `GlobalSetup` never touches, one previously-unseen region per invocation.

`FirstUseValidateAndFormat` covers Parse/IsValidNumber/Format, the same regex-cache path the two
regressions above hit directly. `FirstUseAsYouType`, `FirstUseFindNumbers`, and `FirstUseGeocode`
extend the same coverage to the library's other region-keyed subsystems — `AsYouTypeFormatter` and
`PhoneNumberMatcher` share `PhoneRegex`'s pattern cache, so they're exposed to the identical class
of regression; `PhoneNumberOfflineGeocoder` is architecturally different (a lazily-loaded prefix
map in `PrefixFileReader`, no regex involved) but has the same structural blind spot in its own
benchmark class, so it's covered for symmetry.

**Each `FirstUse*` benchmark draws from its own region pool, and the four pools are disjoint from
each other.** All benchmark classes in this project run inside one BenchmarkDotNet process, so a
region touched by one class's cold-start benchmark is no longer cold for another's if the pools
overlap — see the pool comments in `ColdStartBenchmark.cs` for the exact set each one owns. If you
add a new benchmark that touches multiple regions, either reuse an existing disjoint pool or add a
new one, and make sure at least one benchmark preserves the fresh-instance/never-pre-warmed
property, or this exact regression will ship unnoticed again.

