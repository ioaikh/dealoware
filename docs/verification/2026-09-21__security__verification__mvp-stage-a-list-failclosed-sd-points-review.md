# Verification — Security points vs MVP Stage A #32 Account list fail-closed SD

**Author:** Dealoware Senior Security  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-checklist.md` (10 points)  
**PR:** https://github.com/ioaikh/dealoware/pull/34  
**CI:** Build & Test **pass** (Actions run 35668112927)  
**Tests:** `tests/Dealoware.Api.Tests/StageAFailClosedTests.cs`  
**Issue:** https://github.com/ioaikh/dealoware/issues/32  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-points-review.md`  
**Constraints:** Stage A API/DB only; Stage B/C HOLD; #31 SD confirm PAUSED (not scored here); #7 stub unchanged; PoC $0; no Cognito/MM/DC4.

## Checklist vs PR (Senior score)

| # | Security point | Result | Evidence |
|---|----------------|--------|----------|
| 1 | Authn fail-closed on Neg/Offer/Artifact list+get | **MET** | `ArtifactEndpoints.cs` Get/List: `AuthHelper.GetAuthenticatedSub` → `Results.Unauthorized()` when null (≈96–128). Same pattern `NegotiationEndpoints.cs` list/get (≈72–161), `OfferEndpoints.cs` list/get (≈73–97). Tests: `Artifact_*_UnauthDeny_Returns401`, `Artifact_Get_InvalidAuth_Returns401`, `Negotiation_*_UnauthDeny_Returns401`, `Negotiation_Get_InvalidAuth_Returns401`, `Offer_*_UnauthDeny_Returns401`, `Offer_Get_InvalidAuth_Returns401`. |
| 2 | Negotiation list/get isolation (party; 404 preferred) | **MET** | Query-plane: `NegotiationRepository.GetByIdForPartyAsync` / `GetByParticipantAsync` filter `PartyAParticipantId \|\| PartyBParticipantId` in WHERE (not fetch-then-filter). Endpoints use those methods; stranger → `Results.NotFound()`. Tests: `Negotiation_List_PartyOK_*`, `Negotiation_List_StrangerDeny_ReturnsEmptyListNotForeignRows`, `Negotiation_Get_PartyOK_*`, `Negotiation_Get_StrangerDeny_Returns404NoPrivateFields`, `Negotiation_Get_IDORDeny_Returns404`, `Negotiation_GetByIdForParty_QueryPlaneFilter_NotFetchThenFilter`. |
| 3 | Offer list/get isolation (party via parent Neg) | **MET** | `OfferRepository.GetByIdForPartyAsync` / `GetByParticipantAsync` join Negotiations and filter party. Endpoints + accept/decline/counter use party-scoped lookup → 404. Tests: `Offer_List_PartyOK_*`, `Offer_List_StrangerDeny_ReturnsEmptyListNotForeignRows`, `Offer_Get_*`, `Offer_Get_IDORDeny_Returns404`, `Offer_AcceptDeclineCounter_StrangerDeny_Returns404`, `Offer_GetByIdForParty_QueryPlaneFilter_NotFetchThenFilter`. |
| 4 | Artifact list/get isolation (owner-scoped; align #4) | **MET** | `ArtifactRepository.GetByIdForOwnerAsync` / `GetByOwnerAsync`: `OwnerParticipantId == owner` in WHERE. `GET /artifacts/{id}` uses `GetByIdForOwnerAsync` → NotFound. Tests: `Artifact_List_OwnerOK_*`, `Artifact_List_StrangerDeny_ReturnsEmptyListNotForeignRows`, `Artifact_Get_*`, `Artifact_Get_IDORDeny_Returns404`, `Artifact_GetByIdForOwner_QueryPlaneFilter_NotFetchThenFilter`. |
| 5 | Deny-body / empty-list hygiene | **MET** | Stranger deny tests assert 404 bodies omit private ids (`OwnerParticipantId`, `PartyA/BParticipantId`, `ArtifactId`, `From/ToParticipantId`, `NegotiationId`, `Amount`, `participant:`, `Entities`). List stranger tests return empty `[]` not foreign rows. |
| 6 | Complement #31; do not weaken party/owner rules | **MET** | Tree scan on PR head: no `FieldClass` / `IFieldPolicy` / Field ACL registry. PR hardens query-plane row isolation only; party/owner filters remain strict. #31 SD confirm remains PAUSED — out of this score. |
| 7 | No Stage B/C inventing | **MET** | No Strategy list ACL, agent hard-wall, Cognito, MM/DC4 paths on PR head (tree scan ABSENT). Changes limited to Artifact/Negotiation/Offer list+get (+ offer mutations party-scoped). |
| 8 | Cross-story non-merge | **MET** | Consumes #5 `AuthHelper`; hardens #4/#6 surfaces; does not rewrite #31 or #7 stub. Health remains open (`Health_NoAuth_Returns200`). |
| 9 | Cost / spend PoC $0 | **MET** | Local JWT/ApiKey from #5 only; no IdP/vault/AWS provision in PR. |
| 10 | Evidence + handshake | **MET** | This done-list cites paths/tests for 1–9. Code/Product QA must **not** PASS until Security QA confirms. |

## Soft notes (non-blocking)

- PR body test names use older labels (`Artifact_List_OwnerOK_*` vs body’s `Artifact_List_OwnerOK_ReturnsOnlyOwnArtifacts` style); on-disk `StageAFailClosedTests.cs` names above are authoritative and cover the same cases.
- Naming in code uses Artifact / Negotiation / Offer and `OwnerParticipantId` / `PartyAParticipantId` — aligns Spec semantics (Artifact/Negotiation/Offer).

## Gaps

**None.** All checklist points 1–10 **MET**.

## Done-list (for Security QA)

- [x] DOC-FLOW filed
- [x] Score 10/10 with PR/code/test cites
- [x] CI Build & Test pass cited
- [x] #31 not scored (PAUSED)
- [x] Stage B/C HOLD; #7 stub; PoC $0; no Cognito/MM/DC4
- [ ] Security QA: confirm **PASS** to Chief Security **or** further instructions

## Cost/critical

None.
