using Xunit;

namespace PhoneNumbers.Extensions.Test;

public class TestExtensions
{
    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    [Fact]
    public void TestUSNational()
    {
        Assert.True(PhoneNumber.TryParse("6192987704", "US", out var number));
        Assert.True(PhoneNumber.TryParseValid("6192987704", "US", out number));
        Assert.Equal("(619) 298-7704", Util.Format(number, PhoneNumberFormat.NATIONAL));
    }

    [Fact]
    public void TestGBNational()
    {
        Assert.True(PhoneNumber.TryParse("08445717410", "GB", out var number));
        Assert.True(PhoneNumber.TryParseValid("08445717410", "GB", out number));
        Assert.Equal("0844 571 7410", Util.Format(number, PhoneNumberFormat.NATIONAL));
    }

    [Fact]
    public void TestE164()
    {
        Assert.True(PhoneNumber.TryParse("+16192987704", null, out var number));
        Assert.True(PhoneNumber.TryParseValid("+16192987704", null, out number));
        Assert.Equal("+16192987704", Util.Format(number, PhoneNumberFormat.E164));
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
}
