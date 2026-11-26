import { test, expect, type Locator, type Page, type Request } from '@playwright/test';
import crypto from 'node:crypto';
import { expectNoSeriousAxeFindings } from './axe';
import { collectPageErrors } from './page-errors';
import { authenticatorCode, seedOwner, wrongAuthenticatorCode, type Owner } from '../sign-in/owner';
import { answerCloudflare } from '../signed-out/cloudflare';
import { readQr } from '../enrol/qr-reader';

/**
 * S-A12 Security settings (/admin/settings/security) and the UXR-A40
 * recovery-code banner, against the real app (T04). Start it with
 * e2e/console/run.sh (it runs with the console suite).
 *
 * Every sign-in goes through the real pages, with the seeded owner's real
 * authenticator key or one of its real recovery codes (e2e/sign-in/owner.ts).
 * The step-ups use the owner's recovery codes: the sign-in has just spent the
 * authenticator's current step, and the server refuses a step it has seen.
 *
 * What the server answers is proven by the .NET tests
 * (AdminSecuritySettingsTests, AdminSecuritySettingsPageTests). This file
 * proves what only a browser can: the page drives those answers as the
 * S-A12 note v1.4 and the test design say, in three engines, under its policy.
 * Where a reply cannot be had from the real server in a browser test (a lock
 * that is active, an activity log of more than 20 rows, an empty one), the
 * reply is stood in for, and the test says so.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const HOME = '/admin/';
const PAGE = '/admin/settings/security';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const RECOVERY = '/admin/sign-in/recovery';
const API = '/admin/api/settings/security';
const API_PASSWORD = `${API}/password`;
const API_CODES = `${API}/recovery-codes`;
const API_AUTHENTICATOR = `${API}/authenticator`;
const API_CONFIRM = `${API}/authenticator/confirm`;
const API_ACTIVITY = `${API}/activity`;
const API_DISMISS = `${API}/recovery-notice/dismiss`;
const API_ME = '/admin/api/me';

const TITLE = 'Security settings · Dealoware admin';
const STEP_UP_FAILED = 'We couldn\'t confirm it\'s you. Check your current password and code.';
const STEP_UP_REQUIRED = 'Confirm it\'s you to continue.';
const PASSWORD_CHANGED = 'Your password has been changed. Sign in with your new password and authenticator code.';
const NEW_CODE_FAILED = 'That code didn\'t work. Enter the current code from your authenticator app.';
const MISMATCH = 'These passwords don\'t match.';
const SIGN_IN_GENERIC = 'We couldn\'t sign you in. Check your details and try again later. You can also reset your password.';
const SESSION_ENDED = 'Your session has ended. Sign in again to continue.';
const QR_LABEL = 'Authenticator QR code for Dealoware admin. Scan with your authenticator app.';
const CODES_LEDE = 'Save these codes now. They are shown once.';
const AUTHENTICATOR_CHANGED = 'Your authenticator app has been changed. Your old recovery codes no longer work.';
const COPIED = 'Copied.';
const EMPTY_ACTIVITY = 'No sign-in activity yet.';
const SKIP = 'Skip to main content';
const ET_TIME = /^[A-Z][a-z]{2} \d{1,2}, \d{4}, \d{1,2}:\d{2} (AM|PM) ET$/;
const RECOVERY_CODE = /^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$/;
const QUIET_ZONE = 4;

const PASS_TOKEN = process.env.CONSOLE_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.CONSOLE_E2E_ARTIFACTS;

let owner: Owner;
let cspMessages: string[] = [];
let pageErrors: () => string[] = () => [];

function engine(): string {
  return test.info().project.name;
}

/** WebKit, like Safari as delivered, stops at form controls when Tab is pressed and passes links by. */
function tabStopsAtLinks(): boolean {
  return engine() !== 'webkit';
}

async function shot(page: Page, name: string): Promise<void> {
  if (!ARTIFACTS) return;
  const before = cspMessages.length;
  await page.screenshot({ path: `${ARTIFACTS}/${name}.png`, fullPage: true });
  if (engine() === 'webkit') {
    await page.waitForTimeout(150);
    cspMessages = cspMessages.filter((text, index) => index < before || !/apply a stylesheet/i.test(text));
  }
}

function isPostTo(apiPath: string): (request: Request) => boolean {
  return (request) => new URL(request.url()).pathname === apiPath && request.method() === 'POST';
}

/** A new password the rules accept: hex only, so NFKC leaves it as it is, and no word of the account. */
function newPasswordValue(): string {
  return crypto.randomBytes(12).toString('hex');
}

/** Step 1 and step 2 through the real pages; step 2 with the authenticator, or with a recovery code. */
async function signInHere(page: Page, with2fa: { recovery?: string } = {}): Promise<void> {
  if (new URL(page.url()).pathname !== SIGN_IN) await page.goto(SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await page.locator('#email').fill(owner.email);
  await page.locator('#password').fill(owner.password);
  const continueButton = page.getByRole('button', { name: 'Continue', exact: true });
  await expect(continueButton).toBeEnabled();
  await continueButton.click();
  await expect(page).toHaveURL(ORIGIN + CODE, { timeout: 15_000 });
  if (with2fa.recovery) {
    await page.getByRole('link', { name: 'Use a recovery code instead', exact: true }).click();
    await expect(page).toHaveURL(ORIGIN + RECOVERY);
    await page.locator('#recovery-code').fill(with2fa.recovery);
  } else {
    await page.locator('#code').fill(authenticatorCode(owner.secret!));
  }
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
}

async function signIn(page: Page, with2fa: { recovery?: string } = {}): Promise<void> {
  await signInHere(page, with2fa);
  await expect(page).toHaveURL(ORIGIN + HOME);
  await page.waitForLoadState('load');
}

/** The page's stylesheets apply: a nav link has the shell's minimum height, a section its card border. */
async function expectStyled(page: Page): Promise<void> {
  await page.waitForLoadState('load');
  const link = page.getByRole('navigation', { name: 'Admin' }).getByRole('link').first();
  await expect.poll(() => link.evaluate((el) => getComputedStyle(el).minHeight)).toBe('40px');
  const section = page.locator('.settings-section').first();
  if (await section.count()) {
    await expect.poll(() => section.evaluate((el) => getComputedStyle(el).borderTopStyle)).toBe('solid');
  }
}

/** S-A12, open and filled from the server: the count of unused codes and the activity panel. */
async function openSettings(page: Page, codesLeft = '10 codes left'): Promise<void> {
  await page.goto(PAGE);
  await expect(page).toHaveTitle(TITLE);
  await expect(page.locator('#security-settings')).toBeVisible();
  await expect(page.locator('#codes-left')).toHaveText(codesLeft);
  await expect(page.locator('#activity-wrap:not([hidden]), #activity-empty:not([hidden])')).toHaveCount(1);
  await expectStyled(page);
}

async function expectSignedOut(page: Page): Promise<void> {
  const cookies = await page.context().cookies();
  expect(cookies.find((c) => c.name === 'dw_admin_session')).toBeUndefined();
  expect((await page.request.get(API_ME)).status()).toBe(401);
}

async function expectNoHorizontalScroll(page: Page): Promise<void> {
  const size = await page.evaluate(() => ({
    content: document.documentElement.scrollWidth,
    viewport: document.documentElement.clientWidth,
  }));
  expect(size.content).toBeLessThanOrEqual(size.viewport);
}

/** Nothing about the codes, the key or what was typed is kept by the browser for this site. */
async function expectNothingStored(page: Page): Promise<void> {
  expect(await page.evaluate(() => localStorage.length + sessionStorage.length)).toBe(0);
  expect(new URL(page.url()).search).toBe('');
  expect(new URL(page.url()).hash).toBe('');
}

const ringOf = (locator: Locator) => locator.evaluate((el) => {
  const style = getComputedStyle(el);
  return { style: style.outlineStyle, width: style.outlineWidth };
});

/** Opens the step-up dialog from a button, and fills it: the password and one of the owner's recovery codes. */
async function stepUpWithRecoveryCode(page: Page, password: string, code: string): Promise<void> {
  const dialog = page.getByRole('dialog', { name: 'Confirm it\'s you' });
  await expect(dialog).toBeVisible();
  await page.locator('#step-up-password').fill(password);
  await page.locator('#step-up-factor-switch').click();
  await expect(page.locator('#step-up-recovery-code')).toBeFocused();
  await page.locator('#step-up-recovery-code').fill(code);
}

async function codesOnScreen(list: Locator): Promise<string[]> {
  return (await list.locator('li').allTextContents()).map((c) => c.trim());
}

/** The 20 bytes a base32 key stands for (RFC 4648). */
function keyBytes(base32: string): Buffer {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  const bytes: number[] = [];
  let buffer = 0;
  let bits = 0;
  for (const letter of base32) {
    const value = alphabet.indexOf(letter);
    if (value < 0) throw new Error('not base32');
    buffer = (buffer << 5) | value;
    bits += 5;
    if (bits >= 8) {
      bits -= 8;
      bytes.push((buffer >> bits) & 0xff);
    }
  }
  return Buffer.from(bytes);
}

/** The QR code as the browser painted it: every module's middle asked what is drawn there. */
async function paintedQr(page: Page): Promise<boolean[][]> {
  const qr = page.locator('#authenticator-qr');
  await qr.scrollIntoViewIfNeeded();
  const seen = await qr.evaluate((svg) => {
    const box = svg.getBoundingClientRect();
    const units = (svg as SVGSVGElement).viewBox.baseVal.width;
    const step = box.width / units;
    const grid: boolean[][] = [];
    let offScreen = 0;
    for (let r = 0; r < units; r += 1) {
      const row: boolean[] = [];
      for (let c = 0; c < units; c += 1) {
        const hit = document.elementFromPoint(box.left + (c + 0.5) * step, box.top + (r + 0.5) * step);
        if (!hit || !svg.contains(hit)) offScreen += 1;
        row.push(!!hit && svg.contains(hit) && hit.tagName.toLowerCase() === 'path');
      }
      grid.push(row);
    }
    return { grid, units, offScreen };
  });
  expect(seen.offScreen).toBe(0);
  return seen.grid.slice(QUIET_ZONE, seen.units - QUIET_ZONE).map((row) => row.slice(QUIET_ZONE, seen.units - QUIET_ZONE));
}

/** Waits until the authenticator shows a code of a step the server has not seen yet. */
async function nextStep(page: Page): Promise<void> {
  const step = Math.floor(Date.now() / 30_000);
  await expect.poll(() => Math.floor(Date.now() / 30_000), { timeout: 35_000, intervals: [500] }).toBeGreaterThan(step);
  await page.waitForTimeout(250);
}

/** Stands in for the browser's clipboard: it accepts, and keeps what it was handed. */
async function acceptingClipboard(page: Page): Promise<void> {
  await page.evaluate(() => {
    const written: string[] = [];
    (window as any).__e2eClipboard = written;
    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText(text: string) { written.push(text); return Promise.resolve(); } },
      configurable: true,
    });
  });
}

async function clipboardWritten(page: Page): Promise<string[]> {
  return await page.evaluate(() => (window as any).__e2eClipboard);
}

test.beforeEach(async ({ page }) => {
  owner = seedOwner({ enrolled: true });
  cspMessages = [];
  page.on('console', (message) => {
    const text = message.text();
    if (!/content[- ]security[- ]policy/i.test(text)) return;
    if (text.includes('/favicon.ico')) return;
    const path = new URL(page.url()).pathname;
    if (path.startsWith('/admin/api/') || path.startsWith('/admin/assets/')) return;
    cspMessages.push(text);
  });
  pageErrors = collectPageErrors(page);
  await answerCloudflare(page, { token: PASS_TOKEN });
});

test.afterEach(() => {
  expect(cspMessages).toEqual([]);
  expect(pageErrors()).toEqual([]);
});

test('S-A12 opens from the nav: four sections, the server\'s count, the activity panel without a raw address, and no banner', async ({ page }) => {
  test.setTimeout(60_000);
  await signIn(page);
  await expect(page.locator('#recovery-notice')).toHaveCount(0);
  const response = page.waitForResponse((r) => new URL(r.url()).pathname === PAGE && r.request().method() === 'GET');
  await page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Security settings', exact: true }).click();
  const served = await response;
  expect(served.status()).toBe(200);
  const headers = await served.allHeaders();
  expect(headers['cache-control']).toBe('no-store');
  expect(headers['content-security-policy']).toMatch(/^default-src 'none'; script-src 'nonce-[^']+'; style-src 'nonce-[^']+';/);
  await expect(page.locator('#codes-left')).toHaveText('10 codes left');
  await expectStyled(page);

  await expect(page.locator('h1')).toHaveText('Security settings');
  await expect(page.getByRole('navigation', { name: 'Admin' }).locator('[aria-current="page"]')).toHaveAttribute('href', PAGE);
  await expect(page.locator('main h2:visible')).toHaveText(['Password', 'Recovery codes', 'Authenticator', 'Recent sign-in activity']);
  // The shell's live region first, then each form's own and each field's error line, all polite.
  expect(await page.locator('[aria-live]').evaluateAll((els) => els.map((e) => `${e.id}:${e.getAttribute('aria-live')}`))).toEqual([
    'console-status:polite',
    'password-alert:polite', 'current-password-error:polite', 'new-password-error:polite', 'confirm-password-error:polite',
    'password-code-error:polite', 'password-recovery-code-error:polite',
    'codes-alert:polite', 'authenticator-alert:polite', 'new-key-alert:polite', 'new-key-code-error:polite',
    'activity-alert:polite',
    'step-up-alert:polite', 'step-up-password-error:polite', 'step-up-code-error:polite', 'step-up-recovery-code-error:polite',
  ]);
  await expect(page.getByRole('button', { name: 'Change password', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Create new codes', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Set up again', exact: true })).toBeVisible();
  // The step-up dialog is in the page, closed.
  await expect(page.getByRole('dialog')).toHaveCount(0);

  // Recent sign-in activity: this sign-in is on it, its time in ET, its network a short hashed prefix.
  const table = page.getByRole('table', { name: /Your last 20 sign-in events/ });
  await expect(table).toBeVisible();
  await expect(table.getByRole('columnheader')).toHaveText(['Time', 'Event', 'Network (hashed)']);
  const rows = table.locator('tbody tr');
  expect(await rows.count()).toBeGreaterThan(0);
  expect(await rows.count()).toBeLessThanOrEqual(20);
  await expect(rows.first().locator('td').nth(1)).toHaveText('Signed in');
  await expect(rows.first().locator('td').first()).toHaveText(ET_TIME);
  await expect(rows.first().locator('time')).toHaveAttribute('datetime', /^\d{4}-\d{2}-\d{2}T/);
  for (const network of await rows.locator('td.network').allTextContents()) {
    expect(network).toMatch(/^[A-Za-z0-9+/=]{1,12}$/);
    expect(network).not.toMatch(/\d+\.\d+\.\d+\.\d+|::/);
  }
  await expect(page.locator('#lock-notice')).toBeHidden();
  await expect(page.locator('#activity-empty')).toBeHidden();
  await expect(page.locator('main')).not.toContainText('127.0.0.1');

  // The page's own files came from this site only; nothing is stored.
  const scripts = await page.locator('script').evaluateAll((els) => els.map((e) => e.getAttribute('src')));
  expect(scripts).toEqual([
    '/admin/auth/admin-request.js', '/admin/assets/console.js', '/admin/assets/session-timer.js',
    '/admin/auth/admin-dom.js', '/admin/auth/admin-password.js', '/admin/auth/admin-clipboard.js',
    '/admin/assets/security-settings.js',
  ]);
  await expectNothingStored(page);

  await expectNoSeriousAxeFindings(page, 'sa12');
  await shot(page, 'sa12_desktop');

  await page.setViewportSize({ width: 320, height: 800 });
  await expectStyled(page);
  await expectNoHorizontalScroll(page);
  for (const target of await page.locator('main button:visible, main input:visible').all()) {
    const box = (await target.boundingBox())!;
    expect(box.width).toBeGreaterThanOrEqual(24);
    expect(box.height).toBeGreaterThanOrEqual(24);
  }
  await expectNoSeriousAxeFindings(page, 'sa12_320');
  await shot(page, 'sa12_320');
});

test('change password: mismatch on blur and on submit, show toggles, a refused step-up clears the current password and code only, one request at a time', async ({ page }) => {
  test.setTimeout(60_000);
  await signIn(page);
  await openSettings(page);
  const current = page.locator('#current-password');
  const next = page.locator('#new-password');
  const confirm = page.locator('#confirm-password');
  const code = page.locator('#password-code');
  const recovery = page.locator('#password-recovery-code');
  const alert = page.locator('#password-alert');
  const submit = page.getByRole('button', { name: 'Change password', exact: true });
  const chosen = newPasswordValue();

  // Field order and attributes (note v1.4).
  await expect(page.locator('#password-form input:visible')).toHaveCount(4);
  const order = await page.locator('#password-form input:visible').evaluateAll((els) => els.map((e) => e.id));
  expect(order).toEqual(['current-password', 'new-password', 'confirm-password', 'password-code']);
  await expect(current).toHaveAttribute('autocomplete', 'current-password');
  await expect(next).toHaveAttribute('autocomplete', 'new-password');
  await expect(confirm).toHaveAttribute('autocomplete', 'new-password');
  await expect(code).toHaveAttribute('inputmode', 'numeric');
  await expect(page.locator('#password-form')).toHaveAttribute('aria-describedby', 'password-alert');

  // Show / hide on every password field: the label stays, aria-pressed says it.
  for (const id of ['current-password', 'new-password', 'confirm-password']) {
    const toggle = page.locator(`#${id}-toggle`);
    await expect(toggle).toHaveText('Show');
    await toggle.click();
    await expect(page.locator(`#${id}`)).toHaveAttribute('type', 'text');
    await expect(toggle).toHaveAttribute('aria-pressed', 'true');
    await toggle.click();
    await expect(page.locator(`#${id}`)).toHaveAttribute('type', 'password');
    await expect(toggle).toHaveAttribute('aria-pressed', 'false');
  }

  // Mismatch: on blur, and again on submit, which then sends nothing.
  const posts: Request[] = [];
  page.on('request', (r) => { if (isPostTo(API_PASSWORD)(r)) posts.push(r); });
  await next.fill(chosen);
  await confirm.fill(chosen + 'x');
  await code.focus();
  await expect(page.locator('#confirm-password-error')).toHaveText(MISMATCH);
  await expect(confirm).toHaveAttribute('aria-invalid', 'true');
  await current.fill('not the password at all');
  await page.locator('#password-factor-switch').click();
  await expect(recovery).toBeFocused();
  await expect(page.locator('#password-factor-switch')).toHaveText('Use an authenticator code instead');
  await recovery.fill(owner.recoveryCodes[0]);
  await submit.click();
  await expect(page.locator('#confirm-password-error')).toHaveText(MISMATCH);
  await expect(confirm).toBeFocused();
  expect(posts).toHaveLength(0);
  await expectNoSeriousAxeFindings(page, 'sa12_password_mismatch');

  // A wrong current password: one request, however often the button is pressed while it is out.
  await confirm.fill(chosen);
  let release: () => void = () => {};
  const held = new Promise<void>((resolve) => { release = resolve; });
  await page.route((url) => url.pathname === API_PASSWORD, async (route) => {
    await held;
    await route.continue();
  });
  await submit.click();
  await expect(submit).toBeDisabled();
  await page.locator('#password-form').evaluate((form) => (form as HTMLFormElement).requestSubmit());
  release();
  await expect(alert).toHaveText(STEP_UP_FAILED);
  await page.unroute((url) => url.pathname === API_PASSWORD);
  expect(posts).toHaveLength(1);
  const sent = posts[0].postDataJSON();
  expect(Object.keys(sent).sort()).toEqual(['currentPassword', 'newPassword', 'recoveryCode']);
  expect(new URL(posts[0].url()).search).toBe('');
  expect((await posts[0].allHeaders())['x-csrf-token']).toMatch(/^[0-9A-F]{64}$/);

  // Note v1.4 (b): current password and code cleared, the new ones kept, the focus on Current password.
  await expect(current).toHaveValue('');
  await expect(recovery).toHaveValue('');
  await expect(next).toHaveValue(chosen);
  await expect(confirm).toHaveValue(chosen);
  await expect(current).toBeFocused();
  await expect(alert).toHaveAttribute('aria-live', 'polite');
  await expect(submit).toBeEnabled();
  await expectNoSeriousAxeFindings(page, 'sa12_password_refused');
  await shot(page, 'sa12_password_refused');
  // Still signed in: one failure is counted, nothing more.
  expect((await page.request.get(API_ME)).status()).toBe(200);
});

test('change password: a rule reject names the rule under New password and keeps every field; success ends every session and lands on sign-in with the notice', async ({ page, browser }) => {
  // Two browsers and three sign-ins: more than the default 30 s in the slower engines.
  test.setTimeout(90_000);
  await signIn(page);
  // A second browser, signed in too: the change ends its session as well.
  const other = await browser.newContext({
    baseURL: ORIGIN, ignoreHTTPSErrors: true, proxy: { server: process.env.CONSOLE_E2E_PROXY! },
  });
  const otherPage = await other.newPage();
  await answerCloudflare(otherPage, { token: PASS_TOKEN });
  await signIn(otherPage, { recovery: owner.recoveryCodes[9] });

  await openSettings(page, '9 codes left');
  const current = page.locator('#current-password');
  const next = page.locator('#new-password');
  const confirm = page.locator('#confirm-password');
  const recovery = page.locator('#password-recovery-code');

  // A rule the server alone knows: an account word. The server's message, under the field.
  const rejected = 'dealoware-admin-2026!';
  await current.fill(owner.password);
  await next.fill(rejected);
  await confirm.fill(rejected);
  await page.locator('#password-factor-switch').click();
  await recovery.fill(owner.recoveryCodes[0]);
  const refused = page.waitForResponse((r) => isPostTo(API_PASSWORD)(r.request()));
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  expect((await refused).status()).toBe(400);
  await expect(page.locator('#new-password-error')).not.toHaveText('');
  await expect(next).toHaveAttribute('aria-invalid', 'true');
  await expect(next).toBeFocused();
  await expect(current).toHaveValue(owner.password);
  await expect(next).toHaveValue(rejected);
  await expect(confirm).toHaveValue(rejected);
  await expect(recovery).toHaveValue(owner.recoveryCodes[0]);
  await expect(page.locator('#password-alert')).toHaveText('');

  // The same recovery code (a rule reject spends nothing) and an accepted password.
  const chosen = newPasswordValue();
  await next.fill(chosen);
  await confirm.fill(chosen);
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#form-status')).toHaveText(PASSWORD_CHANGED);
  await expectSignedOut(page);
  expect(new URL(page.url()).hash).toBe('');

  // Every session ended: the other browser's next page is sign-in.
  await otherPage.goto(HOME);
  await expect(otherPage).toHaveURL(ORIGIN + SIGN_IN);
  await expect(otherPage.locator('#form-status')).toHaveText(SESSION_ENDED);
  await other.close();

  // The new password signs in (the authenticator is unchanged; a recovery code, since this step is spent).
  owner = { ...owner, password: chosen };
  await signIn(page, { recovery: owner.recoveryCodes[1] });
  await expectNoSeriousAxeFindings(page, 'sign-in_after_password_change');
});

test('create new codes: the step-up dialog refuses without naming the factor, then the new set is shown once; Continue empties it; old codes no longer sign in, new ones do', async ({ page }) => {
  // Two sign-ins after the set is made, each through the real pages: more than 30 s in WebKit.
  test.setTimeout(90_000);
  await signIn(page);
  await openSettings(page);
  const create = page.getByRole('button', { name: 'Create new codes', exact: true });
  const dialog = page.getByRole('dialog', { name: 'Confirm it\'s you' });
  const posts: Request[] = [];
  page.on('request', (r) => { if (isPostTo(API_CODES)(r)) posts.push(r); });

  // No step-up given yet: the dialog opens before anything is sent.
  await create.click();
  await expect(dialog).toBeVisible();
  await expect(page.locator('#step-up-password')).toBeFocused();
  expect(posts).toHaveLength(0);
  await expectNoSeriousAxeFindings(page, 'sa12_step-up');
  await shot(page, 'sa12_step-up');

  // Wrong password, a good recovery code: one sentence that names neither; both cleared; focus on the password.
  // The reply is held while Escape is pressed twice: the dialog waits for it, and shows it.
  await stepUpWithRecoveryCode(page, 'not the password at all', owner.recoveryCodes[0]);
  let release: () => void = () => {};
  const held = new Promise<void>((resolve) => { release = resolve; });
  await page.route((url) => url.pathname === API_CODES, async (route) => {
    await held;
    await route.continue();
  });
  await page.locator('#step-up-submit').click();
  await expect(page.locator('#step-up-submit')).toBeDisabled();
  await page.keyboard.press('Escape');
  await page.keyboard.press('Escape');
  await expect(dialog).toBeVisible();
  expect(await page.locator('#step-up-dialog').evaluate((d) => (d as HTMLDialogElement).open)).toBe(true);
  release();
  await expect(page.locator('#step-up-alert')).toHaveText(STEP_UP_FAILED);
  await page.unroute((url) => url.pathname === API_CODES);
  await expect(page.locator('#step-up-password')).toHaveValue('');
  await expect(page.locator('#step-up-recovery-code')).toHaveValue('');
  await expect(page.locator('#step-up-password')).toBeFocused();
  await expect(dialog).toBeVisible();
  await expectNoSeriousAxeFindings(page, 'sa12_step-up_refused');
  await shot(page, 'sa12_step-up_refused');

  // The right password and a recovery code: the set is made and shown, once.
  await page.locator('#step-up-password').fill(owner.password);
  await page.locator('#step-up-recovery-code').fill(owner.recoveryCodes[0]);
  await page.locator('#step-up-submit').click();
  await expect(dialog).toBeHidden();
  const panel = page.locator('#codes-new');
  const list = page.locator('#codes-new-list');
  await expect(panel).toBeVisible();
  await expect(page.locator('#codes-new-lede')).toHaveText(CODES_LEDE);
  await expect(page.locator('#codes-new-lede')).toBeFocused();
  await expect(list.locator('li')).toHaveCount(10);
  const fresh = await codesOnScreen(list);
  for (const c of fresh) expect(c).toMatch(RECOVERY_CODE);
  expect(new Set(fresh).size).toBe(10);
  await expect(page.locator('#codes-left')).toHaveText('10 codes left');
  expect(posts).toHaveLength(2);
  expect(Object.keys(posts[1].postDataJSON()).sort()).toEqual(['currentPassword', 'recoveryCode']);
  expect(new URL(posts[1].url()).search).toBe('');
  await expectNothingStored(page);

  // Copy all: the codes as shown, one to a line; the outcome in the status line; the button keeps the focus.
  await acceptingClipboard(page);
  await page.getByRole('button', { name: 'Copy all', exact: true }).click();
  await expect(page.locator('#codes-new-copy-status')).toHaveText(COPIED);
  expect(await clipboardWritten(page)).toEqual([fresh.join('\n')]);
  await expect(page.locator('#codes-new-copy')).toBeFocused();
  await expectNoSeriousAxeFindings(page, 'sa12_new_codes');
  await shot(page, 'sa12_new_codes');

  // Continue only once the box is ticked; then the list is gone from the page.
  const done = page.locator('#codes-new-done');
  await expect(done).toBeDisabled();
  await page.getByLabel('I have saved these recovery codes.').first().check();
  await expect(done).toBeEnabled();
  await done.click();
  await expect(panel).toBeHidden();
  await expect(list.locator('li')).toHaveCount(0);
  await expect(create).toBeFocused();

  // Within five minutes the page sends the action alone; the server has issued one set for this
  // step-up, so it asks for a new one, and the page opens the dialog with its sentence.
  await create.click();
  await expect(dialog).toBeVisible();
  await expect(page.locator('#step-up-alert')).toHaveText(STEP_UP_REQUIRED);
  expect(posts).toHaveLength(3);
  expect(posts[2].postDataJSON()).toEqual({});
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(create).toBeFocused();
  await expect(page.locator('#step-up-alert')).toHaveText('');

  // Leaving and coming back by Back shows no code: the page is asked for anew and holds none.
  await page.goto(HOME);
  await page.goBack();
  await expect(page).toHaveURL(ORIGIN + PAGE);
  await expect(page.locator('#codes-left')).toHaveText('10 codes left');
  await expect(list.locator('li')).toHaveCount(0);

  // Sign out; an old code that was never used no longer signs in, a new one does.
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await signInHere(page, { recovery: owner.recoveryCodes[5] });
  await expect(page.locator('#form-alert')).toHaveText(SIGN_IN_GENERIC);
  await expect(page).toHaveURL(ORIGIN + RECOVERY);
  await signIn(page, { recovery: fresh[4] });
  await expect(page.locator('#recovery-notice-text')).toHaveText('You signed in with a recovery code. You have 9 recovery codes left.');
});

test('set up again: the new QR code reads back as the otpauth address of the key shown; a wrong code is refused; the right one changes the authenticator and shows new codes once', async ({ page }) => {
  test.setTimeout(120_000);
  await signIn(page);
  await openSettings(page);
  const start = page.getByRole('button', { name: 'Set up again', exact: true });

  await start.click();
  await stepUpWithRecoveryCode(page, owner.password, owner.recoveryCodes[2]);
  const started = page.waitForResponse((r) => isPostTo(API_AUTHENTICATOR)(r.request()));
  await page.locator('#step-up-submit').click();
  const reply = await started;
  expect(reply.status()).toBe(200);
  expect(reply.headers()['cache-control']).toBe('no-store');

  // The new key, as on S-A6: QR code, the key in groups, the account name.
  const panel = page.locator('#new-key');
  await expect(panel).toBeVisible();
  await expect(page.locator('#new-key-lede')).toBeFocused();
  const qr = page.getByRole('img', { name: QR_LABEL, exact: true });
  await expect(qr).toBeVisible();
  await expect(qr).toHaveAttribute('aria-describedby', 'manual-key-help');
  const key = ((await page.locator('#manual-key').textContent()) ?? '');
  expect(key).toMatch(/^[A-Z2-7]{32}$/);
  await expect(page.locator('#manual-key span')).toHaveCount(8);
  await expect(page.locator('#account-name')).toHaveText(`Dealoware:${owner.email}`);
  const reading = readQr(await paintedQr(page));
  expect(reading.text).toBe(`otpauth://totp/${encodeURIComponent(`Dealoware:${owner.email}`)}?secret=${key}`
    + '&issuer=Dealoware&digits=6&period=30&algorithm=SHA1');
  await expectNothingStored(page);
  await expectNoSeriousAxeFindings(page, 'sa12_new_key');
  await shot(page, 'sa12_new_key');

  // Copy key: the key itself, without the gaps between its groups.
  await acceptingClipboard(page);
  await page.getByRole('button', { name: 'Copy key', exact: true }).click();
  await expect(page.locator('#copy-key-status')).toHaveText(COPIED);
  expect(await clipboardWritten(page)).toEqual([key]);

  // A wrong code: the server's sentence, the field emptied and focused; the key stays.
  const newKey = keyBytes(key);
  const field = page.locator('#new-key-code');
  await field.fill(wrongAuthenticatorCode(newKey));
  await page.getByRole('button', { name: 'Confirm', exact: true }).click();
  await expect(page.locator('#new-key-alert')).toHaveText(NEW_CODE_FAILED);
  await expect(field).toHaveValue('');
  await expect(field).toBeFocused();
  await expect(page.locator('#manual-key')).toHaveText(key);
  await expectNoSeriousAxeFindings(page, 'sa12_new_key_refused');

  // The right code: the authenticator is changed, and a new set of codes is shown once.
  await field.fill(authenticatorCode(newKey));
  const confirmed = page.waitForResponse((r) => isPostTo(API_CONFIRM)(r.request()));
  await page.getByRole('button', { name: 'Confirm', exact: true }).click();
  expect((await confirmed).status()).toBe(200);
  await expect(panel).toBeHidden();
  await expect(page.locator('#authenticator-qr')).toHaveCount(0);
  await expect(page.locator('#manual-key')).toHaveText('');
  await expect(page.locator('#authenticator-alert')).toHaveText(AUTHENTICATOR_CHANGED);
  const list = page.locator('#reenrol-codes-list');
  await expect(list.locator('li')).toHaveCount(10);
  await expect(page.locator('#reenrol-codes-lede')).toBeFocused();
  await expect(page.locator('#codes-left')).toHaveText('10 codes left');
  const fresh = await codesOnScreen(list);
  await expectNoSeriousAxeFindings(page, 'sa12_reenrolled');
  await shot(page, 'sa12_reenrolled');
  await page.locator('#reenrol-codes-saved').check();
  await page.locator('#reenrol-codes-done').click();
  await expect(list.locator('li')).toHaveCount(0);
  await expect(start).toBeFocused();

  // Within the five minutes, Set up again needs no dialog; Cancel takes the key out of the page.
  await start.click();
  await expect(panel).toBeVisible();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  await expect(panel).toBeHidden();
  await expect(page.locator('#manual-key')).toHaveText('');
  await expect(page.locator('#new-key-qr > *')).toHaveCount(0);
  await expect(start).toBeFocused();

  // At sign-in the old key is refused and the new one works (a step the server has not seen yet).
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await nextStep(page);
  await signInHere(page);
  await expect(page.locator('#form-alert')).toHaveText(SIGN_IN_GENERIC);
  owner = { ...owner, secret: newKey, recoveryCodes: fresh };
  await signIn(page);
});

test('a step-up older than five minutes by the page\'s clock: the next action opens the dialog before it sends anything', async ({ page }) => {
  await page.clock.install();
  await signIn(page);
  await openSettings(page);
  const start = page.getByRole('button', { name: 'Set up again', exact: true });
  await start.click();
  await stepUpWithRecoveryCode(page, owner.password, owner.recoveryCodes[3]);
  await page.locator('#step-up-submit').click();
  await expect(page.locator('#new-key')).toBeVisible();
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();

  const sent: Request[] = [];
  page.on('request', (r) => { if (isPostTo(API_AUTHENTICATOR)(r)) sent.push(r); });
  await page.clock.fastForward('05:01');
  await start.click();
  await expect(page.getByRole('dialog', { name: 'Confirm it\'s you' })).toBeVisible();
  expect(sent).toHaveLength(0);
});

test('the recovery-code banner: on the first page after a recovery-code sign-in, with the server\'s count and the Manage link; Dismiss ends it for the session', async ({ page }) => {
  await signIn(page, { recovery: owner.recoveryCodes[6] });
  const banner = page.locator('#recovery-notice');
  await expect(banner).toBeVisible();
  await expect(page.locator('#recovery-notice-text')).toHaveText('You signed in with a recovery code. You have 9 recovery codes left.');
  const manage = page.getByRole('link', { name: 'Manage recovery codes', exact: true });
  await expect(manage).toHaveAttribute('href', PAGE);
  const dismiss = page.getByRole('button', { name: 'Dismiss', exact: true });
  await expect(dismiss).toBeVisible();
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'banner');
  await shot(page, 'banner_desktop');

  // The link opens S-A12, where the same server count is shown; the banner is still there.
  await manage.click();
  await expect(page).toHaveURL(ORIGIN + PAGE);
  await expect(page.locator('#codes-left')).toHaveText('9 codes left');
  await expect(banner).toBeVisible();

  // Dismiss from the keyboard: one POST with the token; the banner leaves; the focus goes to main.
  const posts: Request[] = [];
  page.on('request', (r) => { if (isPostTo(API_DISMISS)(r)) posts.push(r); });
  await dismiss.focus();
  await expect(dismiss).toBeFocused();
  expect(await ringOf(dismiss)).toEqual({ style: 'solid', width: '2px' });
  await page.keyboard.press('Enter');
  await expect(banner).toHaveCount(0);
  await expect(page.locator('main')).toBeFocused();
  expect(posts).toHaveLength(1);
  expect((await posts[0].allHeaders())['x-csrf-token']).toMatch(/^[0-9A-F]{64}$/);

  // Gone for the rest of the session, on every page.
  await page.reload();
  await expect(page.locator('#security-settings')).toBeVisible();
  await expect(banner).toHaveCount(0);
  await page.goto(HOME);
  await expect(page.locator('h1')).toHaveText('Stats');
  await expect(banner).toHaveCount(0);
});

test('the activity panel shows the server\'s empty state, and the lock notice for an active lock and one ended under 24 hours (replies stood in)', async ({ page }) => {
  await signIn(page);
  let reply: object = { items: [], lock: null };
  await page.route((url) => url.pathname === API_ACTIVITY, (route) => route.fulfill({
    status: 200, contentType: 'application/json', headers: { 'cache-control': 'no-store' }, body: JSON.stringify(reply),
  }));

  await openSettings(page);
  await expect(page.locator('#activity-empty')).toHaveText(EMPTY_ACTIVITY);
  await expect(page.locator('#activity-wrap')).toBeHidden();
  await expect(page.locator('#lock-notice')).toBeHidden();
  await expectNoSeriousAxeFindings(page, 'sa12_activity_empty');

  // An active lock: one line, a text label next to the icon, the server's times in ET.
  reply = {
    items: [
      { timestamp: '2026-11-26T15:04:05Z', event: 'lock_start', reasonClass: 'locked', network: 'AbCd+/12EfGh' },
      { timestamp: '2026-11-26T15:03:00Z', event: 'signin.second_factor_failed', reasonClass: 'bad_2fa', network: 'AbCd+/12EfGh' },
      { timestamp: '2026-11-26T15:02:00Z', event: 'some.future_event', reasonClass: null, network: 'Zz' },
    ],
    lock: { active: true, startedAt: '2026-11-26T15:04:05Z', endsAt: '2026-11-26T15:34:05Z' },
  };
  await page.reload();
  const notice = page.locator('#lock-notice');
  await expect(notice).toBeVisible();
  await expect(notice).toHaveText('!Lock notice: Sign-in to this account has been locked since Nov 26, 2026, 10:04 AM ET. It opens again at Nov 26, 2026, 10:34 AM ET.');
  await expect(notice.locator('.lock-icon')).toHaveAttribute('aria-hidden', 'true');
  await expect(page.locator('#activity-rows tr')).toHaveCount(3);
  await expect(page.locator('#activity-rows tr td:nth-child(2)')).toHaveText([
    'Account locked', 'Code failed after correct password', 'some.future_event',
  ]);
  await expect(page.locator('#activity-rows tr').first().locator('td').first()).toHaveText('Nov 26, 2026, 10:04 AM ET');
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'sa12_lock_notice');
  await shot(page, 'sa12_lock_notice');

  // A lock that ended less than 24 hours ago.
  reply = { items: [], lock: { active: false, startedAt: '2026-11-25T20:00:00Z', endsAt: '2026-11-25T20:30:00Z' } };
  await page.reload();
  await expect(notice).toHaveText('!Lock notice: Sign-in to this account was locked from Nov 25, 2026, 3:00 PM ET to Nov 25, 2026, 3:30 PM ET.');
  await expect(page.locator('#activity-empty')).toBeVisible();
});

test('the server ends the session from S-A12: a lock lands on sign-in with the generic sentence, a lost session with the session-ended sentence (replies stood in)', async ({ page }) => {
  test.setTimeout(90_000);
  await signIn(page);
  await openSettings(page);

  // Note v1.4 (b): the lock ends every session; sign-in shows the generic sentence, no duration, no "locked".
  await page.route((url) => url.pathname === API_PASSWORD, (route) => route.fulfill({
    status: 401, contentType: 'application/json',
    body: JSON.stringify({ error: SIGN_IN_GENERIC, reason: 'signed_out' }),
  }));
  const chosen = newPasswordValue();
  await page.locator('#current-password').fill('not the password at all');
  await page.locator('#new-password').fill(chosen);
  await page.locator('#confirm-password').fill(chosen);
  await page.locator('#password-code').fill('123456');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#form-alert')).toHaveText(SIGN_IN_GENERIC);
  await expect(page.locator('body')).not.toContainText(/locked/i);

  // The gate's own 401 on an action: the session has ended.
  await page.unroute((url) => url.pathname === API_PASSWORD);
  await signIn(page, { recovery: owner.recoveryCodes[8] });
  await openSettings(page, '9 codes left');
  await page.route((url) => url.pathname === API_CODES, (route) => route.fulfill({
    status: 401, contentType: 'application/json', body: JSON.stringify({ error: 'Unauthorized' }),
  }));
  // Give a step-up first, so the page sends the action alone.
  await page.getByRole('button', { name: 'Create new codes', exact: true }).click();
  await stepUpWithRecoveryCode(page, owner.password, owner.recoveryCodes[1]);
  await page.locator('#step-up-submit').click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#form-status')).toHaveText(SESSION_ENDED);
});

test('keyboard: the password form and the dialog in order, each control with its focus ring; Escape closes the dialog back to its button', async ({ page }) => {
  await signIn(page);
  await openSettings(page);
  const ring = { style: 'solid', width: '2px' };
  const formOrder = [
    'current-password', 'current-password-toggle', 'new-password', 'new-password-toggle',
    'confirm-password', 'confirm-password-toggle', 'password-code', 'password-factor-switch', 'password-submit',
  ];

  if (tabStopsAtLinks()) {
    await page.locator('#current-password').focus();
    const seen = ['current-password'];
    for (let i = 1; i < formOrder.length; i += 1) {
      await page.keyboard.press('Tab');
      seen.push(await page.evaluate(() => document.activeElement?.id ?? ''));
    }
    expect(seen).toEqual(formOrder);
    await page.keyboard.press('Tab');
    expect(await page.evaluate(() => document.activeElement?.id)).toBe('codes-create');
  }
  // In every engine: each control takes the focus and shows its ring.
  for (const id of [...formOrder, 'codes-create', 'authenticator-start']) {
    const control = page.locator(`#${id}`);
    await control.focus();
    await expect(control).toBeFocused();
    expect(await ringOf(control)).toEqual(ring);
  }
  // The activity table scrolls on its own on a narrow screen, so it takes the focus too.
  await page.locator('#activity-wrap').focus();
  expect(await ringOf(page.locator('#activity-wrap'))).toEqual(ring);

  // The dialog from the keyboard: the focus starts in it, stays in it, and Escape gives it back.
  const create = page.locator('#codes-create');
  await create.focus();
  await page.keyboard.press('Enter');
  const dialog = page.getByRole('dialog', { name: 'Confirm it\'s you' });
  await expect(dialog).toBeVisible();
  await expect(page.locator('#step-up-password')).toBeFocused();
  for (const id of ['step-up-password', 'step-up-password-toggle', 'step-up-code', 'step-up-factor-switch', 'step-up-submit', 'step-up-cancel']) {
    const control = page.locator(`#${id}`);
    await control.focus();
    await expect(control).toBeFocused();
    expect(await ringOf(control)).toEqual(ring);
  }
  // Modal: the page behind it is inert, so nothing there can take the focus.
  expect(await page.evaluate(() => {
    const behind = document.getElementById('codes-create') as HTMLElement;
    behind.focus();
    return document.activeElement === behind;
  })).toBe(false);
  await page.locator('#step-up-password').fill('typed');
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(create).toBeFocused();
  await expect(page.locator('#step-up-password')).toHaveValue('');

  // Escape closes it from every control in it, fields and buttons alike, and empties it.
  for (const id of ['step-up-code', 'step-up-factor-switch', 'step-up-recovery-code', 'step-up-submit']) {
    await create.focus();
    await page.keyboard.press('Enter');
    await expect(dialog).toBeVisible();
    if (id === 'step-up-recovery-code') await page.locator('#step-up-factor-switch').click();
    const control = page.locator(`#${id}`);
    if (id === 'step-up-code' || id === 'step-up-recovery-code') await control.fill('123456');
    await control.focus();
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(create).toBeFocused();
    await expect(page.locator('#step-up-code')).toHaveValue('');
    await expect(page.locator('#step-up-recovery-code')).toHaveValue('');
  }

  // The skip link still leads to main.
  const skip = page.getByRole('link', { name: SKIP, exact: true });
  await skip.focus();
  await page.keyboard.press('Enter');
  await expect(page.locator('main')).toBeFocused();
});

/**
 * Arranges the owner's next move for the moment the dialog closes: in the same Escape key press,
 * after the page's own handler has closed it. The browser sends the dialog's "close" event only
 * later (Chromium with the next frame), so the move is made before the page hears of the close.
 * `closeHandled` then waits until that event has been through the page, and the timers the page
 * set at that moment have run.
 */
async function onEscape(page: Page, move: 'focus' | 'click', id: string): Promise<void> {
  await page.evaluate(([what, target]) => {
    const next = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return;
      document.removeEventListener('keydown', next);
      const control = document.getElementById(target) as HTMLElement;
      if (what === 'focus') control.focus();
      else control.click();
    };
    document.addEventListener('keydown', next);
    (window as any).__e2eCloseHandled = new Promise<void>((resolve) => {
      window.addEventListener('close', () => setTimeout(resolve, 50), { capture: true, once: true });
    });
  }, [move, id] as const);
}

async function closeHandled(page: Page): Promise<void> {
  await page.evaluate(() => (window as any).__e2eCloseHandled);
}

test('the owner goes on as the dialog closes: a focus put elsewhere stays there, and a dialog opened again at once keeps its action', async ({ page }) => {
  await signIn(page);
  await openSettings(page);
  const create = page.locator('#codes-create');
  const start = page.locator('#authenticator-start');
  const dialog = page.getByRole('dialog', { name: 'Confirm it\'s you' });
  const password = page.locator('#step-up-password');

  // Escape, and the focus moved to another button before the "close" event: it is not pulled back.
  await create.focus();
  await page.keyboard.press('Enter');
  await expect(dialog).toBeVisible();
  await password.fill('typed');
  await onEscape(page, 'focus', 'authenticator-start');
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await closeHandled(page);
  await expect(password).toHaveValue('');
  await expect(start).toBeFocused();

  // Escape, and the dialog opened again before the "close" event: that event is the first dialog's.
  // The one now open keeps its action, so Continue sends it and the server's answer is shown.
  await create.focus();
  await page.keyboard.press('Enter');
  await expect(dialog).toBeVisible();
  await onEscape(page, 'click', 'codes-create');
  await page.keyboard.press('Escape');
  await closeHandled(page);
  await expect(dialog).toBeVisible();
  await expect(password).toBeFocused();
  await password.fill('not the password at all');
  await page.locator('#step-up-code').fill('123456');
  const sent = page.waitForRequest(isPostTo(API_CODES));
  await page.locator('#step-up-submit').click();
  await sent;
  await expect(page.locator('#step-up-alert')).toHaveText(STEP_UP_FAILED);

  // Closed by Escape with nothing else done: the focus is back on its button, as before.
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(create).toBeFocused();
});

test('an Escape typed while text is being composed is left alone by the page', async ({ page }) => {
  await signIn(page);
  await openSettings(page);
  await page.locator('#codes-create').click();
  const dialog = page.getByRole('dialog', { name: 'Confirm it\'s you' });
  await expect(dialog).toBeVisible();
  const field = page.locator('#step-up-code');
  await field.fill('123');
  await field.focus();

  // A browser test cannot open an input method's window, so the key event it sends is stood in for:
  // Escape with isComposing set. What is checked is the page's own part: it neither cancels the event
  // nor closes the dialog. What the engine itself then does with a synthetic Escape differs (WebKit
  // closes a modal dialog on it; a real input method keeps the key from the dialog), so the dialog's
  // state afterwards is not asserted for this case.
  await page.locator('#step-up-dialog').evaluate((d) => {
    const counted = d as HTMLDialogElement & { pageCloses?: number };
    counted.pageCloses = 0;
    const close = counted.close.bind(counted);
    counted.close = (value?: string) => { counted.pageCloses! += 1; close(value); };
  });
  const escape = (composing: boolean) => field.evaluate((input, isComposing) => input.dispatchEvent(
    new KeyboardEvent('keydown', { key: 'Escape', isComposing, bubbles: true, cancelable: true })), composing);
  expect(await escape(true)).toBe(true);
  expect(await page.locator('#step-up-dialog').evaluate((d) => (d as HTMLDialogElement & { pageCloses?: number }).pageCloses)).toBe(0);
  await page.locator('#step-up-dialog').evaluate((d) => { if (!(d as HTMLDialogElement).open) (d as HTMLDialogElement).showModal(); });
  await expect(dialog).toBeVisible();

  // The same event outside a composition is the page's to handle: it cancels it, closes the dialog
  // itself and empties it.
  expect(await escape(false)).toBe(false);
  expect(await page.locator('#step-up-dialog').evaluate((d) => (d as HTMLDialogElement & { pageCloses?: number }).pageCloses)).toBe(1);
  await expect(dialog).toBeHidden();
  await expect(field).toHaveValue('');
});

test.describe('JavaScript off', () => {
  test.use({ javaScriptEnabled: false });

  test('with JavaScript off S-A12 shows its heading and one sentence, and no form', async ({ page }) => {
    const html = await (await page.request.get(SIGN_IN)).text();
    const csrf = html.match(/<meta name="dw-csrf" content="([0-9A-F]{64})">/)![1];
    expect((await page.request.post('/admin/api/auth/sign-in', {
      headers: { 'X-CSRF-TOKEN': csrf, Origin: ORIGIN },
      data: { email: owner.email, password: owner.password, turnstileToken: PASS_TOKEN },
    })).status()).toBe(200);
    expect((await page.request.post('/admin/api/auth/sign-in/code', {
      headers: { 'X-CSRF-TOKEN': csrf, Origin: ORIGIN },
      data: { code: authenticatorCode(owner.secret!) },
    })).status()).toBe(200);

    await page.goto(PAGE);
    await expect(page.locator('h1')).toHaveText('Security settings');
    await expect(page.getByText('Turn on JavaScript to change your security settings.', { exact: true })).toBeVisible();
    await expect(page.locator('#security-settings')).toBeHidden();
    await expect(page.locator('input:visible')).toHaveCount(0);
  });
});
