# libphonenumber-csharp Demo

A Blazor WebAssembly demo site that runs the [libphonenumber-csharp](https://github.com/twcclegg/libphonenumber-csharp) library entirely in the browser. No server-side code — everything executes client-side via WebAssembly.

Deployed to GitHub Pages via the [`deploy-demo.yml`](../../.github/workflows/deploy-demo.yml) workflow.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

Verify your installation:

```bash
dotnet --version
# Should print 9.x.x
```

## Running Locally

From the repo root:

```bash
dotnet run --project csharp/PhoneNumbers.Demo
```

Then open `http://localhost:5000` in your browser. The first load downloads the WebAssembly runtime and metadata (~5–10 MB); subsequent loads are cached.

The dev server supports hot reload — most Razor changes reflect without a full restart.

## Building for Production

```bash
dotnet publish csharp/PhoneNumbers.Demo -c Release -o out/demo
```

The published static files land in `out/demo/wwwroot/`. Serve that directory from any static host (GitHub Pages, Netlify, Azure Static Web Apps, etc.).

### GitHub Pages path prefix

GitHub Pages project sites are served from `/<repo-name>/`, not `/`. Before deploying, patch the `<base href>` in `index.html` and copy it to `404.html` for SPA routing:

```bash
REPO=libphonenumber-csharp
sed -i "s|<base href=\"/\" />|<base href=\"/${REPO}/\" />|g" out/demo/wwwroot/index.html
cp out/demo/wwwroot/index.html out/demo/wwwroot/404.html
touch out/demo/wwwroot/.nojekyll
```

The [`deploy-demo.yml`](../../.github/workflows/deploy-demo.yml) workflow does this automatically on every push to `main` and on a weekly schedule.

## Project Structure

```
PhoneNumbers.Demo/
├── Components/
│   └── Icons/                  # One Razor component per SVG icon (<SearchIcon />, …)
├── Layout/
│   └── MainLayout.razor        # Sidebar + responsive shell
├── Pages/
│   ├── Home.razor              # Hero, quick demo, feature cards
│   ├── ParseValidate.razor     # Parse, validate, inspect properties
│   ├── Formatting.razor        # All format types + out-of-country
│   ├── LiveFormatter.razor     # As-you-type formatter
│   ├── FindNumbers.razor       # Extract numbers from free text
│   └── GeoTimezone.razor       # Geocoding, timezone, carrier lookup
├── wwwroot/
│   ├── css/app.css             # All styles (no external CSS framework)
│   └── index.html
├── App.razor
├── _Imports.razor
├── Program.cs
└── PhoneNumbers.Demo.csproj
```

## Notes

- The project references `PhoneNumbers.csproj` directly (via `ProjectReference`) rather than the NuGet package, so any local changes to the library are immediately reflected in the demo without a publish step.
- The library embeds all phone number metadata as binary resources at build time; there are no runtime XML or zip files to deploy alongside the WASM output.
- `<TreatWarningsAsErrors>` is **not** set in this project (it is set in the main library), so Razor warnings do not fail the demo build.
