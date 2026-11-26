# A7 Step 14, piece 4 — the password-reset pages (S-A8, S-A9, S-A10, S-A5) and the reset request's security check: review

| Field | Value |
|-------|-------|
| Story | A7 Step 14, Core admin auth screens. Earlier pieces: [setup page](2026-11-26__qa__verification__a7-step-14-setup-page.md), [sign-in pages](2026-11-26__qa__verification__a7-step-14-sign-in-pages.md), [authenticator pages](2026-11-26__qa__verification__a7-step-14-authenticator-pages.md), [browser tests in CI](2026-11-26__qa__verification__a7-step-14-browser-tests-in-ci.md). |
| Branch | `claude/t03-admin-auth-screens-4`, with `main` at `c19a66c` merged in |
| Reference | Draft pull request #38, for headings and copy |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below. Two findings move to the next piece, which completes S-A10. |

## How this was reviewed

A reviewer who did not write the code read the whole change against `main` adversarially. It read the reset request's new security check and its throttle order first. The builder and the reviewer were separate Claude Code agents. A third pass made the small fixes and re-ran the full suite and the reset browser suite.

## What the change does

- **S-A8 `/admin/reset`** is a real page in place of placeholder JSON: the email field, the security check and "Back to sign in". It moves on only for a 200 that carries exactly the server's neutral sentence.
- **S-A9 `/admin/reset/sent`** is a real page: the neutral sentence, the one-hour line and the way back. It is the same page for every visitor.
- **S-A10 `/admin/reset/confirm`** and **S-A5 `/admin/link-expired`** now use the shared page shell, stylesheet and page policy. Their behaviour is unchanged.
- **The server now checks the security token on the reset request.** `POST /admin/reset` and its `/admin/api/auth/reset` twin verified nothing before. They now use the sign-in verifier and give the sign-in route's refusal. A locked address still gets its 429 first. A refused token takes no throttle slot, which TD-ADM-040 requires.
- **The sign-in page can open with "Your session has ended…" (`#ended`) or "Your password has been changed…" (`#changed`).** Nothing sends a browser to `#ended` yet: that redirect belongs to the signed-in console (T04).

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Minor | S-A10 treats any 2xx reply as success, and now lands on "Your password has been changed". | Moved to the next piece, which finishes S-A10. That piece makes it move on only for the server's exact success reply. |
| 2 | Minor | S-A10's own browser suite, `e2e/reset-confirm`, is still outside CI. | Moved to the next piece, which moves its cases onto the shared runner and removes the old suite. |
| 3 | Note | A refused security check costs the sender nothing: no throttle slot, one audit row. | Accepted. TD-ADM-040 requires it, and sign-in and setup behave the same way. Known limit: nothing in the application limits the rate of refused requests. A rate limit at the network edge is to be set up with the deployment (T58/T09). |
| 4 | Note | A missing private verification key makes every check fail with nothing in the log, and the start-up warning named only setup and sign-in. | Fixed. A new start-up warning names the missing variable, never its value. The site-key warning now names the reset pages too. |
| 5 | Note | A hand-made link can show either arrival sentence on the real sign-in page. | Accepted. The sentences are fixed, the link carries no text of its own, and it leads only to the genuine page. |
| 6 | Note | With JavaScript off, S-A8 said "set your password" and had no way back. | Fixed. It now says "Turn on JavaScript to request a reset link." The way back sits outside the hidden panel. A browser test covers both. |
| 6a | Note | A throttled S-A8 shows the neutral sentence in the alert box. | Kept: TD-ADM-UI-auth-11 asks for the server's text unchanged. |
| 7 | Note (already on `main`) | A confirm that times out after the server changed the password ends on "This link no longer works". | Moved to the next piece, which handles an unclear reply on S-A10 as the setup page does. |
| 8 | Note | The absolute-expiry session test measured from the machine clock. | Fixed, the same way as its idle-expiry sibling. |

The reviewer found:
- **No account enumeration:** status, body, audit row and page are the same for the owner's address and any other, throttled or not.
- **No step weaker than on `main`:** the confirm and check steps are unchanged.
- **No injection, no open redirect,** and no token in an address, a Referer or a log.

## Tests

- Full suite on Windows after the fixes: 1,520 tests, 1,514 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on this machine.
- Browser tests: 13 reset tests in each of Chromium, Firefox and WebKit, and 41 sign-in tests in each. The GitHub job runs every suite in all three engines on the pull request.

## Limits to know about

1. S-A10 is finished in the next piece. It needs the confirm field, show/hide, the password rules, the recovery-code switch, an exact success check and an unclear-reply path.
2. Reset now needs the Turnstile site key and the private verification key configured. Start-up warns about each when it is missing.
3. No screen-reader pass. The real Cloudflare widget has not been loaded yet.
4. Sentence written new: "Turn on JavaScript to request a reset link." The rest come from draft #38 or the server.
