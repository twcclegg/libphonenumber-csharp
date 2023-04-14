using System;
using System.Collections.Generic;

namespace PhoneNumbers
{
    public partial class PhoneNumberUtil
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Normalizes a string of characters representing a phone number. This performs the following
        /// conversions:
        /// Punctuation is stripped.
        /// For ALPHA/VANITY numbers:
        /// Letters are converted to their numeric representation on a telephone keypad. The keypad
        /// used here is the one defined in ITU Recommendation E.161. This is only done if there are
        /// 3 or more letters in the number, to lessen the risk that such letters are typos.
        /// For other numbers:
        /// Wide-ascii digits are converted to normal ASCII (European) digits.
        /// Arabic-Indic numerals are converted to European numerals.
        /// Spurious alpha characters are stripped.
        /// Arabic-Indic numerals are converted to European numerals.
        /// </summary>
        /// <param name="number">A string of characters representing a phone number.</param>
        /// <returns>The normalized string version of the phone number.</returns>
        public static string Normalize(string number)
        {
            Span<char> result = stackalloc char[number.Length];
            var resultLength = 0;

            Normalize(ref result, ref resultLength, number);
            return new string(result.Slice(0, resultLength));
        }

        /// <summary>
        /// Normalizes a string of characters representing a phone number. This converts wide-ascii and
        /// arabic-indic numerals to European numerals, and strips punctuation and alpha characters.
        /// </summary>
        /// <param name="number">A string of characters representing a phone number.</param>
        /// <returns>The normalized string version of the phone number.</returns>
        public static string NormalizeDigitsOnly(string number)
        {
            Span<char> result = stackalloc char[number.Length];
            var resultLength = 0;

            NormalizeDigits(ref result, ref resultLength, number, false /* strip non-digits */);
            return new string(result.Slice(0, resultLength));
        }

        /// <summary>
        /// Normalizes a string of characters representing a phone number. This strips all characters which
        /// are not diallable on a mobile phone keypad (including all non-ASCII digits).
        /// </summary>
        /// <param name="number"> a string of characters representing a phone number</param>
        /// <returns> the normalized string version of the phone number</returns>
        public static string NormalizeDiallableCharsOnly(string number)
        {
            Span<char> result = stackalloc char[number.Length];
            var resultLength = 0;

            NormalizeHelper(ref result, ref resultLength, number, DiallableCharMappings, true /* remove non matches */);
            return new string(result.Slice(0, resultLength));
        }

        /// <summary>
        /// Converts all alpha characters in a number to their respective digits on a keypad, but retains
        /// existing formatting.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ConvertAlphaCharactersInNumber(string number)
        {
            Span<char> result = stackalloc char[number.Length];
            var resultLength = 0;

            NormalizeHelper(ref result, ref resultLength, number, AlphaPhoneMappings, false);
            return new string(result.Slice(0, resultLength));
        }

        /// <summary>
        /// Formats a phone number in the specified format using default rules. Note that this does not
        /// promise to produce a phone number that the user can dial from where they are - although we do
        /// format in either 'national' or 'international' format depending on what the client asks for, we
        /// do not currently support a more abbreviated format, such as for users in the same "area" who
        /// could potentially dial the number without area code. Note that if the phone number has a
        /// country calling code of 0 or an otherwise invalid country calling code, we cannot work out
        /// which formatting rules to apply so we return the national significant number with no formatting
        /// applied.
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="numberFormat">The format the phone number should be formatted into.</param>
        /// <returns>The formatted phone number.</returns>
        public string Format(PhoneNumber number, PhoneNumberFormat numberFormat)
        {
            if (number.NationalNumber == 0 && number.HasRawInput)
            {
                var rawInput = number.RawInput;
                if (rawInput.Length > 0)
                {
                    return rawInput;
                }
            }

            Span<char> formattedNumber = stackalloc char[90];
            var index = 0;
            Format(ref formattedNumber, ref index, number, numberFormat);
            return new string(formattedNumber.Slice(0, index));
        }

        /// <summary>
        /// Formats a phone number in the specified format using client-defined formatting rules. Note that
        /// if the phone number has a country calling code of zero or an otherwise invalid country calling
        /// code, we cannot work out things like whether there should be a national prefix applied, or how
        /// to format extensions, so we return the national significant number with no formatting applied.
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="numberFormat">The format the phone number should be formatted into.</param>
        /// <param name="userDefinedFormats">Formatting rules specified by clients.</param>
        /// <returns>The formatted phone number.</returns>
        public string FormatByPattern(PhoneNumber number,
            PhoneNumberFormat numberFormat,
            List<NumberFormat> userDefinedFormats)
        {
            Span<char> result = stackalloc char[90];
            var resultLength = 0;

            var countryCallingCode = number.CountryCode;

            // no point to optimize here
            var nationalSignificantNumber = GetNationalSignificantNumber(number);

            // Note getRegionCodeForCountryCode() is used because formatting information for regions which
            // share a country calling code is contained by only one region for performance reasons. For
            // example, for NANPA regions it will be contained in the metadata for US.
            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            if (!HasValidCountryCallingCode(countryCallingCode))
            {
                return nationalSignificantNumber;
            }

            PrefixNumberWithCountryCallingCode(ref result, ref resultLength, countryCallingCode, numberFormat);

            var metadata = GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
            var formattingPattern =
                ChooseFormattingPatternForNumber(userDefinedFormats, nationalSignificantNumber);
            if (formattingPattern == null)
            {
                // If no pattern above is matched, we format the number as a whole.
                AppendToSpan(ref result, ref resultLength, nationalSignificantNumber);
            }
            else
            {
                var numFormatCopy = new NumberFormat.Builder();
                // Before we do a replacement of the national prefix pattern $NP with the national prefix, we
                // need to copy the rule so that subsequent replacements for different numbers have the
                // appropriate national prefix.
                numFormatCopy.MergeFrom(formattingPattern);
                var nationalPrefixFormattingRule = formattingPattern.NationalPrefixFormattingRule;
                if (nationalPrefixFormattingRule.Length > 0)
                {
                    var nationalPrefix = metadata.NationalPrefix;
                    if (nationalPrefix.Length > 0)
                    {
                        // Replace $NP with national prefix and $FG with the first group ($1).
                        nationalPrefixFormattingRule = nationalPrefixFormattingRule.Replace(NpPattern, nationalPrefix);
                        nationalPrefixFormattingRule = nationalPrefixFormattingRule.Replace(FgPattern, "$1");
                        numFormatCopy.SetNationalPrefixFormattingRule(nationalPrefixFormattingRule);
                    }
                    else
                    {
                        // We don't want to have a rule for how to format the national prefix if there isn't one.
                        numFormatCopy.ClearNationalPrefixFormattingRule();
                    }
                }

                // no point to optimize, it works on regexes
                var formatted = FormatNsnUsingPattern(nationalSignificantNumber,
                    numFormatCopy.Build(),
                    numberFormat);

                AppendToSpan(ref result, ref resultLength, formatted);
            }

            MaybeAppendFormattedExtension(ref result, ref resultLength, number, metadata, numberFormat);
            return new string(result.Slice(0, resultLength));
        }

        /// <summary>
        /// Gets the national significant number of the a phone number. Note a national significant number
        /// doesn't contain a national prefix or any formatting.
        /// </summary>
        /// <param name="number">The PhoneNumber object for which the national significant number is needed.</param>
        /// <returns>The national significant number of the PhoneNumber object passed in.</returns>
        public string GetNationalSignificantNumber(PhoneNumber number)
        {
            // If a leading zero(s) has been set, we prefix this now. Note this is not a national prefix.
            Span<char> nationalSignificantNumber = stackalloc char[20];
            SetNationalSignificantNumberToSpan(ref nationalSignificantNumber, number, out var length);
            return new string(nationalSignificantNumber.Slice(0, length));
        }

        internal static void NormalizeDigits(ref Span<char> span,
            ref int index,
            string number,
            bool keepNonDigits)
        {
            index = 0;
            for (var i = 0; i < number.Length; i++)
            {
                var c = number[i];
                if ((uint)(c - '0') <= 9)
                {
                    span[index++] = c;
                }
                else
                {
                    var digit = (int)char.GetNumericValue(c);
                    if (digit != -1)
                    {
                        AppendNumberToSpan(ref span, ref index, digit);
                    }
                    else if (keepNonDigits)
                    {
                        span[index++] = c;
                    }
                }
            }
        }

        private static void Normalize(ref Span<char> span, ref int index, string number)
        {
            if (IsValidAlphaPhone(number))
            {
                NormalizeHelper(ref span, ref index, number, AlphaPhoneMappings, true);
            }
            else
            {
                NormalizeDigits(ref span, ref index, number, false);
            }
        }

        private static void NormalizeHelper(ref Span<char> span,
            ref int index,
            string number,
            IReadOnlyDictionary<char, char> normalizationReplacements,
            bool removeNonMatches)
        {
            index = 0;
            for (var i = 0; i < number.Length; i++)
            {
                var character = number[i];
                if (normalizationReplacements.TryGetValue(char.ToUpperInvariant(character), out var newDigit))
                {
                    span[index++] = newDigit;
                }
                else if (!removeNonMatches)
                {
                    span[index++] = character;
                }
                // If neither of the above are true, we remove this character.
            }
        }

        private static bool IsValidAlphaPhone(string number)
        {
            for (int alpha = 0, i = 0; i < number.Length; i++)
            {
                var lower = number[i] | 0x20;
                if ((uint)(lower - 'a') <= 'z' - 'a' && ++alpha == 3)
                {
                    return true;
                }
            }

            return false;
        }

        /**
         * Appends the formatted extension of a phone number to formattedNumber, if the phone number had
         * an extension specified.
         */
        private static void MaybeAppendFormattedExtension(ref Span<char> span,
            ref int index,
            PhoneNumber number,
            PhoneMetadata metadata,
            PhoneNumberFormat numberFormat)
        {
            if (!number.HasExtension || number.Extension.Length <= 0)
            {
                return;
            }

            if (numberFormat == PhoneNumberFormat.RFC3966)
            {
                AppendToSpan(ref span, ref index, RFC3966_EXTN_PREFIX);
                AppendToSpan(ref span, ref index, number.Extension);
                return;
            }

            if (metadata.HasPreferredExtnPrefix)
            {
                AppendToSpan(ref span, ref index, metadata.PreferredExtnPrefix);
                AppendToSpan(ref span, ref index, number.Extension);
                return;
            }

            AppendToSpan(ref span, ref index, DEFAULT_EXTN_PREFIX);
            AppendToSpan(ref span, ref index, number.Extension);
        }

        private static void SetNationalSignificantNumberToSpan(ref Span<char> nationalNumber,
            PhoneNumber number,
            out int nationalSignificantNumberLength)
        {
            nationalSignificantNumberLength = 0;
            if (number.ItalianLeadingZero)
            {
                for (var i = 0; i < number.NumberOfLeadingZeros; i++)
                {
                    nationalNumber[nationalSignificantNumberLength++] = '0';
                }
            }

            AppendNumberToSpan(ref nationalNumber, ref nationalSignificantNumberLength, number.NationalNumber);
        }

        private static void AppendNumberToSpan(ref Span<char> span, ref int index, ulong numberToAppend)
        {
            if (numberToAppend == 0)
            {
                span[index++] = '0';
            }

            var numberLength = numberToAppend switch
            {
                < 10    => 1,
                < 100   => 2,
                < 1_000 => 3,
                _       => (int)Math.Floor(Math.Log10(numberToAppend)) + 1,
            };

            var maxIndex = index + numberLength - 1;
            var startIndex = index;
            while (numberToAppend > 0)
            {
                span[maxIndex + startIndex - index++] = (char)('0' + numberToAppend % 10);
                numberToAppend /= 10;
            }
        }

        private static void AppendNumberToSpan(ref Span<char> span, ref int index, int numberToAppend)
        {
            if (numberToAppend == 0)
            {
                span[index++] = '0';
            }

            var numberLength = numberToAppend switch
            {
                < 10    => 1,
                < 100   => 2,
                < 1_000 => 3,
                _       => (int)Math.Floor(Math.Log10(numberToAppend)) + 1,
            };

            var maxIndex = index + numberLength - 1;
            var startIndex = index;
            while (numberToAppend > 0)
            {
                span[maxIndex + startIndex - index++] = (char)('0' + numberToAppend % 10);
                numberToAppend /= 10;
            }
        }

        private static void AppendToSpan(ref Span<char> destination, ref int index, string valueToAppend)
        {
            foreach (var c in valueToAppend)
            {
                destination[index++] = c;
            }
        }

        private void Format(ref Span<char> span, ref int index, PhoneNumber number, PhoneNumberFormat numberFormat)
        {
            Span<char> nationalSignificantNumber = stackalloc char[20];
            SetNationalSignificantNumberToSpan(ref nationalSignificantNumber,
                number,
                out var nationalSignificantNumberLength);

            var countryCallingCode = number.CountryCode;
            if (numberFormat == PhoneNumberFormat.E164)
            {
                // Early exit for E164 case since no formatting of the national number needs to be applied.
                // Extensions are not formatted.
                PrefixNumberWithCountryCallingCode(ref span, ref index, countryCallingCode, PhoneNumberFormat.E164);
                for (var i = 0; i < nationalSignificantNumberLength; i++)
                {
                    span[index++] = nationalSignificantNumber[i];
                }

                return;
            }

            // Note getRegionCodeForCountryCode() is used because formatting information for regions which
            // share a country calling code is contained by only one region for performance reasons. For
            // example, for NANPA regions it will be contained in the metadata for US.
            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            if (!HasValidCountryCallingCode(countryCallingCode))
            {
                for (var i = 0; i < nationalSignificantNumberLength; i++)
                {
                    span[index++] = nationalSignificantNumber[i];
                }

                return;
            }

            PrefixNumberWithCountryCallingCode(ref span, ref index, countryCallingCode, numberFormat);
            var metadata = GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
            AppendToSpan(ref span,
                ref index,
                FormatNsn(new string(nationalSignificantNumber.Slice(0, nationalSignificantNumberLength)),
                    metadata,
                    numberFormat));
            MaybeAppendFormattedExtension(ref span, ref index, number, metadata, numberFormat);
        }

        /**
        * A helper function that is used by format and formatByPattern.
        */
        private void PrefixNumberWithCountryCallingCode(ref Span<char> span,
            ref int currentIndex,
            int countryCallingCode,
            PhoneNumberFormat numberFormat)
        {
            switch (numberFormat)
            {
                case PhoneNumberFormat.E164:
                    span[currentIndex++] = PLUS_SIGN;
                    AppendNumberToSpan(ref span, ref currentIndex, countryCallingCode);
                    return;
                case PhoneNumberFormat.INTERNATIONAL:
                    span[currentIndex++] = PLUS_SIGN;
                    AppendNumberToSpan(ref span, ref currentIndex, countryCallingCode);
                    span[currentIndex++] = ' ';
                    return;
                case PhoneNumberFormat.RFC3966:
                    AppendToSpan(ref span, ref currentIndex, RFC3966_PREFIX);
                    span[currentIndex++] = PLUS_SIGN;
                    AppendNumberToSpan(ref span, ref currentIndex, countryCallingCode);
                    span[currentIndex++] = '-';
                    return;
                case PhoneNumberFormat.NATIONAL:
                    return;
                default:
                    return;
            }
        }
#endif
    }
}
