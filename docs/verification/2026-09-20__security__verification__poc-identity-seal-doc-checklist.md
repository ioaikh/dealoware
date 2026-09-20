# Security checklist — Doc · PoC Identity-seal stub (#7)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #7 · Identity-seal stub (no contact exchange)  
**Issue:** https://github.com/ioaikh/dealoware/issues/7  
**PR:** https://github.com/ioaikh/dealoware/pull/19 (MERGED)  
**Product QA Security PASS:** `verification/2026-09-20__security__verification__poc-identity-seal-productqa-qa-confirm.md`  
**Hold:** #8 backlog; real contact-on-accept = MVP **P7**/**A9** — do not invent; do **not** pull [#18](https://github.com/ioaikh/dealoware/issues/18) into PoC  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-identity-seal-doc-checklist.md`

## Scope note
Doc must accurately describe the PoC identity-seal **stub** only (opaque ids; DTO omit contact/PII; Accept = state-only) and must **not** invent contact-on-Accept, Cognito/SSO, Strategy/AI, mature vault, or #18 tenancy as delivered. Real points — not N/A. PoC $0. Soft no-live-dotnet OK if stated. Keep separate from #4/#5/#6 Doc Security except cross-refs.

## Itemized security points (Doc must satisfy)

1. **Authn fail-closed** — Docs state #7-touched Negotiation/Offer APIs require #5 auth; unauthenticated calls fail (401/403).

2. **Party-only (narrow)** — Docs describe party-only access; non-party gets 404/403 without leaking contact/identity/other Negotiations. Do **not** document full #18 tenancy as PoC delivered.

3. **Public DTOs omit contact/PII** — Docs state Negotiation/Offer (and public Participant views on those paths) use **opaque ids only** — no email/phone/name-as-contact/address on PoC responses.

4. **Accept path = state-only** — Docs state Accept/Decline/Counter/Close change **state only**; **no** contact/identity reveal on Accept in PoC.

5. **No contact-exchange surface** — Docs do not describe a PoC contact-exchange endpoint or Accept payload returning counterparty contact; real release deferred to MVP **P7**/**A9**.

6. **Stub as precursor (documented, not MVP)** — Docs document stub as precursor to MVP identity-until-accept + contact-on-accept without claiming vault/KMS or MVP release behavior as delivered.

7. **No-leak evidence cited** — Docs/weave cite tests and/or verification notes proving happy-path flows do not expose counterparty contact PII.

8. **No inventing OUT** — Docs do not invent Strategy/AI, mature vault, settlement, Cognito/SSO/IdP, MM/DC4 as delivered; **#8** stays backlog; **#18** stays out of PoC.

9. **Secrets / host / OUT** — Placeholders only; Authorization-header examples; local/$0; ECS Express sketch; no Cognito/SSO/IdP how-tos as delivered.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
