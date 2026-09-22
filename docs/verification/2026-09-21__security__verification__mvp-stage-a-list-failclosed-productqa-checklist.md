# Security checklist — Product QA · MVP Stage A #32 Account list fail-closed

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #31 — keep separate; not scored here  
**PR:** https://github.com/ioaikh/dealoware/pull/34 (CQ cq:no-refactor)  
**SD Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-qa-confirm.md`  
**Hold:** Stage B/C HOLD. Gate #24 not opened. **#7** stub unchanged. **#31** SD confirm PAUSED. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-checklist.md`

## Scope note
Product QA must evidence that SD Security constraints for Stage A API/DB list+get fail-closed on Negotiations / Offers / Artifacts hold on the delivered PR (#34). Real points — not N/A. Complements #31 Field ACL; does not invent Strategy ACL or agent hard wall. Soft gap no-live-dotnet OK if stated with equivalent evidence (CI + tests), same pattern as prior PoC Product QA.

## Itemized security points (Product QA must evidence)

1. **Authn fail-closed** — Neg/Offer/Artifact list+get reject unauthenticated callers (401/403); no private-field leakage on deny.

2. **Negotiation list/get isolation** — Only authorized (owner/party) records returned; unauthorized → fail-closed (404 preferred) without cross-account leak.

3. **Offer list/get isolation** — Party via parent negotiation; same fail-closed / no cross-account leak (including accept/decline/counter stranger deny if exercised).

4. **Artifact list/get isolation** — Owner-scoped (`OwnerParticipantId` == sub); IDOR fail-closed; align #4.

5. **Deny-body / empty-list hygiene** — Error bodies and empty lists do not leak other Participants’ private fields; stranger list → `[]` not foreign rows.

6. **Complement #31, don’t replace party rules** — List isolation remains owner/party query-plane; do not weaken #6 party-only; do not treat #31 Field ACL as delivered by this Story.

7. **No Stage B/C inventing** — No Strategy list ACL, agent hard-wall, Cognito, or MM/DC4 observed in Product QA scope.

8. **Cross-story non-merge** — Does not rewrite #31 or PoC Stories except harden/consume #4–#7 surfaces.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision observed.

10. **Handshake close** — Product QA must **not** PASS until Security QA confirms these points.

## Handshake next
Product QA weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
