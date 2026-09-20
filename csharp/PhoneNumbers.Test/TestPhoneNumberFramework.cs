using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#endif
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// The framework hooks in PhoneNumber.Framework.cs: Java-parity ToString, the explicit
    /// IParsable implementation, and the TypeConverter. Uses the real shipped metadata, because
    /// that is what a consumer's framework binds against.
    /// </summary>
    public class TestPhoneNumberFramework
    {
        private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

        // Java's Phonenumber.PhoneNumber.toString() output for the same fields, so the two ports
        // stay diffable: "Country Code: 44 National Number: 2070313000".
        [Fact]
        public void ToStringMatchesJavaFormat()
        {
            var number = PhoneUtil.Parse("+442070313000", null);

            Assert.Equal("Country Code: 44 National Number: 2070313000", number.ToString());
        }

        [Fact]
        public void ToStringReportsASingleLeadingZeroWithoutACount()
        {
            // Java sets italian_leading_zero but leaves number_of_leading_zeros at its default for a
            // single zero, printing only "Leading Zero(s): true".
            var number = PhoneUtil.Parse("+390212345678", null);

            Assert.True(number.HasNumberOfLeadingZeros);
            Assert.Equal(1, number.NumberOfLeadingZeros);
            Assert.Equal("Country Code: 39 National Number: 212345678 Leading Zero(s): true", number.ToString());
        }

        [Fact]
        public void ToStringReportsTheCountForMoreThanOneLeadingZero()
        {
            var number = PhoneNumber.CreateBuilder()
                .SetCountryCode(39)
                .SetNationalNumber(212345678UL)
                .SetNumberOfLeadingZeros(2)
                .Build();

            Assert.Equal(
                "Country Code: 39 National Number: 212345678 Leading Zero(s): true Number of leading zeros: 2",
                number.ToString());
        }

        [Fact]
        public void ToStringIncludesTheOptionalFieldsJavaPrints()
        {
            var number = PhoneNumber.CreateBuilder()
                .SetCountryCode(1)
                .SetNationalNumber(6194002404UL)
                .SetExtension("1234")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN)
                .SetPreferredDomesticCarrierCode("15")
                .Build();

            Assert.Equal(
                "Country Code: 1 National Number: 6194002404 Extension: 1234 "
                + "Country Code Source: FROM_NUMBER_WITH_PLUS_SIGN Preferred Domestic Carrier Code: 15",
                number.ToString());
        }

        [Fact]
        public void ToStringIsCultureInvariant()
        {
            CultureInfo swedish;
            try
            {
                // sv-SE formats a negative number with U+2212 MINUS SIGN rather than U+002D, so a
                // country code built negative shows whether the current culture leaked in. Skipped
                // where the runtime has no culture data (globalization-invariant mode).
                swedish = new CultureInfo("sv-SE");
                if (swedish.NumberFormat.NegativeSign != "\u2212")
                {
                    return;
                }
            }
            catch (CultureNotFoundException)
            {
                return;
            }

            var number = PhoneNumber.CreateBuilder().SetCountryCode(-44).SetNationalNumber(1UL).Build();
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = swedish;

                Assert.Equal("Country Code: -44 National Number: 1", number.ToString());
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        // The port folds Java's italian_leading_zero and number_of_leading_zeros into one field, so a
        // hand-built number can reach a state Java would print differently. Parse never produces one
        // (it sets the count to 1 for a single zero), but these pin what the folded field does print.
        [Theory]
        [InlineData(0, "Country Code: 39 National Number: 212345678")]
        [InlineData(1, "Country Code: 39 National Number: 212345678 Leading Zero(s): true")]
        [InlineData(2, "Country Code: 39 National Number: 212345678 Leading Zero(s): true Number of leading zeros: 2")]
        [InlineData(255, "Country Code: 39 National Number: 212345678 Leading Zero(s): true Number of leading zeros: 255")]
        public void ToStringPrintsTheFoldedLeadingZeroField(int numberOfLeadingZeros, string expected)
        {
            var number = PhoneNumber.CreateBuilder()
                .SetCountryCode(39)
                .SetNationalNumber(212345678UL)
                .SetNumberOfLeadingZeros(numberOfLeadingZeros)
                .Build();

            Assert.Equal(expected, number.ToString());
        }

        [Fact]
        public void ToStringDoesNotThrowOnAnEmptyNumber()
            => Assert.Equal("Country Code: 0 National Number: 0", new PhoneNumber().ToString());

#if NET8_0_OR_GREATER
        // The interface is implemented explicitly, so a consumer reaches it through the constraint
        // exactly as ASP.NET Core's binders and generic parsing code do.
        private static T ParseViaConstraint<T>(string s) where T : IParsable<T> => T.Parse(s, null);

        private static bool TryParseViaConstraint<T>(string? s, out T? result) where T : IParsable<T>
            => T.TryParse(s, null, out result);

        [Fact]
        public void IParsableParsesInternationalFormat()
        {
            var number = ParseViaConstraint<PhoneNumber>("+442070313000");

            Assert.Equal(44, number.CountryCode);
            Assert.Equal(2070313000UL, number.NationalNumber);
        }

        [Fact]
        public void IParsableAcceptsRfc3966AsWellAsE164()
        {
            // Documented as "input carrying its own country code", which includes tel: URIs.
            var number = ParseViaConstraint<PhoneNumber>("tel:+44-20-7031-3000");

            Assert.Equal(PhoneUtil.Parse("+442070313000", null), number);
        }

        [Fact]
        public void IParsableThrowsOnInvalidInput()
            => Assert.Throws<NumberParseException>(() => ParseViaConstraint<PhoneNumber>("junk"));

        [Theory]
        [InlineData("+442070313000", true)]
        [InlineData("+1 619 400 2404", true)]
        [InlineData("junk", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        // No region can be inferred, so a national-format number cannot parse here.
        [InlineData("020 7031 3000", false)]
        public void IParsableTryParseReportsSuccessAndNullsOnFailure(string? input, bool expected)
        {
            var parsed = TryParseViaConstraint<PhoneNumber>(input, out var number);

            Assert.Equal(expected, parsed);
            if (expected)
            {
                Assert.NotNull(number);
            }
            else
            {
                Assert.Null(number);
            }
        }

        [Fact]
        public void IParsableTryParseSwallowsOnlyParseFailures()
        {
            // Hostile input reaches the same Parse path the rest of the library guards; anything
            // other than a clean false would be a bug (see TestPublicApiRobustness).
            foreach (var input in new[] { "\0", "+", "＋＋", new string('9', 1000), "tel:+1-800-555-0100" })
            {
                var parsed = TryParseViaConstraint<PhoneNumber>(input, out var number);
                Assert.Equal(parsed, number is not null);
            }
        }
#endif

        [Fact]
        public void TypeConverterRoundTripsThroughE164()
        {
            var converter = TypeDescriptor.GetConverter(typeof(PhoneNumber));

            Assert.True(converter.CanConvertFrom(typeof(string)));
            Assert.True(converter.CanConvertTo(typeof(string)));

            var number = (PhoneNumber?)converter.ConvertFrom("+442070313000");

            Assert.NotNull(number);
            Assert.Equal("+442070313000", converter.ConvertTo(number, typeof(string)));
            // Equal to a parsed instance: both go through Parse, so neither carries RawInput.
            Assert.Equal(PhoneUtil.Parse("+442070313000", null), number);
        }

        [Fact]
        public void TypeConverterRefusesTypesItCannotConvert()
        {
            var converter = TypeDescriptor.GetConverter(typeof(PhoneNumber));
            var number = PhoneUtil.Parse("+442070313000", null);

            Assert.False(converter.CanConvertFrom(typeof(int)));
            Assert.False(converter.CanConvertTo(typeof(int)));
            // The base TypeConverter's documented failure for an unsupported conversion.
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(42));
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null!));
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(number, typeof(int)));
        }

        [Fact]
        public void TypeConverterThrowsFormatExceptionOnInvalidInput()
        {
            var converter = TypeDescriptor.GetConverter(typeof(PhoneNumber));

            var exception = Assert.Throws<FormatException>(() => converter.ConvertFrom("junk"));

            Assert.IsType<NumberParseException>(exception.InnerException);
        }

#if NET10_0_OR_GREATER
        [Fact]
        public void ConfigurationBindsAPhoneNumberProperty()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Sms:From"] = "+442070313000",
                })
                .Build();

            var options = configuration.GetSection("Sms").Get<SmsOptions>();

            Assert.NotNull(options);
            Assert.Equal(PhoneUtil.Parse("+442070313000", null), options!.From);
        }

        [Fact]
        public void ConfigurationSurfacesAnInvalidValueAsAnError()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Sms:From"] = "junk",
                })
                .Build();

            // Loud rather than silently null, which is what binding did before the TypeConverter.
            var exception = Assert.Throws<InvalidOperationException>(
                () => configuration.GetSection("Sms").Get<SmsOptions>());

            Assert.IsType<FormatException>(exception.InnerException);
        }

        [Theory]
        [InlineData("/call/%2B442070313000", HttpStatusCode.OK, "+442070313000")]
        [InlineData("/call/junk", HttpStatusCode.BadRequest, null)]
        public async Task MinimalApiBindsARouteParameter(string path, HttpStatusCode expectedStatus, string? expectedBody)
        {
            await using var host = await StartTestHostAsync(app =>
                app.MapGet("/call/{number}", (PhoneNumber number) => PhoneUtil.Format(number, PhoneNumberFormat.E164)));

            var response = await host.Client.GetAsync(host.BaseAddress + path.TrimStart('/'));

            Assert.Equal(expectedStatus, response.StatusCode);
            if (expectedBody is not null)
            {
                Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task MvcBindsAQueryParameter()
        {
            await using var host = await StartTestHostAsync(app => app.MapControllers(),
                services => services.AddControllers().AddApplicationPart(typeof(TestPhoneNumberFramework).Assembly));

            var ok = await host.Client.GetAsync(host.BaseAddress + "mvc/call?number=%2B442070313000");
            var bad = await host.Client.GetAsync(host.BaseAddress + "mvc/call?number=junk");

            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Equal("+442070313000", await ok.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        }

        /// <summary>
        /// Starts a real Kestrel host on an ephemeral loopback port. TestServer would need the
        /// Microsoft.AspNetCore.TestHost package; the shared framework alone is enough this way, and
        /// binding port 0 keeps concurrent test runs from colliding.
        /// </summary>
        private static async Task<TestApp> StartTestHostAsync(Action<WebApplication> configure,
            Action<IServiceCollection>? configureServices = null)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            configureServices?.Invoke(builder.Services);

            var app = builder.Build();
            configure(app);
            await app.StartAsync();
            return new TestApp(app);
        }

        private sealed class TestApp : IAsyncDisposable
        {
            private readonly WebApplication app;

            public TestApp(WebApplication app)
            {
                this.app = app;
                BaseAddress = app.Urls.First().TrimEnd('/') + "/";
                Client = new HttpClient();
            }

            public string BaseAddress { get; }

            public HttpClient Client { get; }

            public async ValueTask DisposeAsync()
            {
                Client.Dispose();
                await app.DisposeAsync();
            }
        }

#endif

#if NET10_0_OR_GREATER
        private sealed class SmsOptions
        {
            public PhoneNumber? From { get; set; }
        }
#endif
    }

#if NET10_0_OR_GREATER
    [ApiController]
    public class PhoneNumberBindingController : ControllerBase
    {
        [HttpGet("/mvc/call")]
        public string Call([FromQuery] PhoneNumber number)
            => PhoneNumberUtil.GetInstance().Format(number, PhoneNumberFormat.E164);
    }
#endif
}
