"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const { spawnSync } = require("child_process");

const configPath = path.join(__dirname, "playwright.config.cjs");
const src = fs.readFileSync(configPath, "utf8");
assert.ok(
  src.includes("requireResetConfirmProxy(process.env.RESET_CONFIRM_PROXY)"),
  "config must call requireResetConfirmProxy at load"
);
assert.ok(
  !/RESET_CONFIRM_PROXY\s*\?/.test(src),
  "config must not make RESET_CONFIRM_PROXY optional"
);

function loadConfig(env, filePath) {
  const result = spawnSync(
    process.execPath,
    ["-e", "require(" + JSON.stringify(filePath || configPath) + ")"],
    {
      cwd: __dirname,
      env: Object.assign({}, process.env, {
        RESET_CONFIRM_BASE_URL: env.RESET_CONFIRM_BASE_URL,
        RESET_CONFIRM_PROXY: env.RESET_CONFIRM_PROXY
      }),
      encoding: "utf8",
      timeout: 15000
    }
  );
  return result;
}

function assertLoadThrows(env, label, filePath) {
  const result = loadConfig(env, filePath);
  assert.notStrictEqual(result.status, 0, label + " should throw at load");
  const text = String(result.stderr || "") + String(result.stdout || "");
  assert.ok(text.length > 0, label + " should print an error");
}

const goodEnv = {
  RESET_CONFIRM_BASE_URL: "https://admin.core.dealoware.com",
  RESET_CONFIRM_PROXY: "http://127.0.0.1:18080"
};

const ok = loadConfig(goodEnv);
assert.strictEqual(ok.status, 0, "valid env should load config: " + (ok.stderr || ok.stdout));

assertLoadThrows(
  { RESET_CONFIRM_BASE_URL: goodEnv.RESET_CONFIRM_BASE_URL, RESET_CONFIRM_PROXY: "" },
  "missing proxy"
);
assertLoadThrows(
  { RESET_CONFIRM_BASE_URL: "", RESET_CONFIRM_PROXY: goodEnv.RESET_CONFIRM_PROXY },
  "missing base URL"
);

const mutated = src.replace(
  "requireResetConfirmProxy(process.env.RESET_CONFIRM_PROXY)",
  "process.env.RESET_CONFIRM_PROXY ? requireResetConfirmProxy(process.env.RESET_CONFIRM_PROXY) : undefined"
);
assert.notStrictEqual(mutated, src, "M15 mutation must change the config");

const mutatedPath = path.join(__dirname, "playwright.config.m15-tmp.cjs");
fs.writeFileSync(mutatedPath, mutated);
let mutationFailedTheCheck = false;
try {
  assertLoadThrows(
    {
      RESET_CONFIRM_BASE_URL: goodEnv.RESET_CONFIRM_BASE_URL,
      RESET_CONFIRM_PROXY: ""
    },
    "M15 optional proxy",
    mutatedPath
  );
} catch (err) {
  mutationFailedTheCheck = true;
  assert.ok(err, "M15 optional proxy must fail the required-proxy load check");
} finally {
  fs.unlinkSync(mutatedPath);
}
assert.ok(mutationFailedTheCheck, "making the proxy optional must fail the behaviour check");

process.stdout.write("PASS playwright.config load checks\n");
