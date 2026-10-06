# A7 Step 1 — Host, admin route surface, CoreOwner principal, FieldPolicy bind, net10.0 assumed (core admin dashboard)

**Track:** A7 Core admin dashboard build, Core admin at `admin.core.dealoware.com`. Dev Plan Step 1.
**Tracker:** this file is the A7 Story of record until GitHub Issues are re-enabled on the repo.
**Type:** Backend-only. Host, route surface, CoreOwner principal and FieldPolicy binding; no screen content in the step.

## Owner
- **Chief Developer**
- Source: PM admin dashboard checklist A7 lanes (backend and auth Steps 1–6 and 8 with the Chief Developer).

## Acceptance criteria (traced from Spec v2.2 through Dev Plan §4 Step 1 and §5)
- Spec Locked decision 1 / AC1 — Core admin served only at `admin.core.dealoware.com`, not `admin.platform.dealoware.com` (Spec §3).
- Spec Locked decision 2 / AC6 — single CoreOwner system superadmin; not the Participant UI; all admin reads and writes go through Domain FieldPolicy for CoreOwner; no parallel admin ACL (Spec §3, §5, §7, §8.1).
- Same Core modular monolith with a separate admin route surface (SA Option A).
- App TFM assumed `net10.0`; no .NET 10 retarget work under this Story.
- Host isolation: admin routes and admin static assets answer only on `admin.core.dealoware.com` and never on `api.core.dealoware.com` or the raw Express endpoint host; those requests get 404. API routes do not answer on `admin.core.dealoware.com` (except `/health` if the health check needs it), and `AllowedHosts` lists only the expected hosts (Chief Security admin.core exposure review, condition 1).

## Test-design cases this Story must satisfy (test design §3.5, Step 1)
- `TD-ADM-001`, `TD-ADM-002`, `TD-ADM-005`, `TD-ADM-007`

## Gates (in order)
**At PR review:**
1. **Dev Code QA PASS file** under `docs/verification/` (Code QA names it; prior Story pattern `YYYY-MM-DD__sd__verification__<story>.md`). Test design §5: P0 API/integration cases for the mapped TD-ADM IDs are green.
2. **Security PASS file** under `docs/verification/` (prior SD-step pattern `YYYY-MM-DD__security__verification__<story>-sd-qa-confirm.md`).
3. **Bot Manager merges.**

Deploy waits for Ivan's OK via Bot Manager.

## Active holds
- Merge waits for the Dev Code QA PASS file.
- Merge waits for the Security PASS file.
- Bot Manager does the merge, and only after the PASS files above exist.
- Deploy waits for Ivan's OK via Bot Manager.

## Out of scope (Dev Plan Step 12 and §7)
- Platform hosts (`admin.platform.dealoware.com`, `platform.dealoware.com`, their APIs).
- Multi-admin, operator ACL, human-user list, or Participant UI used as admin.
- .NET 10 retarget work (`net10.0` is assumed).

## Rules
- Core admin host only: `admin.core.dealoware.com`. Platform hosts are not Core.
- No password values anywhere: code, fixtures, README, chat, or commit history. Use env placeholders only.
- No AWS account details, account IDs, ARNs, region, SES identity, Turnstile keys, or HMAC key values in code, docs, or PRs.

## Sources
- Dev Plan (closed): [`docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md), sha256 `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf`; §4 Step 1, §5, §6, §7, §10.
- Spec v2.2: [`docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md), sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`.
- Test design (A6 PASS): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md), sha256 `870f348c61e2b3d88c2cd6a99e6ae4398186e96c1a7169a16c0b52e2bdd7bca7`.
- Test design QAQA PASS: [`docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md) (docs PR 19, merge `3803d43`), sha256 `1c95803d85264e6214de9f36becc348a9021924e6bfd19b2047cce1995230010`.
- Security Dev Plan-step checklist pts 1–14: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md).
- Host isolation: Chief Security admin.core exposure review `/workspace/security-out/2026-10-05-admin-core-exposure-pr11-review.md`, sha256 `68230a474c9cb22f1ceb4bae5e17c37052c67746fc518e5249fcf00174cc46b2`, condition 1.
