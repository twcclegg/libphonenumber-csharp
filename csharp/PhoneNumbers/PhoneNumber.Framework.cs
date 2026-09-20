#nullable enable
using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace PhoneNumbers
{
    /// <summary>
    /// The parts of <see cref="PhoneNumber"/> that exist so .NET frameworks can discover the type:
    /// a <see cref="TypeConverter"/> (configuration binding, <c>TypeDescriptor</c>, property grids)
    /// and, on .NET 8.0 and later, <c>IParsable&lt;T&gt;</c> (ASP.NET Core minimal API and MVC
    /// parameter binding, and generic <c>T : IParsable&lt;T&gt;</c> code).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are hooks the frameworks look for <em>on the type itself</em>; nothing outside the
    /// assembly can supply them, which is why they live here rather than in the
    /// <c>libphonenumber-csharp.extensions</c> package with the rest of the C#-shaped API. The
    /// interface is implemented <em>explicitly</em>, so this file adds no named member that could
    /// shadow the richer, region-aware <c>PhoneNumber.TryParse</c> that package provides.
    /// </para>
    /// <para>
    /// Kept apart from the ported <c>Phonenumber.cs</c> so an upstream sync never has to reconcile
    /// it; the only change to that file is the <c>partial</c> keyword.
    /// </para>
    /// </remarks>
    [TypeConverter(typeof(PhoneNumberTypeConverter))]
    public sealed partial class PhoneNumber
#if NET8_0_OR_GREATER
        : IParsable<PhoneNumber>
#endif
    {
        /// <summary>
        /// Returns a diagnostic view of the fields that are set, matching Java's
        /// <c>Phonenumber.PhoneNumber.toString()</c>, e.g.
        /// <c>"Country Code: 44 National Number: 2070313000"</c>.
        /// </summary>
        /// <remarks>
        /// This is for logs and debuggers, not for display or storage: it is not a phone number
        /// format and does not round-trip. Use <see cref="PhoneNumberUtil.Format(PhoneNumber, PhoneNumberFormat)"/>
        /// for a formatted number. Reads only fields already on this instance, so it never loads
        /// metadata and is safe to call from a debugger.
        /// </remarks>
        public override string ToString()
        {
            var outputString = new StringBuilder();
            outputString.Append("Country Code: ").Append(CountryCode.ToString(CultureInfo.InvariantCulture));
            outputString.Append(" National Number: ").Append(NationalNumber.ToString(CultureInfo.InvariantCulture));
            if (HasNumberOfLeadingZeros)
            {
                // Java tracks italian_leading_zero and number_of_leading_zeros as two fields and
                // prints them separately; this port folds them into NumberOfLeadingZeros, where any
                // non-zero value means "has a leading zero" (see
                // PhoneNumberUtil.SetItalianLeadingZerosForPhoneNumber). Java only sets the count
                // when it is not 1, so printing the count only when it is not 1 reproduces Java's
                // output for every number Parse can produce.
                outputString.Append(" Leading Zero(s): true");
                if (NumberOfLeadingZeros != 1)
                {
                    outputString.Append(" Number of leading zeros: ")
                        .Append(NumberOfLeadingZeros.ToString(CultureInfo.InvariantCulture));
                }
            }
            if (HasExtension)
            {
                outputString.Append(" Extension: ").Append(Extension);
            }
            if (HasCountryCodeSource)
            {
                outputString.Append(" Country Code Source: ").Append(CountryCodeSource.ToString());
            }
            if (HasPreferredDomesticCarrierCode)
            {
                outputString.Append(" Preferred Domestic Carrier Code: ").Append(PreferredDomesticCarrierCode);
            }
            return outputString.ToString();
        }

#if NET8_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>
        /// Equivalent to <c>PhoneNumberUtil.GetInstance().Parse(s, null)</c>: the number must be in
        /// international form ("+..."), since no region can be inferred here. <paramref name="provider"/>
        /// is ignored. For region-aware parsing use <see cref="PhoneNumberUtil.Parse(string, string)"/>.
        /// </remarks>
        static PhoneNumber IParsable<PhoneNumber>.Parse(string s, IFormatProvider? provider)
            => PhoneNumberUtil.GetInstance().Parse(s, null);

        /// <inheritdoc />
        /// <remarks>See the remarks on the <c>Parse</c> implementation: international form only.</remarks>
        static bool IParsable<PhoneNumber>.TryParse(string? s, IFormatProvider? provider,
            [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out PhoneNumber result)
        {
            if (s is null)
            {
                result = null;
                return false;
            }

            try
            {
                result = PhoneNumberUtil.GetInstance().Parse(s, null);
                return true;
            }
            catch (NumberParseException)
            {
                result = null;
                return false;
            }
        }
#endif
    }

    /// <summary>
    /// Converts a <see cref="PhoneNumber"/> to and from its E.164 string form, so that
    /// <c>TypeDescriptor</c>-based infrastructure — configuration binding
    /// (<c>IOptions&lt;T&gt;</c>), property grids, older model binders — can round-trip one without
    /// any registration by the consumer.
    /// </summary>
    /// <remarks>
    /// Internal on purpose: it is reached through the <see cref="TypeConverterAttribute"/> on
    /// <see cref="PhoneNumber"/>, so it adds no public API. Strings must be in international form
    /// ("+..."), matching the <c>IParsable&lt;T&gt;</c> implementation.
    /// </remarks>
    internal sealed class PhoneNumberTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is not string stringValue)
            {
                return base.ConvertFrom(context, culture, value);
            }

            try
            {
                return PhoneNumberUtil.GetInstance().Parse(stringValue, null);
            }
            catch (NumberParseException ex)
            {
                throw new FormatException("'" + stringValue + "' is not a valid international phone number.", ex);
            }
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value,
            Type destinationType)
            => value is PhoneNumber phoneNumber && destinationType == typeof(string)
                ? PhoneNumberUtil.GetInstance().Format(phoneNumber, PhoneNumberFormat.E164)
                : base.ConvertTo(context, culture, value, destinationType);
    }
}
