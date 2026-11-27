import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { test, expect, type Locator, type Page, type Request } from '@playwright/test';
import { expectNoSeriousAxeFindings } from './axe';
import { collectPageErrors } from './page-errors';
import { runEntitySeedScript, seedEntities, type Seeded } from './entities';
import { authenticatorCode, seedOwner, type Owner } from '../sign-in/owner';
import { answerCloudflare } from '../signed-out/cloudflare';

/**
 * The four list pages (S-B2 to S-B5) and their detail pages (S-B6) against
 * the real app (T04 piece 2), signed in through the real pages, over the
 * entities seed_entities.py writes into the run's database. Start it with
 * e2e/console/run.sh.
 *
 * The server writes each list from its address; these tests prove what a
 * browser adds: the script that moves to a new address and keeps the focus,
 * the pickers, the live region, the busy table, keyboard use, the tooltip of
 * a time, layout at 320 px, and axe on every list and detail state. What the
 * server answers for each address is proven by the .NET tests
 * (AdminListPageTests). Case ids: TD-ADM-UI-na-B01 to B09, B14, B15, X04,
 * -axe and -kbd; TD-ADM-130 (the list part of the smoke).
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const API_SIGN_IN = '/admin/api/auth/sign-in';
const API_CODE = '/admin/api/auth/sign-in/code';
const PASS_TOKEN = process.env.CONSOLE_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.CONSOLE_E2E_ARTIFACTS;

const LISTS = [
  {
    path: '/admin/participants', name: 'participants', heading: 'Participants',
    columns: ['Name', 'ID', 'Status', 'Created', 'Updated'],
    sorts: ['name', 'created', 'updated'],
    statuses: ['Active', 'Suspended'],
    search: 'Search by display name or Core identifier',
  },
  {
    path: '/admin/artifacts', name: 'artifacts', heading: 'Artifacts',
    columns: ['Name', 'Owner participant', 'Created', 'Updated'],
    sorts: ['name', 'created', 'updated'],
    statuses: [],
    search: 'Search by artifact name',
  },
  {
    path: '/admin/negotiations', name: 'negotiations', heading: 'Negotiations',
    columns: ['ID', 'Artifact', 'Participants', 'Status', 'Created', 'Updated'],
    sorts: ['status', 'created', 'updated'],
    statuses: ['Open', 'Closed', 'Expired'],
    search: 'Search by artifact or participant name',
  },
  {
    path: '/admin/offers', name: 'offers', heading: 'Offers',
    columns: ['ID', 'Negotiation ID', 'Artifact', 'Offering participant', 'Amount', 'Status', 'Created', 'Updated'],
    sorts: ['amount', 'status', 'created', 'updated'],
    statuses: ['Open', 'Accepted', 'Declined', 'Withdrawn', 'Superseded (countered)', 'Cancelled'],
    search: 'Search by artifact, offering participant, or negotiation id',
  },
];

let owner: Owner;
let seeded: Seeded;
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

/** Opens a list (or any page) by its address and waits for it to be whole. */
async function open(page: Page, address: string): Promise<void> {
  await page.goto(address);
  await expectStyled(page);
}

/**
 * After a move, before the pointer acts on the new page: the page has said its count in the
 * live region. console-list.js does that 100 ms after the page loads, and it is the last
 * thing the page does as it loads, so only then is the page whole for the pointer. (The
 * banner's space is kept from the start, so the count moves nothing below it: see the test
 * "the count said after a move moves nothing".)
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

/** The heading cells' words, without the sort mark and the words only screen readers get. */
async function headings(page: Page): Promise<string[]> {
  return page.locator('#list-table thead th').evaluateAll((cells) => cells.map((cell) => {
    const copy = cell.cloneNode(true) as HTMLElement;
    copy.querySelectorAll('.sort-mark, .visually-hidden').forEach((n) => n.remove());
    return (copy.textContent ?? '').trim();
  }));
}

/** The column of the given heading, as text, row by row. */
async function column(page: Page, heading: string): Promise<string[]> {
  const index = (await headings(page)).indexOf(heading);
  expect(index).toBeGreaterThanOrEqual(0);
  return rows(page).evaluateAll((trs, i) => trs.map((tr) => (tr.children[i as number]?.textContent ?? '').trim()), index);
}

function amountOf(text: string): number {
  return Number(text.replace(/[^0-9.]/g, ''));
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

/** The total a list shows, from its range line ("Showing 1–50 of 1,234"), or 0 when it has none. */
async function totalOf(page: Page): Promise<number> {
  const range = page.locator('#list-range');
  if (await range.count() === 0) return 0;
  const text = (await range.textContent()) ?? '';
  return Number(text.slice(text.lastIndexOf(' of ') + 4).replace(/,/g, ''));
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

test('each list: the locked columns in order, Updated newest first, its own statuses and controls; axe; a screenshot', async ({ page }) => {
  test.setTimeout(90_000);
  await signIn(page);
  for (const list of LISTS) {
    await page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: list.heading, exact: true }).click();
    await expect(page).toHaveURL(ORIGIN + list.path);
    await expectStyled(page);
    await expect(page).toHaveTitle(`${list.heading} · Dealoware admin`);

    // B01: the columns, in order; the default order is Updated, newest first.
    expect(await headings(page)).toEqual(list.columns);
    const sorted = page.locator('#list-table th[aria-sort]');
    await expect(sorted).toHaveCount(1);
    await expect(sorted).toHaveAttribute('aria-sort', 'descending');
    await expect(sorted.locator('button')).toHaveAttribute('id', 'sort-updated');
    await expect(rows(page)).toHaveCount(Math.min(50, await totalOf(page)));

    // B02: the sortable headings are buttons in th scope="col"; the others are not.
    const buttons = await page.locator('#list-table th[scope="col"] button.sort-button').evaluateAll((els) => els.map((e) => e.id.replace('sort-', '')));
    expect(buttons.sort()).toEqual([...list.sorts].sort());

    // B03: exactly this list's statuses; never Deleted.
    const statusLabels = await page.locator('fieldset.statuses label').allTextContents();
    expect(statusLabels.sort()).toEqual([...list.statuses].sort());
    await expect(page.getByLabel('Deleted', { exact: true })).toHaveCount(0);
    // B04: amount only on Offers.
    await expect(page.locator('#amountMin')).toHaveCount(list.name === 'offers' ? 1 : 0);
    // B07: the search names what it matches.
    await expect(page.getByLabel(list.search, { exact: true })).toBeVisible();
    // B06 and B08: the page size and the toggle.
    expect(await page.locator('#limit option').allTextContents()).toEqual(['50', '100', '200']);
    await expect(page.locator('#limit')).toHaveValue('50');
    await expect(page.getByLabel('Show deleted', { exact: true })).not.toBeChecked();
    // B06: the range.
    await expect(page.locator('#list-range')).toHaveText(/^Showing 1–\d+ of [\d,]+$/);

    await expectNoSeriousAxeFindings(page, `list_${list.name}`);
    await shot(page, `list_${list.name}_desktop`);
  }
});

test('a heading sorts on the server: one request, the order turns, aria-sort moves, the focus stays on the heading', async ({ page }) => {
  await signIn(page);
  await open(page, '/admin/offers');
  const asked = documents(page);

  await page.locator('#sort-amount').click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?sort=amount');
  await expect(page.locator('#sort-amount')).toBeFocused();
  expect(asked.filter((p) => p.startsWith('/admin/offers'))).toHaveLength(1);
  await expect(page.locator('#list-table th[aria-sort]')).toHaveCount(1);
  await expect(page.locator('#list-table th[aria-sort="descending"] #sort-amount')).toHaveCount(1);
  let amounts = (await column(page, 'Amount')).map(amountOf);
  expect(amounts).toEqual([...amounts].sort((a, b) => b - a));
  await expect(page.locator('#sort-amount .visually-hidden')).toHaveText(' (sorted descending)');

  // The same heading again: the other way.
  await settled(page);
  await page.locator('#sort-amount').click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?sort=amount&dir=asc');
  amounts = (await column(page, 'Amount')).map(amountOf);
  expect(amounts).toEqual([...amounts].sort((a, b) => a - b));
  await expect(page.locator('#list-table th[aria-sort="ascending"] #sort-amount')).toHaveCount(1);

  // By keyboard: Status, from the button itself.
  await page.locator('#sort-status').focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?sort=status');
  await expect(page.locator('#sort-status')).toBeFocused();
  await expect(page.locator('#list-table th[aria-sort] #sort-status')).toHaveCount(1);
});

test('Participants filter by status; the Artifacts owner picker asks for one page of matches as the owner types', async ({ page }) => {
  await signIn(page);
  await open(page, '/admin/participants');
  // A box that moves the page as it changes is clicked, not check()ed: check() reads the box
  // again after the click, and by then the page it reads is the one being left or the new one.
  // The new page's own box, written by the server from the address, is what is checked.
  await page.getByLabel('Suspended', { exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/participants?status=Suspended');
  await expect(page.getByLabel('Suspended', { exact: true })).toBeChecked();
  await expect(page.getByLabel('Suspended', { exact: true })).toBeFocused();
  const badges = await page.locator('#list-table tbody .badge').allTextContents();
  expect(new Set(badges)).toEqual(new Set(['Suspended']));
  expect(await totalOf(page)).toBe(21);
  await settled(page);
  await page.getByLabel('Active', { exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/participants?status=Active%2CSuspended');
  await expect(page.getByLabel('Active', { exact: true })).toBeChecked();
  expect(await totalOf(page)).toBe(seeded.counts.participants);
  await expect(page.locator('#chips')).toContainText('Status: Active, Suspended');

  // The picker: nothing is asked for until the owner types; then one page, ten at most.
  const apiCalls: string[] = [];
  page.on('request', (r) => {
    const url = new URL(r.url());
    // The shell's own session status read (session-timer.js) is not the list's.
    if (url.pathname.startsWith('/admin/api/') && url.pathname !== '/admin/api/session') apiCalls.push(url.pathname + url.search);
  });
  await open(page, '/admin/artifacts');
  expect(apiCalls).toEqual([]);
  const box = page.getByRole('combobox', { name: 'Owner participant' });
  await box.pressSequentially('Seed participant 0', { delay: 20 });
  const options = page.locator('#participant-options [role="option"]');
  await expect(options).toHaveCount(10);
  // Asked for after a pause in typing, never for more than one page of ten.
  expect(apiCalls.length).toBeGreaterThanOrEqual(1);
  expect(apiCalls[apiCalls.length - 1]).toBe('/admin/api/participants?q=Seed%20participant%200&limit=10&sort=name&dir=asc');
  await expect(box).toHaveAttribute('aria-expanded', 'true');

  await box.fill('');
  await box.pressSequentially('Blair', { delay: 20 });
  await expect(options).toHaveText(['Blair Chen']);
  await page.keyboard.press('ArrowDown');
  await expect(box).toHaveAttribute('aria-activedescendant', 'participant-options-0');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/artifacts?participant=' + encodeURIComponent(seeded.participants['Blair Chen'].sub));
  expect(await column(page, 'Name')).toEqual(['Oak desk']);
  await expect(page.getByRole('combobox', { name: 'Owner participant' })).toHaveValue('Blair Chen');
  await expect(page.getByRole('combobox', { name: 'Owner participant' })).toBeFocused();
  await expect(page.locator('#chips')).toContainText('Owner participant: Blair Chen');
  for (const call of apiCalls) expect(new URL(call, ORIGIN).searchParams.get('limit')).toBe('10');

  // Typed text that was not chosen is not a choice: Enter sends no owner at all,
  // never Blair Chen under the new words.
  await box.press('End');
  await box.pressSequentially(' Jr', { delay: 20 });
  await box.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/artifacts');
  await expect(page.locator('#chips')).toHaveCount(0);
  await expect(page.getByRole('combobox', { name: 'Owner participant' })).toHaveValue('');

  // A name that matches nothing says so.
  await box.fill('zzzz-nobody');
  await expect(page.locator('#participant-options [role="option"]')).toHaveText(['No matches']);
  await page.keyboard.press('Escape');
  await expect(page.locator('#participant-options')).toBeHidden();
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'list_artifacts_owner-filter');
});

test('Offers: amount min and max, an exact negotiation id, the artifact picker; chips remove one filter each; Clear all filters', async ({ page }) => {
  await signIn(page);
  await open(page, '/admin/offers');

  await page.getByLabel('Amount min', { exact: true }).fill('100');
  await page.getByLabel('Amount max', { exact: true }).fill('200');
  await page.getByLabel('Amount max', { exact: true }).press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?amountMin=100&amountMax=200');
  const amounts = (await column(page, 'Amount')).map(amountOf);
  expect(amounts.length).toBeGreaterThan(0);
  for (const value of amounts) {
    expect(value).toBeGreaterThanOrEqual(100);
    expect(value).toBeLessThanOrEqual(200);
  }

  // An exact negotiation id; part of one is not a filter.
  await settled(page);
  await page.getByLabel('Negotiation ID', { exact: true }).fill(seeded.negotiations.desk);
  await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
  await expect(page).toHaveURL(new RegExp(`negotiationId=${seeded.negotiations.desk}`));
  expect(new Set(await column(page, 'Negotiation ID'))).toEqual(new Set([seeded.negotiations.desk]));
  // The address names the id before the page's script has run: Enter typed sooner is the form's
  // own GET, as with JavaScript off, and its page says no count.
  await settled(page);
  await page.getByLabel('Negotiation ID', { exact: true }).fill(seeded.negotiations.desk.slice(0, 8));
  await page.getByLabel('Negotiation ID', { exact: true }).press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?amountMin=100&amountMax=200');
  await settled(page);

  // The artifact picker.
  const artifact = page.getByRole('combobox', { name: 'Artifact' });
  await artifact.pressSequentially('Red bic', { delay: 20 });
  await expect(page.locator('#artifact-options [role="option"]')).toHaveText(['Red bicycle']);
  await page.locator('#artifact-options [role="option"]').first().click();
  await expect(page).toHaveURL(new RegExp(`artifact=${seeded.artifacts['Red bicycle']}`));
  expect(new Set(await column(page, 'Artifact'))).toEqual(new Set(['Red bicycle']));

  // Chips: one per filter; removing one drops only that one.
  const chips = page.locator('#chips .chip span');
  await expect(chips).toHaveText(['Artifact: Red bicycle', 'Amount min: 100', 'Amount max: 200']);
  await settled(page);
  await page.getByRole('link', { name: 'Remove Amount max filter', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + `/admin/offers?artifact=${seeded.artifacts['Red bicycle']}&amountMin=100`);
  await expect(chips).toHaveText(['Artifact: Red bicycle', 'Amount min: 100']);
  await settled(page);

  await page.getByRole('link', { name: 'Clear all filters', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers');
  await expect(page.locator('#chips')).toHaveCount(0);
  expect(await totalOf(page)).toBe(seeded.counts.offers);
});

test('paging: 50, 100 or 200 a page; the range and Previous, Next and Page N; any change goes back to page 1; the state comes back', async ({ page }) => {
  test.setTimeout(60_000);
  await signIn(page);
  await open(page, '/admin/offers');
  await page.locator('#limit').selectOption('100');
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?limit=100');
  await expect(page.locator('#limit')).toBeFocused();
  await settled(page);
  await page.getByRole('link', { name: 'Next', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?offset=100&limit=100');
  await expect(page.locator('#list-range')).toHaveText('Showing 101–200 of 1,234');
  await expect(page.getByRole('link', { name: 'Previous', exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Page 2', exact: true })).toHaveAttribute('aria-current', 'page');
  await expect(page.getByRole('link', { name: 'Page 13', exact: true })).toBeVisible();
  await expect(rows(page)).toHaveCount(100);

  // A filter, a sort, a search: each back to page 1.
  await open(page, '/admin/offers?offset=300&limit=100');
  await expect(page.getByRole('link', { name: 'Page 4', exact: true })).toHaveAttribute('aria-current', 'page');
  // Clicked, not check()ed: the box moves the page (see the status filter test).
  await page.getByLabel('Accepted', { exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?limit=100&status=Accepted');
  await expect(page.getByLabel('Accepted', { exact: true })).toBeChecked();
  // Page 2 moves the page: the next step waits for the new address, or it acts on the page being left.
  await settled(page);
  await page.getByRole('link', { name: 'Page 2', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?offset=100&limit=100&status=Accepted');
  await settled(page);
  await page.locator('#sort-created').click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?sort=created&limit=100&status=Accepted');
  await settled(page);
  await page.getByRole('link', { name: 'Page 2', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?sort=created&offset=100&limit=100&status=Accepted');
  await page.locator('#q').fill('bicycle');
  await page.locator('#q').press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?sort=created&q=bicycle&limit=100&status=Accepted');

  // A page past the last shows the last; a size over 200 shows 200, and never asks for more.
  await open(page, '/admin/offers?limit=100&offset=99999');
  await expect(page.locator('#list-range')).toHaveText('Showing 1,201–1,234 of 1,234');
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?offset=1200&limit=100');
  await open(page, '/admin/offers?limit=500');
  await expect(page.locator('#limit')).toHaveValue('200');
  await expect(rows(page)).toHaveCount(200);
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?limit=200');

  // The whole state comes back: reload, Back, a copied link, and back from a detail page.
  const state = '/admin/offers?sort=amount&q=Seed&offset=100&limit=100&includeDeleted=true&status=Open%2CCancelled';
  await open(page, state);
  await expect(page.locator('#list-range')).toHaveText(/^Showing 101–/);
  await page.reload();
  await expectStyled(page);
  expect(here(page)).toBe(state);
  await expect(page.getByLabel('Show deleted', { exact: true })).toBeChecked();
  await expect(page.locator('#q')).toHaveValue('Seed');

  await rows(page).first().locator('a.id').first().click();
  await expect(page.locator('h1')).toHaveText('Offer details');
  // "Back to list" moves the page: wait for the address, do not read it once.
  await page.getByRole('link', { name: 'Back to list', exact: true }).click();
  await expect.poll(() => here(page)).toBe(state);
  await page.goto('/admin/offers');
  await page.goBack();
  await expectStyled(page);
  expect(here(page)).toBe(state);
  const copy = await page.context().newPage();
  await answerCloudflare(copy, { token: PASS_TOKEN });
  await copy.goto(state);
  await expect(copy.locator('#list-range')).toHaveText(await page.locator('#list-range').innerText());
  await expect(copy.locator('#limit')).toHaveValue('100');
  await copy.close();
});

test('search: a label that says what it matches, one request after a pause, Enter at once, "N results" said, Clear', async ({ page }) => {
  await signIn(page);
  await open(page, '/admin/negotiations');
  const all = await totalOf(page);
  const asked = documents(page);

  await page.getByLabel('Search by artifact or participant name', { exact: true }).pressSequentially('bicycl', { delay: 40 });
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations?q=bicycl');
  await expect(page.locator('#q')).toBeFocused();
  await expect(page.locator('#q')).toHaveValue('bicycl');
  // One request for the whole burst of typing.
  expect(asked).toEqual(['/admin/negotiations?q=bicycl']);
  await expect(page.locator('#console-status')).toHaveText('2 results');
  await expect(page.locator('[aria-live]')).toHaveCount(1);
  expect(new Set(await column(page, 'Artifact'))).toEqual(new Set(['Red bicycle']));

  // Enter goes at once, without the pause.
  await page.locator('#q').fill('Garden');
  await page.locator('#q').press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations?q=Garden');
  await expect(page.locator('#console-status')).toHaveText('1 results');

  // Clear: the whole list again.
  await page.getByRole('link', { name: 'Clear', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations');
  expect(await totalOf(page)).toBe(all);
  await expect(page.locator('#q')).toBeFocused();
});

test('the count said after a move moves nothing: the filters and the first row stay put, at full width and at 320 px', async ({ page }) => {
  await signIn(page);
  // A page with only occasional messages keeps no empty box for them.
  await open(page, '/admin/');
  expect((await page.locator('#console-status').boundingBox())?.height).toBe(0);

  // The page's clock holds still, so the count (said 100 ms after load) waits until it is let go.
  await page.clock.install();
  await page.clock.pauseAt(new Date());
  // Each a new address, so each is a whole page load (the same address with a new mark is not).
  for (const [width, limit] of [[1280, 100], [320, 200]]) {
    await page.setViewportSize({ width, height: 900 });
    await open(page, `/admin/participants?limit=${limit}#focus=limit`);
    await expect(page.locator('#limit')).toBeFocused();
    const live = page.locator('#console-status');
    await expect(live).toHaveText('');
    const before = {
      live: await live.boundingBox(),
      form: await page.locator('#list-form').boundingBox(),
      row: await rows(page).first().boundingBox(),
    };

    await page.clock.runFor(150);
    await settled(page);
    await expect(page.locator('[aria-live]')).toHaveCount(1);
    const after = {
      live: await live.boundingBox(),
      form: await page.locator('#list-form').boundingBox(),
      row: await rows(page).first().boundingBox(),
    };
    expect(after.form?.y).toBe(before.form?.y);
    expect(after.row?.y).toBe(before.row?.y);
    // The space kept is the banner's own: the same height empty and full.
    expect(after.live?.height).toBe(before.live?.height);
    expect(after.live?.height).toBeGreaterThan(0);
  }
  await page.clock.resume();
});

test('Show deleted: off on every list, kept on reload; a deleted row shows Deleted beside its status with the time; its detail needs the toggle', async ({ page }) => {
  await signIn(page);
  for (const list of LISTS) {
    await open(page, list.path);
    await expect(page.getByLabel('Show deleted', { exact: true })).not.toBeChecked();
  }

  await open(page, '/admin/offers?sort=created');
  // Clicked, not check()ed: the toggle moves the page (see the status filter test).
  await page.getByLabel('Show deleted', { exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers?sort=created&includeDeleted=true');
  await page.reload();
  await expectStyled(page);
  await expect(page.getByLabel('Show deleted', { exact: true })).toBeChecked();
  await page.locator('#q').fill(seeded.negotiations.bicycle);
  await page.locator('#q').press('Enter');
  const row = rows(page).filter({ has: page.locator(`a[href^="/admin/offers/${seeded.offers.deleted}"]`) });
  await expect(row).toHaveCount(1);
  await settled(page);
  await expect(row.locator('.badges .badge')).toHaveText(['Open', 'Deleted']);
  await expect(row.locator('.badges time')).toHaveText(/^Nov 19, 2026, \d{1,2}:\d{2} (AM|PM) ET$/);
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'list_offers_show-deleted');

  await row.locator('a.id').first().click();
  await expect(page.locator('h1')).toHaveText('Offer details');
  await expectStyled(page);
  await expect(page.locator('.details .badge')).toHaveText(['Open', 'Deleted']);
  // Read-only: no edit or delete control.
  await expect(page.locator('main').getByRole('button')).toHaveCount(0);
  await expectNoSeriousAxeFindings(page, 'detail_offer_deleted');

  // Without the toggle the deleted record is not there.
  const missing = await page.goto(`/admin/offers/${seeded.offers.deleted}`);
  expect(missing!.status()).toBe(404);
  await expect(page.locator('h1')).toHaveText('We can\'t find that page.');
});

test('each Stats total opens its list filtered as it counts, with the same total; Refresh counts again', async ({ page }) => {
  await signIn(page);
  for (const [id, address] of [
    ['stat-participants', '/admin/participants'],
    ['stat-open-negotiations', '/admin/negotiations?status=Open'],
    ['stat-offers', '/admin/offers'],
    ['stat-accepts', '/admin/offers?status=Accepted'],
    ['stat-declines', '/admin/offers?status=Declined'],
  ]) {
    await open(page, '/admin/');
    const count = Number(((await page.locator(`#${id}`).textContent()) ?? '').replace(/,/g, ''));
    await page.locator(`#${id}-label a`).click();
    await expect(page).toHaveURL(ORIGIN + address);
    await expectStyled(page);
    expect(await totalOf(page)).toBe(count);
  }
  await open(page, '/admin/');
  await page.getByRole('link', { name: 'Refresh', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/');
  await expect(page.locator('#stat-offers')).toHaveText('1,234');
});

test('an empty table says "No {type} yet."; a filter miss says so with Clear filters; a zero total shows 0', async ({ page }) => {
  await signIn(page);
  try {
    seedEntities({ empty: ['Offers'] });
    await open(page, '/admin/offers');
    await expect(page.locator('#list-empty')).toHaveText('No offers yet.');
    await expect(page.locator('#list-miss')).toHaveCount(0);
    await expectNoSeriousAxeFindings(page, 'list_offers_empty');
    await open(page, '/admin/');
    await expect(page.locator('#stat-offers')).toHaveText('0');
  } finally {
    seeded = seedEntities();
  }

  await open(page, '/admin/offers?status=Withdrawn');
  await expect(page.locator('#list-miss')).toHaveText('No results match these filters.');
  await expectNoSeriousAxeFindings(page, 'list_offers_filter-miss');
  await page.getByRole('link', { name: 'Clear filters', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers');
  expect(await totalOf(page)).toBe(seeded.counts.offers);
});

test('every time is in ET with its label; the UTC value shows on hover and on keyboard focus', async ({ page }) => {
  await signIn(page);
  await open(page, '/admin/negotiations?q=camera');
  const summer = rows(page).first().locator('time').first();
  await expect(summer).toHaveText('Jul 1, 2026, 12:30 PM ET');
  await expect(summer).toHaveAttribute('title', '2026-07-01 16:30 UTC');
  await expect(summer).toHaveAttribute('datetime', '2026-07-01T16:30:00Z');
  await summer.focus();
  await expect(summer).toBeFocused();
  // The tooltip is the element's data-utc, drawn after it while it has the focus
  // (Firefox reports the rule, attr(data-utc), rather than the text it draws).
  await expect(summer).toHaveAttribute('data-utc', '2026-07-01 16:30 UTC');
  expect(['"2026-07-01 16:30 UTC"', 'attr(data-utc)']).toContain(await summer.evaluate((el) => getComputedStyle(el, '::after').content));
  await page.locator('#q').focus();
  expect(await summer.evaluate((el) => getComputedStyle(el, '::after').content)).toBe('none');

  await open(page, '/admin/negotiations?q=garden');
  const winter = rows(page).first().locator('time').first();
  await expect(winter).toHaveText('Jan 15, 2026, 12:30 PM ET');
  await expect(winter).toHaveAttribute('title', '2026-01-15 17:30 UTC');
});

test('detail pages: each record as text, free text kept as written, Back to list; axe on each; a screenshot', async ({ page }) => {
  await signIn(page);

  await open(page, `/admin/participants?q=${encodeURIComponent('Alex Rivera')}`);
  await rows(page).first().locator('a.id').click();
  await expect(page.locator('h1')).toHaveText('Participant details');
  await expectStyled(page);
  await expect(page.locator('.details')).toContainText('alex.rivera@example.test');
  await expect(page.locator('.details')).toContainText('alex.rivera.contact@example.test');
  await expectNoSeriousAxeFindings(page, 'detail_participant');
  await page.getByRole('link', { name: 'Back to list', exact: true }).click();
  await expect.poll(() => here(page)).toBe('/admin/participants?q=Alex%20Rivera');

  await open(page, `/admin/artifacts/${seeded.artifacts['Red bicycle']}`);
  await expect(page.locator('h1')).toHaveText('Artifact details');
  await expect(page.locator('.details .free-text')).toHaveText('A red city bicycle.\nNew tyres, one owner.');
  await expect(page.locator('.details')).toContainText('Alex Rivera');
  await expectNoSeriousAxeFindings(page, 'detail_artifact');

  await open(page, `/admin/negotiations/${seeded.negotiations.tools}`);
  await expect(page.locator('h1')).toHaveText('Negotiation details');
  await expect(page.locator('.details')).toContainText('Eli Moreau');
  await expect(page.locator('.details')).toContainText('Feb 1, 2026, 12:00 PM ET');
  await expectNoSeriousAxeFindings(page, 'detail_negotiation');

  await open(page, `/admin/offers/${seeded.offers.terms}`);
  await expect(page.locator('h1')).toHaveText('Offer details');
  await expect(page.locator('.details .free-text')).toHaveText('Pick-up in person.\nCash or bank transfer.');
  await expect(page.locator('.details')).toContainText('1,250.50 USD');
  await expect(page.getByRole('link', { name: 'Red bicycle', exact: true })).toHaveAttribute('href', `/admin/artifacts/${seeded.artifacts['Red bicycle']}`);
  await expectNoSeriousAxeFindings(page, 'detail_offer');
  await shot(page, 'detail_offer_desktop');

  // An id nobody has: S-D3.
  const unknown = await page.goto('/admin/negotiations/0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a59');
  expect(unknown!.status()).toBe(404);
  await expect(page.locator('h1')).toHaveText('We can\'t find that page.');
});

test('a list at 320 px: nothing scrolls the page sideways, the table scrolls in its own region; axe; a screenshot', async ({ page }) => {
  // Every control on the page is measured: more than the default 30 s in the slower engines.
  test.setTimeout(90_000);
  await signIn(page);
  await page.setViewportSize({ width: 320, height: 900 });
  await open(page, '/admin/offers?status=Open,Accepted&amountMin=10');
  const size = await page.evaluate(() => ({ content: document.documentElement.scrollWidth, viewport: document.documentElement.clientWidth }));
  expect(size.content).toBeLessThanOrEqual(size.viewport);
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
  await expectNoSeriousAxeFindings(page, 'list_offers_320');
  await shot(page, 'list_offers_320');
});

test('while the next list is on its way the table is marked busy and keeps its rows', async ({ page }) => {
  await signIn(page);
  await open(page, '/admin/negotiations');
  // The next list never arrives (Playwright cannot look into a page whose next page is pending):
  // what stays on screen is what the owner sees while it loads.
  let asked = 0;
  await page.route((url) => url.pathname === '/admin/negotiations' && url.search === '?sort=status', async (route) => {
    asked += 1;
    await route.abort('aborted');
  });
  await page.locator('#sort-status').click({ noWaitAfter: true });
  await expect.poll(() => asked).toBe(1);
  await expect(page.locator('#list-region')).toHaveAttribute('aria-busy', 'true');
  // 29 negotiations are not deleted: one page, still there.
  await expect(rows(page)).toHaveCount(29);
  expect(here(page)).toBe('/admin/negotiations');

  // Shown again (as from the back/forward cache, which a test cannot force): not busy,
  // and a heading moves the list again.
  await page.unrouteAll();
  await page.evaluate(() => window.dispatchEvent(new PageTransitionEvent('pageshow', { persisted: false })));
  await expect(page.locator('#list-region')).not.toHaveAttribute('aria-busy', 'true');
  await page.locator('#sort-status').click();
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations?sort=status');
});

test('the entity seeding script empties tables only in this run\'s own database', async () => {
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
      const run = runEntitySeedScript({ db, empty: [] });
      expect(run.status, `db = ${JSON.stringify(db)}`).toBe(1);
      expect(run.stderr).toContain('only works on the database of a browser-test run');
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

  // Asked to empty a table it does not seed, it refuses too.
  const odd = runEntitySeedScript({ db: process.env.SIGN_IN_E2E_DB, empty: ['AdminSessions'] });
  expect(odd.status).toBe(1);
  expect(odd.stderr).toContain('empty may name Participants, Artifacts, Negotiations and Offers only');
});

test('keyboard only: skip link, nav, search, a heading, a filter and its chip, a page, a row, and back', async ({ page }) => {
  test.setTimeout(60_000);
  await signIn(page);
  await open(page, '/admin/');
  const skip = page.getByRole('link', { name: 'Skip to main content', exact: true });
  if (tabStopsAtLinks()) {
    await page.keyboard.press('Tab');
    await expect(skip).toBeFocused();
    // Through the nav to Negotiations: Stats, Participants, Artifacts, Negotiations.
    for (let i = 0; i < 4; i += 1) await page.keyboard.press('Tab');
    await expect(page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Negotiations', exact: true })).toBeFocused();
  } else {
    await page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Negotiations', exact: true }).focus();
  }
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations');
  await expectStyled(page);

  // The skip link leads into main; the search is the first control there.
  if (tabStopsAtLinks()) {
    await page.keyboard.press('Tab');
    await expect(skip).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.locator('main')).toBeFocused();
    await page.keyboard.press('Tab');
  } else {
    await page.locator('#q').focus();
  }
  await expect(page.locator('#q')).toBeFocused();
  await page.keyboard.type('Alex');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations?q=Alex');
  await expect(page.locator('#q')).toBeFocused();

  // A heading, from the keyboard; the focus stays on it.
  await page.locator('#sort-created').focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations?sort=created&q=Alex');
  await expect(page.locator('#sort-created')).toBeFocused();

  // A filter with Space, and its chip removed with Enter.
  await page.locator('#status-Open').focus();
  await page.keyboard.press('Space');
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations?sort=created&q=Alex&status=Open');
  await expect(page.locator('#status-Open')).toBeFocused();
  await page.getByRole('link', { name: 'Remove Status filter', exact: true }).focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations?sort=created&q=Alex');
  await expect(page.locator('#q')).toBeFocused();

  // A page, on a longer list.
  await open(page, '/admin/participants');
  await page.getByRole('link', { name: 'Next', exact: true }).focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/participants?offset=50');
  await expect(page.getByRole('link', { name: 'Next', exact: true })).toBeFocused();

  // A row, and back.
  await rows(page).first().locator('a.id').focus();
  await page.keyboard.press('Enter');
  await expect(page.locator('h1')).toHaveText('Participant details');
  await page.getByRole('link', { name: 'Back to list', exact: true }).focus();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/participants?offset=50');
  // A page just loaded: until its stylesheet applies, a focused box wears the browser's own ring.
  await expectStyled(page);

  // The page's 2px ring shows on the controls this walk used, in every engine.
  for (const [name, locator] of [
    ['search', page.locator('#q')],
    ['Name heading', page.locator('#sort-name')],
    ['page size', page.locator('#limit')],
    ['Show deleted', page.getByLabel('Show deleted', { exact: true })],
  ] as const) {
    await locator.focus();
    await expect(locator).toBeFocused();
    expect(await locator.evaluate((el) => {
      const style = getComputedStyle(el);
      return `${style.outlineStyle} ${style.outlineWidth}`;
    }), `the focus ring on ${name}`).toBe('solid 2px');
  }
});

test.describe('JavaScript off', () => {
  test.use({ javaScriptEnabled: false });

  test('with JavaScript off the lists still work: Apply filters, a heading, a page, a chip', async ({ page }) => {
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

    await page.goto('/admin/offers');
    await expect(rows(page)).toHaveCount(50);
    // No count ever comes with JavaScript off, so no space is kept for it.
    expect((await page.locator('#console-status').boundingBox())?.height ?? 0).toBe(0);
    await page.getByLabel('Declined', { exact: true }).check();
    await page.getByLabel('Accepted', { exact: true }).check();
    await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
    // The browser sends the form as it is; the server reads it as the same list.
    await expect(page.locator('#chips')).toContainText('Status: Accepted, Declined');
    expect(new Set(await page.locator('#list-table tbody .badge').allTextContents())).toEqual(new Set(['Accepted', 'Declined']));

    await page.locator('#sort-amount').click();
    await expect(page.locator('#list-table th[aria-sort="descending"] #sort-amount')).toHaveCount(1);
    await page.getByRole('link', { name: 'Next', exact: true }).click();
    await expect(page.locator('#list-range')).toHaveText(/^Showing 51–100 of /);
    await page.getByRole('link', { name: 'Remove Status filter', exact: true }).click();
    await expect(page.locator('#chips')).toHaveCount(0);
    expect(await totalOf(page)).toBe(seeded.counts.offers);
  });
});
