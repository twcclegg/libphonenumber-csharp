using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    /// <summary>
    /// The end-to-end workflow plus each of its three phases measured separately, so a cost can be
    /// attributed rather than only totalled. Parse and Format allocate for quite different reasons -
    /// StringBuilder round trips on one side, regex replacement producing a fresh string per step on
    /// the other - and those want different fixes.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net10_0)]
    public class PhoneNumberWorkflowBenchmark
    {
        private PhoneNumberUtil _phoneNumberUtil = null!;
        private PhoneNumberBenchmarkCase[] _phoneNumbers = null!;
        private PhoneNumber[] _parsedNumbers = null!;
        private PhoneNumberBenchmarkCase[] _nationalFormat = null!;
        private PhoneNumberBenchmarkCase[] _withExtension = null!;

        [Params(1000)]
        public int PhoneNumberCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _phoneNumberUtil = PhoneNumberUtil.GetInstance();
            _phoneNumbers = PhoneNumberBenchmarkData.Create(_phoneNumberUtil, PhoneNumberCount);

            // Pre-parsed, so the validate and format phases measure only their own work.
            _parsedNumbers = new PhoneNumber[_phoneNumbers.Length];
            for (var i = 0; i < _phoneNumbers.Length; i++)
            {
                _parsedNumbers[i] =
                    _phoneNumberUtil.Parse(_phoneNumbers[i].NumberToParse, _phoneNumbers[i].DefaultRegion);
            }

            // The seed data is all E164, which is the cheapest thing Parse can be handed: no national
            // prefix to strip and no extension, so the two regex matches that dominate a real parse both
            // fail and cost nothing. Re-render the same numbers the way callers usually supply them.
            _nationalFormat = new PhoneNumberBenchmarkCase[_parsedNumbers.Length];
            _withExtension = new PhoneNumberBenchmarkCase[_parsedNumbers.Length];
            for (var i = 0; i < _parsedNumbers.Length; i++)
            {
                var region = _phoneNumbers[i].DefaultRegion;
                _nationalFormat[i] = new PhoneNumberBenchmarkCase(
                    _phoneNumberUtil.Format(_parsedNumbers[i], PhoneNumberFormat.NATIONAL), region);

                var extended = new PhoneNumber.Builder().MergeFrom(_parsedNumbers[i])
                    .SetExtension("123").Build();
                _withExtension[i] = new PhoneNumberBenchmarkCase(
                    _phoneNumberUtil.Format(extended, PhoneNumberFormat.INTERNATIONAL), region);
            }
        }

        /// <summary>
        /// Kept alongside the phases: parsing warms metadata the later phases reuse, so the whole is
        /// not simply the sum, and this is the figure tracked across previous changes.
        /// </summary>
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

        [Benchmark]
        public int ParseOnly()
        {
            var checksum = 0;
            for (var i = 0; i < _phoneNumbers.Length; i++)
            {
                var phoneNumber = _phoneNumbers[i];
                checksum += _phoneNumberUtil
                    .Parse(phoneNumber.NumberToParse, phoneNumber.DefaultRegion).CountryCode;
            }

            return checksum;
        }

        /// <summary>
        /// The same numbers in national format, so the national prefix actually gets stripped. That
        /// path costs roughly twice what an E164 parse does, and it is what most callers hand in.
        /// </summary>
        [Benchmark]
        public int ParseNationalFormat()
        {
            var checksum = 0;
            for (var i = 0; i < _nationalFormat.Length; i++)
            {
                var phoneNumber = _nationalFormat[i];
                checksum += _phoneNumberUtil
                    .Parse(phoneNumber.NumberToParse, phoneNumber.DefaultRegion).CountryCode;
            }

            return checksum;
        }

        /// <summary>
        /// With an extension, so the extension pattern matches rather than failing. A successful match
        /// on that alternation is by far the most expensive thing a parse can do.
        /// </summary>
        [Benchmark]
        public int ParseWithExtension()
        {
            var checksum = 0;
            for (var i = 0; i < _withExtension.Length; i++)
            {
                var phoneNumber = _withExtension[i];
                checksum += _phoneNumberUtil
                    .Parse(phoneNumber.NumberToParse, phoneNumber.DefaultRegion).CountryCode;
            }

            return checksum;
        }

        [Benchmark]
        public int ValidateOnly()
        {
            var checksum = 0;
            for (var i = 0; i < _parsedNumbers.Length; i++)
            {
                if (_phoneNumberUtil.IsValidNumber(_parsedNumbers[i]))
                    checksum++;
            }

            return checksum;
        }

        /// <summary>
        /// INTERNATIONAL rather than E164: E164 takes an early exit that skips pattern formatting,
        /// so it would not exercise the regex replacement chain in FormatNsnUsingPattern.
        /// </summary>
        [Benchmark]
        public int FormatOnly()
        {
            var checksum = 0;
            for (var i = 0; i < _parsedNumbers.Length; i++)
            {
                checksum += _phoneNumberUtil.Format(_parsedNumbers[i], PhoneNumberFormat.INTERNATIONAL).Length;
            }

            return checksum;
        }
    }
}
