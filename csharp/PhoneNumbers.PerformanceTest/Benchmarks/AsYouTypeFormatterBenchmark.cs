using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net10_0)]
    public class AsYouTypeFormatterBenchmark
    {
        private PhoneNumberUtil _phoneNumberUtil = null!;
        private PhoneNumberBenchmarkCase[] _phoneNumbers = null!;

        [Params(1000)]
        public int PhoneNumberCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _phoneNumberUtil = PhoneNumberUtil.GetInstance();
            _phoneNumbers = PhoneNumberBenchmarkData.Create(_phoneNumberUtil, PhoneNumberCount);
        }

        [Benchmark]
        public int InputDigitPerKeystroke()
        {
            var checksum = 0;

            for (var i = 0; i < _phoneNumbers.Length; i++)
            {
                var phoneNumber = _phoneNumbers[i];
                var formatter = _phoneNumberUtil.GetAsYouTypeFormatter(phoneNumber.DefaultRegion);

                var input = phoneNumber.NumberToParse;
                for (var c = 0; c < input.Length; c++)
                    checksum += formatter.InputDigit(input[c]).Length;
            }

            return checksum;
        }
    }
}
