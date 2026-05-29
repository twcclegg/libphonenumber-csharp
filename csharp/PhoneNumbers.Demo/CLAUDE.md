# CLAUDE.md — PhoneNumbers.Demo

Blazor WebAssembly demo site for the libphonenumber-csharp library, deployed to GitHub Pages.

## Build and run

```bash
dotnet build csharp/PhoneNumbers.Demo
dotnet run --project csharp/PhoneNumbers.Demo
```

Publish for GitHub Pages (static WASM output):

```bash
dotnet publish csharp/PhoneNumbers.Demo -c Release
```

The demo references the main `PhoneNumbers` project directly — changes to the library are reflected immediately on rebuild.

## Styling conventions

### No inline styles

Never use `style="..."` attributes in Razor (`.razor`) or HTML (`.html`) files. All visual styling must live in CSS files:

- Move styles to the relevant per-component CSS file under `wwwroot/css/`.
- If the style is a one-off variant, add a BEM modifier class and define it in CSS.
- The only exception is Blazor's own auto-generated inline styles (e.g. from `@bind` or Blazor framework internals) — do not suppress those.

### CSS custom properties

All themable values are defined as CSS custom properties in `wwwroot/css/app.css` on `:root`. Use existing variables (`--primary`, `--text`, `--border`, `--radius`, etc.) rather than hard-coding colours or sizes.

### BEM naming

All component styles must use BEM (`block__element--modifier`).

- **Block**: the component name in kebab-case — `hero`, `card`, `nav-link`, `result-grid`
- **Element**: a child owned by the block, joined with `__` — `hero__content`, `card__title`, `result-grid__item`
- **Modifier**: a state or variant, joined with `--` — `badge--success`, `btn--primary`, `form-input--lg`

Rules:

- Each component owns one block. Don't reuse another component's block prefix.
- No bare element selectors (`h2`, `p`, `.container`) in component stylesheets — scope everything under the block.
- Modifiers stack on top of the base element class (`class="badge badge--success"`), they don't replace it.
- Avoid descendant selectors across blocks. If `.foo .bar` is tempting, `.bar` probably belongs to `.foo` as `.foo__bar`.
- Don't nest deeper than block > element > modifier in CSS — keep BEM flat and greppable.

### Per-component CSS files

Separate CSS into per-component files where appropriate:

- `wwwroot/css/app.css` — global reset, CSS custom properties, base typography, utility classes.
- Create dedicated CSS files for distinct components (e.g. `sidebar.css`, `card.css`, `hero.css`, `form.css`) when they grow beyond ~50 lines or are reused across multiple pages.
- Import component CSS files from `index.html` or via Blazor CSS isolation (`Component.razor.css`) where it makes sense.
- Shared layout styles (sidebar, main content scaffold) stay in a layout-level file.
- Page-specific one-off styles can live in a Blazor-scoped CSS file (`Pages/PageName.razor.css`).

### Theming

The app currently uses a light theme only. If dark mode is added:

- Define themable values as CSS custom properties with light defaults.
- Override under `[data-theme="dark"]`.
- All foreground/background pairs must meet WCAG AA contrast in both themes (see Accessibility below).

## Accessibility (WCAG AA compliance)

All changes must meet WCAG 2.1 Level AA. This is a hard requirement, not a nice-to-have.

### Colour and contrast

- All text/background pairs must meet 4.5:1 contrast ratio for normal text, 3:1 for large text (18px+ or 14px+ bold) and UI components/graphical objects.
- Verify contrast in both light theme and any future dark theme.
- Don't rely on colour alone to convey meaning — pair with an icon, text label, or shape (e.g. valid/invalid uses both colour and badge text).

### Semantics and structure

- Use semantic HTML: `<nav>`, `<main>`, `<section>`, `<h1>`–`<h6>` in correct hierarchy, `<button>` for actions, `<a>` for navigation.
- All interactive elements must be keyboard-accessible (focusable, operable via Enter/Space, visible focus indicator).
- All form inputs must have associated `<label>` elements (or `aria-label`/`aria-labelledby`).
- Images and icons that convey meaning need `alt` text or `aria-label`. Decorative icons use `aria-hidden="true"`.

### Motion and interaction

- Respect `prefers-reduced-motion`: wrap non-essential animations/transitions in `@media (prefers-reduced-motion: no-preference)`.
- Focus states must be visible — never `outline: none` without a replacement indicator.
- Touch targets must be at least 44x44px on mobile.

### Testing accessibility

- Run axe or Lighthouse accessibility audit before considering a UI change complete.
- Verify keyboard navigation through all interactive flows.
- Check screen reader announcements for dynamic content changes (e.g. parse results appearing).

## Component patterns

### Razor pages

Pages live in `Pages/` and use `@page "/route"`. Each page is self-contained with its `@code` block — no separate code-behind files unless complexity demands it.

### Shared state

Use `PhoneNumberUtil.GetInstance()` for the singleton — don't construct new instances. The instance is expensive and thread-safe.

### Error handling

Wrap `PhoneNumberUtil` calls that can throw (e.g. `Parse`) in try/catch for `NumberParseException`. Display user-friendly error messages inline using `.error-message`.

### Responsive design

The layout is sidebar + main content. At `max-width: 860px` the sidebar collapses off-screen with a toggle button. Grid layouts (`input-row`, `result-grid`, `feature-grid`) collapse to single-column on narrow viewports.

## Testing workflow (mandatory)

Every code change must follow this workflow before the task is considered complete:

1. Run the demo test suite and confirm it passes with zero failures:
   ```bash
   dotnet test csharp/PhoneNumbers.Demo.Tests -p:TargetFrameworks=net9.0
   ```
2. If the change touches any page, component, or logic branch not already exercised by an existing test, add a new test covering it in `csharp/PhoneNumbers.Demo.Tests/Pages/`.
3. Never mark a task done while tests are failing or skipped.

### Valuable tests to add

A test is valuable if it covers an observable behaviour that isn't already tested:

- A new conditional branch in a page (new happy path, new error case, new empty state).
- A user interaction that wasn't tested before (clicking a button, changing a select, clearing an input).
- A regression guard for a bug that was fixed — name it after the symptom, not the root cause.

A test is **not** valuable if it duplicates existing coverage, tests CSS/styling, or just restates the implementation.

## Testing requirements (detail)

All changes must build cleanly (`dotnet build` with no warnings — `TreatWarningsAsErrors` is inherited from the solution).

### Component tests for all logic changes

Every change to a page, component, or service that contains logic must have accompanying component tests. Use bUnit for Blazor component testing. Tests assert on **observable output** — what the user sees and experiences — not on implementation details.

### Always test

- **Rendered output**: text the user reads, results displayed, badges/labels shown, lists and their order.
- **User interactions**: what happens after input changes, button clicks, select changes — assert on the resulting DOM content or callback args.
- **Conditional rendering**: every branch that affects what the user sees — error messages for invalid input, empty states, valid/invalid indicators, different number types.
- **Edge cases**: empty input, whitespace-only input, invalid phone numbers, unknown regions, numbers with no geocoding data.
- **Defaults**: initial state renders correctly (e.g. pre-populated phone number parses on load).

### Never test

- **Styling**: class names, CSS values, computed colours, layout, animations. CSS is verified by eye and accessibility audits.
- **Implementation details**: which internal method was called, internal state shape, component lifecycle hooks, how many times a method fired.
- **Framework behaviour**: that Blazor rerenders, that the router navigates, that event binding works.
- **libphonenumber correctness**: the main library has its own comprehensive test suite. The demo tests verify that the demo *uses* the library correctly and displays results properly.

### Test patterns

- Name each test as a behaviour: `"displays_error_message_when_phone_number_is_unparseable"`, not `"test_parsing"`.
- One concept per test. If asserting "and also", split it.
- Test the component as a user would interact with it — provide input, assert on rendered markup.
- Don't snapshot. Assert on specific text, elements, attributes, or structure you care about.
- Arrange-Act-Assert: set up the component, trigger the interaction, verify the output.

### Visual verification

In addition to component tests, verify UI changes visually in the browser:

- Responsive breakpoints: sidebar toggle appears below 860px, grids collapse to single-column.
- Accessibility: run Lighthouse/axe audit.
- Cross-browser: check in at least Chrome and Firefox.

## Don't

- Don't hand-edit `resources/*.xml` — those are synced from upstream Google.
- Don't add heavyweight JS interop. This is a pure Blazor WASM app.
- Don't add server-side dependencies. The project must remain a static client-side app for GitHub Pages hosting.
- Don't snapshot test Razor output. Assert on specific observable behaviour.
