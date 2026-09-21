# Security checklist — Doc · MVP Stage A #32 Account list fail-closed

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #31 — keep separate Doc track; cross-ref only  
**PR:** https://github.com/ioaikh/dealoware/pull/34 (MERGED)  
**Product QA Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-qa-confirm.md`  
**SD Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-qa-confirm.md`  
**Hold:** Stage B/C HOLD. Gate #24 not opened. **#7** stub unchanged. **#31** SD Security LIVE (separate). PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-checklist.md`

## Scope note
Doc must accurately describe Stage A API/DB list+get fail-closed on Negotiations / Offers / Artifacts (owner/party query-plane isolation) and must **not** invent Strategy ACL, agent hard-wall, Cognito/SSO, Field ACL (#31) delivery, or MM/DC4 as delivered by this Story. Real points — not N/A. PoC $0. Soft no-live-dotnet OK if stated with CI/tests cites.

## Itemized security points (Doc must satisfy)

1. **Authn fail-closed** — Docs state Neg/Offer/Artifact list+get require #5 principal; unauthenticated → 401/403; no private-field leakage on deny.

2. **Negotiation list/get isolation** — Docs state only authorized (owner/party) records; unauthorized → fail-closed (404 preferred) without cross-account leak.

3. **Offer list/get isolation** — Docs state party via parent negotiation; same fail-closed / no cross-account leak (incl. stranger deny on mutations if documented).

4. **Artifact list/get isolation** — Docs state owner-scoped (`OwnerParticipantId` == sub); IDOR fail-closed; align #4.

5. **Deny-body / empty-list hygiene** — Docs state error bodies and empty lists do not leak other Participants’ private fields; stranger list → `[]` not foreign rows.

6. **Complement #31, don’t replace party rules** — Docs state list isolation remains owner/party query-plane; Field ACL (#31) is complement / separate Story — not delivered as this Story’s Field ACL registry.

7. **No Stage B/C inventing** — Docs do not invent Strategy list ACL, agent hard-wall, Cognito, or MM/DC4 as delivered.

8. **Cross-story non-merge** — Docs do not rewrite #31 or PoC Stories except harden/consume cross-refs to #4–#7.

9. **Cost / spend** — Docs state PoC **$0**; no IdP/vault provision as delivered.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
