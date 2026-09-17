---
name: changing-ci-workflows
description: Add or edit a GitHub Actions workflow, Dependabot config, or CI helper script in this repository. Use when touching anything under .github/, adding a CI job or step, bumping an action, changing runners or permissions, writing tooling under lib/, when a workflow change could affect the OSSF Scorecard rating or the NuGet publish, or when adding an MSBuild property to Directory.Build.props or a csproj that could make the build non-deterministic. Covers the SHA-pinning, least-privilege, runner and no-JavaScript conventions the repo holds to without exception.
---

# Changing CI workflows

This repo publishes an OSSF Scorecard rating and uses OIDC trusted publishing to nuget.org, so the
supply-chain conventions below are load-bearing rather than stylistic. Every existing workflow
follows all of them; a new one that doesn't will lower the score or break the publish.

## Non-negotiables

- **Pin every action to a full 40-character commit SHA**, with the human-readable version in a
  trailing comment. All uses in this repo are pinned today — keep it at 100%:

  ```yaml
  - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
  ```

  Never `@v7`, never `@main`. Dependabot bumps these monthly and keeps the comment in sync; let it
  do the bumping rather than hand-editing SHAs.
- **Every workflow declares top-level `permissions:`**, normally `contents: read`. Escalate at the
  *job* level only, only to what that job needs, and say why in a comment. Every workflow here has
  a top-level block — don't add the first one without.
- **`persist-credentials: false` on every `actions/checkout`** unless the job genuinely pushes
  (today only the metadata sync does, with `BOT_ACCESS_TOKEN`).
- **Default to `runs-on: ubuntu-24.04-arm`.** Five workflows run on x64 `ubuntu-latest` — the
  metadata sync, `finalize_metadata_release`, `scorecard`, `triage_metadata_issues` and `codeql`
  (whose `macos-latest` branch is dead template residue; the matrix has only `csharp`) — and only
  `triage_metadata_issues` documents why (the Copilot CLI's npm package lacks reliable arm64
  binaries). A new x64 job should carry a reason in a comment. **There are no Windows or macOS
  runners**, so never write a step that only works on Windows.
- **No new secrets for publishing.** `publish_nuget.yml` exchanges the workflow's OIDC token
  (`id-token: write`) for a short-lived nuget.org key via `NuGet/login`, and the same token signs
  build provenance. Don't reintroduce a stored API key.

## CI tooling: bash or C#, never JavaScript

Scripts under `lib/` are bash (`set -euo pipefail`, `jq` for JSON). Anything needing real data
structures, statistics or a library is a small C# console project like
`csharp/PhoneNumbers.BenchmarkTools/`, run with `dotnet run --project` and kept out of the
solution. A few `lib/*.js` helpers once crept in as an implementation detail and were ported away;
JavaScript belongs only in the Blazor demo's own web assets.

Shared shell functions live in `lib/github-release-helpers.sh`, which is sourced rather than run
and is kept bash-3.2-compatible (no `${var,,}`) so it loads on macOS's stock bash. The scripts
that source it are not held to that — `lib/update-changelog.sh` already uses `mapfile` (bash 4+) —
so don't "fix" such lines, but don't add bash-4 constructs to the helper either.

## Dependabot

`.github/dependabot.yml` covers two ecosystems: `github-actions` at `/`, and `nuget` at `/csharp` —
one entry, because every version lives in `csharp/Directory.Packages.props`. NuGet major updates
are ignored deliberately and minor/patch NuGet updates are grouped; GitHub Actions majors are not
ignored, and `github/codeql-action*` is grouped because the CodeQL Action refuses to run with
mismatched `init`/`analyze` versions. Adding a package
manifest outside `/csharp` means adding an entry; adding another project inside it does not.

## The build must be reproducible

`build_and_run_unit_tests_linux.yml` packs `PhoneNumbers`, cleans, packs again, and fails if any
`PhoneNumbers.dll` in the two `.nupkg`s differs by hash — for every TFM, and after a clean that also
regenerates the metadata bins, so MetadataBuilder's output is covered too. Anything that embeds a
timestamp, an absolute path, a random seed or an unordered collection into the build breaks it with
nothing to point at. `Directory.Build.props` already sets `ContinuousIntegrationBuild` under `CI`
and `EmbedUntrackedSources`; keep new build properties deterministic. `global.json` pins the SDK
(`10.0.100`, `latestFeature`, no prerelease) so the two builds and the release use the same
compiler.

## Path filters go stale

Three workflows are path-filtered, and the lists are duplicated rather than shared — GitHub
Actions has no YAML anchors. If you add a directory that should trigger CI, update *every* copy:

- `run_performance_tests.yml` — duplicated across its `pull_request` and `push` triggers.
- `build_and_run_demo_tests.yml` and `deploy-demo.yml` — both include the library, Extensions,
  `resources/**` and both `Directory.*.props`.

A new source directory that nothing lists is a directory CI silently ignores.

## Before you push

Workflow syntax errors only surface on GitHub, so re-read the trigger and permissions blocks
carefully. `actionlint` catches most of it locally if available. Then check what the change implies
for:

- **CodeQL** (`codeql.yml`) and **Scorecard** (`scorecard.yml`) — both run on a schedule and report
  into the repository's security posture.
- **Required checks on `main`** — `main` requires status checks before any push lands, including
  from automation. That constraint is why the metadata sync opens a PR instead of pushing directly
  (see the `syncing-upstream-metadata` skill); a new job that becomes required affects that path
  too, and the `finalize_metadata_release.yml` gate is a literal bot login.
- **Checkout depth** — the sync's checkout is `fetch-depth: 0` with `filter: blob:none` on purpose
  and must stay that way; the reasons are in
  `.claude/skills/syncing-upstream-metadata/reference/changelog-and-release-internals.md`.
