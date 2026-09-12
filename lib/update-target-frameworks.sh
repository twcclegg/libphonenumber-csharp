#! /bin/bash
# Retargets this repository at the .NET versions Microsoft still supports and opens a PR with the
# result. A new major version ships every November and an older one goes out of support at the same
# time (https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core), so the set
# of target frameworks worth shipping changes once a year in an entirely mechanical way:
# update_target_frameworks.yml runs this every December 1, two weeks after that release, and it only
# opens a PR when the set actually moved.
#
# Which versions count. Microsoft's releases-index.json is the source of truth: a channel counts as
# active while its support-phase is "active" or "maintenance" (maintenance is the last stretch before
# eol, and still supported), and does not while it is "preview", "go-live" (release candidate) or
# "eol". From those, the new set keeps every TFM already targeted that is still active and adds every
# active channel newer than the newest TFM already targeted. So a version out of support is dropped,
# a version that just shipped is picked up, and a version the maintainers deliberately declined to
# target does not come back on its own. netstandard2.0 is not a .NET version and is never touched: it
# is what keeps consumers on a retired runtime resolving an asset once their own TFM's goes away, and
# it is why dropping a TFM here is not a breaking change for them.
#
# Everything the retarget rewrites, and why it all has to move in the same commit:
#   - TargetFramework(s) in every csharp/**/*.csproj - multi-TFM projects get the whole set,
#     single-TFM projects (the tools, the demo, the benchmarks) get the newest one
#   - the SDK pin in global.json, without which the new TFM cannot be built at all
#   - dotnet-version in every workflow, for the same reason
#   - the newest TFM wherever a workflow or project file names it literally: `--framework`,
#     `-p:TargetFrameworks=`, the reproducibility check's TFM list, the MetadataBuilder output path
#   - RuntimeMoniker.NetX_0 on the BenchmarkDotNet jobs, which pins the runtime they measure
#   - the TFM lists and commands quoted in README.md / AGENTS.md / CONTRIBUTING.md
# Deliberately not rewritten: `#if NETx_0_OR_GREATER` guards, which name a floor ("modern .NET")
# rather than a target and stay correct once that TFM is gone, and prose mentioning a dropped TFM,
# which is usually a historical measurement - those are reported for a human to judge instead.
#
# Building and testing the result is left to the PR's own required status checks rather than
# duplicated here, exactly like github-actions-metadata-update.sh. Unlike that PR this one is not
# auto-merged: it changes what the published packages ship, which is worth a maintainer's look.
#
# Exit on any error, treat unset variables as errors, and fail a pipeline if any stage fails. The
# pipefail matters: the release index is read with `curl | jq`, and without it a failed download
# would feed an empty document to the parser and the script would carry on with an empty version
# set - deleting every target framework in the repository. Fail closed instead.
set -euo pipefail

# Exit codes
readonly EXIT_USAGE=2
readonly EXIT_MISSING_PREREQUISITE=3
readonly EXIT_NEEDS_ATTENTION=4

usage() {
    cat <<'EOF'
Usage: update-target-frameworks.sh [options] [github-token]

The GitHub token may be supplied as the positional argument or via the
GITHUB_TOKEN environment variable. It is required unless --dry-run or
--local is used.

Options:
  --dry-run   Apply the rewrite to a throwaway worktree of HEAD and print the
              diff it produces, then stop. This checkout is left alone, and
              nothing is committed, pushed or opened as a PR.
  --local     Rewrite the files in this checkout and stop there, leaving the
              changes in the working tree to inspect with `git diff`. Nothing
              is committed, pushed or opened as a PR.
  -h, --help  Show this help and exit.

Environment variables:
  GITHUB_TOKEN          GitHub token used for the api calls and to push the branch.
  GITHUB_REPOSITORY     owner/name of the repository to open the PR against. Set
                        automatically by GitHub Actions; falls back to the origin
                        remote, so a fork retargets itself.
  BASE_BRANCH           Branch the PR targets, and the branch a real run must be
                        on (default main).
  RELEASES_INDEX_URL    Microsoft's .NET release index.
  RELEASES_INDEX_FALLBACK_URL
                        Read instead when the primary url cannot be reached; set
                        empty to disable the fallback.
  ACTIVE_CHANNELS       Space or comma separated channel versions ("10.0 11.0") to
                        use instead of the release index. Mainly for dry runs - it
                        replays a given year, or a support decision Microsoft has
                        not published yet.
  SDK_FEATURE_BAND      Feature band written into global.json's SDK pin for a new
                        major version (default 100, the band every GA release has).

Examples:
  # what would the December run do right now?
  update-target-frameworks.sh --dry-run

  # replay the December 2026 retarget
  ACTIVE_CHANNELS="10.0 11.0" update-target-frameworks.sh --dry-run
EOF
}

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=./github-release-helpers.sh
source "${SCRIPT_DIR}/github-release-helpers.sh"

GITHUB_TOKEN="${GITHUB_TOKEN:-}"
DRY_RUN="${DRY_RUN:-false}"
LOCAL_ONLY="${LOCAL_ONLY:-false}"
BASE_BRANCH="${BASE_BRANCH:-main}"
RELEASES_INDEX_URL="${RELEASES_INDEX_URL:-https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json}"
# The same document, served from the repository Microsoft generates it in. This job runs once a year,
# so a url that moved in the meantime would otherwise mean a silently skipped retarget.
RELEASES_INDEX_FALLBACK_URL="${RELEASES_INDEX_FALLBACK_URL-https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json}"
ACTIVE_CHANNELS="${ACTIVE_CHANNELS:-}"
SDK_FEATURE_BAND="${SDK_FEATURE_BAND:-100}"

while [ $# -gt 0 ]; do
    case "$1" in
        --dry-run) DRY_RUN=true ;;
        --local) LOCAL_ONLY=true ;;
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
    log "dry run: the rewrite runs against a copy of HEAD, this checkout is left alone"
elif isTrue "${LOCAL_ONLY}"; then
    log "local run: files will be rewritten, nothing will be committed, pushed or opened as a PR"
elif [ -z "${GITHUB_TOKEN}" ]; then
    usage >&2
    fail ${EXIT_USAGE} "GitHub token required"
fi

for tool in curl jq git; do
    if ! command -v "${tool}" &>/dev/null; then
        fail ${EXIT_MISSING_PREREQUISITE} "${tool} required"
    fi
done

if [ ! -f global.json ] || [ ! -d csharp ]; then
    fail 1 "must be run from the root of the repository (no global.json and csharp/ here)"
fi

REPOSITORY_ROOT=$(pwd)
WORK_DIR=$(mktemp -d)
DRY_RUN_WORKTREE=""

cleanup() {
    if [ -n "${DRY_RUN_WORKTREE}" ]; then
        cd "${REPOSITORY_ROOT}"
        git worktree remove --force "${DRY_RUN_WORKTREE}" 2>/dev/null || true
        git worktree prune 2>/dev/null || true
    fi
    rm -rf "${WORK_DIR}"
}
trap cleanup EXIT

# A dry run applies the real rewrite to a throwaway worktree of HEAD and shows the resulting diff,
# rather than predicting the diff from the substitutions it would make. Same code path, actual
# output, and this checkout is never written to - which also means a dry run works from a feature
# branch, or with uncommitted work in progress sitting next to it.
if isTrue "${DRY_RUN}"; then
    DRY_RUN_WORKTREE="${WORK_DIR}/head"
    git worktree add --detach --quiet "${DRY_RUN_WORKTREE}" HEAD
    cd "${DRY_RUN_WORKTREE}"
    log "rewriting a copy of $(git rev-parse --short HEAD) in ${DRY_RUN_WORKTREE}"
fi

# -----------------------------------------------------------------------------------------------
# Reading and writing files
# -----------------------------------------------------------------------------------------------

# sed -i is not portable (BSD sed wants an argument to it), and these files are not all in the same
# encoding or line ending - PhoneNumbers.csproj is utf-8 with a BOM and CRLF. Writing back through
# `cat` keeps the file's mode and inode, and every expression used here is a literal substitution
# that leaves the bytes it does not match alone, BOM and CRs included.
sedInPlace() {
    local expression="$1" file="$2"
    sed "${expression}" "${file}" >"${WORK_DIR}/sed-output"
    cat "${WORK_DIR}/sed-output" >"${file}"
}

# Escape a string for use as a sed BRE pattern, so a TFM's `.` matches only a literal dot.
sedPattern() {
    printf '%s' "$1" | sed 's/[[\.*^$\/&|]/\\&/g'
}

# Escape a string for use as a sed replacement.
sedReplacement() {
    printf '%s' "$1" | sed 's/[\/&|\\]/\\&/g'
}

# substitute <literal-from> <literal-to> <file>... - replaces every occurrence.
substitute() {
    local from="$1" to="$2" file expression
    shift 2

    expression="s|$(sedPattern "${from}")|$(sedReplacement "${to}")|g"

    for file in "$@"; do
        if [ -f "${file}" ] && grep -qF -- "${from}" "${file}"; then
            sedInPlace "${expression}" "${file}"
        fi
    done
}

# substituteExpression <sed-expression> <file>... - for the rewrites that need a pattern rather
# than a literal. POSIX BRE only, so it works with BSD sed too.
substituteExpression() {
    local expression="$1" file
    shift

    for file in "$@"; do
        if [ -f "${file}" ]; then
            sedInPlace "${expression}" "${file}"
        fi
    done
}

# The value of a project's TargetFramework or TargetFrameworks element, with any CR stripped.
projectTfmValue() {
    sed -n 's|.*<TargetFrameworks\{0,1\}>\([^<]*\)</TargetFrameworks\{0,1\}>.*|\1|p' "$1" \
        | head -n 1 \
        | tr -d '\r'
}

# The .NET (so neither netstandard nor .NET Framework) entries of a semicolon separated TFM list.
modernTfmsOf() {
    printf '%s' "$1" | tr ';' '\n' | grep -E '^net[0-9]+\.[0-9]+$' || true
}

# The other entries, in the order they already appear - netstandard2.0 keeps its place at the front.
otherTfmsOf() {
    printf '%s' "$1" | tr ';' '\n' | grep -Ev '^net[0-9]+\.[0-9]+$' | grep -v '^$' || true
}

# True for a project that names one target framework, as the tools, the demo and the benchmarks do.
isSingleTfmProject() {
    [ "$(modernTfmsOf "$(projectTfmValue "$1")" | wc -l)" -le 1 ] && ! grep -q '<TargetFrameworks>' "$1"
}

# Oldest first, deduplicated. Sorting on the number rather than the string keeps net9.0 before
# net10.0, which a lexical sort would not.
sortTfms() {
    sed 's|^net||' | sort -u -V | sed 's|^|net|'
}

# Newline separated on stdin to <delimiter> separated on stdout, with no trailing delimiter.
joinLines() {
    tr '\n' "$1" | sed "s|$1*\$||"
}

# `a`, `b` and `c` - the shape the readme quotes the target framework set in.
proseList() {
    local items item result="" count=0 total
    read -r -a items <<<"$*"
    total=${#items[@]}

    for item in "${items[@]}"; do
        count=$((count + 1))
        if [ "${count}" = 1 ]; then
            result="\`${item}\`"
        elif [ "${count}" = "${total}" ]; then
            result="${result} and \`${item}\`"
        else
            result="${result}, \`${item}\`"
        fi
    done

    printf '%s' "${result}"
}

# -----------------------------------------------------------------------------------------------
# What the repository targets today
# -----------------------------------------------------------------------------------------------

PROJECT_FILES=$(find csharp -name '*.csproj' -not -path '*/obj/*' -not -path '*/bin/*' | sort)
WORKFLOW_FILES=$(find .github/workflows -name '*.yml' | sort)
DOC_FILES=$(find . -name '*.md' -not -path './CHANGELOG.md' -not -path './.git/*' -not -path '*/obj/*' -not -path '*/bin/*' | sort)
SOURCE_FILES=$(find csharp -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' | sort)

if [ -z "${PROJECT_FILES}" ]; then
    fail 1 "no project files under csharp/"
fi

CURRENT_TFMS=$(
    while read -r project; do
        modernTfmsOf "$(projectTfmValue "${project}")"
    done <<<"${PROJECT_FILES}" | sortTfms
)

if [ -z "${CURRENT_TFMS}" ]; then
    fail 1 "no .NET target frameworks found in any project under csharp/"
fi

CURRENT_LATEST_TFM=$(printf '%s\n' "${CURRENT_TFMS}" | tail -n 1)
CURRENT_LATEST_MAJOR=${CURRENT_LATEST_TFM#net}
CURRENT_LATEST_MAJOR=${CURRENT_LATEST_MAJOR%%.*}

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
# is a fail-closed guard: this decides which target frameworks the packages ship, so an index that
# arrives truncated or reshaped has to stop the run rather than quietly shrink the set.
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

if [ -n "${ACTIVE_CHANNELS}" ]; then
    ACTIVE_CHANNELS=$(printf '%s' "${ACTIVE_CHANNELS}" | tr ',' '\n' | tr ' ' '\n' | grep -v '^$')
    log "active channels overridden to: $(printf '%s\n' "${ACTIVE_CHANNELS}" | joinLines ' ')"
else
    fetchReleasesIndex || fail 1 "could not read the .NET release index"
    readChannels "${WORK_DIR}/releases-index.json" >"${CHANNELS_FILE}"
    # "maintenance" is still supported - it is the final stretch before eol. "go-live" is a release
    # candidate and so is excluded: shipping a target framework for an unreleased runtime is not
    # this job, and the SDK band that would have to be pinned with it is not released either.
    ACTIVE_CHANNELS=$(awk -F'\t' '$3 == "active" || $3 == "maintenance" { print $1 }' "${CHANNELS_FILE}")

    if [ -z "${ACTIVE_CHANNELS}" ]; then
        fail 1 "the .NET release index lists no active channel"
    fi
fi

while read -r channel; do
    if [[ ! "${channel}" =~ ^[0-9]+\.[0-9]+$ ]]; then
        fail 1 "unexpected .NET channel version: ${channel}"
    fi
done <<<"${ACTIVE_CHANNELS}"

ACTIVE_TFMS=$(printf '%s\n' "${ACTIVE_CHANNELS}" | sed 's|^|net|' | sortTfms)

# How a channel reads in the PR body, e.g. "lts, supported until 2028-11-14".
channelDescription() {
    local channel="$1" line type phase eol
    line=$(awk -F'\t' -v channel="${channel}" '$1 == channel { print }' "${CHANNELS_FILE}")

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
# The new set: keep what is still active, add anything newer that has shipped
# -----------------------------------------------------------------------------------------------

isActiveTfm() {
    printf '%s\n' "${ACTIVE_TFMS}" | grep -qx -- "$1"
}

isTargetedTfm() {
    printf '%s\n' "${CURRENT_TFMS}" | grep -qx -- "$1"
}

# True when $1 is a later .NET version than $2.
isNewerTfm() {
    [ "$1" != "$2" ] && [ "$(printf '%s\n%s\n' "$1" "$2" | sortTfms | tail -n 1)" = "$1" ]
}

TARGET_TFMS=$(
    {
        while read -r tfm; do
            if isActiveTfm "${tfm}"; then
                printf '%s\n' "${tfm}"
            fi
        done <<<"${CURRENT_TFMS}"

        while read -r tfm; do
            if isNewerTfm "${tfm}" "${CURRENT_LATEST_TFM}"; then
                printf '%s\n' "${tfm}"
            fi
        done <<<"${ACTIVE_TFMS}"
    } | sortTfms
)

if [ -z "${TARGET_TFMS}" ]; then
    fail ${EXIT_NEEDS_ATTENTION} \
        "every targeted framework is out of support and no newer one has shipped - retarget by hand"
fi

DROPPED_TFMS=$(
    while read -r tfm; do
        if ! isActiveTfm "${tfm}"; then
            printf '%s\n' "${tfm}"
        fi
    done <<<"${CURRENT_TFMS}"
)

ADDED_TFMS=$(
    while read -r tfm; do
        if ! isTargetedTfm "${tfm}"; then
            printf '%s\n' "${tfm}"
        fi
    done <<<"${TARGET_TFMS}"
)

TARGET_LATEST_TFM=$(printf '%s\n' "${TARGET_TFMS}" | tail -n 1)
TARGET_LATEST_MAJOR=${TARGET_LATEST_TFM#net}
TARGET_LATEST_MAJOR=${TARGET_LATEST_MAJOR%%.*}

if isNewerTfm "${CURRENT_LATEST_TFM}" "${TARGET_LATEST_TFM}"; then
    fail ${EXIT_NEEDS_ATTENTION} \
        "would move the newest target framework backwards, ${CURRENT_LATEST_TFM} to ${TARGET_LATEST_TFM}"
fi

log "active .NET versions: $(printf '%s\n' "${ACTIVE_TFMS}" | joinLines ' ')"
log "new target framework set: $(printf '%s\n' "${TARGET_TFMS}" | joinLines ' ')"

if [ "${CURRENT_TFMS}" = "${TARGET_TFMS}" ]; then
    log "already targeting every active .NET version, nothing to do"
    exit 0
fi

if [ -n "${ADDED_TFMS}" ]; then
    log "adding: $(printf '%s\n' "${ADDED_TFMS}" | joinLines ' ')"
fi

if [ -n "${DROPPED_TFMS}" ]; then
    log "dropping: $(printf '%s\n' "${DROPPED_TFMS}" | joinLines ' ')"
fi

# -----------------------------------------------------------------------------------------------
# Repository state, and whether this PR already exists
# -----------------------------------------------------------------------------------------------

BRANCH="dotnet-tfm-update/$(printf '%s\n' "${TARGET_TFMS}" | joinLines '-')"

GITHUB_REPOSITORY=$(resolveRepository) \
    || fail 1 "could not determine the target repository, set GITHUB_REPOSITORY to owner/name"

if [[ ! "${GITHUB_REPOSITORY}" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]]; then
    fail 1 "unexpected target repository: ${GITHUB_REPOSITORY}"
fi

log "target repository is ${GITHUB_REPOSITORY}"

if ! isTrue "${DRY_RUN}" && ! isTrue "${LOCAL_ONLY}"; then
    # This PR is not auto-merged, so it can sit for a while - a re-run, or next year's schedule if a
    # maintainer never got to it, must not open a second copy of it.
    EXISTING_PR_COUNT=$(ghApi "https://api.github.com/repos/${GITHUB_REPOSITORY}/pulls?state=open&head=${GITHUB_REPOSITORY%%/*}:${BRANCH}" \
        | jq -er 'length')
    if [ "${EXISTING_PR_COUNT}" -gt 0 ]; then
        log "a PR for ${BRANCH} is already open, nothing to do"
        exit 0
    fi

    # The checkout this runs in is the one that gets committed and pushed, so it is the one that has
    # to be on a clean main. A dry run works in its own worktree of HEAD, so neither applies there.
    if [ "$(git branch --show-current)" != "${BASE_BRANCH}" ]; then
        fail 1 "must be on ${BASE_BRANCH} branch"
    fi

    if [ -n "$(git status --porcelain)" ]; then
        fail 1 "working directory is not clean"
    fi
fi

# -----------------------------------------------------------------------------------------------
# The rewrite
#
# Two passes, in this order, because they would otherwise collide: one over whole TFM *lists*
# (`netstandard2.0;net8.0;net10.0` in a project file, the same list space separated in the
# reproducibility check, spelled out in prose in the readme), then one over the newest TFM named on
# its own (`--framework net10.0`, a build output path, a step name). The newest TFM usually stays in
# the new set, so the second pass must never see a list: rewriting net10.0 inside
# `netstandard2.0;net10.0;net11.0` would turn it into `netstandard2.0;net11.0;net11.0`. Lists are
# therefore stashed behind unique placeholders by the first pass and written out at the end, once the
# second pass has run. A crash in between leaves placeholders in the tree; it is a throwaway
# checkout in CI, and `git checkout -- .` restores it anywhere else.
# -----------------------------------------------------------------------------------------------

ALL_FILES="${PROJECT_FILES}
${WORKFLOW_FILES}
${DOC_FILES}
${SOURCE_FILES}
global.json"

PLACEHOLDERS_FILE="${WORK_DIR}/placeholders.tsv"
: >"${PLACEHOLDERS_FILE}"
PLACEHOLDER_COUNT=0

# stashList <literal-from> <literal-to> - hides a TFM list behind a placeholder and records the value
# writeStashedLists() replaces it with once the single-TFM pass is done.
stashList() {
    local from="$1" to="$2" placeholder

    if [ "${from}" = "${to}" ]; then
        return 0
    fi

    PLACEHOLDER_COUNT=$((PLACEHOLDER_COUNT + 1))
    placeholder="@@tfm-list-${PLACEHOLDER_COUNT}@@"
    printf '%s\t%s\n' "${placeholder}" "${to}" >>"${PLACEHOLDERS_FILE}"
    # shellcheck disable=SC2086 # deliberate: TFM tokens and file paths, no globs or spaces in them
    substitute "${from}" "${placeholder}" ${ALL_FILES}
}

writeStashedLists() {
    while IFS=$'\t' read -r placeholder value; do
        # shellcheck disable=SC2086
        substitute "${placeholder}" "${value}" ${ALL_FILES}
    done <"${PLACEHOLDERS_FILE}"
}

# Single-TFM projects first: the tools, the demo, the benchmarks. Rewritten through the element
# itself rather than as a bare TFM, so one left behind on an older TFM than the rest of the
# repository still lands on the new newest one.
while read -r project; do
    value=$(projectTfmValue "${project}")

    if [ -z "${value}" ]; then
        warn "no TargetFramework(s) element in ${project}, skipping it"
        continue
    fi

    if isSingleTfmProject "${project}" && [ "${value}" != "${TARGET_LATEST_TFM}" ]; then
        substitute ">${value}</TargetFramework" ">${TARGET_LATEST_TFM}</TargetFramework" "${project}"
    fi
done <<<"${PROJECT_FILES}"

# Then the multi-TFM lists, which get the whole set with any netstandard2.0 entry keeping its place
# at the front. Each list is also what the docs quote and what the reproducibility check iterates, so
# it is rewritten everywhere it appears, in all three shapes. Two projects sharing a list produce the
# same pair, hence the dedupe.
TFM_LIST_PAIRS="${WORK_DIR}/tfm-list-pairs"
: >"${TFM_LIST_PAIRS}"

while read -r project; do
    value=$(projectTfmValue "${project}")

    if [ -z "${value}" ] || isSingleTfmProject "${project}"; then
        continue
    fi

    newValue=$(
        {
            otherTfmsOf "${value}"
            printf '%s\n' "${TARGET_TFMS}"
        } | joinLines ';'
    )

    printf '%s\t%s\n' "${value}" "${newValue}" >>"${TFM_LIST_PAIRS}"
done <<<"${PROJECT_FILES}"

while IFS=$'\t' read -r oldList newList; do
    [ -n "${oldList}" ] || continue
    stashList "${oldList}" "${newList}"
    stashList "$(printf '%s' "${oldList}" | tr ';' ' ')" "$(printf '%s' "${newList}" | tr ';' ' ')"
    stashList "$(proseList "$(printf '%s' "${oldList}" | tr ';' ' ')")" \
        "$(proseList "$(printf '%s' "${newList}" | tr ';' ' ')")"
done < <(sort -u "${TFM_LIST_PAIRS}")

if [ "${CURRENT_LATEST_TFM}" != "${TARGET_LATEST_TFM}" ]; then
    # The newest TFM wherever it is named on its own. Safe as a blanket substitution in these files
    # because a reference to the newest TFM is always a live one, unlike a reference to an older TFM
    # - which is as often a note about a past measurement as it is a target (see the file header).
    # C# sources are excluded for that reason: the only live TFM reference in them is the benchmark
    # RuntimeMoniker below, the rest are comments recording what was measured where.
    # shellcheck disable=SC2086
    substitute "${CURRENT_LATEST_TFM}" "${TARGET_LATEST_TFM}" \
        ${PROJECT_FILES} ${WORKFLOW_FILES} ${DOC_FILES}

    # setup-dotnet installs an SDK major version, not a TFM.
    # shellcheck disable=SC2086
    substitute "dotnet-version: ${CURRENT_LATEST_MAJOR}.x" "dotnet-version: ${TARGET_LATEST_MAJOR}.x" \
        ${WORKFLOW_FILES}

    # BenchmarkDotNet's jobs name the runtime they measure.
    # shellcheck disable=SC2086
    substitute "RuntimeMoniker.Net${CURRENT_LATEST_MAJOR}_0" "RuntimeMoniker.Net${TARGET_LATEST_MAJOR}_0" \
        ${SOURCE_FILES} ${DOC_FILES}

    # The same version in prose: the SDK to install, the runtime a benchmark needs. What may follow
    # it is constrained so this stays off ".NET 10.0" as a recorded benchmark table writes it - that
    # is a measurement, not a target - and off any longer number.
    # shellcheck disable=SC2086
    substituteExpression "s|\.NET ${CURRENT_LATEST_MAJOR}\([^.0-9]\)|.NET ${TARGET_LATEST_MAJOR}\1|g" ${DOC_FILES}
    # shellcheck disable=SC2086
    substituteExpression "s|\.NET ${CURRENT_LATEST_MAJOR}\$|.NET ${TARGET_LATEST_MAJOR}|" ${DOC_FILES}

    # global.json pins the SDK feature band with rollForward: latestFeature, so the pin has to move
    # to the new major version or the new TFM cannot be restored, let alone built.
    CURRENT_SDK_PIN=$(jq -er '.sdk.version' global.json)

    if [ "${CURRENT_SDK_PIN%%.*}" = "${CURRENT_LATEST_MAJOR}" ]; then
        NEW_SDK_PIN="${TARGET_LATEST_MAJOR}.0.${SDK_FEATURE_BAND}"
        substitute "\"${CURRENT_SDK_PIN}\"" "\"${NEW_SDK_PIN}\"" global.json
        # The docs quote that pin too, without the quotes around it.
        # shellcheck disable=SC2086
        substitute "${CURRENT_SDK_PIN}" "${NEW_SDK_PIN}" ${DOC_FILES}
    else
        warn "global.json pins sdk ${CURRENT_SDK_PIN}, which is not a ${CURRENT_LATEST_TFM} version - leaving it alone"
    fi
fi

writeStashedLists

# -----------------------------------------------------------------------------------------------
# Check the result
# -----------------------------------------------------------------------------------------------

# Scoped to the files the rewrite touches: this script names the placeholder prefix itself, and a
# tree-wide grep would only ever find that.
# shellcheck disable=SC2086
LEFTOVER_PLACEHOLDERS=$(grep -lF '@@tfm-list-' ${ALL_FILES} || true)
if [ -n "${LEFTOVER_PLACEHOLDERS}" ]; then
    fail 1 "placeholders left behind in: $(printf '%s\n' "${LEFTOVER_PLACEHOLDERS}" | joinLines ' ')"
fi

# Every project has to have landed on the new set - a multi-TFM project on all of it, a single-TFM
# project on the newest. A project this script did not know how to rewrite must not go out
# half-retargeted.
while read -r project; do
    value=$(projectTfmValue "${project}")
    [ -n "${value}" ] || continue

    if isSingleTfmProject "${project}"; then
        expected="${TARGET_LATEST_TFM}"
    else
        expected="${TARGET_TFMS}"
    fi

    actual=$(modernTfmsOf "${value}" | sortTfms)

    if [ "${actual}" != "${expected}" ]; then
        fail 1 "${project} targets $(printf '%s\n' "${actual}" | joinLines ' ') after the rewrite, expected $(printf '%s\n' "${expected}" | joinLines ' ')"
    fi
done <<<"${PROJECT_FILES}"

if [ -z "$(git status --porcelain)" ]; then
    log "the target framework set moved but no file needed rewriting, nothing to do"
    exit 0
fi

# Anything still naming a dropped TFM. Usually prose that should stay as it is - a measurement taken
# on that runtime - and occasionally something this script does not know how to rewrite; either way
# it is a human's call, so report it rather than guess. `#if NETx_0_OR_GREATER` guards are spelled
# differently and so do not show up here, which is right: they stay correct once the TFM is gone.
LEFTOVERS_FILE="${WORK_DIR}/leftovers"
: >"${LEFTOVERS_FILE}"

if [ -n "${DROPPED_TFMS}" ]; then
    while read -r tfm; do
        git grep -n --fixed-strings "${tfm}" -- . ':!CHANGELOG.md' >>"${LEFTOVERS_FILE}" || true
    done <<<"${DROPPED_TFMS}"
fi

LEFTOVERS=$(sort -u "${LEFTOVERS_FILE}" | head -n 25)

log ""
log "rewrote:"
git --no-pager diff --stat | sed 's|^|  |'

if [ -n "${LEFTOVERS}" ]; then
    log ""
    log "still mentioning a dropped target framework:"
    printf '%s\n' "${LEFTOVERS}" | sed 's|^|  |'
fi

if isTrue "${DRY_RUN}"; then
    log ""
    git --no-pager diff
    log ""
    log "dry run complete, a real run would commit that on ${BRANCH}, push it, and open a PR into ${BASE_BRANCH}"
    exit 0
fi

if isTrue "${LOCAL_ONLY}"; then
    log ""
    log "local run complete, the changes are in the working tree"
    exit 0
fi

# -----------------------------------------------------------------------------------------------
# Commit, push, PR
# -----------------------------------------------------------------------------------------------

commaList() {
    printf '%s\n' "$1" | joinLines ',' | sed 's|,|, |g'
}

if [ -n "${ADDED_TFMS}" ] && [ -n "${DROPPED_TFMS}" ]; then
    SUMMARY="add $(commaList "${ADDED_TFMS}"), drop $(commaList "${DROPPED_TFMS}")"
elif [ -n "${ADDED_TFMS}" ]; then
    SUMMARY="add $(commaList "${ADDED_TFMS}")"
else
    SUMMARY="drop $(commaList "${DROPPED_TFMS}")"
fi

TITLE="build: ${SUMMARY}"

git checkout -b "${BRANCH}"
git add -A
git -c user.email='<>' -c user.name='libphonenumber-csharp-bot' commit -m "${TITLE}"

# Force is safe: this branch name is derived from the target framework set, so it belongs to this
# automation alone, and a stale copy from an earlier attempt (the check above only rules out an
# *open* PR) should not block a fresh one.
git push --force origin "HEAD:refs/heads/${BRANCH}"

# The PR body, which quotes TFMs and code in backticks - single quotes throughout, with every value
# passed as a printf argument.
# shellcheck disable=SC2016
{
    printf 'The set of .NET versions Microsoft supports moved, so this retargets the repository at it: %s.\n\n' "${SUMMARY}"

    while read -r tfm; do
        [ -n "${tfm}" ] || continue
        description=$(channelDescription "${tfm#net}")
        printf -- '- Added `%s`%s\n' "${tfm}" "${description:+ — ${description}}"
    done <<<"${ADDED_TFMS}"

    while read -r tfm; do
        [ -n "${tfm}" ] || continue
        description=$(channelDescription "${tfm#net}")
        printf -- '- Dropped `%s`%s\n' "${tfm}" "${description:+ — ${description}}"
    done <<<"${DROPPED_TFMS}"

    printf '\nEvery multi-target project under `csharp/` now targets %s, the single-target ones (tools, demo, benchmarks) target `%s`, `global.json` pins the matching SDK band, and the workflows, project files and docs that name a target framework literally moved with them. `netstandard2.0` is untouched, so consumers still on a retired runtime keep resolving that asset instead of losing one.\n' \
        "$(proseList "$(printf '%s\n' "${TARGET_TFMS}" | joinLines ' ')")" "${TARGET_LATEST_TFM}"

    printf '\n`#if NET*_OR_GREATER` guards are deliberately untouched: they name the floor a code path needs, not a target, and stay correct once that target is gone.\n'

    if [ -n "${LEFTOVERS}" ]; then
        printf '\nStill mentioning a dropped target framework. Mostly measurements taken on that runtime, which should stay as they are, but worth a read before merging:\n\n```\n%s\n```\n' "${LEFTOVERS}"
    fi

    printf '\nOpened by [update_target_frameworks.yml](.github/workflows/update_target_frameworks.yml), which runs every December 1. Not auto-merged: this changes what the published packages ship.\n'
} >"${WORK_DIR}/pr-body.md"

PR_RESPONSE=$(jq -n --arg title "${TITLE}" --arg head "${BRANCH}" --arg base "${BASE_BRANCH}" \
    --rawfile body "${WORK_DIR}/pr-body.md" \
    '{title: $title, head: $head, base: $base, body: $body}' \
    | ghApi -X POST --data @- "https://api.github.com/repos/${GITHUB_REPOSITORY}/pulls")

PR_NUMBER=$(jq -er '.number' <<<"${PR_RESPONSE}")
log "opened PR #${PR_NUMBER}: ${TITLE}"

if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
    {
        printf '## [%s](%s)\n\n' "${TITLE}" "$(jq -er '.html_url' <<<"${PR_RESPONSE}")"
        cat "${WORK_DIR}/pr-body.md"
    } >>"${GITHUB_STEP_SUMMARY}"
fi
