// Compares BenchmarkDotNet's full-JSON results (Statistics.Mean/StandardDeviation/N, in
// nanoseconds) for the PR branch against the PR base, one matching benchmark case at a time, and
// reports only the ones that moved by a real amount. This exists because posting every benchmark's
// raw numbers on every push floods the PR page and the maintainer's email - see
// run_performance_tests.yml for how this fits into the pipeline.
//
// A case only counts as "moved" when BOTH hold:
//   - Welch's t-test on the two means (unequal variances allowed, since a slower run is often also
//     a noisier one) rejects the null hypothesis at p < SignificanceLevel. This is the same 99.9%
//     confidence BenchmarkDotNet itself uses for the "Error" column it already prints, so the bar
//     here isn't a new number to justify.
//   - The relative change in the mean is at least MinRelativeDelta.
//
// The MinRelativeDelta floor is doing most of the real work here, and it needs to be large. Each
// side of the comparison is ONE process launch (base and branch each run once, in the same job -
// see run_performance_tests.yml), and a launch's reported StandardDeviation only captures
// iteration-to-iteration noise *within* that launch. It says nothing about launch-to-launch drift
// (JIT tiering, scheduler placement, thermal/frequency state), which is what actually separates the
// base launch from the branch launch. Measured directly: three consecutive same-code launches of
// ParsingHelpersBenchmark.ExtractPossibleNumber_CleanInput on one machine ranged from 14.75us to
// 17.54us - a 17% spread from noise alone, while each individual launch's StandardDeviation was
// under 1% of its mean. A significance test built only on within-launch variance is badly
// overconfident against that: the same identical-code pair reached p=1e-26 at a 6% delta. So the
// floor here (20%) is set well above that measured single-launch noise band, not tuned to "feels
// right". If this still produces false positives on the real runner, the fix isn't a bigger floor,
// it's giving BenchmarkDotNet LaunchCount > 1 so launch-to-launch variance is actually measured
// instead of assumed away - that costs proportionally more CI time, which is why it isn't done by
// default today.
//
// Usage: dotnet run --project csharp/PhoneNumbers.BenchmarkTools -- <branch-results-dir> <base-results-dir> <output-json-path>

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using MathNet.Numerics.Distributions;

namespace PhoneNumbers.BenchmarkTools;

internal sealed class BenchmarkStatistics
{
    public double Mean { get; set; }
    public double StandardDeviation { get; set; }
    public int N { get; set; }
}

internal sealed class BenchmarkCase
{
    public string FullName { get; set; } = "";
    public string? Method { get; set; }
    public string? Parameters { get; set; }
    public BenchmarkStatistics? Statistics { get; set; }
}

internal sealed class BenchmarkReport
{
    public List<BenchmarkCase>? Benchmarks { get; set; }
}

internal sealed class ChangeEntry
{
    public string FullName { get; set; } = "";
    public string? Method { get; set; }
    public string Parameters { get; set; } = "";
    public double BaseMean { get; set; }
    public double BranchMean { get; set; }
    public string BaseMeanDisplay { get; set; } = "";
    public string BranchMeanDisplay { get; set; } = "";
    public double RelativeDeltaPct { get; set; }
    public double PValue { get; set; }

    // Pre-rendered "base -> branch (+X.X%, p=Y.YYe-Z)" text, computed once here rather than by
    // each consumer: fail-on-benchmark-regression.sh and any future reader just print this field
    // instead of re-deriving the same formatting in another language and risking the two drifting
    // apart (which is exactly why lib/format-benchmark-change.js used to exist as a separate module
    // shared between two JS files - now there is only one place that formats numbers at all).
    public string Display { get; set; } = "";
}

internal sealed class ChangesOutput
{
    public List<ChangeEntry> Regressions { get; set; } = [];
    public List<ChangeEntry> Improvements { get; set; } = [];
}

internal static class Program
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // camelCase output keys: post_performance_test_comment.yml's github-script and
    // fail-on-benchmark-regression.sh both read these field names directly.
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        // The default encoder unicode-escapes characters like '>' and '+' (HTML/JS safety this
        // artifact doesn't need, and JSON.stringify never applied either) - this is a CI artifact
        // a maintainer might open directly to debug a run, so keep it plain text.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine(
                "usage: dotnet run --project csharp/PhoneNumbers.BenchmarkTools -- <branch-results-dir> <base-results-dir> <output-json-path>");
            return 2;
        }

        var branchDir = args[0];
        var baseDir = args[1];
        var outPath = args[2];

        // Both overridable via env for tuning without a code change.
        var significanceLevel = EnvDouble("BENCHMARK_SIGNIFICANCE_LEVEL", 0.001);
        var minRelativeDelta = EnvDouble("BENCHMARK_MIN_RELATIVE_DELTA", 0.2);

        var branchBenchmarks = ReadBenchmarksFromDir(branchDir);
        var baseBenchmarks = ReadBenchmarksFromDir(baseDir);

        var regressions = new List<ChangeEntry>();
        var improvements = new List<ChangeEntry>();

        foreach (var (fullName, branch) in branchBenchmarks)
        {
            if (!baseBenchmarks.TryGetValue(fullName, out var @base))
            {
                continue; // new benchmark case, nothing to compare against.
            }

            var baseStats = @base.Statistics;
            var branchStats = branch.Statistics;
            if (baseStats is null || branchStats is null || baseStats.N < 2 || branchStats.N < 2)
            {
                continue;
            }

            var relativeDelta = (branchStats.Mean - baseStats.Mean) / baseStats.Mean;
            if (Math.Abs(relativeDelta) < minRelativeDelta)
            {
                continue;
            }

            var (t, dof) = WelchTTest(baseStats, branchStats);
            var pValue = TTestTwoTailedPValue(t, dof);
            if (pValue >= significanceLevel)
            {
                continue;
            }

            var entry = new ChangeEntry
            {
                FullName = fullName,
                Method = branch.Method,
                Parameters = branch.Parameters ?? "",
                BaseMean = baseStats.Mean,
                BranchMean = branchStats.Mean,
                BaseMeanDisplay = FormatDuration(baseStats.Mean),
                BranchMeanDisplay = FormatDuration(branchStats.Mean),
                RelativeDeltaPct = relativeDelta * 100,
                PValue = pValue,
            };
            entry.Display = FormatChangeDisplay(entry, showPlusSign: relativeDelta > 0);

            (relativeDelta > 0 ? regressions : improvements).Add(entry);
        }

        regressions.Sort((a, b) => b.RelativeDeltaPct.CompareTo(a.RelativeDeltaPct));
        improvements.Sort((a, b) => a.RelativeDeltaPct.CompareTo(b.RelativeDeltaPct));

        var output = new ChangesOutput { Regressions = regressions, Improvements = improvements };
        File.WriteAllText(outPath, JsonSerializer.Serialize(output, WriteOptions));

        foreach (var r in regressions)
        {
            Console.WriteLine($"REGRESSION: {r.FullName} {r.Display}");
        }

        foreach (var i in improvements)
        {
            Console.WriteLine($"IMPROVEMENT: {i.FullName} {i.Display}");
        }

        if (regressions.Count == 0 && improvements.Count == 0)
        {
            Console.WriteLine("no statistically significant change in any benchmark");
        }

        return 0;
    }

    // Reads the value as unset/empty rather than `double.Parse(...) || fallback`: unlike
    // JavaScript's `Number(x) || fallback`, C#'s numeric types have no "falsy zero" to
    // accidentally discard an explicit 0 override, so a plain TryParse is enough here.
    private static double EnvDouble(string name, double fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(raw))
        {
            return fallback;
        }

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private static Dictionary<string, BenchmarkCase> ReadBenchmarksFromDir(string dir)
    {
        var byFullName = new Dictionary<string, BenchmarkCase>();
        if (!Directory.Exists(dir))
        {
            return byFullName;
        }

        foreach (var file in Directory.EnumerateFiles(dir))
        {
            if (!file.EndsWith("-report-full-compressed.json", StringComparison.Ordinal))
            {
                continue;
            }

            BenchmarkReport? report;
            try
            {
                report = JsonSerializer.Deserialize<BenchmarkReport>(File.ReadAllText(file), ReadOptions);
            }
            catch (JsonException ex)
            {
                // A partially-written/truncated report shouldn't take down the whole comparison
                // (and with it the artifact upload that would otherwise help diagnose why) - skip
                // and keep going.
                Console.Error.WriteLine($"Skipping unreadable benchmark report {file}: {ex.Message}");
                continue;
            }

            foreach (var benchmark in report?.Benchmarks ?? [])
            {
                byFullName[benchmark.FullName] = benchmark;
            }
        }

        return byFullName;
    }

    // Welch's t-test: two-sample, unequal variances, from summary statistics only (no need for the
    // raw per-iteration measurements).
    private static (double T, double Dof) WelchTTest(BenchmarkStatistics baseStats, BenchmarkStatistics branchStats)
    {
        var varAOverN = baseStats.StandardDeviation * baseStats.StandardDeviation / baseStats.N;
        var varBOverN = branchStats.StandardDeviation * branchStats.StandardDeviation / branchStats.N;
        var se = Math.Sqrt(varAOverN + varBOverN);
        if (se == 0)
        {
            return (baseStats.Mean == branchStats.Mean ? 0 : double.PositiveInfinity, baseStats.N + branchStats.N - 2);
        }

        var t = (branchStats.Mean - baseStats.Mean) / se;
        var dof = Math.Pow(varAOverN + varBOverN, 2)
            / (Math.Pow(varAOverN, 2) / (baseStats.N - 1) + Math.Pow(varBOverN, 2) / (branchStats.N - 1));
        return (t, dof);
    }

    // Two-tailed p-value for Student's t-distribution, via MathNet.Numerics rather than a
    // hand-rolled incomplete-beta-function implementation - the numerical-recipes-style code this
    // replaced was untested outside this one call site and not worth maintaining by hand.
    private static double TTestTwoTailedPValue(double t, double dof)
    {
        if (!double.IsFinite(t) || !double.IsFinite(dof) || dof <= 0)
        {
            return 1;
        }

        var distribution = new StudentT(location: 0, scale: 1, freedom: dof);
        return 2 * distribution.CumulativeDistribution(-Math.Abs(t));
    }

    private static string FormatDuration(double ns) => ns switch
    {
        >= 1e9 => $"{(ns / 1e9).ToString("F3", CultureInfo.InvariantCulture)} s",
        >= 1e6 => $"{(ns / 1e6).ToString("F3", CultureInfo.InvariantCulture)} ms",
        >= 1e3 => $"{(ns / 1e3).ToString("F3", CultureInfo.InvariantCulture)} us",
        _ => $"{ns.ToString("F1", CultureInfo.InvariantCulture)} ns",
    };

    private static string FormatChangeDisplay(ChangeEntry entry, bool showPlusSign)
    {
        var sign = showPlusSign && entry.RelativeDeltaPct > 0 ? "+" : "";
        return $"{entry.BaseMeanDisplay} -> {entry.BranchMeanDisplay} "
            + $"({sign}{entry.RelativeDeltaPct.ToString("F1", CultureInfo.InvariantCulture)}%, p={ToExponential(entry.PValue, 2)})";
    }

    // .NET's "E2" format ("1.23E-005") doesn't match JavaScript's Number.prototype.toExponential(2)
    // ("1.23e-5"), which is what the JSON this replaces used to produce and what a maintainer
    // reading old PR comments/history has already seen - lowercase "e", a sign but no zero-padding
    // on the exponent.
    private static string ToExponential(double value, int digits)
    {
        if (value == 0)
        {
            return $"{0.0.ToString("F" + digits, CultureInfo.InvariantCulture)}e+0";
        }

        var exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        var mantissa = value / Math.Pow(10, exponent);
        var mantissaText = mantissa.ToString("F" + digits, CultureInfo.InvariantCulture);
        // Rounding the mantissa to `digits` places can push e.g. 9.995 to "10.00" - bump the
        // exponent instead of ever showing a two-digit leading mantissa.
        if (double.Parse(mantissaText, CultureInfo.InvariantCulture) >= 10)
        {
            exponent++;
            mantissaText = (mantissa / 10).ToString("F" + digits, CultureInfo.InvariantCulture);
        }

        var expSign = exponent < 0 ? "-" : "+";
        return $"{mantissaText}e{expSign}{Math.Abs(exponent)}";
    }
}
