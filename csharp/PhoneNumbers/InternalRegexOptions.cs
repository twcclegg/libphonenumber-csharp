using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    /// <summary>
    /// This utility class determines the correct set of <see cref="RegexOptions"/> to specify when creating
    /// <see cref="Regex"/> instances at runtime within this library.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    internal static class InternalRegexOptions
    {
        /// <summary>
        /// Options for metadata-derived patterns -- everything <see cref="PhoneRegex"/> builds.
        /// <para>
        /// No RegexOptions.Compiled, deliberately. Compiling costs roughly 1.5 ms of IL-emit for each
        /// distinct pattern, and buys about 0.044 us per match; a pattern therefore has to be matched
        /// on the order of 30,000 times before compiling it breaks even. Metadata-derived patterns do
        /// not come close to that in most processes: there are thousands of them, each first built when
        /// some caller happens to touch that region, and a workload spread across regions matches each
        /// one a few thousand times at most.
        /// </para>
        /// <para>
        /// Measured end-to-end (net8.0, total wall time including startup, parse + validate + format):
        /// compiling these is 34x slower for 1,000 operations across 245 regions, 3.7x slower for
        /// 100,000, and still 1.7x slower at 1,000,000. It wins only when a process concentrates very
        /// high volume on very few regions -- 1 region and 100,000+ operations -- and then by 6-20%.
        /// The library has shipped that trade three times by accident, most recently in PR #325, each
        /// time because the benchmark used to justify it pre-warmed every pattern in setup and then
        /// hammered a handful, which is precisely the one shape where compiling wins. Do not change
        /// this on the strength of PhoneNumberWorkflowBenchmark alone.
        /// </para>
        /// </summary>
        public const RegexOptions Interpreted = RegexOptions.CultureInvariant;

        /// <summary>
        /// Options for the library's own fixed, compile-time-known regexes -- the <c>GeneratedRegex</c>
        /// members and <c>static readonly Regex</c> fields scattered across the library. Compiling
        /// those is worth it: there is a fixed handful, each is built once per process, and each is on
        /// a hot path for the life of it. Derived from <see cref="Interpreted"/> so the two sets differ
        /// by nothing except RegexOptions.Compiled, and a semantic flag added here cannot apply to one
        /// group of regexes but not the other.
        /// </summary>
        public const RegexOptions Default = Interpreted | RegexOptions.Compiled;
    }
}
