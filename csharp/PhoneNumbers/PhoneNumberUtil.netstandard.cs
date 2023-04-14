using System.Collections.Generic;
using System.Text;

namespace PhoneNumbers
{
    public partial class PhoneNumberUtil
    {
#if !(NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
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
            var sb = new StringBuilder(number);
            Normalize(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Normalizes a string of characters representing a phone number. This converts wide-ascii and
        /// arabic-indic numerals to European numerals, and strips punctuation and alpha characters.
        /// </summary>
        /// <param name="number">A string of characters representing a phone number.</param>
        /// <returns>The normalized string version of the phone number.</returns>
        public static string NormalizeDigitsOnly(string number) =>
            NormalizeDigits(new StringBuilder(number), false /* strip non-digits */).ToString();

        /// <summary>
        /// Normalizes a string of characters representing a phone number. This strips all characters which
        /// are not diallable on a mobile phone keypad (including all non-ASCII digits).
        /// </summary>
        /// <param name="number"> a string of characters representing a phone number</param>
        /// <returns> the normalized string version of the phone number</returns>
        public static string NormalizeDiallableCharsOnly(string number) =>
            NormalizeHelper(number, DiallableCharMappings, true /* remove non matches */);

        /// <summary>
        /// Converts all alpha characters in a number to their respective digits on a keypad, but retains
        /// existing formatting.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ConvertAlphaCharactersInNumber(string number) =>
            NormalizeHelper(number, AlphaPhoneMappings, false);

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

            var sb = new StringBuilder(20);
            Format(number, numberFormat, sb);
            return sb.ToString();
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
            var nationalNumber = new StringBuilder();
            if (number.ItalianLeadingZero && number.NumberOfLeadingZeros > 0)
            {
                nationalNumber.Append('0', number.NumberOfLeadingZeros);
            }

            nationalNumber.Append(number.NationalNumber);
            return nationalNumber.ToString();
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
            var countryCallingCode = number.CountryCode;
            var nationalSignificantNumber = GetNationalSignificantNumber(number);
            // Note getRegionCodeForCountryCode() is used because formatting information for regions which
            // share a country calling code is contained by only one region for performance reasons. For
            // example, for NANPA regions it will be contained in the metadata for US.
            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            if (!HasValidCountryCallingCode(countryCallingCode))
            {
                return nationalSignificantNumber;
            }

            var metadata = GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
            var formattedNumber = new StringBuilder(20);
            var formattingPattern =
                ChooseFormattingPatternForNumber(userDefinedFormats, nationalSignificantNumber);
            if (formattingPattern == null)
            {
                // If no pattern above is matched, we format the number as a whole.
                formattedNumber.Append(nationalSignificantNumber);
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

                formattedNumber.Append(
                    FormatNsnUsingPattern(nationalSignificantNumber, numFormatCopy.Build(), numberFormat));
            }

            MaybeAppendFormattedExtension(number, metadata, numberFormat, formattedNumber);
            PrefixNumberWithCountryCallingCode(countryCallingCode, numberFormat, formattedNumber);
            return formattedNumber.ToString();
        }
#endif
    }
}
