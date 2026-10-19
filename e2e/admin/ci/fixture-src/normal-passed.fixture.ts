import { test, expect } from '@playwright/test';

test('normal pass', async () => {
  expect(1 + 1).toBe(2);
});
