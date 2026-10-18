#! /bin/bash
# csharp/PhoneNumbers.BenchmarkTools - the tool that decides whether a PR's benchmark numbers moved.
#
# Tested through its command line rather than from a test project on purpose: the project is kept out
# of the solution (run_performance_tests.yml invokes it with `dotnet run --project`), and the contract
# that matters to the pipeline is exactly this one - argv in, a json artifact and an exit code out,
# with post_performance_test_comment.yml and lib/fail-on-benchmark-regression.sh reading the field
# names. Driving it any other way would test something the pipeline does not do.
#
# Every case below is a pair of summary statistics, because that is all BenchmarkDotNet's full json
# gives the tool. The thresholds under test are the two it applies together: a relative change of at
# least 20% AND Welch's t-test at p < 0.001. Each fixture is chosen to isolate one of them - a 5%
# change that is wildly significant, and a 40% change that is pure noise - so a regression in either
# half shows up as a failing test rather than as a quietly wrong PR comment.
#
# Skipped when there is no .NET SDK on PATH; run_ci_tooling_tests.yml installs one.

TOOL_PROJECT="csharp/PhoneNumbers.BenchmarkTools"

# report <dir> <file-stem> <full-name> <mean-ns> <stddev-ns> <n> - one BenchmarkDotNet-shaped report.
# Only the fields the tool reads are present; it deserializes case-insensitively.
report() {
    local dir="$1" stem="$2" fullName="$3" mean="$4" stddev="$5" n="$6"
    mkdir -p "${dir}"
    cat >"${dir}/${stem}-report-full-compressed.json" <<JSON
{
  "Title": "${stem}",
  "Benchmarks": [
    {
      "FullName": "${fullName}",
      "Method": "${fullName##*.}",
      "Parameters": "",
      "Statistics": { "Mean": ${mean}, "StandardDeviation": ${stddev}, "N": ${n} }
    }
  ]
}
JSON
}

# compare [env-assignments...] - runs the tool over ./branch and ./base into ./changes.json.
compare() {
    requireCommand dotnet
    run env "$@" dotnet run -c Release --project "${REPO_ROOT}/${TOOL_PROJECT}" -- \
        "${TEST_DIR}/branch" "${TEST_DIR}/base" "${TEST_DIR}/changes.json"
}

changes() { # <jq-filter>
    jq -r "$1" "${TEST_DIR}/changes.json"
}

# A pair that is clearly a regression: +40% on tight variance (p is about 1e-44).
regressionPair() {
    report "${TEST_DIR}/base" "Base" "A.B.Parse" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 1400.0 14.0 20
}

test_requires_three_arguments() {
    requireCommand dotnet
    run dotnet run -c Release --project "${REPO_ROOT}/${TOOL_PROJECT}" -- "only-one"
    assertStatus 2
    assertOutputContains "usage: dotnet run --project csharp/PhoneNumbers.BenchmarkTools"
}

test_reports_a_regression() {
    regressionPair
    compare
    assertStatus 0 "the comparison itself never fails the build - that is the shell gate's job"
    assertOutputContains "REGRESSION: A.B.Parse"
    assertEquals "1" "$(changes '.regressions | length')"
    assertEquals "0" "$(changes '.improvements | length')"
    assertEquals "A.B.Parse" "$(changes '.regressions[0].fullName')"
}

test_the_output_uses_the_field_names_its_readers_expect() {
    # post_performance_test_comment.yml's github-script and fail-on-benchmark-regression.sh both read
    # these keys by name, so the camelCase policy is part of the contract.
    regressionPair
    compare
    local keys
    keys=$(changes '.regressions[0] | keys_unsorted | join(",")')
    for key in fullName method parameters baseMean branchMean baseMeanDisplay branchMeanDisplay relativeDeltaPct pValue display; do
        assertContains "${keys}" "${key}" "the output is missing the ${key} field"
    done
}

test_the_display_string_is_fully_rendered() {
    regressionPair
    compare
    local display
    display=$(changes '.regressions[0].display')
    assertContains "${display}" "1.000 us -> 1.400 us"
    assertContains "${display}" "(+40.0%"
    # Lowercase e, a sign, no zero padding - matching what the javascript this replaced produced, so
    # old PR comments and new ones read the same.
    run grep -qE 'p=[0-9]\.[0-9]{2}e-[0-9]+\)$' <<<"${display}"
    assertStatus 0 "unexpected p-value formatting in: ${display}"
}

test_reports_an_improvement() {
    report "${TEST_DIR}/base" "Base" "A.B.Parse" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 600.0 8.0 20
    compare
    assertOutputContains "IMPROVEMENT: A.B.Parse"
    assertEquals "0" "$(changes '.regressions | length')"
    assertEquals "1" "$(changes '.improvements | length')"
    assertContains "$(changes '.improvements[0].display')" "(-40.0%"
}

test_ignores_a_change_below_the_relative_floor() {
    # 5% on tight variance is wildly significant (p is about 3e-18) and still must not be reported:
    # the floor is what absorbs launch-to-launch drift, which the within-launch deviation cannot see.
    report "${TEST_DIR}/base" "Base" "A.B.Parse" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 1050.0 10.0 20
    compare
    assertOutputContains "no statistically significant change in any benchmark"
    assertEquals "0" "$(changes '.regressions | length')"
}

test_ignores_a_large_change_that_is_only_noise() {
    # +40% with a standard deviation near the mean: p is about 0.17, so the t-test rejects it even
    # though the floor is cleared.
    report "${TEST_DIR}/base" "Base" "A.B.Parse" 1000.0 900.0 20
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 1400.0 900.0 20
    compare
    assertOutputContains "no statistically significant change in any benchmark"
    assertEquals "0" "$(changes '.regressions | length')"
}

test_the_thresholds_are_overridable() {
    # Same 5% pair as above, with the floor lowered: the tuning knobs documented in the tool have to
    # actually reach the comparison.
    report "${TEST_DIR}/base" "Base" "A.B.Parse" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 1050.0 10.0 20
    compare BENCHMARK_MIN_RELATIVE_DELTA=0.01
    assertEquals "1" "$(changes '.regressions | length')"
    assertContains "$(changes '.regressions[0].display')" "(+5.0%"
}

test_the_significance_level_is_applied_in_both_directions() {
    # +30% on a standard deviation of 200 gives p of about 3e-5: comfortably past the default 0.001
    # bar, and comfortably short of 1e-9. Asserting both ends is the point - a fixture that the
    # default already rejects would pass this test even if the override were never read.
    report "${TEST_DIR}/base" "Base" "A.B.Parse" 1000.0 200.0 20
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 1300.0 200.0 20

    compare
    assertEquals "1" "$(changes '.regressions | length')" \
        "p is about 3e-5, so the default level of 0.001 must report this"

    compare BENCHMARK_SIGNIFICANCE_LEVEL=1e-9
    assertEquals "0" "$(changes '.regressions | length')" \
        "a level of 1e-9 must filter the same change out"
}

test_skips_a_benchmark_with_too_few_iterations() {
    # N < 2 leaves the variance undefined; comparing it would be arithmetic, not evidence.
    report "${TEST_DIR}/base" "Base" "A.B.Parse" 1000.0 10.0 1
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 1400.0 14.0 1
    compare
    assertEquals "0" "$(changes '.regressions | length')"
}

test_skips_a_benchmark_the_base_does_not_have() {
    # A benchmark added by the PR has nothing to compare against and must not read as a regression.
    report "${TEST_DIR}/base" "Base" "A.B.Parse" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "BranchNew" "A.B.BrandNew" 5000.0 10.0 20
    compare
    assertEquals "0" "$(changes '.regressions | length')"
    assertOutputContains "no statistically significant change in any benchmark"
}

test_ignores_files_that_are_not_full_reports() {
    # BenchmarkDotNet writes several files per run; only the full compressed json has the statistics.
    regressionPair
    mv "${TEST_DIR}/branch/Branch-report-full-compressed.json" "${TEST_DIR}/branch/Branch-report.json"
    compare
    assertEquals "0" "$(changes '.regressions | length')"
}

test_skips_an_unreadable_report_and_keeps_going() {
    # A truncated report must not take down the comparison, and with it the artifact upload that
    # would explain why.
    regressionPair
    report "${TEST_DIR}/branch" "Truncated" "A.B.Other" 1000.0 10.0 20
    printf '{"Benchmarks": [' >"${TEST_DIR}/branch/Truncated-report-full-compressed.json"
    compare
    assertStatus 0
    assertOutputContains "Skipping unreadable benchmark report"
    assertEquals "1" "$(changes '.regressions | length')" \
        "the readable pair still has to be compared"
}

test_handles_a_missing_results_directory() {
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 1400.0 14.0 20
    compare
    assertStatus 0
    assertEquals "0" "$(changes '.regressions | length')"
    assertEquals "0" "$(changes '.improvements | length')"
}

test_formats_each_duration_scale() {
    report "${TEST_DIR}/base" "Ns" "A.Ns" 500.0 5.0 20
    report "${TEST_DIR}/branch" "Ns" "A.Ns" 1500.0 10.0 20
    report "${TEST_DIR}/base" "Ms" "A.Ms" 1500000.0 1000.0 20
    report "${TEST_DIR}/branch" "Ms" "A.Ms" 2500000.0 2000.0 20
    report "${TEST_DIR}/base" "S" "A.S" 1500000000.0 1000000.0 20
    report "${TEST_DIR}/branch" "S" "A.S" 2500000000.0 2000000.0 20
    compare
    assertEquals "3" "$(changes '.regressions | length')"
    assertContains "$(changes '.regressions[] | select(.fullName == "A.Ns") | .display')" "500.0 ns -> 1.500 us"
    assertContains "$(changes '.regressions[] | select(.fullName == "A.Ms") | .display')" "1.500 ms -> 2.500 ms"
    assertContains "$(changes '.regressions[] | select(.fullName == "A.S") | .display')" "1.500 s -> 2.500 s"
}

test_sorts_the_worst_regression_and_the_best_improvement_first() {
    # The PR comment is read top-down, so the ordering is part of the output.
    report "${TEST_DIR}/base" "R1" "A.Smaller" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "R1" "A.Smaller" 1250.0 12.0 20
    report "${TEST_DIR}/base" "R2" "A.Bigger" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "R2" "A.Bigger" 1600.0 15.0 20
    report "${TEST_DIR}/base" "I1" "A.SmallWin" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "I1" "A.SmallWin" 700.0 10.0 20
    report "${TEST_DIR}/base" "I2" "A.BigWin" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "I2" "A.BigWin" 500.0 10.0 20
    compare
    assertEquals "A.Bigger" "$(changes '.regressions[0].fullName')"
    assertEquals "A.Smaller" "$(changes '.regressions[1].fullName')"
    assertEquals "A.BigWin" "$(changes '.improvements[0].fullName')"
    assertEquals "A.SmallWin" "$(changes '.improvements[1].fullName')"
}

test_writes_the_artifact_even_when_nothing_moved() {
    # fail-on-benchmark-regression.sh reads this file unconditionally; a missing one would fail the
    # step with a jq error instead of reporting a clean run.
    report "${TEST_DIR}/base" "Base" "A.B.Parse" 1000.0 10.0 20
    report "${TEST_DIR}/branch" "Branch" "A.B.Parse" 1000.0 10.0 20
    compare
    assertStatus 0
    [ -f "${TEST_DIR}/changes.json" ] || failTest "the changes artifact was not written"
    assertEquals "0" "$(changes '.regressions | length')"
}

test_its_output_feeds_the_regression_gate() {
    # The two halves are developed and tested apart; this is the seam between them.
    regressionPair
    compare
    runScript fail-on-benchmark-regression.sh "${TEST_DIR}/changes.json"
    assertStatus 1
    # shellcheck disable=SC2016 # the backticks are markdown in the expected output, not a subshell
    assertOutputContains '- `A.B.Parse`: 1.000 us -> 1.400 us (+40.0%'
}
