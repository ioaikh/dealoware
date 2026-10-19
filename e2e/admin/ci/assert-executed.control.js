'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const { spawnSync } = require('node:child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

const script = path.join(__dirname, 'assert-executed.js');
const fixturesDir = path.join(__dirname, 'fixtures');
const e2eRoot = path.join(__dirname, '../..');
const srcDir = path.join(__dirname, 'fixture-src');
const playwrightBin = path.join(e2eRoot, 'node_modules', '.bin', 'playwright');

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

function assertSafeNonZero(result, forbidden = []) {
  assert.equal(result.error, undefined);
  assert.equal(result.signal, null);
  assert.notEqual(result.status, 0);
  assert.equal(typeof result.status, 'number');
  assertCountsOnly(result);
  const text = `${result.stdout || ''}${result.stderr || ''}`;
  assert.doesNotMatch(text, /Error:/);
  assert.doesNotMatch(text, /SyntaxError/);
  assert.doesNotMatch(text, /at Object\./);
  for (const token of forbidden) {
    assert.doesNotMatch(text, new RegExp(token.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')));
  }
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
    'failed.json',
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

test('failed spec makes Playwright exit non-zero and guard reports failed>=1', () => {
  const dest = path.join(os.tmpdir(), `ac06-failed-${process.pid}.json`);
  const pw = spawnSync(
    playwrightBin,
    [
      'test',
      '-c',
      path.join(srcDir, 'playwright.config.ts'),
      '--project=chromium',
      '--reporter=json',
      'failed.fixture.ts',
    ],
    {
      cwd: e2eRoot,
      encoding: 'utf8',
      env: {
        ...process.env,
        CI: '1',
        PLAYWRIGHT_JSON_OUTPUT_FILE: dest,
      },
    }
  );
  assert.equal(pw.error, undefined);
  assert.notEqual(pw.status, 0);

  const report = readFixture('failed.json');
  assertRealPlaywrightReport(report);
  assert.ok(report.stats.unexpected >= 1);

  const result = runFixture('failed.json');
  assert.equal(result.error, undefined);
  assert.equal(result.signal, null);
  assert.equal(result.status, 0);
  assertCountsOnly(result);
  const failed = Number((/failed=(\d+)/.exec(result.stdout) || [])[1]);
  assert.ok(failed >= 1);
  assert.match(result.stdout, /executed=[1-9]/);

  if (fs.existsSync(dest)) {
    fs.unlinkSync(dest);
  }
});

test('missing report path exits non-zero without crashing or echoing contents', () => {
  const missing = path.join(os.tmpdir(), `ac06-missing-${process.pid}-no-such-file.json`);
  const result = runReport(missing);
  assertSafeNonZero(result, [missing, 'ac06-missing']);
});

test('truncated JSON exits non-zero without crashing or echoing contents', () => {
  const marker = 'AC06_TRUNCATED_MARKER_DO_NOT_ECHO';
  const tmp = path.join(os.tmpdir(), `ac06-truncated-${process.pid}.json`);
  fs.writeFileSync(tmp, `{ "stats": { "expected": 1, "unexpected": ${marker}`);
  const result = runReport(tmp);
  fs.unlinkSync(tmp);
  assertSafeNonZero(result, [marker]);
});

test('non-JSON content exits non-zero without crashing or echoing contents', () => {
  const marker = 'AC06_NONJSON_MARKER_DO_NOT_ECHO';
  const tmp = path.join(os.tmpdir(), `ac06-nonjson-${process.pid}.txt`);
  fs.writeFileSync(tmp, `not-json ${marker}\n`);
  const result = runReport(tmp);
  fs.unlinkSync(tmp);
  assertSafeNonZero(result, [marker, 'not-json']);
});

test('no argument exits non-zero without crashing or echoing contents', () => {
  const result = spawnSync(process.execPath, [script], { encoding: 'utf8' });
  assertSafeNonZero(result);
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
