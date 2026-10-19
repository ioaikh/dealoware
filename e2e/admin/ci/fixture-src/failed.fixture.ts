import { test, expect } from '@playwright/test';

test('deliberate fail', async () => {
  expect(1).toBe(0);
});
