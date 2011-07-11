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
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using NUnit.Framework;

namespace PhoneNumbers.Test
{
    [TestFixture]
    class TestBuildMetadataFromXml
    {
        // Helper method that outputs a DOM element from a XML string.
        private static XmlElement parseXmlString(String xmlString)
        {

            var document = new XmlDocument();
            document.LoadXml(xmlString);
            document.Normalize();
            return document.DocumentElement;
        }

        // Tests validateRE().
        [Test]
        public void TestValidateRERemovesWhiteSpaces()
        {
            String input = " hello world ";
            // Should remove all the white spaces contained in the provided string.
            Assert.AreEqual("helloworld", BuildMetadataFromXml.ValidateRE(input, true));
            // Make sure it only happens when the last parameter is set to true.
            Assert.AreEqual(" hello world ", BuildMetadataFromXml.ValidateRE(input, false));
        }

        [Test]
        public void TestValidateREThrowsException()
        {
            String invalidPattern = "[";
            // Should throw an exception when an invalid pattern is provided independently of the last
            // parameter (remove white spaces).
            try
            {
                BuildMetadataFromXml.ValidateRE(invalidPattern, false);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
            try
            {
                BuildMetadataFromXml.ValidateRE(invalidPattern, true);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
        }

        [Test]
        public void TestValidateRE()
        {
            String validPattern = "[a-zA-Z]d{1,9}";
            // The provided pattern should be left unchanged.
            Assert.AreEqual(validPattern, BuildMetadataFromXml.ValidateRE(validPattern, false));
        }

        // Tests NationalPrefix.
        [Test]
        public void TestGetNationalPrefix()
        {
            String xmlInput = "<territory nationalPrefix='00'/>";
            var territoryElement = parseXmlString(xmlInput);
            Assert.AreEqual("00", BuildMetadataFromXml.GetNationalPrefix(territoryElement));
        }

        // Tests LoadTerritoryTagMetadata().
        [Test]
        public void TestLoadTerritoryTagMetadata()
        {
            String xmlInput =
                "<territory countryCode='33' leadingDigits='2' internationalPrefix='00'" +
                "           preferredInternationalPrefix='0011' nationalPrefixForParsing='0'" +
                "           nationalPrefixTransformRule='9$1'" + // nationalPrefix manually injected.
                "           preferredExtnPrefix=' x' mainCountryForCode='true'" +
                "           leadingZeroPossible='true'>" +
                "</territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder phoneMetadata =
                BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "0", "");
            Assert.AreEqual(33, phoneMetadata.CountryCode);
            Assert.AreEqual("2", phoneMetadata.LeadingDigits);
            Assert.AreEqual("00", phoneMetadata.InternationalPrefix);
            Assert.AreEqual("0011", phoneMetadata.PreferredInternationalPrefix);
            Assert.AreEqual("0", phoneMetadata.NationalPrefixForParsing);
            Assert.AreEqual("9$1", phoneMetadata.NationalPrefixTransformRule);
            Assert.AreEqual("0", phoneMetadata.NationalPrefix);
            Assert.AreEqual(" x", phoneMetadata.PreferredExtnPrefix);
            Assert.True(phoneMetadata.MainCountryForCode);
            Assert.True(phoneMetadata.LeadingZeroPossible);
        }

        [Test]
        public void TestLoadTerritoryTagMetadataSetsBooleanFieldsToFalseByDefault()
        {
            String xmlInput = "<territory countryCode='33'/>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder phoneMetadata =
                BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "", "");
            Assert.False(phoneMetadata.MainCountryForCode);
            Assert.False(phoneMetadata.LeadingZeroPossible);
        }

        [Test]
        public void TestLoadTerritoryTagMetadataSetsNationalPrefixForParsingByDefault()
        {
            String xmlInput = "<territory countryCode='33'/>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder phoneMetadata =
                BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "00", "");
            // When unspecified, nationalPrefixForParsing defaults to nationalPrefix.
            Assert.AreEqual("00", phoneMetadata.NationalPrefix);
            Assert.AreEqual(phoneMetadata.NationalPrefix, phoneMetadata.NationalPrefixForParsing);
        }

        [Test]
        public void TestLoadTerritoryTagMetadataWithRequiredAttributesOnly()
        {
            String xmlInput = "<territory countryCode='33' internationalPrefix='00'/>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            // Should not throw any exception.
            PhoneMetadata.Builder phoneMetadata =
                BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "", "");
        }

        // Tests loadInternationalFormat().
        [Test]
        public void TestLoadInternationalFormat()
        {
            String intlFormat = "$1 $2";
            String xmlInput = "<numberFormat><intlFormat>" + intlFormat + "</intlFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            String nationalFormat = "";

            Assert.True(BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement,
                                                                    nationalFormat));
            Assert.AreEqual(intlFormat, metadata.IntlNumberFormatList[0].Format);
        }

        [Test]
        public void TestLoadInternationalFormatWithBothNationalAndIntlFormatsDefined()
        {
            String intlFormat = "$1 $2";
            String xmlInput = "<numberFormat><intlFormat>" + intlFormat + "</intlFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            String nationalFormat = "$1";

            Assert.True(BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement,
                                                                    nationalFormat));
            Assert.AreEqual(intlFormat, metadata.IntlNumberFormatList[0].Format);
        }

        [Test]
        public void TestLoadInternationalFormatExpectsOnlyOnePattern()
        {
            String xmlInput = "<numberFormat><intlFormat/><intlFormat/></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();

            // Should throw an exception as multiple intlFormats are provided.
            try
            {
                BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement, "");
                Assert.Fail();
            }
            catch (Exception)
            {
                // Test passed.
            }
        }

        [Test]
        public void TestLoadInternationalFormatUsesNationalFormatByDefault()
        {
            String xmlInput = "<numberFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            String nationalFormat = "$1 $2 $3";

            Assert.False(BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement,
                                                                     nationalFormat));
            Assert.AreEqual(nationalFormat, metadata.IntlNumberFormatList[0].Format);
        }

        // Tests LoadNationalFormat().
        [Test]
        public void TestLoadNationalFormat()
        {
            String nationalFormat = "$1 $2";
            String xmlInput = String.Format("<numberFormat><format>{0}</format></numberFormat>",
                                            nationalFormat);
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            NumberFormat.Builder numberFormat = new NumberFormat.Builder();

            Assert.AreEqual(nationalFormat,
                         BuildMetadataFromXml.LoadNationalFormat(metadata, numberFormatElement,
                                                                 numberFormat));
        }

        [Test]
        public void TestLoadNationalFormatRequiresFormat()
        {
            String xmlInput = "<numberFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            NumberFormat.Builder numberFormat = new NumberFormat.Builder();

            try
            {
                BuildMetadataFromXml.LoadNationalFormat(metadata, numberFormatElement, numberFormat);
                Assert.Fail();
            }
            catch (Exception)
            {
                // Test passed.
            }
        }

        [Test]
        public void TestLoadNationalFormatExpectsExactlyOneFormat()
        {
            String xmlInput = "<numberFormat><format/><format/></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            NumberFormat.Builder numberFormat = new NumberFormat.Builder();

            try
            {
                BuildMetadataFromXml.LoadNationalFormat(metadata, numberFormatElement, numberFormat);
                Assert.Fail();
            }
            catch (Exception)
            {
                // Test passed.
            }
        }

        // Tests loadAvailableFormats().
        [Test]
        public void TestLoadAvailableFormats()
        {
            String xmlInput =
                "<territory >" +
                "  <availableFormats>" +
                "    <numberFormat nationalPrefixFormattingRule='($FG)'" +
                "                  carrierCodeFormattingRule='$NP $CC ($FG)'>" +
                "      <format>$1 $2 $3</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(metadata, "AE", element, "0", "");
            Assert.AreEqual("(${1})", metadata.NumberFormatList[0].NationalPrefixFormattingRule);
            Assert.AreEqual("0 $CC (${1})", metadata.NumberFormatList[0].DomesticCarrierCodeFormattingRule);
            Assert.AreEqual("$1 $2 $3", metadata.NumberFormatList[0].Format);
        }

        [Test]
        public void TestLoadAvailableFormatsPropagatesCarrierCodeFormattingRule()
        {
            String xmlInput =
                "<territory carrierCodeFormattingRule='$NP $CC ($FG)'>" +
                "  <availableFormats>" +
                "    <numberFormat nationalPrefixFormattingRule='($FG)'>" +
                "      <format>$1 $2 $3</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(metadata, "AE", element, "0", "");
            Assert.AreEqual("(${1})", metadata.NumberFormatList[0].NationalPrefixFormattingRule);
            Assert.AreEqual("0 $CC (${1})", metadata.NumberFormatList[0].DomesticCarrierCodeFormattingRule);
            Assert.AreEqual("$1 $2 $3", metadata.NumberFormatList[0].Format);
        }

        [Test]
        public void TestLoadAvailableFormatsSetsProvidedNationalPrefixFormattingRule()
        {
            String xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(metadata, "AE", element, "", "($1)");
            Assert.AreEqual("($1)", metadata.NumberFormatList[0].NationalPrefixFormattingRule);
        }

        [Test]
        public void TestLoadAvailableFormatsClearsIntlFormat()
        {
            String xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(metadata, "AE", element, "0", "($1)");
            Assert.AreEqual(0, metadata.IntlNumberFormatCount);
        }

        [Test]
        public void TestLoadAvailableFormatsHandlesMultipleNumberFormats()
        {
            String xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "    <numberFormat><format>$1-$2</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormats(metadata, "AE", element, "0", "($1)");
            Assert.AreEqual("$1 $2 $3", metadata.NumberFormatList[0].Format);
            Assert.AreEqual("$1-$2", metadata.NumberFormatList[1].Format);
        }

        [Test]
        public void TestLoadInternationalFormatDoesNotSetIntlFormatWhenNA()
        {
            String xmlInput = "<numberFormat><intlFormat>NA</intlFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            String nationalFormat = "$1 $2";

            BuildMetadataFromXml.LoadInternationalFormat(metadata, numberFormatElement, nationalFormat);
            Assert.AreEqual(0, metadata.IntlNumberFormatCount);
        }

        // Tests setLeadingDigitsPatterns().
        [Test]
        public void TestSetLeadingDigitsPatterns()
        {
            String xmlInput =
                "<numberFormat>" +
                "<leadingDigits>1</leadingDigits><leadingDigits>2</leadingDigits>" +
                "</numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            NumberFormat.Builder numberFormat = new NumberFormat.Builder();
            BuildMetadataFromXml.SetLeadingDigitsPatterns(numberFormatElement, numberFormat);

            Assert.AreEqual("1", numberFormat.LeadingDigitsPatternList[0]);
            Assert.AreEqual("2", numberFormat.LeadingDigitsPatternList[1]);
        }

        // Tests GetNationalPrefixFormattingRuleFromElement().
        [Test]
        public void TestGetNationalPrefixFormattingRuleFromElement()
        {
            String xmlInput = "<territory nationalPrefixFormattingRule='$NP$FG'/>";
            XmlElement element = parseXmlString(xmlInput);
            Assert.AreEqual("0${1}",
                         BuildMetadataFromXml.GetNationalPrefixFormattingRuleFromElement(element, "0"));
        }

        // Tests getDomesticCarrierCodeFormattingRuleFromElement().
        [Test]
        public void TestGetDomesticCarrierCodeFormattingRuleFromElement()
        {
            String xmlInput = "<territory carrierCodeFormattingRule='$NP$CC $FG'/>";
            XmlElement element = parseXmlString(xmlInput);
            // C#: the output regex differs from Java one
            Assert.AreEqual("0$CC ${1}",
                         BuildMetadataFromXml.GetDomesticCarrierCodeFormattingRuleFromElement(element,
                                                                                              "0"));
        }

        // Tests isValidNumberType().
        [Test]
        public void TestIsValidNumberTypeWithInvalidInput()
        {
            Assert.False(BuildMetadataFromXml.IsValidNumberType("invalidType"));
        }

        // Tests ProcessPhoneNumberDescElement().
        [Test]
        public void TestProcessPhoneNumberDescElementWithInvalidInput()
        {
            XmlElement territoryElement = parseXmlString("<territory/>");

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                null, territoryElement, "invalidType");
            Assert.AreEqual("NA", phoneNumberDesc.PossibleNumberPattern);
            Assert.AreEqual("NA", phoneNumberDesc.NationalNumberPattern);
        }

        [Test]
        public void TestProcessPhoneNumberDescElementMergesWithGeneralDesc()
        {
            PhoneNumberDesc generalDesc = new PhoneNumberDesc.Builder()
                .SetPossibleNumberPattern("\\d{6}").Build();
            XmlElement territoryElement = parseXmlString("<territory><fixedLine/></territory>");

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                generalDesc, territoryElement, "fixedLine");
            Assert.AreEqual("\\d{6}", phoneNumberDesc.PossibleNumberPattern);
        }

        [Test]
        public void TestProcessPhoneNumberDescElementOverridesGeneralDesc()
        {
            PhoneNumberDesc generalDesc = new PhoneNumberDesc.Builder()
                .SetPossibleNumberPattern("\\d{8}").Build();
            String xmlInput =
                "<territory><fixedLine>" +
                "  <possibleNumberPattern>\\d{6}</possibleNumberPattern>" +
                "</fixedLine></territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                generalDesc, territoryElement, "fixedLine");
            Assert.AreEqual("\\d{6}", phoneNumberDesc.PossibleNumberPattern);
        }

        [Test]
        public void TestProcessPhoneNumberDescElementHandlesLiteBuild()
        {
            try
            {
                BuildMetadataFromXml.LiteBuild = true;
                String xmlInput =
                    "<territory><fixedLine>" +
                    "  <exampleNumber>01 01 01 01</exampleNumber>" +
                    "</fixedLine></territory>";
                XmlElement territoryElement = parseXmlString(xmlInput);
                var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                    null, territoryElement, "fixedLine");
                Assert.AreEqual("", phoneNumberDesc.ExampleNumber);
            }
            finally
            {
                // Restore the lite build parameter to its default value (false) to avoid potential
                // side-effects in other tests.
                BuildMetadataFromXml.LiteBuild = false;
            }
        }

        [Test]
        public void TestProcessPhoneNumberDescOutputsExampleNumberByDefault()
        {
            String xmlInput =
                "<territory><fixedLine>" +
                 "  <exampleNumber>01 01 01 01</exampleNumber>" +
                 "</fixedLine></territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                null, territoryElement, "fixedLine");
            Assert.AreEqual("01 01 01 01", phoneNumberDesc.ExampleNumber);
        }

        [Test]
        public void TestProcessPhoneNumberDescRemovesWhiteSpacesInPatterns()
        {
            String xmlInput =
                "<territory><fixedLine>" +
                 "  <possibleNumberPattern>\t \\d { 6 } </possibleNumberPattern>" +
                 "</fixedLine></territory>";
            XmlElement countryElement = parseXmlString(xmlInput);

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElement(
                null, countryElement, "fixedLine");
            Assert.AreEqual("\\d{6}", phoneNumberDesc.PossibleNumberPattern);
        }

        // Tests LoadGeneralDesc().
        [Test]
        public void TestLoadGeneralDescSetsSameMobileAndFixedLinePattern()
        {
            String xmlInput =
                "<territory countryCode=\"33\">" +
                "  <fixedLine><nationalNumberPattern>\\d{6}</nationalNumberPattern></fixedLine>" +
                "  <mobile><nationalNumberPattern>\\d{6}</nationalNumberPattern></mobile>" +
                "</territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            // Should set sameMobileAndFixedPattern to true.
            BuildMetadataFromXml.LoadGeneralDesc(metadata, territoryElement);
            Assert.True(metadata.SameMobileAndFixedLinePattern);
        }

        [Test]
        public void TestLoadGeneralDescSetsAllDescriptions()
        {
            String xmlInput =
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
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadGeneralDesc(metadata, territoryElement);
            Assert.AreEqual("\\d{1}", metadata.FixedLine.NationalNumberPattern);
            Assert.AreEqual("\\d{2}", metadata.Mobile.NationalNumberPattern);
            Assert.AreEqual("\\d{3}", metadata.Pager.NationalNumberPattern);
            Assert.AreEqual("\\d{4}", metadata.TollFree.NationalNumberPattern);
            Assert.AreEqual("\\d{5}", metadata.PremiumRate.NationalNumberPattern);
            Assert.AreEqual("\\d{6}", metadata.SharedCost.NationalNumberPattern);
            Assert.AreEqual("\\d{7}", metadata.PersonalNumber.NationalNumberPattern);
            Assert.AreEqual("\\d{8}", metadata.Voip.NationalNumberPattern);
            Assert.AreEqual("\\d{9}", metadata.Uan.NationalNumberPattern);
        }
    }
}