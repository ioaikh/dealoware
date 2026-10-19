import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.RESET_CONFIRM_BASE_URL;
if (!baseURL) {
  throw new Error('RESET_CONFIRM_BASE_URL is required');
}

export default defineConfig({
  testDir: '.',
  testMatch: '*.spec.ts',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],
  timeout: 20_000,
  use: {
    baseURL,
    ignoreHTTPSErrors: true,
    trace: 'off',
    screenshot: 'off',
    video: 'off',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        ignoreHTTPSErrors: true,
        launchOptions: {
          args: ['--host-resolver-rules=MAP admin.core.dealoware.com 127.0.0.1'],
        },
      },
    },
  ],
  outputDir: './test-results/',
});
