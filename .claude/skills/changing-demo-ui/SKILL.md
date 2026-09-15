---
name: changing-demo-ui
description: Change the Blazor WebAssembly demo site under csharp/PhoneNumbers.Demo — a Razor page, layout, CSS, or its bUnit tests — and verify the result in the browser. Use when editing anything in PhoneNumbers.Demo or PhoneNumbers.Demo.Tests, when asked to run or screenshot the demo, or when a demo test run reports zero tests. Covers the BEM/no-inline-style CSS rules, WCAG AA accessibility checks, what a demo test should and should not assert, and the preview-verification loop.
---

# Changing the demo UI

The demo is a static Blazor WASM app served from GitHub Pages (`deploy-demo.yml`). Pages live in
`Pages/*.razor` (one `@page` route each, self-contained `@code` block, `@inject NavigationManager`
for URL state via `UrlState.cs`), the shell in `Layout/MainLayout.razor`, styles in `wwwroot/css/`
linked from `wwwroot/index.html`.

```
Demo change:
- [ ] 1. Make the change (Razor + CSS rules below)
- [ ] 2. Add or update a bUnit test in PhoneNumbers.Demo.Tests/Pages/
- [ ] 3. dotnet test csharp/PhoneNumbers.Demo.Tests — confirm a non-zero test count
- [ ] 4. Run the "demo" preview and verify visually, at desktop and mobile width
- [ ] 5. Accessibility pass (contrast, keyboard, labels, reduced motion)
```

## 1. Styling rules

- **No `style="…"` attributes** in `.razor` or `.html`. A one-off variant is a BEM modifier class
  defined in CSS. The only exception is markup Blazor itself generates.
- **BEM everywhere**: `block__element--modifier`. One block per component (`hero`, `card`,
  `result-grid`); no bare element selectors (`h2`, `p`) in component files; modifiers stack on the
  base class (`class="badge badge--success"`); no descendant selectors across blocks — if
  `.foo .bar` is tempting, it is `.foo__bar`; never deeper than block > element > modifier.
- **Custom properties** on `:root` in `wwwroot/css/app.css` hold every themable value
  (`--primary`, `--text`, `--border`, `--radius`, …). Never hard-code a colour or size.
- **One CSS file per component** in `wwwroot/css/` (`card.css`, `sidebar.css`, …), added to
  `index.html`. `app.css` holds only the reset, custom properties, base typography and utilities;
  `layout.css` the sidebar + main scaffold. Page-only styles may use `Pages/Name.razor.css`.
- Layout is sidebar + main; at `max-width: 860px` the sidebar collapses behind a toggle and the
  grids (`input-row`, `result-grid`, `feature-grid`) go single-column.
- Light theme only today. If adding dark mode: light defaults in custom properties, overrides under
  `[data-theme="dark"]`, contrast verified in both.

## 2. Tests: what to assert

bUnit, xUnit, classes derive from `BunitContext`, one file per page in
`csharp/PhoneNumbers.Demo.Tests/Pages/`, test names in snake_case describing behaviour
(`shows_valid_badge_for_default_uk_number`). Arrange-Act-Assert; one concept per test.

**Do test**: rendered text and results, badges/labels, list contents and order; what happens after
input changes, clicks, select changes; every conditional branch the user can see (error message,
empty state, valid/invalid); edge inputs (empty, whitespace, unparseable, unknown region, number
with no geocoding data); initial state (the default number parses on load).

**Don't test**: class names, CSS or layout; internal method calls, state shape, lifecycle hooks or
render counts; that Blazor re-renders or routes; libphonenumber correctness — the library has its
own suite, the demo tests only that it *uses* the library and displays the result. No snapshots.

A test is worth adding when it covers a branch, interaction or regression not already covered;
name a regression test after the symptom.

## 3. Run the tests — and check the count

```bash
dotnet test csharp/PhoneNumbers.Demo.Tests
```

**Never add `-p:TargetFrameworks=net10.0` here.** The demo projects set `TargetFramework`
(singular); overriding the plural property from the command line stops the
`xunit.runner.visualstudio` build assets from being applied, the xunit discoverer never reaches the
output directory, and `dotnet test` exits 0 having run nothing. Output ending in
`No test is available in …` is a failure, not a pass — the same project reports dozens of tests
without the switch.

## 4. Verify in the browser

Start the preview with the `demo` configuration in `.claude/launch.json` (it runs
`dotnet run --project csharp/PhoneNumbers.Demo` on <http://localhost:5099>). Then:

1. Reload the page and read the console for errors.
2. Exercise the changed flow — type a number, change the region select, click the button — and read
   the resulting DOM rather than trusting a glance.
3. Resize to the mobile preset: sidebar toggle appears below 860px, grids collapse.
4. Screenshot the result for the user.

The library reference is a project reference, so library changes appear on the next build with no
package step. Publishing (`dotnet publish csharp/PhoneNumbers.Demo -c Release`) runs the trimmer;
if a library change trips it, that is a library bug (see `changing-public-api`), not a demo one.

## 5. Accessibility (WCAG 2.1 AA — required)

- Contrast ≥ 4.5:1 for text, ≥ 3:1 for large text and UI components, in every theme.
- Meaning never by colour alone — pair with text or an icon (valid/invalid uses colour *and* badge
  text).
- Semantic HTML: `<nav>`, `<main>`, `<section>`, heading hierarchy, `<button>` for actions, `<a>`
  for navigation. Every input has a `<label>` or `aria-label`; decorative icons get
  `aria-hidden="true"`.
- Keyboard: everything focusable and operable with Enter/Space, focus always visible (no
  `outline: none` without a replacement), touch targets ≥ 44×44px.
- Non-essential animation inside `@media (prefers-reduced-motion: no-preference)`.
- Run an axe or Lighthouse audit before calling a UI change done; check that dynamic results are
  announced.

## Don'ts

- No heavyweight JS interop; no server-side dependencies — it must stay a static site.
- Don't hand-edit `resources/`.
- Don't construct `PhoneNumberUtil`; use `GetInstance()`. Wrap `Parse` in
  `try/catch (NumberParseException)` and render the message inline with `.error-message`.
