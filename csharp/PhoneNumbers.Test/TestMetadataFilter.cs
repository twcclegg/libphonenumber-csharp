/*
 *  Copyright (C) 2016 The Libphonenumber Authors
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *  http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PhoneNumbers.Test
{


/**
 * Unit tests for {@link MetadataFilter}.
 */
    public class MetadataFilterTest
    {
        private const string ID = "AM";
        private const int COUNTRY_CODE = 374;
        private const string INTERNATIONAL_PREFIX = "0[01]";
        private const string PREFERRED_INTERNATIONAL_PREFIX = "00";
        private const string NATIONAL_NUMBER_PATTERN = "\\d{8}";
        private static readonly int[] PossibleLengths = {8};
        private static readonly int[] PossibleLengthsLocalOnly = {5, 6};
        private const string EXAMPLE_NUMBER = "10123456";

        // If this behavior changes then consider whether the change in the blacklist is intended, or you
        // should change the special build configuration. Also look into any change in the size of the
        // build.
        [Fact]
        public void TestForLiteBuild()
        {
            var blacklist = new Dictionary<string, SortedSet<string>>
            {
                { "fixedLine", new SortedSet<string> { "exampleNumber" } },
                { "mobile", new SortedSet<string> { "exampleNumber" } },
                { "tollFree", new SortedSet<string> { "exampleNumber" } },
                { "premiumRate", new SortedSet<string> { "exampleNumber" } },
                { "sharedCost", new SortedSet<string> { "exampleNumber" } },
                { "personalNumber", new SortedSet<string> { "exampleNumber" } },
                { "voip", new SortedSet<string> { "exampleNumber" } },
                { "pager", new SortedSet<string> { "exampleNumber" } },
                { "uan", new SortedSet<string> { "exampleNumber" } },
                { "emergency", new SortedSet<string> { "exampleNumber" } },
                { "voicemail", new SortedSet<string> { "exampleNumber" } },
                { "shortCode", new SortedSet<string> { "exampleNumber" } },
                { "standardRate", new SortedSet<string> { "exampleNumber" } },
                { "carrierSpecific", new SortedSet<string> { "exampleNumber" } },
                { "smsServices", new SortedSet<string> { "exampleNumber" } },
                { "noInternationalDialling", new SortedSet<string> { "exampleNumber" } }
            };

            Assert.Equal(MetadataFilter.ForLiteBuild(), new MetadataFilter(blacklist));
        }

// If this behavior changes then consider whether the change in the blacklist is intended, or you
// should change the special build configuration. Also look into any change in the size of the
// build.
        [Fact]
        public void TestForSpecialBuild()
        {
            var blacklist = new Dictionary<string, SortedSet<string>>
            {
                { "fixedLine", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "tollFree", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "premiumRate", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "sharedCost", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "personalNumber", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "voip", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "pager", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "uan", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "emergency", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "voicemail", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "shortCode", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "standardRate", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "carrierSpecific", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "smsServices", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                {
                    "noInternationalDialling",
                    new SortedSet<string>(MetadataFilter.ExcludableChildFields)
                },
                { "preferredInternationalPrefix", new SortedSet<string>() },
                { "nationalPrefix", new SortedSet<string>() },
                { "preferredExtnPrefix", new SortedSet<string>() },
                { "nationalPrefixTransformRule", new SortedSet<string>() },
                { "sameMobileAndFixedLinePattern", new SortedSet<string>() },
                { "mainCountryForCode", new SortedSet<string>() },
                { "mobileNumberPortableRegion", new SortedSet<string>() }
            };

            Assert.Equal(MetadataFilter.ForSpecialBuild(), new MetadataFilter(blacklist));
        }

        [Fact]
        public void TestEmptyFilter()
        {
            Assert.Equal(MetadataFilter.EmptyFilter(),
                new MetadataFilter(new Dictionary<string, SortedSet<string>>()));
        }

        [Fact]
        public void testParseFieldMapFromString_parentAsGroup()
        {
            var fieldMap = new Dictionary<string, SortedSet<string>>
            {
                {
                    "fixedLine",
                    new SortedSet<string>(new List<string>
            {
                "nationalNumberPattern",
                "possibleLength",
                "possibleLengthLocalOnly",
                "exampleNumber"
            })
                }
            };

            Assert.Equal(MetadataFilter.ParseFieldMapFromString("fixedLine"), fieldMap);
        }

        [Fact]
        public void testParseFieldMapFromString_childAsGroup()
        {
            var fieldMap = new Dictionary<string, SortedSet<string>>
            {
                { "fixedLine", new SortedSet<string> { "exampleNumber" } },
                { "mobile", new SortedSet<string> { "exampleNumber" } },
                { "tollFree", new SortedSet<string> { "exampleNumber" } },
                { "premiumRate", new SortedSet<string> { "exampleNumber" } },
                { "sharedCost", new SortedSet<string> { "exampleNumber" } },
                { "personalNumber", new SortedSet<string> { "exampleNumber" } },
                { "voip", new SortedSet<string> { "exampleNumber" } },
                { "pager", new SortedSet<string> { "exampleNumber" } },
                { "uan", new SortedSet<string> { "exampleNumber" } },
                { "emergency", new SortedSet<string> { "exampleNumber" } },
                { "voicemail", new SortedSet<string> { "exampleNumber" } },
                { "shortCode", new SortedSet<string> { "exampleNumber" } },
                { "standardRate", new SortedSet<string> { "exampleNumber" } },
                { "carrierSpecific", new SortedSet<string> { "exampleNumber" } },
                { "smsServices", new SortedSet<string> { "exampleNumber" } },
                { "noInternationalDialling", new SortedSet<string> { "exampleNumber" } }
            };

            Assert.Equal(MetadataFilter.ParseFieldMapFromString("exampleNumber"), fieldMap);
        }

        [Fact]
        public void testParseFieldMapFromString_childlessFieldAsGroup()
        {
            var fieldMap = new Dictionary<string, SortedSet<string>>
            {
                { "nationalPrefix", new SortedSet<string>() }
            };

            Assert.Equal(MetadataFilter.ParseFieldMapFromString("nationalPrefix"), fieldMap);
        }

        [Fact]
        public void testParseFieldMapFromString_parentWithOneChildAsGroup()
        {
            var fieldMap = new Dictionary<string, SortedSet<string>>
            {
                { "fixedLine", new SortedSet<string> { "exampleNumber" } }
            };

            Assert.Equal(MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber)"), fieldMap);
        }

        [Fact]
        public void testParseFieldMapFromString_parentWithTwoChildrenAsGroup()
        {
            var fieldMap = new Dictionary<string, SortedSet<string>>
            {
                {
                    "fixedLine",
                    new SortedSet<string>(new List<string>
            {
                "exampleNumber",
                "possibleLength"
            })
                }
            };

            Assert.Equal(
                MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber,possibleLength)"),
                fieldMap);
        }

        [Fact]
        public void testParseFieldMapFromString_mixOfGroups()
        {
            var fieldMap = new Dictionary<string, SortedSet<string>>
            {
                {
                    "uan",
                    new SortedSet<string>(new List<string>
            {
                "possibleLength",
                "exampleNumber",
                "possibleLengthLocalOnly",
                "nationalNumberPattern"
            })
                },
                {
                    "pager",
                    new SortedSet<string>(new List<string>
            {
                "exampleNumber",
                "nationalNumberPattern"
            })
                },
                {
                    "fixedLine",
                    new SortedSet<string>(new List<string>
            {
                "nationalNumberPattern",
                "possibleLength",
                "possibleLengthLocalOnly",
                "exampleNumber"
            })
                },
                { "nationalPrefix", new SortedSet<string>() },
                { "mobile", new SortedSet<string> { "nationalNumberPattern" } },
                { "tollFree", new SortedSet<string> { "nationalNumberPattern" } },
                { "premiumRate", new SortedSet<string> { "nationalNumberPattern" } },
                { "sharedCost", new SortedSet<string> { "nationalNumberPattern" } },
                { "personalNumber", new SortedSet<string> { "nationalNumberPattern" } },
                { "voip", new SortedSet<string> { "nationalNumberPattern" } },
                { "emergency", new SortedSet<string> { "nationalNumberPattern" } },
                { "voicemail", new SortedSet<string> { "nationalNumberPattern" } },
                { "shortCode", new SortedSet<string> { "nationalNumberPattern" } },
                { "standardRate", new SortedSet<string> { "nationalNumberPattern" } },
                { "carrierSpecific", new SortedSet<string> { "nationalNumberPattern" } },
                { "smsServices", new SortedSet<string> { "nationalNumberPattern" } },
                {
                    "noInternationalDialling",
                    new SortedSet<string>(new List<string>
            {
                "nationalNumberPattern"
            })
                }
            };

            Assert.Equal(MetadataFilter.ParseFieldMapFromString(
                    "uan(possibleLength,exampleNumber,possibleLengthLocalOnly)"
                    + ":pager(exampleNumber)"
                    + ":fixedLine"
                    + ":nationalPrefix"
                    + ":nationalNumberPattern"),
                fieldMap);
        }

        // Many of the strings in this test may be possible to express in shorter ways with the current
        // sets of excludable fields, but their shortest representation changes as those sets change, as
        // do their semantics; therefore we allow currently longer expressions, and we allow explicit
        // listing of children, even if these are currently all the children        [Fact]
        [Fact]
        public void TestParseFieldMapFromString_EquivalentExpressions()
        {
            // Listing all excludable parent fields is equivalent to listing all excludable child fields.
            Assert.Equal(
                MetadataFilter.ParseFieldMapFromString(
                    "fixedLine"
                    + ":mobile"
                    + ":tollFree"
                    + ":premiumRate"
                    + ":sharedCost"
                    + ":personalNumber"
                    + ":voip"
                    + ":pager"
                    + ":uan"
                    + ":emergency"
                    + ":voicemail"
                    + ":shortCode"
                    + ":standardRate"
                    + ":carrierSpecific"
                    + ":smsServices"
                    + ":noInternationalDialling"),
                MetadataFilter.ParseFieldMapFromString(
                    "nationalNumberPattern"
                    + ":possibleLength"
                    + ":possibleLengthLocalOnly"
                    + ":exampleNumber"));

            // Order and whitespace don't matter.
            Assert.Equal(
                MetadataFilter.ParseFieldMapFromString(
                    " nationalNumberPattern "
                    + ": uan ( exampleNumber , possibleLengthLocalOnly,     possibleLength ) "
                    + ": nationalPrefix "
                    + ": fixedLine "
                    + ": pager ( exampleNumber ) "),
                MetadataFilter.ParseFieldMapFromString(
                    "uan(possibleLength,exampleNumber,possibleLengthLocalOnly)"
                    + ":pager(exampleNumber)"
                    + ":fixedLine"
                    + ":nationalPrefix"
                    + ":nationalNumberPattern"));

            // Parent explicitly listing all possible children.
            Assert.Equal(
                MetadataFilter.ParseFieldMapFromString(
                    "uan(nationalNumberPattern,possibleLength,exampleNumber,possibleLengthLocalOnly)"),
                MetadataFilter.ParseFieldMapFromString("uan"));

            // All parent's children covered, some implicitly and some explicitly.
            Assert.Equal(
                MetadataFilter.ParseFieldMapFromString(
                    "uan(nationalNumberPattern,possibleLength,exampleNumber):possibleLengthLocalOnly"),
                MetadataFilter.ParseFieldMapFromString("uan:possibleLengthLocalOnly"));

            // Child field covered by all parents explicitly.
            // It seems this will always be better expressed as a wildcard child, but the check is complex
            // and may not be worth it.
            Assert.Equal(
                MetadataFilter.ParseFieldMapFromString(
                    "fixedLine(exampleNumber)"
                    + ":mobile(exampleNumber)"
                    + ":tollFree(exampleNumber)"
                    + ":premiumRate(exampleNumber)"
                    + ":sharedCost(exampleNumber)"
                    + ":personalNumber(exampleNumber)"
                    + ":voip(exampleNumber)"
                    + ":pager(exampleNumber)"
                    + ":uan(exampleNumber)"
                    + ":emergency(exampleNumber)"
                    + ":voicemail(exampleNumber)"
                    + ":shortCode(exampleNumber)"
                    + ":standardRate(exampleNumber)"
                    + ":carrierSpecific(exampleNumber)"
                    + ":smsServices(exampleNumber)"
                    + ":noInternationalDialling(exampleNumber)"),
                MetadataFilter.ParseFieldMapFromString("exampleNumber"));

            // Child field given as a group by itself while it's covered by all parents implicitly.
            // It seems this will always be better expressed without the wildcard child, but the check is
            // complex and may not be worth it.
            Assert.Equal(
                MetadataFilter.ParseFieldMapFromString(
                    "fixedLine"
                    + ":mobile"
                    + ":tollFree"
                    + ":premiumRate"
                    + ":sharedCost"
                    + ":personalNumber"
                    + ":voip"
                    + ":pager"
                    + ":uan"
                    + ":emergency"
                    + ":voicemail"
                    + ":shortCode"
                    + ":standardRate"
                    + ":carrierSpecific"
                    + ":smsServices"
                    + ":noInternationalDialling"
                    + ":exampleNumber"),
                MetadataFilter.ParseFieldMapFromString(
                    "fixedLine"
                    + ":mobile"
                    + ":tollFree"
                    + ":premiumRate"
                    + ":sharedCost"
                    + ":personalNumber"
                    + ":voip"
                    + ":pager"
                    + ":uan"
                    + ":emergency"
                    + ":voicemail"
                    + ":shortCode"
                    + ":standardRate"
                    + ":carrierSpecific"
                    + ":smsServices"
                    + ":noInternationalDialling"));
        }

        [Fact]
        public void testParseFieldMapFromString_RuntimeExceptionCases()
        {
            // Null input.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString(null));

            // Empty input.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString(""));

            // Whitespace input.
            try
            {
                MetadataFilter.ParseFieldMapFromString(" ");
                Assert.True(false);
            }
            catch (Exception)
            {
                // Test passed.
            }

            // Bad token given as only group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("something_else"));

            // Bad token given as last group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine:something_else"));

            // Bad token given as middle group.
            try
            {
                MetadataFilter.ParseFieldMapFromString(
                    "pager:nationalPrefix:something_else:nationalNumberPattern");
                Assert.True(false);
            }
            catch (Exception)
            {
                // Test passed.
            }

            // Childless field given as parent.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("nationalPrefix(exampleNumber)"));

            // Child field given as parent.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("possibleLength(exampleNumber)"));

            // Bad token given as parent.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("something_else(exampleNumber)"));

            // Parent field given as only child.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(uan)"));

            // Parent field given as first child.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(uan,possibleLength)"));

            // Parent field given as last child.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(possibleLength,uan)"));

            // Parent field given as middle child.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(possibleLength,uan,exampleNumber)"));

            // Childless field given as only child.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(nationalPrefix)"));

            // Bad token given as only child.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(something_else)"));

            // Bad token given as last child.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("uan(possibleLength,something_else)"));

            // Empty parent.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("(exampleNumber)"));

            // Whitespace parent.
            try
            {
                MetadataFilter.ParseFieldMapFromString(" (exampleNumber)");
                Assert.True(false);
            }
            catch (Exception)
            {
                // Test passed.
            }

            // Empty child.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine()"));

            // Whitespace child.
            try
            {
                MetadataFilter.ParseFieldMapFromString("fixedLine( )");
                Assert.True(false);
            }
            catch (Exception)
            {
                // Test passed.
            }

            // Empty parent and child.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("()"));

            // Whitespace parent and empty child.
            try
            {
                MetadataFilter.ParseFieldMapFromString(" ()");
                Assert.True(false);
            }
            catch (Exception)
            {
                // Test passed.
            }

            // Parent field given as a group twice.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine:uan:fixedLine"));

            // Parent field given as the parent of a group and as a group by itself.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber):fixedLine"));

            // Parent field given as the parent of one group and then as the parent of another group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber):fixedLine(possibleLength)"));

            // Childless field given twice as a group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("nationalPrefix:uan:nationalPrefix"));

            // Child field given twice as a group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("exampleNumber:uan:exampleNumber"));

            // Child field given first as the only child in a group and then as a group by itself.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber):exampleNumber"));

            // Child field given first as a child in a group and then as a group by itself.
            Assert.Throws<Exception>(() =>
                MetadataFilter.ParseFieldMapFromString(
                    "uan(nationalNumberPattern,possibleLength,exampleNumber)"
                    + ":possibleLengthLocalOnly"
                    + ":exampleNumber"));

            // Child field given twice as children of the same parent.
            Assert.Throws<Exception>(() =>
                MetadataFilter.ParseFieldMapFromString(
                    "fixedLine(possibleLength,exampleNumber,possibleLength)"));

            // Child field given as a group by itself while it's covered by all parents explicitly.
            Assert.Throws<Exception>(() =>
                MetadataFilter.ParseFieldMapFromString(
                    "fixedLine(exampleNumber)"
                    + ":mobile(exampleNumber)"
                    + ":tollFree(exampleNumber)"
                    + ":premiumRate(exampleNumber)"
                    + ":sharedCost(exampleNumber)"
                    + ":personalNumber(exampleNumber)"
                    + ":voip(exampleNumber)"
                    + ":pager(exampleNumber)"
                    + ":uan(exampleNumber)"
                    + ":emergency(exampleNumber)"
                    + ":voicemail(exampleNumber)"
                    + ":shortCode(exampleNumber)"
                    + ":standardRate(exampleNumber)"
                    + ":carrierSpecific(exampleNumber)"
                    + ":noInternationalDialling(exampleNumber)"
                    + ":exampleNumber"));

            // Child field given as a group by itself while it's covered by all parents, some implicitly and
            // some explicitly.
            Assert.Throws<Exception>(() =>
                MetadataFilter.ParseFieldMapFromString(
                    "fixedLine"
                    + ":mobile"
                    + ":tollFree"
                    + ":premiumRate"
                    + ":sharedCost"
                    + ":personalNumber"
                    + ":voip"
                    + ":pager(exampleNumber)"
                    + ":uan(exampleNumber)"
                    + ":emergency(exampleNumber)"
                    + ":voicemail(exampleNumber)"
                    + ":shortCode(exampleNumber)"
                    + ":standardRate(exampleNumber)"
                    + ":carrierSpecific(exampleNumber)"
                    + ":smsServices"
                    + ":noInternationalDialling(exampleNumber)"
                    + ":exampleNumber"));

            // Missing right parenthesis in only group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber"));

            // Missing right parenthesis in first group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber:pager"));

            // Missing left parenthesis in only group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLineexampleNumber)"));

            // Early right parenthesis in only group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(example_numb)er"));

            // Extra right parenthesis at end of only group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber))"));

            // Extra right parenthesis between proper parentheses.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(example_numb)er)"));

            // Extra left parenthesis in only group.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine((exampleNumber)"));

            // Extra level of children.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber(possibleLength))"));

            // Trailing comma in children.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(exampleNumber,)"));

            // Leading comma in children.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(,exampleNumber)"));

            // Empty token between commas.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("fixedLine(possibleLength,,exampleNumber)"));

            // Trailing colon.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("uan:"));

            // Leading colon.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString(":uan"));

            // Empty token between colons.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("uan::fixedLine"));

            // Missing colon between groups.
            Assert.Throws<Exception>(() => MetadataFilter.ParseFieldMapFromString("uan(possibleLength)pager"));
        }

        [Fact]
        public void testComputeComplement_allAndNothing()
        {
            var map1 = new Dictionary<string, SortedSet<string>>
            {
                { "fixedLine", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "mobile", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "tollFree", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "premiumRate", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "sharedCost", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "personalNumber", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "voip", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "pager", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "uan", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "emergency", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "voicemail", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "shortCode", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "standardRate", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "carrierSpecific", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "smsServices", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                {
                    "noInternationalDialling",
                    new SortedSet<string>(MetadataFilter.ExcludableChildFields)
                },
                { "preferredInternationalPrefix", new SortedSet<string>() },
                { "nationalPrefix", new SortedSet<string>() },
                { "preferredExtnPrefix", new SortedSet<string>() },
                { "nationalPrefixTransformRule", new SortedSet<string>() },
                { "sameMobileAndFixedLinePattern", new SortedSet<string>() },
                { "mainCountryForCode", new SortedSet<string>() },
                { "mobileNumberPortableRegion", new SortedSet<string>() }
            };

            var map2 = new Dictionary<string, SortedSet<string>>();

            Assert.Equal(MetadataFilter.ComputeComplement(map1), map2);
            Assert.Equal(MetadataFilter.ComputeComplement(map2), map1);
        }

        [Fact]
        public void testComputeComplement_inBetween()
        {
            var map1 = new Dictionary<string, SortedSet<string>>
            {
                { "fixedLine", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "mobile", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "tollFree", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "premiumRate", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "emergency", new SortedSet<string> { "nationalNumberPattern" } },
                { "voicemail", new SortedSet<string>(new List<string> { "possibleLength", "exampleNumber" }) },
                { "shortCode", new SortedSet<string> { "exampleNumber" } },
                { "standardRate", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "carrierSpecific", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "smsServices", new SortedSet<string> { "nationalNumberPattern" } },
                {
                    "noInternationalDialling",
                    new SortedSet<string>(MetadataFilter.ExcludableChildFields)
                },
                { "nationalPrefixTransformRule", new SortedSet<string>() },
                { "sameMobileAndFixedLinePattern", new SortedSet<string>() },
                { "mainCountryForCode", new SortedSet<string>() },
                { "mobileNumberPortableRegion", new SortedSet<string>() }
            };

            var map2 = new Dictionary<string, SortedSet<string>>
            {
                { "sharedCost", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "personalNumber", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "voip", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "pager", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "uan", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                {
                    "emergency",
                    new SortedSet<string>(new List<string>
            {
                "possibleLength",
                "possibleLengthLocalOnly",
                "exampleNumber"
            })
                },
                {
                    "smsServices",
                    new SortedSet<string>(new List<string>
            {
                "possibleLength",
                "possibleLengthLocalOnly",
                "exampleNumber"
            })
                },
                {
                    "voicemail",
                    new SortedSet<string>(new List<string>
            {
                "nationalNumberPattern",
                "possibleLengthLocalOnly"
            })
                },
                {
                    "shortCode",
                    new SortedSet<string>(new List<string>
            {
                "nationalNumberPattern",
                "possibleLength",
                "possibleLengthLocalOnly"
            })
                },
                { "preferredInternationalPrefix", new SortedSet<string>() },
                { "nationalPrefix", new SortedSet<string>() },
                { "preferredExtnPrefix", new SortedSet<string>() }
            };

            Assert.Equal(MetadataFilter.ComputeComplement(map1), map2);
            Assert.Equal(MetadataFilter.ComputeComplement(map2), map1);
        }

        [Fact]
        public void TestShouldDrop()
        {
            var blacklist = new Dictionary<string, SortedSet<string>>
            {
                { "fixedLine", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "mobile", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "tollFree", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "premiumRate", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "emergency", new SortedSet<string> { "nationalNumberPattern" } },
                {
                    "voicemail",
                    new SortedSet<string>(new List<string>
            {
                "possibleLength",
                "exampleNumber"
            })
                },
                { "shortCode", new SortedSet<string> { "exampleNumber" } },
                { "standardRate", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "carrierSpecific", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                { "smsServices", new SortedSet<string>(MetadataFilter.ExcludableChildFields) },
                {
                    "noInternationalDialling",
                    new SortedSet<string>(MetadataFilter.ExcludableChildFields)
                },
                { "nationalPrefixTransformRule", new SortedSet<string>() },
                { "sameMobileAndFixedLinePattern", new SortedSet<string>() },
                { "mainCountryForCode", new SortedSet<string>() },
                { "mobileNumberPortableRegion", new SortedSet<string>() }
            };

            var filter = new MetadataFilter(blacklist);
            Assert.True(filter.ShouldDrop("fixedLine", "exampleNumber"));
            Assert.False(filter.ShouldDrop("sharedCost", "exampleNumber"));
            Assert.False(filter.ShouldDrop("emergency", "exampleNumber"));
            Assert.True(filter.ShouldDrop("emergency", "nationalNumberPattern"));
            Assert.False(filter.ShouldDrop("preferredInternationalPrefix"));
            Assert.True(filter.ShouldDrop("mobileNumberPortableRegion"));
            Assert.True(filter.ShouldDrop("smsServices", "nationalNumberPattern"));

            // Integration tests starting with flag values.
            Assert.True(BuildMetadataFromXml.GetMetadataFilter(true, false)
                .ShouldDrop("fixedLine", "exampleNumber"));

            // Integration tests starting with blacklist strings.
            Assert.True(new MetadataFilter(MetadataFilter.ParseFieldMapFromString("fixedLine"))
                .ShouldDrop("fixedLine", "exampleNumber"));
            Assert.False(new MetadataFilter(MetadataFilter.ParseFieldMapFromString("uan"))
                .ShouldDrop("fixedLine", "exampleNumber"));

            // Integration tests starting with whitelist strings.
            Assert.False(new MetadataFilter(MetadataFilter.ComputeComplement(
                    MetadataFilter.ParseFieldMapFromString("exampleNumber")))
                .ShouldDrop("fixedLine", "exampleNumber"));
            Assert.True(new MetadataFilter(MetadataFilter.ComputeComplement(
                MetadataFilter.ParseFieldMapFromString("uan"))).ShouldDrop("fixedLine", "exampleNumber"));

            // Integration tests with an empty blacklist.
            Assert.False(new MetadataFilter(new Dictionary<string, SortedSet<string>>())
                .ShouldDrop("fixedLine", "exampleNumber"));
        }

        // Test that a fake PhoneMetadata filtered for liteBuild ends up clearing exactly the expected
        // fields. The lite build is used to clear example_number fields from all PhoneNumberDescs        [Fact]
        [Fact]
        public void TestFilterMetadata_LiteBuild()
        {
            var metadata = FakeArmeniaPhoneMetadata();

            MetadataFilter.ForLiteBuild().FilterMetadata(metadata.MessageBeingBuilt);

            // id, country_code, and international_prefix should never be cleared.
            Assert.Equal(ID, metadata.Id);
            Assert.Equal(COUNTRY_CODE, metadata.CountryCode);
            Assert.Equal(INTERNATIONAL_PREFIX, metadata.InternationalPrefix);

            // preferred_international_prefix should not be cleared in liteBuild.
            Assert.Equal(PREFERRED_INTERNATIONAL_PREFIX, metadata.PreferredInternationalPrefix);

            // All PhoneNumberDescs must have only example_number cleared.
            foreach (var desc in new List<PhoneNumberDesc> {
                metadata.GeneralDesc,
                metadata.FixedLine,
                metadata.Mobile,
                metadata.TollFree})
            {
                Assert.Equal(NATIONAL_NUMBER_PATTERN, desc.NationalNumberPattern);
                Assert.True(ContentsEqual(PossibleLengths, desc.PossibleLengthList.ToList()));
                Assert.True(ContentsEqual(PossibleLengthsLocalOnly, desc.PossibleLengthLocalOnlyList.ToList()));
                Assert.False(desc.HasExampleNumber);
            }
        }

        // Test that a fake PhoneMetadata filtered for specialBuild ends up clearing exactly the expected
        // fields. The special build is used to clear PhoneNumberDescs other than general_desc and mobile,
        // and non-PhoneNumberDesc PhoneMetadata fields that aren't needed for parsing        [Fact]
        [Fact]
        public void TestFilterMetadata_SpecialBuild()
        {
            var metadata = FakeArmeniaPhoneMetadata();

            MetadataFilter.ForSpecialBuild().FilterMetadata(metadata.MessageBeingBuilt);

            // id, country_code, and international_prefix should never be cleared.
            Assert.Equal(ID, metadata.Id);
            Assert.Equal(COUNTRY_CODE, metadata.CountryCode);
            Assert.Equal(INTERNATIONAL_PREFIX, metadata.InternationalPrefix);

            // preferred_international_prefix should be cleared in specialBuild.
            Assert.False(metadata.HasPreferredInternationalPrefix);

            // general_desc should have all fields but example_number; mobile should have all fields.
            foreach (var desc in new List<PhoneNumberDesc>
            {
                metadata.GeneralDesc,
                metadata.Mobile
            })
            {
                Assert.Equal(NATIONAL_NUMBER_PATTERN, desc.NationalNumberPattern);
                Assert.True(ContentsEqual(PossibleLengths, desc.PossibleLengthList.ToList()));
                Assert.True(ContentsEqual(PossibleLengthsLocalOnly, desc.PossibleLengthLocalOnlyList.ToList()));
            }

            Assert.False(metadata.GeneralDesc.HasExampleNumber);
            Assert.Equal(EXAMPLE_NUMBER, metadata.Mobile.ExampleNumber);

            // All other PhoneNumberDescs must have all fields cleared.
            foreach (var desc in new List<PhoneNumberDesc>
            {
                metadata.FixedLine,
                metadata.TollFree
            })
            {
                Assert.False(desc.HasNationalNumberPattern);
                Assert.Empty(desc.PossibleLengthList);
                Assert.Empty(desc.PossibleLengthLocalOnlyList);
                Assert.False(desc.HasExampleNumber);
            }
        }


        // Test that filtering a fake PhoneMetadata with the empty MetadataFilter results in no change        [Fact]
        [Fact]
        public void TestFilterMetadata_EmptyFilter()
        {
            var metadata = FakeArmeniaPhoneMetadata();

            MetadataFilter.EmptyFilter().FilterMetadata(metadata.MessageBeingBuilt);

            // None of the fields should be cleared.
            Assert.Equal(ID, metadata.Id);
            Assert.Equal(COUNTRY_CODE, metadata.CountryCode);
            Assert.Equal(INTERNATIONAL_PREFIX, metadata.InternationalPrefix);
            Assert.Equal(PREFERRED_INTERNATIONAL_PREFIX, metadata.PreferredInternationalPrefix);
            foreach (var desc in new List<PhoneNumberDesc> {
                metadata.GeneralDesc,
                metadata.FixedLine,
                metadata.Mobile,
                metadata.TollFree})
            {
                Assert.Equal(NATIONAL_NUMBER_PATTERN, desc.NationalNumberPattern);
                Assert.True(ContentsEqual(PossibleLengths, desc.PossibleLengthList.ToList()));
                Assert.True(ContentsEqual(PossibleLengthsLocalOnly, desc.PossibleLengthLocalOnlyList.ToList()));
            }

            Assert.False(metadata.GeneralDesc.HasExampleNumber);
            Assert.Equal(EXAMPLE_NUMBER, metadata.FixedLine.ExampleNumber);
            Assert.Equal(EXAMPLE_NUMBER, metadata.Mobile.ExampleNumber);
            Assert.Equal(EXAMPLE_NUMBER, metadata.TollFree.ExampleNumber);
        }

        [Fact]
        public void TestIntegrityOfFieldSets()
        {
            var union = new List<string>()
                .Union(MetadataFilter.ExcludableParentFields)
                .Union(MetadataFilter.ExcludableChildFields)
                .Union(MetadataFilter.ExcludableChildlessFields).ToList();

            // Mutually exclusive sets.
            Assert.True(union.Count == MetadataFilter.ExcludableParentFields.Count
                        + MetadataFilter.ExcludableChildFields.Count
                        + MetadataFilter.ExcludableChildlessFields.Count);

            // Nonempty sets.
            Assert.True(MetadataFilter.ExcludableParentFields.Count > 0
                        && MetadataFilter.ExcludableChildFields.Count > 0
                        && MetadataFilter.ExcludableChildlessFields.Count > 0);

            // Nonempty and canonical field names.
            foreach (var field in union)
            {
                Assert.True(field.Length > 0 && field.Trim().Equals(field));
            }
        }

        [Fact]
        public void TestEquals_WhenNull_ReturnsFalse()
        {
            var result = new MetadataFilter(new Dictionary<string, SortedSet<string>>()).Equals(null);
            Assert.False(result);
        }

        private static PhoneMetadata.Builder FakeArmeniaPhoneMetadata()
        {
            var metadata = new PhoneMetadata.Builder();
            metadata.SetId(ID);
            metadata.SetCountryCode(COUNTRY_CODE);
            metadata.SetInternationalPrefix(INTERNATIONAL_PREFIX);
            metadata.SetPreferredInternationalPrefix(PREFERRED_INTERNATIONAL_PREFIX);
            metadata.SetGeneralDesc(GetFakeArmeniaPhoneNumberDesc(true));
            metadata.SetFixedLine(GetFakeArmeniaPhoneNumberDesc(false));
            metadata.SetMobile(GetFakeArmeniaPhoneNumberDesc(false));
            metadata.SetTollFree(GetFakeArmeniaPhoneNumberDesc(false));
            return metadata;
        }

        private static PhoneNumberDesc GetFakeArmeniaPhoneNumberDesc(bool generalDesc)
        {
            var desc = new PhoneNumberDesc.Builder().SetNationalNumberPattern(NATIONAL_NUMBER_PATTERN);
            if (!generalDesc)
            {
                desc.SetExampleNumber(EXAMPLE_NUMBER);
            }

            foreach (var i in PossibleLengths)
            {
                desc.AddPossibleLength(i);
            }

            foreach (var i in PossibleLengthsLocalOnly)
            {
                desc.AddPossibleLengthLocalOnly(i);
            }

            return desc.Build();
        }

        private static bool ContentsEqual(IReadOnlyList<int> list, IReadOnlyList<int> array)
        {
            if (list.Count != array.Count)
                return false;
            return !list.Where((t, i) => t != array[i]).Any();
        }
    }
}
