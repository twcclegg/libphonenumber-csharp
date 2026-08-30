using System;
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

        // Same reasoning as FirstUseRegions, for AsYouTypeFormatter, PhoneNumberMatcher, and
        // PhoneNumberOfflineGeocoder respectively -- each pool is disjoint from FirstUseRegions AND
        // from every other pool below, since all benchmark classes in this project run inside one
        // BenchmarkDotNet process: a region touched by one class's cold-start benchmark is no longer
        // cold for another class's, if they overlap. Numbers generated via
        // GetExampleNumberForType/Format(E164) against an ad hoc instance outside this project, then
        // hardcoded here -- the same reason FirstUseRegions doesn't derive them at Setup time applies.
        private static readonly PhoneNumberBenchmarkCase[] FirstUseAsYouTypeRegions =
        {
            new("+48123456789", "PL"), new("+902123456789", "TR"), new("+6621234567", "TH"),
            new("+62218350123", "ID"), new("+60323856789", "MY"), new("+6561234567", "SG"),
            new("+2342033123456", "NG"), new("+254202012345", "KE"), new("+20234567890", "EG"),
            new("+966112345678", "SA"), new("+97122345678", "AE"), new("+922123456789", "PK"),
            new("+88027111234", "BD"), new("+380311234567", "UA"), new("+302123456789", "GR"),
            // Padding out to 20 entries -- see this file's class-level remarks on why every FirstUse*
            // pool must have (at least) as many entries as ColdStartBenchmark's iterationCount.
            new("+951234567", "MM"), new("+85523756789", "KH"), new("+85621212862", "LA"),
            new("+97653123456", "MN"), new("+94112345678", "LK"),
        };

        private static readonly PhoneNumberBenchmarkCase[] FirstUseMatcherRegions =
        {
            new("+15062345678", "CA"), new("+541123456789", "AR"), new("+576012345678", "CO"),
            new("+5111234567", "PE"), new("+56600123456", "CL"), new("+582121234567", "VE"),
            new("+59322123456", "EC"), new("+59821231234", "UY"), new("+595212345678", "PY"),
            new("+59122123456", "BO"), new("+351212345678", "PT"), new("+3212345678", "BE"),
            new("+431234567890", "AT"), new("+4532123456", "DK"), new("+4721234567", "NO"),
            // Padding out to 20 entries -- see this file's class-level remarks on why every FirstUse*
            // pool must have (at least) as many entries as ColdStartBenchmark's iterationCount.
            new("+50422123456", "HN"), new("+50222456789", "GT"), new("+50622123456", "CR"),
            new("+5072001234", "PA"), new("+18092345678", "DO"),
        };

        private static readonly PhoneNumberBenchmarkCase[] FirstUseGeocoderRegions =
        {
            new("+420212345678", "CZ"), new("+3612345678", "HU"), new("+40211234567", "RO"),
            new("+358131234567", "FI"), new("+3532212345", "IE"), new("+6432345678", "NZ"),
            new("+97221234567", "IL"), new("+85221234567", "HK"), new("+886221234567", "TW"),
            new("+85328212345", "MO"), new("+97444123456", "QA"), new("+96522345678", "KW"),
            new("+212520123456", "MA"), new("+21312345678", "DZ"), new("+21630010123", "TN"),
            // Padding out to 20 entries -- see this file's class-level remarks on why every FirstUse*
            // pool must have (at least) as many entries as ColdStartBenchmark's iterationCount.
            new("+96262001234", "JO"), new("+9611123456", "LB"), new("+96823123456", "OM"),
            new("+97317001234", "BH"), new("+35722345678", "CY"),
        };

        // The private constant PhoneNumberOfflineGeocoder.GetInstance() passes its own constructor --
        // duplicated here since it's private, not internal, so InternalsVisibleTo doesn't reach it.
        private const string GeocodingDataDirectory = "geocoding.";

        // Must match the `iterationCount` on this class's [SimpleJob] attribute above. BenchmarkDotNet's
        // ColdStart strategy runs all IterationCount iterations inside one process (LaunchCount is 1,
        // not increased for ColdStart), so PhoneRegex's static, process-wide pattern cache persists
        // across every iteration of a given benchmark method within that run -- a FirstUse* pool with
        // fewer than IterationCount entries wraps around via modulo and silently re-touches an
        // already-warm region on the later iterations, contaminating the exact "first use" measurement
        // this class exists to isolate (verified via CheckPoolSize below after this was found missing
        // from three of the four pools -- FirstUseAsYouTypeRegions/FirstUseMatcherRegions/
        // FirstUseGeocoderRegions each originally had only 15 entries against a 20-iteration job).
        private const int IterationCount = 20;

        private static void CheckPoolSize(PhoneNumberBenchmarkCase[] pool, string poolName)
        {
            if (pool.Length < IterationCount)
                throw new InvalidOperationException(
                    $"{poolName} has only {pool.Length} entries but ColdStartBenchmark's iterationCount " +
                    $"is {IterationCount} -- the pool would wrap around and re-touch an already-warm " +
                    "region partway through the run, silently contaminating the 'first use' measurement " +
                    "this class exists to isolate. Add more entries rather than lowering iterationCount.");
        }

        // Advances once per call to the matching FirstUse* benchmark below, so BenchmarkDotNet's 20
        // iterations walk each pool in order without repeats -- each iteration genuinely first-touches
        // a region no earlier iteration, in any of these benchmarks, has seen in this process.
        private int _firstUseIndex;
        private int _firstUseAsYouTypeIndex;
        private int _firstUseMatcherIndex;
        private int _firstUseGeocoderIndex;

        [GlobalSetup]
        public void Setup()
        {
            CheckPoolSize(FirstUseRegions, nameof(FirstUseRegions));
            CheckPoolSize(FirstUseAsYouTypeRegions, nameof(FirstUseAsYouTypeRegions));
            CheckPoolSize(FirstUseMatcherRegions, nameof(FirstUseMatcherRegions));
            CheckPoolSize(FirstUseGeocoderRegions, nameof(FirstUseGeocoderRegions));

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

        /// <summary>
        /// Same gap as <see cref="FirstUseValidateAndFormat"/>, for <see cref="AsYouTypeFormatter"/>.
        /// <see cref="AsYouTypeFormatterBenchmark"/> warms every region's formatter in GlobalSetup
        /// before its timed loop starts, so it cannot see this cost either. A fresh formatter is
        /// still built against the shared <see cref="_warmInstance"/> here (matching how a real
        /// caller would use <see cref="PhoneNumberUtil.GetInstance"/>) -- what's cold is only the
        /// region's pattern, on a region <see cref="FirstUseAsYouTypeRegions"/> guarantees no other
        /// benchmark in this process has touched.
        /// </summary>
        [Benchmark]
        public int FirstUseAsYouType()
        {
            var region = FirstUseAsYouTypeRegions[_firstUseAsYouTypeIndex % FirstUseAsYouTypeRegions.Length];
            _firstUseAsYouTypeIndex++;

            var formatter = _warmInstance.GetAsYouTypeFormatter(region.DefaultRegion);
            var checksum = 0;
            var input = region.NumberToParse;
            for (var c = 0; c < input.Length; c++)
                checksum += formatter.InputDigit(input[c]).Length;
            return checksum;
        }

        /// <summary>
        /// Same gap as <see cref="FirstUseValidateAndFormat"/>, for <see cref="PhoneNumberUtil.FindNumbers"/>
        /// (backed by <see cref="PhoneNumberMatcher"/>). <see cref="PhoneNumberMatcherBenchmark"/> warms
        /// its one fixed default region in GlobalSetup; here each invocation searches text for a number
        /// from a region <see cref="FirstUseMatcherRegions"/> guarantees is still cold.
        /// </summary>
        [Benchmark]
        public int FirstUseFindNumbers()
        {
            var region = FirstUseMatcherRegions[_firstUseMatcherIndex % FirstUseMatcherRegions.Length];
            _firstUseMatcherIndex++;

            var util = new PhoneNumberUtil(
                new EmbeddedResourceMetadataLoader(),
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap());

            var text = $"Call me at {region.NumberToParse} tomorrow.";
            var checksum = 0;
            foreach (var match in util.FindNumbers(text, region.DefaultRegion))
                checksum += match.RawString.Length;
            return checksum;
        }

        /// <summary>
        /// Same gap as <see cref="FirstUseValidateAndFormat"/>, for
        /// <see cref="PhoneNumberOfflineGeocoder"/>. Architecturally distinct from the other
        /// FirstUse* benchmarks -- the geocoder's per-region cost is a lazily-loaded prefix map
        /// (<see cref="PrefixFileReader"/>'s <c>ConcurrentDictionary&lt;string, Lazy&lt;AreaCodeMap&gt;&gt;</c>),
        /// not a <see cref="PhoneRegex"/> pattern compile -- but it shares the same structural blind
        /// spot: <see cref="PhoneNumberOfflineGeocoderBenchmark"/> explicitly warms every prefix map
        /// in GlobalSetup before its timed loop starts. Included for symmetry/completeness across the
        /// library's other region-keyed subsystems, using the internal constructor (see
        /// <c>InternalsVisibleTo</c> in Util.cs) so each invocation gets a geocoder whose prefix-map
        /// cache has never seen <see cref="FirstUseGeocoderRegions"/>.
        /// </summary>
        [Benchmark]
        public int FirstUseGeocode()
        {
            var region = FirstUseGeocoderRegions[_firstUseGeocoderIndex % FirstUseGeocoderRegions.Length];
            _firstUseGeocoderIndex++;

            var util = new PhoneNumberUtil(
                new EmbeddedResourceMetadataLoader(),
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap());
            var geocoder = new PhoneNumberOfflineGeocoder(GeocodingDataDirectory);

            var number = util.Parse(region.NumberToParse, region.DefaultRegion);
            return geocoder.GetDescriptionForNumber(number, Locale.English).Length;
        }
    }
}
