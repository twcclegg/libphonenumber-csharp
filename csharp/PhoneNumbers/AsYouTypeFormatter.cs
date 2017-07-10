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
using System.Text;
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    /**
     * A formatter which formats phone numbers as they are entered.
     *
     * <p>An AsYouTypeFormatter can be created by invoking
     * {@link PhoneNumberUtil#getAsYouTypeFormatter}. After that, digits can be added by invoking
     * {@link #inputDigit} on the formatter instance, and the partially formatted phone number will be
     * returned each time a digit is added. {@link #clear} can be invoked before formatting a new
     * number.
     *
     * <p>See the unittests for more details on how the formatter is to be used.
     *
     * @author Shaopeng Jia
     */
    public class AsYouTypeFormatter
    {
        private string currentOutput = "";
        private readonly StringBuilder formattingTemplate = new StringBuilder();
        // The pattern from numberFormat that is currently used to create formattingTemplate.
        private string currentFormattingPattern = "";
        private readonly StringBuilder accruedInput = new StringBuilder();
        private readonly StringBuilder accruedInputWithoutFormatting = new StringBuilder();
        // This indicates whether AsYouTypeFormatter is currently doing the formatting.
        private bool ableToFormat = true;
        // Set to true when users enter their own formatting. AsYouTypeFormatter will do no formatting at
        // all when this is set to true.
        private bool inputHasFormatting;
        private bool isInternationalFormatting;
        private bool isExpectingCountryCallingCode;
        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
        private readonly string defaultCountry;

        private static readonly PhoneMetadata EmptyMetadata =
            new PhoneMetadata.Builder().SetInternationalPrefix("NA").BuildPartial();
        private readonly PhoneMetadata defaultMetaData;
        private PhoneMetadata currentMetaData;

        // A pattern that is used to match character classes in regular expressions. An example of a
        // character class is [1-4].
        private static readonly Regex CharacterClassPattern = new Regex("\\[([^\\[\\]])*\\]", InternalRegexOptions.Default);
        // Any digit in a regular expression that actually denotes a digit. For example, in the regular
        // expression 80[0-2]\d{6,10}, the first 2 digits (8 and 0) are standalone digits, but the rest
        // are not.
        // Two look-aheads are needed because the number following \\d could be a two-digit number, since
        // the phone number can be as long as 15 digits.
        private static readonly Regex StandaloneDigitPattern = new Regex("\\d(?=[^,}][^,}])", InternalRegexOptions.Default);

        // A pattern that is used to determine if a numberFormat under availableFormats is eligible to be
        // used by the AYTF. It is eligible when the format element under numberFormat contains groups of
        // the dollar sign followed by a single digit, separated by valid phone number punctuation. This
        // prevents invalid punctuation (such as the star sign in Israeli star numbers) getting into the
        // output of the AYTF.
        private static readonly PhoneRegex EligibleFormatPattern =
            new PhoneRegex("[" + PhoneNumberUtil.ValidPunctuation + "]*" +
                "(\\$\\d" + "[" + PhoneNumberUtil.ValidPunctuation + "]*)+",
                InternalRegexOptions.Default);

        // This is the minimum length of national number accrued that is required to trigger the
        // formatter. The first element of the leadingDigitsPattern of each numberFormat contains a
        // regular expression that matches up to this number of digits.
        private static readonly int MinLeadingDigitsLength = 3;

        // The digits that have not been entered yet will be represented by a \u2008, the punctuation
        // space.
        private readonly string digitPlaceholder = "\u2008";
        private readonly Regex digitPattern;
        private int lastMatchPosition;
        // The position of a digit upon which inputDigitAndRememberPosition is most recently invoked, as
        // found in the original sequence of characters the user entered.
        private int originalPosition;
        // The position of a digit upon which inputDigitAndRememberPosition is most recently invoked, as
        // found in accruedInputWithoutFormatting.
        private int positionToRemember;
        // This contains anything that has been entered so far preceding the national significant number,
        // and it is formatted (e.g. with space inserted). For example, this can contain IDD, country
        // code, and/or NDD, etc.
        private readonly StringBuilder prefixBeforeNationalNumber = new StringBuilder();
        // This contains the national prefix that has been extracted. It contains only digits without
        // formatting.
        private string nationalPrefixExtracted = "";
        private readonly StringBuilder nationalNumber = new StringBuilder();
        private readonly List<NumberFormat> possibleFormats = new List<NumberFormat>();

        // A cache for frequently used country-specific regular expressions.
        private readonly RegexCache regexCache = new RegexCache(64);

        /**
         * Constructs an as-you-type formatter. Should be obtained from {@link
         * PhoneNumberUtil#getAsYouTypeFormatter}.
         *
         * @param regionCode  the country/region where the phone number is being entered
         */
        public AsYouTypeFormatter(string regionCode)
        {
            digitPattern = new Regex(digitPlaceholder, InternalRegexOptions.Default);
            defaultCountry = regionCode;
            currentMetaData = GetMetadataForRegion(defaultCountry);
            defaultMetaData = currentMetaData;
        }

        // The metadata needed by this class is the same for all regions sharing the same country calling
        // code. Therefore, we return the metadata for "main" region for this country calling code.
        private PhoneMetadata GetMetadataForRegion(string regionCode)
        {
            int countryCallingCode = phoneUtil.GetCountryCodeForRegion(regionCode);
            string mainCountry = phoneUtil.GetRegionCodeForCountryCode(countryCallingCode);
            PhoneMetadata metadata = phoneUtil.GetMetadataForRegion(mainCountry);
            if (metadata != null)
                return metadata;
            // Set to a default instance of the metadata. This allows us to function with an incorrect
            // region code, even if formatting only works for numbers specified with "+".
            return EmptyMetadata;
        }

        // Returns true if a new template is created as opposed to reusing the existing template.
        private bool MaybeCreateNewTemplate()
        {
            // When there are multiple available formats, the formatter uses the first format where a
            // formatting template could be created.
            while (possibleFormats.Count > 0)
            {
                NumberFormat numberFormat = possibleFormats[0];
                string pattern = numberFormat.Pattern;
                if (currentFormattingPattern.Equals(pattern))
                    return false;
                if (CreateFormattingTemplate(numberFormat))
                {
                    currentFormattingPattern = pattern;
                    // With a new formatting template, the matched position using the old template needs to be
                    // reset.
                    lastMatchPosition = 0;
                    return true;
                }
                possibleFormats.RemoveAt(0);
            }
            ableToFormat = false;
            return false;
        }

        private void GetAvailableFormats(string leadingThreeDigits)
        {
            IList<NumberFormat> formatList =
                (isInternationalFormatting && currentMetaData.IntlNumberFormatCount > 0)
                ? currentMetaData.IntlNumberFormatList
                : currentMetaData.NumberFormatList;
            foreach (NumberFormat format in formatList)
            {
                if (IsFormatEligible(format.Format))
                {
                    possibleFormats.Add(format);
                }
            }
            NarrowDownPossibleFormats(leadingThreeDigits);
        }

        private bool IsFormatEligible(string format)
        {
            return EligibleFormatPattern.MatchAll(format).Success;
        }

        private void NarrowDownPossibleFormats(string leadingDigits)
        {
            int indexOfLeadingDigitsPattern = leadingDigits.Length - MinLeadingDigitsLength;
            for (int i = 0; i != possibleFormats.Count; )
            {
                NumberFormat format = possibleFormats[i];
                if (format.LeadingDigitsPatternCount > indexOfLeadingDigitsPattern)
                {
                    var leadingDigitsPattern =
                        regexCache.GetPatternForRegex(
                            format.LeadingDigitsPatternList[indexOfLeadingDigitsPattern]);
                    var m = leadingDigitsPattern.MatchBeginning(leadingDigits);
                    if (!m.Success)
                    {
                        possibleFormats.RemoveAt(i);
                        continue;
                    }
                }
                // else the particular format has no more specific leadingDigitsPattern, and it should be
                // retained.
                ++i;
            }
        }

        private bool CreateFormattingTemplate(NumberFormat format)
        {
            string numberPattern = format.Pattern;

            // The formatter doesn't format numbers when numberPattern contains "|", e.g.
            // (20|3)\d{4}. In those cases we quickly return.
            if (numberPattern.IndexOf('|') != -1)
            {
                return false;
            }

            // Replace anything in the form of [..] with \d
            numberPattern = CharacterClassPattern.Replace(numberPattern, "\\d");

            // Replace any standalone digit (not the one in d{}) with \d
            numberPattern = StandaloneDigitPattern.Replace(numberPattern, "\\d");
            formattingTemplate.Length = 0;
            string tempTemplate = GetFormattingTemplate(numberPattern, format.Format);
            if (tempTemplate.Length > 0)
            {
                formattingTemplate.Append(tempTemplate);
                return true;
            }
            return false;
        }

        // Gets a formatting template which can be used to efficiently format a partial number where
        // digits are added one by one.
        private string GetFormattingTemplate(string numberPattern, string numberFormat)
        {
            // Creates a phone number consisting only of the digit 9 that matches the
            // numberPattern by applying the pattern to the longestPhoneNumber string.
            string longestPhoneNumber = "999999999999999";
            var m = regexCache.GetPatternForRegex(numberPattern).Match(longestPhoneNumber);
            string aPhoneNumber = m.Groups[0].Value;
            // No formatting template can be created if the number of digits entered so far is longer than
            // the maximum the current formatting rule can accommodate.
            if (aPhoneNumber.Length < nationalNumber.Length)
                return "";
            // Formats the number according to numberFormat
            string template = Regex.Replace(aPhoneNumber, numberPattern, numberFormat);
            // Replaces each digit with character digitPlaceholder
            template = template.Replace("9", digitPlaceholder);
            return template;
        }

        /**
         * Clears the internal state of the formatter, so it can be reused.
         */
        public void Clear()
        {
            currentOutput = "";
            accruedInput.Length = 0;
            accruedInputWithoutFormatting.Length = 0;
            formattingTemplate.Length = 0;
            lastMatchPosition = 0;
            currentFormattingPattern = "";
            prefixBeforeNationalNumber.Length = 0;
            nationalPrefixExtracted = "";
            nationalNumber.Length = 0;
            ableToFormat = true;
            inputHasFormatting = false;
            positionToRemember = 0;
            originalPosition = 0;
            isInternationalFormatting = false;
            isExpectingCountryCallingCode = false;
            possibleFormats.Clear();
            if (!currentMetaData.Equals(defaultMetaData))
            {
                currentMetaData = GetMetadataForRegion(defaultCountry);
            }
        }

        /**
         * Formats a phone number on-the-fly as each digit is entered.
         *
         * @param nextChar  the most recently entered digit of a phone number. Formatting characters are
         *     allowed, but as soon as they are encountered this method formats the number as entered and
         *     not "as you type" anymore. Full width digits and Arabic-indic digits are allowed, and will
         *     be shown as they are.
         * @return  the partially formatted phone number.
         */
        public string InputDigit(char nextChar)
        {
            currentOutput = InputDigitWithOptionToRememberPosition(nextChar, false);
            return currentOutput;
        }

        /**
         * Same as {@link #inputDigit}, but remembers the position where {@code nextChar} is inserted, so
         * that it can be retrieved later by using {@link #getRememberedPosition}. The remembered
         * position will be automatically adjusted if additional formatting characters are later
         * inserted/removed in front of {@code nextChar}.
         */
        public string InputDigitAndRememberPosition(char nextChar)
        {
            currentOutput = InputDigitWithOptionToRememberPosition(nextChar, true);
            return currentOutput;
        }

        private string InputDigitWithOptionToRememberPosition(char nextChar, bool rememberPosition)
        {
            accruedInput.Append(nextChar);
            if (rememberPosition)
            {
                originalPosition = accruedInput.Length;
            }
            // We do formatting on-the-fly only when each character entered is either a digit, or a plus
            // sign (accepted at the start of the number only).
            if (!IsDigitOrLeadingPlusSign(nextChar))
            {
                ableToFormat = false;
                inputHasFormatting = true;
            }
            else
            {
                nextChar = NormalizeAndAccrueDigitsAndPlusSign(nextChar, rememberPosition);
            }
            if (!ableToFormat)
            {
                // When we are unable to format because of reasons other than that formatting chars have been
                // entered, it can be due to really long IDDs or NDDs. If that is the case, we might be able
                // to do formatting again after extracting them.
                if (inputHasFormatting)
                {
                    return accruedInput.ToString();
                }
                if (AttemptToExtractIdd())
                {
                    if (AttemptToExtractCountryCallingCode())
                    {
                        return AttemptToChoosePatternWithPrefixExtracted();
                    }
                }
                else if (AbleToExtractLongerNdd())
                {
                    // Add an additional space to separate long NDD and national significant number for
                    // readability.
                    prefixBeforeNationalNumber.Append(" ");
                    return AttemptToChoosePatternWithPrefixExtracted();
                }
                return accruedInput.ToString();
            }

            // We start to attempt to format only when at least MIN_LEADING_DIGITS_LENGTH digits (the plus
            // sign is counted as a digit as well for this purpose) have been entered.
            switch (accruedInputWithoutFormatting.Length)
            {
                case 0:
                case 1:
                case 2:
                    return accruedInput.ToString();
                case 3:
                    if (AttemptToExtractIdd())
                    {
                        isExpectingCountryCallingCode = true;
                    }
                    else
                    {  // No IDD or plus sign is found, might be entering in national format.
                        nationalPrefixExtracted = RemoveNationalPrefixFromNationalNumber();
                        return AttemptToChooseFormattingPattern();
                    }
                    goto default;
                default:
                    if (isExpectingCountryCallingCode)
                    {
                        if (AttemptToExtractCountryCallingCode())
                        {
                            isExpectingCountryCallingCode = false;
                        }
                        return prefixBeforeNationalNumber + nationalNumber.ToString();
                    }
                    if (possibleFormats.Count > 0)
                    {  // The formatting pattern is already chosen.
                        string tempNationalNumber = InputDigitHelper(nextChar);
                        // See if the accrued digits can be formatted properly already. If not, use the results
                        // from inputDigitHelper, which does formatting based on the formatting pattern chosen.
                        string formattedNumber = AttemptToFormatAccruedDigits();
                        if (formattedNumber.Length > 0)
                        {
                            return formattedNumber;
                        }
                        NarrowDownPossibleFormats(nationalNumber.ToString());
                        if (MaybeCreateNewTemplate())
                        {
                            return InputAccruedNationalNumber();
                        }
                        return ableToFormat
                           ? prefixBeforeNationalNumber + tempNationalNumber
                           : accruedInput.ToString();
                    }
                    else
                    {
                        return AttemptToChooseFormattingPattern();
                    }
            }
        }

        private string AttemptToChoosePatternWithPrefixExtracted()
        {
            ableToFormat = true;
            isExpectingCountryCallingCode = false;
            possibleFormats.Clear();
            return AttemptToChooseFormattingPattern();
        }

        // Some national prefixes are a substring of others. If extracting the shorter NDD doesn't result
        // in a number we can format, we try to see if we can extract a longer version here.
        private bool AbleToExtractLongerNdd()
        {
            if (nationalPrefixExtracted.Length > 0)
            {
                // Put the extracted NDD back to the national number before attempting to extract a new NDD.
                nationalNumber.Insert(0, nationalPrefixExtracted);
                // Remove the previously extracted NDD from prefixBeforeNationalNumber. We cannot simply set
                // it to empty string because people sometimes enter national prefix after country code, e.g
                // +44 (0)20-1234-5678.
                int indexOfPreviousNdd = prefixBeforeNationalNumber.ToString().LastIndexOf(nationalPrefixExtracted, StringComparison.Ordinal);
                prefixBeforeNationalNumber.Length = indexOfPreviousNdd;
            }
            return !nationalPrefixExtracted.Equals(RemoveNationalPrefixFromNationalNumber());
        }

        private bool IsDigitOrLeadingPlusSign(char nextChar)
        {
            return char.IsDigit(nextChar) ||
                (accruedInput.Length == 1 &&
                 PhoneNumberUtil.PlusCharsPattern.MatchAll(char.ToString(nextChar)).Success);
        }

        string AttemptToFormatAccruedDigits()
        {
            foreach (NumberFormat numFormat in possibleFormats)
            {
                var m = regexCache.GetPatternForRegex(numFormat.Pattern);
                if (m.MatchAll(nationalNumber.ToString()).Success)
                {
                    string formattedNumber = m.Replace(nationalNumber.ToString(), numFormat.Format);
                    return prefixBeforeNationalNumber + formattedNumber;
                }
            }
            return "";
        }

        /**
         * Returns the current position in the partially formatted phone number of the character which was
         * previously passed in as the parameter of {@link #inputDigitAndRememberPosition}.
         */
        public int GetRememberedPosition()
        {
            if (!ableToFormat)
            {
                return originalPosition;
            }
            int accruedInputIndex = 0, currentOutputIndex = 0;
            while (accruedInputIndex < positionToRemember && currentOutputIndex < currentOutput.Length)
            {
                if (accruedInputWithoutFormatting[accruedInputIndex] ==
                    currentOutput[currentOutputIndex])
                {
                    accruedInputIndex++;
                }
                currentOutputIndex++;
            }
            return currentOutputIndex;
        }

        // Attempts to set the formatting template and returns a string which contains the formatted
        // version of the digits entered so far.
        private string AttemptToChooseFormattingPattern()
        {
            // We start to attempt to format only when as least MIN_LEADING_DIGITS_LENGTH digits of national
            // number (excluding national prefix) have been entered.
            if (nationalNumber.Length >= MinLeadingDigitsLength)
            {
                GetAvailableFormats(nationalNumber.ToString().Substring(0, MinLeadingDigitsLength));
                return MaybeCreateNewTemplate() ? InputAccruedNationalNumber() : accruedInput.ToString();
            }
            return prefixBeforeNationalNumber + nationalNumber.ToString();
        }

        // Invokes inputDigitHelper on each digit of the national number accrued, and returns a formatted
        // string in the end.
        private string InputAccruedNationalNumber()
        {
            int lengthOfNationalNumber = nationalNumber.Length;
            if (lengthOfNationalNumber > 0)
            {
                string tempNationalNumber = "";
                for (int i = 0; i < lengthOfNationalNumber; i++)
                {
                    tempNationalNumber = InputDigitHelper(nationalNumber[i]);
                }
                return ableToFormat
                    ? prefixBeforeNationalNumber + tempNationalNumber
                    : accruedInput.ToString();
            }
            return prefixBeforeNationalNumber.ToString();
        }

        // Returns the national prefix extracted, or an empty string if it is not present.
        private string RemoveNationalPrefixFromNationalNumber()
        {
            int startOfNationalNumber = 0;
            if (currentMetaData.CountryCode == 1 && nationalNumber[0] == '1')
            {
                startOfNationalNumber = 1;
                prefixBeforeNationalNumber.Append("1 ");
                isInternationalFormatting = true;
            }
            else if (currentMetaData.HasNationalPrefixForParsing)
            {
                var m =
                  regexCache.GetPatternForRegex(currentMetaData.NationalPrefixForParsing).MatchBeginning(nationalNumber.ToString());
                if (m.Success)
                {
                    // When the national prefix is detected, we use international formatting rules instead of
                    // national ones, because national formatting rules could contain local formatting rules
                    // for numbers entered without area code.
                    isInternationalFormatting = true;
                    startOfNationalNumber = m.Groups[0].Index + m.Groups[0].Length;
                    prefixBeforeNationalNumber.Append(nationalNumber.ToString().Substring(0, startOfNationalNumber));
                }
            }
            string nationalPrefix = nationalNumber.ToString().Substring(0, startOfNationalNumber);
            nationalNumber.Remove(0, startOfNationalNumber);
            return nationalPrefix;
        }

        /**
         * Extracts IDD and plus sign to prefixBeforeNationalNumber when they are available, and places
         * the remaining input into nationalNumber.
         *
         * @return  true when accruedInputWithoutFormatting begins with the plus sign or valid IDD for
         *     defaultCountry.
         */
        private bool AttemptToExtractIdd()
        {
            var internationalPrefix =
                regexCache.GetPatternForRegex("\\" + PhoneNumberUtil.PlusSign + "|" +
                    currentMetaData.InternationalPrefix);
            var iddMatcher = internationalPrefix.MatchBeginning(accruedInputWithoutFormatting.ToString());
            if (iddMatcher.Success)
            {
                isInternationalFormatting = true;
                int startOfCountryCallingCode = iddMatcher.Groups[0].Index + iddMatcher.Groups[0].Length;
                nationalNumber.Length = 0;
                nationalNumber.Append(accruedInputWithoutFormatting.ToString().Substring(startOfCountryCallingCode));
                prefixBeforeNationalNumber.Length = 0;
                prefixBeforeNationalNumber.Append(
                    accruedInputWithoutFormatting.ToString().Substring(0, startOfCountryCallingCode));
                if (accruedInputWithoutFormatting[0] != PhoneNumberUtil.PlusSign)
                {
                    prefixBeforeNationalNumber.Append(" ");
                }
                return true;
            }
            return false;
        }

        /**
         * Extracts the country calling code from the beginning of nationalNumber to
         * prefixBeforeNationalNumber when they are available, and places the remaining input into
         * nationalNumber.
         *
         * @return  true when a valid country calling code can be found.
         */
        private bool AttemptToExtractCountryCallingCode()
        {
            if (nationalNumber.Length == 0)
            {
                return false;
            }
            StringBuilder numberWithoutCountryCallingCode = new StringBuilder();
            int countryCode = phoneUtil.ExtractCountryCode(nationalNumber, numberWithoutCountryCallingCode);
            if (countryCode == 0)
            {
                return false;
            }
            nationalNumber.Length = 0;
            nationalNumber.Append(numberWithoutCountryCallingCode);
            string newRegionCode = phoneUtil.GetRegionCodeForCountryCode(countryCode);
            if (PhoneNumberUtil.RegionCodeForNonGeoEntity.Equals(newRegionCode))
            {
                currentMetaData = phoneUtil.GetMetadataForNonGeographicalRegion(countryCode);
            }
            else if (!newRegionCode.Equals(defaultCountry))
            {
                currentMetaData = GetMetadataForRegion(newRegionCode);
            }
            string countryCodeString = countryCode.ToString();
            prefixBeforeNationalNumber.Append(countryCodeString).Append(" ");
            return true;
        }

        // Accrues digits and the plus sign to accruedInputWithoutFormatting for later use. If nextChar
        // contains a digit in non-ASCII format (e.g. the full-width version of digits), it is first
        // normalized to the ASCII version. The return value is nextChar itself, or its normalized
        // version, if nextChar is a digit in non-ASCII format. This method assumes its input is either a
        // digit or the plus sign.
        private char NormalizeAndAccrueDigitsAndPlusSign(char nextChar, bool rememberPosition)
        {
            char normalizedChar;
            if (nextChar == PhoneNumberUtil.PlusSign)
            {
                normalizedChar = nextChar;
                accruedInputWithoutFormatting.Append(nextChar);
            }
            else
            {
                normalizedChar = ((int)char.GetNumericValue(nextChar)).ToString()[0];
                accruedInputWithoutFormatting.Append(normalizedChar);
                nationalNumber.Append(normalizedChar);
            }
            if (rememberPosition)
            {
                positionToRemember = accruedInputWithoutFormatting.Length;
            }
            return normalizedChar;
        }

        private string InputDigitHelper(char nextChar)
        {
            var digitMatcher = digitPattern.Match(formattingTemplate.ToString(), lastMatchPosition);
            if (digitMatcher.Success)
            {
                //XXX: double match, can we fix that?
                digitMatcher = digitPattern.Match(formattingTemplate.ToString());
                string tempTemplate = digitPattern.Replace(formattingTemplate.ToString(), nextChar.ToString(), 1);
                formattingTemplate.Length = 0;
                formattingTemplate.Append(tempTemplate);
                lastMatchPosition = digitMatcher.Groups[0].Index;
                return formattingTemplate.ToString().Substring(0, lastMatchPosition + 1);
            }
            if (possibleFormats.Count == 1)
            {
                // More digits are entered than we could handle, and there are no other valid patterns to
                // try.
                ableToFormat = false;
            }  // else, we just reset the formatting pattern.
            currentFormattingPattern = "";
            return accruedInput.ToString();
        }
    }
}