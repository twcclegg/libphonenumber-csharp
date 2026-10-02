#! /bin/bash
# lib/github-release-helpers.sh - the functions every piece of the release automation sources.
#
# Each one is small enough to look obviously right and has been wrong somewhere at least once: a
# truthiness check that accepts "no", a warn() that emits a multi-line GitHub annotation and silently
# drops everything after the first newline, an auto-merge call that reads {"data":null} as success.
#
# ghApi is overridden rather than reached: these tests assert on the request bodies and urls the
# helper builds, which is the part that decides what a release looks like, and they make no network
# call at all.
#
# shellcheck disable=SC2034 # the package-id and workflow variables are read by the sourced functions
# shellcheck disable=SC2329 # ghApi is overridden here and invoked by the functions under test

sourceHelpers() {
    # The helper is sourced, never run, and expects its caller to have set this already.
    set -euo pipefail
    # shellcheck source=../github-release-helpers.sh
    source "${REPO_ROOT}/lib/github-release-helpers.sh"
}

# Replaces ghApi with a recorder: the request body lands in request-body.json and the arguments
# (method, url) in request-args.txt, and it answers with whatever stubGhApiResponse was given.
stubGhApi() {
    GH_API_RESPONSE="${1:-{\}}"
    ghApi() {
        printf '%s\n' "$*" >>"${TEST_DIR}/request-args.txt"
        # --data @- means the payload arrives on stdin; a call without one must not block.
        case "$*" in
            *"--data @-"*) cat >"${TEST_DIR}/request-body.json" ;;
        esac
        printf '%s' "${GH_API_RESPONSE}"
    }
}

requestBody() {
    cat "${TEST_DIR}/request-body.json"
}

requestArgs() {
    cat "${TEST_DIR}/request-args.txt"
}

test_toLower_lowercases_ascii() {
    sourceHelpers
    assertEquals "true" "$(toLower "TRUE")"
    assertEquals "v9.0.39" "$(toLower "V9.0.39")"
    assertEquals "libphonenumber-csharp" "$(toLower "libphonenumber-csharp")"
}

test_isTrue_accepts_the_documented_spellings() {
    sourceHelpers
    for value in true TRUE True 1 yes YES y Y; do
        isTrue "${value}" || failTest "isTrue should accept ${value}"
    done
}

test_isTrue_rejects_everything_else() {
    sourceHelpers
    # "" and "false" matter most: a workflow input that was left alone arrives as one of those, and a
    # false positive here runs a release step that was meant to be skipped.
    for value in false FALSE no n 0 "" maybe 2 null; do
        if isTrue "${value}"; then
            failTest "isTrue should reject '${value}'"
        fi
    done
}

test_warn_escapes_the_github_annotation() {
    sourceHelpers
    # % and newlines have to be percent-encoded, or the annotation is truncated at the first newline
    # and a literal % swallows the characters after it.
    local output
    output=$(warn "100% of 2
lines" 2>&1)
    assertContains "${output}" "::warning::100%25 of 2%0Alines"
    assertContains "${output}" "warning: 100% of 2" "the plain stderr copy stays human-readable"
}

test_fail_exits_with_the_given_code_and_writes_the_step_summary() {
    sourceHelpers
    local summary="${TEST_DIR}/summary.md"
    : >"${summary}"
    set +e
    (
        GITHUB_STEP_SUMMARY="${summary}"
        fail 4 "needs attention"
    ) >/dev/null 2>&1
    local status=$?
    set -e
    assertEquals "4" "${status}" "fail must exit with the code it was given, not 1"
    assertFileContains "${summary}" "needs attention" \
        "the reason has to reach the run summary, not just the step log"
}

test_createRelease_builds_the_release_payload() {
    sourceHelpers
    stubGhApi
    createRelease "twcclegg/libphonenumber-csharp" "v9.0.40" "0123456789012345678901234567890123456789"

    local body
    body=$(requestBody)
    assertEquals "v9.0.40" "$(jq -r '.tag_name' <<<"${body}")"
    assertEquals "v9.0.40" "$(jq -r '.name' <<<"${body}")"
    assertEquals "0123456789012345678901234567890123456789" "$(jq -r '.target_commitish' <<<"${body}")" \
        "the release has to be pinned to the commit, not to whatever the branch points at later"
    assertEquals "true" "$(jq -r '.generate_release_notes' <<<"${body}")"

    # The version in the nuget links is the tag without its leading "v"; a release whose links 404
    # is the whole failure mode here.
    local notes
    notes=$(jq -r '.body' <<<"${body}")
    assertContains "${notes}" "https://www.nuget.org/packages/libphonenumber-csharp/9.0.40"
    assertContains "${notes}" "https://www.nuget.org/packages/libphonenumber-csharp.extensions/9.0.40"
    assertContains "${notes}" "https://github.com/google/libphonenumber/releases/tag/v9.0.40"

    assertContains "$(requestArgs)" "https://api.github.com/repos/twcclegg/libphonenumber-csharp/releases"
    assertContains "$(requestArgs)" "-X POST"
}

test_createRelease_honours_the_package_id_overrides() {
    sourceHelpers
    # A fork releasing its own package id, which is why these are variables at all.
    NUGET_PACKAGE_ID="someone.fork"
    NUGET_EXTENSIONS_PACKAGE_ID="someone.fork.extras"
    UPSTREAM_REPOSITORY="someone/upstream"
    stubGhApi
    createRelease "someone/their-fork" "v1.2.3" "0123456789012345678901234567890123456789"

    local notes
    notes=$(jq -r '.body' <<<"$(requestBody)")
    assertContains "${notes}" "https://www.nuget.org/packages/someone.fork/1.2.3"
    assertContains "${notes}" "https://www.nuget.org/packages/someone.fork.extras/1.2.3"
    assertContains "${notes}" "https://github.com/someone/upstream/releases/tag/v1.2.3"
    assertNotContains "${notes}" "libphonenumber-csharp"
}

test_dispatchPublish_asks_for_the_publish_workflow_by_ref() {
    sourceHelpers
    stubGhApi
    dispatchPublish "twcclegg/libphonenumber-csharp" "v9.0.40"

    assertEquals "v9.0.40" "$(jq -r '.ref' <<<"$(requestBody)")" \
        "the publish run has to be dispatched against the tag, not a branch"
    assertContains "$(requestArgs)" \
        "https://api.github.com/repos/twcclegg/libphonenumber-csharp/actions/workflows/publish_nuget.yml/dispatches"
}

test_dispatchPublish_honours_the_workflow_override() {
    sourceHelpers
    PUBLISH_WORKFLOW="other_publish.yml"
    stubGhApi
    dispatchPublish "someone/their-fork" "v1.2.3"
    assertContains "$(requestArgs)" "/actions/workflows/other_publish.yml/dispatches"
}

test_armAutoMerge_is_silent_when_github_reports_the_pull_request() {
    sourceHelpers
    stubGhApi '{"data":{"enablePullRequestAutoMerge":{"pullRequest":{"number":477}}}}'
    assertEquals "" "$(armAutoMerge "PR_nodeid")" "success is reported as no message at all"
}

test_armAutoMerge_reports_a_graphql_error_message() {
    sourceHelpers
    # GraphQL answers http 200 with an errors array, so --fail never sees this one.
    stubGhApi '{"errors":[{"message":"Pull request is in clean status"}]}'
    assertEquals "Pull request is in clean status" "$(armAutoMerge "PR_nodeid")"
}

test_armAutoMerge_rejects_a_response_that_reports_nothing() {
    sourceHelpers
    # {"data":null} has no errors key and no pull request either; treating the missing errors key as
    # success would report auto-merge as armed when it is not.
    stubGhApi '{"data":null}'
    assertEquals "github accepted the request without reporting auto-merge as enabled" \
        "$(armAutoMerge "PR_nodeid")"
}

test_armAutoMerge_handles_an_unparseable_response() {
    sourceHelpers
    stubGhApi 'this is not json'
    assertEquals "could not parse github's response" "$(armAutoMerge "PR_nodeid")"
}

test_armAutoMerge_handles_an_unreachable_api() {
    sourceHelpers
    ghApi() { return 7; }
    assertEquals "could not reach the github api" "$(armAutoMerge "PR_nodeid")" \
        "a curl failure must not abort the caller: the PR is already open and only needs a manual merge"
}

test_helpers_stay_bash_3_2_compatible() {
    # This file is sourced by every script and has to load on macOS's stock bash 3.2, which is why
    # toLower pipes through tr instead of using ${var,,}. Comments are stripped first, since the
    # file explains that in prose.
    local code
    code=$(sed 's|#.*||' "${REPO_ROOT}/lib/github-release-helpers.sh")
    run grep -nE '\$\{[A-Za-z_][A-Za-z0-9_]*(,,|\^\^)\}|(mapfile|readarray)[[:space:]]|declare -A' <<<"${code}"
    assertStatus 1 "lib/github-release-helpers.sh must stay bash 3.2 compatible: ${RUN_OUTPUT}"
}
