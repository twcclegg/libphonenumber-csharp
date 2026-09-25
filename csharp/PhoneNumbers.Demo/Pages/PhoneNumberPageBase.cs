using Microsoft.AspNetCore.Components;

namespace PhoneNumbers.Demo.Pages;

/// <summary>
/// Shared state and handlers for the pages built around one phone-number input and a default
/// region (Home, Parse &amp; Validate, Formatting, Geo &amp; Timezone): both are seeded from the URL,
/// the number is re-parsed on every keystroke, and the URL is rewritten only once the input is
/// committed or the region changes.
/// </summary>
public abstract class PhoneNumberPageBase : ComponentBase
{
    protected readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

    [Inject] protected NavigationManager Nav { get; set; } = default!;

    protected string PhoneInput { get; private set; } = "";
    protected string SelectedRegion { get; private set; } = "";
    protected PhoneNumber? Parsed { get; private set; }
    protected string? ParseError { get; private set; }

    /// <summary>The number shown when the URL carries none.</summary>
    protected abstract string DefaultNumber { get; }

    /// <summary>The region selected when the URL carries none (or an unsupported one).</summary>
    protected abstract string DefaultRegion { get; }

    protected override void OnInitialized()
    {
        var (number, region) = UrlState.Read(Nav);
        PhoneInput = number ?? DefaultNumber;
        SelectedRegion = region ?? DefaultRegion;
        UrlState.NormalizeUrl(Nav);
        TryParse();
    }

    /// <summary>
    /// Called after every parse attempt, successful or not, so a page can derive its own results
    /// from <see cref="Parsed"/> (or clear them when it is <c>null</c>).
    /// </summary>
    protected virtual void OnParsed(PhoneNumber? number)
    {
    }

    // @oninput keeps the live results in sync on every keystroke, but does NOT touch the URL.
    protected void OnPhoneInput(ChangeEventArgs e)
    {
        PhoneInput = e.Value?.ToString() ?? "";
        TryParse();
    }

    // @onchange fires when the field is committed (blur / Enter), so the URL is only
    // rewritten once the user is done typing — never mid-keystroke.
    protected void OnPhoneCommit(ChangeEventArgs e)
    {
        PhoneInput = e.Value?.ToString() ?? "";
        TryParse();
        SyncUrl();
    }

    protected void OnRegionChange(string region)
    {
        SelectedRegion = region;
        TryParse();
        SyncUrl();
    }

    protected void SetExample(string number, string region)
    {
        PhoneInput = number;
        SelectedRegion = region;
        TryParse();
        SyncUrl();
    }

    protected void TryParse()
    {
        Parsed = null;
        ParseError = null;
        if (!string.IsNullOrWhiteSpace(PhoneInput))
        {
            try
            {
                Parsed = PhoneUtil.Parse(PhoneInput, SelectedRegion);
            }
            catch (NumberParseException ex)
            {
                ParseError = ex.Message;
            }
        }

        OnParsed(Parsed);
    }

    private void SyncUrl()
        => Nav.NavigateTo(UrlState.Build(Nav, PhoneInput, SelectedRegion), forceLoad: false, replace: true);
}
