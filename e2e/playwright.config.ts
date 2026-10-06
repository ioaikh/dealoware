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
 */
export default defineConfig({
  testDir: './admin',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? [['html'], ['list']] : 'html',

  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL || process.env.ADMIN_BASE_URL || 'http://127.0.0.1:4173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  outputDir: '../test-results/',
});
