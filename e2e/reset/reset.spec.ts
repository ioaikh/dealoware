import { test, expect, type Locator, type Page, type Request } from '@playwright/test';
import { spawnSync } from 'node:child_process';
import crypto from 'node:crypto';
import path from 'node:path';
import { expectNoSeriousAxeFindings } from './axe';
import {
  RUN_DATABASE, authenticatorCode, seedOwner, wrongAuthenticatorCode, type Owner,
} from '../sign-in/owner';
import { answerCloudflare, widgetSays, widgetState, type WidgetSetup } from '../signed-out/cloudflare';

/**
 * The password-reset pages against the real app: S-A8 /admin/reset and S-A9
 * /admin/reset/sent, and what they hand over to: S-A5 /admin/link-expired,
 * S-A10 /admin/reset/confirm, and S-A1 opening with one sentence (the
 * password was changed; S-A11, the session ended).
 * Start it with e2e/reset/run.sh.
 *
 * As in the other signed-out suites, the browser never reaches Cloudflare:
 * every request to it is answered with a stand-in widget
 * (e2e/signed-out/cloudflare.ts) under the page's real policy, and the
 * server checks the widget's token with the runner's stand-in verifier,
 * which accepts exactly one run-time token.
 *
 * Before every test the owner is seeded afresh with a password and an
 * enrolled authenticator (e2e/sign-in/owner.ts), and the reset rows are put
 * back (seed_reset.py): no link but the ones the test asks for, and the
 * address's budget unspent. So the tests do not depend on their order.
 *
 * What a reset link, a wrong code and a throttled address get from the
 * server is proven by the .NET tests, and S-A10's own states by
 * e2e/reset-confirm. This file proves what only a browser can: that the
 * pages work, in three engines, under their policy.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const SIGN_IN = '/admin/sign-in';
const RESET = '/admin/reset';
const SENT = '/admin/reset/sent';
const CONFIRM = '/admin/reset/confirm';
const EXPIRED = '/admin/link-expired';
const API_RESET = '/admin/api/auth/reset';
const THE_PAGES = [RESET, SENT, CONFIRM, EXPIRED, SIGN_IN];

const RESET_TITLE = 'Reset password · Dealoware admin';
const RESET_HEADING = 'Reset password';
const SENT_TITLE = 'Check your email · Dealoware admin';
const SENT_HEADING = 'Check your email';
const SENT_EXPIRY = 'The link expires in 1 hour.';
const EXPIRED_HEADING = 'This link no longer works';
const EXPIRED_RESET = 'Reset links work once and expire after 1 hour.';
const EXPIRED_SETUP =
  'Setup links work once and expire after 24 hours. Ask for a new setup link through the same channel you got this one.';
const NOSCRIPT = 'Turn on JavaScript to set your password.';
const EMAIL_REQUIRED = 'Enter an email address.';
const EMAIL_INVALID = 'Enter a well-formed email address.';
const RETRY = 'That didn\'t go through. Wait a moment and try again.';
// The server's sentences. The pages show them as sent.
const NEUTRAL = 'If that account exists, we\'ve sent instructions.';
const VERIFICATION_FAILED = 'Verification failed, please try again.';
const SIGN_IN_GENERIC =
  'We couldn\'t sign you in. Check your details and try again later. You can also reset your password.';
const PASSWORD_CHANGED = 'Your password has been changed. Sign in with your new password and authenticator code.';
const SESSION_ENDED = 'Your session has ended. Sign in again to continue.';
const CHECK_FAILED = 'The security check failed.';
const CHECK_EXPIRED = 'The security check expired.';

const PASS_TOKEN = process.env.RESET_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.RESET_E2E_ARTIFACTS;
const PYTHON = process.env.SIGN_IN_E2E_PYTHON ?? '';

/** How a test answers one POST of the reset request. */
type Answer =
  | 'real' // let the server answer
  | 'abort' // the connection fails
  | { status: number; json?: unknown; text?: string };

let owner: Owner;
let cspMessages: string[] = [];
let pageErrors: string[] = [];

function engine(): string {
  return test.info().project.name;
}

/** WebKit, like Safari as delivered, stops at form controls when Tab is pressed and passes links by. */
function tabStopsAtLinks(): boolean {
  return engine() !== 'webkit';
}

/**
 * A screenshot of the page as it is. To take one in WebKit, Playwright adds a
 * style sheet of its own for a moment, which these pages' policy rightly
 * refuses; that console line is about the picture, not the page.
 */
async function shot(page: Page, name: string): Promise<void> {
  if (!ARTIFACTS) return;
  const before = cspMessages.length;
  await page.screenshot({ path: `${ARTIFACTS}/${name}.png`, fullPage: true });
  if (engine() === 'webkit') {
    await page.waitForTimeout(150);
    cspMessages = cspMessages.filter((text, index) => index < before || !/apply a stylesheet/i.test(text));
  }
}

/** Stores the owner's password as a reset finds it, the given links as live ones, and an unspent budget. */
function seedReset(tokens: string[] = []): void {
  const run = spawnSync(PYTHON, [path.join(__dirname, 'seed_reset.py')], {
    input: JSON.stringify({
      db: RUN_DATABASE,
      email: owner.email,
      tokenHashes: tokens.map((token) => crypto.createHash('sha256').update(token, 'utf8').digest('hex').toUpperCase()),
    }),
    encoding: 'utf8',
    timeout: 30_000,
    windowsHide: true,
  });
  // Never the input: it holds the hashes.
  if (run.error || run.status !== 0) throw new Error(`seeding the reset rows failed: ${run.error?.message ?? run.stderr}`);
}

function standInForCloudflare(page: Page, setup: WidgetSetup = {}): Promise<{ scripts: number }> {
  return answerCloudflare(page, { token: PASS_TOKEN, ...setup });
}

/** Decides what each reset request gets, and keeps what was sent. Without it every request goes to the server. */
async function answerRequests(
  page: Page,
  answer: (n: number) => Answer | Promise<Answer>,
): Promise<{ sent: Record<string, unknown>[] }> {
  const seen = { sent: [] as Record<string, unknown>[] };
  await page.route((url) => url.pathname === API_RESET, async (route) => {
    seen.sent.push((route.request().postDataJSON() ?? {}) as Record<string, unknown>);
    const reply = await answer(seen.sent.length);
    if (reply === 'real') await route.continue();
    else if (reply === 'abort') await route.abort('connectionfailed');
    else if (reply.json !== undefined) {
      await route.fulfill({ status: reply.status, contentType: 'application/json', body: JSON.stringify(reply.json) });
    } else await route.fulfill({ status: reply.status, contentType: 'text/plain', body: reply.text ?? '' });
  });
  return seen;
}

function isResetRequest(request: Request): boolean {
  return new URL(request.url()).pathname === API_RESET && request.method() === 'POST';
}

function sendButton(page: Page): Locator {
  return page.getByRole('button', { name: 'Send reset link', exact: true });
}

/** S-A8 with the security check answered: the form is on screen and can be sent. */
async function openReset(page: Page, setup: WidgetSetup = {}): Promise<void> {
  await standInForCloudflare(page, setup);
  await page.goto(RESET);
  await expect(page.locator('#reset-request-form')).toBeVisible();
}

/** The one alert of S-A8: the text, announced, with the focus on it, and the page still S-A8. */
async function expectAlert(page: Page, text: string): Promise<void> {
  const alert = page.locator('#form-alert');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveText(text);
  await expect(alert).toBeFocused();
  await expect(page.locator('[role="alert"]')).toHaveCount(1);
  expect(page.url()).toBe(ORIGIN + RESET);
}

/** S-A9 as every visitor gets it. Returns its markup, to compare one arrival with another. */
async function expectSentPage(page: Page): Promise<string> {
  await expect(page).toHaveURL(ORIGIN + SENT);
  await expect(page).toHaveTitle(SENT_TITLE);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText(SENT_HEADING);
  await expect(page.locator('#page-heading')).toBeFocused();
  await expect(page.locator('#reset-sent-message')).toHaveText(NEUTRAL);
  await expect(page.locator('#reset-sent-expiry')).toHaveText(SENT_EXPIRY);
  // The way back and nothing else: no form, no resend.
  await expect(page.locator('a')).toHaveCount(1);
  await expect(page.getByRole('link', { name: 'Back to sign in', exact: true })).toHaveAttribute('href', SIGN_IN);
  await expect(page.locator('form, input, button')).toHaveCount(0);
  return await page.locator('main').innerHTML();
}

async function expectNoHorizontalScroll(page: Page): Promise<void> {
  const size = await page.evaluate(() => ({
    content: document.documentElement.scrollWidth,
    viewport: document.documentElement.clientWidth,
  }));
  expect(size.content).toBeLessThanOrEqual(size.viewport);
}

/** Where the keyboard focus is: the element's id, or its tag name when it has none. */
async function focusStop(page: Page): Promise<string> {
  return await page.evaluate(() => {
    const el = document.activeElement;
    return el ? el.id || el.tagName.toLowerCase() : '';
  });
}

test.beforeEach(async ({ page }) => {
  owner = seedOwner({ enrolled: true });
  seedReset();
  cspMessages = [];
  pageErrors = [];
  page.on('console', (message) => {
    const text = message.text();
    // Chromium and WebKit write "Content Security Policy", Firefox "Content-Security-Policy".
    if (!/content[- ]security[- ]policy/i.test(text)) return;
    // Firefox asks for /favicon.ico by itself, and the pages' default-src 'none' refuses it.
    if (text.includes('/favicon.ico')) return;
    if (!THE_PAGES.includes(new URL(page.url()).pathname)) return;
    cspMessages.push(text);
  });
  page.on('pageerror', (error) => pageErrors.push(error.message));
});

test.afterEach(() => {
  // Nothing the pages load, and nothing the tests stand in for, may be refused by a page's own policy.
  expect(cspMessages).toEqual([]);
  expect(pageErrors).toEqual([]);
});

test('S-A8 is HTML under a nonce policy that allows only Cloudflare, styled, with the cursor in Email', async ({ page }) => {
  const counts = await standInForCloudflare(page);
  const response = await page.goto(RESET);
  expect(response!.status()).toBe(200);
  const csp = (await response!.headersArray()).filter((h) => h.name.toLowerCase() === 'content-security-policy');
  expect(csp).toHaveLength(1);
  const nonce = (await response!.text()).match(/<link rel="stylesheet" href="\/admin\/auth\/signed-out\.css" nonce="([^"]+)">/)![1];
  expect(csp[0].value).toBe(
    `default-src 'none'; script-src 'nonce-${nonce}' https://challenges.cloudflare.com; style-src 'nonce-${nonce}'; `
    + 'connect-src \'self\'; frame-src https://challenges.cloudflare.com; '
    + 'frame-ancestors \'none\'; base-uri \'none\'; form-action \'self\'; object-src \'none\'');
  expect(response!.headers()['cache-control']).toBe('no-store');

  await expect(page.locator('#reset-request-form')).toBeVisible();
  await expect(page).toHaveTitle(RESET_TITLE);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText(RESET_HEADING);
  await expect(page.locator('#email')).toBeFocused();
  await expect(page.locator('#form-alert')).toBeHidden();
  await expect(page.locator('nav')).toHaveCount(0);

  // The linked stylesheet applied under style-src 'nonce-...'.
  const colours = await page.evaluate(() => ({
    card: getComputedStyle(document.querySelector('.auth-card')!).backgroundColor,
    button: getComputedStyle(document.getElementById('reset-submit')!).backgroundColor,
  }));
  expect(colours).toEqual({ card: 'rgb(255, 255, 255)', button: 'rgb(11, 79, 138)' });

  // The widget came from Cloudflare's origin, once, and sits directly above the button it enables.
  await expect(sendButton(page)).toBeEnabled();
  expect(counts.scripts).toBe(1);
  await expect(page.frameLocator('#security-check-widget iframe').getByText('Security check stand-in')).toBeVisible();
  expect(await page.evaluate(() => (window as any).__e2eWidgetRefused)).toBeUndefined();
  expect(await page.evaluate(() => document.getElementById('security-check')!.nextElementSibling!.id)).toBe('reset-submit');

  // The way back to sign-in.
  await expect(page.getByRole('link', { name: 'Back to sign in', exact: true })).toHaveAttribute('href', SIGN_IN);

  await expectNoSeriousAxeFindings(page, 'reset_request');
  await shot(page, 'reset_request');
});

test('S-A8 sends nothing without a fresh security-check token, and says what state the check is in', async ({ page }) => {
  const seen = await answerRequests(page, () => 'real');
  await openReset(page, { auto: false });
  await page.locator('#email').fill(owner.email);

  // Not solved yet: the button is shut, and Enter in the field sends nothing either.
  await expect(sendButton(page)).toBeDisabled();
  await page.locator('#email').press('Enter');
  await widgetSays(page, 'error-callback');
  await expect(page.locator('#security-check-status')).toHaveText(CHECK_FAILED);
  await expect(sendButton(page)).toBeDisabled();
  await page.locator('#email').press('Enter');
  await expect(page.locator('#form-alert')).toBeHidden();
  await expectNoSeriousAxeFindings(page, 'reset_request_check_failed');

  // Solved, then expired: shut again.
  await widgetSays(page, 'callback', PASS_TOKEN);
  await expect(sendButton(page)).toBeEnabled();
  await widgetSays(page, 'expired-callback');
  await expect(page.locator('#security-check-status')).toHaveText(CHECK_EXPIRED);
  await expect(sendButton(page)).toBeDisabled();
  await page.locator('#email').press('Enter');
  expect(seen.sent).toHaveLength(0);

  // Retry asks the widget again; with a fresh token the request goes, once, with that token.
  await page.getByRole('button', { name: 'Retry', exact: true }).click();
  expect((await widgetState(page)).resets).toBe(1);
  await widgetSays(page, 'callback', PASS_TOKEN);
  await expect(page.locator('#security-check-status')).toHaveText('');
  await sendButton(page).click();
  await expect(page).toHaveURL(ORIGIN + SENT);
  expect(seen.sent).toEqual([{ email: owner.email, turnstileToken: PASS_TOKEN }]);
});

test('S-A8 catches an empty and a malformed email in the page, next to the field', async ({ page }) => {
  const seen = await answerRequests(page, () => 'real');
  await openReset(page);
  await expect(sendButton(page)).toBeEnabled();
  const email = page.locator('#email');

  for (const [typed, message] of [['', EMAIL_REQUIRED], ['alex.doe', EMAIL_INVALID], ['alex doe@example.test', EMAIL_INVALID]]) {
    await email.fill(typed);
    await sendButton(page).click();
    await expect(page.locator('#email-error')).toHaveText(message);
    await expect(email).toHaveAttribute('aria-invalid', 'true');
    await expect(email).toHaveAttribute('aria-describedby', 'email-error');
    await expect(email).toBeFocused();
    await expect(page.locator('#form-alert')).toBeHidden();
  }
  await expectNoSeriousAxeFindings(page, 'reset_request_invalid');
  expect(seen.sent).toHaveLength(0);

  // Typing again takes the error away.
  await email.fill('a');
  await expect(page.locator('#email-error')).toHaveText('');
  await expect(email).not.toHaveAttribute('aria-invalid', 'true');
});

test('S-A8 to S-A9: the owner\'s address and an unknown one get the same reply and the same page', async ({ page }) => {
  const pages: string[] = [];
  // The status of each reply. That the two bodies are the same bytes is the .NET tests' to prove:
  // here the page has moved on before a body could be read.
  const replies: number[] = [];
  page.on('response', (response) => {
    if (isResetRequest(response.request())) replies.push(response.status());
  });
  const seen = await answerRequests(page, () => 'real');
  await standInForCloudflare(page);

  for (const address of [owner.email, 'nobody@example.test']) {
    await page.goto(RESET);
    await expect(page.locator('#reset-request-form')).toBeVisible();
    await page.locator('#email').fill(`  ${address} `);
    await expect(sendButton(page)).toBeEnabled();
    await sendButton(page).click();
    pages.push(await expectSentPage(page));
    expect(new URL(page.url()).search).toBe('');
    expect(new URL(page.url()).hash).toBe('');
  }

  // One request each, with the two contract fields and nothing else; one answer for both.
  expect(seen.sent).toEqual([
    { email: owner.email, turnstileToken: PASS_TOKEN },
    { email: 'nobody@example.test', turnstileToken: PASS_TOKEN },
  ]);
  expect(replies).toEqual([200, 200]);
  expect(pages[1]).toBe(pages[0]);
  expect(pages[0]).not.toContain('@');
  expect(await page.evaluate(() => window.localStorage.length + window.sessionStorage.length)).toBe(0);

  await expectNoSeriousAxeFindings(page, 'reset_sent');
  await shot(page, 'reset_sent');

  // S-A9 is the same page opened directly, and its one link leads back to sign-in.
  await page.goto(SENT);
  expect(await expectSentPage(page)).toBe(pages[0]);
  await page.getByRole('link', { name: 'Back to sign in', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await expect(page.locator('#form-status')).toBeHidden();
});

test('S-A8 shows a refused security check in the server\'s words and asks the widget again', async ({ page }) => {
  // The widget hands over a token the verifier does not accept.
  const seen = await answerRequests(page, () => 'real');
  await openReset(page, { token: 'not-the-token-the-verifier-accepts' });
  await page.locator('#email').fill(owner.email);
  await sendButton(page).click();

  await expectAlert(page, VERIFICATION_FAILED);
  await expect(page.locator('#email')).toHaveValue(owner.email);
  expect((await widgetState(page)).resets).toBe(1);
  expect(seen.sent).toHaveLength(1);
  await expectNoSeriousAxeFindings(page, 'reset_request_refused');
});

test('S-A8 stays put on a throttled or unclear reply, sends once while busy, and moves on only for the exact reply', async ({ page }) => {
  let release: (answer: Answer) => void = () => {};
  const held = new Promise<Answer>((resolve) => { release = resolve; });
  const answers: (Answer | Promise<Answer>)[] = [
    { status: 429, json: { message: NEUTRAL } }, // the throttle: the server's text, unchanged
    { status: 500, text: 'oops' },
    { status: 200, json: {} }, // a bare 200 is not the reply
    { status: 200, json: { message: 'Anything else.' } },
    'abort',
    held,
  ];
  const seen = await answerRequests(page, (n) => answers[n - 1]);
  await openReset(page);
  await page.locator('#email').fill(owner.email);

  for (const expected of [NEUTRAL, RETRY, RETRY, RETRY, RETRY]) {
    await expect(sendButton(page)).toBeEnabled();
    await sendButton(page).click();
    await expectAlert(page, expected);
    await expect(page.locator('#email')).toHaveValue(owner.email);
  }
  expect((await widgetState(page)).resets).toBe(5);

  // While a request is on its way the form is busy and a second Enter sends nothing.
  await expect(sendButton(page)).toBeEnabled();
  await page.locator('#email').press('Enter');
  await expect(page.locator('#reset-request-form')).toHaveAttribute('aria-busy', 'true');
  await expect(sendButton(page)).toBeDisabled();
  await page.locator('#email').press('Enter');
  expect(seen.sent).toHaveLength(6);
  release('real');
  await expect(page).toHaveURL(ORIGIN + SENT);
  expect(seen.sent).toHaveLength(6);
});

test('S-A8 loads itself anew once when its anti-forgery pair is stale, then says the request did not go through', async ({ page }) => {
  const seen = await answerRequests(page, () => ({ status: 403, json: { error: 'Forbidden' } }));
  await openReset(page);
  await page.locator('#email').fill(owner.email);
  await sendButton(page).click();

  // Reloaded: the form is empty again, the sentence shows, and the flag is out of the address bar.
  await expectAlert(page, RETRY);
  await expect(page.locator('#email')).toHaveValue('');
  expect(seen.sent).toHaveLength(1);

  // A second 403 does not reload again: what was typed stays.
  await page.locator('#email').fill(owner.email);
  await expect(sendButton(page)).toBeEnabled();
  await sendButton(page).click();
  await expect.poll(() => seen.sent.length).toBe(2);
  await expectAlert(page, RETRY);
  await expect(page.locator('#email')).toHaveValue(owner.email);
});

test('S-A8 and S-A9 by keyboard only, with a visible focus at every stop', async ({ page }) => {
  await openReset(page);
  await expect(page.locator('#email')).toBeFocused();
  await page.keyboard.type(owner.email);
  await expect(sendButton(page)).toBeEnabled();

  // Email, the widget's frame, the button, the way back.
  const stops: string[] = [];
  for (let i = 0; i < 4 && stops[stops.length - 1] !== (tabStopsAtLinks() ? 'back-link' : 'reset-submit'); i += 1) {
    await page.keyboard.press('Tab');
    const stop = await focusStop(page);
    stops.push(stop);
    if (stop === 'reset-submit' || stop === 'back-link') {
      const ring = await page.evaluate(() => {
        const style = getComputedStyle(document.activeElement!);
        return { style: style.outlineStyle, width: style.outlineWidth };
      });
      expect(ring).toEqual({ style: 'solid', width: '2px' });
      const box = await page.locator(`#${stop}`).boundingBox();
      expect(box!.height).toBeGreaterThanOrEqual(24);
    }
  }
  expect(stops.filter((stop) => stop === 'reset-submit' || stop === 'back-link'))
    .toEqual(tabStopsAtLinks() ? ['reset-submit', 'back-link'] : ['reset-submit']);

  // Back to the button, and send with the keyboard.
  await page.locator('#reset-submit').focus();
  await page.keyboard.press('Enter');
  await expectSentPage(page);

  // From the heading, the next stop is the way back; Enter follows it.
  if (tabStopsAtLinks()) await page.keyboard.press('Tab');
  else await page.locator('#back-link').focus();
  await expect(page.locator('#back-link')).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
});

test('S-A8 fits a 320 px screen, with the compact widget', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 640 });
  await openReset(page);
  await expect(sendButton(page)).toBeEnabled();
  expect((await widgetState(page)).size).toBe('compact');
  await expectNoHorizontalScroll(page);
  await shot(page, 'reset_request_320');
});

test('S-A5 on the shared page shell: the reset wording with its way to a new link, the setup wording without one', async ({ page }) => {
  await standInForCloudflare(page);

  // A dead reset link ends here.
  await page.goto(`${CONFIRM}#token=${crypto.randomBytes(32).toString('base64url')}`);
  await expect(page).toHaveURL(`${ORIGIN}${EXPIRED}?for=reset`);
  await expect(page).toHaveTitle(`${EXPIRED_HEADING} · Dealoware admin`);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText(EXPIRED_HEADING);
  await expect(page.locator('#page-heading')).toBeFocused();
  await expect(page.locator('#link-expired-body')).toHaveText(EXPIRED_RESET);
  await expect(page.locator('form, input, button')).toHaveCount(0);
  await expect(page.locator('a')).toHaveCount(1);
  const card = await page.evaluate(() => getComputedStyle(document.querySelector('.auth-card')!).backgroundColor);
  expect(card).toBe('rgb(255, 255, 255)');
  await expectNoSeriousAxeFindings(page, 'link_expired_reset');
  await shot(page, 'link_expired_reset');

  await page.getByRole('link', { name: 'Request a new link', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + RESET);
  await expect(page.locator('#reset-request-form')).toBeVisible();

  await page.goto(`${EXPIRED}?for=setup`);
  await expect(page.locator('h1')).toHaveText(EXPIRED_HEADING);
  await expect(page.locator('#page-heading')).toBeFocused();
  await expect(page.locator('#link-expired-body')).toHaveText(EXPIRED_SETUP);
  await expect(page.locator('a, form, input, button')).toHaveCount(0);
  await expectNoSeriousAxeFindings(page, 'link_expired_setup');
});

test('a reset from the link to sign-in: S-A10 on the shared page shell, a wrong code, the right one, and S-A1 says the password was changed', async ({ page }) => {
  const token = crypto.randomBytes(32).toString('base64url');
  const newPassword = crypto.randomBytes(12).toString('hex');
  seedReset([token]);
  await standInForCloudflare(page);

  await page.goto(`${CONFIRM}#token=${token}`);
  await expect(page.locator('#reset-form')).toBeVisible();
  expect(new URL(page.url()).hash).toBe('');
  await expect(page).toHaveTitle('Set a new password · Dealoware admin');
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText('Set a new password');
  await expect(page.locator('#newPassword')).toBeFocused();
  const colours = await page.evaluate(() => ({
    card: getComputedStyle(document.querySelector('.auth-card')!).backgroundColor,
    button: getComputedStyle(document.querySelector('#reset-form button[type="submit"]')!).backgroundColor,
  }));
  expect(colours).toEqual({ card: 'rgb(255, 255, 255)', button: 'rgb(11, 79, 138)' });
  await expectNoHorizontalScroll(page);
  await expectNoSeriousAxeFindings(page, 'reset_confirm');
  await shot(page, 'reset_confirm');

  // A wrong code: the server's one sentence, the new password kept.
  await page.locator('#newPassword').fill(newPassword);
  await page.locator('#totp').fill(wrongAuthenticatorCode(owner.secret!));
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  const alert = page.locator('#reset-alert');
  await expect(alert).toHaveText(SIGN_IN_GENERIC);
  await expect(alert).toHaveAttribute('role', 'alert');
  await expect(alert).toBeFocused();
  await expect(page.locator('#newPassword')).toHaveValue(newPassword);
  await expectNoSeriousAxeFindings(page, 'reset_confirm_refused');

  // The right code: on to sign-in, which says so in the server's words, in a status line, not an alert.
  await page.locator('#totp').fill(authenticatorCode(owner.secret!));
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  const status = page.locator('#form-status');
  await expect(status).toBeVisible();
  await expect(status).toHaveText(PASSWORD_CHANGED);
  await expect(status).toHaveAttribute('role', 'status');
  await expect(page.locator('#form-alert')).toBeHidden();
  await expect(page.locator('#email')).toBeFocused();
  await expectNoSeriousAxeFindings(page, 'sign-in_password_changed');
  await shot(page, 'sign-in_password_changed');

  // The link is spent: opened again, it ends on S-A5.
  await page.goto(`${CONFIRM}#token=${token}`);
  await expect(page).toHaveURL(`${ORIGIN}${EXPIRED}?for=reset`);
});

test('S-A11: sign-in opened after a session ended says so once, and the flag carries nothing', async ({ page }) => {
  await standInForCloudflare(page);
  await page.goto(`${SIGN_IN}#ended`);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  expect(page.url()).toBe(ORIGIN + SIGN_IN);
  const status = page.locator('#form-status');
  await expect(status).toBeVisible();
  await expect(status).toHaveText(SESSION_ENDED);
  await expect(status).toHaveAttribute('role', 'status');
  await expect(page.locator('#form-alert')).toBeHidden();
  await expect(page.locator('#email')).toBeFocused();
  await expectNoSeriousAxeFindings(page, 'sign-in_session_ended');
  await shot(page, 'sign-in_session_ended');

  // A new try takes the sentence away: the answer to that try stands alone.
  await page.locator('#email').fill(owner.email);
  await page.locator('#password').fill('not-the-password');
  await page.getByRole('button', { name: 'Continue', exact: true }).click();
  await expect(page.locator('#form-alert')).toHaveText(SIGN_IN_GENERIC);
  await expect(status).toBeHidden();

  // A reload has no flag any more, and any other fragment means nothing.
  await page.reload();
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await expect(status).toBeHidden();
  await page.goto('about:blank');
  await page.goto(`${SIGN_IN}#ended&next=https://example.test/`);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await expect(status).toBeHidden();
  await expect(page.locator('#form-alert')).toBeHidden();
});

test.describe('JavaScript off', () => {
  test.use({ javaScriptEnabled: false });

  test('with JavaScript off, S-A8 shows its heading and one sentence, and S-A9 and S-A5 read the same and keep their links', async ({ page }) => {
    await page.goto(RESET);
    await expect(page.locator('#reset-request-form')).toBeHidden();
    const shown = (await page.locator('main').innerText()).split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
    expect(shown).toEqual([RESET_HEADING, NOSCRIPT]);

    // The two pages that are only a message need no script: the text and the links are in the page.
    await page.goto(SENT);
    await expect(page.locator('h1')).toHaveText(SENT_HEADING);
    await expect(page.locator('#reset-sent-message')).toHaveText(NEUTRAL);
    await expect(page.locator('#reset-sent-expiry')).toHaveText(SENT_EXPIRY);
    await page.getByRole('link', { name: 'Back to sign in', exact: true }).click();
    await expect(page).toHaveURL(ORIGIN + SIGN_IN);

    await page.goto(`${EXPIRED}?for=reset`);
    await expect(page.locator('h1')).toHaveText(EXPIRED_HEADING);
    await expect(page.locator('#link-expired-body')).toHaveText(EXPIRED_RESET);
    await page.getByRole('link', { name: 'Request a new link', exact: true }).click();
    await expect(page).toHaveURL(ORIGIN + RESET);
  });
});
