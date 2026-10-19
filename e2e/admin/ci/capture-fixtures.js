#!/usr/bin/env node
'use strict';

/**
 * Capture real Playwright JSON reports for AC-06 fixtures.
 * Prints only scenario names and counts. Never prints env or URL values.
 */

const { spawnSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const ciDir = __dirname;
const e2eRoot = path.join(ciDir, '../..');
const srcDir = path.join(ciDir, 'fixture-src');
const fixturesDir = path.join(ciDir, 'fixtures');
const playwrightBin = path.join(e2eRoot, 'node_modules', '.bin', 'playwright');

const scenarios = [
  {
    name: 'all-skipped',
    output: 'all-skipped.json',
    config: path.join(srcDir, 'playwright.config.ts'),
    args: ['all-skipped.fixture.ts'],
  },
  {
    name: 'zero-tests-empty-glob',
    output: 'zero-tests-empty-glob.json',
    config: path.join(srcDir, 'empty', 'playwright.config.ts'),
    args: [],
  },
  {
    name: 'zero-tests-grep',
    output: 'zero-tests-grep.json',
    config: path.join(srcDir, 'playwright.config.ts'),
    args: ['--grep', 'NO_MATCH_AC06_ZERO_TESTS'],
  },
  {
    name: 'normal-passed',
    output: 'normal-passed.json',
    config: path.join(srcDir, 'playwright.config.ts'),
    args: ['normal-passed.fixture.ts'],
  },
  {
    name: 'mixed-skip',
    output: 'mixed-skip.json',
    config: path.join(srcDir, 'playwright.config.ts'),
    args: ['mixed-skip.fixture.ts'],
  },
  {
    name: 'failed',
    output: 'failed.json',
    config: path.join(srcDir, 'playwright.config.ts'),
    args: ['failed.fixture.ts'],
  },
];

function countsFromReport(report) {
  const stats = report && report.stats ? report.stats : {};
  const passed = Number(stats.expected) || 0;
  const failed = Number(stats.unexpected) || 0;
  const flaky = Number(stats.flaky) || 0;
  const skipped = Number(stats.skipped) || 0;
  return {
    passed,
    failed,
    flaky,
    skipped,
    executed: passed + failed + flaky,
  };
}

function capture(scenario) {
  const dest = path.join(fixturesDir, scenario.output);
  const env = { ...process.env };
  env.CI = '1';
  env.PLAYWRIGHT_JSON_OUTPUT_FILE = dest;

  const result = spawnSync(
    playwrightBin,
    [
      'test',
      '-c',
      scenario.config,
      '--project=chromium',
      '--reporter=json',
      ...scenario.args,
    ],
    {
      cwd: e2eRoot,
      env,
      encoding: 'utf8',
    }
  );

  if (!fs.existsSync(dest)) {
    process.stderr.write(`missing_report name=${scenario.name}\n`);
    process.exit(1);
  }

  let report;
  try {
    report = JSON.parse(fs.readFileSync(dest, 'utf8'));
  } catch {
    process.stderr.write(`invalid_report name=${scenario.name}\n`);
    process.exit(1);
  }

  const counts = countsFromReport(report);
  process.stdout.write(
    `name=${scenario.name} pw_exit=${result.status ?? 1} passed=${counts.passed} failed=${counts.failed} flaky=${counts.flaky} skipped=${counts.skipped} executed=${counts.executed}\n`
  );
}

fs.mkdirSync(fixturesDir, { recursive: true });
for (const scenario of scenarios) {
  capture(scenario);
}
