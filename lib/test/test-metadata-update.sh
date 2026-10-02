#! /bin/bash
# lib/github-actions-metadata-update.sh - the daily sync that decides whether a release happens.
#
# Covered here: the option parsing and every gate that runs before the script first reaches the
# network, which is the part that answers "should there be a release at all". Both version lookups
# are overridable (UPSTREAM_TAG, DEPLOYED_VERSION) precisely so that stretch can be exercised, so
# these tests set both and never make a request.
#
# Not covered here: the upstream clone, the .java/.proto diff gate, the copy, and the push - they
# need the upstream repository and a token. A dry run against the real api is the way to exercise
# those, and the syncing-upstream-metadata skill documents it.

sync() { # [args...]
    runScript github-actions-metadata-update.sh "$@"
}

# Both lookups pinned, so nothing before the gates needs a network. 9.0.39 -> v9.0.40 is the normal
# shape: upstream one release ahead of what is published.
syncWithVersions() { # <upstream-tag> <deployed-version> [args...]
    local upstream="$1" deployed="$2"
    shift 2
    run env UPSTREAM_TAG="${upstream}" DEPLOYED_VERSION="${deployed}" \
        bash "${REPO_ROOT}/lib/github-actions-metadata-update.sh" "$@"
}

test_help_exits_zero() {
    sync --help
    assertStatus 0
    assertOutputContains "Usage: github-actions-metadata-update.sh"
    assertOutputContains "--dry-run"
}

test_rejects_an_unknown_option() {
    sync --no-such-option
    assertStatus 2
    assertOutputContains "unknown option: --no-such-option"
}

test_requires_a_token_outside_a_dry_run() {
    GITHUB_TOKEN="" sync
    assertStatus 2
    assertOutputContains "GitHub token required"
}

test_a_dry_run_needs_no_token() {
    # A maintainer asking "what would tonight's run do?" should not have to mint a token; the run
    # warns about the rate limit instead.
    GITHUB_TOKEN="" UPSTREAM_TAG=v9.0.40 DEPLOYED_VERSION=9.0.40 \
        run bash "${REPO_ROOT}/lib/github-actions-metadata-update.sh" --dry-run
    assertStatus 0
    assertOutputContains "dry run"
    assertOutputContains "versions match, new release not required"
}

test_reports_matching_versions_as_nothing_to_do() {
    syncWithVersions v9.0.40 9.0.40 --dry-run
    assertStatus 0
    assertOutputContains "versions match, new release not required"
}

test_reports_a_published_version_ahead_of_upstream_as_nothing_to_do() {
    # What a C#-only patch release leaves behind: 9.0.40.1 published against upstream v9.0.40.
    syncWithVersions v9.0.40 9.0.41 --dry-run
    assertStatus 0
    assertOutputContains "is ahead of upstream"
    assertOutputNotContains "dry run complete"
}

test_stops_on_an_upstream_major_version_bump() {
    # A major bump is a porting project, not a metadata sync; exit 4 is EXIT_NEEDS_ATTENTION and the
    # workflow surfaces it rather than opening a PR.
    syncWithVersions v10.0.0 9.0.40 --dry-run
    assertStatus 4
    assertOutputContains "major version update: upstream is v10.0.0, this port tracks 9.x"
}

test_the_expected_major_version_is_overridable() {
    # The same gate has to let the bump through once a human has decided to track it.
    run env UPSTREAM_TAG=v10.0.0 DEPLOYED_VERSION=9.0.40 EXPECTED_MAJOR_VERSION=10 \
        bash "${REPO_ROOT}/lib/github-actions-metadata-update.sh" --dry-run
    assertOutputNotContains "major version update"
    if [ "${RUN_STATUS}" = "4" ]; then
        failTest "EXPECTED_MAJOR_VERSION=10 should let a v10 upstream release through: ${RUN_OUTPUT}"
    fi
}

test_rejects_an_upstream_tag_that_is_not_a_release_version() {
    syncWithVersions "refs/heads/master" 9.0.40 --dry-run
    assertStatus 1
    assertOutputContains "unexpected upstream release tag: refs/heads/master"
}

test_rejects_a_published_version_that_is_not_a_version() {
    # getLatestNugetRelease answering with something unexpected must stop the run, not be compared.
    syncWithVersions v9.0.40 "not-a-version" --dry-run
    assertStatus 1
    assertOutputContains "unexpected deployed nuget version: not-a-version"
}

test_must_be_run_from_the_repository_root() {
    # The checkout it runs in is the one that gets committed, so a wrong working directory has to
    # stop it before anything is written.
    mkdir -p elsewhere
    cd elsewhere || failTest "could not enter the scratch directory"
    syncWithVersions v9.0.40 9.0.39 --dry-run
    assertStatus 1
    assertOutputContains "must be run from the root of the repository"
}

test_reports_the_release_it_would_cut() {
    # What a run announces before it starts changing anything: both overridden versions and the
    # repository it resolved. Driven against the fake checkout and the stubs - an earlier version of
    # this test ran in the real repository with no stubs, which made a live compare call and a real
    # shallow clone of upstream on every run.
    syncAgainstDiff "$(compareResponse "resources/PhoneNumberMetadata.xml")"
    assertOutputContains "google/libphonenumber release overridden to v9.0.40"
    assertOutputContains "libphonenumber-csharp version overridden to 9.0.39"
    # someone/their-fork comes from the fake checkout's origin remote: the runner clears
    # GITHUB_REPOSITORY, which resolveRepository would otherwise prefer, so this is the same on a
    # laptop and on a runner.
    assertOutputContains "target repository is someone/their-fork"
    assertOutputContains "fake git: refusing to clone" \
        "it should get as far as the clone, having asked nothing of the real network"
    assertStatus 90
}

# The .java / .proto gate: the one piece of this script that decides whether an unattended release
# can ship a metadata bump whose Java side may need porting by hand. Driven through a fake curl, so
# the upstream diff is whatever the test says it is and no request is made.

# The sync script only warns about a missing jdk on a dry run; a real run stops with exit 3 before
# it reaches any of the gates below, so these tests need javac and java present and skip without them.
requireJdk() {
    requireCommand javac
    requireCommand java
}

syncAgainstDiff() { # <compare-json> [args...]
    local compare="$1"
    shift
    requireJdk
    fakeRepositoryRoot
    stubNetwork "${compare}"
    run env UPSTREAM_TAG=v9.0.40 DEPLOYED_VERSION=9.0.39 GITHUB_TOKEN=unused \
        bash "${REPO_ROOT}/lib/github-actions-metadata-update.sh" "$@"
}

test_stops_when_the_upstream_diff_contains_java_files() {
    syncAgainstDiff "$(compareResponse \
        "resources/PhoneNumberMetadata.xml" \
        "java/libphonenumber/src/com/google/i18n/phonenumbers/PhoneNumberUtil.java")"
    assertStatus 4
    assertOutputContains "upstream diff contains java files:"
    assertOutputContains "PhoneNumberUtil.java"
    assertOutputContains "automatic update not possible"
    assertOutputNotContains "fake git: refusing to clone" \
        "the gate has to stop the run before the upstream clone"
}

test_stops_when_the_upstream_diff_contains_proto_files() {
    syncAgainstDiff "$(compareResponse \
        "resources/PhoneNumberMetadata.xml" \
        "resources/phonemetadata.proto")"
    assertStatus 4
    assertOutputContains "upstream diff contains proto files:"
    assertOutputContains "has proto files, automatic update not possible"
}

test_the_java_check_can_be_overridden() {
    # Reaching the clone is how a test sees the gate was passed: the fake git refuses it, which is
    # the next thing the script does.
    syncAgainstDiff "$(compareResponse \
        "resources/PhoneNumberMetadata.xml" \
        "java/libphonenumber/src/com/google/i18n/phonenumbers/PhoneNumberUtilTest.java")" \
        --skip-java-check
    assertOutputContains "continuing anyway because --skip-java-check"
    assertOutputContains "fake git: refusing to clone"
    assertStatus 90
}

test_the_proto_check_can_be_overridden_by_environment() {
    requireJdk
    fakeRepositoryRoot
    stubNetwork "$(compareResponse "resources/phonemetadata.proto")"
    run env UPSTREAM_TAG=v9.0.40 DEPLOYED_VERSION=9.0.39 GITHUB_TOKEN=unused SKIP_PROTO_CHECK=true \
        bash "${REPO_ROOT}/lib/github-actions-metadata-update.sh"
    assertOutputContains "continuing anyway because --skip-proto-check"
    assertStatus 90
}

test_a_diff_with_only_metadata_passes_the_gates() {
    syncAgainstDiff "$(compareResponse \
        "resources/PhoneNumberMetadata.xml" \
        "resources/geocoding/en/44.txt")"
    assertOutputNotContains "automatic update not possible"
    assertOutputContains "fake git: refusing to clone"
    assertStatus 90
}

test_stops_when_the_compare_response_lists_no_files() {
    # An empty diff means the two tags are identical, or the api answered something unexpected;
    # either way, syncing nothing and cutting a release off it would be worse than stopping.
    #
    # Asserted as "stops without cloning or committing" rather than on the message, because today it
    # stops one step earlier than the script intends: `jq -er '.files // error(...)' | .[].filename`
    # produces no output for an empty array, and `jq -e` exits 4 for that, so `set -e` ends the run
    # before the "no changed files reported" line below it can report anything. Exit 4 is
    # EXIT_NEEDS_ATTENTION, so the workflow still surfaces it as needing a human - but if that line
    # is ever made reachable, this assertion is the one to update.
    syncAgainstDiff '{"files":[]}'
    if [ "${RUN_STATUS}" = "0" ]; then
        failTest "an empty upstream diff must not cut a release: ${RUN_OUTPUT}"
    fi
    assertOutputNotContains "fake git: refusing to clone" "it must stop before the upstream clone"
}

test_stops_when_the_compare_response_has_no_file_list() {
    syncAgainstDiff '{"message":"Not Found"}'
    if [ "${RUN_STATUS}" = "0" ]; then
        failTest "a compare response with no file list must stop the run: ${RUN_OUTPUT}"
    fi
    assertOutputContains "compare response contains no file list"
}

test_warns_when_the_compare_response_is_truncated() {
    # The compare api caps at 300 files, so the gates above can miss a .java file in a huge release;
    # the run says so rather than implying it checked everything.
    local files="" i
    for i in $(seq 1 300); do
        files="${files}${files:+ }resources/geocoding/en/${i}.txt"
    done
    # shellcheck disable=SC2086 # deliberate word splitting: one argument per filename
    syncAgainstDiff "$(compareResponse ${files})"
    assertOutputContains "the compare api returned the maximum of 300 files"
}
