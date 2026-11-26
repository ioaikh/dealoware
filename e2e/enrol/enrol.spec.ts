import { test, expect, type Locator, type Page, type Request, type Response } from '@playwright/test';
import { expectNoSeriousAxeFindings } from './axe';
import { readQr } from './qr-reader';
import { authenticatorCode, seedOwner, wrongAuthenticatorCode, type Owner } from '../sign-in/owner';
import { WIDGET_HOST, answerCloudflare, type WidgetSetup } from '../signed-out/cloudflare';

/**
 * The two pages a first sign-in leads through, against the real app: S-A6
 * /admin/setup/authenticator and S-A7 /admin/setup/recovery-codes.
 * Start it with e2e/enrol/run.sh.
 *
 * Before every test the owner is seeded afresh with a password and no
 * authenticator (the sign-in suite's owner.ts), and nothing left over from
 * the test before. Each test then gets to the pages the way an owner does:
 * by signing in. The authenticator key and the recovery codes are made by
 * the app; the tests read them off the pages, work out the 6-digit code from
 * the key shown, and read the QR code back from what the browser painted.
 *
 * The browser never reaches Cloudflare: the sign-in page's security check is
 * answered with the shared stand-in (e2e/signed-out/cloudflare.ts).
 *
 * The runner starts this file once on each engine: Chromium, Firefox and
 * WebKit.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const AUTHENTICATOR = '/admin/setup/authenticator';
const RECOVERY_CODES = '/admin/setup/recovery-codes';
const HOME = '/admin/';
// A page that needs nothing, to have somewhere to come Back to.
const ELSEWHERE = '/admin/reset';
const API_SIGN_IN = '/admin/api/auth/sign-in';
const API_CODE = '/admin/api/auth/sign-in/code';
const API_SIGN_OUT = '/admin/api/auth/sign-out';
// Where S-A6 asks "is the server answering?" after a reply it could not read.
const API_ME = '/admin/api/me';
const THE_PAGES = [SIGN_IN, CODE, AUTHENTICATOR, RECOVERY_CODES];

const SIGN_IN_HEADING = 'Sign in';
const CODE_HEADING = 'Enter code';
const AUTHENTICATOR_TITLE = 'Authenticator setup · Dealoware admin';
const AUTHENTICATOR_HEADING = 'Authenticator setup';
const AUTHENTICATOR_LEDE =
  'Scan the QR code with your authenticator app, or enter the key by hand. Then enter a 6-digit code to confirm.';
const AUTHENTICATOR_NOSCRIPT = 'Turn on JavaScript to set up your authenticator.';
const QR_LABEL = 'Authenticator QR code for Dealoware admin. Scan with your authenticator app.';
const KEY_HELP = 'If you can\'t scan the code, enter this key in your app instead.';
const RECOVERY_CODES_TITLE = 'Recovery codes · Dealoware admin';
const RECOVERY_CODES_HEADING = 'Recovery codes';
const RECOVERY_CODES_LEDE = 'Save these codes now. They are shown once.';
const RECOVERY_CODES_PURPOSE = 'Each code signs you in once if you can\'t use your authenticator app.';
const RECOVERY_CODES_NOSCRIPT = 'Turn on JavaScript to see your recovery codes.';
const RECOVERY_CODES_ALREADY_SHOWN = 'Your recovery codes have already been shown. They are shown once.';
const SAVED_LABEL = 'I have saved these recovery codes.';
const CODE_REQUIRED = 'Enter the 6-digit code.';
const COPIED = 'Copied.';
const COPY_FAILED = 'Couldn\'t copy. The text is selected so you can copy it yourself.';
const RETRY = 'That didn\'t go through. Wait a moment and try again.';
// The server's sentence for a refused sign-in. The pages show it as sent.
const SIGN_IN_GENERIC =
  'We couldn\'t sign you in. Check your details and try again later. You can also reset your password.';
// The session gate's word when there is no pending sign-in.
const GATE_DENIED = 'Unauthorized';

const QUIET_ZONE = 4;
const RECOVERY_CODE_SHAPE = /^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$/;

const PASS_TOKEN = process.env.ENROL_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.ENROL_E2E_ARTIFACTS;

/** How a test answers one request to an endpoint. */
type Answer =
  | 'real' // let the server answer
  | 'abort' // the connection fails before the server hears of it
  | 'lost' // the server hears and answers, but the answer never arrives
  | { status: number; json?: unknown; text?: string };

let owner: Owner;
let cspMessages: string[] = [];
let pageErrors: string[] = [];
let dialogs: string[] = [];

/** The engine this run is on: chromium, firefox or webkit. */
function engine(): string {
  return test.info().project.name;
}

/** On Windows, WebKit's cookie store does not report SameSite at all (the .NET tests pin what the server sends). */
function reportsSameSite(): boolean {
  return !(engine() === 'webkit' && process.platform === 'win32');
}

/**
 * A screenshot of the page as it is. To take one in WebKit, Playwright adds
 * a style sheet of its own for a moment; the pages' policy refuses it,
 * rightly, and WebKit says so in the console. That line is about the
 * picture, not the page, so it is set aside here.
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

/** Answers everything addressed to Cloudflare; the widget solves at once with the token the verifier accepts. */
function standInForCloudflare(page: Page, setup: WidgetSetup = {}): Promise<{ scripts: number }> {
  return answerCloudflare(page, { token: PASS_TOKEN, ...setup });
}

/** Decides what each request to one path gets. Without it every request goes to the real server. */
async function answer(
  page: Page,
  path: string,
  method: 'GET' | 'POST',
  reply: (n: number) => Answer,
): Promise<{ count: number }> {
  const counts = { count: 0 };
  await page.route((url) => url.pathname === path, async (route) => {
    if (route.request().method() !== method || route.request().isNavigationRequest()) {
      await route.fallback();
      return;
    }
    counts.count += 1;
    const what = reply(counts.count);
    if (what === 'real') {
      await route.fallback();
    } else if (what === 'abort') {
      await route.abort('connectionfailed');
    } else if (what === 'lost') {
      // The server does what was asked; the page never learns of it.
      await route.fetch();
      await route.abort('connectionfailed');
    } else if (what.json !== undefined) {
      await route.fulfill({ status: what.status, contentType: 'application/json', body: JSON.stringify(what.json) });
    } else {
      await route.fulfill({ status: what.status, contentType: 'text/plain', body: what.text ?? '' });
    }
  });
  return counts;
}

function isPostTo(path: string): (request: Request) => boolean {
  return (request) => new URL(request.url()).pathname === path && request.method() === 'POST';
}

function isReplyTo(path: string): (response: Response) => boolean {
  return (response) => isPostTo(path)(response.request());
}

/** Records every request the page makes. */
function watch(page: Page): {
  urls: string[]; referers: string[]; confirms: Request[]; signOuts: Request[]; probes: Request[]; dataRequests: string[];
} {
  const seen = {
    urls: [] as string[],
    referers: [] as string[],
    confirms: [] as Request[],
    signOuts: [] as Request[],
    probes: [] as Request[],
    // Anything a script asked for at the two set-up addresses, as opposed to the browser loading the page.
    dataRequests: [] as string[],
  };
  page.on('request', (request) => {
    const path = new URL(request.url()).pathname;
    seen.urls.push(request.url());
    const referer = request.headers()['referer'];
    if (referer) seen.referers.push(referer);
    if (isPostTo(AUTHENTICATOR)(request)) seen.confirms.push(request);
    if (isPostTo(API_SIGN_OUT)(request)) seen.signOuts.push(request);
    if (path === API_ME) seen.probes.push(request);
    if ((path === AUTHENTICATOR || path === RECOVERY_CODES) && request.method() === 'GET' && !request.isNavigationRequest()) {
      seen.dataRequests.push(request.url());
    }
  });
  return seen;
}

function continueToStepTwo(page: Page): Locator {
  return page.locator('#sign-in-submit');
}

function confirmButton(page: Page): Locator {
  return page.getByRole('button', { name: 'Confirm', exact: true });
}

function copyKeyButton(page: Page): Locator {
  return page.getByRole('button', { name: 'Copy key', exact: true });
}

function copyAllButton(page: Page): Locator {
  return page.getByRole('button', { name: 'Copy all', exact: true });
}

function savedBox(page: Page): Locator {
  return page.getByRole('checkbox', { name: SAVED_LABEL, exact: true });
}

function continueButton(page: Page): Locator {
  return page.locator('#codes-continue');
}

/** Step 1 with the owner's details, through the page. */
async function signInStepOne(page: Page): Promise<void> {
  await page.goto(SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await page.locator('#email').fill(owner.email);
  await page.locator('#password').fill(owner.password);
  await expect(continueToStepTwo(page)).toBeEnabled();
  await continueToStepTwo(page).click();
}

/** A first sign-in, up to the authenticator page: S-A6 is open with a pending sign-in. */
async function reachAuthenticatorPage(page: Page): Promise<void> {
  await standInForCloudflare(page);
  // Somewhere to come Back to: the sign-in page gives its place in the history to S-A6.
  await page.goto(ELSEWHERE);
  await signInStepOne(page);
  await expect(page).toHaveURL(ORIGIN + AUTHENTICATOR);
  await expect(page.locator('#authenticator-form')).toBeVisible();
}

/** The key as S-A6 shows it: 32 base32 characters, without the gaps between its groups. */
async function keyOnPage(page: Page): Promise<string> {
  const key = (await page.locator('#manual-key').textContent()) ?? '';
  expect(key).toMatch(/^[A-Z2-7]{32}$/);
  return key;
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

/** What the QR code must hold for this key: the address authenticator apps read. */
function otpAuthAddress(key: string): string {
  return `otpauth://totp/${encodeURIComponent(`Dealoware:${owner.email}`)}?secret=${key}`
    + '&issuer=Dealoware&digits=6&period=30&algorithm=SHA1';
}

async function confirmAndWait(page: Page): Promise<Response> {
  const [response] = await Promise.all([
    page.waitForResponse(isReplyTo(AUTHENTICATOR)),
    confirmButton(page).click(),
  ]);
  return response;
}

/** S-A6 done with the code for the key on the page: S-A7 is open. Returns that key. */
async function reachRecoveryCodesPage(page: Page): Promise<Buffer> {
  await reachAuthenticatorPage(page);
  const key = keyBytes(await keyOnPage(page));
  await page.locator('#code').fill(authenticatorCode(key));
  await confirmButton(page).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await expect(page.locator('#recovery-codes-list')).toBeVisible();
  return key;
}

async function codesOnPage(page: Page): Promise<string[]> {
  const codes = await page.locator('#recovery-codes-list li').allTextContents();
  expect(codes).toHaveLength(10);
  for (const code of codes) expect(code).toMatch(RECOVERY_CODE_SHAPE);
  expect(new Set(codes).size).toBe(10);
  return codes;
}

/** The one alert of S-A6: the text, announced, with the cursor back in the code field. */
async function expectAlert(page: Page, text: string): Promise<void> {
  const alert = page.locator('#form-alert');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveText(text);
  await expect(alert).toHaveAttribute('role', 'alert');
  await expect(page.locator('[role="alert"]')).toHaveCount(1);
  await expect(page.locator('#code')).toBeFocused();
}

/** Step 1 again, opened because no pending sign-in was left: it says so in the server's sentence. */
async function expectSentBackToStepOne(page: Page): Promise<void> {
  await expect(page.locator('#sign-in-form')).toBeVisible();
  expect(page.url()).toBe(ORIGIN + SIGN_IN);
  await expect(page.locator('h1')).toHaveText(SIGN_IN_HEADING);
  const alert = page.locator('#form-alert');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveText(SIGN_IN_GENERIC);
}

async function expectNotSignedIn(page: Page): Promise<void> {
  const cookies = await page.context().cookies();
  expect(cookies.find((c) => c.name === 'dw_admin_session')).toBeUndefined();
  const me = await page.request.get(API_ME);
  expect(me.status()).toBe(401);
}

async function expectNothingStored(page: Page): Promise<void> {
  expect(await page.evaluate(() => ({
    local: localStorage.length, session: sessionStorage.length, cookie: document.cookie,
  }))).toEqual({ local: 0, session: 0, cookie: '' });
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

type PaintedQr = { modules: boolean[][]; moduleSize: number; width: number; height: number; left: number; right: number };

/**
 * The QR code as the browser painted it. Every module's middle is asked
 * "what is drawn here?": the dark path, or the light square behind it. So
 * this is the picture on the screen, not the markup that was sent. The quiet
 * zone around the symbol must be empty; what is inside it is returned.
 */
async function paintedQr(page: Page): Promise<PaintedQr> {
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
    const path = svg.querySelector('path')!;
    const square = svg.querySelector('rect')!;
    return {
      grid, units, offScreen, step,
      width: box.width, height: box.height, left: box.left, right: box.right,
      unitsHigh: (svg as SVGSVGElement).viewBox.baseVal.height,
      dark: getComputedStyle(path).fill,
      light: getComputedStyle(square).fill,
    };
  });

  // Every module was on screen and inside the picture, which is square, black on white.
  expect(seen.offScreen).toBe(0);
  expect(seen.unitsHigh).toBe(seen.units);
  expect(Math.abs(seen.width - seen.height)).toBeLessThan(0.75);
  expect(seen.dark).toBe('rgb(0, 0, 0)');
  expect(seen.light).toBe('rgb(255, 255, 255)');

  // Four light modules on every side, as a reader needs.
  const last = seen.units - 1;
  for (let r = 0; r < seen.units; r += 1) {
    for (let c = 0; c < seen.units; c += 1) {
      const inQuietZone = r < QUIET_ZONE || c < QUIET_ZONE || r > last - QUIET_ZONE || c > last - QUIET_ZONE;
      if (inQuietZone) expect(seen.grid[r][c], `quiet zone at ${r},${c}`).toBe(false);
    }
  }
  const modules = seen.grid.slice(QUIET_ZONE, seen.units - QUIET_ZONE)
    .map((row) => row.slice(QUIET_ZONE, seen.units - QUIET_ZONE));
  return { modules, moduleSize: seen.step, width: seen.width, height: seen.height, left: seen.left, right: seen.right };
}

/**
 * Stands in for the browser's clipboard, so a test can say what it does and
 * see what it was handed. `execCommand` stands in for the older way of
 * copying the selection, which the page falls back to.
 */
async function standInForClipboard(
  page: Page,
  how: { clipboard: 'accepts' | 'refuses' | 'missing'; execCommand?: boolean },
): Promise<void> {
  await page.evaluate((setup) => {
    const calls = { written: [] as string[], commands: [] as string[], selectedAtCommand: [] as string[] };
    (window as any).__e2eClipboard = calls;
    const clipboard = setup.clipboard === 'missing' ? undefined : {
      writeText(text: string) {
        calls.written.push(text);
        return setup.clipboard === 'accepts' ? Promise.resolve() : Promise.reject(new DOMException('no', 'NotAllowedError'));
      },
    };
    Object.defineProperty(navigator, 'clipboard', { value: clipboard, configurable: true });
    if (setup.execCommand !== undefined) {
      document.execCommand = (command: string) => {
        calls.commands.push(command);
        calls.selectedAtCommand.push(String(getSelection()));
        return setup.execCommand as boolean;
      };
    }
  }, how);
}

/**
 * Watches the browser's real clipboard being asked, without standing in for
 * it: what each call came to, "ok" or the refusal.
 */
async function watchRealClipboard(page: Page): Promise<void> {
  await page.evaluate(() => {
    const outcomes: string[] = [];
    (window as any).__e2eRealClipboard = outcomes;
    const real = navigator.clipboard.writeText.bind(navigator.clipboard);
    navigator.clipboard.writeText = (text: string) => real(text).then(
      () => { outcomes.push('ok'); },
      (error) => { outcomes.push(String(error)); throw error; });
  });
}

async function realClipboardOutcomes(page: Page): Promise<string[]> {
  return await page.evaluate(() => (window as any).__e2eRealClipboard);
}

async function clipboardCalls(page: Page): Promise<{ written: string[]; commands: string[]; selectedAtCommand: string[] }> {
  return await page.evaluate(() => (window as any).__e2eClipboard);
}

async function selectedText(page: Page): Promise<string> {
  return await page.evaluate(() => String(getSelection()));
}

test.beforeEach(async ({ page }) => {
  owner = seedOwner({ enrolled: false });
  cspMessages = [];
  pageErrors = [];
  dialogs = [];
  page.on('console', (message) => {
    const text = message.text();
    // Chromium and WebKit write "Content Security Policy", Firefox "Content-Security-Policy".
    if (!/content[- ]security[- ]policy/i.test(text)) return;
    // Firefox asks for /favicon.ico by itself, and the pages' default-src 'none' refuses it.
    if (text.includes('/favicon.ico')) return;
    // Only these pages are judged here. Elsewhere the server still answers with bare JSON under
    // the site-wide policy, and WebKit's own viewer for such a document trips over that policy.
    if (!THE_PAGES.includes(new URL(page.url()).pathname)) return;
    cspMessages.push(text);
  });
  page.on('pageerror', (error) => pageErrors.push(error.message));
  page.on('dialog', async (dialog) => {
    dialogs.push(dialog.message());
    await dialog.dismiss();
  });
});

test.afterEach(() => {
  // Nothing the pages load may be refused by a page's own policy, and no script may throw.
  expect(cspMessages).toEqual([]);
  expect(pageErrors).toEqual([]);
  expect(dialogs).toEqual([]);
});

test('a first sign-in from start to finish, keyboard only: password, authenticator set-up, recovery codes, then signing in with a fresh code', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  await page.goto(SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await expect(continueToStepTwo(page)).toBeEnabled();

  // No mouse from here on. Step 1: the owner has a password and no authenticator.
  await expect(page.locator('#email')).toBeFocused();
  await page.keyboard.type(owner.email);
  await page.keyboard.press('Tab');
  await expect(page.locator('#password')).toBeFocused();
  await page.keyboard.type(owner.password);
  await page.keyboard.press('Enter');

  // S-A6. Nothing has the cursor yet: the QR code comes first. Tab goes to Copy key, the code field, Confirm.
  await expect(page).toHaveURL(ORIGIN + AUTHENTICATOR);
  await expect(page).toHaveTitle(AUTHENTICATOR_TITLE);
  await expect(page.locator('h1')).toHaveText(AUTHENTICATOR_HEADING);
  await expect(page.locator('#authenticator-form')).toBeVisible();
  expect(await focusStop(page)).toBe('body');
  await expectNotSignedIn(page);
  const keyText = await keyOnPage(page);
  const key = keyBytes(keyText);
  const order: string[] = [];
  for (let i = 0; i < 3; i += 1) {
    await page.keyboard.press('Tab');
    order.push(await focusStop(page));
  }
  expect(order).toEqual(['copy-key', 'code', 'authenticator-submit']);
  const ringOf = (locator: Locator) => locator.evaluate((el) => {
    const style = getComputedStyle(el);
    return { style: style.outlineStyle, width: style.outlineWidth, offset: style.outlineOffset, colour: style.outlineColor };
  });
  const ring = { style: 'solid', width: '2px', offset: '2px', colour: 'rgb(11, 79, 138)' };
  expect(await ringOf(confirmButton(page))).toEqual(ring);
  await page.keyboard.press('Shift+Tab');
  await expect(page.locator('#code')).toBeFocused();
  expect(await ringOf(page.locator('#code'))).toEqual(ring);
  // The code an authenticator app shows for the key on the page, typed the way apps show it.
  const code = authenticatorCode(key);
  await page.keyboard.type(`${code.slice(0, 3)} ${code.slice(3)}`);
  await page.keyboard.press('Enter');

  // S-A7, in place of S-A6. Tab goes to Copy all, the box, Continue. Continue takes the cursor only once the box is ticked.
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await expect(page).toHaveTitle(RECOVERY_CODES_TITLE);
  await expect(page.locator('h1')).toHaveText(RECOVERY_CODES_HEADING);
  await expect(page.locator('#recovery-codes-list')).toBeVisible();
  const codes = await codesOnPage(page);
  expect(seen.confirms).toHaveLength(1);
  expect(seen.confirms[0].postDataJSON()).toEqual({ code });
  const sent = await seen.confirms[0].allHeaders();
  expect(sent['origin']).toBe(ORIGIN);
  expect(sent['x-csrf-token']).toMatch(/^[0-9A-F]{64}$/);
  expect(sent['content-type']).toBe('application/json');
  expect(sent['referer']).toBeUndefined();
  // Setting the authenticator up signed nobody in.
  await expectNotSignedIn(page);

  expect(await focusStop(page)).toBe('body');
  await page.keyboard.press('Tab');
  expect(await focusStop(page)).toBe('copy-codes');
  await page.keyboard.press('Tab');
  expect(await focusStop(page)).toBe('codes-saved');
  expect(await ringOf(savedBox(page))).toEqual(ring);
  await expect(continueButton(page)).toBeDisabled();
  await page.keyboard.press('Space');
  await expect(savedBox(page)).toBeChecked();
  await expect(continueButton(page)).toBeEnabled();
  await page.keyboard.press('Tab');
  expect(await focusStop(page)).toBe('codes-continue');
  expect(await ringOf(continueButton(page))).toEqual(ring);
  await page.keyboard.press('Enter');

  // Step 1 again, in place of S-A7, with no failure sentence: the owner signs in with a fresh code.
  await expect(page.locator('#sign-in-form')).toBeVisible();
  expect(page.url()).toBe(ORIGIN + SIGN_IN);
  await expect(page.locator('#form-alert')).toBeHidden();
  expect(seen.signOuts).toHaveLength(1);
  await expect(continueToStepTwo(page)).toBeEnabled();
  await expect(page.locator('#email')).toBeFocused();
  await page.keyboard.type(owner.email);
  await page.keyboard.press('Tab');
  await page.keyboard.type(owner.password);
  await page.keyboard.press('Enter');

  // Now the server knows the authenticator: the code page, not set-up again.
  await expect(page).toHaveURL(ORIGIN + CODE);
  await expect(page.locator('h1')).toHaveText(CODE_HEADING);
  await expect(page.locator('#code')).toBeFocused();
  // A fresh code: the one that confirmed the set-up is spent, so the next one is used.
  await page.keyboard.type(authenticatorCode(key, new Date(Date.now() + 30_000)));
  await page.keyboard.press('Enter');

  // Signed in, on the path the server named.
  await expect(page).toHaveURL(ORIGIN + HOME);
  const cookies = await page.context().cookies();
  const session = cookies.find((c) => c.name === 'dw_admin_session');
  expect(session, 'the session cookie').toBeDefined();
  expect(session!.httpOnly).toBe(true);
  expect(session!.secure).toBe(true);
  if (reportsSameSite()) expect(session!.sameSite).toBe('Strict');
  expect(cookies.find((c) => c.name === 'dw_admin_pending')).toBeUndefined();
  expect((await page.request.get(API_ME)).status()).toBe(200);

  // On the way, no script asked for the key or the codes, nothing carried them in an address, and no page said where it came from.
  expect(seen.dataRequests).toEqual([]);
  expect(seen.referers).toEqual([]);
  for (const url of seen.urls) {
    expect([new URL(ORIGIN).hostname, WIDGET_HOST]).toContain(new URL(url).hostname);
    expect(url).not.toContain('otpauth');
    expect(url).not.toContain('secret=');
    expect(url.toUpperCase()).not.toContain(keyText);
    for (const recoveryCode of codes) expect(url.toUpperCase()).not.toContain(recoveryCode);
  }
});

test('S-A6 is HTML under a nonce policy with no Cloudflare in it, and shows the QR code, the key and the account name', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  await page.goto(ELSEWHERE);
  const arrival = page.waitForResponse((r) => new URL(r.url()).pathname === AUTHENTICATOR && r.request().isNavigationRequest());
  await signInStepOne(page);
  const response = await arrival;
  await expect(page.locator('#authenticator-form')).toBeVisible();

  expect(response.status()).toBe(200);
  const headers = await response.headersArray();
  const csp = headers.filter((h) => h.name.toLowerCase() === 'content-security-policy');
  expect(csp).toHaveLength(1);
  const html = await response.text();
  const nonce = html.match(/<link rel="stylesheet" href="\/admin\/auth\/signed-out\.css" nonce="([^"]+)">/)![1];
  expect(csp[0].value).toBe(
    `default-src 'none'; script-src 'nonce-${nonce}'; style-src 'nonce-${nonce}'; `
    + 'connect-src \'self\'; frame-ancestors \'none\'; base-uri \'none\'; form-action \'self\'; object-src \'none\'');
  expect(response.headers()['content-type']).toBe('text/html; charset=utf-8');
  expect(response.headers()['cache-control']).toBe('no-store');
  expect(response.headers()['referrer-policy']).toBe('no-referrer');
  expect(response.headers()['x-csrf-token']).toBeUndefined();

  await expect(page).toHaveTitle(AUTHENTICATOR_TITLE);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText(AUTHENTICATOR_HEADING);
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page.getByText(AUTHENTICATOR_LEDE, { exact: true })).toBeVisible();
  await expect(page.getByText(AUTHENTICATOR_NOSCRIPT)).toBeHidden();
  await expect(page.locator('#form-alert')).toBeHidden();
  await expect(page.locator('nav')).toHaveCount(0);
  await expect(page.getByRole('link')).toHaveCount(0);
  // The owner scans first, so nothing has taken the cursor.
  expect(await focusStop(page)).toBe('body');

  // The linked stylesheet applied under style-src 'nonce-...'.
  const colours = await page.evaluate(() => ({
    body: getComputedStyle(document.body).backgroundColor,
    card: getComputedStyle(document.querySelector('.auth-card')!).backgroundColor,
    button: getComputedStyle(document.getElementById('authenticator-submit')!).backgroundColor,
  }));
  expect(colours).toEqual({ body: 'rgb(244, 241, 234)', card: 'rgb(255, 255, 255)', button: 'rgb(11, 79, 138)' });

  // The QR code is one image with a name; its description is the line that offers the key instead.
  const qr = page.getByRole('img', { name: QR_LABEL, exact: true });
  await expect(qr).toBeVisible();
  await expect(qr).toHaveAttribute('id', 'authenticator-qr');
  await expect(qr).toHaveAttribute('aria-describedby', 'manual-key-help');
  await expect(page.locator('#manual-key-help')).toHaveText(KEY_HELP);
  await expect(page.locator('#manual-key-help')).toBeVisible();
  await expect(page.getByRole('img')).toHaveCount(1);
  await expect(page.locator('img')).toHaveCount(0);

  // The key: text that can be selected, in eight groups of four, with a name beside it; and the account name.
  const key = await keyOnPage(page);
  const groups = await page.locator('#manual-key > span').allTextContents();
  expect(groups).toHaveLength(8);
  for (const group of groups) expect(group).toMatch(/^[A-Z2-7]{4}$/);
  expect(groups.join('')).toBe(key);
  const gaps = await page.locator('#manual-key > span').evaluateAll((spans) => spans.map((s) => parseFloat(getComputedStyle(s).marginRight)));
  for (const gap of gaps) expect(gap).toBeGreaterThanOrEqual(6);
  expect(await page.locator('#manual-key').evaluate((el) => getComputedStyle(el).userSelect || (getComputedStyle(el) as any).webkitUserSelect)).not.toBe('none');
  await expect(page.locator('#manual-key')).toHaveAttribute('translate', 'no');
  expect(await page.locator('dl.details dt').allTextContents()).toEqual(['Manual key', 'Account name']);
  await expect(page.locator('#account-name')).toHaveText(`Dealoware:${owner.email}`);

  // One field for the 6-digit code, as on the sign-in code page.
  const code = page.locator('#code');
  await expect(page.getByRole('textbox', { name: 'Authenticator code', exact: true })).toHaveAttribute('id', 'code');
  await expect(code).toHaveAttribute('type', 'text');
  await expect(code).toHaveAttribute('inputmode', 'numeric');
  await expect(code).toHaveAttribute('autocomplete', 'one-time-code');
  await expect(code).toHaveAttribute('autocapitalize', 'none');
  await expect(code).toHaveAttribute('spellcheck', 'false');
  await expect(code).toHaveAttribute('aria-describedby', 'code-error');
  await expect(page.locator('input')).toHaveCount(1);
  expect(await code.evaluate((el) => el.dispatchEvent(
    new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData: new DataTransfer() })))).toBe(true);
  await expect(confirmButton(page)).toBeEnabled();
  await expect(page.getByRole('button')).toHaveCount(2);

  // Opening the page sent nothing, asked for no data, and kept nothing where a script can read it.
  expect(seen.confirms).toHaveLength(0);
  expect(seen.dataRequests).toEqual([]);
  expect(seen.probes).toHaveLength(0);
  await expectNothingStored(page);
  expect(seen.referers).toEqual([]);
  for (const url of seen.urls) {
    expect([new URL(ORIGIN).hostname, WIDGET_HOST]).toContain(new URL(url).hostname);
    expect(url).not.toContain(key);
  }
  // The key is in the page once, as that text: not in an attribute, a link or a hidden field.
  expect(html).not.toContain(key);
  expect(html).not.toContain('otpauth');
  expect(html).not.toContain(' value=');
  await shot(page, 'authenticator_desktop');
});

test('the QR code the browser paints reads back as the otpauth address of the key on the page', async ({ page }) => {
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  const expected = otpAuthAddress(key);

  const painted = await paintedQr(page);
  const reading = readQr(painted.modules);
  expect(reading.text).toBe(expected);
  // 141 bytes for this owner: version 8, 49 modules on a side, at error correction level M.
  expect(reading.bytes).toHaveLength(Buffer.byteLength(expected));
  expect(reading.version).toBe(8);
  expect(painted.modules).toHaveLength(49);
  // Big enough for a phone held up to a screen: 15rem across, over four CSS px to a module.
  expect(painted.width).toBeCloseTo(240, 0);
  expect(painted.moduleSize).toBeGreaterThan(4);

  // The address in the code carries the key the page shows as text, the owner, and the parameters the server checks with.
  const address = new URL(reading.text);
  expect(address.protocol).toBe('otpauth:');
  expect(decodeURIComponent(address.pathname.split('/').pop()!)).toBe(`Dealoware:${owner.email}`);
  expect(Object.fromEntries(address.searchParams)).toEqual({
    secret: key, issuer: 'Dealoware', digits: '6', period: '30', algorithm: 'SHA1',
  });
  expect(await page.locator('#account-name').textContent()).toBe(`Dealoware:${owner.email}`);

  // And the code worked out from that key is the one the server accepts.
  await page.locator('#code').fill(authenticatorCode(keyBytes(address.searchParams.get('secret')!)));
  const reply = await confirmAndWait(page);
  expect(reply.status()).toBe(200);
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);

  // A reader that is handed a damaged picture says so: this check can fail.
  const damaged = painted.modules.map((row) => [...row]);
  for (let i = 0; i < 12; i += 1) damaged[20 + i][30] = !damaged[20 + i][30];
  expect(() => readQr(damaged)).toThrow(/not a readable QR Code/);
});

test('wrong code: the server sentence is shown as sent, the field is cleared, the QR code and the key stay; then the right code works', async ({ page }) => {
  const seen = watch(page);
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  const before = (await paintedQr(page)).modules;
  const code = page.locator('#code');

  await code.fill(wrongAuthenticatorCode(keyBytes(key)));
  const refusal = await confirmAndWait(page);
  expect(refusal.status()).toBe(401);
  expect(await refusal.json()).toEqual({ error: SIGN_IN_GENERIC });
  expect(refusal.headers()['retry-after']).toBeUndefined();

  await expectAlert(page, SIGN_IN_GENERIC);
  await expect(code).toHaveValue('');
  await expect(page).toHaveURL(ORIGIN + AUTHENTICATOR);
  await expect(page.locator('#authenticator-form')).not.toHaveAttribute('aria-busy', 'true');
  await expect(confirmButton(page)).toBeEnabled();
  // Nothing was set up, and what the owner scanned is still there, unchanged.
  expect(await keyOnPage(page)).toBe(key);
  expect((await paintedQr(page)).modules).toEqual(before);
  await expectNotSignedIn(page);
  await shot(page, 'authenticator_error');

  // The alert goes when the owner tries again.
  await code.fill(authenticatorCode(keyBytes(key)));
  await confirmButton(page).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await codesOnPage(page);
  expect(seen.confirms).toHaveLength(2);
  expect(seen.dataRequests).toEqual([]);
});

test('S-A6 never sends by itself, and a code that is not six digits is caught in the browser', async ({ page }) => {
  const seen = watch(page);
  await reachAuthenticatorPage(page);
  const code = page.locator('#code');
  const error = page.locator('#code-error');

  // Six digits typed: nothing is sent until Confirm.
  await code.fill('123456');
  await code.blur();
  await page.waitForTimeout(300);
  expect(seen.confirms).toHaveLength(0);

  for (const notACode of ['', '12345', '1234567', 'abcdef', '12 34 5', '１２３４５６']) {
    await code.fill(notACode);
    await confirmButton(page).click();
    await expect(error, notACode).toHaveText(CODE_REQUIRED);
    await expect(code).toHaveAttribute('aria-invalid', 'true');
    await expect(code).toBeFocused();
    await expect(page.locator('#form-alert')).toBeHidden();
  }
  expect(seen.confirms).toHaveLength(0);

  // Typing clears the message.
  await code.pressSequentially('1');
  await expect(error).toHaveText('');
  await expect(code).not.toHaveAttribute('aria-invalid', 'true');

  // A second Confirm while one is in flight sends nothing.
  let release: () => void = () => {};
  const held = new Promise<void>((resolve) => { release = resolve; });
  await page.route((url) => url.pathname === AUTHENTICATOR, async (route) => {
    if (route.request().method() !== 'POST') return route.fallback();
    await held;
    await route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ error: SIGN_IN_GENERIC }) });
  });
  await code.fill('123456');
  await confirmButton(page).click();
  await expect(page.locator('#authenticator-form')).toHaveAttribute('aria-busy', 'true');
  await expect(confirmButton(page)).toBeDisabled();
  await code.press('Enter');
  await code.press('Enter');
  release();
  await expectAlert(page, SIGN_IN_GENERIC);
  expect(seen.confirms).toHaveLength(1);
});

test('"Copy key" puts the key on the clipboard, says so in the status line and keeps the focus; it stores nothing', async ({ page }) => {
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  const status = page.locator('#copy-status');
  await expect(status).toHaveAttribute('role', 'status');
  await expect(status).toHaveText('');

  if (engine() === 'chromium') {
    // The real clipboard: it accepts the text, and the text read back is the key.
    await page.context().grantPermissions(['clipboard-read', 'clipboard-write'], { origin: ORIGIN });
    await watchRealClipboard(page);
    await copyKeyButton(page).click();
    await expect(status).toHaveText(COPIED);
    expect(await realClipboardOutcomes(page)).toEqual(['ok']);
    expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(key);
  }

  // The browser's asynchronous clipboard is asked, inside the click, with the key and nothing else.
  await standInForClipboard(page, { clipboard: 'accepts' });
  await copyKeyButton(page).click();
  await expect(status).toHaveText(COPIED);
  await expect(copyKeyButton(page)).toBeFocused();
  expect((await clipboardCalls(page)).written).toEqual([key]);
  expect(await selectedText(page)).toBe('');

  // Pressed again, it says so again: the line is emptied first, so the same words are a change.
  await status.evaluate((el) => {
    (window as any).__e2eStatusChanges = [];
    new MutationObserver(() => (window as any).__e2eStatusChanges.push(el.textContent)).observe(el, { childList: true, characterData: true, subtree: true });
  });
  await copyKeyButton(page).focus();
  await page.keyboard.press('Enter');
  await expect.poll(() => page.evaluate(() => (window as any).__e2eStatusChanges)).toEqual(['', COPIED]);
  expect((await clipboardCalls(page)).written).toEqual([key, key]);

  // Nothing was kept anywhere a script can read later, and the key is still on the page.
  await expectNothingStored(page);
  expect(await keyOnPage(page)).toBe(key);
});

test('when the browser will not copy, the key is selected instead and the page says so', async ({ page }) => {
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  const status = page.locator('#copy-status');

  // The clipboard refuses, and so does the older way: the key stays selected for the owner's own copy command.
  await standInForClipboard(page, { clipboard: 'refuses', execCommand: false });
  await copyKeyButton(page).click();
  await expect(status).toHaveText(COPY_FAILED);
  await expect(copyKeyButton(page)).toBeFocused();
  let calls = await clipboardCalls(page);
  expect(calls.written).toEqual([key]);
  expect(calls.commands).toEqual(['copy']);
  // What is selected is the key itself: the gaps between its groups are not characters.
  expect(await selectedText(page)).toBe(key);

  // The clipboard refuses, the older way works: copied, and the selection is put away again.
  await standInForClipboard(page, { clipboard: 'refuses', execCommand: true });
  await copyKeyButton(page).click();
  await expect(status).toHaveText(COPIED);
  calls = await clipboardCalls(page);
  expect(calls.commands).toEqual(['copy']);
  expect(calls.selectedAtCommand).toEqual([key]);
  expect(await selectedText(page)).toBe('');

  // A browser with no asynchronous clipboard at all goes straight to the older way.
  await standInForClipboard(page, { clipboard: 'missing', execCommand: true });
  await copyKeyButton(page).click();
  await expect(status).toHaveText(COPIED);
  calls = await clipboardCalls(page);
  expect(calls.written).toEqual([]);
  expect(calls.selectedAtCommand).toEqual([key]);

  await standInForClipboard(page, { clipboard: 'missing', execCommand: false });
  await copyKeyButton(page).click();
  await expect(status).toHaveText(COPY_FAILED);
  expect(await selectedText(page)).toBe(key);
  await expectNothingStored(page);
});

test('S-A7 shows ten codes as a list, says what they are for and that they are shown once, and sends nothing by itself', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  await page.goto(ELSEWHERE);
  await signInStepOne(page);
  await expect(page.locator('#authenticator-form')).toBeVisible();
  const key = await keyOnPage(page);
  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  const arrival = page.waitForResponse((r) => new URL(r.url()).pathname === RECOVERY_CODES && r.request().isNavigationRequest());
  await confirmButton(page).click();
  const response = await arrival;
  await expect(page.locator('#recovery-codes-list')).toBeVisible();

  expect(response.status()).toBe(200);
  const html = await response.text();
  const nonce = html.match(/<link rel="stylesheet" href="\/admin\/auth\/signed-out\.css" nonce="([^"]+)">/)![1];
  const csp = (await response.headersArray()).filter((h) => h.name.toLowerCase() === 'content-security-policy');
  expect(csp).toHaveLength(1);
  expect(csp[0].value).toBe(
    `default-src 'none'; script-src 'nonce-${nonce}'; style-src 'nonce-${nonce}'; `
    + 'connect-src \'self\'; frame-ancestors \'none\'; base-uri \'none\'; form-action \'self\'; object-src \'none\'');
  expect(response.headers()['content-type']).toBe('text/html; charset=utf-8');
  expect(response.headers()['cache-control']).toBe('no-store');
  expect(response.headers()['referrer-policy']).toBe('no-referrer');

  await expect(page).toHaveTitle(RECOVERY_CODES_TITLE);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText(RECOVERY_CODES_HEADING);
  await expect(page.getByText(RECOVERY_CODES_LEDE, { exact: true })).toBeVisible();
  await expect(page.getByText(RECOVERY_CODES_PURPOSE, { exact: true })).toBeVisible();
  await expect(page.getByText(RECOVERY_CODES_NOSCRIPT)).toBeHidden();
  expect(await focusStop(page)).toBe('body');

  // A real list of ten items, named by the heading.
  const codes = await codesOnPage(page);
  const list = page.getByRole('list', { name: RECOVERY_CODES_HEADING, exact: true });
  await expect(list).toHaveAttribute('id', 'recovery-codes-list');
  await expect(list.getByRole('listitem')).toHaveCount(10);
  expect(await page.locator('#recovery-codes-list').evaluate((el) => el.tagName)).toBe('UL');
  await expect(page.locator('#recovery-codes-list')).toHaveAttribute('translate', 'no');

  // The box is not ticked and Continue cannot be used; there is no link and no form.
  await expect(savedBox(page)).not.toBeChecked();
  await expect(continueButton(page)).toBeDisabled();
  await expect(continueButton(page)).toHaveText('Continue');
  await expect(copyAllButton(page)).toBeEnabled();
  await expect(page.getByRole('link')).toHaveCount(0);
  await expect(page.locator('form')).toHaveCount(0);
  await expect(page.locator('svg')).toHaveCount(0);

  // The codes are in the page once each, as text; the authenticator key is not on this page at all.
  for (const code of codes) expect(html.split(code)).toHaveLength(2);
  expect(html).not.toContain(key);
  expect(html).not.toContain(' value=');
  expect(await page.locator('body').innerText()).not.toContain(key);

  expect(seen.signOuts).toHaveLength(0);
  expect(seen.dataRequests).toEqual([]);
  expect(seen.referers).toEqual([]);
  await expectNothingStored(page);
  for (const url of seen.urls) {
    for (const code of codes) expect(url.toUpperCase()).not.toContain(code);
  }
  await expectNotSignedIn(page);
  await shot(page, 'recovery-codes_desktop');
});

test('"Copy all" puts the ten codes on the clipboard, one to a line, and falls back to selecting them', async ({ page }) => {
  await reachRecoveryCodesPage(page);
  const codes = await codesOnPage(page);
  const status = page.locator('#copy-status');
  await expect(status).toHaveAttribute('role', 'status');
  await expect(status).toHaveText('');

  if (engine() === 'chromium') {
    await page.context().grantPermissions(['clipboard-read', 'clipboard-write'], { origin: ORIGIN });
    await watchRealClipboard(page);
    await copyAllButton(page).click();
    await expect(status).toHaveText(COPIED);
    expect(await realClipboardOutcomes(page)).toEqual(['ok']);
    // Windows hands line ends back as CR LF; what was put there is one code to a line.
    const onClipboard = await page.evaluate(() => navigator.clipboard.readText());
    expect(onClipboard.split(/\r?\n/)).toEqual(codes);
  }

  await standInForClipboard(page, { clipboard: 'accepts' });
  await copyAllButton(page).click();
  await expect(status).toHaveText(COPIED);
  await expect(copyAllButton(page)).toBeFocused();
  expect((await clipboardCalls(page)).written).toEqual([codes.join('\n')]);

  // Refused both ways: the list stays selected, and the selection is the ten codes.
  await standInForClipboard(page, { clipboard: 'refuses', execCommand: false });
  await copyAllButton(page).click();
  await expect(status).toHaveText(COPY_FAILED);
  await expect(copyAllButton(page)).toBeFocused();
  expect((await selectedText(page)).split(/\s+/).filter(Boolean)).toEqual(codes);

  await standInForClipboard(page, { clipboard: 'missing', execCommand: true });
  await copyAllButton(page).click();
  await expect(status).toHaveText(COPIED);
  const calls = await clipboardCalls(page);
  expect(calls.selectedAtCommand[0].split(/\s+/).filter(Boolean)).toEqual(codes);
  expect(await selectedText(page)).toBe('');

  // Copying ticks nothing and stores nothing.
  await expect(savedBox(page)).not.toBeChecked();
  await expect(continueButton(page)).toBeDisabled();
  await expectNothingStored(page);
});

test('Continue works only once "I have saved these recovery codes" is ticked; it then gives the pending sign-in up and goes to step 1', async ({ page }) => {
  const seen = watch(page);
  await reachRecoveryCodesPage(page);
  const codes = await codesOnPage(page);

  // Not ticked: the button is disabled, and pressing it anyway does nothing.
  await expect(continueButton(page)).toBeDisabled();
  await continueButton(page).click({ force: true });
  await continueButton(page).dispatchEvent('click');
  await page.waitForTimeout(200);
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  expect(seen.signOuts).toHaveLength(0);

  // Ticked, unticked, ticked: the button follows the box. The label ticks it too.
  await page.getByText(SAVED_LABEL, { exact: true }).click();
  await expect(savedBox(page)).toBeChecked();
  await expect(continueButton(page)).toBeEnabled();
  await savedBox(page).uncheck();
  await expect(continueButton(page)).toBeDisabled();
  await savedBox(page).check();
  await expect(continueButton(page)).toBeEnabled();

  const pendingBefore = (await page.context().cookies()).find((c) => c.name === 'dw_admin_pending');
  expect(pendingBefore, 'the pending sign-in is still there while the codes are shown').toBeDefined();
  const [signOut] = await Promise.all([
    page.waitForResponse(isReplyTo(API_SIGN_OUT)),
    continueButton(page).click(),
  ]);
  expect(signOut.status()).toBe(200);
  expect(seen.signOuts[0].postDataJSON()).toEqual({});

  // Step 1, plain: no failure sentence, and nobody is signed in.
  await expect(page.locator('#sign-in-form')).toBeVisible();
  expect(page.url()).toBe(ORIGIN + SIGN_IN);
  await expect(page.locator('#form-alert')).toBeHidden();
  expect((await page.context().cookies()).find((c) => c.name === 'dw_admin_pending')).toBeUndefined();
  await expectNotSignedIn(page);

  // Step 1 took the place of S-A7: Back leaves the app's pages, and Forward comes back to step 1, not to the codes.
  await page.goBack();
  await expect(page).toHaveURL(ORIGIN + ELSEWHERE);
  await page.goForward();
  await expect(page.locator('#sign-in-form')).toBeVisible();
  expect(page.url()).toBe(ORIGIN + SIGN_IN);

  // And the address of either set-up page now leads to step 1, which says it has to be done again. The codes are not shown.
  for (const address of [RECOVERY_CODES, AUTHENTICATOR]) {
    await page.goto(address);
    await expectSentBackToStepOne(page);
    const text = await page.locator('body').innerText();
    for (const code of codes) expect(text).not.toContain(code);
  }
  expect(seen.signOuts).toHaveLength(1);
});

test('Continue still leaves when the server cannot be asked to give the pending sign-in up', async ({ page }) => {
  await reachRecoveryCodesPage(page);
  await answer(page, API_SIGN_OUT, 'POST', () => 'abort');
  await savedBox(page).check();
  await continueButton(page).click();
  await expect(page.locator('#sign-in-form')).toBeVisible();
  expect(page.url()).toBe(ORIGIN + SIGN_IN);
  await expect(page.locator('#form-alert')).toBeHidden();
});

test('reload and Back on S-A6: the same key and QR code while the pending sign-in lasts, and never a second key', async ({ page }) => {
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  const modules = (await paintedQr(page)).modules;

  // Back leaves for the page before sign-in (step 1 gave its place to this one); Forward asks the server for S-A6 again.
  // (Back and Forward come before the reload: with Playwright's Firefox, Back straight after a reload stays on the page,
  // for any two pages at all.)
  await page.locator('#code').fill('123456');
  await page.goBack();
  await expect(page).toHaveURL(ORIGIN + ELSEWHERE);
  await page.goForward();
  await expect(page).toHaveURL(ORIGIN + AUTHENTICATOR);
  await expect(page.locator('#authenticator-form')).toBeVisible();
  await expect(page.locator('#manual-key')).toHaveText(key);
  expect((await paintedQr(page)).modules).toEqual(modules);
  // What was typed before leaving is not put back.
  await expect(page.locator('#code')).toHaveValue('');

  // Reload: the same page again, with the same key. The server issues no new one.
  await page.locator('#code').fill('123456');
  await page.reload();
  await expect(page.locator('#authenticator-form')).toBeVisible();
  await expect(page).toHaveURL(ORIGIN + AUTHENTICATOR);
  expect(await keyOnPage(page)).toBe(key);
  expect((await paintedQr(page)).modules).toEqual(modules);
  await expect(page.locator('#form-alert')).toBeHidden();
  // What was typed is not put back by the reload either.
  await expect(page.locator('#code')).toHaveValue('');

  // A page the browser puts away keeps neither the code nor the key nor its QR code.
  await page.locator('#code').fill('123456');
  await page.evaluate(() => window.dispatchEvent(new PageTransitionEvent('pagehide', { persisted: true })));
  await expect(page.locator('#code')).toHaveValue('');
  await expect(page.locator('#manual-key')).toHaveText('');
  await expect(page.locator('#authenticator-qr > *')).toHaveCount(0);
  // And one the browser brings back whole asks the server for itself again.
  const loadedAnew = page.waitForEvent('load');
  await page.evaluate(() => window.dispatchEvent(new PageTransitionEvent('pageshow', { persisted: true })));
  await loadedAnew;
  await expect(page.locator('#manual-key')).toHaveText(key);

  // The key still confirms: it was the same one all along.
  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  await confirmButton(page).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await codesOnPage(page);

  // Once the authenticator is set up, the address of S-A6 shows no key again: the server sends the browser on to the codes.
  const response = await page.goto(AUTHENTICATOR);
  const first = response!.request().redirectedFrom();
  expect(first).not.toBeNull();
  const redirect = await first!.response();
  expect(redirect!.status()).toBe(302);
  expect(redirect!.headers()['location']).toBe(RECOVERY_CODES);
  expect(redirect!.headers()['set-cookie']).toBeUndefined();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await expect(page.locator('#recovery-codes-list')).toBeVisible();
  expect(await page.locator('body').innerText()).not.toContain(key);
  await expect(page.locator('#manual-key')).toHaveCount(0);
});

test('reload and Back on S-A7: the same codes while the pending sign-in lasts, with the box unticked again', async ({ page }) => {
  await reachRecoveryCodesPage(page);
  const codes = await codesOnPage(page);

  // Back, with the box ticked, leaves for the page before sign-in (S-A6 gave its place to this one); Forward asks the
  // server for S-A7 again: the codes are there, the tick is not, and Continue is disabled.
  // (Back and Forward come before the reload, as in the test above.)
  await savedBox(page).check();
  await expect(continueButton(page)).toBeEnabled();
  await page.goBack();
  await expect(page).toHaveURL(ORIGIN + ELSEWHERE);
  await page.goForward();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await expect(page.locator('#recovery-codes-list')).toBeVisible();
  expect(await codesOnPage(page)).toEqual(codes);
  await expect(savedBox(page)).not.toBeChecked();
  await expect(continueButton(page)).toBeDisabled();

  // Reload with the box ticked: the same again.
  await savedBox(page).check();
  await expect(continueButton(page)).toBeEnabled();
  await page.reload();
  await expect(page.locator('#recovery-codes-list')).toBeVisible();
  expect(await codesOnPage(page)).toEqual(codes);
  await expect(savedBox(page)).not.toBeChecked();
  await expect(continueButton(page)).toBeDisabled();

  // A page the browser puts away keeps neither the codes nor the tick; one it brings back whole is asked for anew.
  await savedBox(page).check();
  await page.evaluate(() => window.dispatchEvent(new PageTransitionEvent('pagehide', { persisted: true })));
  await expect(page.locator('#recovery-codes-list li')).toHaveCount(0);
  await expect(savedBox(page)).not.toBeChecked();
  const loadedAnew = page.waitForEvent('load');
  await page.evaluate(() => window.dispatchEvent(new PageTransitionEvent('pageshow', { persisted: true })));
  await loadedAnew;
  await expect(page.locator('#recovery-codes-list li')).toHaveCount(10);
  expect(await codesOnPage(page)).toEqual(codes);
});

test('once the next sign-in has started, S-A7 says the codes were shown once, and S-A6 leads there too: no empty page, no codes', async ({ page }) => {
  await reachRecoveryCodesPage(page);
  const codes = await codesOnPage(page);

  // The owner leaves without Continue and starts signing in again: step 1 replaces the pending sign-in.
  await signInStepOne(page);
  await expect(page).toHaveURL(ORIGIN + CODE);
  await expect(page.locator('#second-step-form')).toBeVisible();

  for (const address of [RECOVERY_CODES, AUTHENTICATOR]) {
    const response = await page.goto(address);
    expect(response!.status()).toBe(200);
    await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
    await expect(page).toHaveTitle(RECOVERY_CODES_TITLE);
    await expect(page.locator('h1')).toHaveText(RECOVERY_CODES_HEADING);
    await expect(page.getByText(RECOVERY_CODES_ALREADY_SHOWN, { exact: true })).toBeVisible();
    const back = page.getByRole('link', { name: 'Back to sign in', exact: true });
    await expect(back).toHaveAttribute('href', SIGN_IN);
    await expect(page.getByRole('link')).toHaveCount(1);
    await expect(page.locator('#recovery-codes-list')).toHaveCount(0);
    await expect(page.getByRole('button')).toHaveCount(0);
    await expect(page.locator('input')).toHaveCount(0);
    const lines = (await page.locator('body').innerText()).split('\n').map((line) => line.trim()).filter(Boolean);
    expect(lines).toEqual([RECOVERY_CODES_HEADING, RECOVERY_CODES_ALREADY_SHOWN, 'Back to sign in']);
    const html = await response!.text();
    for (const code of codes) expect(html).not.toContain(code);
    expect(response!.headers()['cache-control']).toBe('no-store');
  }
  await expectNoSeriousAxeFindings(page, 'S-A7-already-shown');

  // The link is an ordinary one, tall enough to hit, and leads to step 1.
  const box = await page.locator('#back-link').boundingBox();
  expect(box!.height).toBeGreaterThanOrEqual(24);
  await page.locator('#back-link').click();
  await expect(page.locator('#sign-in-form')).toBeVisible();
  expect(page.url()).toBe(ORIGIN + SIGN_IN);
});

test('a direct visit to S-A6 or S-A7 without a pending sign-in lands on step 1, which says so in the server sentence', async ({ page }) => {
  await standInForCloudflare(page);
  for (const visited of [AUTHENTICATOR, RECOVERY_CODES, `${AUTHENTICATOR}?next=${encodeURIComponent('//evil.example/')}`]) {
    const response = await page.goto(visited);

    // The session gate answered the visit itself: a redirect to a constant address, and no cookie.
    const first = response!.request().redirectedFrom();
    expect(first, visited).not.toBeNull();
    expect(first!.url()).toBe(ORIGIN + visited);
    expect(first!.redirectedFrom()).toBeNull();
    const redirect = await first!.response();
    expect(redirect!.status()).toBe(302);
    expect(redirect!.headers()['location']).toBe(`${SIGN_IN}#again`);
    expect(redirect!.headers()['set-cookie']).toBeUndefined();

    // Step 1 is what the browser shows. The flag is gone from the address bar.
    expect(response!.status()).toBe(200);
    await expectSentBackToStepOne(page);
    await expect(page.locator('#authenticator-form')).toHaveCount(0);
    await expect(page.locator('#recovery-codes-list')).toHaveCount(0);
    await expectNothingStored(page);
    expect((await page.context().cookies()).map((c) => c.name)).toEqual(['dw_admin_af']);
  }
  await expectNotSignedIn(page);
});

test('when the pending sign-in is gone by the time the code is sent, the page goes back to step 1, which says so', async ({ page }) => {
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);

  // The page is open; then the pending sign-in runs out.
  await page.context().clearCookies({ name: 'dw_admin_pending' });
  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  const [refusal] = await Promise.all([
    page.waitForResponse(isReplyTo(AUTHENTICATOR)),
    page.waitForRequest((request) => request.isNavigationRequest() && new URL(request.url()).pathname === SIGN_IN),
    confirmButton(page).click(),
  ]);
  // The gate's answer, not the sign-in sentence: there is nothing left to try a code against.
  // Only its word sends the page to step 1; the sign-in sentence would have been shown here.
  expect(refusal.status()).toBe(401);
  await expectSentBackToStepOne(page);
  await expectNotSignedIn(page);
});

test('the gate\'s word is what sends the page back, a throttled reply is shown as sent, and no reply is ever read as markup', async ({ page }) => {
  await reachAuthenticatorPage(page);
  const sentence = '<b>We</b> couldn\'t <img src="x" onerror="window.__e2eMarkupRan = true"> sign you in & <script>window.__e2eMarkupRan = true</script>';
  let reply: Answer = { status: 401, json: { error: sentence } };
  await answer(page, AUTHENTICATOR, 'POST', () => reply);
  const code = page.locator('#code');

  // Whatever the server sends as its sentence is shown as text.
  await code.fill('123456');
  await confirmButton(page).click();
  await expectAlert(page, sentence);
  await expect(code).toHaveValue('');
  expect(await page.locator('#form-alert').evaluate((el) => el.children.length)).toBe(0);
  expect(await page.evaluate(() => (window as any).__e2eMarkupRan)).toBeUndefined();

  // Throttled (429): the server's sentence as sent, with no wait time, and the field cleared.
  reply = { status: 429, json: { error: SIGN_IN_GENERIC } };
  await code.fill('123456');
  await confirmButton(page).click();
  await expectAlert(page, SIGN_IN_GENERIC);
  await expect(code).toHaveValue('');
  await expect(page).toHaveURL(ORIGIN + AUTHENTICATOR);

  // The gate's word alone is not enough to leave: it has to come with a 401.
  reply = { status: 400, json: { error: GATE_DENIED } };
  await code.fill('123456');
  await confirmButton(page).click();
  await expectAlert(page, RETRY);
  await expect(page).toHaveURL(ORIGIN + AUTHENTICATOR);
  await expect(code).toHaveValue('123456');

  // A refusal with no sentence is not shown as an empty alert.
  reply = { status: 401, json: {} };
  await confirmButton(page).click();
  await expectAlert(page, RETRY);
  await expect(code).toHaveValue('123456');

  reply = { status: 401, json: { error: GATE_DENIED } };
  await confirmButton(page).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
});

test('no clear answer, and the server cannot be reached: the retry sentence, what was typed stays, and the next Confirm asks the server before it sends anything', async ({ page }) => {
  const seen = watch(page);
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  const code = page.locator('#code');
  const unclear: Answer[] = [
    'abort',
    { status: 500, json: { error: 'Internal Server Error' } },
    { status: 200, json: {} },
    { status: 200, json: { enrolled: 'true', next: 'recovery-codes' } },
    // "enrolled" alone, or with another next step, is not the server's reply: the page does not move on for it.
    { status: 200, json: { enrolled: true } },
    { status: 200, json: { enrolled: true, next: 'sign-in' } },
    { status: 200, text: 'OK' },
  ];
  let reachable = false;
  let reply: Answer = 'abort';
  const confirms = await answer(page, AUTHENTICATOR, 'POST', () => reply);
  await answer(page, API_ME, 'GET', () => (reachable ? 'real' : 'abort'));

  for (const next of unclear) {
    // A page loaded anew each time, so that every one of these answers is the first that page gets.
    reply = next;
    await page.reload();
    await expect(page.locator('#authenticator-form')).toBeVisible();
    await expect(page.locator('#form-alert')).toBeHidden();
    const sentBefore = confirms.count;
    const probesBefore = seen.probes.length;
    await code.fill('123456');
    await confirmButton(page).click();
    await expectAlert(page, RETRY);
    await expect(code).toHaveValue('123456');
    await expect(page).toHaveURL(ORIGIN + AUTHENTICATOR);
    await expect(confirmButton(page)).toBeEnabled();
    expect(confirms.count).toBe(sentBefore + 1);
    expect(seen.probes.length).toBe(probesBefore + 1);
    // The key is still there to scan.
    expect(await keyOnPage(page)).toBe(key);

    // Confirm again while the server still cannot be reached: it is asked again, and no second code is sent.
    await confirmButton(page).click();
    await expectAlert(page, RETRY);
    expect(confirms.count).toBe(sentBefore + 1);
    expect(seen.probes.length).toBe(probesBefore + 2);
  }
  reply = 'real';

  // The server is back. Confirm asks it, and the page loads itself anew: nothing was set up, so
  // S-A6 is shown again, with the same key, and says the request did not go through.
  reachable = true;
  const loadedAnew = page.waitForEvent('load');
  await confirmButton(page).click();
  await loadedAnew;
  await expect(page.locator('#authenticator-form')).toBeVisible();
  expect(page.url()).toBe(ORIGIN + AUTHENTICATOR);
  await expectAlert(page, RETRY);
  await expect(code).toHaveValue('');
  expect(await keyOnPage(page)).toBe(key);
  // The probe carries nothing of the page's, and its answer is not kept.
  const probe = seen.probes[seen.probes.length - 1];
  expect(probe.method()).toBe('GET');
  expect(probe.postData()).toBeNull();
  expect((await probe.allHeaders())['x-csrf-token']).toBeUndefined();

  // And from there the set-up goes through as usual.
  await code.fill(authenticatorCode(keyBytes(key)));
  await confirmButton(page).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await codesOnPage(page);
  expect(seen.dataRequests).toEqual([]);
});

test('no clear answer, but the code did go through: the page loads itself anew and the server sends it on to the recovery codes', async ({ page }) => {
  const seen = watch(page);
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);

  // The server sets the authenticator up; its answer is lost on the way back.
  await answer(page, AUTHENTICATOR, 'POST', () => 'lost');
  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  await confirmButton(page).click();

  // The owner is not left on a page whose key no longer confirms: the codes are shown.
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await expect(page.locator('#recovery-codes-list')).toBeVisible();
  await codesOnPage(page);
  expect(seen.confirms).toHaveLength(1);
  expect(seen.probes).toHaveLength(1);
  expect(seen.dataRequests).toEqual([]);
  // The note the page left itself for the reload is gone from the address bar.
  expect(page.url()).toBe(ORIGIN + RECOVERY_CODES);
  await expectNotSignedIn(page);
});

test('a 403 means a stale anti-forgery pair: S-A6 loads itself anew once, says the request did not go through, and then works', async ({ page }) => {
  const seen = watch(page);
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);

  // Another tab opens the sign-in page: the server gives this browser a new pair, and this page's is stale.
  expect((await page.request.get(SIGN_IN)).status()).toBe(200);

  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  const loadedAnew = page.waitForEvent('load');
  const refusal = await confirmAndWait(page);
  expect(refusal.status()).toBe(403);
  await loadedAnew;
  await expect(page.locator('#authenticator-form')).toBeVisible();
  expect(page.url()).toBe(ORIGIN + AUTHENTICATOR);
  await expectAlert(page, RETRY);
  await expect(page.locator('#code')).toHaveValue('');
  expect(await keyOnPage(page)).toBe(key);
  expect(seen.confirms).toHaveLength(1);
  expect(seen.probes).toHaveLength(0);

  // A 403 that keeps coming does not make the page reload for ever: the retry sentence, and the form stays.
  const forbidden = await answer(page, AUTHENTICATOR, 'POST', (n) => (n === 1 ? { status: 403, text: 'Forbidden' } : 'real'));
  await page.locator('#code').fill('123456');
  await confirmButton(page).click();
  await expectAlert(page, RETRY);
  await expect(page.locator('#code')).toHaveValue('123456');
  expect(forbidden.count).toBe(1);
  expect(page.url()).toBe(ORIGIN + AUTHENTICATOR);

  // With the new pair the code goes through.
  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  await confirmButton(page).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
});

test('when a script S-A6 needs does not load, it says so and shows neither the key nor the form; S-A7 shows its codes whatever loaded', async ({ page }) => {
  const seen = watch(page);
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  for (const missing of ['admin-request.js', 'admin-dom.js', 'admin-arrival.js', 'admin-clipboard.js']) {
    await page.unrouteAll({ behavior: 'wait' });
    await page.route((url) => url.pathname === `/admin/auth/${missing}`, (route) => route.abort('failed'));
    await page.goto(AUTHENTICATOR);

    const alert = page.locator('#form-alert');
    await expect(alert, missing).toBeVisible();
    await expect(alert).toHaveText(RETRY);
    await expect(page.locator('h1')).toHaveText(AUTHENTICATOR_HEADING);
    await expect(page.locator('#authenticator-form')).toBeHidden();
    await expect(page.locator('#authenticator-key')).toBeHidden();
    await expect(page.locator('#authenticator-qr')).toBeHidden();
    await expect(page.locator('#code')).toBeHidden();
    await expect(page.getByRole('button')).toHaveCount(0);
    expect(await page.locator('body').innerText()).not.toContain(key.slice(0, 4));
  }
  expect(seen.confirms).toHaveLength(0);

  // With everything loaded again the same key is there, and the set-up goes on.
  await page.unrouteAll({ behavior: 'wait' });
  await page.goto(AUTHENTICATOR);
  await expect(page.locator('#authenticator-form')).toBeVisible();
  expect(await keyOnPage(page)).toBe(key);
  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  await confirmButton(page).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  const codes = await codesOnPage(page);

  // S-A7: the codes matter more than the buttons.
  for (const missing of ['admin-arrival.js', 'admin-clipboard.js', 'admin-request.js']) {
    await page.unrouteAll({ behavior: 'wait' });
    await page.route((url) => url.pathname === `/admin/auth/${missing}`, (route) => route.abort('failed'));
    await page.goto(RECOVERY_CODES);
    await expect(page.locator('#recovery-codes-list'), missing).toBeVisible();
    expect(await codesOnPage(page)).toEqual(codes);
    await expect(continueButton(page)).toBeDisabled();
    // Without the clipboard helper there is no copy button to press in vain.
    if (missing === 'admin-clipboard.js') await expect(copyAllButton(page)).toHaveCount(0);
    else await expect(copyAllButton(page)).toBeVisible();
  }

  // Without the request helper Continue cannot ask the server first; it still leaves for step 1.
  await savedBox(page).check();
  await continueButton(page).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('h1')).toHaveText(SIGN_IN_HEADING);
  expect(seen.signOuts).toHaveLength(0);
});

test('nothing on S-A6 or S-A7 stands where a global the scripts read should be', async ({ page }) => {
  const globals = [
    'AdminArrival', 'AdminDom', 'AdminRequest', 'AdminClipboard',
    'location', 'history', 'document', 'navigator', 'performance', 'fetch', 'URL', 'JSON', 'AbortController', 'getSelection',
    'name', 'status', 'top', 'self', 'parent', 'origin', 'length', 'event', 'closed', 'frames', 'opener',
  ];
  const check = async (helpers: string[]) => {
    const names = await page.evaluate(() => Array.from(document.querySelectorAll('[id], [name]'))
      .flatMap((el) => [el.getAttribute('id'), el.getAttribute('name')])
      .filter((name): name is string => !!name));
    expect(names.length).toBeGreaterThan(5);
    for (const name of names) expect(globals).not.toContain(name);
    expect(new Set(names.filter((n) => n !== 'code')).size).toBe(names.filter((n) => n !== 'code').length);
    // The helpers are the scripts' own objects, not elements that happen to carry the name.
    expect(await page.evaluate((list) => list.map((helper) => typeof (window as any)[helper]), helpers))
      .toEqual(helpers.map(() => 'object'));
  };

  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  await check(['AdminArrival', 'AdminDom', 'AdminRequest', 'AdminClipboard']);
  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  await confirmButton(page).click();
  await expect(page.locator('#recovery-codes-list')).toBeVisible();
  await check(['AdminArrival', 'AdminRequest', 'AdminClipboard']);
});

test('usable at 320 px wide and at 200% zoom: no sideways scrolling, and the QR code still fits and still reads back', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 640 });
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  await expectNoHorizontalScroll(page);

  // The QR code fits the column whole, stays square, and still holds the same address, at over four CSS px to a module.
  const narrow = await paintedQr(page);
  expect(narrow.left).toBeGreaterThanOrEqual(0);
  expect(narrow.right).toBeLessThanOrEqual(320);
  expect(narrow.moduleSize).toBeGreaterThan(4);
  expect(readQr(narrow.modules).text).toBe(otpAuthAddress(key));
  await page.evaluate(() => window.scrollTo(0, 0));
  await shot(page, 'authenticator_320');

  for (const selector of ['h1', '#authenticator-qr', '#manual-key', '#account-name', '#copy-key', '#code', '#authenticator-submit']) {
    const box = await page.locator(selector).boundingBox();
    expect(box, selector).not.toBeNull();
    expect(box!.x, selector).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width, selector).toBeLessThanOrEqual(320);
    // WCAG 2.2 target size: at least 24 by 24 CSS px for the controls.
    if (['#copy-key', '#code', '#authenticator-submit'].includes(selector)) {
      expect(box!.width, selector).toBeGreaterThanOrEqual(24);
      expect(box!.height, selector).toBeGreaterThanOrEqual(24);
    }
  }
  // The key wraps between its groups, never inside one.
  const tops = await page.locator('#manual-key > span').evaluateAll((spans) => spans.map((s) => s.getClientRects().length));
  for (const pieces of tops) expect(pieces).toBe(1);

  // With the alert and the field message showing, still no sideways scrolling.
  await answer(page, AUTHENTICATOR, 'POST', (n) => (n === 1 ? { status: 401, json: { error: SIGN_IN_GENERIC } } : 'real'));
  await page.locator('#code').fill('123456');
  await confirmButton(page).click();
  await expectAlert(page, SIGN_IN_GENERIC);
  await expectNoHorizontalScroll(page);
  await expect(page.locator('#form-alert')).toBeInViewport();
  await page.locator('#code').fill('12');
  await confirmButton(page).click();
  await expect(page.locator('#code-error')).toHaveText(CODE_REQUIRED);
  await expectNoHorizontalScroll(page);
  await shot(page, 'authenticator_320_error');

  // A 1280 px window zoomed to 200% lays out as 640 CSS px.
  await page.setViewportSize({ width: 640, height: 400 });
  await expectNoHorizontalScroll(page);
  const zoomed = await paintedQr(page);
  expect(zoomed.width).toBeCloseTo(240, 0);
  expect(readQr(zoomed.modules).text).toBe(otpAuthAddress(key));
  await confirmButton(page).scrollIntoViewIfNeeded();
  await expect(confirmButton(page)).toBeInViewport();

  // On to S-A7 at 320 px.
  await page.setViewportSize({ width: 320, height: 640 });
  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  await confirmButton(page).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await expect(page.locator('#recovery-codes-list')).toBeVisible();
  await expectNoHorizontalScroll(page);
  for (const selector of ['h1', '#recovery-codes-list', '#copy-codes', '#codes-saved', '#codes-continue']) {
    const box = await page.locator(selector).boundingBox();
    expect(box, selector).not.toBeNull();
    expect(box!.x, selector).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width, selector).toBeLessThanOrEqual(320);
    if (selector !== 'h1' && selector !== '#recovery-codes-list') {
      expect(box!.width, selector).toBeGreaterThanOrEqual(24);
      expect(box!.height, selector).toBeGreaterThanOrEqual(24);
    }
  }
  // No code is cut off or broken over two lines.
  const items = await page.locator('#recovery-codes-list li').evaluateAll((list) => list.map((li) => ({
    lines: li.getClientRects().length, overflow: li.scrollWidth > li.clientWidth,
  })));
  for (const item of items) expect(item).toEqual({ lines: 1, overflow: false });
  await shot(page, 'recovery-codes_320');

  await page.setViewportSize({ width: 640, height: 400 });
  await expectNoHorizontalScroll(page);
  await savedBox(page).check();
  await continueButton(page).scrollIntoViewIfNeeded();
  await expect(continueButton(page)).toBeInViewport();
});

test('axe finds nothing serious or critical on S-A6 and S-A7, plain and in their error states', async ({ page }) => {
  await reachAuthenticatorPage(page);
  const key = await keyOnPage(page);
  await expectNoSeriousAxeFindings(page, 'S-A6-form');

  await confirmButton(page).click();
  await expect(page.locator('#code-error')).toHaveText(CODE_REQUIRED);
  await expectNoSeriousAxeFindings(page, 'S-A6-field-error');

  await answer(page, AUTHENTICATOR, 'POST', (n) => (n === 1 ? { status: 401, json: { error: SIGN_IN_GENERIC } } : 'real'));
  await page.locator('#code').fill('123456');
  await confirmButton(page).click();
  await expectAlert(page, SIGN_IN_GENERIC);
  await expectNoSeriousAxeFindings(page, 'S-A6-alert');

  await standInForClipboard(page, { clipboard: 'refuses', execCommand: false });
  await copyKeyButton(page).click();
  await expect(page.locator('#copy-status')).toHaveText(COPY_FAILED);
  await expectNoSeriousAxeFindings(page, 'S-A6-copy-failed');

  await page.locator('#code').fill(authenticatorCode(keyBytes(key)));
  await confirmButton(page).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY_CODES);
  await expect(page.locator('#recovery-codes-list')).toBeVisible();
  await expectNoSeriousAxeFindings(page, 'S-A7-codes');

  await standInForClipboard(page, { clipboard: 'accepts' });
  await copyAllButton(page).click();
  await expect(page.locator('#copy-status')).toHaveText(COPIED);
  await savedBox(page).check();
  await expect(continueButton(page)).toBeEnabled();
  await expectNoSeriousAxeFindings(page, 'S-A7-saved');
});

test.describe('JavaScript off', () => {
  test.use({ javaScriptEnabled: false });

  test('with JavaScript off S-A6 and S-A7 show their heading, one sentence and nothing else', async ({ page }) => {
    const seen = watch(page);
    const csrfOf = (html: string) => html.match(/<meta name="dw-csrf" content="([0-9A-F]{64})">/)![1];
    const stepOne = async () => {
      // Step 1 through the API, as a script-less browser cannot do it: only to get a pending sign-in.
      const html = await (await page.request.get(SIGN_IN)).text();
      const reply = await page.request.post(API_SIGN_IN, {
        headers: { 'X-CSRF-TOKEN': csrfOf(html), Origin: ORIGIN },
        data: { email: owner.email, password: owner.password, turnstileToken: PASS_TOKEN },
      });
      expect(reply.status()).toBe(200);
      return await reply.json();
    };
    expect(await stepOne()).toEqual({ next: 'enroll' });

    const response = await page.goto(AUTHENTICATOR);
    expect(response!.status()).toBe(200);
    await expect(page.locator('h1')).toHaveText(AUTHENTICATOR_HEADING);
    await expect(page.getByText(AUTHENTICATOR_NOSCRIPT, { exact: true })).toBeVisible();
    await expect(page.locator('#authenticator-panel')).toBeHidden();
    await expect(page.locator('#authenticator-qr')).toBeHidden();
    await expect(page.locator('#manual-key')).toBeHidden();
    await expect(page.locator('#code')).toBeHidden();
    await expect(page.getByRole('button')).toHaveCount(0);
    await expect(page.getByRole('img')).toHaveCount(0);
    let lines = (await page.locator('body').innerText()).split('\n').map((line) => line.trim()).filter(Boolean);
    expect(lines).toEqual([AUTHENTICATOR_HEADING, AUTHENTICATOR_NOSCRIPT]);
    await shot(page, 'authenticator_js_off');

    // The set-up itself, again through the API: the key as the page carries it, and the code for it.
    const html = await response!.text();
    const key = Array.from(html.matchAll(/<span>([A-Z2-7]{4})<\/span>/g)).map((m) => m[1]).join('');
    expect(key).toHaveLength(32);
    const confirmed = await page.request.post(AUTHENTICATOR, {
      headers: { 'X-CSRF-TOKEN': csrfOf(html), Origin: ORIGIN },
      data: { code: authenticatorCode(keyBytes(key)) },
    });
    expect(confirmed.status()).toBe(200);
    expect(await confirmed.json()).toEqual({ enrolled: true, next: 'recovery-codes' });

    const codesPage = await page.goto(RECOVERY_CODES);
    expect(codesPage!.status()).toBe(200);
    await expect(page.locator('h1')).toHaveText(RECOVERY_CODES_HEADING);
    await expect(page.getByText(RECOVERY_CODES_NOSCRIPT, { exact: true })).toBeVisible();
    await expect(page.locator('#recovery-codes-panel')).toBeHidden();
    await expect(page.locator('#recovery-codes-list')).toBeHidden();
    await expect(page.getByRole('button')).toHaveCount(0);
    await expect(page.getByRole('checkbox')).toHaveCount(0);
    lines = (await page.locator('body').innerText()).split('\n').map((line) => line.trim()).filter(Boolean);
    expect(lines).toEqual([RECOVERY_CODES_HEADING, RECOVERY_CODES_NOSCRIPT]);

    // Once the codes can no longer be shown the page is plain markup: its sentence and its link need no script.
    expect(await stepOne()).toEqual({ next: 'totp' });
    await page.goto(RECOVERY_CODES);
    await expect(page.getByText(RECOVERY_CODES_ALREADY_SHOWN, { exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Back to sign in', exact: true })).toBeVisible();
    lines = (await page.locator('body').innerText()).split('\n').map((line) => line.trim()).filter(Boolean);
    expect(lines).toEqual([RECOVERY_CODES_HEADING, RECOVERY_CODES_ALREADY_SHOWN, 'Back to sign in']);
    expect(seen.confirms).toHaveLength(0);
  });
});
