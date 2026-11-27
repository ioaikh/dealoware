import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { test, expect, type Locator, type Page, type Request } from '@playwright/test';
import { expectNoSeriousAxeFindings } from './axe';
import { collectPageErrors } from './page-errors';
import { seedEntities, type Seeded } from './entities';
import { authenticatorCode, seedOwner, type Owner } from '../sign-in/owner';
import { answerCloudflare } from '../signed-out/cloudflare';

/**
 * The audit log viewer against the real app (T07, A7 Step 15): S-D1 the list
 * at /admin/audit and S-D2 one entry at /admin/audit/{id}, signed in through
 * the real pages, over the entries seed_audit.py writes into the run's
 * database beside the ones the app writes itself. Start it with
 * e2e/console/run.sh.
 *
 * The server writes both pages from their address; these tests prove what a
 * browser adds: the script that moves to a new address and keeps the focus,
 * the live region, keyboard use, the tooltip of a time, layout at 320 px,
 * and axe on the list, an entry, the empty log and a filter miss. What the
 * server answers for each address is proven by the .NET tests
 * (AdminAuditPageTests, AdminAuditViewerApiTests). Case ids:
 * TD-ADM-UI-na-X02, TD-ADM-UI-na-X03 (the nav entry), TD-ADM-101 step 2,
 * TD-ADM-UI-na-B15, -axe and -kbd.
 *
 * The seeded entries are all dated in 2025, so every test finds them with
 * the range-of-days filter, whatever the app has written in this run.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const API_SIGN_IN = '/admin/api/auth/sign-in';
const API_CODE = '/admin/api/auth/sign-in/code';
const AUDIT = '/admin/audit';
const PASS_TOKEN = process.env.CONSOLE_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.CONSOLE_E2E_ARTIFACTS;
const DB = process.env.SIGN_IN_E2E_DB ?? '';
const PYTHON = process.env.SIGN_IN_E2E_PYTHON ?? '';

const NAV = ['Stats', 'Participants', 'Artifacts', 'Negotiations', 'Offers', 'Audit log', 'Security settings'];
const COLUMNS = ['Time (ET)', 'Actor', 'Action', 'Entity type', 'Entity ID', 'Outcome'];
/** The seeded entries of early March 2025, and the day that holds the 130 for paging. */
const MARCH = 'from=2025-03-01&to=2025-03-31';
const BULK_DAY = 'from=2025-02-10&to=2025-02-10';
const NOT_FOUND = 'We can\'t find that page.';

type AuditSeed = {
  entries: Record<'failed' | 'locked' | 'signedIn' | 'edit' | 'delete' | 'offerEdit' | 'signedOut' | 'summer', string>;
  /** What the viewer shows of the stored IP value, and the rest of it, which it never shows. */
  prefix: string;
  rest: string;
  admin: string;
  typed: string;
  bulk: number;
};

let owner: Owner;
let entities: Seeded;
let audit: AuditSeed;
let cspMessages: string[] = [];
let pageErrors: () => string[] = () => [];

/** Runs seed_audit.py with the given request. The script decides whether it will touch the database named in it. */
function runAuditSeedScript(request: Record<string, unknown>): { status: number | null; stdout: string; stderr: string } {
  const run = spawnSync(PYTHON, [path.join(__dirname, 'seed_audit.py')], {
    input: JSON.stringify(request),
    encoding: 'utf8',
    timeout: 60_000,
  });
  return { status: run.status, stdout: run.stdout, stderr: run.stderr };
}

/** The made-up audit entries (seed), no entry at all (empty), or the made-up ones gone again (remove). */
function seedAudit(mode: 'seed' | 'empty' | 'remove' = 'seed'): AuditSeed {
  if (!DB || !PYTHON) throw new Error('SIGN_IN_E2E_DB and SIGN_IN_E2E_PYTHON are not set: start the suite with e2e/console/run.sh');
  const run = runAuditSeedScript({
    db: DB,
    mode,
    participant: entities.participants['Alex Rivera'].id,
    deleted: entities.participants['Robin Hart'].id,
    offer: entities.offers.terms,
  });
  if (run.status !== 0) throw new Error(`seeding the audit entries failed: ${run.stderr}`);
  return JSON.parse(run.stdout) as AuditSeed;
}

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

async function signIn(page: Page): Promise<void> {
  await page.goto(SIGN_IN);
  await expect(page.locator('#sign-in-form')).toBeVisible();
  await page.locator('#email').fill(owner.email);
  await page.locator('#password').fill(owner.password);
  await page.getByRole('button', { name: 'Continue', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + CODE, { timeout: 15_000 });
  await page.locator('#code').fill(authenticatorCode(owner.secret!));
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/');
  await page.waitForLoadState('load');
}

/** The page has loaded and its stylesheet applies (a nav link is 2.5rem tall): only then is axe or a size fair. */
async function expectStyled(page: Page): Promise<void> {
  await page.waitForLoadState('load');
  const link = page.getByRole('navigation', { name: 'Admin' }).getByRole('link').first();
  await expect.poll(() => link.evaluate((el) => getComputedStyle(el).minHeight)).toBe('40px');
}

/** Opens a page by its address and waits for it to be whole. */
async function open(page: Page, address: string): Promise<void> {
  await page.goto(address);
  await expectStyled(page);
}

/**
 * After a move, before the pointer acts on the new page: the page has said its count in the
 * live region. console-audit.js does that 100 ms after the page loads, and it is the last
 * thing the page does as it loads, so only then is the page whole for the pointer. (The
 * banner's space is kept from the start, so the count moves nothing below it.)
 */
async function settled(page: Page): Promise<void> {
  await expect(page.locator('#console-status')).toHaveText(/^[\d,]+ results$/);
}

/** Where the page is, as path and query. */
function here(page: Page): string {
  const url = new URL(page.url());
  return url.pathname + url.search;
}

function rows(page: Page): Locator {
  return page.locator('#list-table tbody tr');
}

/** The heading cells' words, without the words only screen readers get. */
async function headings(page: Page): Promise<string[]> {
  return page.locator('#list-table thead th').evaluateAll((cells) => cells.map((cell) => {
    const copy = cell.cloneNode(true) as HTMLElement;
    copy.querySelectorAll('.visually-hidden').forEach((n) => n.remove());
    return (copy.textContent ?? '').trim();
  }));
}

/** The column of the given heading, as text, row by row. */
async function column(page: Page, heading: string): Promise<string[]> {
  const index = COLUMNS.indexOf(heading);
  expect(index).toBeGreaterThanOrEqual(0);
  return rows(page).evaluateAll((trs, i) => trs.map((tr) => (tr.children[i as number]?.textContent ?? '').trim()), index);
}

/** Every page the browser asks for as a document, from here on. */
function documents(page: Page): string[] {
  const asked: string[] = [];
  page.on('request', (r: Request) => {
    if (r.isNavigationRequest() && r.frame() === page.mainFrame()) {
      const url = new URL(r.url());
      asked.push(url.pathname + url.search);
    }
  });
  return asked;
}

async function expectNoSidewaysScroll(page: Page): Promise<void> {
  const size = await page.evaluate(() => ({ content: document.documentElement.scrollWidth, viewport: document.documentElement.clientWidth }));
  expect(size.content).toBeLessThanOrEqual(size.viewport);
}

/** Nothing on the page could change anything: outside Sign out there is no button, field or form, and nothing named for a change. */
async function expectReadOnly(page: Page): Promise<void> {
  await expect(page.locator('main').locator('button, input, select, textarea, form, [contenteditable]')).toHaveCount(0);
  await expect(page.locator('main').getByRole('button')).toHaveCount(0);
  await expect(page.getByRole('button', { name: /edit|delete|save|remove|restore/i })).toHaveCount(0);
  await expect(page.getByRole('link', { name: /edit|delete|save|remove|restore/i })).toHaveCount(0);
}

test.beforeAll(() => {
  entities = seedEntities();
  audit = seedAudit();
});

test.afterAll(() => {
  // The made-up entries go; what the app wrote itself stays for the suites that follow.
  if (DB && PYTHON && entities) seedAudit('remove');
});

test.beforeEach(async ({ page }) => {
  owner = seedOwner({ enrolled: true });
  cspMessages = [];
  page.on('console', (message) => {
    const text = message.text();
    if (!/content[- ]security[- ]policy/i.test(text)) return;
    if (text.includes('/favicon.ico')) return;
    const at = new URL(page.url()).pathname;
    if (at.startsWith('/admin/api/') || at.startsWith('/admin/assets/')) return;
    cspMessages.push(text);
  });
  pageErrors = collectPageErrors(page);
  await answerCloudflare(page, { token: PASS_TOKEN });
});

test.afterEach(() => {
  expect(cspMessages).toEqual([]);
  expect(pageErrors()).toEqual([]);
});

test('the audit log, from the nav: the locked columns, newest first with this sign-in on top, its filters, no search box; axe; a screenshot', async ({ page }) => {
  await signIn(page);
  const nav = page.getByRole('navigation', { name: 'Admin' });
  // X03: the nav lists Audit log, between Offers and Security settings.
  await expect(nav.getByRole('link')).toHaveText(NAV);
  await nav.getByRole('link', { name: 'Audit log', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + AUDIT);
  await expectStyled(page);
  await expect(page).toHaveTitle('Audit log · Dealoware admin');
  await expect(page.locator('h1')).toHaveText('Audit log');
  await expect(nav.locator('[aria-current="page"]')).toHaveText('Audit log');
  await expect(nav.getByRole('link')).toHaveText(NAV);

  // X02: the columns, in order; Time, newest first, is the one order the log has.
  expect(await headings(page)).toEqual(COLUMNS);
  const sorted = page.locator('#list-table th[aria-sort]');
  await expect(sorted).toHaveCount(1);
  await expect(sorted).toHaveAttribute('aria-sort', 'descending');
  await expect(sorted).toContainText('Time (ET)');
  await expect(page.locator('#list-table th button')).toHaveCount(0);
  const times = await rows(page).locator('time').evaluateAll((els) => els.map((el) => el.getAttribute('datetime') ?? ''));
  expect(times).toHaveLength(50);
  expect(times).toEqual([...times].sort().reverse());

  // The newest entry is the sign-in that opened this page: the log is the app's own.
  const first = rows(page).first().locator('td');
  await expect(first).toHaveCount(6);
  await expect(first.nth(0).locator('time')).toHaveText(/^[A-Z][a-z]{2} \d{1,2}, \d{4}, \d{1,2}:\d{2} (AM|PM) ET$/);
  await expect(first.nth(1)).toHaveText(owner.email);
  await expect(first.nth(2)).toHaveText('login_success');
  await expect(first.nth(3)).toHaveText('—');
  await expect(first.nth(4)).toHaveText('—');
  await expect(first.nth(5)).toHaveText('Success');

  // Paging: 50 a page to start with; 50, 100 or 200.
  await expect(rows(page)).toHaveCount(50);
  await expect(page.locator('#list-range')).toHaveText(/^Showing 1–50 of [\d,]+$/);
  expect(await page.locator('#limit option').allTextContents()).toEqual(['50', '100', '200']);
  await expect(page.locator('#limit')).toHaveValue('50');

  // Filters: actions (several), an entity type, a range of days. Nothing ticked or chosen.
  const actions = await page.locator('fieldset.statuses label').allTextContents();
  expect(actions).toEqual(expect.arrayContaining(['login_success', 'login_failure', 'sign_out', 'entity_edit', 'entity_delete', 'account.password_changed']));
  expect(new Set(actions).size).toBe(actions.length);
  await expect(page.locator('fieldset.statuses input[type="checkbox"]')).toHaveCount(actions.length);
  await expect(page.locator('fieldset.statuses input:checked')).toHaveCount(0);
  await expect(page.locator('fieldset.statuses legend')).toHaveText('Action');
  expect(await page.getByLabel('Entity type', { exact: true }).locator('option').allTextContents())
    .toEqual(['All types', 'Participant', 'Artifact', 'Negotiation', 'Offer']);
  await expect(page.getByLabel('From', { exact: true })).toHaveAttribute('type', 'date');
  await expect(page.getByLabel('To', { exact: true })).toHaveAttribute('type', 'date');
  await expect(page.locator('#chips')).toHaveCount(0);

  // No search box (the Spec has not decided on one).
  await expect(page.locator('main input[type="search"], main input[type="text"], main input:not([type])')).toHaveCount(0);
  await expect(page.getByRole('searchbox')).toHaveCount(0);

  // The list shows nothing of an IP, and offers nothing that changes the log.
  await expect(page.locator('body')).not.toContainText(audit.prefix);
  await expect(page.locator('main').getByRole('button')).toHaveText(['Apply filters']);
  await expect(page.getByRole('button', { name: /edit|delete|save|remove|restore/i })).toHaveCount(0);

  // One live region; the page's own files only; nothing stored in the browser.
  await expect(page.locator('[aria-live]')).toHaveCount(1);
  const scripts = await page.locator('script').evaluateAll((els) => els.map((e) => e.getAttribute('src')));
  expect(scripts).toEqual(['/admin/auth/admin-request.js', '/admin/assets/console.js', '/admin/assets/session-timer.js', '/admin/assets/console-audit.js']);
  expect(await page.evaluate(() => localStorage.length + sessionStorage.length)).toBe(0);

  await expectNoSeriousAxeFindings(page, 'audit_list');
  await shot(page, 'audit_list_desktop');
});

test('filters ask the server: a range of days, several actions, an entity type; each chip removes its own; reload, Back and a copied link bring the view back', async ({ page }) => {
  test.setTimeout(60_000);
  await signIn(page);
  await open(page, AUDIT);
  const asked = documents(page);

  // A range of days goes with "Apply filters".
  await page.getByLabel('From', { exact: true }).fill('2025-03-01');
  await page.getByLabel('To', { exact: true }).fill('2025-03-31');
  await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?${MARCH}`);
  await expect(rows(page)).toHaveCount(7);
  await expect(page.locator('#console-status')).toHaveText('7 results');
  await expect(page.locator('[aria-live]')).toHaveCount(1);
  expect(await column(page, 'Action')).toEqual(['sign_out', 'entity_edit', 'entity_delete', 'entity_edit', 'login_success', 'login_failure', 'login_failure']);
  expect(await column(page, 'Outcome')).toEqual(['Success', 'Success', 'Success', 'Success', 'Success', 'Denied', 'Failure']);
  expect(await column(page, 'Entity type')).toEqual(['—', 'Offer', 'Participant', 'Participant', '—', '—', '—']);

  // An action asks at once; a second one is added to it.
  // A box that moves the page as it changes is clicked, not check()ed: check() reads the box
  // again after the click, and by then the page it reads is the one being left or the new one.
  // The new page's own box, written by the server from the address, is what is checked.
  await page.getByLabel('login_failure', { exact: true }).click();
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?action=login_failure&${MARCH}`);
  await expect(page.getByLabel('login_failure', { exact: true })).toBeChecked();
  await expect(page.getByLabel('login_failure', { exact: true })).toBeFocused();
  await expect(rows(page)).toHaveCount(2);
  // The actor of a failed sign-in is the address that was typed, shown as text.
  expect(await column(page, 'Actor')).toEqual([audit.typed, audit.typed]);
  await settled(page);
  await page.getByLabel('entity_edit', { exact: true }).click();
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?action=login_failure%2Centity_edit&${MARCH}`);
  await expect(page.getByLabel('entity_edit', { exact: true })).toBeChecked();
  await expect(page.getByLabel('entity_edit', { exact: true })).toBeFocused();
  await expect(rows(page)).toHaveCount(4);
  await expect(page.locator('#console-status')).toHaveText('4 results');

  // The entity type.
  await page.getByLabel('Entity type', { exact: true }).selectOption('Participant');
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?action=login_failure%2Centity_edit&entityType=Participant&${MARCH}`);
  await expect(page.locator('#entityType')).toBeFocused();
  await expect(rows(page)).toHaveCount(1);
  expect(await column(page, 'Entity ID')).toEqual([entities.participants['Alex Rivera'].id]);

  // Chips: one per filter; removing one drops only that one.
  const chips = page.locator('#chips .chip span');
  await expect(chips).toHaveText(['Action: login_failure, entity_edit', 'Entity type: Participant', 'From: Mar 1, 2025', 'To: Mar 31, 2025']);
  await settled(page);
  await page.getByRole('link', { name: 'Remove Entity type filter', exact: true }).click();
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?action=login_failure%2Centity_edit&${MARCH}`);
  await expect(chips).toHaveText(['Action: login_failure, entity_edit', 'From: Mar 1, 2025', 'To: Mar 31, 2025']);
  await expect(page.locator('main')).toBeFocused();
  await expect(rows(page)).toHaveCount(4);

  // Each change was one request for one page.
  expect(asked).toEqual([
    `${AUDIT}?${MARCH}`,
    `${AUDIT}?action=login_failure&${MARCH}`,
    `${AUDIT}?action=login_failure%2Centity_edit&${MARCH}`,
    `${AUDIT}?action=login_failure%2Centity_edit&entityType=Participant&${MARCH}`,
    `${AUDIT}?action=login_failure%2Centity_edit&${MARCH}`,
  ]);

  // The view comes back: reload, Back, a copied link.
  const state = here(page);
  await page.reload();
  await expectStyled(page);
  expect(here(page)).toBe(state);
  await expect(page.getByLabel('login_failure', { exact: true })).toBeChecked();
  await expect(page.getByLabel('entity_edit', { exact: true })).toBeChecked();
  await expect(page.locator('fieldset.statuses input:checked')).toHaveCount(2);
  await expect(page.getByLabel('From', { exact: true })).toHaveValue('2025-03-01');
  await expect(page.getByLabel('To', { exact: true })).toHaveValue('2025-03-31');
  await page.goto('/admin/');
  await page.goBack();
  await expectStyled(page);
  expect(here(page)).toBe(state);
  await expect(rows(page)).toHaveCount(4);
  const copy = await page.context().newPage();
  await answerCloudflare(copy, { token: PASS_TOKEN });
  await copy.goto(state);
  await expect(copy.locator('#list-table tbody tr')).toHaveCount(4);
  await expect(copy.locator('#chips .chip span')).toHaveText(['Action: login_failure, entity_edit', 'From: Mar 1, 2025', 'To: Mar 31, 2025']);
  await copy.close();

  await page.getByRole('link', { name: 'Clear all filters', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + AUDIT);
  await expect(page.locator('#chips')).toHaveCount(0);
  await expect(page.locator('fieldset.statuses input:checked')).toHaveCount(0);
});

test('paging: 50, 100 or 200 a page with the range, Previous, Next and Page N; a change goes back to page 1; past the end shows the last page', async ({ page }) => {
  test.setTimeout(60_000);
  await signIn(page);
  await open(page, `${AUDIT}?${BULK_DAY}`);
  await expect(page.locator('#list-range')).toHaveText('Showing 1–50 of 130');
  await expect(rows(page)).toHaveCount(50);
  await expect(page.getByRole('link', { name: 'Previous', exact: true })).toHaveCount(0);

  await page.getByRole('link', { name: 'Next', exact: true }).click();
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?${BULK_DAY}&offset=50`);
  await expect(page.getByRole('link', { name: 'Next', exact: true })).toBeFocused();
  await expect(page.locator('#list-range')).toHaveText('Showing 51–100 of 130');
  await expect(page.getByRole('link', { name: 'Page 2', exact: true })).toHaveAttribute('aria-current', 'page');
  await expect(page.getByRole('link', { name: 'Previous', exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Page 3', exact: true })).toBeVisible();
  // Newest first across the pages: the second page goes on where the first stopped.
  const times = await rows(page).locator('time').evaluateAll((els) => els.map((el) => el.getAttribute('datetime') ?? ''));
  expect(times[0]).toBe('2025-02-10T15:01:19Z');
  expect(times).toEqual([...times].sort().reverse());

  // The page size, then a filter: each back to page 1.
  await page.locator('#limit').selectOption('100');
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?${BULK_DAY}&limit=100`);
  await expect(page.locator('#limit')).toBeFocused();
  await expect(rows(page)).toHaveCount(100);
  await settled(page);
  await page.getByRole('link', { name: 'Page 2', exact: true }).click();
  // The address without the focus mark: the new page's script has run, so its controls answer.
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?${BULK_DAY}&offset=100&limit=100`);
  await expect(page.locator('#list-range')).toHaveText('Showing 101–130 of 130');
  await expect(page.getByRole('link', { name: 'Next', exact: true })).toHaveCount(0);
  await settled(page);
  // Clicked, not check()ed: the box moves the page (see the filters test).
  await page.getByLabel('sign_out', { exact: true }).click();
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?action=sign_out&${BULK_DAY}&limit=100`);
  await expect(page.getByLabel('sign_out', { exact: true })).toBeChecked();
  await expect(page.locator('#list-range')).toHaveText('Showing 1–65 of 65');

  // A page past the last shows the last; a size over 200 shows 200, and never asks for more.
  await open(page, `${AUDIT}?${BULK_DAY}&offset=99999`);
  await expect(page.locator('#list-range')).toHaveText('Showing 101–130 of 130');
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?${BULK_DAY}&offset=100`);
  await open(page, `${AUDIT}?${BULK_DAY}&limit=500`);
  await expect(page.locator('#limit')).toHaveValue('200');
  await expect(rows(page)).toHaveCount(130);
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?${BULK_DAY}&limit=200`);

  // Reload restores the page of rows.
  await open(page, `${AUDIT}?${BULK_DAY}&offset=100`);
  await page.reload();
  await expectStyled(page);
  expect(here(page)).toBe(`${AUDIT}?${BULK_DAY}&offset=100`);
  await expect(page.locator('#list-range')).toHaveText('Showing 101–130 of 130');
});

test('an entry: its fields, before and after with "Changed" in words on what differs, the IP prefix here only, nothing that changes anything; axe; a screenshot', async ({ page }) => {
  test.setTimeout(60_000);
  await signIn(page);
  const list = `${AUDIT}?action=entity_edit&${MARCH}`;
  await open(page, list);
  const row = rows(page).filter({ has: page.locator(`a[href^="${AUDIT}/${audit.entries.edit}"]`) });
  await expect(row).toHaveCount(1);
  // The Entity ID of a record that exists links to its record.
  const person = entities.participants['Alex Rivera'].id;
  await expect(row.locator('a.id')).toHaveText(person);
  await expect(row.locator('a.id')).toHaveAttribute('href', `/admin/participants/${person}`);

  await row.locator('a.entry').click();
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}/${audit.entries.edit}?action=entity_edit&${MARCH}`);
  await expectStyled(page);
  await expect(page).toHaveTitle('Audit entry · Dealoware admin');
  await expect(page.locator('h1')).toHaveText('Audit entry');
  await expect(page.getByRole('navigation', { name: 'Admin' }).locator('[aria-current="page"]')).toHaveText('Audit log');

  const entry = page.locator('#entry');
  await expect(entry.locator('dt')).toHaveText(['Time (ET)', 'Actor', 'Action', 'Entity type', 'Entity ID', 'Outcome', 'IP HMAC prefix']);
  await expect(entry.locator('dd')).toHaveText(['Mar 3, 2025, 11:30 AM ET', audit.admin, 'entity_edit', 'Participant', person, 'Success', audit.prefix]);
  await expect(entry.locator('time')).toHaveAttribute('title', '2025-03-03 16:30 UTC');
  // The first twelve characters of the stored value, and none of the rest.
  await expect(page.locator('#ip-prefix')).toHaveText(audit.prefix);
  expect(audit.prefix).toHaveLength(12);
  expect(await page.content()).not.toContain(audit.rest);

  // Before and after, side by side as text; "Changed" in words on the two fields that differ, on both sides.
  await expect(page.getByRole('heading', { name: 'Before and after', exact: true })).toBeVisible();
  const before = page.locator('#snapshot-before');
  const after = page.locator('#snapshot-after');
  await expect(before.getByRole('heading')).toHaveText('Before');
  await expect(after.getByRole('heading')).toHaveText('After');
  const fields = ['DisplayName', 'ContactEmail', 'ParticipantActive'];
  await expect(before.locator('dt')).toHaveText(fields);
  await expect(after.locator('dt')).toHaveText(fields);
  await expect(before.locator('.free-text')).toHaveText(['Alex Rivers', 'alex.rivera.contact@example.test', 'false']);
  await expect(after.locator('.free-text')).toHaveText(['Alex Rivera', 'alex.rivera.contact@example.test', 'true']);
  for (const side of [before, after]) {
    const marked = side.locator('.detail').filter({ has: page.locator('.badge-changed') });
    await expect(marked.locator('dt')).toHaveText(['DisplayName', 'ParticipantActive']);
    await expect(side.locator('.badge-changed')).toHaveText(['Changed', 'Changed']);
    for (const marker of await side.locator('.badge-changed').all()) await expect(marker).toBeVisible();
  }
  const [beforeBox, afterBox] = [await before.boundingBox(), await after.boundingBox()];
  expect(Math.round(beforeBox!.y)).toBe(Math.round(afterBox!.y));
  expect(afterBox!.x).toBeGreaterThan(beforeBox!.x + beforeBox!.width - 1);

  // The stored row also holds a field the field policy denies and a key that looks like a credential: neither is on the page.
  const html = await page.content();
  for (const hidden of ['StrategyBody', 'DENIED-FIELD', 'passwordHash', 'CREDENTIAL-LOOKING']) expect(html).not.toContain(hidden);

  // TD-ADM-101 step 2: read, never change.
  await expectReadOnly(page);
  await expect(page.locator('[aria-live]')).toHaveCount(1);
  await expectNoSeriousAxeFindings(page, 'audit_entry');
  await shot(page, 'audit_entry_desktop');

  // The record the entry names opens from it.
  await entry.locator('a.id').click();
  await expect(page).toHaveURL(`${ORIGIN}/admin/participants/${person}`);
  await expect(page.locator('h1')).toHaveText('Participant details');
  await page.goBack();
  await expectStyled(page);
  await expect(page.locator('h1')).toHaveText('Audit entry');

  // Back to the list as it was.
  // The link moves the page: wait for the list's address before reading the page.
  await page.getByRole('link', { name: 'Back to audit log', exact: true }).click();
  await expect.poll(() => here(page)).toBe(list);
  await expectStyled(page);
  await expect(rows(page)).toHaveCount(2);
  await expect(page.getByLabel('entity_edit', { exact: true })).toBeChecked();
});

test('a deleted record\'s id opens nothing; an authentication entry has no before and after; every time is ET with UTC on hover and on focus', async ({ page }) => {
  await signIn(page);
  const gone = entities.participants['Robin Hart'].id;

  // In the list: the id as text with "Deleted", not a link.
  await open(page, `${AUDIT}?action=entity_delete&${MARCH}`);
  await expect(rows(page)).toHaveCount(1);
  const cell = rows(page).first().locator('td').nth(4);
  await expect(cell.locator('span.id')).toHaveText(gone);
  await expect(cell.locator('.badge')).toHaveText('Deleted');
  await expect(cell.locator('a')).toHaveCount(0);
  await expect(page.locator(`a[href^="/admin/participants/${gone}"]`)).toHaveCount(0);

  // On the entry: the same; what was there before, and nothing marked changed.
  await rows(page).first().locator('a.entry').click();
  await expect(page.locator('h1')).toHaveText('Audit entry');
  await expectStyled(page);
  await expect(page.locator('#entry span.id').first()).toHaveText(gone);
  await expect(page.locator('#entry .badge-deleted')).toHaveText('Deleted');
  await expect(page.locator('main a.id')).toHaveCount(0);
  await expect(page.locator('#snapshot-before .free-text')).toHaveText(['Robin Hart', 'true']);
  await expect(page.locator('#snapshot-after')).toContainText('No allowed fields.');
  await expect(page.locator('.badge-changed')).toHaveCount(0);
  await expectReadOnly(page);
  await expectNoSeriousAxeFindings(page, 'audit_entry_deleted-record');
  // And the record's own page is not there (without "Show deleted").
  const record = await page.goto(`/admin/participants/${gone}`);
  expect(record!.status()).toBe(404);
  await expect(page.locator('h1')).toHaveText(NOT_FOUND);

  // An authentication entry: who, what, the outcome and its reason; no snapshots.
  await open(page, `${AUDIT}/${audit.entries.locked}`);
  await expect(page.locator('#entry dt')).toHaveText(['Time (ET)', 'Actor', 'Action', 'Entity type', 'Entity ID', 'Outcome', 'Reason', 'IP HMAC prefix']);
  await expect(page.locator('#entry dd')).toHaveText(['Mar 2, 2025, 10:01 AM ET', audit.typed, 'login_failure', '—', '—', 'Denied', 'locked', audit.prefix]);
  await expect(page.locator('#snapshots')).toHaveCount(0);
  await expectReadOnly(page);
  // The typed address is text on the page, and nowhere else: not the title, not the address.
  await expect(page).toHaveTitle('Audit entry · Dealoware admin');
  expect(page.url()).not.toContain('mallory');
  await expectNoSeriousAxeFindings(page, 'audit_entry_sign-in');

  // B15: a summer time and a winter time, each with its UTC value.
  await open(page, `${AUDIT}/${audit.entries.summer}`);
  const summer = page.locator('#entry time');
  await expect(summer).toHaveText('Jul 1, 2025, 12:30 PM ET');
  await expect(summer).toHaveAttribute('title', '2025-07-01 16:30 UTC');
  await expect(summer).toHaveAttribute('datetime', '2025-07-01T16:30:00Z');
  await summer.focus();
  await expect(summer).toBeFocused();
  // (Firefox reports the rule, attr(data-utc), rather than the text it draws.)
  expect(['"2025-07-01 16:30 UTC"', 'attr(data-utc)']).toContain(await summer.evaluate((el) => getComputedStyle(el, '::after').content));
  await open(page, `${AUDIT}?action=login_success&${MARCH}`);
  const winter = rows(page).first().locator('time');
  await expect(winter).toHaveText('Mar 2, 2025, 10:05 AM ET');
  await expect(winter).toHaveAttribute('title', '2025-03-02 15:05 UTC');
});

test('a filter miss says so with Clear filters; an empty log says "No audit entries yet."; an id no entry has is S-D3; axe on each', async ({ page }) => {
  await signIn(page);
  await open(page, `${AUDIT}?entityType=Artifact&${MARCH}`);
  await expect(page.locator('#list-miss')).toHaveText('No results match these filters.');
  await expect(page.locator('#list-empty')).toHaveCount(0);
  await expect(page.locator('#pager')).toHaveCount(0);
  await expect(page.locator('#chips .chip span')).toHaveText(['Entity type: Artifact', 'From: Mar 1, 2025', 'To: Mar 31, 2025']);
  await expectNoSeriousAxeFindings(page, 'audit_filter-miss');
  await page.getByRole('link', { name: 'Clear filters', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + AUDIT);
  await expect(page.locator('#list-range')).toHaveText(/^Showing 1–50 of [\d,]+$/);

  // The log with nothing in it: only a run's own database can be emptied (seed_audit.py).
  try {
    seedAudit('empty');
    await open(page, AUDIT);
    await expect(page.locator('#list-empty')).toHaveText('No audit entries yet.');
    await expect(page.locator('#list-miss')).toHaveCount(0);
    await expect(page.locator('#pager')).toHaveCount(0);
    await expect(rows(page)).toHaveCount(1);
    await expectNoSeriousAxeFindings(page, 'audit_empty');
    // Reading the log wrote nothing into it.
    await page.reload();
    await expectStyled(page);
    await expect(page.locator('#list-empty')).toHaveText('No audit entries yet.');
  } finally {
    audit = seedAudit();
  }

  // S-D3: an id no entry has, and something that is not an id.
  for (const address of [`${AUDIT}/0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a59`, `${AUDIT}/not-an-id`, `${AUDIT}/${audit.entries.edit}/edit`]) {
    const missing = await page.goto(address);
    expect(missing!.status()).toBe(404);
    await expect(page.locator('h1')).toHaveText(NOT_FOUND);
    await expect(page).toHaveTitle('Page not found · Dealoware admin');
    await expect(page.getByRole('link', { name: 'Go to Stats', exact: true })).toBeVisible();
  }
});

test('the list and an entry at 320 px: nothing scrolls the page sideways, the table scrolls in its own region; axe; screenshots', async ({ page }) => {
  // Every control on the page is measured: more than the default 30 s in the slower engines.
  test.setTimeout(90_000);
  await signIn(page);
  await page.setViewportSize({ width: 320, height: 900 });
  await open(page, `${AUDIT}?action=login_failure,entity_edit&entityType=Participant&${MARCH}`);
  await expectNoSidewaysScroll(page);
  const region = page.locator('#list-region');
  expect(await region.evaluate((el) => el.scrollWidth > el.clientWidth)).toBe(true);
  // Every control is at least 24 by 24 CSS px.
  for (const target of await page.locator('main a:visible, main button:visible, main input:visible, main select:visible').all()) {
    // A link in a table cell is inline text: the WCAG 2.5.8 inline exception.
    if (await target.evaluate((el) => el.closest('#list-table tbody') !== null)) continue;
    const box = await target.boundingBox();
    if (!box || box.width === 0) continue;
    expect(Math.round(box.height)).toBeGreaterThanOrEqual(24);
    expect(Math.round(box.width)).toBeGreaterThanOrEqual(24);
  }
  await expectNoSeriousAxeFindings(page, 'audit_list_320');
  await shot(page, 'audit_list_320');

  // The entry: before and after one under the other.
  await open(page, `${AUDIT}/${audit.entries.edit}`);
  await expectNoSidewaysScroll(page);
  const [before, after] = [await page.locator('#snapshot-before').boundingBox(), await page.locator('#snapshot-after').boundingBox()];
  expect(after!.y).toBeGreaterThan(before!.y + before!.height - 1);
  await expect(page.locator('.badge-changed')).toHaveCount(4);
  await expectNoSeriousAxeFindings(page, 'audit_entry_320');
  await shot(page, 'audit_entry_320');
});

test('keyboard only: skip link, the nav to Audit log, a filter and its chip, a page, an entry, and back', async ({ page }) => {
  test.setTimeout(60_000);
  await signIn(page);
  await open(page, '/admin/');
  const skip = page.getByRole('link', { name: 'Skip to main content', exact: true });
  const auditLink = page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Audit log', exact: true });
  if (tabStopsAtLinks()) {
    await page.keyboard.press('Tab');
    await expect(skip).toBeFocused();
    // Through the nav to Audit log: Stats, Participants, Artifacts, Negotiations, Offers, Audit log.
    for (let i = 0; i < 6; i += 1) await page.keyboard.press('Tab');
    await expect(auditLink).toBeFocused();
  } else {
    await auditLink.focus();
    await expect(auditLink).toBeFocused();
  }
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + AUDIT);
  await expectStyled(page);

  // The skip link leads into main; the first action is the first control there.
  const firstAction = page.locator('#action-login_success');
  if (tabStopsAtLinks()) {
    await page.keyboard.press('Tab');
    await expect(skip).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.locator('main')).toBeFocused();
    await page.keyboard.press('Tab');
  } else {
    await firstAction.focus();
  }
  await expect(firstAction).toBeFocused();

  // A filter with Space, and its chip removed with Enter.
  await page.keyboard.press('Space');
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?action=login_success`);
  await expect(firstAction).toBeFocused();
  await expect(firstAction).toBeChecked();
  await page.getByRole('link', { name: 'Remove Action filter', exact: true }).focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + AUDIT);
  await expect(page.locator('main')).toBeFocused();

  // A page.
  await page.getByRole('link', { name: 'Next', exact: true }).focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?offset=50`);
  await expect(page.getByRole('link', { name: 'Next', exact: true })).toBeFocused();

  // An entry, and back.
  await rows(page).first().locator('a.entry').focus();
  await page.keyboard.press('Enter');
  await expect(page.locator('h1')).toHaveText('Audit entry');
  await page.getByRole('link', { name: 'Back to audit log', exact: true }).focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(`${ORIGIN}${AUDIT}?offset=50`);
  // A page just loaded: until its stylesheet applies, a focused box wears the browser's own ring.
  await expectStyled(page);

  // The page's 2px ring shows on the controls this walk used, in every engine.
  for (const [name, locator] of [
    ['an action', page.locator('#action-login_success')],
    ['entity type', page.locator('#entityType')],
    ['From', page.locator('#from')],
    ['page size', page.locator('#limit')],
    ['Apply filters', page.locator('#apply-filters')],
    ['an entry\'s link', rows(page).first().locator('a.entry')],
    ['Next', page.getByRole('link', { name: 'Next', exact: true })],
  ] as const) {
    await locator.focus();
    await expect(locator).toBeFocused();
    expect(await locator.evaluate((el) => {
      const style = getComputedStyle(el);
      return `${style.outlineStyle} ${style.outlineWidth}`;
    }), `the focus ring on ${name}`).toBe('solid 2px');
  }
});

test('the audit seeding script deletes entries only in this run\'s own database', async () => {
  const elsewhere = fs.mkdtempSync(path.join(os.tmpdir(), 'dw-not-a-run-'));
  try {
    // A database file with the right name, in a folder the runner did not mark.
    const unmarked = path.join(elsewhere, 'e2e.db');
    fs.writeFileSync(unmarked, 'not a database, and it must stay as it is');
    // A marked folder, but another file in it.
    const markedDir = path.join(elsewhere, 'marked');
    fs.mkdirSync(markedDir);
    fs.writeFileSync(path.join(markedDir, '.dealoware-e2e-run'), 'dealoware e2e run\n');
    const otherName = path.join(markedDir, 'dealoware.db');
    fs.writeFileSync(otherName, 'not a database either');
    // A marker with other words in it.
    const wrongMarkerDir = path.join(elsewhere, 'wrong-marker');
    fs.mkdirSync(wrongMarkerDir);
    fs.writeFileSync(path.join(wrongMarkerDir, '.dealoware-e2e-run'), 'something else\n');
    const wrongMarker = path.join(wrongMarkerDir, 'e2e.db');
    fs.writeFileSync(wrongMarker, 'nor this');

    const refusedPaths: unknown[] = [
      unmarked, otherName, wrongMarker, path.join(markedDir, 'e2e.db'), path.join(elsewhere, 'missing', 'e2e.db'),
      '', null, 42,
    ];
    for (const db of refusedPaths) {
      for (const mode of ['seed', 'empty', 'remove']) {
        const run = runAuditSeedScript({ db, mode });
        expect(run.status, `db = ${JSON.stringify(db)}, mode = ${mode}`).toBe(1);
        expect(run.stderr).toContain('only works on the database of a browser-test run');
      }
    }
    expect(fs.readFileSync(unmarked, 'utf8')).toBe('not a database, and it must stay as it is');
    expect(fs.readFileSync(otherName, 'utf8')).toBe('not a database either');
    expect(fs.readFileSync(wrongMarker, 'utf8')).toBe('nor this');
    // A refused path is not created either.
    expect(fs.existsSync(path.join(markedDir, 'e2e.db'))).toBe(false);
    expect(fs.existsSync(path.join(elsewhere, 'missing'))).toBe(false);
  } finally {
    fs.rmSync(elsewhere, { recursive: true, force: true });
  }

  // Asked for something it does not do, or given ids that are not ids, it refuses too.
  const odd = runAuditSeedScript({ db: DB, mode: 'truncate' });
  expect(odd.status).toBe(1);
  expect(odd.stderr).toContain('mode may be seed, empty or remove');
  const noIds = runAuditSeedScript({ db: DB, mode: 'seed', participant: 'x', deleted: 'y', offer: 'z' });
  expect(noIds.status).toBe(1);
  expect(noIds.stderr).toContain('must be the id of a seeded record');
});

test.describe('JavaScript off', () => {
  test.use({ javaScriptEnabled: false });

  test('with JavaScript off the viewer still works: Apply filters with several actions, a chip, an entry and back, a page', async ({ page }) => {
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

    await page.goto(`${AUDIT}?${MARCH}`);
    await expect(rows(page)).toHaveCount(7);
    await page.getByLabel('login_failure', { exact: true }).check();
    await page.getByLabel('sign_out', { exact: true }).check();
    await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
    // The browser sends the form as it is (action twice); the server reads it as the same list.
    await expect(page.locator('#chips')).toContainText('Action: login_failure, sign_out');
    await expect(rows(page)).toHaveCount(3);
    await expect(rows(page).locator('a.entry')).toHaveText(['sign_out', 'login_failure', 'login_failure']);

    await page.getByRole('link', { name: 'Remove Action filter', exact: true }).click();
    await expect(rows(page)).toHaveCount(7);
    await expect(page.locator('#chips')).toContainText('From: Mar 1, 2025');

    // An entry and back.
    await rows(page).nth(3).locator('a.entry').click();
    await expect(page.locator('h1')).toHaveText('Audit entry');
    await expect(page.locator('.badge-changed')).toHaveText(['Changed', 'Changed', 'Changed', 'Changed']);
    await page.getByRole('link', { name: 'Back to audit log', exact: true }).click();
    await expect(rows(page)).toHaveCount(7);

    // A page.
    await page.goto(`${AUDIT}?${BULK_DAY}`);
    await page.getByRole('link', { name: 'Next', exact: true }).click();
    await expect(page.locator('#list-range')).toHaveText('Showing 51–100 of 130');
  });
});
