"use strict";

/**
 * Checks of the shared Playwright configuration (playwright.base.cjs), run by
 * Admin UI CI before any browser starts and by the .NET tests:
 *
 * - it refuses to load without a loopback proxy or without a base URL;
 * - the proxy the guard returns is the one the shared settings and every
 *   engine's project use;
 * - three mutations of the configuration are caught: making the proxy
 *   optional, replacing it with a fixed one, and dropping it.
 *
 * It needs no package: signedOutConfig is handed a stand-in for
 * @playwright/test, so the check runs before `npm ci`.
 */

const assert = require("assert");
const fs = require("fs");
const os = require("os");
const path = require("path");
const { spawnSync } = require("child_process");
const { requireLoopbackProxy } = require("./require-proxy.cjs");

const PREFIX = "CHECK_E2E";
const basePath = path.join(__dirname, "playwright.base.cjs");
const src = fs.readFileSync(basePath, "utf8");

const GUARD_CALL = "guard(requireLoopbackProxy, envPrefix + \"_PROXY\")";
const GOOD = {
  [PREFIX + "_BASE_URL"]: "https://admin.core.dealoware.com",
  [PREFIX + "_PROXY"]: "http://127.0.0.1:18080"
};

/** Loads a copy of the configuration (beside a copy of the guard) in a child process, with these variables. */
function load(source, env) {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "dw-pw-base-"));
  try {
    fs.writeFileSync(path.join(dir, "playwright.base.cjs"), source);
    fs.copyFileSync(path.join(__dirname, "require-proxy.cjs"), path.join(dir, "require-proxy.cjs"));
    const childEnv = Object.assign({}, process.env, env);
    delete childEnv.NODE_PATH;
    const stub = "{ defineConfig: (c) => c, devices: new Proxy({}, { get: () => ({}) }) }";
    return spawnSync(
      process.execPath,
      [
        "-e",
        "const { signedOutConfig } = require(" + JSON.stringify(path.join(dir, "playwright.base.cjs")) + ");"
          + "process.stdout.write(JSON.stringify(signedOutConfig(" + stub + ", " + JSON.stringify(PREFIX) + ")));"
      ],
      { cwd: dir, env: childEnv, encoding: "utf8", timeout: 15000 }
    );
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
}

function loaded(source, label) {
  const result = load(source, GOOD);
  assert.strictEqual(result.status, 0, label + " should load: " + (result.stderr || result.stdout));
  return JSON.parse(result.stdout);
}

/** The good variables with one changed: only that one can be the reason the load fails. */
function assertLoadThrows(source, change, label) {
  const result = load(source, Object.assign({}, GOOD, change));
  assert.notStrictEqual(result.status, 0, label + " should throw at load");
  assert.ok(String(result.stderr).includes(PREFIX + "_"), label + " should name the suite's variable");
}

/** The guard's answer reaches the shared settings and every project. */
function assertProxyEverywhere(config, label) {
  const expected = requireLoopbackProxy(GOOD[PREFIX + "_PROXY"], PREFIX + "_PROXY");
  assert.deepStrictEqual(config.use && config.use.proxy, expected, label + ": use.proxy");
  assert.ok(Array.isArray(config.projects) && config.projects.length === 3, label + ": three projects");
  config.projects.forEach((project, i) => {
    assert.deepStrictEqual(project.use && project.use.proxy, expected, label + ": projects[" + i + "].use.proxy");
  });
  assert.strictEqual(config.use.baseURL, GOOD[PREFIX + "_BASE_URL"], label + ": baseURL");
}

function assertCaught(fn, label) {
  let failed = false;
  try {
    fn();
  } catch (err) {
    failed = true;
  }
  assert.ok(failed, label + " must be caught");
}

assert.ok(src.includes(GUARD_CALL), "the configuration calls the proxy guard at load");
assertProxyEverywhere(loaded(src, "configuration"), "configuration");
assertLoadThrows(src, { [PREFIX + "_PROXY"]: "" }, "missing proxy");
assertLoadThrows(src, { [PREFIX + "_PROXY"]: "http://10.0.0.1:18080" }, "proxy off this machine");
assertLoadThrows(src, { [PREFIX + "_BASE_URL"]: "" }, "missing base URL");

// Mutation: the proxy made optional. The load check catches it.
const optional = src.replace(
  GUARD_CALL,
  "(process.env[envPrefix + \"_PROXY\"] ? " + GUARD_CALL + " : undefined)");
assert.notStrictEqual(optional, src, "the optional-proxy mutation must change the configuration");
assertCaught(() => assertLoadThrows(optional, { [PREFIX + "_PROXY"]: "" }, "optional proxy"), "an optional proxy");

// Mutations: a fixed proxy, and none, where the guard's answer is passed on.
function replaceProxyShorthand(replacement) {
  const next = src.replace(/(\n\s+)proxy,/g, "$1" + replacement);
  assert.notStrictEqual(next, src, "the proxy shorthand must be there to mutate");
  return next;
}
assertCaught(
  () => assertProxyEverywhere(loaded(replaceProxyShorthand("proxy: { server: \"http://127.0.0.1:9999\" },"), "fixed"), "fixed"),
  "a fixed proxy");
assertCaught(() => assertProxyEverywhere(loaded(replaceProxyShorthand(""), "dropped"), "dropped"), "a dropped proxy");

process.stdout.write("PASS playwright.base load checks\n");
