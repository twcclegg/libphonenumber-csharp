#! /bin/bash
# Records one release in CHANGELOG.md, called from github-actions-metadata-update.sh in the same
# commit as the metadata sync. Every release always includes a metadata sync (that is the only
# thing that ever cuts a new tag - see finalize-metadata-release.sh); some releases also bundle
# other work that merged to `main` in the meantime. CHANGELOG.md was rebuilt once, by hand, to
# describe that other work accurately per release; this script only has to keep going from there
# without a human re-curating each entry.
#
# A run of consecutive metadata-only releases (no changes outside resources/) automatically folds
# into a single range entry, so the file does not grow one near-duplicate heading per fortnight
# forever - exactly like the "N releases" ranges already in the file from the historical rebuild,
# just generated instead of researched. A release that is NOT metadata-only always gets its own
# standalone entry and never gets folded into a neighboring run, in either direction: it cannot
# extend a prior run (its own content isn't metadata-only) and, because it carries no run marker
# of its own, a later metadata-only release cannot fold into it either - the next metadata-only
# release starts a brand new run instead.
#
# The fold state lives in an HTML comment immediately above the heading it describes:
#   <!-- changelog-run from=vFROM first=vFIRST start-date=YYYY-MM-DD count=N -->
# `from` is the tag right before the run started (the baseline for the compare link); `first` is
# the first tag actually in the run; `count` is how many consecutive metadata-only releases have
# folded into it so far. Only a heading with this marker directly above it is a candidate to
# extend - a human-written or pre-rebuild heading never has one, so it can never be mistaken for
# a foldable run.
#
# Usage: update-changelog.sh <changelog-file> <github-repo> <upstream-repo> <from-tag> <new-tag>
#                             <metadata-only:true|false> [date:YYYY-MM-DD]
set -euo pipefail

usage() {
    cat >&2 <<'EOF'
Usage: update-changelog.sh <changelog-file> <github-repo> <upstream-repo> <from-tag> <new-tag> <metadata-only:true|false> [date:YYYY-MM-DD]
EOF
}

if [ "$#" -lt 6 ]; then
    echo "missing required argument" >&2
    usage
    exit 2
fi

CHANGELOG_FILE="$1"
GITHUB_REPO="$2"
UPSTREAM_REPO="$3"
FROM_TAG="$4"
NEW_TAG="$5"
METADATA_ONLY="$6"
DATE="${7:-$(date -u +%F)}"

if [ "${METADATA_ONLY}" != "true" ] && [ "${METADATA_ONLY}" != "false" ]; then
    echo "metadata-only must be \"true\" or \"false\", got: ${METADATA_ONLY}" >&2
    usage
    exit 2
fi

NEXT_ENTRY_MARKER='<!-- next-entry -->'
RUN_MARKER_RE='^<!-- changelog-run from=([^ ]+) first=([^ ]+) start-date=([0-9]{4}-[0-9]{2}-[0-9]{2}) count=([0-9]+) -->$'

mapfile -t LINES <"${CHANGELOG_FILE}"

MARKER_INDEX=-1
for i in "${!LINES[@]}"; do
    if [ "${LINES[${i}]}" = "${NEXT_ENTRY_MARKER}" ]; then
        MARKER_INDEX=${i}
        break
    fi
done
if [ "${MARKER_INDEX}" -lt 0 ]; then
    echo "could not find \"${NEXT_ENTRY_MARKER}\" in ${CHANGELOG_FILE}" >&2
    usage
    exit 2
fi

# The line right after the marker (skipping one blank line, which is how every existing entry in
# the file is spaced from the one above it) is the only place a foldable run marker can be.
CANDIDATE_INDEX=$((MARKER_INDEX + 1))
if [ "${CANDIDATE_INDEX}" -lt "${#LINES[@]}" ] && [ "${LINES[${CANDIDATE_INDEX}]}" = "" ]; then
    CANDIDATE_INDEX=$((CANDIDATE_INDEX + 1))
fi

RUN_MATCHED=false
if [ "${METADATA_ONLY}" = "true" ] && [[ "${LINES[${CANDIDATE_INDEX}]:-}" =~ ${RUN_MARKER_RE} ]]; then
    RUN_MATCHED=true
    RUN_FROM="${BASH_REMATCH[1]}"
    RUN_FIRST="${BASH_REMATCH[2]}"
    RUN_START_DATE="${BASH_REMATCH[3]}"
    RUN_COUNT="${BASH_REMATCH[4]}"
fi

formatDateRange() {
    if [ "$1" = "$2" ]; then printf '%s' "$1"; else printf '%s – %s' "$1" "$2"; fi
}

compareLink() { printf 'https://github.com/%s/compare/%s...%s' "$1" "$2" "$3"; }
upstreamReleaseLink() { printf 'https://github.com/%s/releases/tag/%s' "$1" "$2"; }

# buildMetadataOnlyBlock <from> <first> <start-date> <count> <latest> <date>
buildMetadataOnlyBlock() {
    local from=$1 first=$2 startDate=$3 count=$4 latest=$5 date=$6
    local heading dateRange link markerLine body
    if [ "${count}" -eq 1 ]; then heading="${first}"; else heading="${first} – ${latest}"; fi
    dateRange=$(formatDateRange "${startDate}" "${date}")
    link=$(compareLink "${GITHUB_REPO}" "${from}" "${latest}")
    markerLine="<!-- changelog-run from=${from} first=${first} start-date=${startDate} count=${count} -->"
    if [ "${count}" -eq 1 ]; then
        body="Metadata update to upstream [libphonenumber ${latest}]($(upstreamReleaseLink "${UPSTREAM_REPO}" "${latest}"))."
    else
        body="${count} consecutive metadata-only releases (no changes to hand-written source, tests, docs, or CI/build configuration). Latest upstream sync: [libphonenumber ${latest}]($(upstreamReleaseLink "${UPSTREAM_REPO}" "${latest}"))."
    fi
    printf '%s\n## [%s](%s) - %s\n\n%s\n' "${markerLine}" "${heading}" "${link}" "${dateRange}" "${body}"
}

# buildSubstantiveBlock <from> <tag> <date>
buildSubstantiveBlock() {
    local from=$1 tag=$2 date=$3
    local link body
    link=$(compareLink "${GITHUB_REPO}" "${from}" "${tag}")
    body="Includes the metadata sync to upstream [libphonenumber ${tag}]($(upstreamReleaseLink "${UPSTREAM_REPO}" "${tag}")) plus other changes merged to \`main\` since the last release — see the compare link above for the full diff."
    printf '## [%s](%s) - %s\n\n%s\n' "${tag}" "${link}" "${date}" "${body}"
}

if ${RUN_MATCHED}; then
    NEW_BLOCK=$(buildMetadataOnlyBlock "${RUN_FROM}" "${RUN_FIRST}" "${RUN_START_DATE}" "$((RUN_COUNT + 1))" "${NEW_TAG}" "${DATE}")
    REPLACE_FROM=${CANDIDATE_INDEX}
    # The block being replaced is: marker, heading, blank, then one or more body lines running up
    # to (but not including) the next blank line or the end of the file. Measuring it here, rather
    # than assuming a fixed length, means this still splices out exactly the right span if
    # buildMetadataOnlyBlock's body ever grows past one line.
    END=$((CANDIDATE_INDEX + 3))
    while [ "${END}" -lt "${#LINES[@]}" ] && [ "${LINES[${END}]}" != "" ]; do
        END=$((END + 1))
    done
    REPLACE_COUNT=$((END - CANDIDATE_INDEX))
else
    if [ "${METADATA_ONLY}" = "true" ]; then
        NEW_BLOCK=$(buildMetadataOnlyBlock "${FROM_TAG}" "${NEW_TAG}" "${DATE}" 1 "${NEW_TAG}" "${DATE}")
    else
        NEW_BLOCK=$(buildSubstantiveBlock "${FROM_TAG}" "${NEW_TAG}" "${DATE}")
    fi
    # Insert as a new block right after the marker, pushing whatever was there down.
    REPLACE_FROM=$((MARKER_INDEX + 1))
    REPLACE_COUNT=0
fi

TMP_FILE=$(mktemp)
trap 'rm -f "${TMP_FILE}"' EXIT

{
    for ((i = 0; i < REPLACE_FROM; i++)); do
        printf '%s\n' "${LINES[${i}]}"
    done
    # Only the insert path needs a blank line ahead of the new block: the lines already emitted
    # above stop right at the marker text itself there, with no separating blank line yet. The
    # fold path's REPLACE_FROM already points at the existing run marker, so the blank line above
    # it (the one separating it from whatever precedes it) was already emitted in the loop above -
    # adding another one here would double it up.
    if [ "${REPLACE_COUNT}" -eq 0 ]; then
        printf '\n'
    fi
    printf '%s\n' "${NEW_BLOCK}"
    for ((i = REPLACE_FROM + REPLACE_COUNT; i < ${#LINES[@]}; i++)); do
        printf '%s\n' "${LINES[${i}]}"
    done
} >"${TMP_FILE}"

mv "${TMP_FILE}" "${CHANGELOG_FILE}"
trap - EXIT
