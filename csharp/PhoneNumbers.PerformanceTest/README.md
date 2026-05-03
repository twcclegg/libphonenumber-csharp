## Performance testing history

See [Github Actions](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/run_performance_tests_windows.yml) for a history of previous runs, in the logs, you can see the performance results for each method being tested

## Running locally

Install the .NET SDKs or runtimes needed by the benchmark jobs you want to run. The benchmark
project is launched with .NET 9, while BenchmarkDotNet can execute the configured .NET Framework
4.8, .NET 6, and .NET 8 jobs when those runtimes are available locally.

```powershell
cd csharp/PhoneNumbers.PerformanceTest
dotnet run -c Release --framework net9.0 -- --filter "*"
```

BenchmarkDotNet writes detailed reports to `BenchmarkDotNet.Artifacts/results`.

To run only the phone number workflow benchmark, pass a filter after `--`:

```powershell
dotnet run -c Release --framework net9.0 -- --filter "*PhoneNumberWorkflowBenchmark*"
```

The full benchmark includes the `100000` phone-number data set and may take several minutes,
especially when multiple runtime jobs are available on the machine.

The benchmark data is generated from valid example numbers in the bundled metadata and expanded
deterministically to the configured `PhoneNumberCount` values, up to 100,000 inputs. Each benchmark
iteration parses, validates, and formats every number in that data set.

Below you can see a sample of what the results might look like

| Method                              | PhoneNumberCount | Job                | Runtime            | Mean     | Error     | StdDev    | Gen0    | Allocated |
|------------------------------------ |-----------------:|------------------- |------------------- |---------:|----------:|----------:|--------:|----------:|
| ParseValidateAndFormatPhoneNumbers  |             1000 | .NET 8.0           | .NET 8.0           | 1.25 ms  | 0.018 ms  | 0.017 ms  | 31.2500 |   512 KB  |
| ParseValidateAndFormatPhoneNumbers  |            10000 | .NET 8.0           | .NET 8.0           | 12.46 ms | 0.231 ms  | 0.216 ms  | 312.500 |  5120 KB  |
| ParseValidateAndFormatPhoneNumbers  |           100000 | .NET 8.0           | .NET 8.0           | 125.1 ms | 2.48 ms   | 2.32 ms   | 3125.00 | 51200 KB  |

