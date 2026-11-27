#if !NET5_0_OR_GREATER
namespace System
{
    /// <summary>
    /// netstandard2.0 copy of <c>System.ObsoleteAttribute</c> with the <see cref="DiagnosticId"/>
    /// and <see cref="UrlFormat"/> properties .NET 5 added.
    /// </summary>
    /// <remarks>
    /// The compiler matches <c>ObsoleteAttribute</c> by full name, not by assembly, so marking the
    /// old API with this copy makes .NET Framework and other netstandard2.0 consumers see the
    /// per-type <c>PNX000n</c> diagnostic and its docs link instead of a generic CS0618. One
    /// <c>NoWarn</c> id then works on every target framework. It deliberately shadows the
    /// netstandard.dll type, which is why the project suppresses CS0436.
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor |
        AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event |
        AttributeTargets.Interface | AttributeTargets.Delegate,
        Inherited = false)]
    internal sealed class ObsoleteAttribute : Attribute
    {
        public ObsoleteAttribute()
        {
        }

        public ObsoleteAttribute(string? message) => Message = message;

        // CA1019 wants a property named after "error"; the BCL's is IsError, and the compiler
        // reads this constructor by position, so the shape has to match the real attribute.
#pragma warning disable CA1019
        public ObsoleteAttribute(string? message, bool error)
#pragma warning restore CA1019
        {
            Message = message;
            IsError = error;
        }

        public string? Message { get; }

        public bool IsError { get; }

        public string? DiagnosticId { get; set; }

        public string? UrlFormat { get; set; }
    }
}
#endif
