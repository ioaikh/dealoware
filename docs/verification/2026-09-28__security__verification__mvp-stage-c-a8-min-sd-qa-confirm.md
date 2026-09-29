# Security QA — MVP Stage C #68 A8-minimum per-Participant meters + hard budgets SD vs Chief Security SD checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Asked by:** Senior Security / Chief Security — SD review handshake (PRIORITY; Chief Security ordered qa-confirm NOW)  
**Chief checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-checklist.md` (10 points) — **SD checklist only** (not PR-body renumber; not a8-meters; not Dev Plan checklist)  
**SoR checklist twin (GitHub):** `docs/verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-checklist.md` (checklist SoR PR **#77** CLEAR; tip base `7043314`) — Soft Soft CLOSE Soft HOLD SoR → Docs later; do **not** claim Docs SoR unlock  
**Senior Security done-list:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-points-review.md` (**PASS** 10/10) — cited; independent re-score **agrees**  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-qa-confirm.md`  
**Format ref:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-sd-qa-confirm.md` · `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/87 · OPEN  
**HEAD:** `38533a211070d9e1831335cc345f4530e9e07b6e` (verified via `get_pull_request` / `list_check_runs_for_ref`; short `38533a2`)  
**Base tip main (PR base):** `70433141d725b8fbf0177d81caf0f7436b684d20` (short `7043314`; SoR checklist twin CLEAR PR #77)  
**CI:** Build & Test **SUCCESS** (completed ~2026-09-28 21:02 ET) · `mergeable_state` **clean** / MERGEABLE  
**Tests:** `tests/Dealoware.Api.Tests/StageCBudgetMeterTests.cs` (29 Facts; CI green — no live `dotnet test` on this box; static `gh` review @ HEAD)  
**Issue:** https://github.com/ioaikh/dealoware/issues/68 · A8-minimum meters + hard budgets (cutoff)  
**Parent:** #18 · Stage C · roadmap A8 Option 1 — framing-only; does **not** Field-capture #68  
**Siblings:** #66 (primary metered consumer) · #67 (metered path still wall-bound — **mandatory bind**) · #69 — **#69/#18 not scored / not confirmed here**  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-qa-confirm.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD Dev Code QA until this Security QA PASS. Soft Soft CLOSE Soft HOLD SoR → Docs later (no invent Docs SoR unlock). Soft HOLD #69/#18. Gate **#26** backlog until Stage C delivery; Gate **#27** HOLD. PoC **$0**; no Cognito/MM/DC4/vault invent. Keep #66/#67/#69 separate SD. Path MUST be **a8-min** (not a8-meters).

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security SD checklist (KB) | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-checklist.md` | Binding 10 points |
| SoR checklist twin (GitHub) | `docs/verification/…-a8-min-sd-checklist.md` (PR #77 CLEAR; base `7043314`) | Present — Soft Soft CLOSE Soft HOLD SoR → Docs later |
| Senior Security points-review | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-points-review.md` | **PASS** 10/10 — cited; agrees |
| PR #87 | `ioaikh/dealoware` @ `38533a2…` | 18 files; +1396/−1; Budget domain/API/infra + Assistant meter hook + Auth provision + StageCBudgetMeterTests |
| CI | `list_check_runs_for_ref` @ HEAD | Build & Test **SUCCESS**; mergeable_state **clean** |
| Spec / Dev Plan Security PASS | `…a8-min-spec-qa-confirm.md` / `…a8-min-devplan-qa-confirm.md` | Upstream unlock context |
| #67 bind spot-check | `AssistantService` ctor `IAgentGateway` + budget check before `_gateway.InvokeToolAsync` | Mandatory wall bind held |
| Budget status leak spot-check | `BudgetStatus` / `BudgetStatusResponse` 5 fields only; no FieldClass | Held |

## Independent re-score (Security QA)

Score vs **official SD checklist 1–10** (not PR-body renumber; not a8-meters). Static `gh` review of HEAD files/tests — no live `dotnet test` on this box.

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Per-Participant meters — counters scoped to Participant | **MET** | `ParticipantBudget.Create(participantSub, limitUnits)` keys counters on `ParticipantSub`; `BudgetService.CheckBudgetAsync` / `RecordUsageAsync` / `GetBudgetStatusAsync` resolve only via `IBudgetRepository.GetByParticipantSubAsync(ownerSub)`; register auto-provisions via `EnsureBudgetExistsAsync` in `AuthEndpoints.Register`. DI: `IBudgetRepository`/`IBudgetService` in `DependencyInjection.AddInfrastructure`. Tests: `ParticipantBudget_Create_ScopedToParticipant`, `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`, `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`, `Register_ProvisionsBudget`, `Assistant_Invoke_UnderBudget_DecrementsUsage`. |
| 2 | Hard cutoff fail-closed — deny at/over; NOT soft-warn-only | **MET** | `ParticipantBudget.IsExhausted` (`UsedUnits >= LimitUnits`); `TryConsume` returns **false** at/over (no soft allow); `BudgetService.CheckBudgetAsync` → `BudgetCheckResult.Exhausted()` (`BUDGET_EXHAUSTED`); `AssistantService.InvokeAsync` checks budget **before** `_gateway.InvokeToolAsync` and returns `AssistantResult.BudgetExhausted()` — deny server-side, not warn-only. Tests: `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit`, `ParticipantBudget_TryConsume_ReturnsFalse_WhenExhausted`, `ParticipantBudget_TryConsume_ReturnsFalse_WhenOverLimit`, `ParticipantBudget_IsExhausted_WhenAtLimit`, `BudgetCheckResult_Exhausted_HardCutoff_NotSoftWarnOnly`. |
| 3 | Cross-tenant / unauth cannot burn budget | **MET** | Unauth `ownerSub` null/empty → `BudgetCheckResult.Unauthenticated()` (no consume); each budget keyed by `ParticipantSub` — `RecordUsageAsync("participant-a")` does not touch B; `GetBudgetStatusAsync` returns only caller's own row. HTTP: stranger invoke burns only stranger's meter. Tests: `Assistant_CrossTenant_CannotBurnOthersBudget`, `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`, `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`, `BudgetService_CheckBudget_Unauthenticated_ReturnsUnauthenticated`, `Budget_Status_Unauthenticated_Returns401`. |
| 4 | Metered path still wall-bound (#67) — budget status ≠ leak/escalation | **MET** | `AssistantService` still **requires** `IAgentGateway` (ctor null-throws); tool I/O only via `_gateway.InvokeToolAsync` / `IsToolAvailable` after budget allow — #67 bind held. `BudgetStatus` / `BudgetStatusResponse` expose only `LimitUnits`, `UsedUnits`, `RemainingUnits`, `IsExhausted`, `UsagePercentage` — **no** FieldClass / LoginEmail / StrategyBody. `BudgetCheckResult` error strings are uniform codes (`BUDGET_EXHAUSTED`, `NO_BUDGET`, `UNAUTHENTICATED`, `ACCESS_DENIED`) with no private fields. Tests: `BudgetStatus_NoFieldClassLeakage`, `BudgetCheckResult_ErrorMessages_NoPrivateLeak`, `Budget_Status_NoPrivateFieldsInResponse`, `BudgetCheckResult_Exhausted_NoPrivateLeak`. |
| 5 | Authn fail-closed on meter APIs — 401/403/404; no private leak | **MET** | `BudgetEndpoints.GetBudgetStatus`: `AuthHelper.GetAuthenticatedSub` → null/empty → `Results.Unauthorized()` (**401**); budget missing → `Results.NotFound` (**404** uniform deny). Service: null/empty → `Unauthenticated()` / null status. Invalid ApiKey / JWT → 401. Error bodies checked for no LoginEmail/StrategyBody. Tests: `Budget_Status_Unauthenticated_Returns401`, `Budget_Status_InvalidAuth_Returns401`, `Budget_Status_InvalidJwt_Returns401`, `Budget_Status_Unauthenticated_NoPrivateFieldsInError`, `BudgetService_CheckBudget_EmptyOwner_ReturnsUnauthenticated`, `BudgetService_GetBudgetStatus_Unauthenticated_ReturnsNull`. |
| 6 | OUT locked A8-minimum only — mature→V3; no full billing invent | **MET** | PR #87 file set = Budget domain/API/infra + Assistant meter hook + Auth provision + `StageCBudgetMeterTests` (18 files; +1396/−1) — simple counters only (limit/used/remaining). OUT comments name mature metering/analytics → V3; billing/escrow/settlement **not** implemented. No payment/settlement/analytics product classes. Soft O7 not invented as second product. |
| 7 | Sibling surfaces — no #69 owner-admin invent; keep #66/#67/#69 separate | **MET** | No owner-admin UI/suite; only `GET /budget/status` minimal status DTO (5 fields). #66/#67 appear as **cross-ref** / mandatory wall bind in `AssistantService` + DI comments — no #69 product surface. PR does not add UI/bot owner-admin. Tests exercise meter path only (no #69 invent). |
| 8 | No 5th Story / Gate HOLDs — #26 backlog; #27 HOLD; no Cognito/MM/DC4/vault | **MET** | Domain Budget path: no Cognito/IdP/vault/LLM/MCP packages. Infra DI: EF/Sqlite + JWT + existing gateway + Budget scoped services only. Soft OTel/audit = comment weave only ("no 5th Story"); no OTel product Story. Cognito/MM/DC4/vault invent absent from diff (OUT-comment only). Gate #26 backlog / #27 HOLD held in constraints. Health untouched: `Health_StillNoAuthRequired_AfterBudgetFeature`. |
| 9 | Cost / spend — PoC $0; LLM/API → COO→CEO | **MET** | No LLM provision; no AWS/external metered API client added; local SQLite meters only. PoC **$0**. Any future named LLM/API spend → COO → CEO (constraints). |
| 10 | Evidence + handshake — Soft HOLD Dev Code QA until Security QA | **MET** | This confirm + Senior done-list cite paths/tests for 1–9 at PR #87 HEAD `38533a211070d9e1831335cc345f4530e9e07b6e`. **Soft Soft CLOSE Soft HOLD Dev Code QA / Product QA until this Security QA PASS.** Soft Soft CLOSE Soft HOLD SoR → Docs later (no invent here). CI Build & Test **SUCCESS** @ HEAD (completed ~2026-09-28 21:02 ET). |

## Soft notes (non-blocking)

- **Senior SD-step points-review present** (`…a8-min-sd-points-review.md` — **PASS** 10/10). Security QA cites and **agrees** on all 10 MET.
- **CI SUCCESS** — Build & Test completed success on HEAD `38533a2…` (~2026-09-28 21:02 ET). `mergeable_state` **clean** / MERGEABLE. Reported accurately.
- `IBudgetService?` is optional on `AssistantService` ctor for test/compat; production DI always registers `IBudgetService` → hard cutoff on metered path. Prefer non-optional in a follow-up if stronger wiring guarantee wanted (agrees Senior).
- Pre-check `CheckBudgetAsync` + post-success `RecordUsageAsync` is not a single atomic `TryConsume` under concurrency — acceptable for A8-min PoC; race overshoot is soft residual, not soft-warn-only control (agrees Senior).
- Test name `Budget_Status_CrossTenant_Returns404` asserts each principal sees **own** 200 status (isolation), not HTTP 404 against another's id — naming soft; isolation evidence still MET via service + Assistant cross-tenant tests (agrees Senior).
- No end-to-end HTTP Assistant exhaustion case in the 29 tests; hard cutoff is proven at domain/service + AssistantResult codes — soft residual for Product QA matrix (agrees Senior).
- Soft Soft CLOSE Soft HOLD SoR → Docs later — tip checklist SoR at PR base `7043314` / PR #77 noted; do **not** invent handshake SoR / claim Docs SoR unlock.
- Soft HOLD #69/#18; Gate #26 backlog; #27 HOLD.
- No live `dotnet test` on this box — static `gh` review @ HEAD `38533a2…` (CI SUCCESS is the suite evidence).
- **#66 / #67** already PASSed separately (thin-assistant / hardwall SD qa-confirm) — bind/#66 consumer only here; **#69 / parent #18** not reviewed / not confirmed in this document.
- Gate **#26** backlog until Stage C delivery; Gate **#27** HOLD; PoC **$0**. Soft HOLD merge / Soft HOLD siblings per Chief Developer (no merge from Security QA). Path is **a8-min** (not a8-meters).

## Alignment with Senior review

Senior Security done-list (`verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-points-review.md`) scored all 10 **MET** with matching PR #87 / ParticipantBudget / BudgetService / BudgetEndpoints / AssistantService meter hook / DI / StageCBudgetMeterTests / #67 gateway-bind / BudgetStatus no-FieldClass-leak cites at HEAD `38533a2…`. Independent Security QA re-score **agrees** — no gaps; soft notes complement (optional `IBudgetService?`; concurrency race soft residual; CrossTenant_Returns404 naming soft; no e2e HTTP exhaustion soft; Soft Soft CLOSE Soft HOLD SoR → Docs later; Soft HOLD #69/#18). No contradiction.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Per-Participant meters; counters keyed by ParticipantSub | Held (`ParticipantBudget.Create` + scoped repo/service tests) |
| Hard cutoff fail-closed; NOT soft-warn-only | Held (`IsExhausted` / `TryConsume` false / `BUDGET_EXHAUSTED` + Assistant pre-gateway deny) |
| Cross-tenant / unauth cannot burn budget | Held (service isolation + Assistant_CrossTenant_* + 401 unauth tests) |
| Metered path #67 wall-bound; BudgetStatus no FieldClass leak | Held (`IAgentGateway` ctor + InvokeToolAsync after budget allow + BudgetStatus_* / NoPrivateLeak tests) |
| Authn fail-closed on meter APIs; 401/404; no private leak | Held (BudgetEndpoints 401 + NotFound + InvalidAuth/Jwt tests) |
| OUT locked A8-minimum; mature metering/billing → V3 | Held (18-file #68-only counters; OUT comments; no billing classes) |
| No #69 owner-admin invent; keep #66/#67/#69 separate | Held (GET /budget/status 5-field only; no UI/bot) |
| Gate #26 backlog; #27 HOLD; no Cognito/MM/DC4/vault; no 5th Story | Held (DI/csproj clean; Soft OTel weave comment only) |
| PoC $0; LLM spend → COO→CEO | Held |
| Soft Soft CLOSE Soft HOLD Dev Code QA until this PASS; Soft Soft CLOSE Soft HOLD SoR → Docs later | Held |
| Path a8-min (not a8-meters) | Held (DOC-FLOW filename) |

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are non-blocking.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Soft Soft CLOSE Soft HOLD Dev Code QA / Product QA may PASS on Security gate **after** this confirm. Soft Soft CLOSE Soft HOLD SoR → Docs later — do **not** invent Docs SoR unlock. Cost/critical: none. PoC **$0**. Do **not** open Gate #26. Do **not** treat this as #66 / #67 re-confirm / #69 / parent #18 confirm. Gate **#27** HOLD. Soft HOLD #69/#18. Soft HOLD merge / Soft HOLD siblings per Chief Developer — Security QA does **not** merge.
