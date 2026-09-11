# Security checklist — Product QA · PoC Participant auth D6 (#5)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #5 · Participant minimal register/auth (D6)  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**PR:** https://github.com/ioaikh/dealoware/pull/13  
**SD Security PASS:** `verification/2026-09-11__security__verification__poc-auth-sd-qa-confirm.md`  
**CQ:** no-refactor  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-productqa-checklist.md`

## Scope note
Product QA must verify with evidence that SD Security auth constraints hold. Real points — not N/A. PoC $0. No Cognito/SSO. No MM. Keep separate from #4. CQ = no-refactor.

## Itemized security points (Product QA must evidence)

1. **Fail closed** — Artifact create/get/list-own reject unauthenticated callers (401/403); no silent anonymous owner.
2. **Mechanism** — Only API key and/or JWT issue/validate in deliverable; no password UX, cookie sessions, SSO IdP, social login, Cognito.
3. **OIDC-shaped `sub`** — Stable `sub` (or Spec-equivalent) maps to Participant / Artifact owner id as documented.
4. **Secrets** — No committed real keys/signing secrets; env/placeholders; no raw token/key logging in evidence.
5. **Transport** — Auth via `Authorization` header (or Spec equivalent); not query string or Artifact body.
6. **Lifecycle** — Issue + validate + PoC invalidate/re-issue or rotate evidenced; no hard-coded master key in source.
7. **Bootstrap/register** — Seed/bootstrap and/or register within Spec bounds; no platform-owner RBAC.
8. **Authn ≠ authz** — Owner-scope checks on Artifact APIs still enforced with a valid token (#4 coupling verified).
9. **Local/$0 / no IdP spend** — No AWS Cognito/IdP provision; ECS Express Mode sketch only.
10. **Handshake close** — Product QA must not PASS until Security QA confirms these points.

## Handshake next
Product QA weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No managed IdP spend without COO→CEO.
