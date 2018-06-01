/*
 * Copyright (C) 2009 The Libphonenumber Authors
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

/**
 * Library to build phone number metadata from the XML Format.
 *
 * @author Shaopeng Jia
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PhoneNumbers;
using PhoneNumbers.Internal;

namespace PhoneNumbers
{
    public class BuildMetadataFromXml
    {

        // string constants used to fetch the XML nodes and attributes.
        private const string CARRIER_CODE_FORMATTING_RULE = "carrierCodeFormattingRule";
        private const string CARRIER_SPECIFIC = "carrierSpecific";
        private const string COUNTRY_CODE = "countryCode";
        private const string EMERGENCY = "emergency";
        private const string EXAMPLE_NUMBER = "exampleNumber";
        private const string FIXED_LINE = "fixedLine";
        private const string FORMAT = "Format";
        private const string GENERAL_DESC = "generalDesc";
        private const string INTERNATIONAL_PREFIX = "internationalPrefix";
        private const string INTL_FORMAT = "intlFormat";
        private const string LEADING_DIGITS = "leadingDigits";
        private const string MAIN_COUNTRY_FOR_CODE = "mainCountryForCode";
        private const string MOBILE = "mobile";
        private const string MOBILE_NUMBER_PORTABLE_REGION = "mobileNumberPortableRegion";
        private const string NATIONAL_NUMBER_PATTERN = "nationalNumberPattern";
        private const string NATIONAL_PREFIX = "nationalPrefix";
        private const string NATIONAL_PREFIX_FORMATTING_RULE = "nationalPrefixFormattingRule";

        private const string NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING =
            "nationalPrefixOptionalWhenFormatting";

        private const string NATIONAL_PREFIX_FOR_PARSING = "nationalPrefixForParsing";
        private const string NATIONAL_PREFIX_TRANSFORM_RULE = "nationalPrefixTransformRule";
        private const string NO_INTERNATIONAL_DIALLING = "noInternationalDialling";
        private const string NUMBER_FORMAT = "numberFormat";
        private const string PAGER = "pager";
        private const string PATTERN = "pattern";
        private const string PERSONAL_NUMBER = "personalNumber";
        private const string POSSIBLE_LENGTHS = "possibleLengths";
        private const string NATIONAL = "national";
        private const string LOCAL_ONLY = "localOnly";
        private const string PREFERRED_EXTN_PREFIX = "preferredExtnPrefix";
        private const string PREFERRED_INTERNATIONAL_PREFIX = "preferredInternationalPrefix";
        private const string PREMIUM_RATE = "premiumRate";
        private const string SHARED_COST = "sharedCost";
        private const string SHORT_CODE = "shortCode";
        private const string SMS_SERVICES = "smsServices";
        private const string STANDARD_RATE = "standardRate";
        private const string TOLL_FREE = "tollFree";
        private const string UAN = "uan";
        private const string VOICEMAIL = "voicemail";
        private const string VOIP = "voip";

        private static HashSet<string> PHONE_NUMBER_DESCS_WITHOUT_MATCHING_TYPES =
            new HashSet<string> {NO_INTERNATIONAL_DIALLING};

        // Build the PhoneMetadataCollection from the input XML file.
        public static PhoneMetadataCollection buildPhoneMetadataCollection(string inputXmlFile,
            bool liteBuild, bool specialBuild)
        { 
            XDocument document = XDocument.Load(inputXmlFile);
            // TODO: Look for other uses of these constants and possibly pull them out into a separate
            // constants file.
            bool isShortNumberMetadata = inputXmlFile.Contains("ShortNumberMetadata");
            bool isAlternateFormatsMetadata = inputXmlFile.Contains("PhoneNumberAlternateFormats");
            return buildPhoneMetadataCollection(document, liteBuild, specialBuild,
                isShortNumberMetadata, isAlternateFormatsMetadata);
        }


// @VisibleForTesting
        static PhoneMetadataCollection buildPhoneMetadataCollection(XDocument document,
            bool liteBuild, bool specialBuild, bool isShortNumberMetadata,
            bool isAlternateFormatsMetadata)
        {
            //document.getDocumentElement().normalize();
            XElement rootElement = document.Root;
            var territory = rootElement.Elements(XName.Get("territory")).ToList(); //.Elements(XName.Get("territory"));
            PhoneMetadataCollection.Builder metadataCollection = new PhoneMetadataCollection.Builder();
            int numOfTerritories = territory.Count;
            // TODO: Infer filter from a single flag.
            var metadataFilter = GetMetadataFilter(liteBuild, specialBuild);
            for (int i = 0; i < numOfTerritories; i++)
            {
                XElement territoryElement = territory[i];
                // For the main metadata file this should always be set, but for other supplementary data
                // files the country calling code may be all that is needed.
                string regionCode =
                    territoryElement.Attributes(XName.Get("id")).SingleOrDefault()?.Value ?? ""; // todo port good?
                PhoneMetadata.Builder metadata = loadCountryMetadata(regionCode, territoryElement,
                    isShortNumberMetadata, isAlternateFormatsMetadata);
                metadataFilter.FilterMetadata(metadata);
                metadataCollection.AddMetadata(metadata);
            }

            return metadataCollection.Build();
        }

// Build a mapping from a country calling code to the region codes which denote the country/region
// represented by that country code. In the case of multiple countries sharing a calling code,
// such as the NANPA countries, the one indicated with "isMainCountryForCode" in the metadata
// should be first.
        public static Dictionary<int, List<string>> buildCountryCodeToRegionCodeMap(
            PhoneMetadataCollection metadataCollection)
        {
            Dictionary<int, List<string>> countryCodeToRegionCodeMap = new Dictionary<int, List<string>>();
            foreach (PhoneMetadata metadata in metadataCollection.MetadataList)
            {
                string regionCode = metadata.Id;
                int countryCode = metadata.CountryCode;
                if (countryCodeToRegionCodeMap.ContainsKey(countryCode))
                {
                    if (metadata.MainCountryForCode)
                    {
                        countryCodeToRegionCodeMap[countryCode].Insert(0, regionCode);
                    }
                    else
                    {
                        countryCodeToRegionCodeMap[countryCode].Add(regionCode);
                    }
                }
                else
                {
                    // For most countries, there will be only one region code for the country calling code.
                    List<string> listWithRegionCode = new List<string>(1);
                    if (!regionCode.Equals(""))
                    {
                        // For alternate formats, there are no region codes at all.
                        listWithRegionCode.Add(regionCode);
                    }

                    countryCodeToRegionCodeMap.Add(countryCode, listWithRegionCode);
                }
            }

            return countryCodeToRegionCodeMap;
        }

        private static string validateRE(string regex)
        {
            return validateRE(regex, false);
        }

// @VisibleForTesting
        static string validateRE(string regex, bool removeWhitespace)
        {
            // Removes all the whitespace and newline from the regexp. Not using pattern compile options to
            // make it work across programming languages.
            string compressedRegex = removeWhitespace ? regex.Trim() : regex;
            compressedRegex = new Regex(compressedRegex, RegexOptions.Compiled).ToString();
            // We don't ever expect to see | followed by a ) in our metadata - this would be an indication
            // of a bug. If one wants to make something optional, we prefer ? to using an empty group.
            int errorIndex = compressedRegex.IndexOf("|)");
            if (errorIndex >= 0)
            {
                throw new SyntaxErrorException($"| followed by )  {compressedRegex} @ {errorIndex}");
            }

            // return the regex if it is of correct syntax, i.e. compile did not fail with a
            // PatternSyntaxException.
            return compressedRegex;
        }

/**
 * Returns the national prefix of the provided country element.
 */
// @VisibleForTesting
        static string getNationalPrefix(XElement element)
        {
            return element.Attributes(XName.Get(NATIONAL_PREFIX)).SingleOrDefault()?.Value ?? "";

        }

// @VisibleForTesting
        static PhoneMetadata.Builder loadTerritoryTagMetadata(string regionCode, XElement element,
            string nationalPrefix)
        {
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            metadata.SetId(regionCode);
            if (element.Attributes(XName.Get(COUNTRY_CODE)).Any())
            {
                metadata.SetCountryCode(int.Parse(element.Attributes(XName.Get(COUNTRY_CODE)).Single().Value));
            }

            if (element.Attributes(XName.Get(LEADING_DIGITS)).Any())
            {
                metadata.SetLeadingDigits(validateRE(element.Attributes(XName.Get(LEADING_DIGITS)).Single().Value));
            }

            if (element.Attributes(XName.Get(INTERNATIONAL_PREFIX)).Any())
            {
                metadata.SetInternationalPrefix(validateRE(element.Attributes(XName.Get(INTERNATIONAL_PREFIX)).Single()
                    .Value));
            }

            if (element.Attributes(XName.Get(PREFERRED_INTERNATIONAL_PREFIX)).Any())
            {
                metadata.SetPreferredInternationalPrefix(
                    element.Attributes(XName.Get(PREFERRED_INTERNATIONAL_PREFIX)).Single().Value);
            }

            if (element.Attributes(XName.Get(NATIONAL_PREFIX_FOR_PARSING)).Any())
            {
                metadata.SetNationalPrefixForParsing(
                    validateRE(element.Attributes(XName.Get(NATIONAL_PREFIX_FOR_PARSING)).Single().Value, true));
                if (element.Attributes(XName.Get(NATIONAL_PREFIX_TRANSFORM_RULE)).Any())
                {
                    metadata.SetNationalPrefixTransformRule(
                        validateRE(element.Attributes(XName.Get(NATIONAL_PREFIX_TRANSFORM_RULE)).Single().Value));
                }
            }

            if (nationalPrefix.Length != 0)
            {
                metadata.SetNationalPrefix(nationalPrefix);
                if (!metadata.HasNationalPrefixForParsing)
                {
                    metadata.SetNationalPrefixForParsing(nationalPrefix);
                }
            }

            if (element.Attributes(XName.Get(PREFERRED_EXTN_PREFIX)).Any())
            {
                metadata.SetPreferredExtnPrefix(element.Attributes(XName.Get(PREFERRED_EXTN_PREFIX)).Single().Value);
            }

            if (element.Attributes(XName.Get(MAIN_COUNTRY_FOR_CODE)).Any())
            {
                metadata.SetMainCountryForCode(true);
            }

            if (element.Attributes(XName.Get(MOBILE_NUMBER_PORTABLE_REGION)).Any())
            {
                metadata.SetMobileNumberPortableRegion(true);
            }

            return metadata;
        }

/**
 * Extracts the pattern for international Format. If there is no intlFormat, default to using the
 * national Format. If the intlFormat is set to "NA" the intlFormat should be ignored.
 *
 * @throws  Exception if multiple intlFormats have been encountered.
 * @return  whether an international number Format is defined.
 */
// @VisibleForTesting
        static bool loadInternationalFormat(PhoneMetadata.Builder metadata,
            XElement numberFormatElement,
            NumberFormat nationalFormat)
        {
            NumberFormat.Builder intlFormat = new NumberFormat.Builder();
            var intlFormatPattern = numberFormatElement.Elements(XName.Get(INTL_FORMAT)).ToList();
            bool hasExplicitIntlFormatDefined = false;

            if (intlFormatPattern.Count > 1)
            {
                string countryId = metadata.Id.Length > 0
                    ? metadata.Id
                    : metadata.CountryCode.ToString();
                throw new Exception("Invalid number of intlFormat patterns for country: " + countryId);
            }
            else if (intlFormatPattern.Count == 0)
            {
                // Default to use the same as the national pattern if none is defined.
                intlFormat.MergeFrom(nationalFormat);
            }
            else
            {
                intlFormat.SetPattern(numberFormatElement.Attributes(XName.Get(PATTERN)).Single().Value);
                setLeadingDigitsPatterns(numberFormatElement, intlFormat);
                string intlFormatPatternValue = intlFormatPattern[0].FirstAttribute.Value;
                if (!intlFormatPatternValue.Equals("NA"))
                {
                    intlFormat.SetFormat(intlFormatPatternValue);
                }

                hasExplicitIntlFormatDefined = true;
            }

            if (intlFormat.HasFormat)
            {
                metadata.AddIntlNumberFormat(intlFormat);
            }

            return hasExplicitIntlFormatDefined;
        }

/**
 * Extracts the pattern for the national Format.
 *
 * @throws  Exception if multiple or no formats have been encountered.
 */
// @VisibleForTesting
        static void loadNationalFormat(PhoneMetadata.Builder metadata, XElement numberFormatElement,
            NumberFormat.Builder Format)
        {
            setLeadingDigitsPatterns(numberFormatElement, Format);
            Format.SetPattern(validateRE(numberFormatElement.Attributes(XName.Get(PATTERN)).Single().Value));

            var formatPattern = numberFormatElement.Elements(XName.Get(FORMAT)).ToList();
            int numFormatPatterns = formatPattern.Count;
            if (numFormatPatterns != 1)
            {
                string countryId = metadata.Id.Length > 0
                    ? metadata.Id
                    : metadata.CountryCode.ToString();
                throw new Exception("Invalid number of Format patterns (" + numFormatPatterns
                                                                          + ") for country: " + countryId);
            }

            Format.SetFormat(formatPattern[0].FirstAttribute.Value);
        }

/**
 * Extracts the available formats from the provided DOM element. If it does not contain any
 * nationalPrefixFormattingRule, the one passed-in is retained; similarly for
 * nationalPrefixOptionalWhenFormatting. The nationalPrefix, nationalPrefixFormattingRule and
 * nationalPrefixOptionalWhenFormatting values are provided from the parent (territory) element.
 */
// @VisibleForTesting
        static void loadAvailableFormats(PhoneMetadata.Builder metadata,
            XElement element, string nationalPrefix,
            string nationalPrefixFormattingRule,
            bool nationalPrefixOptionalWhenFormatting)
        {
            string carrierCodeFormattingRule = "";
            if (element.Attributes(XName.Get(CARRIER_CODE_FORMATTING_RULE)).Any())
            {
                carrierCodeFormattingRule = validateRE(
                    getDomesticCarrierCodeFormattingRuleFromElement(element, nationalPrefix));
            }

            var numberFormatElements = element.Elements(XName.Get(NUMBER_FORMAT)).ToList();
            bool hasExplicitIntlFormatDefined = false;

            int numOfFormatElements = numberFormatElements.Count;
            if (numOfFormatElements > 0)
            {
                for (int i = 0; i < numOfFormatElements; i++)
                {
                    XElement numberFormatElement = (XElement) numberFormatElements[i];
                    NumberFormat.Builder Format = new NumberFormat.Builder();

                    if (numberFormatElement.Attributes(XName.Get(NATIONAL_PREFIX_FORMATTING_RULE)).Any())
                    {
                        Format.SetNationalPrefixFormattingRule(
                            getNationalPrefixFormattingRuleFromElement(numberFormatElement, nationalPrefix));
                    }
                    else if (!nationalPrefixFormattingRule.Equals(""))
                    {
                        Format.SetNationalPrefixFormattingRule(nationalPrefixFormattingRule);
                    }

                    if (numberFormatElement.Attributes(XName.Get(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING)).Any())
                    {
                        Format.SetNationalPrefixOptionalWhenFormatting(
                            bool.Parse(numberFormatElement.Attributes(XName.Get(
                                NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING)).Single().Value));
                    }
                    else if (Format.NationalPrefixOptionalWhenFormatting
                             != nationalPrefixOptionalWhenFormatting)
                    {
                        // Inherit from the parent field if it is not already the same as the default.
                        Format.SetNationalPrefixOptionalWhenFormatting(nationalPrefixOptionalWhenFormatting);
                    }

                    if (numberFormatElement.Attributes(XName.Get(CARRIER_CODE_FORMATTING_RULE)).Any())
                    {
                        Format.SetDomesticCarrierCodeFormattingRule(validateRE(
                            getDomesticCarrierCodeFormattingRuleFromElement(numberFormatElement,
                                nationalPrefix)));
                    }
                    else if (!carrierCodeFormattingRule.Equals(""))
                    {
                        Format.SetDomesticCarrierCodeFormattingRule(carrierCodeFormattingRule);
                    }

                    loadNationalFormat(metadata, numberFormatElement, Format);
                    metadata.AddNumberFormat(Format);

                    if (loadInternationalFormat(metadata, numberFormatElement, Format.Build()))
                    {
                        hasExplicitIntlFormatDefined = true;
                    }
                }

                // Only a small number of regions need to specify the intlFormats in the xml. For the majority
                // of countries the intlNumberFormat metadata is an exact copy of the national NumberFormat
                // metadata. To minimize the size of the metadata file, we only keep intlNumberFormats that
                // actually differ in some way to the national formats.
                if (!hasExplicitIntlFormatDefined)
                {
                    metadata.ClearIntlNumberFormat();
                }
            }
        }

// @VisibleForTesting
        static void setLeadingDigitsPatterns(XElement numberFormatElement, NumberFormat.Builder Format)
        {
            var leadingDigitsPatternNodes = numberFormatElement.Elements(XName.Get(LEADING_DIGITS)).ToList();
            int numOfLeadingDigitsPatterns = leadingDigitsPatternNodes.Count();
            if (numOfLeadingDigitsPatterns > 0)
            {
                for (int i = 0; i < numOfLeadingDigitsPatterns; i++)
                {
                    Format.AddLeadingDigitsPattern(
                        validateRE(leadingDigitsPatternNodes[i].FirstAttribute.Value, true));
                }
            }
        }

        static string getNationalPrefixFormattingRuleFromElement(XElement element,
            string nationalPrefix)
        {
            string nationalPrefixFormattingRule =
                element.Attributes(XName.Get(NATIONAL_PREFIX_FORMATTING_RULE)).Single().Value;
            // Replace $NP with national prefix and $FG with the first group ($1).
            nationalPrefixFormattingRule =
                nationalPrefixFormattingRule.replaceFirst("\\$NP", nationalPrefix)
                    .replaceFirst("\\$FG", "\\$1");
            return nationalPrefixFormattingRule;
        }

        static string getDomesticCarrierCodeFormattingRuleFromElement(XElement element,
            string nationalPrefix)
        {
            string carrierCodeFormattingRule =
                element.Attributes(XName.Get(CARRIER_CODE_FORMATTING_RULE)).Single().Value;
            // Replace $FG with the first group ($1) and $NP with the national prefix.
            carrierCodeFormattingRule = carrierCodeFormattingRule.replaceFirst("\\$FG", "\\$1")
                .replaceFirst("\\$NP", nationalPrefix);
            return carrierCodeFormattingRule;
        }

/**
 * Checks if the possible lengths provided as a sorted set are equal to the possible lengths
 * stored already in the description pattern. Note that possibleLengths may be empty but must not
 * be null, and the PhoneNumberDesc passed in should also not be null.
 */
        private static bool arePossibleLengthsEqual(HashSet<int> possibleLengths,
            PhoneNumberDesc desc)
        {
            if (possibleLengths.Count != desc.PossibleLengthCount)
            {
                return false;
            }

            // Note that both should be sorted already, and we know they are the same Length.
            int i = 0;
            foreach (int Length in possibleLengths)
            {
                if (Length != desc.GetPossibleLength(i))
                {
                    return false;
                }

                i++;
            }

            return true;
        }

        /**
         * Processes a phone number description element from the XML file and returns it as a
         * PhoneNumberDesc. If the description element is a fixed line or mobile number, the parent
         * description will be used to fill in the whole element if necessary, or any components that are
         * missing. For all other types, the parent description will only be used to fill in missing
         * components if the type has a partial definition. For example, if no "tollFree" element exists,
         * we assume there are no toll free numbers for that locale, and return a phone number description
         * with no national number data and [-1] for the possible lengths. Note that the parent
         * description must therefore already be processed before this method is called on any child
         * elements.
         *
         * @param parentDesc  a generic phone number description that will be used to fill in missing
         *     parts of the description, or null if this is the root node. This must be processed before
         *     this is run on any child elements.
         * @param countryElement  the XML element representing all the country information
         * @param numberType  the name of the number type, corresponding to the appropriate tag in the XML
         *     file with information about that type
         * @return  complete description of that phone number type
         */
        static PhoneNumberDesc.Builder processPhoneNumberDescElement(PhoneNumberDesc parentDesc,
            XElement countryElement,
            string numberType)
        {
            var phoneNumberDescList = countryElement.Elements(XName.Get(numberType)).ToList();
            PhoneNumberDesc.Builder numberDesc = new PhoneNumberDesc.Builder();
            if (!phoneNumberDescList.Any())
            {
                // -1 will never match a possible phone number Length, so is safe to use to ensure this never
                // matches. We don't leave it empty, since for compression reasons, we use the empty list to
                // mean that the generalDesc possible lengths apply.
                numberDesc.AddPossibleLength(-1);
                return numberDesc;
            }

            if (phoneNumberDescList.Count > 1)
            {
                throw new Exception($"Multiple elements with type {numberType} found.");
            }

            XElement element = (XElement) phoneNumberDescList[0];
            if (parentDesc != null)
            {
                // New way of handling possible number lengths. We don't do this for the general
                // description, since these tags won't be present; instead we will calculate its values
                // based on the values for all the other number type descriptions (see
                // setPossibleLengthsGeneralDesc).
                HashSet<int> lengths = new HashSet<int>();
                HashSet<int> localOnlyLengths = new HashSet<int>();
                populatePossibleLengthSets(element, lengths, localOnlyLengths);
                setPossibleLengths(lengths, localOnlyLengths, parentDesc, numberDesc);
            }

            var validPattern = element.Elements(XName.Get(NATIONAL_NUMBER_PATTERN)).ToList();
            if (validPattern.Any())
            {
                numberDesc.SetNationalNumberPattern(
                    validateRE(validPattern[0].FirstAttribute.Value, true));
            }

            var exampleNumber = element.Elements(XName.Get(EXAMPLE_NUMBER)).ToList();
            if (exampleNumber.Any())
            {
                numberDesc.SetExampleNumber(exampleNumber[0].FirstAttribute.Value);
            }

            return numberDesc;
        }

// @VisibleForTesting
        static void setRelevantDescPatterns(PhoneMetadata.Builder metadata, XElement element,
            bool isShortNumberMetadata)
        {
            PhoneNumberDesc.Builder generalDescBuilder = processPhoneNumberDescElement(null, element,
                GENERAL_DESC);
            // Calculate the possible lengths for the general description. This will be based on the
            // possible lengths of the child elements.
            setPossibleLengthsGeneralDesc(
                generalDescBuilder, metadata.Id, element, isShortNumberMetadata);
            metadata.SetGeneralDesc(generalDescBuilder);

            PhoneNumberDesc generalDesc = metadata.GeneralDesc;

            if (!isShortNumberMetadata)
            {
                // HashSet fields used by regular Length phone numbers.
                metadata.SetFixedLine(processPhoneNumberDescElement(generalDesc, element, FIXED_LINE));
                metadata.SetMobile(processPhoneNumberDescElement(generalDesc, element, MOBILE));
                metadata.SetSharedCost(processPhoneNumberDescElement(generalDesc, element, SHARED_COST));
                metadata.SetVoip(processPhoneNumberDescElement(generalDesc, element, VOIP));
                metadata.SetPersonalNumber(processPhoneNumberDescElement(generalDesc, element,
                    PERSONAL_NUMBER));
                metadata.SetPager(processPhoneNumberDescElement(generalDesc, element, PAGER));
                metadata.SetUan(processPhoneNumberDescElement(generalDesc, element, UAN));
                metadata.SetVoicemail(processPhoneNumberDescElement(generalDesc, element, VOICEMAIL));
                metadata.SetNoInternationalDialling(processPhoneNumberDescElement(generalDesc, element,
                    NO_INTERNATIONAL_DIALLING));
                bool mobileAndFixedAreSame = metadata.Mobile.NationalNumberPattern
                    .Equals(metadata.FixedLine.NationalNumberPattern);
                if (metadata.SameMobileAndFixedLinePattern != mobileAndFixedAreSame)
                {
                    // HashSet this if it is not the same as the default.
                    metadata.SetSameMobileAndFixedLinePattern(mobileAndFixedAreSame);
                }

                metadata.SetTollFree(processPhoneNumberDescElement(generalDesc, element, TOLL_FREE));
                metadata.SetPremiumRate(processPhoneNumberDescElement(generalDesc, element, PREMIUM_RATE));
            }
            else
            {
                // HashSet fields used by short numbers.
                metadata.SetStandardRate(processPhoneNumberDescElement(generalDesc, element, STANDARD_RATE));
                metadata.SetShortCode(processPhoneNumberDescElement(generalDesc, element, SHORT_CODE));
                metadata.SetCarrierSpecific(processPhoneNumberDescElement(generalDesc, element,
                    CARRIER_SPECIFIC));
                metadata.SetEmergency(processPhoneNumberDescElement(generalDesc, element, EMERGENCY));
                metadata.SetTollFree(processPhoneNumberDescElement(generalDesc, element, TOLL_FREE));
                metadata.SetPremiumRate(processPhoneNumberDescElement(generalDesc, element, PREMIUM_RATE));
                metadata.SetSmsServices(processPhoneNumberDescElement(generalDesc, element, SMS_SERVICES));
            }
        }

/**
 * Parses a possible Length string into a set of the integers that are covered.
 *
 * @param possibleLengthString  a string specifying the possible lengths of phone numbers. Follows
 *     this syntax: ranges or elements are separated by commas, and ranges are specified in
 *     [min-max] notation, inclusive. For example, [3-5],7,9,[11-14] should be parsed to
 *     3,4,5,7,9,11,12,13,14.
 */
        private static HashSet<int> parsePossibleLengthStringToSet(string possibleLengthString)
        {
            if (possibleLengthString.Length == 0)
            {
                throw new Exception("Empty possibleLength string found.");
            }

            string[] lengths = possibleLengthString.Split(',');
            HashSet<int> lengthSet = new HashSet<int>();
            for (int i = 0; i < lengths.Length; i++)
            {
                string lengthSubstring = lengths[i];
                if (lengthSubstring.Length == 0)
                {
                    throw new Exception(string.Format("Leading, trailing or adjacent commas in possible "
                                                      + "Length string %s, these should only separate numbers or ranges.",
                        possibleLengthString));
                }
                else if (lengthSubstring[0] == '[')
                {
                    if (lengthSubstring[lengthSubstring.Length - 1] != ']')
                    {
                        throw new Exception(string.Format("Missing end of range character in possible "
                                                          + "Length string %s.", possibleLengthString));
                    }

                    // Strip the leading and trailing [], and split on the -.
                    string[] minMax = lengthSubstring.Substring(1, lengthSubstring.Length - 1).Split('-');
                    if (minMax.Length != 2)
                    {
                        throw new Exception(string.Format("Ranges must have exactly one - character in "
                                                          + "missing for %s.", possibleLengthString));
                    }

                    int min = int.Parse(minMax[0]);
                    int max = int.Parse(minMax[1]);
                    // We don't even accept [6-7] since we prefer the shorter 6,7 variant; for a range to be in
                    // use the hyphen needs to replace at least one digit.
                    if (max - min < 2)
                    {
                        throw new Exception(string.Format("The first number in a range should be two or "
                                                          + "more digits lower than the second. Culprit possibleLength string: %s",
                            possibleLengthString));
                    }

                    for (int j = min; j <= max; j++)
                    {
                        if (!lengthSet.Add(j))
                        {
                            throw new Exception(string.Format("Duplicate Length element found (%d) in "
                                                              + "possibleLength string %s", j, possibleLengthString));
                        }
                    }
                }
                else
                {
                    int Length = int.Parse(lengthSubstring);
                    if (!lengthSet.Add(Length))
                    {
                        throw new Exception(string.Format("Duplicate Length element found (%d) in "
                                                          + "possibleLength string %s", Length, possibleLengthString));
                    }
                }
            }

            return lengthSet;
        }

/**
 * Reads the possible lengths present in the metadata and splits them into two sets in one for
 * full-Length numbers, one for local numbers.
 *
 * @param data  one or more phone number descriptions, represented as XML nodes
 * @param lengths  a set to which to add possible lengths of full phone numbers
 * @param localOnlyLengths  a set to which to add possible lengths of phone numbers only diallable
 *     locally (e.g. within a province)
 */
        private static void populatePossibleLengthSets(XElement data, HashSet<int> lengths,
            HashSet<int> localOnlyLengths)
        {
            var possibleLengths = data.Elements(XName.Get(POSSIBLE_LENGTHS)).ToList();
            foreach (var t in possibleLengths)
            {
                XElement element = (XElement) t;
                string nationalLengths =
                    element.Attributes(XName.Get(NATIONAL)).Single().Value; //getAttribute(NATIONAL);
                // We don't add to the phone metadata yet, since we want to sort Length elements found under
                // different nodes first, make sure there are no duplicates between them and that the
                // localOnly lengths don't overlap with the others.
                HashSet<int> thisElementLengths = parsePossibleLengthStringToSet(nationalLengths);
                if (element.Attributes(XName.Get(LOCAL_ONLY)).Any())
                {
                    string localLengths = element.Attributes(XName.Get(LOCAL_ONLY)).Single().Value;
                    HashSet<int> thisElementLocalOnlyLengths = parsePossibleLengthStringToSet(localLengths);
                    HashSet<int> intersection = new HashSet<int>(thisElementLengths);
                    intersection.retainAll(thisElementLocalOnlyLengths);
                    if (intersection.Count != 0)
                    {
                        throw new Exception(string.Format(
                            "Possible Length(s) found specified as a normal and local-only Length in %s",
                            intersection));
                    }

                    // We check again when we set these lengths on the metadata itself in setPossibleLengths
                    // that the elements in localOnly are not also in lengths. For e.g. the generalDesc, it
                    // might have a local-only Length for one type that is a normal Length for another type. We
                    // don't consider this an error, but we do want to remove the local-only lengths.
                    localOnlyLengths.AddAll(thisElementLocalOnlyLengths);
                }

                // It is okay if at this time we have duplicates, because the same Length might be possible
                // for e.g. fixed-line and for mobile numbers, and this method operates potentially on
                // multiple phoneNumberDesc XML elements.
                lengths.AddAll(thisElementLengths);
            }
        }

/**
 * Sets possible lengths in the general description, derived from certain child elements.
 */
// @VisibleForTesting
        static void setPossibleLengthsGeneralDesc(PhoneNumberDesc.Builder generalDesc, string metadataId,
            XElement data, bool isShortNumberMetadata)
        {
            HashSet<int> lengths = new HashSet<int>();
            HashSet<int> localOnlyLengths = new HashSet<int>();
            // The general description node should *always* be present if metadata for other types is
            // present, aside from in some unit tests.
            // (However, for e.g. formatting metadata in PhoneNumberAlternateFormats, no PhoneNumberDesc
            // elements are present).
            var generalDescNodes = data.Elements(XName.Get(GENERAL_DESC)).ToList();
            if (generalDescNodes.Any())
            {
                XElement generalDescNode = (XElement) generalDescNodes[0];
                populatePossibleLengthSets(generalDescNode, lengths, localOnlyLengths);
                if (lengths.Count != 0 || localOnlyLengths.Count != 0)
                {
                    // We shouldn't have anything specified at the "general desc" level: we are going to
                    // calculate this ourselves from child elements.
                    throw new Exception(string.Format("Found possible lengths specified at general "
                                                      + "desc: this should be derived from child elements. Affected country: %s",
                        metadataId));
                }
            }

            if (!isShortNumberMetadata)
            {
                // Make a copy here since we want to remove some nodes, but we don't want to do that on our
                // actual data.
                XElement allDescData = new XElement(data);
                foreach (string tag in PHONE_NUMBER_DESCS_WITHOUT_MATCHING_TYPES)
                {
                    var nodesToRemove = allDescData.Elements(XName.Get(tag)).ToList();
                    // We check when we process phone number descriptions that there are only one of each
                    // type, so this is safe to do.
                    if (nodesToRemove.Any())
                    {
                        nodesToRemove[0].Remove();
                    }
                }

                populatePossibleLengthSets(allDescData, lengths, localOnlyLengths);
            }
            else
            {
                // For short number metadata, we want to copy the lengths from the "short code" section only.
                // This is because it's the more detailed validation pattern, it's not a sub-type of short
                // codes. The other lengths will be checked later to see that they are a sub-set of these
                // possible lengths.
                var shortCodeDescList = data.Elements(XName.Get(SHORT_CODE)).ToList();
                if (shortCodeDescList.Any())
                {
                    XElement shortCodeDesc = (XElement) shortCodeDescList[0];
                    populatePossibleLengthSets(shortCodeDesc, lengths, localOnlyLengths);
                }

                if (localOnlyLengths.Count > 0)
                {
                    throw new Exception("Found local-only lengths in short-number metadata");
                }
            }

            setPossibleLengths(lengths, localOnlyLengths, null, generalDesc);
        }

/**
 * Sets the possible Length fields in the metadata from the sets of data passed in. Checks that
 * the Length is covered by the "parent" phone number description element if one is present, and
 * if the lengths are exactly the same as this, they are not filled in for efficiency reasons.
 *
 * @param parentDesc  the "general description" element or null if desc is the generalDesc itself
 * @param desc  the PhoneNumberDesc object that we are going to set lengths for
 */
        private static void setPossibleLengths(HashSet<int> lengths,
            HashSet<int> localOnlyLengths, PhoneNumberDesc parentDesc, PhoneNumberDesc.Builder desc)
        {
            // We clear these fields since the metadata tends to inherit from the parent element for other
            // fields (via a MergeFrom).
            desc.ClearPossibleLength();
            desc.ClearPossibleLengthLocalOnly();
            // Only add the lengths to this sub-type if they aren't exactly the same as the possible
            // lengths in the general desc (for metadata size reasons).
            if (parentDesc == null || !arePossibleLengthsEqual(lengths, parentDesc))
            {
                foreach (int Length in lengths)
                {
                    if (parentDesc == null || parentDesc.PossibleLengthList.Contains(Length))
                    {
                        desc.AddPossibleLength(Length);
                    }
                    else
                    {
                        // We shouldn't have possible lengths defined in a child element that are not covered by
                        // the general description. We check this here even though the general description is
                        // derived from child elements because it is only derived from a subset, and we need to
                        // ensure *all* child elements have a valid possible Length.
                        throw new Exception(
                            $"Out-of-range possible Length found (${Length}), parent lengths ${parentDesc.PossibleLengthList}.");
                    }
                }
            }

            // We check that the local-only Length isn't also a normal possible Length (only relevant for
            // the general-desc, since within elements such as fixed-line we would throw an exception if we
            // saw this) before adding it to the collection of possible local-only lengths.
            foreach (int Length in localOnlyLengths)
            {
                if (!lengths.Contains(Length))
                {
                    // We check it is covered by either of the possible Length sets of the parent
                    // PhoneNumberDesc, because for example 7 might be a valid localOnly Length for mobile, but
                    // a valid national Length for fixedLine, so the generalDesc would have the 7 removed from
                    // localOnly.
                    if (parentDesc == null || parentDesc.PossibleLengthLocalOnlyList.Contains(Length)
                                           || parentDesc.PossibleLengthList.Contains(Length))
                    {
                        desc.AddPossibleLengthLocalOnly(Length);
                    }
                    else
                    {
                        throw new Exception(
                            $"Out-of-range local-only possible Length found (${Length}), parent Length ${parentDesc.PossibleLengthLocalOnlyList}.");
                    }
                }
            }
        }

// @VisibleForTesting
        static PhoneMetadata.Builder loadCountryMetadata(string regionCode,
            XElement element,
            bool isShortNumberMetadata,
            bool isAlternateFormatsMetadata)
        {
            string nationalPrefix = getNationalPrefix(element);
            PhoneMetadata.Builder metadata = loadTerritoryTagMetadata(regionCode, element, nationalPrefix);
            string nationalPrefixFormattingRule =
                getNationalPrefixFormattingRuleFromElement(element, nationalPrefix);
            loadAvailableFormats(metadata, element, nationalPrefix,
                nationalPrefixFormattingRule,
                element.Attributes(XName.Get(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING)).Any());
            if (!isAlternateFormatsMetadata)
            {
                // The alternate formats metadata does not need most of the patterns to be set.
                setRelevantDescPatterns(metadata, element, isShortNumberMetadata);
            }

            return metadata;
        }

        /**
         * Processes the custom build flags and gets a {@code MetadataFilter} which may be used to
         * filter {@code PhoneMetadata} objects. Incompatible flag combinations throw Exception.
         *
         * @param liteBuild  The liteBuild flag value as given by the command-line
         * @param specialBuild  The specialBuild flag value as given by the command-line
         */
        internal static MetadataFilter GetMetadataFilter(bool liteBuild, bool specialBuild)
        {
            if (specialBuild)
            {
                if (liteBuild)
                {
                    throw new Exception("liteBuild and specialBuild may not both be set");
                }

                return MetadataFilter.ForSpecialBuild();
            }

            if (liteBuild)
            {
                return MetadataFilter.ForLiteBuild();
            }

            return MetadataFilter.EmptyFilter();
        }
    }
}
