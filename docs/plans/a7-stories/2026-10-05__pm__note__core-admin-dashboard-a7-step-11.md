# A7 Step 11 — Fail-closed deny for non-CoreOwner, missing TOTP, expired session (core admin dashboard)

**Track:** A7 Core admin dashboard build, Core admin at `admin.core.dealoware.com`. Dev Plan Step 11.
**Tracker:** this file is the A7 Story of record until GitHub Issues are re-enabled on the repo.
**Type:** UI. Server-side deny on admin routes, plus the E2E UI-smoke case TD-ADM-131 that walks unauthenticated admin pages. This Story carries the UI/UX gate (UX1-X09, UXR-D16).

## Owner
- **Chief Developer**
- Source: CPM confirmed Steps 11–13 as Chief Developer, lane lead.

## Acceptance criteria (traced from Spec v2.2 through Dev Plan §4 Step 11 and §5)
- AC6 / Spec §3, §5, §8.1 — a missing, invalid, or expired session, a missing TOTP step, or any principal other than the single CoreOwner is denied on all admin routes with a safe unauthorized response.
- A denied FieldClass is never written and never dumped into the UI or errors.
- No bypass route that skips FieldPolicy or the session/TOTP gates.
- Host isolation: admin routes and admin static assets answer only on `admin.core.dealoware.com` and never on `api.core.dealoware.com` or the raw Express endpoint host; those requests get 404. API routes do not answer on `admin.core.dealoware.com` (except `/health` if the health check needs it), and `AllowedHosts` lists only the expected hosts (Chief Security admin.core exposure review, condition 1).

## Test-design cases this Story must satisfy (test design §3.5, Step 11)
- `TD-ADM-002`, `TD-ADM-003`, `TD-ADM-004`, `TD-ADM-005`, `TD-ADM-131`

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

## Out of scope (Dev Plan Step 12 and §7)
- Any bypass route that skips FieldPolicy or the session/TOTP gates.

## Rules
- Core admin host only: `admin.core.dealoware.com`. Platform hosts are not Core.
- No password values anywhere: code, fixtures, README, chat, or commit history. Use env placeholders only.
- No AWS account details, account IDs, ARNs, region, SES identity, Turnstile keys, or HMAC key values in code, docs, or PRs.

## Sources
- Dev Plan (closed): [`docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md), sha256 `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf`; §4 Step 11, §5, §6, §7, §10.
- Spec v2.2: [`docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md), sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`.
- Test design (A6 PASS): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md), sha256 `870f348c61e2b3d88c2cd6a99e6ae4398186e96c1a7169a16c0b52e2bdd7bca7`.
- Test design QAQA PASS: [`docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md) (docs PR 19, merge `3803d43`), sha256 `1c95803d85264e6214de9f36becc348a9021924e6bfd19b2047cce1995230010`.
- Security Dev Plan-step checklist pts 1–14: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md).
- UX1 findings: `dealoware-kb/ux/2026-10-05__ux__findings__ux1-admin-requirements.md` (Redlines; not approval).
- UX1 UI requirements addendum (Parts A–D, draft): `dealoware-kb/ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md`, sha256 `dd847e702ce628838c22a78dd4491b4047e52aaabfd9b526cfb73509bf2937a7`. Routing: `dealoware-kb/ux/2026-10-05__ux__redlines__ux1-routing.md`, sha256 `fde55fbae19ee3a0f97d27734f902ba3694cb2d6e4f7cd42a2742e0ec0854b4d`.
- Host isolation: Chief Security admin.core exposure review `/workspace/security-out/2026-10-05-admin-core-exposure-pr11-review.md`, sha256 `68230a474c9cb22f1ceb4bae5e17c37052c67746fc518e5249fcf00174cc46b2`, condition 1.
