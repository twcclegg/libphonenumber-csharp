namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// C#-idiomatic extension methods over <see cref="PhoneNumbers.PhoneNumber"/> for the formatting
    /// and validity checks callers reach for most often, so they read as
    /// <c>number.ToE164()</c> / <c>number.IsValid()</c> instead of
    /// <c>PhoneNumberUtil.GetInstance().Format(number, PhoneNumberFormat.E164)</c>.
    /// </summary>
    public static class PhoneNumberExtensions
    {
        private static readonly PhoneNumberUtil PhoneNumberUtil = PhoneNumberUtil.GetInstance();

        /// <summary>Formats <paramref name="number"/> as E.164, e.g. "+16194002404".</summary>
        public static string ToE164(this PhoneNumbers.PhoneNumber number)
            => PhoneNumberUtil.Format(number, PhoneNumberFormat.E164);

        /// <summary>Formats <paramref name="number"/> in national format, e.g. "(619) 400-2404".</summary>
        public static string ToNationalFormat(this PhoneNumbers.PhoneNumber number)
            => PhoneNumberUtil.Format(number, PhoneNumberFormat.NATIONAL);

        /// <summary>Formats <paramref name="number"/> in international format, e.g. "+1 619-400-2404".</summary>
        public static string ToInternationalFormat(this PhoneNumbers.PhoneNumber number)
            => PhoneNumberUtil.Format(number, PhoneNumberFormat.INTERNATIONAL);

        /// <summary>
        /// Equivalent to <see cref="PhoneNumberUtil.IsValidNumber"/>, as an extension method on an
        /// already-parsed <see cref="PhoneNumbers.PhoneNumber"/>.
        /// </summary>
        public static bool IsValid(this PhoneNumbers.PhoneNumber number)
            => PhoneNumberUtil.IsValidNumber(number);
    }
}
