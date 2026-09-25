using Microsoft.AspNetCore.Components;

namespace PhoneNumbers.Demo;

// Pages of the API reference the demo links to, relative to the docs site's root. deploy-demo.yml
// publishes that site as docs/ beside this app, so each resolves against the app's base URI.
// docfx/build.sh fails the docs build when one of these files or anchors stops existing, so a
// renamed type or a changed overload breaks CI rather than a link.
internal static class DocsLinks
{
    public const string ApiReference = "api/PhoneNumbers.html";
    public const string Articles = "articles/api-differences-from-java.html";

    public static string Resolve(NavigationManager nav, string path) => $"{nav.BaseUri}docs/{path}";
}
