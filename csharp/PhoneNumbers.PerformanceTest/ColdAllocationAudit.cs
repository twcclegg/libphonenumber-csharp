using System;
using System.Globalization;

namespace PhoneNumbers.PerformanceTest
{
    /// <summary>
    /// Measures every managed byte a fresh process allocates getting from a cold library to its first
    /// parsed, validated and formatted numbers, and fails a budget. Run via
    /// <c>dotnet run -c Release -- --cold-allocations</c>.
    /// <para>
    /// <b>Why neither of the other memory checks covers this.</b> MemoryDiagnoser measures a warm
    /// call, after static initializers have run, so it never sees cold-start work at all.
    /// <see cref="RetainedMemoryAudit"/> forces a full collection before it measures, so it sees only
    /// what survives: metadata deserialisation that builds and discards megabytes of intermediate
    /// strings would pass it untouched. This counts the transient bytes too -- what a process that
    /// never collected would hold at the end -- which is the GC work a cold start costs a short-lived
    /// caller (a serverless function, a CLI, the Blazor WASM demo's first render).
    /// </para>
    /// <para>
    /// <see cref="GC.GetTotalAllocatedBytes(bool)"/> counts every thread, so allocations the library
    /// pushes onto the thread pool are included. Must run first thing in its own process: anything
    /// that touches the library before the baseline hides cost from it.
    /// </para>
    /// </summary>
    internal static class ColdAllocationAudit
    {
        /// <summary>
        /// Ceilings, in KB, on bytes allocated (a) from cold through the first number, and (b) from cold
        /// through every number in <see cref="RetainedMemoryAudit.Sample"/>, twice. Both are reported
        /// because they fail differently: (a) catches an eager initializer that loads more than the
        /// first region needs, (b) per-region load cost that one region cannot show. Calibrated to
        /// discriminate rather than merely pass -- measured on net10.0, identical across 3 runs:
        /// <list type="table">
        /// <item><term>this build</term><description>219 KB / 2598 KB</description></item>
        /// <item><term>each metadata stream copied into a MemoryStream before reading</term><description>474 KB / 2916 KB</description></item>
        /// <item><term>metadata patterns built with RegexOptions.Compiled</term><description>362 KB / 5957 KB</description></item>
        /// </list>
        /// The MemoryStream row is the case this audit exists for: it is pure transient garbage, and
        /// RetainedMemoryAudit passes it (1440 KB against its 1600 KB budget). Each budget sits ~30%
        /// above this build. If a metadata update legitimately pushes either up, re-measure every row
        /// and raise it deliberately, recording the new figures here -- do not bump it until CI goes green.
        /// </summary>
        private const long FirstNumberBudgetKilobytes = 285;
        private const long AllRegionsBudgetKilobytes = 3400;

        public static int Run()
        {
            var sample = RetainedMemoryAudit.Sample;
            var baseline = GC.GetTotalAllocatedBytes(precise: true);

            var util = PhoneNumberUtil.GetInstance();
            if (!Exercise(util, sample[0].Number, sample[0].Region))
                return 2;
            var firstKilobytes = (GC.GetTotalAllocatedBytes(precise: true) - baseline) / 1024;

            foreach (var pass in new[] { 0, 1 })
            {
                foreach (var (text, region) in sample)
                {
                    if (!Exercise(util, text, region))
                        return 2;
                }
            }
            var allKilobytes = (GC.GetTotalAllocatedBytes(precise: true) - baseline) / 1024;

            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "Allocated from cold: first number {0} KB (budget {1} KB); {2} numbers across {3} regions {4} KB (budget {5} KB)",
                firstKilobytes,
                FirstNumberBudgetKilobytes,
                sample.Length * 2 + 1,
                sample.Length,
                allKilobytes,
                AllRegionsBudgetKilobytes));

            var failed = false;
            failed |= OverBudget("first number", firstKilobytes, FirstNumberBudgetKilobytes);
            failed |= OverBudget("all regions", allKilobytes, AllRegionsBudgetKilobytes);
            return failed ? 1 : 0;
        }

        private static bool Exercise(PhoneNumberUtil util, string text, string region)
        {
            var parsed = util.Parse(text, region);
            var ok = util.IsValidNumber(parsed);
            var rendered = util.Format(parsed, PhoneNumberFormat.INTERNATIONAL);

            // The measurement is meaningless if the work above was elided as unobserved.
            if (ok && rendered.Length > 0)
                return true;
            Console.Error.WriteLine($"ColdAllocationAudit: sanity check failed -- {text} ({region}) did not validate.");
            return false;
        }

        private static bool OverBudget(string what, long kilobytes, long budgetKilobytes)
        {
            if (kilobytes <= budgetKilobytes)
                return false;
            Console.Error.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "ColdAllocationAudit FAILED: {0} allocated {1} KB from cold, over the {2} KB budget by {3} KB. " +
                "Something on the cold path is allocating work it does not need -- an initializer that " +
                "loads or builds more than the regions in use, or intermediate copies during metadata " +
                "deserialisation, are the usual causes. See this file's remarks for the calibration before " +
                "considering raising the budget.",
                what,
                kilobytes,
                budgetKilobytes,
                kilobytes - budgetKilobytes));
            return true;
        }
    }
}
