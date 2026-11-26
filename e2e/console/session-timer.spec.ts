import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { test, expect, type Locator, type Page, type Request } from '@playwright/test';
import { expectNoSeriousAxeFindings } from './axe';
import { authenticatorCode, seedOwner, type Owner } from '../sign-in/owner';
import { answerCloudflare } from '../signed-out/cloudflare';

/**
 * The session warnings on the signed-in pages against the real app (T04
 * piece 5; Spec §8.7): TD-ADM-UI-auth-14 (idle, "Still there?"),
 * TD-ADM-UI-auth-24 (8 hours, "Your session is ending") and the browser half
 * of TD-ADM-176 (Stay signed in renews the idle window only; the page's
 * timers never keep a session alive). Start it with e2e/console/run.sh.
 *
 * Time: the page runs on Playwright's clock, which the tests move forward
 * with page.clock.fastForward, so no test waits real minutes. The server
 * runs on the real clock, so a test that moves the page 28 minutes on moves
 * the session row back by as much (age_session.py). The status read
 * (GET /admin/api/session) renews nothing, so the tests also use it to read
 * the server's two ends.
 *
 * What the server answers is proven by the .NET tests
 * (AdminSessionTimerTests). This file proves what only a browser can.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const HOME = '/admin/';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const STATUS = '/admin/api/session';
const STAY = '/admin/api/session/stay-signed-in';

const IDLE_TITLE = 'Still there?';
const IDLE_TEXT = 'Your session will end within 2 minutes because there has been no activity.';
const ENDING_TITLE = 'Your session is ending';
const ENDING_TEXT = 'Your session will end within 2 minutes because of the 8-hour limit, and it can\'t be extended. '
  + 'Anything you haven\'t saved will be lost.';
const SESSION_ENDED = 'Your session has ended. Sign in again to continue.';
const SIGNED_OUT = 'You\'ve been signed out.';
const SIGN_IN_TITLE = 'Sign in · Dealoware admin';
const RETRY_SENTENCE = 'That didn\'t go through. Wait a moment and try again.';

const MINUTE = 60;
const HOUR = 60 * MINUTE;
// A few seconds past each mark, so the server is a little further on than the page.
const SLACK = 10;

const PASS_TOKEN = process.env.CONSOLE_E2E_TURNSTILE_TOKEN ?? '';
const DB = process.env.SIGN_IN_E2E_DB ?? '';
const PYTHON = process.env.SIGN_IN_E2E_PYTHON ?? '';

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

/** Moves the server's session back: the last activity and/or the sign-in, in seconds ago. */
function ageSession(times: { lastActiveAgo?: number; createdAgo?: number }): void {
  if (!DB || !PYTHON) throw new Error('SIGN_IN_E2E_DB and SIGN_IN_E2E_PYTHON are not set: start the suite with e2e/console/run.sh');
  const run = spawnSync(PYTHON, [path.join(__dirname, 'age_session.py')], {
    input: JSON.stringify({ db: DB, ...times }),
    encoding: 'utf8',
    timeout: 30_000,
    windowsHide: true,
  });
  if (run.error || run.status !== 0) throw new Error(`moving the session back failed: ${run.error?.message ?? run.stderr}`);
}

type Times = { idleExpiresAt: string; absoluteExpiresAt: string; idleSecondsLeft: number; absoluteSecondsLeft: number };

/** The server's two ends, by the read that renews nothing. */
async function serverTimes(page: Page): Promise<Times> {
  const reply = await page.request.get(STATUS);
  expect(reply.status()).toBe(200);
  return await reply.json() as Times;
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
  await expect(page).toHaveURL(ORIGIN + CODE, { timeout: 15_000 });
  await page.locator('#code').fill(authenticatorCode(owner.secret!));
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
}

function isStatusRead(r: Request): boolean {
  return new URL(r.url()).pathname === STATUS && r.method() === 'GET';
}

/**
 * Signs in and opens the page, styled, once its first status read has been
 * answered: moving the clock while that read is on its way would time it out.
 */
async function signInAndOpen(page: Page, at: string = HOME): Promise<void> {
  let read = page.waitForResponse((r) => isStatusRead(r.request()));
  await signInHere(page);
  await expect(page).toHaveURL(ORIGIN + HOME);
  await read;
  if (at !== HOME) {
    read = page.waitForResponse((r) => isStatusRead(r.request()));
    await page.goto(at);
    await read;
  }
  await expectStyled(page);
}

/** The stylesheet applies: a nav link has the shell's own minimum height (2.5rem). */
async function expectStyled(page: Page): Promise<void> {
  await page.waitForLoadState('load');
  const link = page.getByRole('navigation', { name: 'Admin' }).getByRole('link').first();
  await expect.poll(() => link.evaluate((el) => getComputedStyle(el).minHeight)).toBe('40px');
}

/** The dialog is open, modal, styled (its 2px border), so axe judges it as the owner sees it. */
async function expectModal(dialog: Locator): Promise<void> {
  await expect(dialog).toBeVisible();
  expect(await dialog.evaluate((el) => el.tagName === 'DIALOG' && (el as HTMLDialogElement).open && el.matches(':modal'))).toBe(true);
  await expect.poll(() => dialog.evaluate((el) => getComputedStyle(el).borderTopWidth)).toBe('2px');
}

/** A nav link with the focus, to see it come back when a dialog closes. */
async function focusNavLink(page: Page, label: string): Promise<Locator> {
  const link = page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: label, exact: true });
  await link.focus();
  await expect(link).toBeFocused();
  return link;
}

/** On S-A1, opened with one of its arrival sentences, and no console drawn. */
async function expectSignInWith(page: Page, sentence: string): Promise<void> {
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page).toHaveTitle(SIGN_IN_TITLE);
  await expect(page.locator('#form-status')).toHaveText(sentence);
  await expect(page.locator('.console-nav')).toHaveCount(0);
}

/**
 * Every request the page sends from now until it navigates: "METHOD /path".
 * A navigation ends the list, so what the sign-in page sends is not in it.
 */
function recordUntilNavigation(page: Page): { sent: string[]; navigated: () => boolean } {
  const sent: string[] = [];
  let navigated = false;
  page.on('request', (r) => {
    if (navigated) return;
    if (r.isNavigationRequest()) {
      navigated = true;
      return;
    }
    sent.push(`${r.method()} ${new URL(r.url()).pathname}`);
  });
  return { sent, navigated: () => navigated };
}

test.beforeEach(async ({ page }) => {
  owner = seedOwner({ enrolled: true });
  cspMessages = [];
  pageErrors = [];
  page.on('console', (message) => {
    const text = message.text();
    if (!/content[- ]security[- ]policy/i.test(text)) return;
    if (text.includes('/favicon.ico')) return;
    const p = new URL(page.url()).pathname;
    if (p.startsWith('/admin/api/') || p.startsWith('/admin/assets/')) return;
    cspMessages.push(text);
  });
  page.on('pageerror', (error) => pageErrors.push(error.message));
  await answerCloudflare(page, { token: PASS_TOKEN });
  // The page's clock: it runs as usual until a test moves it on.
  await page.clock.install();
});

test.afterEach(() => {
  expect(cspMessages).toEqual([]);
  expect(pageErrors).toEqual([]);
});

test('UI-auth-14: at 28 minutes idle "Still there?" opens, focus on Stay signed in, announced, Tab stays in it; axe finds nothing serious', async ({ page }) => {
  await signInAndOpen(page);
  const offers = await focusNavLink(page, 'Offers');
  // Nothing shows before the 2 minutes.
  await expect(page.getByRole('dialog')).toHaveCount(0);

  ageSession({ lastActiveAgo: 28 * MINUTE + SLACK });
  await page.clock.fastForward('28:00');

  const dialog = page.getByRole('dialog', { name: IDLE_TITLE });
  await expectModal(dialog);
  await expect(dialog).toHaveAccessibleDescription(IDLE_TEXT);
  await expect(dialog.getByRole('heading', { level: 2 })).toHaveText(IDLE_TITLE);
  await expect(dialog.getByRole('button')).toHaveText(['Stay signed in', 'Sign out']);
  const stay = dialog.getByRole('button', { name: 'Stay signed in', exact: true });
  const signOut = dialog.getByRole('button', { name: 'Sign out', exact: true });
  await expect(stay).toBeFocused();
  // One dialog: never the 8-hour one with it.
  await expect(page.getByRole('alertdialog')).toHaveCount(0);
  await expect(page.locator('dialog')).toHaveCount(1);
  await expectNoSeriousAxeFindings(page, 'session_idle-dialog');

  // The keyboard stays in the dialog.
  if (tabStopsAtLinks()) {
    await page.keyboard.press('Tab');
    await expect(signOut).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(stay).toBeFocused();
    await page.keyboard.press('Shift+Tab');
    await expect(signOut).toBeFocused();
    await page.keyboard.press('Shift+Tab');
    await expect(stay).toBeFocused();
  } else {
    // WebKit, like Safari as delivered, may pass buttons by on Tab: each control is focused directly.
    await signOut.focus();
    await expect(signOut).toBeFocused();
    await stay.focus();
    await expect(stay).toBeFocused();
  }
  // The page behind the dialog cannot be reached.
  await expect(offers).not.toBeFocused();

  // Escape closes it and sends nothing; the focus goes back to where it was.
  const recorded = recordUntilNavigation(page);
  await page.keyboard.press('Escape');
  await expect(page.locator('dialog')).toHaveCount(0);
  await expect(offers).toBeFocused();
  expect(recorded.sent).toEqual([]);
});

test('TD-ADM-176 in the page: Stay signed in sends one POST with the anti-forgery header; the server renews the idle window only', async ({ page }) => {
  await signInAndOpen(page);
  const offers = await focusNavLink(page, 'Offers');
  ageSession({ lastActiveAgo: 28 * MINUTE + SLACK });
  const before = await serverTimes(page);
  expect(before.idleSecondsLeft).toBeLessThanOrEqual(2 * MINUTE);

  await page.clock.fastForward('28:00');
  const dialog = page.getByRole('dialog', { name: IDLE_TITLE });
  await expectModal(dialog);

  const posts: Request[] = [];
  page.on('request', (r) => { if (new URL(r.url()).pathname === STAY) posts.push(r); });
  const answered = page.waitForResponse((r) => new URL(r.url()).pathname === STAY);
  await dialog.getByRole('button', { name: 'Stay signed in', exact: true }).click();
  expect((await answered).status()).toBe(200);
  await expect(page.locator('dialog')).toHaveCount(0);
  await expect(offers).toBeFocused();

  expect(posts).toHaveLength(1);
  expect(posts[0].method()).toBe('POST');
  const sent = await posts[0].allHeaders();
  expect(sent['x-csrf-token']).toMatch(/^[0-9A-F]{64}$/);
  expect(sent['origin']).toBe(ORIGIN);
  expect(posts[0].postData()).toBe('{}');

  // The idle window starts again; the 8-hour end has not moved.
  const after = await serverTimes(page);
  expect(after.absoluteExpiresAt).toBe(before.absoluteExpiresAt);
  expect(after.idleSecondsLeft).toBeGreaterThan(29 * MINUTE);
  expect(Date.parse(after.idleExpiresAt)).toBeGreaterThan(Date.parse(before.idleExpiresAt));

  // And no dialog until the next 28 minutes.
  await page.clock.fastForward('05:00');
  await expect(page.locator('dialog')).toHaveCount(0);
  expect(await page.evaluate(() => localStorage.length + sessionStorage.length)).toBe(0);
});

test('UI-auth-14 and TD-ADM-176: left open, the dialog keeps nothing alive; at the end the page goes to sign-in with the session-ended sentence, and back after sign-in', async ({ page }) => {
  await signInAndOpen(page, '/admin/negotiations');
  ageSession({ lastActiveAgo: 28 * MINUTE + SLACK });
  const recorded = recordUntilNavigation(page);

  await page.clock.fastForward('28:00');
  await expectModal(page.getByRole('dialog', { name: IDLE_TITLE }));
  // A minute with the dialog open: the page sends nothing.
  const sentWhenOpened = recorded.sent.length;
  await page.clock.fastForward('01:00');
  expect(recorded.sent.length).toBe(sentWhenOpened);

  // The server's idle end passes. At the page's end it asks once more, gets the gate's 401,
  // and asks for itself anew: the gate sends it to sign-in.
  ageSession({ lastActiveAgo: 30 * MINUTE + SLACK });
  await page.clock.fastForward('01:00');
  await expectSignInWith(page, SESSION_ENDED);
  // Everything the page sent on its own was a status read: no renewal of any kind.
  expect(recorded.sent.length).toBeGreaterThan(0);
  expect(new Set(recorded.sent)).toEqual(new Set([`GET ${STATUS}`]));
  expect((await page.request.get(STATUS)).status()).toBe(401);
  await expectNoSeriousAxeFindings(page, 'session_ended-after-idle');

  // A full sign-in again (password and code), and the page it was on. A fresh
  // authenticator key, so the code is not the one this run already used.
  owner = seedOwner({ enrolled: true });
  await signInHere(page);
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations');
});

test('Stay signed in with no answer: the retry sentence in the dialog, the focus back on Stay signed in, and a second press works', async ({ page }) => {
  await signInAndOpen(page);
  ageSession({ lastActiveAgo: 28 * MINUTE + SLACK });
  await page.clock.fastForward('28:00');
  const dialog = page.getByRole('dialog', { name: IDLE_TITLE });
  await expectModal(dialog);

  let n = 0;
  await page.route((url) => url.pathname === STAY, async (route) => {
    n += 1;
    if (n === 1) await route.abort('connectionfailed');
    else await route.continue();
  });
  const stay = dialog.getByRole('button', { name: 'Stay signed in', exact: true });
  await stay.click();
  await expect(dialog.getByRole('alert')).toHaveText(RETRY_SENTENCE);
  await expect(stay).toBeEnabled();
  await expect(stay).toBeFocused();
  await expectNoSeriousAxeFindings(page, 'session_idle-dialog_retry');

  await stay.click();
  await expect(page.locator('dialog')).toHaveCount(0);
  expect(n).toBe(2);
  expect((await serverTimes(page)).idleSecondsLeft).toBeGreaterThan(29 * MINUTE);
});

test('UI-auth-14: Sign out in the dialog signs out like the nav\'s Sign out', async ({ page }) => {
  await signInAndOpen(page);
  ageSession({ lastActiveAgo: 28 * MINUTE + SLACK });
  await page.clock.fastForward('28:00');
  const dialog = page.getByRole('dialog', { name: IDLE_TITLE });
  await expectModal(dialog);
  await dialog.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expectSignInWith(page, SIGNED_OUT);
  expect((await page.request.get(STATUS)).status()).toBe(401);
});

test('active in another tab meanwhile: at the end the server says the session goes on, and the dialog closes by itself', async ({ page }) => {
  await signInAndOpen(page);
  ageSession({ lastActiveAgo: 28 * MINUTE + SLACK });
  await page.clock.fastForward('28:00');
  await expectModal(page.getByRole('dialog', { name: IDLE_TITLE }));

  // Another tab's request renewed the session on the server.
  ageSession({ lastActiveAgo: 0 });
  await page.clock.fastForward('02:00');
  await expect(page.locator('dialog')).toHaveCount(0);
  await expect(page).toHaveURL(ORIGIN + HOME);
  expect((await serverTimes(page)).idleSecondsLeft).toBeGreaterThan(29 * MINUTE);
});

test('UI-auth-24: at 7h58m "Your session is ending" opens as an alertdialog with one OK; Escape acts as OK; nothing extends; at 8h the page goes to sign-in', async ({ page }) => {
  await signInAndOpen(page);
  const participants = await focusNavLink(page, 'Participants');
  // 7h58m since sign-in, with activity just now.
  ageSession({ createdAgo: 7 * HOUR + 58 * MINUTE + SLACK, lastActiveAgo: 0 });
  const recorded = recordUntilNavigation(page);

  await page.clock.fastForward('07:58:00');
  const dialog = page.getByRole('alertdialog', { name: ENDING_TITLE });
  await expectModal(dialog);
  await expect(dialog).toHaveAttribute('role', 'alertdialog');
  await expect(dialog).toHaveAccessibleDescription(ENDING_TEXT);
  await expect(dialog.getByRole('button')).toHaveText(['OK']);
  await expect(dialog.getByRole('button', { name: 'OK', exact: true })).toBeFocused();
  // Nothing to extend it with, and the idle dialog is not there.
  await expect(page.getByRole('button', { name: 'Stay signed in' })).toHaveCount(0);
  await expect(page.getByRole('dialog', { name: IDLE_TITLE })).toHaveCount(0);
  await expect(page.locator('dialog')).toHaveCount(1);
  await expectNoSeriousAxeFindings(page, 'session_ending-dialog');

  // Escape acts as OK: closed, and the focus goes back.
  await page.keyboard.press('Escape');
  await expect(page.locator('dialog')).toHaveCount(0);
  await expect(participants).toBeFocused();
  // It does not come back.
  await page.clock.fastForward('00:30');
  await expect(page.locator('dialog')).toHaveCount(0);

  // At 8 hours the server's end passes; at the page's end it asks, gets the 401, and goes to sign-in.
  ageSession({ createdAgo: 8 * HOUR + SLACK });
  await page.clock.fastForward('01:30');
  await expectSignInWith(page, SESSION_ENDED);
  expect(new Set(recorded.sent)).toEqual(new Set([`GET ${STATUS}`]));
  expect((await page.request.get(STATUS)).status()).toBe(401);
});

test('UI-auth-24: idle and 8-hour ends in the same 2 minutes: only "Your session is ending"; OK returns the focus and the idle dialog never stacks on it', async ({ page }) => {
  await signInAndOpen(page);
  const artifacts = await focusNavLink(page, 'Artifacts');
  ageSession({ createdAgo: 7 * HOUR + 58 * MINUTE + SLACK, lastActiveAgo: 28 * MINUTE + SLACK });
  const recorded = recordUntilNavigation(page);

  await page.clock.fastForward('28:00');
  const dialog = page.getByRole('alertdialog', { name: ENDING_TITLE });
  await expectModal(dialog);
  await expect(page.getByRole('dialog', { name: IDLE_TITLE })).toHaveCount(0);
  await expect(page.locator('dialog')).toHaveCount(1);

  if (tabStopsAtLinks()) {
    // One control: Tab stays on it.
    await page.keyboard.press('Tab');
    await expect(dialog.getByRole('button', { name: 'OK', exact: true })).toBeFocused();
  }
  await page.keyboard.press('Enter');
  await expect(page.locator('dialog')).toHaveCount(0);
  await expect(artifacts).toBeFocused();

  await page.clock.fastForward('00:30');
  await expect(page.locator('dialog')).toHaveCount(0);
  expect(new Set(recorded.sent)).toEqual(new Set([`GET ${STATUS}`]));

  ageSession({ createdAgo: 8 * HOUR + SLACK });
  await page.clock.fastForward('01:30');
  await expectSignInWith(page, SESSION_ENDED);
});
