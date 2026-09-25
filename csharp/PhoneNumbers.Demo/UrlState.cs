using Microsoft.AspNetCore.Components;

namespace PhoneNumbers.Demo;

/// <summary>
/// Helpers for encoding the active phone number / region in the page URL so
/// that links are shareable. Query keys: <c>n</c> (number) and <c>r</c> (region).
/// </summary>
public static class UrlState
{
    /// <summary>
    /// Reads the <c>n</c> and <c>r</c> query parameters from the current URL. The region is
    /// upper-cased and dropped (returned as <c>null</c>) unless it is a supported region, so a
    /// hand-edited link can never leave a page parsing with a region its dropdown cannot show.
    /// </summary>
    public static (string? Number, string? Region) Read(NavigationManager nav)
        => Read(nav, LibraryRegions());

    /// <summary>
    /// <see cref="Read(NavigationManager)"/>, checking the region against
    /// <paramref name="supportedRegions"/> instead of the library's metadata.
    /// </summary>
    public static (string? Number, string? Region) Read(NavigationManager nav, IReadOnlySet<string> supportedRegions)
    {
        var (number, region) = ReadRaw(nav);
        return (number, NormalizeRegion(region, supportedRegions));
    }

    /// <summary>
    /// Rewrites the current history entry when the link's <c>r</c> is not in the form
    /// <see cref="Read(NavigationManager)"/> returns it (<c>?r=gb</c> becomes <c>?r=GB</c>, and an
    /// unsupported region is dropped), so the address bar matches the region the page selected.
    /// Does nothing for a link that is already in that form.
    /// </summary>
    public static void NormalizeUrl(NavigationManager nav)
        => NormalizeUrl(nav, LibraryRegions());

    /// <summary>
    /// <see cref="NormalizeUrl(NavigationManager)"/>, checking the region against
    /// <paramref name="supportedRegions"/> instead of the library's metadata.
    /// </summary>
    public static void NormalizeUrl(NavigationManager nav, IReadOnlySet<string> supportedRegions)
    {
        var (number, raw) = ReadRaw(nav);
        if (raw is null)
            return;
        var region = NormalizeRegion(raw, supportedRegions);
        if (region != raw)
            nav.NavigateTo(Build(nav, number, region), forceLoad: false, replace: true);
    }

    private static IReadOnlySet<string> LibraryRegions() => PhoneNumberUtil.GetInstance().GetSupportedRegions();

    private static (string? Number, string? Region) ReadRaw(NavigationManager nav)
    {
        var query = new Uri(nav.Uri).Query;
        if (string.IsNullOrEmpty(query))
            return (null, null);

        string? number = null;
        string? region = null;
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq < 0)
                continue;
            var key = pair[..eq];
            var value = Uri.UnescapeDataString(pair[(eq + 1)..]);
            if (key == "n")
                number = value;
            else if (key == "r")
                region = value;
        }

        return (number, region);
    }

    private static string? NormalizeRegion(string? region, IReadOnlySet<string> supportedRegions)
    {
        if (string.IsNullOrWhiteSpace(region))
            return null;
        var upper = region.Trim().ToUpperInvariant();
        return supportedRegions.Contains(upper) ? upper : null;
    }

    /// <summary>
    /// Builds a relative URL for the current route carrying the supplied number/region,
    /// suitable for <see cref="NavigationManager.NavigateTo(string, bool, bool)"/> with replace.
    /// Empty values are omitted to keep links clean.
    /// </summary>
    public static string Build(NavigationManager nav, string? number, string? region)
    {
        var path = nav.ToBaseRelativePath(nav.Uri);
        var q = path.IndexOf('?');
        if (q >= 0)
            path = path[..q];

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(number))
            parts.Add("n=" + Uri.EscapeDataString(number));
        if (!string.IsNullOrWhiteSpace(region))
            parts.Add("r=" + Uri.EscapeDataString(region));

        return parts.Count == 0 ? path : path + "?" + string.Join("&", parts);
    }
}
