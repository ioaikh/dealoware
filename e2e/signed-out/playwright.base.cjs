"use strict";

const {
  requireResetConfirmProxy,
  requireResetConfirmBaseUrl
} = require("../reset-confirm/require-proxy.cjs");

// The same guard as the reset-confirm run: a base URL, and a proxy that must
// be http://127.0.0.1:<port 1024-65535>. Errors name the suite's variables.
function guard(check, envName) {
  try {
    return check(process.env[envName]);
  } catch (err) {
    throw new Error(String(err.message).replace(/RESET_CONFIRM_[A-Z_]+/g, envName));
  }
}

/**
 * The Playwright configuration every signed-out admin page suite runs with
 * (e2e/bootstrap, e2e/sign-in): one worker, Chromium, the real admin origin
 * reached through the loopback proxy that e2e/signed-out/harness.sh starts.
 *
 * `playwright` is the calling suite's own @playwright/test, so this file
 * needs no package of its own. `envPrefix` is the suite's variable prefix,
 * for example "BOOTSTRAP_E2E". Paths are relative to the suite's own
 * playwright.config.cjs.
 */
function signedOutConfig(playwright, envPrefix) {
  const { defineConfig, devices } = playwright;
  const baseURL = guard(requireResetConfirmBaseUrl, envPrefix + "_BASE_URL");
  const proxy = guard(requireResetConfirmProxy, envPrefix + "_PROXY");

  return defineConfig({
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
}

module.exports = { signedOutConfig };
