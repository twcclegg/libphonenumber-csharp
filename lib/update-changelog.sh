#! /bin/bash
# Records one release in CHANGELOG.md, called from github-actions-metadata-update.sh in the same
# commit as the metadata sync.
#
# The entry is the GitHub release's own notes: the sync passes in the body that GitHub's
# generate-release-notes endpoint produced for this tag (the same generator the Releases API call in
# finalize-metadata-release.sh uses), so the changelog and the release page list the same merged PRs
# in the same order. Nothing is summarised, folded or curated here - an earlier version collapsed
# "bot-only" releases into ranged entries and replaced the PR list with a generic sentence, which
# left the changelog saying nothing about what shipped.
#
# Usage: update-changelog.sh <changelog-file> <github-repo> <upstream-repo> <from-tag> <new-tag>
#                             <release-notes-file> [date:YYYY-MM-DD]
set -euo pipefail

usage() {
    cat >&2 <<'EOF2'
Usage: update-changelog.sh <changelog-file> <github-repo> <upstream-repo> <from-tag> <new-tag> <release-notes-file> [date:YYYY-MM-DD]
EOF2
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
NOTES_FILE="$6"
DATE="${7:-$(date -u +%F)}"

NEXT_ENTRY_MARKER='<!-- next-entry -->'

if [ ! -s "${NOTES_FILE}" ]; then
    echo "release notes file is missing or empty: ${NOTES_FILE}" >&2
    exit 2
fi
if ! grep -qxF "${NEXT_ENTRY_MARKER}" "${CHANGELOG_FILE}"; then
    echo "could not find \"${NEXT_ENTRY_MARKER}\" in ${CHANGELOG_FILE}" >&2
    usage
    exit 2
fi

ENTRY_FILE=$(mktemp)
TMP_FILE=$(mktemp)
trap 'rm -f "${ENTRY_FILE}" "${TMP_FILE}"' EXIT

{
    printf '## [%s](https://github.com/%s/compare/%s...%s) - %s\n\n' \
        "${NEW_TAG}" "${GITHUB_REPO}" "${FROM_TAG}" "${NEW_TAG}" "${DATE}"
    printf 'Metadata sync to upstream [libphonenumber %s](https://github.com/%s/releases/tag/%s).\n\n' \
        "${NEW_TAG}" "${UPSTREAM_REPO}" "${NEW_TAG}"
    # The notes are used as GitHub wrote them, apart from nesting their headings under the entry's
    # and dropping the trailing compare link, which the entry heading already carries.
    sed -e '/^\*\*Full Changelog\*\*/d' -e 's/^## /### /' "${NOTES_FILE}" \
        | cat -s | sed -e :a -e '/^\n*$/{$d;N;ba' -e '}'
} >"${ENTRY_FILE}"

# Insert right after the marker, pushing earlier entries down.
awk -v marker="${NEXT_ENTRY_MARKER}" -v entry="${ENTRY_FILE}" '
    { print }
    $0 == marker && !done {
        print ""
        while ((getline line < entry) > 0) print line
        done = 1
    }' "${CHANGELOG_FILE}" >"${TMP_FILE}"

mv "${TMP_FILE}" "${CHANGELOG_FILE}"
trap - EXIT
rm -f "${ENTRY_FILE}"
