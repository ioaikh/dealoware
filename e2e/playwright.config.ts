import { defineConfig, devices } from "@playwright/test";
import { assertNotLiveAdminHost, adminBaseUrl } from "./admin/helpers/deny-live";

assertNotLiveAdminHost();

const baseURL = adminBaseUrl();

/**
 * Playwright configuration for Admin UI E2E tests.
 * Workflow: .github/workflows/admin-ui-ci.yml (PR #29 / UX1-X08 / TD-ADM-UI-ci-01)
 * Specs: e2e/admin/*.spec.ts — Chromium headless only. Local 127.0.0.1, never live admin.
 */
export default defineConfig({
  testDir: "./admin",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  reporter: process.env.CI ? [["list"]] : [["list"], ["html", { open: "never" }]],
  outputDir: "./test-results/",
  webServer: {
    command:
      "dotnet run --project ../src/Dealoware.Api/Dealoware.Api.csproj --urls http://127.0.0.1:4173 --no-launch-profile",
    url: "http://127.0.0.1:4173/health",
    timeout: 180_000,
    reuseExistingServer: !process.env.CI,
    env: {
      ADMIN_UI_TEST_SEED: "1",
      ADMIN_UI_TEST_SEED_FILE: "/tmp/dealoware-admin-ui-seed.json",
      Admin__UiTestSeed__Enabled: "true",
      Admin__DeleteConfirm__LifetimeSeconds: "2",
      ASPNETCORE_ENVIRONMENT: "Development",
    },
  },
  use: {
    baseURL,
    ignoreHTTPSErrors: true,
    // Public repo: traces and screenshots disabled (PR #29).
    trace: "off",
    screenshot: "off",
    browserName: "chromium",
    headless: true,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"], headless: true },
    },
  ],
});
