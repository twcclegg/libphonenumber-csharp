#! /bin/bash
# Checks the .NET versions this repository targets against the ones Microsoft still supports, and
# files an issue when they have drifted apart - a new major version has shipped, or one this
# repository targets has passed its end of support. check_target_frameworks.yml runs this monthly.
#
# Monthly rather than annually even though .NET's cadence is annual (a new major version every
# November, an older one retired at the same time -
# https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core): support dates do
# not all land in November - .NET 9's ended in May - and a check that finds nothing costs a minute of
# a runner. The run is silent unless something actually moved.
#
# Which versions count. Microsoft's releases-index.json is the source of truth: a channel counts as
# supported while its support-phase is "active" or "maintenance" (maintenance is the last stretch
# before eol, and still supported), and does not while it is "preview", "go-live" (a release
# candidate, so not released) or "eol". The set worth targeting keeps every TFM already targeted that
# is still supported and adds every supported channel newer than the newest one already targeted. So
# a version that has gone out of support is reported as a drop, a version that has just been released
# as an addition, and a version the maintainers deliberately declined to target is not proposed
# again. netstandard2.0 is not a .NET version and never enters into this.
#
# It reports rather than rewrites. Retargeting touches the project files, the SDK pin, every
# workflow's dotnet-version, the benchmarks' RuntimeMoniker and the TFM lists quoted in the docs, and
# it changes what the published packages ship - so the issue says what moved and leaves the change,
# and its timing, to a human. AGENTS.md ("Target frameworks") lists everything that moves with it.
#
# Exit on any error, treat unset variables as errors, and fail a pipeline if any stage fails. The
# pipefail matters: the release index is read with `curl | jq`, and without it a failed download
# would feed an empty document to the parser, leaving an empty supported set that reads as every
# target framework having gone out of support. Fail closed instead.
set -euo pipefail

# Exit codes
readonly EXIT_USAGE=2
readonly EXIT_MISSING_PREREQUISITE=3

usage() {
    cat <<'EOF'
Usage: check-target-frameworks.sh [options] [github-token]

The GitHub token may be supplied as the positional argument or via the
GITHUB_TOKEN environment variable. It is required unless --dry-run is used.

Options:
  --dry-run   Do the lookups and print the issue that would be filed, then
              stop. Nothing is created on GitHub, and no token is needed.
  -h, --help  Show this help and exit.

Environment variables:
  GITHUB_TOKEN          GitHub token used for the api calls. Needs issues: write.
  GITHUB_REPOSITORY     owner/name of the repository to file the issue in. Set
                        automatically by GitHub Actions; falls back to the origin
                        remote, so a fork files against itself.
  RELEASES_INDEX_URL    Microsoft's .NET release index.
  RELEASES_INDEX_FALLBACK_URL
                        Read instead when the primary url cannot be reached; set
                        empty to disable the fallback.
  SUPPORTED_CHANNELS    Space or comma separated channel versions ("10.0 11.0") to
                        use instead of the release index. For dry runs - it replays
                        a given year, or a support decision Microsoft has not
                        published yet.

Examples:
  # is anything out of date right now?
  check-target-frameworks.sh --dry-run

  # what will the check say once net11.0 ships and net8.0 goes out of support?
  SUPPORTED_CHANNELS="10.0 11.0" check-target-frameworks.sh --dry-run
EOF
}

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=./github-release-helpers.sh
source "${SCRIPT_DIR}/github-release-helpers.sh"

GITHUB_TOKEN="${GITHUB_TOKEN:-}"
DRY_RUN="${DRY_RUN:-false}"
RELEASES_INDEX_URL="${RELEASES_INDEX_URL:-https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json}"
# The same document, served from the repository Microsoft generates it in, so a url that moves does
# not silently stop the check.
RELEASES_INDEX_FALLBACK_URL="${RELEASES_INDEX_FALLBACK_URL-https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json}"
SUPPORTED_CHANNELS="${SUPPORTED_CHANNELS:-}"

while [ $# -gt 0 ]; do
    case "$1" in
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
    log "dry run: nothing will be filed"
elif [ -z "${GITHUB_TOKEN}" ]; then
    usage >&2
    fail ${EXIT_USAGE} "GitHub token required"
fi

for tool in curl jq git; do
    if ! command -v "${tool}" &>/dev/null; then
        fail ${EXIT_MISSING_PREREQUISITE} "${tool} required"
    fi
done

if [ ! -d csharp ]; then
    fail 1 "must be run from the root of the repository (no csharp/ here)"
fi

WORK_DIR=$(mktemp -d)
cleanup() {
    rm -rf "${WORK_DIR}"
}
trap cleanup EXIT

# Oldest first, deduplicated. Sorting on the number rather than the string keeps net9.0 before
# net10.0, which a lexical sort would not.
sortTfms() {
    sed 's|^net||' | sort -u -V | sed 's|^|net|'
}

# Newline separated on stdin to <delimiter> separated on stdout, with no trailing delimiter.
joinLines() {
    tr '\n' "$1" | sed "s|$1*\$||"
}

# One TFM per line in, "net10.0, net11.0" out.
commaList() {
    printf '%s\n' "$1" | joinLines ',' | sed 's|,|, |g'
}

# The same, with each entry in backticks for markdown - the sed wraps the match, it is not an
# unexpanded expression.
# shellcheck disable=SC2016
backtickList() {
    commaList "$(printf '%s\n' "$1" | sed 's|.*|`&`|')"
}

# -----------------------------------------------------------------------------------------------
# What the repository targets today
# -----------------------------------------------------------------------------------------------

CURRENT_TFMS=$(
    find csharp -name '*.csproj' -not -path '*/obj/*' -not -path '*/bin/*' \
        -exec sed -n 's|.*<TargetFrameworks\{0,1\}>\([^<]*\)</TargetFrameworks\{0,1\}>.*|\1|p' {} + \
        | tr -d '\r' \
        | tr ';' '\n' \
        | grep -E '^net[0-9]+\.[0-9]+$' \
        | sortTfms
)

if [ -z "${CURRENT_TFMS}" ]; then
    fail 1 "no .NET target frameworks found in any project under csharp/"
fi

CURRENT_LATEST_TFM=$(printf '%s\n' "${CURRENT_TFMS}" | tail -n 1)

log "currently targeting: $(printf '%s\n' "${CURRENT_TFMS}" | joinLines ' ')"

# -----------------------------------------------------------------------------------------------
# What Microsoft still supports
# -----------------------------------------------------------------------------------------------

fetchReleasesIndex() {
    local url
    for url in "${RELEASES_INDEX_URL}" "${RELEASES_INDEX_FALLBACK_URL}"; do
        [ -n "${url}" ] || continue
        if curl --fail --silent --show-error --location --retry 3 --retry-delay 5 \
            "${url}" -o "${WORK_DIR}/releases-index.json"; then
            log "read the .NET release index from ${url}"
            return 0
        fi
        warn "could not read the .NET release index from ${url}"
    done
    return 1
}

# channel-version, release-type, support-phase and eol-date, one channel per line. The length check
# is a fail-closed guard: an index that arrives truncated or reshaped has to stop the run rather than
# report every target framework as unsupported.
readChannels() {
    jq -er '
        (.["releases-index"] // error("release index has no releases-index array"))
        | if length < 5 then
              error("release index lists only \(length) channels, refusing to act on it")
          else . end
        | .[]
        | [.["channel-version"], .["release-type"], .["support-phase"], (.["eol-date"] // "")]
        | @tsv
    ' "$1"
}

CHANNELS_FILE="${WORK_DIR}/channels.tsv"
: >"${CHANNELS_FILE}"

if [ -n "${SUPPORTED_CHANNELS}" ]; then
    SUPPORTED_CHANNELS=$(printf '%s' "${SUPPORTED_CHANNELS}" | tr ',' '\n' | tr ' ' '\n' | grep -v '^$')
    log "supported channels overridden to: $(printf '%s\n' "${SUPPORTED_CHANNELS}" | joinLines ' ')"
else
    fetchReleasesIndex || fail 1 "could not read the .NET release index"
    readChannels "${WORK_DIR}/releases-index.json" >"${CHANNELS_FILE}"
    SUPPORTED_CHANNELS=$(awk -F'\t' '$3 == "active" || $3 == "maintenance" { print $1 }' "${CHANNELS_FILE}")

    if [ -z "${SUPPORTED_CHANNELS}" ]; then
        fail 1 "the .NET release index lists no supported channel"
    fi
fi

while read -r channel; do
    if [[ ! "${channel}" =~ ^[0-9]+\.[0-9]+$ ]]; then
        fail 1 "unexpected .NET channel version: ${channel}"
    fi
done <<<"${SUPPORTED_CHANNELS}"

SUPPORTED_TFMS=$(printf '%s\n' "${SUPPORTED_CHANNELS}" | sed 's|^|net|' | sortTfms)

# How a channel reads in the issue, e.g. "lts, supported until 2028-11-14".
channelDescription() {
    local line type phase eol
    line=$(awk -F'\t' -v channel="$1" '$1 == channel { print }' "${CHANNELS_FILE}")

    if [ -z "${line}" ]; then
        return 0
    fi

    type=$(printf '%s' "${line}" | cut -f2)
    phase=$(printf '%s' "${line}" | cut -f3)
    eol=$(printf '%s' "${line}" | cut -f4)

    if [ "${phase}" = "eol" ]; then
        printf '%s, out of support%s' "${type}" "${eol:+ since ${eol}}"
    else
        printf '%s, %s%s' "${type}" "${phase}" "${eol:+ until ${eol}}"
    fi
}

# -----------------------------------------------------------------------------------------------
# Has anything moved?
# -----------------------------------------------------------------------------------------------

isSupportedTfm() {
    printf '%s\n' "${SUPPORTED_TFMS}" | grep -qx -- "$1"
}

# True when $1 is a later .NET version than $2.
isNewerTfm() {
    [ "$1" != "$2" ] && [ "$(printf '%s\n%s\n' "$1" "$2" | sortTfms | tail -n 1)" = "$1" ]
}

DROPPED_TFMS=$(
    while read -r tfm; do
        if ! isSupportedTfm "${tfm}"; then
            printf '%s\n' "${tfm}"
        fi
    done <<<"${CURRENT_TFMS}"
)

ADDED_TFMS=$(
    while read -r tfm; do
        if isNewerTfm "${tfm}" "${CURRENT_LATEST_TFM}"; then
            printf '%s\n' "${tfm}"
        fi
    done <<<"${SUPPORTED_TFMS}"
)

log "supported .NET versions: $(printf '%s\n' "${SUPPORTED_TFMS}" | joinLines ' ')"

if [ -z "${ADDED_TFMS}" ] && [ -z "${DROPPED_TFMS}" ]; then
    log "every targeted .NET version is still supported and nothing newer has been released, nothing to do"
    exit 0
fi

TARGET_TFMS=$(
    {
        while read -r tfm; do
            if isSupportedTfm "${tfm}"; then
                printf '%s\n' "${tfm}"
            fi
        done <<<"${CURRENT_TFMS}"
        printf '%s\n' "${ADDED_TFMS}"
    } | grep -v '^$' | sortTfms
)

if [ -z "${TARGET_TFMS}" ]; then
    fail 1 "computed an empty target framework set from $(printf '%s\n' "${SUPPORTED_TFMS}" | joinLines ' ')"
fi

if [ -n "${ADDED_TFMS}" ] && [ -n "${DROPPED_TFMS}" ]; then
    SUMMARY="add $(commaList "${ADDED_TFMS}"), drop $(commaList "${DROPPED_TFMS}")"
elif [ -n "${ADDED_TFMS}" ]; then
    SUMMARY="add $(commaList "${ADDED_TFMS}")"
else
    SUMMARY="drop $(commaList "${DROPPED_TFMS}")"
fi

log "target frameworks need updating: ${SUMMARY}"

# -----------------------------------------------------------------------------------------------
# File the issue
# -----------------------------------------------------------------------------------------------

GITHUB_REPOSITORY=$(resolveRepository) \
    || fail 1 "could not determine the target repository, set GITHUB_REPOSITORY to owner/name"

if [[ ! "${GITHUB_REPOSITORY}" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]]; then
    fail 1 "unexpected target repository: ${GITHUB_REPOSITORY}"
fi

TITLE="Retarget at the supported .NET versions: ${SUMMARY}"
# Carries the set it was filed for, so a monthly re-run recognizes its own open issue and a later
# change (another version retiring before the first issue is dealt with) is filed as news rather
# than swallowed as a duplicate.
MARKER="<!-- target-frameworks: $(printf '%s\n' "${TARGET_TFMS}" | joinLines ' ') -->"

# The issue body. Single quotes throughout, with every value passed as a printf argument.
# shellcheck disable=SC2016
{
    printf 'The .NET versions this repository targets no longer match the ones Microsoft supports.\n\n'

    while read -r tfm; do
        [ -n "${tfm}" ] || continue
        description=$(channelDescription "${tfm#net}")
        printf -- '- Add `%s`%s\n' "${tfm}" "${description:+ — ${description}}"
    done <<<"${ADDED_TFMS}"

    while read -r tfm; do
        [ -n "${tfm}" ] || continue
        description=$(channelDescription "${tfm#net}")
        printf -- '- Drop `%s`%s\n' "${tfm}" "${description:+ — ${description}}"
    done <<<"${DROPPED_TFMS}"

    printf '\nSo the set to target is %s, which multi-target projects take in full while the single-target ones (tools, demo, benchmarks) take `%s`.\n' \
        "$(backtickList "${TARGET_TFMS}")" "$(printf '%s\n' "${TARGET_TFMS}" | tail -n 1)"

    printf '\nAGENTS.md ("Target frameworks", under CI and release) lists everything that has to move together: the `TargetFramework(s)` of every project under `csharp/`, the SDK pin in `global.json`, `dotnet-version` in every workflow, the newest TFM wherever a workflow or project file names it literally, the `RuntimeMoniker` on the benchmark jobs, and the target framework lists and commands quoted in the docs.\n'

    printf '\nFiled by [check_target_frameworks.yml](.github/workflows/check_target_frameworks.yml), which checks monthly.\n'
    printf '\n%s\n' "${MARKER}"
} >"${WORK_DIR}/issue-body.md"

if isTrue "${DRY_RUN}"; then
    log ""
    log "would file in ${GITHUB_REPOSITORY}:"
    log ""
    log "${TITLE}"
    log ""
    cat "${WORK_DIR}/issue-body.md"
    exit 0
fi

# One open issue per set. A re-run next month recomputes the same set and finds this, rather than
# filing again every month until someone acts on it. Only the first page is read: with this many open
# issues a duplicate is the lesser problem.
EXISTING_ISSUE=$(ghApi "https://api.github.com/repos/${GITHUB_REPOSITORY}/issues?state=open&per_page=100" \
    | jq -er --arg marker "${MARKER}" '
        [.[] | select(.pull_request == null) | select((.body // "") | contains($marker))]
        | .[0].number // empty
    ')

if [ -n "${EXISTING_ISSUE}" ]; then
    log "issue #${EXISTING_ISSUE} is already open for this set, nothing to do"
    exit 0
fi

ISSUE_RESPONSE=$(jq -n --arg title "${TITLE}" --rawfile body "${WORK_DIR}/issue-body.md" \
    '{title: $title, body: $body}' \
    | ghApi -X POST --data @- "https://api.github.com/repos/${GITHUB_REPOSITORY}/issues")

ISSUE_NUMBER=$(jq -er '.number' <<<"${ISSUE_RESPONSE}")
log "filed issue #${ISSUE_NUMBER}: ${TITLE}"

if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
    {
        printf '## [%s](%s)\n\n' "${TITLE}" "$(jq -er '.html_url' <<<"${ISSUE_RESPONSE}")"
        cat "${WORK_DIR}/issue-body.md"
    } >>"${GITHUB_STEP_SUMMARY}"
fi
