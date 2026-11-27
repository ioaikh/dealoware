import { test, expect, type Page, type Response } from '@playwright/test';
import { collectPageErrors } from './page-errors';
import { seedEntities, type Seeded } from './entities';
import { authenticatorCode, seedOwner, type Owner } from '../sign-in/owner';
import { answerCloudflare } from '../signed-out/cloudflare';

/**
 * The signed-out browser walk (T08a): every signed-in page of the route
 * table, opened in a browser with no session, lands on sign-in with no
 * record data on screen at any moment (TD-ADM-131, TD-ADM-UI-na-X01 step 3);
 * Back and reload after sign-out show no record data (TD-ADM-173); signed
 * in, an unknown address, an id no record has and a deleted record's id are
 * S-D3 with nothing of a record (TD-ADM-UI-na-X01 step 2, TD-ADM-179); and a
 * session that ends while a record is open goes to sign-in and back to that
 * record (TD-ADM-UI-na-B13 steps 1 and 2). Start it with e2e/console/run.sh.
 *
 * "At any moment" is read from the wire: every reply the browser gets on the
 * way (the gate's redirect, the sign-in page, its scripts) is searched for
 * the seeded records' names, addresses and ids, and so is the page at the
 * end. A record that was never sent cannot have been on screen.
 *
 * What the server answers to each route, method and session state is proven
 * by the .NET denial tests (AdminRouteDenialTests). This file proves what
 * only a browser can.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const SESSION_ENDED = 'Your session has ended. Sign in again to continue.';
const SIGNED_OUT = 'You\'ve been signed out.';
const NOT_FOUND = 'We can\'t find that page.';
const NOT_FOUND_TITLE = 'Page not found · Dealoware admin';
const NO_SUCH_ID = '0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a59';

// The signed-in pages of the route table (tests/Dealoware.Api.Tests/AdminRouteTable.cs), as patterns.
// AdminRouteDenialTests holds this list to the table both ways: a new signed-in page must be walked here.
const SIGNED_IN_PAGES = [
  '/admin/',
  '/admin/participants',
  '/admin/artifacts',
  '/admin/negotiations',
  '/admin/offers',
  '/admin/participants/{id:guid}',
  '/admin/artifacts/{id:guid}',
  '/admin/negotiations/{id:guid}',
  '/admin/offers/{id:guid}',
  '/admin/audit',
  '/admin/audit/{id:guid}',
  '/admin/settings/security',
];

const PASS_TOKEN = process.env.CONSOLE_E2E_TURNSTILE_TOKEN ?? '';

let owner: Owner;
let seeded: Seeded;
let cspMessages: string[] = [];
let pageErrors: () => string[] = () => [];

/** A live record of the type the pattern names (the audit entry: any id, since none is opened signed in here). */
function liveId(pattern: string): string {
  if (pattern.startsWith('/admin/participants/')) return seeded.participants['Alex Rivera'].id;
  if (pattern.startsWith('/admin/artifacts/')) return seeded.artifacts['Red bicycle'];
  if (pattern.startsWith('/admin/negotiations/')) return seeded.negotiations.bicycle;
  if (pattern.startsWith('/admin/offers/')) return seeded.offers.terms;
  return NO_SUCH_ID;
}

/** Everything that is record data: every seeded name, the addresses made from them, every id and sub. */
function recordData(): string[] {
  const names = [...Object.keys(seeded.participants), ...Object.keys(seeded.artifacts)];
  const addresses = Object.keys(seeded.participants).map((n) => n.toLowerCase().replace(/ /g, '.') + '@example.test');
  const ids = [
    ...Object.values(seeded.participants).flatMap((p) => [p.id, p.sub]),
    ...Object.values(seeded.artifacts),
    ...Object.values(seeded.negotiations),
    ...Object.values(seeded.offers),
  ];
  return [...names, ...addresses, ...ids].map((v) => v.toLowerCase());
}

function expectNoRecordData(text: string, where: string): void {
  const lower = text.toLowerCase();
  for (const value of recordData()) expect(lower.includes(value), `${where} shows record data: ${value}`).toBe(false);
}

/** Every reply the page gets while `act` runs, read once it is over. Redirects have no body to read. */
async function repliesDuring(page: Page, act: () => Promise<void>): Promise<{ url: string; status: number; text: string }[]> {
  const responses: Response[] = [];
  const listener = (r: Response) => { responses.push(r); };
  page.on('response', listener);
  try {
    await act();
  } finally {
    page.off('response', listener);
  }

  const replies: { url: string; status: number; text: string }[] = [];
  for (const r of responses) {
    const status = r.status();
    const text = status >= 300 && status < 400 ? '' : await r.text().catch(() => '');
    replies.push({ url: r.url(), status, text });
  }
  return replies;
}

/** On S-A1 with this sentence in its status line, and no console drawn. */
async function expectSignInWith(page: Page, sentence: string): Promise<void> {
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#form-status')).toHaveText(sentence);
  await expect(page.locator('.console-nav')).toHaveCount(0);
}

/** Both sign-in steps through the real pages, from the sign-in page the browser is on. */
async function signInHere(page: Page): Promise<void> {
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await page.locator('#email').fill(owner.email);
  await page.locator('#password').fill(owner.password);
  await page.getByRole('button', { name: 'Continue', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + CODE, { timeout: 15_000 });
  await page.locator('#code').fill(authenticatorCode(owner.secret!));
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
}

async function signIn(page: Page): Promise<void> {
  await page.goto(SIGN_IN);
  await signInHere(page);
  await expect(page).toHaveURL(ORIGIN + '/admin/');
  await page.waitForLoadState('load');
}

test.beforeAll(() => {
  seeded = seedEntities();
});

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

test('TD-ADM-131, TD-ADM-UI-na-X01 step 3: every signed-in page, opened signed out, lands on sign-in with the session-ended sentence; no reply on the way carries record data', async ({ page }) => {
  test.setTimeout(90_000);
  const addresses = [...SIGNED_IN_PAGES.map((p) => p.replace('{id:guid}', liveId(p))), '/admin/no-such-page'];

  for (const address of addresses) {
    const replies = await repliesDuring(page, async () => {
      await page.goto(address);
      await expectSignInWith(page, SESSION_ENDED);
    });

    // The first reply is the gate's: a redirect to the fixed sign-in address, not the page.
    expect(new URL(replies[0].url).pathname + new URL(replies[0].url).search, address).toBe(address);
    expect(replies[0].status, address).toBe(302);
    for (const reply of replies) {
      expectNoRecordData(reply.text, `${address}: ${reply.url}`);
      expect(reply.text, `${address}: ${reply.url}`).not.toContain('console-nav');
    }
    expectNoRecordData(await page.content(), `${address}: the page`);
    expect((await page.context().cookies()).find((c) => c.name === 'dw_admin_session')).toBeUndefined();
  }
});

test('TD-ADM-173: after sign-out, Back and reload ask anew and show no record data', async ({ page }) => {
  await signIn(page);
  const person = `/admin/participants/${seeded.participants['Alex Rivera'].id}`;
  const thing = `/admin/artifacts/${seeded.artifacts['Red bicycle']}`;
  await page.goto(person);
  await expect(page.locator('main')).toContainText('Alex Rivera');
  await page.goto(thing);
  await expect(page.locator('main')).toContainText('Red bicycle');

  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expectSignInWith(page, SIGNED_OUT);

  // Sign-out replaced the last page (the artifact) with sign-in, so Back leads to the participant, then to Stats:
  // each asked for anew, each sent to sign-in. Nothing comes from a cache.
  for (const address of [person, '/admin/']) {
    const replies = await repliesDuring(page, async () => {
      await page.goBack();
      await expectSignInWith(page, SESSION_ENDED);
    });
    expect(replies.some((r) => new URL(r.url).pathname === address && r.status === 302), `Back to ${address} was not asked for anew`).toBe(true);
    for (const reply of replies) expectNoRecordData(reply.text, `Back to ${address}: ${reply.url}`);
    expectNoRecordData(await page.content(), `Back to ${address}: the page`);
  }

  await page.reload();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  expectNoRecordData(await page.content(), 'reload');
});

test('TD-ADM-UI-na-X01 step 2, TD-ADM-179: signed in, an unknown address, an id no record has, and a deleted record\'s id are S-D3, with nothing of a record', async ({ page }) => {
  await signIn(page);
  const deleted: Record<string, string> = {
    participants: seeded.participants['Robin Hart'].id,
    artifacts: seeded.artifacts['Broken lamp'],
    negotiations: seeded.negotiations.deleted,
    offers: seeded.offers.deleted,
  };
  const addresses = [
    '/admin/no-such-page',
    '/admin/audit/' + NO_SUCH_ID,
    ...Object.keys(deleted).map((type) => `/admin/${type}/${NO_SUCH_ID}`),
    ...Object.entries(deleted).map(([type, id]) => `/admin/${type}/${id}`),
  ];

  for (const address of addresses) {
    const response = await page.goto(address);
    expect(response!.status(), address).toBe(404);
    await expect(page).toHaveURL(ORIGIN + address);
    await expect(page).toHaveTitle(NOT_FOUND_TITLE);
    await expect(page.locator('h1')).toHaveText(NOT_FOUND);
    await expect(page.locator('main').getByRole('link', { name: 'Stats' })).toHaveAttribute('href', '/admin/');
    expectNoRecordData(await response!.text(), address);
  }
});

test('TD-ADM-UI-na-B13 steps 1 and 2: a session that ends while a list is open: the record link goes to sign-in with no record data, and signing in opens that record', async ({ page }) => {
  await signIn(page);
  await page.goto('/admin/participants?q=Alex%20Rivera');
  const record = `/admin/participants/${seeded.participants['Alex Rivera'].id}`;
  // The record's link carries the list's query, so that its page can lead back to the same view.
  const link = page.locator(`main table a.id[href^="${record}?"]`);
  await expect(link).toBeVisible();

  // The session ends on the server (seeding the owner again ends every session), as after 30 idle minutes.
  owner = seedOwner({ enrolled: true });

  const replies = await repliesDuring(page, async () => {
    await link.click();
    await expectSignInWith(page, SESSION_ENDED);
  });
  expect(replies.some((r) => new URL(r.url).pathname === record && r.status === 302)).toBe(true);
  for (const reply of replies) expectNoRecordData(reply.text, `after the session ended: ${reply.url}`);
  expectNoRecordData(await page.content(), 'after the session ended: the page');

  await signInHere(page);
  await expect.poll(() => new URL(page.url()).pathname).toBe(record);
  await expect(page.locator('main')).toContainText('Alex Rivera');
});
