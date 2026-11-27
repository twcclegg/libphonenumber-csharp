using System;
using System.ComponentModel;

namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// Obsolete: use <see cref="PhoneNumberFormatting"/> from namespace <c>PhoneNumbers</c>:
    /// <c>number.Format(PhoneNumberFormat.E164)</c> and <c>number.IsValidNumber()</c>.
    /// </summary>
    /// <remarks>
    /// C#-idiomatic extension methods over <see cref="PhoneNumbers.PhoneNumber"/> for the formatting
    /// and validity checks callers reach for most often, so they read as
    /// <c>number.ToE164()</c> / <c>number.IsValid()</c> instead of
    /// <c>PhoneNumberUtil.GetInstance().Format(number, PhoneNumberFormat.E164)</c>.
    /// </remarks>
    [Obsolete(Obsoletions.PhoneNumberExtensionsMessage, DiagnosticId = Obsoletions.PhoneNumberExtensionsDiagId, UrlFormat = Obsoletions.UrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PhoneNumberExtensions
    {

        /// <summary>Formats <paramref name="number"/> as E.164, e.g. "+16194002404".</summary>
        public static string ToE164(this PhoneNumbers.PhoneNumber number)
            => number.Format(PhoneNumberFormat.E164);

        /// <summary>Formats <paramref name="number"/> in national format, e.g. "(619) 400-2404".</summary>
        public static string ToNationalFormat(this PhoneNumbers.PhoneNumber number)
            => number.Format(PhoneNumberFormat.NATIONAL);

        /// <summary>Formats <paramref name="number"/> in international format, e.g. "+1 619-400-2404".</summary>
        public static string ToInternationalFormat(this PhoneNumbers.PhoneNumber number)
            => number.Format(PhoneNumberFormat.INTERNATIONAL);

        /// <summary>
        /// Equivalent to <see cref="PhoneNumberUtil.IsValidNumber"/>, as an extension method on an
        /// already-parsed <see cref="PhoneNumbers.PhoneNumber"/>.
        /// </summary>
        public static bool IsValid(this PhoneNumbers.PhoneNumber number)
            => number.IsValidNumber();
    }
}
