using Microsoft.AspNetCore.Components;

namespace PhoneNumbers.Demo;

// Pages of the API reference the demo links to, relative to the docs site's root. deploy-demo.yml
// publishes that site as docs/ beside this app, so each resolves against the app's base URI.
// docfx/build.sh fails the docs build when one of these files or anchors stops existing, so a
// renamed type or a changed overload breaks CI rather than a link.
//
// Every link to one of these carries target="_top" (Target below). The docs sit inside the app's
// base URI, so Blazor would otherwise intercept the click as a client-side route; with no route
// to match it falls back to location.replace, which for a URL with a #fragment only scrolls the
// current page instead of loading the docs. Blazor leaves any link with a target other than
// _self to the browser, and _top is still the same tab.
internal static class DocsLinks
{
    public const string Target = "_top";

    public const string ApiReference = "api/PhoneNumbers.html";
    public const string Articles = "articles/api-differences-from-java.html";

    // The API each feature page exercises. A member's anchor is DocFX's id for that overload.
    public const string Parse = "api/PhoneNumbers.PhoneNumberUtil.html#PhoneNumbers_PhoneNumberUtil_Parse_System_String_System_String_";
    public const string Format = "api/PhoneNumbers.PhoneNumberUtil.html#PhoneNumbers_PhoneNumberUtil_Format_PhoneNumbers_PhoneNumber_PhoneNumbers_PhoneNumberFormat_";
    public const string AsYouTypeFormatter = "api/PhoneNumbers.AsYouTypeFormatter.html";
    public const string FindNumbers = "api/PhoneNumbers.PhoneNumberUtil.html#PhoneNumbers_PhoneNumberUtil_FindNumbers_System_String_System_String_PhoneNumbers_PhoneNumberUtil_Leniency_System_Int64_";
    public const string OfflineGeocoder = "api/PhoneNumbers.PhoneNumberOfflineGeocoder.html";

    public static string Resolve(NavigationManager nav, string path) => $"{nav.BaseUri}docs/{path}";
}
