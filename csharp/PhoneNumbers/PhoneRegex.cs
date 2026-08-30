#nullable enable
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
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reflection;
#endif

namespace PhoneNumbers
{
    /// <summary>
    /// Wraps the three regexes ("raw", "anchored to the whole input", "anchored to the start") built
    /// from a single metadata-derived pattern string.
    /// <para>
    /// <b>Hybrid promote-on-reuse, with off-thread promotion (Lazy&lt;T&gt;-delegated build).</b> Each
    /// of the three regexes starts out built with <see cref="InternalRegexOptions.Interpreted"/>: fast
    /// to construct (no RegexOptions.Compiled IL-emit JIT), so a pattern touched once -- the common
    /// shape for a process that only ever sees a handful of distinct regions -- never pays a
    /// JIT-compile cost it can't amortise. Unlike the hand-rolled-CompareExchange sibling variant of
    /// this file (see perf/coldstart-hybrid-promote), the "build exactly once, even under concurrent
    /// first touches" guarantee for the interpreted build is delegated entirely to
    /// <see cref="Lazy{T}"/> rather than hand-rolled -- see <see cref="RegexHolder"/>.
    /// <see cref="RegexHolder"/> counts how many times each of those three regexes is actually used;
    /// once a given one crosses <see cref="PromotionThreshold"/> uses, it kicks off a background
    /// <see cref="Task"/> that builds the same pattern with <see cref="InternalRegexOptions.Default"/>
    /// (i.e. RegexOptions.Compiled) and atomically swaps it in for subsequent callers once it's ready.
    /// A call already in flight keeps using whichever Regex instance it fetched when it started -- the
    /// swap only affects calls that read <see cref="RegexHolder.Value"/> after the swap has landed.
    /// </para>
    /// <para>
    /// <b>Lookup, separately from the above:</b> on net8.0/net10.0, finding the right
    /// <see cref="PhoneRegex"/> for a pattern string is a <see cref="System.Collections.Frozen.FrozenDictionary{TKey,TValue}"/>
    /// read rather than a <see cref="ConcurrentDictionary{TKey,TValue}"/> one, for the ~2,922 patterns
    /// enumerable at build time from shipped metadata -- see <c>KnownPatterns</c> (net8.0/net10.0
    /// only). This changes
    /// only how <see cref="Get"/> finds the right <see cref="PhoneRegex"/> instance; it has nothing to
    /// do with, and does not change, how or when that instance's own <see cref="Regex"/> objects get
    /// built (still the promote-on-reuse scheme described above, for every pattern regardless of which
    /// dictionary found it). netstandard2.0, which has no <c>System.Collections.Frozen</c>, keeps the
    /// single <see cref="ConcurrentDictionary{TKey,TValue}"/> this class always used.
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

        // Diagnostic-only, not read on any hot path: counts how many background compiles have
        // actually been kicked off (i.e. how many times a RegexHolder's promotion guard was won),
        // across every pattern in the process. Used to quantify "how much extra ThreadPool work does
        // this design cause" when comparing promotion strategies -- see
        // PhoneNumbers.PerformanceTest's PromoteCallCountBenchmark.
        internal static int PromoteCallCount;

        // Diagnostic-only, same caveats as PromoteCallCount: how many background compiles are actually
        // executing concurrently right now / at their peak, across every pattern in the process. This
        // is the number that matters for "does this design flood the ThreadPool under a bursty
        // first-touch" -- one Task.Run per promotion means this can spike to (number of patterns
        // touched in a burst); a queue-backed design should keep it bounded to its worker count.
        internal static int ActiveCompiles;
        internal static int PeakConcurrentCompiles;

        private static void RecordCompileStart()
        {
            var now = Interlocked.Increment(ref ActiveCompiles);
            int peak;
            do
            {
                peak = Volatile.Read(ref PeakConcurrentCompiles);
                if (now <= peak)
                    return;
            } while (Interlocked.CompareExchange(ref PeakConcurrentCompiles, now, peak) != peak);
        }

        private static void RecordCompileEnd() => Interlocked.Decrement(ref ActiveCompiles);

        private readonly string pattern;
        private readonly RegexHolder regex;
        private readonly RegexHolder allRegex;
        private readonly RegexHolder beginRegex;

        // Cached factory delegate so cache-hit lookups never allocate a fresh closure.
        private static readonly Func<string, PhoneRegex> factory = k => new PhoneRegex(k);

#if NET8_0_OR_GREATER
        /// <summary>
        /// One <see cref="PhoneRegex"/> per pattern string enumerable at build time from the shipped
        /// metadata (~2,922 patterns as of this metadata snapshot -- see
        /// PhoneNumbers.MetadataBuilder's <c>RegexPatternCollector</c>). Reading a
        /// <see cref="FrozenDictionary{TKey,TValue}"/> is faster than a <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// read (no interlocked bookkeeping, a perfect/near-perfect hash built once up front), which is
        /// the entire point of splitting the cache this way -- every <see cref="PhoneRegex"/> instance
        /// here is still fully lazy: nothing constructs an actual <see cref="Regex"/> until a caller
        /// first touches <see cref="RegexHolder.Value"/>, exactly as for any pattern outside this set.
        /// This only changes how the <em>lookup</em> that finds the right <see cref="PhoneRegex"/>
        /// works, not how (or when) each pattern's regexes get built.
        /// </summary>
        private static readonly FrozenDictionary<string, PhoneRegex> KnownPatterns = BuildKnownPatterns();

        /// <summary>
        /// Fallback for any pattern not in <see cref="KnownPatterns"/>. Two known, expected sources:
        /// the legacy public <c>RegexCache.GetPatternForRegex</c> / <see cref="PhoneRegex(string)"/>
        /// surface accepts arbitrary caller-supplied text that cannot be known at build time; and
        /// <c>PhoneNumberMetadataForTesting.xml</c> (test-only metadata, never shipped in the published
        /// assembly) is deliberately excluded from <see cref="KnownPatterns"/> by
        /// <c>RegexPatternCollector</c>, so test-only patterns always land here. Anything else landing
        /// here would mean <see cref="KnownPatterns"/>' coverage has drifted from what
        /// <c>PhoneRegex.Get</c>'s real call sites actually touch -- see <see cref="FallbackCacheHits"/>.
        /// </summary>
        private static readonly ConcurrentDictionary<string, PhoneRegex> fallbackCache = new();

        // Diagnostic-only, not read on any hot path: counts how many PhoneRegex.Get calls missed
        // KnownPatterns and fell back to fallbackCache. Same style as PromoteCallCount -- used to
        // confirm KnownPatterns' coverage matches the known, expected exceptions (RegexCache's public
        // surface, PhoneNumberMetadataForTesting.xml patterns during test runs) and nothing else is
        // silently missing the fast path.
        internal static int FallbackCacheHits;

        private static FrozenDictionary<string, PhoneRegex> BuildKnownPatterns()
        {
            // Absent entirely for a custom-built assembly that stripped the resource (or an older
            // MetadataBuilder that predates it) -- fall through to every lookup missing and landing in
            // fallbackCache, functionally identical to (just slower than) the pre-FrozenDictionary
            // cache this replaces, not a hard failure.
            using var raw = typeof(PhoneRegex).Assembly.GetManifestResourceStream(
                "PhoneNumbers.regexpatterns.known_patterns.bin");
            if (raw is null)
                return FrozenDictionary<string, PhoneRegex>.Empty;

            // Same gzip-compressed-manifest-resource contract as EmbeddedResourceMetadataLoader.
            using var gz = new GZipStream(raw, CompressionMode.Decompress);
            var patterns = BuildPrefixMapFromBin.ReadRegexPatternList(gz);

            var builder = new Dictionary<string, PhoneRegex>(patterns.Length, StringComparer.Ordinal);
            foreach (var pattern in patterns)
                builder[pattern] = new PhoneRegex(pattern);
            return builder.ToFrozenDictionary(StringComparer.Ordinal);
        }

        internal static PhoneRegex Get(string regex)
        {
            if (KnownPatterns.TryGetValue(regex, out var known))
                return known;

            Interlocked.Increment(ref FallbackCacheHits);
            return fallbackCache.GetOrAdd(regex, factory);
        }
#else
        private static readonly ConcurrentDictionary<string, PhoneRegex> cache = new();

        internal static PhoneRegex Get(string regex) => cache.GetOrAdd(regex, factory);
#endif

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
        /// and returns a <see cref="Task"/> that completes once all three builds have actually landed --
        /// not merely once they've started. Intended for callers who know ahead of time which patterns
        /// they're about to rely on heavily and want to pay the compile cost before real traffic arrives
        /// -- see <see cref="PhoneNumberUtil.PrewarmRegionsAsync"/>. Idempotent and safe to call
        /// concurrently with normal use or with another prewarm call: at most one compile is ever done
        /// per regex (see <see cref="RegexHolder.StartCompile"/>), so whichever call -- ordinary use
        /// crossing <see cref="PromotionThreshold"/>, or this one -- reserves that holder's compile
        /// first, the other simply joins and awaits the same in-flight (or already finished) build
        /// rather than starting a second one.
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

            // The interpreted build. Lazy<T>'s default thread-safety mode (ExecutionAndPublication)
            // gives us "the factory runs at most once, and every concurrent caller gets the same
            // instance" for free -- a concurrent caller that loses the race blocks on a monitor lock
            // inside Lazy<T> until the winner's factory finishes, rather than the hand-rolled variant's
            // "both may build, one is discarded" race. That trades a small amount of contention (only
            // ever exercised by genuinely concurrent *first* touches of the same pattern) for not having
            // to reason about a benign-build-race by hand.
            private readonly Lazy<Regex> interpreted;

            // Set at most once, by the background compile, once it lands. Deliberately not `volatile`:
            // read/written via Volatile.Read/Write explicitly below, so the field itself can stay plain
            // and avoid the CS0420 ("volatile field passed by ref won't be treated as volatile") warning
            // that TreatWarningsAsErrors would turn into a build failure.
            private Regex? compiled;

            private int useCount;

            // Non-null the moment a compile has been claimed by Promote() or ForcePromoteAsync(),
            // whether or not it has finished yet; awaiting it (as ForcePromoteAsync's callers do) waits
            // for the compile to actually land, not just for it to have started. Set via a single
            // Interlocked.CompareExchange -- see StartCompile().
            private Task? compileTask;

#if NET7_0_OR_GREATER
            // RegexOptions.Compiled silently falls back to interpreted under NativeAOT (there's no
            // runtime codegen available there), so a background compile would be pure waste on that
            // runtime -- it would spin up a Task.Run to build a "compiled" Regex that behaves
            // identically to the interpreted one already in hand, burning CPU and ThreadPool capacity
            // for zero benefit. Checked once per process (static readonly), not per call.
            private static readonly bool CanPromote = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;
#else
            // netstandard2.0 consumers can't be NativeAOT-published in the first place (AOT publishing
            // requires net7.0+), so promotion is always worthwhile on that TFM. `static readonly`
            // rather than `const` so the compiler doesn't constant-fold the `if (!CanPromote)` checks
            // below into unreachable code (CS0162).
            private static readonly bool CanPromote = true;
#endif

            public RegexHolder(string pattern, RegexOptions? fixedOptions = null)
            {
                this.pattern = pattern;
                this.fixedOptions = fixedOptions;

                var options = fixedOptions ?? InternalRegexOptions.Interpreted;
                interpreted = new Lazy<Regex>(() => new Regex(pattern, options));
            }

            public Regex Value
            {
                get
                {
                    // Prefer the compiled build once it's landed; otherwise fall through to the
                    // (possibly still-unbuilt) interpreted Lazy<T>, which builds it on demand.
                    var built = Volatile.Read(ref compiled);
                    if (built is not null)
                        return built; // already promoted -- skip the useCount bookkeeping below entirely

                    built = interpreted.Value;

                    // Fixed-options instances (the obsolete ctor) never promote -- they got exactly the
                    // options the caller asked for and there's nothing to upgrade.
                    if (fixedOptions is null && Interlocked.Increment(ref useCount) == PromotionThreshold)
                        StartCompile();

                    return built;
                }
            }

            /// <summary>
            /// Kicks off this holder's one-and-only background compile, or -- if one has already been
            /// claimed, by a prior threshold-crossing <see cref="Value"/> access or an earlier
            /// <see cref="ForcePromoteAsync"/> call -- returns that same in-flight (or already finished)
            /// <see cref="Task"/> instead of starting a second one. Shared by both call sites so there is
            /// only one place that knows how to build and swap in the compiled Regex.
            /// <para>
            /// A "cold" (unstarted) <see cref="Task"/> lets this holder's compile slot be reserved with a
            /// single <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/>, then started only if
            /// this call actually won that race -- collapsing "has a compile started" and "what's its
            /// result" into the one <see cref="compileTask"/> field without a separate bool guard, and
            /// without starting (and paying the CPU/ThreadPool cost of) a real duplicate compile whenever
            /// two callers race here concurrently (only possible via a <see cref="ForcePromoteAsync"/> /
            /// threshold-crossing cross-path race -- the threshold-crossing path here can't race itself,
            /// since <see cref="Interlocked.Increment(ref int)"/> hands out distinct values to concurrent
            /// callers, so only one of them ever sees <see cref="useCount"/> == <see cref="PromotionThreshold"/>).
            /// </para>
            /// </summary>
            private Task StartCompile()
            {
                // Under NativeAOT, RegexOptions.Compiled is a no-op (falls back to interpreted), so
                // there is nothing to gain by promoting -- stay on the interpreted build permanently.
                // Fixed-options instances (the obsolete ctor) never promote either -- they got exactly
                // the options the caller asked for and there's nothing to upgrade.
                if (!CanPromote || fixedOptions is not null)
                    return Task.CompletedTask;

                var existing = Volatile.Read(ref compileTask);
                if (existing is not null)
                    return existing;

                var candidate = new Task(() =>
                {
                    RecordCompileStart();
                    try
                    {
                        var built = new Regex(pattern, InternalRegexOptions.Default);
                        Volatile.Write(ref compiled, built);
                    }
                    finally
                    {
                        RecordCompileEnd();
                    }
                });

                var winner = Interlocked.CompareExchange(ref compileTask, candidate, null);
                if (winner is not null)
                    return winner; // lost the race -- await/return the winner's task instead

                Interlocked.Increment(ref PromoteCallCount);
                candidate.Start();
                return candidate;
            }

            /// <summary>
            /// Used by <see cref="PhoneRegex.PrewarmAsync"/> to force promotion regardless of
            /// <see cref="useCount"/>, returning a <see cref="Task"/> that completes once the compile
            /// this call either started or joined has actually landed -- not merely started. Delegates
            /// entirely to <see cref="StartCompile"/>, which is also what a normal threshold-crossing
            /// <see cref="Value"/> access uses, so a pattern already promoted (or already being promoted)
            /// through ordinary use is joined rather than compiled twice.
            /// </summary>
            public Task ForcePromoteAsync() => StartCompile();
        }
    }
}
