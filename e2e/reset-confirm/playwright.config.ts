import { createRequire } from 'node:module';
import { defineConfig, devices } from '@playwright/test';

const require = createRequire(import.meta.url);
const { requireResetConfirmProxy } = require('./require-proxy.cjs') as {
  requireResetConfirmProxy: (raw: string | undefined) => { server: string };
};

const baseURL = process.env.RESET_CONFIRM_BASE_URL;
if (!baseURL) {
  throw new Error('RESET_CONFIRM_BASE_URL is required');
}

const proxy = requireResetConfirmProxy(process.env.RESET_CONFIRM_PROXY);

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
