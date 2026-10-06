import { defineConfig } from "@playwright/test";
import { assertNotLiveAdminHost } from "./helpers/deny-live";

assertNotLiveAdminHost();

export default defineConfig({
  testDir: "./specs",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  reporter: [["list"], ["html", { open: "never", outputFolder: "playwright-report" }]],
  outputDir: "test-results",
  webServer: {
    command:
      "export PATH=\"$HOME/.dotnet:$PATH\" && ADMIN_UI_TEST_SEED=1 ADMIN_UI_TEST_SEED_FILE=/tmp/dealoware-admin-ui-seed.json Admin__DeleteConfirm__LifetimeSeconds=2 ASPNETCORE_ENVIRONMENT=Development dotnet run --project ../../src/Dealoware.Api/Dealoware.Api.csproj --urls http://127.0.0.1:5055 --no-launch-profile",
    url: "http://127.0.0.1:5055/health",
    timeout: 120_000,
    reuseExistingServer: !process.env.CI,
  },
  use: {
    baseURL: process.env.ADMIN_BASE_URL || process.env.PLAYWRIGHT_BASE_URL || "http://127.0.0.1:5055",
    ignoreHTTPSErrors: true,
    trace: "retain-on-failure",
    browserName: "chromium",
    headless: true,
  },
  projects: [{ name: "chromium", use: { browserName: "chromium" } }],
});
