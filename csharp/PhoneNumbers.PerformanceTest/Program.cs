using BenchmarkDotNet.Running;

namespace PhoneNumbers.PerformanceTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<PhoneNumberFormatBenchmark>();
        }
    }
}
