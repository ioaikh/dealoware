# Security checklist — SD · MVP Stage A #31 Field ACL registry + API projection

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/31 · Field ACL registry + API projection (FieldClass / IFieldPolicy)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #32 — keep separate SD; cross-ref only  
**Plan:** `plans/2026-09-21__devplan__plan__mvp-stage-a-field-acl-registry.md`  
**Dev Plan Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-qa-confirm.md`  
**Hold:** Stage B/C HOLD (Strategy ACL; ContactEmail share-after-Accept; agent/tool hard wall). Gate #24 not opened. **#7** stub unchanged. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-checklist.md`

## Scope note
SD must implement Spec + Dev Plan Security for Stage A Field ACL on **API/DB projection** only. FieldPolicy = generic any account info (open-ended FieldClass). Real points — not N/A. Do not invent Stage B/C delivery.

## Itemized security points (SD must satisfy)

1. **API/DB scope only** — FieldClass registry + `IFieldPolicy` enforced on API/DB projection paths; agent hard-wall **impl** not delivered (Stage C HOLD).

2. **Open-ended FieldClass registry** — Extensible for any account-info class; LoginEmail/ContactEmail examples, not exhaustive closed set.

3. **Deny-by-default** — Unknown/unregistered FieldClass → deny projection; verified by tests.

4. **LoginEmail User-only (API)** — Not projected to counterparty, stranger, or OwnAgent via API/DB.

5. **ContactEmail rules (no share path)** — OwnAgent Read / counterparty Deny on API; ShareOutbound Deny until Stage B; PoC **#7** stub unchanged.

6. **Authn fail-closed on projection** — Protected projection requires validated #5 principal; 401/403; no private fields in error bodies.

7. **No Stage B/C inventing** — No Strategy ACL, agent hard-wall, Cognito/SSO, or MM/DC4 in this SD.

8. **Cross-story non-merge** — Does not rewrite #32 or PoC Stories except consume/cross-ref.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
