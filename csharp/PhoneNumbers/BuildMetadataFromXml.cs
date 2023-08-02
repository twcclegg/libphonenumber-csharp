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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PhoneNumbers
{
    public class BuildMetadataFromXml
    {
        // String constants used to fetch the XML nodes and attributes.
        private const string CARRIER_CODE_FORMATTING_RULE = "carrierCodeFormattingRule";

        private const string CARRIER_SPECIFIC = "carrierSpecific";
        private const string COUNTRY_CODE = "countryCode";
        private const string EMERGENCY = "emergency";
        private const string EXAMPLE_NUMBER = "exampleNumber";
        private const string FIXED_LINE = "fixedLine";
        private const string FORMAT = "format";
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

        // Build the PhoneMetadataCollection from the input XML file.
        public static PhoneMetadataCollection BuildPhoneMetadataCollection(string name, bool liteBuild, bool specialBuild, bool isShortNumberMetadata, bool isAlternateFormatsMetadata)
            => new(BuildPhoneMetadata(name, null, liteBuild, specialBuild, isShortNumberMetadata, isAlternateFormatsMetadata, nameSuffix: false));

        internal static List<PhoneMetadata> BuildPhoneMetadata(string name, Assembly asm = null,
            bool liteBuild = false, bool specialBuild = false, bool isShortNumberMetadata = false,
            bool isAlternateFormatsMetadata = false,
            bool nameSuffix = true)
        {
            using var input = GetStream(name, asm, nameSuffix);
            return BuildPhoneMetadataFromStream(input, liteBuild, specialBuild, isShortNumberMetadata,
                isAlternateFormatsMetadata);
        }

        internal static Stream GetStream(string name, Assembly asm = null, bool nameSuffix = true)
        {
            asm ??= typeof(PhoneNumberUtil).Assembly;
            if (nameSuffix)
                name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(name, StringComparison.Ordinal)) ??
                       throw new ArgumentException(name + " resource not found");

            return asm.GetManifestResourceStream(name);
        }

        internal static List<PhoneMetadata> BuildPhoneMetadataFromStream(Stream metadataStream,
            bool liteBuild = false, bool specialBuild = false, bool isShortNumberMetadata = false,
            bool isAlternateFormatsMetadata = false)
        {
            var document = XDocument.Load(metadataStream);

            var metadataCollection = new List<PhoneMetadata>();
            var metadataFilter = GetMetadataFilter(liteBuild, specialBuild);
            foreach (var territory in document.Root.Element("territories").Elements())
            {
                // For the main metadata file this should always be set, but for other supplementary data
                // files the country calling code may be all that is needed.
                var regionCode = territory.Attribute("id")?.Value ?? "";
                var metadata = LoadCountry(regionCode, territory, isShortNumberMetadata, isAlternateFormatsMetadata);
                metadataFilter.FilterMetadata(metadata);
                metadataCollection.Add(metadata);
            }

            return metadataCollection;
        }

        // Build a mapping from a country calling code to the region codes which denote the country/region
        // represented by that country code. In the case of multiple countries sharing a calling code,
        // such as the NANPA countries, the one indicated with "isMainCountryForCode" in the metadata
        // should be first.
        public static Dictionary<int, List<string>> BuildCountryCodeToRegionCodeMap(PhoneMetadataCollection metadataCollection)
            => BuildCountryCodeToRegionCodeMap(metadataCollection.metadata);

        internal static Dictionary<int, List<string>> BuildCountryCodeToRegionCodeMap(List<PhoneMetadata> metadataCollection)
        {
            var countryCodeToRegionCodeMap = new Dictionary<int, List<string>>(250); // currently 215 items
            foreach (var metadata in metadataCollection)
            {
                var regionCode = metadata.Id;
                var countryCode = metadata.CountryCode;
                if (countryCodeToRegionCodeMap.TryGetValue(countryCode, out var list))
                {
                    if (metadata.MainCountryForCode)
                        list.Insert(0, regionCode);
                    else
                        list.Add(regionCode);
                }
                else
                {
                    // For most countries, there will be only one region code for the country calling code.
                    var listWithRegionCode = new List<string>(1);
                    if (regionCode.Length > 0)
                        listWithRegionCode.Add(regionCode);
                    countryCodeToRegionCodeMap[countryCode] = listWithRegionCode;
                }
            }
            return countryCodeToRegionCodeMap;
        }

        // enabled only for unit tests
        internal static HashSet<string> ValidPatterns;

        public static string ValidateRE(string regex)
        {
            if (ValidPatterns != null) ValidateRE(regex, false);
            return regex;
        }

        public static string ValidateRE(string regex, bool removeWhitespace)
        {
            // Removes all the whitespace and newline from the regexp. Not using pattern compile options to
            // make it work across programming languages.
            if (removeWhitespace)
            {
                for (int i = 0; i < regex.Length; i++)
                    if (char.IsWhiteSpace(regex[i]))
                    {
                        var sb = new StringBuilder(regex, 0, i, regex.Length);
                        while (++i < regex.Length)
                        {
                            if (!char.IsWhiteSpace(regex[i]))
                                sb.Append(regex[i]);
                        }
                        regex = sb.ToString();
                        break;
                    }
            }

            if (ValidPatterns is { } cache)
                lock (cache)
                    if (!cache.Contains(regex))
                    {
                        _ = new Regex(regex, RegexOptions.CultureInvariant);
                        cache.Add(regex);
                    }
            // return regex itself if it is of correct regex syntax
            // i.e. compile did not fail with a PatternSyntaxException.
            return regex;
        }

        /**
        * Returns the national prefix of the provided country element.
        */
        public static string GetNationalPrefix(XElement element)
        {
            return element.Attribute(NATIONAL_PREFIX)?.Value ?? "";
        }

        public static PhoneMetadata.Builder LoadTerritoryTagMetadata(string regionCode, XElement element, string nationalPrefix)
            => new(LoadTerritoryTag(regionCode, element, nationalPrefix));

        internal static PhoneMetadata LoadTerritoryTag(string regionCode, XElement element, string nationalPrefix)
        {
            var metadata = new PhoneMetadata { Id = regionCode };
            if (element.Attribute(COUNTRY_CODE) is { } a1)
                metadata.CountryCode = int.Parse(a1.Value, CultureInfo.InvariantCulture);
            if (element.Attribute(LEADING_DIGITS) is { } a2)
                metadata.LeadingDigits = ValidateRE(a2.Value);
            if (element.Attribute(INTERNATIONAL_PREFIX) is { } a3)
                metadata.InternationalPrefix = ValidateRE(a3.Value);
            if (element.Attribute(PREFERRED_INTERNATIONAL_PREFIX) is { } a4)
                metadata.PreferredInternationalPrefix = a4.Value;
            if (element.Attribute(NATIONAL_PREFIX_FOR_PARSING) is { } a5)
            {
                metadata.NationalPrefixForParsing = ValidateRE(a5.Value, true);
                if (element.Attribute(NATIONAL_PREFIX_TRANSFORM_RULE) is { } a6)
                    metadata.NationalPrefixTransformRule = ValidateRE(a6.Value);
            }
            if (!string.IsNullOrEmpty(nationalPrefix))
            {
                metadata.NationalPrefix = nationalPrefix;
                if (!metadata.HasNationalPrefixForParsing)
                    metadata.NationalPrefixForParsing = nationalPrefix;
            }
            if (element.Attribute(PREFERRED_EXTN_PREFIX) is { } a7)
                metadata.PreferredExtnPrefix = a7.Value;
            metadata.MainCountryForCode = element.Attribute(MAIN_COUNTRY_FOR_CODE) != null;
            metadata.MobileNumberPortableRegion = element.Attribute(MOBILE_NUMBER_PORTABLE_REGION) != null;
            return metadata;
        }

        /**
        * Extracts the pattern for international format. If there is no intlFormat, default to using the
        * national format. If the intlFormat is set to "NA" the intlFormat should be ignored.
        *
        * @throws  RuntimeException if multiple intlFormats have been encountered.
        * @return  whether an international number format is defined.
        */
        public static bool LoadInternationalFormat(PhoneMetadata.Builder metadata, XElement numberFormatElement, string nationalFormat)
            => LoadInternationalFormat(metadata.MessageBeingBuilt, numberFormatElement, nationalFormat);

        internal static bool LoadInternationalFormat(PhoneMetadata metadata, XElement numberFormatElement, string nationalFormat)
        {
            var intlFormat = new NumberFormat();
            SetLeadingDigitsPatterns(numberFormatElement, intlFormat);
            intlFormat.Pattern = numberFormatElement.Attribute(PATTERN)?.Value ?? "";
            var intlFormatPattern = numberFormatElement.Elements(INTL_FORMAT).ToList();
            var hasExplicitIntlFormatDefined = false;

            if (intlFormatPattern.Count > 1)
                throw new Exception("Invalid number of intlFormat patterns for country: " +
                                    metadata.Id);
            if (intlFormatPattern.Count == 0)
            {
                // Default to use the same as the national pattern if none is defined.
                intlFormat.Format = nationalFormat;
            }
            else
            {
                intlFormat.Format = intlFormatPattern[0].Value;
                hasExplicitIntlFormatDefined = true;
            }

            if (intlFormat.HasFormat)
                metadata.intlNumberFormat_.Add(intlFormat);
            return hasExplicitIntlFormatDefined;
        }

        /**
         * Extracts the pattern for the national format.
         *
         * @throws  RuntimeException if multiple or no formats have been encountered.
         * @return  the national format string.
         */
        public static string LoadNationalFormat(PhoneMetadata.Builder metadata, XElement numberFormatElement, NumberFormat.Builder format)
            => LoadNationalFormat(metadata.MessageBeingBuilt, numberFormatElement, format.MessageBeingBuilt);

        internal static string LoadNationalFormat(PhoneMetadata metadata, XElement numberFormatElement, NumberFormat format)
        {
            SetLeadingDigitsPatterns(numberFormatElement, format);
            format.Pattern = ValidateRE(numberFormatElement.Attribute(PATTERN)?.Value ?? "");

            var formatPattern = numberFormatElement.Elements(FORMAT).ToList();
            if (formatPattern.Count != 1)
                throw new Exception("Invalid number of format patterns for country: " + metadata.Id);
            return format.Format = formatPattern[0].Value;
        }

        /**
        *  Extracts the available formats from the provided DOM element. If it does not contain any
        *  nationalPrefixFormattingRule, the one passed-in is retained. The nationalPrefix,
        *  nationalPrefixFormattingRule and nationalPrefixOptionalWhenFormatting values are provided from
        *  the parent (territory) element.
        */
        public static void LoadAvailableFormats(PhoneMetadata.Builder metadata, XElement element, string nationalPrefix, string nationalPrefixFormattingRule, bool nationalPrefixOptionalWhenFormatting)
            => LoadAvailableFormats(metadata.MessageBeingBuilt, element, nationalPrefix, nationalPrefixFormattingRule, nationalPrefixOptionalWhenFormatting);

        internal static void LoadAvailableFormats(PhoneMetadata metadata, XElement element, string nationalPrefix, string nationalPrefixFormattingRule, bool nationalPrefixOptionalWhenFormatting)
        {
            var carrierCodeFormattingRule = "";
            if (element.Attribute(CARRIER_CODE_FORMATTING_RULE) is { } a)
                carrierCodeFormattingRule = ValidateRE(
                    GetDomesticCarrierCodeFormattingRuleFromElement(a.Value, nationalPrefix));

            var availableFormats = element.Element("availableFormats");
            var hasExplicitIntlFormatDefined = false;

            if (availableFormats != null && availableFormats.HasElements)
            {
                foreach (var numberFormatElement in availableFormats.Elements())
                {
                    var format = new NumberFormat();

                    format.NationalPrefixFormattingRule = numberFormatElement.Attribute(NATIONAL_PREFIX_FORMATTING_RULE) is { } a1
                        ? GetNationalPrefixFormattingRuleFromElement(a1.Value, nationalPrefix)
                        : nationalPrefixFormattingRule;

                    format.NationalPrefixOptionalWhenFormatting = numberFormatElement.Attribute(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING) is { } a2
                        ? bool.Parse(a2.Value) : nationalPrefixOptionalWhenFormatting;

                    format.DomesticCarrierCodeFormattingRule = numberFormatElement.Attribute(CARRIER_CODE_FORMATTING_RULE) is { } a3
                        ? ValidateRE(GetDomesticCarrierCodeFormattingRuleFromElement(a3.Value, nationalPrefix))
                        : carrierCodeFormattingRule;

                    // Extract the pattern for the national format.
                    var nationalFormat = LoadNationalFormat(metadata, numberFormatElement, format);
                    metadata.numberFormat_.Add(format);

                    if (LoadInternationalFormat(metadata, numberFormatElement, nationalFormat))
                        hasExplicitIntlFormatDefined = true;
                }
                // Only a small number of regions need to specify the intlFormats in the xml. For the majority
                // of countries the intlNumberFormat metadata is an exact copy of the national NumberFormat
                // metadata. To minimize the size of the metadata file, we only keep intlNumberFormats that
                // actually differ in some way to the national formats.
                if (!hasExplicitIntlFormatDefined)
                    metadata.intlNumberFormat_.Clear();
            }
        }

        public static void SetLeadingDigitsPatterns(XElement numberFormatElement, NumberFormat.Builder format)
            => SetLeadingDigitsPatterns(numberFormatElement, format.MessageBeingBuilt);

        internal static void SetLeadingDigitsPatterns(XElement numberFormatElement, NumberFormat format)
        {
            foreach (var e in numberFormatElement.Elements(LEADING_DIGITS))
                format.leadingDigitsPattern_.Add(ValidateRE(e.Value, true));
        }

        public static string GetNationalPrefixFormattingRuleFromElement(XElement element, string nationalPrefix)
            => GetNationalPrefixFormattingRuleFromElement(element.Attribute(NATIONAL_PREFIX_FORMATTING_RULE)?.Value ?? "", nationalPrefix);

        private static string GetNationalPrefixFormattingRuleFromElement(string rule, string nationalPrefix)
        {
            // Replace $NP with national prefix and $FG with the first group ($1).
            rule = ReplaceFirst(rule, "$NP", nationalPrefix);
            rule = ReplaceFirst(rule, "$FG", "${1}");
            return rule;
        }

        public static string GetDomesticCarrierCodeFormattingRuleFromElement(XElement element, string nationalPrefix)
            => GetDomesticCarrierCodeFormattingRuleFromElement(element.Attribute(CARRIER_CODE_FORMATTING_RULE)?.Value ?? "", nationalPrefix);

        private static string GetDomesticCarrierCodeFormattingRuleFromElement(string rule, string nationalPrefix)
        {
            // Replace $FG with the first group ($1) and $NP with the national prefix.
            rule = ReplaceFirst(rule, "$FG", "${1}");
            rule = ReplaceFirst(rule, "$NP", nationalPrefix);
            return rule;
        }

        /**
        * Checks if the possible lengths provided as a sorted set are equal to the possible lengths
        * stored already in the description pattern. Note that possibleLengths may be empty but must not
        * be null, and the PhoneNumberDesc passed in should also not be null.
        */
        private static bool ArePossibleLengthsEqual(SortedSet<int> possibleLengths,
            PhoneNumberDesc desc)
        {
            if (possibleLengths.Count != desc.PossibleLengthCount)
                return false;
            // Note that both should be sorted already, and we know they are the same length.
            var i = 0;
            foreach (var length in possibleLengths)
            {
                if (length != desc.GetPossibleLength(i))
                    return false;
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
        * @param generalDesc  a generic phone number description that will be used to fill in missing
        *                     parts of the description
        * @param countryElement  the XML element representing all the country information
        * @param numberType  the name of the number type, corresponding to the appropriate tag in the XML
        *                    file with information about that type
        * @return  complete description of that phone number type
        */
        public static PhoneNumberDesc.Builder ProcessPhoneNumberDescElement(PhoneNumberDesc parentDesc, XElement countryElement, string numberType)
            => new(ProcessPhoneNumberDesc(parentDesc, countryElement, numberType));

        internal static PhoneNumberDesc ProcessPhoneNumberDesc(PhoneNumberDesc parentDesc, XElement countryElement, string numberType)
        {
            var element = countryElement.Element(numberType);
            var numberDesc = new PhoneNumberDesc();
            if (element == null)
            {
                // -1 will never match a possible phone number length, so is safe to use to ensure this never
                // matches. We don't leave it empty, since for compression reasons, we use the empty list to
                // mean that the generalDesc possible lengths apply.
                numberDesc.possibleLength_.Add(-1);
                return numberDesc;
            }

            parentDesc ??= new PhoneNumberDesc();
            var lengths = new SortedSet<int>();
            var localOnlyLengths = new SortedSet<int>();
            PopulatePossibleLengthSets(element.Elements(POSSIBLE_LENGTHS), lengths, localOnlyLengths);
            SetPossibleLengths(lengths, localOnlyLengths, parentDesc, numberDesc);

            var validPattern = element.Element(NATIONAL_NUMBER_PATTERN);
            if (validPattern != null)
                numberDesc.NationalNumberPattern = ValidateRE(validPattern.Value, true);

            var exampleNumber = element.Element(EXAMPLE_NUMBER);
            if (exampleNumber != null)
                numberDesc.ExampleNumber = exampleNumber.Value;

            return numberDesc;
        }

        private static void SetRelevantDescPatterns(PhoneMetadata metadata, XElement element,
            bool isShortNumberMetadata)
        {
            var generalDesc = ProcessPhoneNumberDesc(null, element, GENERAL_DESC);
            // Calculate the possible lengths for the general description. This will be based on the
            // possible lengths of the child elements.
            SetPossibleLengthsGeneralDesc(generalDesc, metadata.Id, element, isShortNumberMetadata);
            metadata.GeneralDesc = generalDesc;

            if (!isShortNumberMetadata)
            {
                // Set fields used by regular length phone numbers.
                metadata.FixedLine = ProcessPhoneNumberDesc(generalDesc, element, FIXED_LINE);
                metadata.Mobile = ProcessPhoneNumberDesc(generalDesc, element, MOBILE);
                metadata.SharedCost = ProcessPhoneNumberDesc(generalDesc, element, SHARED_COST);
                metadata.Voip = ProcessPhoneNumberDesc(generalDesc, element, VOIP);
                metadata.PersonalNumber = ProcessPhoneNumberDesc(generalDesc, element, PERSONAL_NUMBER);
                metadata.Pager = ProcessPhoneNumberDesc(generalDesc, element, PAGER);
                metadata.Uan = ProcessPhoneNumberDesc(generalDesc, element, UAN);
                metadata.Voicemail = ProcessPhoneNumberDesc(generalDesc, element, VOICEMAIL);
                metadata.NoInternationalDialling = ProcessPhoneNumberDesc(generalDesc, element, NO_INTERNATIONAL_DIALLING);
                metadata.SameMobileAndFixedLinePattern = metadata.Mobile.NationalNumberPattern == metadata.FixedLine.NationalNumberPattern;
                metadata.TollFree = ProcessPhoneNumberDesc(generalDesc, element, TOLL_FREE);
                metadata.PremiumRate = ProcessPhoneNumberDesc(generalDesc, element, PREMIUM_RATE);
            }
            else
            {
                // Set fields used by short numbers.
                metadata.StandardRate = ProcessPhoneNumberDesc(generalDesc, element, STANDARD_RATE);
                metadata.ShortCode = ProcessPhoneNumberDesc(generalDesc, element, SHORT_CODE);
                metadata.CarrierSpecific = ProcessPhoneNumberDesc(generalDesc, element, CARRIER_SPECIFIC);
                metadata.Emergency = ProcessPhoneNumberDesc(generalDesc, element, EMERGENCY);
                metadata.TollFree = ProcessPhoneNumberDesc(generalDesc, element, TOLL_FREE);
                metadata.PremiumRate = ProcessPhoneNumberDesc(generalDesc, element, PREMIUM_RATE);
                metadata.SmsServices = ProcessPhoneNumberDesc(generalDesc, element, SMS_SERVICES);
            }
        }

        private static SortedSet<int> ParsePossibleLengthStringToSet(string possibleLengthString)
        {
            if (possibleLengthString.Length == 0)
                throw new Exception("Empty possibleLength string found.");
            var lengths = possibleLengthString.Split(',');
            var lengthSet = new SortedSet<int>();
            foreach (var lengthSubstring in lengths)
            {
                if (lengthSubstring.Length == 0)
                    throw new Exception("Leading, trailing or adjacent commas in possible " +
                                        $"length string {possibleLengthString}, these should only separate numbers or ranges.");
                if (lengthSubstring[0] == '[')
                {
                    if (lengthSubstring[lengthSubstring.Length - 1] != ']')
                        throw new Exception("Missing end of range character in possible " +
                                            $"length string {possibleLengthString}.");
                    // Strip the leading and trailing [], and split on the -.
                    var minMax = lengthSubstring.Substring(1, lengthSubstring.Length - 2).Split('-');
                    if (minMax.Length != 2)
                        throw new Exception("Ranges must have exactly one - character: " +
                                            $"missing for {possibleLengthString}.");
                    var min = int.Parse(minMax[0], CultureInfo.InvariantCulture);
                    var max = int.Parse(minMax[1], CultureInfo.InvariantCulture);
                    // We don't even accept [6-7] since we prefer the shorter 6,7 variant; for a range to be in
                    // use the hyphen needs to replace at least one digit.
                    if (max - min < 2)
                        throw new Exception("The first number in a range should be two or " +
                                            $"more digits lower than the second. Culprit possibleLength string: {possibleLengthString}");
                    for (var j = min; j <= max; j++)
                        if (!lengthSet.Add(j))
                            throw new Exception($"Duplicate length element found ({j}) in " +
                                                $"possibleLength string {possibleLengthString}");
                }
                else
                {
                    var length = int.Parse(lengthSubstring, CultureInfo.InvariantCulture);
                    if (!lengthSet.Add(length))
                        throw new Exception($"Duplicate length element found ({length}) in " +
                                            $"possibleLength string {possibleLengthString}");
                }
            }
            return lengthSet;
        }

        /**
         * Reads the possible lengths present in the metadata and splits them into two sets: one for
         * full-length numbers, one for local numbers.
         *
         * @param data  one or more phone number descriptions, represented as XML nodes
         * @param lengths  a set to which to add possible lengths of full phone numbers
         * @param localOnlyLengths  a set to which to add possible lengths of phone numbers only diallable
         *     locally (e.g. within a province)
         */
        private static void PopulatePossibleLengthSets(IEnumerable<XElement> possibleLengths, SortedSet<int> lengths,
            SortedSet<int> localOnlyLengths)
        {
            foreach (var element in possibleLengths)
            {
                // We don't add to the phone metadata yet, since we want to sort length elements found under
                // different nodes first, make sure there are no duplicates between them and that the
                // localOnly lengths don't overlap with the others.
                var thisElementLengths = ParsePossibleLengthStringToSet(element.Attribute(NATIONAL).Value);
                if (element.Attribute(LOCAL_ONLY) is { } localLengths)
                {
                    var thisElementLocalOnlyLengths = ParsePossibleLengthStringToSet(localLengths.Value);
                    if (thisElementLengths.Overlaps(thisElementLocalOnlyLengths))
                        throw new Exception(
                            $"Possible length(s) found specified as a normal and local-only length: {thisElementLengths.Intersect(thisElementLocalOnlyLengths)}");
                    // We check again when we set these lengths on the metadata itself in setPossibleLengths
                    // that the elements in localOnly are not also in lengths. For e.g. the generalDesc, it
                    // might have a local-only length for one type that is a normal length for another type. We
                    // don't consider this an error, but we do want to remove the local-only lengths.
                    foreach (var length in thisElementLocalOnlyLengths)
                        localOnlyLengths.Add(length);
                }
                // It is okay if at this time we have duplicates, because the same length might be possible
                // for e.g. fixed-line and for mobile numbers, and this method operates potentially on
                // multiple phoneNumberDesc XML elements.
                foreach (var length in thisElementLengths)
                    lengths.Add(length);
            }
        }

        /**
         * Sets possible lengths in the general description, derived from certain child elements.
         */
        private static void SetPossibleLengthsGeneralDesc(PhoneNumberDesc generalDesc, string metadataId,
            XElement data, bool isShortNumberMetadata)
        {
            var lengths = new SortedSet<int>();
            var localOnlyLengths = new SortedSet<int>();
            // The general description node should *always* be present if metadata for other types is
            // present, aside from in some unit tests.
            // (However, for e.g. formatting metadata in PhoneNumberAlternateFormats, no PhoneNumberDesc
            // elements are present).
            var generalDescNode = data.Element(GENERAL_DESC);
            if (generalDescNode != null)
            {
                PopulatePossibleLengthSets(generalDescNode.Elements(POSSIBLE_LENGTHS), lengths, localOnlyLengths);
                if (lengths.Count != 0 || localOnlyLengths.Count != 0)
                    throw new Exception("Found possible lengths specified at general " +
                                        $"desc: this should be derived from child elements. Affected country: {metadataId}");
            }
            if (!isShortNumberMetadata)
            {
                var allDescData = data.Descendants(POSSIBLE_LENGTHS).Where(e => e.Parent.Name != NO_INTERNATIONAL_DIALLING);
                PopulatePossibleLengthSets(allDescData, lengths, localOnlyLengths);
            }
            else
            {
                // For short number metadata, we want to copy the lengths from the "short code" section only.
                // This is because it's the more detailed validation pattern, it's not a sub-type of short
                // codes. The other lengths will be checked later to see that they are a sub-set of these
                // possible lengths.
                var shortCodeDesc = data.Element(SHORT_CODE);
                if (shortCodeDesc != null)
                {
                    PopulatePossibleLengthSets(shortCodeDesc.Elements(POSSIBLE_LENGTHS), lengths, localOnlyLengths);
                }
                if (localOnlyLengths.Count > 0)
                    throw new Exception("Found local-only lengths in short-number metadata");
            }
            SetPossibleLengths(lengths, localOnlyLengths, null, generalDesc);
        }

        /**
        * Sets the possible length fields in the metadata from the sets of data passed in. Checks that
        * the length is covered by the "parent" phone number description element if one is present, and
        * if the lengths are exactly the same as this, they are not filled in for efficiency reasons.
        *
        * @param parentDesc  the "general description" element or null if desc is the generalDesc itself
        * @param desc  the PhoneNumberDesc object that we are going to set lengths for
        */
        private static void SetPossibleLengths(SortedSet<int> lengths,
            SortedSet<int> localOnlyLengths, PhoneNumberDesc parentDesc, PhoneNumberDesc desc)
        {
            // Only add the lengths to this sub-type if they aren't exactly the same as the possible
            // lengths in the general desc (for metadata size reasons).
            if (parentDesc == null || !ArePossibleLengthsEqual(lengths, parentDesc))
                foreach (var length in lengths)
                    if (parentDesc == null || parentDesc.possibleLength_.Contains(length))
                        desc.possibleLength_.Add(length);
                    else
                        throw new Exception(
                            $"Out-of-range possible length found ({length}), parent lengths {string.Join(", ", parentDesc.PossibleLengthList)}.");
            // We check that the local-only length isn't also a normal possible length (only relevant for
            // the general-desc, since within elements such as fixed-line we would throw an exception if we
            // saw this) before adding it to the collection of possible local-only lengths.
            foreach (var length in localOnlyLengths)
                if (!lengths.Contains(length))
                    if (parentDesc == null || parentDesc.possibleLengthLocalOnly_.Contains(length)
                        || parentDesc.possibleLength_.Contains(length))
                        desc.possibleLengthLocalOnly_.Add(length);
                    else
                        throw new Exception(
                            $"Out-of-range local-only possible length found ({length}), parent length {string.Join(", ", parentDesc.PossibleLengthLocalOnlyList)}.");
        }


        private static string ReplaceFirst(string input, string value, string replacement)
        {
            var p = input.IndexOf(value, StringComparison.Ordinal);
            if (p >= 0)
            {
#if NET6_0_OR_GREATER
                return string.Concat(input.AsSpan(0, p), replacement, input.AsSpan(p + value.Length));
#else
                return input.Substring(0, p) + replacement + input.Substring(p + value.Length);
#endif
            }
            return input;
        }

        // @VisibleForTesting
        public static void LoadGeneralDesc(PhoneMetadata.Builder metadata, XElement element)
            => LoadGeneralDesc(metadata.MessageBeingBuilt, element);

        internal static void LoadGeneralDesc(PhoneMetadata metadata, XElement element)
        {
            var generalDesc = ProcessPhoneNumberDesc(null, element, GENERAL_DESC);
            SetPossibleLengthsGeneralDesc(generalDesc, metadata.Id, element, false);

            metadata.FixedLine = ProcessPhoneNumberDesc(generalDesc, element, FIXED_LINE);
            metadata.Mobile = ProcessPhoneNumberDesc(generalDesc, element, MOBILE);
            metadata.TollFree = ProcessPhoneNumberDesc(generalDesc, element, TOLL_FREE);
            metadata.PremiumRate = ProcessPhoneNumberDesc(generalDesc, element, PREMIUM_RATE);
            metadata.SharedCost = ProcessPhoneNumberDesc(generalDesc, element, SHARED_COST);
            metadata.Voip = ProcessPhoneNumberDesc(generalDesc, element, VOIP);
            metadata.PersonalNumber = ProcessPhoneNumberDesc(generalDesc, element, PERSONAL_NUMBER);
            metadata.Pager = ProcessPhoneNumberDesc(generalDesc, element, PAGER);
            metadata.Uan = ProcessPhoneNumberDesc(generalDesc, element, UAN);
            metadata.Voicemail = ProcessPhoneNumberDesc(generalDesc, element, VOICEMAIL);
            metadata.Emergency = ProcessPhoneNumberDesc(generalDesc, element, EMERGENCY);
            metadata.NoInternationalDialling = ProcessPhoneNumberDesc(generalDesc, element, NO_INTERNATIONAL_DIALLING);
            metadata.SameMobileAndFixedLinePattern = metadata.Mobile.NationalNumberPattern == metadata.FixedLine.NationalNumberPattern;
        }

        public static PhoneMetadata.Builder LoadCountryMetadata(string regionCode, XElement element, bool isShortNumberMetadata, bool isAlternateFormatsMetadata)
            => new(LoadCountry(regionCode, element, isShortNumberMetadata, isAlternateFormatsMetadata));

        internal static PhoneMetadata LoadCountry(string regionCode, XElement element, bool isShortNumberMetadata, bool isAlternateFormatsMetadata)
        {
            var nationalPrefix = GetNationalPrefix(element);
            var metadata = LoadTerritoryTag(regionCode, element, nationalPrefix);
            var nationalPrefixFormattingRule = GetNationalPrefixFormattingRuleFromElement(element, nationalPrefix);
            LoadAvailableFormats(metadata, element, nationalPrefix, nationalPrefixFormattingRule, element.Attribute(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING) != null);
            LoadGeneralDesc(metadata, element);
            if (!isAlternateFormatsMetadata)
                SetRelevantDescPatterns(metadata, element, isShortNumberMetadata);
            return metadata;
        }

        public static Dictionary<int, List<string>> GetCountryCodeToRegionCodeMap(string filePrefix)
        {
            var collection = BuildPhoneMetadata(filePrefix); // todo lite/special build
            return BuildCountryCodeToRegionCodeMap(collection);
        }

        /**
         * Processes the custom build flags and gets a {@code MetadataFilter} which may be used to
        * filter {@code PhoneMetadata} objects. Incompatible flag combinations throw RuntimeException.
        *
        * @param liteBuild  The liteBuild flag value as given by the command-line
        * @param specialBuild  The specialBuild flag value as given by the command-line
        */
        // @VisibleForTesting
        internal static MetadataFilter GetMetadataFilter(bool liteBuild, bool specialBuild)
        {
            if (specialBuild)
            {
                if (liteBuild)
                    throw new Exception("liteBuild and specialBuild may not both be set");
                return MetadataFilter.ForSpecialBuild();
            }
            return liteBuild ? MetadataFilter.ForLiteBuild() : MetadataFilter.EmptyFilter();
        }
    }
}
