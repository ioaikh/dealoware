# Security QA — PoC Participant auth Product QA (#5) vs Chief Security Product QA checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-11  
**Verdict:** **PASS** (all 10 points MET)  
**Asked by:** Dealoware QA (Chief QA)  
**Product QA report:** `qa/2026-09-11__qa__qa-report__poc-participant-d6-auth.md`  
**Chief checklist (binding):** `verification/2026-09-11__security__verification__poc-auth-productqa-checklist.md` (10 points)  
**Prior SD Security PASS:** `verification/2026-09-11__security__verification__poc-auth-sd-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/13 (MERGED)  
**SD HEAD:** `32139d76e57f5fa5a5c1068948915bfdf04ffd3c` · **main:** `d8942c8535d81fe3b1d2bf5c6185924f0e231c49`  
**CQ:** no-refactor  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-productqa-qa-confirm.md`  
**Constraints:** PoC $0; no Cognito/SSO; no MM/DC4; keep separate from #4; never skip Chief.

## Soft gap accepted

No live `dotnet`/`curl`; no CI — static `gh` on main + prior SD Security PASS on same SD HEAD. **Accepted.**

## Independent re-score (Security QA)

| # | Point | Product QA | Security QA | Evidence |
|---|-------|------------|-------------|---------|
| 1 | Fail closed | EVIDENCED | **MET** | Artifact APIs AuthHelper; 401 without auth; no silent anon owner |
| 2 | Mechanism | EVIDENCED | **MET** | API key/JWT only; OUT password/cookie/SSO/Cognito |
| 3 | OIDC-shaped `sub` | EVIDENCED | **MET** | `sub` → OwnerParticipantId; JWT claims |
| 4 | Secrets | EVIDENCED | **MET** | Env signing key; placeholder fallback; hashed API keys |
| 5 | Transport | EVIDENCED | **MET** | Authorization header; AuthHeaderOnlyTests |
| 6 | Lifecycle | EVIDENCED | **MET** | revoke + rotate-key + JTI revoke list |
| 7 | Bootstrap/register | EVIDENCED | **MET** | Register with abuse notes; no O1 RBAC |
| 8 | Authn ≠ authz | EVIDENCED | **MET** | Owner-scope 404 with valid auth (other owner) |
| 9 | Local/$0 / no IdP spend | EVIDENCED | **MET** | SQLite; IdentityModel only; ECS Express sketch |
| 10 | Handshake close | HOLD→await | **MET** | This confirm closes gate |

## Handshake status

Security QA → **PASS** confirm to Chief Security. Dealoware QA / Product QA may clear HOLD. Cost/critical: none.
