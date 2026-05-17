using Bunit;
using PhoneNumbers.Demo.Pages;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Pages;

public class HomePageTests : TestContext
{
    [Fact]
    public void shows_valid_badge_for_prepopulated_us_number()
    {
        var cut = RenderComponent<Home>();

        var badge = cut.Find(".badge--success");
        Assert.Equal("Valid", badge.TextContent.Trim());
    }

    [Fact]
    public void shows_e164_format_for_prepopulated_us_number()
    {
        var cut = RenderComponent<Home>();

        var values = cut.FindAll(".result-grid__value--mono");
        Assert.Contains(values, v => v.TextContent.Contains("+16502530000"));
    }

    [Fact]
    public void shows_five_feature_cards()
    {
        var cut = RenderComponent<Home>();

        var cards = cut.FindAll(".feature-grid__card");
        Assert.Equal(5, cards.Count);
    }

    [Fact]
    public void shows_region_count_greater_than_zero()
    {
        var cut = RenderComponent<Home>();

        var badge = cut.Find(".hero__badge:nth-child(2)");
        var text = badge.TextContent.Trim();
        var count = int.Parse(System.Text.RegularExpressions.Regex.Match(text, @"\d+").Value);
        Assert.True(count > 0);
    }

    [Fact]
    public void shows_error_message_for_unparseable_input()
    {
        var cut = RenderComponent<Home>();

        cut.Find("#home-phone").Input("NOTAPHONE");

        cut.Find("[role='alert']");
    }

    [Fact]
    public void hides_results_when_input_is_cleared()
    {
        var cut = RenderComponent<Home>();

        cut.Find("#home-phone").Input("");

        Assert.Empty(cut.FindAll(".result-grid__item"));
        Assert.Empty(cut.FindAll("[role='alert']"));
    }

    [Fact]
    public void shows_invalid_badge_for_structurally_wrong_number()
    {
        var cut = RenderComponent<Home>();

        // A parseable but invalid number (too short)
        cut.Find("#home-phone").Input("+1 555");

        var badge = cut.Find(".badge--error");
        Assert.Equal("Invalid", badge.TextContent.Trim());
    }

    [Fact]
    public void shows_number_type_badge_for_valid_number()
    {
        var cut = RenderComponent<Home>();

        cut.Find(".badge--info");
    }

    [Fact]
    public void changing_region_rerenders_results()
    {
        var cut = RenderComponent<Home>();

        cut.Find("#home-phone").Input("0412 345 678");
        cut.Find("#home-region").Change("AU");

        // Australian local number should parse with AU region
        var items = cut.FindAll(".result-grid__item");
        Assert.NotEmpty(items);
    }

    [Fact]
    public void feature_card_titles_are_present()
    {
        var cut = RenderComponent<Home>();

        var titles = cut.FindAll(".feature-grid__card-title").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains("Parse & Validate", titles);
        Assert.Contains("Formatting", titles);
        Assert.Contains("Live Formatter", titles);
        Assert.Contains("Find Numbers", titles);
        Assert.Contains("Geocoding & Timezone", titles);
    }
}
