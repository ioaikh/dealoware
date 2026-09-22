# Security checklist — SD · MVP Stage B #40 Instant search / discovery (P2)

**Status:** SD Implementation — Security points satisfied with evidence  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security (checklist) + SD Implementation (evidence)  
**Story:** https://github.com/ioaikh/dealoware/issues/40 · Instant search / discovery (P2)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #41 · #42 — keep separate SD; cross-ref only  
**Plan:** `plans/2026-09-22__devplan__plan__mvp-stage-b-instant-search-discovery.md`  
**Dev Plan Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-devplan-qa-confirm.md` (SoR PR #47)  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-qa-confirm.md` (SoR PR #44)  
**Hold:** Gate **#25** backlog. Stage C + #26–#27 + #18 Spec/SD (whole) HOLD. Product QA HOLD until SD-step Security PASS. Saved-search → V1; A1 → V2. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-checklist.md`
**Dependencies:** #31 Field ACL (CLOSED), #32 Account List Fail-Closed (CLOSED)

## Scope note
SD must **implement** Spec + Dev Plan Security for MVP **instant** discovery: authn fail-closed; discovery ≠ owner inventory; search payloads omit secrets; consume #31 Field ACL. Real points — not N/A. Evidence via tests/done-list.

---

## Itemized security points (SD must satisfy) — with implementation evidence

| # | Security Point | Implementation Evidence | Status |
|---|----------------|------------------------|--------|
| 1 | **Authn fail-closed** — Instant-search paths require validated #5 principal; unauth → **401**; error bodies omit private fields / secrets. | `SearchEndpoints.cs:SearchArtifacts()` calls `AuthHelper.GetAuthenticatedSub()` first; returns `Results.Unauthorized()` if `sub` is null/empty. Health endpoint unchanged at `/health`. Tests: `Search_Unauth_Returns401`, `Search_InvalidAuth_Returns401`, `Search_InvalidApiKey_Returns401`, `Search_Unauth_NoPrivateFieldsInError` | ✅ |
| 2 | **Discovery ≠ inventory** — Discovery surface separate from #32 owner list/get; no private inventory dump via search. | `GET /search/artifacts` is a new endpoint separate from `GET /artifacts` (owner inventory). Search does NOT filter by owner; returns artifacts matching query regardless of ownership. `DiscoverableArtifactResponse` omits `OwnerParticipantId`. Tests: `Search_IsSeparateFromOwnerInventory`, `Search_CanDiscoverOthersArtifacts`, `Search_DoesNotDumpPrivateInventory` | ✅ |
| 3 | **Search payload omit secrets** — Serializers omit StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets from search results. | `DiscoverableArtifactResponse` DTO explicitly omits: `OwnerParticipantId`, `LoginEmail`, `ContactEmail`, `StrategyBody`, auth secrets. Only D1-D5 Artifact fields projected (Subject/Intent/Value/Location/Time). Tests: `Search_Results_NoOwnerParticipantId`, `Search_Results_NoLoginEmail`, `Search_Results_NoContactEmail`, `Search_Results_NoStrategyBody`, `Search_Results_NoAuthSecrets` | ✅ |
| 4 | **Discoverable-fields-only** — Search/result fields limited to allowed Artifact fields already on path; no new Artifact schema for search. | Search queries existing D1-D5 fields: `Intent`, `Entities.Name`, `Entities.Description`, `Entities.Properties.Name/Value`. No new schema added. Test: `Search_Auth_ReturnsDiscoverableFieldsOnly` | ✅ |
| 5 | **Uniform deny / no-leak** — Wrong-principal / stranger misuse → fail-closed; uniform deny bodies; no private-field leakage (verified). | Unauthorized returns `Results.Unauthorized()` (empty body). Bad request returns generic error. No participant IDs, emails, or secrets in error responses. Test: `Search_Unauth_NoPrivateFieldsInError` | ✅ |
| 6 | **Consume #31 Field ACL** — Projection omit via `IFieldPolicy` / FieldClass; do **not** rewrite #31. | Uses existing `IFieldPolicy` from #31. `DiscoverableArtifactResponse` design aligned with FieldClass policy: `LoginEmail`/`ContactEmail` never in discovery surface (consistent with Stranger deny policy). No registry rewrite. | ✅ |
| 7 | **No Stage C / #18 inventing** — No Assistant hard wall, Cognito, MM/DC4, or unlocking #18 Spec/SD as a whole. | Endpoint is instant search only. No Assistant, no Cognito, no MM/DC4. | ✅ |
| 8 | **OUT locked** — P2 instant only; saved-search → V1; A1 → V2; gate #25 backlog; do not implement #41/#42 here. | No saved-search persistence. No complementary-intent matching (A1). No Strategy fields (#41). No contact-on-accept (#42). | ✅ |
| 9 | **Cost / spend** — PoC **$0**; no IdP/vault provision. | Local SQLite only. No AWS/IdP provision. No new infrastructure. | ✅ |
| 10 | **Evidence + handshake** — Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms. | `DiscoverySearchTests.cs` with 25 tests covers all security points. `ArtifactRepository.SearchDiscoverableAsync()` applies `Where()` clause at DB level (query-plane filter). | ✅ |

---

## Implementation Files

| File | Purpose |
|------|---------|
| `src/Dealoware.Api/Endpoints/SearchEndpoints.cs` | Discovery search endpoint (`GET /search/artifacts`) |
| `src/Dealoware.Application/Artifacts/Dtos/DiscoverableArtifactResponse.cs` | Discovery-safe DTO (omits secrets) |
| `src/Dealoware.Application/Artifacts/Mapping/ArtifactMapper.cs` | `ToDiscoverableResponse()` mapper |
| `src/Dealoware.Domain/Artifacts/IArtifactRepository.cs` | `SearchDiscoverableAsync()` interface |
| `src/Dealoware.Infrastructure/Persistence/ArtifactRepository.cs` | Search implementation (query-plane filter) |
| `tests/Dealoware.Api.Tests/DiscoverySearchTests.cs` | Security test suite |

---

## Test Matrix (Spec §7.1 style) — 25 tests passing

| Case | Expect | Test Method |
|------|--------|-------------|
| Unauth caller | 401 Unauthorized | `Search_Unauth_Returns401()` |
| Invalid JWT auth | 401 Unauthorized | `Search_InvalidAuth_Returns401()` |
| Invalid API key auth | 401 Unauthorized | `Search_InvalidApiKey_Returns401()` |
| Unauth error hygiene | No private fields in 401 body | `Search_Unauth_NoPrivateFieldsInError()` |
| Auth search | Returns discoverable fields only | `Search_Auth_ReturnsDiscoverableFieldsOnly()` |
| No LoginEmail in results | Absent from response | `Search_Results_NoLoginEmail()` |
| No ContactEmail in results | Absent from response | `Search_Results_NoContactEmail()` |
| No StrategyBody in results | Absent from response | `Search_Results_NoStrategyBody()` |
| No OwnerParticipantId in results | Absent from response | `Search_Results_NoOwnerParticipantId()` |
| No auth secrets in results | No apiKey/dlw_/password | `Search_Results_NoAuthSecrets()` |
| Discovery ≠ owner inventory | Different endpoints, different scopes | `Search_IsSeparateFromOwnerInventory()` |
| Can discover others' artifacts | Search returns artifacts from other owners | `Search_CanDiscoverOthersArtifacts()` |
| No inventory dump | Unrelated query doesn't return private items | `Search_DoesNotDumpPrivateInventory()` |
| Empty query | 400 Bad Request | `Search_EmptyQuery_Returns400()` |
| Missing query | 400 Bad Request | `Search_MissingQuery_Returns400()` |
| Whitespace query | 400 Bad Request | `Search_WhitespaceQuery_Returns400()` |
| Query-plane filter | Results match query, not all artifacts | `Search_QueryPlaneFilter_MatchesQuery()` |
| No match returns empty | Empty results, not error | `Search_NoMatch_ReturnsEmptyResults()` |
| Search by intent | Intent field searchable | `Search_ByIntent_Works()` |
| Search by entity name | Entity name searchable | `Search_ByEntityName_Works()` |
| Search by description | Entity description searchable | `Search_ByEntityDescription_Works()` |
| Case insensitive | Lower/upper case both match | `Search_IsCaseInsensitive()` |
| Respects limit | Limit parameter honored | `Search_RespectsLimit()` |
| Default limit | Default is 50 | `Search_DefaultLimit_Is50()` |
| Health unchanged | No auth required for /health | `Health_StillNoAuthRequired()` |

---

## Explicit OUT (not in this PR)

| OUT | Note |
|-----|------|
| Saved search / market monitoring | V1 HOLD |
| A1 complementary-intent matching | V2 HOLD |
| Strategy ACL / CRUD | #41 HOLD |
| Contact on accept | #42 HOLD |
| Stage C agent/tool hard wall | HOLD |
| Gate #25 unlock | After Stage B delivery |
| Cognito/SSO | Out |
| MotorMarket/DC4 | Out |

---

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
