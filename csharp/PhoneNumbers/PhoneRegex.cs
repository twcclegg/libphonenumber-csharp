/*
 * Copyright (C) 2011 Patrick Mezard
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
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    public sealed class PhoneRegex
    {
#if NET7_0_OR_GREATER
        private readonly Regex regex;
        private readonly Regex allRegex;
        private readonly Regex beginRegex;
        #else
        private readonly Lazy<Regex> regex;
        private readonly Lazy<Regex> allRegex;
        private readonly Lazy<Regex> beginRegex;
#endif

#if NET7_0_OR_GREATER
        public PhoneRegex(Regex regex, Regex allRegex, Regex beginRegex)
        {
            this.regex = regex;
            this.allRegex = allRegex;
            this.beginRegex = beginRegex;
        }

        [Obsolete("Use source generated regexs in >=NET7")]
#endif
        // ReSharper disable once UnusedParameter.Local
        public PhoneRegex(string pattern, RegexOptions options = RegexOptions.None)
        {
            #if !NET7_0_OR_GREATER
            regex = new Lazy<Regex>(() => new Regex(pattern, options), true);
            allRegex = new Lazy<Regex>(() => new Regex($"^(?:{pattern})$", options), true);
            beginRegex = new Lazy<Regex>(() => new Regex($"^(?:{pattern})", options), true);
#endif
        }

        public bool IsMatch(string value) => regex.Value().IsMatch(value);
        public Match Match(string value) => regex.Value().Match(value);
        public string Replace(string value, string replacement) => regex.Value().Replace(value, replacement);

        public bool IsMatchAll(string value) => allRegex.Value().IsMatch(value);
        public Match MatchAll(string value) => allRegex.Value().Match(value);

        public bool IsMatchBeginning(string value) => beginRegex.Value().IsMatch(value);
        public Match MatchBeginning(string value) => beginRegex.Value().Match(value);
    }

    public static class LazyHelper
    {
        #if NET7_0_OR_GREATER
        internal static Regex Value(this Regex regex)
        {
            return regex;
        }
        #else
        internal static T Value<T>(this Lazy<T> lazyRegex)
        {
            return lazyRegex.Value;
        }
#endif
    }
}
