# Security checklist — Spec · PoC Identity-seal stub (#7)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #7 · Identity-seal stub (no contact exchange)  
**Issue:** https://github.com/ioaikh/dealoware/issues/7  
**Depends on:** #5 Participant principal + #6 Negotiation/Offer surface (Security PASS)  
**Hold:** #8 backlog; real contact-on-accept = MVP **P7**/**A9** — do not invent; do **not** pull [#18](https://github.com/ioaikh/dealoware/issues/18) account-authz into PoC  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-identity-seal-spec-checklist.md`

## Scope note
Spec-binding checklist for the PoC **identity-seal stub** only: public Negotiation/Offer (and related) DTOs omit counterparty contact/PII; opaque ids; Accept does **not** release contact. **Do not invent AC** beyond issue #7. Real points — not N/A. PoC local/$0. No Cognito/SSO. No MotorMarket. No Strategy/AI. No mature PII vault.

## Itemized security points (Spec must bind)

1. **Authn fail-closed** — Any #7-touched Negotiation/Offer read or write that can expose another party’s data requires a validated #5 principal. Unauthenticated → 401/403. No anonymous identity/contact surfaces.

2. **Authorization — party-only (narrow)** — Only the two Participants in that 1:1 Negotiation may read/write that Negotiation/Offer. Non-party → 404 preferred (or 403) **without** leaking contact, identity fields, or other Negotiations’ contents. Do **not** expand Spec into full #18 tenancy/Strategy isolation.

3. **Public DTOs omit contact/PII** — Spec binds Negotiation/Offer (and public Participant views used on those paths) to **opaque ids only**. No email, phone, real name, address, or other contact/PII fields on PoC Negotiation/Offer API responses.

4. **Accept path does not release contact** — Spec requires Accept / Decline / Counter / Close (and any seal-related flags) to change **state only**. PoC must **not** deliver counterparty contact or identity reveal on Accept.

5. **No contact-exchange surface** — Spec forbids a PoC endpoint or Accept response payload that exchanges or returns counterparty contact. Real release deferred to MVP **P7**/**A9**.

6. **Stub as precursor (documented, not implemented)** — Spec documents the stub as precursor to MVP identity-until-accept + contact-on-accept, without implementing that MVP behavior or inventing vault/KMS requirements.

7. **No-leak evidence** — Spec requires tests and/or explicit verification notes proving happy-path PoC Negotiation/Offer flows do **not** expose counterparty contact PII.

8. **No inventing OUT** — Spec cites #7 AC + OUT only: no Strategy/AI, no mature PII vault, no settlement/checkout, no Cognito/SSO/IdP, zero MotorMarket/DC4; **#8** stays backlog.

9. **Secrets / host / no spend** — Reuse #5 secret/header hygiene (Authorization header; no tokens in query/body; no committed secrets). Local/$0; ECS Express Mode sketch only; **no** Cognito/SSO/IdP provision.

10. **Traceability + handshake** — Spec cites issue #7 AC + OUT only (no invented Stories). Maps these Security points for Dev Plan/SD. Spec QA must **not** PASS until Security QA confirms.

## Handshake next
1. Senior Spec weaves/answers points in Spec (cite sections).
2. Senior Security reviews → done-list to Security QA.
3. Security QA PASS to Chief Security (or further instructions).
4. Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS / IdP spend. Cost/critical → COO → CEO.
