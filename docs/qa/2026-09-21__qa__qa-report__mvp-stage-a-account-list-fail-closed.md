# QA Report — MVP Stage A Account list fail-closed (#32)

**Status:** **PASS** — Security QA confirm landed (pts 1–10 MET); QAQA confirmed Product QA PASS to Chief (no bounce)  
**Date:** 2026-09-21  
**Author:** Dealoware Senior Product QA  
**Confirm to:** Dealoware QA (Chief QA) via QAQA **after** Security handshake  
**Story:** GitHub issue #32 · Account list fail-closed (negotiations / offers / artifacts)  
**Issue:** https://github.com/ioaikh/dealoware/issues/32  
**PR:** https://github.com/ioaikh/dealoware/pull/34 (**MERGED**)  
**PR HEAD:** `cf5b0bf400501dea9eafd237d7573656b559d918`  
**main merge commit:** `3812525301fe9767f0678736843d82a693f992c8`  
**MergedAt:** 2026-09-21T23:45:32Z (≈ 2026-09-21 19:45 EDT)  
**CI (PR):** SUCCESS Build & Test (pre-merge)  
**CI (main @ merge):** https://github.com/ioaikh/dealoware/actions/runs/35669004132 — **SUCCESS**  
**Security checklist:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-checklist.md`  
**Security QA confirm (this step):** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-qa-confirm.md` — **PASS** (pts 1–10 MET)  
**Prior SD:** `verification/2026-09-21__sd__verification__mvp-stage-a-account-list-fail-closed.md` (SD PASS @ `cf5b0bf…`)  
**Prior SD Security QA:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-qa-confirm.md` (PASS 10/10)  
**CQ:** `cq:no-refactor`  
**DOC-FLOW:** `qa/2026-09-21__qa__qa-report__mvp-stage-a-account-list-fail-closed.md`  
**Constraints:** PoC $0; #31 OUT/separate; Stage B/C HOLD; gate #24 not opened; no MotorMarket; no inventing  

## Method

- `gh pr view 34` / contents at **main** `38125253…` (no clone)
- KB: Product QA Security checklist; SD verification (cite only — do not re-litigate Code QA)
- Soft gap: no live `dotnet test` on this box → **CI + `StageAFailClosedTests.cs` equivalent** (prior PoC pattern)
- Re-verified on merge commit after CPM merge (not stale PR HEAD only)

## Product acceptance criteria

| AC | Verdict | Evidence (main `38125253…`) |
|----|---------|------------------------------|
| 1 Artifact list/get owner-scoped; stranger/wrong/unauth fail-closed | **MET** | `Artifact_List_OwnerOK_*`; `Artifact_List_StrangerDeny_ReturnsEmptyListNotForeignRows`; `Artifact_Get_OwnerOK_*`; `Artifact_Get_StrangerDeny_Returns404NoPrivateFields`; `Artifact_Get_IDORDeny_*`; `Artifact_*_UnauthDeny_Returns401`; `Artifact_Get_InvalidAuth_*`; query-plane `Artifact_GetByIdForOwner_QueryPlaneFilter_*` |
| 2 Negotiation list/get party A\|B only; cross-tenant + IDOR fail-closed | **MET** | `Negotiation_List_PartyOK_*`; `Negotiation_List_StrangerDeny_ReturnsEmptyListNotForeignRows`; `Negotiation_Get_PartyOK_*` (A+B 200); `Negotiation_Get_StrangerDeny_Returns404NoPrivateFields`; `Negotiation_Get_IDORDeny_*`; unauth/invalid 401; `Negotiation_GetByIdForParty_QueryPlaneFilter_*` |
| 3 Offer list/get via party to parent negotiation; IDOR/cross-tenant fail-closed | **MET** | `Offer_List_PartyOK_*`; `Offer_List_StrangerDeny_ReturnsEmptyListNotForeignRows`; `Offer_Get_PartyOK_*`; `Offer_Get_StrangerDeny_Returns404NoPrivateFields`; `Offer_Get_IDORDeny_*`; unauth/invalid 401; `Offer_AcceptDeclineCounter_StrangerDeny_Returns404`; `Offer_GetByIdForParty_QueryPlaneFilter_*` |
| 4 Unauth → 401; wrong principal → 403/404; uniform deny; no private-field leak | **MET** | Unauth list/get → 401 across Artifact/Neg/Offer; stranger get → 404 with `DoesNotContain` private fields; stranger list → `[]` not foreign rows |
| 5 Automated tests: owner/party OK; stranger; IDOR; unauth | **MET** | `tests/Dealoware.Api.Tests/StageAFailClosedTests.cs` (30 Facts); main CI SUCCESS run 35669004132 |

## Security checklist points 1–10 (Product QA evidence)

| # | Point | Verdict | Evidence |
|---|-------|---------|----------|
| 1 | Authn fail-closed (list+get) | **EVIDENCED** | `*_UnauthDeny_Returns401` + `*_InvalidAuth_Returns401` for Artifact/Neg/Offer; Health remains open (`Health_NoAuth_Returns200`) |
| 2 | Negotiation list/get isolation | **EVIDENCED** | Party OK + stranger empty/404 + IDOR 404 + query-plane filter Fact |
| 3 | Offer list/get isolation (+ stranger accept/decline/counter) | **EVIDENCED** | Party via parent; stranger empty/404; `Offer_AcceptDeclineCounter_StrangerDeny_Returns404` |
| 4 | Artifact list/get OwnerParticipantId isolation | **EVIDENCED** | Owner OK; stranger empty/404; IDOR; query-plane Fact |
| 5 | Deny-body / empty-list hygiene | **EVIDENCED** | Stranger list → empty; stranger get bodies assert no Owner/Party/Artifact private fields |
| 6 | Complement #31; don’t weaken party rules | **EVIDENCED** | PR scoped to list+get fail-closed; no Field ACL invent; #31 OUT |
| 7 | No Stage B/C inventing | **EVIDENCED** | No Strategy ACL / agent hard-wall / Cognito / MM-DC4 in Product QA scope |
| 8 | Cross-story non-merge | **EVIDENCED** | Hardens #4–#7 surfaces only; does not rewrite #31 |
| 9 | Cost / spend $0 | **EVIDENCED** | No IdP/vault provision; PoC $0 |
| 10 | Handshake close | **PASS** | Security QA `…list-failclosed-productqa-qa-confirm.md` PASS (pts 1–10 MET); soft gaps accepted |

## Soft gaps / non-blockers

- No live `dotnet restore|build|test` on evidence box (`dotnet` absent) — CI + tests equivalent stated
- Named IDOR Facts use random GUIDs; cross-tenant stranger cases cover foreign-owned IDs
- Residual unscoped `GetByIdAsync` on CreateNegotiation flagged in SD soft notes — outside list+get AC (cite only)

## OUT / HOLD (verified)

- #31 Field ACL not delivered by this Story  
- Stage B/C · gate #24 · MotorMarket · inventing  

## Disposition

**PASS** — Security QA `…list-failclosed-productqa-qa-confirm.md` PASS (pts 1–10 MET); soft gaps (no live dotnet / CI equivalent) accepted.  
QAQA confirmed Product QA PASS to Chief (no bounce).  
Do not set GitHub status from this step alone (PM/Chief owns gate). #31 remains OUT/not confirmed.

### Done-list (QAQA)

- [x] Evidence at main merge `38125253…` (+ PR HEAD `cf5b0bf…`)
- [x] AC 1–5 + Sec 1–9 woven with citations
- [x] Product-step Security QA confirm PASS (pt 10)
- [x] QAQA confirm Product QA PASS to Chief
