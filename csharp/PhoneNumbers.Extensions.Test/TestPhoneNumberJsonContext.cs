using System;
using System.Text.Json;
using Xunit;

namespace PhoneNumbers.Extensions.Test
{
    public class TestPhoneNumberJsonContext
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        [Theory]
        [InlineData("+16194002404", "+16194002404", null)]
        [InlineData("+16194002404", "6194002404", "US")]
        [InlineData("+448443351801", "+448443351801", null)]
        [InlineData("+448443351801", "0844 335 1801", "GB")]
        [InlineData("+380445004973", "0445004973", "UA")]
#if NET6_0_OR_GREATER
        public void DefaultOptions_RoundTripsThroughSourceGenContext(string expected, string input, string? region)
#else
        public void DefaultOptions_RoundTripsThroughSourceGenContext(string expected, string input, string region)
#endif
        {
            var number = Util.Parse(input, region);

            var json = JsonSerializer.Serialize(number, PhoneNumberJsonOptions.Default);
            // Compare via the plain-string reading, not the raw JSON text: System.Text.Json's
            // default encoder escapes '+' (as "+") for HTML/JS safety, so the literal text
            // isn't `"+E164..."` even though it decodes to that string.
            Assert.Equal(expected, JsonSerializer.Deserialize<string>(json));

            var roundTripped = JsonSerializer.Deserialize<PhoneNumbers.PhoneNumber>(json, PhoneNumberJsonOptions.Default);
            Assert.Equal(expected, Util.Format(roundTripped!, PhoneNumberFormat.E164));
        }

        [Fact]
        public void Default_ResolvesTypeInfoThroughContext()
        {
            // The TypeInfoResolver must actually be able to produce a JsonTypeInfo for PhoneNumber
            // (i.e. PhoneNumberJsonContext really does cover it) rather than silently falling back
            // to reflection, which would defeat the point under trimming/AOT.
            var typeInfo = PhoneNumberJsonOptions.Default.GetTypeInfo(typeof(PhoneNumbers.PhoneNumber));

            Assert.NotNull(typeInfo);
            Assert.Same(PhoneNumberJsonContext.Default, PhoneNumberJsonOptions.Default.TypeInfoResolver);
        }

        [Fact]
        public void Create_CopiesBaseOptionsAndStillWiresConverter()
        {
            var baseOptions = new JsonSerializerOptions { WriteIndented = true };

            var options = PhoneNumberJsonOptions.Create(baseOptions);

            Assert.True(options.WriteIndented);
            var number = Util.Parse("+16194002404", null);
            var json = JsonSerializer.Serialize(number, options);
            Assert.Equal("+16194002404", JsonSerializer.Deserialize<string>(json));
        }

        [Fact]
        public void RawContext_BypassesConverterAndRecursesForever()
        {
            // Documents the gotcha in PhoneNumberJsonContext's remarks. PhoneNumber.DefaultInstanceForType
            // is a public get-only property that returns `this`; the source-generated member-based
            // serializer for PhoneNumber (used when you go through the raw JsonTypeInfo<T> instead of
            // through options wired with PhoneNumberConverter) walks it and recurses without end,
            // rather than ever producing the E.164 string PhoneNumberConverter would.
            var number = Util.Parse("+16194002404", null);

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(number, PhoneNumberJsonContext.Default.PhoneNumber));
        }
    }
}
