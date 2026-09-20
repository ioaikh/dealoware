# Verification — Security points vs PoC Negotiation SD (#6)

**Author:** Dealoware Senior Security  
**Date:** 2026-09-20  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Checklist (binding):** `verification/2026-09-20__security__verification__poc-negotiation-sd-checklist.md` (10 points)  
**Evidence:** https://github.com/ioaikh/dealoware/pull/15 · branch `cursor/negotiation-offer-d7-d10-6cc1`  
**Plan:** `plans/2026-09-20__devplan__plan__poc-negotiation-offers-d7-d10.md`  
**Dev Plan Security PASS:** `verification/2026-09-20__security__verification__poc-negotiation-devplan-qa-confirm.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/6  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-negotiation-sd-points-review.md`  
**Constraints:** PoC $0; no Cognito/SSO; no MM/DC4; #7–#8 not invented; consume #4/#5 only. Reviewed via `gh` remote reads (no clone).

## Sources checked

| Source | Path / link | Result |
|--------|-------------|--------|
| SD Security checklist (Chief) | `…poc-negotiation-sd-checklist.md` | Binding 10 points |
| PR #15 | https://github.com/ioaikh/dealoware/pull/15 | Negotiation/Offer endpoints, domain, tests, README |
| Key paths | `NegotiationEndpoints.cs`, `OfferEndpoints.cs`, `Negotiation.cs`, `Offer.cs`, `IntentComplement.cs`, `OfferResponse.cs`, `NegotiationEndpointTests.cs` | Reviewed |
| Diff scan | Cognito/SSO/settlement/MM/DC4 packages | No hits (JWT/IdentityModel + EF SQLite only) |

## Checklist vs SD (evidence)

| # | Security point | Result | Evidence |
|---|----------------|--------|----------|
| 1 | **Authn fail-closed** | **MET** | All Negotiation/Offer handlers call `AuthHelper.GetAuthenticatedSub`; missing/invalid → **401**. Tests: `*_WithoutAuth_Returns401` on create/get/place/accept/decline/counter/close. Health open: `Health_WithoutAuth_Returns200`. |
| 2 | **Party-only authz** | **MET** | `GetByIdForPartyAsync` / party checks; non-party → **404** (no leak). Accept/Decline/Counter require `toParticipantId`. Tests: `GetNegotiation_NonParty_Returns404`, `PlaceOffer_NonParty_Returns404`, `CloseNegotiation_NonParty_Returns404`, `AcceptOffer_NotRecipient_Returns404` (+ decline/counter). |
| 3 | **Strictly 1:1** | **MET** | `Negotiation.Create` requires distinct PartyA/PartyB + one ArtifactId; same-party → errors. Tests: `CreateNegotiation_SamePartyAAndB_Returns400`. No multi-party routes. |
| 4 | **Complementary intents** | **MET** | `IntentComplement.AreComplementary` at create; non-complementary → **400**. Tests: complementary pairs Theory succeed; `CreateNegotiation_NonComplementaryIntents_Returns400`. |
| 5 | **Offer state-machine** | **MET** | Place/Accept/Decline/Counter with status gates; one-open-per-side (`HasOpenOfferFrom` → 400); Counter supersedes prior; illegal → **409**. Tests: place one-open, counter supersede, illegal accept/decline, `AcceptOffer_AlreadyAccepted_Returns409`. |
| 6 | **Close cancels opens** | **MET** | Close cancels all open Offers; post-Close mutations → **409**. Tests: `CloseNegotiation_CancelsAllOpenOffers`, `CloseNegotiation_PostClose_MutationsReturn409`. |
| 7 | **D10 expiration** | **MET** | `CheckAndApplyExpiration` on mutate; Expired + cancel opens; Accept/Counter/place/decline → **409**; GET still OK for party. Tests: `Expiration_ExpiredNegotiation_MutationsReturn409_GETReturnsOK`, `Expiration_OfferMutationsOnExpiredNegotiation_Return409`. |
| 8 | **No contact/PII on Accept** | **MET** | `OfferResponse` has amount/currency/terms only — no contact fields; Accept cancels other opens, state-only. Test asserts no contact/email/phone in Accept response Terms. #7 backlog not implemented. |
| 9 | **Secrets / host / OUT** | **MET** | Reuses #5 Authorization header path; SQLite + IdentityModel JWT only; no Cognito/SSO/settlement/MM packages in csproj. README OUT list matches issue. |
| 10 | **Evidence / handshake** | **MET** | This done-list cites paths/tests. **Dev Code QA / Product QA must not PASS until Security QA confirms.** |

## Soft notes (non-blocking)

- Free-form `terms` string could theoretically carry PII typed by a client; Spec/PoC accepts thin terms with doc/test hygiene (no contact fields in schema). Soft only.
- No live `dotnet test` on this box — evidence from static `gh` review + PR-claimed 91 tests.

## Gaps for Senior Developer

**None.**

## Done-list (for Security QA)

- [x] Path/name DOC-FLOW: `verification/2026-09-20__security__verification__poc-negotiation-sd-points-review.md`
- [x] All 10 checklist points scored with file/test evidence from PR #15
- [x] Kept separate from #4/#5 inventing; #7–#8 not opened
- [x] Security QA: confirm **PASS** (`verification/2026-09-20__security__verification__poc-negotiation-sd-qa-confirm.md`) — Code QA unlock; DOC-FLOW closed

## Cost/critical

None. No Cognito/IdP spend. No escalate.
