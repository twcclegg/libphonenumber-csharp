#nullable disable
/*
 * Copyright (C) 2013 The Libphonenumber Authors
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
using System.Reflection;

namespace PhoneNumbers
{
    /// <summary>
    /// A phone prefix mapper which provides carrier information related to a phone number.
    /// <para>
    /// Carrier data is the one the number was originally allocated to. If the country supports mobile
    /// number portability the number might not belong to the returned carrier anymore.
    /// </para>
    /// </summary>
    public class PhoneNumberToCarrierMapper
    {
        // Corresponds to resources/carrier/ embedded with LinkBase="carrier".
        private const string MAPPING_DATA_DIRECTORY = "carrier.";

        private static readonly Lazy<PhoneNumberToCarrierMapper> s_instance =
            new Lazy<PhoneNumberToCarrierMapper>(() => new PhoneNumberToCarrierMapper(MAPPING_DATA_DIRECTORY));

        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
        private readonly PrefixFileReader prefixFileReader;

        // @VisibleForTesting
        internal PhoneNumberToCarrierMapper(string phonePrefixDataDirectory, Assembly asm = null)
        {
            prefixFileReader = new PrefixFileReader(phonePrefixDataDirectory, asm);
        }

        /// <summary>
        /// Gets the singleton <see cref="PhoneNumberToCarrierMapper"/> instance.
        /// </summary>
        public static PhoneNumberToCarrierMapper GetInstance() => s_instance.Value;

        /// <summary>
        /// Returns a carrier name for the given phone number, in the language provided.
        /// <para>
        /// The carrier name is the one the number was originally allocated to. If the country supports
        /// mobile number portability the number might not belong to the returned carrier anymore.
        /// If no mapping is found an empty string is returned.
        /// </para>
        /// <para>
        /// This method assumes the validity of the number passed in has already been checked, and that
        /// the number is suitable for carrier lookup. We consider mobile and pager numbers possible
        /// candidates for carrier lookup.
        /// </para>
        /// </summary>
        /// <param name="number">A valid phone number for which we want to get a carrier name.</param>
        /// <param name="languageCode">The language code in which the name should be written.</param>
        /// <returns>A carrier name for the given phone number, or empty string if not found.</returns>
        public string GetNameForValidNumber(PhoneNumber number, Locale languageCode)
        {
            // No script is specified — Java's Locale.getScript() has no equivalent on this
            // port's Locale type. Matches the same omission in PhoneNumberOfflineGeocoder.
            return prefixFileReader.GetDescriptionForNumber(
                number, languageCode.Language, "", languageCode.Country);
        }

        /// <summary>
        /// Gets the name of the carrier for the given phone number, in the language provided.
        /// As per <see cref="GetNameForValidNumber"/> but explicitly checks the validity of
        /// the number passed in, and returns empty string if the number is not a mobile or pager number.
        /// </summary>
        /// <param name="number">The phone number for which we want to get a carrier name.</param>
        /// <param name="languageCode">The language code in which the name should be written.</param>
        /// <returns>A carrier name for the given phone number, or empty string if the number passed
        /// in is invalid or is not a mobile/pager number.</returns>
        public string GetNameForNumber(PhoneNumber number, Locale languageCode)
        {
            if (IsMobile(phoneUtil.GetNumberType(number)))
                return GetNameForValidNumber(number, languageCode);
            return "";
        }

        /// <summary>
        /// Gets the name of the carrier for the given phone number only when it is 'safe' to display
        /// to users. A carrier name is considered safe if the number is valid and for a region that
        /// doesn't support mobile number portability.
        /// </summary>
        /// <param name="number">The phone number for which we want to get a carrier name.</param>
        /// <param name="languageCode">The language code in which the name should be written.</param>
        /// <returns>A carrier name that is safe to display to users, or the empty string.</returns>
        public string GetSafeDisplayName(PhoneNumber number, Locale languageCode)
        {
            if (phoneUtil.IsMobileNumberPortableRegion(phoneUtil.GetRegionCodeForNumber(number)))
                return "";
            return GetNameForNumber(number, languageCode);
        }

        private static bool IsMobile(PhoneNumberType numberType) =>
            numberType == PhoneNumberType.MOBILE
            || numberType == PhoneNumberType.FIXED_LINE_OR_MOBILE
            || numberType == PhoneNumberType.PAGER;
    }
}
