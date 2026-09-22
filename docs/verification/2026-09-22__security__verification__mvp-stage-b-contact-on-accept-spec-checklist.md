# Security checklist — Spec · MVP Stage B #42 Contact on accept (P7 / A9)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/42 · Contact on accept — identity seal → contact (P7 / A9 minimum)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #41 — keep separate Spec; cross-ref only  
**Precursor:** PoC **#7** identity-seal stub CLOSED — **extend**, do not rewrite #7 history  
**Architecture:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` (ContactEmail ShareOutbound-after-Accept; HasAcceptGrant)  
**Baselines:** Stage A #31 ShareOutbound(ContactEmail) Deny until this Story; #32 CLOSED; gate #24 CA PASS  
**Hold:** Gate **#25** backlog. Stage C + #26–#27 + #18 Spec/SD HOLD. Mature PII vault → V3. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-spec-checklist.md`

## Scope note
Spec-binding for MVP **identity-until-accept + contact on accept**: counterparty receives **ContactEmail** only after User Accept on **that** offer/negotiation via FieldPolicy **ShareOutbound(ContactEmail)** + Accept-grant context. Real points — not N/A. Do not invent mature vault, LoginEmail-on-Accept, or Stage C.

## Itemized security points (Spec must bind)

1. **Pre-Accept seal held** — Until Accept, Negotiation/Offer APIs continue to **omit** counterparty contact PII (opaque ids as needed); extend #7 stub — **no regression**.

2. **Accept grant record** — Spec binds User Accept on that offer/negotiation to resourceContext **HasAcceptGrant** (or equivalent) usable by FieldPolicy.

3. **ShareOutbound only after Accept** — Spec binds **ShareOutbound(ContactEmail)** release to counterparty **only** when Accept is recorded on **that** offer/negotiation; before Accept → ShareOutbound **Deny** for all principals (fail-closed).

4. **ContactEmail policy rows (Stage B)** — Spec binds: **User** Read/Write; **OwnAgent Read** allowed (not ShareOutbound by default); **Counterparty** Deny until ShareOutbound grant; **Stranger / Unauth** Deny.

5. **LoginEmail never on Accept** — Spec binds **LoginEmail** remains **User-only** (OwnAgent Deny; **not** shared on Accept); do **not** conflate LoginEmail with ContactEmail.

6. **Authn / stranger fail-closed** — Unauthenticated → deny; stranger still deny post-Accept for ContactEmail; uniform deny bodies without private-field leakage.

7. **Extend #7 under ACL, don’t rewrite** — Spec extends seal → contact under #18 / #31 FieldPolicy; does not rewrite #7 CLOSED history or invent #18 Spec/SD delivery (#18 Spec/SD stays HOLD).

8. **OUT locked** — Spec documents **P7** / **A9** MVP **minimum**; mature PII vault retention/erasure → **V3**; distinct from #31 registry and #32 list isolation; gate **#25** backlog; no Cognito/SSO/MM inventing.

9. **Cost / spend** — PoC **$0**; no IdP/vault provision required to accept this Spec (mature vault out).

10. **Traceability + handshake** — Spec cites #42 AC + #18 Option A Stage B + #7 precursor only. Spec QA must **not** PASS until Security QA confirms. Keep #40/#41 separate.

## Handshake next
Senior Spec weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
