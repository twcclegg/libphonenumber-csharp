## Performance testing history

See [Github Actions](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/run_performance_tests_windows.yml) for a history of previous runs, in the logs, you can see the performance results for each method being tested

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

The `PhoneNumberWorkflowBenchmark` runs the largest data sets (up to `10000` numbers) and is
the slowest; the full suite still completes in a few minutes on a single runtime.

Other available benchmarks:

- `*AsYouTypeFormatterBenchmark*` — per-keystroke cost of `AsYouTypeFormatter.InputDigit` over
  a representative set of regional numbers.
- `*PhoneNumberMatcherBenchmark*` — `PhoneNumberUtil.FindNumbers` over a synthetic text body
  with phone numbers embedded between filler sentences.
- `*ParsingHelpersBenchmark*` — `PhoneNumberUtil.ExtractPossibleNumber` measured separately
  for clean inputs (no leading junk) and inputs that force the strip path.
- `*ColdStartBenchmark*` — cost a consumer pays the first time they touch the library: bare
  `PhoneNumberUtil` construction, construction plus lazy-load of every region's metadata,
  and an isolated first-region lookup. Uses BDN's `RunStrategy.ColdStart` with
  `invocationCount: 1` so each measurement is a genuine first-use, not a steady-state loop.

The benchmark data is generated from valid example numbers in the bundled metadata and expanded
deterministically to the configured `PhoneNumberCount` values, up to 10,000 inputs. Each benchmark
iteration parses, validates, and formats every number in that data set.

Below you can see a sample of what the results might look like

| Method                              | PhoneNumberCount | Job                | Runtime            | Mean     | Error     | StdDev    | Gen0    | Allocated |
|------------------------------------ |-----------------:|------------------- |------------------- |---------:|----------:|----------:|--------:|----------:|
| ParseValidateAndFormatPhoneNumbers  |             1000 | .NET 10.0          | .NET 10.0          | 1.25 ms  | 0.018 ms  | 0.017 ms  | 31.2500 |   512 KB  |
| ParseValidateAndFormatPhoneNumbers  |            10000 | .NET 10.0          | .NET 10.0          | 12.46 ms | 0.231 ms  | 0.216 ms  | 312.500 |  5120 KB  |

