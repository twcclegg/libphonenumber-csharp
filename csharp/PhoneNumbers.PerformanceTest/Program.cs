using BenchmarkDotNet.Running;
using PhoneNumbers.PerformanceTest.Benchmarks;

namespace PhoneNumbers.PerformanceTest
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<PhoneNumberFormatBenchmark>();

            BenchmarkRunner.Run<PhoneNumberParseBenchmark>();
        }
    }
}
