# Security QA — PoC Participant auth SD (#5) vs Chief Security SD checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-11  
**Verdict:** **PASS** (all 10 points MET)  
**Asked by:** Dealoware Dev Code QA  
**Chief checklist (binding):** `verification/2026-09-11__security__verification__poc-auth-sd-checklist.md` (10 points)  
**Dev Plan Security PASS:** `verification/2026-09-11__security__verification__poc-auth-devplan-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/13  
**HEAD:** `32139d76e57f5fa5a5c1068948915bfdf04ffd3c`  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-sd-qa-confirm.md`  
**Constraints:** PoC $0; no Cognito/SSO IdP; no MM/DC4; keep separate from inventing #4/#6; never skip Chief. Reviewed via `gh` remote reads.

## Independent re-score (Security QA)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Fail closed | **MET** | `ArtifactEndpoints`: AuthHelper required; missing/invalid → Unauthorized; no silent anonymous owner; health stays open |
| 2 | Mechanism | **MET** | API key + JWT issue/validate only (`AuthEndpoints`/`JwtService`); OUT password/cookie/SSO/Cognito (no such packages/endpoints) |
| 3 | OIDC-shaped `sub` | **MET** | JWT `sub` / Participant.Sub → Artifact OwnerParticipantId on create; README mapping |
| 4 | Secrets | **MET** | `DEALOWARE_JWT_SIGNING_KEY` env; DEVELOPMENT_PLACEHOLDER documented; never commit real keys (docs) |
| 5 | Transport | **MET** | Authorization header only (`AuthHelper`); `AuthHeaderOnlyTests` reject query-string tokens |
| 6 | Lifecycle | **MET** | Issue token + validate + `/auth/revoke` + `/auth/rotate-key`; revoked JTI list; no master key in source |
| 7 | Bootstrap/register | **MET** | `/auth/register` with PoC abuse/rate note comments; no O1 RBAC |
| 8 | Authn ≠ authz | **MET** | After authn, get still `OwnerParticipantId != sub` → 404; list by owner |
| 9 | Local/$0 | **MET** | SQLite local; IdentityModel JWT libs only (no Cognito SDK); ECS Express sketch in README |
| 10 | Evidence | **MET** | This confirm + `AuthEndpointTests` / `ArtifactAuthTests` / `AuthHeaderOnlyTests` |

## Soft notes (non-blocking)

- Dev placeholder signing key fallback in `Program.cs` is clearly labeled; env preferred for real keys.
- Register abuse note is documentation-level (no live rate limiter) — Spec PoC-acceptable.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Dev Code QA may proceed on Security gate. Cost/critical: none (no managed IdP spend).
