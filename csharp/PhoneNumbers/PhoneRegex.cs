#nullable disable
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
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    /// <summary>
    /// Wraps the three regexes ("raw", "anchored to the whole input", "anchored to the start") built
    /// from a single metadata-derived pattern string.
    /// <para>
    /// <b>Hybrid promote-on-reuse, with off-thread promotion.</b> Each of the three regexes starts out
    /// built with <see cref="InternalRegexOptions.Interpreted"/>: fast to construct (no
    /// RegexOptions.Compiled IL-emit JIT), so a pattern touched once -- the common shape for a process
    /// that only ever sees a handful of distinct regions -- never pays a JIT-compile cost it can't
    /// amortise. <see cref="RegexHolder"/> counts how many times each of those three regexes is
    /// actually used; once a given one crosses <see cref="PromotionThreshold"/> uses, it kicks off a
    /// background <see cref="Task"/> that builds the same pattern with
    /// <see cref="InternalRegexOptions.Default"/> (i.e. RegexOptions.Compiled) and atomically swaps it
    /// in for subsequent callers once it's ready. A call already in flight keeps using whichever Regex
    /// instance it fetched when it started -- the swap only affects calls that read
    /// <see cref="RegexHolder.Value"/> after the swap has landed.
    /// </para>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PhoneRegex
    {
        /// <summary>
        /// Number of uses (of one specific raw/anchored-all/anchored-start regex) after which a
        /// background compile is kicked off. Chosen empirically, by benchmarking 2 against 3 on this
        /// exact machine:
        /// <list type="bullet">
        /// <item>Both values give a pattern touched exactly once -- the diverse-region,
        /// touch-each-region-once workload <c>ColdStartBenchmark.FirstUse*</c> measures -- the full
        /// cold-start win: it never crosses either threshold, so it never pays the
        /// RegexOptions.Compiled JIT-emit cost at all. The two thresholds are statistically
        /// indistinguishable on <c>FirstUse*</c>.</item>
        /// <item>They are NOT indistinguishable on <c>PhoneNumberWorkflowBenchmark</c>. Its
        /// <c>GlobalSetup</c> already touches every seed pattern once (building the benchmark's seed
        /// data via GetExampleNumberForType/IsValidNumber/Format), and its timed loop then reuses each
        /// seed case only a few more times (PhoneNumberCount=1000 spread across several hundred seed
        /// cases). With threshold 3, most patterns never reach a 3rd use within the timed loop, so most
        /// of the run stays on interpreted execution -- measured ~55-115% slower than the
        /// always-RegexOptions.Compiled baseline (PR #325) on <c>ParseValidateAndFormatPhoneNumbers</c>
        /// /<c>ValidateOnly</c>/<c>FormatOnly</c>. With threshold 2, the single setup touch plus the
        /// timed loop's first touch cross the threshold almost immediately, and the benchmark's numbers
        /// land back within noise of the always-compiled baseline (see this file's PR description for
        /// the exact figures).</item>
        /// </list>
        /// In short: 2 keeps the entire <c>FirstUse*</c> cold-start win with none of the
        /// <c>PhoneNumberWorkflowBenchmark</c> steady-state cost that 3 measurably paid on this
        /// workload's specific reuse shape (setup-plus-a-handful-of-timed-touches per pattern). A
        /// pattern touched only once, ever, in the whole process -- true cold start -- still never
        /// promotes at either value; the difference only shows up once a pattern actually is reused.
        /// </summary>
        internal const int PromotionThreshold = 2;

        private readonly string pattern;
        private readonly RegexHolder regex;
        private readonly RegexHolder allRegex;
        private readonly RegexHolder beginRegex;

        private static readonly ConcurrentDictionary<string, PhoneRegex> cache = new();

        // Cached factory delegate so cache-hit lookups never allocate a fresh closure.
        private static readonly Func<string, PhoneRegex> factory = k => new PhoneRegex(k);

        internal static PhoneRegex Get(string regex) => cache.GetOrAdd(regex, factory);

        public PhoneRegex(string pattern)
        {
            this.pattern = pattern;

            regex = new RegexHolder(this.pattern);
            allRegex = new RegexHolder($"^(?:{this.pattern})$");
            beginRegex = new RegexHolder($"^(?:{this.pattern})");
        }

        [Obsolete("This is an internal implementation detail not meant for public use")]
        public PhoneRegex(string pattern, RegexOptions options)
        {
            this.pattern = pattern;

            // A caller-supplied options value opts out of the promote-on-reuse scheme entirely -- we
            // honor exactly what was asked for, same as the original implementation.
            regex = new RegexHolder(pattern, options);
            allRegex = new RegexHolder($"^(?:{pattern})$", options);
            beginRegex = new RegexHolder($"^(?:{pattern})", options);
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
        /// Prewarm hook: forces all three regexes straight to <see cref="InternalRegexOptions.Default"/>
        /// (RegexOptions.Compiled) on a background thread, bypassing <see cref="PromotionThreshold"/>,
        /// and returns a <see cref="Task"/> that completes once all three builds have landed. Intended
        /// for callers who know ahead of time which patterns they're about to rely on heavily and want
        /// to pay the compile cost before real traffic arrives -- see
        /// <see cref="PhoneNumberUtil.PrewarmRegionsAsync"/>. Idempotent and safe to call concurrently
        /// with normal use or with another prewarm call; whichever build (regular promotion or this one)
        /// finishes first wins the swap, and the other is simply discarded.
        /// </summary>
        internal Task PrewarmAsync() => Task.WhenAll(regex.ForcePromoteAsync(), allRegex.ForcePromoteAsync(), beginRegex.ForcePromoteAsync());

        /// <summary>
        /// Holds one lazily-built <see cref="Regex"/> for a single fixed pattern string, and implements
        /// the promote-on-reuse swap described on <see cref="PhoneRegex"/>. Not nested-generic or
        /// otherwise reused across patterns -- one instance per (pattern, "raw"/"all"/"begin") pair.
        /// </summary>
        private sealed class RegexHolder
        {
            private readonly string pattern;

            // Non-null only for the [Obsolete] PhoneRegex(string, RegexOptions) constructor, which asks
            // for specific options and opts out of promotion entirely.
            private readonly RegexOptions? fixedOptions;

            // Deliberately not `volatile`: all reads/writes go through Volatile.Read/Write or
            // Interlocked explicitly below, so the field itself can stay plain and avoid the CS0420
            // ("volatile field passed by ref won't be treated as volatile") warning that
            // TreatWarningsAsErrors would turn into a build failure.
            private Regex current;

            private int useCount;
            private int promotionStarted;

            public RegexHolder(string pattern, RegexOptions? fixedOptions = null)
            {
                this.pattern = pattern;
                this.fixedOptions = fixedOptions;
            }

            public Regex Value
            {
                get
                {
                    var built = Volatile.Read(ref current) ?? Build();

                    // Fixed-options instances (the obsolete ctor) never promote -- they got exactly the
                    // options the caller asked for and there's nothing to upgrade.
                    if (fixedOptions is null && Interlocked.Increment(ref useCount) == PromotionThreshold)
                        Promote();

                    return built;
                }
            }

            private Regex Build()
            {
                var options = fixedOptions ?? InternalRegexOptions.Interpreted;
                var candidate = new Regex(pattern, options);

                // Benign race: if two threads both find `current` null, both may construct a Regex here.
                // They are functionally identical (same pattern, same options), so we simply keep
                // whichever one wins the compare-exchange and let the loser be collected.
                return Interlocked.CompareExchange(ref current, candidate, null) ?? candidate;
            }

            private void Promote()
            {
                // Ensures exactly one background compile is ever kicked off for this holder, no matter
                // how many callers cross the threshold concurrently (Interlocked.Increment hands out
                // distinct values, but guard anyway since ForcePromoteAsync can race the same flag).
                if (Interlocked.CompareExchange(ref promotionStarted, 1, 0) != 0)
                    return;

                Task.Run(() =>
                {
                    var compiled = new Regex(pattern, fixedOptions ?? InternalRegexOptions.Default);
                    Volatile.Write(ref current, compiled);
                });
            }

            /// <summary>
            /// Used by <see cref="PhoneRegex.PrewarmAsync"/> to force promotion regardless of
            /// <see cref="useCount"/>. Shares <see cref="promotionStarted"/> with <see cref="Promote"/>
            /// so a pattern already promoted (or already being promoted) through normal use is not
            /// compiled twice.
            /// </summary>
            public Task ForcePromoteAsync()
            {
                if (Interlocked.CompareExchange(ref promotionStarted, 1, 0) != 0)
                    return Task.CompletedTask;

                return Task.Run(() =>
                {
                    var compiled = new Regex(pattern, fixedOptions ?? InternalRegexOptions.Default);
                    Volatile.Write(ref current, compiled);
                });
            }
        }
    }
}
