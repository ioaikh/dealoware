# Security checklist — SD · PoC Identity-seal stub (#7)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #7 · Identity-seal stub (no contact exchange)  
**Issue:** https://github.com/ioaikh/dealoware/issues/7  
**Plan:** `plans/2026-09-20__devplan__plan__poc-identity-seal-stub.md`  
**Dev Plan Security PASS:** `verification/2026-09-20__security__verification__poc-identity-seal-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-20__security__verification__poc-identity-seal-spec-qa-confirm.md`  
**Hold:** #8 backlog; real contact-on-accept = MVP **P7**/**A9** — do not invent; do **not** pull [#18](https://github.com/ioaikh/dealoware/issues/18) into PoC  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-identity-seal-sd-checklist.md`

## Scope note
SD must implement Spec + Dev Plan Security gates for the identity-seal **stub** only (DTO omit contact/PII; opaque ids; Accept = state-only). Real points — not N/A. PoC $0. No Cognito/SSO. No Strategy/AI. No mature PII vault. Extend #5/#6 code only — do not rewrite those Stories.

## Itemized security points (SD must satisfy)

1. **Authn fail-closed** — #7-touched Negotiation/Offer APIs require validated #5 principal; unauthenticated → 401/403; no anonymous identity/contact surfaces.

2. **Party-only authz (narrow)** — Non-party cannot view/mutate that Negotiation/Offer; 404 preferred (or 403) without leaking contact, identity fields, or other Negotiations. Do **not** implement full #18 tenancy/Strategy isolation.

3. **Public DTOs omit contact/PII** — Negotiation/Offer (and public Participant views on those paths) return **opaque ids only** — no email, phone, real name-as-contact, or address fields.

4. **Accept path = state-only** — Accept/Decline/Counter/Close (and any seal flag) change **state only**; zero contact/identity-reveal fields in responses.

5. **No contact-exchange surface** — No PoC endpoint or Accept payload returns counterparty contact; real release deferred to MVP **P7**/**A9**.

6. **Stub precursor only** — Optional `identitySealed` (if present) is stub-only and must **not** act as a contact-release signal; no vault/KMS/MVP release behavior.

7. **No-leak evidence** — Automated tests and/or explicit verification notes prove happy-path create/get/place/Accept/Decline/Counter/Close flows do **not** expose counterparty contact PII.

8. **No inventing OUT** — No Strategy/AI, mature vault, settlement, Cognito/SSO/IdP, MM/DC4; **#8** stays backlog; **#18** stays out of PoC.

9. **Secrets / host / no spend** — Authorization-header hygiene; no committed secrets; local/$0; ECS Express sketch only; **no** Cognito/SSO/IdP provision.

10. **Evidence + handshake** — Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
