# T07 — audit log viewer (S-D1, S-D2): review

| Field | Value |
|-------|-------|
| Scope | The audit list page, the audit entry page, three additions to the read-only audit API, and "Audit log" in the console menu |
| Branch | `claude/t07-audit-viewer` |
| Rules | Story A7 Step 15; Dev Plan Step 15; Spec audit sections; test design TD-ADM-100, 101, 053, 064, 150 and TD-ADM-UI-na-X02, X03 |
| Reference | Draft pull request #42, as a checklist and for labels only. It was written against a stand-in for the API. |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## How this was reviewed

A reviewer who did not write the code read the committed code adversarially while the builder was still writing the browser tests, then made a second pass over the fixes and the tests. It checked data exposure first, then:

- that the viewer only reads
- encoding
- bounded reads
- the outcome table
- the shell

The builder and the reviewer were separate Claude Code agents. The required checks on GitHub ran on every push.

## What the change does

- **`/admin/audit`** lists the log newest first. Columns are Time (ET), Actor, Action, Entity type, Entity ID and Outcome. Filters are action (several), entity type and a date range. It has chips, paging of 50, 100 or 200, and states for an empty log, no match and could-not-load. It has no search box, and no IP, snapshot or reason in the list.
- **`/admin/audit/{id}`** shows one entry. Before and after are shown as text, side by side, with a "Changed" mark on each field that differs. The 12-character prefix of the hashed IP appears here only. An unknown or malformed id gets "We can't find that page."
- **API:** `GET /admin/api/audit/{id}` returns one entry. The action filter takes several names from the fixed list. Each entry carries an outcome (success, failure or denied), worked out from the action and its reason class by one table, `AdminAuditOutcome`.
- **Read-only.** Every mutating method on the collection and on an entry answers 405. The pages have no control that changes anything.
- **Menu.** "Audit log" sits between Offers and Security settings.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important | The entry page and the API showed the reason of a failed step-up or password change. "bad_2fa" told the holder of a stolen session cookie that a guessed password was right. The activity feed already hid this. | Fixed in the read model, so the page, the entry API and the list API all withhold it. The builder also closed the same hole in the list's reason filter. The stored row keeps its reason. |
| 2 | Minor | The read-time filter for fields the owner may not read matched only one exact spelling. | Fixed. It ignores case and separators, and matches every field class the domain names. |
| 3 | Note | An action unknown to the table showed as "Success". Two reset actions were written as string literals. | Fixed. The literals are constants now. An unknown action ending in `_failed` or `_failure` shows as Failure. A test fails if an action is written without a row in the table. |
| 4 | Note | "IP HMAC prefix" showed the word `command` for entries written by the operator command. | Fixed. It shows "—". |
| 5 | Note | A no-op edit had been made to the public sign-in script. | Reverted. |
| 6 | Note | The page tests covered only "no session". | Added: a session whose second step is not done, and a session of another address. |

The reviewer found:

- No raw IP anywhere.
- No secret key reaches a page.
- No route changes anything.
- Every value is encoded.
- Paging and filters run in SQL, with the limit capped at 200.
- No open redirect.
- Nothing polls or keeps the session alive.

## Tests

- Full suite on Windows: 1,913 tests, 1,907 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on this machine.
- `e2e/console` on Chromium locally: 61 passed, including the 10 new audit tests. GitHub ran every suite in Chromium, Firefox and WebKit on each push.
- New copy: "Success", "Failure", "Denied" and "Reason". Everything else comes from the test design, draft #42's labels, or T04.

## Limits to know about

1. Action names and entity types show as stored (for example `entity_edit`), with no friendlier labels.
2. The list has no sort control and no actor filter. An actor can be an email address, which must not go into a URL.
3. An unknown action name in the API's filter is now a 400. Before, it returned no rows.
4. The correlation id is in the API but not on the page.
5. No screen-reader pass.
