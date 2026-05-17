using Bunit;
using PhoneNumbers.Demo.Pages;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Pages;

public class ParseValidatePageTests : TestContext
{
    [Fact]
    public void shows_valid_badge_for_default_uk_number()
    {
        var cut = RenderComponent<ParseValidate>();

        var badge = cut.Find(".badge--success");
        Assert.Equal("Valid", badge.TextContent.Trim());
    }

    [Fact]
    public void shows_parsed_properties_for_valid_number()
    {
        var cut = RenderComponent<ParseValidate>();

        var rows = cut.FindAll(".data-table__row");
        Assert.NotEmpty(rows);

        var keys = rows.Select(r => r.QuerySelector(".data-table__key")?.TextContent.Trim()).ToList();
        Assert.Contains("Country Code", keys);
        Assert.Contains("National Number", keys);
        Assert.Contains("Region", keys);
    }

    [Fact]
    public void shows_region_info_section_for_valid_number()
    {
        var cut = RenderComponent<ParseValidate>();

        var cardTitles = cut.FindAll(".card__title").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains(cardTitles, t => t.StartsWith("Region Info:"));
    }

    [Fact]
    public void shows_error_message_for_unparseable_input()
    {
        var cut = RenderComponent<ParseValidate>();

        cut.Find("#parse-phone").Input("NOT_A_NUMBER");

        var alert = cut.Find("[role='alert']");
        Assert.NotEmpty(alert.TextContent.Trim());
    }

    [Fact]
    public void shows_empty_state_when_input_is_blank()
    {
        var cut = RenderComponent<ParseValidate>();

        cut.Find("#parse-phone").Input("");

        cut.Find(".empty-state__message");
        Assert.Empty(cut.FindAll(".data-table__row"));
    }

    [Fact]
    public void shows_invalid_badge_for_invalid_number()
    {
        var cut = RenderComponent<ParseValidate>();

        cut.Find("#parse-phone").Input("+1 555");

        var badge = cut.Find(".badge--error");
        Assert.Equal("Invalid", badge.TextContent.Trim());
    }

    [Fact]
    public void shows_number_type_in_info_badge()
    {
        var cut = RenderComponent<ParseValidate>();

        var infoBadge = cut.Find(".badge--info");
        Assert.NotEmpty(infoBadge.TextContent.Trim());
    }

    [Fact]
    public void clicking_us_example_button_shows_us_results()
    {
        var cut = RenderComponent<ParseValidate>();

        cut.Find("button:first-child").Click();

        var rows = cut.FindAll(".data-table__row");
        var regionRow = rows.FirstOrDefault(r => r.QuerySelector(".data-table__key")?.TextContent.Trim() == "Region");
        Assert.NotNull(regionRow);
        Assert.Contains("US", regionRow.QuerySelector(".data-table__value")?.TextContent ?? "");
    }

    [Fact]
    public void shows_possible_reason_badge()
    {
        var cut = RenderComponent<ParseValidate>();

        var neutralBadge = cut.Find(".badge--neutral");
        Assert.NotEmpty(neutralBadge.TextContent.Trim());
    }

    [Fact]
    public void shows_validation_section_with_is_valid_and_is_possible()
    {
        var cut = RenderComponent<ParseValidate>();

        var labels = cut.FindAll(".result-grid__label").Select(l => l.TextContent.Trim()).ToList();
        Assert.Contains("Is Valid", labels);
        Assert.Contains("Is Possible", labels);
    }
}
