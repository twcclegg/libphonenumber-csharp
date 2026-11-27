namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// The <c>[Obsolete]</c> messages and diagnostic ids for the original
    /// <c>PhoneNumbers.Extensions</c> API, kept together so the ids, the dates and the docs anchors
    /// stay in step with <c>docs/diagnostics.md</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="UrlFormat"/> is compiled into every consumer's warnings, so the page and its
    /// anchors must stay where they are for as long as any build can still see these types. The
    /// anchors are explicit <c>&lt;a id="PNX0001"&gt;</c> elements because DocFX lower-cases the ids
    /// it generates from headings, and <c>{0}</c> is replaced with the id exactly as written here.
    /// </para>
    /// <para>
    /// PNX0001's message carries the date from which <see cref="PhoneNumber"/>, the one old type
    /// whose name collides with a core type, is removed from the reference assembly (it stays in
    /// the runtime assembly, so compiled code keeps running). Moving that date means changing it
    /// here, in <c>docs/diagnostics.md</c> and in the migration guide together.
    /// </para>
    /// </remarks>
    internal static class Obsoletions
    {
        internal const string UrlFormat = "https://twcclegg.github.io/libphonenumber-csharp/docs/articles/diagnostics.html#{0}";

        internal const string PhoneNumberDiagId = "PNX0001";
        internal const string PhoneNumberMessage =
            "PhoneNumbers.Extensions.PhoneNumber is obsolete and will be hidden from compilation in the first release on or after 2027-03-01 " +
            "(code already compiled against it keeps running). Use PhoneNumber.TryParse / TryParseValid from namespace PhoneNumbers on C# 14, " +
            "or PhoneNumberParser.TryParse / TryParseValid on earlier versions, and remove 'using PhoneNumbers.Extensions;'.";

        internal const string PhoneNumberExtensionsDiagId = "PNX0002";
        internal const string PhoneNumberExtensionsMessage =
            "PhoneNumberExtensions is obsolete. Use number.Format(PhoneNumberFormat.E164 / NATIONAL / INTERNATIONAL) and number.IsValidNumber() " +
            "from namespace PhoneNumbers.";

        internal const string JsonDiagId = "PNX0003";
        internal const string JsonMessage =
            "The PhoneNumbers.Extensions JSON types are obsolete. Use PhoneNumberJsonConverter from namespace PhoneNumbers.Text.Json, " +
            "registered with options.AddPhoneNumberConverter() or listed on your own JsonSerializerContext.";

        internal const string PhoneNumberAttributeDiagId = "PNX0004";
        internal const string PhoneNumberAttributeMessage =
            "PhoneNumberAttribute is obsolete. Use [ValidPhoneNumber(DefaultRegion = \"...\")] from namespace " +
            "PhoneNumbers.ComponentModel.DataAnnotations.";

        internal const string PhoneNumberTypeConverterDiagId = "PNX0005";
        internal const string PhoneNumberTypeConverterMessage =
            "PhoneNumberTypeConverter is obsolete. PhoneNumber has a built-in TypeConverter; delete the TypeDescriptor.AddAttributes call " +
            "that registers this one.";
    }
}
