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

using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    public sealed class PhoneRegex
    {
        private readonly string pattern;
        private readonly RegexOptions options;
        private Regex regex;
        private Regex allRegex;
        private Regex beginRegex;

        public PhoneRegex(string pattern)
            : this(pattern, RegexOptions.None)
        {
        }

        public PhoneRegex(string pattern, RegexOptions options)
        {
            this.pattern = pattern;
            this.options = options | InternalRegexOptions.Default;
        }

        private Regex GetRegex()
        {
            if (regex is null) lock (this) regex ??= new Regex(pattern, options);
            return regex;
        }

        private Regex GetAllRegex()
        {
            if (allRegex is null) lock (this) allRegex ??= new Regex($"^(?:{pattern})$", options);
            return allRegex;
        }

        private Regex GetBeginRegex()
        {
            if (beginRegex is null) lock (this) beginRegex ??= new Regex($"^(?:{pattern})", options);
            return beginRegex;
        }

        public bool IsMatch(string value) => GetRegex().IsMatch(value);
        public Match Match(string value) => GetRegex().Match(value);
        public string Replace(string value, string replacement) => GetRegex().Replace(value, replacement);

        public bool IsMatchAll(string value) => GetAllRegex().IsMatch(value);
        public Match MatchAll(string value) => GetAllRegex().Match(value);

        public bool IsMatchBeginning(string value) => GetBeginRegex().IsMatch(value);
        public Match MatchBeginning(string value) => GetBeginRegex().Match(value);
    }
}
