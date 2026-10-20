import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.RESET_CONFIRM_BASE_URL;
if (!baseURL) {
  throw new Error('RESET_CONFIRM_BASE_URL is required');
}

const proxy = process.env.RESET_CONFIRM_PROXY
  ? { server: process.env.RESET_CONFIRM_PROXY }
  : undefined;

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
    proxy,
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
        proxy,
      },
    },
  ],
  outputDir: './test-results/',
});
