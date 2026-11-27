# T05b — edit mode on the console detail pages (A7 Step 9, page side): review

| Field | Value |
|-------|-------|
| Scope | Edit mode on `/admin/{type}/{id}` (no new address): the controls the server writes into each detail page, the page script `console-edit.js`, the confirm dialogs for Close, Expire and Cancel offer, and the unsaved-changes guard |
| Branch | `claude/t05-edit-page` (pull request #74) |
| Rules | Story A7 Step 9 (UXR-B14 to B17); test design TD-ADM-UI-na-B10 (narrowed), na-expire, na-B11, na-B12, -axe and -kbd; the edit API's replies (`AdminEditEndpoints`, review record `2026-11-26__qa__verification__t05-edit-api.md`) |
| Reference | Draft pull request #41. Its page shell and test harness are not used. |
| Date | 2026-11-26 |
| Result | Pending: PASS once CI is green on the final head |

## What the page does

- **Edit is a mode of the same address.** An Edit button shows, beside each field the owner may change, its control; what cannot change stays text. Save sends one PATCH with only the changed fields and the detail read's `ETag` as `If-Match`. Cancel puts the page's values back.
- **What can change, per type.** A control is written only where the field policy lets the Core owner write the field and the edit API has a path for it:
  - participant: display name, and Active or Suspended;
  - artifact: the subject entity's name and description (a select picks the entity when there are several) and the owner, by typeahead;
  - negotiation: the end time, typed in ET, while it is open and not past its end;
  - offer: no field. On an offer the console can only cancel (the owner's decision of 26 Nov 2026), so the test design's Amount, Currency and Terms inputs are not built.
- **Actions** are buttons with a modal confirm dialog: Close and Expire on an open negotiation (Expire alone once its end time has passed), Cancel on an open offer. The Close and Expire dialogs say how many open offers will be cancelled, from the server's count.
- **Nothing for a soft-deleted record**, or a read without an edit token: no control, no dialog, no edit script.
- **Replies.**
  - 200: the page loads itself anew and says what was done in the shell's live region.
  - 400 with field messages: the server's sentences under each field and in a summary that takes the focus; values kept.
  - Other 400: "You can't do that. Nothing was changed."
  - 409: the server's sentence and Reload, which loads the record as it now is.
  - 401: sign-in with the session-ended note.
  - 403: the shell's reload-once.
  - No answer: the retry sentence.
- **Unsaved changes.** While a value differs, a link, Sign out, Back and picking another subject entity ask "Discard your changes?" in the page's own dialog. With nothing changed, nothing is asked.
- Without JavaScript the page only reads: every control starts hidden.

## How this was reviewed

A reviewer who did not write the code read the pull request adversarially, then re-reviewed the fixes. It covered:
- data exposure and encoding;
- the request and its tokens;
- reply handling;
- dialogs and focus;
- history and the guard;
- browser differences;
- the tests.

The builder and the reviewer were separate Claude Code agents.

## Findings and what was done

### First review: merge after fixes

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | High | At 320 px an artifact page was wider than the screen (340 px, WebKit 353), in every engine on Linux. A 36-character id stayed on one line in a grid cell that could not shrink. | Fixed. Detail cells may shrink and an id breaks inside its row. Tested at 320 px. |
| 2 | High | The 401 test expected `#ended` in the address, which the sign-in page takes off. | Fixed in the test. It now expects the sign-in page and its session-ended sentence. |
| 3 | Medium | Firefox puts values back on reload and on Back. After a 409 and Reload, a radio the browser put back, or a subject entity picked before, could be taken for the record's. | Fixed. The form has `autocomplete="off"`, and every control is set to the server's value before changes are measured. Tested: a 409, then Reload, with a radio and the entity select. |
| 4 | Low | Leaving edit mode by a link, Sign out or Discard left edit mode's history entry, so Back had to be pressed twice. | Fixed. Every way out removes the entry first. Tested: one Back after leaving by a link. |
| 5 | Low | A crafted `#edit-saved` (or `-closed`, `-expired`, `-cancelled`) made the page announce a change that had not happened. | Fixed. The note is kept in the history entry's state, which only the script sets just before it reloads, and is used once. Tested. |
| 6 | Low | Discard worked while a save was out. | Fixed. Nothing leaves while a save is out. Tested. |
| 7 | Low | Picking another subject entity silently dropped what was typed for the current one. | Fixed. It asks first. A stored name with a line break no longer counts as a change just by being shown. Tested. |
| 8 | Low | Back with changes pushed a new history entry inside `popstate`, which Chromium may skip. | Fixed. Keep editing now steps forward onto the existing entry. |
| 9 | Note | Missing tests: a soft-deleted artifact and negotiation; Sign out then Discard. | Added. |

### Re-review: merge after fixes (1 to 9 hold)

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 10 | High | On load, a page on edit mode's history entry stepped back in history. In WebKit, opening the same address again timed out; the likely cause, not confirmed, is that WebKit keeps the entry's history state. A first fix stepped back only after a real reload. It still hung on a WebKit reload, and in Firefox one Back after the reload stayed on the record. | Fixed. On any load that lands on edit mode's entry, the page makes the entry a plain one and starts in view mode. It never moves in history while loading. Tested with the page's own reload (`location.reload()`, as F5 is): the page starts in view mode, loads once and adds no history entry; Back then leaves within two steps and stays left. Playwright's `page.reload()` in Firefox loads the address as a new history entry, even on a page without this script (logged: `history.length` 2 to 3 on a plain reload), which made a third Back in CI. So the test does not use it. |
| 11 | Medium | In edit mode the skip link added a history entry that the page read as Back: edit mode closed, or the discard dialog opened and Back then needed pressing twice. | Fixed. A link within the page only moves the focus while editing, and adds no entry. Tested: the skip link keeps edit mode and the typing, puts the focus in main, and Back still asks once. |
| 12 | Low | After Edit, Back and Forward, a second Edit made Back do nothing once. | Fixed. Forward onto an old edit entry in view mode turns it into a plain entry. Tested. |
| 13 | Optional | After Back with changes, Keep editing put the focus on Cancel. | Fixed. It returns to the field being typed in. Tested. |

The reviewer found:
- **No injection.** Every value the server writes is encoded, and the script puts text in through `textContent` only.
- **Safe requests.** The PATCH sets no fetch mode and follows no redirect.
- **Nothing kept in the browser.** The script uses no web storage.
- **Nothing exposed.** The edit token is a comparison value, and the page carries nothing about the owner or the session.
- **Offers stay as sent.** No control on any page can change an offer's amount, currency or terms.

## Tests

- **.NET page tests.** `AdminEditPageTests`: 7 of 7. They cover the controls per type and state, everything starting hidden, the page's token being the one the edit API takes, soft-deleted records, encoding, nothing sensitive in the page, and the script's house rules.
- **Final head.** Run on Chromium here only; the result stays pending until CI is green on all three engines.
- **Full suite on Windows.** 2,078 tests: 2,067 passed, 6 skipped, 5 failed. The same five fail on untouched `main` on this machine. The six skipped are the PostgreSQL-only tests and one older placeholder.
- **Browser tests.** `e2e/console/edit.spec.ts`: 13 tests, all passing on Chromium here. GitHub runs them on Chromium, Firefox and WebKit on Linux. They cover:
  - each type's edit;
  - the server's sentences inline;
  - 409 with Reload;
  - the Expire, Close and Cancel offer dialogs (keyboard, Escape, backdrop, one request, Retry);
  - the guard (links, Back, Sign out, entity switch);
  - history (reload, skip link, Forward);
  - the session and token paths;
  - 320 px;
  - axe on the view, edit, error, conflict and dialog states.

## Limits to know about

1. After a save the page loads itself anew to show the new values, rather than updating them in place.
2. After a 409, Reload drops the typed values. The test design asks for them to stay beside the new version for reference; the page keeps nothing stale instead.
3. A reload, closing the tab or typing an address in edit mode is not asked about. The page does not use the browser's own leave prompt.
4. After a reload in edit mode, it takes one extra Back to leave the record: the record's own history entry is still behind the reloaded one. The page never moves in history while it loads, so it cannot remove that entry.
5. In edit mode the owner's row label and its field's label both read "Owner participant".
6. Return to the same record after signing in again (test design B13) is not covered here.

## New wording for the owner's approval

Taken from the test design: Save, Cancel, "Saving…", "Changes saved.", "Discard your changes?", Discard, Keep editing, Reload, Retry, "Nothing was changed.", "You can't do that.", "Expire negotiation {ID}?", "{N} open offers will be cancelled.", "No open offers will be cancelled.", Expire negotiation, "Expiring…", "Negotiation {ID} expired.". The field sentences and the 409 are the edit API's, already approved.

New, in `AdminConsoleCopy.cs`:

| Where | Sentence |
|-------|----------|
| Button that opens edit mode | Edit |
| Heading of the error summary | Your changes were not saved. |
| Any other refused change | You can't do that. Nothing was changed. (two test-design sentences joined) |
| Field labels | Display name · Subject entity · Name · " (required)" after a required field's label |
| Hint under Ends at | Eastern Time (ET). |
| Close | Close negotiation · Closing… · Close negotiation {ID}? · Negotiation {ID} closed. |
| One open offer | 1 open offer will be cancelled. |
| Cancel offer | Cancel offer · Cancelling… · Keep offer · Cancel offer {ID}? · Offer {ID} cancelled. |
| Cancel offer dialog body | Its amount, currency and terms stay as sent. Cancelled is final. |
