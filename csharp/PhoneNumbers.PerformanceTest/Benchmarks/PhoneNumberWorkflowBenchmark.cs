using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net48)]
    [SimpleJob(RuntimeMoniker.Net80)]
    [SimpleJob(RuntimeMoniker.Net90)]
    public class PhoneNumberWorkflowBenchmark
    {
#if NETFRAMEWORK
        private PhoneNumberUtil _phoneNumberUtil = null;
        private PhoneNumberBenchmarkCase[] _phoneNumbers = null;
#else
        private PhoneNumberUtil _phoneNumberUtil = null!;
        private PhoneNumberBenchmarkCase[] _phoneNumbers = null!;
#endif

        [Params(1000, 10000, 100000)]
        public int PhoneNumberCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _phoneNumberUtil = PhoneNumberUtil.GetInstance();
            _phoneNumbers = PhoneNumberBenchmarkData.Create(_phoneNumberUtil, PhoneNumberCount);
        }

        [Benchmark]
        public int ParseValidateAndFormatPhoneNumbers()
        {
            var checksum = 0;

            for (var i = 0; i < _phoneNumbers.Length; i++)
            {
                var phoneNumber = _phoneNumbers[i];
                var parsedNumber = _phoneNumberUtil.Parse(phoneNumber.NumberToParse, phoneNumber.DefaultRegion);

                if (_phoneNumberUtil.IsValidNumber(parsedNumber))
                    checksum++;

                checksum += _phoneNumberUtil.Format(parsedNumber, PhoneNumberFormat.INTERNATIONAL).Length;
            }

            return checksum;
        }
    }
}
