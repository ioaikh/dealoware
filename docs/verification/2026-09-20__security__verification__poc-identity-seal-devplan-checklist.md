# Security checklist — Dev Plan · PoC Identity-seal stub (#7)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #7 · Identity-seal stub (no contact exchange)  
**Issue:** https://github.com/ioaikh/dealoware/issues/7  
**Spec:** `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md`  
**Spec Security PASS:** `verification/2026-09-20__security__verification__poc-identity-seal-spec-qa-confirm.md`  
**Hold:** #8 backlog; real contact-on-accept = MVP **P7**/**A9** — do not invent; do **not** pull [#18](https://github.com/ioaikh/dealoware/issues/18) into PoC  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-identity-seal-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate** Spec Security constraints for SD on the identity-seal **stub** only (DTO omit contact/PII; opaque ids; Accept = state-only). Real points — not N/A. PoC $0. No Cognito/SSO. No Strategy/AI. No mature PII vault. Extend #5/#6 only — do not rewrite those plans.

## Itemized security points (Dev Plan must weave)

1. **Authn fail-closed tasks** — Plan requires #5 principal on all #7-touched Negotiation/Offer reads/writes; include verify unauthenticated → 401/403 (no anonymous identity/contact surfaces).

2. **Party-only authz verify (narrow)** — Plan includes verify: non-party cannot view/mutate that Negotiation/Offer; 404 preferred without contact/identity/cross-Negotiation leak. **Do not** schedule full #18 tenancy/Strategy isolation work.

3. **DTO omit contact/PII tasks** — Plan schedules response/DTO work so Negotiation/Offer (and public Participant views on those paths) expose **opaque ids only** — no email/phone/name-as-contact/address fields.

4. **Accept path = state-only** — Plan tasks for Accept/Decline/Counter/Close (and any seal flag) must **not** include contact release or identity reveal payloads.

5. **No contact-exchange surface** — Plan explicitly excludes any PoC endpoint or Accept response that returns counterparty contact; real release deferred to MVP **P7**/**A9**.

6. **Stub precursor only** — Plan documents stub as precursor to MVP identity-until-accept + contact-on-accept without scheduling vault/KMS or MVP release behavior.

7. **No-leak evidence tasks** — Plan includes automated tests and/or explicit verification notes proving happy-path create/get/place/Accept/Decline/Counter/Close flows do **not** expose counterparty contact PII.

8. **No inventing OUT** — Plan cites #7 AC+OUT only: no Strategy/AI, mature vault, settlement, Cognito/SSO/IdP, MM/DC4; **#8** stays backlog; **#18** stays out of PoC.

9. **Host / secrets / no spend** — Local/$0; reuse #5 Authorization-header hygiene; ECS Express sketch only; **no** Cognito/SSO/IdP provision tasks.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
