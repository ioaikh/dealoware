# Spec note — Core admin S-A12 password change (UXR-A42)

| Field | Value |
|-------|--------|
| Written by | Dealoware Senior Spec |
| Date | 2026-10-05 (~9:36pm ET) |
| Revision | **v1.4** (Chief Spec edit, ~9:44pm ET) — Spec QA cite fixes only: the notice-email line is listed plainly as out of this note (no attribution), and the TOTP same-transaction line cites the Security QA 47491c1a confirm file. Prior tips `6fc85dc6…` (v1.3) and `4817afc2…` (v1.2) VOID. v1.3 log: added the accepted TOTP step recorded in the same transaction as the password update and recovery burn, and the notice email out of this note. v1.2 log: re-cites answers tip `6f4db2d61c7cc6ce133cc3d93d1db74bc3d408a23a6aa7f446b56fb0db7be2f1` (prior `7504914d…` VOID); only step-up failures count toward lockout; recovery burned atomically; adds Chief Spec decision tip `6721779a8bf39b1edee42cde9e6e564fafec4761a2c9448c536f0d0c06eb90b9`. |
| Status | Draft for Spec QA + Security QA (v1.3 `6fc85dc6…`, v1.2 `4817afc2…`, and v1.1 `47491c1a…` replaced). UI/UX QA cross-checks after. Amends the **S-A12 password change** coverage in password-rules v2.3 and the **X01 S-A12** row in ux1-redlines v2. Does **not** edit those passed files. |
| Main Spec (unchanged file) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` — tip sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` (v2.2). Leave that file as-is. |
| Host | `admin.core.dealoware.com` only |
| PoC | $0 |

---

## Sources (cite only)

| Source | Path / role |
|--------|-------------|
| Chief Security answers **item 5** | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` — tip sha256 `6f4db2d61c7cc6ce133cc3d93d1db74bc3d408a23a6aa7f446b56fb0db7be2f1` — **binding** for S-A12 password-change step-up, Security (a)/(b), audit names, replay/burn, normalisation |
| Chief Security answers **item 1** | Same file tip `6f4db2d6…` — password rules (context-word v1.2); Spec password-rules note carries the UI rule-reject messages |
| Password-rules Spec note v2.3 | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` — tip sha256 `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` — **no** confirm-mismatch string (four rule-reject messages only) |
| UX1 redlines Spec note v2 | `specs/2026-10-05__spec__spec__core-admin-ux1-redlines-note.md` — tip sha256 `eb63276fc51217c3c82772c0b8de26ead1337f6e382938a1d736a05d13f6f2d0` — A05 step-up shape |
| Sign-in steps Spec note v1 | `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` — tip sha256 `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` — A16 condition 2 |
| Lockout Spec note v5.5 | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` — tip sha256 `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` |
| UX1-A03 lockout message r2 | `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md` — tip sha256 `0817b7676a0103d606197c07fb6ac48782c12fbd363304637e5340b86af6d3f0` — generic lockout copy |
| UX1-A16 decision | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` — tip sha256 `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34` — **condition 2** |
| UX addendum UXR-A42 / UXR-A22 / UXR-A04 / UXR-A23 | `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` — mismatch copy **UXR-A23** |
| Chief Spec decision (points 1–4) | `specs/2026-10-05__spec__spec__core-admin-sa12-chief-spec-decision.md` — tip sha256 `6721779a8bf39b1edee42cde9e6e564fafec4761a2c9448c536f0d0c06eb90b9` — **binding** for Chief Spec decision points 1–4 |
| Chief UI/UX additions (a)–(c) | Chief Spec relay / Chief UI/UX, **2026-10-05 ~9:27–9:30pm ET** — **binding** for lock UX, after-failure form/focus/a11y, show/hide + confirm checks |
| Main Spec | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` |

---

## Purpose

Lock S-A12 **Change password** so it **MUST** require the **current password** plus a current authenticator or recovery code (step-up on submit), per Chief Security answers **item 5** and UXR-A42. Carry password-rules v2.3 for the new password. **Never write a password or secret value.** Build stays held. PoC $0.

---

## Change password form (S-A12)

On `/settings/security`, the **Password** section hosts **Change password**.

- **Heading / labels:** UXR-A42 — screen heading "Security settings"; section "Password"; control "Change password"
- **Field order:** (1) Current password (`autocomplete="current-password"`); (2) New password (`autocomplete="new-password"`); (3) Confirm new password (`autocomplete="new-password"`); (4) Authenticator code (`autocomplete="one-time-code"` `inputmode="numeric"`) with switch "Use a recovery code instead" (recovery: `autocomplete="one-time-code"`, no `inputmode` restriction), per UXR-A04
- **Step-up on submit:** The server **MUST** check the current password plus a current TOTP or recovery code **together on submit**, the same step-up as re-enrol and regenerate in ux1-redlines v2 **A05** / answers item 5. A live session alone is **never** enough
- **Transport and cache:** The form is **POST only** and **MUST** carry an **anti-forgery** token. The page **MUST** be served with `Cache-Control: no-store`
- **No Turnstile** on S-A12 (including this form)
- **Paste:** Never blocked (UXR-A04 / password-rules v2.3)
- **Success copy:** On success, land on **S-A1** with: `Your password has been changed. Sign in with your new password and authenticator code.`
- **Secrets:** **MUST NOT** put passwords or codes in URLs, audit bodies, logs, or error text

---

## Wrong current password or code

If the current password is wrong, or the authenticator / recovery code is wrong or unusable, the form **MUST** show **one** generic inline message and **MUST NOT** name which factor failed:

> We couldn't confirm it's you. Check your current password and code.

### Security confirm (a) — failure counting and audit (answers item 5)

Each failed **step-up** submit (wrong current password or code) **MUST** count **once** toward the **one shared account failure counter** in UX1-A16 decision **condition 2** (and the lockout note), and **MUST** write audit event **`account.password_change_failed`**. That event **MUST NEVER** contain the password or the code. **Only** those step-up failures count toward the lockout limit. New-password **rule rejects** and confirm **mismatches** **MUST NOT** count toward the failure counter or lockout.

### Security confirm (b) — lock on fail (answers item 5)

If that failure causes the account to **lock**, the server **MUST** end **every** session for that account at once, **including the current one**, and land the user on **S-A1**. Lockout v5.5 rules apply. The client shows the **generic UX1-A03** message (no duration, no word "locked"):

> We couldn't sign you in. Check your details and try again later. You can also reset your password.

---

## Chief UI/UX additions (a)–(c)

Cite as **Chief UI/UX additions (a)–(c)** (Chief Spec / Chief UI/UX, 2026-10-05 ~9:27–9:30pm ET). They sit on top of Chief Spec’s decision points **1–4** in `specs/2026-10-05__spec__spec__core-admin-sa12-chief-spec-decision.md` tip sha256 `6721779a8bf39b1edee42cde9e6e564fafec4761a2c9448c536f0d0c06eb90b9`.

**(a) Lock while signed in.** The current session ends and the user lands on **S-A1** with the generic UX1-A03 text, with **no** duration and **no** "locked" wording. (Same landing already locked under Security confirm (b) above; this labels the UI/UX requirement.)

**(b) After a step-up failure.** Clear **Current password** and **Authenticator code** (or the recovery code), keep **New password** and **Confirm new password**, and move focus to **Current password**. Announce the generic message in a polite live region tied to the form with `aria-describedby` (WCAG 2.2 SC 3.3.1 and 4.1.3). Submit allows only one request in flight at a time, so it is disabled until the response comes back.

**(c) Password fields.** Every password field (Current, New, Confirm) gets a show/hide toggle (UXR-A22 / UXR-A04). Confirm-new uses the **UXR-A23** mismatch message `These passwords don't match.`, checked on blur and on submit. (**Note:** password-rules v2.3 has **no** confirm-mismatch string — only the four new-password rule-reject messages — so this note keeps UXR-A23.)

---

## TOTP replay and recovery burn (answers item 5)

- The **TOTP last-accepted-step replay guard** from ux1-redlines v2 **A05** **MUST** apply on this submit
- A **recovery code** is burned **atomically in the same transaction as the password update**, and **only** when the whole submit succeeds (not on a failed attempt)
- The **accepted TOTP step** (last-accepted-step for the replay guard) **MUST** be recorded in that **same transaction** as the password update and any recovery-code burn (Security QA confirm `verification/2026-10-05__security__verification__core-admin-sa12-password-change-note-47491c1a-confirm.md`, OBS item 1)

---

## New password (password-rules v2.3; UXR-A22 / UXR-A42; answers item 5)

New password and confirm **MUST** follow password-rules tip `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` and answers **item 1**:

- Length, NFKC, blocklist / context-word v1.2, hashing floor, helper text, client length-only checks, and the four named inline rule-reject messages
- After the **same normalisation** as password-rules, the new password **MUST NOT** equal the **current** password
- A **rule reject** on the new password, or a confirm **mismatch**, does **not** count toward the shared account failure counter, account lockout, or IP throttle (password-rules rule 9 / answers item 5) and does **not** write `account.password_change_failed`
- On **success**, the server **MUST** write audit event **`account.password_changed`** (never containing the password or code), **MUST** end **every** session **including this one**, and show **S-A1** with the success copy in the form section
- Authenticator enrolment is **unchanged** (TOTP stays enrolled)

---

## What this note amends (cite only; files unchanged)

- Password-rules v2.3 **S-A12 password change** — this note supplies current-password + code step-up, POST/anti-forgery/no-store, generic step-up failure copy, Security (a)/(b), audit event names, replay/burn, and success including this session
- UX1 redlines v2 **X01 S-A12** — password change on that screen uses this note; re-enrol / regenerate step-up in A05 stays as written

Do **not** edit password-rules tip `1a7b342c…`, ux1-redlines tip `eb63276f…`, lockout tip `8a194eb9…`, or sign-in steps tip `0a3db5f4…` in this write.

---

## Out of this note

- Editing the passed password-rules, ux1-redlines, lockout, or sign-in tips
- Re-enrol / regenerate flows already locked in ux1-redlines A05 (except sharing the same step-up check on this submit)
- Inventing password values, secrets, Turnstile keys, AWS account IDs, Stories, code, CDK, spend, or deploy unlock
- A password-change notice email (not part of v1)

---

## Active holds

- Build and deploy stay held; this note is not a build unlock
- No admin.core deploy or attach until A11, the PR #11 conditions, and Ivan's OK via Bot Manager
- Do not invent password values, secrets, Turnstile keys, HMAC keys, or AWS account IDs
- Spec QA and Security QA review (v1.3 `6fc85dc6…` replaced by this v1.4); UI/UX QA cross-checks after Spec QA
- Passed sibling tips stay untouched; queued nits wait for a later batch

PoC **$0**.

---

## Handshake

1. Spec QA verifies vs Chief Spec decision tip `6721779a…` points 1–4 + answers tip `6f4db2d6…` item 5 + Chief UI/UX (a)–(c).
2. Security QA confirms (v1.1 tip `47491c1a…` VOID).
3. UI/UX QA cross-checks after Spec QA.

---

## Done-list

- [x] Form bullets: labels, field order, step-up, POST/anti-forgery/no-store, no Turnstile, paste, success copy
- [x] Security (a)/(b): only step-up failures → shared counter + `account.password_change_failed`; rule rejects/mismatches do not; lock ends every session → S-A1 + UX1-A03
- [x] Chief UI/UX (a)–(c): lock UX label; after-failure clear/focus/live-region/single-flight; show/hide + UXR-A23 mismatch (v2.3 has none)
- [x] TOTP replay; recovery burn atomic with password update on whole-submit success; new ≠ current after normalisation; success ends every session including this one; `account.password_changed`
- [x] Cites answers tip `6f4db2d6…` + Chief Spec decision tip `6721779a…`; prior Spec tip `47491c1a…` VOID; passed files not edited
- [x] One Active holds list; no password values; PoC $0
