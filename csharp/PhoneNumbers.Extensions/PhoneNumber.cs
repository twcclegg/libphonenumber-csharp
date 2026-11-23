using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// Obsolete: use <c>PhoneNumber.TryParse</c> / <c>TryParseValid</c> from namespace
    /// <c>PhoneNumbers</c> (C# 14), or <see cref="PhoneNumberParser"/> on earlier C# versions.
    /// </summary>
    /// <remarks>
    /// This class shares its simple name with <see cref="PhoneNumbers.PhoneNumber"/>, so a file
    /// importing both namespaces cannot name either (CS0104). From the first release on or after
    /// 2027-03-01 it is removed from the reference assembly and stays in the runtime assembly:
    /// code compiled against it keeps running, but new compilations no longer see it.
    /// </remarks>
    [Obsolete(Obsoletions.PhoneNumberMessage, DiagnosticId = Obsoletions.PhoneNumberDiagId, UrlFormat = Obsoletions.UrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PhoneNumber
    {
        public static bool TryParse(string? input, [NotNullWhen(true)] out PhoneNumbers.PhoneNumber? number)
            => PhoneNumberParser.TryParse(input, null, out number);

        public static bool TryParse(string? input, string? region, [NotNullWhen(true)] out PhoneNumbers.PhoneNumber? number)
            => PhoneNumberParser.TryParse(input, region, out number);

        public static bool TryParseValid(string? input, [NotNullWhen(true)] out PhoneNumbers.PhoneNumber? number)
            => TryParseValid(input, null, out number);

        /// <remarks>
        /// Unlike <see cref="PhoneNumberParser"/>'s <c>TryParseValid</c>, this sets
        /// <paramref name="number"/> to the parsed number when it parses but is not valid, as it
        /// always has.
        /// </remarks>
        public static bool TryParseValid(string? input, string? region, [NotNullWhen(true)] out PhoneNumbers.PhoneNumber? number)
            => PhoneNumberParser.TryParse(input, region, out number) && number.IsValidNumber();
    }
}
