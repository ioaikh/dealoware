# A7 Step 14, piece 1 — the first-time setup page (S-A4): code, security and UI review

| Field | Value |
|-------|-------|
| Story | A7 Step 14, Core admin auth screens. This is the first of several pieces. |
| Branch | `claude/t03-admin-auth-screens-1`, on `main` at `1c066b3` |
| Reference | Draft pull request #38, which is partly superseded. Its markup and stylesheet are reused. Its scripts and tests were written against a server contract that never landed, and are rewritten. |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## How this was reviewed

The agent teams named in the Story's gates are not running. The gates were met as follows.

| Gate in the Story | How it was met |
|-------------------|----------------|
| Dev Code QA | A reviewer who did not write the code read the whole change against `main`. The builder and the reviewer were separate Claude Code agents. A third pass re-read the new server branch and re-ran both test sets. |
| Security | The same independent review was adversarial: the link token in the browser, injection, the content security policy, the start-up guard, fail-closed behaviour, static assets, test-only code. |
| UI/UX | No UX team exists to approve it. In its place: 30 browser tests against the real application, an axe scan with no finding of any impact in four states, keyboard-only completion, 320 px width and 200% zoom, contrast computed from the stylesheet, and screenshots for the owner, who is the only user of this page. No screen-reader pass was done. |
| CI | The pull request's own run. The browser tests are not in CI yet. That is a later piece. |

## What the change does

- `GET /admin/bootstrap` returns the real page in place of placeholder JSON.
- Adds the shared shell for signed-out admin pages, the stylesheet, and the security-check script that later screens reuse.
- Adds a public Turnstile site key setting. With no site key the page cannot be submitted, and says so.
- Outside Development the application refuses to start unless Turnstile verification points at Cloudflare.
- Adds a link pre-check, so a dead link never shows the password form.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important | The widget's wrapper had the ID `turnstile`, which makes `window.turnstile` an element before Cloudflare's script runs. The real widget might then never install, and the form could never be submitted. | Fixed. The IDs are renamed, and a test fails if any element shadows that global. |
| 2 | Important | A dead link still showed the form and was rejected only after typing. Test design TD-ADM-UI-auth-09 forbids that. | Fixed. The page asks the server first. The check reuses the existing route with a flag, needs no new exemption, consumes nothing, takes a slot of the shared throttle budget, and gives one answer for every kind of dead link. |
| 3 | Minor | Any 2xx reply was treated as success. | Fixed. Only `passwordSet: true` counts. |
| 4 | Minor | The token was removed from the address bar by the fourth script. | Fixed. A new first script does it. |
| 5 | Minor | After a timeout the page said the password could not be set, when it might have been. | Fixed. The page checks the link again and goes to sign-in if it is spent. |
| 6 | Minor | The show button changed both its label and its pressed state. | Fixed. The label stays, and the pressed state changes. |
| 7 | Minor | An empty site key outside Development gave no hint to the operator. | Fixed. One warning at start-up. |

No token leak, injection or way to submit without the security check was found. The token is in the link's fragment, is never sent in the page request, and is not stored anywhere.

## Tests

- Full suite on Windows: 1,355 tests, 1,349 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on that machine.
- Browser tests: 30 of 30, run with `bash e2e/bootstrap/run.sh` against the real application.

## Limits to know about

1. **The real Cloudflare widget has never been loaded.** Every test uses a stand-in. Before the page is used in production, load it once with the real site key and confirm that the widget appears and that the content security policy does not block it.
2. After setting the password the owner is sent to `/admin/sign-in`, which is still placeholder JSON until the next piece.
3. Test design TD-ADM-UI-auth-08 expects setup to lead straight to authenticator enrolment. The server sends the owner to sign-in first, and that is kept.
4. The binding UX requirements (UXR-A01 to A45) are not in the repository. The page's wording comes from the server's own text, draft #38 and the test design. Two sentences were written new: "We couldn't set your password. Wait a moment and try again." and "The security check is unavailable."
5. Every link check takes a slot of the throttle budget that reset and setup share.
