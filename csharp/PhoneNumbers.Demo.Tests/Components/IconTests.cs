using Bunit;
using PhoneNumbers.Demo.Components.Icons;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Components;

public class IconTests : BunitContext
{
    public static TheoryData<Type> AllIcons()
    {
        var data = new TheoryData<Type>();
        foreach (var t in typeof(IconBase).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(IconBase))))
            data.Add(t);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllIcons))]
    public void every_icon_renders_a_single_decorative_svg(Type icon)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, icon);
            builder.CloseComponent();
        });

        var svg = Assert.Single(cut.FindAll("svg"));
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Equal("0 0 24 24", svg.GetAttribute("viewBox"));
        Assert.NotEmpty(svg.Children);
    }

    [Fact]
    public void forwards_unmatched_attributes_to_the_svg()
    {
        var cut = Render<SearchIcon>(p => p.AddUnmatched("data-testid", "search"));

        Assert.Equal("search", cut.Find("svg").GetAttribute("data-testid"));
    }
}
