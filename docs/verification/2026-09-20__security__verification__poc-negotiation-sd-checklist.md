# Security checklist — SD · PoC 1:1 Negotiation + Offers (#6)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #6 · D7–D9, P4 + D10  
**Issue:** https://github.com/ioaikh/dealoware/issues/6  
**Plan:** `plans/2026-09-20__devplan__plan__poc-negotiation-offers-d7-d10.md`  
**Dev Plan Security PASS:** `verification/2026-09-20__security__verification__poc-negotiation-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-20__security__verification__poc-negotiation-spec-qa-confirm.md`  
**Hold:** #7–#8 backlog  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-negotiation-sd-checklist.md`

## Scope note
SD must implement Spec + Dev Plan Security gates in code. Real points — not N/A. PoC $0. No Cognito/SSO. No inventing. Consume #4 Artifact + #5 auth; do not re-open those Stories.

## Itemized security points (SD must satisfy)

1. **Authn fail-closed** — Negotiation/Offer APIs require validated #5 principal; unauthenticated → 401/403.
2. **Party-only authz** — Non-party cannot view/mutate; 404 preferred (or 403) without leaking other Negotiations.
3. **Strictly 1:1** — Reject create/join that adds a third Participant or second Artifact.
4. **Complementary intents** — Start Negotiation only when Artifact intents complementary (per Spec); else reject.
5. **Offer state-machine** — Legal place/Accept/Decline/Counter only; illegal transitions fail closed; honor one-open-per-side / exclusivity if Spec locked.
6. **Close cancels opens** — Close cancels all open offers; post-Close Accept/Counter on cancelled offers rejected.
7. **D10 expiration** — After expiry, Spec-named writes (Accept/Counter etc.) fail; no stale-client bypass.
8. **No contact/PII on Accept** — Accept changes state only; no contact exchange / identity reveal (#7 backlog).
9. **Secrets / host / OUT** — Authorization header hygiene; no committed secrets; local/$0; no Cognito/SSO/settlement/MM modules.
10. **Evidence** — Done-list cites paths/tests for 1–9; Code/Product QA must not PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
