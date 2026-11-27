#! /bin/bash
# Assertions and fixtures for the lib/ automation tests. Sourced by run-tests.sh once per test, not
# run directly.
#
# A test is a function named test_*. The runner calls it in its own subshell, with `set -e` on, the
# working directory set to a fresh empty $TEST_DIR, and $REPO_ROOT pointing at the repository. It
# fails by calling failTest (which every assertion below does for you) and skips by calling skip, so a
# test that cannot run here - one needing a .NET SDK, say - reports as skipped rather than passed.
#
# Kept to bash 3.2 constructs, like lib/github-release-helpers.sh: no associative arrays, no
# mapfile, no ${var,,}, so the harness itself loads on macOS's stock bash. That is not true of every
# script under test - lib/update-changelog.sh uses mapfile deliberately - so those tests guard with
# requireBashVersion and skip rather than fail there.
#
# shellcheck disable=SC2329 # every function here is called from the test files, not from this one

readonly EXIT_SKIP=77

# Named failTest, not fail: github-release-helpers.sh defines its own fail(), and a test that
# sources it would otherwise find the assertions calling the script's version - which takes an exit
# code as its first argument and turns every assertion message into "numeric argument required".
failTest() {
    printf 'FAIL: %s\n' "$*" >&2
    exit 1
}

skip() {
    printf 'SKIP: %s\n' "$*" >&2
    exit ${EXIT_SKIP}
}

# run <command>... - runs it with stdout and stderr captured together, and without the test's
# `set -e` aborting on a non-zero exit, which is the expected outcome for most of these scripts.
# Leaves the result in RUN_STATUS and RUN_OUTPUT for the assertions below.
run() {
    set +e
    RUN_OUTPUT=$("$@" 2>&1)
    RUN_STATUS=$?
    set -e
    return 0
}

# runScript <lib-script-name> [args...] - the common case: run one of the scripts under test.
runScript() {
    local script="$1"
    shift
    run bash "${REPO_ROOT}/lib/${script}" "$@"
}

assertStatus() { # <expected> [message]
    if [ "${RUN_STATUS}" != "$1" ]; then
        failTest "${2:-unexpected exit status}
  expected status: $1
    actual status: ${RUN_STATUS}
    output: ${RUN_OUTPUT}"
    fi
}

assertEquals() { # <expected> <actual> [message]
    if [ "$1" != "$2" ]; then
        failTest "${3:-value mismatch}
  expected: $1
    actual: $2"
    fi
}

assertContains() { # <haystack> <needle> [message]
    case "$1" in
        *"$2"*) return 0 ;;
    esac
    failTest "${3:-missing expected text}
  looked for: $2
          in: $1"
}

assertNotContains() { # <haystack> <needle> [message]
    case "$1" in
        *"$2"*)
            failTest "${3:-found text that should be absent}
  should not contain: $2
                  in: $1"
            ;;
    esac
    return 0
}

assertOutputContains() { # <needle> [message]
    assertContains "${RUN_OUTPUT}" "$1" "${2:-script output is missing expected text}"
}

assertOutputNotContains() { # <needle> [message]
    assertNotContains "${RUN_OUTPUT}" "$1" "${2:-script output contains text that should be absent}"
}

assertFileContains() { # <file> <needle> [message]
    [ -f "$1" ] || failTest "no such file: $1"
    assertContains "$(cat "$1")" "$2" "${3:-$1 is missing expected text}"
}

assertFileNotContains() { # <file> <needle> [message]
    [ -f "$1" ] || failTest "no such file: $1"
    assertNotContains "$(cat "$1")" "$2" "${3:-$1 contains text that should be absent}"
}

# assertOccurrences <file> <fixed-string> <count> - guards against a splice that duplicates the
# block it was supposed to replace, which reads fine line by line and is wrong as a whole. Counts
# occurrences rather than matching lines (grep -c would report two on one line as one).
assertOccurrences() { # <file> <needle> <expected-count> [message]
    local actual
    actual=$(grep -oF -- "$2" "$1" | grep -c . || true)
    if [ "${actual}" != "$3" ]; then
        failTest "${4:-wrong number of occurrences}
  looked for: $2
          in: $1
    expected: $3
      actual: ${actual}
$(cat "$1")"
    fi
}

requireCommand() { # <command> - skip the test when a tool this environment lacks is needed
    command -v "$1" >/dev/null 2>&1 || skip "$1 is not installed"
}

# requireBashVersion <major> - skip when the bash that will run the script under test is too old.
# Checks the `bash` on PATH, which is what runScript invokes, rather than the one running the suite:
# on macOS those are often different versions.
requireBashVersion() { # <major>
    local major
    major=$(bash -c 'printf %s "${BASH_VERSINFO[0]}"')
    if [ "${major}" -lt "$1" ]; then
        skip "the bash on PATH is ${major}.x, this test needs bash $1+"
    fi
}

# fakeRepositoryRoot - the minimum tree the sync script accepts as a checkout: resources/,
# lib/DumpLocale.java, an origin remote to resolve a repository from, and a clean main branch, since
# the script refuses to run anywhere else. Used instead of the real repository so a test can drive
# the script's full non-dry-run path without the real checkout being touched.
fakeRepositoryRoot() {
    # In a subdirectory, not $TEST_DIR itself: the stubs below live next to it, and an untracked file
    # inside the checkout would trip the script's own clean-tree check.
    mkdir -p "${TEST_DIR}/checkout"
    cd "${TEST_DIR}/checkout" || failTest "could not enter the fake checkout"
    mkdir -p resources lib
    : >lib/DumpLocale.java
    : >resources/PhoneNumberMetadata.xml
    git init --quiet .
    git checkout --quiet -b main 2>/dev/null || true
    git remote add origin "https://github.com/someone/their-fork.git"
    git add -A
    git -c user.email="test@example.com" -c user.name="test" commit --quiet -m "fake checkout"
}

# stubNetwork <compare-api-json> - puts a fake curl and a fake git on PATH, so a test can drive the
# sync script's gates with a given upstream diff and no network at all. The fake curl answers the
# compare call and refuses anything else; the fake git passes everything through to the real one
# except `clone`, which would otherwise fetch the whole upstream repository.
stubNetwork() {
    local realGit
    realGit=$(command -v git)
    mkdir -p "${TEST_DIR}/bin"
    printf '%s' "$1" >"${TEST_DIR}/compare.json"

    cat >"${TEST_DIR}/bin/curl" <<'CURL'
#! /bin/bash
for arg in "$@"; do
    case "${arg}" in
        *"/compare/"*)
            cat "${TEST_DIR}/compare.json"
            exit 0
            ;;
        *"/pulls?"*)
            # No metadata PR open: the run is opening the first one rather than refreshing it.
            printf '[]'
            exit 0
            ;;
    esac
done
echo "fake curl: unexpected request: $*" >&2
exit 7
CURL

    cat >"${TEST_DIR}/bin/git" <<GIT
#! /bin/bash
for arg in "\$@"; do
    if [ "\${arg}" = "clone" ]; then
        echo "fake git: refusing to clone upstream in a test" >&2
        exit 90
    fi
done
exec "${realGit}" "\$@"
GIT

    chmod +x "${TEST_DIR}/bin/curl" "${TEST_DIR}/bin/git"
    PATH="${TEST_DIR}/bin:${PATH}"
    export PATH
}

# compareResponse <filename>... - a compare api response listing exactly these changed files.
compareResponse() {
    local json="" name
    for name in "$@"; do
        [ -z "${json}" ] || json="${json},"
        json="${json}{\"filename\":\"${name}\"}"
    done
    printf '{"files":[%s]}' "${json}"
}
