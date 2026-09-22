# Security checklist — Spec · MVP Stage A #31 Field ACL registry + API projection

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/31 · Field ACL registry + API projection (FieldClass / IFieldPolicy)  
**Parent:** #18 · Option A (CEO ACCEPTED) · Stage A  
**Sibling:** #32 (account list fail-closed) — keep separate Spec; cross-ref only  
**Architecture:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` · Stage A SA checklist `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md`  
**Hold:** Stage B/C (Strategy ACL; ContactEmail share-after-Accept; agent/tool hard wall) HOLD. Gate #24 not opened by Spec alone. **#7** identity-seal stub unchanged. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-checklist.md`

## Scope note
Spec-binding for Stage A Field ACL on **API/DB projection** only. FieldPolicy is **generic any account info** (open-ended FieldClass). Real points — not N/A. Do not invent Stage B/C delivery. No MotorMarket. No Cognito inventing.

## Itemized security points (Spec must bind)

1. **Stage A API/DB scope** — Spec binds #31 to FieldClass registry + `IFieldPolicy` on API/DB projection paths only; agent/tool hard-wall **implementation** remains Stage C HOLD (dual wall remains accepted end-state).

2. **Open-ended FieldClass** — Spec requires an extensible registry for **any** account-info class; LoginEmail / ContactEmail are examples, not an exhaustive closed set.

3. **Deny-by-default policy** — Unknown/unregistered FieldClass → deny projection; no silent allow.

4. **LoginEmail User-only (API)** — Spec binds LoginEmail example as User-only on API/DB: not projected to counterparty, stranger, or OwnAgent via API.

5. **ContactEmail rules (API, no share path)** — Spec binds ContactEmail API projection consistent with #18 OwnAgent-read / counterparty-deny **without** delivering share-after-Accept (Stage B HOLD) and without rewriting PoC **#7** stub.

6. **Fail-closed authn** — Protected projection paths require validated principal; unauthenticated → 401/403 with no private-field leakage in errors.

7. **No Stage B/C inventing** — Spec cites #31 AC + OUT only: no Strategy ACL, no agent hard-wall impl, no Cognito/SSO inventing, no MM/DC4.

8. **Cross-story non-merge** — Spec does not rewrite #32, #7, or PoC #3–#8 AC except cross-refs; Field ACL does not replace negotiation party-only rules.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision required to accept this Spec.

10. **Traceability + handshake** — Spec cites #31 AC + Option A / #18 only. Spec QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Spec weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
