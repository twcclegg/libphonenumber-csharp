using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using HomePage = PhoneNumbers.Demo.Pages.Home;

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
        Rerender(cut);

        Assert.Contains("blazor-focus-on-navigate", cut.Markup);
    }

    [Fact]
    public void keeps_focus_untouched_when_the_page_rerenders_without_navigating()
    {
        var cut = RenderComponent();

        Rerender(cut);

        Assert.Empty(cut.Markup);
    }

    private IRenderedComponent<FocusOnRouteChange> RenderComponent() =>
        Render<FocusOnRouteChange>(p => p
            .Add(c => c.RouteData, NewRouteData())
            .Add(c => c.Selector, "h1"));

    private void NavigateTo(string uri) =>
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

    // The router hands the component a fresh RouteData on every render, so mirror that.
    private static void Rerender(IRenderedComponent<FocusOnRouteChange> cut) =>
        cut.Render(p => p.Add(c => c.RouteData, NewRouteData()));

    private static RouteData NewRouteData() => new(typeof(HomePage), new Dictionary<string, object?>());
}
