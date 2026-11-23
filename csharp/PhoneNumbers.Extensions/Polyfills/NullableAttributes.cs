#if !NETCOREAPP3_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    // netstandard2.0 has none of the nullable flow attributes. The compiler recognises these by
    // name, so an internal copy is enough for netstandard2.0 consumers on C# 8+ to see the same
    // annotations as net8.0+ consumers. The modern targets use the BCL's own.

    /// <summary>
    /// The out parameter is not null when the method returns <see cref="ReturnValue"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        public bool ReturnValue { get; }
    }
}
#endif
