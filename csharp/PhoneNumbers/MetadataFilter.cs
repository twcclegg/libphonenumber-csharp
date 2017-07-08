﻿/*
 * Copyright (C) 2016 The Libphonenumber Authors
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

namespace PhoneNumbers
{

    /**
     * Class to encapsulate the metadata filtering logic and restrict visibility into raw data
     * structures.
     *
     * <p>
     * WARNING: This is an internal API which is under development and subject to backwards-incompatible
     * changes without notice. Any changes are not guaranteed to be reflected in the versioning scheme
     * of the public API, nor in release notes.
     */
    public class MetadataFilter
    {
        // The following 3 sets comprise all the PhoneMetadata fields as defined at phonemetadata.proto
        // which may be excluded from customized serializations of the binary metadata. Fields that are
        // core to the library functionality may not be listed here.
        // excludableParentFields are PhoneMetadata fields of type PhoneNumberDesc.
        // excludableChildFields are PhoneNumberDesc fields of primitive type.
        // excludableChildlessFields are PhoneMetadata fields of primitive type.
        // Currently we support only one non-primitive type and the depth of the "family tree" is 2,
        // meaning a field may have only direct descendants, who may not have descendants of their own. If
        // this changes, the blacklist handling in this class should also change.
        // @VisibleForTesting
        private static readonly SortedSet<String> excludableParentFields = new SortedSet<String>
        {
            "fixedLine",
            "mobile",
            "tollFree",
            "premiumRate",
            "sharedCost",
            "personalNumber",
            "voip",
            "pager",
            "uan",
            "emergency",
            "voicemail",
            "shortCode",
            "standardRate",
            "carrierSpecific",
            "noInternationalDialling"
        };

        // Note: If this set changes, the descHasData implementation must change in PhoneNumberUtil.
        // The current implementation assumes that all PhoneNumberDesc fields are present here, since it
        // "clears" a PhoneNumberDesc field by simply clearing all of the fields under it. See the comment
        // above, about all 3 sets, for more about these fields.
        // @VisibleForTesting
        private static readonly SortedSet<String> excludableChildFields = new SortedSet<String>
        {
            "nationalNumberPattern",
            "possibleLength",
            "possibleLengthLocalOnly",
            "exampleNumber"
        };

        // @VisibleForTesting
        private static readonly SortedSet<String> excludableChildlessFields = new SortedSet<String>
        {
            "preferredInternationalPrefix",
            "nationalPrefix",
            "preferredExtnPrefix",
            "nationalPrefixTransformRule",
            "sameMobileAndFixedLinePattern",
            "mainCountryForCode",
            "leadingZeroPossible",
            "mobileNumberPortableRegion"
        };

        private Dictionary<String, SortedSet<String>> blacklist;

        // Note: If changing the blacklist here or the name of the method, update documentation about
        // affected methods at the same time:
        // https://github.com/googlei18n/libphonenumber/blob/master/FAQ.md#what-is-the-metadatalitejsmetadata_lite-option
        internal static MetadataFilter ForLiteBuild()
        {
            // "exampleNumber" is a blacklist.
            return new MetadataFilter(ParseFieldMapFromString("exampleNumber"));
        }

        internal static MetadataFilter ForSpecialBuild()
        {
            // "mobile" is a whitelist.
            return new MetadataFilter(ComputeComplement(ParseFieldMapFromString("mobile")));
        }

        internal static MetadataFilter EmptyFilter()
        {
            // Empty blacklist, meaning we filter nothing.
            return new MetadataFilter(new Dictionary<String, SortedSet<String>>());
        }

        // @VisibleForTesting
        MetadataFilter(Dictionary<String, SortedSet<String>> blacklist)
        {
            this.blacklist = blacklist;
        }

        public override bool Equals(Object obj)
        {
            return blacklist.Equals(((MetadataFilter) obj).blacklist);
        }

        public override int GetHashCode()
        {
            return blacklist.GetHashCode();
        }

        /**
         * Clears certain fields in {@code metadata} as defined by the {@code MetadataFilter} instance.
         * Note that this changes the mutable {@code metadata} object, and is not thread-safe. If this
         * method does not return successfully, do not assume {@code metadata} has not changed.
         *
         * @param metadata  The {@code PhoneMetadata} object to be filtered
         */
        internal void FilterMetadata(PhoneMetadata.Builder metadata)
        {
            // TODO: Consider clearing if the filtered PhoneNumberDesc is empty.
            if (metadata.HasFixedLine)
            {
                metadata.SetFixedLine(GetFiltered("fixedLine", metadata.FixedLine));
            }
            if (metadata.HasMobile)
            {
                metadata.SetMobile(GetFiltered("mobile", metadata.Mobile));
            }
            if (metadata.HasTollFree)
            {
                metadata.SetTollFree(GetFiltered("tollFree", metadata.TollFree));
            }
            if (metadata.HasPremiumRate)
            {
                metadata.SetPremiumRate(GetFiltered("premiumRate", metadata.PremiumRate));
            }
            if (metadata.HasSharedCost)
            {
                metadata.SetSharedCost(GetFiltered("sharedCost", metadata.SharedCost));
            }
            if (metadata.HasPersonalNumber)
            {
                metadata.SetPersonalNumber(GetFiltered("personalNumber", metadata.PersonalNumber));
            }
            if (metadata.HasVoip)
            {
                metadata.SetVoip(GetFiltered("voip", metadata.Voip));
            }
            if (metadata.HasPager)
            {
                metadata.SetPager(GetFiltered("pager", metadata.Pager));
            }
            if (metadata.HasUan)
            {
                metadata.SetUan(GetFiltered("uan", metadata.Uan));
            }
            if (metadata.HasEmergency)
            {
                metadata.SetEmergency(GetFiltered("emergency", metadata.Emergency));
            }
            if (metadata.HasVoicemail)
            {
                metadata.SetVoicemail(GetFiltered("voicemail", metadata.Voicemail));
            }
            if (metadata.HasShortCode)
            {
                metadata.SetShortCode(GetFiltered("shortCode", metadata.ShortCode));
            }
            if (metadata.HasStandardRate)
            {
                metadata.SetStandardRate(GetFiltered("standardRate", metadata.StandardRate));
            }
            if (metadata.HasCarrierSpecific)
            {
                metadata.SetCarrierSpecific(GetFiltered("carrierSpecific", metadata.CarrierSpecific));
            }
            if (metadata.HasNoInternationalDialling)
            {
                metadata.SetNoInternationalDialling(GetFiltered("noInternationalDialling",
                    metadata.NoInternationalDialling));
            }

            if (ShouldDrop("preferredInternationalPrefix"))
            {
                metadata.ClearPreferredInternationalPrefix();
            }
            if (ShouldDrop("nationalPrefix"))
            {
                metadata.ClearNationalPrefix();
            }
            if (ShouldDrop("preferredExtnPrefix"))
            {
                metadata.ClearPreferredExtnPrefix();
            }
            if (ShouldDrop("nationalPrefixTransformRule"))
            {
                metadata.ClearNationalPrefixTransformRule();
            }
            if (ShouldDrop("sameMobileAndFixedLinePattern"))
            {
                metadata.ClearSameMobileAndFixedLinePattern();
            }
            if (ShouldDrop("mainCountryForCode"))
            {
                metadata.ClearMainCountryForCode();
            }
            if (ShouldDrop("leadingZeroPossible"))
            {
                metadata.ClearLeadingZeroPossible();
            }
            if (ShouldDrop("mobileNumberPortableRegion"))
            {
                metadata.ClearMobileNumberPortableRegion();
            }
        }

        /**
         * The input blacklist or whitelist string is expected to be of the form "a(b,c):d(e):f", where
         * b and c are children of a, e is a child of d, and f is either a parent field, a child field, or
         * a childless field. Order and whitespace don't matter. We throw Exception for any
         * duplicates, malformed strings, or strings where field tokens do not correspond to strings in
         * the sets of excludable fields. We also throw Exception for empty strings since such
         * strings should be treated as a special case by the flag checking code and not passed here.
         */
        // @VisibleForTesting
        static Dictionary<String, SortedSet<String>> ParseFieldMapFromString(String str)
        {
            if (str == null)
            {
                throw new Exception("Null string should not be passed to ParseFieldMapFromString");
            }
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new Exception("Null nor empty string should not be passed to ParseFieldMapFromString");
            }

            Dictionary<String, SortedSet<String>> fieldMap = new Dictionary<String, SortedSet<String>>();
            SortedSet<String> wildcardChildren = new SortedSet<String>();
            foreach (String group in str.Split(':'))
            {
                int leftParenIndex = group.IndexOf('(');
                int rightParenIndex = group.IndexOf(')');
                if (leftParenIndex < 0 && rightParenIndex < 0)
                {
                    if (excludableParentFields.Contains(group))
                    {
                        if (fieldMap.ContainsKey(group))
                        {
                            throw new Exception(group + " given more than once in " + str);
                        }
                        fieldMap.Add(group, new SortedSet<String>(excludableChildFields));
                    }
                    else if (excludableChildlessFields.Contains(group))
                    {
                        if (fieldMap.ContainsKey(group))
                        {
                            throw new Exception(group + " given more than once in " + str);
                        }
                        fieldMap.Add(group, new SortedSet<String>());
                    }
                    else if (excludableChildFields.Contains(group))
                    {
                        if (wildcardChildren.Contains(group))
                        {
                            throw new Exception(group + " given more than once in " + str);
                        }
                        wildcardChildren.Add(group);
                    }
                    else
                    {
                        throw new Exception(group + " is not a valid token");
                    }
                }
                else if (leftParenIndex > 0 && rightParenIndex == group.Length - 1)
                {
                    // We don't check for duplicate parentheses or illegal characters since these will be caught
                    // as not being part of valid field tokens.
                    String parent = group.Substring(0, leftParenIndex);
                    if (!excludableParentFields.Contains(parent))
                    {
                        throw new Exception(parent + " is not a valid parent token");
                    }
                    if (fieldMap.ContainsKey(parent))
                    {
                        throw new Exception(parent + " given more than once in " + str);
                    }
                    SortedSet<String> children = new SortedSet<String>();
                    foreach (String child in group.Substring(leftParenIndex + 1, rightParenIndex - leftParenIndex)
                        .Split(','))
                    {
                        if (!excludableChildFields.Contains(child))
                        {
                            throw new Exception(child + " is not a valid child token");
                        }
                        if (!children.Add(child))
                        {
                            throw new Exception(child + " given more than once in " + group);
                        }
                    }
                    fieldMap.Add(parent, children);
                }
                else
                {
                    throw new Exception("Incorrect location of parantheses in " + group);
                }
            }
            foreach (String wildcardChild in wildcardChildren)
            {
                foreach (String parent in excludableParentFields)
                {
                    SortedSet<String> children = fieldMap[parent];
                    if (children == null)
                    {
                        children = new SortedSet<String>();
                        fieldMap.Add(parent, children);
                    }
                    if (!children.Add(wildcardChild)
                        && fieldMap[parent].Count != excludableChildFields.Count)
                    {
                        // The map already Contains parent -> wildcardChild but not all possible children.
                        // So wildcardChild was given explicitly as a child of parent, which is a duplication
                        // since it's also given as a wildcard child.
                        throw new Exception(
                            wildcardChild + " is present by itself so remove it from " + parent + "'s group");
                    }
                }
            }
            return fieldMap;
        }

        // Does not check that legal tokens are used, assuming that fieldMap is constructed using
        // ParseFieldMapFromString(String) which does check. If fieldMap Contains illegal tokens or parent
        // fields with no children or other unexpected state, the behavior of this function is undefined.
        // @VisibleForTesting
        static Dictionary<String, SortedSet<String>> ComputeComplement(
            Dictionary<String, SortedSet<String>> fieldMap)
        {
            Dictionary<String, SortedSet<String>> complement = new Dictionary<String, SortedSet<String>>();
            foreach (String parent in excludableParentFields)
            {
                if (!fieldMap.ContainsKey(parent))
                {
                    complement.Add(parent, new SortedSet<String>(excludableChildFields));
                }
                else
                {
                    SortedSet<String> otherChildren = fieldMap[parent];
                    // If the other map has all the children for this parent then we don't want to include the
                    // parent as a key.
                    if (otherChildren.Count != excludableChildFields.Count)
                    {
                        SortedSet<String> children = new SortedSet<String>();
                        foreach (String child in excludableChildFields)
                        {
                            if (!otherChildren.Contains(child))
                            {
                                children.Add(child);
                            }
                        }
                        complement.Add(parent, children);
                    }
                }
            }
            foreach (String childlessField in excludableChildlessFields)
            {
                if (!fieldMap.ContainsKey(childlessField))
                {
                    complement.Add(childlessField, new SortedSet<String>());
                }
            }
            return complement;
        }

        // @VisibleForTesting
        bool ShouldDrop(String parent, String child)
        {
            if (!excludableParentFields.Contains(parent))
            {
                throw new Exception(parent + " is not an excludable parent field");
            }
            if (!excludableChildFields.Contains(child))
            {
                throw new Exception(child + " is not an excludable child field");
            }
            return blacklist.ContainsKey(parent) && blacklist[parent].Contains(child);
        }

        // @VisibleForTesting
        bool ShouldDrop(String childlessField)
        {
            if (!excludableChildlessFields.Contains(childlessField))
            {
                throw new Exception(childlessField + " is not an excludable childless field");
            }
            return blacklist.ContainsKey(childlessField);
        }

        private PhoneNumberDesc GetFiltered(String type, PhoneNumberDesc desc)
        {
            PhoneNumberDesc.Builder builder = new PhoneNumberDesc.Builder().MergeFrom(desc);
            if (ShouldDrop(type, "nationalNumberPattern"))
            {
                builder.ClearNationalNumberPattern();
            }
            if (ShouldDrop(type, "possibleLength"))
            {
                builder.ClearPossibleLength();
            }
            if (ShouldDrop(type, "possibleLengthLocalOnly"))
            {
                builder.ClearPossibleLengthLocalOnly();
            }
            if (ShouldDrop(type, "exampleNumber"))
            {
                builder.ClearExampleNumber();
            }
            return builder.Build();
        }
    }
}