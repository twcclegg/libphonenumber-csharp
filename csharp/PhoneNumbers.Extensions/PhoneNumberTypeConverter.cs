using System;
using System.ComponentModel;
using System.Globalization;

namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// Converts a <see cref="PhoneNumbers.PhoneNumber"/> to and from its E.164 string
    /// representation.
    /// </summary>
    /// <remarks>
    /// No longer needed: <see cref="PhoneNumbers.PhoneNumber"/> carries a built-in
    /// <see cref="TypeConverter"/> from the core package, which <c>TypeDescriptor</c>-based binding
    /// uses without any registration. Remove any
    /// <c>TypeDescriptor.AddAttributes(typeof(PhoneNumbers.PhoneNumber), new TypeConverterAttribute(typeof(PhoneNumberTypeConverter)))</c>
    /// call, which replaces the built-in converter with this one.
    /// </remarks>
    public class PhoneNumberTypeConverter : TypeConverter
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is not string stringValue)
            {
                return base.ConvertFrom(context, culture, value);
            }

            try
            {
                return Util.Parse(stringValue, null);
            }
            catch (NumberParseException ex)
            {
                throw new FormatException($"'{stringValue}' is not a valid phone number.", ex);
            }
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
            => value is PhoneNumbers.PhoneNumber phoneNumber && destinationType == typeof(string)
                ? Util.Format(phoneNumber, PhoneNumberFormat.E164)
                : base.ConvertTo(context, culture, value, destinationType);
    }
}
