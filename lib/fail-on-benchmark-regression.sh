#! /bin/bash
# Fails (exit 1) when PhoneNumbers.BenchmarkTools found a statistically significant regression.
# Kept as a separate step/script from the comparison itself so the benchmark artifact upload -
# including this file's input - still happens even when this step fails; see the ordering note in
# run_performance_tests.yml next to "Fail on benchmarks that produced no result", which does the
# same thing for a different check.
#
# Usage: fail-on-benchmark-regression.sh <significant-changes-json-path>
set -euo pipefail

if [ "$#" -lt 1 ]; then
    echo "usage: fail-on-benchmark-regression.sh <significant-changes-json-path>" >&2
    exit 2
fi

CHANGES_PATH="$1"

REGRESSION_COUNT=$(jq '.regressions | length' "${CHANGES_PATH}")

if [ "${REGRESSION_COUNT}" -eq 0 ]; then
    echo "no statistically significant regression"
    exit 0
fi

# The "display" field is already fully formatted by PhoneNumbers.BenchmarkTools - nothing here
# reformats a number, so there is only ever one place that can get that formatting wrong.
LINES=$(jq -r '.regressions[] | "- `" + .fullName + "`: " + .display' "${CHANGES_PATH}")

if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
    {
        echo "## Benchmark regressions"
        echo "${LINES}"
        echo
    } >>"${GITHUB_STEP_SUMMARY}"
fi

echo "statistically significant regression(s) found:" >&2
echo "${LINES}" >&2
exit 1
