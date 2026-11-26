import { test, expect, type Locator, type Page, type Request, type Response } from '@playwright/test';
import crypto from 'crypto';
import { expectNoSeriousAxeFindings } from './axe';

/**
 * First-time setup page (S-A4, /admin/bootstrap) against the real app.
 * Start it with e2e/bootstrap/run.sh.
 *
 * The browser never reaches Cloudflare: every request to
 * challenges.cloudflare.com is answered here with a stand-in widget, loaded
 * under the page's real Content-Security-Policy. The server checks the
 * widget's token with the stand-in verifier run.sh starts, which accepts
 * exactly one run-time token.
 *
 * The tests run in file order on one worker. Order matters: the owner can be
 * set up once, so everything that needs a live link comes before the test
 * that uses it up, and the test that throttles this address comes last.
 */

const PAGE = '/admin/bootstrap';
const API_PATH = '/admin/api/auth/bootstrap';
const SIGN_IN_PATH = '/admin/sign-in';
const LINK_EXPIRED_URL = /\/admin\/link-expired\?for=setup$/;
const WIDGET_HOST = 'challenges.cloudflare.com';
const WIDGET_SCRIPT = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';
const SITE_KEY = 'e2e-site-key';

const TITLE = 'Set password · Dealoware admin';
const HEADING = 'Set password';
const NOSCRIPT = 'Turn on JavaScript to set your password.';
const LINK_EXPIRED_HEADING = 'This link no longer works';
const LINK_EXPIRED_SETUP =
  'Setup links work once and expire after 24 hours. Ask for a new setup link through the same channel you got this one.';
const TOO_SHORT = 'Use at least 15 characters.';
const TOO_COMMON = 'This password is too common. Choose another.';
const MISMATCH = 'These passwords don\'t match.';
const VERIFICATION_FAILED = 'Verification failed, please try again.';
const SIGN_IN_GENERIC =
  'We couldn\'t sign you in. Check your details and try again later. You can also reset your password.';
const SUBMIT_RETRY = 'We couldn\'t set your password. Wait a moment and try again.';
const CHECK_LOADING = 'Security check is loading.';
const CHECK_FAILED = 'The security check failed.';
const CHECK_EXPIRED = 'The security check expired.';
const CHECK_UNAVAILABLE =
  'The security check is unavailable. Check your network or disable extensions that block scripts, then retry.';
const CHECK_NOT_CONFIGURED = 'The security check is unavailable.';

const LIVE_TOKEN = process.env.BOOTSTRAP_E2E_TOKEN ?? '';
const EXPIRED_TOKEN = process.env.BOOTSTRAP_E2E_EXPIRED_TOKEN ?? '';
const PASS_TOKEN = process.env.BOOTSTRAP_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.BOOTSTRAP_E2E_ARTIFACTS;

// Made up for this run. Hex only, so it holds no common or account word.
const GOOD_PASSWORD = crypto.randomBytes(16).toString('hex');
// Long enough, but it contains a word the server refuses.
const REFUSED_PASSWORD = `dealoware ${crypto.randomBytes(8).toString('hex')}`;

/** The stand-in for Cloudflare's api.js. It draws a frame from Cloudflare's origin, as the real one does. */
const WIDGET_STAND_IN = `
(function () {
  var cfg = window.__e2eWidget || {};
  var state = window.__e2eWidgetState = { renders: 0, resets: 0, options: null };
  function solve() {
    if (cfg.auto && state.options) setTimeout(function () { state.options.callback(cfg.token); }, 0);
  }
  window.turnstile = {
    render: function (host, options) {
      state.renders += 1;
      state.options = options;
      state.sitekey = options.sitekey;
      state.size = options.size;
      var frame = document.createElement('iframe');
      frame.src = 'https://${WIDGET_HOST}/e2e-widget';
      frame.title = 'Security check';
      frame.width = options.size === 'compact' ? '150' : '300';
      frame.height = options.size === 'compact' ? '140' : '65';
      host.appendChild(frame);
      solve();
      return 'e2e-widget';
    },
    reset: function () { state.resets += 1; solve(); },
    remove: function () {}
  };
})();
`;

const WIDGET_FRAME =
  '<!DOCTYPE html><html lang="en"><head><meta charset="utf-8"><title>Security check</title></head>'
  + '<body><main><p>Security check stand-in</p></main></body></html>';

/** 'ok': the stand-in at once. 'abort': the request fails. A promise: the stand-in once it resolves. */
type ScriptReply = 'ok' | 'abort' | Promise<void>;

type WidgetSetup = {
  /** Solve at once with this token (default: the token the verifier accepts). */
  auto?: boolean;
  token?: string;
  /** What the n-th request for Cloudflare's script gets (default: the stand-in). */
  script?: (n: number) => ScriptReply;
};

let cspMessages: string[] = [];

async function shot(page: Page, name: string): Promise<void> {
  if (!ARTIFACTS) return;
  await page.screenshot({ path: `${ARTIFACTS}/${name}.png`, fullPage: true });
}

/** Answers everything addressed to Cloudflare. Returns the script request count. */
async function standInForCloudflare(page: Page, setup: WidgetSetup = {}): Promise<{ scripts: number }> {
  const counts = { scripts: 0 };
  await page.addInitScript((cfg) => {
    (window as any).__e2eWidget = cfg;
  }, { auto: setup.auto ?? true, token: setup.token ?? PASS_TOKEN });
  await page.route((url) => url.hostname === WIDGET_HOST, async (route) => {
    const url = new URL(route.request().url());
    if (url.pathname === '/turnstile/v0/api.js') {
      counts.scripts += 1;
      const reply = setup.script ? setup.script(counts.scripts) : 'ok';
      if (reply === 'abort') {
        await route.abort('failed');
        return;
      }
      if (reply !== 'ok') await reply;
      await route.fulfill({ status: 200, contentType: 'text/javascript', body: WIDGET_STAND_IN });
      return;
    }
    if (url.pathname === '/e2e-widget') {
      await route.fulfill({ status: 200, contentType: 'text/html; charset=utf-8', body: WIDGET_FRAME });
      return;
    }
    await route.fulfill({ status: 404, body: '' });
  });
  return counts;
}

/** Calls one of the callbacks the page gave the widget, as Cloudflare would. */
async function widgetSays(page: Page, callback: string, value?: string): Promise<void> {
  await page.evaluate(([name, arg]) => {
    (window as any).__e2eWidgetState.options[name as string](arg);
  }, [callback, value]);
}

async function widgetState(page: Page): Promise<{ renders: number; resets: number; sitekey: string; size: string }> {
  return await page.evaluate(() => {
    const s = (window as any).__e2eWidgetState;
    return { renders: s.renders, resets: s.resets, sitekey: s.sitekey, size: s.size };
  });
}

function isSubmit(request: Request): boolean {
  return new URL(request.url()).pathname === API_PATH && request.method() === 'POST';
}

function isSubmitReply(response: Response): boolean {
  return isSubmit(response.request());
}

/** Records every request the page makes: where it went, and the setup requests among them. */
function watch(page: Page): { urls: string[]; referers: string[]; submits: Request[]; cloudflare: string[] } {
  const seen = { urls: [] as string[], referers: [] as string[], submits: [] as Request[], cloudflare: [] as string[] };
  page.on('request', (request) => {
    seen.urls.push(request.url());
    const referer = request.headers()['referer'];
    if (referer) seen.referers.push(referer);
    if (isSubmit(request)) seen.submits.push(request);
    if (new URL(request.url()).hostname === WIDGET_HOST) seen.cloudflare.push(request.url());
  });
  return seen;
}

async function openForm(page: Page, token = LIVE_TOKEN): Promise<void> {
  await page.goto(`${PAGE}#token=${encodeURIComponent(token)}`);
  await expect(page.locator('#bootstrap-form')).toBeVisible();
}

function submitButton(page: Page): Locator {
  return page.getByRole('button', { name: 'Set password', exact: true });
}

async function fillBoth(page: Page, password: string, confirm = password): Promise<void> {
  await page.locator('#new-password').fill(password);
  await page.locator('#confirm-password').fill(confirm);
}

async function submitAndWait(page: Page): Promise<Response> {
  const [response] = await Promise.all([
    page.waitForResponse(isSubmitReply),
    submitButton(page).click(),
  ]);
  return response;
}

async function expectLinkExpiredPage(page: Page): Promise<void> {
  await expect(page).toHaveURL(LINK_EXPIRED_URL);
  await expect(page.getByRole('heading', { name: LINK_EXPIRED_HEADING, exact: true })).toBeVisible();
  await expect(page.getByText(LINK_EXPIRED_SETUP, { exact: true })).toBeVisible();
  await expect(page.locator('form')).toHaveCount(0);
  await expect(page.locator('input')).toHaveCount(0);
}

async function expectAlert(page: Page, text: string): Promise<void> {
  const alert = page.locator('#form-alert');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveText(text);
  await expect(alert).toHaveAttribute('role', 'alert');
  await expect(alert).toBeFocused();
  await expect(page.locator('[role="alert"]')).toHaveCount(1);
}

async function expectNoHorizontalScroll(page: Page): Promise<void> {
  const size = await page.evaluate(() => ({
    content: document.documentElement.scrollWidth,
    viewport: document.documentElement.clientWidth,
  }));
  expect(size.content).toBeLessThanOrEqual(size.viewport);
}

test.beforeEach(async ({ page }) => {
  cspMessages = [];
  page.on('console', (message) => {
    if (/content security policy/i.test(message.text())) cspMessages.push(message.text());
  });
});

test.afterEach(() => {
  // Nothing the page loads, and nothing the tests stand in for, may be refused by its own policy.
  expect(cspMessages).toEqual([]);
});

test('the page is HTML under a nonce policy that allows only Cloudflare, styled, with the token gone from the address bar', async ({ page, baseURL }) => {
  const seen = watch(page);
  const counts = await standInForCloudflare(page);
  const response = await page.goto(`${PAGE}#token=${encodeURIComponent(LIVE_TOKEN)}`);
  expect(response).not.toBeNull();
  expect(response!.status()).toBe(200);

  const headers = await response!.headersArray();
  const csp = headers.filter((h) => h.name.toLowerCase() === 'content-security-policy');
  expect(csp).toHaveLength(1);
  const html = await response!.text();
  const nonceMatch = html.match(/<link rel="stylesheet" href="\/admin\/auth\/signed-out\.css" nonce="([^"]+)">/);
  expect(nonceMatch).not.toBeNull();
  const nonce = nonceMatch![1];
  expect(csp[0].value).toBe(
    `default-src 'none'; script-src 'nonce-${nonce}' https://challenges.cloudflare.com; style-src 'nonce-${nonce}'; `
    + 'connect-src \'self\'; frame-src https://challenges.cloudflare.com; '
    + 'frame-ancestors \'none\'; base-uri \'none\'; form-action \'self\'; object-src \'none\'');
  expect(response!.headers()['content-type']).toBe('text/html; charset=utf-8');
  expect(response!.headers()['cache-control']).toBe('no-store');
  expect(response!.headers()['referrer-policy']).toBe('no-referrer');
  expect(html).not.toContain(LIVE_TOKEN);

  // The script showed the form, cleared the address bar and put the cursor in the first field.
  await expect(page.locator('#bootstrap-form')).toBeVisible();
  expect(page.url()).toBe(`${baseURL}${PAGE}`);
  expect(new URL(page.url()).hash).toBe('');
  await expect(page).toHaveTitle(TITLE);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText(HEADING);
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page.locator('#new-password')).toBeFocused();
  await expect(page.getByText(NOSCRIPT)).toBeHidden();

  // The linked stylesheet applied under style-src 'nonce-...'.
  const colours = await page.evaluate(() => ({
    body: getComputedStyle(document.body).backgroundColor,
    card: getComputedStyle(document.querySelector('.auth-card')!).backgroundColor,
    button: getComputedStyle(document.getElementById('bootstrap-submit')!).backgroundColor,
  }));
  expect(colours).toEqual({ body: 'rgb(244, 241, 234)', card: 'rgb(255, 255, 255)', button: 'rgb(11, 79, 138)' });

  // The widget script and its frame came from Cloudflare's origin, once, and were allowed.
  await expect(submitButton(page)).toBeEnabled();
  await expect(page.locator('#turnstile-status')).toHaveText('');
  expect(counts.scripts).toBe(1);
  expect(seen.cloudflare[0]).toBe(WIDGET_SCRIPT);
  await expect(page.frameLocator('#turnstile-widget iframe').getByText('Security check stand-in')).toBeVisible();
  const widget = await widgetState(page);
  expect(widget).toEqual({ renders: 1, resets: 0, sitekey: SITE_KEY, size: 'normal' });

  // The link token is in the script's memory only.
  expect(await page.content()).not.toContain(LIVE_TOKEN);
  expect(JSON.stringify(await page.context().cookies())).not.toContain(LIVE_TOKEN);
  expect(await page.evaluate(() => JSON.stringify([{ ...localStorage }, { ...sessionStorage }]))).not.toContain(LIVE_TOKEN);
  for (const url of seen.urls) expect(url).not.toContain(LIVE_TOKEN);
  expect(seen.referers).toEqual([]);
  expect(seen.submits).toHaveLength(0);
  await shot(page, 'bootstrap_desktop');
});

test('no token in the link goes to the link-expired page and sends nothing', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  await page.goto(PAGE);
  await expectLinkExpiredPage(page);
  expect(seen.submits).toHaveLength(0);
  expect(seen.cloudflare).toEqual([]);
});

test('a reload has no token and lands on the link-expired page; the emailed link still works', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  await openForm(page);
  await page.reload();
  await expectLinkExpiredPage(page);
  expect(seen.submits).toHaveLength(0);
  await openForm(page);
  await expect(page.locator('#new-password')).toBeFocused();
});

test('submit works only with a fresh security-check token: loading, failed, retry, ready, expired, unavailable', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page, { auto: false });
  await openForm(page);
  const status = page.locator('#turnstile-status');
  const retry = page.locator('#turnstile-retry');
  const submit = submitButton(page);
  await fillBoth(page, GOOD_PASSWORD);

  await expect(status).toHaveText(CHECK_LOADING);
  await expect(status).toHaveAttribute('role', 'status');
  await expect(submit).toBeDisabled();
  await expect(retry).toBeHidden();
  await page.locator('#confirm-password').press('Enter');

  await widgetSays(page, 'error-callback');
  await expect(status).toHaveText(CHECK_FAILED);
  await expect(submit).toBeDisabled();
  await expect(retry).toBeVisible();
  await expect(retry).toHaveText('Retry');
  await page.locator('#confirm-password').press('Enter');

  // Retry re-runs the widget and keeps keyboard focus in place.
  await retry.click();
  await expect(status).toHaveText(CHECK_LOADING);
  await expect(status).toBeFocused();
  await expect(retry).toBeHidden();
  await expect(submit).toBeDisabled();
  expect((await widgetState(page)).resets).toBe(1);

  await widgetSays(page, 'callback', 'first-token');
  await expect(status).toHaveText('');
  await expect(submit).toBeEnabled();

  await widgetSays(page, 'expired-callback');
  await expect(status).toHaveText(CHECK_EXPIRED);
  await expect(submit).toBeDisabled();
  await expect(retry).toBeVisible();
  await page.locator('#confirm-password').press('Enter');

  await widgetSays(page, 'timeout-callback');
  await expect(status).toHaveText(CHECK_UNAVAILABLE);
  await expect(submit).toBeDisabled();
  await expect(retry).toBeVisible();
  expect(seen.submits).toHaveLength(0);

  // After a failure the next submit carries the new token, never the old one.
  let sentToken: unknown;
  await page.route((url) => url.pathname === API_PATH, async (route) => {
    sentToken = (route.request().postDataJSON() as { turnstileToken?: string }).turnstileToken;
    await route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: JSON.stringify({ error: VERIFICATION_FAILED }),
    });
  });
  await retry.click();
  await widgetSays(page, 'callback', 'second-token');
  await expect(submit).toBeEnabled();
  await submit.click();
  await expectAlert(page, VERIFICATION_FAILED);
  expect(sentToken).toBe('second-token');
  expect(seen.submits).toHaveLength(1);
});

test('Cloudflare script blocked: the check is unavailable, nothing can be sent, and Retry loads it', async ({ page }) => {
  const seen = watch(page);
  const counts = await standInForCloudflare(page, { script: (n) => (n === 1 ? 'abort' : 'ok') });
  await openForm(page);
  const status = page.locator('#turnstile-status');
  await expect(status).toHaveText(CHECK_UNAVAILABLE);
  await expect(submitButton(page)).toBeDisabled();
  await fillBoth(page, GOOD_PASSWORD);
  await page.locator('#confirm-password').press('Enter');
  await page.evaluate(() => (document.getElementById('bootstrap-form') as HTMLFormElement).requestSubmit());
  expect(seen.submits).toHaveLength(0);

  await page.locator('#turnstile-retry').click();
  await expect(submitButton(page)).toBeEnabled();
  await expect(status).toHaveText('');
  expect(counts.scripts).toBe(2);
});

test('Cloudflare script that is slow: unavailable after ten seconds, and Retry waits for the same request', async ({ page }) => {
  // The page's timers move only when this test moves them.
  await page.clock.install();
  await page.clock.pauseAt(new Date());
  let arrive: (() => void) | undefined;
  const onItsWay = new Promise<void>((resolve) => {
    arrive = resolve;
  });
  const counts = await standInForCloudflare(page, { script: () => onItsWay });
  // The pending script holds the load event back, so do not wait for it.
  await page.goto(`${PAGE}#token=${encodeURIComponent(LIVE_TOKEN)}`, { waitUntil: 'domcontentloaded' });
  const status = page.locator('#turnstile-status');
  const retry = page.locator('#turnstile-retry');
  await expect(status).toHaveText(CHECK_LOADING);
  await page.clock.fastForward(9999);
  await expect(status).toHaveText(CHECK_LOADING);
  await expect(retry).toBeHidden();
  await page.clock.fastForward(1);
  await expect(status).toHaveText(CHECK_UNAVAILABLE);
  await expect(submitButton(page)).toBeDisabled();
  await expect(retry).toBeVisible();

  // Retry does not ask Cloudflare twice for a script that is still on its way.
  await retry.click();
  await expect(status).toHaveText(CHECK_LOADING);
  await expect(submitButton(page)).toBeDisabled();
  await page.clock.fastForward(10000);
  await expect(status).toHaveText(CHECK_UNAVAILABLE);
  await retry.click();
  await expect(status).toHaveText(CHECK_LOADING);
  expect(counts.scripts).toBe(1);

  arrive!();
  await page.clock.resume();
  await expect(submitButton(page)).toBeEnabled();
  await expect(status).toHaveText('');
  expect(await widgetState(page)).toMatchObject({ renders: 1, resets: 0 });
});

test('Cloudflare script that arrives after the ten seconds: the check recovers without Retry', async ({ page }) => {
  await page.clock.install();
  await page.clock.pauseAt(new Date());
  let arrive: (() => void) | undefined;
  const onItsWay = new Promise<void>((resolve) => {
    arrive = resolve;
  });
  await standInForCloudflare(page, { script: () => onItsWay });
  await page.goto(`${PAGE}#token=${encodeURIComponent(LIVE_TOKEN)}`, { waitUntil: 'domcontentloaded' });
  const status = page.locator('#turnstile-status');
  await page.clock.fastForward(10000);
  await expect(status).toHaveText(CHECK_UNAVAILABLE);
  await expect(submitButton(page)).toBeDisabled();

  arrive!();
  await expect(status).toHaveText(CHECK_LOADING);
  await expect(page.locator('#turnstile-retry')).toBeHidden();
  await expect(submitButton(page)).toBeDisabled();
  await page.clock.resume();
  await expect(submitButton(page)).toBeEnabled();
});

test('with no site key the page fails closed: it says so, loads nothing from Cloudflare and cannot send', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  // The page as the server renders it when Admin:Turnstile:SiteKey is empty (the .NET tests pin that markup).
  await page.route((url) => url.pathname === PAGE, async (route) => {
    const response = await route.fetch();
    const html = (await response.text()).replace(/ data-sitekey="[^"]*"/, '');
    await route.fulfill({ response, body: html });
  });
  await openForm(page);
  expect(await page.locator('#turnstile').getAttribute('data-sitekey')).toBeNull();
  await expect(page.locator('#turnstile-status')).toHaveText(CHECK_NOT_CONFIGURED);
  await expect(page.locator('#turnstile')).toHaveAttribute('data-state', 'unconfigured');
  await expect(page.locator('#turnstile-retry')).toBeHidden();
  await expect(submitButton(page)).toBeDisabled();

  await fillBoth(page, GOOD_PASSWORD);
  await page.locator('#confirm-password').press('Enter');
  await submitButton(page).click({ force: true });
  await page.evaluate(() => (document.getElementById('bootstrap-form') as HTMLFormElement).requestSubmit());
  await expect(submitButton(page)).toBeDisabled();
  expect(seen.submits).toHaveLength(0);
  expect(seen.cloudflare).toEqual([]);
});

test('confirm that does not match, and a password that is too short, are caught in the browser', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  await openForm(page);
  const newPassword = page.locator('#new-password');
  const confirm = page.locator('#confirm-password');
  await expect(page.locator('#password-helper')).toHaveText(
    'Use 15 to 128 characters. Avoid common or account-related words.');
  await expect(newPassword).toHaveAttribute('aria-describedby', 'password-helper new-password-error');

  // Mismatch: the message sits beside Confirm password, which takes the focus.
  await fillBoth(page, GOOD_PASSWORD, `${GOOD_PASSWORD}x`);
  await submitButton(page).click();
  await expect(page.locator('#confirm-password-error')).toHaveText(MISMATCH);
  await expect(confirm).toHaveAttribute('aria-invalid', 'true');
  await expect(confirm).toHaveAttribute('aria-describedby', 'confirm-password-error');
  await expect(confirm).toBeFocused();
  await expect(newPassword).not.toHaveAttribute('aria-invalid');
  await expect(page.locator('#new-password-error')).toHaveText('');
  await expect(newPassword).toHaveValue(GOOD_PASSWORD);
  await expect(confirm).toHaveValue(`${GOOD_PASSWORD}x`);
  await expect(page.locator('#form-alert')).toBeHidden();

  // Typing clears the message.
  await confirm.fill(GOOD_PASSWORD);
  await expect(page.locator('#confirm-password-error')).toHaveText('');
  await expect(confirm).not.toHaveAttribute('aria-invalid');

  // Too short: the server's own sentence, on leaving the field and on submit.
  await newPassword.fill('x'.repeat(14));
  await newPassword.press('Tab');
  await expect(page.locator('#new-password-error')).toHaveText(TOO_SHORT);
  await expect(newPassword).toHaveAttribute('aria-invalid', 'true');
  await confirm.fill('y'.repeat(14));
  await submitButton(page).click();
  await expect(page.locator('#new-password-error')).toHaveText(TOO_SHORT);
  await expect(page.locator('#confirm-password-error')).toHaveText(MISMATCH);
  await expect(newPassword).toBeFocused();
  await expect(page).toHaveURL(new RegExp(`${PAGE}$`));
  expect(seen.submits).toHaveLength(0);
  await shot(page, 'bootstrap_error');

  // Length is counted after NFKC, in code points, as the server counts it:
  // 14 astral characters are 28 UTF-16 units and still too short; one ligature
  // that NFKC opens into three letters makes 13 + 3 = 16.
  await fillBoth(page, '\u{1F600}'.repeat(14));
  await submitButton(page).click();
  await expect(page.locator('#new-password-error')).toHaveText(TOO_SHORT);
  await page.route((url) => url.pathname === API_PATH, async (route) => {
    await route.fulfill({ status: 500, body: '' });
  });
  await fillBoth(page, `${'z'.repeat(13)}ﬃ`);
  await submitButton(page).click();
  await expect(page.locator('#new-password-error')).toHaveText('');
  await expect.poll(() => seen.submits.length).toBe(1);
});

test('Show and Hide reveal each password on its own, and both fields are new-password fields', async ({ page }) => {
  await standInForCloudflare(page);
  await openForm(page);
  for (const [field, label] of [['new-password', 'New password'], ['confirm-password', 'Confirm password']]) {
    const input = page.locator(`#${field}`);
    const toggle = page.locator(`#${field}-toggle`);
    await expect(input).toHaveAttribute('type', 'password');
    await expect(input).toHaveAttribute('autocomplete', 'new-password');
    await expect(page.locator(`label[for="${field}"]`)).toHaveText(label);
    await expect(page.getByRole('textbox', { name: label, exact: true })).toHaveAttribute('id', field);
    await expect(toggle).toHaveText('Show');
    await expect(toggle).toHaveAttribute('aria-pressed', 'false');
    await expect(toggle).toHaveAttribute('aria-controls', field);
    await expect(page.getByRole('button', { name: `Show ${label}`, exact: true })).toHaveCount(1);
    await input.fill(GOOD_PASSWORD);
    await toggle.click();
    await expect(input).toHaveAttribute('type', 'text');
    await expect(toggle).toHaveText('Hide');
    await expect(toggle).toHaveAttribute('aria-pressed', 'true');
    await expect(input).toHaveValue(GOOD_PASSWORD);
    await toggle.click();
    await expect(input).toHaveAttribute('type', 'password');
    await expect(toggle).toHaveText('Show');
    await expect(toggle).toHaveAttribute('aria-pressed', 'false');
  }
  // Confirm password is not a form field the browser could send.
  await expect(page.locator('#confirm-password')).not.toHaveAttribute('name');
});

test('security check refused by the server (401): its text is shown, the widget is reset and the form stays usable', async ({ page }) => {
  await standInForCloudflare(page, { token: 'a-token-the-verifier-refuses' });
  await openForm(page);
  await fillBoth(page, GOOD_PASSWORD);
  const response = await submitAndWait(page);
  expect(response.status()).toBe(401);
  const body = await response.json();
  expect(body).toEqual({ error: VERIFICATION_FAILED });

  await expectAlert(page, body.error);
  await expect(page.locator('#form-alert')).not.toHaveText(SIGN_IN_GENERIC);
  await expect(page).toHaveURL(new RegExp(`${PAGE}$`));
  await expect(page.locator('#bootstrap-form')).toBeVisible();
  await expect(page.locator('#new-password')).toHaveValue(GOOD_PASSWORD);
  await expect(page.locator('#confirm-password')).toHaveValue(GOOD_PASSWORD);
  await expect(page.locator('#new-password')).not.toHaveAttribute('aria-invalid');
  // The spent token is gone: the widget ran again and only then is submit back.
  await expect.poll(async () => (await widgetState(page)).resets).toBe(1);
  await expect(submitButton(page)).toBeEnabled();
  await expect(page.locator('#bootstrap-form')).not.toHaveAttribute('aria-busy');
});

test('password refused by the server (400): its text is shown under New password and the link stays usable', async ({ page }) => {
  await standInForCloudflare(page);
  await openForm(page);
  await fillBoth(page, REFUSED_PASSWORD);
  const response = await submitAndWait(page);
  expect(response.status()).toBe(400);
  const body = await response.json();
  expect(body).toEqual({ error: TOO_COMMON, reason: 'common_or_context' });

  // Only the three contract fields were sent, with the anti-forgery header and the exact origin.
  const request = response.request();
  expect(request.postDataJSON()).toEqual({
    token: LIVE_TOKEN,
    newPassword: REFUSED_PASSWORD,
    turnstileToken: PASS_TOKEN,
  });
  const sent = await request.allHeaders();
  expect(sent['origin']).toBe('https://admin.core.dealoware.com');
  expect(sent['x-csrf-token']).toMatch(/^[0-9A-F]{64}$/);
  expect(sent['content-type']).toBe('application/json');
  expect(sent['referer']).toBeUndefined();

  const field = page.locator('#new-password');
  await expect(page.locator('#new-password-error')).toHaveText(body.error);
  await expect(field).toHaveAttribute('aria-invalid', 'true');
  await expect(field).toBeFocused();
  await expect(field).toHaveValue(REFUSED_PASSWORD);
  await expect(page.locator('#confirm-password')).toHaveValue(REFUSED_PASSWORD);
  await expect(page.locator('#form-alert')).toBeHidden();
  await expect(page).toHaveURL(new RegExp(`${PAGE}$`));
  await expect.poll(async () => (await widgetState(page)).resets).toBe(1);
  await expect(submitButton(page)).toBeEnabled();
  await shot(page, 'bootstrap_server_error');
});

test('throttled reply (429): the server message is shown as sent', async ({ page }) => {
  const message = 'Slow down. Wait a moment and try again.';
  await standInForCloudflare(page);
  await page.route((url) => url.pathname === API_PATH, async (route) => {
    await route.fulfill({ status: 429, contentType: 'application/json', body: JSON.stringify({ message }) });
  });
  await openForm(page);
  await fillBoth(page, GOOD_PASSWORD);
  await submitButton(page).click();
  await expectAlert(page, message);
  await expect(page.locator('#bootstrap-form')).toBeVisible();
  await expect(page.locator('#new-password')).toHaveValue(GOOD_PASSWORD);
  await expect(submitButton(page)).toBeEnabled();
});

test('network failure, an unexpected reply and a time-out show the retry sentence and leave the form usable', async ({ page }) => {
  await page.clock.install();
  const seen = watch(page);
  await standInForCloudflare(page);
  let n = 0;
  await page.route((url) => url.pathname === API_PATH, async (route) => {
    n += 1;
    if (n === 1) {
      await route.abort('connectionfailed');
    } else if (n === 2) {
      await route.fulfill({ status: 500, contentType: 'text/plain', body: 'Internal Server Error' });
    } else {
      await new Promise(() => { /* never answered: the page gives up after 15 s */ });
    }
  });
  await openForm(page);
  await fillBoth(page, GOOD_PASSWORD);

  for (const attempt of [1, 2, 3]) {
    await expect(submitButton(page)).toBeEnabled();
    await submitButton(page).click();
    if (attempt === 3) {
      await expect(page.locator('#bootstrap-form')).toHaveAttribute('aria-busy', 'true');
      await page.clock.fastForward(15000);
    }
    await expectAlert(page, SUBMIT_RETRY);
    await expect(page.locator('#bootstrap-form')).toBeVisible();
    await expect(page.locator('#bootstrap-form')).not.toHaveAttribute('aria-busy');
    await expect(page.locator('#new-password')).toHaveValue(GOOD_PASSWORD);
    await expect(page.locator('#confirm-password')).toHaveValue(GOOD_PASSWORD);
  }
  expect(seen.submits).toHaveLength(3);
  await expect(page).toHaveURL(new RegExp(`${PAGE}$`));
});

test('a second submit while one is in flight sends nothing', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  let release: (() => void) | undefined;
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  await page.route((url) => url.pathname === API_PATH, async (route) => {
    await gate;
    await route.fulfill({
      status: 400,
      contentType: 'application/json',
      body: JSON.stringify({ error: TOO_COMMON, reason: 'common_or_context' }),
    });
  });
  await openForm(page);
  await fillBoth(page, GOOD_PASSWORD);
  const form = page.locator('#bootstrap-form');
  await submitButton(page).click();
  await expect(form).toHaveAttribute('aria-busy', 'true');
  await expect(submitButton(page)).toBeDisabled();

  await submitButton(page).click({ force: true });
  await page.locator('#confirm-password').press('Enter');
  await page.evaluate(() => (document.getElementById('bootstrap-form') as HTMLFormElement).requestSubmit());
  expect(seen.submits).toHaveLength(1);

  release!();
  await expect(page.locator('#new-password-error')).toHaveText(TOO_COMMON);
  await expect(form).not.toHaveAttribute('aria-busy');
  expect(seen.submits).toHaveLength(1);
});

test('usable at 320 px wide and at 200% zoom: no sideways scrolling, every control in view', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 640 });
  await standInForCloudflare(page);
  await openForm(page);
  await expect(submitButton(page)).toBeEnabled();
  await expectNoHorizontalScroll(page);
  // The standard 300 px widget does not fit this column, so the compact one is asked for.
  expect((await widgetState(page)).size).toBe('compact');
  for (const selector of [
    'h1', '#new-password', '#new-password-toggle', '#confirm-password', '#confirm-password-toggle',
    '#turnstile-widget iframe', '#bootstrap-submit',
  ]) {
    const box = await page.locator(selector).boundingBox();
    expect(box, selector).not.toBeNull();
    expect(box!.x, selector).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width, selector).toBeLessThanOrEqual(320);
    // WCAG 2.2 target size: at least 24 by 24 CSS px for the controls.
    if (selector !== 'h1') {
      expect(box!.width, selector).toBeGreaterThanOrEqual(24);
      expect(box!.height, selector).toBeGreaterThanOrEqual(24);
    }
  }
  await shot(page, 'bootstrap_320');

  // With both messages and the alert showing, still no sideways scrolling.
  await page.route((url) => url.pathname === API_PATH, async (route) => {
    await route.fulfill({ status: 429, contentType: 'application/json', body: JSON.stringify({ message: SIGN_IN_GENERIC }) });
  });
  await fillBoth(page, GOOD_PASSWORD);
  await submitButton(page).click();
  await expectAlert(page, SIGN_IN_GENERIC);
  await fillBoth(page, 'short', 'different');
  await expect(submitButton(page)).toBeEnabled();
  await submitButton(page).click();
  await expect(page.locator('#new-password-error')).toHaveText(TOO_SHORT);
  await expectNoHorizontalScroll(page);
  await shot(page, 'bootstrap_320_error');

  // A 1280 px window zoomed to 200% lays out as 640 CSS px.
  await page.setViewportSize({ width: 640, height: 400 });
  await expectNoHorizontalScroll(page);
  await submitButton(page).scrollIntoViewIfNeeded();
  await expect(submitButton(page)).toBeInViewport();
});

test('axe finds nothing serious or critical in the form, its errors, the alert and a failed security check', async ({ page }) => {
  await standInForCloudflare(page);
  await page.route((url) => url.pathname === API_PATH, async (route) => {
    await route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: JSON.stringify({ error: VERIFICATION_FAILED }),
    });
  });
  await openForm(page);
  await expect(submitButton(page)).toBeEnabled();
  await expectNoSeriousAxeFindings(page, 'S-A4-form');

  await fillBoth(page, 'short', 'different');
  await submitButton(page).click();
  await expect(page.locator('#confirm-password-error')).toHaveText(MISMATCH);
  await expectNoSeriousAxeFindings(page, 'S-A4-field-errors');

  await page.locator('#new-password-toggle').click();
  await fillBoth(page, GOOD_PASSWORD);
  await submitButton(page).click();
  await expectAlert(page, VERIFICATION_FAILED);
  await expectNoSeriousAxeFindings(page, 'S-A4-alert');

  await widgetSays(page, 'error-callback');
  await expect(page.locator('#turnstile-retry')).toBeVisible();
  await expectNoSeriousAxeFindings(page, 'S-A4-security-check-failed');
});

test('an expired link, and a link that was never issued, land on the link-expired page', async ({ page }) => {
  await standInForCloudflare(page);
  for (const token of [EXPIRED_TOKEN, crypto.randomBytes(32).toString('hex')]) {
    await openForm(page, token);
    await fillBoth(page, GOOD_PASSWORD);
    const response = await submitAndWait(page);
    expect(response.status()).toBe(303);
    expect(response.headers()['location']).toBe('/admin/link-expired?for=setup');
    await expectLinkExpiredPage(page);
  }
});

test('happy path, keyboard only: the real server sets the password and the page goes to sign-in', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  await openForm(page);
  await expect(submitButton(page)).toBeEnabled();

  // No mouse from here on. Tab from the first field to the submit button, typing on the way.
  await expect(page.locator('#new-password')).toBeFocused();
  await page.keyboard.type(GOOD_PASSWORD);
  const order = ['new-password'];
  for (let i = 0; i < 8 && order[order.length - 1] !== 'bootstrap-submit'; i += 1) {
    await page.keyboard.press('Tab');
    const stop = await page.evaluate(() => {
      const el = document.activeElement;
      return el ? el.id || el.tagName.toLowerCase() : '';
    });
    order.push(stop);
    if (stop === 'confirm-password') await page.keyboard.type(GOOD_PASSWORD);
    if (stop === 'new-password-toggle') {
      // Space works the toggle, and works it back.
      await page.keyboard.press('Space');
      await expect(page.locator('#new-password')).toHaveAttribute('type', 'text');
      await page.keyboard.press('Space');
      await expect(page.locator('#new-password')).toHaveAttribute('type', 'password');
    }
  }
  // The security check's own frame may take a stop between the fields and the button.
  expect(order.filter((stop) => stop !== 'iframe')).toEqual([
    'new-password', 'new-password-toggle', 'confirm-password', 'confirm-password-toggle', 'bootstrap-submit',
  ]);
  const ring = await submitButton(page).evaluate((el) => {
    const style = getComputedStyle(el);
    return { style: style.outlineStyle, width: style.outlineWidth, offset: style.outlineOffset, colour: style.outlineColor };
  });
  expect(ring).toEqual({ style: 'solid', width: '2px', offset: '2px', colour: 'rgb(11, 79, 138)' });

  const [response] = await Promise.all([
    page.waitForResponse(isSubmitReply),
    page.keyboard.press('Enter'),
  ]);
  // The reply body ({"passwordSet":true,"next":"sign-in"}) is pinned by the .NET tests;
  // the page leaves on the status alone and the browser drops the body with it.
  expect(response.status()).toBe(200);
  await expect(page).toHaveURL(new RegExp(`${SIGN_IN_PATH}$`));

  expect(seen.submits).toHaveLength(1);
  expect(seen.submits[0].postDataJSON()).toEqual({
    token: LIVE_TOKEN,
    newPassword: GOOD_PASSWORD,
    turnstileToken: PASS_TOKEN,
  });
  for (const url of seen.urls) expect(url).not.toContain(LIVE_TOKEN);
  expect(seen.referers).toEqual([]);
});

test('the link that was just used lands on the link-expired page', async ({ page }) => {
  await standInForCloudflare(page);
  await openForm(page);
  await fillBoth(page, GOOD_PASSWORD);
  const response = await submitAndWait(page);
  expect(response.status()).toBe(303);
  await expectLinkExpiredPage(page);
});

test.describe('JavaScript off', () => {
  test.use({ javaScriptEnabled: false });

  test('with JavaScript off the page shows the heading, one sentence and no form', async ({ page }) => {
    const seen = watch(page);
    await page.goto(`${PAGE}#token=${encodeURIComponent(LIVE_TOKEN)}`);
    await expect(page.locator('h1')).toHaveText(HEADING);
    await expect(page.getByText(NOSCRIPT, { exact: true })).toBeVisible();
    await expect(page.locator('#bootstrap-panel')).toBeHidden();
    await expect(page.locator('#bootstrap-form')).toBeHidden();
    await expect(page.locator('#new-password')).toBeHidden();
    await expect(submitButton(page)).toHaveCount(0);
    const lines = (await page.locator('body').innerText()).split('\n').map((line) => line.trim()).filter(Boolean);
    expect(lines).toEqual([HEADING, NOSCRIPT]);
    expect(seen.cloudflare).toEqual([]);
    await shot(page, 'bootstrap_js_off');
  });
});

test('throttled by the real server (429): the page shows the server message, with no wait time', async ({ page }) => {
  await standInForCloudflare(page);
  await openForm(page, crypto.randomBytes(32).toString('hex'));
  // Use up this address's setup budget with links that were never issued.
  const throttled = await page.evaluate(async ({ api, turnstileToken }) => {
    const csrf = document.querySelector('meta[name="dw-csrf"]')!.getAttribute('content')!;
    for (let i = 1; i <= 40; i += 1) {
      const r = await fetch(api, {
        method: 'POST',
        redirect: 'manual',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
        body: JSON.stringify({ token: `never-issued-${i}`, newPassword: '', turnstileToken }),
      });
      if (r.status === 429) return { after: i, body: await r.json(), retryAfter: r.headers.get('Retry-After') };
    }
    return null;
  }, { api: API_PATH, turnstileToken: PASS_TOKEN });
  expect(throttled).not.toBeNull();
  expect(throttled!.body).toEqual({ message: SIGN_IN_GENERIC });
  expect(throttled!.retryAfter).toBeNull();

  await fillBoth(page, GOOD_PASSWORD);
  const response = await submitAndWait(page);
  expect(response.status()).toBe(429);
  expect(response.headers()['retry-after']).toBeUndefined();
  await expectAlert(page, SIGN_IN_GENERIC);
  await expect(page.locator('#bootstrap-form')).toBeVisible();
  await expect(page.locator('#new-password')).toHaveValue(GOOD_PASSWORD);
  await expect(page).toHaveURL(new RegExp(`${PAGE}$`));
});
