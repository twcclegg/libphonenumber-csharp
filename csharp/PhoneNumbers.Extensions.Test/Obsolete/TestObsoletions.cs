// Names the obsolete types on purpose, to read their attributes.
#pragma warning disable PNX0001, PNX0002, PNX0003, PNX0004, PNX0005
using System;
using System.ComponentModel;
using System.Reflection;
using Xunit;

namespace PhoneNumbers.Test.Extensions
{
    /// <summary>
    /// The contract of the obsolete API's warnings: a per-type diagnostic id consumers can
    /// suppress individually, a docs link built from it, the date in PNX0001, and hiding from
    /// IntelliSense (and so from the docs site's API reference).
    /// </summary>
    public class TestObsoletions
    {
        public static TheoryData<Type, string> ObsoleteTypes => new()
        {
            { typeof(PhoneNumbers.Extensions.PhoneNumber), "PNX0001" },
            { typeof(PhoneNumbers.Extensions.PhoneNumberExtensions), "PNX0002" },
            { typeof(PhoneNumbers.Extensions.PhoneNumberConverter), "PNX0003" },
            { typeof(PhoneNumbers.Extensions.PhoneNumberJsonContext), "PNX0003" },
            { typeof(PhoneNumbers.Extensions.PhoneNumberJsonOptions), "PNX0003" },
            { typeof(PhoneNumbers.Extensions.PhoneNumberAttribute), "PNX0004" },
            { typeof(PhoneNumbers.Extensions.PhoneNumberTypeConverter), "PNX0005" },
        };

        [Theory]
        [MemberData(nameof(ObsoleteTypes))]
        public void ObsoleteWithDiagnosticIdAndDocsLink(Type type, string diagnosticId)
        {
            var obsolete = type.GetCustomAttribute<ObsoleteAttribute>();

            Assert.NotNull(obsolete);
            Assert.False(obsolete.IsError);
            Assert.Equal(diagnosticId, obsolete.DiagnosticId);
            Assert.Equal("https://twcclegg.github.io/libphonenumber-csharp/docs/articles/diagnostics.html#{0}", obsolete.UrlFormat);
            Assert.False(string.IsNullOrWhiteSpace(obsolete.Message));
        }

        [Theory]
        [MemberData(nameof(ObsoleteTypes))]
        public void HiddenFromIntelliSense(Type type, string diagnosticId)
        {
            Assert.StartsWith("PNX", diagnosticId, StringComparison.Ordinal);
            Assert.Equal(EditorBrowsableState.Never, type.GetCustomAttribute<EditorBrowsableAttribute>()?.State);
        }

        [Fact]
        public void Pnx0001StatesTheHideDate()
        {
            var message = typeof(PhoneNumbers.Extensions.PhoneNumber).GetCustomAttribute<ObsoleteAttribute>()!.Message;

            Assert.Contains("first release on or after 2027-03-01", message, StringComparison.Ordinal);
        }

        [Fact]
        public void NoReplacementIsObsolete()
        {
            foreach (var type in new[]
            {
                typeof(PhoneNumberParser), typeof(PhoneNumberFormatting),
                typeof(PhoneNumbers.Text.Json.PhoneNumberJsonConverter),
                typeof(PhoneNumbers.Text.Json.PhoneNumberJsonSerializerOptionsExtensions),
                typeof(PhoneNumbers.ComponentModel.DataAnnotations.ValidPhoneNumberAttribute),
            })
            {
                Assert.Null(type.GetCustomAttribute<ObsoleteAttribute>());
                Assert.Null(type.GetCustomAttribute<EditorBrowsableAttribute>());
            }
        }

        [Fact]
        public void OldTryParseValid_StillReturnsTheParsedNumberWhenInvalid()
        {
            // The forwarding kept the old out-value behaviour; only the new member nulls it.
            Assert.False(PhoneNumbers.Extensions.PhoneNumber.TryParseValid("1235557704", "US", out var number));
            Assert.NotNull(number);

            Assert.False(PhoneNumber.TryParseValid("1235557704", "US", out PhoneNumber? newNumber));
            Assert.Null(newNumber);
        }
    }
}
