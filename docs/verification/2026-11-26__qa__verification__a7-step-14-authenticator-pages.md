# A7 Step 14, piece 3 — authenticator set-up and recovery codes (S-A6, S-A7): code, security and UI review

| Field | Value |
|-------|-------|
| Story | A7 Step 14, Core admin auth screens. This is the third piece, after [the setup page](2026-11-26__qa__verification__a7-step-14-setup-page.md) and [the sign-in pages](2026-11-26__qa__verification__a7-step-14-sign-in-pages.md). |
| Branch | `claude/t03-admin-auth-screens-3`, on `main` at `a68e3a5` |
| Reference | Draft pull request #38, for headings and copy |
| Date | 2026-11-26 |
| Result | **PASS**, after the fixes listed below |

## How this was reviewed

Pieces 1 and 2 were reviewed the same way. A reviewer who did not write the code read the whole change against `main` adversarially, with the two secrets first: the authenticator key and the recovery codes. The builder and the reviewer were separate Claude Code agents. A third pass read the server changes and the fixes, and ran the full test suite again. No UX team exists, so browser tests against the real application stood in for the UI gate. They ran in three browser engines, with an axe scan of each page, keyboard-only completion and screenshots.

## What the change does

- `GET /admin/setup/authenticator` (S-A6) returns a page, not JSON. The page shows the QR code, the key in groups of four with a copy button, and the code field.
- `GET /admin/setup/recovery-codes` (S-A7) returns a page with the ten codes, a copy button, an "I have saved these codes" box, then Continue.
- The server writes the key, the QR code and the codes into a no-store page. No script asks for them, and no address hands them out as data.
- The QR code is made on the server by an encoder in this repository, `AdminQrCode`, with no third-party library. It supports byte mode, level M, versions 1 to 40, and all eight masks. It is drawn as inline SVG with its quiet zone.
- A browser `GET` of either page with no sign-in in progress is redirected to `/admin/sign-in#again`, as the code and recovery pages already are. No exemption is added.

## Findings and what was done

| # | Severity | Finding | Outcome |
|---|----------|---------|---------|
| 1 | Important (already on `main`) | The authenticator key waiting to be confirmed was made at the first password step, then kept across every later sign-in and across a password reset. Anyone who saw the set-up page once, for example on a shared screen or with an old password, held the key the owner confirmed later. | Fixed. Every password step now issues a new key while the owner is not set up. Within one sign-in in progress, a reload shows the same key. Setting a password through first-time setup or reset drops a waiting key. |
| 2 | Minor | `POST /admin/setup/authenticator` with no code still answered with the key and its otpauth address, while the comments said it did not. | Fixed. A missing or blank code is treated as a wrong code: the same 401 and sentence, counted and audited as a failed attempt. The JSON path is deleted. |
| 3 | Minor | The page moved on for `{"enrolled":true}` alone, not for the server's whole reply. | Fixed. It moves on only for `{"enrolled":true,"next":"recovery-codes"}`. |
| 4 | Note (already on `main`) | Step 1 cleared the recovery-codes reveal on an entity it had read earlier. A set-up that finished in between could leave the codes showable to the new sign-in. | Fixed. The reveal is now cleared on the database row. |
| 5 | Note | The copy helper could reject, although its comment said it never does. | Fixed. |
| 6 | Note | A comment overstated when the codes stop being shown. | Corrected. |
| 7 | Note | Recovery codes are never seen if the five minutes of the sign-in in progress run out around the confirming step. The same happens if the tab is restored after that time. | Accepted as a known limit, below. |
| 8 | Note | Continue expires the browser's cookie. The server's record of the sign-in in progress stays until it runs out. | Accepted. The stored codes are hashed. The copy kept for showing them is encrypted with the same key as the authenticator secret, and the next sign-in or second step clears it. |

The review found no leak of either secret: they were not in headers, URLs, attributes, logs, audit rows, caches or files in the repository. It also found no injection and no way past the session gate.

## The QR encoder

The reviewer wrote an encoder of their own and compared 163 inputs: real otpauth addresses, plus both boundary lengths of every version, each with all eight masks. Every symbol was identical. The builder's tests check the encoder against:

- the published codewords of ISO/IEC 18004 Annex I;
- two worked examples;
- the standard's format, version and capacity tables;
- pinned symbols for versions 1, 5, 7 and 10;
- an independently written reader in the test project;
- in each browser, a reader of the grid the browser drew, compared with the otpauth address.

No phone has scanned it yet. That must be checked once before the console is used for real.

## Tests

- Full suite on Windows, after the fixes: 1,483 tests, 1,477 passed, 1 skipped, 5 failed. The same five fail on untouched `main` on that machine.
- Browser tests against the real application, each in Chromium, Firefox and WebKit: 25 for these pages, 41 for the sign-in pages, 30 for the setup page.

## Limits to know about

1. Recovery codes that are not seen within the five minutes of the sign-in in progress cannot be shown again. The security settings page (S-A12, now part of T04) is where new ones will be made.
2. No Download button for the codes, though draft #38 and TD-ADM-UI-auth-08 have one.
3. No phone has scanned the QR code, and no screen-reader pass was done.
4. The browser tests do not run in CI yet. The last piece of this Story adds them.
5. The WebKit used is Playwright's Windows build, not Safari on a Mac.
6. Sentences written new for these pages:
   - "If you can't scan the code, enter this key in your app instead."
   - "Account name"
   - "Turn on JavaScript to set up your authenticator."
   - "Each code signs you in once if you can't use your authenticator app."
   - "Turn on JavaScript to see your recovery codes."
   - "Your recovery codes have already been shown."
   - "Couldn't copy. The text is selected so you can copy it yourself."
