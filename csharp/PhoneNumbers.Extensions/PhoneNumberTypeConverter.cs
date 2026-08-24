using System;
using System.ComponentModel;
using System.Globalization;

namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// Converts a <see cref="PhoneNumbers.PhoneNumber"/> to and from its E.164 string
    /// representation. Not applied automatically — register it where needed, e.g.
    /// <c>TypeDescriptor.AddAttributes(typeof(PhoneNumbers.PhoneNumber), new TypeConverterAttribute(typeof(PhoneNumberTypeConverter)));</c>
    /// </summary>
    public class PhoneNumberTypeConverter : TypeConverter
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => value is string stringValue
                ? Util.ParseAndKeepRawInput(stringValue, null)
                : base.ConvertFrom(context, culture, value);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
            => value is PhoneNumbers.PhoneNumber phoneNumber && destinationType == typeof(string)
                ? Util.Format(phoneNumber, PhoneNumberFormat.E164)
                : base.ConvertTo(context, culture, value, destinationType);
    }
}
