# Verification — Security points vs PoC Auth Product QA evidence (#5)

**Author:** Dealoware Senior Security  
**Date:** 2026-09-11  
**Verdict:** **PASS** (all 10 Product QA-step Security points MET) — catch-up after Security QA confirm  
**Checklist (binding):** `verification/2026-09-11__security__verification__poc-auth-productqa-checklist.md` (10 points)  
**Product QA report:** `qa/2026-09-11__qa__qa-report__poc-participant-d6-auth.md`  
**Security QA PASS (already on file):** `verification/2026-09-11__security__verification__poc-auth-productqa-qa-confirm.md`  
**Prior SD Security PASS:** `verification/2026-09-11__security__verification__poc-auth-sd-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/13 (MERGED) · SD HEAD `32139d76…` · main `d8942c85…`  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**CQ:** no-refactor  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-productqa-points-review.md`  
**Constraints:** PoC $0; no Cognito/SSO; no MM/DC4; keep separate from #4. Catch-up OK per Chief.

## Sources checked

| Source | Path / link | Result |
|--------|-------------|--------|
| Auth Product QA Security checklist (Chief) | `verification/2026-09-11__security__verification__poc-auth-productqa-checklist.md` | Binding 10 points |
| Product QA report | `qa/2026-09-11__qa__qa-report__poc-participant-d6-auth.md` | Sec 1–9 EVIDENCED; #10 closed by Security QA |
| Security QA confirm | `verification/2026-09-11__security__verification__poc-auth-productqa-qa-confirm.md` | **PASS** already filed |
| Senior SD points review | `verification/2026-09-11__security__verification__poc-auth-sd-points-review.md` | Prior 10/10 MET |

## Checklist vs Product QA evidence (Senior score)

| # | Security point | Result | Evidence |
|---|----------------|--------|----------|
| 1 | **Fail closed** | **MET** | Product QA §1: AuthHelper on create/get/list-own; 401 without auth; no silent anon owner. Aligns SD PASS. |
| 2 | **Mechanism** | **MET** | §2: API key/JWT only; OUT password/cookie/SSO/Cognito. |
| 3 | **OIDC-shaped `sub`** | **MET** | §3: `sub` → OwnerParticipantId; JWT claims; README mapping. |
| 4 | **Secrets** | **MET** | §4: env signing key; placeholder fallback; hashed API keys; no raw logging in evidence. Soft note: PoC JWT placeholder (non-blocking). |
| 5 | **Transport** | **MET** | §5: Authorization header; AuthHeaderOnlyTests. |
| 6 | **Lifecycle** | **MET** | §6: revoke + rotate-key + JTI list. |
| 7 | **Bootstrap/register** | **MET** | §7: `/auth/register` abuse notes; no O1 RBAC. |
| 8 | **Authn ≠ authz** | **MET** | §8: owner-scope 404 with valid auth (other owner); list by owner. |
| 9 | **Local/$0 / no IdP spend** | **MET** | §9: SQLite; IdentityModel only; ECS Express sketch; no Cognito. |
| 10 | **Handshake close** | **MET** | Security QA confirm on file — Product QA / Dealoware QA may clear HOLD. |

## Soft gaps (non-blocking)

- No live `dotnet`/`curl`; no CI — static `gh` + prior SD Security PASS accepted (same as Security QA).

## Gaps

**None.** Catch-up only — aligns with Security QA PASS. Kept separate from #4.

## Done-list (for Security QA)

- [x] Path/name DOC-FLOW: `verification/2026-09-11__security__verification__poc-auth-productqa-points-review.md`
- [x] All 10 checklist points scored vs Product QA report + prior SD review
- [x] Aligns with Security QA **PASS** already filed
- [x] Kept separate from #4

## Cost/critical

None. No escalate.
