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
# A release that is not metadata-only is itemized from <range>, if one is given and this is run
# inside the repository: one bullet per pull request merged into it, filed under a Keep a Changelog
# heading by its conventional-commit prefix. Without a range it keeps the older behaviour of naming
# the sync and pointing at the compare link.
#
# Usage: update-changelog.sh <changelog-file> <github-repo> <upstream-repo> <from-tag> <new-tag>
#                             <metadata-only:true|false> [date:YYYY-MM-DD] [range]
set -euo pipefail

usage() {
    cat >&2 <<'EOF'
Usage: update-changelog.sh <changelog-file> <github-repo> <upstream-repo> <from-tag> <new-tag> <metadata-only:true|false> [date:YYYY-MM-DD] [range]
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
RANGE="${8:-}"

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
        body="${count} consecutive releases carrying no hand-written changes — metadata syncs plus automated dependency updates. Latest upstream sync: [libphonenumber ${latest}]($(upstreamReleaseLink "${UPSTREAM_REPO}" "${latest}"))."
    fi
    printf '%s\n## [%s](%s) - %s\n\n%s\n' "${markerLine}" "${heading}" "${link}" "${dateRange}" "${body}"
}

# itemizeRange <range>
# One bullet per pull request merged in the range, under Keep a Changelog headings, oldest first.
# --first-parent walks main's own history, so the unit is the merge - the PR's number and branch
# from the subject github writes, its title from the first line of the body - rather than every
# commit that went into it; a commit pushed straight to main is one bullet of its own. The branch
# name is what identifies the bots: this automation's own syncs are dropped (the entry's opening
# sentence already names the sync) and dependabot's collapse onto one line. A sync pushed straight
# to main, as they were before v9.0.38, has no branch name to go on and is recognised by its author
# instead - SYNC_COMMIT_AUTHOR, which the caller has already resolved from the api.
itemizeRange() {
    git log --first-parent --reverse --format='%x1e%s%x1f%b%x1f%an' "$1" 2>/dev/null | awk '
        BEGIN {
            RS = "\036"
            FS = "\037"
            # The conventional-commit prefixes AGENTS.md mandates, and where each belongs. A
            # prefix outside this list is left on the title: "Extensions: ..." is part of what the
            # title says, not a label to strip.
            n = split("feat:Added fix:Fixed perf:Performance docs:Docs refactor:Changed " \
                      "style:Changed test:Changed ci:Changed build:Changed chore:Changed " \
                      "internal:Changed revert:Changed", pairs, " ")
            for (i = 1; i <= n; i++) {
                split(pairs[i], kv, ":")
                section[kv[1]] = kv[2]
            }
        }

        NF {
            # git separates commits with a newline, which lands on both ends of a record.
            subject = $1
            sub(/^\n/, "", subject)
            author = $3
            sub(/\n$/, "", author)
            title = $2
            sub(/\n.*/, "", title)

            branch = ""
            if (subject ~ /^Merge pull request #[0-9]+ from /) {
                pr = subject
                sub(/^Merge pull request #/, "", pr)
                sub(/[^0-9].*/, "", pr)
                branch = subject
                sub(/^.* from [^\/]*\//, "", branch)
            } else {
                title = subject
                pr = (title ~ /\(#[0-9]+\)$/) ? title : ""
                sub(/.*\(#/, "", pr)
                sub(/\).*/, "", pr)
                sub(/ *\(#[0-9]+\)$/, "", title)
            }
            if (title == "") title = subject
            if (branch ~ /^metadata-update\//) next
            if (branch == "" && author != "" && author == ENVIRON["SYNC_COMMIT_AUTHOR"]) next

            type = ""
            scope = ""
            if (match(title, /^[A-Za-z]+(\([^()]*\))?!?: /)) {
                # Held because the scope match below overwrites RLENGTH.
                prefixLength = RLENGTH
                prefix = substr(title, 1, prefixLength - 2)
                sub(/!$/, "", prefix)
                if (match(prefix, /\([^()]*\)$/)) {
                    scope = substr(prefix, RSTART + 1, RLENGTH - 2)
                    type = tolower(substr(prefix, 1, RSTART - 1))
                } else {
                    type = tolower(prefix)
                }
                if (type in section) title = substr(title, prefixLength + 1)
                else type = scope = ""
            }

            if (branch ~ /^dependabot\// || scope == "deps") {
                dependencies = dependencies (dependencies ? ", " : "") "#" pr
                dependencyCount++
                next
            }

            # Sentence case only where that is unambiguously safe: a lowercase first word with no
            # capital, dot or slash in it. `ByteBuffer`, dotnet test and net8.0 stay as written.
            first = title
            sub(/ .*$/, "", first)
            if (title ~ /^[a-z]/ && first !~ /[A-Z.\/_]/) title = toupper(substr(title, 1, 1)) substr(title, 2)
            if (scope != "") title = "(" toupper(substr(scope, 1, 1)) substr(scope, 2) ") " title
            if (title !~ /[.!?]$/) title = title "."

            where = (type in section) ? section[type] : "Changed"
            bullets[where] = bullets[where] "- " title (pr ? " (#" pr ")" : "") "\n"
        }

        END {
            n = split("Added Changed Performance Fixed Docs", order, " ")
            for (i = 1; i <= n; i++) {
                if (order[i] in bullets) printf "%s### %s\n%s", (printed++ ? "\n" : ""), order[i], bullets[order[i]]
            }
            if (dependencyCount) {
                printf "%s### Dependencies\n- %d automated dependency update%s. (%s)\n", \
                    (printed++ ? "\n" : ""), dependencyCount, (dependencyCount > 1 ? "s" : ""), dependencies
            }
        }'
}

# buildSubstantiveBlock <from> <tag> <date> [range]
buildSubstantiveBlock() {
    local from=$1 tag=$2 date=$3 range=${4:-}
    local link body items=""
    link=$(compareLink "${GITHUB_REPO}" "${from}" "${tag}")
    [ -n "${range}" ] && items=$(itemizeRange "${range}")
    if [ -n "${items}" ]; then
        body="Metadata sync to upstream [libphonenumber ${tag}]($(upstreamReleaseLink "${UPSTREAM_REPO}" "${tag}")), plus the work below that merged to \`main\` since ${from}."
        printf '## [%s](%s) - %s\n\n%s\n\n%s\n' "${tag}" "${link}" "${date}" "${body}" "${items}"
    else
        body="Includes the metadata sync to upstream [libphonenumber ${tag}]($(upstreamReleaseLink "${UPSTREAM_REPO}" "${tag}")) plus other changes merged to \`main\` since the last release — see the compare link above for the full diff."
        printf '## [%s](%s) - %s\n\n%s\n' "${tag}" "${link}" "${date}" "${body}"
    fi
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
        NEW_BLOCK=$(buildSubstantiveBlock "${FROM_TAG}" "${NEW_TAG}" "${DATE}" "${RANGE}")
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
