using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneNumbers.ComponentModel.DataAnnotations
{
    /// <summary>
    /// Validates that a property, field or parameter holds a valid phone number, by the same check
    /// as <see cref="PhoneNumberUtil.IsValidNumber(PhoneNumber)"/> rather than a format regex.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value may be a <see cref="string"/>, parsed with <see cref="DefaultRegion"/>, or an
    /// already parsed <see cref="PhoneNumber"/>. Any other type is invalid. <see langword="null"/>
    /// is valid, as with the built-in DataAnnotations attributes; add <see cref="RequiredAttribute"/>
    /// to require a value. An empty string is invalid (ASP.NET Core model binding turns an empty
    /// form or query value into <see langword="null"/> before this runs).
    /// </para>
    /// <para>
    /// For a <see cref="PhoneNumber"/> property, <see cref="DefaultRegion"/> plays no part: the
    /// number was parsed before validation, by whatever bound it (System.Text.Json and model binding
    /// accept international format only). To accept national-format input, bind a
    /// <see cref="string"/> property and validate that.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public record Contact(string Name, [property: ValidPhoneNumber(DefaultRegion = "GB")] string Phone);
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class ValidPhoneNumberAttribute : ValidationAttribute
    {
        /// <summary>
        /// Creates the attribute with the default error message,
        /// <c>"The {0} field is not a valid phone number."</c>.
        /// </summary>
        public ValidPhoneNumberAttribute()
            : base("The {0} field is not a valid phone number.")
        {
        }

        /// <summary>
        /// The two-letter (CLDR) region a national-format string is interpreted in, e.g.
        /// <c>"GB"</c>. When <see langword="null"/> (the default), only strings in international
        /// format (a leading <c>+</c>) can be valid.
        /// </summary>
        public string? DefaultRegion { get; set; }

        /// <inheritdoc />
        public override bool IsValid(object? value)
            => value switch
            {
                null => true,
                string text => PhoneNumberParser.TryParseValid(text, DefaultRegion, out _),
                PhoneNumber number => number.IsValidNumber(),
                _ => false,
            };
    }
}
