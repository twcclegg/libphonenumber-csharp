using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// The ported tests check behaviour on valid input, which is what the Java originals cover. These
    /// check that hostile input fails in a documented way rather than crashing. Two bugs of exactly
    /// that kind turned up by accident - an unbounded stackalloc reachable from the public Normalize
    /// entry points, and a KeyNotFoundException geocoding an Ascension Island number - and neither
    /// was visible to a suite that only feeds in well-formed numbers.
    ///
    /// Driven by [Fact] over an array rather than [Theory] with [MemberData]: xunit serialises theory
    /// arguments for discovery, and some of these strings (lone surrogates, NUL) do not survive that.
    ///
    /// Uses the production metadata rather than the test file, since that is what ships and is where
    /// the awkward regions live.
    /// </summary>
    public class TestPublicApiRobustness
    {
        private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

        /// <summary>
        /// Chosen to break assumptions rather than to be plausible: encoding edges, digits that are
        /// not ASCII, characters carrying a numeric value that is not a decimal digit, and shapes
        /// that resemble a phone number without being one.
        /// </summary>
        private static readonly string[] HostileInputs =
        {
            "",
            " ",
            "\t\n\r",
            "\0",
            "\u0001\u007f",                 // control characters
            new string('9', 10_000),
            new string('(', 2_000),
            "\ud800",                       // lone high surrogate
            "\udc00",                       // lone low surrogate
            "\ud83d\ude00",                 // astral pair
            "\u202e123456\u202c",           // right-to-left override
            "\u200b\u200c\u200d",           // zero-width characters
            "\u0660\u0661\u0662\u0663",     // Arabic-Indic digits
            "\u0966\u0967\u0968\u0969",     // Devanagari digits
            "\uff10\uff11\uff12\uff13",     // fullwidth digits
            "\u00bd\u00be\u2167",           // numeric value, but not decimal digits
            "\u0bf1",                       // Tamil one hundred: numeric value 100
            "+",
            "+++++",
            "\uff0b\uff0b",                 // fullwidth plus
            "-",
            "()",
            "...",
            "tel:",
            "tel:;phone-context=",
            "tel:+1;ext=",
            "tel:+1;phone-context=",
            "1-800-FLOWERS",
            "abc",
            "+1 (650) 253-0000 ext. " + new string('1', 500),
        };

        /// <summary>Region codes a caller might plausibly pass, valid or not.</summary>
        private static readonly string[] HostileRegions =
        {
            "",
            " ",
            "ZZ",
            "XX",
            "us",
            "001",
            "USA",
            new string('U', 500),
        };

        [Fact]
        public void ParseFailsOnlyWithNumberParseException()
        {
            AssertOverInputs("Parse", input => PhoneUtil.Parse(input, "US"), typeof(NumberParseException));
            AssertOverInputs("ParseAndKeepRawInput",
                input => PhoneUtil.ParseAndKeepRawInput(input, "US"), typeof(NumberParseException));
        }

        [Fact]
        public void ParseWithAnOddRegionFailsOnlyWithNumberParseException()
        {
            var failures = new List<string>();
            foreach (var region in HostileRegions)
            {
                Record("Parse(national)", region, failures,
                    () => PhoneUtil.Parse("6502530000", region), typeof(NumberParseException));
                // An international number carries its own country code, so the region is not consulted.
                Record("Parse(international)", region, failures,
                    () => PhoneUtil.Parse("+16502530000", region), typeof(NumberParseException));
            }

            Assert.Empty(failures);
        }

        [Fact]
        public void NormalizersNeverThrow()
        {
            AssertOverInputs("Normalize", input => PhoneNumberUtil.Normalize(input));
            AssertOverInputs("NormalizeDigitsOnly", input => PhoneNumberUtil.NormalizeDigitsOnly(input));
            AssertOverInputs("NormalizeDiallableCharsOnly",
                input => PhoneNumberUtil.NormalizeDiallableCharsOnly(input));
            AssertOverInputs("ConvertAlphaCharactersInNumber",
                input => PhoneNumberUtil.ConvertAlphaCharactersInNumber(input));
        }

        [Fact]
        public void ViabilityHelpersNeverThrow()
        {
            AssertOverInputs("IsViablePhoneNumber", input => PhoneNumberUtil.IsViablePhoneNumber(input));
            AssertOverInputs("ExtractPossibleNumber", input => PhoneNumberUtil.ExtractPossibleNumber(input));
        }

        [Fact]
        public void FindNumbersNeverThrows()
        {
            AssertOverInputs("FindNumbers", input =>
            {
                var count = 0;
                foreach (var _ in PhoneUtil.FindNumbers(input, "US"))
                    count++;
                return count;
            });
        }

        [Fact]
        public void AsYouTypeFormatterNeverThrows()
        {
            AssertOverInputs("AsYouTypeFormatter", input =>
            {
                var formatter = PhoneUtil.GetAsYouTypeFormatter("US");
                var last = "";
                // The formatter is per-keystroke and the long inputs are covered by the normalizers,
                // so a bounded prefix is enough to reach every branch.
                for (var i = 0; i < input.Length && i < 200; i++)
                    last = formatter.InputDigit(input[i]);
                return last;
            });
        }

        /// <summary>
        /// The stackalloc behind these used to be sized from the input, so a large enough string
        /// killed the process with an uncatchable StackOverflowException.
        /// </summary>
        [Fact]
        public void NormalizersHandleInputFarBeyondAnyPlausibleNumber()
        {
            var huge = new string('7', 200_000);

            Assert.Equal(huge, PhoneNumberUtil.Normalize(huge));
            Assert.Equal(huge, PhoneNumberUtil.NormalizeDigitsOnly(huge));
            Assert.Equal(huge, PhoneNumberUtil.NormalizeDiallableCharsOnly(huge));
            Assert.Equal(huge, PhoneNumberUtil.ConvertAlphaCharactersInNumber(huge));
        }

        /// <summary>
        /// Runs the read-only surface over an example number from every supported region. This is the
        /// shape that would have caught the geocoder throwing KeyNotFoundException for Ascension
        /// Island, whose region code has no entry in the locale table.
        /// </summary>
        [Fact]
        public void EveryRegionSurvivesTheReadOnlySurface()
        {
            var geocoder = PhoneNumberOfflineGeocoder.GetInstance();
            var carrierMapper = PhoneNumberToCarrierMapper.GetInstance();
            var timeZonesMapper = PhoneNumberToTimeZonesMapper.GetInstance();
            var shortInfo = ShortNumberInfo.GetInstance();
            var failures = new List<string>();

            foreach (var regionCode in PhoneUtil.GetSupportedRegions())
            {
                var example = PhoneUtil.GetExampleNumber(regionCode);
                if (example is null)
                    continue;

                try
                {
                    PhoneUtil.IsValidNumber(example);
                    PhoneUtil.GetNumberType(example);
                    PhoneUtil.Format(example, PhoneNumberFormat.INTERNATIONAL);
                    PhoneUtil.Format(example, PhoneNumberFormat.RFC3966);
                    PhoneUtil.FormatOutOfCountryCallingNumber(example, "US");
                    geocoder.GetDescriptionForNumber(example, Locale.English);
                    carrierMapper.GetNameForNumber(example, Locale.English);
                    timeZonesMapper.GetTimeZonesForNumber(example);
                    shortInfo.IsPossibleShortNumber(example);
                }
                catch (Exception e)
                {
                    failures.Add($"{regionCode}: {e.GetType().Name}: {e.Message}");
                }
            }

            Assert.Empty(failures);
        }

        private static void AssertOverInputs(string what, Func<string, object> call, Type? allowed = null)
        {
            var failures = new List<string>();
            foreach (var input in HostileInputs)
                Record(what, Describe(input), failures, () => call(input), allowed);

            Assert.Empty(failures);
        }

        private static void Record(string what, string input, ICollection<string> failures,
            Func<object> call, Type? allowed)
        {
            try
            {
                call();
            }
            catch (Exception e) when (allowed is null || e.GetType() != allowed)
            {
                failures.Add($"{what}({input}) threw {e.GetType().Name}: {e.Message}");
            }
            catch (Exception)
            {
                // The documented failure for this entry point.
            }
        }

        /// <summary>Renders an input for a failure message without pasting control characters into it.</summary>
        private static string Describe(string input)
        {
            if (input.Length > 40)
                return FormattableString.Invariant($"<{input.Length} chars starting {(int)input[0]:x4}>");

            var text = new System.Text.StringBuilder(input.Length + 2);
            foreach (var c in input)
            {
                if (c is >= ' ' and <= '~')
                    text.Append(c);
                else
                    text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }
    }
}
