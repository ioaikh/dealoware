# A7 Step 14, piece 2 — the sign-in pages (S-A1, S-A2, S-A3): code, security and UI review

| Field | Value |
|-------|-------|
| Story | A7 Step 14, Core admin auth screens. This is the second piece. The first is in [the setup page review](2026-11-26__qa__verification__a7-step-14-setup-page.md). |
| Branch | `claude/t03-admin-auth-screens-2`, on `main` at `144a588` |
| Reference | Draft pull request #38. Its markup and copy are reused. Its scripts are not. |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## How this was reviewed

The same way as the first piece. A reviewer who did not write the code read the whole change against `main`, adversarially. The builder and the reviewer were separate Claude Code agents. A third pass read the one server change and re-ran every test set. No UX team exists, so the UI gate was met by browser tests against the real application in three browser engines, an axe scan of each page and its error state, keyboard-only completion, and screenshots for the owner.

## What the change does

- `GET /admin/sign-in`, `/admin/sign-in/code` and `/admin/sign-in/recovery` return real pages in place of placeholder JSON.
- The browser tests now run in Chromium, Firefox and WebKit, on a runner the setup page's tests share.
- One server change: a `GET` of the code page or the recovery page with no sign-in in progress is redirected to `/admin/sign-in`. Before, it got the session gate's 401 JSON. No exemption is added, no cookie changes, and the `POST`s and every `/admin/api/` path keep their 401.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important | The pages sent their requests with `mode: "same-origin"`. With the pages' no-referrer policy, Firefox and Safari then send `Origin: null`, which the origin gate refuses, so sign-in failed in both. Chromium does not do this, and the tests ran in Chromium only. | Fixed, and confirmed by experiment: before the fix Firefox and WebKit were refused with 403. The tests now run in all three engines. |
| 2 | Important | "Back to sign in" was a plain link, so the sign-in in progress stayed usable and the browser's Back button reopened the code page (TD-ADM-UI-auth-07 and -21). | Fixed. The link signs out first. |
| 3 | Important | A direct visit to the code or recovery page with no sign-in in progress showed a bare 401. | Fixed, by the server change above. |
| 4 | Important | After being sent back to step 1 the page said nothing (TD-ADM-UI-auth-23). | Partly fixed. The generic sentence now shows. The email is not carried over, because that needs web storage or the URL. |
| 5 | Minor | A stale anti-forgery token gave a retry message that could never succeed. | Fixed. The page reloads once. |
| 6 | Minor | If the request helper failed to load, the button stayed disabled. | Fixed. |
| 7 | Minor | A typed password or code could survive in the back/forward cache. | Fixed. It is cleared on leaving the page. |
| 8 | Minor | The test seed script would empty tables in any database file it was given. | Fixed. It refuses any file outside the run's own folder. |

The review found no credential leak, no injection and no open redirect. After a verified code the page follows the server's `next` only when it is a plain path under `/admin/`. Wrong email, wrong password and a throttled address all show the server's one sentence.

## Tests

- Full suite on Windows: 1,415 tests, 1,409 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on that machine.
- Browser tests against the real application: 41 for the sign-in pages and 30 for the setup page, each in Chromium, Firefox and WebKit.
- Two faults showed only outside Chromium, and both were fixed in the page: Firefox laid the links out just under 24 px high, and WebKit restored a typed code on Back.

## Limits to know about

1. **Do not put the admin console in front of the owner yet.** A first sign-in leads to `/admin/setup/authenticator`, which still returns JSON. "Forgot your password?" leads to a placeholder, and `/admin/` has no page. The next pieces build them.
2. Signing out from the code page expires the cookies. The server's record of the sign-in in progress stays until it runs out after five minutes. Without the cookie a browser cannot use it.
3. `returnPath` is not sent at sign-in. Nothing hands the page one, and TD-ADM-177 forbids it in sign-in addresses. The server then uses `/admin/`.
4. The WebKit used here is Playwright's Windows build, not Safari on a Mac.
5. The real Cloudflare widget has still never been loaded, and no screen-reader pass was done.
6. Two sentences were written new: "Turn on JavaScript to sign in." and "That didn't go through. Wait a moment and try again."
