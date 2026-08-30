using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace PhoneNumbers.Extensions
{
    /// <summary>
    /// Source-generated <see cref="JsonSerializerContext"/> for <see cref="PhoneNumbers.PhoneNumber"/>,
    /// so consumers building trimmed or Native AOT apps do not need to hand-write one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The metadata this context generates for <see cref="PhoneNumbers.PhoneNumber"/> is a plain,
    /// member-based (reflection-free) serializer — it knows nothing about
    /// <see cref="PhoneNumberConverter"/>. Do not use it to actually serialize a
    /// <see cref="PhoneNumbers.PhoneNumber"/>: <see cref="PhoneNumbers.PhoneNumber"/>.DefaultInstanceForType is a
    /// public get-only property that returns the type's static default instance, which is itself a
    /// <see cref="PhoneNumbers.PhoneNumber"/> exposing the same DefaultInstanceForType property, so
    /// member-based serialization walks straight into infinite recursion (a
    /// <see cref="System.InvalidOperationException"/> for exceeding the writer's max depth, or a real
    /// stack overflow at larger depth limits) instead of ever producing JSON. This context exists only
    /// so <see cref="PhoneNumbers.PhoneNumber"/> can be *resolved*
    /// (given a <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo"/> to satisfy the type
    /// graph) when it appears as a member of another type covered by source generation — e.g. a
    /// consumer's own <see cref="JsonSerializerContext"/> combined with this one via
    /// <see cref="JsonTypeInfoResolver"/>.Combine — provided a converter is also registered so that
    /// resolved-but-broken metadata is never actually invoked.
    /// </para>
    /// <para>
    /// <b>Actual serialization always goes through <see cref="PhoneNumberConverter"/>, never through
    /// the metadata generated here.</b> Prefer <see cref="PhoneNumberJsonOptions.Default"/> (or
    /// <see cref="PhoneNumberJsonOptions.Create"/>), which wires both together correctly. If you build
    /// your own <see cref="JsonSerializerOptions"/> instead, you must set both:
    /// <code>
    /// options.TypeInfoResolver = PhoneNumberJsonContext.Default;
    /// options.Converters.Add(new PhoneNumberConverter());
    /// </code>
    /// and always serialize via <c>JsonSerializer.Serialize(value, options)</c> /
    /// <c>JsonSerializer.Deserialize(json, typeof(T), options)</c> — never via the raw
    /// <c>PhoneNumberJsonContext.Default.PhoneNumber</c> <see cref="JsonTypeInfo{T}"/> directly. That
    /// overload uses the <see cref="JsonTypeInfo{T}"/>'s own baked-in (member-based) converter and
    /// bypasses <see cref="JsonSerializerOptions.Converters"/> entirely, hitting the recursion above.
    /// </para>
    /// </remarks>
    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(PhoneNumbers.PhoneNumber))]
    public partial class PhoneNumberJsonContext : JsonSerializerContext
    {
    }
}
