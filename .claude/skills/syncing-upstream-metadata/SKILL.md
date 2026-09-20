---
name: syncing-upstream-metadata
description: Work with the automated upstream metadata sync and the release it triggers — reviewing a metadata-update/* PR, diagnosing a sync that stopped or failed, running it manually or as a dry run, understanding how CHANGELOG.md entries fold, or cutting a release. Use when phone metadata needs bumping to a new google/libphonenumber release, when the sync's .java / .proto gate has tripped, or when touching lib/github-actions-metadata-update.sh, lib/update-changelog.sh, lib/finalize-metadata-release.sh or their workflows.
---

# Syncing upstream metadata

`resources/` is copied verbatim from the latest `google/libphonenumber` release. The daily
`create_new_release_on_new_metadata_update.yml` workflow drives
`lib/github-actions-metadata-update.sh`, which copies upstream `resources/` (less upstream's
`metadata/` csv tables, which nothing here reads), regenerates `resources/locale/country_names.txt`
via `lib/DumpLocale.java`, writes the `CHANGELOG.md` entry, and opens a `metadata-update/*` PR
against `main` **with auto-merge off, for a maintainer to review and merge** — that is the intended
way a metadata release ships. If the next daily run finds that PR still open, it regenerates the
sync onto the same branch, force-pushes, and turns auto-merge on as a backstop, so a release is
never stalled because nobody looked. Once the PR merges, by hand or by auto-merge,
`finalize_metadata_release.yml` runs `lib/finalize-metadata-release.sh` to tag the merge commit,
cut the GitHub release, and dispatch the NuGet publish.

`README.md` § "Metadata updates" is the user-facing description; this skill is the working detail.
The reasoning behind the review-then-backstop flow, the changelog fold, the commit identity and the
checkout depth is in
[reference/changelog-and-release-internals.md](reference/changelog-and-release-internals.md) —
read it before changing any of those four things.

## Reviewing a `metadata-update/*` PR

Expect only `resources/**`, the regenerated locale data, and one `CHANGELOG.md` entry to change.
Anything else — a `.cs` edit, a csproj change — means something went wrong; investigate rather
than approving.

`resources/**` is marked `linguist-generated` in `.gitattributes`, so GitHub collapses all of it and
the visible diff is usually the `CHANGELOG.md` entry alone. That is the point — but it means the
check above is a check of the **file list**, not of what renders: read the changed-files tab (or
`git diff --name-only origin/main...`) before concluding nothing else moved.

Check the PR's own status checks. Test failures on a metadata bump are usually genuine: a region's
example number or formatting rule changed upstream, and a ported test asserts the old value.
Fix the *test* to match the new metadata; never edit `resources/` to make a test pass.

**Don't push fixes onto the `metadata-update/*` branch.** The next daily run force-pushes a fresh
sync over it, deliberately, so anything else there never reaches a release. A test that needs
updating gets its own PR to `main`; the metadata PR is then regenerated on top of it.

**Closing the PR does not decline the release** — the next run just opens another. There is no
notion of rejecting a sync; a bad upstream release is resolved by upstream's next release.

## When the sync stops

The script inspects the upstream diff first and **exits before touching anything if it contains
`.java` or `.proto` files**, because those may need porting by hand and an unattended bump would
silently skip them. Exit code 4 (`EXIT_NEEDS_ATTENTION`) is that gate.

When it trips:

1. Read the upstream diff for the release. Test-only, build-file, or Java-specific changes need
   nothing here.
2. Anything touching parsing, formatting, validation or matching behaviour → port it first with the
   `porting-upstream-changes` skill, then let the sync run.
3. Only once the Java changes are handled (or judged irrelevant), re-run with the override — from
   the Actions UI tick **skip_java_check** / **skip_proto_check**, or locally pass
   `--skip-java-check` / `--skip-proto-check`. Say in the PR *why* the override was safe.

Exit 4 also fires when upstream's major version no longer matches `EXPECTED_MAJOR_VERSION`
(default 9) — that is a porting project, not a metadata bump, and needs a human decision. Other
exit codes: 2 usage, 3 a missing prerequisite (tooling, token, or a token whose account is not the
one `finalize_metadata_release.yml` gates on).

## Running it locally

`--dry-run` performs every read-only step — version lookups, the upstream diff gates, the clone —
reports what it would do, and stops before the first change to the working tree. It needs no token
and downgrades the "clean main" requirement to a warning, so it works from a feature branch.

```bash
lib/github-actions-metadata-update.sh --dry-run
```

Replay a specific historical pair (useful for reproducing a gate that tripped):

```bash
UPSTREAM_TAG=v9.0.33 DEPLOYED_VERSION=9.0.32 lib/github-actions-metadata-update.sh --dry-run --skip-java-check
```

`--help` lists every flag and environment variable (`UPSTREAM_REPOSITORY`, `NUGET_PACKAGE_ID`,
`EXPECTED_MAJOR_VERSION`, …). A real run needs a GitHub token and pushes a branch — do not run one
without the user explicitly asking.

## The changelog

`lib/update-changelog.sh` writes the release's entry in the same commit as the sync. Consecutive
releases whose every commit was authored by the sync account or dependabot fold into one ranged
entry; any human commit since the last release — including one that only touches `resources/` or
`CHANGELOG.md` — gives the release its own entry. So if you hand-edit `CHANGELOG.md`, expect the
next release to start a new entry rather than extend the current run; that is intended.

## Releases generally

Releases are tag-driven: a `vX.Y.Z` tag fires `publish_nuget.yml`, which packs both packable
projects at the tag's version and pushes them with their `.snupkg` to nuget.org via trusted
publishing (GitHub OIDC — there is no API key secret). Metadata bumps get their tag automatically
from `finalize_metadata_release.yml`; any other release means pushing a tag by hand, which is the
maintainer's call, not something to do unprompted. After a release ships, bump
`PackageValidationBaselineVersion` in `csharp/PhoneNumbers/PhoneNumbers.csproj` and
`csharp/PhoneNumbers.Extensions/PhoneNumbers.Extensions.csproj` together to the released version so
package validation compares against what is actually on nuget.org.
