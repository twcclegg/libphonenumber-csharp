/*
 * Copyright (C) 2009 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PhoneNumbers.Test
{
    [TestFixture]
    class TestPhoneNumberMatcher: TestMetadataTestCase
    {
        /** See {@link PhoneNumberUtilTest#testParseNationalNumber()}. */
        [Test]
        public void TestFindNationalNumber()
        {
            // same cases as in testParseNationalNumber
            doTestFindInContext("033316005", "NZ");
            // ("33316005", RegionCode.NZ) is omitted since the national prefix is obligatory for these
            // types of numbers in New Zealand.
            // National prefix attached and some formatting present.
            doTestFindInContext("03-331 6005", "NZ");
            doTestFindInContext("03 331 6005", "NZ");
            // Testing international prefixes.
            // Should strip country code.
            doTestFindInContext("0064 3 331 6005", "NZ");
            // Try again, but this time we have an international number with Region Code US. It should
            // recognize the country code and parse accordingly.
            doTestFindInContext("01164 3 331 6005", "US");
            doTestFindInContext("+64 3 331 6005", "US");

            doTestFindInContext("64(0)64123456", "NZ");
            // Check that using a "/" is fine in a phone number.
            doTestFindInContext("123/45678", "DE");
            doTestFindInContext("123-456-7890", "US");
        }

        /** See {@link PhoneNumberUtilTest#testParseWithInternationalPrefixes()}. */
        [Test]
        public void TestFindWithInternationalPrefixes()
        {
            doTestFindInContext("+1 (650) 333-6000", "NZ");
            doTestFindInContext("1-650-333-6000", "US");
            // Calling the US number from Singapore by using different service providers
            // 1st test: calling using SingTel IDD service (IDD is 001)
            doTestFindInContext("0011-650-333-6000", "SG");
            // 2nd test: calling using StarHub IDD service (IDD is 008)
            doTestFindInContext("0081-650-333-6000", "SG");
            // 3rd test: calling using SingTel V019 service (IDD is 019)
            doTestFindInContext("0191-650-333-6000", "SG");
            // Calling the US number from Poland
            doTestFindInContext("0~01-650-333-6000", "PL");
            // Using "++" at the start.
            doTestFindInContext("++1 (650) 333-6000", "PL");
            // Using a full-width plus sign.
            doTestFindInContext("\uFF0B1 (650) 333-6000", "SG");
            // The whole number, including punctuation, is here represented in full-width form.
            doTestFindInContext("\uFF0B\uFF11\u3000\uFF08\uFF16\uFF15\uFF10\uFF09" +
                "\u3000\uFF13\uFF13\uFF13\uFF0D\uFF16\uFF10\uFF10\uFF10",
                "SG");
        }

        /** See {@link PhoneNumberUtilTest#testParseWithLeadingZero()}. */
        [Test]
        public void TestFindWithLeadingZero()
        {
            doTestFindInContext("+39 02-36618 300", "NZ");
            doTestFindInContext("02-36618 300", "IT");
            doTestFindInContext("312 345 678", "IT");
        }

        /** See {@link PhoneNumberUtilTest#testParseNationalNumberArgentina()}. */
        [Test]
        public void TestFindNationalNumberArgentina()
        {
            // Test parsing mobile numbers of Argentina.
            doTestFindInContext("+54 9 343 555 1212", "AR");
            doTestFindInContext("0343 15 555 1212", "AR");

            doTestFindInContext("+54 9 3715 65 4320", "AR");
            doTestFindInContext("03715 15 65 4320", "AR");

            // Test parsing fixed-line numbers of Argentina.
            doTestFindInContext("+54 11 3797 0000", "AR");
            doTestFindInContext("011 3797 0000", "AR");

            doTestFindInContext("+54 3715 65 4321", "AR");
            doTestFindInContext("03715 65 4321", "AR");

            doTestFindInContext("+54 23 1234 0000", "AR");
            doTestFindInContext("023 1234 0000", "AR");
        }

        /** See {@link PhoneNumberUtilTest#testParseWithXInNumber()}. */
        [Test]
        public void TestFindWithXInNumber()
        {
            doTestFindInContext("(0xx) 123456789", "AR");
            // A case where x denotes both carrier codes and extension symbol.
            doTestFindInContext("(0xx) 123456789 x 1234", "AR");

            // This test is intentionally constructed such that the number of digit after xx is larger than
            // 7, so that the number won't be mistakenly treated as an extension, as we allow extensions up
            // to 7 digits. This assumption is okay for now as all the countries where a carrier selection
            // code is written in the form of xx have a national significant number of length larger than 7.
            doTestFindInContext("011xx5481429712", "US");
        }

        /** See {@link PhoneNumberUtilTest#testParseNumbersMexico()}. */
        [Test]
        public void TestFindNumbersMexico()
        {
            // Test parsing fixed-line numbers of Mexico.
            doTestFindInContext("+52 (449)978-0001", "MX");
            doTestFindInContext("01 (449)978-0001", "MX");
            doTestFindInContext("(449)978-0001", "MX");

            // Test parsing mobile numbers of Mexico.
            doTestFindInContext("+52 1 33 1234-5678", "MX");
            doTestFindInContext("044 (33) 1234-5678", "MX");
            doTestFindInContext("045 33 1234-5678", "MX");
        }

        /** See {@link PhoneNumberUtilTest#testParseNumbersWithPlusWithNoRegion()}. */
        [Test]
        public void TestFindNumbersWithPlusWithNoRegion()
        {
            // "ZZ" is allowed only if the number starts with a '+' - then the country code can be
            // calculated.
            doTestFindInContext("+64 3 331 6005", "ZZ");
            // Null is also allowed for the region code in these cases.
            doTestFindInContext("+64 3 331 6005", null);
        }

        /** See {@link PhoneNumberUtilTest#testParseExtensions()}. */
        [Test]
        public void TestFindExtensions()
        {
            doTestFindInContext("03 331 6005 ext 3456", "NZ");
            doTestFindInContext("03-3316005x3456", "NZ");
            doTestFindInContext("03-3316005 int.3456", "NZ");
            doTestFindInContext("03 3316005 #3456", "NZ");
            doTestFindInContext("0~0 1800 7493 524", "PL");
            doTestFindInContext("(1800) 7493.524", "US");
            // Check that the last instance of an extension token is matched.
            doTestFindInContext("0~0 1800 7493 524 ~1234", "PL");
            // Verifying bug-fix where the last digit of a number was previously omitted if it was a 0 when
            // extracting the extension. Also verifying a few different cases of extensions.
            doTestFindInContext("+44 2034567890x456", "NZ");
            doTestFindInContext("+44 2034567890x456", "GB");
            doTestFindInContext("+44 2034567890 x456", "GB");
            doTestFindInContext("+44 2034567890 X456", "GB");
            doTestFindInContext("+44 2034567890 X 456", "GB");
            doTestFindInContext("+44 2034567890 X  456", "GB");
            doTestFindInContext("+44 2034567890  X 456", "GB");

            doTestFindInContext("(800) 901-3355 x 7246433", "US");
            doTestFindInContext("(800) 901-3355 , ext 7246433", "US");
            doTestFindInContext("(800) 901-3355 ,extension 7246433", "US");
            // The next test differs from PhoneNumberUtil -> when matching we don't consider a lone comma to
            // indicate an extension, although we accept it when parsing.
            doTestFindInContext("(800) 901-3355 ,x 7246433", "US");
            doTestFindInContext("(800) 901-3355 ext: 7246433", "US");
        }

        [Test]
        public void TestFindInterspersedWithSpace()
        {
            doTestFindInContext("0 3   3 3 1   6 0 0 5", "NZ");
        }

        /**
        * Test matching behavior when starting in the middle of a phone number.
        */
        [Test]
        public void TestIntermediateParsePositions()
        {
            String text = "Call 033316005  or 032316005!";
            //             |    |    |    |    |    |
            //             0    5   10   15   20   25

            // Iterate over all possible indices.
            for (int i = 0; i <= 5; i++)
                AssertEqualRange(text, i, 5, 14);
            // 7 and 8 digits in a row are still parsed as number.
            AssertEqualRange(text, 6, 6, 14);
            AssertEqualRange(text, 7, 7, 14);
            // Anything smaller is skipped to the second instance.
            for (int i = 8; i <= 19; i++)
                AssertEqualRange(text, i, 19, 28);
        }

        [Test]
        public void TestMatchWithSurroundingZipcodes()
        {
            String number = "415-666-7777";
            String zipPreceding = "My address is CA 34215 - " + number + " is my number.";
            PhoneNumber expectedResult = phoneUtil.Parse(number, "US");

            var iterator = phoneUtil.FindNumbers(zipPreceding, "US").GetEnumerator();
            PhoneNumberMatch match = iterator.MoveNext() ? iterator.Current : null;
            Assert.IsNotNull(match, "Did not find a number in '" + zipPreceding + "'; expected " + number);
            Assert.AreEqual(expectedResult, match.Number);
            Assert.AreEqual(number, match.RawString);

            // Now repeat, but this time the phone number has spaces in it. It should still be found.
            number = "(415) 666 7777";

            String zipFollowing = "My number is " + number + ". 34215 is my zip-code.";
            iterator = phoneUtil.FindNumbers(zipFollowing, "US").GetEnumerator();

            PhoneNumberMatch matchWithSpaces = iterator.MoveNext() ? iterator.Current : null;
            Assert.IsNotNull(matchWithSpaces, "Did not find a number in '" + zipFollowing + "'; expected " + number);
            Assert.AreEqual(expectedResult, matchWithSpaces.Number);
            Assert.AreEqual(number, matchWithSpaces.RawString);
        }

        [Test]
        public void TestIsLatinLetter()
        {
            Assert.That(PhoneNumberMatcher.IsLatinLetter('c'));
            Assert.That(PhoneNumberMatcher.IsLatinLetter('C'));
            Assert.That(PhoneNumberMatcher.IsLatinLetter('\u00C9'));
            Assert.That(PhoneNumberMatcher.IsLatinLetter('\u0301'));  // Combining acute accent
            // Punctuation, digits and white-space are not considered "latin letters".
            Assert.False(PhoneNumberMatcher.IsLatinLetter(':'));
            Assert.False(PhoneNumberMatcher.IsLatinLetter('5'));
            Assert.False(PhoneNumberMatcher.IsLatinLetter('-'));
            Assert.False(PhoneNumberMatcher.IsLatinLetter('.'));
            Assert.False(PhoneNumberMatcher.IsLatinLetter(' '));
            Assert.False(PhoneNumberMatcher.IsLatinLetter('\u6211'));  // Chinese character
            Assert.False(PhoneNumberMatcher.IsLatinLetter('\u306E'));  // Hiragana letter no
        }

        [Test]
        public void TestMatchesWithSurroundingLatinChars()
        {
            List<NumberContext> possibleOnlyContexts = new List<NumberContext>(5);
            possibleOnlyContexts.Add(new NumberContext("abc", "def"));
            possibleOnlyContexts.Add(new NumberContext("abc", ""));
            possibleOnlyContexts.Add(new NumberContext("", "def"));
            // Latin capital letter e with an acute accent.
            possibleOnlyContexts.Add(new NumberContext("\u00C9", ""));
            // e with an acute accent decomposed (with combining mark)
            possibleOnlyContexts.Add(new NumberContext("e\u0301", ""));

            // Numbers should not be considered valid, if they are surrounded by Latin characters, but
            // should be considered possible.
            FindMatchesInContexts(possibleOnlyContexts, false, true);
        }

        [Test]
        public void TestMoneyNotSeenAsPhoneNumber()
        {
            List<NumberContext> possibleOnlyContexts = new List<NumberContext>();
            possibleOnlyContexts.Add(new NumberContext("$", ""));
            possibleOnlyContexts.Add(new NumberContext("", "$"));
            possibleOnlyContexts.Add(new NumberContext("\u00A3", ""));  // Pound sign
            possibleOnlyContexts.Add(new NumberContext("\u00A5", ""));  // Yen sign
            FindMatchesInContexts(possibleOnlyContexts, false, true);
        }

        [Test]
        public void TestPercentageNotSeenAsPhoneNumber()
        {
            List<NumberContext> possibleOnlyContexts = new List<NumberContext>();
            possibleOnlyContexts.Add(new NumberContext("", "%"));
            // Numbers followed by % should be dropped.
            FindMatchesInContexts(possibleOnlyContexts, false, true);
        }


        [Test]
        public void TestPhoneNumberWithLeadingOrTrailingMoneyMatches()
        {
            // Because of the space after the 20 (or before the 100) these dollar amounts should not stop
            // the actual number from being found.
            List<NumberContext> contexts = new List<NumberContext>();
            contexts.Add(new NumberContext("$20 ", ""));
            contexts.Add(new NumberContext("", " 100$"));
            FindMatchesInContexts(contexts, true, true);
        }

        [Test]
        public void TestMatchesWithSurroundingLatinCharsAndLeadingPunctuation()
        {
            // Contexts with trailing characters. Leading characters are okay here since the numbers we will
            // insert start with punctuation, but trailing characters are still not allowed.
            List<NumberContext> possibleOnlyContexts = new List<NumberContext>();
            possibleOnlyContexts.Add(new NumberContext("abc", "def"));
            possibleOnlyContexts.Add(new NumberContext("", "def"));
            possibleOnlyContexts.Add(new NumberContext("", "\u00C9"));

            // Numbers should not be considered valid, if they have trailing Latin characters, but should be
            // considered possible.
            String numberWithPlus = "+14156667777";
            String numberWithBrackets = "(415)6667777";
            FindMatchesInContexts(possibleOnlyContexts, false, true, "US", numberWithPlus);
            FindMatchesInContexts(possibleOnlyContexts, false, true, "US", numberWithBrackets);

            List<NumberContext> validContexts = new List<NumberContext>();
            validContexts.Add(new NumberContext("abc", ""));
            validContexts.Add(new NumberContext("\u00C9", ""));
            validContexts.Add(new NumberContext("\u00C9", "."));  // Trailing punctuation.
            validContexts.Add(new NumberContext("\u00C9", " def"));  // Trailing white-space.

            // Numbers should be considered valid, since they start with punctuation.
            FindMatchesInContexts(validContexts, true, true, "US", numberWithPlus);
            FindMatchesInContexts(validContexts, true, true, "US", numberWithBrackets);
        }

        [Test]
        public void TestMatchesWithSurroundingChineseChars()
        {
            List<NumberContext> validContexts = new List<NumberContext>();
            validContexts.Add(new NumberContext("\u6211\u7684\u7535\u8BDD\u53F7\u7801\u662F", ""));
            validContexts.Add(new NumberContext("", "\u662F\u6211\u7684\u7535\u8BDD\u53F7\u7801"));
            validContexts.Add(new NumberContext("\u8BF7\u62E8\u6253", "\u6211\u5728\u660E\u5929"));

            // Numbers should be considered valid, since they are surrounded by Chinese.
            FindMatchesInContexts(validContexts, true, true);
        }

        [Test]
        public void testMatchesWithSurroundingPunctuation()
        {
            List<NumberContext> validContexts = new List<NumberContext>();
            validContexts.Add(new NumberContext("My number-", ""));  // At end of text.
            validContexts.Add(new NumberContext("", ".Nice day."));  // At start of text.
            validContexts.Add(new NumberContext("Tel:", "."));  // Punctuation surrounds number.
            validContexts.Add(new NumberContext("Tel: ", " on Saturdays."));  // White-space is also fine.

            // Numbers should be considered valid, since they are surrounded by punctuation.
            FindMatchesInContexts(validContexts, true, true);
        }

        [Test]
        public void TestMatchesMultiplePhoneNumbersSeparatedByPhoneNumberPunctuation()
        {
            String text = "Call 650-253-4561 -- 455-234-3451";
            String region = "US";

            PhoneNumber number1 = new PhoneNumber.Builder()
                .SetCountryCode(phoneUtil.GetCountryCodeForRegion(region))
                .SetNationalNumber(6502534561L)
                .Build();
            PhoneNumberMatch match1 = new PhoneNumberMatch(5, "650-253-4561", number1);

            PhoneNumber number2 = new PhoneNumber.Builder()
                .SetCountryCode(phoneUtil.GetCountryCodeForRegion(region))
                .SetNationalNumber(4552343451L)
                .Build();
            PhoneNumberMatch match2 = new PhoneNumberMatch(21, "455-234-3451", number2);

            var matches = phoneUtil.FindNumbers(text, region).GetEnumerator();
            matches.MoveNext();
            Assert.AreEqual(match1, matches.Current);
            matches.MoveNext();
            Assert.AreEqual(match2, matches.Current);
        }

        [Test]
        public void testDoesNotMatchMultiplePhoneNumbersSeparatedWithNoWhiteSpace()
        {
            // No white-space found between numbers - neither is found.
            String text = "Call 650-253-4561--455-234-3451";
            String region = "US";

            Assert.True(hasNoMatches(phoneUtil.FindNumbers(text, region)));
        }

        /**
         * Strings with number-like things that shouldn't be found under any level.
         */
        private static readonly NumberTest[] IMPOSSIBLE_CASES = {
    new NumberTest("12345", "US"),
    new NumberTest("23456789", "US"),
    new NumberTest("234567890112", "US"),
    new NumberTest("650+253+1234", "US"),
    new NumberTest("3/10/1984", "CA"),
    new NumberTest("03/27/2011", "US"),
    new NumberTest("31/8/2011", "US"),
    new NumberTest("1/12/2011", "US"),
    new NumberTest("10/12/82", "DE"),
    new NumberTest("650x2531234", RegionCode.US),
    new NumberTest("2012-01-02 08:00", RegionCode.US),
    new NumberTest("2012/01/02 08:00", RegionCode.US),
    new NumberTest("20120102 08:00", RegionCode.US),
  };

        /**
         * Strings with number-like things that should only be found under "possible".
         */
        private static readonly NumberTest[] POSSIBLE_ONLY_CASES = {
    // US numbers cannot start with 7 in the test metadata to be valid.
    new NumberTest("7121115678", "US"),
    // 'X' should not be found in numbers at leniencies stricter than POSSIBLE, unless it represents
    // a carrier code or extension.
    new NumberTest("1650 x 253 - 1234", "US"),
    new NumberTest("650 x 253 - 1234", "US"),
    new NumberTest("6502531x234", "US"),
    new NumberTest("(20) 3346 1234", RegionCode.GB),  // Non-optional NP omitted
  };

        /**
         * Strings with number-like things that should only be found up to and including the "valid"
         * leniency level.
         */
        private static readonly NumberTest[] VALID_CASES = {
    new NumberTest("65 02 53 00 00", "US"),
    new NumberTest("6502 538365", "US"),
    new NumberTest("650//253-1234", "US"),  // 2 slashes are illegal at higher levels
    new NumberTest("650/253/1234", "US"),
    new NumberTest("9002309. 158", "US"),
    new NumberTest("12 7/8 - 14 12/34 - 5", "US"),
    new NumberTest("12.1 - 23.71 - 23.45", "US"),
    new NumberTest("800 234 1 111x1111", "US"),
    new NumberTest("1979-2011 100", RegionCode.US),
    new NumberTest("+494949-4-94", "DE"),  // National number in wrong format
    new NumberTest("\uFF14\uFF11\uFF15\uFF16\uFF16\uFF16\uFF16-\uFF17\uFF17\uFF17", RegionCode.US),
    new NumberTest("2012-0102 08", RegionCode.US),  // Very strange formatting.
    new NumberTest("2012-01-02 08", RegionCode.US),
     // Breakdown assistance number with unexpected formatting.
    new NumberTest("1800-1-0-10 22", RegionCode.AU),
    new NumberTest("030-3-2 23 12 34", RegionCode.DE),
    new NumberTest("03 0 -3 2 23 12 34", RegionCode.DE),
    new NumberTest("(0)3 0 -3 2 23 12 34", RegionCode.DE),
    new NumberTest("0 3 0 -3 2 23 12 34", RegionCode.DE),
  };

        /**
         * Strings with number-like things that should only be found up to and including the
         * "strict_grouping" leniency level.
         */
        private static readonly NumberTest[] STRICT_GROUPING_CASES = {
    new NumberTest("(415) 6667777", "US"),
    new NumberTest("415-6667777", "US"),
    // Should be found by strict grouping but not exact grouping, as the last two groups are
    // formatted together as a block.
    new NumberTest("0800-2491234", "DE"),
    // Doesn't match any formatting in the test file, but almost matches an alternate format (the
    // last two groups have been squashed together here).
    new NumberTest("0900-1 123123", RegionCode.DE),
    new NumberTest("(0)900-1 123123", RegionCode.DE),
    new NumberTest("0 900-1 123123", RegionCode.DE),
  };

        /**
         * Strings with number-like things that should be found at all levels.
         */
        private static readonly NumberTest[] EXACT_GROUPING_CASES = {
            new NumberTest("\uFF14\uFF11\uFF15\uFF16\uFF16\uFF16\uFF17\uFF17\uFF17\uFF17", "US"),
            new NumberTest("\uFF14\uFF11\uFF15-\uFF16\uFF16\uFF16-\uFF17\uFF17\uFF17\uFF17", "US"),
            new NumberTest("4156667777", "US"),
            new NumberTest("4156667777 x 123", "US"),
            new NumberTest("415-666-7777", "US"),
            new NumberTest("415/666-7777", "US"),
            new NumberTest("415-666-7777 ext. 503", "US"),
            new NumberTest("1 415 666 7777 x 123", "US"),
            new NumberTest("+1 415-666-7777", "US"),
            new NumberTest("+494949 49", "DE"),
            new NumberTest("+49-49-34", "DE"),
            new NumberTest("+49-4931-49", "DE"),
            new NumberTest("04931-49", "DE"),  // With National Prefix
            new NumberTest("+49-494949", "DE"),  // One group with country code
            new NumberTest("+49-494949 ext. 49", "DE"),
            new NumberTest("+49494949 ext. 49", "DE"),
            new NumberTest("0494949", "DE"),
            new NumberTest("0494949 ext. 49", "DE"),
            new NumberTest("01 (33) 3461 2234", RegionCode.MX),  // Optional NP present
            new NumberTest("(33) 3461 2234", RegionCode.MX),  // Optional NP omitted
            new NumberTest("1800-10-10 22", RegionCode.AU),  // Breakdown assistance number.
            // Doesn't match any formatting in the test file, but matches an alternate format exactly.
            new NumberTest("0900-1 123 123", RegionCode.DE),
            new NumberTest("(0)900-1 123 123", RegionCode.DE),
            new NumberTest("0 900-1 123 123", RegionCode.DE),
        };

        [Test]
        public void TestMatchesWithPossibleLeniency()
        {
            List<NumberTest> testCases = new List<NumberTest>();
            testCases.AddRange(STRICT_GROUPING_CASES);
            testCases.AddRange(EXACT_GROUPING_CASES);
            testCases.AddRange(VALID_CASES);
            testCases.AddRange(POSSIBLE_ONLY_CASES);
            doTestNumberMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.POSSIBLE);
        }

        [Test]
        public void TestNonMatchesWithPossibleLeniency()
        {
            List<NumberTest> testCases = new List<NumberTest>();
            testCases.AddRange(IMPOSSIBLE_CASES);
            doTestNumberNonMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.POSSIBLE);
        }

        [Test]
        public void TestMatchesWithValidLeniency()
        {
            List<NumberTest> testCases = new List<NumberTest>();
            testCases.AddRange(STRICT_GROUPING_CASES);
            testCases.AddRange(EXACT_GROUPING_CASES);
            testCases.AddRange(VALID_CASES);
            doTestNumberMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.VALID);
        }

        [Test]
        public void TestNonMatchesWithValidLeniency()
        {
            List<NumberTest> testCases = new List<NumberTest>();
            testCases.AddRange(IMPOSSIBLE_CASES);
            testCases.AddRange(POSSIBLE_ONLY_CASES);
            doTestNumberNonMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.VALID);
        }

        [Test]
        public void TestMatchesWithStrictGroupingLeniency()
        {
            List<NumberTest> testCases = new List<NumberTest>();
            testCases.AddRange(STRICT_GROUPING_CASES);
            testCases.AddRange(EXACT_GROUPING_CASES);
            doTestNumberMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.STRICT_GROUPING);
        }

        [Test]
        public void TestNonMatchesWithStrictGroupLeniency()
        {
            List<NumberTest> testCases = new List<NumberTest>();
            testCases.AddRange(IMPOSSIBLE_CASES);
            testCases.AddRange(POSSIBLE_ONLY_CASES);
            testCases.AddRange(VALID_CASES);
            doTestNumberNonMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.STRICT_GROUPING);
        }

        [Test]
        public void TestMatchesWithExactGroupingLeniency()
        {
            List<NumberTest> testCases = new List<NumberTest>();
            testCases.AddRange(EXACT_GROUPING_CASES);
            doTestNumberMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.EXACT_GROUPING);
        }

        [Test]
        public void TestNonMatchesExactGroupLeniency()
        {
            List<NumberTest> testCases = new List<NumberTest>();
            testCases.AddRange(IMPOSSIBLE_CASES);
            testCases.AddRange(POSSIBLE_ONLY_CASES);
            testCases.AddRange(VALID_CASES);
            testCases.AddRange(STRICT_GROUPING_CASES);
            doTestNumberNonMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.EXACT_GROUPING);
        }

        private void doTestNumberMatchesForLeniency(List<NumberTest> testCases,
                                                    PhoneNumberUtil.Leniency leniency)
        {
            int noMatchFoundCount = 0;
            int wrongMatchFoundCount = 0;
            foreach (NumberTest test in testCases)
            {
                var iterator =
                    findNumbersForLeniency(test.rawString, test.region, leniency);
                PhoneNumberMatch match = iterator.FirstOrDefault();
                if (match == null)
                {
                    noMatchFoundCount++;
                    Console.WriteLine("No match found in " + test.ToString() + " for leniency: " + leniency);
                }
                else
                {
                    if (!test.rawString.Equals(match.RawString))
                    {
                        wrongMatchFoundCount++;
                        Console.WriteLine("Found wrong match in test " + test.ToString() +
                                          ". Found " + match.RawString);
                    }
                }
            }
            Assert.AreEqual(0, noMatchFoundCount);
            Assert.AreEqual(0, wrongMatchFoundCount);
        }

        private void doTestNumberNonMatchesForLeniency(List<NumberTest> testCases,
                                                       PhoneNumberUtil.Leniency leniency)
        {
            int matchFoundCount = 0;
            foreach (NumberTest test in testCases)
            {
                var iterator =
                    findNumbersForLeniency(test.rawString, test.region, leniency);
                PhoneNumberMatch match = iterator.FirstOrDefault();
                if (match != null)
                {
                    matchFoundCount++;
                    Console.WriteLine("Match found in " + test.ToString() + " for leniency: " + leniency);
                }
            }
            Assert.AreEqual(0, matchFoundCount);
        }

        /**
         * Helper method which tests the contexts provided and ensures that:
         * -- if isValid is true, they all find a test number inserted in the middle when leniency of
         *  matching is set to VALID; else no test number should be extracted at that leniency level
         * -- if isPossible is true, they all find a test number inserted in the middle when leniency of
         *  matching is set to POSSIBLE; else no test number should be extracted at that leniency level
         */
        private void FindMatchesInContexts(List<NumberContext> contexts, bool isValid,
                                           bool isPossible, String region, String number)
        {
            if (isValid)
            {
                doTestInContext(number, region, contexts, PhoneNumberUtil.Leniency.VALID);
            }
            else
            {
                foreach (NumberContext context in contexts)
                {
                    String text = context.leadingText + number + context.trailingText;
                    Assert.That(hasNoMatches(phoneUtil.FindNumbers(text, region)),
                        "Should not have found a number in " + text);
                }
            }
            if (isPossible)
            {
                doTestInContext(number, region, contexts, PhoneNumberUtil.Leniency.POSSIBLE);
            }
            else
            {
                foreach (NumberContext context in contexts)
                {
                    String text = context.leadingText + number + context.trailingText;
                    Assert.That(hasNoMatches(phoneUtil.FindNumbers(text, region, PhoneNumberUtil.Leniency.POSSIBLE,
                                                                  long.MaxValue)),
                                                                  "Should not have found a number in " + text);
                }
            }
        }

        /**
         * Variant of FindMatchesInContexts that uses a default number and region.
         */
        private void FindMatchesInContexts(List<NumberContext> contexts, bool isValid, bool isPossible)
        {
            String region = "US";
            String number = "415-666-7777";

            FindMatchesInContexts(contexts, isValid, isPossible, region, number);
        }


        [Test]
        public void TestNonMatchingBracketsAreInvalid()
        {
            // The digits up to the ", " form a valid US number, but it shouldn't be matched as one since
            // there was a non-matching bracket present.
            Assert.That(hasNoMatches(phoneUtil.FindNumbers(
            "80.585 [79.964, 81.191]", "US")));

            // The trailing "]" is thrown away before parsing, so the resultant number, while a valid US
            // number, does not have matching brackets.
            Assert.That(hasNoMatches(phoneUtil.FindNumbers(
            "80.585 [79.964]", "US")));

            Assert.That(hasNoMatches(phoneUtil.FindNumbers(
            "80.585 ((79.964)", "US")));

            // This case has too many sets of brackets to be valid.
            Assert.That(hasNoMatches(phoneUtil.FindNumbers(
            "(80).(585) (79).(9)64", "US")));
        }

        [Test]
        public void TestNoMatchIfRegionIsNull()
        {
            // Fail on non-international prefix if region code is null.
            Assert.That(hasNoMatches(phoneUtil.FindNumbers(
                "Random text body - number is 0331 6005, see you there", null)));
        }

        [Test]
        public void TestNoMatchInEmptyString()
        {
            Assert.That(hasNoMatches(phoneUtil.FindNumbers("", "US")));
            Assert.That(hasNoMatches(phoneUtil.FindNumbers("  ", "US")));
        }

        [Test]
        public void TestNoMatchIfNoNumber()
        {
            Assert.That(hasNoMatches(phoneUtil.FindNumbers(
                "Random text body - number is foobar, see you there", "US")));
        }

        [Test]
        public void TestSequences()
        {
            // Test multiple occurrences.
            String text = "Call 033316005  or 032316005!";
            String region = "NZ";

            PhoneNumber number1 = new PhoneNumber.Builder()
                .SetCountryCode(phoneUtil.GetCountryCodeForRegion(region))
                .SetNationalNumber(33316005).Build();
            PhoneNumberMatch match1 = new PhoneNumberMatch(5, "033316005", number1);

            PhoneNumber number2 = new PhoneNumber.Builder()
                .SetCountryCode(phoneUtil.GetCountryCodeForRegion(region))
                .SetNationalNumber(32316005).Build();
            PhoneNumberMatch match2 = new PhoneNumberMatch(19, "032316005", number2);

            var matches = phoneUtil.FindNumbers(text, region, PhoneNumberUtil.Leniency.POSSIBLE, long.MaxValue).GetEnumerator();
            matches.MoveNext();
            Assert.AreEqual(match1, matches.Current);
            matches.MoveNext();
            Assert.AreEqual(match2, matches.Current);
        }

        [Test]
        public void TestNullInput()
        {
            Assert.That(hasNoMatches(phoneUtil.FindNumbers(null, "US")));
            Assert.That(hasNoMatches(phoneUtil.FindNumbers(null, null)));
        }

        [Test]
        public void TestMaxMatches()
        {
            // Set up text with 100 valid phone numbers.
            StringBuilder numbers = new StringBuilder();
            for (int i = 0; i < 100; i++)
                numbers.Append("My info: 415-666-7777,");

            // Matches all 100. Max only applies to failed cases.
            List<PhoneNumber> expected = new List<PhoneNumber>(100);
            PhoneNumber number = phoneUtil.Parse("+14156667777", null);
            for (int i = 0; i < 100; i++)
                expected.Add(number);

            var iterable = phoneUtil.FindNumbers(numbers.ToString(), "US", PhoneNumberUtil.Leniency.VALID, 10);
            List<PhoneNumber> actual = new List<PhoneNumber>(100);
            foreach (var match in iterable)
                actual.Add(match.Number);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestMaxMatchesInvalid()
        {
            // Set up text with 10 invalid phone numbers followed by 100 valid.
            StringBuilder numbers = new StringBuilder();
            for (int i = 0; i < 10; i++)
                numbers.Append("My address 949-8945-0");
            for (int i = 0; i < 100; i++)
                numbers.Append("My info: 415-666-7777,");

            var iterable = phoneUtil.FindNumbers(numbers.ToString(), "US", PhoneNumberUtil.Leniency.VALID, 10);
            Assert.IsFalse(iterable.GetEnumerator().MoveNext());
        }

        [Test]
        public void TestMaxMatchesMixed()
        {
            // Set up text with 100 valid numbers inside an invalid number.
            StringBuilder numbers = new StringBuilder();
            for (int i = 0; i < 100; i++)
                numbers.Append("My info: 415-666-7777 123 fake street");

            // Only matches the first 10 despite there being 100 numbers due to max matches.
            List<PhoneNumber> expected = new List<PhoneNumber>(100);
            PhoneNumber number = phoneUtil.Parse("+14156667777", null);
            for (int i = 0; i < 10; i++)
                expected.Add(number);

            var iterable = phoneUtil.FindNumbers(numbers.ToString(), "US", PhoneNumberUtil.Leniency.VALID, 10);
            List<PhoneNumber> actual = new List<PhoneNumber>(100);
            foreach (var match in iterable)
                actual.Add(match.Number);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNonPlusPrefixedNumbersNotFoundForInvalidRegion()
        {
            // Does not start with a "+", we won't match it.
            var iterable = phoneUtil.FindNumbers("1 456 764 156", RegionCode.ZZ);
            var iterator = iterable.GetEnumerator();
            
            Assert.IsFalse(iterator.MoveNext());
            Assert.IsFalse(iterator.MoveNext());
        }


        [Test]
        public void TestEmptyIteration()
        {
            var iterable = phoneUtil.FindNumbers("", "ZZ");
            var iterator = iterable.GetEnumerator();

            Assert.IsFalse(iterator.MoveNext());
            Assert.IsFalse(iterator.MoveNext());
        }

        [Test]
        public void TestSingleIteration()
        {
            var iterable = phoneUtil.FindNumbers("+14156667777", "ZZ");

            // With hasNext() -> next().
            var iterator = iterable.GetEnumerator();
            // Double hasNext() to ensure it does not advance.
            Assert.That(iterator.MoveNext());
            Assert.IsNotNull(iterator.Current);
            Assert.IsFalse(iterator.MoveNext());

            // With next() only.
            iterator = iterable.GetEnumerator();
            Assert.That(iterator.MoveNext());
            Assert.IsFalse(iterator.MoveNext());
        }

        /**
        * Asserts that another number can be found in {@code text} starting at {@code index}, and that
        * its corresponding range is {@code [start, end)}.
        */
        private void AssertEqualRange(String text, int index, int start, int end)
        {
            String sub = text.Substring(index);
            var matches =
                phoneUtil.FindNumbers(sub, "NZ", PhoneNumberUtil.Leniency.POSSIBLE, long.MaxValue).GetEnumerator();
            Assert.That(matches.MoveNext());
            PhoneNumberMatch match = matches.Current;
            Assert.AreEqual(start - index, match.Start);
            Assert.AreEqual(end - start, match.Length);
            Assert.AreEqual(sub.Substring(match.Start, match.Length), match.RawString);
        }

        /**
        * Tests numbers found by {@link PhoneNumberUtil#FindNumbers(CharSequence, String)} in various
        * textual contexts.
        *
        * @param number the number to test and the corresponding region code to use
        */
        private void doTestFindInContext(String number, String defaultCountry)
        {
            findPossibleInContext(number, defaultCountry);

            PhoneNumber parsed = phoneUtil.Parse(number, defaultCountry);
            if (phoneUtil.IsValidNumber(parsed))
                findValidInContext(number, defaultCountry);
        }

        private void findPossibleInContext(String number, String defaultCountry)
        {
            List<NumberContext> contextPairs = new List<NumberContext>();
            contextPairs.Add(new NumberContext("", ""));  // no context
            contextPairs.Add(new NumberContext("   ", "\t"));  // whitespace only
            contextPairs.Add(new NumberContext("Hello ", ""));  // no context at end
            contextPairs.Add(new NumberContext("", " to call me!"));  // no context at start
            contextPairs.Add(new NumberContext("Hi there, call ", " to reach me!"));  // no context at start
            contextPairs.Add(new NumberContext("Hi there, call ", ", or don't"));  // with commas
            // Three examples without whitespace around the number.
            contextPairs.Add(new NumberContext("Hi call", ""));
            contextPairs.Add(new NumberContext("", "forme"));
            contextPairs.Add(new NumberContext("Hi call", "forme"));
            // With other small numbers.
            contextPairs.Add(new NumberContext("It's cheap! Call ", " before 6:30"));
            // With a second number later.
            contextPairs.Add(new NumberContext("Call ", " or +1800-123-4567!"));
            contextPairs.Add(new NumberContext("Call me on June 2 at", ""));  // with a Month-Day date
            // With publication pages.
            contextPairs.Add(new NumberContext(
            "As quoted by Alfonso 12-15 (2009), you may call me at ", ""));
            contextPairs.Add(new NumberContext(
            "As quoted by Alfonso et al. 12-15 (2009), you may call me at ", ""));
            // With dates, written in the American style.
            contextPairs.Add(new NumberContext(
            "As I said on 03/10/2011, you may call me at ", ""));
            // With trailing numbers after a comma. The 45 should not be considered an extension.
            contextPairs.Add(new NumberContext("", ", 45 days a year"));
            // With a postfix stripped off as it looks like the start of another number.
            contextPairs.Add(new NumberContext("Call ", "/x12 more"));

            doTestInContext(number, defaultCountry, contextPairs, PhoneNumberUtil.Leniency.POSSIBLE);
        }

        /**
        * Tests valid numbers in contexts that fail for {@link Leniency#POSSIBLE}.
        */
        private void findValidInContext(String number, String defaultCountry)
        {
            List<NumberContext> contextPairs = new List<NumberContext>();
            // With other small numbers.
            contextPairs.Add(new NumberContext("It's only 9.99! Call ", " to buy"));
            // With a number Day.Month.Year date.
            contextPairs.Add(new NumberContext("Call me on 21.6.1984 at ", ""));
            // With a number Month/Day date.
            contextPairs.Add(new NumberContext("Call me on 06/21 at ", ""));
            // With a number Day.Month date.
            contextPairs.Add(new NumberContext("Call me on 21.6. at ", ""));
            // With a number Month/Day/Year date.
            contextPairs.Add(new NumberContext("Call me on 06/21/84 at ", ""));
            doTestInContext(number, defaultCountry, contextPairs, PhoneNumberUtil.Leniency.VALID);
        }

        private void doTestInContext(String number, String defaultCountry,
            List<NumberContext> contextPairs, PhoneNumberUtil.Leniency leniency)
        {
            foreach (var context in contextPairs)
            {
                String prefix = context.leadingText;
                String text = prefix + number + context.trailingText;

                int start = prefix.Length;
                int length = number.Length;
                var iterator = phoneUtil.FindNumbers(text, defaultCountry, leniency, long.MaxValue);

                PhoneNumberMatch match = iterator.First();
                Assert.IsNotNull(match, "Did not find a number in '" + text + "'; expected '" + number + "'");

                String extracted = text.Substring(match.Start, match.Length);
                Assert.That(start == match.Start && length == match.Length,
                    "Unexpected phone region in '" + text + "'; extracted '" + extracted + "'");
                Assert.AreEqual(number, extracted);
                Assert.AreEqual(match.RawString, extracted);

                EnsureTermination(text, defaultCountry, leniency);
            }
        }

        /**
        * Exhaustively searches for phone numbers from each index within {@code text} to test that
        * finding matches always terminates.
        */
        private void EnsureTermination(String text, String defaultCountry, PhoneNumberUtil.Leniency leniency)
        {
            for (int index = 0; index <= text.Length; index++)
            {
                String sub = text.Substring(index);
                StringBuilder matches = new StringBuilder();
                // Iterates over all matches.
                foreach (var match in phoneUtil.FindNumbers(sub, defaultCountry, leniency, long.MaxValue))
                    matches.Append(", ").Append(match.ToString());
            }
        }

        private IEnumerable<PhoneNumberMatch> findNumbersForLeniency(
      String text, String defaultCountry, PhoneNumberUtil.Leniency leniency)
        {
            return phoneUtil.FindNumbers(text, defaultCountry, leniency, long.MaxValue);
        }

        /**
        * Returns true if there were no matches found.
        */
        private bool hasNoMatches(IEnumerable<PhoneNumberMatch> iterable)
        {
            return !iterable.GetEnumerator().MoveNext();
        }

        /**
        * Small class that holds the context of the number we are testing against. The test will
        * insert the phone number to be found between leadingText and trailingText.
        */
        private class NumberContext
        {
            public readonly String leadingText;
            public readonly String trailingText;

            public NumberContext(String leadingText, String trailingText)
            {
                this.leadingText = leadingText;
                this.trailingText = trailingText;
            }
        }

        /**
           * Small class that holds the number we want to test and the region for which it should be valid.
           */
        private class NumberTest
        {
            public readonly String rawString;
            public readonly String region;

            public NumberTest(String rawString, String regionCode)
            {
                this.rawString = rawString;
                this.region = regionCode;
            }

            public override String ToString()
            {
                return rawString + " (" + region + ")";
            }
        }

    }
}
