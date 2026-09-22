# Security checklist — Product QA · MVP Stage A #31 Field ACL registry + API projection

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/31 · Field ACL registry + API projection (FieldClass / IFieldPolicy)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #32 — CLOSED done (separate); cross-ref only  
**PR:** https://github.com/ioaikh/dealoware/pull/37 (CQ cq:no-refactor)  
**SD Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-qa-confirm.md`  
**Hold:** Stage B/C HOLD (Strategy ACL; ContactEmail share-after-Accept; agent/tool hard wall). Gate #24 not opened. **#7** stub unchanged. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-checklist.md`

## Scope note
Product QA must evidence that SD Security constraints for Stage A Field ACL on **API/DB projection** hold on the delivered PR (#37). FieldPolicy = generic any account info (open-ended FieldClass). Real points — not N/A. Do not invent Stage B/C delivery. Soft gap no-live-dotnet OK if stated with equivalent evidence (CI + FieldAclTests), same pattern as #32 Product QA.

## Itemized security points (Product QA must evidence)

1. **API/DB scope only** — FieldClass registry + `IFieldPolicy` enforced on API/DB projection paths; agent hard-wall **impl** not delivered (Stage C HOLD).

2. **Open-ended FieldClass registry** — Extensible for any account-info class; LoginEmail/ContactEmail (and DisplayName if present) are starters/examples, not an exhaustive closed set.

3. **Deny-by-default** — Unknown/unregistered FieldClass → deny projection; evidenced by tests.

4. **LoginEmail User-only (API)** — Not projected to counterparty, stranger, or OwnAgent via API/DB.

5. **ContactEmail rules (no share path)** — OwnAgent Read / counterparty Deny on API; ShareOutbound Deny until Stage B; PoC **#7** stub unchanged (Accept remains state-only).

6. **Authn fail-closed on projection** — Protected projection requires validated #5 principal; 401/403; no private fields in error bodies.

7. **No Stage B/C inventing** — No Strategy ACL, agent hard-wall, Cognito/SSO, or MM/DC4 observed in Product QA scope.

8. **Cross-story non-merge** — Does not rewrite #32 or PoC Stories except consume/cross-ref; #32 remains separate closed track.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision observed.

10. **Handshake close** — Product QA must **not** PASS until Security QA confirms these points.

## Handshake next
Product QA weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
