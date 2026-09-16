# Sync flow, changelog fold, sync identity and checkout depth — why they are the way they are

Four decisions in `lib/github-actions-metadata-update.sh` and its workflow look arbitrary and are
not. Each was reached after the simpler alternative failed in production. `README.md` and the
script's header comment point here for the reasoning.

## Contents

- The sync opens a PR and stops; merging it is the intended path
- The release flow deliberately has no notion of declining, and no guards against same-day runs
- The fold is decided by authorship, not by paths
- The fold check needs full history
- The sync commits as the account its token belongs to

## The sync opens a PR and stops; merging it is the intended path

`create_new_release_on_new_metadata_update.yml` runs daily. Finding no open `metadata-update/*`
PR, it syncs and opens one with auto-merge off, for a maintainer to read and merge. Finding one
already open, it regenerates that sync onto the same branch, force-pushes, and turns auto-merge on
— the backstop, so a release is not stalled by nobody looking.

Before this, the script opened the PR and enabled auto-merge in the same breath, so the 08:00 sync
was usually merged, tagged, released and on nuget.org before anyone was awake; the only chance to
look at one was to catch it during the few minutes its checks took.

Three things follow from regenerating rather than arming what is already there, and all of them
are why it is worth the second sync:

- GitHub only accepts `enablePullRequestAutoMerge` on a PR that is *blocked* from merging (on one
  that could merge right now it answers "Pull request is in clean status"), and the force-push is
  what makes it blocked again. A day-old PR whose checks passed cannot be armed at all; merging it
  over the API instead would mean verifying the check rollup by hand, since the bot account
  bypasses `main`'s required status checks and the API would not refuse a red PR.
- `metadata-update/*` sits outside every ruleset, so the branch stays writable while the PR is
  open. The force-push is also what guarantees a commit pushed to that branch in the meantime never
  reaches a release.
- The checks gating the merge are minutes old rather than a day stale against a `main` that has
  moved.

The cost is that the commit which merges is not the one read the day before.

A failure to arm auto-merge on the backstop run is fatal, not a warning: the job runs daily, so a
permanent failure would otherwise loop silently, burning a build a day on a release that never
ships while every run reports green.

## The release flow deliberately has no notion of declining, and no guards against same-day runs

Closing the PR just means the next run opens another: a rejected sync either gets skipped once and
resumed at upstream's next release, or blocks releases indefinitely, and upstream's next release
resolves it either way. Nothing but the schedule or a person can start a run, so a second run on
the same day means someone chose to advance the release. Upstream releases are at least five days
apart, so at most one metadata PR is ever open, and a maintainer merging or closing by hand happens
hours either side of a scheduled job rather than inside one — guards for those interleavings were
written and removed.

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
