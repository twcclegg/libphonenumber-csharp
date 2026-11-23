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
    /// A successful parse says nothing about validity. Use <see cref="TryParseValid(string, string, out PhoneNumber)"/>,
    /// or check <c>number.IsValidNumber()</c>, before trusting a number from user input.
    /// </para>
    /// </remarks>
    public static class PhoneNumberParser
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

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
