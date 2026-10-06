import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Admin UI E2E tests.
 *
 * References:
 * - Epic: UX1-X08
 * - Task: TD-ADM-UI-ci-01
 * - Decision: Chief QA AGREED
 *
 * CI runs Chromium only per workflow requirements.
 *
 * ## App Start (Phase 0)
 *
 * No admin UI frontend exists yet. When a buildable admin UI is added:
 *
 * 1. Add a build/serve script to e2e/package.json or the admin UI package
 * 2. Uncomment the webServer block below and configure the command
 * 3. Tests will auto-start the server before running
 *
 * Example webServer config (uncomment when admin UI exists):
 *
 *   webServer: {
 *     command: 'npm run preview',
 *     url: 'http://127.0.0.1:4173',
 *     reuseExistingServer: !process.env.CI,
 *     timeout: 120000,
 *   },
 *
 * For local development before webServer is configured, start the app manually:
 *   cd ../src/Dealoware.Api && dotnet run
 *   # Then run tests pointing at the running server
 */
export default defineConfig({
  testDir: './admin',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? [['html'], ['list']] : 'html',

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

  // Phase 0: No admin UI frontend yet. Uncomment webServer when admin UI build exists.
  // webServer: {
  //   command: 'npm run preview',  // or 'dotnet run' for backend, etc.
  //   url: 'http://127.0.0.1:4173',
  //   reuseExistingServer: !process.env.CI,
  //   timeout: 120000,
  // },
});
