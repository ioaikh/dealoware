import { test, expect, type Locator, type Page, type Request, type Response } from '@playwright/test';
import { expectNoSeriousAxeFindings } from './axe';
import { authenticatorCode, seedOwner, wrongAuthenticatorCode, type Owner } from './owner';
import {
  WIDGET_HOST, WIDGET_SCRIPT, answerCloudflare, widgetSays, widgetState, type WidgetSetup,
} from '../signed-out/cloudflare';

/**
 * The three sign-in pages against the real app: S-A1 /admin/sign-in, S-A2
 * /admin/sign-in/code and S-A3 /admin/sign-in/recovery.
 * Start it with e2e/sign-in/run.sh.
 *
 * The browser never reaches Cloudflare: every request to
 * challenges.cloudflare.com is answered with a stand-in widget
 * (e2e/signed-out/cloudflare.ts), loaded under the page's real
 * Content-Security-Policy. The server checks the widget's token with the
 * stand-in verifier the runner starts, which accepts exactly one run-time
 * token.
 *
 * Before every test the owner is seeded afresh (owner.ts): a password and an
 * enrolled authenticator unless the test asks otherwise, and no pending
 * sign-in, session, failure count or lock left from the test before. So the
 * tests do not depend on their order, and a test may fail a sign-in on
 * purpose without locking the next one out. They still run on one worker:
 * there is one owner.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const RECOVERY = '/admin/sign-in/recovery';
const RESET = '/admin/reset';
const ENROLL = '/admin/setup/authenticator';
const HOME = '/admin/';
const API_SIGN_IN = '/admin/api/auth/sign-in';
const API_CODE = '/admin/api/auth/sign-in/code';
const API_RECOVERY = '/admin/api/auth/sign-in/recovery';
// Where step 2 asks "am I signed in?" after a reply it could not read.
const API_ME = '/admin/api/me';
const SITE_KEY = 'e2e-site-key';

const SIGN_IN_TITLE = 'Sign in · Dealoware admin';
const SIGN_IN_HEADING = 'Sign in';
const CODE_TITLE = 'Enter code · Dealoware admin';
const CODE_HEADING = 'Enter code';
const RECOVERY_TITLE = 'Recovery code · Dealoware admin';
const RECOVERY_HEADING = 'Recovery code';
const NOSCRIPT = 'Turn on JavaScript to sign in.';
const EMAIL_REQUIRED = 'Enter an email address.';
const EMAIL_INVALID = 'Enter a well-formed email address.';
const PASSWORD_REQUIRED = 'Enter a password.';
const CODE_REQUIRED = 'Enter the 6-digit code.';
const RECOVERY_REQUIRED = 'Enter a recovery code.';
const RETRY = 'That didn\'t go through. Wait a moment and try again.';
// The server's two sentences. The pages show them as sent.
const SIGN_IN_GENERIC =
  'We couldn\'t sign you in. Check your details and try again later. You can also reset your password.';
const VERIFICATION_FAILED = 'Verification failed, please try again.';
// The session gate's word when there is no pending sign-in.
const GATE_DENIED = 'Unauthorized';
const CHECK_LOADING = 'Security check is loading.';
const CHECK_FAILED = 'The security check failed.';
const CHECK_EXPIRED = 'The security check expired.';
const CHECK_UNAVAILABLE =
  'The security check is unavailable. Check your network or disable extensions that block scripts, then retry.';
const CHECK_NOT_CONFIGURED = 'The security check is unavailable.';

/** Globals the pages' scripts read. No element id or control name may be one of them. */
const GLOBALS_THE_SCRIPTS_READ = [
  'turnstile', 'AdminDom', 'AdminPassword', 'AdminTurnstile', 'AdminRequest',
  'location', 'history', 'document', 'fetch', 'URL', 'JSON', 'AbortController',
  'name', 'status', 'top', 'self', 'parent', 'origin', 'length', 'event', 'closed', 'frames', 'opener',
];

const PASS_TOKEN = process.env.SIGN_IN_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.SIGN_IN_E2E_ARTIFACTS;

/** How a test answers one POST to a sign-in endpoint. */
type Answer =
  | 'real' // let the server answer
  | 'abort' // the connection fails
  | 'never' // no answer at all
  | { status: number; json?: unknown; text?: string; headers?: Record<string, string> }
  | { sendInstead: Record<string, unknown> }; // the server answers a changed request

let owner: Owner;
let cspMessages: string[] = [];
let pageErrors: string[] = [];
let dialogs: string[] = [];

async function shot(page: Page, name: string): Promise<void> {
  if (!ARTIFACTS) return;
  await page.screenshot({ path: `${ARTIFACTS}/${name}.png`, fullPage: true });
}

/**
 * Answers everything addressed to Cloudflare. Unless the test says otherwise
 * the widget solves at once with the token the verifier accepts.
 */
function standInForCloudflare(page: Page, setup: WidgetSetup = {}): Promise<{ scripts: number }> {
  return answerCloudflare(page, { token: PASS_TOKEN, ...setup });
}

/** Decides what each POST to one endpoint gets. Without it every request goes to the real server. */
async function answerPosts(
  page: Page,
  apiPath: string,
  answer: (n: number, sent: Record<string, unknown>) => Answer | Promise<Answer>,
): Promise<{ count: number }> {
  const counts = { count: 0 };
  await page.route((url) => url.pathname === apiPath, async (route) => {
    const sent = (route.request().postDataJSON() ?? {}) as Record<string, unknown>;
    counts.count += 1;
    const reply = await answer(counts.count, sent);
    if (reply === 'real') {
      await route.continue();
    } else if (reply === 'abort') {
      await route.abort('connectionfailed');
    } else if (reply === 'never') {
      await new Promise(() => { /* the page gives up after 15 s */ });
    } else if ('sendInstead' in reply) {
      await route.continue({ postData: JSON.stringify(reply.sendInstead) });
    } else if (reply.json !== undefined) {
      await route.fulfill({
        status: reply.status, contentType: 'application/json', body: JSON.stringify(reply.json), headers: reply.headers,
      });
    } else {
      await route.fulfill({ status: reply.status, contentType: 'text/plain', body: reply.text ?? '', headers: reply.headers });
    }
  });
  return counts;
}

function isPostTo(apiPath: string): (request: Request) => boolean {
  return (request) => new URL(request.url()).pathname === apiPath && request.method() === 'POST';
}

function isReplyTo(apiPath: string): (response: Response) => boolean {
  return (response) => isPostTo(apiPath)(response.request());
}

/** Records every request the page makes: where it went, and the sign-in requests among them. */
function watch(page: Page): {
  urls: string[]; referers: string[]; cloudflare: string[];
  signIns: Request[]; codes: Request[]; recoveries: Request[]; signedInChecks: Request[];
} {
  const seen = {
    urls: [] as string[],
    referers: [] as string[],
    cloudflare: [] as string[],
    signIns: [] as Request[],
    codes: [] as Request[],
    recoveries: [] as Request[],
    signedInChecks: [] as Request[],
  };
  page.on('request', (request) => {
    seen.urls.push(request.url());
    const referer = request.headers()['referer'];
    if (referer) seen.referers.push(referer);
    if (new URL(request.url()).hostname === WIDGET_HOST) seen.cloudflare.push(request.url());
    if (isPostTo(API_SIGN_IN)(request)) seen.signIns.push(request);
    if (isPostTo(API_CODE)(request)) seen.codes.push(request);
    if (isPostTo(API_RECOVERY)(request)) seen.recoveries.push(request);
    if (new URL(request.url()).pathname === API_ME) seen.signedInChecks.push(request);
  });
  return seen;
}

function continueButton(page: Page): Locator {
  return page.getByRole('button', { name: 'Continue', exact: true });
}

/** The step 2 button, on the code page and on the recovery page. */
function signInButton(page: Page): Locator {
  return page.getByRole('button', { name: 'Sign in', exact: true });
}

/** S-A1 with the security check answered: the form is on screen and can be sent. */
async function openSignIn(page: Page, setup: WidgetSetup = {}): Promise<void> {
  await standInForCloudflare(page, setup);
  await page.goto(SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
}

async function fillSignIn(page: Page, email: string, password: string): Promise<void> {
  await page.locator('#email').fill(email);
  await page.locator('#password').fill(password);
}

async function continueAndWait(page: Page): Promise<Response> {
  const [response] = await Promise.all([
    page.waitForResponse(isReplyTo(API_SIGN_IN)),
    continueButton(page).click(),
  ]);
  return response;
}

/** Step 1 done with the right details, through the page: the code page is open with a pending sign-in. */
async function reachCodePage(page: Page): Promise<void> {
  await openSignIn(page);
  await fillSignIn(page, owner.email, owner.password);
  await expect(continueButton(page)).toBeEnabled();
  await continueButton(page).click();
  await expect(page).toHaveURL(ORIGIN + CODE);
  await expect(page.locator('#second-step-form')).toBeVisible();
}

async function reachRecoveryPage(page: Page): Promise<void> {
  await reachCodePage(page);
  await page.getByRole('link', { name: 'Use a recovery code instead', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY);
  await expect(page.locator('#second-step-form')).toBeVisible();
}

async function sendCodeAndWait(page: Page, apiPath: string): Promise<Response> {
  const [response] = await Promise.all([
    page.waitForResponse(isReplyTo(apiPath)),
    signInButton(page).click(),
  ]);
  return response;
}

/** The one alert of S-A1: the text, announced, with the focus on it. */
async function expectAlert(page: Page, text: string): Promise<void> {
  const alert = page.locator('#form-alert');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveText(text);
  await expect(alert).toHaveAttribute('role', 'alert');
  await expect(alert).toBeFocused();
  await expect(page.locator('[role="alert"]')).toHaveCount(1);
}

/** The one alert of S-A2 and S-A3: the same region, announced, with the cursor back in the field. */
async function expectAlertAboveField(page: Page, text: string, field: Locator): Promise<void> {
  const alert = page.locator('#form-alert');
  await expect(alert).toBeVisible();
  await expect(alert).toHaveText(text);
  await expect(alert).toHaveAttribute('role', 'alert');
  await expect(page.locator('[role="alert"]')).toHaveCount(1);
  await expect(field).toBeFocused();
}

async function expectSignedIn(page: Page, path: string): Promise<void> {
  await expect(page).toHaveURL(ORIGIN + path);
  const cookies = await page.context().cookies();
  const session = cookies.find((c) => c.name === 'dw_admin_session');
  expect(session, 'the session cookie').toBeDefined();
  expect(session!.httpOnly).toBe(true);
  expect(session!.secure).toBe(true);
  expect(session!.sameSite).toBe('Strict');
  expect(cookies.find((c) => c.name === 'dw_admin_pending'), 'the pending sign-in is used up').toBeUndefined();
  const me = await page.request.get('/admin/api/me');
  expect(me.status()).toBe(200);
}

async function expectNotSignedIn(page: Page): Promise<void> {
  const cookies = await page.context().cookies();
  expect(cookies.find((c) => c.name === 'dw_admin_session')).toBeUndefined();
  const me = await page.request.get('/admin/api/me');
  expect(me.status()).toBe(401);
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
  cspMessages = [];
  pageErrors = [];
  dialogs = [];
  page.on('console', (message) => {
    if (/content security policy/i.test(message.text())) cspMessages.push(message.text());
  });
  page.on('pageerror', (error) => pageErrors.push(error.message));
  page.on('dialog', async (dialog) => {
    dialogs.push(dialog.message());
    await dialog.dismiss();
  });
});

test.afterEach(() => {
  // Nothing the pages load, and nothing the tests stand in for, may be refused by a page's own policy.
  expect(cspMessages).toEqual([]);
  expect(pageErrors).toEqual([]);
  expect(dialogs).toEqual([]);
});

test('S-A1 is HTML under a nonce policy that allows only Cloudflare, styled, with the cursor in Email', async ({ page }) => {
  const seen = watch(page);
  const counts = await standInForCloudflare(page);
  const response = await page.goto(SIGN_IN);
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
  expect(response!.headers()['x-csrf-token']).toBeUndefined();

  await expect(page.locator('#sign-in-form')).toBeVisible();
  await expect(page).toHaveTitle(SIGN_IN_TITLE);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText(SIGN_IN_HEADING);
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page.locator('#email')).toBeFocused();
  await expect(page.getByText(NOSCRIPT)).toBeHidden();
  await expect(page.locator('#form-alert')).toBeHidden();
  await expect(page.locator('nav')).toHaveCount(0);

  // The linked stylesheet applied under style-src 'nonce-...'.
  const colours = await page.evaluate(() => ({
    body: getComputedStyle(document.body).backgroundColor,
    card: getComputedStyle(document.querySelector('.auth-card')!).backgroundColor,
    button: getComputedStyle(document.getElementById('sign-in-submit')!).backgroundColor,
  }));
  expect(colours).toEqual({ body: 'rgb(244, 241, 234)', card: 'rgb(255, 255, 255)', button: 'rgb(11, 79, 138)' });

  // The widget script and its frame came from Cloudflare's origin, once, and were allowed.
  await expect(continueButton(page)).toBeEnabled();
  await expect(page.locator('#security-check-status')).toHaveText('');
  expect(counts.scripts).toBe(1);
  expect(seen.cloudflare[0]).toBe(WIDGET_SCRIPT);
  await expect(page.frameLocator('#security-check-widget iframe').getByText('Security check stand-in')).toBeVisible();
  expect(await widgetState(page)).toEqual({ renders: 1, resets: 0, sitekey: SITE_KEY, size: 'normal' });

  // The primary button says Continue; the one link goes to the reset-request page.
  await expect(page.locator('#sign-in-submit')).toHaveText('Continue');
  await expect(page.getByRole('button', { name: 'Sign in', exact: true })).toHaveCount(0);
  const reset = page.getByRole('link', { name: 'Forgot your password?', exact: true });
  await expect(reset).toHaveAttribute('href', RESET);
  await expect(page.getByRole('link')).toHaveCount(1);

  // Opening the page sends nothing and keeps nothing where a script can read it.
  expect(seen.signIns).toHaveLength(0);
  expect(await page.evaluate(() => ({
    local: localStorage.length, session: sessionStorage.length, cookie: document.cookie,
  }))).toEqual({ local: 0, session: 0, cookie: '' });
  expect(seen.referers).toEqual([]);
  for (const url of seen.urls) expect([new URL(ORIGIN).hostname, WIDGET_HOST]).toContain(new URL(url).hostname);
  await shot(page, 'sign-in_desktop');
});

test('S-A1 fields: types, autocomplete, paste allowed, and a show toggle that keeps one label', async ({ page }) => {
  await openSignIn(page);
  const email = page.locator('#email');
  const password = page.locator('#password');
  const toggle = page.locator('#password-toggle');

  await expect(email).toHaveAttribute('type', 'email');
  await expect(email).toHaveAttribute('autocomplete', 'username');
  await expect(email).toHaveAttribute('inputmode', 'email');
  await expect(email).toHaveAttribute('autocapitalize', 'none');
  await expect(email).toHaveAttribute('spellcheck', 'false');
  await expect(email).toHaveAttribute('aria-describedby', 'email-error');
  await expect(page.getByRole('textbox', { name: 'Email', exact: true })).toHaveAttribute('id', 'email');
  await expect(password).toHaveAttribute('type', 'password');
  await expect(password).toHaveAttribute('autocomplete', 'current-password');
  await expect(password).toHaveAttribute('aria-describedby', 'password-error');
  await expect(page.locator('label[for="password"]')).toHaveText('Password');

  // Nothing stops a paste, into either field.
  for (const field of [email, password]) {
    expect(await field.evaluate((el) => el.dispatchEvent(
      new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData: new DataTransfer() })))).toBe(true);
  }

  await expect(toggle).toHaveText('Show');
  await expect(toggle).toHaveAttribute('aria-controls', 'password');
  await expect(page.getByRole('button', { name: 'Show Password', exact: true, pressed: false })).toHaveCount(1);
  const unpressed = await toggle.evaluate((el) => getComputedStyle(el).backgroundColor);
  await password.fill(owner.password);
  await toggle.click();
  await expect(password).toHaveAttribute('type', 'text');
  await expect(password).toHaveValue(owner.password);
  // Same words; only the pressed state, and how the button looks, changed.
  await expect(toggle).toHaveText('Show');
  await expect(page.getByRole('button', { name: 'Show Password', exact: true, pressed: true })).toHaveCount(1);
  expect(await toggle.evaluate((el) => getComputedStyle(el).backgroundColor)).not.toBe(unpressed);
  await toggle.click();
  await expect(password).toHaveAttribute('type', 'password');
  await expect(toggle).toHaveAttribute('aria-pressed', 'false');
  await expect(page.getByText('Hide')).toHaveCount(0);
});

test('S-A1 empty or malformed fields are caught in the browser: the message sits by the field and nothing is sent', async ({ page }) => {
  const seen = watch(page);
  await openSignIn(page);
  await expect(continueButton(page)).toBeEnabled();
  const email = page.locator('#email');
  const password = page.locator('#password');

  // Both empty: both messages, and the cursor goes to the first one.
  await continueButton(page).click();
  await expect(page.locator('#email-error')).toHaveText(EMAIL_REQUIRED);
  await expect(page.locator('#password-error')).toHaveText(PASSWORD_REQUIRED);
  await expect(email).toHaveAttribute('aria-invalid', 'true');
  await expect(password).toHaveAttribute('aria-invalid', 'true');
  await expect(email).toBeFocused();
  await expect(page.locator('#form-alert')).toBeHidden();

  // An email that does not look like one. Typing clears the message it sits under.
  await email.fill('not-an-email');
  await expect(page.locator('#email-error')).toHaveText('');
  await expect(email).not.toHaveAttribute('aria-invalid');
  await password.fill(owner.password);
  await expect(password).not.toHaveAttribute('aria-invalid');
  await password.press('Enter');
  await expect(page.locator('#email-error')).toHaveText(EMAIL_INVALID);
  await expect(page.locator('#password-error')).toHaveText('');
  await expect(email).toBeFocused();
  await expect(password).toHaveValue(owner.password);

  // A password missing: the cursor goes there.
  await email.fill(owner.email);
  await password.fill('');
  await continueButton(page).click();
  await expect(page.locator('#password-error')).toHaveText(PASSWORD_REQUIRED);
  await expect(page.locator('#email-error')).toHaveText('');
  await expect(password).toBeFocused();

  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  expect(seen.signIns).toHaveLength(0);
  // The security check was not spent on any of it.
  expect((await widgetState(page)).resets).toBe(0);
});

test('full sign-in, keyboard only: Continue goes to the code page, the code signs in and lands on /admin/', async ({ page }) => {
  const seen = watch(page);
  await openSignIn(page);
  await expect(continueButton(page)).toBeEnabled();

  // No mouse from here on. S-A1: Email, Password, its Show toggle, the security check, Continue, the reset link.
  await expect(page.locator('#email')).toBeFocused();
  await page.keyboard.type(owner.email);
  const order = ['email'];
  for (let i = 0; i < 8 && order[order.length - 1] !== 'reset-link'; i += 1) {
    await page.keyboard.press('Tab');
    const stop = await focusStop(page);
    order.push(stop);
    if (stop === 'password') await page.keyboard.type(owner.password);
    if (stop === 'password-toggle') {
      // Space works the toggle, and works it back.
      await page.keyboard.press('Space');
      await expect(page.locator('#password')).toHaveAttribute('type', 'text');
      await page.keyboard.press('Space');
      await expect(page.locator('#password')).toHaveAttribute('type', 'password');
    }
  }
  // The security check's own frame may take a stop between the fields and the button.
  expect(order.filter((stop) => stop !== 'iframe')).toEqual([
    'email', 'password', 'password-toggle', 'sign-in-submit', 'reset-link',
  ]);
  await page.keyboard.press('Shift+Tab');
  await expect(continueButton(page)).toBeFocused();
  const ring = await continueButton(page).evaluate((el) => {
    const style = getComputedStyle(el);
    return { style: style.outlineStyle, width: style.outlineWidth, offset: style.outlineOffset, colour: style.outlineColor };
  });
  expect(ring).toEqual({ style: 'solid', width: '2px', offset: '2px', colour: 'rgb(11, 79, 138)' });

  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + CODE);

  // Step 1 sent the three contract fields and nothing else, with the anti-forgery header and the exact origin.
  expect(seen.signIns).toHaveLength(1);
  expect(seen.signIns[0].postDataJSON()).toEqual({
    email: owner.email, password: owner.password, turnstileToken: PASS_TOKEN,
  });
  const sent = await seen.signIns[0].allHeaders();
  expect(sent['origin']).toBe(ORIGIN);
  expect(sent['x-csrf-token']).toMatch(/^[0-9A-F]{64}$/);
  expect(sent['content-type']).toBe('application/json');
  expect(sent['referer']).toBeUndefined();

  // S-A2: the code field has the cursor; then Sign in, the recovery link, Back to sign in.
  await expect(page).toHaveTitle(CODE_TITLE);
  await expect(page.locator('h1')).toHaveText(CODE_HEADING);
  await expect(page.locator('#code')).toBeFocused();
  await expectNotSignedIn(page);
  await shot(page, 'sign-in_code');
  await page.keyboard.type(authenticatorCode(owner.secret!));
  const secondOrder = ['code'];
  for (let i = 0; i < 3; i += 1) {
    await page.keyboard.press('Tab');
    secondOrder.push(await focusStop(page));
  }
  expect(secondOrder).toEqual(['code', 'second-step-submit', 'other-step-link', 'back-link']);
  await page.keyboard.press('Shift+Tab');
  await page.keyboard.press('Shift+Tab');
  await expect(signInButton(page)).toBeFocused();
  await page.keyboard.press('Enter');

  // The server named /admin/ (no return path was asked for), and the session exists only now.
  await expectSignedIn(page, HOME);
  expect(seen.codes).toHaveLength(1);
  expect(Object.keys(seen.codes[0].postDataJSON())).toEqual(['code']);
  expect(seen.signIns).toHaveLength(1);
  expect(seen.referers).toEqual([]);
});

test('the path the server names after the code is followed when it is a plain admin path', async ({ page }) => {
  const seen = watch(page);
  // The page sends no return path of its own. Here one is added on the way to the
  // server, which checks it against its list and hands it back after the code.
  await answerPosts(page, API_SIGN_IN, (_n, sent) => {
    expect(Object.keys(sent).sort()).toEqual(['email', 'password', 'turnstileToken']);
    return { sendInstead: { ...sent, returnPath: '/admin/participants' } };
  });
  await reachCodePage(page);
  expect(page.url()).toBe(ORIGIN + CODE);

  await page.locator('#code').fill(authenticatorCode(owner.secret!));
  await signInButton(page).click();
  await expectSignedIn(page, '/admin/participants');
  expect(seen.codes).toHaveLength(1);
});

test('a hostile or malformed "next" is never followed: the page goes to /admin/ instead', async ({ page }) => {
  const seen = watch(page);
  let reply: Record<string, unknown> = {};
  await reachCodePage(page);
  const counts = await answerPosts(page, API_CODE, () => ({ status: 200, json: reply }));

  const refused: unknown[] = [
    'https://evil.example/',
    'http://evil.example/admin/',
    '//evil.example/admin/',
    '/\\evil.example',
    '/admin/\\evil.example',
    '/admin//evil.example',
    'javascript:alert(document.domain)',
    'data:text/html,<script>alert(1)</script>',
    '/admin/../health',
    '/admin/%2e%2e/health',
    '/admin/participants?next=//evil.example',
    '/admin/participants#x',
    '/admin/participants/',
    '/admin',
    '/health',
    'admin/participants',
    ' /admin/participants',
    '/admin/participants\n',
    '/admin/part\u0000icipants',
    '/admin/part​icipants',
    `/admin/${'a'.repeat(2048)}`,
    '',
    null,
    42,
    true,
    ['/admin/participants'],
    { path: '/admin/participants' },
  ];
  for (const value of refused) {
    reply = { verified: true, next: value };
    await page.locator('#code').fill('123456');
    await signInButton(page).click();
    await expect(page, `next = ${JSON.stringify(value)}`).toHaveURL(ORIGIN + HOME);
    // Nothing was signed in (the reply was the test's), so the pending sign-in still opens the code page.
    await page.goto(CODE);
    await expect(page.locator('#second-step-form')).toBeVisible();
  }

  // A reply without any "next" goes to /admin/ too.
  reply = { verified: true };
  await page.locator('#code').fill('123456');
  await signInButton(page).click();
  await expect(page).toHaveURL(ORIGIN + HOME);

  // Control: the same stand-in reply with a plain admin path is followed.
  await page.goto(CODE);
  reply = { verified: true, next: '/admin/audit' };
  await page.locator('#code').fill('123456');
  await signInButton(page).click();
  await expect(page).toHaveURL(`${ORIGIN}/admin/audit`);

  expect(counts.count).toBe(refused.length + 2);
  // Nothing was asked of any other host, at any point.
  for (const url of seen.urls) expect([new URL(ORIGIN).hostname, WIDGET_HOST]).toContain(new URL(url).hostname);
});

test('wrong password: the server sentence is shown as sent, the email stays, the password is cleared, and the form still works', async ({ page }) => {
  const seen = watch(page);
  await openSignIn(page);
  await fillSignIn(page, owner.email, `${owner.password}-wrong`);
  const response = await continueAndWait(page);
  expect(response.status()).toBe(401);
  expect(response.headers()['retry-after']).toBeUndefined();
  const body = await response.json();
  expect(body).toEqual({ error: SIGN_IN_GENERIC });

  await expectAlert(page, body.error);
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await expect(page.locator('#email')).toHaveValue(owner.email);
  await expect(page.locator('#password')).toHaveValue('');
  // No field is blamed: the server did not say which detail was wrong.
  await expect(page.locator('#email')).not.toHaveAttribute('aria-invalid');
  await expect(page.locator('#password')).not.toHaveAttribute('aria-invalid');
  await expect(page.locator('#email-error')).toHaveText('');
  await expect(page.locator('#password-error')).toHaveText('');
  // The spent token is gone: the widget ran again and only then is Continue back.
  await expect.poll(async () => (await widgetState(page)).resets).toBe(1);
  await expect(continueButton(page)).toBeEnabled();
  await expect(page.locator('#sign-in-form')).not.toHaveAttribute('aria-busy');
  await expectNotSignedIn(page);
  await shot(page, 'sign-in_error');

  // Only the password has to be typed again.
  await page.locator('#password').fill(owner.password);
  await continueButton(page).click();
  await expect(page).toHaveURL(ORIGIN + CODE);
  expect(seen.signIns).toHaveLength(2);
});

test('an email that is not the owner\'s gets exactly the same answer as a wrong password', async ({ page }) => {
  await openSignIn(page);
  await fillSignIn(page, 'someone.else@example.test', owner.password);
  const response = await continueAndWait(page);
  expect(response.status()).toBe(401);
  expect(await response.json()).toEqual({ error: SIGN_IN_GENERIC });
  await expectAlert(page, SIGN_IN_GENERIC);
  await expect(page.locator('#email')).toHaveValue('someone.else@example.test');
  await expect(page.locator('#password')).toHaveValue('');
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
});

test('security check refused by the server (401): its own sentence is shown, the widget is reset and the form stays usable', async ({ page }) => {
  await openSignIn(page, { token: 'a-token-the-verifier-refuses' });
  await fillSignIn(page, owner.email, owner.password);
  const response = await continueAndWait(page);
  expect(response.status()).toBe(401);
  const body = await response.json();
  expect(body).toEqual({ error: VERIFICATION_FAILED });

  await expectAlert(page, body.error);
  await expect(page.locator('#form-alert')).not.toHaveText(SIGN_IN_GENERIC);
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#email')).toHaveValue(owner.email);
  await expect.poll(async () => (await widgetState(page)).resets).toBe(1);
  await expect(continueButton(page)).toBeEnabled();
  await expect(page.locator('#sign-in-form')).not.toHaveAttribute('aria-busy');
  // No pending sign-in came of it.
  expect((await page.context().cookies()).find((c) => c.name === 'dw_admin_pending')).toBeUndefined();
});

test('Continue works only with a fresh security-check token: loading, failed, retry, ready, expired, unavailable', async ({ page }) => {
  const seen = watch(page);
  let sentToken: unknown;
  await answerPosts(page, API_SIGN_IN, (_n, sent) => {
    sentToken = sent.turnstileToken;
    return { status: 401, json: { error: VERIFICATION_FAILED } };
  });
  await openSignIn(page, { auto: false });
  const status = page.locator('#security-check-status');
  const retry = page.locator('#security-check-retry');
  const submit = continueButton(page);
  await fillSignIn(page, owner.email, owner.password);

  await expect(status).toHaveText(CHECK_LOADING);
  await expect(status).toHaveAttribute('role', 'status');
  await expect(submit).toBeDisabled();
  await expect(submit).toHaveAttribute('aria-describedby', 'security-check-status');
  await expect(retry).toBeHidden();
  await page.locator('#password').press('Enter');

  await widgetSays(page, 'error-callback');
  await expect(status).toHaveText(CHECK_FAILED);
  await expect(submit).toBeDisabled();
  await expect(retry).toBeVisible();
  await page.locator('#password').press('Enter');

  // Retry re-runs the widget and keeps keyboard focus in place.
  await retry.click();
  await expect(status).toHaveText(CHECK_LOADING);
  await expect(status).toBeFocused();
  await expect(submit).toBeDisabled();
  expect((await widgetState(page)).resets).toBe(1);

  await widgetSays(page, 'callback', 'first-token');
  await expect(status).toHaveText('');
  await expect(submit).toBeEnabled();

  await widgetSays(page, 'expired-callback');
  await expect(status).toHaveText(CHECK_EXPIRED);
  await expect(submit).toBeDisabled();
  await page.locator('#password').press('Enter');

  await widgetSays(page, 'timeout-callback');
  await expect(status).toHaveText(CHECK_UNAVAILABLE);
  await expect(submit).toBeDisabled();
  await submit.click({ force: true });
  await page.evaluate(() => (document.getElementById('sign-in-form') as HTMLFormElement).requestSubmit());
  expect(seen.signIns).toHaveLength(0);

  // After a failure the next request carries the new token, never the old one.
  await retry.click();
  await widgetSays(page, 'callback', 'second-token');
  await expect(submit).toBeEnabled();
  await submit.click();
  await expectAlert(page, VERIFICATION_FAILED);
  expect(sentToken).toBe('second-token');
  expect(seen.signIns).toHaveLength(1);
});

test('with no site key S-A1 fails closed: it says so, loads nothing from Cloudflare and cannot send', async ({ page }) => {
  const seen = watch(page);
  // The page as the server renders it when Admin:Turnstile:SiteKey is empty (the .NET tests pin that markup).
  await page.route((url) => url.pathname === SIGN_IN, async (route) => {
    const response = await route.fetch();
    const html = (await response.text()).replace(/ data-sitekey="[^"]*"/, '');
    await route.fulfill({ response, body: html });
  });
  await openSignIn(page);
  expect(await page.locator('#security-check').getAttribute('data-sitekey')).toBeNull();
  await expect(page.locator('#security-check-status')).toHaveText(CHECK_NOT_CONFIGURED);
  await expect(page.locator('#security-check')).toHaveAttribute('data-state', 'unconfigured');
  await expect(page.locator('#security-check-retry')).toBeHidden();
  await expect(continueButton(page)).toBeDisabled();

  await fillSignIn(page, owner.email, owner.password);
  await page.locator('#password').press('Enter');
  await continueButton(page).click({ force: true });
  await page.evaluate(() => (document.getElementById('sign-in-form') as HTMLFormElement).requestSubmit());
  await expect(continueButton(page)).toBeDisabled();
  expect(seen.signIns).toHaveLength(0);
  expect(seen.cloudflare).toEqual([]);
});

test('nothing on S-A1 stands where Cloudflare installs window.turnstile, or any other global the scripts read', async ({ page }) => {
  let arrive: (() => void) | undefined;
  const onItsWay = new Promise<void>((resolve) => {
    arrive = resolve;
  });
  await standInForCloudflare(page, { script: () => onItsWay });
  // The pending script holds the load event back, so do not wait for it.
  await page.goto(SIGN_IN, { waitUntil: 'domcontentloaded' });
  await expect(page.locator('#security-check-status')).toHaveText(CHECK_LOADING);

  // The form is up and Cloudflare's script has not arrived: the name must still be free.
  const before = await page.evaluate((globals) => ({
    turnstile: typeof (window as any).turnstile,
    ids: Array.from(document.querySelectorAll('[id]'), (el) => el.id),
    names: Array.from(document.querySelectorAll('[name]'), (el) => el.getAttribute('name') ?? ''),
    elementsInTheirPlace: globals.filter((name) => (window as any)[name] instanceof Element),
  }), GLOBALS_THE_SCRIPTS_READ);
  expect(before.turnstile).toBe('undefined');
  expect(before.elementsInTheirPlace).toEqual([]);
  expect(before.ids).toContain('security-check');
  expect(before.ids).toContain('sign-in-form');
  const taken = GLOBALS_THE_SCRIPTS_READ.map((name) => name.toLowerCase());
  for (const name of [...before.ids, ...before.names]) {
    expect(taken, `id or name "${name}"`).not.toContain(name.toLowerCase());
  }

  arrive!();
  await expect(continueButton(page)).toBeEnabled();
  expect(await page.evaluate(() => ({
    installed: typeof (window as any).turnstile.render,
    refused: (window as any).__e2eWidgetRefused === true,
  }))).toEqual({ installed: 'function', refused: false });
});

test('throttled by the real server (429): the page shows the server sentence as sent, with no wait time', async ({ page }) => {
  await openSignIn(page);
  await expect(continueButton(page)).toBeEnabled();

  // Failed sign-ins from this address, each for a different unknown email, until the server throttles.
  const throttled = await page.evaluate(async ({ api, token }) => {
    const csrf = document.querySelector('meta[name="dw-csrf"]')!.getAttribute('content')!;
    for (let i = 1; i <= 40; i += 1) {
      const r = await fetch(api, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
        body: JSON.stringify({ email: `nobody-${i}@example.test`, password: 'not-the-password', turnstileToken: token }),
      });
      if (r.status === 429) return { after: i, body: await r.json(), retryAfter: r.headers.get('Retry-After') };
      if (r.status !== 401) return { after: i, body: `unexpected ${r.status}`, retryAfter: null };
    }
    return null;
  }, { api: API_SIGN_IN, token: PASS_TOKEN });
  expect(throttled).not.toBeNull();
  expect(throttled!.body).toEqual({ error: SIGN_IN_GENERIC });
  expect(throttled!.retryAfter).toBeNull();
  expect(throttled!.after).toBeLessThanOrEqual(21);

  // Now even the right details get the 429, and the page says only what the server said.
  await fillSignIn(page, owner.email, owner.password);
  const response = await continueAndWait(page);
  expect(response.status()).toBe(429);
  expect(response.headers()['retry-after']).toBeUndefined();
  expect(await response.json()).toEqual({ error: SIGN_IN_GENERIC });
  await expectAlert(page, SIGN_IN_GENERIC);
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await expect(page.locator('#email')).toHaveValue(owner.email);
  await expect(page.locator('#password')).toHaveValue('');
  await expect(continueButton(page)).toBeEnabled();
  const text = (await page.locator('body').innerText()).toLowerCase();
  for (const banned of ['locked', 'minute', 'second', 'attempt', 'try again in']) expect(text).not.toContain(banned);

  // While throttled the code page is not served either: the gate answers with the same sentence.
  const codePage = await page.request.get(CODE);
  expect(codePage.status()).toBe(429);
  expect(await codePage.json()).toEqual({ error: SIGN_IN_GENERIC });
});

test('a throttled reply on step 2 (429) is shown as sent, and the field is cleared', async ({ page }) => {
  await reachCodePage(page);
  const counts = await answerPosts(page, API_CODE, () => ({ status: 429, json: { error: SIGN_IN_GENERIC } }));
  const code = page.locator('#code');
  await code.fill('123456');
  await signInButton(page).click();
  await expectAlertAboveField(page, SIGN_IN_GENERIC, code);
  await expect(code).toHaveValue('');
  await expect(page).toHaveURL(ORIGIN + CODE);
  await expect(signInButton(page)).toBeEnabled();
  expect(counts.count).toBe(1);
});

test('an owner who has not enrolled an authenticator is sent to authenticator set-up, not to the code page', async ({ page }) => {
  owner = seedOwner({ enrolled: false });
  const seen = watch(page);
  await openSignIn(page);
  await fillSignIn(page, owner.email, owner.password);
  await expect(continueButton(page)).toBeEnabled();
  const navigation = page.waitForRequest((request) => request.isNavigationRequest() && request.url() === ORIGIN + ENROLL);
  await continueButton(page).click();
  await navigation;
  await expect(page).toHaveURL(ORIGIN + ENROLL);
  expect(seen.signIns).toHaveLength(1);
  expect(seen.codes).toHaveLength(0);
  expect(seen.urls).not.toContain(ORIGIN + CODE);
  await expectNotSignedIn(page);
});

test('a 200 from step 1 that names no known next step is not followed', async ({ page }) => {
  const replies: unknown[] = [
    {},
    { next: 'home' },
    { next: 'TOTP' },
    { next: ['totp'] },
    { next: 'https://evil.example/' },
    { next: '/admin/sign-in/code' },
    { error: 'second_factor' },
    { verified: true, next: '/admin/' },
  ];
  const counts = await answerPosts(page, API_SIGN_IN, (n) => ({ status: 200, json: replies[n - 1] }));
  await openSignIn(page);
  await fillSignIn(page, owner.email, owner.password);
  for (const [index, reply] of replies.entries()) {
    await expect(continueButton(page)).toBeEnabled();
    await continueButton(page).click();
    await expectAlert(page, RETRY);
    await expect(page, `reply ${JSON.stringify(reply)}`).toHaveURL(ORIGIN + SIGN_IN);
    // Nothing was refused, so nothing typed is thrown away.
    await expect(page.locator('#email')).toHaveValue(owner.email);
    await expect(page.locator('#password')).toHaveValue(owner.password);
    expect(counts.count).toBe(index + 1);
  }
});

test('S-A1 with no clear answer (network failure, server error, a refusal it does not know, a redirect, time-out): the retry sentence, and the form stays', async ({ page }) => {
  await page.clock.install();
  const counts = await answerPosts(page, API_SIGN_IN, (n) => {
    if (n === 1) return 'abort';
    if (n === 2) return { status: 500, text: 'Internal Server Error' };
    if (n === 3) return { status: 403, json: { error: 'Forbidden' } };
    if (n === 4) return { status: 401, text: 'Unauthorized' };
    if (n === 5) return { status: 401, json: { error: 42 } };
    if (n === 6) return { status: 302, text: '', headers: { location: '/admin/sign-in/code' } };
    return 'never';
  });
  await openSignIn(page);
  await fillSignIn(page, owner.email, owner.password);

  for (const attempt of [1, 2, 3, 4, 5, 6, 7]) {
    await expect(continueButton(page)).toBeEnabled();
    await continueButton(page).click();
    if (attempt === 7) {
      await expect(page.locator('#sign-in-form')).toHaveAttribute('aria-busy', 'true');
      await page.clock.fastForward(15000);
    }
    await expectAlert(page, RETRY);
    await expect(page.locator('#form-alert')).not.toHaveText(SIGN_IN_GENERIC);
    await expect(page.locator('#sign-in-form')).not.toHaveAttribute('aria-busy');
    await expect(page.locator('#email')).toHaveValue(owner.email);
    await expect(page.locator('#password')).toHaveValue(owner.password);
    expect(counts.count).toBe(attempt);
  }
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
});

test('S-A1: a second Continue while one is in flight sends nothing', async ({ page }) => {
  const seen = watch(page);
  let release: (() => void) | undefined;
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  await answerPosts(page, API_SIGN_IN, async () => {
    await gate;
    return { status: 401, json: { error: SIGN_IN_GENERIC } };
  });
  await openSignIn(page);
  await fillSignIn(page, owner.email, owner.password);
  const form = page.locator('#sign-in-form');
  await continueButton(page).click();
  await expect(form).toHaveAttribute('aria-busy', 'true');
  await expect(continueButton(page)).toBeDisabled();

  await continueButton(page).click({ force: true });
  await page.locator('#password').press('Enter');
  await page.evaluate(() => (document.getElementById('sign-in-form') as HTMLFormElement).requestSubmit());
  expect(seen.signIns).toHaveLength(1);

  release!();
  await expectAlert(page, SIGN_IN_GENERIC);
  await expect(form).not.toHaveAttribute('aria-busy');
  expect(seen.signIns).toHaveLength(1);
});

test('S-A2: one numeric one-time-code field, no security check, and a tighter policy than S-A1', async ({ page }) => {
  await reachCodePage(page);
  const seen = watch(page);
  const response = await page.goto(CODE);
  expect(response!.status()).toBe(200);
  const html = await response!.text();
  const nonce = html.match(/<link rel="stylesheet" href="\/admin\/auth\/signed-out\.css" nonce="([^"]+)">/)![1];
  expect(response!.headers()['content-security-policy']).toBe(
    `default-src 'none'; script-src 'nonce-${nonce}'; style-src 'nonce-${nonce}'; connect-src 'self'; `
    + 'frame-ancestors \'none\'; base-uri \'none\'; form-action \'self\'; object-src \'none\'');
  expect(response!.headers()['cache-control']).toBe('no-store');
  expect(response!.headers()['referrer-policy']).toBe('no-referrer');

  const code = page.locator('#code');
  await expect(page.locator('#second-step-form')).toBeVisible();
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(code).toBeFocused();
  await expect(code).toHaveAttribute('type', 'text');
  await expect(code).toHaveAttribute('inputmode', 'numeric');
  await expect(code).toHaveAttribute('autocomplete', 'one-time-code');
  await expect(code).toHaveAttribute('autocapitalize', 'none');
  await expect(code).toHaveAttribute('spellcheck', 'false');
  await expect(code).toHaveAttribute('aria-describedby', 'code-error');
  await expect(page.getByRole('textbox', { name: 'Authenticator code', exact: true })).toHaveAttribute('id', 'code');
  await expect(page.locator('input')).toHaveCount(1);
  await expect(signInButton(page)).toBeEnabled();
  await expect(page.locator('#security-check')).toHaveCount(0);
  await expect(page.locator('iframe')).toHaveCount(0);
  await expect(page.locator('nav')).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Use a recovery code instead', exact: true })).toHaveAttribute('href', RECOVERY);
  await expect(page.getByRole('link', { name: 'Back to sign in', exact: true })).toHaveAttribute('href', SIGN_IN);
  expect(await code.evaluate((el) => el.dispatchEvent(
    new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData: new DataTransfer() })))).toBe(true);

  // Nothing on this step asks Cloudflare for anything.
  expect(seen.cloudflare).toEqual([]);
  expect(await page.evaluate(() => typeof (window as any).turnstile)).toBe('undefined');

  // The pending sign-in is a cookie no script can read. It is not in the address, the page or any storage.
  const pending = (await page.context().cookies()).find((c) => c.name === 'dw_admin_pending');
  expect(pending, 'the pending sign-in cookie').toBeDefined();
  expect(pending!.httpOnly).toBe(true);
  expect(pending!.secure).toBe(true);
  expect(pending!.sameSite).toBe('Strict');
  expect(pending!.path).toBe('/admin');
  expect(page.url()).toBe(ORIGIN + CODE);
  expect(await page.content()).not.toContain(pending!.value);
  expect(await page.evaluate(() => ({
    local: localStorage.length, session: sessionStorage.length, cookie: document.cookie,
  }))).toEqual({ local: 0, session: 0, cookie: '' });
  for (const url of seen.urls) expect(url).not.toContain(pending!.value);
  expect(seen.referers).toEqual([]);
});

test('S-A2 never sends by itself, and a code that is not six digits is caught in the browser', async ({ page }) => {
  await reachCodePage(page);
  const seen = watch(page);
  const code = page.locator('#code');

  // A complete code sits there until the owner sends it.
  await code.pressSequentially('123456');
  await page.waitForTimeout(600);
  expect(seen.codes).toHaveLength(0);
  await expect(page).toHaveURL(ORIGIN + CODE);

  for (const typed of ['', '12345', '1234567', '12a456', 'abcdef']) {
    await code.fill(typed);
    await signInButton(page).click();
    await expect(page.locator('#code-error'), `"${typed}"`).toHaveText(CODE_REQUIRED);
    await expect(code).toHaveAttribute('aria-invalid', 'true');
    await expect(code).toBeFocused();
    await expect(page.locator('#form-alert')).toBeHidden();
  }
  // Typing clears the message.
  await code.fill('1');
  await expect(page.locator('#code-error')).toHaveText('');
  await expect(code).not.toHaveAttribute('aria-invalid');
  expect(seen.codes).toHaveLength(0);
});

test('wrong code: the same server sentence, the field cleared with the cursor in it; then the right code, typed with a space, signs in', async ({ page }) => {
  await reachCodePage(page);
  const seen = watch(page);
  const code = page.locator('#code');

  await code.fill(wrongAuthenticatorCode(owner.secret!));
  const refusal = await sendCodeAndWait(page, API_CODE);
  expect(refusal.status()).toBe(401);
  expect(refusal.headers()['retry-after']).toBeUndefined();
  const body = await refusal.json();
  // Word for word what step 1 says: nothing names the code.
  expect(body).toEqual({ error: SIGN_IN_GENERIC });
  await expectAlertAboveField(page, body.error, code);
  await expect(code).toHaveValue('');
  await expect(code).not.toHaveAttribute('aria-invalid');
  await expect(page.locator('#code-error')).toHaveText('');
  await expect(page).toHaveURL(ORIGIN + CODE);
  await expect(page.locator('#second-step-form')).not.toHaveAttribute('aria-busy');
  await expect(signInButton(page)).toBeEnabled();
  await expectNotSignedIn(page);

  // Many apps show the code as "123 456". The space is dropped before it is sent.
  const right = authenticatorCode(owner.secret!);
  await code.fill(`${right.slice(0, 3)} ${right.slice(3)}`);
  await signInButton(page).click();
  await expectSignedIn(page, HOME);
  expect(seen.codes).toHaveLength(2);
  expect(seen.codes[1].postDataJSON()).toEqual({ code: right });
});

test('recovery-code sign-in: from the code page and back, a code typed in lower case with spaces signs in, and the same code does not work twice', async ({ page }) => {
  await reachCodePage(page);
  const seen = watch(page);

  // To the recovery page and back to the code page, then to the recovery page for good.
  await page.getByRole('link', { name: 'Use a recovery code instead', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY);
  await expect(page).toHaveTitle(RECOVERY_TITLE);
  await expect(page.locator('h1')).toHaveText(RECOVERY_HEADING);
  await page.getByRole('link', { name: 'Use an authenticator code instead', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + CODE);
  await page.getByRole('link', { name: 'Use a recovery code instead', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY);

  const field = page.locator('#recovery-code');
  await expect(field).toBeFocused();
  await expect(field).toHaveAttribute('type', 'text');
  await expect(field).toHaveAttribute('autocomplete', 'one-time-code');
  await expect(field).not.toHaveAttribute('inputmode');
  await expect(field).toHaveAttribute('autocapitalize', 'none');
  await expect(field).toHaveAttribute('spellcheck', 'false');
  await expect(page.getByRole('textbox', { name: 'Recovery code', exact: true })).toHaveAttribute('id', 'recovery-code');
  await expect(page.locator('input')).toHaveCount(1);
  await expect(page.getByRole('link', { name: 'Back to sign in', exact: true })).toHaveAttribute('href', SIGN_IN);
  await shot(page, 'sign-in_recovery');

  // Empty: caught in the browser.
  await signInButton(page).click();
  await expect(page.locator('#recovery-code-error')).toHaveText(RECOVERY_REQUIRED);
  await expect(field).toHaveAttribute('aria-invalid', 'true');
  expect(seen.recoveries).toHaveLength(0);

  // A code that was never issued: the same sentence as everywhere else.
  await field.fill('AAAA-BBBB-CCCC');
  const refusal = await sendCodeAndWait(page, API_RECOVERY);
  expect(refusal.status()).toBe(401);
  expect(await refusal.json()).toEqual({ error: SIGN_IN_GENERIC });
  await expectAlertAboveField(page, SIGN_IN_GENERIC, field);
  await expect(field).toHaveValue('');
  await expect(page).toHaveURL(ORIGIN + RECOVERY);

  // A real one, typed the way a person might: lower case, spaces for dashes, a space at each end.
  const real = owner.recoveryCodes[3];
  const typed = ` ${real.toLowerCase().replace(/-/g, ' ')} `;
  await field.fill(typed);
  await signInButton(page).click();
  await expectSignedIn(page, HOME);
  expect(seen.recoveries).toHaveLength(2);
  expect(seen.recoveries[1].postDataJSON()).toEqual({ recoveryCode: typed.trim() });

  // The code is spent: a second sign-in with it is refused like any wrong code.
  await page.context().clearCookies();
  await page.goto(SIGN_IN);
  await fillSignIn(page, owner.email, owner.password);
  await expect(continueButton(page)).toBeEnabled();
  await continueButton(page).click();
  await expect(page).toHaveURL(ORIGIN + CODE);
  await page.getByRole('link', { name: 'Use a recovery code instead', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY);
  await page.locator('#recovery-code').fill(real);
  const second = await sendCodeAndWait(page, API_RECOVERY);
  expect(second.status()).toBe(401);
  expect(await second.json()).toEqual({ error: SIGN_IN_GENERIC });
  await expectAlertAboveField(page, SIGN_IN_GENERIC, page.locator('#recovery-code'));
  await expectNotSignedIn(page);

  // Another unused code still works.
  await page.locator('#recovery-code').fill(owner.recoveryCodes[7]);
  await signInButton(page).click();
  await expectSignedIn(page, HOME);
});

test('a direct visit to the code page or the recovery page without a pending sign-in: the server serves no page at all', async ({ page }) => {
  for (const path of [CODE, RECOVERY]) {
    const response = await page.goto(path);
    // The session gate answers before the page handler runs. No form, no script, nothing to use.
    expect(response!.status(), path).toBe(401);
    expect(response!.headers()['content-type']).toContain('application/json');
    expect(await response!.text()).toBe(`{"error":"${GATE_DENIED}"}`);
    await expect(page.locator('form')).toHaveCount(0);
    await expect(page.locator('input')).toHaveCount(0);
    await expect(page.locator('h1')).toHaveCount(0);
    expect(response!.headers()['location']).toBeUndefined();
  }
  expect(await page.context().cookies()).toEqual([]);
});

test('when the pending sign-in is gone by the time the code is sent, the page goes back to step 1', async ({ page }) => {
  for (const step of ['code', 'recovery'] as const) {
    owner = seedOwner({ enrolled: true });
    await page.context().clearCookies();
    if (step === 'code') await reachCodePage(page);
    else await reachRecoveryPage(page);
    const api = step === 'code' ? API_CODE : API_RECOVERY;

    // The page is open; then the pending sign-in runs out.
    await page.context().clearCookies({ name: 'dw_admin_pending' });
    await page.locator(step === 'code' ? '#code' : '#recovery-code')
      .fill(step === 'code' ? authenticatorCode(owner.secret!) : owner.recoveryCodes[0]);
    const [refusal] = await Promise.all([
      page.waitForResponse(isReplyTo(api)),
      page.waitForRequest((request) => request.isNavigationRequest() && request.url() === ORIGIN + SIGN_IN),
      signInButton(page).click(),
    ]);
    // The gate's answer, not the sign-in sentence: there is nothing left to try a code against.
    expect(refusal.status(), step).toBe(401);

    await expect(page).toHaveURL(ORIGIN + SIGN_IN);
    await expect(page.locator('#sign-in-form')).toBeVisible();
    await expect(page.locator('h1')).toHaveText(SIGN_IN_HEADING);
    // Step 1 starts over, with a new security check.
    await expect(page.locator('#email')).toHaveValue('');
    await expect(page.locator('#password')).toHaveValue('');
    await expect(continueButton(page)).toBeEnabled();
    await expectNotSignedIn(page);
  }
});

test('the gate\'s word is what sends the page back: a 401 with that word goes to step 1, any other 401 text is shown', async ({ page }) => {
  await reachCodePage(page);
  let reply: Answer = { status: 401, json: { error: 'Some other refusal.' } };
  await answerPosts(page, API_CODE, () => reply);
  const code = page.locator('#code');

  await code.fill('123456');
  await signInButton(page).click();
  await expectAlertAboveField(page, 'Some other refusal.', code);
  await expect(page).toHaveURL(ORIGIN + CODE);

  reply = { status: 403, json: { error: GATE_DENIED } };
  await code.fill('123456');
  await signInButton(page).click();
  await expectAlertAboveField(page, RETRY, code);
  await expect(page).toHaveURL(ORIGIN + CODE);

  reply = { status: 401, json: { error: GATE_DENIED } };
  await code.fill('123456');
  await signInButton(page).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
});

test('whatever the server sends as its sentence is shown as text, never read as markup', async ({ page }) => {
  const sentence = '<b>We</b> couldn\'t <img src="x" onerror="window.__e2eMarkupRan = true"> sign you in & <script>window.__e2eMarkupRan = true</script>';
  await answerPosts(page, API_SIGN_IN, (n) => (n === 1 ? { status: 401, json: { error: sentence } } : 'real'));
  await openSignIn(page);
  await fillSignIn(page, owner.email, owner.password);
  await expect(continueButton(page)).toBeEnabled();
  await continueButton(page).click();
  await expectAlert(page, sentence);
  await expect(page.locator('#form-alert *')).toHaveCount(0);

  await fillSignIn(page, owner.email, owner.password);
  await expect(continueButton(page)).toBeEnabled();
  await continueButton(page).click();
  await expect(page).toHaveURL(ORIGIN + CODE);
  await answerPosts(page, API_CODE, () => ({ status: 429, json: { error: sentence } }));
  await page.locator('#code').fill('123456');
  await signInButton(page).click();
  await expectAlertAboveField(page, sentence, page.locator('#code'));
  await expect(page.locator('#form-alert *')).toHaveCount(0);
  expect(await page.evaluate(() => (window as any).__e2eMarkupRan)).toBeUndefined();
});

test('a 200 from step 2 that does not say verified is not followed, and no other unclear answer is either: the page asks whether it is signed in, and it is not', async ({ page }) => {
  await page.clock.install();
  await reachCodePage(page);
  const seen = watch(page);
  // Each reply, and whether the page then asks the server if this browser is signed in.
  const replies: Array<[Answer, 'asks' | 'does not ask']> = [
    [{ status: 200, json: { next: '/admin/participants' } }, 'asks'],
    [{ status: 200, json: { verified: 'true', next: '/admin/participants' } }, 'asks'],
    [{ status: 200, json: { verified: 1, next: '/admin/participants' } }, 'asks'],
    [{ status: 200, json: { error: 'ok' } }, 'asks'],
    [{ status: 200, text: '' }, 'asks'],
    [{ status: 204, text: '' }, 'asks'],
    [{ status: 500, text: 'Internal Server Error' }, 'asks'],
    [{ status: 302, text: '', headers: { location: '/admin/' } }, 'asks'],
    ['abort', 'asks'],
    ['never', 'asks'],
    // A refusal the page does not know is still a refusal: nothing was accepted, so there is nothing to ask.
    [{ status: 403, json: { error: 'Forbidden' } }, 'does not ask'],
    [{ status: 401, text: 'Unauthorized' }, 'does not ask'],
  ];
  const counts = await answerPosts(page, API_CODE, (n) => replies[n - 1][0]);
  const code = page.locator('#code');
  await code.fill('123456');

  let asked = 0;
  for (const [index, [reply, then]] of replies.entries()) {
    await expect(signInButton(page)).toBeEnabled();
    await signInButton(page).click();
    if (reply === 'never') {
      await expect(page.locator('#second-step-form')).toHaveAttribute('aria-busy', 'true');
      await expect(signInButton(page)).toBeDisabled();
      await page.clock.fastForward(15000);
    }
    await expectAlertAboveField(page, RETRY, code);
    await expect(page, `reply ${JSON.stringify(reply)}`).toHaveURL(ORIGIN + CODE);
    await expect(page.locator('#second-step-form')).not.toHaveAttribute('aria-busy');
    // Nothing was refused, so the code is still there to send again.
    await expect(code).toHaveValue('123456');
    expect(counts.count).toBe(index + 1);
    if (then === 'asks') asked += 1;
    expect(seen.signedInChecks, `reply ${JSON.stringify(reply)}`).toHaveLength(asked);
  }
  // The question itself carries nothing of the page's: a plain GET, no anti-forgery token, no body.
  expect(seen.signedInChecks[0].method()).toBe('GET');
  expect(seen.signedInChecks[0].postData()).toBeNull();
  expect((await seen.signedInChecks[0].allHeaders())['x-csrf-token']).toBeUndefined();
  await expectNotSignedIn(page);
});

test('no clear answer on step 2, but the code did go through: the page finds this browser signed in and goes to /admin/', async ({ page }) => {
  for (const lost of ['a 200 that does not say verified', 'a reply that never arrives whole'] as const) {
    owner = seedOwner({ enrolled: true });
    await page.context().clearCookies();
    await page.unrouteAll({ behavior: 'wait' });
    await reachCodePage(page);
    const seen = watch(page);
    // The real server takes the code and signs this browser in; only its reply does not reach the page.
    let accepted: number | undefined;
    await page.route((url) => url.pathname === API_CODE, async (route) => {
      accepted = (await route.fetch()).status();
      if (lost === 'a 200 that does not say verified') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ error: 'ok' }) });
      } else {
        await route.abort('connectionfailed');
      }
    });

    await page.locator('#code').fill(authenticatorCode(owner.secret!));
    await Promise.all([
      page.waitForRequest((request) => request.isNavigationRequest() && request.url() === ORIGIN + HOME),
      signInButton(page).click(),
    ]);
    expect(accepted, lost).toBe(200);
    await expectSignedIn(page, HOME);
    expect(seen.codes, lost).toHaveLength(1);
    expect(seen.signedInChecks, lost).toHaveLength(1);
  }
});

test('S-A2: a second Sign in while one is in flight sends nothing', async ({ page }) => {
  await reachCodePage(page);
  const seen = watch(page);
  let release: (() => void) | undefined;
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  await answerPosts(page, API_CODE, async () => {
    await gate;
    return { status: 401, json: { error: SIGN_IN_GENERIC } };
  });
  const form = page.locator('#second-step-form');
  await page.locator('#code').fill('123456');
  await signInButton(page).click();
  await expect(form).toHaveAttribute('aria-busy', 'true');
  await expect(signInButton(page)).toBeDisabled();

  await signInButton(page).click({ force: true });
  await page.locator('#code').press('Enter');
  await page.evaluate(() => (document.getElementById('second-step-form') as HTMLFormElement).requestSubmit());
  expect(seen.codes).toHaveLength(1);

  release!();
  await expectAlertAboveField(page, SIGN_IN_GENERIC, page.locator('#code'));
  await expect(form).not.toHaveAttribute('aria-busy');
  expect(seen.codes).toHaveLength(1);
});

test('"Back to sign in" leaves step 2 for step 1, where everything has to be entered again', async ({ page }) => {
  await reachCodePage(page);
  await page.getByRole('link', { name: 'Back to sign in', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await expect(page.locator('#email')).toHaveValue('');
  await expect(page.locator('#password')).toHaveValue('');
  await expect(page.locator('#email')).toBeFocused();
  // A new security check is asked for.
  await expect(continueButton(page)).toBeEnabled();
  expect((await widgetState(page)).renders).toBe(1);
  await expectNotSignedIn(page);
});

test('nothing in the address of S-A1 reaches the page or the request', async ({ page }) => {
  const seen = watch(page);
  await standInForCloudflare(page);
  const address = `${SIGN_IN}?returnPath=${encodeURIComponent('https://evil.example/')}`
    + `&next=${encodeURIComponent('//evil.example/')}&email=${encodeURIComponent('mallory@example.test')}`;
  await page.goto(address);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await expect(page.locator('#email')).toHaveValue('');
  expect(await page.content()).not.toContain('evil.example');
  expect(await page.content()).not.toContain('mallory');

  await fillSignIn(page, owner.email, owner.password);
  await expect(continueButton(page)).toBeEnabled();
  await continueButton(page).click();
  await expect(page).toHaveURL(ORIGIN + CODE);
  expect(seen.signIns[0].postDataJSON()).toEqual({
    email: owner.email, password: owner.password, turnstileToken: PASS_TOKEN,
  });

  await page.locator('#code').fill(authenticatorCode(owner.secret!));
  await signInButton(page).click();
  await expectSignedIn(page, HOME);
  for (const url of seen.urls) expect(new URL(url).hostname).not.toBe('evil.example');
});

test('usable at 320 px wide and at 200% zoom: no sideways scrolling on S-A1, S-A2 or S-A3, with their messages showing', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 640 });
  await answerPosts(page, API_SIGN_IN, (n) => (n === 1 ? { status: 429, json: { error: SIGN_IN_GENERIC } } : 'real'));
  await openSignIn(page);
  await expect(continueButton(page)).toBeEnabled();
  await expectNoHorizontalScroll(page);
  // The standard 300 px widget does not fit this column, so the compact one is asked for.
  expect((await widgetState(page)).size).toBe('compact');
  for (const selector of [
    'h1', '#email', '#password', '#password-toggle', '#security-check-widget iframe', '#sign-in-submit', '#reset-link',
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

  // With the alert showing, and then with both field messages, still no sideways scrolling.
  await fillSignIn(page, owner.email, owner.password);
  await continueButton(page).click();
  await expectAlert(page, SIGN_IN_GENERIC);
  await expectNoHorizontalScroll(page);
  await shot(page, 'sign-in_320_error');
  await page.locator('#email').fill('');
  await expect(continueButton(page)).toBeEnabled();
  await continueButton(page).click();
  await expect(page.locator('#email-error')).toHaveText(EMAIL_REQUIRED);
  await expect(page.locator('#password-error')).toHaveText(PASSWORD_REQUIRED);
  await expect(page.locator('#form-alert')).toBeHidden();
  await expectNoHorizontalScroll(page);

  // A 1280 px window zoomed to 200% lays out as 640 CSS px.
  await page.setViewportSize({ width: 640, height: 400 });
  await expectNoHorizontalScroll(page);
  await continueButton(page).scrollIntoViewIfNeeded();
  await expect(continueButton(page)).toBeInViewport();

  // On to step 2 at 320 px.
  await page.setViewportSize({ width: 320, height: 640 });
  await fillSignIn(page, owner.email, owner.password);
  await expect(continueButton(page)).toBeEnabled();
  await continueButton(page).click();
  await expect(page).toHaveURL(ORIGIN + CODE);
  await answerPosts(page, API_CODE, () => ({ status: 401, json: { error: SIGN_IN_GENERIC } }));
  await page.locator('#code').fill('123456');
  await signInButton(page).click();
  await expectAlertAboveField(page, SIGN_IN_GENERIC, page.locator('#code'));
  await expectNoHorizontalScroll(page);
  for (const selector of ['h1', '#code', '#second-step-submit', '#other-step-link', '#back-link']) {
    const box = await page.locator(selector).boundingBox();
    expect(box, selector).not.toBeNull();
    expect(box!.x, selector).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width, selector).toBeLessThanOrEqual(320);
    if (selector !== 'h1') {
      expect(box!.width, selector).toBeGreaterThanOrEqual(24);
      expect(box!.height, selector).toBeGreaterThanOrEqual(24);
    }
  }
  await shot(page, 'sign-in_code_320_error');

  await page.locator('#other-step-link').click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY);
  await expect(page.locator('#second-step-form')).toBeVisible();
  await expectNoHorizontalScroll(page);
  await page.setViewportSize({ width: 640, height: 400 });
  await expectNoHorizontalScroll(page);
  await signInButton(page).scrollIntoViewIfNeeded();
  await expect(signInButton(page)).toBeInViewport();
});

test('axe finds nothing serious or critical on S-A1, S-A2 and S-A3, plain and in their error states', async ({ page }) => {
  await answerPosts(page, API_SIGN_IN, (n) => (n === 1 ? { status: 401, json: { error: SIGN_IN_GENERIC } } : 'real'));
  await openSignIn(page);
  await expect(continueButton(page)).toBeEnabled();
  await expectNoSeriousAxeFindings(page, 'S-A1-form');

  await continueButton(page).click();
  await expect(page.locator('#password-error')).toHaveText(PASSWORD_REQUIRED);
  await expectNoSeriousAxeFindings(page, 'S-A1-field-errors');

  // The toggle pressed, so the pressed colours are scanned too.
  await page.locator('#password-toggle').click();
  await fillSignIn(page, owner.email, owner.password);
  await continueButton(page).click();
  await expectAlert(page, SIGN_IN_GENERIC);
  await expectNoSeriousAxeFindings(page, 'S-A1-alert');

  await widgetSays(page, 'error-callback');
  await expect(page.locator('#security-check-retry')).toBeVisible();
  await expectNoSeriousAxeFindings(page, 'S-A1-security-check-failed');
  await page.locator('#security-check-retry').click();

  await fillSignIn(page, owner.email, owner.password);
  await expect(continueButton(page)).toBeEnabled();
  await continueButton(page).click();
  await expect(page).toHaveURL(ORIGIN + CODE);
  await expect(page.locator('#second-step-form')).toBeVisible();
  await expectNoSeriousAxeFindings(page, 'S-A2-form');

  await signInButton(page).click();
  await expect(page.locator('#code-error')).toHaveText(CODE_REQUIRED);
  await expectNoSeriousAxeFindings(page, 'S-A2-field-error');

  await answerPosts(page, API_CODE, () => ({ status: 401, json: { error: SIGN_IN_GENERIC } }));
  await page.locator('#code').fill('123456');
  await signInButton(page).click();
  await expectAlertAboveField(page, SIGN_IN_GENERIC, page.locator('#code'));
  await expectNoSeriousAxeFindings(page, 'S-A2-alert');

  await page.locator('#other-step-link').click();
  await expect(page).toHaveURL(ORIGIN + RECOVERY);
  await expect(page.locator('#second-step-form')).toBeVisible();
  await expectNoSeriousAxeFindings(page, 'S-A3-form');

  await signInButton(page).click();
  await expect(page.locator('#recovery-code-error')).toHaveText(RECOVERY_REQUIRED);
  await expectNoSeriousAxeFindings(page, 'S-A3-field-error');

  await answerPosts(page, API_RECOVERY, () => ({ status: 401, json: { error: SIGN_IN_GENERIC } }));
  await page.locator('#recovery-code').fill('AAAA-BBBB-CCCC');
  await signInButton(page).click();
  await expectAlertAboveField(page, SIGN_IN_GENERIC, page.locator('#recovery-code'));
  await expectNoSeriousAxeFindings(page, 'S-A3-alert');
});

test.describe('JavaScript off', () => {
  test.use({ javaScriptEnabled: false });

  test('with JavaScript off each of the three pages shows its heading, one sentence and no form', async ({ page }) => {
    const seen = watch(page);
    await page.goto(SIGN_IN);
    await expect(page.locator('h1')).toHaveText(SIGN_IN_HEADING);
    await expect(page.getByText(NOSCRIPT, { exact: true })).toBeVisible();
    await expect(page.locator('#sign-in-panel')).toBeHidden();
    await expect(page.locator('#sign-in-form')).toBeHidden();
    await expect(page.locator('#email')).toBeHidden();
    await expect(continueButton(page)).toHaveCount(0);
    await expect(page.getByRole('link')).toHaveCount(0);
    let lines = (await page.locator('body').innerText()).split('\n').map((line) => line.trim()).filter(Boolean);
    expect(lines).toEqual([SIGN_IN_HEADING, NOSCRIPT]);
    await shot(page, 'sign-in_js_off');

    // Step 1 through the API, as a script-less browser cannot do it: only to get a pending sign-in.
    const html = await (await page.request.get(SIGN_IN)).text();
    const csrf = html.match(/<meta name="dw-csrf" content="([0-9A-F]{64})">/)![1];
    const step1 = await page.request.post(API_SIGN_IN, {
      headers: { 'X-CSRF-TOKEN': csrf, Origin: ORIGIN },
      data: { email: owner.email, password: owner.password, turnstileToken: PASS_TOKEN },
    });
    expect(step1.status()).toBe(200);
    expect(await step1.json()).toEqual({ next: 'totp' });

    for (const [path, heading] of [[CODE, CODE_HEADING], [RECOVERY, RECOVERY_HEADING]]) {
      const response = await page.goto(path);
      expect(response!.status()).toBe(200);
      await expect(page.locator('h1')).toHaveText(heading);
      await expect(page.getByText(NOSCRIPT, { exact: true })).toBeVisible();
      await expect(page.locator('#second-step-panel')).toBeHidden();
      await expect(page.locator('input')).toBeHidden();
      await expect(signInButton(page)).toHaveCount(0);
      await expect(page.getByRole('link')).toHaveCount(0);
      lines = (await page.locator('body').innerText()).split('\n').map((line) => line.trim()).filter(Boolean);
      expect(lines).toEqual([heading, NOSCRIPT]);
    }
    expect(seen.cloudflare).toEqual([]);
    expect(seen.codes).toHaveLength(0);
  });
});
