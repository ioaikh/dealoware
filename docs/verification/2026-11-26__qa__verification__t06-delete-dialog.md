# T06b — the delete dialog of the admin console (A7 Step 10, page side): review

| Field | Value |
|-------|-------|
| Scope | Delete on the detail pages and the list rows, the confirm dialog, the typed confirmation, the blocked artifact, the expired or changed token, and where the page goes after a delete |
| Branch | `claude/t06-delete-dialog` (pull request #78), on `main` with the delete server (#75) and the edit page branch (#74) |
| Rules | Story A7 Step 10; Spec §9 (confirm before delete), §11 (soft delete, cascades, open negotiation); test design TD-ADM-UI-del-01 to del-06 (UXR-C01 to C07), with the -axe and -kbd cases for the dialog |
| Server | The delete API of T06a (`AdminDeleteEndpoints`), unchanged |
| Reference | Draft pull request #43, used as a checklist only. None of its code is used. |
| Date | 2026-11-26 |
| Result | **PASS.** The review says MERGE after its second and third passes; the five required checks are the final record on the last commit. The owner accepted the six limits and approved the wording on 26 Nov 2026. |

## What was built

1. **Delete, where a record can be deleted.** Beside Edit and the actions on a detail page, and at the end of each list row's last cell. No column is added: the columns stay the test design's (TD-ADM-UI-na-B01). A row's Delete is named for its row ("Delete Alex Rivera") to a screen reader. A soft-deleted record has no Delete, on a list or on its page. Every Delete starts hidden; the script shows it, so without JavaScript nothing can be deleted.
2. **The server is asked first.** Delete sends `POST /admin/api/{type}/{id}/delete-intent`. While it is out, Delete, Edit and the actions are disabled. Then one modal dialog opens, with the focus on Cancel:
   - the title "Delete {type} {name}?" (a negotiation and an offer by their ID), the ID, what goes with it as a list of the server's own counts, an open negotiation named as open with its offers' count, the undo statement, the named destructive button and Cancel;
   - a typed confirmation for a participant with records that go with it, and for an open negotiation: the button works only when the name or ID is typed exactly, case and spaces included; pasted text counts;
   - for an artifact negotiations still use, "This artifact can't be deleted" and each negotiation as a link to its page. Close is the only button; there is no destructive button in the page at all.
3. **One DELETE.** The confirm token goes in `X-Admin-Confirm-Token`, never in an address, with the anti-forgery header. While it is out the button says "Deleting…" and is disabled, Cancel too, and the dialog does not close: a double click sends one request.
4. **Nothing deleted, asked again.** When the token is no longer valid, or the record or what goes with it changed, the dialog asks the server again and shows its fresh counts with a sentence. What was typed is kept for that answer.
5. **Afterwards.** The page goes to the list, with the state the detail page carried (search, sort, page, "Show deleted"), and the list says "{Type} {name} deleted." in the shell's live region. The row is gone; with "Show deleted" on it is back with the Deleted badge and no Delete. When the deleted row was the only one on the last page, the list shows the page before it (the list's existing rule for a page past the end).
6. **Failures.** The server's own fault (its audit row could not be written, so the whole delete rolled back) or its 400 keeps the dialog open with "Nothing was deleted." and Retry. A 404 (the record is gone, perhaps by a first try whose answer was lost) goes to the list, which says the record could not be found. A 401 goes to sign-in. A 403 loads the page anew once. No answer, or one the page does not know, says it could not confirm the delete, with Retry.
7. **House rules.** One shell (`AdminSignedInPage`), the nonce policy, the anti-forgery meta. `textContent` only, no web storage, no fetch `mode`. The dialog handles Escape itself in a capture-phase document listener that leaves text composition alone. Escape, Cancel, Close and the backdrop close it and give the focus back to the Delete at once (or to the main area, should the Delete be gone); never while a request is out. `aria-busy` is on the dialog only while a request is out. Every open starts empty. Answers that arrive as the page goes away are dropped. No new exemption from the session gate; the script is served only with a session.

## How this was reviewed

An independent reviewer read the committed code (cf16084, 22cbb2d). The builder and the reviewer were separate Claude Code agents. The review said MERGE AFTER FIXES.

A second pass read the fixes (22cbb2d..1f9a7b6) and accepted each, including the two items kept on purpose (rows 9 and 10). It found two Low points, both fixed in d4242d5: one test's comment named a path the test does not take, and the no-clear-answer sentence said "Reload" while the focus was on Retry. It now reads "We couldn't confirm the delete. Try again to see whether it went through."

A third pass read 21f0c95, a Firefox fix. Firefox reported Playwright's own `page.reload()` as a plain navigation, so on the first 403 the page rightly loaded itself anew once. The step now lets that first 403 reload the page, then checks that a second 403 on that page shows "Nothing was deleted." with Retry and does not reload. The rule "a page loads itself anew at most once" is still tested. The review said MERGE once WebKit, whose runner had failed to install the browser, ran green.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | High | What was typed carried over to the next open, after Cancel; on a list, to another row of the same name. | Fixed. Every close empties the dialog; a new open starts empty. Kept only when the server is asked again within one open. e2e: reopen after Cancel, and another row renamed to the same name. |
| 2 | High | Build & Test red: the shell and edit page tests pinned the old script lists and actions. | Fixed. Both updated; the new page tests committed. |
| 3 | Medium | `aria-busy` stayed on the dialog after the token, Blocked and 404 answers, and into later opens. | Fixed. Cleared on every answer and on close. e2e checks it after each answer. |
| 4 | Medium | The verification record was missing. | This record. |
| 5 | Medium | No e2e for 403, Blocked on the DELETE, 404 on the DELETE, 428, or reopen after Cancel; the rollback was faked only by the browser. | Added, each in delete.spec.ts. The rollback runs for real in .NET (below); in e2e the route fake only drives the page's handling of that answer. |
| 6 | Low | While the intent was out, Edit could start under the dialog; a hidden Delete left the focus on the body. | Fixed. Delete, Edit and the actions are disabled while it is out. A gone Delete gives the focus to the main area. e2e covers both. |
| 7 | Low | Deleting the only row of the last page might show an empty page. | Already handled by the list page (a page past the end shows the last one). e2e proves it lands on the page before. |
| 8 | Low | A 404 on the DELETE left the owner on the deleted record's page. | Fixed. The page goes to the list, which says the record could not be found. |
| 9 | Low | Loading the list with `location.reload()` made it a "reload", which turned off its one automatic reload over a refused anti-forgery token. | Fixed by keeping the reload (it is the only way the history entry's state reaches the next page) and treating a list that arrived after a delete as not reloaded by the owner. e2e: a 403 on such a list loads it anew once. |
| 10 | Low | The display name sat in `history.state`. | Kept on purpose, and noted here: the test design's locked sentence "{Type} {name} deleted." needs the name, and once the row is gone the list cannot read it. The state holds only that name (or that the record was missing), never the token or anything else, and the list takes it off as soon as it has read it. e2e checks the state is empty after arrival and after a reload. |
| 11 | Low | Two tests waited for an address that had not changed, and one used a fixed pause. | Fixed. They wait for the page's load event; the pause is gone. |

## Tests

- **`AdminDeletePageTests`** (new): which detail pages and rows have a Delete and which never do; each starts hidden; the dialog's markup, words and return address; encoding of a hostile name; the page's way through the delete API (intent, DELETE with the header, replay refused, the blocked answer); **TD-ADM-102 with the real audit-insert fault** (the delete rolls back, the answer is the exact 500 the script reads, the same token then deletes); the script's house rules and the server codes it reads.
- **`AdminDeleteApiTests.AuditInsertFails_NothingDeleted_OnEachRoute`** (T06a) holds the rollback for all four types.
- **`AdminConsoleShellTests`, `AdminEditPageTests`**: updated to the list pages' two new scripts and to Delete in the actions bar.
- **e2e `delete.spec.ts`** (new), Chromium locally; Firefox and WebKit run on GitHub: TD-ADM-UI-del-01 to del-06, axe on every dialog state (each type, typed, blocked, expired, failed, 320 px), keyboard-only use from a list row, 320 px, soft-deleted records without Delete, the session and token paths, and the review's cases above.
- **e2e `edit.spec.ts`**: now expects Delete beside the actions.

### Results, on Windows

- **.NET**, the `AdminConsole*`, `AdminEdit*` and `AdminDelete*` classes after the fixes: 346 tests, 337 passed, 9 skipped (PostgreSQL-only), 0 failed.
- **e2e on Chromium** after the fixes: `delete.spec.ts` and `edit.spec.ts`, 27 passed. Before the fixes, the whole console suite on Chromium: 83 passed, so Delete on the list and detail pages broke none of the list, audit, shell, security-settings or session-timer specs.
- **Firefox and WebKit** run on GitHub only (Admin UI CI). The pull request's required checks on its last commit are the final record.
- **Commits**: cf16084 (the dialog), 22cbb2d (its browser tests), 1e6474a and 500f78c (page tests and pinned script lists), df1f9f1 (the review's fixes), 1f9a7b6 (this record), d4242d5 (the second pass's two points), 21f0c95 (the Firefox test fix), and the final update of this record.
- **On GitHub**, before the final update: on 334db3b, Build & Test, PostgreSQL Sec Tests, Chromium and Firefox passed; WebKit stopped before any test, because the runner could not install the browser.

## The owner's decisions

On 26 Nov 2026 Ivan accepted the six limits below (with T06a's) and approved the new wording, including "This confirmation is no longer valid." in place of the test design's "This confirmation expired."

## New wording, for the owner's approval

All in `AdminDeleteCopy.cs`. The test design gives some of these words (UXR-C01 to C07); the rest are new.

From the test design: "Delete", "Delete {type} {name}?", "Delete {type}", "Cancel", "Deleting…", "This can't be restored from the admin.", "This artifact can't be deleted", "It is used by these negotiations:", "Close", "Type {value} to confirm", "{Type} {name} deleted.", "Nothing was deleted.", "Retry".

| Key | Sentence | When |
|-----|----------|------|
| `IdLine` | "ID: {id}" | The record's ID in the dialog |
| `AlsoDeleted` | "Also deleted with it:" | Above the counts |
| `NothingElse` | "Nothing else is deleted with it." | Nothing goes with the record |
| counts | "1 negotiation", "{n} negotiations", "1 offer", "{n} offers" | The list of counts |
| `OpenNegotiation*` | "This negotiation is open. It will no longer show in the lists unless “Show deleted” is on." / "This negotiation is open and has 1 offer. They will no longer show in the lists unless “Show deleted” is on." / "This negotiation is open and has {n} offers. They will no longer show in the lists unless “Show deleted” is on." | An open negotiation |
| `OneOpenNegotiation`, `OpenNegotiations` | "1 of these negotiations is open. It and its offers will no longer show in the lists unless “Show deleted” is on." / "{n} of these negotiations are open. They and their offers will no longer show in the lists unless “Show deleted” is on." | A participant with open negotiations |
| `BlockedMore` | "And {n} more." | More than 100 negotiations use the artifact |
| `ChangedReview` | "This record changed after this dialog opened. Nothing was deleted. Review the details again." | The delete API's `Conflict` |
| `NotFound` | "We can't find this record. It may have been deleted already." | A 404, said on the list |
| `UnclearDelete` | "We couldn't confirm the delete. Try again to see whether it went through." | No clear answer to the DELETE; the destructive button reads Retry |

Changed during the review, old and new:

| Key | Was | Now | Why |
|-----|-----|-----|-----|
| `Expired` | "This confirmation expired. Review the details again." (the test design's) | "This confirmation is no longer valid. Review the details again." | Also shown when another tab asked for a newer token, which is not an expiry |
| `ChangedReview` | "… after you opened the delete. …" | "… after this dialog opened. …" | Plainer |
| open warnings | "… It will leave the active lists …" | "… will no longer show in the lists unless “Show deleted” is on." | Says what happens |
| no clear answer to the DELETE | "That didn't go through. Wait a moment and try again." | `UnclearDelete` above | The delete may have gone through. The old sentence stays for the delete-intent, which changes nothing. |
| `UnclearDelete` | "We couldn't confirm the delete. Reload the page to see whether it went through." | "We couldn't confirm the delete. Try again to see whether it went through." | Matches the Retry button the focus is on (re-review) |

## Limits to know about

1. At 320 px a list row's Delete is in the last column, reached by scrolling the table sideways, as every column already is.
2. Typed confirmation is asked for a participant with records that go with it and for an open negotiation only (UXR-C06). A participant with none, an artifact, an offer and a closed negotiation are confirmed by the button alone.
3. The dialog cannot be used without JavaScript, by design: the delete needs the intent's token.
4. The time a token stays valid is the server's five minutes; the page does not count it down.
