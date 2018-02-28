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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PhoneNumbers.Internal;

namespace PhoneNumbers
{

/**
 * Methods for getting information about short phone numbers, such as short codes and emergency
 * numbers. Note that most commercial short numbers are not handled here, but by the
 * {@link PhoneNumberUtil}.
 *
 * @author Shaopeng Jia
 * @author David Yonge-Mallo
 */
    public class ShortNumberInfo
    {
        private static readonly ShortNumberInfo Instance =
        new ShortNumberInfo(RegexBasedMatcher.create());

        // In these countries, if extra digits are added to an emergency number, it no longer connects
        // to the emergency service.
        private static readonly HashSet<string> RegionsWhereEmergencyNumbersMustBeExact =
            new HashSet<string>
            {
                "BR",
                "CL",
                "NI"
            };

/** Cost categories of short numbers. */
        public enum ShortNumberCost
        {
            TOLL_FREE,
            STANDARD_RATE,
            PREMIUM_RATE,
            UNKNOWN_COST
        }

/** Returns the singleton instance of the ShortNumberInfo. */
        public static ShortNumberInfo GetInstance()
        {
            return Instance;
        }

        // IMatcherApi supports the basic matching method for checking if a given national number matches
        // a national number pattern defined in the given {@code PhoneNumberDesc}.
        private readonly IMatcherApi matcherApi;

        // A mapping from a country calling code to the region codes which denote the region represented
        // by that country calling code. In the case of multiple regions sharing a calling code, such as
        // the NANPA regions, the one indicated with "isMainCountryForCode" in the metadata should be
        // first.
        private readonly Dictionary<int, List<string>> countryCallingCodeToRegionCodeMap;

        private ShortNumberInfo(IMatcherApi matcherApi)
        {
            this.matcherApi = matcherApi;
            // TODO: Create ShortNumberInfo for a given map
            countryCallingCodeToRegionCodeMap =
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap();
        }

/**
 * Returns a list with the region codes that match the specific country calling code. For
 * non-geographical country calling codes, the region code 001 is returned. Also, in the case
 * of no region code being found, an empty list is returned.
 */
        private List<string> GetRegionCodesForCountryCode(int countryCallingCode)
        {
            List<string> regionCodes = countryCallingCodeToRegionCodeMap[countryCallingCode];
            return regionCodes ?? new List<string>();
        }

/**
 * Helper method to check that the country calling code of the number matches the region it's
 * being dialed from.
 */
        private bool RegionDialingFromMatchesNumber(PhoneNumber number,
            string regionDialingFrom)
        {
            List<string> regionCodes = GetRegionCodesForCountryCode(number.CountryCode);
            return regionCodes.Contains(regionDialingFrom);
        }

/**
 * Check whether a short number is a possible number when dialed from the given region. This
 * provides a more lenient check than {@link #isValidShortNumberForRegion}.
 *
 * @param number the short number to check
 * @param regionDialingFrom the region from which the number is dialed
 * @return whether the number is a possible short number
 */
        public bool IsPossibleShortNumberForRegion(PhoneNumber number, string regionDialingFrom)
        {
            if (!RegionDialingFromMatchesNumber(number, regionDialingFrom))
            {
                return false;
            }

            PhoneMetadata phoneMetadata =
                MetadataManager.GetShortNumberMetadataForRegion(regionDialingFrom);
            if (phoneMetadata == null)
            {
                return false;
            }

            int numberLength = getNationalSignificantNumber(number).Length;
            return phoneMetadata.GeneralDesc.PossibleLengthList.Contains(numberLength);
        }

        /**
         * Check whether a short number is a possible number. If a country calling code is shared by
         * multiple regions, this returns true if it's possible in any of them. This provides a more
         * lenient check than {@link #isValidShortNumber}. See {@link
         * #isPossibleShortNumberForRegion(PhoneNumber, string)} for details.
         *
         * @param number the short number to check
         * @return whether the number is a possible short number
         */
        public bool IsPossibleShortNumber(PhoneNumber number)
        {
            List<string> regionCodes = GetRegionCodesForCountryCode(number.CountryCode);
            int shortNumberLength = getNationalSignificantNumber(number).Length;
            foreach (var region in regionCodes)
            {
                var phoneMetadata = MetadataManager.GetShortNumberMetadataForRegion(region);
                if (phoneMetadata == null)
                {
                    continue;
                }

                if (phoneMetadata.GeneralDesc.PossibleLengthList.Contains(shortNumberLength))
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * Tests whether a short number matches a valid pattern in a region. Note that this doesn't verify
         * the number is actually in use, which is impossible to tell by just looking at the number
         * itself.
         *
         * @param number the short number for which we want to test the validity
         * @param regionDialingFrom the region from which the number is dialed
         * @return whether the short number matches a valid pattern
         */
        public bool IsValidShortNumberForRegion(PhoneNumber number, string regionDialingFrom)
        {
            if (!RegionDialingFromMatchesNumber(number, regionDialingFrom))
            {
                return false;
            }

            PhoneMetadata phoneMetadata =
                MetadataManager.GetShortNumberMetadataForRegion(regionDialingFrom);
            if (phoneMetadata == null)
            {
                return false;
            }

            string shortNumber = getNationalSignificantNumber(number);
            PhoneNumberDesc generalDesc = phoneMetadata.GeneralDesc;
            if (!MatchesPossibleNumberAndNationalNumber(shortNumber, generalDesc))
            {
                return false;
            }

            PhoneNumberDesc shortNumberDesc = phoneMetadata.ShortCode;
            return MatchesPossibleNumberAndNationalNumber(shortNumber, shortNumberDesc);
        }

        /**
         * Tests whether a short number matches a valid pattern. If a country calling code is shared by
         * multiple regions, this returns true if it's valid in any of them. Note that this doesn't verify
         * the number is actually in use, which is impossible to tell by just looking at the number
         * itself. See {@link #isValidShortNumberForRegion(PhoneNumber, string)} for details.
         *
         * @param number the short number for which we want to test the validity
         * @return whether the short number matches a valid pattern
         */
        public bool IsValidShortNumber(PhoneNumber number)
        {
            List<string> regionCodes = GetRegionCodesForCountryCode(number.CountryCode);
            string regionCode = GetRegionCodeForShortNumberFromRegionList(number, regionCodes);
            if (regionCodes.Count > 1 && regionCode != null)
            {
                // If a matching region had been found for the phone number from among two or more regions,
                // then we have already implicitly verified its validity for that region.
                return true;
            }

            return IsValidShortNumberForRegion(number, regionCode);
        }

        /**
         * Gets the expected cost category of a short number when dialed from a region (however, nothing
         * is implied about its validity). If it is important that the number is valid, then its validity
         * must first be checked using {@link #isValidShortNumberForRegion}. Note that emergency numbers
         * are always considered toll-free. Example usage:
         * <pre>{@code
         * // The region for which the number was parsed and the region we subsequently check against
         * // need not be the same. Here we parse the number in the US and check it for Canada.
         * PhoneNumber number = phoneUtil.parse("110", "US");
         * ...
         * string regionCode = "CA";
         * ShortNumberInfo shortInfo = ShortNumberInfo.getInstance();
         * if (shortInfo.isValidShortNumberForRegion(shortNumber, regionCode)) {
         *   ShortNumberCost cost = shortInfo.getExpectedCostForRegion(number, regionCode);
         *   // Do something with the cost information here.
         * }}</pre>
         *
         * @param number the short number for which we want to know the expected cost category
         * @param regionDialingFrom the region from which the number is dialed
         * @return the expected cost category for that region of the short number. Returns UNKNOWN_COST if
         *     the number does not match a cost category. Note that an invalid number may match any cost
         *     category.
         */
        public ShortNumberCost GetExpectedCostForRegion(PhoneNumber number, string regionDialingFrom)
        {
            if (!RegionDialingFromMatchesNumber(number, regionDialingFrom))
            {
                return ShortNumberCost.UNKNOWN_COST;
            }

            // Note that regionDialingFrom may be null, in which case phoneMetadata will also be null.
            PhoneMetadata phoneMetadata = MetadataManager.GetShortNumberMetadataForRegion(
                regionDialingFrom);
            if (phoneMetadata == null)
            {
                return ShortNumberCost.UNKNOWN_COST;
            }

            string shortNumber = getNationalSignificantNumber(number);

            // The possible lengths are not present for a particular sub-type if they match the general
            // description; for this reason, we check the possible lengths against the general description
            // first to allow an early exit if possible.
            if (!phoneMetadata.GeneralDesc.PossibleLengthList.Contains(shortNumber.Length))
            {
                return ShortNumberCost.UNKNOWN_COST;
            }

            // The cost categories are tested in order of decreasing expense, since if for some reason the
            // patterns overlap the most expensive matching cost category should be returned.
            if (MatchesPossibleNumberAndNationalNumber(shortNumber, phoneMetadata.PremiumRate))
            {
                return ShortNumberCost.PREMIUM_RATE;
            }

            if (MatchesPossibleNumberAndNationalNumber(shortNumber, phoneMetadata.StandardRate))
            {
                return ShortNumberCost.STANDARD_RATE;
            }

            if (MatchesPossibleNumberAndNationalNumber(shortNumber, phoneMetadata.TollFree))
            {
                return ShortNumberCost.TOLL_FREE;
            }

            if (IsEmergencyNumber(shortNumber, regionDialingFrom))
            {
                // Emergency numbers are implicitly toll-free.
                return ShortNumberCost.TOLL_FREE;
            }

            return ShortNumberCost.UNKNOWN_COST;
        }

        /**
         * Gets the expected cost category of a short number (however, nothing is implied about its
         * validity). If the country calling code is unique to a region, this method behaves exactly the
         * same as {@link #getExpectedCostForRegion(PhoneNumber, string)}. However, if the country
         * calling code is shared by multiple regions, then it returns the highest cost in the sequence
         * PREMIUM_RATE, UNKNOWN_COST, STANDARD_RATE, TOLL_FREE. The reason for the position of
         * UNKNOWN_COST in this order is that if a number is UNKNOWN_COST in one region but STANDARD_RATE
         * or TOLL_FREE in another, its expected cost cannot be estimated as one of the latter since it
         * might be a PREMIUM_RATE number.
         * <p>
         * For example, if a number is STANDARD_RATE in the US, but TOLL_FREE in Canada, the expected
         * cost returned by this method will be STANDARD_RATE, since the NANPA countries share the same
         * country calling code.
         * <p>
         * Note: If the region from which the number is dialed is known, it is highly preferable to call
         * {@link #getExpectedCostForRegion(PhoneNumber, string)} instead.
         *
         * @param number the short number for which we want to know the expected cost category
         * @return the highest expected cost category of the short number in the region(s) with the given
         *     country calling code
         */
        public ShortNumberCost GetExpectedCost(PhoneNumber number)
        {
            List<string> regionCodes = GetRegionCodesForCountryCode(number.CountryCode);
            if (regionCodes.Count == 0)
            {
                return ShortNumberCost.UNKNOWN_COST;
            }

            if (regionCodes.Count == 1)
            {
                return GetExpectedCostForRegion(number, regionCodes[0]);
            }

            ShortNumberCost cost = ShortNumberCost.TOLL_FREE;
            foreach (string regionCode in regionCodes)
            {
                ShortNumberCost costForRegion = GetExpectedCostForRegion(number, regionCode);
                switch (costForRegion)
                {
                    case ShortNumberCost.PREMIUM_RATE:
                        return ShortNumberCost.PREMIUM_RATE;
                    case ShortNumberCost.UNKNOWN_COST:
                        cost = ShortNumberCost.UNKNOWN_COST;
                        break;
                    case ShortNumberCost.STANDARD_RATE:
                        if (cost != ShortNumberCost.UNKNOWN_COST)
                        {
                            cost = ShortNumberCost.STANDARD_RATE;
                        }
                        break;
                    case ShortNumberCost.TOLL_FREE:
                        // Do nothing.
                        break;
                }
            }
            return cost;
        }

        // Helper method to get the region code for a given phone number, from a list of possible region
        // codes. If the list Contains more than one region, the first region for which the number is
        // valid is returned.
        private string GetRegionCodeForShortNumberFromRegionList(PhoneNumber number,
            List<string> regionCodes)
        {
            if (!regionCodes.Any())
            {
                return null;
            }
            else if (regionCodes.Count == 1)
            {
                return regionCodes[0];
            }

            string nationalNumber = getNationalSignificantNumber(number);
            foreach (string regionCode in regionCodes)
            {
                PhoneMetadata phoneMetadata = MetadataManager.GetShortNumberMetadataForRegion(regionCode);
                if (phoneMetadata != null
                    && MatchesPossibleNumberAndNationalNumber(nationalNumber, phoneMetadata.ShortCode))
                {
                    // The number is valid for this region.
                    return regionCode;
                }
            }
            return null;
        }

        /**
         * Convenience method to get a list of what regions the library has metadata for.
         */
        HashSet<string> GetSupportedRegions()
        {
            return MetadataManager.GetSupportedShortNumberRegions();
        }

        /**
         * Gets a valid short number for the specified region.
         *
         * @param regionCode the region for which an example short number is needed
         * @return a valid short number for the specified region. Returns an empty string when the
         *     metadata does not contain such information.
         */
        private string GetExampleShortNumber(string regionCode)
        {
            PhoneMetadata phoneMetadata = MetadataManager.GetShortNumberMetadataForRegion(regionCode);
            if (phoneMetadata == null)
            {
                return "";
            }

            PhoneNumberDesc desc = phoneMetadata.ShortCode;
            return desc.ExampleNumber ?? string.Empty;
        }

        /**
         * Gets a valid short number for the specified cost category.
         *
         * @param regionCode the region for which an example short number is needed
         * @param cost the cost category of number that is needed
         * @return a valid short number for the specified region and cost category. Returns an empty
         *     string when the metadata does not contain such information, or the cost is UNKNOWN_COST.
         */
        private string GetExampleShortNumberForCost(string regionCode, ShortNumberCost cost)
        {
            PhoneMetadata phoneMetadata = MetadataManager.GetShortNumberMetadataForRegion(regionCode);
            if (phoneMetadata == null)
            {
                return "";
            }

            PhoneNumberDesc desc = null;
            switch (cost)
            {
                case ShortNumberCost.TOLL_FREE:
                    desc = phoneMetadata.TollFree;
                    break;
                case ShortNumberCost.STANDARD_RATE:
                    desc = phoneMetadata.StandardRate;
                    break;
                case ShortNumberCost.PREMIUM_RATE:
                    desc = phoneMetadata.PremiumRate;
                    break;
                // UNKNOWN_COST numbers are computed by the process of elimination from the other cost
                // categories.
            }
            return desc?.ExampleNumber ?? string.Empty;
        }

        /**
         * Returns true if the given number, exactly as dialed, might be used to connect to an emergency
         * service in the given region.
         * <p>
         * This method accepts a string, rather than a PhoneNumber, because it needs to distinguish
         * cases such as "+1 911" and "911", where the former may not connect to an emergency service in
         * all cases but the latter would. This method takes into account cases where the number might
         * contain formatting, or might have additional digits appended (when it is okay to do that in
         * the specified region).
         *
         * @param number the phone number to test
         * @param regionCode the region where the phone number is being dialed
         * @return whether the number might be used to connect to an emergency service in the given region
         */
        public bool ConnectsToEmergencyNumber(string number, string regionCode)
        {
            return MatchesEmergencyNumberHelper(number, regionCode, true /* allows prefix match */);
        }

        /**
         * Returns true if the given number exactly matches an emergency service number in the given
         * region.
         * <p>
         * This method takes into account cases where the number might contain formatting, but doesn't
         * allow additional digits to be appended. Note that {@code isEmergencyNumber(number, region)}
         * implies {@code connectsToEmergencyNumber(number, region)}.
         *
         * @param number the phone number to test
         * @param regionCode the region where the phone number is being dialed
         * @return whether the number exactly matches an emergency services number in the given region
         */
        public bool IsEmergencyNumber(string number, string regionCode)
        {
            return MatchesEmergencyNumberHelper(number, regionCode, false /* doesn't allow prefix match */);
        }

        private bool MatchesEmergencyNumberHelper(string number, string regionCode,
            bool allowPrefixMatch)
        {
            string possibleNumber = PhoneNumberUtil.ExtractPossibleNumber(number);
            if (PhoneNumberUtil.PlusCharsPattern.matcher(possibleNumber).lookingAt())
            {
                // Returns false if the number starts with a plus sign. We don't believe dialing the country
                // code before emergency numbers (e.g. +1911) works, but later, if that proves to work, we can
                // add additional logic here to handle it.
                return false;
            }

            PhoneMetadata metadata = MetadataManager.GetShortNumberMetadataForRegion(regionCode);
            if (metadata == null || !metadata.HasEmergency)
            {
                return false;
            }

            string normalizedNumber = PhoneNumberUtil.NormalizeDigitsOnly(possibleNumber);
            bool allowPrefixMatchForRegion =
                allowPrefixMatch && !RegionsWhereEmergencyNumbersMustBeExact.Contains(regionCode);
            return matcherApi.matchNationalNumber(normalizedNumber, metadata.Emergency,
                allowPrefixMatchForRegion);
        }

/**
 * Given a valid short number, determines whether it is carrier-specific (however, nothing is
 * implied about its validity). Carrier-specific numbers may connect to a different end-point, or
 * not connect at all, depending on the user's carrier. If it is important that the number is
 * valid, then its validity must first be checked using {@link #isValidShortNumber} or
 * {@link #isValidShortNumberForRegion}.
 *
 * @param number  the valid short number to check
 * @return whether the short number is carrier-specific, assuming the input was a valid short
 *     number
 */
        public bool IsCarrierSpecific(PhoneNumber number)
        {
            List<string> regionCodes = GetRegionCodesForCountryCode(number.CountryCode);
            string regionCode = GetRegionCodeForShortNumberFromRegionList(number, regionCodes);
            string nationalNumber = getNationalSignificantNumber(number);
            PhoneMetadata phoneMetadata = MetadataManager.GetShortNumberMetadataForRegion(regionCode);
            return phoneMetadata != null
                   && MatchesPossibleNumberAndNationalNumber(nationalNumber,
                       phoneMetadata.CarrierSpecific);
        }

        /**
         * Given a valid short number, determines whether it is carrier-specific when dialed from the
         * given region (however, nothing is implied about its validity). Carrier-specific numbers may
         * connect to a different end-point, or not connect at all, depending on the user's carrier. If
         * it is important that the number is valid, then its validity must first be checked using
         * {@link #isValidShortNumber} or {@link #isValidShortNumberForRegion}. Returns false if the
         * number doesn't match the region provided.
         *
         * @param number  the valid short number to check
         * @param regionDialingFrom  the region from which the number is dialed
         * @return  whether the short number is carrier-specific in the provided region, assuming the
         *     input was a valid short number
         */
        public bool IsCarrierSpecificForRegion(PhoneNumber number, string regionDialingFrom)
        {
            if (!RegionDialingFromMatchesNumber(number, regionDialingFrom))
            {
                return false;
            }

            string nationalNumber = getNationalSignificantNumber(number);
            PhoneMetadata phoneMetadata =
                MetadataManager.GetShortNumberMetadataForRegion(regionDialingFrom);
            return phoneMetadata != null
                   && MatchesPossibleNumberAndNationalNumber(nationalNumber,
                       phoneMetadata.CarrierSpecific);
        }

        /**
         * Given a valid short number, determines whether it is an SMS service (however, nothing is
         * implied about its validity). An SMS service is where the primary or only intended usage is to
         * receive and/or send text messages (SMSs). This includes MMS as MMS numbers downgrade to SMS if
         * the other party isn't MMS-capable. If it is important that the number is valid, then its
         * validity must first be checked using {@link #isValidShortNumber} or {@link
         * #isValidShortNumberForRegion}. Returns false if the number doesn't match the region provided.
         *
         * @param number  the valid short number to check
         * @param regionDialingFrom  the region from which the number is dialed
         * @return  whether the short number is an SMS service in the provided region, assuming the input
         *     was a valid short number
         */
        public bool IsSmsServiceForRegion(PhoneNumber number, string regionDialingFrom)
        {
            if (!RegionDialingFromMatchesNumber(number, regionDialingFrom))
            {
                return false;
            }

            PhoneMetadata phoneMetadata =
                MetadataManager.GetShortNumberMetadataForRegion(regionDialingFrom);
            return phoneMetadata != null
                   && MatchesPossibleNumberAndNationalNumber(getNationalSignificantNumber(number),
                       phoneMetadata.SmsServices);
        }

        /**
         * Gets the national significant number of the a phone number. Note a national significant number
         * doesn't contain a national prefix or any formatting.
         * <p>
         * This is a temporary duplicate of the {@code getNationalSignificantNumber} method from
         * {@code PhoneNumberUtil}. Ultimately a canonical static version should exist in a separate
         * utility class (to prevent {@code ShortNumberInfo} needing to depend on PhoneNumberUtil).
         *
         * @param number  the phone number for which the national significant number is needed
         * @return  the national significant number of the PhoneNumber object passed in
         */
        private static string getNationalSignificantNumber(PhoneNumber number)
        {
            // If leading zero(s) have been set, we prefix this now. Note this is not a national prefix.
            StringBuilder nationalNumber = new StringBuilder();
            for (var i = 0; i < number.NumberOfLeadingZeros; ++i)
                nationalNumber.Append('0');

            nationalNumber.Append(number.NationalNumber);
            return nationalNumber.ToString();
        }

        // TODO: Once we have benchmarked ShortNumberInfo, consider if it is worth keeping
        // this performance optimization.
        private bool MatchesPossibleNumberAndNationalNumber(string number,
            PhoneNumberDesc numberDesc)
        {
            if (numberDesc.PossibleLengthCount > 0
                && !numberDesc.PossibleLengthList.Contains(number.Length))
            {
                return false;
            }

            return matcherApi.matchNationalNumber(number, numberDesc, false);
        }
    }
}
