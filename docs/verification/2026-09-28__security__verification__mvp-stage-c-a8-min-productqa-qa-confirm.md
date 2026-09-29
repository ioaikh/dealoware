# Security QA — MVP Stage C #68 A8-minimum per-Participant meters + hard budgets Product QA vs Chief Security Product QA checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 Product-QA-step Security points MET)  
**Asked by:** Senior Security / Senior Product QA / Dealoware QAQA / Chief Security (PRIORITY — Product QA HOLD PASS pts 1–9; pt 10 handshake; Soft Soft CLOSE Soft HOLD score LIFTED; SoR CLEAR)  
**Product QA report:** `qa/2026-09-28__qa__qa-report__mvp-stage-c-a8-min-meters-budgets.md` (HOLD PASS pending this confirm; Sec 1–9 EVIDENCED; pt 10 HOLD)  
**Chief checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` (10 points)  
**SoR checklist twin (GitHub):** Soft Soft CLOSE Soft HOLD SoR twin **CLEAR** PR **#101** @ `2762b95838e6e0292a9ba43f5ce118f4dced3cb8` (short `2762b95`; content `cc203fef`) → `docs/verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` (HTTP 200 on main; verified `get_file_contents`)  
**Senior Security Product QA points-review:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-points-review.md` (**PASS** 10/10) — cited; independent re-score **agrees**  
**Prior SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-qa-confirm.md` (PASS 10/10; SoR twin PR **#93** @ `0b40cab`) — supporting only; does **not** close Product-step pt 10  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-qa-confirm.md`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-devplan-qa-confirm.md`  
**Format ref:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-productqa-qa-confirm.md` · `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/87 (**MERGED**)  
**Impl merge commit:** `c5485cfdd1c1f35a0af32c792fb588935f606ddf` (short `c5485cf`)  
**PR head cited:** `c2768c1546b862d509c883af1e1fb2e875f3f9e6` (short `c2768c1`)  
**Live tip:** `main` @ `02e577584d6e693c8ec6ffa1e8b4a8e5e3cc1940` (short `02e5775`; docs Soft Soft CLOSE Soft HOLD SoR ahead of impl tip)  
**CI (impl @ c5485cf):** https://github.com/ioaikh/dealoware/actions/runs/36506996510 — **SUCCESS**  
**CI (PR head @ c2768c1):** https://github.com/ioaikh/dealoware/actions/runs/36506810520 — **SUCCESS**  
**Tests:** `tests/Dealoware.Api.Tests/StageCBudgetMeterTests.cs` (29 Facts; present @ `c5485cf` via `get_file_contents`)  
**Issue:** https://github.com/ioaikh/dealoware/issues/68 · A8-minimum meters + hard budgets (cutoff)  
**Parent:** #18 · Stage C · roadmap A8 Option 1 — framing-only; does **not** Field-capture #68  
**Siblings:** #66 (primary metered consumer) · #67 (metered path wall-bound — **bind only**; do **not** re-score) · #69 — **OUT** of this Story (Soft HOLD #69/#18 separate; cross-ref only)  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-qa-confirm.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (after this confirm + QAQA). Soft Soft CLOSE Soft HOLD status:done until CBA. Soft Soft CLOSE Soft HOLD SoR Product QA twin **CLEAR** PR **#101** @ `2762b95` (do **not** invent handshake / Docs SoR unlock from this file alone). Soft **#41** OUT via **#66+#67** (do not re-open). Gate **#26** backlog; Gate **#27** HOLD. PoC **$0**; no Cognito/MM/DC4/vault invent. Soft: no-live-dotnet OK (CI SUCCESS both + StageCBudgetMeterTests ×29). Path **a8-min** (not a8-meters). Scope **#68 ONLY**.

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security Product QA checklist | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` | Binding 10 points — ISSUED on KB |
| SoR checklist twin | PR **#101** @ `2762b95` → `docs/verification/…a8-min-productqa-checklist.md` | **CLEAR** (HTTP 200 on main; `get_file_contents`) |
| Product QA report | `qa/2026-09-28__qa__qa-report__mvp-stage-c-a8-min-meters-budgets.md` | HOLD PASS pts 1–9 EVIDENCED; pt 10 HOLD |
| Senior Security Product QA points-review | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-points-review.md` | **PASS** 10/10 — cited; agrees |
| Prior SD Security PASS | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-qa-confirm.md` | PASS 10/10 — supporting only; does **not** close Product pt 10 |
| Spec / Dev Plan Security PASS | `…a8-min-spec-qa-confirm.md` / `…a8-min-devplan-qa-confirm.md` | Upstream unlock context |
| Impl PR #87 | MERGED @ `c5485cfdd1c1f35a0af32c792fb588935f606ddf` | Verified via Product QA + Senior + spot-check @ tip |
| Live tip | `02e5775…` | Docs Soft Soft CLOSE Soft HOLD SoR ahead of impl; Product verify held to **`c5485cf`** |
| CI impl | run 36506996510 @ `c5485cf…` | **SUCCESS** |
| CI PR head | run 36506810520 @ `c2768c1…` | **SUCCESS** |
| Tests | `StageCBudgetMeterTests.cs` ×**29** @ `c5485cf` | Present via `get_file_contents` |
| BudgetEndpoints spot-check | `src/Dealoware.Api/Endpoints/BudgetEndpoints.cs` @ `c5485cf` | Unauth → 401; missing → 404; own-only |

## Soft gaps accepted (non-blocking)

| Soft gap | Disposition |
|----------|-------------|
| No live `dotnet test` on evidence box | **Accepted** — CI SUCCESS runs 36506996510 @ `c5485cf` + 36506810520 @ `c2768c1` + `StageCBudgetMeterTests.cs` ×29 + prior SD Security PASS (thin-assistant / hardwall Product QA pattern) |
| **`IBudgetService?` optional** | **Accepted** — `AssistantService` ctor default `null` skips check/record when omitted; tip DI **does** register `IBudgetService` so production path wired. Prefer required bind for fail-closed-by-construction later. Agrees Senior + Product QA. |
| **HTTP at/over-budget `/assistant/invoke` deny** missing Fact | **Accepted** — at/over covered by **domain** `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit` (+ entity TryConsume). Register default **1000** makes HTTP exhaust costly without low-limit fixture. Endpoint maps `BUDGET_EXHAUSTED` → **400 BadRequest** (still deny; not soft-warn). Agrees Senior + Product QA. |
| **`Budget_Status_CrossTenant_Returns404` naming** | **Accepted** — name implies 404; body asserts both owner and stranger get **200** (each own budget). Own-only surface OK; API has no other-sub lookup. Rename preferred; isolation evidence still MET. Agrees Senior + Product QA. |
| Strategy-only / no-tool invoke does not consume | **Accepted** — meter check+record only when `ToolName` present; matches “metered Assistant/tool” lock. |
| Pre-check + post-success record not atomic under concurrency | **Accepted** — A8-min PoC race overshoot soft residual; not soft-warn-only control. |
| Soft Soft CLOSE Soft HOLD SoR / Doc | **Accepted** — SoR twin CLEAR PR **#101** @ `2762b95` (HTTP 200); this confirm does **not** invent Docs SoR unlock; Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (Product QA / QAQA may clear after this) |
| Soft Soft CLOSE Soft HOLD status:done | **Accepted** — held until CBA; not cleared by this confirm |
| Soft **#41** OUT via **#66+#67** | **Accepted** — do **not** re-open; #66/#67 already Product QA Security PASSed separately |
| Tip `02e5775` docs Soft Soft CLOSE Soft HOLD SoR ahead of impl `c5485cf` | **Accepted** — Product verify held to **`c5485cf`** |
| Do **not** re-score #67 wall | **Accepted** — bind cite only |
| Soft HOLD #69/#18 separate | **Accepted** — OUT of this Story |

## Independent re-score (Security QA)

Score vs **official Product QA checklist 1–10** only. Scope **#68 ONLY**; #67 bind cite prior PASS — **do not re-score #67**. Agree Senior when evidence matches.

| # | Point | Senior | Security QA | Evidence |
|---|-------|--------|-------------|----------|
| 1 | Per-Participant meters — counters scoped to Participant | **MET** | **MET** | QA Sec #1 / AC1 + re-spot @ `c5485cf…`: `ParticipantBudget.Create(participantSub, limitUnits)` keys on `ParticipantSub`; `BudgetService.CheckBudgetAsync` / `RecordUsageAsync` / `GetBudgetStatusAsync` resolve only via `IBudgetRepository.GetByParticipantSubAsync(ownerSub)`; `AuthEndpoints.Register` → `EnsureBudgetExistsAsync(participant.Sub, 1000)`; DI `AddScoped<IBudgetRepository, BudgetRepository>` + `AddScoped<IBudgetService, BudgetService>`. Tests: `ParticipantBudget_Create_ScopedToParticipant`, `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`, `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`, `Register_ProvisionsBudget`, `Assistant_Invoke_UnderBudget_DecrementsUsage`, `Budget_Status_ReturnsValidStatus`. |
| 2 | Hard cutoff fail-closed — NOT soft-warn-only | **MET** | **MET** | QA Sec #2 / AC2 + re-spot: `ParticipantBudget.IsExhausted` (`UsedUnits >= LimitUnits`); `TryConsume` returns **false** at/over; `BudgetService.CheckBudgetAsync` → `BudgetCheckResult.Exhausted()` (`BUDGET_EXHAUSTED`); `AssistantService.InvokeAsync` checks budget **before** `_gateway.InvokeToolAsync` and returns `AssistantResult.BudgetExhausted()` — deny server-side, not warn-only; `RecordUsageAsync` only after gateway success. Tests: `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit`, `ParticipantBudget_TryConsume_ReturnsFalse_WhenExhausted`, `ParticipantBudget_TryConsume_ReturnsFalse_WhenOverLimit`, `ParticipantBudget_IsExhausted_WhenAtLimit`, `BudgetCheckResult_Exhausted_HardCutoff_NotSoftWarnOnly`. Soft residual: optional `IBudgetService?` (accepted). |
| 3 | Cross-tenant / unauth cannot burn budget | **MET** | **MET** | QA Sec #3 / AC3 + re-spot: null/empty `ownerSub` → `BudgetCheckResult.Unauthenticated()` (no consume); each budget keyed by `ParticipantSub` — `RecordUsageAsync("participant-a")` does not touch B; `BudgetEndpoints` own-sub only via `AuthHelper.GetAuthenticatedSub` — no other-sub lookup API. HTTP: stranger invoke burns only stranger's meter. Tests: `Assistant_CrossTenant_CannotBurnOthersBudget`, `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`, `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`, `BudgetService_CheckBudget_Unauthenticated_ReturnsUnauthenticated`, `Budget_Status_Unauthenticated_Returns401`. |
| 4 | Metered path still wall-bound (#67) — do **NOT** re-score #67 | **MET** | **MET** | QA Sec #4 + re-spot: `AssistantService` still **requires** `IAgentGateway` (ctor null-throws); meter check only inside tool path after `IsToolAvailable` / before `InvokeToolAsync`; `BudgetStatus` / `BudgetStatusResponse` expose only `LimitUnits`, `UsedUnits`, `RemainingUnits`, `IsExhausted`, `UsagePercentage` — **no** FieldClass / LoginEmail / StrategyBody. Bind only — #67 Product QA already PASSed separately (`…hardwall-productqa-qa-confirm.md`). Tests: `BudgetStatus_NoFieldClassLeakage`, `BudgetCheckResult_ErrorMessages_NoPrivateLeak`, `Budget_Status_NoPrivateFieldsInResponse`, `BudgetCheckResult_Exhausted_NoPrivateLeak`. |
| 5 | Authn fail-closed on meter APIs / GET `/budget/status` | **MET** | **MET** | QA Sec #5 + re-spot `BudgetEndpoints.GetBudgetStatus` @ `c5485cf…`: `AuthHelper.GetAuthenticatedSub` → null/empty → `Results.Unauthorized()` (**401**); budget missing → `Results.NotFound` (**404** uniform deny); own-only surface (no other-sub lookup). Invalid ApiKey / JWT → 401. Error bodies checked for no LoginEmail/StrategyBody. Tests: `Budget_Status_Unauthenticated_Returns401`, `Budget_Status_InvalidAuth_Returns401`, `Budget_Status_InvalidJwt_Returns401`, `Budget_Status_Unauthenticated_NoPrivateFieldsInError`, `BudgetService_CheckBudget_EmptyOwner_ReturnsUnauthenticated`, `BudgetService_GetBudgetStatus_Unauthenticated_ReturnsNull`. Soft: `Budget_Status_CrossTenant_Returns404` name vs own-only 200/200 — isolation still MET. |
| 6 | OUT locked A8-minimum only — mature → V3 | **MET** | **MET** | QA Sec #6 / AC5 + re-spot: PR #87 = 18 files (+1396/−1) Budget domain/API/infra + Assistant meter hook + Auth provision + `StageCBudgetMeterTests` — simple counters only. OUT comments name mature metering/analytics → V3; billing/escrow/settlement **not** implemented. `GET /budget/status` min status only (5 fields). Soft O7 not invented as second product. |
| 7 | Sibling surfaces — no #69 invent; keep #66/#67/#69 separate | **MET** | **MET** | QA Sec #7 + re-spot: no owner-admin UI/suite in #68 file set; only `GET /budget/status` minimal status DTO. #66/#67 appear as **cross-ref** / mandatory wall bind in `AssistantService` + DI comments — no #69 product surface. Soft Soft CLOSE Soft HOLD #69 SD separate (do not invent). |
| 8 | No 5th Story / Gate HOLDs; Soft #41 OUT via #66+#67 | **MET** | **MET** | QA Sec #8 / AC6 + re-spot: Soft OTel/audit = comment weave only ("no 5th Story"); no Cognito/MM/DC4/vault invent in diff. Gate **#26** backlog; Gate **#27** HOLD held in constraints. Soft **#41** OUT via **#66+#67** (do **not** re-open / re-score). Health untouched: `Health_StillNoAuthRequired_AfterBudgetFeature`. |
| 9 | Cost / spend — PoC $0; spend → COO→CEO | **MET** | **MET** | QA Sec #9 + re-spot scope: no LLM SDK/provision; no AWS/external metered API client; local SQLite meters only. PoC **$0**. Any named LLM/API spend → COO → CEO. CI SUCCESS both SHAs. |
| 10 | Handshake close — Product QA HOLD until Security QA; #18 not Field-capture | **MET** | **MET** | Product QA correctly HOLDs PASS / pt 10 until this Security QA `…a8-min-productqa-qa-confirm.md`. Prior SD Security PASS does **not** close Product-step pt 10. Parent #18 framing-only — does **not** Field-capture #68. **This confirm closes Product QA Security gate.** Soft Soft CLOSE Soft HOLD Doc / status:done / SoR remain held per constraints (SoR twin already CLEAR PR #101 — no invent unlock). |

## Alignment with Senior review

Senior Security Product QA points-review (`verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-points-review.md`) scored all 10 **MET** (**PASS** 10/10) vs Product QA HOLD PASS report + MERGED PR #87 @ `c5485cf…` + CI SUCCESS both runs + StageCBudgetMeterTests ×29 + SoR twin CLEAR PR **#101** @ `2762b95`. Independent Security QA re-score vs Chief Product QA checklist + Product QA report + Senior done-list + spot-check (`StageCBudgetMeterTests.cs` ×29 + `BudgetEndpoints.cs` @ `c5485cf` via `get_file_contents`; SoR twin HTTP 200 on main) — **all 10 MET**; **agrees** Senior; **no reopen**. Soft notes align (optional `IBudgetService?`; domain-only at/over HTTP deny; CrossTenant_Returns404 naming; Soft Soft CLOSE Soft HOLD Doc / status:done; Soft #41 OUT via #66+#67; Soft HOLD #69/#18; Gate #26 backlog / #27 HOLD; PoC $0). No contradiction.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Per-Participant meters; counters keyed by ParticipantSub | Held (`ParticipantBudget.Create` + scoped repo/service + Register_ProvisionsBudget) |
| Hard cutoff fail-closed; NOT soft-warn-only | Held (`IsExhausted` / `TryConsume` false / `BUDGET_EXHAUSTED` + Assistant pre-gateway deny) |
| Cross-tenant / unauth cannot burn budget | Held (service isolation + Assistant_CrossTenant_* + 401 unauth tests) |
| Metered path #67 wall-bound; BudgetStatus no FieldClass leak; do not re-score #67 | Held (`IAgentGateway` ctor + InvokeToolAsync after budget allow + BudgetStatus_* / NoPrivateLeak; cite hardwall-productqa-qa-confirm PASS) |
| Authn fail-closed on meter APIs; 401/404; no private leak | Held (BudgetEndpoints 401 + NotFound + InvalidAuth/Jwt tests @ `c5485cf`) |
| OUT locked A8-minimum only; mature → V3; no billing invent | Held (PR #87 counters-only; OUT comments) |
| No #69 owner-admin invent; #66/#67/#69 separate Product QA | Held (#68-only scope; min status DTO only) |
| No 5th Story; Gate #26 backlog; #27 HOLD; Soft #41 OUT via #66+#67 | Held (constraints + Soft weave only) |
| PoC $0; no Cognito/MM/DC4/vault invent | Held |
| Soft Soft CLOSE Soft HOLD Doc until Product QA PASS; status:done until CBA; SoR twin CLEAR #101 — no invent unlock | Held |
| Evidence on impl `c5485cf…` / tip `02e5775…` + CI SUCCESS both | Held (runs 36506996510 / 36506810520) |
| Path a8-min (not a8-meters); scope #68 ONLY | Held |

## Gaps

**None.** All checklist points 1–10 **MET**. Soft residuals (`IBudgetService?`; missing HTTP at/over invoke Fact; CrossTenant_Returns404 naming) accepted non-blockers — agree Senior + Product QA.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Product QA / QAQA may clear HOLD and PASS to Chief (QAQA meta-PASS after this). Soft Soft CLOSE Soft HOLD Doc may clear on Product QA PASS after this confirm + QAQA. Soft Soft CLOSE Soft HOLD status:done remains until CBA. Soft Soft CLOSE Soft HOLD SoR Product QA checklist twin **CLEAR** PR **#101** @ `2762b95` (HTTP 200) — do **not** invent Docs SoR unlock / handshake Soft Soft CLOSE Soft HOLD SoR from this file alone. Soft **#41** OUT via **#66+#67** — do not re-open. Cost/critical: none. PoC **$0**. Do **not** open Gate #26. Do **not** treat this as #66 / #67 / #69 / parent #18 confirm (#66/#67 already PASSed separately — bind/cross-ref only; Soft HOLD #69/#18 separate). Gate **#27** HOLD. Path **a8-min**. Scope **#68 ONLY**.
