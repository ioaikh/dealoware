# Verification — Security points vs MVP Stage C #68 A8-minimum per-Participant meters + hard budgets SD

**Author:** Dealoware Senior Security  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-checklist.md`  
**SoR checklist twin:** `docs/verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-checklist.md` (PR #77 CLEAR; tip base `7043314`)  
**PR:** https://github.com/ioaikh/dealoware/pull/87 · OPEN · HEAD `38533a211070d9e1831335cc345f4530e9e07b6e`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-qa-confirm.md`  
**Tests:** `tests/Dealoware.Api.Tests/StageCBudgetMeterTests.cs` (29 new; CI Build & Test **success** on HEAD)  
**Issue:** https://github.com/ioaikh/dealoware/issues/68  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-points-review.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD Dev Code QA until Security QA · Soft Soft CLOSE Soft HOLD SoR → Docs later (no handshake SoR invent) · Soft HOLD #69/#18 · Gate **#26** backlog until Stage C delivery · Gate **#27** HOLD · PoC **$0** · no Cognito/MM/DC4/vault invent · keep #66/#67/#69 separate SD · no Gate #26 unlock invent

## Scope note

SD implements Spec + Dev Plan Security for **A8-minimum** per-Participant meters + **hard cutoff** fail-closed (**not** soft-warn-only). Primary consumer #66; metered path still wall-bound (#67). Scored on code/tests at PR #87 HEAD — not PR-body trust alone. Reject soft-warn-only as sole control; reject mature billing / owner-admin / Cognito invent. Soft Soft CLOSE Soft HOLD Dev Code QA until Security QA.

## Checklist vs PR (official 1–10; not PR-body renumber)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Per-Participant meters — counters scoped to Participant | **MET** | `ParticipantBudget.Create(participantSub, limitUnits)` keys counters on `ParticipantSub`; `BudgetService.CheckBudgetAsync` / `RecordUsageAsync` / `GetBudgetStatusAsync` resolve only via `IBudgetRepository.GetByParticipantSubAsync(ownerSub)`; register auto-provisions via `EnsureBudgetExistsAsync(participant.Sub, 1000)` in `AuthEndpoints.Register`. DI: `IBudgetRepository`/`IBudgetService` in `DependencyInjection.AddInfrastructure`. Tests: `ParticipantBudget_Create_ScopedToParticipant`, `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`, `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`, `Register_ProvisionsBudget`, `Assistant_Invoke_UnderBudget_DecrementsUsage`. |
| 2 | Hard cutoff fail-closed — deny at/over; NOT soft-warn-only | **MET** | `ParticipantBudget.IsExhausted` (`UsedUnits >= LimitUnits`); `TryConsume` returns **false** at/over (no soft allow); `BudgetService.CheckBudgetAsync` → `BudgetCheckResult.Exhausted()` (`BUDGET_EXHAUSTED`); `AssistantService.InvokeAsync` checks budget **before** `_gateway.InvokeToolAsync` and returns `AssistantResult.BudgetExhausted()` — deny server-side, not warn-only. Tests: `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit`, `ParticipantBudget_TryConsume_ReturnsFalse_WhenExhausted`, `ParticipantBudget_TryConsume_ReturnsFalse_WhenOverLimit`, `ParticipantBudget_IsExhausted_WhenAtLimit`, `BudgetCheckResult_Exhausted_HardCutoff_NotSoftWarnOnly`. |
| 3 | Cross-tenant / unauth cannot burn budget | **MET** | Unauth `ownerSub` null/empty → `BudgetCheckResult.Unauthenticated()` (no consume); each budget keyed by `ParticipantSub` — `RecordUsageAsync("participant-a")` does not touch B; `GetBudgetStatusAsync` returns only caller's own row (null if none). HTTP: stranger invoke burns only stranger's meter. Tests: `Assistant_CrossTenant_CannotBurnOthersBudget`, `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`, `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`, `BudgetService_CheckBudget_Unauthenticated_ReturnsUnauthenticated`, `Budget_Status_Unauthenticated_Returns401`. |
| 4 | Metered path still wall-bound (#67) — budget status ≠ leak/escalation | **MET** | `AssistantService` still **requires** `IAgentGateway` (ctor null-throws); tool I/O only via `_gateway.InvokeToolAsync` / `IsToolAvailable` after budget allow — #67 bind held. `BudgetStatus` / `BudgetStatusResponse` expose only `LimitUnits`, `UsedUnits`, `RemainingUnits`, `IsExhausted`, `UsagePercentage` — **no** FieldClass / LoginEmail / StrategyBody. `BudgetCheckResult` error strings are uniform codes (`BUDGET_EXHAUSTED`, `NO_BUDGET`, `UNAUTHENTICATED`, `ACCESS_DENIED`) with no private fields. Tests: `BudgetStatus_NoFieldClassLeakage`, `BudgetCheckResult_ErrorMessages_NoPrivateLeak`, `Budget_Status_NoPrivateFieldsInResponse`, `BudgetCheckResult_Exhausted_NoPrivateLeak`. |
| 5 | Authn fail-closed on meter APIs — 401/403/404; no private leak | **MET** | `BudgetEndpoints.GetBudgetStatus`: `AuthHelper.GetAuthenticatedSub` → null/empty → `Results.Unauthorized()` (**401**); budget missing → `Results.NotFound` (**404** uniform deny). Service: null/empty → `Unauthenticated()` / null status. Invalid ApiKey / JWT → 401. Error bodies checked for no LoginEmail/StrategyBody. Tests: `Budget_Status_Unauthenticated_Returns401`, `Budget_Status_InvalidAuth_Returns401`, `Budget_Status_InvalidJwt_Returns401`, `Budget_Status_Unauthenticated_NoPrivateFieldsInError`, `BudgetService_CheckBudget_EmptyOwner_ReturnsUnauthenticated`, `BudgetService_GetBudgetStatus_Unauthenticated_ReturnsNull`. |
| 6 | OUT locked A8-minimum only — mature→V3; no full billing invent | **MET** | PR #87 file set = Budget domain/API/infra + Assistant meter hook + Auth provision + `StageCBudgetMeterTests` (18 files) — simple counters only (limit/used/remaining). OUT comments name mature metering/analytics → V3; billing/escrow/settlement **not** implemented (grep hits only in OUT comments). No payment/settlement/analytics product classes. Soft O7 not invented as second product. |
| 7 | Sibling surfaces — no #69 owner-admin invent; keep #66/#67/#69 separate | **MET** | No owner-admin UI/suite; only `GET /budget/status` minimal status DTO (5 fields). #66/#67 appear as **cross-ref** / mandatory wall bind in `AssistantService` + DI comments — no #69 product surface. PR does not add UI/bot owner-admin. Tests region documents "no #69 invent here". |
| 8 | No 5th Story / Gate HOLDs — #26 backlog; #27 HOLD; no Cognito/MM/DC4/vault | **MET** | Domain csproj: **no** PackageReferences. Infra csproj: EF/Sqlite + JWT only (no Cognito/IdP/vault/LLM/MCP). Soft OTel/audit = comment weave only ("no 5th Story"); no OTel product Story. Cognito/MM/DC4/vault invent absent from diff (OUT-comment only for billing/#69). Gate #26 backlog / #27 HOLD held in constraints. Health untouched: `Health_StillNoAuthRequired_AfterBudgetFeature`. |
| 9 | Cost / spend — PoC $0; LLM/API → COO→CEO | **MET** | No LLM provision; no AWS/external metered API client added; local SQLite meters only. PoC **$0**. Any future named LLM/API spend → COO → CEO (constraints). |
| 10 | Evidence + handshake — Soft HOLD Dev Code QA until Security QA | **MET** | This done-list cites paths/tests for 1–9 at PR #87 HEAD `38533a211070d9e1831335cc345f4530e9e07b6e`; PR body Soft HOLD Dev Code QA until Security QA; Soft Soft CLOSE Soft HOLD SoR → Docs later (no invent here). CI Build & Test **success** @ HEAD (completed ~2026-09-28 21:02 ET). |

## Soft notes (non-blocking)

- `IBudgetService?` is optional on `AssistantService` ctor for test/compat; production DI always registers `IBudgetService` → hard cutoff on metered path. Prefer non-optional in a follow-up if Security QA wants stronger wiring guarantee.
- Pre-check `CheckBudgetAsync` + post-success `RecordUsageAsync` is not a single atomic `TryConsume` under concurrency — acceptable for A8-min PoC; race overshoot is soft residual, not soft-warn-only control.
- Test name `Budget_Status_CrossTenant_Returns404` asserts each principal sees **own** 200 status (isolation), not HTTP 404 against another's id — naming soft; isolation evidence still MET via service + Assistant cross-tenant tests.
- No end-to-end HTTP Assistant exhaustion case in the 29 tests; hard cutoff is proven at domain/service + AssistantResult codes — soft residual for Product QA matrix.
- Soft Soft CLOSE Soft HOLD SoR → Docs later — do **not** invent handshake SoR in this file.
- Soft HOLD #69/#18; Gate #26 backlog; #27 HOLD.

## Gaps

**None.**

## Done-list (for Security QA)

- [x] DOC-FLOW filed; Spec + Dev Plan Security PASS cited
- [x] 10/10 with code/test cites at PR #87 HEAD `38533a211070d9e1831335cc345f4530e9e07b6e`
- [x] Hard cutoff (IsExhausted / TryConsume false / BUDGET_EXHAUSTED) verified — not soft-warn-only
- [x] Assistant still requires IAgentGateway (#67); BudgetStatus has no FieldClass/private leak
- [x] No #69/#billing/Cognito invent; Gate #26 backlog; #27 HOLD; PoC $0
- [x] CI Build & Test **success** on HEAD
- [ ] Soft HOLD Dev Code QA / Product QA until Security QA confirms
- [ ] → Security QA confirm **PASS** → Chief Security
- [ ] Soft Soft CLOSE Soft HOLD SoR → Docs later (no invent)

## Cost/critical

None. PoC **$0**. Any named LLM/API/IdP spend → COO → CEO.
