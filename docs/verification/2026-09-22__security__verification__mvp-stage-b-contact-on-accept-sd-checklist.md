# Security checklist — SD · MVP Stage B #42 Contact on accept (P7 / A9)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/42 · Contact on accept (P7 / A9 minimum)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #41 — keep separate SD; cross-ref only  
**Precursor:** PoC **#7** CLOSED — extend, do not rewrite  
**Plan:** `plans/2026-09-22__devplan__plan__mvp-stage-b-contact-on-accept.md`  
**Dev Plan Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-devplan-qa-confirm.md` (SoR PR #47)  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-qa-confirm.md` (SoR PR #44)  
**Hold:** Gate **#25** backlog. Stage C + #26–#27 + #18 Spec/SD (whole) HOLD. Mature vault → V3. Product QA HOLD until SD-step Security PASS. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-checklist.md`

## Scope note
SD must **implement** Spec + Dev Plan Security for identity-until-accept + ContactEmail ShareOutbound only after Accept (`HasAcceptGrant`). Real points — not N/A. LoginEmail never on Accept. Evidence via tests/done-list.

## Itemized security points (SD must satisfy)

1. **Pre-Accept seal** — Negotiation/Offer APIs omit counterparty contact PII until Accept; extend #7; no regression (verified).

2. **Accept-grant** — User Accept persists grant; `resourceContext.HasAcceptGrant` (or equivalent) for FieldPolicy on that offer/negotiation.

3. **ShareOutbound-after-Accept** — ContactEmail ShareOutbound only with Accept grant to authorized counterparty; before Accept → Deny all (verified).

4. **ContactEmail policy rows** — User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny (verified).

5. **LoginEmail never-on-Accept** — LoginEmail remains User-only; never shared on Accept; distinct from ContactEmail (verified).

6. **Authn / stranger fail-closed** — Unauth deny; stranger deny ContactEmail post-Accept; uniform deny bodies; no private leakage.

7. **Extend #7 under ACL** — Extend seal→contact; do **not** rewrite #7 history or invent #18 Spec/SD unlock.

8. **OUT locked** — P7/A9 minimum; mature vault → V3; gate #25 backlog; no Cognito/MM inventing; no Stage C share-tool.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
