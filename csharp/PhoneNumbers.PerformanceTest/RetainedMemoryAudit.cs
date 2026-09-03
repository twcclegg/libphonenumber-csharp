using System;
using System.Globalization;
using System.Threading;

namespace PhoneNumbers.PerformanceTest
{
    /// <summary>
    /// Measures how much managed memory the library still holds after a representative amount of work,
    /// and fails a budget. Run via <c>dotnet run -c Release -- --retained-memory</c>.
    /// <para>
    /// <b>Why this is not a BenchmarkDotNet benchmark.</b> MemoryDiagnoser measures allocations during
    /// an additional run of the benchmark method, by which point every static initializer has already
    /// completed and allocates nothing, so it reports the steady-state cost of a warm call. Measured,
    /// not assumed: across a change that added 3.4 MB of permanently-retained static state,
    /// <see cref="Benchmarks.ProcessStartBenchmark"/>'s Allocated column moved only 129 KB -> 144 KB.
    /// </para>
    /// <para>
    /// <b>Retained, not allocated.</b> Do the work, force a full collection, measure what survived.
    /// Transient garbage is ignored; what remains is what the process holds for the rest of its life.
    /// </para>
    /// <para>
    /// <b>What this does and does not catch.</b> It measures the GC heap, so it sees managed retention:
    /// eagerly-populated static caches, and Regex wrappers that are held after they stop being needed.
    /// It does NOT see the cost of RegexOptions.Compiled itself, because emitted code does not live on
    /// the GC heap -- a build that compiles every metadata pattern on first touch reports roughly the
    /// same number here while costing tens of MB of RSS. Do not read a pass as "memory is fine";
    /// read it as "nothing is retaining managed objects it should not".
    /// </para>
    /// </summary>
    internal static class RetainedMemoryAudit
    {
        // A spread of regions rather than one: retention that grows per pattern -- a promoted holder
        // that fails to release its superseded interpreted Regex, say -- is invisible at one region,
        // where it is a single object against the metadata's own footprint.
        private static readonly (string Number, string Region)[] Sample =
        {
            ("+41441234567", "CH"), ("+16502530000", "US"), ("+442079460958", "GB"),
            ("+493083050", "DE"), ("+33142685300", "FR"), ("+81332245000", "JP"),
            ("+861065525566", "CN"), ("+911123014000", "IN"), ("+551122222222", "BR"),
            ("+61261621111", "AU"), ("+525512345678", "MX"), ("+27111234567", "ZA"),
            ("+82212345678", "KR"), ("+390612345678", "IT"), ("+34912345678", "ES"),
            ("+31201234567", "NL"), ("+46812345678", "SE"), ("+48123456789", "PL"),
            ("+902123456789", "TR"), ("+6621234567", "TH"),
        };

        /// <summary>
        /// Ceiling on managed memory retained after the work below. Calibrated to discriminate rather
        /// than merely pass -- measured on net10.0, 3 runs each, variance under 3 KB:
        /// <list type="table">
        /// <item><term>this build</term><description>1218 KB</description></item>
        /// <item><term>metadata patterns built with RegexOptions.Compiled</term><description>3209 KB</description></item>
        /// </list>
        /// 1600 KB sits above this build with ~30% headroom and well below the regression. If a
        /// metadata update legitimately pushes this up, re-measure both numbers and raise it
        /// deliberately, recording the new figures here -- do not bump it until CI goes green.
        /// </summary>
        private const long BudgetKilobytes = 1600;

        public static int Run()
        {
            // See ColdStartBenchmark on why this loader is constructed directly rather than via
            // PhoneNumberUtil.GetInstance()'s cached singleton.
#pragma warning disable CS0618
            var util = new PhoneNumberUtil(
                new EmbeddedResourceMetadataLoader(),
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap());
#pragma warning restore CS0618

            // Twice through, so every pattern crosses the promotion threshold and any per-holder
            // retention is actually established before we measure.
            foreach (var pass in new[] { 0, 1 })
            {
                foreach (var (text, region) in Sample)
                {
                    var parsed = util.Parse(text, region);
                    var ok = util.IsValidNumber(parsed);
                    var rendered = util.Format(parsed, PhoneNumberFormat.INTERNATIONAL);

                    // The measurement is meaningless if the work above was elided as unobserved.
                    if (!ok || rendered.Length == 0)
                    {
                        Console.Error.WriteLine(
                            $"RetainedMemoryAudit: sanity check failed -- {text} ({region}) did not validate on pass {pass}.");
                        return 2;
                    }
                }
            }

            // Let queued background compiles land, so promoted holders are in their final state.
            Thread.Sleep(2000);

            // Two collections with finalizers drained between: the first may queue objects for
            // finalization and only the second can reclaim those.
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);

            var retainedKilobytes = GC.GetTotalMemory(forceFullCollection: true) / 1024;

            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "Retained after {2} numbers across {3} regions: {0} KB (budget {1} KB)",
                retainedKilobytes,
                BudgetKilobytes,
                Sample.Length * 2,
                Sample.Length));

            if (retainedKilobytes <= BudgetKilobytes)
                return 0;

            Console.Error.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "RetainedMemoryAudit FAILED: {0} KB retained exceeds the {1} KB budget by {2} KB. " +
                "Something is retaining managed objects it does not need -- an eagerly-populated static " +
                "cache, or a Regex held after it was superseded, are the usual causes. See this file's " +
                "remarks for the calibration before considering raising the budget.",
                retainedKilobytes,
                BudgetKilobytes,
                retainedKilobytes - BudgetKilobytes));
            return 1;
        }
    }
}
