#! /bin/bash
# Middle step of the metadata-update flow: github-actions-metadata-update.sh opens the
# metadata-update/* PR and deliberately leaves auto-merge off, so the PR sits as a review
# window. This script turns auto-merge on once that window has elapsed; from there GitHub's
# own auto-merge does the rest, so a PR whose required checks are red or still running waits
# for them exactly as it did before.
#
# The delay is a floor on the *automatic* path only. A maintainer can still merge (or close)
# the PR by hand at any point during the window, and that is the point of having one.
set -euo pipefail

usage() {
    cat <<'EOF'
Usage: enable-metadata-auto-merge.sh

Enables auto-merge on every open metadata-update PR that has been open longer than the soak
window. Takes no arguments; everything comes from the environment.

Environment variables:
  GITHUB_TOKEN             GitHub token used for the api calls. Must be the bot account's PAT,
                           not the ambient GITHUB_TOKEN - see the note below on why.
  GITHUB_REPOSITORY        owner/name of the repository to act on. Set automatically by
                           GitHub Actions.
  AUTO_MERGE_DELAY_HOURS   How long a PR must have been open before auto-merge is enabled
                           (default 18).
  BOT_LOGIN                Account whose PRs are eligible (default libphonenumber-csharp-bot).
  DRY_RUN                  Report what would happen and change nothing.

Why the bot's PAT and not GITHUB_TOKEN: GitHub does not raise workflow-triggering events for
actions taken by GITHUB_TOKEN. The merge this eventually causes has to fire the
pull_request closed event that finalize_metadata_release.yml listens for, so whoever enables
auto-merge has to be a real account. This is the same reason the PR itself is opened with
BOT_ACCESS_TOKEN.
EOF
}

if [ $# -ne 0 ]; then
    usage >&2
    exit 2
fi

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=./github-release-helpers.sh
source "${SCRIPT_DIR}/github-release-helpers.sh"

DRY_RUN="${DRY_RUN:-false}"
BOT_LOGIN="${BOT_LOGIN:-libphonenumber-csharp-bot}"

if [ -z "${GITHUB_REPOSITORY:-}" ]; then
    fail 2 "GITHUB_REPOSITORY is not set"
fi
if [ -z "${GITHUB_TOKEN:-}" ] && ! isTrue "${DRY_RUN}"; then
    fail 2 "GITHUB_TOKEN is not set"
fi
case "${AUTO_MERGE_DELAY_HOURS}" in
    '' | *[!0-9]*) fail 2 "AUTO_MERGE_DELAY_HOURS must be a whole number of hours, got '${AUTO_MERGE_DELAY_HOURS}'" ;;
esac

# -d is GNU, -r is BSD/macOS; this only formats a log line, so fall back to the raw epoch
# rather than failing the run if neither spelling works.
formatEpoch() {
    date -u -d "@$1" '+%F %T UTC' 2>/dev/null \
        || date -u -r "$1" '+%F %T UTC' 2>/dev/null \
        || echo "epoch $1"
}

NOW_EPOCH=$(date -u +%s)
CUTOFF_EPOCH=$((NOW_EPOCH - AUTO_MERGE_DELAY_HOURS * 3600))
log "enabling auto-merge on ${GITHUB_REPOSITORY} metadata PRs opened before $(formatEpoch "${CUTOFF_EPOCH}")"

OPEN_PRS=$(ghApi "https://api.github.com/repos/${GITHUB_REPOSITORY}/pulls?state=open&base=main&per_page=100")

# created_at never changes across a close/reopen, so a PR a maintainer closed to hold for
# manual review and later reopened would otherwise look just as "past the window" as one that
# has been open, untouched, the whole time - the very case the window exists to protect.
# Treat any PR that has ever been reopened as needing a human to merge it, not this script.
hasBeenReopened() {
    local pr_number="$1" events
    if ! events=$(ghApi "https://api.github.com/repos/${GITHUB_REPOSITORY}/issues/${pr_number}/events?per_page=100" 2>/dev/null); then
        warn "could not fetch the timeline for PR #${pr_number} to check for a manual reopen; treating it as reopened to be safe"
        return 0
    fi
    jq -e 'any(.[]; .event == "reopened")' <<<"${events}" >/dev/null 2>&1
}

# Eligibility is deliberately strict. The branch name alone decides nothing: anyone can push a
# branch called metadata-update/anything, and this script's whole job is to arrange a merge to
# main without a human in the loop. So the PR must also be authored by the bot and come from a
# branch in this repository rather than a fork - the same gate finalize_metadata_release.yml
# applies before it cuts a release.
ELIGIBLE=$(jq -r --arg bot "${BOT_LOGIN}" --arg repo "${GITHUB_REPOSITORY}" --argjson cutoff "${CUTOFF_EPOCH}" '
    .[]
    | select(.head.ref | startswith("metadata-update/"))
    | select(.user.login == $bot)
    | select(.head.repo.full_name == $repo)
    | select(.draft | not)
    | select((.created_at | fromdateiso8601) <= $cutoff)
    | "\(.number)\t\(.node_id)\t\(.head.ref)\t\(.created_at)"
' <<<"${OPEN_PRS}")

if [ -z "${ELIGIBLE}" ]; then
    log "no metadata PR has been open longer than ${AUTO_MERGE_DELAY_HOURS}h, nothing to do"
    exit 0
fi

while IFS=$'\t' read -r PR_NUMBER PR_NODE_ID PR_BRANCH PR_CREATED; do
    [ -n "${PR_NUMBER}" ] || continue
    log "PR #${PR_NUMBER} (${PR_BRANCH}) opened ${PR_CREATED} is past the ${AUTO_MERGE_DELAY_HOURS}h window"

    if hasBeenReopened "${PR_NUMBER}"; then
        log "  #${PR_NUMBER} was closed and reopened at some point; leaving it for a manual merge instead of auto-enabling"
        continue
    fi

    if isTrue "${DRY_RUN}"; then
        log "  dry run: would enable auto-merge"
        continue
    fi

    # GraphQL errors come back as HTTP 200 with an "errors" field, so --fail does not catch
    # them - check the body. None of these are fatal to the run: a PR that cannot take
    # auto-merge (already enabled, repository setting off, branch protection unsatisfiable)
    # still merges by hand, and the remaining PRs should still be processed. A transport
    # failure (curl exhausting its retries) is handled the same way: warn and move on to the
    # next PR rather than letting set -e abort the whole run over one PR.
    if ! AUTOMERGE_RESPONSE=$(jq -n --arg id "${PR_NODE_ID}" \
        '{query: "mutation($id: ID!) { enablePullRequestAutoMerge(input: {pullRequestId: $id, mergeMethod: MERGE}) { clientMutationId } }", variables: {id: $id}}' \
        | ghApi -X POST --data @- "https://api.github.com/graphql"); then
        warn "could not reach the GitHub API to enable auto-merge on PR #${PR_NUMBER}; it will be retried on the next run"
        continue
    fi

    if jq -e '.errors' <<<"${AUTOMERGE_RESPONSE}" >/dev/null 2>&1; then
        ERROR_MESSAGE=$(jq -r '.errors[0].message' <<<"${AUTOMERGE_RESPONSE}")
        case "${ERROR_MESSAGE}" in
            # Already on from an earlier run of this workflow - the PR is simply waiting for
            # its checks, which is the expected steady state between runs.
            *"already enabled"*)
                log "  auto-merge is already enabled on #${PR_NUMBER}"
                ;;
            *)
                warn "could not enable auto-merge on PR #${PR_NUMBER}: ${ERROR_MESSAGE}"
                warn "the PR is still valid, it just needs a manual merge once its checks pass"
                ;;
        esac
    else
        log "  enabled auto-merge on #${PR_NUMBER}"
    fi
done <<<"${ELIGIBLE}"
