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
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Xunit;

namespace PhoneNumbers.Test
{
    public class TestBuildMetadataFromXml
    {
        // Helper method that outputs a DOM element from a XML string.
        private static XElement ParseXmlString(string xmlString)
        {
            using (var reader = new StringReader(xmlString))
            {
                return XDocument.Load(reader).Root ?? throw new Exception("Failed to ParseXmlString");
            }
        }

        // Tests validateRE().
        [Fact]
        public void TestValidateRERemovesWhiteSpaces()
        {
            var input = " hello world ";
            // Should remove all the white spaces contained in the provided string.
            Assert.Equal("helloworld", BuildMetadataFromXml.ValidateRE(input, true));
            // Make sure it only happens when the last parameter is set to true.
            Assert.Equal(" hello world ", BuildMetadataFromXml.ValidateRE(input, false));
        }

        [Fact]
        public void TestValidateREThrowsException()
        {
            var invalidPattern = "[";
            // Should throw an exception when an invalid pattern is provided independently of the last
            // parameter (remove white spaces).
            try
            {
                BuildMetadataFromXml.ValidateRE(invalidPattern, false);
                Assert.True(false);
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
            try
            {
                BuildMetadataFromXml.ValidateRE(invalidPattern, true);
                Assert.True(false);
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
        }

        [Fact]
        public void TestValidateRE()
        {
            var validPattern = "[a-zA-Z]d{1,9}";
            // The provided pattern should be left unchanged.
            Assert.Equal(validPattern, BuildMetadataFromXml.ValidateRE(validPattern, false));
        }

        // Tests NarrowDigitClassToAscii(): the character-class-aware \d -> [0-9] rewrite used for
        // metadata patterns that are actually matched against (already ASCII-normalized) input --
        // see ValidateAndNarrowPatternRE's doc comment for exactly which fields that is.
        [Theory]
        // Real nationalNumberPattern values, pulled verbatim from resources/PhoneNumberMetadata.xml.
        [InlineData("6\\d{4}", "6[0-9]{4}")]
        [InlineData("[2-47]\\d{4}", "[2-47][0-9]{4}")]
        // Real internationalPrefix values.
        [InlineData("(?:0|1(?:1[0-69]|2[02-5]|5[13-58]|69|7[0167]|8[018]))0",
            "(?:0|1(?:1[0-69]|2[02-5]|5[13-58]|69|7[0167]|8[018]))0")]
        [InlineData("0(?:0|1[3-9]\\d)", "0(?:0|1[3-9][0-9])")]
        // Real nationalPrefixForParsing values (note the trailing "$" anchor and alternation).
        [InlineData("(000[2569]\\d{4,6})$|(?:(?:003768)0?)|0",
            "(000[2569][0-9]{4,6})$|(?:(?:003768)0?)|0")]
        [InlineData("(5\\d{6})$|1", "(5[0-9]{6})$|1")]
        // Real numberFormat "pattern" attribute values.
        [InlineData("(\\d)(\\d{2,3})(\\d{2})(\\d{2})", "([0-9])([0-9]{2,3})([0-9]{2})([0-9]{2})")]
        [InlineData("(\\d)(\\d{2})(\\d{3,4})", "([0-9])([0-9]{2})([0-9]{3,4})")]
        // \d already inside a character class must substitute the bare "0-9" in place, not wrap a
        // second, nested set of brackets around it.
        [InlineData("[\\d-]", "[0-9-]")]
        [InlineData("[a\\d-]", "[a0-9-]")]
        [InlineData("[^\\d]", "[^0-9]")]
        // A \d immediately after the class-opening bracket (no leading literal ']' or '^' first).
        [InlineData("[\\d]", "[0-9]")]
        // A literal ']' as the first class member (POSIX idiom) must not be mistaken for the class
        // closing, so the \d later in the same class still narrows in place.
        [InlineData("[]\\d]", "[]0-9]")]
        [InlineData("[^]\\d]", "[^]0-9]")]
        // An escaped backslash followed by a literal "d" is not \d and must be left alone.
        [InlineData("\\\\d", "\\\\d")]
        // No backslash at all: fast path, unchanged.
        [InlineData("[0-9]{3}", "[0-9]{3}")]
        // Escaped literal characters elsewhere in the pattern must survive untouched.
        [InlineData("\\(\\d{3}\\)", "\\([0-9]{3}\\)")]
        public void TestNarrowDigitClassToAscii(string input, string expected)
        {
            Assert.Equal(expected, BuildMetadataFromXml.NarrowDigitClassToAscii(input));
            // The result must always be a well-formed regex (this also catches the classic
            // nested-bracket bug the character-class-aware rewrite exists to avoid: naively wrapping
            // an in-class \d as "[0-9]" instead of substituting "0-9" produces invalid syntax like
            // "[[0-9]-]", which would fail to compile here).
            _ = new System.Text.RegularExpressions.Regex(BuildMetadataFromXml.NarrowDigitClassToAscii(input));
        }

        [Fact]
        public void TestNarrowDigitClassToAsciiPreservesMatchSemantics()
        {
            // The narrowed pattern must still match/reject exactly the ASCII input the original did --
            // narrowing only removes the (out-of-scope, for already-normalized input) Unicode digit
            // categories, it must not change ASCII-digit behavior.
            const string original = "(000[2569]\\d{4,6})$|(?:(?:003768)0?)|0";
            var narrowed = BuildMetadataFromXml.NarrowDigitClassToAscii(original);
            var originalRegex = new System.Text.RegularExpressions.Regex("^(?:" + original + ")$");
            var narrowedRegex = new System.Text.RegularExpressions.Regex("^(?:" + narrowed + ")$");
            foreach (var candidate in new[] { "0", "0002569123", "00025691234", "003768", "0037680", "1" })
                Assert.Equal(originalRegex.IsMatch(candidate), narrowedRegex.IsMatch(candidate));
        }

        [Fact]
        public void TestNarrowDigitClassToAsciiThrowsOnDInsideCharacterClass()
        {
            // \D cannot in general be soundly unioned into an existing bracket expression's other
            // members, and this shape does not occur anywhere in the shipped metadata today -- see
            // NarrowDigitClassToAscii's doc comment. Surfacing it loudly is preferable to silently
            // emitting an incorrect pattern.
            Assert.Throws<NotSupportedException>(() => BuildMetadataFromXml.NarrowDigitClassToAscii("[\\D-]"));
        }

        [Fact]
        public void TestValidateAndNarrowPatternRENarrowsAndValidates()
        {
            Assert.Equal("[0-9]{6}", BuildMetadataFromXml.ValidateAndNarrowPatternRE("\\d{6}"));
            // removeWhitespace still applies, exactly as for ValidateRE.
            Assert.Equal("[0-9]{6}", BuildMetadataFromXml.ValidateAndNarrowPatternRE("\t \\d { 6 } ", true));
            // An invalid pattern still throws, same as ValidateRE (ArgumentException or the more
            // specific RegexParseException the BCL derives it from).
            Assert.ThrowsAny<ArgumentException>(() => BuildMetadataFromXml.ValidateAndNarrowPatternRE("["));
        }

        // Tests NationalPrefix.
        [Fact]
        public void TestGetNationalPrefix()
        {
            var xmlInput = "<territory nationalPrefix='00'/>";
            var territoryElement = ParseXmlString(xmlInput);
            Assert.Equal("00", BuildMetadataFromXml.GetNationalPrefix(territoryElement));
        }

        // Tests LoadTerritoryTagMetadata().
        [Fact]
        public void TestLoadTerritoryTagMetadata()
        {
            var xmlInput =
                "<territory countryCode='33' leadingDigits='2' internationalPrefix='00'" +
                "           preferredInternationalPrefix='00~11' nationalPrefixForParsing='0'" +
                "           nationalPrefixTransformRule='9$1'" + // nationalPrefix manually injected.
                "           preferredExtnPrefix=' x' mainCountryForCode='true'" +
                "           mobileNumberPortableRegion='true'>" +
                "</territory>";
            var territoryElement = ParseXmlString(xmlInput);
            var phoneMetadata =
            BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "0");
            Assert.Equal(33, phoneMetadata.CountryCode);
            Assert.Equal("2", phoneMetadata.LeadingDigits);
            Assert.Equal("00", phoneMetadata.InternationalPrefix);
            Assert.Equal("00~11", phoneMetadata.PreferredInternationalPrefix);
            Assert.Equal("0", phoneMetadata.NationalPrefixForParsing);
            Assert.Equal("9$1", phoneMetadata.NationalPrefixTransformRule);
            Assert.Equal("0", phoneMetadata.NationalPrefix);
            Assert.Equal(" x", phoneMetadata.PreferredExtnPrefix);
            Assert.True(phoneMetadata.MainCountryForCode);
            Assert.True(phoneMetadata.MobileNumberPortableRegion);
        }

        [Fact]
        public void TestLoadTerritoryTagMetadataSetsBooleanFieldsToFalseByDefault()
        {
            var xmlInput = "<territory countryCode='33'/>";
            var territoryElement = ParseXmlString(xmlInput);
            var phoneMetadata =
                BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "");
            Assert.False(phoneMetadata.MainCountryForCode);
        }

        [Fact]
        public void TestLoadTerritoryTagMetadataSetsNationalPrefixForParsingByDefault()
        {
            var xmlInput = "<territory countryCode='33'/>";
            var territoryElement = ParseXmlString(xmlInput);
            var phoneMetadata =
                BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "00");
            // When unspecified, nationalPrefixForParsing defaults to nationalPrefix.
            Assert.Equal("00", phoneMetadata.NationalPrefix);
            Assert.Equal(phoneMetadata.NationalPrefix, phoneMetadata.NationalPrefixForParsing);
        }

        [Fact]
        public void TestLoadTerritoryTagMetadataWithRequiredAttributesOnly()
        {
            var xmlInput = "<territory countryCode='33' internationalPrefix='00'/>";
            var territoryElement = ParseXmlString(xmlInput);
            // Should not throw any exception.
            BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "");
        }

        // Tests loadInternationalFormat().
        [Fact]
        public void TestLoadInternationalFormat()
        {
            var intlFormat = "$1 $2";
            var xmlInput = "<numberFormat><intlFormat>" + intlFormat + "</intlFormat></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var nationalFormat = "";

            Assert.True(BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement,
                                                                    nationalFormat));
            Assert.Equal(intlFormat, metadata.IntlNumberFormatList[0].Format);
        }

        [Fact]
        public void TestLoadInternationalFormatWithBothNationalAndIntlFormatsDefined()
        {
            var intlFormat = "$1 $2";
            var xmlInput = "<numberFormat><intlFormat>" + intlFormat + "</intlFormat></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var nationalFormat = "$1";

            Assert.True(BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement,
                                                                    nationalFormat));
            Assert.Equal(intlFormat, metadata.IntlNumberFormatList[0].Format);
        }

        [Fact]
        public void TestLoadInternationalFormatExpectsOnlyOnePattern()
        {
            var xmlInput = "<numberFormat><intlFormat/><intlFormat/></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();

            // Should throw an exception as multiple intlFormats are provided.
            Assert.Throws<Exception>(() =>
                BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement, ""));
        }

        [Fact]
        public void TestLoadInternationalFormatWithNaIsIgnored()
        {
            // When the intlFormat element contains "NA", it indicates the international format
            // should be ignored entirely (not output as a literal "NA"). The format is dropped
            // and the entry is not added to intlNumberFormat. See upstream
            // BuildMetadataFromXml.loadInternationalFormat.
            var xmlInput = "<numberFormat><intlFormat>NA</intlFormat></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var nationalFormat = "$1 $2";

            Assert.True(BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement,
                                                                    nationalFormat));
            Assert.Empty(metadata.IntlNumberFormatList);
        }

        [Fact]
        public void TestLoadInternationalFormatUsesNationalFormatByDefault()
        {
            var xmlInput = "<numberFormat></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var nationalFormat = "$1 $2 $3";

            Assert.False(BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement,
                                                                     nationalFormat));
            Assert.Equal(nationalFormat, metadata.IntlNumberFormatList[0].Format);
        }

        // Tests LoadNationalFormat().
        [Fact]
        public void TestLoadNationalFormat()
        {
            var nationalFormat = "$1 $2";
            var xmlInput = string.Format(CultureInfo.InvariantCulture,
                "<numberFormat><format>{0}</format></numberFormat>",
                                            nationalFormat);
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var numberFormat = new NumberFormat.Builder();

            Assert.Equal(nationalFormat,
                         BuildMetadataFromXml.LoadNationalFormat(metadata, numberFormatElement,
                                                                 numberFormat));
        }

        [Fact]
        public void TestLoadNationalFormatRequiresFormat()
        {
            var xmlInput = "<numberFormat></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var numberFormat = new NumberFormat.Builder();

            Assert.Throws<Exception>(() =>
                BuildMetadataFromXml.LoadNationalFormat(metadata, numberFormatElement, numberFormat));
        }

        [Fact]
        public void TestLoadNationalFormatExpectsExactlyOneFormat()
        {
            var xmlInput = "<numberFormat><format/><format/></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var numberFormat = new NumberFormat.Builder();

            Assert.Throws<Exception>(() =>
                BuildMetadataFromXml.LoadNationalFormat(metadata, numberFormatElement, numberFormat));
        }

        // Tests loadAvailableFormats().
        [Fact]
        public void TestLoadAvailableFormats()
        {
            var xmlInput =
                "<territory >" +
                "  <availableFormats>" +
                "    <numberFormat nationalPrefixFormattingRule='($FG)'" +
                "                  carrierCodeFormattingRule='$NP $CC ($FG)'>" +
                "      <format>$1 $2 $3</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(
                metadata, element, "0", "", false /* NP not optional */);
            Assert.Equal("(${1})", metadata.NumberFormatList[0].NationalPrefixFormattingRule);
            Assert.Equal("0 $CC (${1})", metadata.NumberFormatList[0].DomesticCarrierCodeFormattingRule);
            Assert.Equal("$1 $2 $3", metadata.NumberFormatList[0].Format);
        }

        [Fact]
        public void TestLoadAvailableFormatsPropagatesCarrierCodeFormattingRule()
        {
            var xmlInput =
                "<territory carrierCodeFormattingRule='$NP $CC ($FG)'>" +
                "  <availableFormats>" +
                "    <numberFormat nationalPrefixFormattingRule='($FG)'>" +
                "      <format>$1 $2 $3</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(
                metadata, element, "0", "", false /* NP not optional */);
            Assert.Equal("(${1})", metadata.NumberFormatList[0].NationalPrefixFormattingRule);
            Assert.Equal("0 $CC (${1})", metadata.NumberFormatList[0].DomesticCarrierCodeFormattingRule);
            Assert.Equal("$1 $2 $3", metadata.NumberFormatList[0].Format);
        }

        [Fact]
        public void TestLoadAvailableFormatsSetsProvidedNationalPrefixFormattingRule()
        {
            var xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(
                metadata, element, "0", "($1)", false /* NP not optional */);
            Assert.Equal("($1)", metadata.NumberFormatList[0].NationalPrefixFormattingRule);
        }

        [Fact]
        public void TestLoadAvailableFormatsClearsIntlFormat()
        {
            var xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(
                metadata, element, "0", "($1)", false /* NP not optional */);
            Assert.Equal(0, metadata.IntlNumberFormatCount);
        }

        [Fact]
        public void TestLoadAvailableFormatsHandlesMultipleNumberFormats()
        {
            var xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "    <numberFormat><format>$1-$2</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(
                metadata, element, "0", "($1)", false /* NP not optional */);
            Assert.Equal("$1 $2 $3", metadata.NumberFormatList[0].Format);
            Assert.Equal("$1-$2", metadata.NumberFormatList[1].Format);
        }

        // Tests setLeadingDigitsPatterns().
        [Fact]
        public void TestSetLeadingDigitsPatterns()
        {
            var xmlInput =
                "<numberFormat>" +
                "<leadingDigits>1</leadingDigits><leadingDigits>2</leadingDigits>" +
                "</numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var numberFormat = new NumberFormat.Builder();
            BuildMetadataFromXml.SetLeadingDigitsPatterns(numberFormatElement, numberFormat);

            Assert.Equal("1", numberFormat.LeadingDigitsPatternList[0]);
            Assert.Equal("2", numberFormat.LeadingDigitsPatternList[1]);
        }

        // Tests GetNationalPrefixFormattingRuleFromElement().
        [Fact]
        public void TestGetNationalPrefixFormattingRuleFromElement()
        {
            var xmlInput = "<territory nationalPrefixFormattingRule='$NP$FG'/>";
            var element = ParseXmlString(xmlInput);
            Assert.Equal("0${1}",
                         BuildMetadataFromXml.GetNationalPrefixFormattingRuleFromElement(element, "0"));
        }

        // Tests getDomesticCarrierCodeFormattingRuleFromElement().
        [Fact]
        public void TestGetDomesticCarrierCodeFormattingRuleFromElement()
        {
            var xmlInput = "<territory carrierCodeFormattingRule='$NP$CC $FG'/>";
            var element = ParseXmlString(xmlInput);
            // C#: the output regex differs from Java one
            Assert.Equal("0$CC ${1}",
                         BuildMetadataFromXml.GetDomesticCarrierCodeFormattingRuleFromElement(element,
                                                                                              "0"));
        }

        // Tests ProcessPhoneNumberDescElement().
        [Fact]
        public void TestProcessPhoneNumberDescElementWithInvalidInput()
        {
            var territoryElement = ParseXmlString("<territory/>");

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                null, territoryElement, "invalidType");
            Assert.False(phoneNumberDesc.HasNationalNumberPattern);
        }

        [Fact]
        public void TestProcessPhoneNumberDescElementOverridesGeneralDesc()
        {
            var generalDesc = new PhoneNumberDesc.Builder()
                .SetNationalNumberPattern("\\d{8}").Build();
            var xmlInput =
                "<territory><fixedLine>" +
                "  <nationalNumberPattern>\\d{6}</nationalNumberPattern>" +
                "</fixedLine></territory>";
            var territoryElement = ParseXmlString(xmlInput);

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                generalDesc, territoryElement, "fixedLine");
            // \d is narrowed to the ASCII-only [0-9] for patterns that are matched against input --
            // see BuildMetadataFromXml.NarrowDigitClassToAscii.
            Assert.Equal("[0-9]{6}", phoneNumberDesc.NationalNumberPattern);
        }

        [Fact]
        public void TestProcessPhoneNumberDescOutputsExampleNumberByDefault()
        {
            var xmlInput =
                "<territory><fixedLine>" +
                 "  <exampleNumber>01 01 01 01</exampleNumber>" +
                 "</fixedLine></territory>";
            var territoryElement = ParseXmlString(xmlInput);

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                null, territoryElement, "fixedLine");
            Assert.Equal("01 01 01 01", phoneNumberDesc.ExampleNumber);
        }

        [Fact]
        public void TestProcessPhoneNumberDescRemovesWhiteSpacesInPatterns()
        {
            var xmlInput =
                "<territory><fixedLine>" +
                 "  <nationalNumberPattern>\t \\d { 6 } </nationalNumberPattern>" +
                 "</fixedLine></territory>";
            var countryElement = ParseXmlString(xmlInput);

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                null, countryElement, "fixedLine");
            // \d is narrowed to the ASCII-only [0-9] for patterns that are matched against input --
            // see BuildMetadataFromXml.NarrowDigitClassToAscii.
            Assert.Equal("[0-9]{6}", phoneNumberDesc.NationalNumberPattern);
        }

        // Tests LoadGeneralDesc().
        [Fact]
        public void TestLoadGeneralDescSetsSameMobileAndFixedLinePattern()
        {
            var xmlInput =
                "<territory countryCode=\"33\">" +
                "  <fixedLine><nationalNumberPattern>\\d{6}</nationalNumberPattern></fixedLine>" +
                "  <mobile><nationalNumberPattern>\\d{6}</nationalNumberPattern></mobile>" +
                "</territory>";
            var territoryElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            // Should set sameMobileAndFixedPattern to true.
            BuildMetadataFromXml.LoadGeneralDesc(metadata, territoryElement);
            Assert.True(metadata.SameMobileAndFixedLinePattern);
        }

        [Fact]
        public void TestLoadGeneralDescSetsAllDescriptions()
        {
            var xmlInput =
                "<territory countryCode=\"33\">" +
                "  <fixedLine><nationalNumberPattern>\\d{1}</nationalNumberPattern></fixedLine>" +
                "  <mobile><nationalNumberPattern>\\d{2}</nationalNumberPattern></mobile>" +
                "  <pager><nationalNumberPattern>\\d{3}</nationalNumberPattern></pager>" +
                "  <tollFree><nationalNumberPattern>\\d{4}</nationalNumberPattern></tollFree>" +
                "  <premiumRate><nationalNumberPattern>\\d{5}</nationalNumberPattern></premiumRate>" +
                "  <sharedCost><nationalNumberPattern>\\d{6}</nationalNumberPattern></sharedCost>" +
                "  <personalNumber><nationalNumberPattern>\\d{7}</nationalNumberPattern></personalNumber>" +
                "  <voip><nationalNumberPattern>\\d{8}</nationalNumberPattern></voip>" +
                "  <uan><nationalNumberPattern>\\d{9}</nationalNumberPattern></uan>" +
                "  <shortCode><nationalNumberPattern>\\d{10}</nationalNumberPattern></shortCode>" +
                 "</territory>";
            var territoryElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadGeneralDesc(metadata, territoryElement);
            // \d is narrowed to the ASCII-only [0-9] for patterns that are matched against input --
            // see BuildMetadataFromXml.NarrowDigitClassToAscii.
            Assert.Equal("[0-9]{1}", metadata.FixedLine.NationalNumberPattern);
            Assert.Equal("[0-9]{2}", metadata.Mobile.NationalNumberPattern);
            Assert.Equal("[0-9]{3}", metadata.Pager.NationalNumberPattern);
            Assert.Equal("[0-9]{4}", metadata.TollFree.NationalNumberPattern);
            Assert.Equal("[0-9]{5}", metadata.PremiumRate.NationalNumberPattern);
            Assert.Equal("[0-9]{6}", metadata.SharedCost.NationalNumberPattern);
            Assert.Equal("[0-9]{7}", metadata.PersonalNumber.NationalNumberPattern);
            Assert.Equal("[0-9]{8}", metadata.Voip.NationalNumberPattern);
            Assert.Equal("[0-9]{9}", metadata.Uan.NationalNumberPattern);
        }
    }
}
