# Security checklist — Dev Plan · MVP Stage A #32 Account list fail-closed

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #31 — keep separate plan; cross-ref only  
**Spec:** `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md`  
**Spec Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md`  
**Hold:** Stage B/C HOLD. Gate #24 not opened. **#7** stub unchanged. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate** Spec Security for Stage A API/DB list+get fail-closed on Negotiations / Offers / Artifacts. Real points — not N/A. Complements #31 Field ACL; does not invent Strategy ACL or agent hard wall.

## Itemized security points (Dev Plan must weave)

1. **Authn fail-closed tasks** — Plan requires #5 principal on Neg/Offer/Artifact list+get; verify unauthenticated → 401/403 with no private leak.

2. **Negotiation list/get isolation tasks** — Plan schedules owner/party filters + verify unauthorized fail-closed (404 preferred) without cross-account leak.

3. **Offer list/get isolation tasks** — Same for offers (party via parent negotiation).

4. **Artifact list/get isolation tasks** — Same for artifacts (owner-scoped; align #4).

5. **Deny-body / empty-list hygiene** — Plan includes verify: error bodies and empty lists do not leak other Participants’ private fields.

6. **Complement #31 Field ACL** — Plan cross-refs #31 for field projection; does not replace party/owner list rules with Field ACL alone.

7. **No Stage B/C inventing** — No Strategy list ACL, agent hard-wall, Cognito, or MM/DC4 tasks.

8. **Cross-story non-merge** — Keep separate from #31 plan and PoC Stories except consume patterns.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision tasks.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
