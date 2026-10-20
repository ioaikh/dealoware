# A7 Step 13 — Self-verify before handoff to Code QA (core admin dashboard)

**Track:** A7 Core admin dashboard build, Core admin at `admin.core.dealoware.com`. Dev Plan Step 13.
**Tracker:** this file is the A7 Story of record until GitHub Issues are re-enabled on the repo.
**Type:** Backend-only. Self-verify before handoff; no screens built in this Story.

## Owner
- **Chief Developer**
- Source: CPM confirmed Steps 11–13 as Chief Developer, lane lead.

## Acceptance criteria (traced from Spec v2.2 through Dev Plan §4 Step 13 and §5)
- Before handoff, the implementer confirms every Step 13 checklist line in the Dev Plan: host only `admin.core.dealoware.com`; CoreOwner with FieldPolicy dual wall and no parallel ACL; bootstrap, TOTP, reset with link plus 2FA, Turnstile only, lockout and session per Spec; raw IP in counters only; audit IP keyed HMAC-SHA256; SES through the mail interface with no AWS SDK in Core; lists, search, paging, stats, edit, delete, confirm, soft-delete, cascades, and audit per Spec.
- No password values, no AWS account IDs, no process-global flood limiter without approval.
- Traceability: cite Spec v2.2 sha256 and the Security handshake files (Spec 15/15, Dev Plan 14/14).
- Security Dev Plan-step points 1–14 evidence ready for the Security PASS file.
- Covers all Product AC1–AC9 through Steps 1–12; handoff to Code QA, never skipping it.

## Test-design cases this Story must satisfy (test design §3.5, Step 13)
- `TD-ADM-121`, `TD-ADM-122`, `TD-ADM-161`

## Gates (in order)
**At PR review:**
1. **Dev Code QA PASS file** under `docs/verification/` (Code QA names it; prior Story pattern `YYYY-MM-DD__sd__verification__<story>.md`). Test design §5: P0 API/integration cases for the mapped TD-ADM IDs are green.
2. **Security PASS file** under `docs/verification/` (prior SD-step pattern `YYYY-MM-DD__security__verification__<story>-sd-qa-confirm.md`).
3. **Bot Manager merges.**

Deploy waits for Ivan's OK via Bot Manager.
- Before handoff, also confirm the UI/UX gates have cleared on the UI Stories (Steps 2, 3, 4, 5, 7, 8, 9, 10, 11, 14, 15).

## Active holds
- Merge waits for the Dev Code QA PASS file.
- Merge waits for the Security PASS file.
- Bot Manager does the merge, and only after the PASS files above exist.
- Deploy waits for Ivan's OK via Bot Manager.
- Merge waits for the Chief Docs PR(s) that land KB-only cites on main: no Docs PR yet for `qa/2026-10-05__qa__test-design__core-admin-dashboard.md (prior tip on main at 41677bb3)` (`8df39255…`).
- A process-global auth flood limiter waits for explicit Chief Security approval.

## Out of scope (Dev Plan Step 12 and §7)
- LLM or paid API provisioning (any LLM spend goes COO to CEO).

## Rules
- Core admin host only: `admin.core.dealoware.com`. Platform hosts are not Core.
- No password values anywhere: code, fixtures, README, chat, or commit history. Use env placeholders only.
- No AWS account details, account IDs, ARNs, region, SES identity, Turnstile keys, or HMAC key values in code, docs, or PRs.

## Sources
- Dev Plan: [`docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md) on main at commit `6c832941360bc61dc013c45d1475c6cfde4dcf8f`, sha256 `b3788114b4ab890a8117158807773d3e5f8fe84bec4ee0f2f0f0d5df03014a58`; §4 Step 13, §5, §6, §7, §10.
- Dev Plan QA PASS on tip `b3788114…`: [`docs/verification/2026-10-05__devplan__verification__core-admin-dashboard-ux-cite-fix.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__devplan__verification__core-admin-dashboard-ux-cite-fix.md), sha256 `f152aed44bdd59dbebd0f3b955ddfd961aad7b03c283b0e9c71ffaaed94654dc` (on main at `6c832941360bc61dc013c45d1475c6cfde4dcf8f`). Steps 7 and 9 amendment QA PASS also on main: [`docs/verification/2026-10-05__devplan__verification__core-admin-dashboard-steps-7-9-amend.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__devplan__verification__core-admin-dashboard-steps-7-9-amend.md), sha256 `a601577dbe51a6d012ac05f7907f473b7a204e6b32e79cd81bc30e2493ab3a2a`.
- Security QA on tip `b3788114…`: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-b3788114-qa-confirm.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-b3788114-qa-confirm.md), sha256 `640b84adba884d9ae4de9299e0e7da97d5bd2749da3ff82385f44eb1cd8a0b55` (on main at `6c832941360bc61dc013c45d1475c6cfde4dcf8f`). Prior tip `41928cbe…` Security re-check also on main: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-41928cbe-qa-confirm.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-41928cbe-qa-confirm.md), sha256 `4d1970ba4caf2efa29e4d40415ffde30dcbe2bdb622c656ea008a43eb87b9090`.
- Spec v2.2: [`docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md), sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`.
- Test design on main (A6 pin): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md) at commit `41677bb3a01188e4cc18054e3fc7765bda992278`, sha256 `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9`.
- Test design r5 (current): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md) on main at commit `6c832941360bc61dc013c45d1475c6cfde4dcf8f`, sha256 `7bb1dd3b24a630dfca10c3b96969aa1f7d4815108d83fbc497475e1a1864a26f`. Test design QA PASS: [`docs/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-r5-condition3.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-r5-condition3.md) on main at commit `6c832941360bc61dc013c45d1475c6cfde4dcf8f`, sha256 `7a2e06a4bb05f270ad4a9d97764f6d260cc2ebe17b6819c5538b129e64a44a30`.
- Prior Test design QA PASS on the main pin: [`docs/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a16-final-cites.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a16-final-cites.md), sha256 `1d086eb000ab596125d07716465f57e4124dd6c80fea06c9a4659ef5ceb42548`.
- Security Dev Plan-step checklist pts 1–14: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md).
