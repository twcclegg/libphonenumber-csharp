using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// Keep both FsCheck imports: the OpenSSF Scorecard fuzzing check looks for them (see
// PhoneNumbers.Test/TestPhoneNumberProperties.cs).
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace PhoneNumbers.Test.Extensions
{
    /// <summary>
    /// Holds <see cref="PhoneNumberParser.IsViableCandidate"/>, the up-front check that lets
    /// <c>TryParse</c> skip a doomed <see cref="PhoneNumberUtil.Parse(string, string)"/>, to its one
    /// promise: it never rejects anything <c>Parse</c> accepts. A false rejection would make
    /// <c>TryParse</c> return false for a number <c>Parse</c> handles, so every property here is
    /// stated as "Parse succeeded implies the check passed".
    /// </summary>
    /// <remarks>
    /// Random strings almost never parse, so on their own they would test nothing. The generated
    /// cases start from every region's example numbers and dress them the ways <c>Parse</c>
    /// tolerates: each output format, RFC 3966 <c>tel:</c> URIs with a <c>phone-context</c>,
    /// extensions, leading junk that <c>Parse</c> strips, trailing text, fullwidth and non-ASCII
    /// digits and the fullwidth plus sign, against valid, invalid and missing default regions.
    /// <see cref="GeneratedCasesMostlyParse"/> keeps that generator honest.
    /// </remarks>
    public class TestPhoneNumberParserPrecheck
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        private static readonly string[] Regions =
            Util.GetSupportedRegions().OrderBy(r => r, StringComparer.Ordinal).ToArray();

        private static readonly string?[] CallerRegions =
            Regions.Concat(new string?[] { null, "", " ", "ZZ", "XX", "us", "gb", "001", "USA" }).ToArray();

        private static readonly PhoneNumber[] Examples = CreateExamples();

        private static readonly string[] LeadingJunk =
        {
            "", "", "", "Tel: ", "tel:", "abc ", "(", "[", " ", "Phone number: ", "#", "*", "x", "--",
            "⁠", "電話 ", "﻿", "call ", ".", "（",
        };

        private static readonly string[] TrailingJunk =
        {
            "", "", "", " ", ")", ".", " (home)", " call after 5", " ", "#", " x", ",", " / 2",
        };

        [Property(MaxTest = 3000)]
        public void PrecheckNeverRejectsAGeneratedNumberParseAccepts(
            int exampleSeed, int shapeSeed, int digitSeed, int junkSeed, int regionSeed)
        {
            var (input, region) = Generate(exampleSeed, shapeSeed, digitSeed, junkSeed, regionSeed);

            AssertPrecheckAdmitsWhateverParseAccepts(input, region);
        }

        [Property(MaxTest = 3000)]
        public void PrecheckNeverRejectsAnArbitraryStringParseAccepts(string? input, int regionSeed)
        {
            AssertPrecheckAdmitsWhateverParseAccepts(input, CallerRegions[Mod(regionSeed, CallerRegions.Length)]);
        }

        [Property(MaxTest = 1000)]
        public void PrecheckNeverRejectsAnArbitraryStringWithDigitsParseAccepts(string? prefix, string? suffix, int exampleSeed, int regionSeed)
        {
            // Arbitrary text around a real number in its national digits: the shape where leading
            // junk and missing plus signs interact with the region check.
            var example = Examples[Mod(exampleSeed, Examples.Length)];
            var input = prefix + Util.GetNationalSignificantNumber(example) + suffix;

            AssertPrecheckAdmitsWhateverParseAccepts(input, CallerRegions[Mod(regionSeed, CallerRegions.Length)]);
        }

        [Fact]
        public void GeneratedCasesMostlyParse()
        {
            // Without this the generated property could pass by generating nothing Parse accepts.
            var parsed = 0;
            const int Total = 2000;
            for (var i = 0; i < Total; i++)
            {
                var (input, region) = Generate(i * 7919, i * 104729, i * 31, i * 131, i * 17);
                if (TryParseWithoutPrecheck(input, region))
                {
                    parsed++;
                }
            }

            Assert.True(parsed > Total / 3, $"only {parsed} of {Total} generated inputs parsed");
        }

        [Theory]
        [InlineData("tel:+1-650-253-0000", null)]
        [InlineData("tel:253-0000;phone-context=+1-650", null)]
        [InlineData("tel:253-0000;phone-context=+1-650", "ZZ")]
        [InlineData("tel:2530000;phone-context=www.google.com", "US")]
        [InlineData("tel:+1-650-253-0000;isub=12345", null)]
        [InlineData("253-0000;phone-context=+1-650", null)]
        [InlineData("Tel: +44 20 7031 3000", null)]
        [InlineData("abc +44 20 7031 3000 def", null)]
        [InlineData("＋44 20 7031 3000", null)]
        [InlineData("+４４ ２０ ７０３１ ３０００", null)]
        [InlineData("٠١٢٣٤٥٦٧٨", "EG")]
        [InlineData("1-800-FLOWERS", "US")]
        [InlineData("+1 800 FLOWERS", null)]
        [InlineData("12", "US")]
        [InlineData("020 7031 3000 ext. 123", "GB")]
        [InlineData("++44 20 7031 3000", null)]
        public void PrecheckAdmitsShapesParseAccepts(string input, string? region)
        {
            Assert.True(TryParseWithoutPrecheck(input, region), "the case should parse; fix the test data");
            Assert.True(PhoneNumberParser.IsViableCandidate(input, region));
        }

        [Theory]
        [InlineData(null, "US")]
        [InlineData("", "US")]
        [InlineData("junk", "US")]
        [InlineData("1", "US")]
        [InlineData("+1", null)]
        [InlineData("a1b", "US")]
        [InlineData("6502530000", null)]
        [InlineData("6502530000", "")]
        [InlineData("6502530000", "ZZ")]
        [InlineData("6502530000", "us")]
        [InlineData("tel:6502530000;phone-context=example.com", null)]
        public void PrecheckRejectsInputParseRefuses(string? input, string? region)
        {
            Assert.False(TryParseWithoutPrecheck(input, region), "the case should not parse; fix the test data");
            Assert.False(PhoneNumberParser.IsViableCandidate(input, region));
        }

        [Fact]
        public void PrecheckRejectsOverlongInput()
        {
            var input = "+1 " + new string('5', 248);
            Assert.Equal(251, input.Length);

            Assert.False(TryParseWithoutPrecheck(input, null));
            Assert.False(PhoneNumberParser.IsViableCandidate(input, null));
            Assert.True(PhoneNumberParser.IsViableCandidate(input.Substring(1), "US"));
        }

        [Fact]
        public void TryParseFailsOnlyByReturningFalse()
        {
            // The curated hostile strings from PhoneNumbers.Test/TestPublicApiRobustness, through the
            // Extensions entry points. Not a [Theory]: lone surrogates and NUL do not survive xunit's
            // serialisation of theory arguments.
            string?[] hostile =
            {
                null, "", " ", "\0", "\uD800", "\uDC00", "+", "++", "+\uD800", "tel:", "tel:;phone-context=",
                ";phone-context=+", "tel:+1;phone-context=+1", "tel:1;isub=", new string('9', 250),
                new string('9', 251), "+" + new string('9', 249), "+1" + new string('x', 300),
                "١٢", "+١٢٣", "1-800-" + new string('A', 200),
            };

            foreach (var input in hostile)
            {
                foreach (var region in CallerRegions)
                {
                    PhoneNumberParser.TryParse(input, region, out _);
                    PhoneNumberParser.TryParseValid(input, region, out _);
                }
            }
        }

        private static void AssertPrecheckAdmitsWhateverParseAccepts(string? input, string? region)
        {
            if (TryParseWithoutPrecheck(input, region))
            {
                Assert.True(PhoneNumberParser.IsViableCandidate(input, region),
                    $"rejected {Describe(input)} with region {Describe(region)}, which Parse accepts");
            }
        }

        private static bool TryParseWithoutPrecheck(string? input, string? region)
        {
            try
            {
                Util.Parse(input, region);
                return true;
            }
            catch (NumberParseException)
            {
                return false;
            }
        }

        private static (string Input, string? Region) Generate(
            int exampleSeed, int shapeSeed, int digitSeed, int junkSeed, int regionSeed)
        {
            var example = Examples[Mod(exampleSeed, Examples.Length)];
            var nsn = Util.GetNationalSignificantNumber(example);
            var cc = "+" + example.CountryCode;
            var ownRegion = Util.GetRegionCodeForNumber(example);

            var shape = Mod(shapeSeed, 12);
            var text = shape switch
            {
                0 => Util.Format(example, PhoneNumberFormat.E164),
                1 => Util.Format(example, PhoneNumberFormat.INTERNATIONAL),
                2 => Util.Format(example, PhoneNumberFormat.NATIONAL),
                3 => Util.Format(example, PhoneNumberFormat.RFC3966),
                4 => "tel:" + nsn + ";phone-context=" + cc,
                5 => nsn + ";phone-context=" + cc,
                6 => "tel:" + nsn + ";phone-context=example.com",
                7 => Util.Format(example, PhoneNumberFormat.INTERNATIONAL) + " ext. 12",
                8 => Util.Format(example, PhoneNumberFormat.RFC3966) + ";isub=99",
                9 => "00" + example.CountryCode + nsn,
                10 => "(" + nsn[..Math.Min(3, nsn.Length)] + ") " + nsn[Math.Min(3, nsn.Length)..],
                _ => cc.Replace("+", "＋", StringComparison.Ordinal) + " " + nsn,
            };

            text = RewriteDigits(text, Mod(digitSeed, 4));

            // Leading junk only where Parse strips it: not in front of an RFC 3966 URI.
            var lead = text.StartsWith("tel:", StringComparison.Ordinal) || text.Contains(";phone-context", StringComparison.Ordinal)
                ? ""
                : LeadingJunk[Mod(junkSeed, LeadingJunk.Length)];
            var trail = TrailingJunk[Mod(junkSeed / 7, TrailingJunk.Length)];

            // A third each: the number's own region (so national formats parse), no region (so
            // only a plus sign can tell Parse the country), and anything a caller might pass.
            var region = Mod(regionSeed, 3) switch
            {
                0 => ownRegion,
                1 => null,
                _ => CallerRegions[Mod(regionSeed / 3, CallerRegions.Length)],
            };

            return (lead + text + trail, region);
        }

        private static string RewriteDigits(string text, int script)
        {
            // 0: ASCII as formatted; 1: fullwidth; 2: Arabic-Indic; 3: Devanagari.
            if (script == 0)
            {
                return text;
            }

            var zero = script switch { 1 => '０', 2 => '٠', _ => '०' };
            var builder = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                builder.Append(c is >= '0' and <= '9' ? (char)(zero + (c - '0')) : c);
            }

            return builder.ToString();
        }

        private static PhoneNumber[] CreateExamples()
        {
            var examples = new List<PhoneNumber>();
            foreach (var region in Regions)
            {
                foreach (var type in Enum.GetValues<PhoneNumberType>())
                {
                    var example = Util.GetExampleNumberForType(region, type);
                    if (example != null)
                    {
                        examples.Add(example);
                    }
                }
            }

            foreach (var callingCode in Util.GetSupportedGlobalNetworkCallingCodes().OrderBy(c => c))
            {
                var example = Util.GetExampleNumberForNonGeoEntity(callingCode);
                if (example != null)
                {
                    examples.Add(example);
                }
            }

            return examples.ToArray();
        }

        private static string Describe(string? value)
            => value is null ? "null" : "\"" + string.Concat(value.Select(c => c < 0x20 || c > 0x7E ? $"\\u{(int)c:X4}" : c.ToString())) + "\"";

        private static int Mod(int value, int modulus) => (int)((uint)value % (uint)modulus);
    }
}
