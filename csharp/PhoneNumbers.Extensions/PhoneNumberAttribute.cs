using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using PhoneNumbers.ComponentModel.DataAnnotations;

namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// Obsolete: use <see cref="ValidPhoneNumberAttribute"/>, whose <c>DefaultRegion</c> replaces
    /// <see cref="Region"/>.
    /// </summary>
    /// <remarks>
    /// Validates that a property, field, or parameter is a valid phone number, using the same
    /// validity check as <see cref="PhoneNumberUtil.IsValidNumber"/> rather than a loose format
    /// regex. Accepts a <see cref="string"/> (parsed via <see cref="Region"/>) or an already-parsed
    /// <see cref="PhoneNumbers.PhoneNumber"/>.
    /// </remarks>
    [Obsolete(Obsoletions.PhoneNumberAttributeMessage, DiagnosticId = Obsoletions.PhoneNumberAttributeDiagId, UrlFormat = Obsoletions.UrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        public string? Region { get; set; }

        public override bool IsValid(object? value)
            => ValidPhoneNumberAttribute.IsValid(value, Region);
    }
}
