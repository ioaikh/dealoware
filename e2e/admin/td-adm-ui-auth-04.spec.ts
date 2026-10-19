import { test, expect } from '@playwright/test';

test.describe('TD-ADM-UI-auth-04', () => {
  test.skip(true, 'screens not built');
  test('S-A1 step 1 primary button label is Continue', async ({ page }) => {
    // Local/CI host only, never a live host
    await page.goto('/admin/sign-in');
    const primary = page.getByRole('button', { name: 'Continue', exact: true });
    await expect(primary).toBeVisible();
    await expect(page.getByRole('button', { name: 'Sign in', exact: true })).toHaveCount(0);
  });
});
