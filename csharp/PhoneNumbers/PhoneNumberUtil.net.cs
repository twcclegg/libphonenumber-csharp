using System;
using System.Collections.Generic;

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER

namespace PhoneNumbers
{
    public partial class PhoneNumberUtil
    {
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
        public static string Normalize(string? number)
        {
            if (number == null)
            {
                return string.Empty;
            }

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
        public static string NormalizeDigitsOnly(string? number)
        {
            if (number == null)
            {
                return string.Empty;
            }

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
        public static string NormalizeDiallableCharsOnly(string? number)
        {
            if (number == null)
            {
                return string.Empty;
            }

            Span<char> result = stackalloc char[number.Length];
            var resultLength = 0;

            NormalizeHelper(ref result, ref resultLength, number, MapDiallableChar, true /* remove non matches */);
            return new string(result.Slice(0, resultLength));
        }

        /// <summary>
        /// Converts all alpha characters in a number to their respective digits on a keypad, but retains
        /// existing formatting.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ConvertAlphaCharactersInNumber(string? number)
        {
            if (number == null)
            {
                return string.Empty;
            }

            Span<char> result = stackalloc char[number.Length];
            var resultLength = 0;

            NormalizeHelper(ref result, ref resultLength, number, MapAlphaPhone, false);
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
            if (number.NationalNumber == 0)
            {
                // Unparseable numbers that kept their raw input just use that.
                // This is the only case where a number can be formatted as E164 without a
                // leading '+' symbol (but the original number wasn't parseable anyway).
                var rawInput = number.RawInput;
                if (rawInput.Length > 0 || !number.HasCountryCode)
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

            Span<char> result = stackalloc char[90];
            var resultLength = 0;
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
                // Before we do a replacement of the national prefix pattern $NP with the national prefix, we
                // need to copy the rule so that subsequent replacements for different numbers have the
                // appropriate national prefix.
                var nationalPrefixFormattingRule = formattingPattern.NationalPrefixFormattingRule;
                if (nationalPrefixFormattingRule.Length > 0)
                {
                    formattingPattern = formattingPattern.Clone();
                    var nationalPrefix = metadata.NationalPrefix;
                    if (nationalPrefix.Length > 0)
                    {
                        // Replace $NP with national prefix and $FG with the first group ($1).
                        nationalPrefixFormattingRule = nationalPrefixFormattingRule.Replace(NpPattern, nationalPrefix);
                        nationalPrefixFormattingRule = nationalPrefixFormattingRule.Replace(FgPattern, "$1");
                        formattingPattern.NationalPrefixFormattingRule = nationalPrefixFormattingRule;
                    }
                    else
                    {
                        // We don't want to have a rule for how to format the national prefix if there isn't one.
                        formattingPattern.NationalPrefixFormattingRule = "";
                    }
                }

                // no point to optimize, it works on regexes
                var formatted = FormatNsnUsingPattern(nationalSignificantNumber, formattingPattern, numberFormat);

                AppendToSpan(ref result, ref resultLength, formatted);
            }

            MaybeAppendFormattedExtension(ref result, ref resultLength, number, metadata, numberFormat);
            return new string(result.Slice(0, resultLength));
        }

        internal static string GetNationalSignificantNumberImpl(PhoneNumber number)
        {
            // If a leading zero(s) has been set, we prefix this now. Note this is not a national prefix.
            Span<char> nationalSignificantNumber = stackalloc char[20];
            SetNationalSignificantNumberToSpan(ref nationalSignificantNumber, number, out var length);
            return new string(nationalSignificantNumber.Slice(0, length));
        }

        /// <summary>
        /// Formats a phone number in national format for dialing using the carrier as specified in the
        /// carrierCode. The carrierCode will always be used regardless of whether the
        /// phone number already has a preferred domestic carrier code stored. If carrierCode
        /// contains an empty string, returns the number in national format without any carrier code.
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="carrierCode">The carrier selection code to be used.</param>
        /// <returns>
        /// The formatted phone number in national format for dialing using the carrier as
        /// specified in the carrierCode.
        /// </returns>
        public string FormatNationalNumberWithCarrierCode(PhoneNumber number, string carrierCode)
        {
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

            Span<char> result = stackalloc char[90];
            var resultLength = 0;

            PrefixNumberWithCountryCallingCode(ref result,
                ref resultLength,
                countryCallingCode,
                PhoneNumberFormat.NATIONAL);

            var metadata = GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
            var nsn = FormatNsn(nationalSignificantNumber,
                metadata,
                PhoneNumberFormat.NATIONAL,
                carrierCode);

            AppendToSpan(ref result, ref resultLength, nsn);
            MaybeAppendFormattedExtension(ref result, ref resultLength, number, metadata, PhoneNumberFormat.NATIONAL);
            return new string(result.Slice(0, resultLength));
        }

        /// <summary>
        /// Formats a phone number for out-of-country dialing purposes.If no regionCallingFrom is
        /// supplied, we format the number in its INTERNATIONAL format.If the country calling code is the
        /// same as that of the region where the number is from, then NATIONAL formatting will be applied.
        /// <para>
        /// If the number itself has a country calling code of zero or an otherwise invalid country
        /// calling code, then we return the number with no formatting applied.
        /// </para>
        /// <para>
        /// Note this function takes care of the case for calling inside of NANPA and between Russia and
        /// Kazakhstan (who share the same country calling code). In those cases, no international prefix
        /// is used.For regions which have multiple international prefixes, the number in its
        /// INTERNATIONAL format will be returned instead.
        /// </para>
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="regionCallingFrom">The region where the call is being placed.</param>
        /// <returns>The formatted phone number.</returns>
        public string FormatOutOfCountryCallingNumber(PhoneNumber number, string regionCallingFrom)
        {
            if (!IsValidRegionCode(regionCallingFrom))
            {
                return Format(number, PhoneNumberFormat.INTERNATIONAL);
            }

            Span<char> nationalSignificantNumberSpan = stackalloc char[90];
            SetNationalSignificantNumberToSpan(ref nationalSignificantNumberSpan,
                number,
                out var nationalSignificantNumberLength);

            var countryCallingCode = number.CountryCode;
            if (!HasValidCountryCallingCode(countryCallingCode))
            {
                return new string(nationalSignificantNumberSpan.Slice(0, nationalSignificantNumberLength));
            }

            if (countryCallingCode == NANPA_COUNTRY_CODE)
            {
                if (IsNANPACountry(regionCallingFrom))
                {
                    // For NANPA regions, return the national format for these regions but prefix it with the
                    // country calling code.
                    Span<char> innerResultSpan = stackalloc char[90];
                    var innerResultLength = 0;

                    AppendNumberToSpan(ref innerResultSpan, ref innerResultLength, countryCallingCode);
                    innerResultSpan[innerResultLength++] = ' ';

                    Format(ref innerResultSpan, ref innerResultLength, number, PhoneNumberFormat.NATIONAL);

                    return new string(innerResultSpan.Slice(0, innerResultLength));
                }
            }
            else if (countryCallingCode == GetCountryCodeForValidRegion(regionCallingFrom))
            {
                // For regions that share a country calling code, the country calling code need not be dialled.
                // This also applies when dialling within a region, so this if clause covers both these cases.
                // Technically this is the case for dialling from La Reunion to other overseas departments of
                // France (French Guiana, Martinique, Guadeloupe), but not vice versa - so we don't cover this
                // edge case for now and for those cases return the version including country calling code.
                // Details here: http://www.petitfute.com/voyage/225-info-pratiques-reunion
                return Format(number, PhoneNumberFormat.NATIONAL);
            }

            var metadataForRegionCallingFrom = GetMetadataForRegion(regionCallingFrom);
            var internationalPrefix = metadataForRegionCallingFrom.InternationalPrefix;

            // In general, if there is a preferred international prefix, use that. Otherwise, for regions
            // that have multiple international prefixes, the international format of the number is
            // returned since we would not know which one to use.
            var internationalPrefixForFormatting = "";
            if (metadataForRegionCallingFrom.HasPreferredInternationalPrefix)
            {
                internationalPrefixForFormatting =
                    metadataForRegionCallingFrom.PreferredInternationalPrefix;
            }
            else if (UniqueInternationalPrefix().IsMatch(internationalPrefix))
            {
                internationalPrefixForFormatting = internationalPrefix;
            }

            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            var metadataForRegion =
                GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);

            var nationalSignificantNumber =
                new string(nationalSignificantNumberSpan.Slice(0, nationalSignificantNumberLength));

            var formattedNationalNumber =
                FormatNsn(nationalSignificantNumber,
                    metadataForRegion,
                    PhoneNumberFormat.INTERNATIONAL);

            Span<char> result = stackalloc char[90];
            var resultLength = 0;

            if (internationalPrefixForFormatting.Length > 0)
            {
                AppendToSpan(ref result, ref resultLength, internationalPrefixForFormatting);
                result[resultLength++] = ' ';
                AppendNumberToSpan(ref result, ref resultLength, countryCallingCode);
                result[resultLength++] = ' ';
            }
            else
            {
                PrefixNumberWithCountryCallingCode(ref result,
                    ref resultLength,
                    countryCallingCode,
                    PhoneNumberFormat.INTERNATIONAL);
            }

            AppendToSpan(ref result, ref resultLength, formattedNationalNumber);
            MaybeAppendFormattedExtension(ref result,
                ref resultLength,
                number,
                metadataForRegion,
                PhoneNumberFormat.INTERNATIONAL);

            return new string(result.Slice(0, resultLength));
        }

        /// <summary>
        /// Formats a phone number for out-of-country dialing purposes.
        /// Note that in this version, if the number was entered originally using alpha characters and
        /// this version of the number is stored in raw_input, this representation of the number will be
        /// used rather than the digit representation. Grouping information, as specified by characters
        /// such as "-" and " ", will be retained.
        /// <p>
        /// <b>Caveats:</b>
        /// </p>
        /// <ul>
        /// <li>
        /// This will not produce good results if the country calling code is both present in the raw
        /// input _and_ is the start of the national number. This is not a problem in the regions
        /// which typically use alpha numbers.
        /// </li>
        /// <li>
        /// This will also not produce good results if the raw input has any grouping information
        /// within the first three digits of the national number, and if the function needs to strip
        /// preceding digits/words in the raw input before these digits. Normally people group the
        /// first three digits together so this is not a huge problem - and will be fixed if it
        /// proves to be so.
        /// </li>
        /// </ul>
        /// </summary>
        /// <param name="number">the phone number that needs to be formatted</param>
        /// <param name="regionCallingFrom">the region where the call is being placed</param>
        /// <returns>the formatted phone number</returns>
        public string FormatOutOfCountryKeepingAlphaChars(PhoneNumber number, string regionCallingFrom)
        {
            var rawInput = number.RawInput;
            // If there is no raw input, then we can't keep alpha characters because there aren't any.
            // In this case, we return formatOutOfCountryCallingNumber.
            if (rawInput.Length == 0)
            {
                return FormatOutOfCountryCallingNumber(number, regionCallingFrom);
            }

            var countryCode = number.CountryCode;
            if (!HasValidCountryCallingCode(countryCode))
            {
                return rawInput;
            }

            // Strip any prefix such as country calling code, IDD, that was present. We do this by comparing
            // the number in raw_input with the parsed number.
            // To do this, first we normalize punctuation. We retain number grouping symbols such as " "
            // only.

            Span<char> intermediateResult = stackalloc char[90];
            var intermediateResultLength = 0;

            NormalizeHelper(ref intermediateResult,
                ref intermediateResultLength,
                rawInput,
                MapAllPlusNumberGroupingSymbols,
                true);
            // Now we trim everything before the first three digits in the parsed number. We choose three
            // because all valid alpha numbers have 3 digits at the start - if it does not, then we don't
            // trim anything at all. Similarly, if the national number was less than three digits, we don't
            // trim anything at all.

            Span<char> nationalNumberSpan = stackalloc char[90];
            SetNationalSignificantNumberToSpan(ref nationalNumberSpan, number, out var nationalNumberLength);
            if (nationalNumberLength > 3)
            {
                var firstNationalNumberDigit = intermediateResult.IndexOf(nationalNumberSpan.Slice(0, 3));
                if (firstNationalNumberDigit != -1)
                {
                    intermediateResultLength -= firstNationalNumberDigit;
                    intermediateResult = intermediateResult.Slice(firstNationalNumberDigit, intermediateResultLength);
                }
            }

            var metadataForRegionCallingFrom = GetMetadataForRegion(regionCallingFrom);
            if (countryCode == NANPA_COUNTRY_CODE)
            {
                if (IsNANPACountry(regionCallingFrom))
                {
                    Span<char> innerResultSpan = stackalloc char[90];
                    var innerResultLength = 0;

                    AppendNumberToSpan(ref innerResultSpan, ref innerResultLength, countryCode);
                    innerResultSpan[innerResultLength++] = ' ';

                    return string.Concat(
                        innerResultSpan.Slice(0, innerResultLength),
                        intermediateResult.Slice(0, intermediateResultLength));
                }
            }
            else if (IsValidRegionCode(regionCallingFrom) &&
                     countryCode == GetCountryCodeForValidRegion(regionCallingFrom))
            {
                var nationalNumber = new string(nationalNumberSpan.Slice(0, nationalNumberLength));
                var resultString = new string(intermediateResult.Slice(0, intermediateResultLength));
                var formattingPattern =
                    ChooseFormattingPatternForNumber(metadataForRegionCallingFrom.numberFormat_,
                        nationalNumber);
                if (formattingPattern == null)
                    // If no pattern above is matched, we format the original input.
                {
                    return resultString;
                }

                var newFormat = formattingPattern.Clone();
                // The first group is the first group of digits that the user wrote together.
                newFormat.Pattern = "(\\d+)(.*)";
                // Here we just concatenate them back together after the national prefix has been fixed.
                newFormat.Format = "$1$2";
                // Now we format using this pattern instead of the default pattern, but with the national
                // prefix prefixed if necessary.
                // This will not work in the cases where the pattern (and not the leading digits) decide
                // whether a national prefix needs to be used, since we have overridden the pattern to match
                // anything, but that is not the case in the metadata to date.
                return FormatNsnUsingPattern(resultString, newFormat, PhoneNumberFormat.NATIONAL);
            }

            var internationalPrefixForFormatting = "";
            // If an unsupported region-calling-from is entered, or a country with multiple international
            // prefixes, the international format of the number is returned, unless there is a preferred
            // international prefix.
            if (metadataForRegionCallingFrom != null)
            {
                var internationalPrefix = metadataForRegionCallingFrom.InternationalPrefix;
                internationalPrefixForFormatting =
                    UniqueInternationalPrefix().IsMatch(internationalPrefix)
                        ? internationalPrefix
                        : metadataForRegionCallingFrom.PreferredInternationalPrefix;
            }

            Span<char> result1 = stackalloc char[90];
            var result1Length = 0;

            if (internationalPrefixForFormatting.Length > 0)
            {
                AppendToSpan(ref result1, ref result1Length, internationalPrefixForFormatting);
                result1[result1Length++] = ' ';
                AppendNumberToSpan(ref result1, ref result1Length, countryCode);
                result1[result1Length++] = ' ';
            }
            else
            {
                // Invalid region entered as country-calling-from (so no metadata was found for it) or the
                // region chosen has multiple international dialling prefixes.
                // LOGGER.log(Level.WARNING,
                // "Trying to format number from invalid region "
                // + regionCallingFrom
                // + ". International formatting applied.");
                PrefixNumberWithCountryCallingCode(ref result1,
                    ref result1Length,
                    countryCode,
                    PhoneNumberFormat.INTERNATIONAL);
            }

            var regionCode = GetRegionCodeForCountryCode(countryCode);
            var metadataForRegion = GetMetadataForRegionOrCallingCode(countryCode, regionCode);
            MaybeAppendFormattedExtension(ref intermediateResult,
                ref intermediateResultLength,
                number,
                metadataForRegion,
                PhoneNumberFormat.INTERNATIONAL);

            return string.Concat(result1.Slice(0, result1Length),
                intermediateResult.Slice(0, intermediateResultLength));
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
                NormalizeHelper(ref span, ref index, number, MapAlphaPhone, true);
            }
            else
            {
                NormalizeDigits(ref span, ref index, number, false);
            }
        }

        private static void NormalizeHelper(ref Span<char> span,
            ref int index,
            string number,
            Func<char, char> normalizationReplacements,
            bool removeNonMatches)
        {
            index = 0;
            for (var i = 0; i < number.Length; i++)
            {
                var character = number[i];
                if (normalizationReplacements(character) is > '\0' and var newDigit)
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
            if (!number.HasExtension)
            {
                return;
            }

            if (numberFormat == PhoneNumberFormat.RFC3966)
            {
                AppendToSpan(ref span, ref index, RFC3966_EXTN_PREFIX);
            }
            else if (metadata.HasPreferredExtnPrefix)
            {
                AppendToSpan(ref span, ref index, metadata.PreferredExtnPrefix);
            }
            else
            {
                AppendToSpan(ref span, ref index, DEFAULT_EXTN_PREFIX);
            }
            AppendToSpan(ref span, ref index, number.Extension);
        }

        private static void SetNationalSignificantNumberToSpan(ref Span<char> nationalNumber,
            PhoneNumber number,
            out int nationalSignificantNumberLength)
        {
            nationalSignificantNumberLength = 0;
            for (var i = 0; i < number.NumberOfLeadingZeros; i++)
                nationalNumber[nationalSignificantNumberLength++] = '0';

            number.NationalNumber.TryFormat(nationalNumber[nationalSignificantNumberLength..], out var charsWritten);
            nationalSignificantNumberLength += charsWritten;
        }

        private static void AppendNumberToSpan(ref Span<char> span, ref int index, int numberToAppend)
        {
            checked((uint)numberToAppend).TryFormat(span[index..], out var charsWritten);
            index += charsWritten;
        }

        private static void AppendToSpan(ref Span<char> destination, ref int index, string valueToAppend)
        {
            valueToAppend.AsSpan().CopyTo(destination[index..]);
            index += valueToAppend.Length;
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
    }
}
#endif
