# Security checklist — SD · MVP Stage A #32 Account list fail-closed

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #31 — keep separate SD; cross-ref only  
**Plan:** `plans/2026-09-21__devplan__plan__mvp-stage-a-account-list-fail-closed.md`  
**Dev Plan Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md`  
**Hold:** Stage B/C HOLD. Gate #24 not opened. **#7** stub unchanged. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-checklist.md`

## Scope note
SD must implement Spec + Dev Plan Security for Stage A API/DB list+get fail-closed on Negotiations / Offers / Artifacts. Real points — not N/A. Complements #31 Field ACL; does not invent Strategy ACL or agent hard wall.

## Itemized security points (SD must satisfy)

1. **Authn fail-closed** — Neg/Offer/Artifact list+get require validated #5 principal; unauthenticated → 401/403 with no private-field leakage.

2. **Negotiation list/get isolation** — Only authorized (owner/party) records; unauthorized → fail-closed (404 preferred) without cross-account leak.

3. **Offer list/get isolation** — Party via parent negotiation; same fail-closed / no cross-account leak.

4. **Artifact list/get isolation** — Owner-scoped (`OwnerParticipantId` == sub); IDOR fail-closed; align #4.

5. **Deny-body / empty-list hygiene** — Error bodies and empty lists do not leak other Participants’ private fields.

6. **Complement #31, don’t replace party rules** — Field ACL (when present) projects fields; list isolation remains owner/party query-plane — do not weaken #6 party-only.

7. **No Stage B/C inventing** — No Strategy list ACL, agent hard-wall, Cognito, or MM/DC4.

8. **Cross-story non-merge** — Does not rewrite #31 or PoC Stories except harden/consume.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
