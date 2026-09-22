# Security checklist — Product QA · MVP Stage B #42 Contact on accept (P7 / A9)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/42 · Contact on accept (P7 / A9 minimum)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 separate; #41 HOLD (no Product QA checklist yet)  
**Precursor:** PoC **#7** CLOSED — extend, do not rewrite  
**PR:** https://github.com/ioaikh/dealoware/pull/52 MERGED @ `ac5bc13…` (CQ cq:no-refactor)  
**SD Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-qa-confirm.md` (SoR PR #55)  
**SD checklist (ref):** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-checklist.md` (SoR PR #49)  
**Hold:** Gate **#25** backlog. Stage C + #26–#27 + #18 Spec/SD (whole) HOLD. Mature vault → V3. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-productqa-checklist.md`

## Scope note
Product QA must evidence that SD Security constraints for identity-until-accept + ContactEmail ShareOutbound only after Accept hold on delivered PR #52. Soft gap no-live-dotnet OK if stated with equivalent evidence (CI + StageBContactOnAcceptTests / IdentitySealTests). Real points — not N/A. LoginEmail never on Accept.

## Itemized security points (Product QA must evidence)

1. **Pre-Accept seal** — Negotiation/Offer APIs omit counterparty contact PII until Accept; #7 extended (no regression).

2. **Accept-grant** — User Accept persists grant; `HasAcceptGrant` (or equivalent) available to FieldPolicy for that offer/negotiation.

3. **ShareOutbound-after-Accept** — ContactEmail ShareOutbound only with Accept grant to authorized counterparty; before Accept → Deny all.

4. **ContactEmail policy rows** — User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny.

5. **LoginEmail never-on-Accept** — LoginEmail remains User-only; never shared on Accept; distinct from ContactEmail.

6. **Authn / stranger fail-closed** — Unauth deny; stranger deny ContactEmail post-Accept; uniform deny; no private leakage.

7. **Extend #7 under ACL** — Seal→contact extended; #7 history not rewritten; #18 Spec/SD not unlocked.

8. **OUT locked** — P7/A9 minimum; mature vault → V3; gate #25 backlog; no Cognito/MM inventing; no Stage C share-tool observed.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision observed.

10. **Handshake close** — Product QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Product QA weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
