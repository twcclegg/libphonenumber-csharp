# Changelog fold, sync identity and checkout depth — why they are the way they are

Three decisions in `lib/github-actions-metadata-update.sh` and its workflow look arbitrary and are
not. Each was reached after the simpler alternative failed in production.

## Contents

- The fold is decided by authorship, not by paths
- The fold check needs full history
- The sync commits as the account its token belongs to

## The fold is decided by authorship, not by paths

`lib/update-changelog.sh` collapses consecutive metadata-only releases into one ranged entry. The
sync tells it which releases qualify by asking whether **every commit since the last release was
written by the sync account or by dependabot**. `Co-authored-by` trailers count as authors, since a
squash merge records only the PR's author and a human fix pushed onto a dependabot PR would
otherwise fold away.

Dependabot has to be ignored because its monthly updates land between fortnightly metadata releases
often enough to break most runs — over v9.0.12..v9.0.27 that alone is the difference between
sixteen entries and one.

Authorship rather than paths because a path list cannot tell a maintainer's own CI change, which
deserves an entry, from dependabot's; and because the sync's own generated output kept counting as
substantive work under the old file-based rule.

The trade-off: a human commit touching only `resources/` or only `CHANGELOG.md` now breaks a run.
Hand-editing either is worth its own entry anyway.

The fold state lives in an HTML comment directly above the heading it describes
(`<!-- changelog-run from=… first=… start-date=… count=N -->`). Only a heading with that marker is a
candidate to extend, so a hand-written heading can never be mistaken for a foldable run.

## The fold check needs full history

The sync's `actions/checkout` uses `fetch-depth: 0` with `filter: blob:none` (the blobs are never
read). Don't shallow it, and don't fetch the tag shallowly instead: a `--depth=1` fetch grafts the
tag as a parentless root and writes `.git/shallow` even into a full clone, after which
`v<tag>..HEAD` covers the wrong commits — measured here, one range grew from 350 to 1648 commits and
another collapsed to zero. `git merge-base --is-ancestor` still answers yes in that state and is no
guard, which is why the script tests `git rev-parse --is-shallow-repository` and fails closed.

## The sync commits as the account its token belongs to

The commit author is resolved from the GitHub API rather than hardcoded, as
`<id>+<login>@users.noreply.github.com` — the form GitHub resolves back to an account. For years
it committed as `libphonenumber-csharp-bot <>`, a bare name with no email, which GitHub links to no
account at all.

The run aborts (exit 3) if that account is not the one `finalize_metadata_release.yml` gates on,
because that gate is a literal login (`github.event.pull_request.user.login == '…'`) and a
mismatch would leave the sync working perfectly while no release ever followed. Pointing the sync
at a different bot means updating the workflow gate in the same change.
