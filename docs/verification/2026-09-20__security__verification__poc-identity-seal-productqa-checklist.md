# Security checklist — Product QA · PoC Identity-seal stub (#7)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #7 · Identity-seal stub (no contact exchange)  
**Issue:** https://github.com/ioaikh/dealoware/issues/7  
**PR:** https://github.com/ioaikh/dealoware/pull/19 (MERGED)  
**SD Security PASS:** `verification/2026-09-20__security__verification__poc-identity-seal-sd-qa-confirm.md`  
**Hold:** #8 backlog; real contact-on-accept = MVP **P7**/**A9** — do not invent; do **not** pull [#18](https://github.com/ioaikh/dealoware/issues/18) into PoC  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-identity-seal-productqa-checklist.md`

## Scope note
Product QA must evidence that SD Security constraints for the identity-seal **stub** hold on **main** (PR #19 merged). Real points — not N/A. PoC $0. No Cognito/SSO. Soft gap no-live-dotnet OK if stated with equivalent evidence (same pattern as #4/#5/#6).

## Itemized security points (Product QA must evidence)

1. **Authn fail-closed** — #7-touched Negotiation/Offer APIs reject unauthenticated callers (401/403); no anonymous identity/contact surfaces.

2. **Party-only authz (narrow)** — Non-party cannot view/mutate; 404 preferred (or 403) without contact/identity/cross-Negotiation leak. Do **not** require full #18 tenancy evidence in PoC.

3. **Public DTOs omit contact/PII** — Negotiation/Offer (and public Participant views on those paths) expose **opaque ids only** — no email/phone/name-as-contact/address on happy-path responses.

4. **Accept path = state-only** — Accept/Decline/Counter/Close responses are state-only; **no** contact/identity reveal on Accept.

5. **No contact-exchange surface** — No PoC endpoint or Accept payload returns counterparty contact; real release deferred to MVP **P7**/**A9**.

6. **Stub precursor only** — Optional `identitySealed` (if present) is stub-only and does **not** act as a contact-release signal; no vault/KMS/MVP release behavior observed.

7. **No-leak evidence** — Tests and/or explicit verification notes prove happy-path create/get/place/Accept/Decline/Counter/Close flows do **not** expose counterparty contact PII.

8. **No inventing OUT** — No Strategy/AI, mature vault, settlement, Cognito/SSO/IdP, MM/DC4 delivered; **#8** backlog; **#18** out of PoC.

9. **Secrets / host / no spend** — No committed secrets; Authorization-header hygiene; local/$0; no Cognito/SSO/IdP provision.

10. **Handshake close** — Product QA must **not** PASS until Security QA confirms these points.

## Handshake next
Product QA weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
