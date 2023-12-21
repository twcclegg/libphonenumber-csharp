using System;
using System.Collections.Generic;
using System.Text;

#if !(NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)

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
            NormalizeHelper(number, MapDiallableChar, true /* remove non matches */);

        /// <summary>
        /// Converts all alpha characters in a number to their respective digits on a keypad, but retains
        /// existing formatting.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ConvertAlphaCharactersInNumber(string number) =>
            NormalizeHelper(number, MapAlphaPhone, false);

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
            // Unparseable numbers that kept their raw input just use that.
            // This is the only case where a number can be formatted as E164 without a
            // leading '+' symbol (but the original number wasn't parseable anyway).
            if (number.NationalNumber == 0)
            {
                var rawInput = number.RawInput;
                if (rawInput.Length > 0 || !number.HasCountryCode)
                {
                    return rawInput;
                }
            }

            var sb = new StringBuilder(20);
            Format(number, numberFormat, sb);
            return sb.ToString();
        }

        internal static string GetNationalSignificantNumberImpl(PhoneNumber number)
        {
            // If a leading zero(s) has been set, we prefix this now. Note this is not a national prefix.
            if (!number.HasNumberOfLeadingZeros)
                return number.NationalNumber.ToString();

            var nationalNumber = new StringBuilder();
            nationalNumber.Append('0', number.NumberOfLeadingZeros);
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
            PrefixNumberWithCountryCallingCode(countryCallingCode, numberFormat, formattedNumber);
            var formattingPattern =
                ChooseFormattingPatternForNumber(userDefinedFormats, nationalSignificantNumber);
            if (formattingPattern == null)
            {
                // If no pattern above is matched, we format the number as a whole.
                formattedNumber.Append(nationalSignificantNumber);
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

                formattedNumber.Append(
                    FormatNsnUsingPattern(nationalSignificantNumber, formattingPattern, numberFormat));
            }

            MaybeAppendFormattedExtension(number, metadata, numberFormat, formattedNumber);
            return formattedNumber.ToString();
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
            var nationalSignificantNumber = GetNationalSignificantNumber(number);
            // Note getRegionCodeForCountryCode() is used because formatting information for regions which
            // share a country calling code is contained by only one region for performance reasons. For
            // example, for NANPA regions it will be contained in the metadata for US.
            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            if (!HasValidCountryCallingCode(countryCallingCode))
            {
                return nationalSignificantNumber;
            }

            var formattedNumber = new StringBuilder(20);
            var metadata = GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
            formattedNumber.Append(FormatNsn(nationalSignificantNumber,
                metadata,
                PhoneNumberFormat.NATIONAL,
                carrierCode));
            MaybeAppendFormattedExtension(number, metadata, PhoneNumberFormat.NATIONAL, formattedNumber);
            return formattedNumber.ToString();
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
                // LOGGER.log(Level.WARNING,
                //      "Trying to format number from invalid region "
                //      + regionCallingFrom
                //      + ". International formatting applied.");
                return Format(number, PhoneNumberFormat.INTERNATIONAL);
            }

            var countryCallingCode = number.CountryCode;
            var nationalSignificantNumber = GetNationalSignificantNumber(number);
            if (!HasValidCountryCallingCode(countryCallingCode))
            {
                return nationalSignificantNumber;
            }

            if (countryCallingCode == NANPA_COUNTRY_CODE)
            {
                if (IsNANPACountry(regionCallingFrom))
                {
                    // For NANPA regions, return the national format for these regions but prefix it with the
                    // country calling code.
                    return countryCallingCode + " " + Format(number, PhoneNumberFormat.NATIONAL);
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
            var formattedNationalNumber =
                FormatNsn(nationalSignificantNumber,
                    metadataForRegion,
                    PhoneNumberFormat.INTERNATIONAL);
            var formattedNumber = new StringBuilder();
            if (internationalPrefixForFormatting.Length > 0)
            {
                formattedNumber.Append(internationalPrefixForFormatting).Append(' ').Append(countryCallingCode).Append(' ');
            }
            else
            {
                formattedNumber.Append(PLUS_SIGN).Append(countryCallingCode).Append(' ');
            }

            formattedNumber.Append(formattedNationalNumber);
            MaybeAppendFormattedExtension(number, metadataForRegion, PhoneNumberFormat.INTERNATIONAL, formattedNumber);
            return formattedNumber.ToString();
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
            rawInput = NormalizeHelper(rawInput, MapAllPlusNumberGroupingSymbols, true);
            // Now we trim everything before the first three digits in the parsed number. We choose three
            // because all valid alpha numbers have 3 digits at the start - if it does not, then we don't
            // trim anything at all. Similarly, if the national number was less than three digits, we don't
            // trim anything at all.
            var nationalNumber = GetNationalSignificantNumber(number);
            if (nationalNumber.Length > 3)
            {
                var firstNationalNumberDigit =
                    rawInput.IndexOf(nationalNumber.Substring(0, 3), StringComparison.Ordinal);
                if (firstNationalNumberDigit != -1)
                {
                    rawInput = rawInput.Substring(firstNationalNumberDigit);
                }
            }

            var metadataForRegionCallingFrom = GetMetadataForRegion(regionCallingFrom);
            if (countryCode == NANPA_COUNTRY_CODE)
            {
                if (IsNANPACountry(regionCallingFrom))
                {
                    return countryCode + " " + rawInput;
                }
            }
            else if (IsValidRegionCode(regionCallingFrom) &&
                     countryCode == GetCountryCodeForValidRegion(regionCallingFrom))
            {
                var formattingPattern =
                    ChooseFormattingPatternForNumber(metadataForRegionCallingFrom.numberFormat_,
                        nationalNumber);
                if (formattingPattern == null)
                    // If no pattern above is matched, we format the original input.
                {
                    return rawInput;
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
                return FormatNsnUsingPattern(rawInput, newFormat, PhoneNumberFormat.NATIONAL);
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

            var formattedNumber = new StringBuilder();
            if (internationalPrefixForFormatting.Length > 0)
            {
                formattedNumber.Append(internationalPrefixForFormatting).Append(' ').Append(countryCode).Append(' ');
            }
            else
            {
                // Invalid region entered as country-calling-from (so no metadata was found for it) or the
                // region chosen has multiple international dialling prefixes.
                formattedNumber.Append(PLUS_SIGN).Append(countryCode).Append(' ');
            }

            formattedNumber.Append(rawInput);
            var regionCode = GetRegionCodeForCountryCode(countryCode);
            var metadataForRegion = GetMetadataForRegionOrCallingCode(countryCode, regionCode);
            MaybeAppendFormattedExtension(number, metadataForRegion, PhoneNumberFormat.INTERNATIONAL, formattedNumber);
            return formattedNumber.ToString();
        }
    }
}
#endif
