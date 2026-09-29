# Verification — Security points vs MVP Stage C #68 A8-minimum per-Participant meters + hard budgets Product QA

**Author:** Dealoware Senior Security  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 Product-QA-step Security points MET — pt 10 closes via this done-list → Security QA confirm)  
**Checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` (10 points)  
**SoR twin (checklist):** Soft Soft CLOSE Soft HOLD SoR twin `docs/verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` — **CLEAR** PR **#101** @ `2762b95838e6e0292a9ba43f5ce118f4dced3cb8` (HTTP 200). Live tip `02e5775` (docs Soft Soft CLOSE Soft HOLD SoR ahead of impl). Checklist **ISSUED** on KB.  
**Product QA report:** `qa/2026-09-28__qa__qa-report__mvp-stage-c-a8-min-meters-budgets.md` (HOLD PASS; Sec 1–9 EVIDENCED; pt 10 HOLD)  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/87 · **MERGED** @ `c5485cfdd1c1f35a0af32c792fb588935f606ddf`  
**PR head cited:** `c2768c1546b862d509c883af1e1fb2e875f3f9e6`  
**CI (impl @ c5485cf):** https://github.com/ioaikh/dealoware/actions/runs/36506996510 — **SUCCESS** (verified `list_check_runs_for_ref`)  
**CI (PR head @ c2768c1):** https://github.com/ioaikh/dealoware/actions/runs/36506810520 — **SUCCESS** (verified `list_check_runs_for_ref`)  
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-qa-confirm.md` (PASS 10/10; SoR twin PR **#93** @ `0b40cab`)  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-qa-confirm.md`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-devplan-qa-confirm.md`  
**SD checklist (ref):** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-checklist.md`  
**Tests:** `tests/Dealoware.Api.Tests/StageCBudgetMeterTests.cs` (29 Facts; present @ `c5485cf`)  
**Issue:** https://github.com/ioaikh/dealoware/issues/68 · A8-minimum meters + hard budgets (cutoff)  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-points-review.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (after Security QA). Soft Soft CLOSE Soft HOLD status:done until CBA. Soft Soft CLOSE Soft HOLD SoR twin CLEAR PR **#101** @ `2762b95` (do **not** invent handshake). Soft Soft CLOSE Soft HOLD #69 SD separate (do **not** invent #69). Soft **#41** OUT via **#66+#67** (do not re-open). Gate **#26** backlog; Gate **#27** HOLD. Parent #18 framing-only — does **not** Field-capture #68. PoC **$0**; no Cognito/MM/DC4/vault invent.

## Scope note

Product QA verifies Spec + Plan + SD Security for **A8-minimum** per-Participant meters + **hard cutoff** fail-closed (**not** soft-warn-only). Primary consumer #66; metered path still wall-bound (#67). Scored vs official Product QA checklist 1–10 against Product QA HOLD PASS report **and** independent re-spot of code/tests at MERGED impl `c5485cf…` via `cursor-github` `get_file_contents` (not report trust alone; no clone). Scope **#68 ONLY** — do **not** re-score #67 wall; do **not** invent #69. Soft Soft CLOSE Soft HOLD Doc / status:done / SoR unlocks remain held.

## Checklist vs Product QA (official 1–10)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Per-Participant meters — counters scoped to Participant | **MET** | QA Sec #1 / AC1 + re-spot @ `c5485cf…`: `ParticipantBudget.Create(participantSub, limitUnits)` keys on `ParticipantSub`; `BudgetService.CheckBudgetAsync` / `RecordUsageAsync` / `GetBudgetStatusAsync` resolve only via `IBudgetRepository.GetByParticipantSubAsync(ownerSub)`; `AuthEndpoints.Register` → `EnsureBudgetExistsAsync(participant.Sub, 1000)`; DI `AddScoped<IBudgetRepository, BudgetRepository>` + `AddScoped<IBudgetService, BudgetService>`. Tests: `ParticipantBudget_Create_ScopedToParticipant`, `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`, `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`, `Register_ProvisionsBudget`, `Assistant_Invoke_UnderBudget_DecrementsUsage`, `Budget_Status_ReturnsValidStatus`. |
| 2 | Hard cutoff fail-closed — NOT soft-warn-only | **MET** | QA Sec #2 / AC2 + re-spot: `ParticipantBudget.IsExhausted` (`UsedUnits >= LimitUnits`); `TryConsume` returns **false** at/over; `BudgetService.CheckBudgetAsync` → `BudgetCheckResult.Exhausted()` (`BUDGET_EXHAUSTED`); `AssistantService.InvokeAsync` checks budget **before** `_gateway.InvokeToolAsync` and returns `AssistantResult.BudgetExhausted()` — deny server-side, not warn-only; `RecordUsageAsync` only after gateway success. Tests: `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit`, `ParticipantBudget_TryConsume_ReturnsFalse_WhenExhausted`, `ParticipantBudget_TryConsume_ReturnsFalse_WhenOverLimit`, `ParticipantBudget_IsExhausted_WhenAtLimit`, `BudgetCheckResult_Exhausted_HardCutoff_NotSoftWarnOnly`. Soft residual: optional `IBudgetService?` (see Soft notes). |
| 3 | Cross-tenant / unauth cannot burn budget | **MET** | QA Sec #3 / AC3 + re-spot: null/empty `ownerSub` → `BudgetCheckResult.Unauthenticated()` (no consume); each budget keyed by `ParticipantSub` — `RecordUsageAsync("participant-a")` does not touch B; `BudgetEndpoints` own-sub only via `AuthHelper.GetAuthenticatedSub` — no other-sub lookup API. HTTP: stranger invoke burns only stranger's meter. Tests: `Assistant_CrossTenant_CannotBurnOthersBudget`, `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`, `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`, `BudgetService_CheckBudget_Unauthenticated_ReturnsUnauthenticated`, `Budget_Status_Unauthenticated_Returns401`. |
| 4 | Metered path still wall-bound (#67) — do **NOT** re-score #67 | **MET** | QA Sec #4 + re-spot: `AssistantService` still **requires** `IAgentGateway` (ctor null-throws); meter check only inside tool path after `IsToolAvailable` / before `InvokeToolAsync`; `BudgetStatus` / `BudgetStatusResponse` expose only `LimitUnits`, `UsedUnits`, `RemainingUnits`, `IsExhausted`, `UsagePercentage` — **no** FieldClass / LoginEmail / StrategyBody. Bind only — #67 Product QA already PASSed separately. Tests: `BudgetStatus_NoFieldClassLeakage`, `BudgetCheckResult_ErrorMessages_NoPrivateLeak`, `Budget_Status_NoPrivateFieldsInResponse`, `BudgetCheckResult_Exhausted_NoPrivateLeak`. |
| 5 | Authn fail-closed on meter APIs / GET `/budget/status` | **MET** | QA Sec #5 + re-spot `BudgetEndpoints.GetBudgetStatus` @ `c5485cf…`: `AuthHelper.GetAuthenticatedSub` → null/empty → `Results.Unauthorized()` (**401**); budget missing → `Results.NotFound` (**404** uniform deny); own-only surface (no other-sub lookup). Invalid ApiKey / JWT → 401. Error bodies checked for no LoginEmail/StrategyBody. Tests: `Budget_Status_Unauthenticated_Returns401`, `Budget_Status_InvalidAuth_Returns401`, `Budget_Status_InvalidJwt_Returns401`, `Budget_Status_Unauthenticated_NoPrivateFieldsInError`, `BudgetService_CheckBudget_EmptyOwner_ReturnsUnauthenticated`, `BudgetService_GetBudgetStatus_Unauthenticated_ReturnsNull`. Soft: `Budget_Status_CrossTenant_Returns404` name vs own-only 200/200 — isolation still MET. |
| 6 | OUT locked A8-minimum only — mature → V3 | **MET** | QA Sec #6 / AC5 + re-spot: PR #87 = 18 files (+1396/−1) Budget domain/API/infra + Assistant meter hook + Auth provision + `StageCBudgetMeterTests` — simple counters only. OUT comments name mature metering/analytics → V3; billing/escrow/settlement **not** implemented. `GET /budget/status` min status only (5 fields). Soft O7 not invented as second product. |
| 7 | Sibling surfaces — no #69 invent; keep #66/#67/#69 separate | **MET** | QA Sec #7 + re-spot: no owner-admin UI/suite in #68 file set; only `GET /budget/status` minimal status DTO. #66/#67 appear as **cross-ref** / mandatory wall bind in `AssistantService` + DI comments — no #69 product surface. Soft Soft CLOSE Soft HOLD #69 SD separate (do not invent). |
| 8 | No 5th Story / Gate HOLDs; Soft #41 OUT via #66+#67 | **MET** | QA Sec #8 / AC6 + re-spot: Soft OTel/audit = comment weave only ("no 5th Story"); no Cognito/MM/DC4/vault invent in diff. Gate **#26** backlog; Gate **#27** HOLD held in constraints. Soft **#41** OUT via **#66+#67** (do **not** re-open / re-score). Health untouched: `Health_StillNoAuthRequired_AfterBudgetFeature`. |
| 9 | Cost / spend — PoC $0; spend → COO→CEO | **MET** | QA Sec #9 + re-spot scope: no LLM SDK/provision; no AWS/external metered API client; local SQLite meters only. PoC **$0**. Any named LLM/API spend → COO → CEO. CI SUCCESS both SHAs. |
| 10 | Handshake close — Product QA HOLD until Security QA; #18 not Field-capture | **MET** | Product QA correctly HOLDs PASS / pt 10 until Security QA `…a8-min-productqa-qa-confirm.md`. Prior SD Security PASS does **not** close Product-step pt 10. Parent #18 framing-only — does **not** Field-capture #68. This done-list correctly holds Soft HOLD until Security QA Product-step confirm. Soft Soft CLOSE Soft HOLD Doc / status:done / SoR remain held. |

## Soft notes (non-blocking; align Product QA + SD)

- No live `dotnet test` on evidence box — CI SUCCESS runs 36506996510 @ `c5485cf` and 36506810520 @ `c2768c1` + `StageCBudgetMeterTests.cs` ×**29** equivalent accepted.
- **`IBudgetService?` optional** — `AssistantService` ctor default `null` skips check/record when omitted; tip DI **does** register `IBudgetService` so production path wired. Soft residual OK (prefer required bind for fail-closed-by-construction later).
- **HTTP at/over-budget `/assistant/invoke` deny** — missing Fact; at/over covered by **domain** `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit` (+ entity TryConsume). Soft residual OK (domain exhaust proven). Register default **1000** makes HTTP exhaust costly without a low-limit fixture. Endpoint maps `BUDGET_EXHAUSTED` via default → **400 BadRequest** (still deny; not soft-warn).
- **`Budget_Status_CrossTenant_Returns404`** — name implies 404; body asserts both owner and stranger get **200** (each own budget). Own-only surface is OK; API has no other-sub lookup. Rename preferred; isolation evidence still MET.
- Strategy-only / no-tool invoke does not consume — meter check+record only when `ToolName` present; matches “metered Assistant/tool” lock.
- Pre-check `CheckBudgetAsync` + post-success `RecordUsageAsync` is not a single atomic `TryConsume` under concurrency — acceptable for A8-min PoC; race overshoot is soft residual, not soft-warn-only control.
- Soft Soft CLOSE Soft HOLD SoR Product QA checklist twin — checklist **ISSUED** on KB; Soft Soft CLOSE Soft HOLD SoR CLEAR PR **#101** @ `2762b95` (HTTP 200). Do **not** invent handshake Soft Soft CLOSE Soft HOLD SoR.
- Soft Soft CLOSE Soft HOLD #69 SD separate — do **not** invent #69.
- Soft **#41** OUT via **#66+#67** — do not re-open.
- Tip `c5485cf` is impl merge; live main may be ahead with docs Soft Soft CLOSE Soft HOLD SoR — Product verify held to **`c5485cf`**.
- Do **not** re-score #67 wall as this Story — bind evidence only.

## Gaps

**None.** Soft residuals (`IBudgetService?`; missing HTTP at/over invoke Fact; CrossTenant_Returns404 naming) accepted non-blockers.

## Done-list (for Security QA)

- [x] DOC-FLOW filed; Spec + Dev Plan + SD Security PASS cited
- [x] Score 10/10 vs Product QA checklist + Product QA HOLD PASS report + MERGED PR #87 @ `c5485cf…` (code/test re-spot: `ParticipantBudget`, `BudgetService`, `BudgetEndpoints`, `AssistantService`, `DependencyInjection`, `BudgetStatus`/`BudgetStatusResponse`, `StageCBudgetMeterTests` ×29)
- [x] Soft #41 OUT via #66+#67; Gate #26 backlog; #27 HOLD; parent #18 not Field-capture; #69 OUT (do not invent); PoC $0; no Cognito/MM/DC4
- [x] CI Build & Test **SUCCESS** on impl `c5485cf` and PR head `c2768c1`
- [x] Soft Soft CLOSE Soft HOLD SoR twin CLEAR PR **#101** @ `2762b95` (HTTP 200; do not invent handshake)
- [x] Pt 10 handshake gate correctly stated — Product QA HOLD was correct process; Soft HOLD until Security QA `…a8-min-productqa-qa-confirm.md`
- [ ] Security QA → `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-qa-confirm.md`
- [ ] Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (after Security QA)
- [ ] Soft Soft CLOSE Soft HOLD status:done until CBA
- [x] Soft Soft CLOSE Soft HOLD SoR twin → Docs CLEAR PR **#101** (do **not** invent handshake)

## Cost/critical

None. PoC **$0**. Any named LLM/API/IdP spend → COO → CEO.
