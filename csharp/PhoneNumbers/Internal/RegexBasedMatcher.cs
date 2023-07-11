/*
 * Copyright (C) 2014 The Libphonenumber Authors
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

using System.Text.RegularExpressions;

namespace PhoneNumbers.Internal
{
    public sealed class RegexBasedMatcher : IMatcherApi
    {
        public static IMatcherApi Create()
        {
            return new RegexBasedMatcher();
        }

        #if !NET7_0_OR_GREATER
        private static readonly RegexCache RegexCache = new RegexCache(100);
#endif

        private RegexBasedMatcher()
        {
        }

#if NET7_0_OR_GREATER
        public bool MatchNationalNumber(string number, PhoneNumberDesc numberDesc, bool allowPrefixMatch)
        {
            var nationalNumberPattern = numberDesc.NationalNumberPattern;
            // We don't want to consider it a prefix match when matching non-empty input against an empty
            // pattern.
            var regex = RegexCache.GetOrAddPatternForRegex(nationalNumberPattern,
                _ => new PhoneRegex(new Regex(nationalNumberPattern), new Regex(nationalNumberPattern), new Regex("")));

            return nationalNumberPattern.Length > 0 &&
                   Match(number, regex, allowPrefixMatch);
        }
#else
        public bool MatchNationalNumber(string number, PhoneNumberDesc numberDesc, bool allowPrefixMatch)
        {
            var nationalNumberPattern = numberDesc.NationalNumberPattern;
            // We don't want to consider it a prefix match when matching non-empty input against an empty
            // pattern.

            RegexCache.GetPatternForRegex(nationalNumberPattern);
            return nationalNumberPattern.Length > 0 &&
                   Match(number, regex, allowPrefixMatch);
        }
#endif

        private static bool Match(string number, PhoneRegex pattern, bool allowPrefixMatch)
            => allowPrefixMatch ? pattern.IsMatchBeginning(number) : pattern.IsMatchAll(number);
    }
}
