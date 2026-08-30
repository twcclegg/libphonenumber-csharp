#! /bin/bash
# Shared by github-actions-metadata-update.sh (opens the metadata-update PR) and
# finalize-metadata-release.sh (tags + releases it once merged) - both scripts source this
# rather than each carrying its own copy of the release-creation payload.
#
# Not meant to be run directly: it only defines functions and expects the caller to already
# have `set -euo pipefail`.

# Lower casing without ${var,,}, which needs bash 4 - macOS still ships bash 3.2.
toLower() {
    printf '%s' "$1" | tr '[:upper:]' '[:lower:]'
}

isTrue() {
    case "$(toLower "$1")" in
        true | 1 | yes | y) return 0 ;;
        *) return 1 ;;
    esac
}

log() {
    echo "$*"
}

warn() {
    echo "warning: $*" >&2
    # Also emit a GitHub Actions warning annotation, so a non-fatal problem shows up as a yellow
    # banner on the workflow run summary instead of only a line buried in step output that nobody
    # reads unless something else already prompted them to look - see the missing-CHANGELOG-marker
    # warning in github-actions-metadata-update.sh for what this matters for. `%`, CR and LF have to
    # be percent-escaped in the message: https://docs.github.com/actions/using-workflows/workflow-commands-for-github-actions
    local message="$*"
    message="${message//%/%25}"
    message="${message//$'\r'/%0D}"
    message="${message//$'\n'/%0A}"
    echo "::warning::${message}"
}

# fail <exit-code> <message>
fail() {
    local code=$1
    shift
    echo "error: $*" >&2
    if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
        echo "$*" >>"${GITHUB_STEP_SUMMARY}"
    fi
    exit "${code}"
}

NUGET_PACKAGE_ID="${NUGET_PACKAGE_ID:-libphonenumber-csharp}"
NUGET_EXTENSIONS_PACKAGE_ID="${NUGET_EXTENSIONS_PACKAGE_ID:-${NUGET_PACKAGE_ID}.extensions}"
UPSTREAM_REPOSITORY="${UPSTREAM_REPOSITORY:-google/libphonenumber}"
PUBLISH_WORKFLOW="${PUBLISH_WORKFLOW:-publish_nuget.yml}"

# Authenticated api calls, so the job is not subject to the unauthenticated rate limit shared
# by every action runner on the same address. The header is built as an array so the token
# stays a single argument, and is omitted entirely when there is no token (dry runs only).
GITHUB_AUTH_HEADER=()
if [ -n "${GITHUB_TOKEN:-}" ]; then
    GITHUB_AUTH_HEADER=(-H "Authorization: Bearer ${GITHUB_TOKEN}")
fi

ghApi() {
    curl --fail --silent --show-error --location --retry 3 --retry-delay 5 \
        -H "Accept: application/vnd.github+json" \
        -H "X-GitHub-Api-Version: 2022-11-28" \
        ${GITHUB_AUTH_HEADER[@]+"${GITHUB_AUTH_HEADER[@]}"} \
        "$@"
}

# generate_release_notes appends the commit/PR changelog below the links.
createRelease() {
    jq -n --arg tag "$2" --arg version "${2#v}" --arg commit "$3" \
        --arg pkg "${NUGET_PACKAGE_ID}" --arg ext "${NUGET_EXTENSIONS_PACKAGE_ID}" \
        --arg upstream "${UPSTREAM_REPOSITORY}" '
        {
            tag_name: $tag,
            name: $tag,
            target_commitish: $commit,
            generate_release_notes: true,
            body: (
                "[\($pkg) \($version)](https://www.nuget.org/packages/\($pkg)/\($version))"
                + " · [\($ext) \($version)](https://www.nuget.org/packages/\($ext)/\($version))"
                + " · [upstream \($tag)](https://github.com/\($upstream)/releases/tag/\($tag))"
            )
        }' \
        | ghApi -X POST --data @- "https://api.github.com/repos/$1/releases" >/dev/null
}

# github suppresses push events from GITHUB_TOKEN, so ask for the publish run directly.
dispatchPublish() {
    jq -n --arg ref "$2" '{ref: $ref}' \
        | ghApi -X POST --data @- \
            "https://api.github.com/repos/$1/actions/workflows/${PUBLISH_WORKFLOW}/dispatches" >/dev/null
}
