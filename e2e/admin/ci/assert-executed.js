#!/usr/bin/env node
'use strict';

/**
 * AC-06: SKIP is not PASS.
 * Reads Playwright JSON reporter output (never scrapes stdout).
 * Exit 1 unless executed count (passed + failed + flaky) is greater than 0.
 * Covers all-skipped (skip / fixme / conditional skip) and zero collected.
 * Prints only counts. Never prints env, URLs, or report contents.
 */

const fs = require('fs');

function num(value) {
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
}

function emptyCounts() {
  return { passed: 0, failed: 0, flaky: 0, skipped: 0 };
}

function printCounts(counts) {
  const executed = counts.passed + counts.failed + counts.flaky;
  process.stdout.write(
    `passed=${counts.passed} failed=${counts.failed} flaky=${counts.flaky} skipped=${counts.skipped} executed=${executed}\n`
  );
  return executed;
}

function walkSuite(suite, counts) {
  if (!suite || typeof suite !== 'object') {
    return;
  }
  for (const spec of suite.specs || []) {
    if (!spec || typeof spec !== 'object') {
      continue;
    }
    for (const t of spec.tests || []) {
      if (!t || typeof t !== 'object') {
        continue;
      }
      switch (t.status) {
        case 'expected':
          counts.passed += 1;
          break;
        case 'unexpected':
          counts.failed += 1;
          break;
        case 'flaky':
          counts.flaky += 1;
          break;
        case 'skipped':
          counts.skipped += 1;
          break;
        default:
          break;
      }
    }
  }
  for (const child of suite.suites || []) {
    walkSuite(child, counts);
  }
}

function countsFromReport(report) {
  const counts = emptyCounts();
  if (!report || typeof report !== 'object') {
    return counts;
  }

  const stats = report.stats;
  if (stats && typeof stats === 'object') {
    const hasOfficial =
      'expected' in stats || 'unexpected' in stats || 'flaky' in stats || 'skipped' in stats;
    if (hasOfficial) {
      counts.passed = num(stats.expected);
      counts.failed = num(stats.unexpected);
      counts.flaky = num(stats.flaky);
      counts.skipped = num(stats.skipped);
      return counts;
    }
  }

  for (const suite of report.suites || []) {
    walkSuite(suite, counts);
  }
  return counts;
}

function main() {
  const reportPath = process.argv[2];
  const counts = emptyCounts();

  if (!reportPath) {
    const executed = printCounts(counts);
    process.exit(executed > 0 ? 0 : 1);
  }

  let raw;
  try {
    raw = fs.readFileSync(reportPath, 'utf8');
  } catch {
    const executed = printCounts(counts);
    process.exit(executed > 0 ? 0 : 1);
  }

  let report;
  try {
    report = JSON.parse(raw);
  } catch {
    const executed = printCounts(counts);
    process.exit(executed > 0 ? 0 : 1);
  }

  const executed = printCounts(countsFromReport(report));
  process.exit(executed > 0 ? 0 : 1);
}

main();
