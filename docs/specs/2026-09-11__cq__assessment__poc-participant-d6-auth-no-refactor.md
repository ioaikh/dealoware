# CQ Assessment — PoC Participant D6 auth #5 / PR #13 — NO REFACTOR

**Author:** Dealoware Senior CQ  
**Date:** 2026-09-11  
**Verdict:** **cq:no-refactor**  
**Issue:** https://github.com/ioaikh/dealoware/issues/5  
**PR:** https://github.com/ioaikh/dealoware/pull/13 · HEAD `32139d76e57f5fa5a5c1068948915bfdf04ffd3c`  
**Confirm path:** CQ QA → Chief CQ → CPM gate  
**Constraints:** No product code in this artifact. No MotorMarket/DC4. Do not invent Stories. No Cognito/SSO.

## Verdict

No refactor requirement spec. Product D6 lock (API key/JWT; OIDC-shaped `sub`; Authorization header only; fail-closed Artifact) holds. Soft PoC notes below are **non-gate**.

## Evidence

1. **Product D6:** Register + issue/validate API key and JWT; no Cognito/SSO/password UX/cookie/social in PR tree.
2. **OIDC-shaped principal:** `Participant.Sub = participant:{guid}`; JWT claims include sub/iss/aud/jti; Artifact owner maps to `sub` (X-PoC-Owner-Id removed).
3. **Layering:** Domain Participants + credential ports; Application Auth DTOs only; Infrastructure `JwtService` + EF repos; Api `AuthHelper` + `AuthEndpoints`; JWT validate not duplicated (AuthHelper → JwtService).
4. **Secrets hygiene:** API keys stored hashed (SHA-256); signing key env → config → DEVELOPMENT_PLACEHOLDER (Spec §4 env/placeholders); no Jwt secrets in committed appsettings.
5. **Fail-closed Artifact:** create/get/list-own require AuthHelper; missing/invalid → 401; non-owned get → 404; header-only transport.
6. **`/health`:** remains Auth:none `{status:ok}`.
7. **Lifecycle:** POST `/auth/register|token|revoke|rotate-key` — Spec PoC-minimal issue/validate/invalidate/rotate.
8. **OUT:** No MM/DC4, Cognito, SSO, password UX, Strategy/AI in code.
9. **Peer PASS (KB):** SD verify + Security QA 10/10 on matching HEAD; plan/spec KB-only; BA note on `docs/plans/`.
10. **Tests:** 50 Facts — AuthEndpointTests, ArtifactAuthTests, AuthHeaderOnlyTests, updated ArtifactEndpointTests, Health retained.

## Soft notes (explicitly non-gate)

| Note | Why non-gate for PoC #5 |
|------|-------------------------|
| `DEVELOPMENT_PLACEHOLDER` JWT signing fallback | Spec/plan allow env/placeholders for PoC local/$0 |
| API-key verify logic in both AuthHelper and AuthEndpoints | Duplication smell; extract later when auth surface grows |
| `GetAllValidAsync` / `CleanupExpiredAsync` unused | Dead port methods; hygiene later |
| TokenJti revoke: no jti↔sub ownership bind; ExpiresAt≈UtcNow+7d | Spec allows simple revoke list for PoC |
| JWT-only rotate path may leave prior API keys valid | Spec rotate = re-issue + simple revoke note; API-key rotate revokes old key |
| KeyPrefix index not unique | PoC risk low; tighten if collisions appear |
| Test gaps (revoked JWT on Artifact path, etc.) | Soft coverage note for QA Team; not structure refactor |
| `EnsureCreatedAsync` retained | PoC SQLite bootstrap |

## Affected functionality (QA coordination)

1. `POST /auth/register` bootstrap  
2. `POST /auth/token` issue JWT  
3. `POST /auth/revoke` (API key and/or TokenJti)  
4. `POST /auth/rotate-key`  
5. Authorization header only (ApiKey / Bearer JWT / Bearer dlw_) — no query/body tokens  
6. Artifact create/get/list-own fail-closed on auth; owner-scope authz retained  
7. `GET /health` still open  
8. Zero Cognito/SSO/password UX; zero MM/DC4  

## Done-list for CQ QA

- [ ] Product D6 lock (API key/JWT; no Cognito/SSO/password UX)
- [ ] Fail-closed Artifact; X-PoC removed; `/health` open
- [ ] Soft notes marked non-gate
- [ ] Affected-functionality list complete
- [ ] No silent scope creep / no MM/DC4 / no invented Stories
- [ ] Confirm PASS to Chief CQ only

