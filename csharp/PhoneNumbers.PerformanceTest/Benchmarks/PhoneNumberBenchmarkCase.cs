namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    internal struct PhoneNumberBenchmarkCase
    {
        public PhoneNumberBenchmarkCase(string numberToParse, string defaultRegion)
        {
            NumberToParse = numberToParse;
            DefaultRegion = defaultRegion;
        }

        public string NumberToParse { get; }

        public string DefaultRegion { get; }
    }
}
