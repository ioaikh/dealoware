# Security checklist — Spec · PoC 1:1 Negotiation + Offers (#6)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #6 · D7–D9, P4 + D10 thin expiration  
**Issue:** https://github.com/ioaikh/dealoware/issues/6  
**Depends on:** #3 O10, #4 Artifact, #5 auth (all Security PASS)  
**Hold:** #7–#8 backlog — do not invent identity-seal contact exchange or later Stories  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-negotiation-spec-checklist.md`

## Scope note
Spec-binding checklist for 1:1 Negotiation + Offer lifecycle. **Do not invent AC** beyond issue #6. Real points — not N/A. PoC local/$0. No Cognito/SSO. No MotorMarket. No settlement.

## Itemized security points (Spec must bind)

1. **Authn fail-closed** — All Negotiation/Offer mutating and read APIs that expose another party’s data require validated #5 principal (API key/JWT). Unauthenticated → 401/403. No anonymous negotiate/offer.

2. **Authorization — party-only** — Spec binds who may start, view, offer, accept/decline/counter, and close: only the two Participants in that 1:1 Negotiation (plus rules for who may initiate against which Artifact). Non-party → 404 preferred or 403 without leaking other Negotiations’ contents.

3. **Strictly 1:1** — Spec forbids multi-party / multi-Artifact Negotiations in PoC. Reject create/join that would add a third Participant or second Artifact to one Negotiation.

4. **Complementary intents** — Spec requires complementary Artifact intents for starting a Negotiation (per AC); reject non-complementary pairs without inventing new intent product rules beyond Story/Product sources.

5. **Offer state-machine integrity** — Spec locks legal transitions for place / Accept / Decline / Counter (and any single-open-offer or exclusivity rule Spec chooses from SA guidance). Unauthorized or illegal transitions must fail closed (no silent overwrite of another party’s open offer).

6. **Close cancels open offers** — Spec requires Close to cancel all open offers for that Negotiation (atomic or equivalent consistency). Closed Negotiation must reject further Accept/Counter on cancelled offers.

7. **Expiration (D10 thin)** — Spec defines when a Negotiation is expired and that Accept/Counter (and other Spec-named writes) fail after expiry. No bypass via stale client state.

8. **No contact / PII on Accept** — Spec must **not** deliver contact exchange or identity reveal on Accept (#7 backlog / MVP P7·A9). PoC Accept changes offer/negotiation state only — no new PII surfaces.

9. **Secrets / host / no spend** — Reuse #5 secret/header hygiene (Authorization header; no tokens in query/body; no committed secrets). Local/$0; ECS Express Mode sketch only; **no** Cognito/SSO/IdP provision; no settlement/checkout modules; zero MM/DC4.

10. **Traceability + handshake** — Spec cites issue #6 AC + OUT only (no invented Stories). Maps these Security points for Dev Plan/SD. Spec QA must not PASS until Security QA confirms.

## Handshake next
1. Senior Spec weaves/answers points in Spec (cite sections).
2. Senior Security reviews → done-list to Security QA.
3. Security QA PASS to Chief Security (or further instructions).
4. Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS / IdP spend. Cost/critical → COO → CEO.
