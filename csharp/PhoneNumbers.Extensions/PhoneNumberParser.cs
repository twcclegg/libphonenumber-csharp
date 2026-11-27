using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PhoneNumbers
{
    /// <summary>
    /// Non-throwing parsing for <see cref="PhoneNumber"/>. On C# 14 and later these read as static
    /// members of <see cref="PhoneNumber"/> itself: <c>PhoneNumber.TryParse(s, "GB", out var n)</c>.
    /// On C# 13 and earlier, and from Visual Basic, call them on this class instead:
    /// <c>PhoneNumberParser.TryParse(s, "GB", out var n)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Parsing is <see cref="PhoneNumberUtil.Parse(string, string)"/>'s, unchanged: the same inputs
    /// succeed and produce equal <see cref="PhoneNumber"/> instances. Where <c>Parse</c> would throw
    /// <see cref="NumberParseException"/>, these return <see langword="false"/> instead.
    /// </para>
    /// <para>
    /// A successful parse says nothing about validity. Use <c>TryParseValid</c>, or check
    /// <c>number.IsValidNumber()</c>, before trusting a number from user input.
    /// </para>
    /// <para>
    /// The members, declared in a C# 14 <c>extension(PhoneNumber)</c> block (which the generated API
    /// reference does not list yet):
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>bool TryParse(string? s, out PhoneNumber? result)</c>: international
    /// format only (E.164, <c>"+44 20 7031 3000"</c>, <c>"tel:+44-20-7031-3000"</c>).</description></item>
    /// <item><description><c>bool TryParse(string? s, string? defaultRegion, out PhoneNumber? result)</c>:
    /// also national format, interpreted in <c>defaultRegion</c> (e.g. <c>"GB"</c>).</description></item>
    /// <item><description><c>bool TryParseValid(string? s, out PhoneNumber? result)</c> and
    /// <c>bool TryParseValid(string? s, string? defaultRegion, out PhoneNumber? result)</c>: as above,
    /// and <see langword="true"/> only for a valid number; <c>result</c> is <see langword="null"/>
    /// otherwise.</description></item>
    /// </list>
    /// <para>
    /// After a <see langword="true"/> return, <c>result</c> is not <see langword="null"/>
    /// (<c>[NotNullWhen(true)]</c>).
    /// </para>
    /// </remarks>
    public static class PhoneNumberParser
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        // GetSupportedRegions returns a fresh copy per call; take one for the life of the process.
        private static readonly HashSet<string> SupportedRegions = Util.GetSupportedRegions();

        // PhoneNumberUtil.MAX_INPUT_STRING_LENGTH, which is internal to the core assembly.
        private const int MaxInputLength = 250;

        /// <summary>
        /// Whether <see cref="PhoneNumberUtil.Parse(string, string)"/> could possibly accept
        /// <paramref name="s"/>. A <see langword="false"/> is certain; a <see langword="true"/> only
        /// means it is worth calling <c>Parse</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A failed parse otherwise costs a thrown and caught <see cref="NumberParseException"/>,
        /// as much as a successful parse, so this rejects the input <c>Parse</c> is certain to refuse
        /// without calling it. It only rejects on conditions that <c>Parse</c> checks against text
        /// that is wholly made of characters from <paramref name="s"/>: its working copy is built from
        /// substrings of the input (after stripping leading junk, or after rearranging an RFC 3966
        /// <c>phone-context</c>), so a character missing from the input is missing from it too.
        /// </para>
        /// <list type="bullet">
        /// <item><description><see langword="null"/> or longer than 250 characters: <c>Parse</c>
        /// throws NOT_A_NUMBER or TOO_LONG before anything else.</description></item>
        /// <item><description>Fewer than two decimal digits: <c>IsViablePhoneNumber</c> needs at
        /// least two (<c>\d</c> in .NET regex is <see cref="char.IsDigit(char)"/>, any script).</description></item>
        /// <item><description>No plus sign (<c>+</c> or fullwidth <c>U+FF0B</c>) when
        /// <paramref name="defaultRegion"/> is not a supported region: <c>Parse</c> needs the number to
        /// start with one to know its country, and throws INVALID_COUNTRY_CODE.</description></item>
        /// </list>
        /// <para>
        /// The FsCheck properties in <c>TestPhoneNumberParserPrecheck</c> hold it to never rejecting
        /// anything <c>Parse</c> accepts.
        /// </para>
        /// </remarks>
        internal static bool IsViableCandidate([NotNullWhen(true)] string? s, string? defaultRegion)
        {
            if (s is null || s.Length < 2 || s.Length > MaxInputLength)
            {
                return false;
            }

            var needsPlus = defaultRegion is null || !SupportedRegions.Contains(defaultRegion);
            var digits = 0;
            var hasPlus = false;
            foreach (var c in s)
            {
                if (char.IsDigit(c))
                {
                    digits++;
                }
                else if (c is '+' or '\uFF0B')
                {
                    hasPlus = true;
                }

                if (digits >= 2 && (hasPlus || !needsPlus))
                {
                    return true;
                }
            }

            return false;
        }

        // CA1034 reads an extension block as a public nested type, which it is only in metadata.
#pragma warning disable CA1034
        /// <summary>
        /// Non-throwing parsing members on <see cref="PhoneNumber"/>.
        /// </summary>
        extension(PhoneNumber)
#pragma warning restore CA1034
        {
            /// <summary>
            /// Parses a number written in international format: E.164 (<c>"+442070313000"</c>), a
            /// formatted international number (<c>"+44 20 7031 3000"</c>) or an RFC 3966 URI
            /// (<c>"tel:+44-20-7031-3000"</c>).
            /// </summary>
            /// <param name="s">The text to parse. May be <see langword="null"/>.</param>
            /// <param name="result">The parsed number, or <see langword="null"/> when parsing fails.</param>
            /// <returns><see langword="true"/> if <paramref name="s"/> was parsed; otherwise <see langword="false"/>.</returns>
            /// <remarks>
            /// Equivalent to <c>TryParse(s, null, out result)</c>. A national-format number such as
            /// <c>"020 7031 3000"</c> fails, because there is no region to interpret it in.
            /// </remarks>
            public static bool TryParse(string? s, [NotNullWhen(true)] out PhoneNumber? result)
                => PhoneNumberParser.TryParse(s, null, out result);

            /// <summary>
            /// Parses a phone number, interpreting a national-format number as belonging to
            /// <paramref name="defaultRegion"/>.
            /// </summary>
            /// <param name="s">The text to parse. May be <see langword="null"/>.</param>
            /// <param name="defaultRegion">
            /// The two-letter (CLDR) region the number is expected to be from, e.g. <c>"GB"</c>. Only
            /// used when <paramref name="s"/> is not in international format. <see langword="null"/>
            /// or <c>"ZZ"</c> accepts international numbers only.
            /// </param>
            /// <param name="result">The parsed number, or <see langword="null"/> when parsing fails.</param>
            /// <returns><see langword="true"/> if <paramref name="s"/> was parsed; otherwise <see langword="false"/>.</returns>
            public static bool TryParse(string? s, string? defaultRegion, [NotNullWhen(true)] out PhoneNumber? result)
            {
                if (!IsViableCandidate(s, defaultRegion))
                {
                    result = null;
                    return false;
                }

                try
                {
                    result = Util.Parse(s, defaultRegion);
                    return true;
                }
                catch (NumberParseException)
                {
                    result = null;
                    return false;
                }
            }

            /// <summary>
            /// Parses a number written in international format and checks that it is a valid number,
            /// as <see cref="PhoneNumberUtil.IsValidNumber(PhoneNumber)"/> defines it.
            /// </summary>
            /// <param name="s">The text to parse. May be <see langword="null"/>.</param>
            /// <param name="result">
            /// The parsed number when it is valid; otherwise <see langword="null"/>, including when
            /// <paramref name="s"/> parsed but is not a valid number.
            /// </param>
            /// <returns><see langword="true"/> if <paramref name="s"/> parsed to a valid number; otherwise <see langword="false"/>.</returns>
            public static bool TryParseValid(string? s, [NotNullWhen(true)] out PhoneNumber? result)
                => PhoneNumberParser.TryParseValid(s, null, out result);

            /// <summary>
            /// Parses a phone number, interpreting a national-format number as belonging to
            /// <paramref name="defaultRegion"/>, and checks that it is a valid number, as
            /// <see cref="PhoneNumberUtil.IsValidNumber(PhoneNumber)"/> defines it.
            /// </summary>
            /// <param name="s">The text to parse. May be <see langword="null"/>.</param>
            /// <param name="defaultRegion">
            /// The two-letter (CLDR) region the number is expected to be from, e.g. <c>"GB"</c>. Only
            /// used when <paramref name="s"/> is not in international format. <see langword="null"/>
            /// or <c>"ZZ"</c> accepts international numbers only.
            /// </param>
            /// <param name="result">
            /// The parsed number when it is valid; otherwise <see langword="null"/>, including when
            /// <paramref name="s"/> parsed but is not a valid number.
            /// </param>
            /// <returns><see langword="true"/> if <paramref name="s"/> parsed to a valid number; otherwise <see langword="false"/>.</returns>
            public static bool TryParseValid(string? s, string? defaultRegion, [NotNullWhen(true)] out PhoneNumber? result)
            {
                if (PhoneNumberParser.TryParse(s, defaultRegion, out var number) && Util.IsValidNumber(number))
                {
                    result = number;
                    return true;
                }

                result = null;
                return false;
            }
        }
    }
}
