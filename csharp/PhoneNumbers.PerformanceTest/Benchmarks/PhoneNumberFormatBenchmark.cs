using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net48)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net70)]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class PhoneNumberFormatBenchmark
    {
        private PhoneNumberUtil phoneNumberUtil;
        private PhoneNumber phoneNumber;

        [GlobalSetup]
        public void Setup()
        {
            phoneNumberUtil = PhoneNumberUtil.GetInstance();
            phoneNumber = phoneNumberUtil.Parse("+14156667777", "US");
        }

        [Benchmark]
        public void FormatPhoneNumber()
        {
            phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.INTERNATIONAL);
        }
    }
}


