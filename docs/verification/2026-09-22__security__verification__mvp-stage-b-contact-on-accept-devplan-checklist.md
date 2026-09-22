# Security checklist — Dev Plan · MVP Stage B #42 Contact on accept (P7 / A9)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/42 · Contact on accept (P7 / A9 minimum)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #41 — keep separate plan; cross-ref only  
**Precursor:** PoC **#7** CLOSED — extend, do not rewrite  
**Spec:** `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md`  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-qa-confirm.md` (SoR PR #44)  
**Hold:** Gate **#25** backlog. Stage C + #26–#27 + #18 Spec/SD HOLD. Mature vault → V3. SD HOLD until Dev Plan QA + Security PASS. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate** Spec Security for identity-until-accept + ContactEmail ShareOutbound only after Accept (HasAcceptGrant). Real points — not N/A. LoginEmail never on Accept.

## Itemized security points (Dev Plan must weave)

1. **Pre-Accept seal tasks** — Plan schedules omit counterparty contact PII until Accept; extend #7; no regression tests.

2. **Accept-grant tasks** — Plan schedules HasAcceptGrant (or equivalent) in resourceContext on User Accept for that offer/negotiation.

3. **ShareOutbound-after-Accept tasks** — Plan schedules ContactEmail ShareOutbound only with Accept grant; before Accept → Deny all; verify.

4. **ContactEmail policy-row tasks** — Plan schedules Stage B rows: User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny.

5. **LoginEmail never-on-Accept tasks** — Plan binds LoginEmail User-only; never shared on Accept; verify distinct from ContactEmail.

6. **Authn / stranger fail-closed tasks** — Unauth deny; stranger deny post-Accept; uniform deny bodies; verify.

7. **Extend #7 under ACL** — Plan extends seal→contact; does not rewrite #7 history or invent #18 Spec/SD.

8. **OUT locked** — P7/A9 minimum; mature vault → V3; gate #25 backlog; no Cognito/MM inventing.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision tasks.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
