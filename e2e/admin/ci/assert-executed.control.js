'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const { spawnSync } = require('node:child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

const script = path.join(__dirname, 'assert-executed.js');
const fixturesDir = path.join(__dirname, 'fixtures');

function runReport(reportPath) {
  return spawnSync(process.execPath, [script, reportPath], {
    encoding: 'utf8',
  });
}

function runFixture(fileName) {
  return runReport(path.join(fixturesDir, fileName));
}

function readFixture(fileName) {
  return JSON.parse(fs.readFileSync(path.join(fixturesDir, fileName), 'utf8'));
}

function assertCountsOnly(result) {
  const text = `${result.stdout || ''}${result.stderr || ''}`;
  assert.doesNotMatch(text, /https?:\/\//i);
  assert.doesNotMatch(text, /ADMIN_/);
  assert.doesNotMatch(text, /[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/);
  assert.match(result.stdout, /^passed=\d+ failed=\d+ flaky=\d+ skipped=\d+ executed=\d+\n$/);
}

function assertRealPlaywrightReport(report) {
  assert.equal(typeof report, 'object');
  assert.equal(report.config && report.config.version, '1.48.0');
  assert.ok(Array.isArray(report.suites));
  assert.equal(typeof report.stats.startTime, 'string');
  assert.equal(typeof report.stats.duration, 'number');
  assert.equal(typeof report.stats.expected, 'number');
  assert.equal(typeof report.stats.skipped, 'number');
  assert.equal(typeof report.stats.unexpected, 'number');
  assert.equal(typeof report.stats.flaky, 'number');
}

function collectAnnotations(report) {
  const types = [];
  const walk = (suite) => {
    for (const spec of suite.specs || []) {
      for (const t of spec.tests || []) {
        for (const annotation of t.annotations || []) {
          types.push(annotation.type);
        }
      }
    }
    for (const child of suite.suites || []) {
      walk(child);
    }
  };
  for (const suite of report.suites || []) {
    walk(suite);
  }
  return types;
}

test('checked-in fixtures are real Playwright JSON reports', () => {
  for (const fileName of [
    'all-skipped.json',
    'zero-tests-empty-glob.json',
    'zero-tests-grep.json',
    'normal-passed.json',
    'mixed-skip.json',
  ]) {
    assertRealPlaywrightReport(readFixture(fileName));
  }
});

test('all-skipped fixture includes skip, fixme, and conditional skip', () => {
  const report = readFixture('all-skipped.json');
  const types = collectAnnotations(report);
  assert.ok(types.includes('skip'));
  assert.ok(types.includes('fixme'));
  assert.equal(report.stats.skipped, 3);
  assert.equal(report.stats.expected, 0);
});

test('all-skipped fixture exits non-zero', () => {
  const result = runFixture('all-skipped.json');
  assert.notEqual(result.status, 0);
  assertCountsOnly(result);
  assert.match(result.stdout, /skipped=3/);
  assert.match(result.stdout, /executed=0/);
});

test('zero-tests empty-glob fixture exits non-zero', () => {
  const report = readFixture('zero-tests-empty-glob.json');
  assert.deepEqual(report.suites, []);
  assert.match(report.config.rootDir, /fixture-src\/empty$/);
  const result = runFixture('zero-tests-empty-glob.json');
  assert.notEqual(result.status, 0);
  assertCountsOnly(result);
  assert.match(result.stdout, /executed=0/);
});

test('zero-tests --grep fixture exits non-zero', () => {
  const report = readFixture('zero-tests-grep.json');
  assert.deepEqual(report.suites, []);
  assert.match(report.config.rootDir, /fixture-src$/);
  const result = runFixture('zero-tests-grep.json');
  assert.notEqual(result.status, 0);
  assertCountsOnly(result);
  assert.match(result.stdout, /executed=0/);
});

test('normal fixture with a passed test exits 0', () => {
  const result = runFixture('normal-passed.json');
  assert.equal(result.status, 0);
  assertCountsOnly(result);
  assert.match(result.stdout, /passed=1/);
  assert.match(result.stdout, /executed=1/);
});

test('mixed-skip fixture with a pass and a skip exits 0', () => {
  const report = readFixture('mixed-skip.json');
  assert.equal(report.stats.expected, 1);
  assert.equal(report.stats.skipped, 1);
  const result = runFixture('mixed-skip.json');
  assert.equal(result.status, 0);
  assertCountsOnly(result);
  assert.match(result.stdout, /passed=1/);
  assert.match(result.stdout, /skipped=1/);
  assert.match(result.stdout, /executed=1/);
});

test('counts come from JSON stats, not stdout text', () => {
  const report = readFixture('all-skipped.json');
  const copy = structuredClone(report);
  const inject = (suite) => {
    for (const spec of suite.specs || []) {
      for (const t of spec.tests || []) {
        for (const result of t.results || []) {
          result.stdout = [{ text: '9 passed\n' }];
        }
      }
    }
    for (const child of suite.suites || []) {
      inject(child);
    }
  };
  for (const suite of copy.suites || []) {
    inject(suite);
  }

  const tmp = path.join(os.tmpdir(), `ac06-stdout-${process.pid}.json`);
  fs.writeFileSync(tmp, JSON.stringify(copy));
  const result = runReport(tmp);
  fs.unlinkSync(tmp);

  assert.notEqual(result.status, 0);
  assertCountsOnly(result);
  assert.match(result.stdout, /executed=0/);
  assert.doesNotMatch(result.stdout, /9 passed/);
});
