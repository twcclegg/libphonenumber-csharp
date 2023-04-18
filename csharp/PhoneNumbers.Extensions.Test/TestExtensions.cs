using System.Collections.Generic;
using System.Text;
using Xunit;
using PhoneNumbers.Extensions;

namespace PhoneNumbers.Extensions.Test
{
    public class TestExtensions
    {
        private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

        public static IEnumerable<object[]> TryParseValidInvalidTestData
        {
            get
            {
                yield return new object[]
                {
                    new string(' ', 251),
                };
            }
        }

        [Fact]
        public void TestUSNational()
        {
            Assert.True(PhoneNumber.TryParse("6192987704", "US", out var number));
            Assert.True(PhoneNumber.TryParseValid("6192987704", "US", out number));
            Assert.Equal("(619) 298-7704", Util.Format(number, PhoneNumberFormat.NATIONAL));
        }

        [Fact]
        public void TestUSNational_Old()
        {
            Assert.True(PhoneNumber.TryParse("6192987704", "US", out var number));
            Assert.True(PhoneNumber.TryParseValid("6192987704", "US", out number));

            var sb = new StringBuilder(20);
            Util.Format(number, PhoneNumberFormat.NATIONAL, sb);
            Assert.Equal("(619) 298-7704", sb.ToString());
        }

        [Fact]
        public void TestGBNational()
        {
            Assert.True(PhoneNumber.TryParse("08445717410", "GB", out var number));
            Assert.True(PhoneNumber.TryParseValid("08445717410", "GB", out number));
            Assert.Equal("0844 571 7410", Util.Format(number, PhoneNumberFormat.NATIONAL));
        }

        [Fact]
        public void TestGBNational_Old()
        {
            Assert.True(PhoneNumber.TryParse("08445717410", "GB", out var number));
            Assert.True(PhoneNumber.TryParseValid("08445717410", "GB", out number));

            var sb = new StringBuilder(20);
            Util.Format(number, PhoneNumberFormat.NATIONAL, sb);
            Assert.Equal("0844 571 7410", sb.ToString());
        }

        [Fact]
        public void TestE164()
        {
            Assert.True(PhoneNumber.TryParse("+16192987704", null, out var number));
            Assert.True(PhoneNumber.TryParseValid("+16192987704", null, out number));
            Assert.Equal("+16192987704", Util.Format(number, PhoneNumberFormat.E164));
        }

        [Fact]
        public void TestE164_Old()
        {
            Assert.True(PhoneNumber.TryParse("+16192987704", null, out var number));
            Assert.True(PhoneNumber.TryParseValid("+16192987704", null, out number));

            var sb = new StringBuilder(20);
            Util.Format(number, PhoneNumberFormat.E164, sb);
            Assert.Equal("+16192987704", sb.ToString());
        }

        [Fact]
        public void TestInvalidCountryCode()
        {
            Assert.False(PhoneNumber.TryParse("1235557704", "ZX", out var number));
            Assert.Null(number);
        }

        [Fact]
        public void TestInvalidNumberForRegion()
        {
            Assert.False(PhoneNumber.TryParseValid("1235557704", "US", out var number));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("+3809")]
        [InlineData("+380123456789012345678")]
        [MemberData(nameof(TryParseValidInvalidTestData))]
        public void TryParseValid_WhenInvalidInput_ThenResultIsFalse(string number)
        {
            var isValid = PhoneNumber.TryParseValid(number, null, out var phoneNumber);

            Assert.False(isValid);
            Assert.Null(phoneNumber);
        }
    }
}
