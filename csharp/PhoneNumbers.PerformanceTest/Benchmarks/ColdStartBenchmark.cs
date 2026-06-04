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

        [GlobalSetup]
        public void Setup()
        {
            // Force JIT of the metadata-loading path so we measure steady-state cold-start cost
            // rather than first-ever-invocation JIT noise. We deliberately use a different region
            // than TargetRegion so the per-region cache stays cold for that one in FirstRegionLookup.
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
    }
}
