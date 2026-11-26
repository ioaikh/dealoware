# T04 piece 4 — security settings page (S-A12) and the recovery-code banner: review

| Field | Value |
|-------|-------|
| Scope | The page at `/admin/settings/security`, the recovery-code banner (UXR-A40), and the small server change behind the banner |
| Branch | `claude/t04-security-settings-page` |
| Rules | S-A12 password-change note v1.4; test design TD-ADM-UI-auth-10, 13, 16, 18, 19, 20 and TD-ADM-171, 174 |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## How this was reviewed

A reviewer who did not write the code read the change adversarially, covering, in order:
1. secrets in the browser;
2. the banner and its server change;
3. the step-up logic;
4. the helpers loaded from the signed-out pages;
5. regressions.

The builder and the reviewer were separate Claude Code agents. The required checks on GitHub found the main fault: it showed on Linux WebKit only. The builder reproduced it there in a container before fixing it.

## What the change does

- **Change password.** The fields follow the order in note v1.4, with show/hide, a mismatch check and a recovery-code switch. After a refusal the current password and the code are cleared and the new password is kept. Success ends every session and lands on sign-in.
- **Recovery codes.** The page shows how many are left. "Create new codes" asks for a step-up in a dialog. The new codes are shown once, with copy and an "I have saved these" box.
- **Authenticator.** "Set up again" asks for a step-up, then shows a new QR code and key. A code from the new app confirms it, and new codes are shown once.
- **Recent sign-in activity.** The owner's last 20 events, in Eastern Time, with hashed networks and no IP address, plus the 24-hour lock notice.
- **Recovery-code banner.** Signed-in pages show it after a sign-in with a recovery code, with the number of codes left and a link to this page. It can be dismissed per session.
- **Server change.**
  - A new table, `AdminSessionRecoveryNotices` (migration 500, additive only), records that a session signed in with a recovery code. The row is written in the same save as the session and is deleted with it.
  - A new POST route dismisses the banner. It sits behind the session gate, the Origin check and the anti-forgery token.
  - No existing contract changes.
- **Secrets.** Codes, the key and the QR code exist only in the page while they are shown. They are written with `textContent` and SVG elements, are never stored, and are cleared on Continue, on Cancel and on leaving the page.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important | On Linux WebKit, Escape typed in the step-up dialog's password or code field did not close the dialog. The page relied on the browser's own close request, which that engine does not raise from a text field. A Safari user could not close the dialog from the keyboard. | Fixed in the page. It now handles Escape itself while the dialog is open, and closes the dialog unless a request is out. Reproduced before the fix and confirmed after it on Linux WebKit. |
| 2 | Minor | In Chromium, a second Escape while a request was out could close the dialog, and a refusal arriving afterwards was then not shown. | Fixed by the same handler, which listens on the document. A new test holds a reply, presses Escape twice and checks that the refusal shows in the open dialog. |
| 3 | Minor | A shell test's "one live region" check had been narrowed for every page to make room for this one. | Restored for every other page. This page asserts its own exact set of live regions, and a new .NET test covers the shell markup the shared test no longer checks here. |
| 4 | Note | If the application ran before migration 500, every signed-in page would answer 500. | The banner lookup now fails soft: the page is served with no banner, and a warning logs the exception type only. The sign-in write still fails loudly. The deploy order (migration first) is noted in the code. |
| 5 | Note | If the reply to "Create new codes" or to the re-enrolment confirm is lost after the server committed, the new codes are never seen. | Accepted. The page shows the retry sentence and reads the count again; the owner creates new codes once more. |
| 6 | Note | The banner's count is stale on this page after new codes are created, until the next page load. | Accepted. |

The reviewer found no secret in a URL, in storage, in a log or in the page after leaving it. The QR path data cannot inject anything. No session can read or dismiss another session's banner, and nothing gets around the Origin check, the anti-forgery token or the session gate.

## Tests

- Full suite on Windows after the fixes: 1,760 tests, 1,754 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on this machine.
- `e2e/console` after the fixes: 35 tests in each of Chromium, Firefox and WebKit on Windows, and the same tests on Linux WebKit in a container.
- The five required checks on GitHub ran on the final head, which also carries the list pages merged from `main`.

## Limits to know about

1. A recovery-code sign-in through the older path, used only when no owner account row exists, sets no banner.
2. The lock notice and the session-ended cases are tested in the browser with stood-in replies. The server side is covered by the tests of the server actions.
3. No screen-reader pass was done. The WebKit used is Playwright's build, not Safari itself.
4. The new copy on this page is listed in the builder's report. Ivan approved it on 26 Nov 2026.
