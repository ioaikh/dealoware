# A7 Step 7 — Lists, sort/filter, server-side paging, name search, stats (core admin dashboard)

**Track:** A7 Core admin dashboard build, Core admin at `admin.core.dealoware.com`. Dev Plan Step 7.
**Tracker:** this file is the A7 Story of record until GitHub Issues are re-enabled on the repo.
**Type:** UI. Entity list screens with sort/filter, paging, name search, and the stats view.

## Owner
- **Senior Developer**
- Source: PM admin dashboard checklist A7 lanes (entity API and UI Steps 7, 9, 10 with the Senior Developer).

## Acceptance criteria (traced from Spec v2.2 through Dev Plan §4 Step 7 and §5)
- AC4 / Spec §4.5 as amended by the Spec status-list note (UXR-B03 filters, UXR-B07 badges) — negotiation statuses are Open, Closed, Expired only (no negotiation-level Withdrawn, Accepted or Declined). Offer statuses are Open, Accepted, Declined, Withdrawn, Superseded, Cancelled; `Withdrawn` applies to offers only and is a first-class value; Superseded shows as "Superseded (countered)". Each list's status filter offers exactly its own values; statuses are text badges, never color alone; soft-deleted rows show a "Deleted" badge behind the CoreOwner-only toggle, and Deleted is not a status.
- Soft-deleted rows appear only through the CoreOwner-only toggle; using the toggle is a read and is not audited (Spec §4.5, §10).
- AC4 / Spec §4.5 — server-side sort/filter on status, created/updated, participant, artifact, value/price, negotiation id; offset/limit paging, default 50, max 200; no client-side full-table dump.
- AC2–AC4 / Spec §4.6 — case-insensitive contains name search on all four tables per Product definitions; parameterized; FieldPolicy applied to results.
- AC5 / Spec §6 — stats: Participants, open negotiations (`Status = Open` and `DeletedAt IS NULL`), offers, accepts, declines; all exclude soft-deleted.

## Test-design cases this Story must satisfy (test design §3.5, Step 7)
- `TD-ADM-006`, `TD-ADM-060`, `TD-ADM-061`, `TD-ADM-062`, `TD-ADM-063`, `TD-ADM-064`, `TD-ADM-065`, `TD-ADM-066`, `TD-ADM-070`, `TD-ADM-130`, `TD-ADM-140`

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
- Charts and a data warehouse.
- A human-user list on Core.

## Rules
- Core admin host only: `admin.core.dealoware.com`. Platform hosts are not Core.
- No password values anywhere: code, fixtures, README, chat, or commit history. Use env placeholders only.
- No AWS account details, account IDs, ARNs, region, SES identity, Turnstile keys, or HMAC key values in code, docs, or PRs.

## Sources
- Dev Plan (closed): [`docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/plans/2026-10-05__devplan__plan__core-admin-dashboard.md), sha256 `5fefb550e0c6565820d552dabe60356493d54474fd0871083884cee643bd4eaf`; §4 Step 7, §5, §6, §7, §10.
- Spec v2.2: [`docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/specs/2026-10-05__spec__spec__core-admin-dashboard.md), sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`.
- Test design (A6 PASS): [`docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md`](https://github.com/ioaikh/dealoware/blob/main/docs/qa/2026-10-05__qa__test-design__core-admin-dashboard.md), sha256 `870f348c61e2b3d88c2cd6a99e6ae4398186e96c1a7169a16c0b52e2bdd7bca7`.
- Test design QAQA PASS: [`docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__qa__verification__core-admin-dashboard-test-design.md) (docs PR 19, merge `3803d43`), sha256 `1c95803d85264e6214de9f36becc348a9021924e6bfd19b2047cce1995230010`.
- Security Dev Plan-step checklist pts 1–14: [`docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md`](https://github.com/ioaikh/dealoware/blob/main/docs/verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md).
- UX1 findings: `dealoware-kb/ux/2026-10-05__ux__findings__ux1-admin-requirements.md` (Redlines; not approval).
- UX1 UI requirements addendum (Parts A–D, draft): `dealoware-kb/ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md`, sha256 `dd847e702ce628838c22a78dd4491b4047e52aaabfd9b526cfb73509bf2937a7`. Routing: `dealoware-kb/ux/2026-10-05__ux__redlines__ux1-routing.md`, sha256 `fde55fbae19ee3a0f97d27734f902ba3694cb2d6e4f7cd42a2742e0ec0854b4d`.
- Spec status-list note (§4.5 status amendment, written by the Senior Spec): KB `specs/2026-10-05__spec__spec__core-admin-status-list-note.md`, sha256 `0a5ff57b2c55059373494e8452ad2d0931f055aae830c1704336859bbcfaa910`. Spec QA PASS: KB `verification/2026-10-05__spec__verification__core-admin-status-list-note.md`, sha256 `ce3ca2590d2e9cedc20c0dfec905744ee06e8bc6be3c39b20d2b223733b2deca`.
