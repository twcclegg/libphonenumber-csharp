#! /bin/bash
# Syncs resources/ from the latest google/libphonenumber release, regenerates the locale
# data, and opens a PR with the result. Building and testing is left to that PR's own
# required status checks rather than duplicated here - see finalize-metadata-release.sh for
# the half that tags the merge commit, creates the GitHub release, and publishes to NuGet
# once the PR merges.
#
# This used to commit and push straight to main. It switched to opening a PR because main
# now requires status checks before any push lands, including from this automation's
# GITHUB_TOKEN - a direct push can never satisfy that (the checks have nothing to run
# against yet), so GitHub rejects it outright.
#
# No open PR: sync, open one with auto-merge off, stop. One already open: regenerate onto the same
# branch, force-push, turn auto-merge on. See "CI and release" in AGENTS.md for why.
#
# Exit on any error, treat unset variables as errors, and fail a pipeline if any
# stage fails. The pipefail matters here: every network read below is `curl | jq`,
# and without it a failed curl would feed empty input to the parser and the script
# would happily proceed with an empty version string — potentially cutting a bogus
# release. Fail closed instead.
set -euo pipefail

# Exit codes
readonly EXIT_USAGE=2
readonly EXIT_MISSING_PREREQUISITE=3
readonly EXIT_NEEDS_ATTENTION=4

usage() {
    cat <<'EOF'
Usage: github-actions-metadata-update.sh [options] [github-token]

The GitHub token may be supplied as the positional argument or via the
GITHUB_TOKEN environment variable. It is required unless --dry-run is used.

Options:
  --skip-java-check    Continue even when the upstream diff contains .java files.
                       Only use this when those changes have been reviewed and do
                       not need porting to the C# library.
  --skip-proto-check   Continue even when the upstream diff contains .proto files.
  --dry-run            Run every read-only step - version lookups, repository
                       checks, the upstream diff gates and the upstream clone -
                       report what would happen, then stop before the first
                       change to the working tree. Nothing is copied, generated,
                       committed, pushed or released. On a branch other than a
                       clean main the usual hard checks become warnings, so a dry
                       run works from a feature branch.
  -h, --help           Show this help and exit.

Environment variables:
  GITHUB_TOKEN             GitHub token used for the api calls and to push the branch.
  GITHUB_REPOSITORY        owner/name of the repository to open the PR against. Set
                           automatically by GitHub Actions; falls back to the origin
                           remote, so a fork releases to itself.
  UPSTREAM_REPOSITORY      Repository the metadata comes from
                           (default google/libphonenumber).
  NUGET_PACKAGE_ID         Package whose published version is compared against
                           the upstream release (default libphonenumber-csharp).
  NUGET_EXTENSIONS_PACKAGE_ID
                           Helper package of C#-idiomatic additions beyond the java
                           port, linked from the release notes
                           (default <NUGET_PACKAGE_ID>.extensions).
  SKIP_JAVA_CHECK          Same as --skip-java-check (true/1/yes).
  SKIP_PROTO_CHECK         Same as --skip-proto-check (true/1/yes).
  DRY_RUN                  Same as --dry-run (true/1/yes).
  UPSTREAM_TAG             Use this upstream tag (e.g. v9.0.33) instead of asking
                           github for the latest release. Mainly for dry runs.
  DEPLOYED_VERSION         Use this published version (e.g. 9.0.32) instead of
                           asking nuget.org. Mainly for dry runs - together with
                           UPSTREAM_TAG it replays any historical release pair.
  EXPECTED_MAJOR_VERSION   Upstream major version this port tracks (default 9).

Examples:
  # what would the nightly run do right now?
  github-actions-metadata-update.sh --dry-run

  # replay a release that changed java files, with the override in place
  UPSTREAM_TAG=v9.0.33 DEPLOYED_VERSION=9.0.32 \
      github-actions-metadata-update.sh --dry-run --skip-java-check
EOF
}

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=./github-release-helpers.sh
source "${SCRIPT_DIR}/github-release-helpers.sh"

GITHUB_TOKEN="${GITHUB_TOKEN:-}"
SKIP_JAVA_CHECK="${SKIP_JAVA_CHECK:-false}"
SKIP_PROTO_CHECK="${SKIP_PROTO_CHECK:-false}"
DRY_RUN="${DRY_RUN:-false}"
UPSTREAM_TAG="${UPSTREAM_TAG:-}"
DEPLOYED_VERSION="${DEPLOYED_VERSION:-}"
EXPECTED_MAJOR_VERSION="${EXPECTED_MAJOR_VERSION:-9}"

while [ $# -gt 0 ]; do
    case "$1" in
        --skip-java-check) SKIP_JAVA_CHECK=true ;;
        --skip-proto-check) SKIP_PROTO_CHECK=true ;;
        --dry-run) DRY_RUN=true ;;
        -h | --help)
            usage
            exit 0
            ;;
        -*)
            usage >&2
            fail ${EXIT_USAGE} "unknown option: $1"
            ;;
        *) GITHUB_TOKEN="$1" ;;
    esac
    shift
done

if isTrue "${DRY_RUN}"; then
    log "dry run: no files will be changed, nothing will be committed, pushed or released"
    if [ -z "${GITHUB_TOKEN}" ]; then
        warn "no github token, api calls will be unauthenticated and subject to a much lower rate limit"
    fi
elif [ -z "${GITHUB_TOKEN}" ]; then
    usage >&2
    fail ${EXIT_USAGE} "GitHub token required"
fi

for tool in curl jq git; do
    if ! command -v "${tool}" &>/dev/null; then
        fail ${EXIT_MISSING_PREREQUISITE} "${tool} required"
    fi
done

# Only needed once the script starts generating, which a dry run never reaches - report
# them there rather than refusing to run.
for tool in javac java; do
    if ! command -v "${tool}" &>/dev/null; then
        if isTrue "${DRY_RUN}"; then
            warn "${tool} not found, a real run would stop here"
        else
            fail ${EXIT_MISSING_PREREQUISITE} "${tool} required"
        fi
    fi
done

GITHUB_ACTION_WORKING_DIRECTORY=$(pwd)

# Which repository this run targets. Actions sets GITHUB_REPOSITORY for us; when
# it is not set fall back to the origin remote, so a fork or a scratch clone
# releases to itself instead of to the upstream project.
resolveRepository() {
    local url

    if [ -n "${GITHUB_REPOSITORY:-}" ]; then
        echo "${GITHUB_REPOSITORY}"
        return 0
    fi

    url=$(git remote get-url origin 2>/dev/null || true)
    url="${url%.git}"

    case "${url}" in
        *github.com[:/]*)
            echo "${url##*github.com}" | sed 's|^[:/]*||'
            ;;
        *) return 1 ;;
    esac
}

getLatestGitHubRelease() {
    ghApi "https://api.github.com/repos/$1/releases/latest" | jq -er '.tag_name'
}

# The flat container index is specified as a list of versions, not a sorted one,
# so pick the highest stable version rather than trusting document order.
getLatestNugetRelease() {
    local packageId
    packageId=$(toLower "$1")

    curl --fail --silent --show-error --location --retry 3 --retry-delay 5 \
        "https://api.nuget.org/v3-flatcontainer/${packageId}/index.json" \
        | jq -er '.versions[] | select(test("-") | not)' \
        | sort -V \
        | tail -n 1
}

getReleaseDelta() {
    ghApi "https://api.github.com/repos/$1/compare/$2...$3"
}

if [ -n "${UPSTREAM_TAG}" ]; then
    UPSTREAM_GITHUB_RELEASE_TAG="${UPSTREAM_TAG}"
    log "${UPSTREAM_REPOSITORY} release overridden to ${UPSTREAM_GITHUB_RELEASE_TAG}"
else
    UPSTREAM_GITHUB_RELEASE_TAG=$(getLatestGitHubRelease "${UPSTREAM_REPOSITORY}")
    log "${UPSTREAM_REPOSITORY} latest release is ${UPSTREAM_GITHUB_RELEASE_TAG}"
fi

if [ -n "${DEPLOYED_VERSION}" ]; then
    DEPLOYED_NUGET_TAG="${DEPLOYED_VERSION}"
    log "${NUGET_PACKAGE_ID} version overridden to ${DEPLOYED_NUGET_TAG}"
else
    DEPLOYED_NUGET_TAG=$(getLatestNugetRelease "${NUGET_PACKAGE_ID}")
    log "${NUGET_PACKAGE_ID} latest release is ${DEPLOYED_NUGET_TAG}"
fi

if [[ ! "${UPSTREAM_GITHUB_RELEASE_TAG}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    fail 1 "unexpected upstream release tag: ${UPSTREAM_GITHUB_RELEASE_TAG}"
fi

if [[ ! "${DEPLOYED_NUGET_TAG}" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    fail 1 "unexpected deployed nuget version: ${DEPLOYED_NUGET_TAG}"
fi

UPSTREAM_VERSION="${UPSTREAM_GITHUB_RELEASE_TAG#v}"
UPSTREAM_MAJOR_VERSION="${UPSTREAM_VERSION%%.*}"

if [ "${UPSTREAM_MAJOR_VERSION}" != "${EXPECTED_MAJOR_VERSION}" ]; then
    fail ${EXIT_NEEDS_ATTENTION} \
        "major version update: upstream is ${UPSTREAM_GITHUB_RELEASE_TAG}, this port tracks ${EXPECTED_MAJOR_VERSION}.x"
fi

if [ "${DEPLOYED_NUGET_TAG}" = "${UPSTREAM_VERSION}" ]; then
    log "versions match, new release not required"
    exit 0
fi

# Nothing to do when the published package is already ahead of upstream, which
# happens after a C# only patch release.
OLDEST_VERSION=$(printf '%s\n%s\n' "${UPSTREAM_VERSION}" "${DEPLOYED_NUGET_TAG}" | sort -V | head -n 1)
if [ "${OLDEST_VERSION}" = "${UPSTREAM_VERSION}" ]; then
    log "deployed version ${DEPLOYED_NUGET_TAG} is ahead of upstream ${UPSTREAM_VERSION}, new release not required"
    exit 0
fi

# The checkout this script runs in is the one that gets committed and pushed, so
# it is the one that has to be on a clean main.
cd "${GITHUB_ACTION_WORKING_DIRECTORY}"

if [ ! -d resources ] || [ ! -f lib/DumpLocale.java ]; then
    fail 1 "must be run from the root of the repository (no resources/ and lib/DumpLocale.java here)"
fi

GITHUB_REPOSITORY=$(resolveRepository) \
    || fail 1 "could not determine the target repository, set GITHUB_REPOSITORY to owner/name"

if [[ ! "${GITHUB_REPOSITORY}" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]]; then
    fail 1 "unexpected target repository: ${GITHUB_REPOSITORY}"
fi

log "target repository is ${GITHUB_REPOSITORY}"

BRANCH="metadata-update/${UPSTREAM_GITHUB_RELEASE_TAG}"

# Which half of the flow this run is. An open PR for this branch is the only state either half
# depends on, so nothing has to be persisted anywhere.
REFRESHING_PR_NUMBER=""
REFRESHING_PR_NODE_ID=""
if ! isTrue "${DRY_RUN}"; then
    OPEN_PRS=$(ghApi "https://api.github.com/repos/${GITHUB_REPOSITORY}/pulls?state=open&base=main&head=${GITHUB_REPOSITORY%%/*}:${BRANCH}")
    REFRESHING_PR_NUMBER=$(jq -r '.[0].number // empty' <<<"${OPEN_PRS}")
    REFRESHING_PR_NODE_ID=$(jq -r '.[0].node_id // empty' <<<"${OPEN_PRS}")

    if [ -n "${REFRESHING_PR_NUMBER}" ]; then
        log "PR #${REFRESHING_PR_NUMBER} for ${UPSTREAM_GITHUB_RELEASE_TAG} was not merged by hand, so this run regenerates it and enables auto-merge"
    fi
fi

# A dry run changes nothing, so neither of these is dangerous there - downgrade
# them to warnings so the pipeline can be exercised from a feature branch.
requireRepositoryState() {
    if isTrue "${DRY_RUN}"; then
        warn "$1 (ignored for the dry run)"
    else
        fail 1 "$1"
    fi
}

if [ "$(git branch --show-current)" != "main" ]; then
    requireRepositoryState "must be on main branch"
fi

if [ -n "$(git status --porcelain)" ]; then
    requireRepositoryState "working directory is not clean"
fi

WORK_DIR=$(mktemp -d)
cleanup() {
    rm -rf "${WORK_DIR}"
}
trap cleanup EXIT

COMPARE_JSON=$(getReleaseDelta "${UPSTREAM_REPOSITORY}" "v${DEPLOYED_NUGET_TAG}" "${UPSTREAM_GITHUB_RELEASE_TAG}")
FILES=$(jq -er '.files // error("compare response contains no file list") | .[].filename' <<<"${COMPARE_JSON}")

if [ -z "${FILES}" ]; then
    fail 1 "no changed files reported between v${DEPLOYED_NUGET_TAG} and ${UPSTREAM_GITHUB_RELEASE_TAG}"
fi

# The compare api returns at most 300 files, so the checks below can miss changes
# in a very large release.
if [ "$(jq -r '.files | length' <<<"${COMPARE_JSON}")" -ge 300 ]; then
    warn "the compare api returned the maximum of 300 files, the change list may be truncated"
fi

JAVA_FILES=$(grep -E '\.java$' <<<"${FILES}" || true)
if [ -n "${JAVA_FILES}" ]; then
    printf 'upstream diff contains java files:\n%s\n' "${JAVA_FILES}"
    if isTrue "${SKIP_JAVA_CHECK}"; then
        warn "continuing anyway because --skip-java-check / SKIP_JAVA_CHECK is set"
    else
        fail ${EXIT_NEEDS_ATTENTION} \
            "has java files, automatic update not possible (re-run with --skip-java-check or SKIP_JAVA_CHECK=true to override)"
    fi
fi

PROTO_FILES=$(grep -E '\.proto$' <<<"${FILES}" || true)
if [ -n "${PROTO_FILES}" ]; then
    printf 'upstream diff contains proto files:\n%s\n' "${PROTO_FILES}"
    if isTrue "${SKIP_PROTO_CHECK}"; then
        warn "continuing anyway because --skip-proto-check / SKIP_PROTO_CHECK is set"
    else
        fail ${EXIT_NEEDS_ATTENTION} \
            "has proto files, automatic update not possible (re-run with --skip-proto-check or SKIP_PROTO_CHECK=true to override)"
    fi
fi

# Cloning upstream is safe either way: it only writes to the temporary directory,
# and it is the last thing that can fail before the working tree is touched.
git -c advice.detachedHead=false clone --quiet --depth 1 --branch "${UPSTREAM_GITHUB_RELEASE_TAG}" \
    "https://github.com/${UPSTREAM_REPOSITORY}.git" "${WORK_DIR}/libphonenumber"

# resources/metadata/ is ~1300 per-calling-code csv files (ranges, examples, operators,
# shortcodes, ...) that upstream generates for its own tooling, about 72 MiB of the 87 MiB
# resources/ used to weigh. Nothing in this port reads them: the build generates its binary
# metadata from PhoneNumberMetadata.xml and friends, and the geocoding, carrier, timezone and
# locale tables come from their own directories. They were only ever committed here because this
# script copied the whole upstream directory in one go.
UPSTREAM_RESOURCES_EXCLUDES=(metadata)

UPSTREAM_RESOURCES="${WORK_DIR}/libphonenumber/resources"
if [ -z "$(ls -A "${UPSTREAM_RESOURCES}" 2>/dev/null)" ]; then
    fail 1 "upstream resources directory is missing or empty"
fi

# Everything past this point writes to the working tree or to github.
if isTrue "${DRY_RUN}"; then
    log ""
    log "dry run complete, a real run would now:"
    log "  - replace ${GITHUB_ACTION_WORKING_DIRECTORY}/resources with $(find "${UPSTREAM_RESOURCES}" -type f | wc -l | tr -d ' ') files from ${UPSTREAM_GITHUB_RELEASE_TAG}, less ${UPSTREAM_RESOURCES_EXCLUDES[*]}/"
    log "  - regenerate resources/locale/country_names.txt with $(java -version 2>&1 | head -n 1 || echo 'the local jdk')"
    log "  - add a CHANGELOG.md entry for ${UPSTREAM_GITHUB_RELEASE_TAG}"
    log "  - commit \"feat: automatic upgrade to ${UPSTREAM_GITHUB_RELEASE_TAG}\" on ${BRANCH} and push it"
    log "  - open a PR from ${BRANCH} into main for review, leaving auto-merge off"
    log "  - if nobody merges it, a later run regenerates that branch and enables auto-merge as a backstop"
    log "  - on merge, finalize-metadata-release.sh creates release ${UPSTREAM_GITHUB_RELEASE_TAG} and dispatches ${PUBLISH_WORKFLOW}"
    exit 0
fi

rm -rf "${GITHUB_ACTION_WORKING_DIRECTORY:?}/resources"
mkdir -p "${GITHUB_ACTION_WORKING_DIRECTORY}/resources"
cp -r "${UPSTREAM_RESOURCES}/." "${GITHUB_ACTION_WORKING_DIRECTORY}/resources/"

# Upstream directories this port does not read, dropped again right after the copy. Kept as an
# exclusion list rather than an allow-list on purpose: a copy-everything-then-remove keeps
# resources/ a verbatim mirror of upstream apart from these named exceptions, so anything new
# upstream adds arrives on its own and is visible in the sync PR's diff. An allow-list would
# silently drop it instead, and a file this port needs going missing is the worse failure.
for excluded in "${UPSTREAM_RESOURCES_EXCLUDES[@]}"; do
    rm -rf "${GITHUB_ACTION_WORKING_DIRECTORY:?}/resources/${excluded:?}"
done

# Generate into the temporary directory first, so a failure part way through can
# never leave a truncated country_names.txt or a stray DumpLocale.class behind for
# `git add -A` to pick up. This has to run after the resources/ replacement above,
# which wipes the directory this writes into: the locale data is generated from the
# local jdk rather than copied from upstream.
cd "${GITHUB_ACTION_WORKING_DIRECTORY}/lib"
javac -d "${WORK_DIR}/classes" DumpLocale.java
java -cp "${WORK_DIR}/classes" DumpLocale >"${WORK_DIR}/country_names.txt"

if [ ! -s "${WORK_DIR}/country_names.txt" ]; then
    fail 1 "DumpLocale produced no output"
fi

mkdir -p "${GITHUB_ACTION_WORKING_DIRECTORY}/resources/locale"
mv "${WORK_DIR}/country_names.txt" "${GITHUB_ACTION_WORKING_DIRECTORY}/resources/locale/country_names.txt"

cd "${GITHUB_ACTION_WORKING_DIRECTORY}"
if [ -z "$(git status --porcelain)" ]; then
    log "no changes after metadata sync, new release not required"
    exit 0
fi

# Record this release in CHANGELOG.md in the same commit as the metadata sync, rather than as a
# separate PR once the tag exists: the version number is already known here (it's
# UPSTREAM_GITHUB_RELEASE_TAG itself - this port tracks upstream's version 1:1), so there is
# nothing to guess. The finalize step (finalize-metadata-release.sh) only tags and releases an
# existing commit; it can't push a follow-up commit of its own; main's branch-protection ruleset
# requires a PR for every push, with no bypass for any actor, including this automation's own
# bot account - the same reason this script opens a PR instead of pushing directly (see the
# file-level comment above). Doing it here keeps everything in the one PR that already goes
# through that ruleset.
CHANGELOG_FILE="${GITHUB_ACTION_WORKING_DIRECTORY}/CHANGELOG.md"
if [ -f "${CHANGELOG_FILE}" ] && grep -qF '<!-- next-entry -->' "${CHANGELOG_FILE}"; then
    # Every release always includes a metadata sync (that's the only thing that ever cuts a tag),
    # but some releases also bundle other work merged to `main` in between - a version number alone
    # doesn't say which. Diff this repo's own history since the last release (not the upstream diff
    # checked above, which is google/libphonenumber's) against everything but resources/ itself and
    # this bookkeeping file, so update-changelog.sh can tell whether this release is foldable into a
    # prior metadata-only run or needs its own standalone entry. Deliberately NOT excluded:
    # CountryCodeToRegionCodeMap.cs - despite its name, it is hand-maintained (its own header still
    # says "todo make this file automatically generated"), so a change to it is real, hand-relevant
    # content, not a mechanical byproduct of this sync. Fetching just the one tag works even from a
    # shallow checkout: a tree-level `git diff` needs both commits' trees, not a connected history
    # between them.
    METADATA_ONLY=true
    if git fetch --quiet --depth=1 origin "refs/tags/v${DEPLOYED_NUGET_TAG}:refs/tags/v${DEPLOYED_NUGET_TAG}" 2>/dev/null \
        && git rev-parse -q --verify "v${DEPLOYED_NUGET_TAG}" >/dev/null; then
        NON_METADATA_FILES=$(git diff --name-only "v${DEPLOYED_NUGET_TAG}" HEAD -- . ':!resources' ':!CHANGELOG.md')
        if [ -n "${NON_METADATA_FILES}" ]; then
            METADATA_ONLY=false
        fi
    else
        # Fail closed: better to give this release its own entry than to silently fold real changes
        # away as if they never happened because the one tag needed to check couldn't be fetched.
        warn "could not fetch v${DEPLOYED_NUGET_TAG} to check for non-metadata changes since the last release"
        METADATA_ONLY=false
    fi

    bash "${SCRIPT_DIR}/update-changelog.sh" "${CHANGELOG_FILE}" "${GITHUB_REPOSITORY}" "${UPSTREAM_REPOSITORY}" \
        "v${DEPLOYED_NUGET_TAG}" "${UPSTREAM_GITHUB_RELEASE_TAG}" "${METADATA_ONLY}" "$(date -u +%F)"
else
    warn "CHANGELOG.md missing or missing the '<!-- next-entry -->' marker, skipping changelog update"
fi

git checkout -b "${BRANCH}"
git add -A
git -c user.email='<>' -c user.name='libphonenumber-csharp-bot' \
    commit -m "feat: automatic upgrade to ${UPSTREAM_GITHUB_RELEASE_TAG}"

# Force is safe: this branch carries nothing but this automation's own PRs, and overwriting
# whatever is on it is the point rather than a side effect.
git push --force origin "HEAD:refs/heads/${BRANCH}"



if [ -n "${REFRESHING_PR_NUMBER}" ]; then
    PR_NUMBER="${REFRESHING_PR_NUMBER}"
    PR_NODE_ID="${REFRESHING_PR_NODE_ID}"
    log "refreshed PR #${PR_NUMBER} with a newly generated ${UPSTREAM_GITHUB_RELEASE_TAG} sync"
else
    PR_BODY=$(cat <<EOF
Syncs \`resources/\` from [${UPSTREAM_REPOSITORY} ${UPSTREAM_GITHUB_RELEASE_TAG}](https://github.com/${UPSTREAM_REPOSITORY}/releases/tag/${UPSTREAM_GITHUB_RELEASE_TAG}), regenerates \`resources/locale/country_names.txt\`, and records the release in \`CHANGELOG.md\`.

**Review and merge this when you are happy with it** - that is the intended way for a metadata release to ship.

If it is still open at the next daily [create_new_release_on_new_metadata_update.yml](.github/workflows/create_new_release_on_new_metadata_update.yml) run, that run regenerates this branch from ${UPSTREAM_GITHUB_RELEASE_TAG} and turns auto-merge on, so a sync is never left stalled because nobody was around. Don't push fixes to this branch - that regeneration force-pushes over anything else that is there, deliberately: the commit that merges is always one this automation just built.

On merge, [finalize_metadata_release.yml](.github/workflows/finalize_metadata_release.yml) tags the merge commit, creates the GitHub release, and dispatches the NuGet publish.
EOF
    )

    PR_RESPONSE=$(jq -n --arg title "feat: automatic upgrade to ${UPSTREAM_GITHUB_RELEASE_TAG}" \
        --arg head "${BRANCH}" --arg base "main" --arg body "${PR_BODY}" \
        '{title: $title, head: $head, base: $base, body: $body}' \
        | ghApi -X POST --data @- "https://api.github.com/repos/${GITHUB_REPOSITORY}/pulls")

    PR_NUMBER=$(jq -er '.number' <<<"${PR_RESPONSE}")
    PR_NODE_ID=$(jq -er '.node_id' <<<"${PR_RESPONSE}")
    # Auto-merge stays off: this PR is for a person to read and merge.
    log "opened PR #${PR_NUMBER} for ${UPSTREAM_GITHUB_RELEASE_TAG} with auto-merge off; a later run arms it if nobody merges it first"
    exit 0
fi

# Immediately after the force-push, which is the only moment github accepts this: the mutation is
# rejected unless the PR is blocked from merging, and the push has just put its checks back into
# pending.
AUTOMERGE_ERROR=$(armAutoMerge "${PR_NODE_ID}")
if [ -z "${AUTOMERGE_ERROR}" ]; then
    log "enabled auto-merge on PR #${PR_NUMBER}; it merges once its required checks pass"
else
    # Fatal rather than a warning: this runs again every day, so a permanent failure would
    # otherwise loop silently - regenerate, force-push, warn, exit 0 - burning a build a day on a
    # release that never ships while every run reports green.
    warn "the PR is still valid, it just needs a merge by hand once its checks pass"
    fail 1 "could not enable auto-merge on PR #${PR_NUMBER}: ${AUTOMERGE_ERROR}"
fi
