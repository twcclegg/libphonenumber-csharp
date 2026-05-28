using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net48)]
    [SimpleJob(RuntimeMoniker.Net80)]
    [SimpleJob(RuntimeMoniker.Net90)]
    public class PhoneNumberMatcherBenchmark
    {
        // Filler text interleaved between embedded numbers so the matcher has to skip non-number
        // content. Kept short to keep total input length proportional to PhoneNumberCount.
        private const string Filler = " Lorem ipsum dolor sit amet, consectetur adipiscing elit. Call ";

#if NETFRAMEWORK
        private PhoneNumberUtil _phoneNumberUtil = null;
        private string _defaultRegion = null;
        private string _text = null;
#else
        private PhoneNumberUtil _phoneNumberUtil = null!;
        private string _defaultRegion = null!;
        private string _text = null!;
#endif

        [Params(100, 1000)]
        public int PhoneNumberCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _phoneNumberUtil = PhoneNumberUtil.GetInstance();
            var cases = PhoneNumberBenchmarkData.Create(_phoneNumberUtil, PhoneNumberCount);

            // FindNumbers takes a single default region. Pick the most common one in the seed
            // set so a meaningful share of the numbers parse against region-local formats.
            _defaultRegion = cases[0].DefaultRegion;

            var sb = new StringBuilder(PhoneNumberCount * (Filler.Length + 16));
            for (var i = 0; i < cases.Length; i++)
            {
                sb.Append(Filler);
                sb.Append(cases[i].NumberToParse);
            }
            _text = sb.ToString();
        }

        [Benchmark]
        public int FindNumbers_Valid()
        {
            var checksum = 0;
            foreach (var match in _phoneNumberUtil.FindNumbers(_text, _defaultRegion))
                checksum += match.RawString.Length;
            return checksum;
        }

        // STRICT_GROUPING exercises AllNumberGroupsRemainGrouped, which the default VALID leniency
        // does not. Useful to measure the matcher's group-formatting validation path.
        [Benchmark]
        public int FindNumbers_StrictGrouping()
        {
            var checksum = 0;
            foreach (var match in _phoneNumberUtil.FindNumbers(_text, _defaultRegion,
                         PhoneNumberUtil.Leniency.STRICT_GROUPING, long.MaxValue))
                checksum += match.RawString.Length;
            return checksum;
        }
    }
}
