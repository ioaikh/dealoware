# A7 Step 14, piece 6 — the set-new-password page (S-A10) finished, and its browser tests in CI: review

| Field | Value |
|-------|-------|
| Story | A7 Step 14, Core admin auth screens. This is the last piece. Earlier: [setup page](2026-11-26__qa__verification__a7-step-14-setup-page.md), [sign-in pages](2026-11-26__qa__verification__a7-step-14-sign-in-pages.md), [authenticator pages](2026-11-26__qa__verification__a7-step-14-authenticator-pages.md), [browser tests in CI](2026-11-26__qa__verification__a7-step-14-browser-tests-in-ci.md), [reset pages](2026-11-26__qa__verification__a7-step-14-reset-pages.md). |
| Branch | `claude/t03-reset-confirm-page`, with `main` at `558a32c` merged in |
| Date | 2026-11-26 |
| Result | **PASS**, after the fix listed below |

## How this was reviewed

A reviewer who did not write the code read the change against `main` adversarially. It ran the page script's harness with five cases of its own and read the CI run. The builder and the reviewer were separate Claude Code agents. A third pass made the fix, added a test for it, and ran the full suite and the reset tests again.

## What the change does

- **Exact success reply.** S-A10 says "password changed" only for the server's exact success reply. After any unclear reply it checks the link again. If the link is spent, it goes to plain sign-in. Otherwise it keeps the form and what was typed.
- **The first-setup page's password fields.** S-A10 uses them byte for byte: confirm field, show/hide, helper text, the 15-to-128 code-point length check after NFKC, mismatch text, and the server's named reason shown inline (TD-ADM-UI-auth-10).
- **A switch between authenticator code and recovery code.** Only one field is on screen, and only that field is sent (TD-ADM-UI-auth-12). The server already makes a recovery code single-use.
- **The October suite `e2e/reset-confirm/` is folded into `e2e/reset/`.** CI now runs it in Chromium, Firefox and WebKit. Every old case maps to a new one; the old "resubmit on time-out without a check" case was changed on purpose, because it was the fault the earlier review found. The loopback-proxy guard moves to `e2e/signed-out/`.
- **Closed here:** finding 4 of the CI review and findings 1, 2 and 7 of the reset-pages review.
- No route or server contract changes.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Minor | A submit timed out. The re-check found the link still usable. The server then committed. The next press got "this link no longer works", although the typed password had been set. | Fixed. After any unclear reply, a later "link no longer works" leads to sign-in, which tells the owner. Only a 400 or 401 settles it again. A harness case covers it. |
| 2 | Minor | The retry sentence, "We couldn't set your password…", is shown even when the change may have gone through. | Accepted, and the code comment is corrected. The next press leads to sign-in, which tells the owner. A neutral sentence would be new copy. |
| 3 | Note | If a helper script fails to load, the page says to try again, but a reload cannot work, because the token has already left the address bar. | Accepted. Re-opening the emailed link works. |
| 4 | Note | The re-check takes a slot of the reset throttle for each address. | Accepted. It touches neither the account lock nor the failure count. |
| 5 | Note | The submission follows redirects, to see the 303 to the link-expired page. | Accepted. It moves on only to that exact same-origin path. A redirect anywhere else triggers the re-check. |
| 6 | Note | The token stays in the script's memory after the page is left. | Accepted. Every way out replaces the page, after the token is spent. |
| 7 | Note | Field errors may be announced twice. | Accepted. This is the same pattern as the other signed-out pages. |

The reviewer found:
- **The token** never reaches the page markup, an address the page builds, storage, a Referer or error text.
- **Only the chosen second factor is sent.** A recovery code is cleared on leaving the page, on a 401 and on a switch.
- **No branch can claim "password changed" without the server's exact reply.**
- **The moved guards are logic-identical** to the old ones.

## Tests

- Full suite on Windows before the fix: 1,522 tests, 1,516 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on this machine.
- After the fix, the reset test classes: 118 of 118 passed, including the new harness case.
- CI before the fix: every job passed. The browser tests ran 28 reset, 30 setup, 25 enrolment and 41 sign-in tests in each engine, with none failed, flaky or skipped.

## Limits to know about

1. A stale anti-forgery token on submit shows the retry sentence. Re-opening the emailed link is the only recovery.
2. The switch is a secondary button under the submit button. Draft #38 used a plain link.
3. No screen-reader pass. No real back/forward-cache test.
4. No new sentence was written. All copy is reused from the setup page, the sign-in pages or the server.
