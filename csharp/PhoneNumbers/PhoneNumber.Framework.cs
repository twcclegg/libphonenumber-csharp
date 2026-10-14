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
    /// Because the type now converts from a string, frameworks that decide how to treat a type by
    /// asking that question treat <see cref="PhoneNumber"/> as a simple value rather than an object:
    /// </para>
    /// <list type="bullet">
    /// <item><description>ASP.NET Core MVC (<c>[ApiController]</c>) and minimal APIs infer a
    /// <see cref="PhoneNumber"/> parameter from the route or query string, not the request body. An
    /// endpoint that read one from a JSON body without an explicit attribute needs
    /// <c>[FromBody]</c>.</description></item>
    /// <item><description>Newtonsoft.Json reads and writes it as an E.164 string instead of an
    /// object. JSON written by earlier versions in the object shape no longer deserializes.
    /// System.Text.Json is unaffected.</description></item>
    /// </list>
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
        /// <para>
        /// This is for logs and debuggers, not for display or storage: it is not a phone number
        /// format and does not round-trip. Use <see cref="PhoneNumberUtil.Format(PhoneNumber, PhoneNumberFormat)"/>
        /// for a formatted number. Reads only fields already on this instance, so it never loads
        /// metadata and is safe to call from a debugger.
        /// </para>
        /// <para>
        /// Output matches Java's for every number <see cref="PhoneNumberUtil.Parse(string, string)"/> and
        /// <see cref="PhoneNumberUtil.ParseAndKeepRawInput(string, string)"/> can produce. It can differ for a
        /// number assembled by hand through <see cref="Builder"/>, because Java stores
        /// <c>italian_leading_zero</c> and <c>number_of_leading_zeros</c> as two fields and this port
        /// folds them into <see cref="NumberOfLeadingZeros"/> alone — for example
        /// <c>SetNumberOfLeadingZeros(1)</c> prints "Leading Zero(s): true" here where Java would print
        /// "Number of leading zeros: 1". See <c>TestPhoneNumberFramework</c> for the cases.
        /// </para>
        /// <para>
        /// Like Java's, this does not include <see cref="RawInput"/>, so two instances that
        /// <see cref="Equals(PhoneNumber)"/> reports as different can print identically.
        /// </para>
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
                outputString.Append(" Country Code Source: ").Append(CountryCodeSource);
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
        /// <para>
        /// Parses like <c>PhoneNumberUtil.GetInstance().Parse(s, null)</c>: with no region to fall back
        /// on, the input must carry its own country calling code — E.164 ("+442070313000") or an
        /// RFC 3966 URI ("tel:+44-20-7031-3000") — and a national-format number fails.
        /// <paramref name="provider"/> is ignored. For region-aware parsing use
        /// <see cref="PhoneNumberUtil.Parse(string, string)"/>.
        /// </para>
        /// <para>
        /// Unlike <c>PhoneNumberUtil.Parse</c>, failures follow the <c>IParsable&lt;T&gt;</c> contract that
        /// generic callers catch: <see cref="ArgumentNullException"/> for a null <paramref name="s"/>, and
        /// <see cref="FormatException"/> for unparseable input, with the
        /// <see cref="NumberParseException"/> (and its <see cref="NumberParseException.ErrorType"/>) as
        /// the inner exception.
        /// </para>
        /// </remarks>
        static PhoneNumber IParsable<PhoneNumber>.Parse(string s, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            try
            {
                return PhoneNumberUtil.GetInstance().Parse(s, null);
            }
            catch (NumberParseException ex)
            {
                throw new FormatException("The input is not a valid international phone number.", ex);
            }
        }

        /// <inheritdoc />
        /// <remarks>See the remarks on the <c>Parse</c> implementation for the accepted input.</remarks>
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
    /// Converts a <see cref="PhoneNumber"/> from a string and to its E.164 string form, so that
    /// <c>TypeDescriptor</c>-based infrastructure — configuration binding
    /// (<c>IOptions&lt;T&gt;</c>), property grids, MVC model binding, Newtonsoft.Json — can read and
    /// write one without any registration by the consumer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Internal on purpose: it is reached through the <see cref="TypeConverterAttribute"/> on
    /// <see cref="PhoneNumber"/>, so it adds no public API. Strings must carry their own country
    /// calling code (E.164 or an RFC 3966 <c>tel:</c> URI), matching the <c>IParsable&lt;T&gt;</c>
    /// implementation; an unconvertible string throws <see cref="FormatException"/>, which is what
    /// <c>TypeDescriptor</c>-based binders expect. An empty string converts to <see langword="null"/>,
    /// so a blank optional configuration value leaves the property unset rather than failing startup.
    /// </para>
    /// <para>
    /// The string form is E.164, which holds only the country calling code and national number.
    /// Converting to a string therefore drops <see cref="PhoneNumber.Extension"/>,
    /// <see cref="PhoneNumber.PreferredDomesticCarrierCode"/>, <see cref="PhoneNumber.CountryCodeSource"/>
    /// and <see cref="PhoneNumber.RawInput"/>: a number with an extension does not round-trip. Store
    /// a <c>RFC3966</c>-formatted string yourself where the extension matters.
    /// </para>
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

            // ConfigurationBinder only short-circuits "" for Nullable<T>; for a reference type it
            // hands the empty string here, and a blank optional setting must not crash startup.
            if (stringValue.Length == 0)
            {
                return null;
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
