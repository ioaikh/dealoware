# Security checklist — Spec · MVP Stage A #32 Account list fail-closed

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-21  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/32 · Account list fail-closed (negotiations / offers / artifacts)  
**Parent:** #18 · Option A (CEO ACCEPTED) · Stage A  
**Sibling:** #31 (Field ACL registry + API projection) — keep separate Spec; cross-ref only  
**Architecture:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` · Stage A SA checklist `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md`  
**Hold:** Stage B/C HOLD. Gate #24 not opened by Spec alone. **#7** stub unchanged. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-checklist.md`

## Scope note
Spec-binding for Stage A **API/DB IDOR defense**: negotiation / offer / artifact list+get fail-closed to authorized account. Real points — not N/A. Complements #31 Field ACL; does not invent Strategy ACL or agent hard wall. No MotorMarket.

## Itemized security points (Spec must bind)

1. **Fail-closed authn** — List/get paths for Negotiations / Offers / Artifacts require validated principal; unauthenticated → 401/403 with no private-field leakage.

2. **Negotiation list/get isolation** — Spec binds list/get to return only records the principal is authorized to see (owner/party); unauthorized → fail-closed (404 preferred or 403) without leaking other accounts’ negotiations.

3. **Offer list/get isolation** — Same fail-closed / no cross-account offer leak.

4. **Artifact list/get isolation** — Same fail-closed / owner-scoped (align #4); no cross-tenant Artifact leak.

5. **No private-field leakage in denies** — Error bodies and empty lists must not reveal other Participants’ private fields or existence beyond product-allowed signals.

6. **Complement #31, don’t replace party rules** — Spec cross-refs Field ACL for field projection; list isolation does not weaken 1:1 negotiation party-only rules from #6.

7. **No Stage B/C inventing** — No Strategy list ACL inventing; no agent/tool hard-wall; no Cognito inventing; no MM/DC4.

8. **Cross-story non-merge** — Spec does not rewrite #31, #7, or PoC Stories except cross-refs.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision required for this Spec.

10. **Traceability + handshake** — Spec cites #32 AC + Option A / #18 only. Spec QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Spec weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
