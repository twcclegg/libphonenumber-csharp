using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net48)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net70)]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class PhoneNumberParseBenchmark
    {
#if NETFRAMEWORK
        private PhoneNumberUtil _phoneNumberUtil = null;
#else
        private PhoneNumberUtil _phoneNumberUtil = null!;
#endif

        [GlobalSetup]
        public void Setup()
        {
            _phoneNumberUtil = PhoneNumberUtil.GetInstance();
        }

        [Benchmark]
        public void ParsePhoneNumber()
        {
            _phoneNumberUtil.Parse("+14156667777", "US");
        }
    }
}
