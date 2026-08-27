#!/usr/bin/env node
// Shared by compare-benchmarks.js and fail-on-benchmark-regression.js so the
// "base -> branch (+X.X%, p=...)" text can't drift out of sync between the two.
'use strict';

function formatBenchmarkDelta(entry, { showPlusSign = false } = {}) {
  const sign = showPlusSign && entry.relativeDeltaPct > 0 ? '+' : '';
  return (
    `${entry.baseMeanDisplay} -> ${entry.branchMeanDisplay} ` +
    `(${sign}${entry.relativeDeltaPct.toFixed(1)}%, p=${entry.pValue.toExponential(2)})`
  );
}

module.exports = { formatBenchmarkDelta };
