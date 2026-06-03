using Bunit;
using PhoneNumbers.Demo.Pages;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Pages;

public class LiveFormatterPageTests : TestContext
{
    [Fact]
    public void shows_placeholder_when_no_digits_entered()
    {
        var cut = RenderComponent<LiveFormatter>();

        var placeholder = cut.Find(".live-formatter__placeholder");
        Assert.Equal("Formatted output will appear here", placeholder.TextContent.Trim());
    }

    [Fact]
    public void output_has_no_active_modifier_when_empty()
    {
        var cut = RenderComponent<LiveFormatter>();

        var output = cut.Find(".live-formatter__output");
        Assert.False(output.ClassList.Contains("live-formatter__output--active"));
    }

    [Fact]
    public void output_gains_active_modifier_after_typing_digits()
    {
        var cut = RenderComponent<LiveFormatter>();

        cut.Find("#live-input").Input("1650");

        var output = cut.Find(".live-formatter__output--active");
        Assert.NotEmpty(output.TextContent.Trim());
    }

    [Fact]
    public void shows_step_by_step_formatting_after_input()
    {
        var cut = RenderComponent<LiveFormatter>();

        cut.Find("#live-input").Input("1650");

        var steps = cut.FindAll("#live-steps .format-list__item");
        Assert.Equal(4, steps.Count);
    }

    [Fact]
    public void each_step_shows_the_character_that_was_input()
    {
        var cut = RenderComponent<LiveFormatter>();

        cut.Find("#live-input").Input("123");

        var stepNames = cut.FindAll(".format-list__name").Select(n => n.TextContent.Trim()).ToList();
        Assert.Contains(stepNames, n => n.Contains("'1'"));
        Assert.Contains(stepNames, n => n.Contains("'2'"));
        Assert.Contains(stepNames, n => n.Contains("'3'"));
    }

    [Fact]
    public void strips_non_digit_non_plus_characters_from_input()
    {
        var cut = RenderComponent<LiveFormatter>();

        // Input contains letters and spaces which should be stripped
        cut.Find("#live-input").Input("abc 123 def");

        // Only 3 digit steps should appear (letters and spaces stripped)
        var steps = cut.FindAll("#live-steps .format-list__item");
        Assert.Equal(3, steps.Count);
    }

    [Fact]
    public void clearing_input_removes_steps_and_shows_placeholder()
    {
        var cut = RenderComponent<LiveFormatter>();

        cut.Find("#live-input").Input("1650");
        cut.Find("#live-input").Input("");

        cut.Find(".live-formatter__placeholder");
        Assert.Empty(cut.FindAll(".format-list__item"));
    }

    [Fact]
    public void shows_region_selector()
    {
        var cut = RenderComponent<LiveFormatter>();

        var select = cut.Find("#live-region");
        Assert.NotNull(select);
    }

    [Fact]
    public void caret_tracking_prompts_to_start_typing_when_empty()
    {
        var cut = RenderComponent<LiveFormatter>();

        Assert.Contains("Start typing to watch the remembered caret position",
            cut.Markup);
    }

    [Fact]
    public void caret_tracking_reports_a_remembered_position_after_typing()
    {
        var cut = RenderComponent<LiveFormatter>();

        cut.Find("#live-input").Input("1650");

        // The badge reads "index N of M", reported once the formatter has a position to remember.
        var badge = cut.Find(".result-grid .badge--accent").TextContent.Trim();
        Assert.Matches(@"^index \d+ of \d+$", badge);
    }

    [Fact]
    public void formatted_output_is_split_around_a_caret_marker()
    {
        var cut = RenderComponent<LiveFormatter>();

        cut.Find("#live-input").Input("1650");

        var output = cut.Find(".live-formatter__output--active");
        Assert.NotNull(output.QuerySelector(".live-formatter__caret"));
        Assert.Equal(2, output.QuerySelectorAll(".live-formatter__text").Length);
    }

    [Fact]
    public void remembered_caret_position_never_exceeds_the_formatted_length()
    {
        var cut = RenderComponent<LiveFormatter>();

        cut.Find("#live-input").Input("6502530000");

        var badge = cut.Find(".result-grid .badge--accent").TextContent.Trim();
        var parts = badge.Replace("index ", "").Split(" of ");
        var caret = int.Parse(parts[0]);
        var length = int.Parse(parts[1]);
        Assert.True(caret <= length, $"caret {caret} should not exceed length {length}");
    }

    [Fact]
    public void links_to_parse_validate_for_full_analysis()
    {
        var cut = RenderComponent<LiveFormatter>();

        var link = cut.Find("a.live-formatter__link");
        Assert.Equal("parse", link.GetAttribute("href"));
    }

    [Fact]
    public void example_chip_populates_input_and_formats_it()
    {
        var cut = RenderComponent<LiveFormatter>();

        cut.FindAll("button.btn--chip").First(b => b.TextContent.Contains("US number")).Click();

        var input = cut.Find("#live-input");
        Assert.Equal("6502530000", input.GetAttribute("value"));

        // The live formatter output should be populated (active) after the chip fills the input.
        var output = cut.Find(".live-formatter__output--active");
        Assert.NotEmpty(output.TextContent.Trim());
    }
}
