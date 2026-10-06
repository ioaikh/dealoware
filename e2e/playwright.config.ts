import { defineConfig, devices } from "@playwright/test";
import path from "node:path";

const repoRoot = path.resolve(__dirname, "..");

/**
 * Playwright configuration for Admin UI E2E tests.
 *
 * References:
 * - Epic: UX1-X08
 * - Task: TD-ADM-UI-ci-01
 * - Decision: Chief QA AGREED
 * - Route note r5 sha c5e9d232
 *
 * CI runs Chromium only. The API is started locally (deny-live).
 */
export default defineConfig({
  testDir: "./admin",
  testMatch: /.*\.spec\.ts/,
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [["list"]],

  use: {
    baseURL: process.env.ADMIN_BASE_URL || "http://127.0.0.1:4173",
    browserName: "chromium",
    headless: true,
    // Public repo: traces and screenshots disabled to prevent credential leakage.
    trace: "off",
    screenshot: "off",
    video: "off"
  },

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] }
    }
  ],

  outputDir: "./test-results/",

  webServer: {
    command: "dotnet run --project src/Dealoware.Api --urls http://127.0.0.1:4173 --no-launch-profile",
    cwd: repoRoot,
    url: "http://127.0.0.1:4173/health",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      ASPNETCORE_ENVIRONMENT: "Development",
      DOTNET_ROOT: process.env.DOTNET_ROOT ?? `${process.env.HOME}/.dotnet`,
      PATH: process.env.PATH ?? "",
      AdminHost__AllowedHosts__0: "admin.core.dealoware.com",
      AdminHost__AllowedHosts__1: "127.0.0.1"
    }
  }
});
