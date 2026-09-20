# Security checklist — SD · PoC Participant auth D6 (#5)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-11  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #5 · Participant minimal register/auth (D6)  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**Plan:** `plans/2026-09-11__devplan__plan__poc-participant-d6-auth.md`  
**Dev Plan Security PASS:** `verification/2026-09-11__security__verification__poc-auth-devplan-qa-confirm.md`  
**Parallel:** Coordinate with #4 Artifact SD — protect create/get/list-own; keep authn≠authz.  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-sd-checklist.md`

## Scope note
SD-binding checklist for API key/JWT implementation. Real points — not N/A. PoC $0. No Cognito/SSO IdP. No MM. No inventing.

## Itemized security points (SD must satisfy)

1. **Fail closed** — Artifact create/get/list-own require validated principal; unauthenticated → 401/403 (no silent anonymous owner).
2. **Mechanism** — Implement API key and/or JWT issue+validate only. **Out:** password UX, cookie sessions, SSO IdP, social login, Cognito.
3. **OIDC-shaped `sub`** — Stable `sub` (or Spec-equivalent) maps to Participant id / Artifact `ownerParticipantId`.
4. **Secrets** — Signing material/API keys via env only; never commit; never log raw tokens/keys; docs placeholders.
5. **Transport** — Credentials via `Authorization` header (or Spec equivalent); not query string or Artifact body.
6. **Lifecycle** — Issue + validate + PoC-minimal invalidate/re-issue or rotate; no hard-coded master key in source.
7. **Bootstrap/register** — Seed/bootstrap and/or register per Spec bounds; no platform-owner RBAC (O1 out).
8. **Authn ≠ authz** — Do not remove #4 owner-scope checks; valid token ≠ owns Artifact.
9. **Local/$0** — No AWS IdP/Cognito provision; ECS Express Mode sketch only.
10. **Evidence** — Done-list cites paths/tests for 1–9; Code/Product QA must not PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No managed IdP spend without COO→CEO.
