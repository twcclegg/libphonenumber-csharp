using System;

namespace PhoneNumbers
{
    /// <summary>
    /// The <see cref="PhoneNumberUtil"/> calls made most often on a parsed number, as extension
    /// methods on <see cref="PhoneNumber"/>: <c>number.Format(PhoneNumberFormat.E164)</c> rather than
    /// <c>PhoneNumberUtil.GetInstance().Format(number, PhoneNumberFormat.E164)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each method keeps the name of the Java <c>PhoneNumberUtil</c> method it calls and behaves
    /// exactly like it, so Google's documentation applies unchanged. They are ordinary extension
    /// methods rather than C# 14 extension members, so they work from any C# version and from
    /// Visual Basic.
    /// </para>
    /// <para>
    /// The names deliberately differ from the obsolete <c>PhoneNumbers.Extensions.PhoneNumberExtensions</c>
    /// methods (<c>ToE164</c>, <c>IsValid</c>, …): reusing them would make a call ambiguous (CS0121)
    /// in a file that still imports both namespaces.
    /// </para>
    /// </remarks>
    public static class PhoneNumberFormatting
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        /// <summary>
        /// Formats <paramref name="number"/> in the given format, e.g. <c>"+442070313000"</c> for
        /// <see cref="PhoneNumberFormat.E164"/> or <c>"020 7031 3000"</c> for
        /// <see cref="PhoneNumberFormat.NATIONAL"/>.
        /// </summary>
        /// <param name="number">The number to format.</param>
        /// <param name="numberFormat">The format to use.</param>
        /// <returns>The formatted number.</returns>
        /// <remarks>Calls <see cref="PhoneNumberUtil.Format(PhoneNumber, PhoneNumberFormat)"/>.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="number"/> is <see langword="null"/>.</exception>
        public static string Format(this PhoneNumber number, PhoneNumberFormat numberFormat)
            => Util.Format(NotNull(number), numberFormat);

        /// <summary>
        /// Whether <paramref name="number"/> is a valid number in the region its country calling code
        /// and national number place it in: it matches a known numbering pattern, not just a
        /// plausible length.
        /// </summary>
        /// <param name="number">The number to check.</param>
        /// <returns><see langword="true"/> if the number is valid; otherwise <see langword="false"/>.</returns>
        /// <remarks>Calls <see cref="PhoneNumberUtil.IsValidNumber(PhoneNumber)"/>.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="number"/> is <see langword="null"/>.</exception>
        public static bool IsValidNumber(this PhoneNumber number)
            => Util.IsValidNumber(NotNull(number));

        /// <summary>
        /// The type of <paramref name="number"/> (mobile, fixed line, toll free, …), or
        /// <see cref="PhoneNumberType.UNKNOWN"/> if it is not a valid number.
        /// </summary>
        /// <param name="number">The number to classify.</param>
        /// <returns>The number's type.</returns>
        /// <remarks>Calls <see cref="PhoneNumberUtil.GetNumberType(PhoneNumber)"/>.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="number"/> is <see langword="null"/>.</exception>
        public static PhoneNumberType GetNumberType(this PhoneNumber number)
            => Util.GetNumberType(NotNull(number));

        /// <summary>
        /// The two-letter (CLDR) region <paramref name="number"/> belongs to, e.g. <c>"GB"</c>;
        /// <c>"001"</c> for a non-geographical number such as +800; or <see langword="null"/> if no
        /// region matches its country calling code.
        /// </summary>
        /// <param name="number">The number to look up.</param>
        /// <returns>The region code, or <see langword="null"/>.</returns>
        /// <remarks>
        /// Calls <see cref="PhoneNumberUtil.GetRegionCodeForNumber(PhoneNumber)"/>. A calling code
        /// shared by several regions (+1, +7, +44, …) is resolved by the national number, which only
        /// works for a valid number; an invalid one may get no region.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="number"/> is <see langword="null"/>.</exception>
        public static string? GetRegionCodeForNumber(this PhoneNumber number)
            => Util.GetRegionCodeForNumber(NotNull(number));

        private static PhoneNumber NotNull(PhoneNumber number)
            => number ?? throw new ArgumentNullException(nameof(number));
    }
}
