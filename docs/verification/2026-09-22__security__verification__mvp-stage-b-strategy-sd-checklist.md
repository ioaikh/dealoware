# Security checklist — SD · MVP Stage B #41 Minimal Strategy create/edit (P3)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). **SD CLOSED — Security + Code QA PASS.**  
**Date:** 2026-09-22  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/41 · Minimal Strategy create/edit (P3 partial)  
**Parent:** #18 · Option A · Stage B named slice  
**Siblings:** #40 · #42 — keep separate SD; cross-ref only  
**Plan:** `plans/2026-09-22__devplan__plan__mvp-stage-b-minimal-strategy-crud.md`  
**Dev Plan Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-devplan-qa-confirm.md` (SoR PR #47)  
**Spec Security PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-spec-qa-confirm.md` (SoR PR #44)  
**Hold:** Gate **#25** backlog. Stage C Assistant (#26) + #27 + #18 Spec/SD (whole) HOLD. Product QA HOLD until SD-step Security PASS. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-sd-checklist.md`

## Scope note
SD must **implement** Spec + Dev Plan Security for minimal Strategy CRUD + **StrategyBody** FieldClass ACL (Owner+OwnAgent only). Real points — not N/A. No Assistant runtime. Evidence via tests/done-list.

---

## Itemized security points (SD must satisfy)

| # | Security Point | Requirement | Status | Evidence |
|---|----------------|-------------|--------|----------|
| 1 | **Owner-scoped query-plane** | create/edit/get/list-own bound to owning Participant on query plane; IDOR fail-closed | **MET** | `StrategyRepository.GetByIdForOwnerAsync()` uses `WHERE Id = @id AND OwnerParticipantId = @ownerParticipantId`; `GetByOwnerAsync()` uses `WHERE OwnerParticipantId = @ownerParticipantId`; Tests: `Strategy_GetByIdForOwner_QueryPlaneFilter_NotFetchThenFilter` |
| 2 | **StrategyBody FieldClass ACL** | `IFieldPolicy` rows: User R/W; OwnAgent R/W for owner; Counterparty/Stranger/Unauth Deny | **MET** | `FieldClass.StrategyBody` added; `FieldPolicy.EvaluateStrategyBody()` implements matrix; Tests: `FieldPolicy_StrategyBody_*` (User OK, OwnAgent OK, Counterparty Deny, Stranger Deny, Unauth Deny) |
| 3 | **Never-to-counterparty** | Negotiation DTOs never expose StrategyBody / private Strategy fields | **MET** | `NegotiationMapper` has no Strategy references; Tests: `Negotiation_Response_NeverExposesStrategyBody`, `NegotiationList_Response_NeverExposesStrategyBody` |
| 4 | **Authn fail-closed** | Unauth **401**; wrong principal **403**/**404**; uniform deny bodies; no private leakage | **MET** | All endpoints check `AuthHelper.GetAuthenticatedSub()` first; 404 responses are empty/minimal; Tests: `Strategy_*_UnauthDeny_Returns401`, `Strategy_Get_InvalidAuth_Returns401`, `Strategy_DenyError_NoPrivateFields` |
| 5 | **OwnAgent = API policy only** | OwnAgent StrategyBody R/W is API policy evidence only — **no** Assistant / tool runtime delivery | **MET** | `FieldPolicy.EvaluateStrategyBody()` allows OwnAgent R/W; no Assistant code; Tests: `FieldPolicy_StrategyBody_OwnAgentOK_ReadWrite` |
| 6 | **Consume #31, don't rewrite** | Add StrategyBody enforcement on #31 registry; keep #40/#42 separate | **MET** | Added `FieldClass.StrategyBody` and `FieldPolicy.EvaluateStrategyBody()` to existing #31 implementation; no #40/#42 code |
| 7 | **No Stage C / Assistant inventing** | No thin/full Assistant, #26 hard wall, Cognito, MM/DC4 | **MET** | Code contains only opaque/text StrategyBody with no evaluation logic; OUT notes in `Strategy.cs` and `StrategyEndpoints.cs` |
| 8 | **OUT locked** | P3 minimal; free-form → V1; A5 sandbox → V4; X1 Assistant → Stage C; gate #25 backlog | **MET** | Minimal CRUD only; no condition engine; no sandbox; no Assistant |
| 9 | **Cost / spend** | PoC **$0**; no IdP/vault provision | **MET** | No cloud resources added; Test: `Health_StillNoAuthRequired` |
| 10 | **Evidence + handshake** | Done-list cites paths/tests for 1–9; Code/Product QA must **not** PASS until Security QA confirms | **MET** | This checklist + PR #51 evidence table; 33 new tests all passing |

---

## Implementation Summary

### Domain Layer (`Dealoware.Domain`)
- `Strategy` entity with `OwnerParticipantId` for query-plane ownership
- `IStrategyRepository` interface with owner-scoped query methods
- `FieldClass.StrategyBody` registered in FieldClass registry
- `FieldPolicy.EvaluateStrategyBody()` policy rows added

### Infrastructure Layer (`Dealoware.Infrastructure`)
- `StrategyRepository` with query-plane authorization
- `StrategyEntityConfiguration` with indexes on `OwnerParticipantId`
- `DealowareDbContext` updated with `Strategies` DbSet
- `DependencyInjection` registers `IStrategyRepository`

### Application Layer (`Dealoware.Application`)
- `CreateStrategyRequest`, `UpdateStrategyRequest`, `StrategyResponse` DTOs
- `StrategyMapper` with field ACL projection

### API Layer (`Dealoware.Api`)
- `StrategyEndpoints`: POST/GET/PUT/PATCH/DELETE `/strategies`
- All endpoints enforce owner-scope at query plane
- 401 for unauth, 404 for non-owner (no info leak)

### Test Coverage (`Dealoware.Api.Tests`)
- `StrategyCrudTests` with 33 tests covering:
  - Owner CRUD OK
  - Stranger/IDOR deny (404)
  - Unauth deny (401)
  - StrategyBody ACL matrix
  - Negotiation DTO isolation
  - Query-plane verification
  - Error hygiene (no private fields)

---

## OUT of Scope (Stage B HOLD)

| Item | Stage/Version |
|------|---------------|
| Free-form condition evaluation engine | V1 |
| Strategy sandbox (A5) | V4 |
| Assistant runtime (thin or full) | Stage C / X1 |
| #40 Instant search | Stage B sibling |
| #42 Contact on accept | Stage B sibling |
| Cognito/SSO | Out |
| Vault/KMS | Out |
| MotorMarket/DC4 | Out |

---

## Files Changed

```
src/Dealoware.Domain/Strategies/Strategy.cs (NEW)
src/Dealoware.Domain/Strategies/IStrategyRepository.cs (NEW)
src/Dealoware.Domain/FieldAcl/FieldClass.cs (MODIFIED - added StrategyBody)
src/Dealoware.Domain/FieldAcl/FieldPolicy.cs (MODIFIED - added StrategyBody policy)
src/Dealoware.Infrastructure/Persistence/StrategyRepository.cs (NEW)
src/Dealoware.Infrastructure/Persistence/StrategyEntityConfiguration.cs (NEW)
src/Dealoware.Infrastructure/Persistence/DealowareDbContext.cs (MODIFIED)
src/Dealoware.Infrastructure/DependencyInjection.cs (MODIFIED)
src/Dealoware.Application/Strategies/Dtos/CreateStrategyRequest.cs (NEW)
src/Dealoware.Application/Strategies/Dtos/UpdateStrategyRequest.cs (NEW)
src/Dealoware.Application/Strategies/Dtos/StrategyResponse.cs (NEW)
src/Dealoware.Application/Strategies/Mapping/StrategyMapper.cs (NEW)
src/Dealoware.Api/Endpoints/StrategyEndpoints.cs (NEW)
src/Dealoware.Api/Program.cs (MODIFIED)
tests/Dealoware.Api.Tests/StrategyCrudTests.cs (NEW)
docs/verification/2026-09-22__security__verification__mvp-stage-b-strategy-sd-checklist.md (MODIFIED)
```

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
