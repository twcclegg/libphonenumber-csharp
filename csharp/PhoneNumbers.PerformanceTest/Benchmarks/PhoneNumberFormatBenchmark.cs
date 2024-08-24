using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net48)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class PhoneNumberFormatBenchmark
    {
#if NETFRAMEWORK
        private PhoneNumberUtil _phoneNumberUtil = null;
#else
        private PhoneNumberUtil _phoneNumberUtil = null!;
#endif

#if NETFRAMEWORK
        private PhoneNumber _phoneNumber;
#else
        private PhoneNumber _phoneNumber = null!;
#endif
        [GlobalSetup]
        public void Setup()
        {
            _phoneNumberUtil = PhoneNumberUtil.GetInstance();
            _phoneNumber = _phoneNumberUtil.Parse("+14156667777", "US");
        }

        [Benchmark]
        public void FormatPhoneNumber()
        {
            _phoneNumberUtil.Format(_phoneNumber, PhoneNumberFormat.INTERNATIONAL);
        }
    }
}
