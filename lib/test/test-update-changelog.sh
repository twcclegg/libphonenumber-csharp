#! /bin/bash
# lib/update-changelog.sh - the CHANGELOG.md entry the metadata sync writes, and the run-folding that
# keeps a year of fortnightly metadata releases from becoming 26 near-identical headings.
#
# The fold is a small state machine kept in an HTML comment above the heading it describes, and every
# interesting case is a splice: extend the run above the marker, or insert a fresh block below it,
# without duplicating or eating a neighbouring entry. Nothing else in this repository exercises it -
# a mistake here is only visible in the released changelog, after the fact.

CHANGELOG="changelog.md"

# A changelog whose body after the marker is <<<extra>>>, so a test can set up exactly the state it
# cares about and assert the rest came through untouched.
writeChangelog() { # <lines after the marker...>
    {
        printf '# Changelog\n\n'
        printf 'Preamble that must survive every rewrite.\n\n'
        printf '<!-- next-entry -->\n'
        if [ "$#" -gt 0 ]; then
            printf '\n'
            printf '%s\n' "$@"
        fi
    } >"${CHANGELOG}"
}

# The existing release the new entry is spliced above, so a test can prove it was not disturbed.
olderEntry() {
    printf '## [v9.0.30](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.29...v9.0.30) - 2026-01-01\n\nAn older release that must stay exactly as it is.'
}

updateChangelog() { # <from-tag> <new-tag> <metadata-only> [date]
    # The script reads the changelog with mapfile, which is bash 4+; on macOS's stock bash 3.2 these
    # tests skip rather than fail. The argument-validation tests below stop before that line.
    requireBashVersion 4
    runScript update-changelog.sh "${CHANGELOG}" \
        "twcclegg/libphonenumber-csharp" "google/libphonenumber" "$@"
}

test_rejects_too_few_arguments() {
    runScript update-changelog.sh "${CHANGELOG}" owner/repo upstream/repo v1.0.0 v1.0.1
    assertStatus 2
    assertOutputContains "missing required argument"
    assertOutputContains "Usage:"
}

test_rejects_a_metadata_only_value_that_is_not_true_or_false() {
    writeChangelog "$(olderEntry)"
    updateChangelog v9.0.30 v9.0.31 maybe 2026-02-01
    assertStatus 2
    assertOutputContains 'metadata-only must be "true" or "false", got: maybe'
}

test_rejects_a_changelog_without_the_marker() {
    printf '# Changelog\n\nNo marker here.\n' >"${CHANGELOG}"
    updateChangelog v9.0.30 v9.0.31 true 2026-02-01
    assertStatus 2
    assertOutputContains 'could not find "<!-- next-entry -->"'
}

test_a_metadata_only_release_starts_a_run_of_one() {
    writeChangelog "$(olderEntry)"
    updateChangelog v9.0.30 v9.0.31 true 2026-02-01
    assertStatus 0

    assertFileContains "${CHANGELOG}" \
        "<!-- changelog-run from=v9.0.30 first=v9.0.31 start-date=2026-02-01 count=1 -->"
    assertFileContains "${CHANGELOG}" \
        "## [v9.0.31](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.30...v9.0.31) - 2026-02-01"
    assertFileContains "${CHANGELOG}" \
        "Metadata update to upstream [libphonenumber v9.0.31](https://github.com/google/libphonenumber/releases/tag/v9.0.31)."
    assertFileContains "${CHANGELOG}" "An older release that must stay exactly as it is."
    assertFileContains "${CHANGELOG}" "Preamble that must survive every rewrite."
    assertOccurrences "${CHANGELOG}" "<!-- next-entry -->" 1 \
        "the marker has to stay, exactly once, for the next release to find"
}

test_a_second_metadata_only_release_extends_the_run_in_place() {
    writeChangelog \
        "<!-- changelog-run from=v9.0.30 first=v9.0.31 start-date=2026-02-01 count=1 -->" \
        "## [v9.0.31](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.30...v9.0.31) - 2026-02-01" \
        "" \
        "Metadata update to upstream [libphonenumber v9.0.31](https://github.com/google/libphonenumber/releases/tag/v9.0.31)." \
        "" \
        "$(olderEntry)"
    updateChangelog v9.0.31 v9.0.32 true 2026-02-15
    assertStatus 0

    # The run's baseline (from=) stays at the release before the run started, so the compare link
    # still spans the whole range rather than just the last fortnight.
    assertFileContains "${CHANGELOG}" \
        "<!-- changelog-run from=v9.0.30 first=v9.0.31 start-date=2026-02-01 count=2 -->"
    assertFileContains "${CHANGELOG}" \
        "## [v9.0.31 – v9.0.32](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.30...v9.0.32) - 2026-02-01 – 2026-02-15"
    assertFileContains "${CHANGELOG}" "2 consecutive releases carrying no hand-written changes"
    assertFileContains "${CHANGELOG}" \
        "Latest upstream sync: [libphonenumber v9.0.32](https://github.com/google/libphonenumber/releases/tag/v9.0.32)."

    # Extending means replacing the block, not adding a second one next to it.
    assertOccurrences "${CHANGELOG}" "<!-- changelog-run" 1
    assertOccurrences "${CHANGELOG}" "## [v9.0.31" 1
    assertFileNotContains "${CHANGELOG}" "count=1 -->"
    assertFileNotContains "${CHANGELOG}" "Metadata update to upstream [libphonenumber v9.0.31]"
    assertFileContains "${CHANGELOG}" "An older release that must stay exactly as it is."
}

test_extending_a_run_replaces_a_multi_line_body_whole() {
    # The script measures the block it replaces by scanning to the next blank line rather than
    # assuming a fixed length, so a body that grows past one line still splices out cleanly.
    writeChangelog \
        "<!-- changelog-run from=v9.0.30 first=v9.0.31 start-date=2026-02-01 count=3 -->" \
        "## [v9.0.31 – v9.0.33](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.30...v9.0.33) - 2026-02-01 – 2026-03-01" \
        "" \
        "3 consecutive releases carrying no hand-written changes — metadata syncs plus automated" \
        "dependency updates. Latest upstream sync: libphonenumber v9.0.33." \
        "" \
        "$(olderEntry)"
    updateChangelog v9.0.33 v9.0.34 true 2026-03-15
    assertStatus 0

    assertFileContains "${CHANGELOG}" "count=4 -->"
    assertFileContains "${CHANGELOG}" "## [v9.0.31 – v9.0.34]"
    assertFileNotContains "${CHANGELOG}" "dependency updates. Latest upstream sync: libphonenumber v9.0.33." \
        "the whole old block has to go, including the second line of its body"
    assertOccurrences "${CHANGELOG}" "3 consecutive releases" 0
    assertFileContains "${CHANGELOG}" "An older release that must stay exactly as it is."
}

test_a_substantive_release_gets_its_own_entry() {
    writeChangelog "$(olderEntry)"
    updateChangelog v9.0.30 v9.0.31 false 2026-02-01
    assertStatus 0

    assertFileContains "${CHANGELOG}" \
        "## [v9.0.31](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.30...v9.0.31) - 2026-02-01"
    assertFileContains "${CHANGELOG}" \
        "Includes the metadata sync to upstream [libphonenumber v9.0.31](https://github.com/google/libphonenumber/releases/tag/v9.0.31)"
    # No run marker: that is what stops a later metadata-only release folding into this entry.
    assertFileNotContains "${CHANGELOG}" "<!-- changelog-run"
}

test_a_substantive_release_never_extends_the_run_above_it() {
    writeChangelog \
        "<!-- changelog-run from=v9.0.30 first=v9.0.31 start-date=2026-02-01 count=2 -->" \
        "## [v9.0.31 – v9.0.32](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.30...v9.0.32) - 2026-02-01 – 2026-02-15" \
        "" \
        "2 consecutive releases carrying no hand-written changes — metadata syncs plus automated dependency updates. Latest upstream sync: [libphonenumber v9.0.32](https://github.com/google/libphonenumber/releases/tag/v9.0.32)." \
        "" \
        "$(olderEntry)"
    updateChangelog v9.0.32 v9.0.33 false 2026-03-01
    assertStatus 0

    assertFileContains "${CHANGELOG}" "<!-- changelog-run from=v9.0.30 first=v9.0.31 start-date=2026-02-01 count=2 -->" \
        "the existing run keeps its count: this release is not part of it"
    assertFileContains "${CHANGELOG}" "## [v9.0.33]"
    assertFileContains "${CHANGELOG}" "Includes the metadata sync to upstream [libphonenumber v9.0.33]"
    assertOccurrences "${CHANGELOG}" "<!-- changelog-run" 1
}

test_a_metadata_only_release_after_a_substantive_one_starts_a_fresh_run() {
    # The chain is broken by the entry in between, which carries no marker, so the new release must
    # start at count=1 rather than reaching past it to resume the old run.
    writeChangelog \
        "## [v9.0.33](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.32...v9.0.33) - 2026-03-01" \
        "" \
        "Includes the metadata sync to upstream libphonenumber v9.0.33 plus other changes." \
        "" \
        "<!-- changelog-run from=v9.0.30 first=v9.0.31 start-date=2026-02-01 count=2 -->" \
        "## [v9.0.31 – v9.0.32](https://github.com/twcclegg/libphonenumber-csharp/compare/v9.0.30...v9.0.32) - 2026-02-01 – 2026-02-15" \
        "" \
        "2 consecutive releases carrying no hand-written changes."
    updateChangelog v9.0.33 v9.0.34 true 2026-03-15
    assertStatus 0

    assertFileContains "${CHANGELOG}" \
        "<!-- changelog-run from=v9.0.33 first=v9.0.34 start-date=2026-03-15 count=1 -->"
    assertFileContains "${CHANGELOG}" "count=2 -->" "the older run stays as it was"
    assertOccurrences "${CHANGELOG}" "<!-- changelog-run" 2
    assertFileContains "${CHANGELOG}" "Includes the metadata sync to upstream libphonenumber v9.0.33 plus other changes."
}

test_the_new_entry_goes_directly_below_the_marker() {
    writeChangelog "$(olderEntry)"
    updateChangelog v9.0.30 v9.0.31 true 2026-02-01
    assertStatus 0

    # Order matters as much as content: a newest-first changelog with the entry in the wrong place
    # still reads as valid markdown.
    local layout
    layout=$(grep -nE '^(<!-- next-entry -->|<!-- changelog-run|## \[)' "${CHANGELOG}" | cut -d: -f2-)
    assertContains "${layout}" "<!-- next-entry -->"
    assertEquals "<!-- next-entry -->" "$(printf '%s\n' "${layout}" | sed -n 1p)"
    assertContains "$(printf '%s\n' "${layout}" | sed -n 2p)" "<!-- changelog-run"
    assertContains "$(printf '%s\n' "${layout}" | sed -n 3p)" "## [v9.0.31]"
    assertContains "$(printf '%s\n' "${layout}" | sed -n 4p)" "## [v9.0.30]"
}

test_blank_lines_are_not_doubled_on_either_path() {
    writeChangelog "$(olderEntry)"
    updateChangelog v9.0.30 v9.0.31 true 2026-02-01
    assertStatus 0
    run grep -c '^$' "${CHANGELOG}"
    local afterInsert="${RUN_OUTPUT}"

    updateChangelog v9.0.31 v9.0.32 true 2026-02-15
    assertStatus 0
    run grep -c '^$' "${CHANGELOG}"
    assertEquals "${afterInsert}" "${RUN_OUTPUT}" \
        "folding replaces a block of the same shape, so the blank-line count cannot drift"

    # Two consecutive blank lines anywhere mean a splice added a separator that was there already.
    run awk 'prev == "" && $0 == "" { print NR; found = 1 } { prev = $0 } END { exit found ? 1 : 0 }' "${CHANGELOG}"
    assertStatus 0 "found consecutive blank lines at line(s): ${RUN_OUTPUT}"
}

test_the_date_defaults_to_today_in_utc() {
    writeChangelog "$(olderEntry)"
    updateChangelog v9.0.30 v9.0.31 true
    assertStatus 0
    assertFileContains "${CHANGELOG}" "start-date=$(date -u +%F)"
    assertFileContains "${CHANGELOG}" "- $(date -u +%F)"
}

test_the_real_changelog_still_carries_the_marker() {
    # The sync warns and skips the changelog entry when this is missing, so a release would ship with
    # no entry at all and nothing would fail.
    assertFileContains "${REPO_ROOT}/CHANGELOG.md" "<!-- next-entry -->"
}
