import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Admin UI E2E tests.
 *
 * References:
 * - Epic: UX1-X08
 * - Task: TD-ADM-UI-ci-01
 * - Decision: Chief QA AGREED
 *
 * CI runs Chromium only. Tests target a local host only
 * (http://127.0.0.1:4173). They never call admin.core.dealoware.com.
 *
 * Public repo: traces and screenshots stay off so cookies and session
 * data are not written to world-readable reports.
 */
export default defineConfig({
  testDir: './admin',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: process.env.CI ? [['list']] : [['list'], ['html', { open: 'never' }]],

  use: {
    baseURL: process.env.ADMIN_BASE_URL || 'http://127.0.0.1:4173',
    extraHTTPHeaders: {
      Host: 'admin.core.dealoware.com',
    },
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
      'dotnet run --project ../tests/Dealoware.AdminUi.Host/Dealoware.AdminUi.Host.csproj --configuration Release --urls http://127.0.0.1:4173',
    url: 'http://127.0.0.1:4173/health',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
