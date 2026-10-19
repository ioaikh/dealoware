import { test, expect } from '@playwright/test';

test('mixed pass', async () => {
  expect(2).toBe(2);
});

test.skip('mixed skip', async () => {
  throw new Error('must not run');
});
