using Microsoft.AspNetCore.Components;

namespace PhoneNumbers.Demo.Components.Icons;

/// <summary>
/// Base class for the demo's inline SVG icons. Every icon is decorative: it renders an
/// <c>aria-hidden</c> <c>&lt;svg&gt;</c> on a 24×24 viewBox and takes its size, stroke and fill
/// from the CSS of the block it sits in. Unmatched attributes (<c>class</c>, <c>data-*</c>, …)
/// are forwarded to the <c>&lt;svg&gt;</c> element.
/// </summary>
public abstract class IconBase : ComponentBase
{
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }
}
