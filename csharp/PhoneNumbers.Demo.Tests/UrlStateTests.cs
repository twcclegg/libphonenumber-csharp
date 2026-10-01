using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PhoneNumbers.Demo;
using Xunit;

namespace PhoneNumbers.Demo.Tests;

public class UrlStateTests : BunitContext
{
    // A stand-in for the library's region list, so the region rules are tested without metadata.
    private static readonly IReadOnlySet<string> Regions = new HashSet<string> { "GB", "US" };

    private NavigationManager Nav => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void reads_number_and_region_from_query()
    {
        Nav.NavigateTo("/?n=%2B44%2020%207946%200958&r=GB");

        var (number, region) = UrlState.Read(Nav);

        Assert.Equal("+44 20 7946 0958", number);
        Assert.Equal("GB", region);
    }

    [Fact]
    public void returns_nulls_when_query_is_absent()
    {
        Nav.NavigateTo("/");

        var (number, region) = UrlState.Read(Nav);

        Assert.Null(number);
        Assert.Null(region);
    }

    [Fact]
    public void upper_cases_a_lower_case_region()
    {
        Nav.NavigateTo("/?r=gb");

        var (_, region) = UrlState.Read(Nav, Regions);

        Assert.Equal("GB", region);
    }

    [Theory]
    [InlineData("JP")]
    [InlineData("001")]
    [InlineData("%20")]
    public void ignores_a_region_that_is_not_supported(string raw)
    {
        Nav.NavigateTo("/?n=123&r=" + raw);

        var (number, region) = UrlState.Read(Nav, Regions);

        Assert.Equal("123", number);
        Assert.Null(region);
    }

    [Fact]
    public void checks_the_region_against_the_library_by_default()
    {
        Nav.NavigateTo("/?r=jp");

        var (_, region) = UrlState.Read(Nav);

        Assert.Equal("JP", region);
    }

    [Theory]
    [InlineData("/parse?n=123&r=gb", "parse?n=123&r=GB")]
    [InlineData("/parse?n=123&r=JP", "parse?n=123")]
    public void normalize_url_rewrites_a_region_that_read_had_to_change(string url, string expected)
    {
        Nav.NavigateTo(url);

        UrlState.NormalizeUrl(Nav, Regions);

        Assert.Equal(Nav.BaseUri + expected, Nav.Uri);
    }

    [Theory]
    [InlineData("/parse?n=123&r=GB")]
    [InlineData("/parse?n=123")]
    [InlineData("/")]
    public void normalize_url_leaves_a_link_that_is_already_normal(string url)
    {
        Nav.NavigateTo(url);
        var before = Nav.Uri;

        UrlState.NormalizeUrl(Nav, Regions);

        Assert.Equal(before, Nav.Uri);
    }

    [Fact]
    public void build_omits_empty_number_but_keeps_region()
    {
        Nav.NavigateTo("/parse");

        var url = UrlState.Build(Nav, "", "US");

        Assert.Equal("parse?r=US", url);
    }

    [Fact]
    public void build_escapes_number_and_keeps_current_route()
    {
        Nav.NavigateTo("/geo?n=old");

        var url = UrlState.Build(Nav, "+44 20", "GB");

        Assert.StartsWith("geo?", url);
        Assert.Contains("n=%2B44%2020", url);
        Assert.Contains("r=GB", url);
    }
}
