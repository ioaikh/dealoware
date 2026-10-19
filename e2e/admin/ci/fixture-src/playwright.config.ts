import { defineConfig, devices } from '@playwright/test';

/**
 * Isolated config for capturing AC-06 fixture reports.
 * Not used by Admin UI CI. testMatch is *.fixture.ts so spec discovery
 * and the main admin suite never collect these files.
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
