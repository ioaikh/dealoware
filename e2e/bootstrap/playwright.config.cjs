"use strict";

const { defineConfig, devices } = require("@playwright/test");
const {
  requireResetConfirmProxy,
  requireResetConfirmBaseUrl
} = require("../reset-confirm/require-proxy.cjs");

// The same guard as the reset-confirm run: a base URL, and a proxy that must
// be http://127.0.0.1:<port 1024-65535>. Errors name this run's variables.
function guard(check, envName) {
  try {
    return check(process.env[envName]);
  } catch (err) {
    throw new Error(String(err.message).replace(/RESET_CONFIRM_[A-Z_]+/g, envName));
  }
}

const baseURL = guard(requireResetConfirmBaseUrl, "BOOTSTRAP_E2E_BASE_URL");
const proxy = guard(requireResetConfirmProxy, "BOOTSTRAP_E2E_PROXY");

module.exports = defineConfig({
  testDir: ".",
  testMatch: "*.spec.ts",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [["list"]],
  timeout: 30_000,
  use: {
    baseURL,
    ignoreHTTPSErrors: true,
    proxy,
    trace: "off",
    screenshot: "off",
    video: "off",
  },
  projects: [
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
        ignoreHTTPSErrors: true,
        proxy,
      },
    },
  ],
  outputDir: "./test-results/",
});
