# CQ Assessment — MVP Stage A Account list fail-closed #32 / PR #34 — NO REFACTOR

**Author:** Dealoware Senior CQ  
**Date:** 2026-09-21  
**Verdict:** **cq:no-refactor**  
**Issue:** https://github.com/ioaikh/dealoware/issues/32  
**PR:** https://github.com/ioaikh/dealoware/pull/34 · HEAD `cf5b0bf400501dea9eafd237d7573656b559d918`  
**Confirm path:** CQ QA → Chief CQ → CPM gate  
**Constraints:** Stage A only; #31 Field ACL HOLD; Stage B/C + #24 HOLD; PoC $0; no Cognito/SSO/vault; no MotorMarket/DC4.

## Verdict

No refactor requirement spec. Query-plane owner/party scoping for Artifact get + Negotiation/Offer list+get meets Stage A; fail-closed 401/404; OUT honored. Soft notes **non-gate**.

## Evidence

1. **Domain ports:** `IArtifactRepository.GetByIdForOwnerAsync`; `IOfferRepository.GetByIdForPartyAsync` + `GetByParticipantAsync` (Negotiation party ports pre-existing).
2. **Query-plane Infra:** Artifact `Id && OwnerParticipantId`; Negotiation party A|B in WHERE; Offer JOIN Negotiations + party WHERE — not fetch-then-filter for list/get/mutations in scope.
3. **API:** Artifact get owner-scoped; `GET /negotiations` list; `GET /offers` + `GET /offers/{id}`; accept/decline/counter use party-scoped offer lookup; AuthHelper → 401; stranger → 404.
4. **Tests:** `StageAFailClosedTests.cs` (~643 lines, 30 Facts) — owner/party OK, stranger deny, IDOR 404, unauth 401, empty-list hygiene, health open.
5. **CI:** Actions run SUCCESS at same HEAD.
6. **OUT:** No #31 Field ACL, Stage B/C Strategy, #24, Cognito/SSO/vault, MM/DC4 in PR.
7. **Peer PASS (KB):** SD verify + Security QA 10/10 matching HEAD; Spec/plan on KB.

## Soft notes (explicitly non-gate)

| Note | Why non-gate |
|------|----------------|
| Parallel AuthHelper+scoped-repo+404 pattern ×3 resources | Expected Stage A depth; extract shared helper later if lists grow |
| Offer JOIN WHERE duplicated in get+list | Small duplication; consolidate later |
| `CreateNegotiation` still uses unscoped `artifactRepository.GetByIdAsync` | Outside Stage A list/get plane; Security soft; tighten if Product binds existence-leak AC |
| OrderBy in-memory on Neg/Offer lists | PoC/MVP scale OK |
| ListNegotiations may SaveChanges (expiration side-effect) | Pre-existing D10 pattern on get paths |
| 404 chosen over 403 for strangers | Spec/Security-aligned non-leak |

## Affected functionality (QA coordination)

1. Artifact `GET` by id — owner-scoped (stranger → 404)  
2. `GET /negotiations` — party-scoped list (empty `[]` OK)  
3. Negotiation get/mutations — party-scoped retained  
4. `GET /offers` + `GET /offers/{id}` — party-via-parent  
5. Offer accept/decline/counter — party-scoped lookup  
6. Unauth → 401 on protected routes; `/health` still open  
7. No private field leak to strangers; no #31/#24/Stage B/C invent  

## Done-list for CQ QA

- [ ] Query-plane owner/party for Artifact/Negotiation/Offer list+get
- [ ] Fail-closed 401/404; health open
- [ ] Soft notes non-gate (incl. CreateNegotiation unscoped artifact get)
- [ ] Affected-functionality complete
- [ ] No #31 / Stage B/C / #24 / Cognito / MM/DC4 invent
- [ ] Confirm PASS to Chief CQ only

