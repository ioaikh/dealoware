# Security QA — MVP Stage A #32 Account list fail-closed SD vs Chief Security SD checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-21  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Asked by:** Bot Manager / Chief Security — SD review handshake (PRIORITY ESCALATION)  
**Chief checklist (binding):** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-checklist.md` (10 points)  
**Senior Security done-list:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-points-review.md` (PASS 10/10)  
**Dev Plan Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md`  
**Format ref:** `verification/2026-09-20__security__verification__poc-identity-seal-sd-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/34  
**HEAD:** `cf5b0bf400501dea9eafd237d7573656b559d918` (verified via `gh pr view 34`; matches PR headRefOid)  
**CI:** Build & Test **SUCCESS** (Actions run 35668112927 @ HEAD)  
**Tests:** `tests/Dealoware.Api.Tests/StageAFailClosedTests.cs` (30 new; PR claims 192 total pass; CI green)  
**Issue:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #31 Field ACL — **not scored / not confirmed here** (keep separate)  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-qa-confirm.md`  
**Constraints:** Stage A only; Stage B/C + gate #24 OUT; PoC **$0**; no Cognito/MM/DC4; #7 stub unchanged; never skip Chief. Reviewed via `gh` remote reads (no clone). Soft: no live `dotnet test` on this box — CI SUCCESS used.

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security SD checklist | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-checklist.md` | Binding 10 points |
| Senior Security points-review | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-points-review.md` | PASS 10/10 — present |
| PR #34 | `ioaikh/dealoware` @ `cf5b0bf…` | 9 files; Artifact/Neg/Offer endpoints + repos + StageAFailClosedTests |
| CI | Actions run 35668112927 | SUCCESS @ same HEAD |
| Dev Plan Security PASS | `…list-failclosed-devplan-qa-confirm.md` | Upstream unlock context |

## Independent re-score (Security QA)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Authn fail-closed — Neg/Offer/Artifact list+get require validated #5 principal; unauth → 401/403; no private-field leakage | **MET** | `ArtifactEndpoints` Get/List: `AuthHelper.GetAuthenticatedSub` → `Results.Unauthorized()` when sub null. Same on `NegotiationEndpoints` ListNegotiations/GetNegotiation and `OfferEndpoints` ListOffers/GetOffer. Tests: `Artifact_*_UnauthDeny_Returns401`, `Artifact_Get_InvalidAuth_Returns401`, `Negotiation_*_UnauthDeny_Returns401`, `Negotiation_Get_InvalidAuth_Returns401`, `Offer_*_UnauthDeny_Returns401`, `Offer_Get_InvalidAuth_Returns401`. Health stays open (`Health_NoAuth_Returns200`). |
| 2 | Negotiation list/get isolation — owner/party only; unauthorized → 404 preferred; no cross-account leak | **MET** | Query-plane: `NegotiationRepository.GetByIdForPartyAsync` / `GetByParticipantAsync` filter `PartyAParticipantId \|\| PartyBParticipantId` in WHERE. Endpoints call those methods; stranger → `Results.NotFound()`. Tests: `Negotiation_List_PartyOK_*`, `Negotiation_List_StrangerDeny_ReturnsEmptyListNotForeignRows`, `Negotiation_Get_PartyOK_*`, `Negotiation_Get_StrangerDeny_Returns404NoPrivateFields`, `Negotiation_Get_IDORDeny_Returns404`, `Negotiation_GetByIdForParty_QueryPlaneFilter_NotFetchThenFilter`. |
| 3 | Offer list/get isolation — party via parent Negotiation; same fail-closed | **MET** | `OfferRepository.GetByIdForPartyAsync` / `GetByParticipantAsync` JOIN Negotiations + party WHERE. List/get + accept/decline/counter use party-scoped lookup → 404. Tests: `Offer_List_PartyOK_*`, `Offer_List_StrangerDeny_ReturnsEmptyListNotForeignRows`, `Offer_Get_*`, `Offer_Get_IDORDeny_Returns404`, `Offer_AcceptDeclineCounter_StrangerDeny_Returns404`, `Offer_GetByIdForParty_QueryPlaneFilter_NotFetchThenFilter`. |
| 4 | Artifact list/get isolation — OwnerParticipantId == sub; IDOR fail-closed; align #4 | **MET** | `ArtifactRepository.GetByIdForOwnerAsync` / `GetByOwnerAsync`: `OwnerParticipantId == owner` in WHERE. `GET /artifacts/{id}` uses `GetByIdForOwnerAsync` → NotFound for stranger/IDOR. Tests: `Artifact_List_OwnerOK_*`, `Artifact_List_StrangerDeny_ReturnsEmptyListNotForeignRows`, `Artifact_Get_*`, `Artifact_Get_IDORDeny_Returns404`, `Artifact_GetByIdForOwner_QueryPlaneFilter_NotFetchThenFilter`. |
| 5 | Deny-body / empty-list hygiene — errors and empty lists do not leak other Participants’ private fields | **MET** | Stranger deny tests assert 404 bodies omit `OwnerParticipantId` / `participant:` / `Entities`; `PartyA/BParticipantId` / `ArtifactId`; `From/ToParticipantId` / `NegotiationId` / `Amount`. List stranger tests return empty `[]` not foreign rows. List/get deny paths use parameterless `Results.NotFound()`. |
| 6 | Complement #31; do not weaken #6 party-only — Field ACL projects fields; list isolation stays owner/party query-plane | **MET** | PR files: endpoints/repos/tests only — no Field ACL registry / FieldClass / IFieldPolicy. Query-plane row isolation hardened; party/owner filters remain strict. **#31 not scored here.** |
| 7 | No Stage B/C inventing — no Strategy list ACL, agent hard-wall, Cognito, or MM/DC4 | **MET** | Diff/tree: no Strategy ACL, Cognito, MM/DC4, agent hard-wall. Changes limited to Artifact/Negotiation/Offer list+get (+ offer mutations party-scoped). PR Explicit OUT aligns. |
| 8 | Cross-story non-merge — harden/consume #4–#7; do not rewrite #31 or PoC Stories | **MET** | Consumes #5 `AuthHelper`; hardens #4 Artifact / #6 Neg+Offer surfaces; does not rewrite #31 or #7 stub. Health remains open. Sibling #31 kept separate. |
| 9 | Cost / spend — PoC **$0**; no IdP/vault provision | **MET** | Local JWT/ApiKey from #5 only; no Cognito/IdP/vault/AWS provision in PR. |
| 10 | Evidence + handshake — done-list cites 1–9; Code/Product QA must **not** PASS until Security QA confirms | **MET** | This confirm + Senior done-list cite paths/tests for 1–9. CI SUCCESS @ HEAD. **Dev Code QA / Product QA must not PASS until this Security QA confirm.** |

## Soft notes (non-blocking)

- **Senior SD points-review present** and scored 10/10 MET — independent Security QA re-score **agrees**; no content gaps.
- `CreateNegotiation` still uses unscope `artifactRepository.GetByIdAsync` for existence (returns `NotFound({ error = "Artifact not found" })`) — **outside list+get #32 surface**; soft observe only, not a checklist fail.
- No live `dotnet test` on this box — static `gh` review + CI Build & Test SUCCESS @ HEAD (aligns prior SD QA posture).
- **#31 not reviewed / not confirmed** in this document.

## Alignment with Senior review

Senior Security done-list (`…list-failclosed-sd-points-review.md`) scored all 10 **MET** with matching PR #34 / endpoint / repository / `StageAFailClosedTests` cites and CI run 35668112927. Independent Security QA re-score **agrees** — no gaps; soft notes complement (CreateNegotiation GetByIdAsync observe; #31 unscored).

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Fail-closed list+get (401 unauth / 404 stranger) | Held (endpoints + StageAFailClosedTests) |
| Owner/party isolation query-plane | Held (GetByIdForOwner / GetByIdForParty / GetByParticipant WHERE+JOIN) |
| No private leak in deny/empty | Held (NoPrivateFields + EmptyListNotForeignRows tests) |
| Stage A only; Stage B/C + #24 OUT | Held (PR OUT; no Strategy/Cognito/MM/DC4) |
| #31 separate — not scored here | Held |
| PoC $0; no Cognito/MM/DC4 | Held |
| #7 stub unchanged | Held (not touched in PR files) |

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are non-blocking.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Bot Manager / Senior Developer / Chief Developer may proceed. Dev Code QA / Product QA may PASS on Security gate after this confirm. Cost/critical: none. No AWS/IdP/vault spend. Do **not** open gate #24. Do **not** treat this as #31 confirm.
