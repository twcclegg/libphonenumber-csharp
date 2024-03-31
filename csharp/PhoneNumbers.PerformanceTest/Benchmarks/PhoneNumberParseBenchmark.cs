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
        private PhoneNumberUtil phoneNumberUtil;

        [GlobalSetup]
        public void Setup()
        {
            phoneNumberUtil = PhoneNumberUtil.GetInstance();
        }

        [Benchmark]
        public void ParsePhoneNumber()
        {
            phoneNumberUtil.Parse("+14156667777", "US");
        }
    }
}
