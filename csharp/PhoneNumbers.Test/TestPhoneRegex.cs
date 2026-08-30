using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// Covers <see cref="PhoneRegex"/>'s hybrid promote-on-reuse scheme: a pattern is built interpreted
    /// on first use and later promoted to <c>RegexOptions.Compiled</c> in the background once it's been
    /// reused <see cref="PhoneRegex.PromotionThreshold"/> times. Every pattern here embeds a fresh GUID
    /// so tests never share (or race on) a cache entry with each other or with any other test in the
    /// process -- <see cref="PhoneRegex"/>'s pattern cache is a static, process-wide
    /// <c>ConcurrentDictionary</c>.
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

#pragma warning disable CS0618 // intentionally exercising the obsolete ctor
        [Fact]
        public void ObsoleteOptionsConstructorStillHonorsCallerSuppliedOptions()
        {
            var (_, marker, pattern) = UniqueCase("obsolete");
            var regex = new PhoneRegex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            Assert.True(regex.IsMatch($"12345_{marker.ToUpperInvariant()}"));
        }
#pragma warning restore CS0618

        [Fact]
        public void CachedGetReturnsSameInstanceForSamePattern()
        {
            var (_, _, pattern) = UniqueCase("cache");
            var a = PhoneRegex.Get(pattern);
            var b = PhoneRegex.Get(pattern);

            Assert.Same(a, b);
        }

        /// <summary>
        /// Drives one pattern through many more than <see cref="PhoneRegex.PromotionThreshold"/> uses,
        /// from many threads at once, and asserts every single call -- before, during, and after
        /// whichever call happens to trigger the background compile -- returns a correct result. This
        /// is the scenario the hybrid scheme's thread-safety hinges on: a caller in flight must never
        /// observe a torn or incorrect match because another thread swapped in the compiled Regex
        /// underneath it.
        /// </summary>
        [Fact]
        public async Task ConcurrentReuseAcrossThresholdNeverProducesAnIncorrectMatch()
        {
            var (groupName, marker, pattern) = UniqueCase("concurrent");
            var regex = new PhoneRegex(pattern);

            var matchingValue = $"999_{marker}";
            var nonMatchingValue = $"999_{marker}_nope";

            const int workers = 8;
            const int iterationsPerWorker = 200; // >> PromotionThreshold, so promotion is guaranteed to fire mid-run

            var tasks = new List<Task>();
            for (var w = 0; w < workers; w++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (var i = 0; i < iterationsPerWorker; i++)
                    {
                        Assert.True(regex.IsMatch(matchingValue));
                        Assert.True(regex.IsMatchAll(matchingValue));
                        Assert.True(regex.IsMatchBeginning(matchingValue));
                        Assert.False(regex.IsMatchAll(nonMatchingValue));
                        Assert.True(regex.IsMatchBeginning(nonMatchingValue));

                        var m = regex.MatchAll(matchingValue);
                        Assert.True(m.Success);
                        Assert.Equal("999", m.Groups[groupName].Value);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Give the background promotion (kicked off well before this point, given
            // workers * iterationsPerWorker far exceeds PromotionThreshold) a moment to land, then
            // confirm the pattern is still correct once it has -- i.e. the swapped-in compiled Regex
            // behaves identically to the interpreted one it replaced.
            await Task.Delay(200);

            Assert.True(regex.IsMatch(matchingValue));
            Assert.False(regex.IsMatch($"abc_{marker}_nope"));
        }

        [Fact]
        public async Task RepeatedUseOfSharedCacheEntryStaysCorrectThroughPromotion()
        {
            var (_, marker, pattern) = UniqueCase("shared");
            var value = $"777_{marker}";

            const int iterations = 50; // >> PromotionThreshold
            for (var i = 0; i < iterations; i++)
            {
                var regex = PhoneRegex.Get(pattern);
                Assert.True(regex.IsMatch(value));
                Assert.True(regex.IsMatchAll(value));
            }

            await Task.Delay(200);

            var finalRegex = PhoneRegex.Get(pattern);
            Assert.True(finalRegex.IsMatch(value));
            Assert.True(finalRegex.IsMatchAll(value));
        }
    }
}
