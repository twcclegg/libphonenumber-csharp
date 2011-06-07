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
    class TestPhoneNumberMatcher
    {
        private PhoneNumberUtil phoneUtil;

        [SetUp]
        protected void SetUp()
        {
            phoneUtil = TestPhoneNumberUtil.InitializePhoneUtilForTesting();
        }

        /** See {@link PhoneNumberUtilTest#testParseNationalNumber()}. */
        [Test]
        public void TestFindNationalNumber()
        {
            // same cases as in testParseNationalNumber
            doTestFindInContext("033316005", "NZ");
            doTestFindInContext("33316005", "NZ");
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
            doTestFindInContext("(800) 901-3355 , 7246433", "US");
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
            String zipPreceding = "My address is CA 34215. " + number + " is my number.";
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
            foreach(var match in iterable)
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

            // Only matches the first 5 despite there being 100 numbers due to max matches.
            // There are two false positives per line as "123" is also tried.
            List<PhoneNumber> expected = new List<PhoneNumber>(100);
            PhoneNumber number = phoneUtil.Parse("+14156667777", null);
            for (int i = 0; i < 5; i++)
                expected.Add(number);

            var iterable = phoneUtil.FindNumbers(numbers.ToString(), "US", PhoneNumberUtil.Leniency.VALID, 10);
            List<PhoneNumber> actual = new List<PhoneNumber>(100);
            foreach(var match in iterable)
                actual.Add(match.Number);
            Assert.AreEqual(expected, actual);
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
            Assert.AreEqual(match.RawString, sub.Substring(match.Start, match.Length));
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
            List<NumberContext> contextPairs = new List<NumberContext>(15);
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
            contextPairs.Add(new NumberContext("Call me on June 21 at", ""));  // with a Month-Day date
            // With publication pages.
            contextPairs.Add(new NumberContext(
            "As quoted by Alfonso 12-15 (2009), you may call me at ", ""));
            contextPairs.Add(new NumberContext(
            "As quoted by Alfonso et al. 12-15 (2009), you may call me at ", ""));
            // With dates, written in the American style.
            contextPairs.Add(new NumberContext(
            "As I said on 03/10/2011, you may call me at ", ""));
            contextPairs.Add(new NumberContext(
            "As I said on 03/27/2011, you may call me at ", ""));
            contextPairs.Add(new NumberContext(
            "As I said on 31/8/2011, you may call me at ", ""));
            contextPairs.Add(new NumberContext(
            "As I said on 1/12/2011, you may call me at ", ""));
            contextPairs.Add(new NumberContext(
            "I was born on 10/12/82. Please call me at ", ""));
            // With a postfix stripped off as it looks like the start of another number
            contextPairs.Add(new NumberContext("Call ", "/x12 more"));

            doTestInContext(number, defaultCountry, contextPairs, PhoneNumberUtil.Leniency.POSSIBLE);
        }

        /**
        * Tests valid numbers in contexts that fail for {@link Leniency#POSSIBLE}.
        */
        private void findValidInContext(String number, String defaultCountry)
        {
            List<NumberContext> contextPairs = new List<NumberContext>(5);
            // With other small numbers.
            contextPairs.Add(new NumberContext("It's only 9.99! Call ", " to buy"));
            // With a number Day.Month.Year date.
            contextPairs.Add(new NumberContext("Call me on 21.6.1984 at ", ""));
            // With a number Month/Day date.
            contextPairs.Add(new NumberContext("Call me on 06/21 at ", ""));
            // With a number Day.Month date
            contextPairs.Add(new NumberContext("Call me on 21.6. at ", ""));
            // With a number Month/Day/Year date.
            contextPairs.Add(new NumberContext("Call me on 06/21/84 at ", ""));
            doTestInContext(number, defaultCountry, contextPairs, PhoneNumberUtil.Leniency.VALID);
        }

        private void doTestInContext(String number, String defaultCountry,
            List<NumberContext> contextPairs, PhoneNumberUtil.Leniency leniency)
        {
            foreach(var context in contextPairs)
            {
                String prefix = context.leadingText;
                String text = prefix + number + context.trailingText;

                int start = prefix.Length;
                int length = number.Length;
                var iterable = phoneUtil.FindNumbers(text, defaultCountry, leniency, long.MaxValue);

                PhoneNumberMatch match = iterable.First();
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
                foreach(var match in phoneUtil.FindNumbers(sub, defaultCountry, leniency, long.MaxValue))
                    matches.Append(", ").Append(match.ToString());
            }
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
    }
}
