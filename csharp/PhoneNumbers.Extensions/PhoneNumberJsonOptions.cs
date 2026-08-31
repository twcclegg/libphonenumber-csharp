using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// Ready-made <see cref="JsonSerializerOptions"/> for serializing <see cref="PhoneNumbers.PhoneNumber"/>
    /// under Native AOT / trimming, where the reflection-based default resolver is unavailable.
    /// Combines <see cref="PhoneNumberJsonContext"/> (source-generated type metadata) with
    /// <see cref="PhoneNumberConverter"/> (the actual read/write logic) so callers cannot wire the two
    /// together incorrectly — see the remarks on <see cref="PhoneNumberJsonContext"/> for why doing it
    /// by hand is easy to get wrong.
    /// </summary>
    public static class PhoneNumberJsonOptions
    {
        /// <summary>
        /// A shared, ready-to-use <see cref="JsonSerializerOptions"/> instance for a bare
        /// <see cref="PhoneNumbers.PhoneNumber"/>, e.g.
        /// <c>JsonSerializer.Serialize(number, PhoneNumberJsonOptions.Default)</c>. Instances of
        /// <see cref="JsonSerializerOptions"/> are safe to reuse concurrently once first used, so this
        /// is safe to share across a whole application.
        /// </summary>
        public static JsonSerializerOptions Default { get; } = Create();

        /// <summary>
        /// Builds a new <see cref="JsonSerializerOptions"/> with <see cref="PhoneNumberJsonContext"/>
        /// and <see cref="PhoneNumberConverter"/> wired together correctly. Use this instead of
        /// <see cref="Default"/> when you need to layer in your own settings (e.g. combining
        /// <see cref="PhoneNumberJsonContext.Default"/> with your own <see cref="JsonSerializerContext"/>
        /// via <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver"/>.Combine for a
        /// DTO that has a <see cref="PhoneNumbers.PhoneNumber"/> property), or when you don't want to
        /// mutate the shared <see cref="Default"/> instance.
        /// </summary>
        /// <param name="baseOptions">
        /// Optional options to copy other settings from (e.g. <see cref="JsonSerializerOptions.WriteIndented"/>).
        /// If <paramref name="baseOptions"/> already has a <see cref="JsonSerializerOptions.TypeInfoResolver"/>
        /// set (e.g. your own <see cref="JsonSerializerContext"/>, or a resolver already combined via
        /// <c>JsonTypeInfoResolver.Combine</c>), it is combined with
        /// <see cref="PhoneNumberJsonContext.Default"/> rather than replaced, so a DTO type resolved by your
        /// own context can still contain a <see cref="PhoneNumbers.PhoneNumber"/> property. A
        /// <see cref="PhoneNumberConverter"/> is always appended to <see cref="JsonSerializerOptions.Converters"/>.
        /// </param>
        public static JsonSerializerOptions Create(JsonSerializerOptions baseOptions = null)
        {
            var options = baseOptions is null ? new JsonSerializerOptions() : new JsonSerializerOptions(baseOptions);
            options.TypeInfoResolver = options.TypeInfoResolver is { } existingResolver
                ? JsonTypeInfoResolver.Combine(existingResolver, PhoneNumberJsonContext.Default)
                : PhoneNumberJsonContext.Default;
            options.Converters.Add(new PhoneNumberConverter());
            return options;
        }

        /// <summary>
        /// Serializes a <see cref="PhoneNumbers.PhoneNumber"/> to its E.164 JSON string using
        /// <see cref="Default"/>. Prefer this over calling
        /// <c>JsonSerializer.Serialize(number, PhoneNumberJsonOptions.Default)</c> yourself: that call
        /// goes through the <see cref="JsonSerializerOptions"/>-based overload, which the trimming/AOT
        /// analyzers always flag (IL2026/IL3050) because they cannot see that <see cref="Default"/>
        /// never actually falls back to reflection. This method carries that guarantee instead, so a
        /// consumer project with trim/AOT analysis on gets no warning for using it.
        /// </summary>
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming",
            "IL2026:RequiresUnreferencedCode",
            Justification = "Default always resolves PhoneNumber via PhoneNumberJsonContext + PhoneNumberConverter, never reflection.")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT",
            "IL3050:RequiresDynamicCode",
            Justification = "Default always resolves PhoneNumber via PhoneNumberJsonContext + PhoneNumberConverter, never reflection.")]
#endif
        public static string Serialize(PhoneNumbers.PhoneNumber number)
            => JsonSerializer.Serialize(number, Default);

        /// <summary>
        /// Deserializes a <see cref="PhoneNumbers.PhoneNumber"/> from its E.164 JSON string using
        /// <see cref="Default"/>. See <see cref="Serialize"/> for why this is preferable to calling
        /// <c>JsonSerializer.Deserialize&lt;PhoneNumber&gt;(json, PhoneNumberJsonOptions.Default)</c>
        /// directly under trim/AOT analysis.
        /// </summary>
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming",
            "IL2026:RequiresUnreferencedCode",
            Justification = "Default always resolves PhoneNumber via PhoneNumberJsonContext + PhoneNumberConverter, never reflection.")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT",
            "IL3050:RequiresDynamicCode",
            Justification = "Default always resolves PhoneNumber via PhoneNumberJsonContext + PhoneNumberConverter, never reflection.")]
#endif
        public static PhoneNumbers.PhoneNumber Deserialize(string json)
            => JsonSerializer.Deserialize<PhoneNumbers.PhoneNumber>(json, Default);
    }
}
