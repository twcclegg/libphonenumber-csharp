## Performance testing history

See [Github Actions](https://github.com/twcclegg/libphonenumber-csharp/actions/workflows/run_performance_tests_windows.yml) for a history of previous runs, in the logs, you can see the performance results for each method being tested

Below you can see a sample of what the results might look like

| Method            | Job                | Runtime            | Mean     | Error     | StdDev    | Gen0   | Allocated |
|------------------ |------------------- |------------------- |---------:|----------:|----------:|-------:|----------:|
| FormatPhoneNumber | .NET 6.0           | .NET 6.0           | 1.234 us | 0.0158 us | 0.0124 us | 0.0076 |     152 B |
| FormatPhoneNumber | .NET 8.0           | .NET 8.0           | 1.141 us | 0.0080 us | 0.0071 us | 0.0076 |     152 B |
| FormatPhoneNumber | .NET Framework 4.8 | .NET Framework 4.8 | 2.746 us | 0.0114 us | 0.0101 us | 0.1335 |     851 B |

