import { test, expect, chromium, type Page } from '@playwright/test';
import crypto from 'crypto';

const CHECK_PATH = '/admin/api/auth/reset/confirm/check';
const CONFIRM_PATH = '/admin/api/auth/reset/confirm';
const CHECK_RETRY = 'We couldn\'t check your link. Wait a moment and try again.';
const SIGN_IN_GENERIC =
  'We couldn\'t sign you in. Check your details and try again later. You can also reset your password.';
const LINK_EXPIRED = 'This link is no longer valid. Request a new one.';
const NOSCRIPT = 'Turn on JavaScript to set your password.';
const TOO_SHORT = 'Use at least 15 characters.';
const FACTOR_ERROR = 'Enter your authenticator code or a recovery code.';
const LIVE_TOKEN = process.env.RESET_CONFIRM_TOKEN ?? '';
const SUCCESS_TOKEN = process.env.RESET_CONFIRM_SUCCESS_TOKEN ?? '';
const TOTP_SECRET_HEX = process.env.RESET_CONFIRM_TOTP_SECRET_HEX ?? '';
const NEW_PASSWORD = 'radio station later!';
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

function totpCode(secretHex: string, nowMs = Date.now()): string {
  const secret = Buffer.from(secretHex, 'hex');
  const timestep = Math.floor(nowMs / 1000 / 30);
  const buf = Buffer.alloc(8);
  buf.writeBigUInt64BE(BigInt(timestep));
  const hmac = crypto.createHmac('sha1', secret).update(buf).digest();
  const offset = hmac[hmac.length - 1] & 0x0f;
  const binary =
    ((hmac[offset] & 0x7f) << 24)
    | (hmac[offset + 1] << 16)
    | (hmac[offset + 2] << 8)
    | hmac[offset + 3];
  return String(binary % 1_000_000).padStart(6, '0');
}

async function expectNoSa5(page: Page): Promise<void> {
  await expect(page.getByText(LINK_EXPIRED, { exact: true })).toHaveCount(0);
}

async function expectNonLiveMessage(page: Page, text: string): Promise<string> {
  const alert = page.locator('#reset-alert');
  const retry = page.locator('#reset-retry');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveText(text);
  await expect(alert).toHaveAttribute('tabindex', '-1');
  await expect(alert).not.toHaveAttribute('role');
  await expect(alert).not.toHaveAttribute('aria-live');
  await expect(retry).toBeVisible();
  await expect(retry).toHaveText('Try again');
  await expect(retry).not.toHaveAttribute('aria-disabled');
  await expect(retry).not.toHaveAttribute('disabled');
  const alertId = await alert.getAttribute('id');
  expect(alertId).toBeTruthy();
  await expect(retry).toHaveAttribute('aria-describedby', alertId!);
  const order = await page.evaluate(() => {
    const message = document.getElementById('reset-alert');
    const button = document.getElementById('reset-retry');
    if (!message || !button) return 0;
    return message.compareDocumentPosition(button) & Node.DOCUMENT_POSITION_FOLLOWING;
  });
  expect(order).toBeTruthy();
  const liveAncestor = await alert.evaluate((el) => {
    let node: HTMLElement | null = el.parentElement;
    while (node) {
      const role = node.getAttribute('role');
      if (node.getAttribute('aria-live') || role === 'alert' || role === 'status') {
        return node.id || node.tagName;
      }
      node = node.parentElement;
    }
    return null;
  });
  expect(liveAncestor).toBeNull();
  await expectNoSa5(page);
  return alertId!;
}

async function expectRetryFocused(page: Page, text: string): Promise<void> {
  await expectNonLiveMessage(page, text);
  await expect(page.locator('#reset-retry')).toBeFocused();
}

async function expectRepeatFailureFocused(page: Page, text: string): Promise<void> {
  await expectNonLiveMessage(page, text);
  const alert = page.locator('#reset-alert');
  await expect(alert).toBeFocused();
  const box = await alert.boundingBox();
  expect(box).toBeTruthy();
  const viewport = page.viewportSize();
  expect(viewport).toBeTruthy();
  expect(box!.x).toBeGreaterThanOrEqual(0);
  expect(box!.y).toBeGreaterThanOrEqual(0);
  expect(box!.x + box!.width).toBeLessThanOrEqual(viewport!.width);
  expect(box!.y + box!.height).toBeLessThanOrEqual(viewport!.height);
  const focusStyle = await alert.evaluate((el) => {
    const style = getComputedStyle(el);
    return {
      outlineStyle: style.outlineStyle,
      outlineWidth: style.outlineWidth,
      outlineOffset: style.outlineOffset,
    };
  });
  expect(focusStyle.outlineStyle).not.toBe('none');
  expect(focusStyle.outlineWidth).not.toBe('0px');
  expect(focusStyle.outlineOffset).toBe('2px');
}

async function openForm(page: Page, token = LIVE_TOKEN): Promise<void> {
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(token)}`);
  await expect(page.locator('#reset-form')).toBeVisible();
}

test.describe.configure({ mode: 'serial' });

test('confirm page has exactly one nonce CSP and drops the fragment', async ({ page }) => {
  const response = await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  expect(response).not.toBeNull();
  const csp = (await response!.headersArray()).filter(
    (h) => h.name.toLowerCase() === 'content-security-policy');
  expect(csp).toHaveLength(1);
  const html = await response!.text();
  expect(html).not.toContain('data-timeout-ms');
  expect(html).not.toContain('__dwResetTimeoutMs');
  const nonceMatch = html.match(/<script src="\/admin\/auth\/reset-confirm\.js" nonce="([^"]+)"><\/script>/);
  expect(nonceMatch).not.toBeNull();
  const nonce = nonceMatch![1];
  expect(nonce.length).toBeGreaterThan(0);
  expect(csp[0].value).toBe(
    `default-src 'none'; script-src 'nonce-${nonce}'; style-src 'nonce-${nonce}'; connect-src 'self'; `
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
  await openForm(page);
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

test('check timeout uses the Playwright clock and focuses Try again', async ({ page }) => {
  await page.clock.install();
  await page.clock.pauseAt(new Date());
  await page.route((url) => isCheck(url.href), async () => {
    await new Promise(() => { /* hang until the clock fires the 15 s abort */ });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await page.clock.fastForward(15000);
  await expectRetryFocused(page, CHECK_RETRY);
});

test('retry check stays on Try again as Checking… and ignores extra presses', async ({ page }) => {
  let checks = 0;
  let releaseSecond: (() => void) | undefined;
  const secondGate = new Promise<void>((resolve) => {
    releaseSecond = resolve;
  });
  await page.route((url) => isCheck(url.href), async (route) => {
    checks += 1;
    if (checks === 1) {
      await route.fulfill({ status: 500, body: 'error' });
      return;
    }
    await secondGate;
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ valid: true }),
    });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expectRetryFocused(page, CHECK_RETRY);
  const retry = page.locator('#reset-retry');
  await retry.click();
  await expect(retry).toBeVisible();
  await expect(retry).toBeFocused();
  await expect(retry).toHaveAttribute('aria-disabled', 'true');
  await expect(retry).not.toHaveAttribute('disabled');
  await expect(retry).toHaveText('Checking…');
  await expect(page.locator('#reset-status')).toBeHidden();
  await expect(page.locator('#reset-alert')).toBeVisible();
  await expect(page.locator('#reset-alert')).not.toHaveAttribute('role');
  await expect(page.locator('#reset-alert')).not.toHaveAttribute('aria-live');
  const during = checks;
  await retry.click({ force: true });
  await retry.click({ force: true });
  expect(checks).toBe(during);
  expect(checks).toBe(2);
  releaseSecond!();
  await expect(page.locator('#reset-form')).toBeVisible();
  await expect(page.locator('#newPassword')).toBeFocused();
});

test('repeat check 500 restores Try again and focuses the non-live message', async ({ page }) => {
  let checks = 0;
  await page.route((url) => isCheck(url.href), async (route) => {
    checks += 1;
    await route.fulfill({ status: 500, body: 'error' });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await expectRetryFocused(page, CHECK_RETRY);
  const retry = page.locator('#reset-retry');
  await retry.click();
  await expect(retry).toHaveAttribute('aria-disabled', 'true');
  await expect(retry).toHaveText('Checking…');
  await expectRepeatFailureFocused(page, CHECK_RETRY);
  expect(checks).toBe(2);
  await retry.click();
  await expect(retry).toHaveAttribute('aria-disabled', 'true');
  await expectRepeatFailureFocused(page, CHECK_RETRY);
  expect(checks).toBe(3);
});

test('repeat check timeout restores Try again and focuses the non-live message', async ({ page }) => {
  await page.clock.install();
  await page.clock.pauseAt(new Date());
  await page.route((url) => isCheck(url.href), async () => {
    await new Promise(() => { /* hang until the clock fires the 15 s abort */ });
  });
  await page.goto(`/admin/reset/confirm#token=${encodeURIComponent(LIVE_TOKEN)}`);
  await page.clock.fastForward(15000);
  await expectRetryFocused(page, CHECK_RETRY);
  await page.locator('#reset-retry').click();
  await expect(page.locator('#reset-retry')).toHaveText('Checking…');
  await page.clock.fastForward(15000);
  await expectRepeatFailureFocused(page, CHECK_RETRY);
});

test('submit 400 puts the server rule under New password and focuses it', async ({ page }) => {
  await openForm(page);
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
  await openForm(page);
  await page.locator('#newPassword').fill(NEW_PASSWORD);
  await page.locator('#totp').fill('123456');
  await page.locator('#recoveryCode').fill('AAAA-BBBB');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  const alert = page.locator('#reset-alert');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveText(SIGN_IN_GENERIC);
  await expect(alert).toBeFocused();
  await expect(page.locator('#newPassword')).toHaveValue(NEW_PASSWORD);
  await expect(page.locator('#totp')).toHaveValue('');
  await expect(page.locator('#recoveryCode')).toHaveValue('');
  await expect(page.locator('#reset-form')).toBeVisible();
  await expect(page.locator('#reset-retry')).toBeHidden();
  await shot(page, 'reset_confirm_submit_401');
});

test('submit 429 Try again resubmits once with zero check requests', async ({ page }) => {
  let confirms = 0;
  let checks = 0;
  await page.route((url) => isConfirm(url.href), async (route) => {
    if (route.request().method() !== 'POST') {
      await route.continue();
      return;
    }
    confirms += 1;
    if (confirms === 1) {
      await route.fulfill({
        status: 429,
        contentType: 'application/json',
        body: JSON.stringify({ message: CONFIRM_429_MESSAGE }),
      });
      return;
    }
    const posted = route.request().postDataJSON() as {
      newPassword?: string;
      totp?: string;
    };
    expect(posted.newPassword).toBe(NEW_PASSWORD);
    expect(posted.totp).toBe('123456');
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'ok' }),
    });
  });
  page.on('request', (req) => {
    if (isCheck(req.url())) checks += 1;
  });
  await openForm(page);
  const checksAfterLoad = checks;
  await page.locator('#newPassword').fill(NEW_PASSWORD);
  await page.locator('#totp').fill('123456');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expectRetryFocused(page, CONFIRM_429_MESSAGE);
  await page.locator('#reset-retry').click();
  await expect(page).toHaveURL(/\/admin\/sign-in$/);
  expect(confirms).toBe(2);
  expect(checks).toBe(checksAfterLoad);
  await expectNoSa5(page);
});

test('submit timeout Try again resubmits once with zero check requests', async ({ page }) => {
  let confirms = 0;
  let checks = 0;
  await page.clock.install();
  await page.clock.pauseAt(new Date());
  await page.route((url) => isConfirm(url.href), async (route) => {
    if (route.request().method() !== 'POST') {
      await route.continue();
      return;
    }
    confirms += 1;
    if (confirms === 1) {
      await new Promise(() => { /* hang until the clock fires the 15 s abort */ });
      return;
    }
    const posted = route.request().postDataJSON() as {
      newPassword?: string;
      totp?: string;
    };
    expect(posted.newPassword).toBe(NEW_PASSWORD);
    expect(posted.totp).toBe('123456');
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'ok' }),
    });
  });
  page.on('request', (req) => {
    if (isCheck(req.url())) checks += 1;
  });
  await openForm(page);
  const checksAfterLoad = checks;
  await page.locator('#newPassword').fill(NEW_PASSWORD);
  await page.locator('#totp').fill('123456');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await page.clock.fastForward(15000);
  await expectRetryFocused(page, CHECK_RETRY);
  await page.locator('#reset-retry').click();
  await expect(page).toHaveURL(/\/admin\/sign-in$/);
  expect(confirms).toBe(2);
  expect(checks).toBe(checksAfterLoad);
  await expectNoSa5(page);
});

test('neither 2FA field filled does not send and errors under Authenticator code', async ({ page }) => {
  let confirms = 0;
  page.on('request', (req) => {
    if (isConfirm(req.url()) && req.method() === 'POST') confirms += 1;
  });
  await openForm(page);
  await page.locator('#newPassword').fill(NEW_PASSWORD);
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  const totp = page.locator('#totp');
  await expect(page.locator('#totp-error')).toHaveText(FACTOR_ERROR);
  await expect(totp).toHaveAttribute('aria-invalid', 'true');
  await expect(totp).toHaveAttribute('aria-describedby', 'totp-error');
  await expect(totp).toBeFocused();
  expect(confirms).toBe(0);
  await expect(page).not.toHaveURL(/\/admin\/link-expired/);
  await expectNoSa5(page);
});

test('both 2FA fields filled sends only the authenticator code', async ({ page }) => {
  let posted: { totp?: string; recoveryCode?: string } | undefined;
  await page.route((url) => isConfirm(url.href), async (route) => {
    if (route.request().method() !== 'POST') {
      await route.continue();
      return;
    }
    posted = route.request().postDataJSON() as { totp?: string; recoveryCode?: string };
    await route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: JSON.stringify({ error: SIGN_IN_GENERIC }),
    });
  });
  await openForm(page);
  await page.locator('#newPassword').fill(NEW_PASSWORD);
  await page.locator('#totp').fill('123456');
  await page.locator('#recoveryCode').fill('AAAA-BBBB');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expect(page.locator('#reset-alert')).toHaveText(SIGN_IN_GENERIC);
  expect(posted?.totp).toBe('123456');
  expect(posted?.recoveryCode).toBe('');
  await expect(page).not.toHaveURL(/\/admin\/link-expired/);
  await expectNoSa5(page);
});

test('wrong then right authenticator code completes the reset', async ({ page }) => {
  await openForm(page, SUCCESS_TOKEN);
  await page.locator('#newPassword').fill(NEW_PASSWORD);
  await page.locator('#totp').fill('000000');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expect(page.locator('#reset-alert')).toHaveText(SIGN_IN_GENERIC);
  await page.locator('#totp').fill(totpCode(TOTP_SECRET_HEX));
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expect(page).toHaveURL(/\/admin\/sign-in$/);
  await expectNoSa5(page);
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
    expect(html).not.toContain('data-timeout-ms');
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
