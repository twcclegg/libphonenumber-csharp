using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    /// <summary>
    /// <c>PhoneNumberParser.TryParse</c> from the Extensions package, split by outcome. A failed
    /// parse used to cost as much as a successful one because every failure went through
    /// <see cref="NumberParseException"/>; the rejection cases show what the up-front check saves,
    /// and <see cref="TryParse_UnparseableAfterPrecheck"/> shows the exception path that remains
    /// for input the check cannot rule out.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net10_0)]
    public class ExtensionsTryParseBenchmark
    {
        private PhoneNumberBenchmarkCase[] _valid = null!;
        private string[] _nationalWithoutRegion = null!;
        private string[] _text = null!;
        private string[] _unparseable = null!;

        [Params(1000)]
        public int PhoneNumberCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var phoneNumberUtil = PhoneNumberUtil.GetInstance();
            _valid = PhoneNumberBenchmarkData.Create(phoneNumberUtil, PhoneNumberCount);

            _nationalWithoutRegion = new string[_valid.Length];
            _text = new string[_valid.Length];
            _unparseable = new string[_valid.Length];
            string[] words = { "junk", "n/a", "none", "unknown", "call me", "TBD", "-", "see notes" };
            for (var i = 0; i < _valid.Length; i++)
            {
                var number = phoneNumberUtil.Parse(_valid[i].NumberToParse, null);
                _nationalWithoutRegion[i] = phoneNumberUtil.Format(number, PhoneNumberFormat.NATIONAL);
                _text[i] = words[i % words.Length];
                // A plus and enough digits to pass the check, but no such country calling code.
                _unparseable[i] = "+999 " + number.NationalNumber;
            }

            // Warm the regions so the valid case measures parsing, not first-use metadata loads.
            TryParse_Valid();
        }

        [Benchmark(Baseline = true)]
        public int TryParse_Valid()
        {
            var parsed = 0;
            for (var i = 0; i < _valid.Length; i++)
            {
                if (PhoneNumberParser.TryParse(_valid[i].NumberToParse, _valid[i].DefaultRegion, out _))
                    parsed++;
            }
            return parsed;
        }

        [Benchmark]
        public int TryParse_Empty()
        {
            var parsed = 0;
            for (var i = 0; i < _valid.Length; i++)
            {
                if (PhoneNumberParser.TryParse("", "US", out _))
                    parsed++;
            }
            return parsed;
        }

        [Benchmark]
        public int TryParse_Text()
        {
            var parsed = 0;
            for (var i = 0; i < _text.Length; i++)
            {
                if (PhoneNumberParser.TryParse(_text[i], "US", out _))
                    parsed++;
            }
            return parsed;
        }

        [Benchmark]
        public int TryParse_NationalWithoutRegion()
        {
            var parsed = 0;
            for (var i = 0; i < _nationalWithoutRegion.Length; i++)
            {
                if (PhoneNumberParser.TryParse(_nationalWithoutRegion[i], null, out _))
                    parsed++;
            }
            return parsed;
        }

        [Benchmark]
        public int TryParse_UnparseableAfterPrecheck()
        {
            var parsed = 0;
            for (var i = 0; i < _unparseable.Length; i++)
            {
                if (PhoneNumberParser.TryParse(_unparseable[i], null, out _))
                    parsed++;
            }
            return parsed;
        }
    }
}
