using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net10_0)]
    public class ParsingHelpersBenchmark
    {
#if NETFRAMEWORK
        private string[] _inputs = null;
        private string[] _inputsWithLeadingJunk = null;
#else
        private string[] _inputs = null!;
        private string[] _inputsWithLeadingJunk = null!;
#endif

        [Params(1000)]
        public int PhoneNumberCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var phoneNumberUtil = PhoneNumberUtil.GetInstance();
            var cases = PhoneNumberBenchmarkData.Create(phoneNumberUtil, PhoneNumberCount);

            _inputs = new string[cases.Length];
            _inputsWithLeadingJunk = new string[cases.Length];
            for (var i = 0; i < cases.Length; i++)
            {
                _inputs[i] = cases[i].NumberToParse;
                // Forces ExtractPossibleNumber to actually slice (the common "clean input" case
                // is measured separately by _inputs).
                _inputsWithLeadingJunk[i] = "abc " + cases[i].NumberToParse;
            }
        }

        [Benchmark]
        public int ExtractPossibleNumber_CleanInput()
        {
            var checksum = 0;
            for (var i = 0; i < _inputs.Length; i++)
                checksum += PhoneNumberUtil.ExtractPossibleNumber(_inputs[i]).Length;
            return checksum;
        }

        [Benchmark]
        public int ExtractPossibleNumber_WithLeadingJunk()
        {
            var checksum = 0;
            for (var i = 0; i < _inputsWithLeadingJunk.Length; i++)
                checksum += PhoneNumberUtil.ExtractPossibleNumber(_inputsWithLeadingJunk[i]).Length;
            return checksum;
        }
    }
}
