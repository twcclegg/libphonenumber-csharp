using Bunit;
using PhoneNumbers.Demo.Components.Icons;
using PhoneNumbers.Demo.Layout;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Layout;

public class MainLayoutTests : BunitContext
{
    public MainLayoutTests()
    {
        JSInterop.Setup<string>("phoneDemo.getTheme").SetResult("light");
        JSInterop.SetupVoid("phoneDemo.copyText", _ => true).SetVoidResult();
    }

    [Fact]
    public void theme_toggle_offers_dark_mode_with_a_moon_icon_by_default()
    {
        var cut = Render<MainLayout>();

        var toggles = cut.FindAll("button[aria-label='Switch to dark mode']");
        Assert.NotEmpty(toggles);
        Assert.All(toggles, b => Assert.Equal(Render<MoonIcon>().Markup, b.QuerySelector("svg")!.OuterHtml));
    }

    [Fact]
    public void theme_toggle_swaps_to_sun_icon_and_light_mode_label_after_switching_to_dark()
    {
        JSInterop.Setup<string>("phoneDemo.toggleTheme").SetResult("dark");
        var cut = Render<MainLayout>();

        cut.Find("button[aria-label='Switch to dark mode']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("button[aria-label='Switch to dark mode']"));
            var toggles = cut.FindAll("button[aria-label='Switch to light mode']");
            Assert.NotEmpty(toggles);
            Assert.All(toggles, b => Assert.Equal(Render<SunIcon>().Markup, b.QuerySelector("svg")!.OuterHtml));
        });
    }

    [Fact]
    public void copy_link_button_confirms_with_link_copied_and_a_check_icon()
    {
        var cut = Render<MainLayout>();

        cut.Find("button[aria-label='Copy shareable link']").Click();

        cut.WaitForAssertion(() =>
        {
            var copied = cut.FindAll("button[aria-label='Link copied']");
            Assert.NotEmpty(copied);
            Assert.All(copied, b => Assert.Equal(Render<CheckIcon>().Markup, b.QuerySelector("svg")!.OuterHtml));
        });
        JSInterop.VerifyInvoke("phoneDemo.copyText");
    }
}
