using System.ComponentModel.DataAnnotations;

namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// Validates that a property, field, or parameter is a valid phone number, using the same
    /// validity check as <see cref="PhoneNumberUtil.IsValidNumber"/> rather than a loose format
    /// regex. Accepts a <see cref="string"/> (parsed via <see cref="Region"/>) or an already-parsed
    /// <see cref="PhoneNumbers.PhoneNumber"/>.
    /// </summary>
    public sealed class PhoneNumberAttribute : ValidationAttribute
    {
        public PhoneNumberAttribute() : base("The field {0} is not a valid phone number.")
        {
        }

        /// <summary>
        /// Default region used to interpret a national-format string, e.g. "US". Not needed for
        /// numbers already in E.164 format (leading "+"), or when the value is a
        /// <see cref="PhoneNumbers.PhoneNumber"/>.
        /// </summary>
        public string Region { get; set; }

        public override bool IsValid(object value)
            => value switch
            {
                null => true,
                string stringValue => PhoneNumber.TryParseValid(stringValue, Region, out _),
                PhoneNumbers.PhoneNumber phoneNumber => PhoneNumberUtil.GetInstance().IsValidNumber(phoneNumber),
                _ => false,
            };
    }
}
