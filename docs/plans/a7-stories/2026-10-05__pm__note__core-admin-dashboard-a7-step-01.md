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
- Admin page and admin UI asset routes use the `/admin/` prefix (A11 item 8; Chief Security A11 review v0.2 `/workspace/security-out/2026-10-05-a11-admin-core-predeploy-review-v0.md`, sha256 `76e3170b5a1bdb3d8deccdd3173246d2e294b05577cf597900ea40b83530920c`; prior tips `7bc7a3aa…` and `2b596971…` superseded; Architecture route-prefix note r5 `architecture/2026-10-05__ca__architecture__core-admin-route-prefix-note.md`, sha256 `c5e9d232ec67b563de7083dc587fafad4da27d9e59d9760ea496be9c9e3768b7`; prior r4 `b7dc5eca…`, r3 `b079a814…`, r2 `a4fa4351…` and r1 `f03a8c68…` VOID). Architecture QA formal PASS `verification/2026-10-05__sa__verification__core-admin-route-prefix-note.md`, sha256 `a09ea9cce1676581a69d33595a6104bd574fd9c8ed5b6890742adb0c42b607a4`; Security QA CLEAR `verification/2026-10-05__security__verification__core-admin-route-prefix-note-c5e9d232-qa-confirm.md`, sha256 `0cdc9eca92a57aa3919167c91641aa406a4cdd9d263688ad5dce8f30f38bbd30`. Spec path-prefix note v1.2.1 `specs/2026-10-05__spec__spec__core-admin-path-prefix-note.md`, sha256 `60116f1d34aef9a8c2fbe907715e1c197970532d0212e084af0e3bbb86da6709` (prior tips `92b416bc…`, `b7f8515d…`, `1c5c4989…`, `cb477044…` VOID); Security QA PASS `verification/2026-10-05__security__verification__core-admin-path-prefix-note-60116f1d-qa-confirm.md`, sha256 `f706e4bcec5109689f3587d2218a801a8bdc60de156437309b254206a498ee2d`; Spec QA PASS `verification/2026-10-05__spec__verification__core-admin-path-prefix-note.md`, sha256 `3345b184744135dde82ede949c3ede9ef4ed119f3693e02d44fa1e1de1f3d880`. Canonical examples: `/admin/sign-in`, `/admin/sign-in/code`, `/admin/sign-in/recovery`, `/admin/bootstrap`, `/admin/link-expired`, `/admin/setup/authenticator`, `/admin/setup/recovery-codes`, `/admin/reset`, `/admin/reset/sent`, `/admin/reset/confirm`, `/admin/settings/security`, `/admin/`, `/admin/participants`, `/admin/artifacts`, `/admin/negotiations`, `/admin/offers`, `/admin/{type}/{id}`, `/admin/audit`, `/admin/audit/{id}`, POST `/admin/sign-out`. `/health` stays outside `/admin/`. Core API top-level paths on `api.core` are unchanged.
- Admin JSON APIs stay under `/admin/api/…`. Only the signed-out exemption set in the route-prefix note r5 §2 may bypass the session gate; no other `/admin/api/**` path is exempt.

## Test-design cases this Story must satisfy (test design §3.5, Step 1)
- `TD-ADM-001`, `TD-ADM-002`, `TD-ADM-005`, `TD-ADM-007`, `TD-ADM-029`

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
- Merge waits for the Chief Docs PR(s) that land KB-only cites on main: no Docs PR yet for `verification/2026-10-05__security__verification__core-admin-route-prefix-note-c5e9d232-qa-confirm.md` (`0cdc9eca…`); `verification/2026-10-05__spec__verification__core-admin-path-prefix-note.md` (`3345b184…`); `specs/2026-10-05__spec__spec__core-admin-path-prefix-note.md` (`60116f1d…`); `/workspace/security-out/2026-10-05-admin-core-exposure-pr11-review.md` (`68230a47…`); `/workspace/security-out/2026-10-05-a11-admin-core-predeploy-review-v0.md` (`76e3170b…`); `qa/2026-10-05__qa__test-design__core-admin-dashboard.md (prior tip on main at 41677bb3)` (`8df39255…`); `verification/2026-10-05__sa__verification__core-admin-route-prefix-note.md` (`a09ea9cc…`); `architecture/2026-10-05__ca__architecture__core-admin-route-prefix-note.md` (`c5e9d232…`); `verification/2026-10-05__security__verification__core-admin-path-prefix-note-60116f1d-qa-confirm.md` (`f706e4bc…`).

## Out of scope (Dev Plan Step 12 and §7)
- Platform hosts (`admin.platform.dealoware.com`, `platform.dealoware.com`, their APIs).
- Multi-admin, operator ACL, human-user list, or Participant UI used as admin.
- .NET 10 retarget work (`net10.0` is assumed).

## Rules
- Core admin host only: `admin.core.dealoware.com`. Platform hosts are not Core.
- No password values anywhere: code, fixtures, README, chat, or commit history. Use env placeholders only.
- No AWS account details, account IDs, ARNs, region, SES identity, Turnstile keys, or HMAC key values in code, docs, or PRs.

## Sources
- Dev Plan: [`docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md) on main at commit `6c832941360bc61dc013c45d1475c6cfde4dcf8f`, sha256 `b3788114b4ab890a8117158807773d3e5f8fe84bec4ee0f2f0f0d5df03014a58`; §4 Step 1, §5, §6, §7, §10.
- Dev Plan QA PASS on tip `b3788114…`: [`docs/verification/2026-10-05__devplan__verification__core-admin-dashboard-ux-cite-fix.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__devplan__verification__core-admin-dashboard-ux-cite-fix.md), sha256 `f152aed44bdd59dbebd0f3b955ddfd961aad7b03c283b0e9c71ffaaed94654dc` (on main at `6c832941360bc61dc013c45d1475c6cfde4dcf8f`). Steps 7 and 9 amendment QA PASS also on main: [`docs/verification/2026-10-05__devplan__verification__core-admin-dashboard-steps-7-9-amend.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__devplan__verification__core-admin-dashboard-steps-7-9-amend.md), sha256 `a601577dbe51a6d012ac05f7907f473b7a204e6b32e79cd81bc30e2493ab3a2a`.
- Security QA on tip `b3788114…`: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-b3788114-qa-confirm.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-b3788114-qa-confirm.md), sha256 `640b84adba884d9ae4de9299e0e7da97d5bd2749da3ff82385f44eb1cd8a0b55` (on main at `6c832941360bc61dc013c45d1475c6cfde4dcf8f`). Prior tip `41928cbe…` Security re-check also on main: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-41928cbe-qa-confirm.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-41928cbe-qa-confirm.md), sha256 `4d1970ba4caf2efa29e4d40415ffde30dcbe2bdb622c656ea008a43eb87b9090`.
- Spec v2.2: [`docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md), sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`.
- Test design on main (A6 pin): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md) at commit `41677bb3a01188e4cc18054e3fc7765bda992278`, sha256 `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9`.
- Test design r5 (current): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md) on main at commit `6c832941360bc61dc013c45d1475c6cfde4dcf8f`, sha256 `7bb1dd3b24a630dfca10c3b96969aa1f7d4815108d83fbc497475e1a1864a26f`. Test design QA PASS: [`docs/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-r5-condition3.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-r5-condition3.md) on main at commit `6c832941360bc61dc013c45d1475c6cfde4dcf8f`, sha256 `7a2e06a4bb05f270ad4a9d97764f6d260cc2ebe17b6819c5538b129e64a44a30`.
- Prior Test design QA PASS on the main pin: [`docs/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a16-final-cites.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a16-final-cites.md), sha256 `1d086eb000ab596125d07716465f57e4124dd6c80fea06c9a4659ef5ceb42548`.
- Security Dev Plan-step checklist pts 1–14: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md).
- Host isolation: Chief Security admin.core exposure review `/workspace/security-out/2026-10-05-admin-core-exposure-pr11-review.md`, sha256 `68230a474c9cb22f1ceb4bae5e17c37052c67746fc518e5249fcf00174cc46b2`, condition 1.
- `/admin/` route prefix (A11 item 8): Chief Security A11 review v0.2 `/workspace/security-out/2026-10-05-a11-admin-core-predeploy-review-v0.md`, sha256 `76e3170b5a1bdb3d8deccdd3173246d2e294b05577cf597900ea40b83530920c` (prior `2b596971…` superseded); Architecture note r5 sha256 `c5e9d232…` (prior r4 `b7dc5eca…` and r3 `b079a814…` VOID); Arch QA formal PASS `a09ea9cc…`; Security QA CLEAR `0cdc9eca…`. Spec path-prefix note v1.2.1 sha256 `60116f1d…`; Spec QA PASS `3345b184…`; Security QA PASS `f706e4bc…` (prior `92b416bc…` VOID).
