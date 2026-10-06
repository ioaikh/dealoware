# A7 Step 14 — Auth screens UI: sign-in, 2FA enrol and verify, recovery codes, Turnstile states, email-link reset, lockout message (core admin dashboard)

**Track:** A7 Core admin dashboard build, Core admin at `admin.core.dealoware.com`. Dev Plan Steps 2–5 (auth UI twin).
**Tracker:** this file is the A7 Story of record until GitHub Issues are re-enabled on the repo.
**Type:** UI. Auth screens: sign-in, 2FA enrol and verify, recovery codes, Turnstile states, email-link reset, lockout message.

## Owner
- **Accountable:** Chief Developer
- **Implementer:** Senior Developer
- CPM assigned this split for Steps 14 and 15 on 2026-10-05.

## Acceptance criteria (traced from Spec v2.2 through Dev Plan §4 Steps 2–5 and §5)
- AC7 / Spec §8.1–§8.7 — auth screens for CoreOwner sign-in (email + password + TOTP), bootstrap password-set, TOTP enrol and verify, recovery-code reveal and use, password reset (email link + 2FA), Turnstile on login / reset / bootstrap, and lockout or rate-limit messaging. Routes and screen inventory come from Spec §8 once UX1-A01 lands; this Story builds the screens Spec and UX1 name.
- AC7 / Spec §8.5 — Cloudflare Turnstile only on login, reset, and bootstrap password-set; UI states for loading, ready, failed, expired, and unavailable (UX1-A04).
- AC7 / Spec §8.6 — lockout and rate-limit messaging for the Spec numbers (5/15→30 account; 20/15→30 IP → HTTP 429). Copy shows only what the server returns; no lock length or countdown is shown (Chief Security UX1-A03 decision; UXR-A01).
- AC6 / Spec §8.1 fail-closed — unauthenticated or missing-TOTP navigation of these screens lands on a safe deny or the next required auth step; no entity data in the HTML.
- Relates to Dev Plan Steps 2–5 (auth backend) as their UI twin. Backend Stories stay separate.
- Screen inventory (UX1-A14, UXR-A12): S-A1 sign-in step 1, S-A2 sign-in code, S-A3 recovery code, S-A4 bootstrap set password, S-A5 link no longer works, S-A6 authenticator setup, S-A7 recovery codes, S-A8 reset request, S-A9 check your email, S-A10 set a new password, S-A11 session ended, S-A12 security settings. Every built auth route maps to one screen. Route names are UI/UX proposals waiting for Spec to adopt or rename them and SA to confirm paths.
- UI requirements: UXR-A01–UXR-A44 in the UX1 addendum, per screen as listed there. Wording, password rules, step shape, idle warning, and the S-A12 panel are decided or proposed but not yet locked in Spec; build them only as Spec adopts them.

## UX1 findings this Story must satisfy (cite only; do not invent copy)
- Source: `dealoware-kb/ux/2026-10-05__ux__findings__ux1-admin-requirements.md` (verdict: Redlines — not approval).
- Relevant findings: UX1-A01 (screen inventory), UX1-A02 (non-enumerating error copy), UX1-A03 (lockout messaging), UX1-A04 (Turnstile UI states), UX1-A05 (TOTP enrol QR + confirm), UX1-A06 (recovery-codes UX), UX1-A07 (reset expiry and resend), UX1-A08 / A20 (session timeout UX), UX1-A09 (auth control states), UX1-A10 (WCAG 2.2 AA on auth), UX1-A14 (auth screens previously unowned — this Story closes that gap), UX1-A15 (sign-out), UX1-A16 (sign-in step shape), UX1-A17 (password policy), UX1-A18 (reset-request non-enumeration), UX1-A19 (bootstrap link states), UX1-A21 (TOTP change / recovery regenerate).
- UX1 addendum (Parts A–D, draft) Part A gives the screen inventory S-A1–S-A12 and requirements UXR-A01–UXR-A44 (draft, not approved).
- Build against Spec §8 locks that already exist. Where UX1 asks Spec for new copy or inventory, wait for Spec to land it; do not invent strings, routes, or a11y rules here.

## Test-design cases this Story must satisfy (test design §3.5 Steps 2–5 plus UI smoke §4.14)
- `TD-ADM-010`, `TD-ADM-011`, `TD-ADM-012`, `TD-ADM-020`, `TD-ADM-021`, `TD-ADM-022`, `TD-ADM-030`, `TD-ADM-031`, `TD-ADM-040`, `TD-ADM-041`, `TD-ADM-050`, `TD-ADM-051`, `TD-ADM-052`, `TD-ADM-130`, `TD-ADM-131`, `TD-ADM-150`
- Note: A6 has strong backend auth coverage and thin UI smoke (TD-ADM-040, 130, 131). UX1-A12 lists auth UI gaps with no TD-ADM id yet; new UI cases wait for QA after Spec locks the copy.

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
- Spec still has to land the §8 screen inventory, error copy, password policy, and WCAG lock that UX1 asks for (UX1-A01, A02, A10, A17). The UX1 file is redlines, not approval.
- TOTP digits/period and recovery code count wait for SA (test design OQ2 / UX1-A05).
- Lockout copy stays generic and never shows the lock length (Chief Security UX1-A03 decision, `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md`); Spec copy still has to adopt it.
- Sign-in step shape (one form or password-then-code) waits for Spec + Security (UX1-A16).
- A process-global auth flood limiter waits for explicit Chief Security approval.

## Out of scope (Dev Plan Step 12 and §7)
- Charts, warehouse, settlement, platform admin hosts, CDK or bot-platform internals.
- Invented copy, routes, password rules, or a11y rules beyond what Spec and UX1 approval provide.

## Rules
- Core admin host only: `admin.core.dealoware.com`. Platform hosts are not Core.
- No password values anywhere: code, fixtures, README, chat, or commit history. Use env placeholders only.
- No AWS account details, account IDs, ARNs, region, SES identity, Turnstile keys, or HMAC key values in code, docs, or PRs.

## Sources
- Dev Plan (closed): [`docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md), sha256 `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf`; §4 Steps 2–5, §5, §6, §7, §10.
- Spec v2.2: [`docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md), sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`.
- Test design (A6 PASS): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md), sha256 `870f348c61e2b3d88c2cd6a99e6ae4398186e96c1a7169a16c0b52e2bdd7bca7`.
- Test design QAQA PASS: [`docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md) (docs PR 19, merge `3803d43`), sha256 `1c95803d85264e6214de9f36becc348a9021924e6bfd19b2047cce1995230010`.
- Security Dev Plan-step checklist pts 1–14: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md).
- UX1 findings: `dealoware-kb/ux/2026-10-05__ux__findings__ux1-admin-requirements.md` (Redlines; not approval).
- UX1 UI requirements addendum (Parts A–D, draft): `dealoware-kb/ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md`, sha256 `dd847e702ce628838c22a78dd4491b4047e52aaabfd9b526cfb73509bf2937a7`. Routing: `dealoware-kb/ux/2026-10-05__ux__redlines__ux1-routing.md`, sha256 `fde55fbae19ee3a0f97d27734f902ba3694cb2d6e4f7cd42a2742e0ec0854b4d`.
