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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace PhoneNumbers
{
    class BuildMetadataFromXml
    {
        private static bool liteBuild;

        // Build the PhoneMetadataCollection from the input XML file.
        public static PhoneMetadataCollection BuildPhoneMetadataCollection(Stream input, bool liteBuild)
        {
            BuildMetadataFromXml.liteBuild = liteBuild;
            var document = new XmlDocument();
            document.Load(input);
            document.Normalize();
            var metadataCollection = new PhoneMetadataCollection.Builder();
            foreach (XmlElement territory in document.GetElementsByTagName("territory"))
            {
                var regionCode = territory.GetAttribute("id");
                PhoneMetadata metadata = LoadCountryMetadata(regionCode, territory);
                metadataCollection.AddMetadata(metadata);
            }
            return metadataCollection.Build();
        }

        // Build a mapping from a country calling code to the region codes which denote the country/region
        // represented by that country code. In the case of multiple countries sharing a calling code,
        // such as the NANPA countries, the one indicated with "isMainCountryForCode" in the metadata
        // should be first.
        public static Dictionary<int, List<String>> BuildCountryCodeToRegionCodeMap(
            PhoneMetadataCollection metadataCollection)
        {
            Dictionary<int, List<String>> countryCodeToRegionCodeMap =
                new Dictionary<int, List<String>>();
            foreach (PhoneMetadata metadata in metadataCollection.MetadataList)
            {
                String regionCode = metadata.Id;
                int countryCode = metadata.CountryCode;
                if (countryCodeToRegionCodeMap.ContainsKey(countryCode))
                {
                    if (metadata.MainCountryForCode)
                        countryCodeToRegionCodeMap[countryCode].Insert(0, regionCode);
                    else
                        countryCodeToRegionCodeMap[countryCode].Add(regionCode);
                }
                else
                {
                    // For most countries, there will be only one region code for the country calling code.
                    List<String> listWithRegionCode = new List<String>(1);
                    listWithRegionCode.Add(regionCode);
                    countryCodeToRegionCodeMap[countryCode] = listWithRegionCode;
                }
            }
            return countryCodeToRegionCodeMap;
        }

        private static String ValidateRE(String regex)
        {
            return ValidateRE(regex, false);
        }

        private static String ValidateRE(String regex, bool removeWhitespace)
        {
            // Removes all the whitespace and newline from the regexp. Not using pattern compile options to
            // make it work across programming languages.
            if (removeWhitespace)
                regex = Regex.Replace(regex, "\\s", "");
            new Regex(regex, RegexOptions.Compiled);
            // return regex itself if it is of correct regex syntax
            // i.e. compile did not fail with a PatternSyntaxException.
            return regex;
        }

        private static PhoneMetadata LoadCountryMetadata(String regionCode, XmlElement element)
        {
            var metadata = new PhoneMetadata.Builder();
            metadata.SetId(regionCode);
            metadata.SetCountryCode(int.Parse(element.GetAttribute("countryCode")));
            if (element.HasAttribute("leadingDigits"))
                metadata.SetLeadingDigits(ValidateRE(element.GetAttribute("leadingDigits")));
            metadata.SetInternationalPrefix(ValidateRE(element.GetAttribute("internationalPrefix")));
            if (element.HasAttribute("preferredInternationalPrefix"))
            {
                String preferredInternationalPrefix = element.GetAttribute("preferredInternationalPrefix");
                metadata.SetPreferredInternationalPrefix(preferredInternationalPrefix);
            }
            if (element.HasAttribute("nationalPrefixForParsing"))
            {
                metadata.SetNationalPrefixForParsing(
                    ValidateRE(element.GetAttribute("nationalPrefixForParsing")));
                if (element.HasAttribute("nationalPrefixTransformRule"))
                {
                    metadata.SetNationalPrefixTransformRule(
                    ValidateRE(element.GetAttribute("nationalPrefixTransformRule")));
                }
            }
            String nationalPrefix = "";
            String nationalPrefixFormattingRule = "";
            if (element.HasAttribute("nationalPrefix"))
            {
                nationalPrefix = element.GetAttribute("nationalPrefix");
                metadata.SetNationalPrefix(nationalPrefix);
                nationalPrefixFormattingRule =
                    GetNationalPrefixFormattingRuleFromElement(element, nationalPrefix);

                if (!metadata.HasNationalPrefixForParsing)
                    metadata.SetNationalPrefixForParsing(nationalPrefix);
            }
            String carrierCodeFormattingRule = "";
            if (element.HasAttribute("carrierCodeFormattingRule"))
            {
                carrierCodeFormattingRule = ValidateRE(
                    GetDomesticCarrierCodeFormattingRuleFromElement(element, nationalPrefix));
            }
            if (element.HasAttribute("preferredExtnPrefix"))
                metadata.SetPreferredExtnPrefix(element.GetAttribute("preferredExtnPrefix"));
            if (element.HasAttribute("mainCountryForCode"))
                metadata.SetMainCountryForCode(true);
            if (element.HasAttribute("leadingZeroPossible"))
                metadata.SetLeadingZeroPossible(true);

            // Extract availableFormats
            var numberFormatElements = element.GetElementsByTagName("numberFormat");
            bool hasExplicitIntlFormatDefined = false;

            int numOfFormatElements = numberFormatElements.Count;
            if (numOfFormatElements > 0)
            {
                foreach (XmlElement numberFormatElement in numberFormatElements)
                {
                    var format = new NumberFormat.Builder();

                    if (numberFormatElement.HasAttribute("nationalPrefixFormattingRule"))
                    {
                        format.SetNationalPrefixFormattingRule(
                            GetNationalPrefixFormattingRuleFromElement(numberFormatElement, nationalPrefix));
                    }
                    else
                    {
                        format.SetNationalPrefixFormattingRule(nationalPrefixFormattingRule);
                    }
                    if (numberFormatElement.HasAttribute("carrierCodeFormattingRule"))
                    {
                        format.SetDomesticCarrierCodeFormattingRule(ValidateRE(
                            GetDomesticCarrierCodeFormattingRuleFromElement(
                                numberFormatElement, nationalPrefix)));
                    }
                    else
                    {
                        format.SetDomesticCarrierCodeFormattingRule(carrierCodeFormattingRule);
                    }

                    // Extract the pattern for the national format.
                    setLeadingDigitsPatterns(numberFormatElement, format);
                    format.SetPattern(ValidateRE(numberFormatElement.GetAttribute("pattern")));

                    var formatPattern = numberFormatElement.GetElementsByTagName("format");
                    if (formatPattern.Count != 1)
                        throw new Exception("Invalid number of format patterns for country: " + regionCode);
                    String nationalFormat = formatPattern[0].InnerText;
                    format.SetFormat(nationalFormat);
                    metadata.AddNumberFormat(format);

                    // Extract the pattern for international format. If there is no intlFormat, default to
                    // using the national format. If the intlFormat is set to "NA" the intlFormat should be
                    // ignored.
                    var intlFormat = new NumberFormat.Builder();
                    setLeadingDigitsPatterns(numberFormatElement, intlFormat);
                    intlFormat.SetPattern(numberFormatElement.GetAttribute("pattern"));
                    var intlFormatPattern = numberFormatElement.GetElementsByTagName("intlFormat");

                    if (intlFormatPattern.Count > 1)
                        throw new Exception("Invalid number of intlFormat patterns for country: " + regionCode);
                    if (intlFormatPattern.Count == 0)
                    {
                        // Default to use the same as the national pattern if none is defined.
                        intlFormat.SetFormat(nationalFormat);
                    }
                    else
                    {
                        String intlFormatPatternValue = intlFormatPattern[0].InnerText;
                        if (!intlFormatPatternValue.Equals("NA"))
                            intlFormat.SetFormat(intlFormatPatternValue);
                        hasExplicitIntlFormatDefined = true;
                    }

                    if (intlFormat.HasFormat)
                        metadata.AddIntlNumberFormat(intlFormat);
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

            PhoneNumberDesc generalDesc = new PhoneNumberDesc();
            generalDesc = ProcessPhoneNumberDescElement(generalDesc, element, "generalDesc");
            metadata.SetGeneralDesc(generalDesc);
            metadata.SetFixedLine(ProcessPhoneNumberDescElement(generalDesc, element, "fixedLine"));
            metadata.SetMobile(ProcessPhoneNumberDescElement(generalDesc, element, "mobile"));
            metadata.SetTollFree(ProcessPhoneNumberDescElement(generalDesc, element, "tollFree"));
            metadata.SetPremiumRate(ProcessPhoneNumberDescElement(generalDesc, element, "premiumRate"));
            metadata.SetSharedCost(ProcessPhoneNumberDescElement(generalDesc, element, "sharedCost"));
            metadata.SetVoip(ProcessPhoneNumberDescElement(generalDesc, element, "voip"));
            metadata.SetPersonalNumber(ProcessPhoneNumberDescElement(generalDesc, element,
                                                             "personalNumber"));
            metadata.SetPager(ProcessPhoneNumberDescElement(generalDesc, element, "pager"));
            metadata.SetUan(ProcessPhoneNumberDescElement(generalDesc, element, "uan"));
            metadata.SetNoInternationalDialling(ProcessPhoneNumberDescElement(generalDesc, element,
                                                                      "noInternationalDialling"));

            if (metadata.Mobile.NationalNumberPattern.Equals(
                metadata.FixedLine.NationalNumberPattern))
                metadata.SetSameMobileAndFixedLinePattern(true);
            return metadata.Build();
        }

        private static void setLeadingDigitsPatterns(XmlElement numberFormatElement, NumberFormat.Builder format)
        {
            foreach (XmlElement e in numberFormatElement.GetElementsByTagName("leadingDigits"))
            {
                format.AddLeadingDigitsPattern(ValidateRE(e.InnerText, true));
            }
        }

        private static String GetNationalPrefixFormattingRuleFromElement(XmlElement element,
            String nationalPrefix)
        {
            String nationalPrefixFormattingRule = element.GetAttribute("nationalPrefixFormattingRule");
            // Replace $NP with national prefix and $FG with the first group ($1).
            nationalPrefixFormattingRule = ReplaceFirst(nationalPrefixFormattingRule, "$NP", nationalPrefix);
            nationalPrefixFormattingRule = ReplaceFirst(nationalPrefixFormattingRule, "$FG", "${1}");
            return nationalPrefixFormattingRule;
        }

        private static String GetDomesticCarrierCodeFormattingRuleFromElement(XmlElement element,
            String nationalPrefix)
        {
            String carrierCodeFormattingRule = element.GetAttribute("carrierCodeFormattingRule");
            // Replace $FG with the first group ($1) and $NP with the national prefix.
            carrierCodeFormattingRule = ReplaceFirst(carrierCodeFormattingRule, "$FG", "${1}");
            carrierCodeFormattingRule = ReplaceFirst(carrierCodeFormattingRule, "$NP", nationalPrefix);
            return carrierCodeFormattingRule;
        }

        /**
        * Processes a phone number description element from the XML file and returns it as a
        * PhoneNumberDesc. If the description element is a fixed line or mobile number, the general
        * description will be used to fill in the whole element if necessary, or any components that are
        * missing. For all other types, the general description will only be used to fill in missing
        * components if the type has a partial definition. For example, if no "tollFree" element exists,
        * we assume there are no toll free numbers for that locale, and return a phone number description
        * with "NA" for both the national and possible number patterns.
        *
        * @param generalDesc  a generic phone number description that will be used to fill in missing
        *                     parts of the description
        * @param countryElement  the XML element representing all the country information
        * @param numberType  the name of the number type, corresponding to the appropriate tag in the XML
        *                    file with information about that type
        * @return  complete description of that phone number type
        */
        private static PhoneNumberDesc ProcessPhoneNumberDescElement(PhoneNumberDesc generalDesc,
            XmlElement countryElement, String numberType)
        {
            var phoneNumberDescList = countryElement.GetElementsByTagName(numberType);
            var numberDesc = new PhoneNumberDesc.Builder();
            if (phoneNumberDescList.Count == 0 &&
                (!numberType.Equals("fixedLine") && !numberType.Equals("mobile") &&
                !numberType.Equals("generalDesc")))
            {
                numberDesc.SetNationalNumberPattern("NA");
                numberDesc.SetPossibleNumberPattern("NA");
                return numberDesc.Build();
            }
            numberDesc.MergeFrom(generalDesc);
            if (phoneNumberDescList.Count > 0)
            {
                XmlElement element = (XmlElement)phoneNumberDescList[0];
                var possiblePattern = element.GetElementsByTagName("possibleNumberPattern");
                if (possiblePattern.Count > 0)
                    numberDesc.SetPossibleNumberPattern(ValidateRE(possiblePattern[0].InnerText, true));

                var validPattern = element.GetElementsByTagName("nationalNumberPattern");
                if (validPattern.Count > 0)
                    numberDesc.SetNationalNumberPattern(ValidateRE(validPattern[0].InnerText, true));

                if (!liteBuild)
                {
                    var exampleNumber = element.GetElementsByTagName("exampleNumber");
                    if (exampleNumber.Count > 0)
                        numberDesc.SetExampleNumber(exampleNumber[0].InnerText);
                }
            }
            return numberDesc.Build();
        }

        private static String ReplaceFirst(String input, String value, String replacement)
        {
            var p = input.IndexOf(value);
            if (p >= 0)
                input = input.Substring(0, p) + replacement + input.Substring(p + value.Length);
            return input;
        }
    }
}
