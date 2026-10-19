import { defineConfig, devices } from '@playwright/test';

/**
 * Empty testDir — captures a real zero-collected JSON report.
 */
export default defineConfig({
  testDir: '.',
  testMatch: '**/*.fixture.ts',
  fullyParallel: true,
  forbidOnly: true,
  retries: 0,
  workers: 1,
  reporter: [['json', { outputFile: 'fixture-report.json' }]],
  use: {
    trace: 'off',
    screenshot: 'off',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
