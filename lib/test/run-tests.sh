#! /bin/bash
# Runs the tests for the bash automation under lib/ and for PhoneNumbers.BenchmarkTools, the two
# parts of this repository's CI tooling that the dotnet test suite does not reach.
#
# Plain bash on purpose: no bats, no npm, nothing to install. The scripts under test are bash and
# jq, so their tests should need exactly what they need - a contributor clones the repo and runs
# this. run_ci_tooling_tests.yml runs the same command.
#
# Each lib/test/test-*.sh file holds functions named test_*; every one runs in its own subshell with
# `set -e`, its own empty working directory, and helpers.sh sourced for assertions. A test that
# needs a tool this machine lacks skips itself rather than failing - see requireCommand.
#
# Usage: lib/test/run-tests.sh [--filter PATTERN] [--verbose]
#
#   --filter PATTERN   Only run tests whose "file::function" name contains PATTERN.
#   --verbose          Print each test's own output, pass or fail.
#
# Deliberately not `set -e`: a failing test must be reported and the rest of the suite still run.
set -uo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
REPO_ROOT=$(cd "${SCRIPT_DIR}/../.." && pwd)
export REPO_ROOT

FILTER=""
VERBOSE=false

usage() {
    cat <<'EOF'
Usage: lib/test/run-tests.sh [--filter PATTERN] [--verbose]

  --filter PATTERN   Only run tests whose "file::function" name contains PATTERN.
  --verbose          Print each test's own output, pass or fail.
  -h, --help         Show this help and exit.
EOF
}

while [ $# -gt 0 ]; do
    case "$1" in
        --filter)
            # A missing or flag-shaped value means the pattern was forgotten; running the whole suite
            # or nothing at all would both look like the filter worked.
            case "${2:-}" in
                "" | -*)
                    echo "error: --filter needs a pattern" >&2
                    usage >&2
                    exit 2
                    ;;
            esac
            FILTER="$2"
            shift
            ;;
        --filter=*)
            FILTER="${1#--filter=}"
            if [ -z "${FILTER}" ]; then
                echo "error: --filter needs a pattern" >&2
                exit 2
            fi
            ;;
        -v | --verbose) VERBOSE=true ;;
        -h | --help)
            usage
            exit 0
            ;;
        *)
            echo "unknown option: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
    shift
done

if [ ! -d "${REPO_ROOT}/lib" ]; then
    echo "error: cannot find lib/ from ${SCRIPT_DIR}" >&2
    exit 1
fi

# Every environment variable the scripts under test read, cleared before a single test runs: each
# test sets what it needs explicitly, so the suite behaves the same on a laptop and on a runner.
# Without this the ambient CI environment leaks in - GITHUB_REPOSITORY makes the sync script resolve
# the real repository instead of the fake checkout's origin remote, DRY_RUN would turn the non-dry
# tests into dry runs, and GITHUB_STEP_SUMMARY collects a dozen fake release errors in the real job
# summary (the two tests that cover summary-writing point it at a file in their own $TEST_DIR).
unset GITHUB_TOKEN GITHUB_REPOSITORY GITHUB_STEP_SUMMARY \
    DRY_RUN SKIP_JAVA_CHECK SKIP_PROTO_CHECK EXPECTED_MAJOR_VERSION \
    UPSTREAM_TAG UPSTREAM_REPOSITORY DEPLOYED_VERSION \
    NUGET_PACKAGE_ID NUGET_EXTENSIONS_PACKAGE_ID PUBLISH_WORKFLOW \
    BENCHMARK_SIGNIFICANCE_LEVEL BENCHMARK_MIN_RELATIVE_DELTA

PASSED=0
FAILED=0
SKIPPED=0
FAILED_NAMES=""

for file in "${SCRIPT_DIR}"/test-*.sh; do
    [ -f "${file}" ] || continue
    fileName=$(basename "${file}" .sh)

    # Enumerated by reading the file rather than by sourcing it and listing functions, so a syntax
    # error in one test file cannot take the runner's own shell with it.
    testNames=$(sed -n 's/^\(test_[A-Za-z0-9_]*\)() *{.*/\1/p' "${file}")

    if [ -z "${testNames}" ]; then
        echo "warning: no test_* functions in ${file}" >&2
        continue
    fi

    # A test the pattern above cannot see would be silently skipped - the worst thing a test suite
    # can do - so count the declarations a looser pattern finds and insist the two agree. The usual
    # cause is a brace on the next line, or an indented definition.
    declaredCount=$(grep -cE '^[[:space:]]*test_[A-Za-z0-9_]*[[:space:]]*\(\)' "${file}" || true)
    runnableCount=$(printf '%s\n' "${testNames}" | grep -c . || true)
    if [ "${declaredCount}" != "${runnableCount}" ]; then
        FAILED=$((FAILED + 1))
        FAILED_NAMES="${FAILED_NAMES}  ${fileName} (unrunnable test declarations)
"
        printf 'FAILED %s: declares %s test_* functions but only %s can be run\n' \
            "${fileName}" "${declaredCount}" "${runnableCount}" >&2
        printf '       write each one as "test_name() {" with the brace on the same line\n' >&2
        continue
    fi

    for testName in ${testNames}; do
        fullName="${fileName}::${testName}"

        if [ -n "${FILTER}" ]; then
            case "${fullName}" in
                *"${FILTER}"*) ;;
                *) continue ;;
            esac
        fi

        TEST_DIR=$(mktemp -d)
        export TEST_DIR
        output=$(
            {
                cd "${TEST_DIR}" || exit 1
                set -e
                # shellcheck source=./helpers.sh
                source "${SCRIPT_DIR}/helpers.sh"
                # shellcheck source=/dev/null
                source "${file}"
                "${testName}"
            } 2>&1
        )
        status=$?
        rm -rf "${TEST_DIR}"

        case "${status}" in
            0)
                PASSED=$((PASSED + 1))
                printf 'ok     %s\n' "${fullName}"
                ;;
            77)
                SKIPPED=$((SKIPPED + 1))
                printf 'skip   %s%s\n' "${fullName}" "$(printf '%s' "${output}" | sed -n 's/^SKIP: / - /p' | head -n 1)"
                ;;
            *)
                FAILED=$((FAILED + 1))
                FAILED_NAMES="${FAILED_NAMES}  ${fullName}
"
                printf 'FAILED %s\n' "${fullName}"
                printf '%s\n' "${output}" | sed 's|^|       |'
                continue
                ;;
        esac

        if ${VERBOSE} && [ -n "${output}" ]; then
            printf '%s\n' "${output}" | sed 's|^|       |'
        fi
    done
done

printf '\n%s passed, %s failed, %s skipped\n' "${PASSED}" "${FAILED}" "${SKIPPED}"

if [ "${FAILED}" -gt 0 ]; then
    printf 'failed tests:\n%s' "${FAILED_NAMES}"
    exit 1
fi

if [ "$((PASSED + SKIPPED))" -eq 0 ]; then
    echo "error: no tests ran" >&2
    exit 1
fi
