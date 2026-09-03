using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    /// <summary>
    /// What a consumer pays between process start and their first answer, including every static
    /// initializer on the way there.
    /// <para>
    /// <see cref="ColdStartBenchmark"/> cannot see that cost, and neither can
    /// <see cref="PhoneNumberWorkflowBenchmark"/>: a BenchmarkDotNet job runs its warmup and all
    /// measured iterations in one process, and a static initializer runs at most once per process, so
    /// the warmup iteration absorbs it and no measured iteration can see it again. This job uses
    /// <c>launchCount: 20, warmupCount: 0, iterationCount: 1, invocationCount: 1</c> so that the
    /// measured call is the first thing its process does.
    /// </para>
    /// <para>
    /// Each process actually invokes the method twice, not once -- MemoryDiagnoser performs its own
    /// extra run, in the same process, after the measured one (verified by logging pid and method name
    /// from inside the benchmark: 20 processes per method, two invocations each). That does not
    /// compromise the Mean, which is taken from the first, genuinely cold call; it is why the Allocated
    /// column is meaningless here, since the diagnoser's run happens once the static initializers have
    /// completed.
    /// </para>
    /// <para>
    /// <b>Read the Mean column, and read it as a trend.</b> Allocated is actively misleading: across a
    /// change that added 3.4 MB of permanently-retained static state it moved only 129 KB -> 144 KB.
    /// Retained memory is covered by <c>RetainedMemoryAudit</c> instead. The Ratio column is not
    /// dependable either -- at one invocation per process the baseline's own ratio has been observed at
    /// 1.01 with RatioSD 0.15, and Ratio answers a different question from the one the baseline is here
    /// to answer (see <see cref="ConstructUtilFromProcessStart"/>): compare the Means by subtraction.
    /// </para>
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.Net10_0, launchCount: 20, warmupCount: 0, iterationCount: 1, invocationCount: 1)]
    public class ProcessStartBenchmark
    {
        private const string Region = "CH";
        private const string NumberToParse = "+41441234567";

        // No [GlobalSetup]: anything there would run before the measured call and absorb exactly the
        // static-initialization cost this class exists to report.

        /// <summary>
        /// Metadata loading only, without touching PhoneRegex's pattern cache. Subtract this Mean from
        /// <see cref="SingleNumberFromProcessStart"/>'s to isolate what first touching that cache costs
        /// -- a difference, not the Ratio column, which reports a ratio and is unreliable at one
        /// invocation per process.
        /// </summary>
        [Benchmark(Baseline = true)]
        public PhoneMetadata ConstructUtilFromProcessStart()
        {
            // See ColdStartBenchmark on why this loader is constructed directly.
#pragma warning disable CS0618
            var util = new PhoneNumberUtil(
                new EmbeddedResourceMetadataLoader(),
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap());
#pragma warning restore CS0618
            return util.GetMetadataForRegion(Region);
        }

        /// <summary>
        /// What a one-shot consumer does: start, parse one number, validate, format, exit. Everything
        /// is cold -- metadata, the pattern cache, and every static initializer behind both.
        /// </summary>
        [Benchmark]
        public string SingleNumberFromProcessStart()
        {
#pragma warning disable CS0618
            var util = new PhoneNumberUtil(
                new EmbeddedResourceMetadataLoader(),
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap());
#pragma warning restore CS0618

            var number = util.Parse(NumberToParse, Region);
            return util.IsValidNumber(number)
                ? util.Format(number, PhoneNumberFormat.INTERNATIONAL)
                : string.Empty;
        }
    }
}
