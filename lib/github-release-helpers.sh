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

# Turns on github's auto-merge, so the PR lands by itself once its required checks pass. Only
# accepted while the PR is blocked from merging - on one that could be merged right now github
# answers "Pull request is in clean status" - so call it straight after a push, while the checks
# it just triggered are still pending. Echoes github's error message, or nothing on success: a
# graphql error is an http 200 with an "errors" array, so --fail cannot catch it, and `jq -e`
# cannot be the test either, since its exit status does not separate "no errors" from "that body
# did not parse".
armAutoMerge() {
    local response
    if ! response=$(jq -n --arg id "$1" \
        '{query: "mutation($id: ID!) { enablePullRequestAutoMerge(input: {pullRequestId: $id, mergeMethod: MERGE}) { pullRequest { number } } }", variables: {id: $id}}' \
        | ghApi -X POST --data @- "https://api.github.com/graphql"); then
        echo "could not reach the github api"
        return 0
    fi

    # Success is the mutation actually reporting back a pull request, not merely the absence of an
    # "errors" key: {"data":null} and an errors entry whose message is empty both used to read as
    # "enabled auto-merge" on a run where nothing had been enabled.
    jq -er '
        if ((.errors // []) | length) > 0 then (if ((.errors[0].message // "") | length) > 0 then .errors[0].message else "unknown error" end)
        elif (.data.enablePullRequestAutoMerge.pullRequest.number | type) == "number" then ""
        else "github accepted the request without reporting auto-merge as enabled"
        end' <<<"${response}" 2>/dev/null \
        || echo "could not parse github's response"
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
