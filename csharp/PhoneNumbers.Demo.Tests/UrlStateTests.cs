using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PhoneNumbers.Demo;
using Xunit;

namespace PhoneNumbers.Demo.Tests;

public class UrlStateTests : TestContext
{
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
