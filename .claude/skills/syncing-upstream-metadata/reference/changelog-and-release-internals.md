# Sync flow, changelog generation, sync identity and checkout depth — why they are the way they are

Several decisions in `lib/github-actions-metadata-update.sh` and its workflow look arbitrary and are
not. Each was reached after the simpler alternative failed in production. `README.md` and the
script's header comment point here for the reasoning.

## Contents

- The sync opens a PR and stops; merging it is the intended path
- The release flow deliberately has no notion of declining, and no guards against same-day runs
- The changelog entry is the release's own notes
- The notes need a reachable previous tag
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

## The changelog entry is the release's own notes

`lib/update-changelog.sh` writes the entry from the body of GitHub's
`POST /releases/generate-notes` for the new tag (`generateReleaseNotes` in
`github-release-helpers.sh`), the same generator `createRelease` asks for. The changelog and the
release page therefore list the same PRs. The notes cannot contain the sync's own PR, so the sync
appends its line; when the PR does not exist yet the line carries a placeholder number that is
replaced after the PR is opened (amend + force-push of the sync branch, auto-merge still off).

This replaced a fold-by-authorship scheme that merged "bot-only" releases into ranged entries and
described any release with human commits as "plus other changes merged to `main`". That left the
changelog saying nothing about what shipped (v9.0.41 listed 17 PRs on its release page and none
in the changelog). The notes are used verbatim apart from heading depth; don't re-summarise them.

## The notes need a reachable previous tag

`previous_tag_name` must exist on GitHub; the sync uses the deployed NuGet version's tag.

## The sync commits as the account its token belongs to

The commit author is resolved from the GitHub API rather than hardcoded, as
`<id>+<login>@users.noreply.github.com` — the form GitHub resolves back to an account. For years
it committed as `libphonenumber-csharp-bot <>`, a bare name with no email, which GitHub links to no
account at all.

The run aborts (exit 3) if that account is not the one `finalize_metadata_release.yml` gates on,
because that gate is a literal login (`github.event.pull_request.user.login == '…'`) and a
mismatch would leave the sync working perfectly while no release ever followed. Pointing the sync
at a different bot means updating the workflow gate in the same change.
