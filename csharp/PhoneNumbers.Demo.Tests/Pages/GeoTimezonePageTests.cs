using Bunit;
using PhoneNumbers.Demo.Pages;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Pages;

public class GeoTimezonePageTests : TestContext
{
    [Fact]
    public void shows_geographic_description_for_us_mountain_view_number()
    {
        var cut = RenderComponent<GeoTimezone>();

        var labels = cut.FindAll(".result-grid__label").Select(l => l.TextContent.Trim()).ToList();
        Assert.Contains("Description", labels);

        var descriptionItem = cut.FindAll(".result-grid__item")
            .FirstOrDefault(i => i.QuerySelector(".result-grid__label")?.TextContent.Trim() == "Description");
        Assert.NotNull(descriptionItem);
        var descText = descriptionItem.QuerySelector(".result-grid__value")?.TextContent.Trim() ?? "";
        Assert.NotEmpty(descText);
    }

    [Fact]
    public void shows_timezone_badges_for_us_number()
    {
        var cut = RenderComponent<GeoTimezone>();

        var timezoneBadges = cut.FindAll(".badge--accent");
        Assert.NotEmpty(timezoneBadges);
    }

    [Fact]
    public void shows_number_details_section_with_valid_badge()
    {
        var cut = RenderComponent<GeoTimezone>();

        cut.Find(".badge--success");

        var keys = cut.FindAll(".data-table__key").Select(k => k.TextContent.Trim()).ToList();
        Assert.Contains("Formatted", keys);
        Assert.Contains("Valid", keys);
        Assert.Contains("Number Type", keys);
        Assert.Contains("Is Geographical", keys);
    }

    [Fact]
    public void shows_error_for_unparseable_input()
    {
        var cut = RenderComponent<GeoTimezone>();

        cut.Find("#geo-phone").Input("INVALID");

        cut.Find("[role='alert']");
    }

    [Fact]
    public void shows_empty_state_when_input_cleared()
    {
        var cut = RenderComponent<GeoTimezone>();

        cut.Find("#geo-phone").Input("");

        cut.Find(".empty-state__message");
    }

    [Fact]
    public void clicking_uk_example_shows_london_description()
    {
        var cut = RenderComponent<GeoTimezone>();

        var ukButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("UK"));
        Assert.NotNull(ukButton);
        ukButton.Click();

        var descriptionItem = cut.FindAll(".result-grid__item")
            .FirstOrDefault(i => i.QuerySelector(".result-grid__label")?.TextContent.Trim() == "Description");
        Assert.NotNull(descriptionItem);
        var descText = descriptionItem.QuerySelector(".result-grid__value")?.TextContent.Trim() ?? "";
        Assert.NotEmpty(descText);
    }

    [Fact]
    public void shows_carrier_section()
    {
        var cut = RenderComponent<GeoTimezone>();

        var cardTitles = cut.FindAll(".card__title").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains("Carrier", cardTitles);
    }

    [Fact]
    public void shows_geographic_location_section()
    {
        var cut = RenderComponent<GeoTimezone>();

        var cardTitles = cut.FindAll(".card__title").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains("Geographic Location", cardTitles);
        Assert.Contains("Time Zones", cardTitles);
    }
}
