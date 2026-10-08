using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using HomePage = PhoneNumbers.Demo.Pages.Home;
using ParseValidatePage = PhoneNumbers.Demo.Pages.ParseValidate;

namespace PhoneNumbers.Demo.Tests;

public class FocusOnRouteChangeTests : BunitContext
{
    [Fact]
    public void does_not_move_focus_on_initial_load()
    {
        var cut = RenderComponent();

        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void moves_focus_to_the_heading_after_navigating_to_another_page()
    {
        var cut = RenderComponent();

        NavigateTo("/parse");
        Rerender(cut, typeof(ParseValidatePage));

        Assert.Contains("blazor-focus-on-navigate", cut.Markup);
    }

    [Fact]
    public void keeps_focus_untouched_when_the_page_rerenders_without_navigating()
    {
        var cut = RenderComponent();

        Rerender(cut, typeof(HomePage));

        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void keeps_focus_untouched_when_only_the_query_string_changes_on_the_same_page()
    {
        // Every page syncs its input state into the query string on blur/change (SyncUrl in
        // Home.razor and friends), which is a same-page NavigateTo, not a real navigation. This
        // is the regression this component exists to not have: comparing Nav.Uri instead of
        // the matched page type treated this as the first "navigation" and mounted
        // FocusOnNavigate, stealing focus back to the heading the user had just interacted away
        // from.
        var cut = RenderComponent();

        NavigateTo("/?number=15551234567");
        Rerender(cut, typeof(HomePage));

        Assert.Empty(cut.Markup);
    }

    private IRenderedComponent<FocusOnRouteChange> RenderComponent() =>
        Render<FocusOnRouteChange>(p => p
            .Add(c => c.RouteData, NewRouteData(typeof(HomePage)))
            .Add(c => c.Selector, "h1"));

    private void NavigateTo(string uri) =>
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

    // The router hands the component a fresh RouteData on every render, so mirror that.
    private static void Rerender(IRenderedComponent<FocusOnRouteChange> cut, Type pageType) =>
        cut.Render(p => p.Add(c => c.RouteData, NewRouteData(pageType)));

    private static RouteData NewRouteData(Type pageType) => new(pageType, new Dictionary<string, object?>());
}
