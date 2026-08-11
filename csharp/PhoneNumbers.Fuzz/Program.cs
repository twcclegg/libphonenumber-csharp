using System;
using System.Linq;
using System.Text;
using SharpFuzz;

namespace PhoneNumbers.Fuzz
{
    /// <summary>
    /// Coverage-guided fuzz target for the untrusted-input surface named in SECURITY.md: the strings
    /// callers pass to the public API. libFuzzer drives this through the libfuzzer-dotnet bridge -
    /// see README.md in this directory for how to run it.
    ///
    /// Anything thrown out of <see cref="Fuzz"/> is reported as a crash, so the expected failure
    /// (NumberParseException for input that is not a phone number) is caught here and everything
    /// else is left to escape.
    /// </summary>
    internal static class Program
    {
        private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

        /// <summary>Ordered so a given input byte always selects the same region.</summary>
        private static readonly string[] Regions =
            PhoneUtil.GetSupportedRegions().OrderBy(r => r, StringComparer.Ordinal).ToArray();

        private static readonly PhoneNumberOfflineGeocoder Geocoder = PhoneNumberOfflineGeocoder.GetInstance();
        private static readonly PhoneNumberToCarrierMapper CarrierMapper = PhoneNumberToCarrierMapper.GetInstance();
        private static readonly PhoneNumberToTimeZonesMapper TimeZonesMapper = PhoneNumberToTimeZonesMapper.GetInstance();
        private static readonly ShortNumberInfo ShortInfo = ShortNumberInfo.GetInstance();

        public static void Main() => Fuzzer.LibFuzzer.Run(Fuzz);

        private static void Fuzz(ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
                return;

            // First byte picks the default region, the rest is the number. Giving the fuzzer a byte
            // to steer with is what lets it reach region-specific parsing and formatting branches.
            var region = Regions[span[0] % Regions.Length];
            var input = Encoding.UTF8.GetString(span.Slice(1));

            PhoneNumberUtil.Normalize(input);
            PhoneNumberUtil.NormalizeDigitsOnly(input);
            PhoneNumberUtil.NormalizeDiallableCharsOnly(input);
            PhoneNumberUtil.ConvertAlphaCharactersInNumber(input);
            PhoneNumberUtil.IsViablePhoneNumber(input);
            PhoneNumberUtil.ExtractPossibleNumber(input);

            FindNumbers(input, region);
            FormatAsYouType(input, region);

            PhoneNumber number;
            try
            {
                number = PhoneUtil.Parse(input, region);
            }
            catch (NumberParseException)
            {
                return;
            }

            ExerciseReadOnlySurface(number);
        }

        private static void FindNumbers(string input, string region)
        {
            foreach (var _ in PhoneUtil.FindNumbers(input, region))
            {
                // Enumerating is the point: the matcher does its work lazily.
            }
        }

        private static void FormatAsYouType(string input, string region)
        {
            var formatter = PhoneUtil.GetAsYouTypeFormatter(region);

            // Bounded because the formatter is per-keystroke: without a cap a long input turns a
            // single fuzz iteration into thousands of calls and starves the rest of the target.
            for (var i = 0; i < input.Length && i < 200; i++)
                formatter.InputDigit(input[i]);
        }

        private static void ExerciseReadOnlySurface(PhoneNumber number)
        {
            PhoneUtil.IsValidNumber(number);
            PhoneUtil.IsPossibleNumber(number);
            PhoneUtil.GetNumberType(number);
            PhoneUtil.GetRegionCodeForNumber(number);

            PhoneUtil.Format(number, PhoneNumberFormat.E164);
            PhoneUtil.Format(number, PhoneNumberFormat.INTERNATIONAL);
            PhoneUtil.Format(number, PhoneNumberFormat.NATIONAL);
            PhoneUtil.Format(number, PhoneNumberFormat.RFC3966);
            PhoneUtil.FormatOutOfCountryCallingNumber(number, "US");

            Geocoder.GetDescriptionForNumber(number, Locale.English);
            CarrierMapper.GetNameForNumber(number, Locale.English);
            TimeZonesMapper.GetTimeZonesForNumber(number);
            ShortInfo.IsPossibleShortNumber(number);
        }
    }
}
