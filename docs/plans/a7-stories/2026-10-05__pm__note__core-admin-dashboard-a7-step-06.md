# A7 Step 6 — SES behind mail interface; no invented AWS provisioning (core admin dashboard)

**Track:** A7 Core admin dashboard build, Core admin at `admin.core.dealoware.com`. Dev Plan Step 6.
**Tracker:** this file is the A7 Story of record until GitHub Issues are re-enabled on the repo.
**Type:** Backend-only. Mail interface and SES adapter; server-side only.

## Owner
- **Chief Developer**
- Source: PM admin dashboard checklist A7 lanes (backend and auth Steps 1–6 and 8 with the Chief Developer).

## Acceptance criteria (traced from Spec v2.2 through Dev Plan §4 Step 6 and §5)
- AC7 / Spec §8.9 — bootstrap and password-reset mail go through a mail interface (port/adapter) with Amazon SES behind it.
- No direct AWS SES SDK package or SES types in the Core app delivery path.
- CFO estimate cited as an estimate only (Turnstile $0/mo; SES under $0.01/mo assumed; Finance QA PASS). It is not spend approval.

## Test-design cases this Story must satisfy (test design §3.5, Step 6)
- `TD-ADM-110`, `TD-ADM-150`

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
- SES provisioning, identity, region, from-address, and any spend wait for a separate spend unlock (CPM to COO to CEO). PoC stays at $0.

## Out of scope (Dev Plan Step 12 and §7)
- A direct AWS SES SDK dependency in the Core app.

## Rules
- Core admin host only: `admin.core.dealoware.com`. Platform hosts are not Core.
- No password values anywhere: code, fixtures, README, chat, or commit history. Use env placeholders only.
- No AWS account details, account IDs, ARNs, region, SES identity, Turnstile keys, or HMAC key values in code, docs, or PRs.

## Sources
- Dev Plan (closed): [`docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md), sha256 `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf`; §4 Step 6, §5, §6, §7, §10.
- Spec v2.2: [`docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md), sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`.
- Test design (A6 PASS): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md), sha256 `870f348c61e2b3d88c2cd6a99e6ae4398186e96c1a7169a16c0b52e2bdd7bca7`.
- Test design QAQA PASS: [`docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md) (docs PR 19, merge `3803d43`), sha256 `1c95803d85264e6214de9f36becc348a9021924e6bfd19b2047cce1995230010`.
- Security Dev Plan-step checklist pts 1–14: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md).
