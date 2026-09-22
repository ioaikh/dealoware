# Security SD Checklist — MVP Stage B: Contact on Accept (#42)

**Status:** SD implementation verification for Stage B contact-on-accept (P7 / A9 minimum)  
**Date:** 2026-09-22  
**Author:** Dealoware Security  
**Story:** https://github.com/ioaikh/dealoware/issues/42 · Contact on accept (P7 / A9 minimum)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #41 — keep separate SD; cross-ref only  
**Precursor:** PoC **#7** CLOSED — extend, do not rewrite  
**Plan:** `plans/2026-09-22__devplan__plan__mvp-stage-b-contact-on-accept.md`  
**Dev Plan Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-qa-confirm.md`  
**Hold:** Gate **#25** backlog. Stage C + #26–#27 + #18 Spec/SD (whole) HOLD. Mature vault → V3. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-checklist.md`

## Binding Sources

| Source | Role |
|--------|------|
| #42 AC | Binding acceptance criteria |
| #31 Field ACL | Baseline FieldPolicy + FieldClass |
| #7 Identity Seal | PoC stub extended (not rewritten) |
| Architecture Option A | CEO-accepted dual wall; open-ended FieldClass |

---

## Security Checklist Points (1–10) — Implementation Evidence

| # | Security Point | Expected Behavior | Evidence |
|---|----------------|-------------------|----------|
| 1 | **Pre-Accept seal held** — Negotiation/Offer APIs omit counterparty contact PII until Accept; extend #7; no regression | Pre-Accept: No ContactEmail/LoginEmail in responses; IdentitySealed=true | Tests: `PreAccept_GetNegotiation_NoContactEmail_NoLoginEmail`, `PreAccept_GetOffer_NoContactEmail_NoLoginEmail`; `FieldPolicy` denies ShareOutbound without grant |
| 2 | **Accept-grant** — User Accept persists grant; `resourceContext.HasAcceptGrant` for FieldPolicy | AcceptGrant entity created on Accept; FieldResourceContext.HasAcceptGrant = true | `AcceptGrant.CreatePair()` called in AcceptOffer; `FieldResourceContext.ForAcceptedNegotiation()` sets HasAcceptGrant=true; Test: `Accept_HasAcceptGrant_PersistsInDatabase` |
| 3 | **ShareOutbound-after-Accept** — ContactEmail ShareOutbound only with Accept grant to authorized counterparty; before Accept → Deny all | Pre-Accept: ShareOutbound returns false; Post-Accept: returns true for counterparty | `FieldPolicy.EvaluateShareOutbound()` checks HasAcceptGrant + PrincipalType.Counterparty; Tests: `PreAccept_ShareOutbound_Deny_FieldPolicy`, `PostAccept_ShareOutbound_Allow_FieldPolicy` |
| 4 | **ContactEmail policy rows** — User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny | Matrix enforced by FieldPolicy | Tests: `ContactEmail_PolicyMatrix_PreAccept` (theory), `ContactEmail_Counterparty_ShareOutbound_AllowedPostAccept`; existing FieldAclTests |
| 5 | **LoginEmail never-on-Accept** — LoginEmail remains User-only; never shared on Accept; distinct from ContactEmail | LoginEmail never in Accept response; ShareOutbound always false | `EvaluateShareOutbound` returns false for LoginEmail; Tests: `Accept_LoginEmail_NeverIncluded`, `ShareOutbound_LoginEmail_AlwaysDeny`, `LoginEmail_PolicyMatrix` |
| 6 | **Authn/stranger fail-closed** — Unauth deny; stranger deny ContactEmail post-Accept; uniform deny bodies; no private leakage | 401 for unauth; 404 for stranger; no PII in error bodies | Tests: `Unauth_CannotAccessNegotiation_NoPiiLeak`, `Unauth_CannotAcceptOffer`, `PostAccept_StrangerCannotAccessNegotiation` |
| 7 | **Extend #7 under ACL** — Extend seal→contact; do not rewrite #7 history; no Stage C share-tool | AcceptOffer returns AcceptOfferResponse with ContactEmail; existing OfferResponse unchanged; no agent tool | IdentitySealTests updated for Stage B; NegotiationMapper.ToAcceptOfferResponse() added; OfferResponse unchanged |
| 8 | **OUT locked** — P7/A9 minimum; mature vault → V3; gate #25 backlog; no Cognito/MM; no Stage C share-tool | Not implemented | No vault/KMS; no agent share tool; no strategy ACL; gate #25 not opened |
| 9 | **Cost/spend** — PoC **$0**; no IdP/vault provision | No cloud services provisioned | SQLite in-memory for tests; no AWS dependencies |
| 10 | **Evidence + handshake** — Done-list cites paths/tests for 1–9; Security QA confirms | This document; 242 tests pass | `dotnet test` — 242 passed, 0 failed |

---

## Test Evidence Summary

| Test Suite | Tests | Status |
|------------|-------|--------|
| StageBContactOnAcceptTests | 21 | PASS |
| IdentitySealTests | 29 | PASS |
| FieldAclTests | 29 | PASS |
| PocNegotiationScenarioTests | 52 | PASS |
| Other tests | 111 | PASS |
| **Total** | **242** | **PASS** |

### Key Test Methods

| Security Point | Test Method |
|----------------|-------------|
| 1 | `PreAccept_GetNegotiation_NoContactEmail_NoLoginEmail` |
| 2 | `Accept_HasAcceptGrant_PersistsInDatabase` |
| 3 | `PreAccept_ShareOutbound_Deny_FieldPolicy`, `PostAccept_ShareOutbound_Allow_FieldPolicy` |
| 4 | `ContactEmail_PolicyMatrix_PreAccept`, `ContactEmail_Counterparty_ShareOutbound_AllowedPostAccept` |
| 5 | `Accept_LoginEmail_NeverIncluded`, `ShareOutbound_LoginEmail_AlwaysDeny` |
| 6 | `Unauth_CannotAccessNegotiation_NoPiiLeak`, `PostAccept_StrangerCannotAccessNegotiation` |
| 7 | `Accept_CounterpartyReceivesContactEmail_LoginEmailNever`, `AcceptOffer_Response_ContactEmailShared_IdentitySealedFalse_Stage42` |

---

## Code Changes Summary

| Component | Change |
|-----------|--------|
| `FieldResourceContext` | Added `HasAcceptGrant`; new `ForAcceptedNegotiation()` factory |
| `FieldPolicy` | `EvaluateShareOutbound()` checks HasAcceptGrant for ContactEmail |
| `AcceptGrant` | New entity for persisting Accept grants |
| `IAcceptGrantRepository` | New repository interface |
| `AcceptGrantRepository` | Repository implementation |
| `AcceptOfferResponse` | New DTO with CounterpartyContactEmail |
| `NegotiationMapper` | `ToAcceptOfferResponse()` with field ACL projection |
| `OfferEndpoints` | AcceptOffer creates grants, returns ContactEmail |
| Tests | 21 new Stage B tests; updated existing tests |

---

## Done-list (Security SD)

- [x] Security points 1–10 documented with evidence
- [x] Pre-Accept seal maintained (#7 no regression)
- [x] Accept grant persistence implemented
- [x] ShareOutbound(ContactEmail) enabled only with grant
- [x] LoginEmail NEVER shared on Accept
- [x] Stranger/unauth fail-closed verified
- [x] #7 extended, not rewritten
- [x] OUT scope verified (no vault/Stage C/gate #25)
- [x] 242 tests pass
- [x] Ready for Security QA review

## Handshake next

Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical

No AWS/IdP spend. Cost/critical → COO → CEO.
