import { test, expect, type Page, type Request } from '@playwright/test';
import { expectNoSeriousAxeFindings } from './axe';
import { seedEntities, type Seeded } from './entities';
import { authenticatorCode, seedOwner, type Owner } from '../sign-in/owner';
import { answerCloudflare } from '../signed-out/cloudflare';

/**
 * Edit mode of the detail pages (T05, A7 Step 9) against the real app and
 * its edit API, signed in through the real pages, over the entities
 * seed_entities.py writes (with its EDIT ROWS). Start it with
 * e2e/console/run.sh.
 *
 * What the server writes into each page (which controls, for which record)
 * is proven by the .NET tests (AdminEditPageTests); these prove what the
 * browser adds: edit mode and its one PATCH, the server's sentences inline,
 * the 409 and Reload, the confirm dialogs (keyboard, Escape, backdrop, one
 * request, Retry), the unsaved-changes guard, the session and token paths,
 * axe on view, edit, error and dialog states, and 320 px. Case ids:
 * TD-ADM-UI-na-B10 (narrowed: on an offer the console can only cancel),
 * na-expire, na-B11, na-B12, -axe and -kbd.
 */

const ORIGIN = 'https://admin.core.dealoware.com';
const SIGN_IN = '/admin/sign-in';
const CODE = '/admin/sign-in/code';
const PASS_TOKEN = process.env.CONSOLE_E2E_TURNSTILE_TOKEN ?? '';
const ARTIFACTS = process.env.CONSOLE_E2E_ARTIFACTS;

const CONFLICT = 'This record changed after you opened it. Reload it and try again.';
const RETRY = "That didn't go through. Wait a moment and try again.";
const SESSION_ENDED = 'Your session has ended. Sign in again to continue.';
const SIGNED_OUT = 'You've been signed out.';

/** The sign-in page with one of its notes: it takes the note off the address and says its sentence. */
async function expectSignInWith(page: Page, sentence: string): Promise<void> {
  await expect(page).toHaveURL(ORIGIN + SIGN_IN);
  await expect(page.locator('#form-status')).toHaveText(sentence);
  await expect(page.locator('.console-nav')).toHaveCount(0);
}

type EditSeeded = Seeded & {
  negotiations: Seeded['negotiations'] & Record<'expire-three' | 'expire-none' | 'window' | 'lapsed', string>;
  offers: Seeded['offers'] & Record<'expire-open-1' | 'expire-open-2' | 'expire-open-3' | 'expire-accepted', string>;
};

let owner: Owner;
let seeded: EditSeeded;
let cspMessages: string[] = [];
let pageErrors: string[] = [];

function engine(): string {
  return test.info().project.name;
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

async function open(page: Page, address: string): Promise<void> {
  await page.goto(address);
  await expectStyled(page);
}

function here(page: Page): string {
  const url = new URL(page.url());
  return url.pathname + url.search + url.hash;
}

/** Every PATCH the page sends, from here on. */
function patchesOf(page: Page): Request[] {
  const sent: Request[] = [];
  page.on('request', (r) => {
    if (r.method() === 'PATCH') sent.push(r);
  });
  return sent;
}

/** Holds every PATCH until released, so the page can be read while its request is out. */
async function holdPatches(page: Page): Promise<() => void> {
  let release!: () => void;
  const gate = new Promise<void>((resolve) => { release = resolve; });
  await page.route((url) => url.pathname.startsWith('/admin/api/'), async (route) => {
    if (route.request().method() !== 'PATCH') return route.fallback();
    await gate;
    await route.continue();
  });
  return release;
}

/** Answers every PATCH with the given reply, without reaching the server. */
async function answerPatches(page: Page, status: number, body: object): Promise<void> {
  await page.route((url) => url.pathname.startsWith('/admin/api/'), async (route) => {
    if (route.request().method() !== 'PATCH') return route.fallback();
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

const live = (page: Page) => page.locator('#console-status');
const editButton = (page: Page) => page.getByRole('button', { name: 'Edit', exact: true });
const saveButton = (page: Page) => page.getByRole('button', { name: 'Save', exact: true });

test.beforeEach(async ({ page }) => {
  seeded = seedEntities({ edit: true }) as EditSeeded;
  owner = seedOwner({ enrolled: true });
  cspMessages = [];
  pageErrors = [];
  page.on('console', (message) => {
    const text = message.text();
    if (!/content[- ]security[- ]policy/i.test(text)) return;
    if (text.includes('/favicon.ico')) return;
    const path = new URL(page.url()).pathname;
    if (path.startsWith('/admin/api/') || path.startsWith('/admin/assets/')) return;
    cspMessages.push(text);
  });
  page.on('pageerror', (error) => pageErrors.push(error.message));
  await answerCloudflare(page, { token: PASS_TOKEN });
});

test.afterEach(() => {
  expect(cspMessages).toEqual([]);
  expect(pageErrors).toEqual([]);
});

// The suites after this one start from the rows without the edit ones.
test.afterAll(() => {
  seedEntities();
});

test('participant: Edit shows Display name and Active or Suspended; Save waits for a change; "Saving…" and one PATCH; "Changes saved."; axe; a screenshot', async ({ page }) => {
  await signIn(page);
  const casey = seeded.participants['Casey Novak'];
  await open(page, `/admin/participants/${casey.id}`);
  await expect(editButton(page)).toBeVisible();
  await expect(page.locator('#edit-displayName')).toBeHidden();
  await expect(saveButton(page)).toBeHidden();
  await expectNoSeriousAxeFindings(page, 'edit_participant_view');

  await editButton(page).click();
  await expect(page.getByLabel('Display name', { exact: true })).toBeFocused();
  await expect(page.getByLabel('Display name', { exact: true })).toHaveValue('Casey Novak');
  await expect(page.getByRole('radio', { name: 'Suspended', exact: true })).toBeChecked();
  await expect(editButton(page)).toBeHidden();
  await expect(saveButton(page)).toBeDisabled();
  // What cannot change stays text.
  await expect(page.locator('.details')).toContainText('casey.novak@example.test');
  await expect(page.locator('main input:visible, main textarea:visible, main select:visible')).toHaveCount(3);
  expect(here(page)).toBe(`/admin/participants/${casey.id}`);
  await expectNoSeriousAxeFindings(page, 'edit_participant_edit');
  await shot(page, 'edit_participant_desktop');

  await page.getByLabel('Display name', { exact: true }).fill('Casey N. Novak');
  await page.getByRole('radio', { name: 'Active', exact: true }).check();
  await expect(saveButton(page)).toBeEnabled();

  const patches = patchesOf(page);
  const release = await holdPatches(page);
  await saveButton(page).dblclick();
  const saving = page.locator('#edit-save');
  await expect(saving).toHaveText('Saving…');
  await expect(saving).toBeDisabled();
  release();

  await expect(live(page)).toHaveText('Changes saved.');
  expect(patches).toHaveLength(1);
  expect(patches[0].postDataJSON()).toEqual({ displayName: 'Casey N. Novak', isActive: true });
  expect(patches[0].headers()['if-match']).toMatch(/^"\d+-[0-9a-f]{16}"$/);
  expect(patches[0].url()).toBe(`${ORIGIN}/admin/api/participants/${casey.id}`);
  await expect(page.locator('.details')).toContainText('Casey N. Novak');
  await expect(page.locator('.details .badge')).toHaveText(['Active']);
  await expect(editButton(page)).toBeFocused();
  expect(here(page)).toBe(`/admin/participants/${casey.id}`);
});

test('artifact: pick the subject entity when there are several; its description and the owner by typeahead; the page shows the new values', async ({ page }) => {
  await signIn(page);
  const parts = seeded.artifacts['Bike parts'];
  await open(page, `/admin/artifacts/${parts}`);
  await editButton(page).click();
  const ownerBox = page.getByRole('combobox', { name: 'Owner participant' });
  await expect(ownerBox).toBeFocused();
  await expect(ownerBox).toHaveValue('Dana Okafor');

  const entity = page.getByLabel('Subject entity', { exact: true });
  expect((await entity.locator('option').allTextContents()).sort()).toEqual(['Frame', 'Wheel']);
  await entity.selectOption({ label: 'Wheel' });
  await expect(page.getByLabel('Name (required)', { exact: true })).toHaveValue('Wheel');
  await expect(page.getByLabel('Description', { exact: true })).toHaveValue('Front wheel, 28 inch.');
  // Picking the entity is not a change.
  await expect(saveButton(page)).toBeDisabled();
  await page.getByLabel('Description', { exact: true }).fill('Front wheel, 28 inch.\nWith its tyre.');

  await ownerBox.fill('Blair');
  await page.getByRole('option', { name: 'Blair Chen', exact: true }).click();
  await expect(ownerBox).toHaveValue('Blair Chen');
  await expectNoSeriousAxeFindings(page, 'edit_artifact_edit');

  const patches = patchesOf(page);
  await saveButton(page).click();
  await expect(live(page)).toHaveText('Changes saved.');
  const body = patches[0].postDataJSON();
  expect(Object.keys(body).sort()).toEqual(['description', 'entityId', 'ownerParticipantId']);
  expect(body.description).toBe('Front wheel, 28 inch.\nWith its tyre.');
  expect(body.ownerParticipantId).toBe(seeded.participants['Blair Chen'].sub);
  await expect(page.locator('.details')).toContainText('With its tyre.');
  await expect(page.locator('.details')).toContainText('Blair Chen');
  await expect(page.locator('.details')).toContainText('Steel frame, 54 cm.');
});

test('a value the server refuses: its sentences under each field and in a summary that takes the focus and links to them; values kept; a refused change says so; axe', async ({ page }) => {
  await signIn(page);
  const bicycle = seeded.artifacts['Red bicycle'];
  await open(page, `/admin/artifacts/${bicycle}`);
  await editButton(page).click();
  const name = page.getByLabel('Name (required)', { exact: true });
  const ownerBox = page.getByRole('combobox', { name: 'Owner participant' });
  await name.fill('   ');
  await ownerBox.fill('nobody by this name');
  await expect(page.getByRole('option', { name: 'No matches', exact: true })).toBeVisible();
  const patches = patchesOf(page);
  await saveButton(page).click();

  const summary = page.locator('#edit-summary');
  await expect(summary).toBeFocused();
  await expect(summary.getByRole('heading')).toHaveText('Your changes were not saved.');
  await expect(summary.getByRole('link')).toHaveText(['Name is required.', 'Choose an existing participant.']);
  await expect(page.locator('#edit-name-error')).toHaveText('Name is required.');
  await expect(page.locator('#edit-ownerParticipantId-error')).toHaveText('Choose an existing participant.');
  await expect(name).toHaveAttribute('aria-invalid', 'true');
  await expect(ownerBox).toHaveAttribute('aria-invalid', 'true');
  await expect(name).toHaveValue('   ');
  await expect(ownerBox).toHaveValue('nobody by this name');
  expect(patches).toHaveLength(1);
  // Nothing typed comes back in an error.
  await expect(page.locator('main')).not.toContainText('nobody by this name', { useInnerText: true });
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'edit_errors');
  await shot(page, 'edit_errors_desktop');

  await summary.getByRole('link', { name: 'Name is required.' }).click();
  await expect(name).toBeFocused();
  expect(here(page)).toBe(`/admin/artifacts/${bicycle}`);
  await name.fill('Red bicycle, serviced');
  await expect(page.locator('#edit-name-error')).toHaveText('');

  // The generic refusal: one sentence, nothing changed, the values still there.
  await answerPatches(page, 400, { error: 'BadRequest' });
  await saveButton(page).click();
  await expect(page.locator('#edit-alert')).toHaveText("You can't do that. Nothing was changed.");
  await expect(page.locator('#edit-alert')).toBeFocused();
  await expect(name).toHaveValue('Red bicycle, serviced');
});

test('409: the server\'s sentence and Reload, typed values kept until then; Reload shows the record as it now is; axe; a screenshot', async ({ page }) => {
  await signIn(page);
  const dana = seeded.participants['Dana Okafor'];
  await open(page, `/admin/participants/${dana.id}`);
  await editButton(page).click();
  await page.getByLabel('Display name', { exact: true }).fill('Dana from the first tab');

  // Another tab saves first.
  const other = await page.context().newPage();
  await open(other, `/admin/participants/${dana.id}`);
  await editButton(other).click();
  await other.getByLabel('Display name', { exact: true }).fill('Dana from the second tab');
  await saveButton(other).click();
  await expect(live(other)).toHaveText('Changes saved.');
  await other.close();

  await saveButton(page).click();
  const conflict = page.locator('#edit-conflict');
  await expect(conflict).toBeFocused();
  await expect(conflict).toContainText(CONFLICT);
  await expect(page.getByLabel('Display name', { exact: true })).toHaveValue('Dana from the first tab');
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'edit_conflict');
  await shot(page, 'edit_conflict_desktop');

  await conflict.getByRole('button', { name: 'Reload', exact: true }).click();
  await expect(page.locator('.details')).toContainText('Dana from the second tab');
  await expect(editButton(page)).toBeVisible();
  await expect(page.locator('#edit-conflict')).toBeHidden();
  await expect(page.locator('#edit-displayName')).toBeHidden();
  await editButton(page).click();
  await expect(page.getByLabel('Display name', { exact: true })).toHaveValue('Dana from the second tab');
  expect(here(page)).toBe(`/admin/participants/${dana.id}`);
});

test('Expire negotiation: the server\'s count, focus on Cancel, Escape and the backdrop change nothing, "Expiring…" and one request, its open offers cancelled; axe; a screenshot', async ({ page }) => {
  await signIn(page);
  const id = seeded.negotiations['expire-three'];
  await open(page, `/admin/negotiations/${id}`);
  const expire = page.getByRole('button', { name: 'Expire negotiation', exact: true });
  const patches = patchesOf(page);

  // By keyboard.
  await expire.focus();
  await page.keyboard.press('Enter');
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await expect(dialog).toHaveAttribute('aria-modal', 'true');
  await expect(dialog).toHaveAccessibleName(`Expire negotiation ${id}?`);
  await expect(dialog.locator('#action-dialog-body')).toHaveText('3 open offers will be cancelled.');
  const cancel = dialog.getByRole('button', { name: 'Cancel', exact: true });
  const confirm = dialog.getByRole('button', { name: 'Expire negotiation', exact: true });
  await expect(cancel).toBeFocused();
  if (engine() !== 'webkit') {
    // The focus stays in the dialog.
    for (let i = 0; i < 4; i += 1) {
      await page.keyboard.press('Tab');
      expect(await focusInside(page, '#action-dialog')).toBe(true);
    }
  }
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'edit_expire_dialog');
  await shot(page, 'edit_expire_dialog_desktop');

  await confirm.focus();
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(expire).toBeFocused();
  // The backdrop.
  await expire.click();
  await expect(dialog).toBeVisible();
  await page.mouse.click(4, 4);
  await expect(dialog).toBeHidden();
  expect(patches).toHaveLength(0);
  await expect(page.locator('.details .badge')).toHaveText(['Open']);

  // A double click sends one request.
  await expire.click();
  const release = await holdPatches(page);
  await confirm.dblclick();
  const busy = page.locator('#action-dialog-confirm');
  await expect(busy).toHaveText('Expiring…');
  await expect(busy).toBeDisabled();
  release();
  await expect(live(page)).toHaveText(`Negotiation ${id} expired.`);
  expect(patches).toHaveLength(1);
  expect(patches[0].postDataJSON()).toEqual({ status: 'Expired' });
  await expect(page.locator('.details .badge')).toHaveText(['Expired']);
  // Final: no action left.
  await expect(page.locator('main').getByRole('button')).toHaveCount(0);

  for (const offer of ['expire-open-1', 'expire-open-2', 'expire-open-3'] as const) {
    await open(page, `/admin/offers/${seeded.offers[offer]}`);
    await expect(page.locator('.details .badge')).toHaveText(['Cancelled']);
  }
  await open(page, `/admin/offers/${seeded.offers['expire-accepted']}`);
  await expect(page.locator('.details .badge')).toHaveText(['Accepted']);
});

test('Expire refused: the dialog stays with "Nothing was changed." and Retry; Retry goes through; with no open offers the dialog says so', async ({ page }) => {
  await signIn(page);
  const id = seeded.negotiations['expire-none'];
  await open(page, `/admin/negotiations/${id}`);
  await page.getByRole('button', { name: 'Expire negotiation', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog.locator('#action-dialog-body')).toHaveText('No open offers will be cancelled.');

  await answerPatches(page, 400, { error: 'BadRequest' });
  await dialog.getByRole('button', { name: 'Expire negotiation', exact: true }).click();
  await expect(dialog.locator('#action-dialog-alert')).toHaveText('Nothing was changed.');
  const retry = dialog.getByRole('button', { name: 'Retry', exact: true });
  await expect(retry).toBeFocused();
  await expect(dialog).toBeVisible();
  await expectNoSeriousAxeFindings(page, 'edit_expire_failed');

  await page.unroute((url) => url.pathname.startsWith('/admin/api/'));
  await page.unrouteAll({ behavior: 'wait' });
  await retry.click();
  await expect(live(page)).toHaveText(`Negotiation ${id} expired.`);
  await expect(page.locator('.details .badge')).toHaveText(['Expired']);
});

test('negotiation: Ends at in ET, refused before Starts at with the server\'s sentence, saved after it; then Close; a lapsed one can only be expired', async ({ page }) => {
  await signIn(page);
  const id = seeded.negotiations.window;
  await open(page, `/admin/negotiations/${id}`);
  await editButton(page).click();
  const endsAt = page.getByLabel('Ends at', { exact: true });
  await expect(endsAt).toBeFocused();
  // 17:00 UTC on 15 January is noon in New York.
  await expect(endsAt).toHaveValue('2099-01-15T12:00');
  await expect(page.locator('#edit-endsAt-hint')).toHaveText('Eastern Time (ET).');

  await endsAt.fill('2098-12-31T09:00');
  await saveButton(page).click();
  await expect(page.locator('#edit-endsAt-error')).toHaveText('Ends at must be after Starts at.');
  await expect(page.locator('#edit-summary')).toBeFocused();

  const patches = patchesOf(page);
  await endsAt.fill('2099-02-01T09:30');
  await saveButton(page).click();
  await expect(live(page)).toHaveText('Changes saved.');
  expect(patches[0].postDataJSON()).toEqual({ endsAt: '2099-02-01T14:30:00.000Z' });
  await expect(page.locator('.details')).toContainText('Feb 1, 2099, 9:30 AM ET');

  await page.getByRole('button', { name: 'Close negotiation', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toHaveAccessibleName(`Close negotiation ${id}?`);
  await expect(dialog.locator('#action-dialog-body')).toHaveText('No open offers will be cancelled.');
  await expect(dialog.getByRole('button', { name: 'Cancel', exact: true })).toBeFocused();
  await dialog.getByRole('button', { name: 'Close negotiation', exact: true }).click();
  await expect(live(page)).toHaveText(`Negotiation ${id} closed.`);
  await expect(page.locator('.details .badge')).toHaveText(['Closed']);

  await open(page, `/admin/negotiations/${seeded.negotiations.lapsed}`);
  await expect(page.locator('main').getByRole('button')).toHaveText(['Expire negotiation']);
});

test('offer: no field to change, only Cancel offer; Keep offer has the focus, Escape keeps it; cancelled, its amount and terms as sent', async ({ page }) => {
  await signIn(page);
  const id = seeded.offers.terms;
  await open(page, `/admin/offers/${id}`);
  await expect(editButton(page)).toHaveCount(0);
  await expect(page.locator('main input, main textarea, main select')).toHaveCount(0);
  const cancelOffer = page.getByRole('button', { name: 'Cancel offer', exact: true });
  await cancelOffer.click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toHaveAccessibleName(`Cancel offer ${id}?`);
  await expect(dialog.locator('#action-dialog-body')).toHaveText('Its amount, currency and terms stay as sent. Cancelled is final.');
  const keep = dialog.getByRole('button', { name: 'Keep offer', exact: true });
  await expect(keep).toBeFocused();
  await expectNoSeriousAxeFindings(page, 'edit_cancel_offer_dialog');
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(cancelOffer).toBeFocused();

  const patches = patchesOf(page);
  await cancelOffer.click();
  await dialog.getByRole('button', { name: 'Cancel offer', exact: true }).click();
  await expect(live(page)).toHaveText(`Offer ${id} cancelled.`);
  expect(patches.map((p) => p.postDataJSON())).toEqual([{ status: 'Cancelled' }]);
  await expect(page.locator('.details .badge')).toHaveText(['Cancelled']);
  await expect(page.locator('.details')).toContainText('1,250.50 USD');
  await expect(page.locator('.details')).toContainText('Cash or bank transfer.');
  await expect(page.locator('main').getByRole('button')).toHaveCount(0);
});

test('unsaved changes: nothing asked with nothing changed; a link, Back and Sign out ask "Discard your changes?"; Keep editing keeps them, Discard leaves; Cancel puts them back; axe', async ({ page }) => {
  await signIn(page);
  const alex = seeded.participants['Alex Rivera'];
  const address = `/admin/participants/${alex.id}`;
  await open(page, address);
  const name = page.getByLabel('Display name', { exact: true });

  // Nothing changed: Back leaves edit mode, and a link leaves the page, without a question.
  await editButton(page).click();
  await page.evaluate(() => history.back());
  await expect(name).toBeHidden();
  await expect(editButton(page)).toBeFocused();
  expect(here(page)).toBe(address);
  await editButton(page).click();
  await page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Offers', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/offers');

  await open(page, address);
  await editButton(page).click();
  await name.fill('Alex R.');
  const discard = page.getByRole('dialog', { name: 'Discard your changes?' });
  const keep = discard.getByRole('button', { name: 'Keep editing', exact: true });

  // A link.
  await page.getByRole('link', { name: 'Back to list', exact: true }).click();
  await expect(discard).toBeVisible();
  await expect(keep).toBeFocused();
  await expectStyled(page);
  await expectNoSeriousAxeFindings(page, 'edit_discard_dialog');
  await keep.click();
  await expect(discard).toBeHidden();
  await expect(name).toHaveValue('Alex R.');
  expect(here(page)).toBe(address);

  // Escape is Keep editing.
  await page.getByRole('link', { name: 'Back to list', exact: true }).click();
  await expect(discard).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(discard).toBeHidden();
  await expect(name).toHaveValue('Alex R.');

  // Back.
  await page.evaluate(() => history.back());
  await expect(discard).toBeVisible();
  await keep.click();
  await expect(name).toHaveValue('Alex R.');
  expect(here(page)).toBe(address);

  // Sign out.
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await expect(discard).toBeVisible();
  await keep.click();
  await expect(name).toHaveValue('Alex R.');
  expect(here(page)).toBe(address);

  // Cancel: the page's values back, no question.
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  await expect(discard).toBeHidden();
  await expect(name).toBeHidden();
  await expect(page.locator('.details')).toContainText('Alex Rivera');
  await editButton(page).click();
  await expect(name).toHaveValue('Alex Rivera');

  // Discard leaves.
  await name.fill('Alex R.');
  await page.getByRole('navigation', { name: 'Admin' }).getByRole('link', { name: 'Stats', exact: true }).click();
  await expect(discard).toBeVisible();
  await discard.getByRole('button', { name: 'Discard', exact: true }).click();
  await expect(page).toHaveURL(ORIGIN + '/admin/');
  await open(page, address);
  await expect(page.locator('.details')).toContainText('Alex Rivera');
});

test('the session and the token: a 401 goes to sign-in with "session ended"; a refused token loads the page anew once, which says to try again; no answer says to try again', async ({ page }) => {
  await signIn(page);
  const eli = seeded.participants['Eli Moreau'];
  const address = `/admin/participants/${eli.id}`;

  // No answer at all.
  await open(page, address);
  await page.route((url) => url.pathname.startsWith('/admin/api/'), async (route) => {
    if (route.request().method() !== 'PATCH') return route.fallback();
    await route.abort('failed');
  });
  await editButton(page).click();
  await page.getByLabel('Display name', { exact: true }).fill('Eli M.');
  await saveButton(page).click();
  await expect(page.locator('#edit-alert')).toHaveText(RETRY);
  await expect(page.getByLabel('Display name', { exact: true })).toHaveValue('Eli M.');
  await page.unrouteAll({ behavior: 'wait' });

  // 403: the token was refused; still signed in, so the page loads anew once.
  await open(page, address);
  await answerPatches(page, 403, { error: 'Forbidden' });
  await editButton(page).click();
  await page.getByLabel('Display name', { exact: true }).fill('Eli M.');
  await saveButton(page).click();
  await expect(live(page)).toHaveText(RETRY);
  expect(here(page)).toBe(address);
  await expect(page.locator('#edit-displayName')).toBeHidden();
  await page.unrouteAll({ behavior: 'wait' });

  // 401 from the session gate.
  await open(page, address);
  await answerPatches(page, 401, { error: 'Unauthorized' });
  await editButton(page).click();
  await page.getByLabel('Display name', { exact: true }).fill('Eli M.');
  await saveButton(page).click();
  await expectSignInWith(page, SESSION_ENDED);
});

test('keyboard only: Edit, a field, Enter saves; at 320 px nothing scrolls sideways and every control is big enough; axe; a screenshot', async ({ page }) => {
  await signIn(page);
  const blair = seeded.participants['Blair Chen'];
  await open(page, `/admin/participants/${blair.id}`);
  await editButton(page).focus();
  await page.keyboard.press('Enter');
  const name = page.getByLabel('Display name', { exact: true });
  await expect(name).toBeFocused();
  await expect.poll(() => name.evaluate((el) => getComputedStyle(el).outlineStyle)).toBe('solid');
  await page.keyboard.press('ControlOrMeta+a');
  await page.keyboard.type('Blair C.');
  await page.keyboard.press('Enter');
  await expect(live(page)).toHaveText('Changes saved.');
  await expect(page.locator('.details')).toContainText('Blair C.');

  await page.setViewportSize({ width: 320, height: 900 });
  await open(page, `/admin/artifacts/${seeded.artifacts['Bike parts']}`);
  await editButton(page).click();
  const size = await page.evaluate(() => ({ content: document.documentElement.scrollWidth, viewport: document.documentElement.clientWidth }));
  expect(size.content).toBeLessThanOrEqual(size.viewport);
  for (const target of await page.locator('main button:visible, main input:visible, main select:visible, main textarea:visible').all()) {
    const box = await target.boundingBox();
    if (!box || box.width === 0) continue;
    expect(Math.round(box.height)).toBeGreaterThanOrEqual(24);
    expect(Math.round(box.width)).toBeGreaterThanOrEqual(24);
  }
  await expectNoSeriousAxeFindings(page, 'edit_artifact_320');
  await shot(page, 'edit_artifact_320');
});
