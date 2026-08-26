#!/usr/bin/env node
// Compares BenchmarkDotNet's full-JSON results (Statistics.Mean/StandardDeviation/N, in
// nanoseconds) for the PR branch against the PR base, one matching benchmark case at a time,
// and reports only the ones that moved by a real amount. This exists because posting every
// benchmark's raw numbers on every push floods the PR page and the maintainer's email - see
// run_performance_tests.yml and post_performance_test_comment.yml for how the two halves fit
// together.
//
// A case only counts as "moved" when BOTH hold:
//   - Welch's t-test on the two means (unequal variances allowed, since a slower run is often
//     also a noisier one) rejects the null hypothesis at p < SIGNIFICANCE_LEVEL. This is the
//     same 99.9% confidence BenchmarkDotNet itself uses for the "Error" column it already
//     prints, so the bar here isn't a new number to justify.
//   - The relative change in the mean is at least MIN_RELATIVE_DELTA.
//
// The MIN_RELATIVE_DELTA floor is doing most of the real work here, and it needs to be large.
// Each side of the comparison is ONE process launch (base and branch each run once, in the same
// job - see run_performance_tests.yml), and a launch's reported StandardDeviation only captures
// iteration-to-iteration noise *within* that launch. It says nothing about launch-to-launch drift
// (JIT tiering, scheduler placement, thermal/frequency state), which is what actually separates
// the base launch from the branch launch. Measured directly: three consecutive same-code launches
// of ParsingHelpersBenchmark.ExtractPossibleNumber_CleanInput on one machine ranged from 14.75us
// to 17.54us - a 17% spread from noise alone, while each individual launch's StandardDeviation was
// under 1% of its mean. A significance test built only on within-launch variance is badly
// overconfident against that: the same identical-code pair reached p=1e-26 at a 6% delta. So the
// floor here (20%) is set well above that measured single-launch noise band, not tuned to "feels
// right". If this still produces false positives on the real runner, the fix isn't a bigger floor,
// it's giving BenchmarkDotNet LaunchCount > 1 so launch-to-launch variance is actually measured
// instead of assumed away - that costs proportionally more CI time, which is why it isn't done by
// default today.
//
// Usage: node lib/compare-benchmarks.js <branch-results-dir> <base-results-dir> <output-json-path>

'use strict';

const fs = require('fs');
const path = require('path');

// Both overridable via env for tuning without a code change.
const SIGNIFICANCE_LEVEL = Number(process.env.BENCHMARK_SIGNIFICANCE_LEVEL) || 0.001;
const MIN_RELATIVE_DELTA = Number(process.env.BENCHMARK_MIN_RELATIVE_DELTA) || 0.2;

// --- Student's t-distribution two-tailed p-value, via the regularized incomplete beta
// function. Standard numerical-recipes-style implementation; no external dependencies.

function logGamma(x) {
  const cof = [
    76.18009172947146, -86.50532032941677, 24.01409824083091,
    -1.231739572450155, 0.1208650973866179e-2, -0.5395239384953e-5,
  ];
  let y = x;
  let tmp = x + 5.5;
  tmp -= (x + 0.5) * Math.log(tmp);
  let ser = 1.000000000190015;
  for (let j = 0; j < 6; j++) {
    y += 1;
    ser += cof[j] / y;
  }
  return -tmp + Math.log((2.5066282746310005 * ser) / x);
}

function betacf(x, a, b) {
  const MAXIT = 200;
  const EPS = 3e-14;
  const FPMIN = 1e-300;
  const qab = a + b;
  const qap = a + 1;
  const qam = a - 1;
  let c = 1;
  let d = 1 - (qab * x) / qap;
  if (Math.abs(d) < FPMIN) d = FPMIN;
  d = 1 / d;
  let h = d;
  for (let m = 1; m <= MAXIT; m++) {
    const m2 = 2 * m;
    let aa = (m * (b - m) * x) / ((qam + m2) * (a + m2));
    d = 1 + aa * d;
    if (Math.abs(d) < FPMIN) d = FPMIN;
    c = 1 + aa / c;
    if (Math.abs(c) < FPMIN) c = FPMIN;
    d = 1 / d;
    h *= d * c;
    aa = (-(a + m) * (qab + m) * x) / ((a + m2) * (qap + m2));
    d = 1 + aa * d;
    if (Math.abs(d) < FPMIN) d = FPMIN;
    c = 1 + aa / c;
    if (Math.abs(c) < FPMIN) c = FPMIN;
    d = 1 / d;
    const del = d * c;
    h *= del;
    if (Math.abs(del - 1) < EPS) break;
  }
  return h;
}

function regularizedIncompleteBeta(x, a, b) {
  if (x <= 0) return 0;
  if (x >= 1) return 1;
  const bt = Math.exp(
    logGamma(a + b) - logGamma(a) - logGamma(b) + a * Math.log(x) + b * Math.log(1 - x),
  );
  if (x < (a + 1) / (a + b + 2)) {
    return (bt * betacf(x, a, b)) / a;
  }
  return 1 - (bt * betacf(1 - x, b, a)) / b;
}

function tTestTwoTailedPValue(t, dof) {
  if (!Number.isFinite(t) || !Number.isFinite(dof) || dof <= 0) return 1;
  return regularizedIncompleteBeta(dof / (dof + t * t), dof / 2, 0.5);
}

// Welch's t-test: two-sample, unequal variances, from summary statistics only (no need for
// the raw per-iteration measurements).
function welchTTest(meanA, sdA, nA, meanB, sdB, nB) {
  const varAOverN = (sdA * sdA) / nA;
  const varBOverN = (sdB * sdB) / nB;
  const se = Math.sqrt(varAOverN + varBOverN);
  if (se === 0) {
    return { t: meanA === meanB ? 0 : Infinity, dof: nA + nB - 2 };
  }
  const t = (meanB - meanA) / se;
  const dof =
    Math.pow(varAOverN + varBOverN, 2) /
    (Math.pow(varAOverN, 2) / (nA - 1) + Math.pow(varBOverN, 2) / (nB - 1));
  return { t, dof };
}

// --- BenchmarkDotNet result loading.

function readBenchmarksFromDir(dir) {
  if (!fs.existsSync(dir)) return new Map();
  const byFullName = new Map();
  for (const file of fs.readdirSync(dir)) {
    if (!file.endsWith('-report-full-compressed.json')) continue;
    const report = JSON.parse(fs.readFileSync(path.join(dir, file), 'utf8'));
    for (const b of report.Benchmarks || []) {
      byFullName.set(b.FullName, b);
    }
  }
  return byFullName;
}

function formatDuration(ns) {
  if (ns >= 1e9) return `${(ns / 1e9).toFixed(3)} s`;
  if (ns >= 1e6) return `${(ns / 1e6).toFixed(3)} ms`;
  if (ns >= 1e3) return `${(ns / 1e3).toFixed(3)} us`;
  return `${ns.toFixed(1)} ns`;
}

function main() {
  const [, , branchDir, baseDir, outPath] = process.argv;
  if (!branchDir || !baseDir || !outPath) {
    console.error(
      'usage: node lib/compare-benchmarks.js <branch-results-dir> <base-results-dir> <output-json-path>',
    );
    process.exit(2);
  }

  const branchBenchmarks = readBenchmarksFromDir(branchDir);
  const baseBenchmarks = readBenchmarksFromDir(baseDir);

  const regressions = [];
  const improvements = [];

  for (const [fullName, branch] of branchBenchmarks) {
    const base = baseBenchmarks.get(fullName);
    if (!base) continue; // new benchmark case, nothing to compare against.

    const baseStats = base.Statistics;
    const branchStats = branch.Statistics;
    if (!baseStats || !branchStats || baseStats.N < 2 || branchStats.N < 2) continue;

    const relativeDelta = (branchStats.Mean - baseStats.Mean) / baseStats.Mean;
    if (Math.abs(relativeDelta) < MIN_RELATIVE_DELTA) continue;

    const { t, dof } = welchTTest(
      baseStats.Mean,
      baseStats.StandardDeviation,
      baseStats.N,
      branchStats.Mean,
      branchStats.StandardDeviation,
      branchStats.N,
    );
    const pValue = tTestTwoTailedPValue(t, dof);
    if (pValue >= SIGNIFICANCE_LEVEL) continue;

    const entry = {
      fullName,
      method: branch.Method,
      parameters: branch.Parameters || '',
      baseMean: baseStats.Mean,
      branchMean: branchStats.Mean,
      baseMeanDisplay: formatDuration(baseStats.Mean),
      branchMeanDisplay: formatDuration(branchStats.Mean),
      relativeDeltaPct: relativeDelta * 100,
      pValue,
    };
    (relativeDelta > 0 ? regressions : improvements).push(entry);
  }

  regressions.sort((a, b) => b.relativeDeltaPct - a.relativeDeltaPct);
  improvements.sort((a, b) => a.relativeDeltaPct - b.relativeDeltaPct);

  fs.writeFileSync(outPath, JSON.stringify({ regressions, improvements }, null, 2));

  for (const r of regressions) {
    console.log(
      `REGRESSION: ${r.fullName} ${r.baseMeanDisplay} -> ${r.branchMeanDisplay} ` +
        `(+${r.relativeDeltaPct.toFixed(1)}%, p=${r.pValue.toExponential(2)})`,
    );
  }
  for (const i of improvements) {
    console.log(
      `IMPROVEMENT: ${i.fullName} ${i.baseMeanDisplay} -> ${i.branchMeanDisplay} ` +
        `(${i.relativeDeltaPct.toFixed(1)}%, p=${i.pValue.toExponential(2)})`,
    );
  }
  if (regressions.length === 0 && improvements.length === 0) {
    console.log('no statistically significant change in any benchmark');
  }
}

main();
