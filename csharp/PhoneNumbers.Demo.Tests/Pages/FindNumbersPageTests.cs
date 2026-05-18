using Bunit;
using PhoneNumbers.Demo.Pages;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Pages;

public class FindNumbersPageTests : TestContext
{
    [Fact]
    public void shows_no_results_when_text_is_empty()
    {
        var cut = RenderComponent<FindNumbers>();

        Assert.Empty(cut.FindAll(".match-list__item"));
        Assert.Empty(cut.FindAll(".empty-state__message"));
    }

    [Fact]
    public void shows_empty_state_when_text_has_no_phone_numbers()
    {
        var cut = RenderComponent<FindNumbers>();

        cut.Find("#find-text").Input("Hello world, no numbers here.");

        cut.Find(".empty-state__message");
        Assert.Empty(cut.FindAll(".match-list__item"));
    }

    [Fact]
    public void finds_international_number_in_text()
    {
        var cut = RenderComponent<FindNumbers>();

        cut.Find("#find-text").Input("Call us at +1 650 253 0000 for support.");

        var items = cut.FindAll(".match-list__item");
        Assert.NotEmpty(items);
    }

    [Fact]
    public void shows_raw_matched_string_for_each_found_number()
    {
        var cut = RenderComponent<FindNumbers>();

        cut.Find("#find-text").Input("Call +1 650 253 0000 today.");

        var raw = cut.Find(".match-list__raw");
        Assert.NotEmpty(raw.TextContent.Trim());
    }

    [Fact]
    public void shows_correct_count_heading_for_single_match()
    {
        var cut = RenderComponent<FindNumbers>();

        cut.Find("#find-text").Input("Call +1 650 253 0000.");

        var titles = cut.FindAll(".card__title").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains(titles, t => t.Contains("1 number"));
    }

    [Fact]
    public void shows_plural_count_for_multiple_matches()
    {
        var cut = RenderComponent<FindNumbers>();

        cut.Find("#find-text").Input("Call +1 650 253 0000 or +44 20 7946 0958.");

        var titles = cut.FindAll(".card__title").Select(t => t.TextContent.Trim()).ToList();
        Assert.Contains(titles, t => t.Contains("numbers"));
    }

    [Fact]
    public void shows_valid_badge_for_valid_found_number()
    {
        var cut = RenderComponent<FindNumbers>();

        cut.Find("#find-text").Input("Call +1 650 253 0000.");

        cut.Find(".badge--success");
    }

    [Fact]
    public void shows_position_metadata_for_each_match()
    {
        var cut = RenderComponent<FindNumbers>();

        cut.Find("#find-text").Input("Call +1 650 253 0000 today.");

        var meta = cut.Find(".match-list__meta");
        Assert.Contains("Position", meta.TextContent);
    }

    [Fact]
    public void load_sample_button_populates_text_and_finds_numbers()
    {
        var cut = RenderComponent<FindNumbers>();

        cut.Find("button").Click();

        var items = cut.FindAll(".match-list__item");
        Assert.NotEmpty(items);
    }

    [Fact]
    public void clear_button_removes_text_and_results()
    {
        var cut = RenderComponent<FindNumbers>();

        cut.Find("button").Click(); // Load sample
        var buttons = cut.FindAll("button");
        buttons[1].Click(); // Clear

        Assert.Empty(cut.FindAll(".match-list__item"));
        Assert.Empty(cut.FindAll(".empty-state__message"));
    }
}
