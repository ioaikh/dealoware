import { test, expect, chromium, type Page, type Route } from '@playwright/test';

const CHECK_PATH = '/admin/api/auth/reset/confirm/check';
const CONFIRM_PATH = '/admin/api/auth/reset/confirm';
const ORIGIN = 'https://admin.core.dealoware.com';
const CHECK_RETRY = 'We couldn\'t check your link. Wait a moment and try again.';
const SIGN_IN_GENERIC =
  'We couldn\'t sign you in. Check your details and try again later. You can also reset your password.';
const LINK_EXPIRED = 'This link is no longer valid. Request a new one.';
const NOSCRIPT = 'Turn on JavaScript to set your password.';
const TOO_SHORT = 'Use at least 15 characters.';
const LIVE_TOKEN = process.env.RESET_CONFIRM_TOKEN ?? '';
const CONFIRM_429_MESSAGE = 'Slow down. Wait a moment and try again.';
const ARTIFACTS = process.env.RESET_CONFIRM_ARTIFACTS;

async function shot(page: Page, name: string): Promise<void> {
  if (!ARTIFACTS) return;
  await page.screenshot({ path: `${ARTIFACTS}/${name}.png`, fullPage: true });
}

function isCheck(url: string): boolean {
  return new URL(url).pathname === CHECK_PATH;
}

function isConfirm(url: string): boolean {
  return new URL(url).pathname === CONFIRM_PATH;
}

async function rewriteOrigin(route: Route): Promise<void> {
  const method = route.request().method();
  if (method === 'GET' || method === 'HEAD' || method === 'OPTIONS') {
    await route.continue();
    return;
  }
  const headers = { ...route.request().headers(), origin: ORIGIN };
  await route.continue({ headers });
}

async function expectNoSa5(page: Page): Promise<void> {
  await expect(page.getByText(LINK_EXPIRED, { exact: true })).toHaveCount(0);
}

async function expectRetryFocused(page: Page, text: string): Promise<void> {
  const alert = page.locator('#reset-alert');
  const retry = page.locator('#reset-retry');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveAttribute('role', 'alert');
  await expect(alert).toHaveText(text);
  await expect(retry).toBeVisible();
  await expect(retry).toBeFocused();
  await expectNoSa5(page);
}

test.describe.configure({ mode: 'serial' });

test.beforeEach(async ({ page }) => {
  await page.route('**/*', rewriteOrigin);
});

test('confirm page has exactly one nonce CSP and drops the fragment', async ({ page }) => {
  const response = await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  expect(response).not.toBeNull();
  const csp = (await response!.headersArray()).filter(
    (h) => h.name.toLowerCase() === 'content-security-policy');
  expect(csp).toHaveLength(1);
  const html = await response!.text();
  const nonceMatch = html.match(/<script src="\/admin\/auth\/reset-confirm\.js" nonce="([^"]+)"><\/script>/);
  expect(nonceMatch).not.toBeNull();
  const nonce = nonceMatch![1];
  expect(nonce.length).toBeGreaterThan(0);
  expect(csp[0].value).toBe(
    `default-src 'none'; script-src 'nonce-${nonce}'; connect-src 'self'; `
    + 'frame-ancestors \'none\'; base-uri \'none\'; form-action \'self\'; object-src \'none\'');
  expect(await page.locator('script[src="/admin/auth/reset-confirm.js"]').evaluate((el) => (el as HTMLScriptElement).nonce)).toBe(nonce);
  await expect(page.locator('#reset-form')).toBeVisible();
  expect(new URL(page.url()).hash).toBe('');
  await expect(page.locator('#newPassword')).toBeFocused();
});

test('no fragment goes to link-expired with zero check requests', async ({ page }) => {
  let checks = 0;
  page.on('request', (req) => {
    if (isCheck(req.url())) checks += 1;
  });
  await page.goto('/admin/reset/confirm');
  await expect(page).toHaveURL(/\/admin\/link-expired$/);
  await expect(page.getByText(LINK_EXPIRED, { exact: true })).toBeVisible();
  expect(checks).toBe(0);
});

test('dead token goes to link-expired', async ({ page }) => {
  await page.goto('/admin/reset/confirm#token=dead-token-not-issued');
  await expect(page).toHaveURL(/\/admin\/link-expired$/);
  await expect(page.getByText(LINK_EXPIRED, { exact: true })).toBeVisible();
});

test('good token shows the form with focus on New password', async ({ page }) => {
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expect(page.locator('#reset-form')).toBeVisible();
  await expect(page.locator('#newPassword')).toBeFocused();
  await expect(page.getByRole('button', { name: 'Change password', exact: true })).toBeVisible();
  await shot(page, 'reset_confirm_form_focus');
});

test('check 429 shows retry with Try again focused and no S-A5', async ({ page }) => {
  await page.route((url) => isCheck(url.href), async (route) => {
    await route.fulfill({
      status: 429,
      contentType: 'application/json',
      body: JSON.stringify({ message: SIGN_IN_GENERIC }),
    });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expectRetryFocused(page, CHECK_RETRY);
  await shot(page, 'reset_confirm_check_retry');
});

test('check 500 shows retry with Try again focused and no S-A5', async ({ page }) => {
  await page.route((url) => isCheck(url.href), async (route) => {
    await route.fulfill({ status: 500, body: 'error' });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expectRetryFocused(page, CHECK_RETRY);
});

test('check timeout uses the injectable clock and focuses Try again', async ({ page }) => {
  await page.addInitScript(() => {
    (window as Window & { __dwResetTimeoutMs?: number }).__dwResetTimeoutMs = 50;
  });
  await page.route((url) => isCheck(url.href), async () => {
    await new Promise(() => { /* hang until the injectable timeout aborts */ });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expectRetryFocused(page, CHECK_RETRY);
});

test('submit 400 puts the server rule under New password and focuses it', async ({ page }) => {
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expect(page.locator('#newPassword')).toBeVisible();
  await page.locator('#newPassword').fill('short-value');
  await page.locator('#totp').fill('123456');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  const field = page.locator('#newPassword');
  await expect(page.locator('#newPassword-error')).toHaveText(TOO_SHORT);
  await expect(field).toHaveAttribute('aria-invalid', 'true');
  await expect(field).toHaveAttribute('aria-describedby', 'newPassword-error');
  await expect(field).toHaveValue('short-value');
  await expect(field).toBeFocused();
  await expect(page.locator('#reset-form')).toBeVisible();
  await shot(page, 'reset_confirm_submit_400');
});

test('submit 401 shows the sign-in alert, keeps the password, and clears 2FA', async ({ page }) => {
  await page.route((url) => isConfirm(url.href), async (route) => {
    if (route.request().method() !== 'POST') {
      await route.continue();
      return;
    }
    await route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: JSON.stringify({ error: SIGN_IN_GENERIC }),
    });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expect(page.locator('#newPassword')).toBeVisible();
  await page.locator('#newPassword').fill('radio station later!');
  await page.locator('#totp').fill('123456');
  await page.locator('#recoveryCode').fill('AAAA-BBBB');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  const alert = page.locator('#reset-alert');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveText(SIGN_IN_GENERIC);
  await expect(alert).toBeFocused();
  await expect(page.locator('#newPassword')).toHaveValue('radio station later!');
  await expect(page.locator('#totp')).toHaveValue('');
  await expect(page.locator('#recoveryCode')).toHaveValue('');
  await expect(page.locator('#reset-form')).toBeVisible();
  await expect(page.locator('#reset-retry')).toBeHidden();
  await shot(page, 'reset_confirm_submit_401');
});

test('submit 429 calls showRetry with the server message and focuses Try again', async ({ page }) => {
  await page.route((url) => isConfirm(url.href), async (route) => {
    if (route.request().method() !== 'POST') {
      await route.continue();
      return;
    }
    await route.fulfill({
      status: 429,
      contentType: 'application/json',
      body: JSON.stringify({ message: CONFIRM_429_MESSAGE }),
    });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expect(page.locator('#newPassword')).toBeVisible();
  await page.locator('#newPassword').fill('radio station later!');
  await page.locator('#totp').fill('123456');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expectRetryFocused(page, CONFIRM_429_MESSAGE);
});

test('submit timeout uses the injectable clock and the generic retry text', async ({ page }) => {
  await page.addInitScript(() => {
    (window as Window & { __dwResetTimeoutMs?: number }).__dwResetTimeoutMs = 50;
  });
  await page.route((url) => isConfirm(url.href), async (route) => {
    if (route.request().method() !== 'POST') {
      await route.continue();
      return;
    }
    await new Promise(() => { /* hang until the injectable timeout aborts */ });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expect(page.locator('#newPassword')).toBeVisible();
  await page.locator('#newPassword').fill('radio station later!');
  await page.locator('#totp').fill('123456');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expectRetryFocused(page, CHECK_RETRY);
});

test('with JavaScript off, only the noscript text shows', async () => {
  const browser = await chromium.launch({
    args: [
      '--disable-javascript',
      '--host-resolver-rules=MAP admin.core.dealoware.com 127.0.0.1',
    ],
  });
  try {
    const context = await browser.newContext({
      ignoreHTTPSErrors: true,
      baseURL: test.info().project.use.baseURL,
      javaScriptEnabled: false,
    });
    const page = await context.newPage();
    await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
    const html = await page.content();
    expect(html).toContain(`<noscript>${NOSCRIPT}</noscript>`);
    expect(html).toContain('id="reset-status" role="status" hidden');
    await expect(page.locator('#reset-status')).toBeHidden();
    await expect(page.locator('#reset-form')).toBeHidden();
    await expect(page.locator('#reset-alert')).toBeHidden();
    await expect(page.locator('#reset-retry')).toBeHidden();
    const visible = (await page.locator('body').innerText()).trim();
    expect(visible).toBe(NOSCRIPT);
    expect(visible).not.toContain('Checking your link');
    await shot(page, 'reset_confirm_js_off');
  } finally {
    await browser.close();
  }
});
