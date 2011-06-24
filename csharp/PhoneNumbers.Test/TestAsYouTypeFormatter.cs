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
    class TestAsYouTypeFormatter
    {
        private PhoneNumberUtil phoneUtil;

        [TestFixtureSetUp]
        public void SetupFixture()
        {
            phoneUtil = TestPhoneNumberUtil.InitializePhoneUtilForTesting();
        }

        [Test]
        public void TestInvalidRegion()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("ZZ");
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+4", formatter.InputDigit('4'));
            Assert.AreEqual("+48 ", formatter.InputDigit('8'));
            Assert.AreEqual("+48 8", formatter.InputDigit('8'));
            Assert.AreEqual("+48 88", formatter.InputDigit('8'));
            Assert.AreEqual("+48 88 1", formatter.InputDigit('1'));
            Assert.AreEqual("+48 88 12", formatter.InputDigit('2'));
            Assert.AreEqual("+48 88 123", formatter.InputDigit('3'));
            Assert.AreEqual("+48 88 123 1", formatter.InputDigit('1'));
            Assert.AreEqual("+48 88 123 12", formatter.InputDigit('2'));

            formatter.Clear();
            Assert.AreEqual("6", formatter.InputDigit('6'));
            Assert.AreEqual("65", formatter.InputDigit('5'));
            Assert.AreEqual("650", formatter.InputDigit('0'));
            Assert.AreEqual("6502", formatter.InputDigit('2'));
            Assert.AreEqual("65025", formatter.InputDigit('5'));
            Assert.AreEqual("650253", formatter.InputDigit('3'));
        }

        [Test]
        public void TestTooLongNumberMatchingMultipleLeadingDigits()
        {
            // See http://code.google.com/p/libphonenumber/issues/detail?id=36
            // The bug occurred last time for countries which have two formatting rules with exactly the
            // same leading digits pattern but differ in length.
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("ZZ");
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+8", formatter.InputDigit('8'));
            Assert.AreEqual("+81 ", formatter.InputDigit('1'));
            Assert.AreEqual("+81 9", formatter.InputDigit('9'));
            Assert.AreEqual("+81 90", formatter.InputDigit('0'));
            Assert.AreEqual("+81 90 1", formatter.InputDigit('1'));
            Assert.AreEqual("+81 90 12", formatter.InputDigit('2'));
            Assert.AreEqual("+81 90 123", formatter.InputDigit('3'));
            Assert.AreEqual("+81 90 1234", formatter.InputDigit('4'));
            Assert.AreEqual("+81 90 1234 5", formatter.InputDigit('5'));
            Assert.AreEqual("+81 90 1234 56", formatter.InputDigit('6'));
            Assert.AreEqual("+81 90 1234 567", formatter.InputDigit('7'));
            Assert.AreEqual("+81 90 1234 5678", formatter.InputDigit('8'));
            Assert.AreEqual("+81 90 12 345 6789", formatter.InputDigit('9'));
            Assert.AreEqual("+81901234567890", formatter.InputDigit('0'));
        }


        [Test]
        public void TestAYTFUS()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("US");
            Assert.AreEqual("6", formatter.InputDigit('6'));
            Assert.AreEqual("65", formatter.InputDigit('5'));
            Assert.AreEqual("650", formatter.InputDigit('0'));
            Assert.AreEqual("650 2", formatter.InputDigit('2'));
            Assert.AreEqual("650 25", formatter.InputDigit('5'));
            Assert.AreEqual("650 253", formatter.InputDigit('3'));
            // Note this is how a US local number (without area code) should be formatted.
            Assert.AreEqual("650 2532", formatter.InputDigit('2'));
            Assert.AreEqual("650 253 22", formatter.InputDigit('2'));
            Assert.AreEqual("650 253 222", formatter.InputDigit('2'));
            Assert.AreEqual("650 253 2222", formatter.InputDigit('2'));

            formatter.Clear();
            Assert.AreEqual("1", formatter.InputDigit('1'));
            Assert.AreEqual("16", formatter.InputDigit('6'));
            Assert.AreEqual("1 65", formatter.InputDigit('5'));
            Assert.AreEqual("1 650", formatter.InputDigit('0'));
            Assert.AreEqual("1 650 2", formatter.InputDigit('2'));
            Assert.AreEqual("1 650 25", formatter.InputDigit('5'));
            Assert.AreEqual("1 650 253", formatter.InputDigit('3'));
            Assert.AreEqual("1 650 253 2", formatter.InputDigit('2'));
            Assert.AreEqual("1 650 253 22", formatter.InputDigit('2'));
            Assert.AreEqual("1 650 253 222", formatter.InputDigit('2'));
            Assert.AreEqual("1 650 253 2222", formatter.InputDigit('2'));

            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("01", formatter.InputDigit('1'));
            Assert.AreEqual("011 ", formatter.InputDigit('1'));
            Assert.AreEqual("011 4", formatter.InputDigit('4'));
            Assert.AreEqual("011 44 ", formatter.InputDigit('4'));
            Assert.AreEqual("011 44 6", formatter.InputDigit('6'));
            Assert.AreEqual("011 44 61", formatter.InputDigit('1'));
            Assert.AreEqual("011 44 6 12", formatter.InputDigit('2'));
            Assert.AreEqual("011 44 6 123", formatter.InputDigit('3'));
            Assert.AreEqual("011 44 6 123 1", formatter.InputDigit('1'));
            Assert.AreEqual("011 44 6 123 12", formatter.InputDigit('2'));
            Assert.AreEqual("011 44 6 123 123", formatter.InputDigit('3'));
            Assert.AreEqual("011 44 6 123 123 1", formatter.InputDigit('1'));
            Assert.AreEqual("011 44 6 123 123 12", formatter.InputDigit('2'));
            Assert.AreEqual("011 44 6 123 123 123", formatter.InputDigit('3'));

            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("01", formatter.InputDigit('1'));
            Assert.AreEqual("011 ", formatter.InputDigit('1'));
            Assert.AreEqual("011 5", formatter.InputDigit('5'));
            Assert.AreEqual("011 54 ", formatter.InputDigit('4'));
            Assert.AreEqual("011 54 9", formatter.InputDigit('9'));
            Assert.AreEqual("011 54 91", formatter.InputDigit('1'));
            Assert.AreEqual("011 54 9 11", formatter.InputDigit('1'));
            Assert.AreEqual("011 54 9 11 2", formatter.InputDigit('2'));
            Assert.AreEqual("011 54 9 11 23", formatter.InputDigit('3'));
            Assert.AreEqual("011 54 9 11 231", formatter.InputDigit('1'));
            Assert.AreEqual("011 54 9 11 2312", formatter.InputDigit('2'));
            Assert.AreEqual("011 54 9 11 2312 1", formatter.InputDigit('1'));
            Assert.AreEqual("011 54 9 11 2312 12", formatter.InputDigit('2'));
            Assert.AreEqual("011 54 9 11 2312 123", formatter.InputDigit('3'));
            Assert.AreEqual("011 54 9 11 2312 1234", formatter.InputDigit('4'));

            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("01", formatter.InputDigit('1'));
            Assert.AreEqual("011 ", formatter.InputDigit('1'));
            Assert.AreEqual("011 2", formatter.InputDigit('2'));
            Assert.AreEqual("011 24", formatter.InputDigit('4'));
            Assert.AreEqual("011 244 ", formatter.InputDigit('4'));
            Assert.AreEqual("011 244 2", formatter.InputDigit('2'));
            Assert.AreEqual("011 244 28", formatter.InputDigit('8'));
            Assert.AreEqual("011 244 280", formatter.InputDigit('0'));
            Assert.AreEqual("011 244 280 0", formatter.InputDigit('0'));
            Assert.AreEqual("011 244 280 00", formatter.InputDigit('0'));
            Assert.AreEqual("011 244 280 000", formatter.InputDigit('0'));
            Assert.AreEqual("011 244 280 000 0", formatter.InputDigit('0'));
            Assert.AreEqual("011 244 280 000 00", formatter.InputDigit('0'));
            Assert.AreEqual("011 244 280 000 000", formatter.InputDigit('0'));

            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+4", formatter.InputDigit('4'));
            Assert.AreEqual("+48 ", formatter.InputDigit('8'));
            Assert.AreEqual("+48 8", formatter.InputDigit('8'));
            Assert.AreEqual("+48 88", formatter.InputDigit('8'));
            Assert.AreEqual("+48 88 1", formatter.InputDigit('1'));
            Assert.AreEqual("+48 88 12", formatter.InputDigit('2'));
            Assert.AreEqual("+48 88 123", formatter.InputDigit('3'));
            Assert.AreEqual("+48 88 123 1", formatter.InputDigit('1'));
            Assert.AreEqual("+48 88 123 12", formatter.InputDigit('2'));
            Assert.AreEqual("+48 88 123 12 1", formatter.InputDigit('1'));
            Assert.AreEqual("+48 88 123 12 12", formatter.InputDigit('2'));
        }

        [Test]
        public void TestAYTFUSFullWidthCharacters()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("US");
            Assert.AreEqual("\uFF16", formatter.InputDigit('\uFF16'));
            Assert.AreEqual("\uFF16\uFF15", formatter.InputDigit('\uFF15'));
            Assert.AreEqual("650", formatter.InputDigit('\uFF10'));
            Assert.AreEqual("650 2", formatter.InputDigit('\uFF12'));
            Assert.AreEqual("650 25", formatter.InputDigit('\uFF15'));
            Assert.AreEqual("650 253", formatter.InputDigit('\uFF13'));
            Assert.AreEqual("650 2532", formatter.InputDigit('\uFF12'));
            Assert.AreEqual("650 253 22", formatter.InputDigit('\uFF12'));
            Assert.AreEqual("650 253 222", formatter.InputDigit('\uFF12'));
            Assert.AreEqual("650 253 2222", formatter.InputDigit('\uFF12'));
        }

        [Test]
        public void TestAYTFUSMobileShortCode()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("US");
            Assert.AreEqual("*", formatter.InputDigit('*'));
            Assert.AreEqual("*1", formatter.InputDigit('1'));
            Assert.AreEqual("*12", formatter.InputDigit('2'));
            Assert.AreEqual("*121", formatter.InputDigit('1'));
            Assert.AreEqual("*121#", formatter.InputDigit('#'));
        }

        [Test]
        public void TestAYTFUSVanityNumber()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("US");
            Assert.AreEqual("8", formatter.InputDigit('8'));
            Assert.AreEqual("80", formatter.InputDigit('0'));
            Assert.AreEqual("800", formatter.InputDigit('0'));
            Assert.AreEqual("800 ", formatter.InputDigit(' '));
            Assert.AreEqual("800 M", formatter.InputDigit('M'));
            Assert.AreEqual("800 MY", formatter.InputDigit('Y'));
            Assert.AreEqual("800 MY ", formatter.InputDigit(' '));
            Assert.AreEqual("800 MY A", formatter.InputDigit('A'));
            Assert.AreEqual("800 MY AP", formatter.InputDigit('P'));
            Assert.AreEqual("800 MY APP", formatter.InputDigit('P'));
            Assert.AreEqual("800 MY APPL", formatter.InputDigit('L'));
            Assert.AreEqual("800 MY APPLE", formatter.InputDigit('E'));
        }

        [Test]
        public void TestAYTFAndRememberPositionUS()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("US");
            Assert.AreEqual("1", formatter.InputDigitAndRememberPosition('1'));
            Assert.AreEqual(1, formatter.GetRememberedPosition());
            Assert.AreEqual("16", formatter.InputDigit('6'));
            Assert.AreEqual("1 65", formatter.InputDigit('5'));
            Assert.AreEqual(1, formatter.GetRememberedPosition());
            Assert.AreEqual("1 650", formatter.InputDigitAndRememberPosition('0'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("1 650 2", formatter.InputDigit('2'));
            Assert.AreEqual("1 650 25", formatter.InputDigit('5'));
            // Note the remembered position for digit "0" changes from 4 to 5, because a space is now
            // inserted in the front.
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("1 650 253", formatter.InputDigit('3'));
            Assert.AreEqual("1 650 253 2", formatter.InputDigit('2'));
            Assert.AreEqual("1 650 253 22", formatter.InputDigit('2'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("1 650 253 222", formatter.InputDigitAndRememberPosition('2'));
            Assert.AreEqual(13, formatter.GetRememberedPosition());
            Assert.AreEqual("1 650 253 2222", formatter.InputDigit('2'));
            Assert.AreEqual(13, formatter.GetRememberedPosition());
            Assert.AreEqual("165025322222", formatter.InputDigit('2'));
            Assert.AreEqual(10, formatter.GetRememberedPosition());
            Assert.AreEqual("1650253222222", formatter.InputDigit('2'));
            Assert.AreEqual(10, formatter.GetRememberedPosition());

            formatter.Clear();
            Assert.AreEqual("1", formatter.InputDigit('1'));
            Assert.AreEqual("16", formatter.InputDigitAndRememberPosition('6'));
            Assert.AreEqual(2, formatter.GetRememberedPosition());
            Assert.AreEqual("1 65", formatter.InputDigit('5'));
            Assert.AreEqual("1 650", formatter.InputDigit('0'));
            Assert.AreEqual(3, formatter.GetRememberedPosition());
            Assert.AreEqual("1 650 2", formatter.InputDigit('2'));
            Assert.AreEqual("1 650 25", formatter.InputDigit('5'));
            Assert.AreEqual(3, formatter.GetRememberedPosition());
            Assert.AreEqual("1 650 253", formatter.InputDigit('3'));
            Assert.AreEqual("1 650 253 2", formatter.InputDigit('2'));
            Assert.AreEqual("1 650 253 22", formatter.InputDigit('2'));
            Assert.AreEqual(3, formatter.GetRememberedPosition());
            Assert.AreEqual("1 650 253 222", formatter.InputDigit('2'));
            Assert.AreEqual("1 650 253 2222", formatter.InputDigit('2'));
            Assert.AreEqual("165025322222", formatter.InputDigit('2'));
            Assert.AreEqual(2, formatter.GetRememberedPosition());
            Assert.AreEqual("1650253222222", formatter.InputDigit('2'));
            Assert.AreEqual(2, formatter.GetRememberedPosition());

            formatter.Clear();
            Assert.AreEqual("6", formatter.InputDigit('6'));
            Assert.AreEqual("65", formatter.InputDigit('5'));
            Assert.AreEqual("650", formatter.InputDigit('0'));
            Assert.AreEqual("650 2", formatter.InputDigit('2'));
            Assert.AreEqual("650 25", formatter.InputDigit('5'));
            Assert.AreEqual("650 253", formatter.InputDigit('3'));
            Assert.AreEqual("650 2532", formatter.InputDigitAndRememberPosition('2'));
            Assert.AreEqual(8, formatter.GetRememberedPosition());
            Assert.AreEqual("650 253 22", formatter.InputDigit('2'));
            Assert.AreEqual(9, formatter.GetRememberedPosition());
            Assert.AreEqual("650 253 222", formatter.InputDigit('2'));
            // No more formatting when semicolon is entered.
            Assert.AreEqual("650253222;", formatter.InputDigit(';'));
            Assert.AreEqual(7, formatter.GetRememberedPosition());
            Assert.AreEqual("650253222;2", formatter.InputDigit('2'));

            formatter.Clear();
            Assert.AreEqual("6", formatter.InputDigit('6'));
            Assert.AreEqual("65", formatter.InputDigit('5'));
            Assert.AreEqual("650", formatter.InputDigit('0'));
            // No more formatting when users choose to do their own formatting.
            Assert.AreEqual("650-", formatter.InputDigit('-'));
            Assert.AreEqual("650-2", formatter.InputDigitAndRememberPosition('2'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("650-25", formatter.InputDigit('5'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("650-253", formatter.InputDigit('3'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("650-253-", formatter.InputDigit('-'));
            Assert.AreEqual("650-253-2", formatter.InputDigit('2'));
            Assert.AreEqual("650-253-22", formatter.InputDigit('2'));
            Assert.AreEqual("650-253-222", formatter.InputDigit('2'));
            Assert.AreEqual("650-253-2222", formatter.InputDigit('2'));

            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("01", formatter.InputDigit('1'));
            Assert.AreEqual("011 ", formatter.InputDigit('1'));
            Assert.AreEqual("011 4", formatter.InputDigitAndRememberPosition('4'));
            Assert.AreEqual("011 48 ", formatter.InputDigit('8'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("011 48 8", formatter.InputDigit('8'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("011 48 88", formatter.InputDigit('8'));
            Assert.AreEqual("011 48 88 1", formatter.InputDigit('1'));
            Assert.AreEqual("011 48 88 12", formatter.InputDigit('2'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("011 48 88 123", formatter.InputDigit('3'));
            Assert.AreEqual("011 48 88 123 1", formatter.InputDigit('1'));
            Assert.AreEqual("011 48 88 123 12", formatter.InputDigit('2'));
            Assert.AreEqual("011 48 88 123 12 1", formatter.InputDigit('1'));
            Assert.AreEqual("011 48 88 123 12 12", formatter.InputDigit('2'));

            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+1", formatter.InputDigit('1'));
            Assert.AreEqual("+1 6", formatter.InputDigitAndRememberPosition('6'));
            Assert.AreEqual("+1 65", formatter.InputDigit('5'));
            Assert.AreEqual("+1 650", formatter.InputDigit('0'));
            Assert.AreEqual(4, formatter.GetRememberedPosition());
            Assert.AreEqual("+1 650 2", formatter.InputDigit('2'));
            Assert.AreEqual(4, formatter.GetRememberedPosition());
            Assert.AreEqual("+1 650 25", formatter.InputDigit('5'));
            Assert.AreEqual("+1 650 253", formatter.InputDigitAndRememberPosition('3'));
            Assert.AreEqual("+1 650 253 2", formatter.InputDigit('2'));
            Assert.AreEqual("+1 650 253 22", formatter.InputDigit('2'));
            Assert.AreEqual("+1 650 253 222", formatter.InputDigit('2'));
            Assert.AreEqual(10, formatter.GetRememberedPosition());

            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+1", formatter.InputDigit('1'));
            Assert.AreEqual("+1 6", formatter.InputDigitAndRememberPosition('6'));
            Assert.AreEqual("+1 65", formatter.InputDigit('5'));
            Assert.AreEqual("+1 650", formatter.InputDigit('0'));
            Assert.AreEqual(4, formatter.GetRememberedPosition());
            Assert.AreEqual("+1 650 2", formatter.InputDigit('2'));
            Assert.AreEqual(4, formatter.GetRememberedPosition());
            Assert.AreEqual("+1 650 25", formatter.InputDigit('5'));
            Assert.AreEqual("+1 650 253", formatter.InputDigit('3'));
            Assert.AreEqual("+1 650 253 2", formatter.InputDigit('2'));
            Assert.AreEqual("+1 650 253 22", formatter.InputDigit('2'));
            Assert.AreEqual("+1 650 253 222", formatter.InputDigit('2'));
            Assert.AreEqual("+1650253222;", formatter.InputDigit(';'));
            Assert.AreEqual(3, formatter.GetRememberedPosition());
        }

        [Test]
        public void TestAYTFGBFixedLine()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("GB");
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("02", formatter.InputDigit('2'));
            Assert.AreEqual("020", formatter.InputDigit('0'));
            Assert.AreEqual("020 7", formatter.InputDigitAndRememberPosition('7'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("020 70", formatter.InputDigit('0'));
            Assert.AreEqual("020 703", formatter.InputDigit('3'));
            Assert.AreEqual(5, formatter.GetRememberedPosition());
            Assert.AreEqual("020 7031", formatter.InputDigit('1'));
            Assert.AreEqual("020 7031 3", formatter.InputDigit('3'));
            Assert.AreEqual("020 7031 30", formatter.InputDigit('0'));
            Assert.AreEqual("020 7031 300", formatter.InputDigit('0'));
            Assert.AreEqual("020 7031 3000", formatter.InputDigit('0'));
        }

        [Test]
        public void TestAYTFGBTollFree()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("gb");
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("08", formatter.InputDigit('8'));
            Assert.AreEqual("080", formatter.InputDigit('0'));
            Assert.AreEqual("080 7", formatter.InputDigit('7'));
            Assert.AreEqual("080 70", formatter.InputDigit('0'));
            Assert.AreEqual("080 703", formatter.InputDigit('3'));
            Assert.AreEqual("080 7031", formatter.InputDigit('1'));
            Assert.AreEqual("080 7031 3", formatter.InputDigit('3'));
            Assert.AreEqual("080 7031 30", formatter.InputDigit('0'));
            Assert.AreEqual("080 7031 300", formatter.InputDigit('0'));
            Assert.AreEqual("080 7031 3000", formatter.InputDigit('0'));
        }

        [Test]
        public void TestAYTFGBPremiumRate()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("GB");
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("09", formatter.InputDigit('9'));
            Assert.AreEqual("090", formatter.InputDigit('0'));
            Assert.AreEqual("090 7", formatter.InputDigit('7'));
            Assert.AreEqual("090 70", formatter.InputDigit('0'));
            Assert.AreEqual("090 703", formatter.InputDigit('3'));
            Assert.AreEqual("090 7031", formatter.InputDigit('1'));
            Assert.AreEqual("090 7031 3", formatter.InputDigit('3'));
            Assert.AreEqual("090 7031 30", formatter.InputDigit('0'));
            Assert.AreEqual("090 7031 300", formatter.InputDigit('0'));
            Assert.AreEqual("090 7031 3000", formatter.InputDigit('0'));
        }

        [Test]
        public void TestAYTFNZMobile()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("NZ");
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("02", formatter.InputDigit('2'));
            Assert.AreEqual("021", formatter.InputDigit('1'));
            Assert.AreEqual("02-11", formatter.InputDigit('1'));
            Assert.AreEqual("02-112", formatter.InputDigit('2'));
            // Note the unittest is using fake metadata which might produce non-ideal results.
            Assert.AreEqual("02-112 3", formatter.InputDigit('3'));
            Assert.AreEqual("02-112 34", formatter.InputDigit('4'));
            Assert.AreEqual("02-112 345", formatter.InputDigit('5'));
            Assert.AreEqual("02-112 3456", formatter.InputDigit('6'));
        }

        [Test]
        public void TestAYTFDE()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("DE");
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("03", formatter.InputDigit('3'));
            Assert.AreEqual("030", formatter.InputDigit('0'));
            Assert.AreEqual("030/1", formatter.InputDigit('1'));
            Assert.AreEqual("030/12", formatter.InputDigit('2'));
            Assert.AreEqual("030/123", formatter.InputDigit('3'));
            Assert.AreEqual("030/1234", formatter.InputDigit('4'));

            // 04134 1234
            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("04", formatter.InputDigit('4'));
            Assert.AreEqual("041", formatter.InputDigit('1'));
            Assert.AreEqual("041 3", formatter.InputDigit('3'));
            Assert.AreEqual("041 34", formatter.InputDigit('4'));
            Assert.AreEqual("04134 1", formatter.InputDigit('1'));
            Assert.AreEqual("04134 12", formatter.InputDigit('2'));
            Assert.AreEqual("04134 123", formatter.InputDigit('3'));
            Assert.AreEqual("04134 1234", formatter.InputDigit('4'));

            // 08021 2345
            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("08", formatter.InputDigit('8'));
            Assert.AreEqual("080", formatter.InputDigit('0'));
            Assert.AreEqual("080 2", formatter.InputDigit('2'));
            Assert.AreEqual("080 21", formatter.InputDigit('1'));
            Assert.AreEqual("08021 2", formatter.InputDigit('2'));
            Assert.AreEqual("08021 23", formatter.InputDigit('3'));
            Assert.AreEqual("08021 234", formatter.InputDigit('4'));
            Assert.AreEqual("08021 2345", formatter.InputDigit('5'));

            // 00 1 650 253 2250
            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("00", formatter.InputDigit('0'));
            Assert.AreEqual("00 1 ", formatter.InputDigit('1'));
            Assert.AreEqual("00 1 6", formatter.InputDigit('6'));
            Assert.AreEqual("00 1 65", formatter.InputDigit('5'));
            Assert.AreEqual("00 1 650", formatter.InputDigit('0'));
            Assert.AreEqual("00 1 650 2", formatter.InputDigit('2'));
            Assert.AreEqual("00 1 650 25", formatter.InputDigit('5'));
            Assert.AreEqual("00 1 650 253", formatter.InputDigit('3'));
            Assert.AreEqual("00 1 650 253 2", formatter.InputDigit('2'));
            Assert.AreEqual("00 1 650 253 22", formatter.InputDigit('2'));
            Assert.AreEqual("00 1 650 253 222", formatter.InputDigit('2'));
            Assert.AreEqual("00 1 650 253 2222", formatter.InputDigit('2'));
        }

        [Test]
        public void TestAYTFAR()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("AR");
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("01", formatter.InputDigit('1'));
            Assert.AreEqual("011", formatter.InputDigit('1'));
            Assert.AreEqual("011 7", formatter.InputDigit('7'));
            Assert.AreEqual("011 70", formatter.InputDigit('0'));
            Assert.AreEqual("011 703", formatter.InputDigit('3'));
            Assert.AreEqual("011 7031", formatter.InputDigit('1'));
            Assert.AreEqual("011 7031-3", formatter.InputDigit('3'));
            Assert.AreEqual("011 7031-30", formatter.InputDigit('0'));
            Assert.AreEqual("011 7031-300", formatter.InputDigit('0'));
            Assert.AreEqual("011 7031-3000", formatter.InputDigit('0'));
        }

        [Test]
        public void TestAYTFARMobile()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("AR");
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+5", formatter.InputDigit('5'));
            Assert.AreEqual("+54 ", formatter.InputDigit('4'));
            Assert.AreEqual("+54 9", formatter.InputDigit('9'));
            Assert.AreEqual("+54 91", formatter.InputDigit('1'));
            Assert.AreEqual("+54 9 11", formatter.InputDigit('1'));
            Assert.AreEqual("+54 9 11 2", formatter.InputDigit('2'));
            Assert.AreEqual("+54 9 11 23", formatter.InputDigit('3'));
            Assert.AreEqual("+54 9 11 231", formatter.InputDigit('1'));
            Assert.AreEqual("+54 9 11 2312", formatter.InputDigit('2'));
            Assert.AreEqual("+54 9 11 2312 1", formatter.InputDigit('1'));
            Assert.AreEqual("+54 9 11 2312 12", formatter.InputDigit('2'));
            Assert.AreEqual("+54 9 11 2312 123", formatter.InputDigit('3'));
            Assert.AreEqual("+54 9 11 2312 1234", formatter.InputDigit('4'));
        }

        [Test]
        public void TestAYTFKR()
        {
            // +82 51 234 5678
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("KR");
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+8", formatter.InputDigit('8'));
            Assert.AreEqual("+82 ", formatter.InputDigit('2'));
            Assert.AreEqual("+82 5", formatter.InputDigit('5'));
            Assert.AreEqual("+82 51", formatter.InputDigit('1'));
            Assert.AreEqual("+82 51-2", formatter.InputDigit('2'));
            Assert.AreEqual("+82 51-23", formatter.InputDigit('3'));
            Assert.AreEqual("+82 51-234", formatter.InputDigit('4'));
            Assert.AreEqual("+82 51-234-5", formatter.InputDigit('5'));
            Assert.AreEqual("+82 51-234-56", formatter.InputDigit('6'));
            Assert.AreEqual("+82 51-234-567", formatter.InputDigit('7'));
            Assert.AreEqual("+82 51-234-5678", formatter.InputDigit('8'));

            // +82 2 531 5678
            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+8", formatter.InputDigit('8'));
            Assert.AreEqual("+82 ", formatter.InputDigit('2'));
            Assert.AreEqual("+82 2", formatter.InputDigit('2'));
            Assert.AreEqual("+82 25", formatter.InputDigit('5'));
            Assert.AreEqual("+82 2-53", formatter.InputDigit('3'));
            Assert.AreEqual("+82 2-531", formatter.InputDigit('1'));
            Assert.AreEqual("+82 2-531-5", formatter.InputDigit('5'));
            Assert.AreEqual("+82 2-531-56", formatter.InputDigit('6'));
            Assert.AreEqual("+82 2-531-567", formatter.InputDigit('7'));
            Assert.AreEqual("+82 2-531-5678", formatter.InputDigit('8'));

            // +82 2 3665 5678
            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+8", formatter.InputDigit('8'));
            Assert.AreEqual("+82 ", formatter.InputDigit('2'));
            Assert.AreEqual("+82 2", formatter.InputDigit('2'));
            Assert.AreEqual("+82 23", formatter.InputDigit('3'));
            Assert.AreEqual("+82 2-36", formatter.InputDigit('6'));
            Assert.AreEqual("+82 2-366", formatter.InputDigit('6'));
            Assert.AreEqual("+82 2-3665", formatter.InputDigit('5'));
            Assert.AreEqual("+82 2-3665-5", formatter.InputDigit('5'));
            Assert.AreEqual("+82 2-3665-56", formatter.InputDigit('6'));
            Assert.AreEqual("+82 2-3665-567", formatter.InputDigit('7'));
            Assert.AreEqual("+82 2-3665-5678", formatter.InputDigit('8'));

            // 02-114
            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("02", formatter.InputDigit('2'));
            Assert.AreEqual("021", formatter.InputDigit('1'));
            Assert.AreEqual("02-11", formatter.InputDigit('1'));
            Assert.AreEqual("02-114", formatter.InputDigit('4'));

            // 02-1300
            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("02", formatter.InputDigit('2'));
            Assert.AreEqual("021", formatter.InputDigit('1'));
            Assert.AreEqual("02-13", formatter.InputDigit('3'));
            Assert.AreEqual("02-130", formatter.InputDigit('0'));
            Assert.AreEqual("02-1300", formatter.InputDigit('0'));

            // 011-456-7890
            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("01", formatter.InputDigit('1'));
            Assert.AreEqual("011", formatter.InputDigit('1'));
            Assert.AreEqual("011-4", formatter.InputDigit('4'));
            Assert.AreEqual("011-45", formatter.InputDigit('5'));
            Assert.AreEqual("011-456", formatter.InputDigit('6'));
            Assert.AreEqual("011-456-7", formatter.InputDigit('7'));
            Assert.AreEqual("011-456-78", formatter.InputDigit('8'));
            Assert.AreEqual("011-456-789", formatter.InputDigit('9'));
            Assert.AreEqual("011-456-7890", formatter.InputDigit('0'));

            // 011-9876-7890
            formatter.Clear();
            Assert.AreEqual("0", formatter.InputDigit('0'));
            Assert.AreEqual("01", formatter.InputDigit('1'));
            Assert.AreEqual("011", formatter.InputDigit('1'));
            Assert.AreEqual("011-9", formatter.InputDigit('9'));
            Assert.AreEqual("011-98", formatter.InputDigit('8'));
            Assert.AreEqual("011-987", formatter.InputDigit('7'));
            Assert.AreEqual("011-9876", formatter.InputDigit('6'));
            Assert.AreEqual("011-9876-7", formatter.InputDigit('7'));
            Assert.AreEqual("011-9876-78", formatter.InputDigit('8'));
            Assert.AreEqual("011-9876-789", formatter.InputDigit('9'));
            Assert.AreEqual("011-9876-7890", formatter.InputDigit('0'));
        }

        [Test]
        public void TestAYTF_MX()
        {
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("MX");

            // +52 800 123 4567
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+5", formatter.InputDigit('5'));
            Assert.AreEqual("+52 ", formatter.InputDigit('2'));
            Assert.AreEqual("+52 8", formatter.InputDigit('8'));
            Assert.AreEqual("+52 80", formatter.InputDigit('0'));
            Assert.AreEqual("+52 800", formatter.InputDigit('0'));
            Assert.AreEqual("+52 800 1", formatter.InputDigit('1'));
            Assert.AreEqual("+52 800 12", formatter.InputDigit('2'));
            Assert.AreEqual("+52 800 123", formatter.InputDigit('3'));
            Assert.AreEqual("+52 800 123 4", formatter.InputDigit('4'));
            Assert.AreEqual("+52 800 123 45", formatter.InputDigit('5'));
            Assert.AreEqual("+52 800 123 456", formatter.InputDigit('6'));
            Assert.AreEqual("+52 800 123 4567", formatter.InputDigit('7'));

            // +52 55 1234 5678
            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+5", formatter.InputDigit('5'));
            Assert.AreEqual("+52 ", formatter.InputDigit('2'));
            Assert.AreEqual("+52 5", formatter.InputDigit('5'));
            Assert.AreEqual("+52 55", formatter.InputDigit('5'));
            Assert.AreEqual("+52 55 1", formatter.InputDigit('1'));
            Assert.AreEqual("+52 55 12", formatter.InputDigit('2'));
            Assert.AreEqual("+52 55 123", formatter.InputDigit('3'));
            Assert.AreEqual("+52 55 1234", formatter.InputDigit('4'));
            Assert.AreEqual("+52 55 1234 5", formatter.InputDigit('5'));
            Assert.AreEqual("+52 55 1234 56", formatter.InputDigit('6'));
            Assert.AreEqual("+52 55 1234 567", formatter.InputDigit('7'));
            Assert.AreEqual("+52 55 1234 5678", formatter.InputDigit('8'));

            // +52 212 345 6789
            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+5", formatter.InputDigit('5'));
            Assert.AreEqual("+52 ", formatter.InputDigit('2'));
            Assert.AreEqual("+52 2", formatter.InputDigit('2'));
            Assert.AreEqual("+52 21", formatter.InputDigit('1'));
            Assert.AreEqual("+52 212", formatter.InputDigit('2'));
            Assert.AreEqual("+52 212 3", formatter.InputDigit('3'));
            Assert.AreEqual("+52 212 34", formatter.InputDigit('4'));
            Assert.AreEqual("+52 212 345", formatter.InputDigit('5'));
            Assert.AreEqual("+52 212 345 6", formatter.InputDigit('6'));
            Assert.AreEqual("+52 212 345 67", formatter.InputDigit('7'));
            Assert.AreEqual("+52 212 345 678", formatter.InputDigit('8'));
            Assert.AreEqual("+52 212 345 6789", formatter.InputDigit('9'));

            // +52 1 55 1234 5678
            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+5", formatter.InputDigit('5'));
            Assert.AreEqual("+52 ", formatter.InputDigit('2'));
            Assert.AreEqual("+52 1", formatter.InputDigit('1'));
            Assert.AreEqual("+52 15", formatter.InputDigit('5'));
            Assert.AreEqual("+52 1 55", formatter.InputDigit('5'));
            Assert.AreEqual("+52 1 55 1", formatter.InputDigit('1'));
            Assert.AreEqual("+52 1 55 12", formatter.InputDigit('2'));
            Assert.AreEqual("+52 1 55 123", formatter.InputDigit('3'));
            Assert.AreEqual("+52 1 55 1234", formatter.InputDigit('4'));
            Assert.AreEqual("+52 1 55 1234 5", formatter.InputDigit('5'));
            Assert.AreEqual("+52 1 55 1234 56", formatter.InputDigit('6'));
            Assert.AreEqual("+52 1 55 1234 567", formatter.InputDigit('7'));
            Assert.AreEqual("+52 1 55 1234 5678", formatter.InputDigit('8'));

            // +52 1 541 234 5678
            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+5", formatter.InputDigit('5'));
            Assert.AreEqual("+52 ", formatter.InputDigit('2'));
            Assert.AreEqual("+52 1", formatter.InputDigit('1'));
            Assert.AreEqual("+52 15", formatter.InputDigit('5'));
            Assert.AreEqual("+52 1 54", formatter.InputDigit('4'));
            Assert.AreEqual("+52 1 541", formatter.InputDigit('1'));
            Assert.AreEqual("+52 1 541 2", formatter.InputDigit('2'));
            Assert.AreEqual("+52 1 541 23", formatter.InputDigit('3'));
            Assert.AreEqual("+52 1 541 234", formatter.InputDigit('4'));
            Assert.AreEqual("+52 1 541 234 5", formatter.InputDigit('5'));
            Assert.AreEqual("+52 1 541 234 56", formatter.InputDigit('6'));
            Assert.AreEqual("+52 1 541 234 567", formatter.InputDigit('7'));
            Assert.AreEqual("+52 1 541 234 5678", formatter.InputDigit('8'));
        }

        [Test]
        public void TestAYTFMultipleLeadingDigitPatterns()
        {
            // +81 50 2345 6789
            AsYouTypeFormatter formatter = phoneUtil.GetAsYouTypeFormatter("JP");
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+8", formatter.InputDigit('8'));
            Assert.AreEqual("+81 ", formatter.InputDigit('1'));
            Assert.AreEqual("+81 5", formatter.InputDigit('5'));
            Assert.AreEqual("+81 50", formatter.InputDigit('0'));
            Assert.AreEqual("+81 50 2", formatter.InputDigit('2'));
            Assert.AreEqual("+81 50 23", formatter.InputDigit('3'));
            Assert.AreEqual("+81 50 234", formatter.InputDigit('4'));
            Assert.AreEqual("+81 50 2345", formatter.InputDigit('5'));
            Assert.AreEqual("+81 50 2345 6", formatter.InputDigit('6'));
            Assert.AreEqual("+81 50 2345 67", formatter.InputDigit('7'));
            Assert.AreEqual("+81 50 2345 678", formatter.InputDigit('8'));
            Assert.AreEqual("+81 50 2345 6789", formatter.InputDigit('9'));

            // +81 222 12 5678
            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+8", formatter.InputDigit('8'));
            Assert.AreEqual("+81 ", formatter.InputDigit('1'));
            Assert.AreEqual("+81 2", formatter.InputDigit('2'));
            Assert.AreEqual("+81 22", formatter.InputDigit('2'));
            Assert.AreEqual("+81 22 2", formatter.InputDigit('2'));
            Assert.AreEqual("+81 22 21", formatter.InputDigit('1'));
            Assert.AreEqual("+81 2221 2", formatter.InputDigit('2'));
            Assert.AreEqual("+81 222 12 5", formatter.InputDigit('5'));
            Assert.AreEqual("+81 222 12 56", formatter.InputDigit('6'));
            Assert.AreEqual("+81 222 12 567", formatter.InputDigit('7'));
            Assert.AreEqual("+81 222 12 5678", formatter.InputDigit('8'));

            // +81 3332 2 5678
            formatter.Clear();
            Assert.AreEqual("+", formatter.InputDigit('+'));
            Assert.AreEqual("+8", formatter.InputDigit('8'));
            Assert.AreEqual("+81 ", formatter.InputDigit('1'));
            Assert.AreEqual("+81 3", formatter.InputDigit('3'));
            Assert.AreEqual("+81 33", formatter.InputDigit('3'));
            Assert.AreEqual("+81 33 3", formatter.InputDigit('3'));
            Assert.AreEqual("+81 3332", formatter.InputDigit('2'));
            Assert.AreEqual("+81 3332 2", formatter.InputDigit('2'));
            Assert.AreEqual("+81 3332 2 5", formatter.InputDigit('5'));
            Assert.AreEqual("+81 3332 2 56", formatter.InputDigit('6'));
            Assert.AreEqual("+81 3332 2 567", formatter.InputDigit('7'));
            Assert.AreEqual("+81 3332 2 5678", formatter.InputDigit('8'));
        }
    }
}
