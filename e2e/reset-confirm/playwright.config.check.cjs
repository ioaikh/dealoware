"use strict";

const assert = require("assert");
const fs = require("fs");
const os = require("os");
const path = require("path");
const { spawnSync } = require("child_process");
const { requireResetConfirmProxy } = require("./require-proxy.cjs");

const configPath = path.join(__dirname, "playwright.config.cjs");
const src = fs.readFileSync(configPath, "utf8");

const UNCONDITIONAL =
  "requireResetConfirmProxy(process.env.RESET_CONFIRM_PROXY)";
const M20_PROXY = { server: "http://127.0.0.1:9999" };

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

function spawnConfig(env, configFile, evalSource) {
  const childEnv = Object.assign({}, process.env);
  delete childEnv.NODE_PATH;
  childEnv.RESET_CONFIRM_BASE_URL = env.RESET_CONFIRM_BASE_URL;
  childEnv.RESET_CONFIRM_PROXY = env.RESET_CONFIRM_PROXY;
  return spawnSync(
    process.execPath,
    ["-e", evalSource],
    {
      cwd: path.dirname(configFile),
      env: childEnv,
      encoding: "utf8",
      timeout: 15000
    }
  );
}

function loadConfig(env, configFile) {
  return spawnConfig(
    env,
    configFile,
    "require(" + JSON.stringify(configFile) + ")"
  );
}

function exportStubbedConfig(env, configFile) {
  return spawnConfig(
    env,
    configFile,
    "process.stdout.write(JSON.stringify(require(" + JSON.stringify(configFile) + ")))"
  );
}

function assertLoadThrows(env, label, configFile) {
  const result = loadConfig(env, configFile);
  assert.notStrictEqual(result.status, 0, label + " should throw at load");
  const text = String(result.stderr || "") + String(result.stdout || "");
  assert.ok(text.length > 0, label + " should print an error");
}

function assertGuardResultReachesProxySettings(config, env, label) {
  const expected = requireResetConfirmProxy(env.RESET_CONFIRM_PROXY);
  assert.ok(config && config.use, label + ": exported config must have use");
  assert.ok(config.use.proxy, label + ": use.proxy must be set");
  assert.strictEqual(
    config.use.proxy.server,
    expected.server,
    label + ": use.proxy.server must equal requireResetConfirmProxy().server"
  );
  assert.deepStrictEqual(
    config.use.proxy,
    expected,
    label + ": use.proxy must equal requireResetConfirmProxy() return"
  );
  assert.ok(
    Array.isArray(config.projects) && config.projects.length > 0,
    label + ": exported config must have projects"
  );
  config.projects.forEach(function (project, i) {
    const name = label + ": projects[" + i + "]";
    assert.ok(project && project.use, name + ".use must be set");
    assert.deepStrictEqual(
      project.use.proxy,
      expected,
      name + ".use.proxy must equal requireResetConfirmProxy() return"
    );
  });
}

function parseStubbedExport(result, label) {
  assert.strictEqual(
    result.status,
    0,
    label + " should load via stubbed defineConfig: " + (result.stderr || result.stdout)
  );
  assert.ok(result.stdout, label + ": stubbed export must write JSON");
  return JSON.parse(result.stdout);
}

const goodEnv = {
  RESET_CONFIRM_BASE_URL: "https://admin.core.dealoware.com",
  RESET_CONFIRM_PROXY: "http://127.0.0.1:18080"
};

withStubbedPlaywright(src, (cfg) => {
  const exported = parseStubbedExport(
    exportStubbedConfig(goodEnv, cfg),
    "valid env"
  );
  assertGuardResultReachesProxySettings(exported, goodEnv, "playwright.config.cjs");
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

function replaceUseProxyShorthand(source, replacement) {
  const next = source.replace(/(\n\s+)proxy,/g, "$1" + replacement);
  assert.notStrictEqual(next, source, "proxy shorthand sites must change");
  assert.ok(next.includes(UNCONDITIONAL), "guard call must remain");
  return next;
}

function assertWiringMutationFailsEquality(mutatedSource, label) {
  assertConfigCallsGuardAtLoad(mutatedSource, label);
  withStubbedPlaywright(mutatedSource, (cfg) => {
    const exported = parseStubbedExport(
      exportStubbedConfig(goodEnv, cfg),
      label
    );
    let equalityFailed = false;
    try {
      assertGuardResultReachesProxySettings(exported, goodEnv, label);
    } catch (err) {
      equalityFailed = true;
      assert.ok(err, label + " must fail exported-proxy equality");
    }
    assert.ok(equalityFailed, label + " must fail the guard-result wiring check");
  });
}

const m20 = replaceUseProxyShorthand(
  src,
  "proxy: " + JSON.stringify(M20_PROXY) + ","
);
assertWiringMutationFailsEquality(m20, "M20");

const dropResult = replaceUseProxyShorthand(src, "");
assertWiringMutationFailsEquality(dropResult, "drop-result");

process.stdout.write("PASS playwright.config load checks\n");
