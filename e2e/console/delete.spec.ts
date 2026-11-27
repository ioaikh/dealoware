import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { test, expect, type Locator, type Page, type Request } from '@playwright/test';
import { expectNoSeriousAxeFindings } from './axe';
import { seedEntities, type Seeded } from './entities';
import { authenticatorCode, seedOwner, type Owner } from '../sign-in/owner';
import { answerCloudflare } from '../signed-out/cloudflare';

/**
 * The delete dialog (T06b, A7 Step 10) against the real app and its delete
 * API, signed in through the real pages, over the entities seed_entities.py
 * writes. Start it with e2e/console/run.sh.
 *
 * What the server writes into each page (which Delete, the dialog's words)
 * is proven by the .NET tests (AdminDeletePageTests); these prove what the
 * browser does with it: the dialog filled from the delete-intent, the typed
 * confirmation, the blocked artifact, the expired or changed token, one
 * DELETE, where the page goes afterwards, keyboard and focus, axe and 320 px.
 * Case ids: TD-ADM-UI-del-01 to del-06, with -axe and -kbd for the dialog.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const PASS_TOKEN = process.env.CONSOLE_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.CONSOLE_E2E_ARTIFACTS;
const DB = process.env.SIGN_IN_E2E_DB ?? '';
const PYTHON = process.env.SIGN_IN_E2E_PYTHON ?? '';

const UNDO = "This can't be restored from the admin.";
const NOTHING_ELSE = 'Nothing else is deleted with it.';
const EXPIRED = 'This confirmation is no longer valid. Review the details again.';
const CHANGED = 'This record changed after this dialog opened. Nothing was deleted. Review the details again.';
const UNCLEAR_DELETE = "We couldn't confirm the delete. Try again to see whether it went through.";
const NOT_FOUND = "We can't find this record. It may have been deleted already.";
const NOTHING_DELETED = 'Nothing was deleted.';
const SESSION_ENDED = 'Your session has ended. Sign in again to continue.';
const RETRY = "That didn't go through. Wait a moment and try again.";

type Kind = 'participants' | 'artifacts' | 'negotiations' | 'offers';

let owner: Owner;
let seeded: Seeded;
let cspMessages: string[] = [];
let pageErrors: string[] = [];

function engine(): string {
  return test.info().project.name;
}

/** WebKit's Tab skips buttons and links by default: there the test puts the focus on them itself. */
function tabStopsAtButtons(): boolean {
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

/** Changes the run's database behind the page's back (delete_rows.py, which refuses any other database). */
function changeRows(request: { expireTokens?: boolean; addOfferTo?: string; addNegotiationFor?: string }): void {
  const run = spawnSync(PYTHON, [path.join(__dirname, 'delete_rows.py')], {
    input: JSON.stringify({ db: DB, ...request }),
    encoding: 'utf8',
    timeout: 30_000,
  });
  if (run.status !== 0) throw new Error(`delete_rows.py failed: ${run.stderr}`);
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

/** The page has loaded and its stylesheet applies (a nav link is 2.5rem tall): only then is axe, a size or a focus ring fair. */
async function expectStyled(page: Page): Promise<void> {
  await page.waitForLoadState('load');
  const link = page.getByRole('navigation', { name: 'Admin' }).getByRole('link').first();
  await expect.poll(() => link.evaluate((el) => getComputedStyle(el).minHeight)).toBe('40px');
}

async function open(page: Page, address: string): Promise<void> {
  await page.goto(address);
  await expectStyled(page);
}

function here(page: Page): string {
  const url = new URL(page.url());
  return url.pathname + url.search + url.hash;
}

/** One record's id, read from the list API by its exact name. */
async function idByName(page: Page, kind: 'participants' | 'artifacts', name: string): Promise<string> {
  const id = await page.evaluate(async ([k, n]) => {
    const reply = await fetch(`/admin/api/${k}?q=${encodeURIComponent(n)}`);
    const body = await reply.json();
    const item = (body.items as Array<Record<string, string>>).find((x) => (x.displayName ?? x.name) === n);
    return item ? item.id : '';
  }, [kind, name] as const);
  expect(id).not.toBe('');
  return id.toLowerCase();
}

/** A delete as the dialog makes it, sent straight to the API: intent, then DELETE with its token. */
async function deleteThroughApi(page: Page, kind: Kind, id: string): Promise<number> {
  return page.evaluate(async ([k, i]) => {
    const csrf = document.querySelector('meta[name="dw-csrf"]')!.getAttribute('content')!;
    const intent = await fetch(`/admin/api/${k}/${i}/delete-intent`, { method: 'POST', headers: { 'X-CSRF-TOKEN': csrf } });
    const body = await intent.json();
    const done = await fetch(`/admin/api/${k}/${i}`, {
      method: 'DELETE',
      headers: { 'X-CSRF-TOKEN': csrf, 'X-Admin-Confirm-Token': body.confirmToken },
    });
    return done.status;
  }, [kind, id] as const);
}

/** How many entity_delete rows the audit log holds. */
async function deleteAuditRows(page: Page): Promise<number> {
  return page.evaluate(async () => {
    const reply = await fetch('/admin/api/audit?action=entity_delete');
    return (await reply.json()).total as number;
  });
}

/** Every DELETE the page sends, from here on. */
function deletesOf(page: Page): Request[] {
  const sent: Request[] = [];
  page.on('request', (r) => {
    if (r.method() === 'DELETE') sent.push(r);
  });
  return sent;
}

/** Holds every DELETE until released, so the page can be read while its request is out. */
async function holdDeletes(page: Page): Promise<() => void> {
  let release!: () => void;
  const gate = new Promise<void>((resolve) => { release = resolve; });
  await page.route((url) => url.pathname.startsWith('/admin/api/'), async (route) => {
    if (route.request().method() !== 'DELETE') return route.fallback();
    await gate;
    await route.continue();
  });
  return release;
}

/** Answers every DELETE with the given reply, without reaching the server. */
async function answerDeletes(page: Page, status: number, body: object): Promise<void> {
  await page.route((url) => url.pathname.startsWith('/admin/api/'), async (route) => {
    if (route.request().method() !== 'DELETE') return route.fallback();
    await route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

/** The focus is on an element inside the given one. */
async function focusInside(page: Page, selector: string): Promise<boolean> {
  return page.evaluate((s) => {
    const box = document.querySelector(s);
    return !!box && !!document.activeElement && box.contains(document.activeElement);
  }, selector);
}

/** Opens the dialog with the given Delete and returns the intent the server answered with. */
async function openDelete(page: Page, trigger: Locator): Promise<Record<string, any>> {
  const answered = page.waitForResponse((r) => r.url().endsWith('/delete-intent'));
  await trigger.click();
  const reply = await answered;
  await expect(dialog(page)).toBeVisible();
  return reply.json();
}

/** Runs the action and waits for the page to load itself anew (the address may stay the same). */
async function reloadedBy(page: Page, action: () => Promise<void>): Promise<void> {
  const loaded = page.waitForEvent('load');
  await action();
  await loaded;
  await expectStyled(page);
}

async function busyMark(page: Page): Promise<boolean> {
  return page.locator('#delete-dialog').evaluate((d) => d.hasAttribute('aria-busy'));
}

const live = (page: Page) => page.locator('#console-status');
const dialog = (page: Page) => page.locator('#delete-dialog');
const detailDelete = (page: Page) => page.locator('#delete-button');
const cancelIn = (page: Page) => dialog(page).getByRole('button', { name: 'Cancel', exact: true });
const rowsOf = (page: Page) => page.locator('#list-table tbody tr');

/** The texts of the dialog's buttons, in order. */
async function buttonsOf(page: Page): Promise<string[]> {
  return dialog(page).locator('button').allTextContents();
}

test.beforeEach(async ({ page }) => {
  seeded = seedEntities();
  owner = seedOwner({ enrolled: true });
  cspMessages = [];
  pageErrors = [];
  page.on('console', (message) => {
    const text = message.text();
    if (!/content[- ]security[- ]policy/i.test(text)) return;
    if (text.includes('/favicon.ico')) return;
    const at = new URL(page.url()).pathname;
    if (at.startsWith('/admin/api/') || at.startsWith('/admin/assets/')) return;
    cspMessages.push(text);
  });
  page.on('pageerror', (error) => pageErrors.push(error.message));
  await answerCloudflare(page, { token: PASS_TOKEN });
});

test.afterEach(() => {
  expect(cspMessages).toEqual([]);
  expect(pageErrors).toEqual([]);
});

// The suites after this one start from the rows as seeded.
test.afterAll(() => {
  seedEntities();
});

test('TD-ADM-UI-del-01: each type names itself, its ID, what goes with it as the server counts it, the undo statement, the named button and Cancel; axe', async ({ page }) => {
  await signIn(page);

  // An offer: nothing goes with it.
  const offer = seeded.offers.terms;
  await open(page, `/admin/offers/${offer}`);
  await expect(detailDelete(page)).toHaveText('Delete');
  let intent = await openDelete(page, detailDelete(page));
  expect(intent.cascade).toMatchObject({ offers: 1, negotiations: 0 });
  await expect(dialog(page)).toHaveAccessibleName(`Delete offer ${offer}?`);
  await expect(dialog(page).locator('.delete-id')).toHaveText(`ID: ${offer}`);
  await expect(dialog(page).locator('#delete-dialog-content')).toContainText(NOTHING_ELSE);
  await expect(dialog(page).locator('.delete-undo')).toHaveText(UNDO);
  expect(await buttonsOf(page)).toEqual(['Delete offer', 'Cancel']);
  await expect(dialog(page).getByRole('button', { name: 'Delete offer', exact: true })).toBeEnabled();
  await expect(dialog(page).locator('input')).toHaveCount(0);
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'delete_offer_dialog');
  await shot(page, 'delete_offer_dialog_desktop');
  await cancelIn(page).click();
  await expect(dialog(page)).toBeHidden();

  // An artifact no negotiation uses.
  const lonely = await idByName(page, 'artifacts', 'Seed artifact 150');
  await open(page, `/admin/artifacts/${lonely}`);
  intent = await openDelete(page, detailDelete(page));
  expect(intent.identity.name).toBe('Seed artifact 150');
  await expect(dialog(page)).toHaveAccessibleName('Delete artifact Seed artifact 150?');
  await expect(dialog(page).locator('.delete-id')).toHaveText(`ID: ${lonely}`);
  await expect(dialog(page).locator('#delete-dialog-content')).toContainText(NOTHING_ELSE);
  expect(await buttonsOf(page)).toEqual(['Delete artifact', 'Cancel']);
  await cancelIn(page).click();

  // An open negotiation: its offers, counted by the server, and the warning.
  const desk = seeded.negotiations.desk;
  await open(page, `/admin/negotiations/${desk}`);
  intent = await openDelete(page, detailDelete(page));
  const deskOffers = intent.cascade.offers as number;
  expect(deskOffers).toBeGreaterThan(1);
  await expect(dialog(page)).toHaveAccessibleName(`Delete negotiation ${desk}?`);
  await expect(dialog(page).locator('#delete-dialog-content')).toContainText('Also deleted with it:');
  await expect(dialog(page).locator('.delete-list li')).toHaveText([`${deskOffers.toLocaleString('en-US')} offers`]);
  await expect(dialog(page).locator('.delete-warning')).toHaveText(
    `This negotiation is open and has ${deskOffers.toLocaleString('en-US')} offers. They will no longer show in the lists unless “Show deleted” is on.`);
  await expect(dialog(page).locator('.delete-undo')).toHaveText(UNDO);
  expect(await buttonsOf(page)).toEqual(['Delete negotiation', 'Cancel']);
  await expectNoSeriousAxeFindings(page, 'delete_negotiation_dialog');
  await cancelIn(page).click();

  // A participant: the negotiations it is a party to and their offers, and the open ones among them.
  const casey = seeded.participants['Casey Novak'];
  await open(page, `/admin/participants/${casey.id}`);
  intent = await openDelete(page, detailDelete(page));
  const { negotiations, offers, openNegotiations } = intent.cascade as Record<string, number>;
  expect(negotiations).toBe(1);
  expect(openNegotiations).toBe(1);
  await expect(dialog(page)).toHaveAccessibleName('Delete participant Casey Novak?');
  await expect(dialog(page).locator('.delete-id')).toHaveText(`ID: ${casey.id}`);
  await expect(dialog(page).locator('.delete-list li')).toHaveText(['1 negotiation', `${offers.toLocaleString('en-US')} offers`]);
  await expect(dialog(page).locator('.delete-warning')).toHaveText(
    '1 of these negotiations is open. It and its offers will no longer show in the lists unless “Show deleted” is on.');
  expect(await buttonsOf(page)).toEqual(['Delete participant', 'Cancel']);
  await shot(page, 'delete_participant_dialog_desktop');
  await cancelIn(page).click();
  await expect(detailDelete(page)).toBeFocused();
});

test('TD-ADM-UI-del-02: by keyboard; the focus starts on Cancel and stays in the dialog; Escape and the backdrop close it and give the focus back; cancelling sends no DELETE and writes no audit row', async ({ page }) => {
  await signIn(page);
  const offer = seeded.offers.accepted;
  await open(page, `/admin/offers/${offer}`);
  const auditsBefore = await deleteAuditRows(page);
  const deletes = deletesOf(page);
  const trigger = detailDelete(page);

  await trigger.focus();
  await page.keyboard.press('Enter');
  const box = dialog(page);
  await expect(box).toBeVisible();
  await expect(box).toHaveAttribute('aria-modal', 'true');
  await expect(page.getByRole('dialog')).toHaveAccessibleName(`Delete offer ${offer}?`);
  await expect(cancelIn(page)).toBeFocused();
  if (tabStopsAtButtons()) {
    for (const key of ['Tab', 'Tab', 'Tab', 'Shift+Tab', 'Shift+Tab', 'Shift+Tab']) {
      await page.keyboard.press(key);
      expect(await focusInside(page, '#delete-dialog')).toBe(true);
    }
  }

  // Escape: closed, the focus on Delete.
  await page.keyboard.press('Escape');
  await expect(box).toBeHidden();
  await expect(trigger).toBeFocused();
  // Cancel.
  await trigger.click();
  await expect(cancelIn(page)).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(box).toBeHidden();
  await expect(trigger).toBeFocused();
  // The backdrop.
  await trigger.click();
  await expect(box).toBeVisible();
  await page.mouse.click(4, 4);
  await expect(box).toBeHidden();
  await expect(trigger).toBeFocused();

  expect(deletes).toHaveLength(0);
  expect(await deleteAuditRows(page)).toBe(auditsBefore);
  await expect(page.locator('.details')).toContainText(offer);

  // Confirmed by keyboard: one DELETE, the token in its header and never in the address.
  await trigger.focus();
  await page.keyboard.press('Enter');
  await expect(cancelIn(page)).toBeFocused();
  const confirm = box.getByRole('button', { name: 'Delete offer', exact: true });
  if (tabStopsAtButtons()) await page.keyboard.press('Shift+Tab');
  else await confirm.focus();
  await expect(confirm).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(ORIGIN + '/admin/offers');
  await expect(live(page)).toHaveText(`Offer ${offer} deleted.`);
  expect(deletes).toHaveLength(1);
  expect(deletes[0].url()).toBe(`${ORIGIN}/admin/api/offers/${offer}`);
  expect(deletes[0].headers()['x-admin-confirm-token']).toMatch(/^[0-9a-f]{64}$/);
  expect(deletes[0].headers()['x-csrf-token']).toMatch(/^[0-9A-F]{64}$/);
  expect(await deleteAuditRows(page)).toBe(auditsBefore + 1);
});

test('TD-ADM-UI-del-03: a token that ran out while the dialog was open, and a record that changed: nothing deleted, the server asked again, fresh counts; a double click sends one DELETE', async ({ page }) => {
  await signIn(page);
  const desk = seeded.negotiations.desk;
  await open(page, `/admin/negotiations/${desk}`);
  const deletes = deletesOf(page);
  let intent = await openDelete(page, detailDelete(page));
  const first = intent.cascade.offers as number;
  const box = dialog(page);
  await box.getByLabel(`Type ${desk} to confirm`).fill(desk);

  // A party makes an offer, and five minutes pass.
  changeRows({ addOfferTo: desk, expireTokens: true });
  let again = page.waitForResponse((r) => r.url().endsWith('/delete-intent'));
  await box.getByRole('button', { name: 'Delete negotiation', exact: true }).click();
  intent = await (await again).json();
  await expect(page.locator('#delete-dialog-alert')).toHaveText(EXPIRED);
  expect(intent.cascade.offers).toBe(first + 1);
  await expect(box.locator('.delete-list li')).toHaveText([`${(first + 1).toLocaleString('en-US')} offers`]);
  await expect(cancelIn(page)).toBeFocused();
  // Busy only while a request was out.
  expect(await busyMark(page)).toBe(false);
  // What was typed is kept within this open; the button works again.
  await expect(box.getByLabel(`Type ${desk} to confirm`)).toHaveValue(desk);
  await expect(box.getByRole('button', { name: 'Delete negotiation', exact: true })).toBeEnabled();
  await expectNoSeriousAxeFindings(page, 'delete_expired');

  // Another offer, the token still good: the record changed.
  changeRows({ addOfferTo: desk });
  again = page.waitForResponse((r) => r.url().endsWith('/delete-intent'));
  await box.getByRole('button', { name: 'Delete negotiation', exact: true }).click();
  intent = await (await again).json();
  await expect(page.locator('#delete-dialog-alert')).toHaveText(CHANGED);
  await expect(box.locator('.delete-list li')).toHaveText([`${(first + 2).toLocaleString('en-US')} offers`]);
  expect(await busyMark(page)).toBe(false);
  expect(deletes).toHaveLength(2);

  // Nothing was deleted by either.
  const status = await page.evaluate(async (id) => (await fetch(`/admin/api/negotiations/${id}`)).status, desk);
  expect(status).toBe(200);

  // A double click: "Deleting…", disabled, Cancel too, and one request.
  const release = await holdDeletes(page);
  await box.getByRole('button', { name: 'Delete negotiation', exact: true }).dblclick();
  const busy = box.locator('button.danger');
  await expect(busy).toHaveText('Deleting…');
  await expect(busy).toBeDisabled();
  await expect(cancelIn(page)).toBeDisabled();
  expect(await busyMark(page)).toBe(true);
  // Nothing closes it while the request is out.
  await page.keyboard.press('Escape');
  await page.mouse.click(4, 4);
  await expect(box).toBeVisible();
  release();
  await expect(page).toHaveURL(ORIGIN + '/admin/negotiations');
  await expect(live(page)).toHaveText(`Negotiation ${desk} deleted.`);
  expect(deletes).toHaveLength(3);
});

test('TD-ADM-UI-del-04: an artifact negotiations use: its title, each negotiation a link, Close only and no Delete in the dialog; once they are deleted the normal dialog', async ({ page }) => {
  await signIn(page);
  const bicycle = seeded.artifacts['Red bicycle'];
  await open(page, `/admin/artifacts/${bicycle}`);
  await detailDelete(page).click();
  const box = dialog(page);
  await expect(box).toBeVisible();
  await expect(page.getByRole('dialog')).toHaveAccessibleName("This artifact can't be deleted");
  await expect(box.locator('#delete-dialog-content > p').first()).toHaveText('It is used by these negotiations:');
  const links = box.locator('.delete-list a');
  const expected = [seeded.negotiations.bicycle, seeded.negotiations['second-bicycle']].sort();
  await expect(links).toHaveCount(2);
  expect((await links.allTextContents()).sort()).toEqual(expected);
  for (const id of expected) await expect(box.getByRole('link', { name: id })).toHaveAttribute('href', `/admin/negotiations/${id}`);
  expect(await buttonsOf(page)).toEqual(['Close']);
  await expect(box.locator('button.danger')).toHaveCount(0);
  await expect(box.locator('input')).toHaveCount(0);
  const close = box.getByRole('button', { name: 'Close', exact: true });
  await expect(close).toBeFocused();
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'delete_blocked');
  await shot(page, 'delete_blocked_desktop');
  await page.keyboard.press('Escape');
  await expect(detailDelete(page)).toBeFocused();

  // A link opens the negotiation.
  await detailDelete(page).click();
  await box.getByRole('link', { name: expected[0] }).click();
  await expect(page).toHaveURL(`${ORIGIN}/admin/negotiations/${expected[0]}`);
  await expect(page.locator('h1')).toHaveText('Negotiation details');

  // Both negotiations deleted: the artifact can go.
  for (const id of expected) expect(await deleteThroughApi(page, 'negotiations', id)).toBe(200);
  await open(page, `/admin/artifacts/${bicycle}`);
  await openDelete(page, detailDelete(page));
  await expect(page.getByRole('dialog')).toHaveAccessibleName('Delete artifact Red bicycle?');
  expect(await buttonsOf(page)).toEqual(['Delete artifact', 'Cancel']);
});

test('TD-ADM-UI-del-05: the typed confirmation, for a participant with records that go with it and for an open negotiation: exact text only, pasted text too', async ({ page }) => {
  await signIn(page);
  const casey = seeded.participants['Casey Novak'];
  await open(page, `/admin/participants/${casey.id}`);
  await openDelete(page, detailDelete(page));
  const box = dialog(page);
  const typed = box.getByLabel('Type Casey Novak to confirm', { exact: true });
  const confirm = box.getByRole('button', { name: 'Delete participant', exact: true });
  await expect(confirm).toBeDisabled();
  for (const attempt of ['Casey', 'casey novak', 'Casey Novak ', ' Casey Novak', 'Casey  Novak']) {
    await typed.fill(attempt);
    await expect(confirm).toBeDisabled();
  }
  await typed.fill('');
  await typed.focus();
  // Pasted, as an input method or a paste puts text in.
  await page.keyboard.insertText('Casey Novak');
  await expect(confirm).toBeEnabled();
  await expectNoSeriousAxeFindings(page, 'delete_typed');
  await typed.fill('Casey Novak!');
  await expect(confirm).toBeDisabled();
  await page.keyboard.press('Escape');
  await expect(box).toBeHidden();

  // A participant with nothing that goes with it needs nothing typed.
  const lonely = await idByName(page, 'participants', 'Seed participant 050');
  await open(page, `/admin/participants/${lonely}`);
  await openDelete(page, detailDelete(page));
  await expect(box.locator('input')).toHaveCount(0);
  await expect(box.getByRole('button', { name: 'Delete participant', exact: true })).toBeEnabled();
  await cancelIn(page).click();

  // An open negotiation: its ID typed, and it is named as open with its offers' count.
  const bicycle = seeded.negotiations.bicycle;
  await open(page, `/admin/negotiations/${bicycle}`);
  const intent = await openDelete(page, detailDelete(page));
  const n = (intent.cascade.offers as number).toLocaleString('en-US');
  await expect(box.locator('.delete-warning')).toHaveText(`This negotiation is open and has ${n} offers. They will no longer show in the lists unless “Show deleted” is on.`);
  const confirmNegotiation = box.getByRole('button', { name: 'Delete negotiation', exact: true });
  await expect(confirmNegotiation).toBeDisabled();
  await box.getByLabel(`Type ${bicycle} to confirm`).fill(bicycle.toUpperCase());
  await expect(confirmNegotiation).toBeDisabled();
  await box.getByLabel(`Type ${bicycle} to confirm`).fill(bicycle);
  await expect(confirmNegotiation).toBeEnabled();
  await cancelIn(page).click();

  // A closed negotiation needs nothing typed.
  await open(page, `/admin/negotiations/${seeded.negotiations.camera}`);
  await openDelete(page, detailDelete(page));
  await expect(box.locator('input')).toHaveCount(0);
  await expect(box.locator('.delete-warning')).toHaveCount(0);
});

test('TD-ADM-UI-del-06: after a delete the list, with its state, says so; the row is gone, and with Show deleted it is back with Deleted and no Delete; a failed delete keeps the dialog with "Nothing was deleted." and Retry', async ({ page }) => {
  await signIn(page);

  // From a list row, on a searched list.
  const name = 'Seed participant 050';
  const searched = `/admin/participants?q=${encodeURIComponent(name)}`;
  await open(page, searched);
  const row = rowsOf(page).filter({ hasText: name });
  await expect(row).toHaveCount(1);
  const rowDelete = row.getByRole('button', { name: `Delete ${name}`, exact: true });
  await openDelete(page, rowDelete);
  await reloadedBy(page, () => dialog(page).getByRole('button', { name: 'Delete participant', exact: true }).click());
  expect(here(page)).toBe(searched);
  await expect(live(page)).toHaveText(`Participant ${name} deleted.`);
  await expect(page.locator('#main')).toBeFocused();
  await expect(rowsOf(page).filter({ hasText: name })).toHaveCount(0);
  // The note was used once and is gone from the history entry: nothing of the name is kept there.
  expect(await page.evaluate(() => history.state)).toBeNull();
  await page.reload();
  await expectStyled(page);
  expect(await page.evaluate(() => history.state)).toBeNull();
  await expect(live(page)).toHaveText('');
  await page.getByLabel('Show deleted', { exact: true }).click();
  await page.waitForURL(/includeDeleted=true/);
  await expectStyled(page);
  const deletedRow = rowsOf(page).filter({ hasText: name });
  await expect(deletedRow.locator('.badges .badge')).toHaveText(['Suspended', 'Deleted']);
  await expect(deletedRow.getByRole('button')).toHaveCount(0);

  // From a detail page: the list it came from, its state kept.
  const offer = seeded.offers.superseded;
  await open(page, `/admin/offers/${offer}?sort=created&dir=asc`);
  await openDelete(page, detailDelete(page));

  // The server fails it (its audit row could not be written, so all of it rolled back).
  await answerDeletes(page, 500, { error: 'Internal Server Error', traceId: 'x' });
  await dialog(page).getByRole('button', { name: 'Delete offer', exact: true }).click();
  await expect(page.locator('#delete-dialog-alert')).toHaveText(NOTHING_DELETED);
  const retry = dialog(page).getByRole('button', { name: 'Retry', exact: true });
  await expect(retry).toBeFocused();
  await expect(dialog(page)).toBeVisible();
  await expectNoSeriousAxeFindings(page, 'delete_failed');
  await shot(page, 'delete_failed_desktop');
  await page.unrouteAll({ behavior: 'wait' });
  await retry.click();
  await page.waitForURL(`${ORIGIN}/admin/offers?sort=created&dir=asc`);
  await expect(live(page)).toHaveText(`Offer ${offer} deleted.`);
  const missing = await page.goto(`/admin/offers/${offer}`);
  expect(missing!.status()).toBe(404);

  // An artifact and a closed negotiation, each from its page.
  const lonely = await idByName(page, 'artifacts', 'Seed artifact 150');
  await open(page, `/admin/artifacts/${lonely}`);
  await openDelete(page, detailDelete(page));
  await dialog(page).getByRole('button', { name: 'Delete artifact', exact: true }).click();
  await page.waitForURL(`${ORIGIN}/admin/artifacts`);
  await expect(live(page)).toHaveText('Artifact Seed artifact 150 deleted.');

  const camera = seeded.negotiations.camera;
  await open(page, `/admin/negotiations/${camera}`);
  await openDelete(page, detailDelete(page));
  await dialog(page).getByRole('button', { name: 'Delete negotiation', exact: true }).click();
  await page.waitForURL(`${ORIGIN}/admin/negotiations`);
  await expect(live(page)).toHaveText(`Negotiation ${camera} deleted.`);
  await open(page, `/admin/negotiations/${camera}?includeDeleted=true`);
  await expect(page.locator('.details .badge')).toHaveText(['Closed', 'Deleted']);
  // A soft-deleted record has no Delete.
  await expect(detailDelete(page)).toHaveCount(0);
  await expect(page.locator('main').getByRole('button')).toHaveCount(0);
});

test('soft-deleted records show no Delete, on a list or a page; every other row has one, named for its row', async ({ page }) => {
  await signIn(page);
  await open(page, '/admin/participants?includeDeleted=true&q=Robin');
  const robin = rowsOf(page).filter({ hasText: 'Robin Hart' });
  await expect(robin.locator('.badge-deleted')).toHaveCount(1);
  await expect(robin.getByRole('button')).toHaveCount(0);
  await open(page, `/admin/participants/${seeded.participants['Robin Hart'].id}?includeDeleted=true`);
  await expect(page.locator('main').getByRole('button')).toHaveCount(0);
  await expect(page.locator('#delete-dialog')).toHaveCount(0);

  await open(page, '/admin/offers?includeDeleted=true&negotiationId=' + seeded.negotiations.bicycle);
  const gone = rowsOf(page).filter({ has: page.locator(`a[href^="/admin/offers/${seeded.offers.deleted}"]`) });
  await expect(gone.getByRole('button')).toHaveCount(0);
  const kept = rowsOf(page).filter({ has: page.locator(`a[href^="/admin/offers/${seeded.offers.terms}"]`) });
  await expect(kept.getByRole('button', { name: `Delete ${seeded.offers.terms}`, exact: true })).toBeVisible();

  await open(page, '/admin/participants?q=Alex');
  await expect(rowsOf(page).first().getByRole('button', { name: 'Delete Alex Rivera', exact: true })).toBeVisible();
  await expectNoSeriousAxeFindings(page, 'delete_list_rows');
});

test('keyboard only: from a list row, the name typed, Enter deletes; the list says so', async ({ page }) => {
  await signIn(page);
  const name = 'Seed participant 001';
  await open(page, `/admin/participants?q=${encodeURIComponent(name)}`);
  const rowDelete = rowsOf(page).filter({ hasText: name }).getByRole('button', { name: `Delete ${name}`, exact: true });
  if (tabStopsAtButtons()) {
    await page.locator('#q').focus();
    for (let i = 0; i < 40; i += 1) {
      await page.keyboard.press('Tab');
      if (await rowDelete.evaluate((el) => el === document.activeElement)) break;
    }
  } else {
    await rowDelete.focus();
  }
  await expect(rowDelete).toBeFocused();
  await expect.poll(() => rowDelete.evaluate((el) => getComputedStyle(el).outlineStyle)).toBe('solid');
  await page.keyboard.press('Enter');
  const box = dialog(page);
  await expect(box).toBeVisible();
  await expect(cancelIn(page)).toBeFocused();
  const typed = box.getByLabel(`Type ${name} to confirm`, { exact: true });
  const confirm = box.getByRole('button', { name: 'Delete participant', exact: true });
  if (tabStopsAtButtons()) {
    // Back past the disabled button to the box.
    await page.keyboard.press('Shift+Tab');
  } else {
    await typed.focus();
  }
  await expect(typed).toBeFocused();
  await page.keyboard.type(name);
  if (tabStopsAtButtons()) await page.keyboard.press('Tab');
  else await confirm.focus();
  await expect(confirm).toBeFocused();
  await reloadedBy(page, () => page.keyboard.press('Enter'));
  expect(here(page)).toBe(`/admin/participants?q=${encodeURIComponent(name)}`);
  await expect(live(page)).toHaveText(`Participant ${name} deleted.`);
  await expect(rowsOf(page).filter({ hasText: name })).toHaveCount(0);
});

test('at 320 px the dialog fits, nothing scrolls sideways and every control is big enough; axe; screenshots', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 900 });
  await signIn(page);
  const casey = seeded.participants['Casey Novak'];
  await open(page, `/admin/participants/${casey.id}`);
  await openDelete(page, detailDelete(page));
  await dialog(page).getByLabel('Type Casey Novak to confirm', { exact: true }).fill('Casey');
  const sizes = await page.evaluate(() => {
    const d = document.getElementById('delete-dialog')!.getBoundingClientRect();
    return { content: document.documentElement.scrollWidth, viewport: document.documentElement.clientWidth, left: d.left, right: d.right };
  });
  expect(sizes.content).toBeLessThanOrEqual(sizes.viewport);
  expect(sizes.left).toBeGreaterThanOrEqual(0);
  expect(sizes.right).toBeLessThanOrEqual(sizes.viewport);
  for (const target of await dialog(page).locator('button, input').all()) {
    const box = await target.boundingBox();
    expect(box).not.toBeNull();
    expect(Math.round(box!.height)).toBeGreaterThanOrEqual(24);
    expect(Math.round(box!.width)).toBeGreaterThanOrEqual(24);
  }
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'delete_dialog_320');
  await shot(page, 'delete_dialog_320');
  await cancelIn(page).click();

  // An open negotiation's long ID in the title and the label wraps inside the dialog.
  await open(page, `/admin/negotiations/${seeded.negotiations.desk}`);
  await openDelete(page, detailDelete(page));
  const width = await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth);
  expect(width).toBe(true);
  const inner = await dialog(page).evaluate((d) => d.scrollWidth <= d.clientWidth);
  expect(inner).toBe(true);
  await shot(page, 'delete_negotiation_320');
  await cancelIn(page).click();

  await open(page, '/admin/artifacts?q=Seed%20artifact%2015');
  const list = await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth);
  expect(list).toBe(true);
  await expectNoSeriousAxeFindings(page, 'delete_list_320');
  await shot(page, 'delete_list_320');
});

test('the session and the token: a 401 goes to sign-in with "session ended"; no answer says to try again, with Retry; nothing deleted', async ({ page }) => {
  await signIn(page);
  const offer = seeded.offers.terms;
  const address = `/admin/offers/${offer}`;

  // No answer at all to the DELETE.
  await open(page, address);
  await page.route((url) => url.pathname.startsWith('/admin/api/'), async (route) => {
    if (route.request().method() !== 'DELETE') return route.fallback();
    await route.abort('failed');
  });
  await openDelete(page, detailDelete(page));
  await dialog(page).getByRole('button', { name: 'Delete offer', exact: true }).click();
  await expect(page.locator('#delete-dialog-alert')).toHaveText(UNCLEAR_DELETE);
  await expect(dialog(page).getByRole('button', { name: 'Retry', exact: true })).toBeFocused();
  expect(await busyMark(page)).toBe(false);
  expect(here(page)).toBe(address);
  await cancelIn(page).click();
  await expect(detailDelete(page)).toBeFocused();
  await page.unrouteAll({ behavior: 'wait' });

  // No answer to the intent: the dialog says so, with Close.
  await page.route((url) => url.pathname.endsWith('/delete-intent'), (route) => route.abort('failed'));
  await detailDelete(page).click();
  await expect(dialog(page)).toBeVisible();
  await expect(dialog(page).locator('#delete-dialog-content')).toHaveText(RETRY);
  expect(await buttonsOf(page)).toEqual(['Close']);
  await page.keyboard.press('Escape');
  await expect(detailDelete(page)).toBeFocused();
  await page.unrouteAll({ behavior: 'wait' });
  // Nothing was deleted.
  expect(await page.evaluate(async (id) => (await fetch(`/admin/api/offers/${id}`)).status, offer)).toBe(200);

  // 401 from the session gate.
  await answerDeletes(page, 401, { error: 'Unauthorized' });
  await openDelete(page, detailDelete(page));
  await dialog(page).getByRole('button', { name: 'Delete offer', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#form-status')).toHaveText(SESSION_ENDED);
});

test('every open starts empty: what was typed, before Cancel, is gone on the next open, for this record and for another row of the same name', async ({ page }) => {
  await signIn(page);
  const casey = seeded.participants['Casey Novak'];
  await open(page, `/admin/participants/${casey.id}`);
  await openDelete(page, detailDelete(page));
  const box = dialog(page);
  await box.getByLabel('Type Casey Novak to confirm', { exact: true }).fill('Casey Novak');
  await expect(box.getByRole('button', { name: 'Delete participant', exact: true })).toBeEnabled();
  await cancelIn(page).click();
  await openDelete(page, detailDelete(page));
  await expect(box.getByLabel('Type Casey Novak to confirm', { exact: true })).toHaveValue('');
  await expect(box.getByRole('button', { name: 'Delete participant', exact: true })).toBeDisabled();
  await expect(page.locator('#delete-dialog-alert')).toHaveText('');
  await page.keyboard.press('Escape');

  // Two rows with one name: typed for the first, nothing carries to the second.
  const name = 'Seed participant 001';
  const second = await idByName(page, 'participants', 'Seed participant 002');
  const renamed = await page.evaluate(async ([id, n]) => {
    const csrf = document.querySelector('meta[name="dw-csrf"]')!.getAttribute('content')!;
    const read = await fetch(`/admin/api/participants/${id}`);
    const reply = await fetch(`/admin/api/participants/${id}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf, 'If-Match': read.headers.get('ETag')! },
      body: JSON.stringify({ displayName: n }),
    });
    return reply.status;
  }, [second, name] as const);
  expect(renamed).toBe(200);
  await open(page, `/admin/participants?q=${encodeURIComponent(name)}`);
  const deletes = page.getByRole('button', { name: `Delete ${name}`, exact: true });
  await expect(deletes).toHaveCount(2);
  await openDelete(page, deletes.nth(0));
  await box.getByLabel(`Type ${name} to confirm`, { exact: true }).fill(name);
  await cancelIn(page).click();
  await expect(deletes.nth(0)).toBeFocused();
  await openDelete(page, deletes.nth(1));
  await expect(box.getByLabel(`Type ${name} to confirm`, { exact: true })).toHaveValue('');
  await expect(box.getByRole('button', { name: 'Delete participant', exact: true })).toBeDisabled();
});

test('while the server is asked, Delete, Edit and the actions are disabled; a Delete that has gone gives the focus to the main area', async ({ page }) => {
  await signIn(page);
  const desk = seeded.negotiations.desk;
  await open(page, `/admin/negotiations/${desk}`);
  let release!: () => void;
  const gate = new Promise<void>((resolve) => { release = resolve; });
  await page.route((url) => url.pathname.endsWith('/delete-intent'), async (route) => {
    await gate;
    await route.continue();
  });
  await detailDelete(page).click();
  await expect(detailDelete(page)).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Edit', exact: true })).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Expire negotiation', exact: true })).toBeDisabled();
  release();
  await expect(dialog(page)).toBeVisible();
  await expect(cancelIn(page)).toBeFocused();
  await cancelIn(page).click();
  await expect(detailDelete(page)).toBeEnabled();
  await expect(page.getByRole('button', { name: 'Edit', exact: true })).toBeEnabled();
  await expect(detailDelete(page)).toBeFocused();
  await page.unrouteAll({ behavior: 'wait' });

  // The Delete that opened it is gone when the dialog closes: the focus goes to the main area, never the body.
  await openDelete(page, detailDelete(page));
  await page.evaluate(() => { (document.getElementById('delete-button') as HTMLButtonElement).hidden = true; });
  await page.keyboard.press('Escape');
  await expect(dialog(page)).toBeHidden();
  await expect(page.locator('#main')).toBeFocused();
});

test('answers to the DELETE: Blocked shows the negotiations, 404 goes to the list which says so, 428 is unclear with Retry, 403 loads the page anew once; none deletes anything', async ({ page }) => {
  await signIn(page);

  // Blocked: a negotiation on the artifact started after the dialog opened.
  const lonely = await idByName(page, 'artifacts', 'Seed artifact 150');
  await open(page, `/admin/artifacts/${lonely}`);
  await openDelete(page, detailDelete(page));
  changeRows({ addNegotiationFor: lonely });
  await dialog(page).getByRole('button', { name: 'Delete artifact', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveAccessibleName("This artifact can't be deleted");
  await expect(dialog(page).locator('.delete-list a')).toHaveCount(1);
  expect(await buttonsOf(page)).toEqual(['Close']);
  await expect(dialog(page).getByRole('button', { name: 'Close', exact: true })).toBeFocused();
  expect(await busyMark(page)).toBe(false);
  await page.keyboard.press('Escape');

  // Deleted elsewhere in the same session while the dialog was open: that delete's own intent replaced the
  // dialog's token, so this DELETE is refused as ConfirmTokenInvalid, the dialog asks again, and that intent's
  // 404 sends the page to the list. A 404 on the DELETE itself is the route fake further down.
  const offer = seeded.offers.terms;
  await open(page, `/admin/offers/${offer}?sort=created`);
  await openDelete(page, detailDelete(page));
  const other = await page.context().newPage();
  await open(other, `/admin/offers/${offer}`);
  expect(await deleteThroughApi(other, 'offers', offer)).toBe(200);
  await other.close();
  await dialog(page).getByRole('button', { name: 'Delete offer', exact: true }).click();
  await page.waitForURL(`${ORIGIN}/admin/offers?sort=created`);
  await expectStyled(page);
  await expect(live(page)).toHaveText(NOT_FOUND);
  expect(await page.evaluate(() => history.state)).toBeNull();

  // 428 (no token reached the server): no clear answer, Retry.
  const accepted = seeded.offers.accepted;
  await open(page, `/admin/offers/${accepted}`);
  await answerDeletes(page, 428, { error: 'PreconditionRequired' });
  await openDelete(page, detailDelete(page));
  await dialog(page).getByRole('button', { name: 'Delete offer', exact: true }).click();
  await expect(page.locator('#delete-dialog-alert')).toHaveText(UNCLEAR_DELETE);
  await expect(dialog(page).getByRole('button', { name: 'Retry', exact: true })).toBeFocused();
  await page.unrouteAll({ behavior: 'wait' });
  await cancelIn(page).click();

  // 404 straight from the DELETE, on a list: the list loads itself anew and says so.
  const listed = '/admin/participants?q=Seed%20participant%20060';
  await open(page, listed);
  await answerDeletes(page, 404, { error: 'NotFound' });
  await openDelete(page, rowsOf(page).first().getByRole('button', { name: 'Delete Seed participant 060', exact: true }));
  await reloadedBy(page, () => dialog(page).getByRole('button', { name: 'Delete participant', exact: true }).click());
  expect(here(page)).toBe(listed);
  await expect(live(page)).toHaveText(NOT_FOUND);
  await page.unrouteAll({ behavior: 'wait' });

  // 403 on a page the owner loaded anew: it does not load itself again; nothing was deleted, with Retry.
  await page.reload();
  await expectStyled(page);
  await answerDeletes(page, 403, { error: 'Forbidden' });
  await openDelete(page, rowsOf(page).first().getByRole('button', { name: 'Delete Seed participant 060', exact: true }));
  await dialog(page).getByRole('button', { name: 'Delete participant', exact: true }).click();
  await expect(page.locator('#delete-dialog-alert')).toHaveText(NOTHING_DELETED);
  await expect(dialog(page).getByRole('button', { name: 'Retry', exact: true })).toBeFocused();
  await page.unrouteAll({ behavior: 'wait' });
  await cancelIn(page).click();
  await expect(rowsOf(page).filter({ hasText: 'Seed participant 060' })).toHaveCount(1);
});

test('a 403 on a list that loaded itself after a delete still loads it anew once', async ({ page }) => {
  await signIn(page);
  await open(page, '/admin/participants?q=Seed%20participant%2005');
  await openDelete(page, rowsOf(page).filter({ hasText: 'Seed participant 050' }).getByRole('button'));
  await reloadedBy(page, () => dialog(page).getByRole('button', { name: 'Delete participant', exact: true }).click());
  await expect(live(page)).toHaveText('Participant Seed participant 050 deleted.');
  await answerDeletes(page, 403, { error: 'Forbidden' });
  await openDelete(page, rowsOf(page).filter({ hasText: 'Seed participant 051' }).getByRole('button'));
  await reloadedBy(page, () => dialog(page).getByRole('button', { name: 'Delete participant', exact: true }).click());
  expect(here(page)).toBe('/admin/participants?q=Seed%20participant%2005');
  await expect(live(page)).toHaveText(RETRY);
  await page.unrouteAll({ behavior: 'wait' });
  await expect(rowsOf(page).filter({ hasText: 'Seed participant 051' })).toHaveCount(1);
});

test('deleting the one row of the last page: the list shows the page before it', async ({ page }) => {
  await signIn(page);
  await open(page, '/admin/participants?offset=200');
  // 205 participants: the last page holds 5. Four go by the API, the last by its row.
  const ids = await rowsOf(page).locator('button.delete-open').evaluateAll((bs) => bs.map((b) => b.getAttribute('data-delete-id')!));
  expect(ids).toHaveLength(5);
  for (const id of ids.slice(0, 4)) expect(await deleteThroughApi(page, 'participants', id)).toBe(200);
  await open(page, '/admin/participants?offset=200');
  await expect(rowsOf(page)).toHaveCount(1);
  await openDelete(page, rowsOf(page).locator('button.delete-open'));
  await reloadedBy(page, () => dialog(page).locator('button.danger').click());
  await expect(page).toHaveURL(`${ORIGIN}/admin/participants?offset=150`);
  await expect(rowsOf(page)).toHaveCount(50);
  await expect(live(page)).toHaveText(/^Participant .+ deleted\.$/);
  await expect(page.locator('#page-4')).toHaveAttribute('aria-current', 'page');
});
