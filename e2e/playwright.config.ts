import { defineConfig, devices } from '@playwright/test';
import { DB_FILE, SESSION_FILE } from './admin/helpers/session';

/**
 * Playwright configuration for Admin UI E2E tests.
 *
 * References:
 * - Epic: UX1-X08
 * - Task: TD-ADM-UI-ci-01
 * - Decision: Chief QA AGREED
 *
 * CI runs Chromium only per workflow requirements.
 * Specs hit a local in-job Debug API on 127.0.0.1 — never a live admin host.
 * The Debug harness is compiled out of Release and 404s in Production.
 */
export default defineConfig({
  testDir: './admin',
  testIgnore: ['**/helpers/**'],
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: process.env.CI ? [['list']] : 'list',

  use: {
    baseURL: process.env.ADMIN_BASE_URL || 'http://127.0.0.1:4173',
    // Public repo: traces and screenshots disabled to prevent credential leakage.
    // Artifacts are world-readable on public repos.
    trace: 'off',
    screenshot: 'off',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  outputDir: './test-results/',

  webServer: {
    command:
      'dotnet run --project ../src/Dealoware.Api/Dealoware.Api.csproj --no-launch-profile --configuration Debug --urls http://127.0.0.1:4173',
    url: 'http://127.0.0.1:4173/health',
    reuseExistingServer: !process.env.CI,
    timeout: 180000,
    stdout: 'pipe',
    stderr: 'pipe',
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      DEALOWARE_ADMIN_UI_TEST: '1',
      DEALOWARE_ADMIN_UI_TEST_SESSION_FILE: SESSION_FILE,
      ConnectionStrings__DefaultConnection: `Data Source=${DB_FILE}`,
    },
  },
});
