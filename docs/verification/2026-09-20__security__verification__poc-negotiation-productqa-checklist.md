# Security checklist — Product QA · PoC 1:1 Negotiation + Offers (#6)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #6 · D7–D9, P4 + D10  
**Issue:** https://github.com/ioaikh/dealoware/issues/6  
**PR:** https://github.com/ioaikh/dealoware/pull/15 (MERGED)  
**SD Security PASS:** `verification/2026-09-20__security__verification__poc-negotiation-sd-qa-confirm.md`  
**Hold:** #7–#8 backlog  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-negotiation-productqa-checklist.md`

## Scope note
Product QA must evidence that SD Security constraints hold on **main**. Real points — not N/A. PoC $0. No Cognito/SSO. No inventing. Soft gap no-live-dotnet OK if stated with equivalent evidence (same pattern as #4/#5).

## Itemized security points (Product QA must evidence)

1. **Authn fail-closed** — Negotiation/Offer APIs reject unauthenticated callers (401/403).
2. **Party-only authz** — Non-party cannot view/mutate; 404 preferred (or 403) without cross-negotiation leak.
3. **Strictly 1:1** — No third Participant / second Artifact on one Negotiation.
4. **Complementary intents** — Non-complementary start rejected per Spec.
5. **Offer state-machine** — Illegal place/Accept/Decline/Counter transitions fail closed; one-open-per-side / exclusivity honored if Spec locked.
6. **Close cancels opens** — Close cancels open offers; post-Close Accept/Counter rejected.
7. **D10 expiration** — Spec-named writes fail after expiry.
8. **No contact/PII on Accept** — Accept is state-only; no contact exchange / identity reveal.
9. **Secrets / host / OUT** — No Cognito/SSO/settlement/MM; local/$0; no committed secrets; header auth hygiene.
10. **Handshake close** — Product QA must not PASS until Security QA confirms these points.

## Handshake next
Product QA weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
