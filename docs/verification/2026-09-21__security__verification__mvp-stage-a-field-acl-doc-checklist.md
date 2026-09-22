# Security checklist — Doc · MVP Stage A #31 Field ACL registry + API projection

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/31 · Field ACL registry + API projection (FieldClass / IFieldPolicy)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #32 — CLOSED done (separate); cross-ref only  
**PR:** https://github.com/ioaikh/dealoware/pull/37 (MERGED · main `fb47fdd3…`)  
**Product QA Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-qa-confirm.md`  
**SD Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-qa-confirm.md`  
**Hold:** Stage B/C HOLD (Strategy ACL; ContactEmail share-after-Accept; agent/tool hard wall). Gate #24 not opened. **#7** stub unchanged. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-checklist.md`

## Scope note
Doc must accurately describe Stage A Field ACL on **API/DB projection** only (open-ended FieldClass registry + `IFieldPolicy`; deny-by-default; LoginEmail User-only; ContactEmail no ShareOutbound) and must **not** invent Strategy ACL, agent hard-wall, Cognito/SSO, contact-on-Accept, or MM/DC4 as delivered. Real points — not N/A. PoC $0. Soft no-live-dotnet OK if stated with CI/tests cites. Keep #32 Doc Security separate except cross-refs.

## Itemized security points (Doc must satisfy)

1. **API/DB scope only** — Docs state FieldClass registry + `IFieldPolicy` on API/DB projection paths; agent hard-wall **impl** not delivered (Stage C HOLD).

2. **Open-ended FieldClass registry** — Docs state extensible FieldClass for any account-info; LoginEmail/ContactEmail/DisplayName (if documented) are starters/examples, not an exhaustive closed set.

3. **Deny-by-default** — Docs state unknown/unregistered FieldClass → deny projection; cite tests/evidence.

4. **LoginEmail User-only (API)** — Docs state LoginEmail is not projected to counterparty, stranger, or OwnAgent via API/DB.

5. **ContactEmail rules (no share path)** — Docs state OwnAgent Read / counterparty Deny on API; ShareOutbound Deny until Stage B; PoC **#7** stub unchanged (Accept = state-only).

6. **Authn fail-closed on projection** — Docs state protected projection requires #5 principal; 401/403; no private fields in error bodies.

7. **No Stage B/C inventing** — Docs do not invent Strategy ACL, agent hard-wall, Cognito/SSO, or MM/DC4 as delivered.

8. **Cross-story non-merge** — Docs do not rewrite #32 or PoC Stories except consume/cross-ref; #32 remains separate CLOSED track.

9. **Cost / spend** — Docs state PoC **$0**; no IdP/vault provision as delivered.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
