using Microsoft.AspNetCore.Components;

namespace PhoneNumbers.Demo;

/// <summary>
/// Helpers for encoding the active phone number / region in the page URL so
/// that links are shareable. Query keys: <c>n</c> (number) and <c>r</c> (region).
/// </summary>
public static class UrlState
{
    /// <summary>Reads the <c>n</c> and <c>r</c> query parameters from the current URL.</summary>
    public static (string? Number, string? Region) Read(NavigationManager nav)
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
