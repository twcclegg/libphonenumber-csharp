using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneNumbers.PerformanceTest.Benchmarks
{
    internal static class PhoneNumberBenchmarkData
    {
        private static readonly PhoneNumberType[] RepresentativeTypes =
        {
            PhoneNumberType.FIXED_LINE,
            PhoneNumberType.MOBILE,
            PhoneNumberType.FIXED_LINE_OR_MOBILE,
            PhoneNumberType.TOLL_FREE,
            PhoneNumberType.PREMIUM_RATE,
            PhoneNumberType.SHARED_COST,
            PhoneNumberType.VOIP,
            PhoneNumberType.PERSONAL_NUMBER,
            PhoneNumberType.PAGER,
            PhoneNumberType.UAN,
            PhoneNumberType.VOICEMAIL
        };

        public static PhoneNumberBenchmarkCase[] Create(PhoneNumberUtil phoneNumberUtil, int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Phone number count must be positive.");

            var seedCases = CreateSeedCases(phoneNumberUtil);
            var benchmarkCases = new PhoneNumberBenchmarkCase[count];

            for (var i = 0; i < benchmarkCases.Length; i++)
                benchmarkCases[i] = seedCases[i % seedCases.Count];

            return benchmarkCases;
        }

        private static List<PhoneNumberBenchmarkCase> CreateSeedCases(PhoneNumberUtil phoneNumberUtil)
        {
            var seedCases = new List<PhoneNumberBenchmarkCase>();
            var seenNumbers = new HashSet<string>(StringComparer.Ordinal);
            var supportedRegions = phoneNumberUtil.GetSupportedRegions()
                .OrderBy(regionCode => regionCode, StringComparer.Ordinal);

            foreach (var regionCode in supportedRegions)
            {
                foreach (var type in RepresentativeTypes)
                {
                    var exampleNumber = phoneNumberUtil.GetExampleNumberForType(regionCode, type);

                    if (exampleNumber == null || !phoneNumberUtil.IsValidNumber(exampleNumber))
                        continue;

                    var numberToParse = phoneNumberUtil.Format(exampleNumber, PhoneNumberFormat.E164);
                    var key = regionCode + "\0" + numberToParse;

                    if (seenNumbers.Add(key))
                        seedCases.Add(new PhoneNumberBenchmarkCase(numberToParse, regionCode));
                }
            }

            if (seedCases.Count == 0)
                throw new InvalidOperationException("Unable to create phone number performance benchmark data.");

            return seedCases;
        }
    }
}
