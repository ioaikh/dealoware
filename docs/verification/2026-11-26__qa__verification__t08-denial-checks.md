# T08a — the admin console's final denial checks and the offer-history test: review

| Field | Value |
|-------|-------|
| Scope | One route table of every `/admin` route, checked against the app's endpoints; one denial test over every row; a signed-out browser walk; the offer-history test, also in the PostgreSQL job |
| Branch | `claude/t08-denial-checks` (pull request #80) |
| Rules | Story A7 Steps 11 to 13; Dev Plan Steps 11 to 13; Spec §3, §5, §8.1, §12; test design TD-ADM-001 to 005, 101, 131, 141, 173, 175, 178, 179, UI-na-X01, UI-na-B13 |
| Date | 2026-11-26 |
| Result | **MERGE**, after the fixes listed below |

## What was built

Tests only. No product code changed, and no denial check found a hole.

1. **The route table** (`AdminRouteTable.cs`). Every route the app maps under `/admin` is one row: its pattern as mapped, its methods, its class (signed out, sign-in in progress, signed in, test only), and, for a page a browser opens, its Spec X01 screen. A route is added with one line.
   - A test reads the app's endpoint data source and fails on a mapped route with no row, and on a row with no route. Leaving the test-only probe out of the table shows that it is caught.
   - The signed-out and sign-in-in-progress rows must be exactly the gate's exemption list, with the right token kind. The anti-forgery exemption list must be empty.
   - Every page row is one Spec X01 screen, and every X01 screen is a page row or says why it has none: S-A11 is S-A1 opened at `#ended`; S-C1 is the delete dialog; Toggle is a list parameter; S-D3 is the gate's answer to an unknown page; S-D4 is not built (T08 architecture step, question 2).
   - The test exception probe is compiled into Debug builds only. The table marks it so, and the check passes in both Debug and Release.
   - The two static folders (`/admin/auth`, before the gate; `/admin/assets`, after it) are listed too. A source scan of the API project finds exactly these two static mounts and no other way of serving files from disk. A live probe asks for every other file on disk under `/admin` without a session and gets the gate's refusal.
2. **The denial test** (`AdminRouteDenialTests`), one theory per class, each over every row it applies to. Each refusal is checked for its status, that the reply has no record data (seeded names, both e-mail addresses, terms, every id), and that the four entity tables are unchanged.

   | Check | Rows | Expected | Case |
   |-------|------|----------|------|
   | No cookie; a forged one; an unknown id; idle over 30 minutes; over 8 hours; second step not done; another address; a signed-out session; an engine token; a sign-in in progress only. Each mapped method and OPTIONS. | Signed in, test only | Page GET: 302 to sign-in with the "session ended" note, empty body. Anything else: 401 `{"error":"Unauthorized"}`. | 002, 003, 004 |
   | No pending token; a forged, run-out or used one | Sign-in in progress | Page GET: 302 to step 1. Anything else: 401. A usable token opens step 2 (200). | 004 |
   | No session | Signed out | On the exemption list; its page answers 200 with `no-store` | 003 |
   | No Origin or Referer; another site's Origin; Origin `null`; another site's Referer; no anti-forgery header; a wrong one; a header without its cookie. A good session. | Every row with a change method | 403 with the generic body; the session survives; no audit row | 003, 175 |
   | Every method the route does not map (GET, HEAD, OPTIONS, POST, PUT, PATCH, DELETE), with a good session | Every pattern | 405, or S-D3's 404 for a page GET. Never a success. | 003 |
   | POST, PUT, PATCH, DELETE | The audit routes | 405; the log and the entry unchanged | 101 |
   | GET and HEAD of both sign-out paths | Sign-out | 405; the session stays good; no audit row | 175 |
   | A good session | Every signed-in page and read, and every signed-in file | 200 with `Cache-Control: no-store` | 173 |
   | A missing id and a soft-deleted id, with the same request | Every signed-in route with a record id (read, page, edit, delete intent, delete) | The same status, body (the page's nonce and token masked) and size | 179 |
   | Bad values in editable fields, and every field the owner may not write | Every signed-in write | Each route's own answer (400, 428, 403, or 200 for a route that reads no body); the values never repeated; no record changed | 005, 141 |
   | Every row and every file | On the engine's address | 404, empty body | 001 |
   | Every engine route | On the admin address | 404, empty body | 001 |
   | Every engine route | On the engine's address, with and without the admin session cookie | The same answer | 002 |
   | The test-only routes | In Production | Not mapped; 404 with a good session | 001 |

3. **The signed-out browser walk** (`e2e/console/signed-out-walk.spec.ts`), five tests.
   - **TD-ADM-131, UI-na-X01 step 3.** Every signed-in page of the table, and an unknown page, opened signed out, lands on sign-in with the session-ended sentence. Every reply on the way (the redirect, the page, its scripts) and the page at the end are searched for record data; none has any. A .NET test holds the walk's page list to the table both ways, so a new page must be walked.
   - **TD-ADM-173.** After sign-out, Back twice and reload: each record page is asked for anew, sent to sign-in, and shows no record data. Nothing comes from a cache.
   - **UI-na-X01 step 2, TD-ADM-179.** Signed in, an unknown page, an unknown id of each type, an unknown audit entry and a deleted record of each type are S-D3 (404) with no record data.
   - **UI-na-B13 steps 1 and 2.** A session that ends while a list is open: the record link goes to sign-in with no record data, and signing in opens that record.
   - **UI-na-B13 step 3.** A session that ends while an edit is open (the edit page from pull request #74): Save gets the gate's 401 and goes to sign-in with the session-ended sentence. No reply carries record data, and nothing is saved. Signing in lands on Stats, not on the record: see finding N1.
4. **The offer-history test** (`AdminOfferHistoryTests`, TD-ADM-178).
   - **The data.** Offers in every status (Open, Accepted, Declined, Withdrawn, Superseded, Cancelled), one already deleted, several open offers beside the ones the actions touch.
   - **The actions.** Edit a participant, an artifact and an end time; close, expire, cancel; delete an offer, an open and a closed negotiation, a participant and an artifact, each with its cascade. Each on fresh records, and all in turn on one set.
   - **After each.** Every offer row is still in the table. Its negotiation, sender, receiver, amount, currency, terms (as UTF-8 bytes) and sent time are identical. Its status moved Open to Cancelled or not at all. A deleted row stays deleted. The offers whose status or delete mark changed are exactly the ones the action names.
   - **The refused versions.** A stale version (409); writing amount, currency, terms, sent time, sender, receiver or negotiation; setting Accepted, Withdrawn, or Open again; a delete without its confirm token (428); a blocked artifact delete (409). Each changes nothing in the four entity tables.
   - **A source check.** No code in an `Admin` folder under `src/` removes rows from the offers table. The check first proves it finds what it looks for.
   - **CI.** The PostgreSQL job's filter now includes `AdminOfferHistoryTests`.

## How this was reviewed

One reviewer read the committed code up to 34f9de8. The reviewer did not write it. The builder and the reviewer were separate Claude Code agents. Verdict: **MERGE AFTER FIXES**; the piece was found sound.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| M1 | Medium | The offer-history test took Open to Cancelled, or a new delete mark, on any offer, and checked only the target of the action. An action that also cancelled or deleted other offers would have passed. | Fixed. Each action's changed set must equal the set it names. A third open offer sits beside the ones touched, and other negotiations hold open offers. Shown by widening the offer cancel to its siblings for one run: both tests failed. Reverted. |
| M2 | Medium | The static mounts were typed into the table by hand and not checked against the app. A third mount under `/admin` before the gate would have served its files with no test failing. | Fixed. A source scan of the API project: exactly two `UseStaticFiles`, one `PhysicalFileProvider`, none of `MapStaticAssets`, `UseFileServer`, `UseDirectoryBrowser`, `MapFallbackToFile` or a file result; each mount's text and place against the gate; `wwwroot/admin` holds the signed-out folder only. A live probe asks for every other file on disk under `/admin` without a session and gets the gate's refusal. |
| M3 | Medium | TD-ADM-131, UI-na-B13 and this record were missing. | Done. The walk's titles carry the IDs; this record is the last commit. |
| L1 | Low | OPTIONS was not among the methods tried. | Added to the no-session and wrong-method checks. |
| L2 | Low | TD-ADM-179 names a hidden record; for DELETE the token refusal comes before any lookup. | Said in the test. The policy hides fields from the Core owner, never whether a record exists, so there is no hidden third case. The DELETE stops at its confirm token, which no missing or deleted record can have, so it too answers both alike. |
| L3 | Low | Nothing showed the pending cookie was built right. | Added: a usable pending token opens step 2 (200). |
| L4 | Low | Route patterns were compared without normalising the leading slash. | Normalised. |
| L5 | Low | The write test checked a status for PATCH only. | Every write route now has its expected answer; a new write route without one fails. |
| L6 | Low | The denial host named a PostgreSQL database it never used. | Dropped. The denial checks run on SQLite. |
| L7 | Low | The source check's reach. | Noted under Limits. |
| N1 | Low, for the owner | Found by the B13 step 3 walk after #74 merged. When the session ends during an edit save, signing in lands on Stats, not on the record that was open. The test design asks for the same record. The edit script's 401 is an API reply, and the gate keeps a page to return to only for a page GET. Not a security gap: no record data is shown and nothing is saved. | Not changed: fixing it changes built behaviour. Open for the owner. The walk checks what holds and does not assert the landing page. |

## Tests

- **`AdminRouteDenialTests`** on Windows with SQLite: 221 tests in a Debug build, 219 in Release (the Debug-only probe row), all passed. Run again after `main` with #74 and #78 was merged in: still 221, all passed, with the new `console-edit.js` and `console-delete.js` in the signed-in file checks. The two pull requests add no route.
- **`AdminOfferHistoryTests`** on Windows with SQLite: 26 tests, all passed.
- **Mutation check:** with the offer cancel widened to the other open offers of its negotiation, the single-action and the all-in-turn tests failed.
- **The browser walk** on Chromium through the local runner: 4 tests passed; after the merge, with step 3 added, 5 passed. The whole console suite on Chromium: 67 passed.
- **Full suite on Windows** (Debug, SQLite): 2,389 tests, 2,373 passed, 10 skipped, 6 failed. Five are the known Windows failures on `main`: `AdminTotpTests.SecSc6_NewServiceInstanceKeepsFactorLock` and four `PostmanCollectionHygieneTests.RunNewmanScript_*`. The sixth, `AdminAuditPostgresAppendOnlyTests.SecR2_AdminAuditPostgres_InsertWorks_UpdateDeleteTruncateRaise`, needs `DB_HOST` and fails on any machine without PostgreSQL; see Limits.
- **Tests already on `main` that must stay green** (TD-ADM-006, 007, 011, 021, 041, 054, 150): passed in the full run.
- **CI:** the PostgreSQL job runs `AdminOfferHistoryTests`; GitHub runs the walk on Chromium, Firefox and WebKit. The pull request's five required checks on its last commit are the final record.

## Limits to know about

1. The source check covers only the `Admin` folders under `src/`. Shared services an admin route calls are covered at run time by the behaviour tests, not by the scan.
2. The offer history is protected by the admin code and these tests, not by a database rule. The rule that makes it unalterable for everyone is a later task.
3. TD-ADM-UI-na-B13 step 4 is not walked: it needs S-D4 ("You can't do that."), which is not built. Whether to build it is a question for the owner (T08 architecture step, question 2; recommended: do not build it). Step 3 lands on Stats after sign-in, not on the record (finding N1).
4. Pull requests #74 (the edit page) and #78 (the delete dialog, `AdminDeletePage`) are merged in and covered: they add no route, and their signed-in files are in the file checks, which read the folder from disk.
5. `AdminAuditPostgresAppendOnlyTests.SecR2_AdminAuditPostgres_InsertWorks_UpdateDeleteTruncateRaise` fails on any machine without `DB_HOST`. It is not touched here and runs in the PostgreSQL job.
