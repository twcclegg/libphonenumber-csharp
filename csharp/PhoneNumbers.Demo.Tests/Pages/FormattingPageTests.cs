using Bunit;
using PhoneNumbers.Demo.Pages;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Pages;

public class FormattingPageTests : TestContext
{
    [Fact]
    public void shows_all_four_standard_formats_on_init()
    {
        var cut = RenderComponent<Formatting>();

        var formatNames = cut.FindAll(".format-list__name").Select(n => n.TextContent.Trim()).ToList();
        Assert.Contains("E.164", formatNames);
        Assert.Contains("International", formatNames);
        Assert.Contains("National", formatNames);
        Assert.Contains("RFC 3966", formatNames);
    }

    [Fact]
    public void shows_e164_format_value_for_prepopulated_us_number()
    {
        var cut = RenderComponent<Formatting>();

        var items = cut.FindAll(".format-list__item");
        var e164Item = items.FirstOrDefault(i => i.QuerySelector(".format-list__name")?.TextContent.Trim() == "E.164");
        Assert.NotNull(e164Item);
        Assert.Contains("+16502530000", e164Item.QuerySelector(".format-list__value")?.TextContent ?? "");
    }

    [Fact]
    public void shows_contextual_formats_section()
    {
        var cut = RenderComponent<Formatting>();

        var cardTitles = cut.FindAll(".card__title").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains("Contextual Formats", cardTitles);
    }

    [Fact]
    public void shows_out_of_country_and_mobile_dialing_formats()
    {
        var cut = RenderComponent<Formatting>();

        var formatNames = cut.FindAll(".format-list__name").Select(n => n.TextContent.Trim()).ToList();
        Assert.Contains("Out-of-Country", formatNames);
        Assert.Contains("Mobile Dialing", formatNames);
    }

    [Fact]
    public void shows_error_for_unparseable_input()
    {
        var cut = RenderComponent<Formatting>();

        cut.Find("#format-phone").Input("INVALID");

        cut.Find("[role='alert']");
    }

    [Fact]
    public void shows_empty_state_when_input_cleared()
    {
        var cut = RenderComponent<Formatting>();

        cut.Find("#format-phone").Input("");

        cut.Find(".empty-state__message");
        Assert.Empty(cut.FindAll(".format-list__item"));
    }

    [Fact]
    public void shows_national_significant_number_in_other_properties()
    {
        var cut = RenderComponent<Formatting>();

        var formatNames = cut.FindAll(".format-list__name").Select(n => n.TextContent.Trim()).ToList();
        Assert.Contains("National Significant", formatNames);
        Assert.Contains("Can Dial Intl.", formatNames);
    }

    [Fact]
    public void changing_phone_number_updates_all_formats()
    {
        var cut = RenderComponent<Formatting>();

        cut.Find("#format-phone").Input("+44 20 7946 0958");

        var items = cut.FindAll(".format-list__item");
        var e164Item = items.FirstOrDefault(i => i.QuerySelector(".format-list__name")?.TextContent.Trim() == "E.164");
        Assert.NotNull(e164Item);
        Assert.Contains("+442079460958", e164Item.QuerySelector(".format-list__value")?.TextContent ?? "");
    }
}
