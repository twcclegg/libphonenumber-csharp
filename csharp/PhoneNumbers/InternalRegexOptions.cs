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
        /// Options for the library's fixed, compile-time-known regexes (the <c>GeneratedRegex</c>
        /// attributes and static readonly <see cref="Regex"/> fields scattered across the library) and
        /// for a <see cref="PhoneRegex"/> pattern once it has been promoted -- see
        /// <see cref="Interpreted"/> and PhoneRegex.cs's promote-on-reuse scheme. RegexOptions.Compiled
        /// does IL-emit JIT work the first time each unique pattern is constructed; for the fixed
        /// regexes that cost is paid once per process and is negligible. For metadata-derived patterns,
        /// which number in the thousands across all regions and are built lazily per pattern the first
        /// time a caller touches that region, paying it unconditionally is the cold-start regression
        /// PhoneRegex.cs's hybrid scheme exists to avoid.
        /// </summary>
        public const RegexOptions
            Default = RegexOptions.Compiled | RegexOptions.CultureInvariant;

        /// <summary>
        /// Options for a <see cref="PhoneRegex"/> pattern's first build: fast to construct (no
        /// RegexOptions.Compiled IL-emit JIT), but slower per-match execution than <see cref="Default"/>.
        /// Used only for the initial build of a metadata-derived pattern in <see cref="PhoneRegex"/>'s
        /// cache; the fixed regexes elsewhere in the library always use <see cref="Default"/> directly,
        /// since they're built once per process regardless.
        /// </summary>
        public const RegexOptions Interpreted = RegexOptions.CultureInvariant;
    }
}