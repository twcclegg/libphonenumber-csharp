using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PhoneNumbers.Demo.Pages;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Pages;

// The contract Home, Parse & Validate, Formatting and Geo & Timezone all rely on, tested once
// through a page that adds nothing of its own.
public class PhoneNumberPageBaseTests : BunitContext
{
    private NavigationManager Nav => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void seeds_number_and_region_from_the_url()
    {
        Nav.NavigateTo("/?n=020%207946%200958&r=GB");

        var page = Render<TestPage>().Instance;

        Assert.Equal("020 7946 0958", page.Input);
        Assert.Equal("GB", page.Region);
        Assert.Equal(44, page.Number?.CountryCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/?r=XX")]
    public void falls_back_to_the_defaults_when_the_url_has_no_usable_values(string url)
    {
        Nav.NavigateTo(url);

        var page = Render<TestPage>().Instance;

        Assert.Equal("+1 650 253 0000", page.Input);
        Assert.Equal("US", page.Region);
    }

    [Fact]
    public void typing_reparses_but_leaves_the_url_alone()
    {
        var cut = Render<TestPage>();
        var before = Nav.Uri;

        cut.InvokeAsync(() => cut.Instance.Type("+44 20 7946 0958"));

        Assert.Equal(44, cut.Instance.Number?.CountryCode);
        Assert.Equal(before, Nav.Uri);
    }

    [Fact]
    public void committing_reparses_and_writes_the_number_to_the_url()
    {
        var cut = Render<TestPage>();

        cut.InvokeAsync(() => cut.Instance.Commit("+44 20 7946 0958"));

        Assert.Equal(44, cut.Instance.Number?.CountryCode);
        Assert.Equal("+44 20 7946 0958", UrlState.Read(Nav).Number);
    }

    [Fact]
    public void changing_region_reparses_and_writes_the_region_to_the_url()
    {
        var cut = Render<TestPage>();
        cut.InvokeAsync(() => cut.Instance.Type("020 7946 0958"));

        cut.InvokeAsync(() => cut.Instance.PickRegion("GB"));

        Assert.Equal(44, cut.Instance.Number?.CountryCode);
        Assert.Equal("GB", UrlState.Read(Nav).Region);
    }

    [Fact]
    public void a_failed_parse_shows_the_error_and_still_reaches_on_parsed_with_null()
    {
        var cut = Render<TestPage>();

        cut.InvokeAsync(() => cut.Instance.Type("not a number"));

        Assert.NotNull(cut.Instance.Error);
        Assert.Null(cut.Instance.Number);
        Assert.Null(cut.Instance.OnParsedCalls[^1]);
    }

    private sealed class TestPage : PhoneNumberPageBase
    {
        public List<PhoneNumber?> OnParsedCalls { get; } = new();

        protected override string DefaultNumber => "+1 650 253 0000";
        protected override string DefaultRegion => "US";

        protected override void OnParsed(PhoneNumber? number) => OnParsedCalls.Add(number);

        public string Input => PhoneInput;
        public string Region => SelectedRegion;
        public PhoneNumber? Number => Parsed;
        public string? Error => ParseError;

        public void Type(string value) => OnPhoneInput(new ChangeEventArgs { Value = value });
        public void Commit(string value) => OnPhoneCommit(new ChangeEventArgs { Value = value });
        public void PickRegion(string region) => OnRegionChange(region);
    }
}
