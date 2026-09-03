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
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    /// <summary>
    /// Wraps the three regexes ("raw", "anchored to the whole input", "anchored to the start") built
    /// from a single metadata-derived pattern string.
    /// <para>
    /// These are built with <see cref="InternalRegexOptions.Interpreted"/>, deliberately, and must stay
    /// that way -- see <see cref="InternalRegexOptions.Interpreted"/> for the measurements. Unlike the
    /// library's fixed regexes, which are compile-time-known and built once per process, these are
    /// metadata-derived: there are thousands of them, each built the first time some caller happens to
    /// touch that region, and RegexOptions.Compiled costs roughly 1.5 ms of IL-emit per pattern that
    /// only repays after tens of thousands of matches of that same pattern.
    /// </para>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PhoneRegex
    {
        private readonly string pattern;
        private readonly Lazy<Regex> regex;
        private readonly Lazy<Regex> allRegex;
        private readonly Lazy<Regex> beginRegex;

        private static readonly ConcurrentDictionary<string, PhoneRegex> cache = new();

        // Cached factory delegate so cache-hit lookups never allocate a fresh closure.
        private static readonly Func<string, PhoneRegex> factory = k => new PhoneRegex(k);

        internal static PhoneRegex Get(string regex) => cache.GetOrAdd(regex, factory);

        public PhoneRegex(string pattern)
        {
            this.pattern = pattern;

            regex = new Lazy<Regex>(() => new Regex(this.pattern, InternalRegexOptions.Interpreted), true);
            allRegex = new Lazy<Regex>(() => new Regex($"^(?:{this.pattern})$", InternalRegexOptions.Interpreted), true);
            beginRegex = new Lazy<Regex>(() => new Regex($"^(?:{this.pattern})", InternalRegexOptions.Interpreted), true);
        }

        [Obsolete("This is an internal implementation detail not meant for public use")]
        public PhoneRegex(string pattern, RegexOptions options)
        {
            this.pattern = pattern;

            regex = new Lazy<Regex>(() => new Regex(pattern, options), true);
            allRegex = new Lazy<Regex>(() => new Regex($"^(?:{pattern})$", options), true);
            beginRegex = new Lazy<Regex>(() => new Regex($"^(?:{pattern})", options), true);
        }

        public bool IsMatch(string value) => regex.Value.IsMatch(value);
        public Match Match(string value) => regex.Value.Match(value);
        public string Replace(string value, string replacement) => regex.Value.Replace(value, replacement);

        public bool IsMatchAll(string value) => allRegex.Value.IsMatch(value);

#if NET7_0_OR_GREATER
        /// <summary>
        /// Lets callers test a slice without materialising it. Internal because a public member here
        /// would exist on the net8.0 and net10.0 assets but not on netstandard2.0. At a major version
        /// the string overloads should become span overloads outright - see issue #375.
        /// </summary>
        internal bool IsMatchAll(ReadOnlySpan<char> value) => allRegex.Value.IsMatch(value);
#endif
        public Match MatchAll(string value) => allRegex.Value.Match(value);

        public bool IsMatchBeginning(string value) => beginRegex.Value.IsMatch(value);

#if NET7_0_OR_GREATER
        /// <summary>
        /// Length of the match anchored at the start, or -1 if there is none. EnumerateMatches yields
        /// a ValueMatch struct, so a caller that only needs the length never materialises a Match.
        /// </summary>
        internal int MatchBeginningLength(ReadOnlySpan<char> value)
        {
            foreach (var match in beginRegex.Value.EnumerateMatches(value))
                return match.Length;
            return -1;
        }
#endif
        public Match MatchBeginning(string value) => beginRegex.Value.Match(value);

        /// <summary>
        /// Options the three regexes were actually built with. Exists so a test can fail if
        /// metadata-derived patterns are ever switched back to RegexOptions.Compiled -- the regression
        /// this library has shipped three times, and which no test has ever caught.
        /// </summary>
        internal RegexOptions[] BuiltOptions =>
            new[] { regex.Value.Options, allRegex.Value.Options, beginRegex.Value.Options };
    }
}
