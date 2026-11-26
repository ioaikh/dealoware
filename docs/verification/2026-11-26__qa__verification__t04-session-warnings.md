# T04 piece 5 — session warnings and "Stay signed in": review

| Field | Value |
|-------|-------|
| Scope | Warnings before a signed-in session ends, the status read and the renewal behind them |
| Branch | `claude/t04-session-warnings` |
| Rules | Spec §8.7 (30 minutes idle, 8 hours absolute); test design TD-ADM-176, TD-ADM-UI-auth-14 and -24 |
| Date | 2026-11-26 |
| Result | **PASS**, after the fix below |

## How this was reviewed

A reviewer who did not write the code read the change adversarially, looking first at anything that could keep a session alive or extend it, then at the script, the test helper that ages a session row, and regressions. The builder and the reviewer were separate Claude Code agents. A third pass ran the full suite.

## What the change does

- **`GET /admin/api/session`** gives the idle end and the 8-hour end. It is the one request the session gate does not count as activity: a GET of exactly that canonical path. Every other request, and every other spelling of that path, renews the idle window as before. The full session check still runs first.
- **`POST /admin/api/session/stay-signed-in`** renews the idle window only, never the 8-hour end. It is behind the Origin and anti-forgery checks and reads nothing the client sends.
- **"Still there?"** (UI-auth-14) opens 2 minutes before the idle end, with "Stay signed in" and "Sign out".
- **"Your session is ending"** (UI-auth-24) opens 2 minutes before the 8-hour end, with one "OK".
- At the end the page reloads, and the gate sends it to sign-in with "Your session has ended", keeping the page for after sign-in.
- No timer keeps the session alive. Only the owner pressing "Stay signed in", or using the console, does.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Minor | A status reply sent before "Stay signed in" but arriving after it could reopen "Still there?" with stale times, for up to 2 minutes. | Fixed. Requests are numbered, and a status reply older than the latest "Stay signed in" is dropped. A browser test holds such a reply back and then releases it. Before the fix the test failed on Chromium. |
| 2 | Note | The code comment said the 8-hour dialog never comes back once closed. It does come back on another page opened in the last 2 minutes. | Comment corrected. TD-ADM-UI-auth-24 allows this. |
| 3 | Note | With several tabs open, every tab reloads to sign-in at the end. The page kept for after sign-in is the tab that reloaded last. | Accepted. No admin data stays on screen after expiry. |
| 4 | Note | If the idle end comes first and the 8-hour end is less than 2 minutes after it, the 8-hour dialog shows and no "Stay signed in" is offered. | Accepted. This matches the test design's case where both warnings fall in the same 2 minutes; the owner loses at most 2 minutes. |
| 5 | Note | A future page that asks before unloading could stop the timer if the owner cancels. | To remember when edit pages are built (T05). |

The reviewer found:
- No way to extend a session beyond 8 hours or keep it alive without the owner.
- No way for another site to read the status or renew a session.
- The test helper `e2e/console/age_session.py` refuses any database but a test run's own.

## Tests

- Full suite on Windows: 1,745 tests, 1,739 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on this machine. After the fix, `AdminSessionTimerTests` and `AdminConsoleShellTests`: 143 of 143 passed.
- `e2e/console`: 24 tests in each of Chromium, Firefox and WebKit (9 for the warnings). On GitHub all five checks passed before the fix.

## New copy, waiting for the owner's wording

- "Your session will end within 2 minutes because there has been no activity."
- "Your session will end within 2 minutes because of the 8-hour limit, and it can't be extended. Anything you haven't saved will be lost."

The locked UXR-A38 and UXR-A45 texts are in the UX addendum, which is not in this repository. The titles and buttons come from the test design.

## Limits

1. TD-ADM-176 words the ending "Your session ended. Sign in again to continue."; the page keeps the existing "Your session has ended. Sign in again to continue."
2. "Stay signed in" after a new sign-in in another tab shows the retry sentence; it does not reload like "Sign out" does.
3. Unsaved-form cases of the test design wait for the edit pages. No screen-reader pass.
