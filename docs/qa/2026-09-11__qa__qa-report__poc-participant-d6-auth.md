# QA Report — PoC Participant D6 auth (Product QA evidence)

**Status:** PASS — Security QA confirm landed; QAQA confirmed to Chief (no bounce)  
**Date:** 2026-09-11  
**Author:** Dealoware Senior Product QA  
**Confirm to:** Dealoware QA (Chief QA) via QAQA **after** Security handshake  
**Story:** GitHub issue #5 · Participant minimal register/auth (D6)  
**Issue:** https://github.com/ioaikh/dealoware/issues/5 (still OPEN at evidence time)  
**PR:** https://github.com/ioaikh/dealoware/pull/13 (MERGED)  
**SD HEAD (PR head):** `32139d76e57f5fa5a5c1068948915bfdf04ffd3c`  
**main tip / merge commit:** `d8942c8535d81fe3b1d2bf5c6185924f0e231c49`  
**Merge parents:** `8e4e9f93b428457428c3dee1538bf330709f99be` + `32139d76…`  
**Branch (merged):** `cursor/participant-auth-5a27`  
**MergedAt:** 2026-09-11T02:49:16Z (≈ 2026-09-10 22:49 EDT)  
**Security checklist:** `verification/2026-09-11__security__verification__poc-auth-productqa-checklist.md`  
**Security QA confirm (this step):** `verification/2026-09-11__security__verification__poc-auth-productqa-qa-confirm.md` — PASS (pts 1–10 MET)  
**Prior SD:** `verification/2026-09-11__sd__verification__poc-participant-d6-auth.md` (SD PASS @ `32139d76…`)  
**Prior SD Security QA:** `verification/2026-09-11__security__verification__poc-auth-sd-qa-confirm.md` (PASS, 1–10 MET @ `32139d76…`)  
**DOC-FLOW:** `qa/2026-09-11__qa__qa-report__poc-participant-d6-auth.md`  
**CQ:** no-refactor  

## Method
- `gh pr view 13` / `gh api` contents & tree at **main** `d8942c85…` (no clone)
- KB: productqa checklist, SD verification, ORG-OPS handshake
- Soft gap: .NET SDK absent on evidence box → live `dotnet test` / `curl` not run
- CI: `gh pr checks 13` → no checks reported on branch
- Confirm: `…poc-auth-productqa-qa-confirm.md` PASS (pts 1–10 MET)

## Product acceptance criteria

| AC | Verdict | Evidence (main `d8942c8535d81fe3b1d2bf5c6185924f0e231c49`) |
|----|---------|----------|
| 1 Register/bootstrap Participant | SOFT | `POST /auth/register` creates `Participant` + issues hashed API key once (`AuthEndpoints.Register`); `Participant.Sub` = `participant:{guid}`; AuthEndpointTests register Facts |
| 2 API key / JWT issue+validate | SOFT | `POST /auth/token` → `JwtService.IssueToken`; AuthHelper validates Bearer JWT or ApiKey/`dlw_` Bearer; validate path checks participant active + JTI revoke list |
| 3 Authenticated Artifact PoC APIs (fail-closed) | SOFT | Create/Get/List require AuthHelper; empty/invalid → 401; ArtifactAuthTests (no-auth, invalid key/JWT, revoked key); Negotiation APIs N/A (no Negotiation surface on tip; #6) |
| 4 OIDC-compatible principal `sub` | SOFT | Domain + JWT `sub`/`iss=dealoware`/`aud=dealoware-api`; owner = `sub` on create; README OIDC mapping table |
| 5 No full UX / password productization; no Cognito/SSO IdP | SOFT | Bootstrap displayName only; no password/cookie/SSO endpoints; tree/package scan: IdentityModel JWT only; no Cognito SDK |
| Health still present | SOFT | `Program.cs` `GET /health` open `{status:ok}`; HealthEndpointTests + ArtifactAuthTests Health_* |
| README ECS Express / local/$0 | SOFT | Auth env vars + curl cookbook; ECS Express Mode sketch; local/$0; SQLite; not Cognito |

## Security checklist points 1–10 (Product QA evidence)

| # | Point | Verdict | Evidence |
|---|-------|---------|----------|
| 1 | Fail closed Artifact APIs | EVIDENCED | `ArtifactEndpoints`: AuthHelper; missing/invalid → Unauthorized; tests Create/Get/List without auth → 401; no silent anon owner (X-PoC-Owner-Id removed from auth path) |
| 2 | Mechanism API key/JWT only | EVIDENCED | `/auth/register|token|revoke|rotate-key`; JwtService; OUT password/cookie/SSO/Cognito |
| 3 | OIDC-shaped `sub` → owner | EVIDENCED | `Participant.Sub` → `Artifact.OwnerParticipantId`; JWT RegisteredClaimNames.Sub; tests assert owner = sub |
| 4 | Secrets via env/placeholders | EVIDENCED | `DEALOWARE_JWT_SIGNING_KEY` / lifetime env; DEVELOPMENT_PLACEHOLDER fallback; API key SHA-256 hashed; README “shown once” |
| 5 | Authorization header only | EVIDENCED | AuthHelper reads Authorization only; AuthHeaderOnlyTests reject query/body token patterns (8 Facts) |
| 6 | Lifecycle revoke/rotate | EVIDENCED | `/auth/revoke` (API key + TokenJti); `/auth/rotate-key`; JwtService checks `IRevokedTokenRepository`; tests revoke/rotate |
| 7 | Bootstrap/register within Spec; no O1 RBAC | EVIDENCED | Register abuse/rate notes (docs); no platform-owner RBAC |
| 8 | Authn ≠ authz owner-scope | EVIDENCED | Get: `OwnerParticipantId != sub` → 404; List: `GetByOwnerAsync(sub)`; `GetArtifact_OtherOwnerArtifact_ValidAuth_Returns404_AuthnNotAuthz` |
| 9 | Local/$0; no Cognito; ECS Express sketch | EVIDENCED | EF SQLite; System.IdentityModel.Tokens.Jwt; README ECS Express / local/$0 |
| 10 | Handshake close | PASS | Checklist + `…poc-auth-productqa-qa-confirm.md` PASS (pts 1–10 MET) |

## Soft gaps / non-blockers
- No live `dotnet restore|build|test` or `curl` on this box (`dotnet` absent)
- No CI checks on PR branch
- Relies on static `gh` evidence + prior SD / SD-Security PASS for runtime claims
- Issue #5 may remain OPEN post-merge (PM may close)
- Negotiation auth requirement deferred — no Negotiation endpoints on tip

## Disposition

**PASS** — Security QA `…poc-auth-productqa-qa-confirm.md` PASS (pts 1–10 MET); QAQA confirmed Product QA PASS to Chief (no bounce).  
Soft gaps (no live dotnet / no CI) accepted as non-blocking.  
Do not set GitHub status from this step alone (PM/Chief owns gate).

### Done-list (QAQA)
- [x] Evidence at main merge `d8942c85…` (+ SD HEAD `32139d76…`)
- [x] AC + Sec 1–9 woven with citations
- [x] Product-step Security QA confirm PASS
- [x] QAQA confirmed Product QA PASS to Chief
