# BA business verification — Story #5 Participant minimal register/auth (D6)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-11 (BA verify executed 2026-09-20 ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/5  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/13 (MERGED @ `d8942c8535d81fe3b1d2bf5c6185924f0e231c49`)  
**Docs PR:** https://github.com/ioaikh/dealoware/pull/14 (OPEN — soft gap OK)  
**Method:** Business-intent AC check from Story body + Product lock + merged PR evidence on main + KB Product QA / Security / Doc (not code review). No invented requirements.

## Product lock (binding)

- PoC auth = **API key / JWT**
- Password with fuller registration at **MVP**
- **OIDC-compatible principal shape only** — no SSO IdP
- **No Cognito/SSO** in PoC

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | Register or bootstrap a Participant principal | **PASS** | `POST /auth/register` (`AuthEndpoints.Register`) creates `Participant` + issues hashed API key once; `Participant.Sub` = `participant:{guid}`; AuthEndpointTests register Facts (PR #13 / main). |
| 2 | PoC auth = API key / JWT (issue/validate for API calls) | **PASS** | `POST /auth/token` → `JwtService.IssueToken`; `AuthHelper` validates Bearer JWT or ApiKey/`dlw_` Bearer; lifecycle `/auth/revoke` + `/auth/rotate-key`; Product QA + SD verify on main. |
| 3 | Authenticated calls required for Artifact / Negotiation PoC APIs | **PASS** | Artifact create/get/list-own require AuthHelper; empty/invalid → 401; ArtifactAuthTests (no-auth, invalid, revoked). Negotiation PoC APIs **N/A on tip** (no Negotiation surface; Story #6 backlog) — same disposition as Product QA. |
| 4 | Principal claim shape OIDC-compatible (shape only — no SSO IdP) | **PASS** | Domain + JWT `sub` / `iss=dealoware` / `aud=dealoware-api`; owner = `sub` on Artifact create; README OIDC mapping table; no IdP shipped. |
| 5 | No full UX registration / password productization in PoC (MVP) | **PASS** | Bootstrap `displayName` only; no password/cookie/SSO endpoints; Infra csproj = IdentityModel JWT only (no Cognito SDK); Product QA tree/package scan clean. |

## Out of scope held

| OOS item (Story #5) | Held? | Evidence |
|---------------------|-------|----------|
| Cognito / SSO IdP (O9 → V3) | **Yes** | No Cognito/SSO packages or endpoints; Product lock + README local/$0; Security ProductQA/Doc confirms MET |
| Platform-owner users/permissions/roles (O1 → V3) | **Yes** | Register bounded bootstrap only; no O1 RBAC surface (SD / Product QA / Security) |
| Strategy / AI | **Yes** | Not in PR #13 file set / Program.cs routes |
| MotorMarket / DC4 | **Yes** | No MM/DC4 paths in tree/diff; Infra packages = EF SQLite + IdentityModel only |
| Full UX registration / password login (MVP) | **Yes** | Explicitly absent; displayName bootstrap only |
| #6–#8 unlock | **Yes** | Negotiation absent; CPM/PM comments keep #6–#8 backlog |

## Soft gaps (non-blocking for BA business verify)

- No live `dotnet restore|build|test` or `curl` on evidence box (same soft gap as Product QA / Security — accepted).
- No CI checks reported on PR #13 branch.
- Docs PR #14 still **OPEN** at verify time; impl PR #13 **MERGED** (soft gap OK per CBA).
- Negotiation auth requirement deferred until Negotiation surface exists (#6).

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Product QA | PASS | `qa/2026-09-11__qa__qa-report__poc-participant-d6-auth.md` |
| Product QA Security | PASS (1–10 MET) | `verification/2026-09-11__security__verification__poc-auth-productqa-qa-confirm.md` |
| Doc Security weave | PASS | `ops/2026-09-11__docs__ops__poc-auth-doc-security-weave.md` |
| Doc Security QA | PASS (1–10 MET) | `verification/2026-09-11__security__verification__poc-auth-doc-qa-confirm.md` |
| SD / CQ | PASS / `cq:no-refactor` | PR #13; `verification/2026-09-11__sd__verification__poc-participant-d6-auth.md` |

## Recommendation to CBA

**PASS** — deliverable meets Story #5 business AC, Product lock (API key/JWT; OIDC shape only; no Cognito/SSO), and OOS. Hand to BAQA for verify-QA; eng `done` HOLD until CBA final confirm after BAQA.
