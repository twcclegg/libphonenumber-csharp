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
        public const RegexOptions
            Default = RegexOptions.Compiled | RegexOptions.CultureInvariant;
    }
}