# Security checklist — Dev Plan · MVP Stage A #31 Field ACL registry + API projection

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/31 · Field ACL registry + API projection (FieldClass / IFieldPolicy)  
**Parent:** #18 · Option A · Stage A  
**Sibling:** #32 — keep separate plan; cross-ref only  
**Spec:** `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md`  
**Spec Security PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-qa-confirm.md`  
**Hold:** Stage B/C HOLD (Strategy ACL; ContactEmail share-after-Accept; agent/tool hard wall). Gate #24 not opened. **#7** stub unchanged. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate** Spec Security for Stage A Field ACL on **API/DB projection** only. FieldPolicy = generic any account info (open-ended FieldClass). Real points — not N/A. Do not invent Stage B/C delivery.

## Itemized security points (Dev Plan must weave)

1. **API/DB scope tasks** — Plan schedules FieldClass registry + `IFieldPolicy` on API/DB projection only; agent hard-wall **impl** remains Stage C HOLD.

2. **Open-ended FieldClass registry tasks** — Plan requires extensible registry (not email-only); LoginEmail/ContactEmail as examples.

3. **Deny-by-default tasks** — Plan includes unknown/unregistered FieldClass → deny projection; verify tests.

4. **LoginEmail User-only (API) tasks** — Plan schedules projection: User-only; OwnAgent Deny on API/DB; verify.

5. **ContactEmail rules (no share) tasks** — Plan schedules OwnAgent Read / counterparty Deny; ShareOutbound Deny until Stage B; do not rewrite #7 stub.

6. **Authn fail-closed on projection** — Plan requires principal on protected projection paths; 401/403; no private fields in errors.

7. **No Stage B/C inventing** — No Strategy ACL, agent hard-wall, Cognito, or MM/DC4 tasks.

8. **Cross-story non-merge** — Keep separate from #32 plan and PoC Stories except cross-refs / consume patterns.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision tasks.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
