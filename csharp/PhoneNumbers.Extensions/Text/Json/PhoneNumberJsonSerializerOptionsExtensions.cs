using System;
using System.Text.Json;

namespace PhoneNumbers.Text.Json
{
    /// <summary>
    /// Registers <see cref="PhoneNumberJsonConverter"/> on a <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public static class PhoneNumberJsonSerializerOptionsExtensions
    {
        /// <summary>
        /// Adds a <see cref="PhoneNumberJsonConverter"/> to <paramref name="options"/>, so every
        /// <see cref="PhoneNumber"/> it serializes, at any depth, is an E.164 JSON string.
        /// </summary>
        /// <param name="options">The options to configure. Must not have been used yet.</param>
        /// <returns><paramref name="options"/>, for chaining.</returns>
        /// <remarks>
        /// <para>
        /// For ASP.NET Core: <c>builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.AddPhoneNumberConverter())</c>
        /// (minimal APIs) or <c>.AddJsonOptions(o => o.JsonSerializerOptions.AddPhoneNumberConverter())</c> (MVC).
        /// </para>
        /// <para>
        /// The options keep whatever <see cref="JsonSerializerOptions.TypeInfoResolver"/> they have.
        /// Under trimming or Native AOT that must be your own source-generated context; registering
        /// the converter on the context with <c>[JsonSourceGenerationOptions(Converters = ...)]</c>
        /// works too and needs no call here.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="options"/> has already been used to serialize, and is read-only.</exception>
        public static JsonSerializerOptions AddPhoneNumberConverter(this JsonSerializerOptions options)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            options.Converters.Add(new PhoneNumberJsonConverter());
            return options;
        }
    }
}
