# A7 Step 12 — Explicit holds, OUT list, and gate pack (gate pack, no implementation) (core admin dashboard)

**Track:** A7 Core admin dashboard build, Core admin at `admin.core.dealoware.com`. Dev Plan Step 12.
**Tracker:** this file is the A7 Story of record until GitHub Issues are re-enabled on the repo.
**Type:** Backend-only. Gate pack, no implementation.

## Owner
- **Chief Developer**
- Source: CPM confirmed Steps 11–13 as Chief Developer, lane lead.

## Acceptance criteria (traced from Spec v2.2 through Dev Plan §4 Step 12 and §5)
- This Story is a gate pack. No code is delivered. It tracks that the delivery for Steps 1–11 respects the Dev Plan Step 12 and §7 OUT list.
- AC8 / Spec §12 OUT — no inbound bot connector, platform admin, human registration, payments, or token allowance.
- AC9 / Spec §12 — build and deploy gated as stated; no invented AWS; PoC $0 until a separate spend unlock.
- AC1 / Spec Locked decision 1 — Core admin host only.

## Test-design cases this Story must satisfy (test design §3.5, Step 12)
- `TD-ADM-006`, `TD-ADM-007`, `TD-ADM-011`, `TD-ADM-021`, `TD-ADM-041`, `TD-ADM-054`, `TD-ADM-120`, `TD-ADM-121`, `TD-ADM-150`, `TD-ADM-160`

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
- A process-global auth flood limiter waits for explicit Chief Security approval.

## Out of scope (Dev Plan Step 12 and §7)
- Platform admin hosts; human-user list on Core; Participant UI as admin; multiple admin humans or operator ACL.
- Email OTP fallback; CAPTCHA other than Turnstile; direct AWS SES SDK in Core; SSO/IdP.
- Settlement, escrow, checkout; charts, warehouse, cost views; MotorMarket and DC4; App Runner; CDK or bot-platform internals.
- Inbound bot connector (comes after this track). Harden redeploy H1 (separate track).
- .NET 10 retarget (`net10.0` assumed). Hard delete (deferred). Stage C bot-isolation Stories (cross-reference only).

## Rules
- Core admin host only: `admin.core.dealoware.com`. Platform hosts are not Core.
- No password values anywhere: code, fixtures, README, chat, or commit history. Use env placeholders only.
- No AWS account details, account IDs, ARNs, region, SES identity, Turnstile keys, or HMAC key values in code, docs, or PRs.

## Sources
- Dev Plan (closed): [`docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md), sha256 `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf`; §4 Step 12, §5, §6, §7, §10.
- Spec v2.2: [`docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md), sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`.
- Test design (A6 PASS): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md), sha256 `870f348c61e2b3d88c2cd6a99e6ae4398186e96c1a7169a16c0b52e2bdd7bca7`.
- Test design QAQA PASS: [`docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md) (docs PR 19, merge `3803d43`), sha256 `1c95803d85264e6214de9f36becc348a9021924e6bfd19b2047cce1995230010`.
- Security Dev Plan-step checklist pts 1–14: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md).
