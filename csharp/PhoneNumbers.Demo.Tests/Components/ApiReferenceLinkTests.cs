using Bunit;
using PhoneNumbers.Demo.Components;
using Xunit;

namespace PhoneNumbers.Demo.Tests.Components;

public class ApiReferenceLinkTests : BunitContext
{
    [Fact]
    public void loads_the_named_api_from_the_docs_published_beside_the_demo()
    {
        var cut = Render<ApiReferenceLink>(p => p
            .Add(l => l.Api, "AsYouTypeFormatter")
            .Add(l => l.Path, "api/PhoneNumbers.AsYouTypeFormatter.html"));

        var link = cut.Find("a");

        Assert.Equal("AsYouTypeFormatter", link.TextContent.Trim());
        Assert.Equal("http://localhost/docs/api/PhoneNumbers.AsYouTypeFormatter.html", link.GetAttribute("href"));
        // _top: the same tab, but a target Blazor's router leaves to the browser (see DocsLinks).
        Assert.Equal("_top", link.GetAttribute("target"));
    }

    [Fact]
    public void accessible_name_starts_with_the_visible_text_and_says_where_it_goes()
    {
        var cut = Render<ApiReferenceLink>(p => p
            .Add(l => l.Api, "PhoneNumberUtil.Parse")
            .Add(l => l.Path, "api/PhoneNumbers.PhoneNumberUtil.html"));

        Assert.Equal("PhoneNumberUtil.Parse in the API reference", cut.Find("a").GetAttribute("aria-label"));
    }
}
