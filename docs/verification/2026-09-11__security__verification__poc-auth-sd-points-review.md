# Verification — Security points vs PoC Auth SD (#5)

**Author:** Dealoware Senior Security  
**Date:** 2026-09-11  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Checklist (binding):** `verification/2026-09-11__security__verification__poc-auth-sd-checklist.md` (10 points)  
**Evidence:** https://github.com/ioaikh/dealoware/pull/13 · branch `cursor/participant-auth-5a27`  
**Plan:** `plans/2026-09-11__devplan__plan__poc-participant-d6-auth.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**DOC-FLOW:** `verification/2026-09-11__security__verification__poc-auth-sd-points-review.md`  
**Constraints:** PoC $0; no Cognito/SSO IdP; no MM/DC4; keep separate from #4 Artifact domain inventing. Reviewed via `gh` remote reads (no clone).

## Sources checked

| Source | Path / link | Result |
|--------|-------------|--------|
| Auth SD Security checklist (Chief) | `verification/2026-09-11__security__verification__poc-auth-sd-checklist.md` | Binding 10 points |
| PR #13 | https://github.com/ioaikh/dealoware/pull/13 | Auth endpoints, AuthHelper, JwtService, Artifact wiring, tests, README |
| Key paths | `AuthHelper.cs`, `AuthEndpoints.cs`, `ArtifactEndpoints.cs`, `JwtService.cs`, `Program.cs`, `ApiKeyCredential.cs`, `*.csproj`, tests, `README.md` | Reviewed |
| Diff scan | Cognito/MM/DC4/password/SSO/cookie | No hits |

## Checklist vs SD (evidence)

| # | Security point | Result | Evidence |
|---|----------------|--------|----------|
| 1 | **Fail closed** | **MET** | Artifact create/get/list-own call `AuthHelper.GetAuthenticatedSub`; missing/invalid → **401**. Tests: without Authorization → 401. Health stays open (`MapGet("/health")`). |
| 2 | **Mechanism** | **MET** | API key (`ApiKey` / `Bearer dlw_…`) + JWT issue/validate (`JwtService`). Packages: IdentityModel JWT only — no Cognito/OIDC IdP/password/cookie/social. |
| 3 | **OIDC-shaped `sub`** | **MET** | JWT claim `sub`; `OwnerParticipantId` set from authenticated `sub`; README documents mapping. |
| 4 | **Secrets** | **MET** | `DEALOWARE_JWT_SIGNING_KEY` env + config; API keys stored as SHA-256 hash only (`ApiKeyCredential`); raw key returned once. README placeholders. Soft note: code fallback `DEVELOPMENT_PLACEHOLDER_KEY_…` is clearly a PoC placeholder (not a prod secret). |
| 5 | **Transport** | **MET** | Authorization header only (`AuthHelper`). `AuthHeaderOnlyTests`: query/body tokens → 401. |
| 6 | **Lifecycle** | **MET** | Issue (`/auth/token`), validate, revoke (`/auth/revoke` + JTI list), rotate-key. No master key in source. |
| 7 | **Bootstrap/register** | **MET** | `POST /auth/register` creates Participant + API key; abuse/rate note in code comments (PoC local). No O1 RBAC roles. |
| 8 | **Authn ≠ authz** | **MET** | After authn, get still `OwnerParticipantId != sub` → **404**. Test `GetArtifact_OtherOwnerArtifact_ValidAuth_Returns404_AuthnNotAuthz`. List filters by owner `sub`. |
| 9 | **Local/$0** | **MET** | SQLite + local host; JWT packages only; no Cognito/AWS IdP provision. README ECS Express sketch retained. |
| 10 | **Evidence** | **MET** | This done-list cites paths/tests. **Dev Code QA / Product QA must not PASS until Security QA confirms.** |

## Soft notes (non-blocking)

- Development JWT signing-key placeholder in `Program.cs` is acceptable for PoC; production must set `DEALOWARE_JWT_SIGNING_KEY`.
- Register abuse controls are documented notes, not enforced rate limits (Spec PoC-bound OK).

## Gaps for Senior Developer

**None.** Interim `X-PoC-Owner-Id` removed from Artifact endpoints — #5 principal required (fail closed).

## Done-list (for Security QA)

- [x] Path/name DOC-FLOW: `verification/2026-09-11__security__verification__poc-auth-sd-points-review.md`
- [x] All 10 checklist points scored with file/test evidence from PR #13
- [x] Kept separate from #4 Artifact SD review
- [x] Security QA: confirm **PASS** (`verification/2026-09-11__security__verification__poc-auth-sd-qa-confirm.md`) — DOC-FLOW catch-up closed

## Cost/critical

None. No Cognito/IdP spend. No escalate.
