import { test, expect, type Locator, type Page, type Request, type Response } from '@playwright/test';
import { expectNoSeriousAxeFindings } from './axe';
import { authenticatorCode, seedOwner, type Owner } from '../sign-in/owner';
import { answerCloudflare, type WidgetSetup } from '../signed-out/cloudflare';

/**
 * The signed-in console shell against the real app (T04 piece 1): S-B1 Stats
 * at /admin/, the nav and the pages it leads to, S-D3, sign-out, and the
 * session gate's way from a signed-in page to sign-in and back.
 * Start it with e2e/console/run.sh.
 *
 * Every sign-in goes through the real pages (S-A1, then S-A2), as an owner
 * would: the browser never reaches Cloudflare (e2e/signed-out/cloudflare.ts
 * answers for it), and the server checks the widget's token with the
 * runner's stand-in verifier.
 *
 * Before every test the owner is seeded afresh with a password and an
 * enrolled authenticator (e2e/sign-in/owner.ts), which also ends every
 * session and every sign-in in progress. A test that needs a session to end
 * half-way seeds again: the server then has no session for the browser's
 * cookie, exactly as after the 30-minute idle or 8-hour limit.
 *
 * What the server answers to each session state, cookie and method is proven
 * by the .NET tests (AdminConsoleShellTests). This file proves what only a
 * browser can: that the pages work, in three engines, under their policy.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const HOME = '/admin/';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const API_SIGN_IN = '/admin/api/auth/sign-in';
const API_CODE = '/admin/api/auth/sign-in/code';
const SIGN_OUT = '/admin/sign-out';
const API_ME = '/admin/api/me';

const NAV = [
  { path: '/admin/', label: 'Stats', title: 'Stats · Dealoware admin', heading: 'Stats' },
  { path: '/admin/participants', label: 'Participants', title: 'Participants · Dealoware admin', heading: 'Participants' },
  { path: '/admin/artifacts', label: 'Artifacts', title: 'Artifacts · Dealoware admin', heading: 'Artifacts' },
  { path: '/admin/negotiations', label: 'Negotiations', title: 'Negotiations · Dealoware admin', heading: 'Negotiations' },
  { path: '/admin/offers', label: 'Offers', title: 'Offers · Dealoware admin', heading: 'Offers' },
  {
    path: '/admin/settings/security', label: 'Security settings',
    title: 'Security settings · Dealoware admin', heading: 'Security settings',
  },
];
const STATS = ['Participants', 'Open negotiations', 'Offers', 'Accepts', 'Declines'];
const NOT_BUILT = 'This page is not built yet.';
const NOT_FOUND_TITLE = 'Page not found · Dealoware admin';
const NOT_FOUND = 'We can\'t find that page.';
const SKIP = 'Skip to main content';
const SESSION_ENDED = 'Your session has ended. Sign in again to continue.';
const SIGNED_OUT = 'You\'ve been signed out.';
const SIGN_IN_TITLE = 'Sign in · Dealoware admin';
const RETRY_SENTENCE = 'That didn\'t go through. Wait a moment and try again.';

const PASS_TOKEN = process.env.CONSOLE_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.CONSOLE_E2E_ARTIFACTS;

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

function standInForCloudflare(page: Page, setup: WidgetSetup = {}): Promise<{ scripts: number }> {
  return answerCloudflare(page, { token: PASS_TOKEN, ...setup });
}

function isPostTo(apiPath: string): (request: Request) => boolean {
  return (request) => new URL(request.url()).pathname === apiPath && request.method() === 'POST';
}

/** Both sign-in steps through the real pages, from the sign-in page the browser is on (or opens). */
async function signInHere(page: Page): Promise<void> {
  if (new URL(page.url()).pathname !== SIGN_IN) await page.goto(SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await page.locator('#email').fill(owner.email);
  await page.locator('#password').fill(owner.password);
  const continueButton = page.getByRole('button', { name: 'Continue', exact: true });
  await expect(continueButton).toBeEnabled();
  await continueButton.click();
  // The password check is deliberately slow, and slowest on the run's first sign-in.
  await expect(page).toHaveURL(ORIGIN + CODE, { timeout: 15_000 });
  await page.locator('#code').fill(authenticatorCode(owner.secret!));
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
}

/** Signed in, on Stats, with its scripts loaded. */
async function signIn(page: Page): Promise<void> {
  await signInHere(page);
  await expect(page).toHaveURL(ORIGIN + HOME);
  await expect(page.locator('h1')).toHaveText('Stats');
  await page.waitForLoadState('load');
}

async function expectSignedOut(page: Page): Promise<void> {
  const cookies = await page.context().cookies();
  expect(cookies.find((c) => c.name === 'dw_admin_session')).toBeUndefined();
  expect((await page.request.get(API_ME)).status()).toBe(401);
}

/** On S-A1, opened with one of its arrival sentences in the status line. */
async function expectSignInWith(page: Page, sentence: string): Promise<void> {
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page).toHaveTitle(SIGN_IN_TITLE);
  const status = page.locator('#form-status');
  await expect(status).toBeVisible();
  await expect(status).toHaveText(sentence);
  await expect(page.locator('.console-nav')).toHaveCount(0);
  await expect(page.locator('#stat-participants')).toHaveCount(0);
}

async function expectNoHorizontalScroll(page: Page): Promise<void> {
  const size = await page.evaluate(() => ({
    content: document.documentElement.scrollWidth,
    viewport: document.documentElement.clientWidth,
  }));
  expect(size.content).toBeLessThanOrEqual(size.viewport);
}

/** Where the keyboard focus is: the element's id, its link target, or its tag name. */
async function focusStop(page: Page): Promise<string> {
  return await page.evaluate(() => {
    const el = document.activeElement;
    if (!el) return '';
    if (el.id) return el.id;
    const href = el.getAttribute('href');
    return href ? `link:${href}` : el.tagName.toLowerCase();
  });
}

/** The shell every signed-in page shares, with this page's title, heading and current nav entry. */
async function expectShell(page: Page, title: string, heading: string, current: string | null): Promise<void> {
  await expect(page).toHaveTitle(title);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText(heading);
  const nav = page.getByRole('navigation', { name: 'Admin' });
  await expect(nav.getByRole('link')).toHaveText(NAV.map((n) => n.label));
  await expect(nav.getByRole('link', { name: 'Audit log' })).toHaveCount(0);
  await expect(nav.getByRole('button', { name: 'Sign out', exact: true })).toBeVisible();
  const marked = nav.locator('[aria-current="page"]');
  if (current === null) {
    await expect(marked).toHaveCount(0);
  } else {
    await expect(marked).toHaveCount(1);
    await expect(marked).toHaveAttribute('href', current);
  }
  await expect(page.getByRole('link', { name: SKIP, exact: true })).toHaveAttribute('href', '#main');
  // One live region of the shell's own; a page's forms keep theirs inside their sections (S-A12).
  await expect(page.locator('main > [aria-live]')).toHaveCount(1);
  await expect(page.locator('#console-status')).toHaveAttribute('aria-live', 'polite');
  await expectStyled(page);
}

/**
 * The page has loaded and its stylesheet applies: a nav link has the shell's own
 * minimum height (2.5rem). An axe scan, or a size check, before that would judge
 * the page unstyled.
 */
async function expectStyled(page: Page): Promise<void> {
  await page.waitForLoadState('load');
  const link = page.getByRole('navigation', { name: 'Admin' }).getByRole('link').first();
  await expect.poll(() => link.evaluate((el) => getComputedStyle(el).minHeight)).toBe('40px');
}

const ringOf = (locator: Locator) => locator.evaluate((el) => {
  const style = getComputedStyle(el);
  return { style: style.outlineStyle, width: style.outlineWidth };
});

test.beforeEach(async ({ page }) => {
  owner = seedOwner({ enrolled: true });
  cspMessages = [];
  pageErrors = [];
  page.on('console', (message) => {
    const text = message.text();
    if (!/content[- ]security[- ]policy/i.test(text)) return;
    // Firefox asks for /favicon.ico by itself, and the pages' default-src 'none' refuses it.
    if (text.includes('/favicon.ico')) return;
    // Judged: the console pages and the sign-in pages. A bare 405 or a JSON reply is not a page.
    const path = new URL(page.url()).pathname;
    if (path === SIGN_OUT || path.startsWith('/admin/api/') || path.startsWith('/admin/assets/')) return;
    cspMessages.push(text);
  });
  page.on('pageerror', (error) => pageErrors.push(error.message));
  await standInForCloudflare(page);
});

test.afterEach(() => {
  // Nothing the pages load may be refused by their own policy, and no script may throw.
  expect(cspMessages).toEqual([]);
  expect(pageErrors).toEqual([]);
});

test('signing in with no page asked for lands on Stats: five totals, the nav, the skip link, one live region', async ({ page }) => {
  const response = page.waitForResponse((r) => new URL(r.url()).pathname === HOME && r.request().method() === 'GET');
  await signIn(page);
  const stats = await response;
  expect(stats.status()).toBe(200);
  const headers = await stats.allHeaders();
  expect(headers['cache-control']).toBe('no-store');
  expect(headers['referrer-policy']).toBe('no-referrer');
  expect(headers['content-security-policy']).toMatch(/^default-src 'none'; script-src 'nonce-[^']+'; style-src 'nonce-[^']+';/);

  await expectShell(page, 'Stats · Dealoware admin', 'Stats', HOME);
  // Five totals, each a label and a whole number, written by the server.
  const labels = page.locator('.stats dt');
  await expect(labels).toHaveText(STATS);
  const counts = await page.locator('.stats dd').allTextContents();
  expect(counts).toHaveLength(5);
  for (const count of counts) expect(count).toMatch(/^\d{1,3}(,\d{3})*$/);
  // And they are the read API's.
  const api = await (await page.request.get('/admin/api/stats')).json();
  expect(counts.map((c) => Number(c.replace(/,/g, '')))).toEqual([
    api.participants, api.openNegotiations, api.offers, api.accepts, api.declines,
  ]);
  const asOf = page.locator('#stats-as-of');
  await expect(asOf).toHaveText(/^As of [A-Z][a-z]{2} \d{1,2}, \d{4}, \d{1,2}:\d{2} (AM|PM) ET$/);
  await expect(asOf.locator('time')).toHaveAttribute('datetime', /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$/);

  // The live region is there and empty; nothing is stored in the browser.
  await expect(page.locator('#console-status')).toHaveText('');
  expect(await page.evaluate(() => localStorage.length + sessionStorage.length)).toBe(0);
  // The page's own files came only from this site, the signed-in ones from behind the gate.
  const scripts = await page.locator('script').evaluateAll((els) => els.map((e) => e.getAttribute('src')));
  expect(scripts).toEqual(['/admin/auth/admin-request.js', '/admin/assets/console.js']);

  await expectNoSeriousAxeFindings(page, 'stats');
  await shot(page, 'stats_desktop');
});

test('Stats at 320 px: nothing scrolls sideways, every nav entry and Sign out stay on screen', async ({ page }) => {
  await signIn(page);
  await page.setViewportSize({ width: 320, height: 800 });
  await expectStyled(page);
  await expectNoHorizontalScroll(page);
  const nav = page.getByRole('navigation', { name: 'Admin' });
  for (const entry of NAV) await expect(nav.getByRole('link', { name: entry.label, exact: true })).toBeInViewport();
  await expect(nav.getByRole('button', { name: 'Sign out', exact: true })).toBeInViewport();
  // Every target is at least 24 by 24 CSS px.
  for (const target of await nav.locator('a, button').all()) {
    const box = (await target.boundingBox())!;
    expect(box.width).toBeGreaterThanOrEqual(24);
    expect(box.height).toBeGreaterThanOrEqual(24);
  }
  await expectNoSeriousAxeFindings(page, 'stats_320');
  await shot(page, 'stats_320');
});

test('every nav entry opens its page in one step, with its title, one h1 and aria-current on it alone', async ({ page }) => {
  // Seven pages, each scanned by axe: more than the default 30 s in the slower engines.
  test.setTimeout(90_000);
  await signIn(page);
  for (const entry of NAV) {
    await page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: entry.label, exact: true }).click();
    await expect(page).toHaveURL(ORIGIN + entry.path);
    await expectShell(page, entry.title, entry.heading, entry.path);
    // S-A12 is built: e2e/console/security-settings.spec.ts.
    if (entry.path !== HOME && entry.path !== '/admin/settings/security') {
      await expect(page.getByText(NOT_BUILT, { exact: true })).toBeVisible();
    }
    await expectNoSeriousAxeFindings(page, `page_${entry.label.toLowerCase().replace(/ /g, '-')}`);
  }

  // S-D3: an address no page answers. The nav stays, nothing is marked, and Stats is one link away.
  const missing = await page.goto('/admin/no-such-page');
  expect(missing!.status()).toBe(404);
  await expectShell(page, NOT_FOUND_TITLE, NOT_FOUND, null);
  await expectNoSeriousAxeFindings(page, 's-d3');
  await page.getByRole('link', { name: 'Go to Stats', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + HOME);

  // /admin is sent to /admin/.
  await page.goto('/admin');
  await expect(page).toHaveURL(ORIGIN + HOME);
});

test('keyboard: the skip link is the first stop and moves the focus to main; the nav and Sign out follow in order', async ({ page }) => {
  await signIn(page);
  // Start from the top of a freshly loaded page.
  await page.goto(HOME);
  const skip = page.getByRole('link', { name: SKIP, exact: true });
  const ring = { style: 'solid', width: '2px' };
  if (tabStopsAtLinks()) {
    await page.keyboard.press('Tab');
    await expect(skip).toBeFocused();
    await expect(skip).toBeInViewport();
    expect(await ringOf(skip)).toEqual(ring);
    const order: string[] = [];
    for (let i = 0; i < NAV.length + 1; i += 1) {
      await page.keyboard.press('Tab');
      order.push(await focusStop(page));
    }
    expect(order).toEqual([...NAV.map((n) => `link:${n.path}`), 'sign-out']);
    expect(await ringOf(page.locator('#sign-out'))).toEqual(ring);
    // Back to the skip link, and through it.
    for (let i = 0; i < NAV.length + 1; i += 1) await page.keyboard.press('Shift+Tab');
    await expect(skip).toBeFocused();
  } else {
    // WebKit, like Safari as delivered, passes links by when Tab is pressed, and on Linux a
    // fresh page holds no focus for Tab to start from. So, as the sign-in suite does for its
    // reset link, each control is focused directly and must take the focus and show its ring:
    // the skip link, every nav link and Sign out.
    await skip.focus();
    await expect(skip).toBeFocused();
    await expect(skip).toBeInViewport();
    expect(await ringOf(skip)).toEqual(ring);
    for (const entry of NAV) {
      const link = page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: entry.label, exact: true });
      await link.focus();
      await expect(link).toBeFocused();
      expect(await ringOf(link)).toEqual(ring);
    }
    await page.locator('#sign-out').focus();
    await expect(page.locator('#sign-out')).toBeFocused();
    expect(await ringOf(page.locator('#sign-out'))).toEqual(ring);
    await skip.focus();
  }
  await page.keyboard.press('Enter');
  await expect(page.locator('main')).toBeFocused();
  expect(new URL(page.url()).pathname).toBe(HOME);

  // A nav link opens its page from the keyboard.
  const offers = page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Offers', exact: true });
  await offers.focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/offers');
  await page.waitForLoadState('load');

  // And Sign out works from the keyboard too.
  await page.locator('#sign-out').focus();
  await page.keyboard.press('Enter');
  await expectSignInWith(page, SIGNED_OUT);
});

test('Sign out sends one POST with the anti-forgery header, lands on sign-in with its sentence, and Back or reload shows no page', async ({ page }) => {
  await signIn(page);
  await page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Participants', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/participants');
  await page.waitForLoadState('load');

  const posts: Request[] = [];
  page.on('request', (r) => { if (isPostTo(SIGN_OUT)(r)) posts.push(r); });
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expectSignInWith(page, SIGNED_OUT);
  await expect(page.locator('#form-alert')).toBeHidden();
  expect(posts).toHaveLength(1);
  const sent = await posts[0].allHeaders();
  expect(sent['x-csrf-token']).toMatch(/^[0-9A-F]{64}$/);
  expect(sent['origin']).toBe(ORIGIN);
  expect(sent['referer']).toBeUndefined();
  expect(new URL(posts[0].url()).search).toBe('');
  await expectSignedOut(page);
  await expectNoSeriousAxeFindings(page, 'sign-in_after_sign-out');
  await shot(page, 'sign-in_after_sign-out');

  // Back leads to Stats, which is asked for anew and sends the browser to sign-in.
  await page.goBack();
  await expectSignInWith(page, SESSION_ENDED);
  await page.reload();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#stat-participants')).toHaveCount(0);

  // A POST without the page's token is refused, and nothing changes.
  const bare = await page.request.post(SIGN_OUT, { headers: { Origin: ORIGIN } });
  expect(bare.status()).toBe(403);
  await expectSignedOut(page);
});

test('opening /admin/sign-out by its address signs nobody out', async ({ page }) => {
  await signIn(page);
  const response = await page.goto(SIGN_OUT);
  expect(response!.status()).toBe(405);
  expect((await page.request.get(API_ME)).status()).toBe(200);
  await page.goto(HOME);
  await expectShell(page, 'Stats · Dealoware admin', 'Stats', HOME);
});

test('a signed-in page asked for without a session goes to sign-in with the session-ended sentence, and no console is ever drawn', async ({ page }) => {
  const documents: Response[] = [];
  page.on('response', (r) => { if (r.request().isNavigationRequest() && r.frame() === page.mainFrame()) documents.push(r); });
  const landed = await page.goto('/admin/participants');

  // One redirect, from the gate, to a fixed address; then the sign-in page.
  const first = landed!.request().redirectedFrom();
  expect(first).not.toBeNull();
  expect(new URL(first!.url()).pathname).toBe('/admin/participants');
  const redirect = await first!.response();
  expect(redirect!.status()).toBe(302);
  expect(redirect!.headers()['location']).toBe('/admin/sign-in#ended');
  await expectSignInWith(page, SESSION_ENDED);
  for (const doc of documents) {
    if (doc.status() !== 200) continue;
    expect(new URL(doc.url()).pathname).toBe(SIGN_IN);
    expect(await doc.text()).not.toContain('console-nav');
  }

  // The signed-in files are not handed out either.
  for (const file of ['/admin/assets/console.js', '/admin/assets/console.css']) {
    const reply = await page.request.get(file);
    expect(reply.status()).toBe(401);
    expect(await reply.text()).toBe('{"error":"Unauthorized"}');
  }
  // An API path keeps its 401, without a redirect.
  const api = await page.request.get('/admin/api/stats', { maxRedirects: 0 });
  expect(api.status()).toBe(401);

  await expectNoSeriousAxeFindings(page, 'sign-in_after_session-end');
  await shot(page, 'sign-in_after_session-end');

  // Signing in from here returns to the page that was asked for.
  await signInHere(page);
  await expect(page).toHaveURL(ORIGIN + '/admin/participants');
  await expectShell(page, 'Participants · Dealoware admin', 'Participants', '/admin/participants');
  const file = await page.request.get('/admin/assets/console.js');
  expect(file.status()).toBe(200);
});

test('a session that ends mid-way: the next page goes to sign-in, and signing in returns to the same list view with its checked query', async ({ page }) => {
  await signIn(page);
  await page.goto('/admin/offers?status=open&sort=amount&bogus=1');
  await expectShell(page, 'Offers · Dealoware admin', 'Offers', '/admin/offers');

  // The server ends every session (as the idle or 8-hour limit would); the browser still holds its cookie.
  owner = seedOwner({ enrolled: true });
  expect((await page.context().cookies()).find((c) => c.name === 'dw_admin_session')).toBeDefined();

  const steps: Request[] = [];
  page.on('request', (r) => { if (isPostTo(API_SIGN_IN)(r) || isPostTo(API_CODE)(r)) steps.push(r); });
  await page.reload();
  await expectSignInWith(page, SESSION_ENDED);
  // The page asked for is not in the sign-in address, nor in any step's address or body.
  expect(new URL(page.url()).search).toBe('');
  await signInHere(page);
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?sort=amount&status=Open');
  await expectShell(page, 'Offers · Dealoware admin', 'Offers', '/admin/offers');
  expect(steps).toHaveLength(2);
  for (const step of steps) {
    expect(new URL(step.url()).search).toBe('');
    expect(step.postData() ?? '').not.toContain('offers');
  }
  // The kept page is gone once used: the next sign-in lands on Stats.
  expect((await page.context().cookies()).find((c) => c.name === 'dw_admin_return')).toBeUndefined();

  // And by a nav link: the session ends again, the owner clicks Negotiations, signs in, and is there.
  owner = seedOwner({ enrolled: true });
  await page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Negotiations', exact: true }).click();
  await expectSignInWith(page, SESSION_ENDED);
  await signInHere(page);
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations');
});

test('a page no sign-in returns to (an unknown address) lands on Stats after sign-in', async ({ page }) => {
  await page.goto('/admin/offers');
  await expectSignInWith(page, SESSION_ENDED);
  // A later visit to an address that is not on the list replaces the kept page with none.
  await page.goto('/admin/no-such-page');
  await expectSignInWith(page, SESSION_ENDED);
  await signIn(page);
});

test('two tabs: a page opened in the other tab does not spoil this tab\'s Sign out', async ({ page }) => {
  await signIn(page);
  const other = await page.context().newPage();
  await standInForCloudflare(other);
  await other.goto('/admin/offers');
  await expect(other.locator('h1')).toHaveText('Offers');
  await page.bringToFront();
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expectSignInWith(page, SIGNED_OUT);
  await other.close();
});

test('signed out in the other tab: Sign out here lands on sign-in with the session-ended sentence', async ({ page }) => {
  await signIn(page);
  const other = await page.context().newPage();
  await standInForCloudflare(other);
  await other.goto(HOME);
  await other.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expect(other).toHaveURL(ORIGIN + SIGN_IN);
  await other.close();

  await page.bringToFront();
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expectSignInWith(page, SESSION_ENDED);
  await expectSignedOut(page);
});

test('no answer to Sign out, while still signed in: the retry sentence in the live region, and the button works again', async ({ page }) => {
  await signIn(page);
  let n = 0;
  await page.route((url) => url.pathname === SIGN_OUT, async (route) => {
    n += 1;
    if (n === 1) await route.abort('connectionfailed');
    else await route.continue();
  });
  const button = page.getByRole('button', { name: 'Sign out', exact: true });
  await button.click();
  await expect(page.locator('#console-status')).toHaveText(RETRY_SENTENCE);
  await expect(button).toBeEnabled();
  await expect(button).toBeFocused();
  expect((await page.request.get(API_ME)).status()).toBe(200);
  await button.click();
  await expectSignInWith(page, SIGNED_OUT);
});

test('a newer anti-forgery pair from another tab: Sign out here reloads once, then signs out', async ({ page }) => {
  await signIn(page);
  // Another tab opens the sign-in page, which hands this browser a new anti-forgery pair:
  // the token in this page no longer matches, and the server refuses it with a 403.
  const other = await page.context().newPage();
  await standInForCloudflare(other);
  await other.goto(SIGN_IN);
  await expect(other.locator('#sign-in-form')).toBeVisible();
  await other.close();

  await page.bringToFront();
  const replies: number[] = [];
  page.on('response', (r) => { if (isPostTo(SIGN_OUT)(r.request())) replies.push(r.status()); });
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  // Still signed in, so the page loads itself anew, once, and says the request did not go through.
  await expect(page.locator('#console-status')).toHaveText(RETRY_SENTENCE);
  expect(new URL(page.url()).hash).toBe('');
  expect(new URL(page.url()).pathname).toBe(HOME);
  expect(replies).toEqual([403]);
  expect((await page.request.get(API_ME)).status()).toBe(200);
  await page.waitForLoadState('load');

  // The reloaded page carries the current pair: Sign out now works.
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expectSignInWith(page, SIGNED_OUT);
  expect(replies).toEqual([403, 200]);
  await expectSignedOut(page);
});

test.describe('JavaScript off', () => {
  test.use({ javaScriptEnabled: false });

  test('with JavaScript off Stats still shows its totals and the nav, written by the server', async ({ page }) => {
    // A browser without scripts cannot sign in through the pages: both steps go through the API.
    const html = await (await page.request.get(SIGN_IN)).text();
    const csrf = html.match(/<meta name="dw-csrf" content="([0-9A-F]{64})">/)![1];
    const step1 = await page.request.post(API_SIGN_IN, {
      headers: { 'X-CSRF-TOKEN': csrf, Origin: ORIGIN },
      data: { email: owner.email, password: owner.password, turnstileToken: PASS_TOKEN },
    });
    expect(step1.status()).toBe(200);
    const step2 = await page.request.post(API_CODE, {
      headers: { 'X-CSRF-TOKEN': csrf, Origin: ORIGIN },
      data: { code: authenticatorCode(owner.secret!) },
    });
    expect(step2.status()).toBe(200);

    await page.goto(HOME);
    await expect(page.locator('h1')).toHaveText('Stats');
    await expect(page.locator('.stats dt')).toHaveText(STATS);
    await expect(page.locator('#stats-as-of')).toHaveText(/^As of .+ ET$/);
    await expect(page.getByRole('navigation', { name: 'Admin' }).getByRole('link')).toHaveText(NAV.map((n) => n.label));
    await page.getByRole('link', { name: 'Negotiations', exact: true }).click();
    await expect(page.locator('h1')).toHaveText('Negotiations');
  });

  test('with JavaScript off Sign out still signs out: the form carries the token and lands on sign-in', async ({ page }) => {
    const html = await (await page.request.get(SIGN_IN)).text();
    const csrf = html.match(/<meta name="dw-csrf" content="([0-9A-F]{64})">/)![1];
    expect((await page.request.post(API_SIGN_IN, {
      headers: { 'X-CSRF-TOKEN': csrf, Origin: ORIGIN },
      data: { email: owner.email, password: owner.password, turnstileToken: PASS_TOKEN },
    })).status()).toBe(200);
    expect((await page.request.post(API_CODE, {
      headers: { 'X-CSRF-TOKEN': csrf, Origin: ORIGIN },
      data: { code: authenticatorCode(owner.secret!) },
    })).status()).toBe(200);

    await page.goto(HOME);
    await expect(page.locator('h1')).toHaveText('Stats');
    const posts: Request[] = [];
    page.on('request', (r) => { if (isPostTo(SIGN_OUT)(r)) posts.push(r); });
    await page.getByRole('button', { name: 'Sign out', exact: true }).click();

    // The browser sent the form itself: one urlencoded POST with the hidden field, no header token.
    await expect(page).toHaveURL(new RegExp(`^${ORIGIN}${SIGN_IN}(#signed-out)?$`));
    expect(posts).toHaveLength(1);
    const sent = await posts[0].allHeaders();
    expect(sent['content-type']).toBe('application/x-www-form-urlencoded');
    expect(sent['x-csrf-token']).toBeUndefined();
    // Under the no-referrer policy the browser sends "Origin: null" and no Referer with a form
    // POST; the server takes it as same-origin only on this route, by the browser's Sec-Fetch-*.
    expect(sent['origin']).toBe('null');
    expect(sent['referer']).toBeUndefined();
    expect(posts[0].postData()).toMatch(/^dw_csrf=[0-9A-F]{64}$/);
    // The server answered with the sign-in page, and the session is gone.
    const redirect = await posts[0].response();
    expect(redirect!.status()).toBe(303);
    expect(redirect!.headers()['location']).toBe('/admin/sign-in#signed-out');
    await expect(page.locator('h1')).toHaveText('Sign in');
    await expectSignedOut(page);
  });
});
