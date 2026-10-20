"use strict";

const { defineConfig, devices } = require("@playwright/test");
const {
  requireResetConfirmProxy,
  requireResetConfirmBaseUrl
} = require("./require-proxy.cjs");

const baseURL = requireResetConfirmBaseUrl(process.env.RESET_CONFIRM_BASE_URL);
const proxy = requireResetConfirmProxy(process.env.RESET_CONFIRM_PROXY);

module.exports = defineConfig({
  testDir: ".",
  testMatch: "*.spec.ts",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [["list"]],
  timeout: 20_000,
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
