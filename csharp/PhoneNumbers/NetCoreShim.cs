using System.Xml.Linq;

namespace PhoneNumbers {
    internal static class NetCoreShim {
        internal static bool HasAttribute(this XElement element, string attribute)
            => element.Attribute(attribute) != null;

        internal static string GetAttribute(this XElement element, string attribute)
            => element.Attribute(attribute)?.Value ?? string.Empty;
    }
}