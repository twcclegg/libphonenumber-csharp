#! /bin/bash
# Syncs resources/ + LocaleData.cs from the latest google/libphonenumber release,
# builds and tests, then commits, pushes and creates a GitHub release.
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
  GITHUB_TOKEN             GitHub token used for the api calls and the release.
  GITHUB_REPOSITORY        owner/name of the repository to commit to and release
                           from. Set automatically by GitHub Actions; falls back
                           to the origin remote, so a fork releases to itself.
  UPSTREAM_REPOSITORY      Repository the metadata comes from
                           (default google/libphonenumber).
  NUGET_PACKAGE_ID         Package whose published version is compared against
                           the upstream release (default libphonenumber-csharp).
  SKIP_JAVA_CHECK          Same as --skip-java-check (true/1/yes).
  SKIP_PROTO_CHECK         Same as --skip-proto-check (true/1/yes).
  DRY_RUN                  Same as --dry-run (true/1/yes).
  UPSTREAM_TAG             Use this upstream tag (e.g. v9.0.33) instead of asking
                           github for the latest release. Mainly for dry runs.
  DEPLOYED_VERSION         Use this published version (e.g. 9.0.32) instead of
                           asking nuget.org. Mainly for dry runs - together with
                           UPSTREAM_TAG it replays any historical release pair.
  EXPECTED_MAJOR_VERSION   Upstream major version this port tracks (default 9).
  TEST_TARGET_FRAMEWORK    Framework used for the pre-commit test run (default net10.0).

Examples:
  # what would the nightly run do right now?
  github-actions-metadata-update.sh --dry-run

  # replay a release that changed java files, with the override in place
  UPSTREAM_TAG=v9.0.33 DEPLOYED_VERSION=9.0.32 \
      github-actions-metadata-update.sh --dry-run --skip-java-check
EOF
}

log() {
    echo "$*"
}

warn() {
    echo "warning: $*" >&2
}

# fail <exit-code> <message>
fail() {
    local code=$1
    shift
    echo "error: $*" >&2
    if [ -n "${GITHUB_STEP_SUMMARY:-}" ]
    then
        echo "$*" >> "${GITHUB_STEP_SUMMARY}"
    fi
    exit "${code}"
}

# Lower casing without ${var,,}, which needs bash 4 - macOS still ships bash 3.2.
toLower() {
    printf '%s' "$1" | tr '[:upper:]' '[:lower:]'
}

isTrue() {
    case "$(toLower "$1")" in
        true|1|yes|y) return 0 ;;
        *) return 1 ;;
    esac
}

GITHUB_TOKEN="${GITHUB_TOKEN:-}"
SKIP_JAVA_CHECK="${SKIP_JAVA_CHECK:-false}"
SKIP_PROTO_CHECK="${SKIP_PROTO_CHECK:-false}"
DRY_RUN="${DRY_RUN:-false}"
UPSTREAM_TAG="${UPSTREAM_TAG:-}"
DEPLOYED_VERSION="${DEPLOYED_VERSION:-}"
EXPECTED_MAJOR_VERSION="${EXPECTED_MAJOR_VERSION:-9}"
TEST_TARGET_FRAMEWORK="${TEST_TARGET_FRAMEWORK:-net10.0}"

while [ $# -gt 0 ]
do
    case "$1" in
        --skip-java-check) SKIP_JAVA_CHECK=true ;;
        --skip-proto-check) SKIP_PROTO_CHECK=true ;;
        --dry-run) DRY_RUN=true ;;
        -h|--help) usage; exit 0 ;;
        -*) usage >&2; fail ${EXIT_USAGE} "unknown option: $1" ;;
        *) GITHUB_TOKEN="$1" ;;
    esac
    shift
done

if isTrue "${DRY_RUN}"
then
    log "dry run: no files will be changed, nothing will be committed, pushed or released"
    if [ -z "${GITHUB_TOKEN}" ]
    then
        warn "no github token, api calls will be unauthenticated and subject to a much lower rate limit"
    fi
elif [ -z "${GITHUB_TOKEN}" ]
then
    usage >&2
    fail ${EXIT_USAGE} "GitHub token required"
fi

for tool in curl jq git
do
    if ! command -v "${tool}" &> /dev/null
    then
        fail ${EXIT_MISSING_PREREQUISITE} "${tool} required"
    fi
done

# Only needed once the script starts generating and building, which a dry run
# never reaches - report them there rather than refusing to run.
for tool in javac java dotnet
do
    if ! command -v "${tool}" &> /dev/null
    then
        if isTrue "${DRY_RUN}"
        then
            warn "${tool} not found, a real run would stop here"
        else
            fail ${EXIT_MISSING_PREREQUISITE} "${tool} required"
        fi
    fi
done

UPSTREAM_REPOSITORY="${UPSTREAM_REPOSITORY:-google/libphonenumber}"
NUGET_PACKAGE_ID="${NUGET_PACKAGE_ID:-libphonenumber-csharp}"
GITHUB_ACTION_WORKING_DIRECTORY=$(pwd)

# Which repository this run targets. Actions sets GITHUB_REPOSITORY for us; when
# it is not set fall back to the origin remote, so a fork or a scratch clone
# releases to itself instead of to the upstream project.
resolveRepository() {
    local url

    if [ -n "${GITHUB_REPOSITORY:-}" ]
    then
        echo "${GITHUB_REPOSITORY}"
        return 0
    fi

    url=$(git remote get-url origin 2> /dev/null || true)
    url="${url%.git}"

    case "${url}" in
        *github.com[:/]*) echo "${url##*github.com}" | sed 's|^[:/]*||' ;;
        *) return 1 ;;
    esac
}

# Authenticated api calls, so the job is not subject to the unauthenticated rate
# limit shared by every action runner on the same address. The header is built as
# an array so the token stays a single argument, and is omitted entirely when
# there is no token (dry runs only).
GITHUB_AUTH_HEADER=()
if [ -n "${GITHUB_TOKEN}" ]
then
    GITHUB_AUTH_HEADER=(-H "Authorization: Bearer ${GITHUB_TOKEN}")
fi

ghApi() {
    curl --fail --silent --show-error --location --retry 3 --retry-delay 5 \
        -H "Accept: application/vnd.github+json" \
        -H "X-GitHub-Api-Version: 2022-11-28" \
        "${GITHUB_AUTH_HEADER[@]}" \
        "$@"
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

createRelease() {
    jq -n --arg tag "$2" '{tag_name: $tag, name: $tag}' \
        | ghApi -X POST --data @- "https://api.github.com/repos/$1/releases" > /dev/null
}

if [ -n "${UPSTREAM_TAG}" ]
then
    UPSTREAM_GITHUB_RELEASE_TAG="${UPSTREAM_TAG}"
    log "${UPSTREAM_REPOSITORY} release overridden to ${UPSTREAM_GITHUB_RELEASE_TAG}"
else
    UPSTREAM_GITHUB_RELEASE_TAG=$(getLatestGitHubRelease ${UPSTREAM_REPOSITORY})
    log "${UPSTREAM_REPOSITORY} latest release is ${UPSTREAM_GITHUB_RELEASE_TAG}"
fi

if [ -n "${DEPLOYED_VERSION}" ]
then
    DEPLOYED_NUGET_TAG="${DEPLOYED_VERSION}"
    log "${NUGET_PACKAGE_ID} version overridden to ${DEPLOYED_NUGET_TAG}"
else
    DEPLOYED_NUGET_TAG=$(getLatestNugetRelease ${NUGET_PACKAGE_ID})
    log "${NUGET_PACKAGE_ID} latest release is ${DEPLOYED_NUGET_TAG}"
fi

if [[ ! "${UPSTREAM_GITHUB_RELEASE_TAG}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]
then
    fail 1 "unexpected upstream release tag: ${UPSTREAM_GITHUB_RELEASE_TAG}"
fi

if [[ ! "${DEPLOYED_NUGET_TAG}" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]
then
    fail 1 "unexpected deployed nuget version: ${DEPLOYED_NUGET_TAG}"
fi

UPSTREAM_VERSION="${UPSTREAM_GITHUB_RELEASE_TAG#v}"
UPSTREAM_MAJOR_VERSION="${UPSTREAM_VERSION%%.*}"

if [ "${UPSTREAM_MAJOR_VERSION}" != "${EXPECTED_MAJOR_VERSION}" ]
then
    fail ${EXIT_NEEDS_ATTENTION} \
        "major version update: upstream is ${UPSTREAM_GITHUB_RELEASE_TAG}, this port tracks ${EXPECTED_MAJOR_VERSION}.x"
fi

if [ "${DEPLOYED_NUGET_TAG}" = "${UPSTREAM_VERSION}" ]
then
    log "versions match, new release not required"
    exit 0
fi

# Nothing to do when the published package is already ahead of upstream, which
# happens after a C# only patch release.
OLDEST_VERSION=$(printf '%s\n%s\n' "${UPSTREAM_VERSION}" "${DEPLOYED_NUGET_TAG}" | sort -V | head -n 1)
if [ "${OLDEST_VERSION}" = "${UPSTREAM_VERSION}" ]
then
    log "deployed version ${DEPLOYED_NUGET_TAG} is ahead of upstream ${UPSTREAM_VERSION}, new release not required"
    exit 0
fi

# The checkout this script runs in is the one that gets committed and pushed, so
# it is the one that has to be on a clean main.
cd "${GITHUB_ACTION_WORKING_DIRECTORY}"

if [ ! -d resources ] || [ ! -f lib/DumpLocale.java ]
then
    fail 1 "must be run from the root of the repository (no resources/ and lib/DumpLocale.java here)"
fi

GITHUB_REPOSITORY=$(resolveRepository) \
    || fail 1 "could not determine the target repository, set GITHUB_REPOSITORY to owner/name"

if [[ ! "${GITHUB_REPOSITORY}" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]]
then
    fail 1 "unexpected target repository: ${GITHUB_REPOSITORY}"
fi

log "target repository is ${GITHUB_REPOSITORY}"

# A dry run changes nothing, so neither of these is dangerous there - downgrade
# them to warnings so the pipeline can be exercised from a feature branch.
requireRepositoryState() {
    if isTrue "${DRY_RUN}"
    then
        warn "$1 (ignored for the dry run)"
    else
        fail 1 "$1"
    fi
}

if [ "$(git branch --show-current)" != "main" ]
then
    requireRepositoryState "must be on main branch"
fi

if [ -n "$(git status --porcelain)" ]
then
    requireRepositoryState "working directory is not clean"
fi

WORK_DIR=$(mktemp -d)
cleanup() {
    rm -rf "${WORK_DIR}"
}
trap cleanup EXIT

COMPARE_JSON=$(getReleaseDelta ${UPSTREAM_REPOSITORY} "v${DEPLOYED_NUGET_TAG}" "${UPSTREAM_GITHUB_RELEASE_TAG}")
FILES=$(jq -er '.files // error("compare response contains no file list") | .[].filename' <<< "${COMPARE_JSON}")

if [ -z "${FILES}" ]
then
    fail 1 "no changed files reported between v${DEPLOYED_NUGET_TAG} and ${UPSTREAM_GITHUB_RELEASE_TAG}"
fi

# The compare api returns at most 300 files, so the checks below can miss changes
# in a very large release.
if [ "$(jq -r '.files | length' <<< "${COMPARE_JSON}")" -ge 300 ]
then
    warn "the compare api returned the maximum of 300 files, the change list may be truncated"
fi

JAVA_FILES=$(grep -E '\.java$' <<< "${FILES}" || true)
if [ -n "${JAVA_FILES}" ]
then
    printf 'upstream diff contains java files:\n%s\n' "${JAVA_FILES}"
    if isTrue "${SKIP_JAVA_CHECK}"
    then
        warn "continuing anyway because --skip-java-check / SKIP_JAVA_CHECK is set"
    else
        fail ${EXIT_NEEDS_ATTENTION} \
            "has java files, automatic update not possible (re-run with --skip-java-check or SKIP_JAVA_CHECK=true to override)"
    fi
fi

PROTO_FILES=$(grep -E '\.proto$' <<< "${FILES}" || true)
if [ -n "${PROTO_FILES}" ]
then
    printf 'upstream diff contains proto files:\n%s\n' "${PROTO_FILES}"
    if isTrue "${SKIP_PROTO_CHECK}"
    then
        warn "continuing anyway because --skip-proto-check / SKIP_PROTO_CHECK is set"
    else
        fail ${EXIT_NEEDS_ATTENTION} \
            "has proto files, automatic update not possible (re-run with --skip-proto-check or SKIP_PROTO_CHECK=true to override)"
    fi
fi

if ! isTrue "${DRY_RUN}"
then
    git config --global user.email '<>'
    git config --global user.name 'libphonenumber-csharp-bot'
fi

# Cloning upstream is safe either way: it only writes to the temporary directory,
# and it is the last thing that can fail before the working tree is touched.
git -c advice.detachedHead=false clone --quiet --depth 1 --branch "${UPSTREAM_GITHUB_RELEASE_TAG}" \
    "https://github.com/${UPSTREAM_REPOSITORY}.git" "${WORK_DIR}/libphonenumber"

UPSTREAM_RESOURCES="${WORK_DIR}/libphonenumber/resources"
if [ -z "$(ls -A "${UPSTREAM_RESOURCES}" 2> /dev/null)" ]
then
    fail 1 "upstream resources directory is missing or empty"
fi

# Everything past this point writes to the working tree or to github.
if isTrue "${DRY_RUN}"
then
    log ""
    log "dry run complete, a real run would now:"
    log "  - replace ${GITHUB_ACTION_WORKING_DIRECTORY}/resources with $(find "${UPSTREAM_RESOURCES}" -type f | wc -l | tr -d ' ') files from ${UPSTREAM_GITHUB_RELEASE_TAG}"
    log "  - regenerate csharp/PhoneNumbers/LocaleData.cs with $(java -version 2>&1 | head -n 1 || echo 'the local jdk')"
    log "  - run dotnet restore, build and test (${TEST_TARGET_FRAMEWORK})"
    log "  - commit \"feat: automatic upgrade to ${UPSTREAM_GITHUB_RELEASE_TAG}\" and push to main"
    log "  - create release ${UPSTREAM_GITHUB_RELEASE_TAG} in ${GITHUB_REPOSITORY}"
    exit 0
fi

rm -rf "${GITHUB_ACTION_WORKING_DIRECTORY:?}/resources"/*
cp -r "${UPSTREAM_RESOURCES}/." "${GITHUB_ACTION_WORKING_DIRECTORY}/resources/"

# Generate into the temporary directory first, so a failure part way through can
# never leave a truncated LocaleData.cs or a stray DumpLocale.class behind for
# `git add -A` to pick up.
cd "${GITHUB_ACTION_WORKING_DIRECTORY}/lib"
javac -d "${WORK_DIR}/classes" DumpLocale.java
java -cp "${WORK_DIR}/classes" DumpLocale > "${WORK_DIR}/LocaleData.cs"

if [ ! -s "${WORK_DIR}/LocaleData.cs" ]
then
    fail 1 "DumpLocale produced no output"
fi

mv "${WORK_DIR}/LocaleData.cs" "${GITHUB_ACTION_WORKING_DIRECTORY}/csharp/PhoneNumbers/LocaleData.cs"

cd "${GITHUB_ACTION_WORKING_DIRECTORY}"
if [ -z "$(git status --porcelain)" ]
then
    log "no changes after metadata sync, new release not required"
    exit 0
fi

# Ensure project builds and passes tests before committing
cd "${GITHUB_ACTION_WORKING_DIRECTORY}/csharp"
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal "-p:TargetFrameworks=${TEST_TARGET_FRAMEWORK}"

cd "${GITHUB_ACTION_WORKING_DIRECTORY}"
git add -A
git commit -m "feat: automatic upgrade to ${UPSTREAM_GITHUB_RELEASE_TAG}"
git push

createRelease "${GITHUB_REPOSITORY}" "${UPSTREAM_GITHUB_RELEASE_TAG}"
log "created release ${UPSTREAM_GITHUB_RELEASE_TAG}"
