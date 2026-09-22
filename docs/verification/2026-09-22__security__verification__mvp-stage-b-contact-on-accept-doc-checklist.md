# Security checklist — Doc · MVP Stage B #42 Contact on accept (P7 / A9)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/42 · Contact on accept (P7 / A9 minimum)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #41 — keep separate Doc; cross-ref only  
**Precursor:** PoC **#7** CLOSED — extend, do not rewrite  
**PR:** https://github.com/ioaikh/dealoware/pull/52 MERGED @ `ac5bc136…`  
**Product QA Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-productqa-qa-confirm.md`  
**SD Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-qa-confirm.md`  
**Hold:** Gate **#25** backlog. Stage C + #18 Spec/SD (whole) HOLD. Mature vault → V3. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-doc-checklist.md`

## Scope note
Doc must accurately describe identity-until-accept + ContactEmail ShareOutbound only after Accept (`HasAcceptGrant`) and must **not** invent LoginEmail-on-Accept, mature vault, Stage C share-tool, Cognito, or MM/DC4 as delivered. Real points — not N/A. Extend #7; do not rewrite #7 history.

## Itemized security points (Doc must satisfy)

1. **Pre-Accept seal** — Docs state Neg/Offer omit counterparty contact PII until Accept; #7 extended (no regression).

2. **Accept-grant** — Docs state Accept persists grant; `HasAcceptGrant` (or equivalent) for FieldPolicy.

3. **ShareOutbound-after-Accept** — Docs state ContactEmail ShareOutbound only with Accept grant to authorized counterparty; Deny before Accept.

4. **ContactEmail policy rows** — Docs state User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny.

5. **LoginEmail never-on-Accept** — Docs state LoginEmail User-only; never shared on Accept; distinct from ContactEmail.

6. **Authn / stranger fail-closed** — Docs state unauth deny; stranger deny ContactEmail post-Accept; uniform deny; no private leakage.

7. **Extend #7 under ACL** — Docs state seal→contact extended; #7 history not rewritten; #18 Spec/SD not unlocked.

8. **OUT locked** — Docs state P7/A9 minimum; vault → V3; gate #25 backlog; no Cognito/MM inventing; no Stage C share-tool as delivered.

9. **Cost / spend** — Docs state PoC **$0**; no IdP/vault provision as delivered.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
