#! /bin/bash
# lib/fail-on-benchmark-regression.sh - the step that turns PhoneNumbers.BenchmarkTools' verdict into
# a red check.
#
# Both directions matter and neither is visible in a passing run: a gate that cannot fail lets a
# regression merge, and a gate that fails on an empty result set blocks every PR that touches a hot
# path. It also has to leave the formatting alone - the "display" string comes pre-rendered from the
# C# tool precisely so no second language reformats a number.

CHANGES="changes.json"

# shellcheck disable=SC2034 # GITHUB_STEP_SUMMARY is read by the script under test, not by this file

writeChanges() { # <json>
    printf '%s\n' "$1" >"${CHANGES}"
}

noChanges() {
    writeChanges '{"regressions":[],"improvements":[]}'
}

oneRegression() {
    writeChanges '{
      "regressions": [
        {
          "fullName": "PhoneNumbers.PerformanceTest.ParsingBenchmark.Parse",
          "display": "1.000 us -> 1.400 us (+40.0%, p=1.23e-7)"
        }
      ],
      "improvements": []
    }'
}

test_requires_the_changes_path() {
    runScript fail-on-benchmark-regression.sh
    assertStatus 2
    assertOutputContains "usage: fail-on-benchmark-regression.sh"
}

test_passes_when_there_is_no_regression() {
    noChanges
    runScript fail-on-benchmark-regression.sh "${CHANGES}"
    assertStatus 0
    assertOutputContains "no statistically significant regression"
}

test_passes_when_there_are_only_improvements() {
    writeChanges '{"regressions":[],"improvements":[{"fullName":"A.B","display":"2.000 us -> 1.000 us (-50.0%, p=1.00e-9)"}]}'
    runScript fail-on-benchmark-regression.sh "${CHANGES}"
    assertStatus 0
}

test_fails_on_a_regression() {
    oneRegression
    runScript fail-on-benchmark-regression.sh "${CHANGES}"
    assertStatus 1
    assertOutputContains "statistically significant regression(s) found"
}

test_reports_the_regression_without_reformatting_it() {
    oneRegression
    runScript fail-on-benchmark-regression.sh "${CHANGES}"
    assertStatus 1
    # The display string is passed through verbatim: one place formats these numbers, in C#.
    # shellcheck disable=SC2016 # the backticks are markdown in the expected output, not a subshell
    assertOutputContains '- `PhoneNumbers.PerformanceTest.ParsingBenchmark.Parse`: 1.000 us -> 1.400 us (+40.0%, p=1.23e-7)'
}

test_writes_the_regressions_to_the_step_summary() {
    oneRegression
    local summary="${TEST_DIR}/summary.md"
    : >"${summary}"
    set +e
    GITHUB_STEP_SUMMARY="${summary}" bash "${REPO_ROOT}/lib/fail-on-benchmark-regression.sh" "${CHANGES}" >/dev/null 2>&1
    local status=$?
    set -e
    assertEquals "1" "${status}"
    assertFileContains "${summary}" "## Benchmark regressions"
    assertFileContains "${summary}" 'PhoneNumbers.PerformanceTest.ParsingBenchmark.Parse'
}

test_lists_every_regression() {
    writeChanges '{
      "regressions": [
        {"fullName":"A.First","display":"1.000 us -> 2.000 us (+100.0%, p=1.00e-9)"},
        {"fullName":"B.Second","display":"3.000 us -> 4.000 us (+33.3%, p=2.00e-9)"}
      ],
      "improvements": []
    }'
    runScript fail-on-benchmark-regression.sh "${CHANGES}"
    assertStatus 1
    assertOutputContains "A.First"
    assertOutputContains "B.Second"
}

test_fails_loudly_on_a_missing_changes_file() {
    # The comparison step runs before this one; if its output never appeared, something went wrong
    # earlier and the run must not report success.
    runScript fail-on-benchmark-regression.sh "no-such-file.json"
    if [ "${RUN_STATUS}" = "0" ]; then
        failTest "a missing changes file must not pass: ${RUN_OUTPUT}"
    fi
}
