# Security checklist — Doc · PoC 1:1 Negotiation + Offers (#6)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #6 · D7–D9, P4 + D10  
**Issue:** https://github.com/ioaikh/dealoware/issues/6  
**PR:** https://github.com/ioaikh/dealoware/pull/15 (MERGED)  
**Product QA Security PASS:** `verification/2026-09-20__security__verification__poc-negotiation-productqa-qa-confirm.md`  
**Hold:** #7–#8 backlog  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-negotiation-doc-checklist.md`

## Scope note
Doc must not invent multi-party, Cognito/SSO, contact-on-Accept, or settlement. Real points — not N/A. PoC $0. Soft no-live-dotnet OK if stated. Keep separate from #4/#5 Doc Security except cross-refs.

## Itemized security points (Doc must satisfy)

1. **Authn fail-closed** — Docs state Negotiation/Offer APIs require #5 auth; unauthenticated calls fail.
2. **Party-only** — Docs describe party-only access; non-party gets 404/403 without leaking other Negotiations.
3. **Strictly 1:1** — Docs do not describe multi-party / multi-Artifact Negotiations as PoC delivered.
4. **Complementary intents** — Docs note complementary-intent requirement for start.
5. **Offer lifecycle** — Docs accurately describe place / Accept / Decline / Counter fail-closed rules (incl. one-open-per-side if Spec).
6. **Close cancels opens** — Docs state Close cancels open offers; no further Accept/Counter on cancelled.
7. **D10 expiration** — Docs describe thin expiry and fail-closed writes after expiry.
8. **No contact/PII on Accept** — Docs explicitly defer contact exchange / identity reveal to #7 backlog; Accept = state only.
9. **Secrets / host / OUT** — Placeholders only; Authorization header examples; local/$0; ECS Express sketch; no Cognito/SSO/settlement/MM how-tos as delivered.
10. **Handshake close** — Docs QA must not PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
