#!/usr/bin/env node
// Fails (exit 1) when lib/compare-benchmarks.js found a statistically significant regression.
// Kept as a separate step/script from the comparison itself so the benchmark artifact upload -
// including this file's input - still happens even when this step fails; see the ordering note
// in run_performance_tests.yml next to "Fail on benchmarks that produced no result", which does
// the same thing for a different check.
//
// Usage: node lib/fail-on-benchmark-regression.js <significant-changes-json-path>

'use strict';

const fs = require('fs');
const { formatBenchmarkDelta } = require('./format-benchmark-change');

const [, , changesPath] = process.argv;
if (!changesPath) {
  console.error('usage: node lib/fail-on-benchmark-regression.js <significant-changes-json-path>');
  process.exit(2);
}

const { regressions } = JSON.parse(fs.readFileSync(changesPath, 'utf8'));

if (regressions.length === 0) {
  console.log('no statistically significant regression');
  process.exit(0);
}

const lines = regressions.map(
  (r) => `- \`${r.fullName}\`: ${formatBenchmarkDelta(r, { showPlusSign: true })}`,
);

const summaryPath = process.env.GITHUB_STEP_SUMMARY;
if (summaryPath) {
  fs.appendFileSync(summaryPath, ['## Benchmark regressions', ...lines, ''].join('\n'));
}

console.error('statistically significant regression(s) found:');
console.error(lines.join('\n'));
process.exit(1);
