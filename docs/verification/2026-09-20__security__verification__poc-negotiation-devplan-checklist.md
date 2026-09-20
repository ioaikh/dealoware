# Security checklist — Dev Plan · PoC 1:1 Negotiation + Offers (#6)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #6 · D7–D9, P4 + D10  
**Issue:** https://github.com/ioaikh/dealoware/issues/6  
**Spec:** `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md`  
**Spec Security PASS:** `verification/2026-09-20__security__verification__poc-negotiation-spec-qa-confirm.md`  
**Hold:** #7–#8 backlog  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-negotiation-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate** Spec Security constraints for SD. Real points — not N/A. PoC $0. No Cognito/SSO. No inventing. Keep separate from #4/#5 plans except consume of Artifact + auth.

## Itemized security points (Dev Plan must weave)

1. **Authn on all Negotiation/Offer APIs** — Plan tasks require #5 principal on create/get/list (as Spec), place/accept/decline/counter/close; verify unauthenticated fail closed.
2. **Party-only authz verify** — Plan includes verify steps: non-party cannot view/mutate; 404 preferred (or 403) without cross-negotiation leak.
3. **1:1 enforcement tasks** — Plan schedules validation rejecting third Participant or second Artifact on one Negotiation.
4. **Complementary-intent gate** — Plan includes start-Negotiation check for complementary intents (per Spec); reject otherwise.
5. **Offer state-machine tasks** — Plan sequences place/Accept/Decline/Counter with illegal-transition fail-closed tests (incl. one-open-per-side / exclusivity if Spec locked).
6. **Close cancels opens** — Plan requires Close to cancel all open offers + verify post-Close Accept/Counter rejected.
7. **D10 expiration tasks** — Plan includes thin expiry + verify Accept/Counter (Spec-named writes) fail after expiry.
8. **No contact/PII on Accept** — Plan explicitly excludes contact exchange / identity reveal tasks (#7 backlog); Accept = state change only.
9. **Host / secrets / OUT** — Local/$0; reuse #5 header/secret hygiene; no Cognito/SSO/settlement/MM modules in plan tasks.
10. **Handshake close** — Dev Plan QA must not PASS until Security QA confirms these points.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
