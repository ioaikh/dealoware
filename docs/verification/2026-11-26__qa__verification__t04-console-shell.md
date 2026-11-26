# T04 piece 1 — signed-in console shell, Stats, sign-out and session-ended handling: review

| Field | Value |
|-------|-------|
| Scope | The first signed-in pages of the admin console, the session gate's answer to a page asked for without a session, the way back after sign-in, and sign-out |
| Branch | `claude/t04-console-shell` |
| Rules | Story A7 Step 7; Spec §6 and §8; test design TD-ADM-131, 173, 175, 177 and 179, UI-auth-17, UI-na-X01, X03 and B13 |
| Reference | Draft pull request #39. Its shell, routes and test harness are not reused. |
| Date | 2026-11-26 |
| Result | **PASS**, after two rounds of fixes |

## How this was reviewed

A reviewer who did not write the code read the change adversarially. It covered the return path (open redirect), the gate change, static-file gating, anti-forgery reuse, the pages, sign-out and regressions. It then re-reviewed each fix commit. The builder and the reviewer were separate Claude Code agents. A third pass read the gate changes and ran the full suite after each round. The required checks on GitHub ran on every head and caught one regression, listed below as finding 2a.

## What the change does

- **A signed-in page shell.** It has a skip link, the menu, one live region, a per-page nonce CSP, no-store, the anti-forgery meta and a sign-out form.
- **Stats at `/admin/`.** The five totals, "As of … ET". `/admin` redirects to `/admin/`.
- **Placeholders and S-D3.** The four list pages and Security settings are placeholders for the next pieces. S-D3 shows "We can't find that page."
- **Signed-in scripts and styles sit behind the session gate.** They are served from `/admin/assets/`. Before the gate, only `/admin/auth/**` is public under `/admin`.
- **A page GET without a valid session gets a 302 to `/admin/sign-in#ended`.** Every `/admin/api/**` path, every asset and every other method keeps the 401.
- **The page asked for is carried across sign-in.** It travels in `dw_admin_return`, a short-lived HttpOnly, Secure, SameSite=Strict cookie scoped to the sign-in path. It is validated again at step 1 and never placed in a URL (TD-ADM-177). Only allow-listed paths and list query keys are kept.
- **Sign-out is audited (`sign_out`) and lands on "You've been signed out."** A GET never signs out.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important | Two console tests failed on Linux WebKit. axe ran before the stylesheet applied. In WebKit, Tab does not stop on links. | Fixed. The tests wait until the stylesheet has applied before scanning. On WebKit each control is focused directly and its focus ring checked. Chromium and Firefox keep the full Tab-order check. |
| 2 | Minor | With JavaScript off, sign-out gave a bare 403 and the owner stayed signed in. | Fixed. The sign-out form carries a hidden token, and only `POST /admin/sign-out` accepts it. The form post is answered with a 303 to sign-in. |
| 2a | Important (found by CI) | The first fix for 2 set `Referrer-Policy: same-origin` on signed-in pages. That broke the project's rule that no page says where it came from: two existing tests failed. It would also have let the sign-in page's third-party security-check script read the address of the last console page. | Replaced. Signed-in pages stay under `no-referrer`. Under that policy browsers send `Origin: null` with a form POST. The origin gate accepts that one case only on `POST /admin/sign-out`, only with `Sec-Fetch-Site: same-origin` and `Sec-Fetch-Mode: navigate`, which a page cannot set, and only with the hidden form token. A header token does not pass on that branch. |
| 3 | Minor | After a new sign-in in another tab, "Sign out" in an already open console tab failed with 403 on every press. | Fixed. On a 403 while the session is still valid, the page reloads once to pick up the current token. |
| 4 | Note | A `sign_out` audit row was written for an already expired session. | Fixed. The audit is written only for a session the gate would still accept. |
| 5 | Note | An encoded `?` or `#` in a page path could become a query on return. | Fixed. Such a path is not kept. |
| 6 | Note | The anti-forgery token is not bound to the session, nor rotated when sign-in completes. A planted cookie is now reused rather than replaced. | Accepted for now. The Origin check and the header-only token already stop forgery. Recorded as hardening. |
| 7 | Minor | A link from another site arrives without the SameSite=Strict session cookie and lands on "Your session has ended", though the session is still valid. | Accepted. It comes from SameSite=Strict. The page asked for is kept, so signing in returns there. |

The reviewer found:
- **No open redirect.** Only allow-listed `/admin/` pages, with re-encoded list values, can be returned to.
- **No path that gets further without a session than on `main`.**
- **No signed-in file reachable without a session.**
- **No entity data in a page before the session check.**
- **No forced sign-out or login forgery from another site.**

## Tests

- Full suite on Windows, last commit: 1,662 tests, 1,656 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on this machine.
- New `e2e/console` suite: 15 tests per engine. On GitHub the last head passed in Chromium, Firefox and WebKit, and so did the bootstrap, enrolment, reset and sign-in suites.
- New sentences: "This page is not built yet.", "Go to Stats", and the title "Page not found · Dealoware admin". The rest comes from the test design, Spec §6 or draft #39.

## Limits to know about

1. Browsers too old to send `Sec-Fetch-*` cannot sign out with JavaScript off. The request is refused, which fails closed.
2. "Audit log" is not in the menu until T07. S-D4 "You can't do that." is not built, because neither the Spec nor the Story says when it shows. `Clear-Site-Data` (a SHOULD in TD-ADM-173) is not sent.
3. No screen-reader pass.
