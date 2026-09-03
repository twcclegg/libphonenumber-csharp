using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// Covers <see cref="PhoneRegex"/>. Every pattern embeds a fresh GUID so tests never share or race
    /// on a cache entry -- the pattern cache is static and process-wide.
    /// </summary>
    public class TestPhoneRegex
    {
        /// <summary>Builds a (group name, literal marker, pattern) triple unique to the calling test.</summary>
        private static (string GroupName, string Marker, string Pattern) UniqueCase(string label)
        {
            var marker = $"{label}_{Guid.NewGuid():N}";
            var groupName = label + "g";
            return (groupName, marker, $"(?<{groupName}>\\d{{2,5}})_{marker}");
        }

        [Fact]
        public void PublicApiSurfaceStillWorks()
        {
            var (groupName, marker, pattern) = UniqueCase("basic");
            var regex = new PhoneRegex(pattern);
            var value = $"12345_{marker}";

            Assert.True(regex.IsMatch(value));
            Assert.True(regex.Match(value).Success);
            Assert.Equal("replaced", regex.Replace(value, "replaced"));
            Assert.True(regex.IsMatchAll(value));
            Assert.True(regex.MatchAll(value).Success);
            Assert.True(regex.IsMatchBeginning(value));
            Assert.True(regex.MatchBeginning(value).Success);
            Assert.Equal("12345", regex.Match(value).Groups[groupName].Value);

            Assert.False(regex.IsMatch("no digits here"));
            Assert.False(regex.IsMatchAll(value + " trailing junk"));
            Assert.True(regex.IsMatchBeginning(value + " trailing junk"));
        }

        /// <summary>
        /// The regression guard this file exists for. Metadata-derived patterns must not be built with
        /// RegexOptions.Compiled: it costs ~1.5 ms of IL-emit per pattern and only repays after tens of
        /// thousands of matches of that same pattern, which metadata patterns rarely see.
        /// <para>
        /// This library has shipped that regression three times -- 8.8.0, 8.13.0 and 9.0.30 (PR #325) --
        /// and no test has ever caught it. Measured end-to-end on net8.0, compiling these costs 34x for
        /// 1,000 operations across 245 regions and is still 1.7x slower at 1,000,000. If this test
        /// fails, read <see cref="InternalRegexOptions.Interpreted"/> before changing it.
        /// </para>
        /// </summary>
        [Fact]
        public void MetadataPatternsAreNeverCompiled()
        {
            var (_, marker, pattern) = UniqueCase("notcompiled");
            var regex = PhoneRegex.Get(pattern);
            Assert.True(regex.IsMatch($"12_{marker}"));

            foreach (var options in regex.BuiltOptions)
            {
                Assert.False(options.HasFlag(RegexOptions.Compiled),
                    "metadata-derived patterns must be interpreted -- see InternalRegexOptions.Interpreted");
                Assert.Equal(InternalRegexOptions.Interpreted, options);
            }
        }

        /// <summary>
        /// The library's own fixed regexes are a different case and do stay compiled: a fixed handful,
        /// built once per process, hot for the life of it. The two option sets must differ by nothing
        /// except RegexOptions.Compiled, so that a semantic flag cannot be added to one and not the
        /// other.
        /// </summary>
        [Fact]
        public void FixedRegexOptionsDifferFromMetadataOnesOnlyByCompiled()
        {
            Assert.Equal(InternalRegexOptions.Interpreted | RegexOptions.Compiled, InternalRegexOptions.Default);
            Assert.Equal(InternalRegexOptions.Interpreted, InternalRegexOptions.Default & ~RegexOptions.Compiled);
            Assert.True(InternalRegexOptions.Default.HasFlag(RegexOptions.Compiled));
            Assert.True(InternalRegexOptions.Interpreted.HasFlag(RegexOptions.CultureInvariant));
        }

        [Fact]
        public void CachedGetReturnsSameInstanceForSamePattern()
        {
            var (_, _, pattern) = UniqueCase("cache");
            Assert.Same(PhoneRegex.Get(pattern), PhoneRegex.Get(pattern));
        }

#pragma warning disable CS0618 // intentionally exercising the obsolete ctor
        /// <summary>
        /// Caller-supplied options are still honoured exactly. This is back-compat for a constructor
        /// that should never have been public, not a supported way to opt into compiled patterns.
        /// </summary>
        [Fact]
        public void ObsoleteOptionsConstructorStillHonorsCallerSuppliedOptions()
        {
            var (_, marker, pattern) = UniqueCase("obsolete");

            var ignoreCase = new PhoneRegex(pattern, RegexOptions.IgnoreCase);
            Assert.True(ignoreCase.IsMatch($"12345_{marker.ToUpperInvariant()}"));

            var compiled = new PhoneRegex(pattern, RegexOptions.Compiled);
            Assert.True(compiled.IsMatch($"12345_{marker}"));
            Assert.All(compiled.BuiltOptions, o => Assert.True(o.HasFlag(RegexOptions.Compiled)));
        }
#pragma warning restore CS0618

        /// <summary>
        /// A cache entry is shared process-wide, so concurrent first touches of the same pattern must
        /// build it exactly once and every caller must get correct results throughout. Half the workers
        /// hold one instance and half re-fetch through <see cref="PhoneRegex.Get"/>.
        /// </summary>
        [Fact]
        public async Task ConcurrentFirstUseOfASharedCacheEntryStaysCorrect()
        {
            var (groupName, marker, pattern) = UniqueCase("concurrent");
            var regex = PhoneRegex.Get(pattern);

            var matchingValue = $"999_{marker}";
            var nonMatchingValue = $"999_{marker}_nope";

            const int workers = 8;
            const int iterationsPerWorker = 200;

            var tasks = new List<Task>();
            for (var w = 0; w < workers; w++)
            {
                var reFetchEachTime = w % 2 == 0;
                tasks.Add(Task.Run(() =>
                {
                    for (var i = 0; i < iterationsPerWorker; i++)
                    {
                        var r = reFetchEachTime ? PhoneRegex.Get(pattern) : regex;

                        Assert.True(r.IsMatch(matchingValue));
                        Assert.True(r.IsMatchAll(matchingValue));
                        Assert.True(r.IsMatchBeginning(matchingValue));
                        Assert.False(r.IsMatchAll(nonMatchingValue));
                        Assert.True(r.IsMatchBeginning(nonMatchingValue));

                        var m = r.MatchAll(matchingValue);
                        Assert.True(m.Success);
                        Assert.Equal("999", m.Groups[groupName].Value);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Assert.True(regex.IsMatch(matchingValue));
            Assert.False(regex.IsMatch($"abc_{marker}_nope"));
        }
    }
}
