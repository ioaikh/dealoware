"use strict";

const assert = require("assert");
const fs = require("fs");
const os = require("os");
const path = require("path");
const { spawnSync } = require("child_process");

const configPath = path.join(__dirname, "playwright.config.cjs");
const src = fs.readFileSync(configPath, "utf8");

const UNCONDITIONAL =
  "requireResetConfirmProxy(process.env.RESET_CONFIRM_PROXY)";

function assertConfigCallsGuardAtLoad(source, label) {
  assert.ok(
    source.includes(UNCONDITIONAL),
    label + ": config must call requireResetConfirmProxy at load"
  );
  assert.ok(
    source.includes("requireResetConfirmBaseUrl(process.env.RESET_CONFIRM_BASE_URL)"),
    label + ": config must call requireResetConfirmBaseUrl at load"
  );
  assert.ok(
    !/RESET_CONFIRM_PROXY\s*\?/.test(source),
    label + ": config must not make RESET_CONFIRM_PROXY optional"
  );
}

assertConfigCallsGuardAtLoad(src, "playwright.config.cjs");

const stubIndex = [
  "\"use strict\";",
  "module.exports = {",
  "  defineConfig(config) { return config; },",
  "  devices: { \"Desktop Chrome\": {} }",
  "};",
  ""
].join("\n");

function withStubbedPlaywright(configSource, fn) {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "dw-pw-stub-"));
  const pkg = path.join(dir, "node_modules", "@playwright", "test");
  fs.mkdirSync(pkg, { recursive: true });
  fs.writeFileSync(
    path.join(pkg, "package.json"),
    JSON.stringify({ name: "@playwright/test", main: "index.js" })
  );
  fs.writeFileSync(path.join(pkg, "index.js"), stubIndex);
  fs.copyFileSync(
    path.join(__dirname, "require-proxy.cjs"),
    path.join(dir, "require-proxy.cjs")
  );
  const cfg = path.join(dir, "playwright.config.cjs");
  fs.writeFileSync(cfg, configSource);
  try {
    return fn(cfg, dir);
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
}

function loadConfig(env, configFile) {
  const childEnv = Object.assign({}, process.env);
  delete childEnv.NODE_PATH;
  childEnv.RESET_CONFIRM_BASE_URL = env.RESET_CONFIRM_BASE_URL;
  childEnv.RESET_CONFIRM_PROXY = env.RESET_CONFIRM_PROXY;
  return spawnSync(
    process.execPath,
    ["-e", "require(" + JSON.stringify(configFile) + ")"],
    {
      cwd: path.dirname(configFile),
      env: childEnv,
      encoding: "utf8",
      timeout: 15000
    }
  );
}

function assertLoadThrows(env, label, configFile) {
  const result = loadConfig(env, configFile);
  assert.notStrictEqual(result.status, 0, label + " should throw at load");
  const text = String(result.stderr || "") + String(result.stdout || "");
  assert.ok(text.length > 0, label + " should print an error");
}

const goodEnv = {
  RESET_CONFIRM_BASE_URL: "https://admin.core.dealoware.com",
  RESET_CONFIRM_PROXY: "http://127.0.0.1:18080"
};

withStubbedPlaywright(src, (cfg) => {
  const ok = loadConfig(goodEnv, cfg);
  assert.strictEqual(
    ok.status,
    0,
    "valid env should load config via stub: " + (ok.stderr || ok.stdout)
  );
  assertLoadThrows(
    { RESET_CONFIRM_BASE_URL: goodEnv.RESET_CONFIRM_BASE_URL, RESET_CONFIRM_PROXY: "" },
    "missing proxy",
    cfg
  );
  assertLoadThrows(
    { RESET_CONFIRM_BASE_URL: "", RESET_CONFIRM_PROXY: goodEnv.RESET_CONFIRM_PROXY },
    "missing base URL",
    cfg
  );
});

const mutated = src.replace(
  UNCONDITIONAL,
  "process.env.RESET_CONFIRM_PROXY ? requireResetConfirmProxy(process.env.RESET_CONFIRM_PROXY) : undefined"
);
assert.notStrictEqual(mutated, src, "M15 mutation must change the config");

let staticMutationFailed = false;
try {
  assertConfigCallsGuardAtLoad(mutated, "M15");
} catch (err) {
  staticMutationFailed = true;
  assert.ok(err, "M15 optional proxy must fail the load-site pin");
}
assert.ok(staticMutationFailed, "making the proxy optional must fail the text pin");

withStubbedPlaywright(mutated, (cfg) => {
  let loadMutationFailed = false;
  try {
    assertLoadThrows(
      {
        RESET_CONFIRM_BASE_URL: goodEnv.RESET_CONFIRM_BASE_URL,
        RESET_CONFIRM_PROXY: ""
      },
      "M15 optional proxy",
      cfg
    );
  } catch (err) {
    loadMutationFailed = true;
    assert.ok(err, "M15 optional proxy must fail the required-proxy load check");
  }
  assert.ok(loadMutationFailed, "making the proxy optional must fail the behaviour check");
});

process.stdout.write("PASS playwright.config load checks\n");
