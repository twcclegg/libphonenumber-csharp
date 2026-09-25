using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PhoneNumbers.Demo.Components.Icons;
using PhoneNumbers.Demo.Layout;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Layout;

public class MainLayoutTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();

    public MainLayoutTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        JSInterop.Setup<string>("phoneDemo.getTheme").SetResult("light");
        JSInterop.SetupVoid("phoneDemo.copyText", _ => true).SetVoidResult();
    }

    [Fact]
    public void sidebar_links_every_page_in_order_then_the_reference()
    {
        var cut = Render<MainLayout>();

        var links = cut.FindAll("nav a")
            .Skip(1) // the logo
            .Select(a => (a.GetAttribute("href"), a.TextContent.Trim()))
            .ToList();

        Assert.Equal(
            new (string?, string)[]
            {
                ("", "Home"),
                ("parse", "Parse & Validate"),
                ("format", "Formatting"),
                ("live", "Live Formatter"),
                ("find", "Find Numbers"),
                ("geo", "Geo & Timezone"),
            },
            links.Take(6));
        Assert.Equal(new[] { "API Reference", "Articles", "GitHub", "NuGet" }, links.Skip(6).Select(l => l.Item2));
    }

    [Theory]
    [InlineData("API Reference", "http://localhost/docs/api/PhoneNumbers.html")]
    [InlineData("Articles", "http://localhost/docs/articles/api-differences-from-java.html")]
    public void reference_links_load_the_docs_site_in_the_same_tab(string text, string href)
    {
        var cut = Render<MainLayout>();

        var link = cut.FindAll("nav a").Single(a => a.TextContent.Trim() == text);

        Assert.Equal(href, link.GetAttribute("href"));
        // _top: the same tab, but a target Blazor's router leaves to the browser (see DocsLinks).
        Assert.Equal("_top", link.GetAttribute("target"));
    }

    [Theory]
    [InlineData("/", "Home")]
    [InlineData("/geo?n=%2B44&r=GB", "Geo & Timezone")]
    [InlineData("/nowhere", "Home")]
    public void mobile_topbar_names_the_current_page(string url, string title)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(url);

        var cut = Render<MainLayout>();

        Assert.Equal(title, cut.Find(".topbar__title").TextContent.Trim());
    }

    [Fact]
    public void share_and_theme_buttons_are_in_both_the_sidebar_and_the_mobile_topbar()
    {
        var cut = Render<MainLayout>();

        Assert.Single(cut.FindAll("nav button[aria-label='Copy shareable link']"));
        Assert.Single(cut.FindAll("main button[aria-label='Copy shareable link']"));
        Assert.Single(cut.FindAll("nav button[aria-label='Switch to dark mode']"));
        Assert.Single(cut.FindAll("main button[aria-label='Switch to dark mode']"));
    }

    [Fact]
    public void button_tooltips_match_their_accessible_names_after_a_copy()
    {
        var cut = Render<MainLayout>();

        cut.Find("button[aria-label='Copy shareable link']").Click();

        cut.WaitForAssertion(() =>
        {
            var buttons = cut.FindAll("button[aria-label='Link copied'], button[aria-label='Switch to dark mode']");
            Assert.Equal(4, buttons.Count);
            Assert.All(buttons, b => Assert.Equal(b.GetAttribute("aria-label"), b.GetAttribute("title")));
        });
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

    [Fact]
    public async Task copying_the_link_again_keeps_the_confirmation_for_its_full_time()
    {
        var cut = Render<MainLayout>();

        cut.Find("button[aria-label='Copy shareable link']").Click();
        _time.Advance(TimeSpan.FromMilliseconds(1000));
        cut.Find("button[aria-label='Link copied']").Click();

        // The first click's 1.5s timer expires; the second click's does not.
        _time.Advance(TimeSpan.FromMilliseconds(800));
        await cut.InvokeAsync(() => { }); // let the expired timer's continuation run
        Assert.NotEmpty(cut.FindAll("button[aria-label='Link copied']"));

        _time.Advance(TimeSpan.FromMilliseconds(700));
        await cut.InvokeAsync(() => { });
        Assert.Empty(cut.FindAll("button[aria-label='Link copied']"));
    }
}
