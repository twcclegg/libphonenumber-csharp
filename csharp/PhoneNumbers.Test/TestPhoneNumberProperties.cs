using System;
using System.Globalization;
using System.Linq;
// FsCheck is imported even where only the [Property] attribute is used: the OpenSSF Scorecard
// fuzzing check detects .NET property-based testing by matching "using FsCheck;" (or
// "using FsCheck.Xunit;") in a .cs file, so removing either import silently drops that check to
// zero. See https://github.com/ossf/scorecard/blob/main/docs/checks.md#fuzzing.
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// The property-based counterpart to <see cref="TestPublicApiRobustness"/>. That class feeds a
    /// curated list of hostile strings through the public surface; this one generates them, so the
    /// space between the hand-picked cases is covered too, and a failure shrinks to a minimal input.
    ///
    /// Two kinds of property live here. Most assert only that a call fails in a documented way
    /// rather than crashing, which is the same contract the curated tests check. The last few assert
    /// real invariants - E.164 round-trips, validity implying possibility, match symmetry - and a
    /// failure in those is a genuine bug rather than a hostile-input gap.
    ///
    /// Uses the production metadata, since that is what ships and is where the awkward regions live.
    /// </summary>
    public class TestPhoneNumberProperties
    {
        private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

        /// <summary>Ordered so a shrunk counterexample names the same region on every run.</summary>
        private static readonly string[] Regions =
            PhoneUtil.GetSupportedRegions().OrderBy(r => r, StringComparer.Ordinal).ToArray();

        /// <summary>Region codes a caller might plausibly pass, valid or not.</summary>
        private static readonly string[] CallerRegions =
            Regions.Concat(new[] { "", " ", "ZZ", "XX", "us", "001", "USA" }).ToArray();

        [Property(MaxTest = 500)]
        public void ParseFailsOnlyWithNumberParseException(string? input, int regionSeed)
        {
            var region = CallerRegions[Mod(regionSeed, CallerRegions.Length)];

            try
            {
                PhoneUtil.Parse(input, region);
            }
            catch (NumberParseException)
            {
                // The documented failure for unparseable input.
            }

            try
            {
                PhoneUtil.ParseAndKeepRawInput(input, region);
            }
            catch (NumberParseException)
            {
                // The documented failure for unparseable input.
            }
        }

        [Property(MaxTest = 500)]
        public void NormalizersNeverThrow(string? input)
        {
            var text = input ?? "";

            PhoneNumberUtil.Normalize(text);
            PhoneNumberUtil.NormalizeDigitsOnly(text);
            PhoneNumberUtil.NormalizeDiallableCharsOnly(text);
            PhoneNumberUtil.ConvertAlphaCharactersInNumber(text);
            PhoneNumberUtil.IsViablePhoneNumber(text);
            PhoneNumberUtil.ExtractPossibleNumber(text);
        }

        /// <summary>
        /// Normalizing already-normalized output must be a no-op. A normalizer that keeps rewriting
        /// its own output would make callers that normalize defensively behave differently from
        /// callers that do not.
        /// </summary>
        [Property(MaxTest = 500)]
        public void NormalizeIsIdempotent(string? input)
        {
            var once = PhoneNumberUtil.Normalize(input ?? "");
            Assert.Equal(once, PhoneNumberUtil.Normalize(once));

            var digits = PhoneNumberUtil.NormalizeDigitsOnly(input ?? "");
            Assert.Equal(digits, PhoneNumberUtil.NormalizeDigitsOnly(digits));
        }

        [Property(MaxTest = 200)]
        public void FindNumbersNeverThrows(string? haystack, int regionSeed)
        {
            var region = CallerRegions[Mod(regionSeed, CallerRegions.Length)];

            foreach (var _ in PhoneUtil.FindNumbers(haystack ?? "", region))
            {
                // Enumerating is the point: the matcher does its work lazily.
            }
        }

        [Property(MaxTest = 200)]
        public void AsYouTypeFormatterNeverThrows(string? input, int regionSeed)
        {
            var region = CallerRegions[Mod(regionSeed, CallerRegions.Length)];
            var formatter = PhoneUtil.GetAsYouTypeFormatter(region);
            var text = input ?? "";

            // Per-keystroke by design, and long inputs are covered by the normalizers, so a bounded
            // prefix reaches every branch without making the property slow.
            for (var i = 0; i < text.Length && i < 200; i++)
                formatter.InputDigit(text[i]);
        }

        /// <summary>
        /// The shape that caught the geocoder throwing KeyNotFoundException for Ascension Island,
        /// generalised from example numbers to mutations of them.
        /// </summary>
        [Property(MaxTest = 200)]
        public void ReadOnlySurfaceNeverThrows(int regionSeed, int digitIndex, int digitValue)
        {
            var number = Candidate(regionSeed, digitIndex, digitValue);
            if (number == null)
                return;

            PhoneUtil.IsValidNumber(number);
            PhoneUtil.GetNumberType(number);
            PhoneUtil.GetRegionCodeForNumber(number);
            PhoneUtil.Format(number, PhoneNumberFormat.E164);
            PhoneUtil.Format(number, PhoneNumberFormat.INTERNATIONAL);
            PhoneUtil.Format(number, PhoneNumberFormat.NATIONAL);
            PhoneUtil.Format(number, PhoneNumberFormat.RFC3966);
            PhoneUtil.FormatOutOfCountryCallingNumber(number, "US");
            PhoneNumberOfflineGeocoder.GetInstance().GetDescriptionForNumber(number, Locale.English);
            PhoneNumberToCarrierMapper.GetInstance().GetNameForNumber(number, Locale.English);
            PhoneNumberToTimeZonesMapper.GetInstance().GetTimeZonesForNumber(number);
            ShortNumberInfo.GetInstance().IsPossibleShortNumber(number);
        }

        /// <summary>
        /// E.164 is the canonical wire form, so formatting to it and parsing back must land on the
        /// same number. Compares the formatted strings rather than the objects, since parsing also
        /// populates fields (raw input, country-code source) the original does not carry.
        /// </summary>
        [Property(MaxTest = 200)]
        public void ValidNumbersRoundTripThroughE164(int regionSeed, int digitIndex, int digitValue)
        {
            var number = Candidate(regionSeed, digitIndex, digitValue);
            if (number == null || !PhoneUtil.IsValidNumber(number))
                return;

            var e164 = PhoneUtil.Format(number, PhoneNumberFormat.E164);
            var reparsed = PhoneUtil.Parse(e164, null);

            Assert.Equal(e164, PhoneUtil.Format(reparsed, PhoneNumberFormat.E164));
        }

        /// <summary>
        /// Possibility is a length check and validity is the full pattern match, so validity is the
        /// strictly stronger claim: anything valid must also be possible.
        /// </summary>
        [Property(MaxTest = 200)]
        public void ValidNumbersAreAlsoPossible(int regionSeed, int digitIndex, int digitValue)
        {
            var number = Candidate(regionSeed, digitIndex, digitValue);
            if (number == null || !PhoneUtil.IsValidNumber(number))
                return;

            Assert.True(PhoneUtil.IsPossibleNumber(number),
                FormattableString.Invariant($"+{number.CountryCode}{number.NationalNumber} is valid but not possible"));
        }

        /// <summary>"Could these be the same number" cannot depend on the order of the arguments.</summary>
        [Property(MaxTest = 200)]
        public void IsNumberMatchIsSymmetric(int firstSeed, int secondSeed, int digitIndex, int digitValue)
        {
            var first = Candidate(firstSeed, digitIndex, digitValue);
            var second = Candidate(secondSeed, digitIndex + 1, digitValue + 1);
            if (first == null || second == null)
                return;

            Assert.Equal(PhoneUtil.IsNumberMatch(first, second), PhoneUtil.IsNumberMatch(second, first));
        }

        /// <summary>
        /// Builds a number by taking a region's example and rewriting one digit. Wholly random
        /// digits are almost never valid for any region, so mutating a known-good number is what
        /// keeps the validity-guarded properties from degenerating into no-ops.
        /// </summary>
        private static PhoneNumber? Candidate(int regionSeed, int digitIndex, int digitValue)
        {
            var example = PhoneUtil.GetExampleNumber(Regions[Mod(regionSeed, Regions.Length)]);
            if (example == null)
                return null;

            var digits = example.NationalNumber.ToString(CultureInfo.InvariantCulture).ToCharArray();
            digits[Mod(digitIndex, digits.Length)] = (char)('0' + Mod(digitValue, 10));

            // A leading zero is lost on the way back to ulong, which just yields a shorter number.
            if (!ulong.TryParse(new string(digits), NumberStyles.None, CultureInfo.InvariantCulture, out var mutated))
                return null;

            return new PhoneNumber.Builder()
                .SetCountryCode(example.CountryCode)
                .SetNationalNumber(mutated)
                .Build();
        }

        /// <summary>
        /// Non-negative remainder. Math.Abs is not usable here: the generators produce int.MinValue,
        /// which it cannot negate.
        /// </summary>
        private static int Mod(int value, int modulus)
        {
            if (modulus <= 0)
                return 0;

            var remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
