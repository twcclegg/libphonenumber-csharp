# AGENTS.md — PhoneNumbers.Demo

Blazor WebAssembly demo for libphonenumber-csharp, deployed to GitHub Pages by `deploy-demo.yml`.
Pure client-side: no server code, no heavyweight JS interop. It references the `PhoneNumbers`
project directly, so it also proves the library works trimmed under WASM.

The full styling, accessibility, testing and browser-verification procedure is the
`changing-demo-ui` skill (`.claude/skills/changing-demo-ui/SKILL.md`). The rules below are the
ones every change must respect.

## Build, run, test

```bash
dotnet build csharp/PhoneNumbers.Demo
dotnet run --project csharp/PhoneNumbers.Demo          # open the URL the SDK prints
dotnet test csharp/PhoneNumbers.Demo.Tests             # bUnit; must report a test count, not "No test is available"
dotnet publish csharp/PhoneNumbers.Demo -c Release     # what GitHub Pages serves
```

Do **not** pass `-p:TargetFrameworks=net10.0` to the demo test command the way the repo-root
command does: this project sets `TargetFramework` (singular), and overriding `TargetFramework**s**`
drops the xunit runner assets, so `dotnet test` exits 0 having discovered zero tests.

## Rules

- **No inline `style="…"`** in `.razor` or `.html`. Styling lives in `wwwroot/css/`, one file per
  component, using BEM (`block__element--modifier`) and the custom properties on `:root` in
  `app.css` (`--primary`, `--text`, `--border`, `--radius`, …) — never hard-coded colours or sizes.
- **WCAG 2.1 AA is a hard requirement**: 4.5:1 text contrast, semantic HTML, keyboard-operable
  controls with visible focus, labelled inputs, `prefers-reduced-motion` respected, meaning never
  conveyed by colour alone.
- **Every logic change gets a bUnit test** in `csharp/PhoneNumbers.Demo.Tests/Pages/`, asserting on
  what the user sees — never on CSS, class names, or implementation details.
- Use `PhoneNumberUtil.GetInstance()`; never construct a new instance. Wrap `Parse` in
  `try/catch (NumberParseException)` and show the error inline with `.error-message`.
- Don't hand-edit `resources/` and don't add server-side dependencies.
