using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    /// <summary>
    /// Cold-start measurements. Each invocation builds a fresh <see cref="PhoneNumberUtil"/> so the
    /// embedded-resource metadata cache is empty — this is the cost a consumer pays on their first
    /// use of the library, before any region metadata has been loaded.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 1, iterationCount: 20, invocationCount: 1)]
    public class ColdStartBenchmark
    {
        // The country-code-to-region map and one fresh PhoneNumberUtil are kept around so the
        // FirstRegionLookup benchmark has a pre-constructed util whose region cache has NOT been
        // touched for the target region (we pick a region we never look up during setup).
        private PhoneNumberUtil _warmInstance = null!;
        private string[] _supportedRegions = null!;

        // Region selected for FirstRegionLookup. Chosen as a small-but-real region so its metadata
        // payload size is representative of the average region rather than an outlier like US/CN.
        private const string TargetRegion = "CH";

        // Fixed, hardcoded (region, E164 number) pairs for FirstUseValidateAndFormat -- deliberately
        // NOT derived via GetExampleNumberForType/IsValidNumber/Format at setup time, because calling
        // those would itself compile and cache that region's regex patterns before the timed run
        // starts (PhoneRegex's pattern cache is a static, process-wide ConcurrentDictionary -- see
        // PhoneRegex.cs -- so anything touched in GlobalSetup, or on an earlier iteration of this same
        // benchmark, is no longer "cold" for the rest of the process). One region per iteration, 20
        // iterations, 20 distinct regions: nothing here overlaps TargetRegion or anything Setup()
        // touches, and each entry is used at most once per benchmark run.
        private static readonly PhoneNumberBenchmarkCase[] FirstUseRegions =
        {
            new("+1 6502530000", "US"), new("+442079460958", "GB"), new("+493083050", "DE"),
            new("+33142685300", "FR"), new("+81332245000", "JP"), new("+861065525566", "CN"),
            new("+911123014000", "IN"), new("+551122222222", "BR"), new("+61261621111", "AU"),
            new("+74951234567", "RU"), new("+525512345678", "MX"), new("+27111234567", "ZA"),
            new("+82212345678", "KR"), new("+390612345678", "IT"), new("+34912345678", "ES"),
            new("+31201234567", "NL"), new("+46812345678", "SE"), new("+41441234567", "CH"),
            new("+639171234567", "PH"), new("+842812345678", "VN"),
        };

        // Advances once per FirstUseValidateAndFormat call, so BenchmarkDotNet's 20 iterations walk
        // FirstUseRegions in order without repeats -- each iteration genuinely first-touches a region
        // no earlier iteration in this process has seen.
        private int _firstUseIndex;

        [GlobalSetup]
        public void Setup()
        {
            // Force JIT of the metadata-loading path so we measure steady-state cold-start cost
            // rather than first-ever-invocation JIT noise. We deliberately use a different region
            // than TargetRegion (and never touch anything in FirstUseRegions) so those caches stay
            // cold for the benchmarks that measure them.
            _warmInstance = PhoneNumberUtil.GetInstance();
            _supportedRegions = new string[_warmInstance.GetSupportedRegions().Count];
            _warmInstance.GetSupportedRegions().CopyTo(_supportedRegions);
        }

        /// <summary>
        /// Bare construction: builds the country-code map and runs the constructor. No region
        /// metadata is loaded — that all happens lazily on first <see cref="PhoneNumberUtil.Parse"/>.
        /// </summary>
        [Benchmark]
        public PhoneNumberUtil CreateInstance()
        {
            return new PhoneNumberUtil(
                new EmbeddedResourceMetadataLoader(),
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap());
        }

        /// <summary>
        /// Construct + force-load every region's metadata. Represents a long-running process that
        /// will eventually touch every region — the total cold cost they pay across their lifetime.
        /// </summary>
        [Benchmark]
        public int CreateInstanceAndLoadAllRegions()
        {
            var util = new PhoneNumberUtil(
                new EmbeddedResourceMetadataLoader(),
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap());

            var checksum = 0;
            for (var i = 0; i < _supportedRegions.Length; i++)
            {
                var meta = util.GetMetadataForRegion(_supportedRegions[i]);
                if (meta != null)
                    checksum++;
            }
            return checksum;
        }

        /// <summary>
        /// Isolated per-region lazy load against a pre-constructed instance. Builds one fresh util
        /// per invocation so <see cref="PhoneNumberUtil.GetMetadataForRegion"/> hits the binary
        /// loader instead of the in-memory cache.
        /// </summary>
        [Benchmark]
        public PhoneMetadata FirstRegionLookup()
        {
            var util = new PhoneNumberUtil(
                new EmbeddedResourceMetadataLoader(),
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap());
            return util.GetMetadataForRegion(TargetRegion);
        }

        /// <summary>
        /// The other three benchmarks in this class only measure metadata *loading*
        /// (<see cref="PhoneNumberUtil.GetMetadataForRegion"/>) -- not Parse, IsValidNumber, or
        /// Format. Metadata loading is not where a per-region cold cost actually lives: it's
        /// consistently sub-millisecond after the first call in this class (see
        /// <see cref="FirstRegionLookup"/>). The expensive part is each region's *first* use of the
        /// validation and formatting regex patterns cached by <see cref="PhoneRegex"/> -- a
        /// process-wide, pattern-keyed cache that a fresh <see cref="PhoneNumberUtil"/> instance does
        /// not reset. A region visited for the first time anywhere in this process pays the regex
        /// JIT-compile cost (dozens of ms, not microseconds, when
        /// <see cref="InternalRegexOptions.Default"/> includes <c>RegexOptions.Compiled</c>); a
        /// region visited again anywhere in the process, even years later against a brand-new
        /// PhoneNumberUtil, is already warm.
        ///
        /// This is the specific shape of cost this class was missing: it only shows up across many
        /// *distinct, never-before-touched* regions, which is why the existing benchmarks here
        /// (single-region, metadata-only) and <see cref="PhoneNumberWorkflowBenchmark"/> (diverse
        /// regions, but pre-warmed in GlobalSetup before the timed run) both miss it. See the "regex
        /// cache" and "cold start" sections of README.md for the two real regressions this exact gap
        /// let through unnoticed for years.
        /// </summary>
        [Benchmark]
        public int FirstUseValidateAndFormat()
        {
            var region = FirstUseRegions[_firstUseIndex % FirstUseRegions.Length];
            _firstUseIndex++;

            var util = new PhoneNumberUtil(
                new EmbeddedResourceMetadataLoader(),
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap());

            var number = util.Parse(region.NumberToParse, region.DefaultRegion);
            var checksum = util.IsValidNumber(number) ? 1 : 0;
            checksum += util.Format(number, PhoneNumberFormat.NATIONAL).Length;
            checksum += util.Format(number, PhoneNumberFormat.E164).Length;
            return checksum;
        }
    }
}
