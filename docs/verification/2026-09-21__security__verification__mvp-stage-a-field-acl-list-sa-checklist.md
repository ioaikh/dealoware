# Security checklist — Architecture (SA) · MVP Stage A #31 + #32 (Field ACL + account list fail-closed)

**Status:** Chief Security itemized points for Architecture step (handshake per `ops/ORG-OPS.md`). Issue **before** Architecture QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Stories:** https://github.com/ioaikh/dealoware/issues/31 · https://github.com/ioaikh/dealoware/issues/32  
**Parent:** #18 · Option A (CEO ACCEPTED) · Stage A slice  
**Deliverable:** `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md`  
**Binding proposal:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md`  
**Hold:** Agent/tool hard wall = **Stage C HOLD** (dual wall accepted architecturally; Stage A = **API/DB only**). Strategy ACL / ContactEmail share-after-Accept = Stage B/C HOLD. **#7** identity-seal stub unchanged. Gate **#24** not opened by this checklist alone. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md`

## Scope note
Architecture for Stage A named slice only: (1) open-ended FieldClass registry + API/DB projection (`IFieldPolicy`); (2) account list fail-closed for negotiations / offers / artifacts. Real points — not N/A. Do not invent Stage B/C delivery. No MotorMarket. No Cognito inventing.

## Itemized security points (Architecture must answer)

1. **Stage A scope lock** — Architecture binds Stage A to API/DB FieldPolicy + account list fail-closed only; explicitly **HOLDs** agent/tool hard-wall **implementation** to Stage C (while keeping dual-wall Option A as the accepted end-state).

2. **Open-ended FieldClass registry** — Registry is extensible for **any** account-info class (not email-only); LoginEmail / ContactEmail are **examples**, not an exhaustive closed set. New classes = new FieldClass + policy rows — without inventing product FieldClasses beyond CEO examples unless labeled illustrative.

3. **IFieldPolicy on API/DB projection** — Every Stage A protected field projection path evaluates `IFieldPolicy` (or equivalent) server-side; deny-by-default for unknown/unregistered classes.

4. **LoginEmail User-only (API side)** — Architecture binds LoginEmail (example) as User-only on API/DB: not projected to counterparty/stranger; **not** to OwnAgent via API either. (Agent-tool plane enforcement remains Stage C.)

5. **ContactEmail OwnAgent vs counterparty (API side)** — Architecture binds ContactEmail read rules for API projection consistent with #18 (OwnAgent may read; counterparty deny until Accept) **without** delivering share-after-Accept (Stage B HOLD) or rewriting PoC **#7** stub.

6. **Account list fail-closed — Negotiations** — Negotiation list/get paths return only records the principal is authorized to see (owner/party); unauthorized → fail-closed with **no** private-field leakage.

7. **Account list fail-closed — Offers** — Offer list/get paths same fail-closed / no cross-account leak.

8. **Account list fail-closed — Artifacts** — Artifact list/get paths same fail-closed / owner-scoped (align #4 owner model); no cross-tenant leak.

9. **No inventing / no spend / #7 preserved** — No Strategy ACL inventing in Stage A; no agent hard-wall impl; no Cognito/SSO/IdP inventing; no MM/DC4; PoC **$0**; #7 contact-omit stub remains as-is.

10. **Traceability + handshake** — Architecture cites #31/#32 AC + Option A / #18 proposal only. Maps these Security points for later Spec/SD. Architecture QA must **not** PASS until Security QA confirms. Gate #24 stays backlog until Stage A delivery moment (PM/CA).

## Handshake next
1. Senior Architect answers points in the Stage A architecture doc (cite sections).
2. Senior Security → done-list to Security QA (or Architecture QA asks Security QA with evidence).
3. Security QA PASS to Chief Security (or further instructions).
4. Chief Security PASS/HOLD to CPM + Chief Architect.

## Cost/critical
No AWS / IdP spend. Cost/critical → COO → CEO.
