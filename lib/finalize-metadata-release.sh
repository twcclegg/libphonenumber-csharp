#! /bin/bash
# Creates the GitHub release and dispatches the NuGet publish for a metadata-update PR that
# has just been merged - the second half of github-actions-metadata-update.sh, which only
# opens that PR and stops. Split across two scripts because they run from two different
# triggers (schedule/dispatch vs. pull_request closed) with nothing else in common: this
# half needs no upstream checkout, build, or test, just the tag and the merge commit.
set -euo pipefail

usage() {
    cat <<'EOF'
Usage: finalize-metadata-release.sh <upstream-tag-or-branch> <release-commit> [github-token]

<upstream-tag-or-branch> is either a bare tag (v9.0.38) or a metadata-update branch name
(metadata-update/v9.0.38, as created by github-actions-metadata-update.sh) - the
metadata-update/ prefix is stripped if present, so the caller can pass the merged PR's head
ref directly.

The GitHub token may be supplied as the third positional argument or via the GITHUB_TOKEN
environment variable.

Environment variables:
  GITHUB_TOKEN             GitHub token used for the api calls.
  GITHUB_REPOSITORY        owner/name of the repository to release in. Set automatically by
                           GitHub Actions.
  NUGET_PACKAGE_ID         Package linked from the release notes (default libphonenumber-csharp).
  NUGET_EXTENSIONS_PACKAGE_ID
                           Companion package linked from the release notes
                           (default <NUGET_PACKAGE_ID>.extensions).
  UPSTREAM_REPOSITORY      Repository the metadata came from (default google/libphonenumber).
  PUBLISH_WORKFLOW         Workflow dispatched to publish the release to nuget.org
                           (default publish_nuget.yml).
EOF
}

if [ $# -lt 2 ] || [ $# -gt 3 ]; then
    usage >&2
    exit 2
fi

TAG_OR_BRANCH="$1"
RELEASE_COMMIT="$2"
GITHUB_TOKEN="${3:-${GITHUB_TOKEN:-}}"

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=./github-release-helpers.sh
source "${SCRIPT_DIR}/github-release-helpers.sh"

if [ -z "${GITHUB_TOKEN}" ]; then
    usage >&2
    fail 2 "GitHub token required"
fi

UPSTREAM_GITHUB_RELEASE_TAG="${TAG_OR_BRANCH#metadata-update/}"

if [[ ! "${UPSTREAM_GITHUB_RELEASE_TAG}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    fail 1 "unexpected upstream release tag: ${UPSTREAM_GITHUB_RELEASE_TAG}"
fi

if [[ ! "${RELEASE_COMMIT}" =~ ^[0-9a-f]{40}$ ]]; then
    fail 1 "unexpected release commit: ${RELEASE_COMMIT}"
fi

: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY required}"

createRelease "${GITHUB_REPOSITORY}" "${UPSTREAM_GITHUB_RELEASE_TAG}" "${RELEASE_COMMIT}"
log "created release ${UPSTREAM_GITHUB_RELEASE_TAG} at ${RELEASE_COMMIT}"

dispatchPublish "${GITHUB_REPOSITORY}" "${UPSTREAM_GITHUB_RELEASE_TAG}"
log "dispatched ${PUBLISH_WORKFLOW} for ${UPSTREAM_GITHUB_RELEASE_TAG}"
