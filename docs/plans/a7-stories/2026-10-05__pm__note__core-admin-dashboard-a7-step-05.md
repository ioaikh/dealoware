# A7 Step 5 — Turnstile only, lockout and rate limits, session rules (core admin dashboard)

**Track:** A7 Core admin dashboard build, Core admin at `admin.core.dealoware.com`. Dev Plan Step 5.
**Tracker:** this file is the A7 Story of record until GitHub Issues are re-enabled on the repo.
**Type:** UI. Turnstile on the login, reset and bootstrap pages; lockout and session behavior on login. Auth screen UI that this Story's API serves is owned by Step 14.

## Owner
- **Chief Developer**
- Source: PM admin dashboard checklist A7 lanes (backend and auth Steps 1–6 and 8 with the Chief Developer).

## Acceptance criteria (traced from Spec v2.2 through Dev Plan §4 Step 5 and §5)
- AC7 / Spec §8.5 — Cloudflare Turnstile only on login, password reset, and bootstrap password-set.
- AC7 / Spec §8.6 — per account: 5 failed password or 2FA attempts in 15 min lock the account for 30 min (or until a completed reset).
- AC7 / Spec §8.6 — per IP: 20 failed logins in 15 min throttle for 30 min with HTTP 429; same per-IP window on reset requests.
- AC7 / Spec §8.7 — server-side session row in Core Postgres; HttpOnly, Secure, SameSite=Strict cookie on `admin.core.dealoware.com`; idle 30 min sliding, absolute 8 h, then full re-auth.
- Raw client IP lives only in short-lived rate-limit counters, never in audit, other logs, or metrics.
- UI deliverables (UX1-A11): this step's screens are S-A1 (sign-in step 1) and S-A11 (session ended), built in Step 14 against UX1 addendum UXR-A01–UXR-A03, UXR-A13–UXR-A15, UXR-A38, UXR-A39, UXR-A41. This Story's API supports those screens. The addendum is a draft, not approved; its routes and copy wait for Spec to adopt them (see Active holds). Definition of done adds the TD-ADM UI cases QA writes for these UXR rows (addendum A.5) once they exist.

## Test-design cases this Story must satisfy (test design §3.5, Step 5)
- `TD-ADM-040`, `TD-ADM-041`, `TD-ADM-050`, `TD-ADM-051`, `TD-ADM-052`, `TD-ADM-053`, `TD-ADM-054`

## Gates (in order)
**Before code starts (UI/UX first launch rule):** UX1 approval of this Story's part: Chief UI/UX approval plus a UI/UX QA PASS file under `docs/verification/` (Chief UI/UX names it).

**At PR review:**
1. **Dev Code QA PASS file** under `docs/verification/` (Code QA names it; prior Story pattern `YYYY-MM-DD__sd__verification__<story>.md`). Test design §5: P0 API/integration cases for the mapped TD-ADM IDs are green.
2. **Security PASS file** under `docs/verification/` (prior SD-step pattern `YYYY-MM-DD__security__verification__<story>-sd-qa-confirm.md`).
3. **Chief UI/UX approval plus a UI/UX QA PASS file** on the built UI, under `docs/verification/`.
4. **Bot Manager merges.**

Deploy waits for Ivan's OK via Bot Manager.

## Active holds
- Merge waits for the Dev Code QA PASS file.
- Merge waits for the Security PASS file.
- Bot Manager does the merge, and only after the PASS files above exist.
- Deploy waits for Ivan's OK via Bot Manager.
- UI build waits for UX1 approval of these screens.
- Whether UI test cases run headless in CI on every PR that touches admin UI files (UX1-X08, UXR-D15) is a decision for Chief QA and Chief DevOps. Addendum D.7 proposes that until then UI cases run locally and block merge by review.
- The UI deliverables for this step's screens (routes and copy in the UX1 addendum) wait for Chief Spec to adopt them.
- A process-global auth flood limiter waits for explicit Chief Security approval. This Story builds per-IP and per-account limits only.
- Whether CAPTCHA failures count toward the lockout budgets waits for SA (test design OQ1). Resolved on the SA side: Turnstile failures do not count (UX1-A13 Arch QA PASS); Spec copy still waits.
- Sliding vs fixed lockout windows wait for SA (test design OQ3).
- Retry-After on the 429 response is asserted only if Spec or SA later locks it (test design OQ5).
- Lockout copy stays generic and never shows the lock length (Chief Security UX1-A03 decision, `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md`); Spec copy still has to adopt it.

## Out of scope (Dev Plan Step 12 and §7)
- AWS WAF CAPTCHA, reCAPTCHA, and other CAPTCHA providers.

## Rules
- Core admin host only: `admin.core.dealoware.com`. Platform hosts are not Core.
- No password values anywhere: code, fixtures, README, chat, or commit history. Use env placeholders only.
- No AWS account details, account IDs, ARNs, region, SES identity, Turnstile keys, or HMAC key values in code, docs, or PRs.

## Sources
- Dev Plan (closed): [`docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md), sha256 `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf`; §4 Step 5, §5, §6, §7, §10.
- Spec v2.2: [`docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md), sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`.
- Test design (A6 PASS): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md), sha256 `870f348c61e2b3d88c2cd6a99e6ae4398186e96c1a7169a16c0b52e2bdd7bca7`.
- Test design QAQA PASS: [`docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md) (docs PR 19, merge `3803d43`), sha256 `1c95803d85264e6214de9f36becc348a9021924e6bfd19b2047cce1995230010`.
- Security Dev Plan-step checklist pts 1–14: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md).
- UX1 findings: `dealoware-kb/ux/2026-10-05__ux__findings__ux1-admin-requirements.md` (Redlines; not approval).
- UX1 UI requirements addendum (Parts A–D, draft): `dealoware-kb/ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md`, sha256 `dd847e702ce628838c22a78dd4491b4047e52aaabfd9b526cfb73509bf2937a7`. Routing: `dealoware-kb/ux/2026-10-05__ux__redlines__ux1-routing.md`, sha256 `fde55fbae19ee3a0f97d27734f902ba3694cb2d6e4f7cd42a2742e0ec0854b4d`.
