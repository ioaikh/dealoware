# QA Report — MVP Stage C A8-minimum per-Participant meters + hard budgets (#68)

**Status:** **PASS** — Product AC 1–6 MET (soft gaps scored); Security pts 1–10 PASS (Security QA `productqa-qa-confirm` 10/10 + Senior Security points-review 10/10 + QAQA PASS)  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Product QA  
**Confirm to:** Dealoware QA (Chief QA) via QAQA  
**Story:** GitHub issue #68 · A8-minimum per-Participant meters + hard budgets (cutoff)  
**Issue:** https://github.com/ioaikh/dealoware/issues/68  
**PR (impl):** https://github.com/ioaikh/dealoware/pull/87 (**MERGED**)  
**Impl merge / SD tip (verify):** `c5485cfdd1c1f35a0af32c792fb588935f606ddf`  
**PR head cited:** `c2768c1546b862d509c883af1e1fb2e875f3f9e6`  
**SD Soft Soft CLOSE Soft HOLD SoR handshake (#93):** `0b40cabb922ed640715c344ffe800d342a82ffa5`  
**CI (PR head @ c2768c1):** https://github.com/ioaikh/dealoware/actions/runs/36506810520 — **SUCCESS**  
**CI (tip @ c5485cf):** https://github.com/ioaikh/dealoware/actions/runs/36506996510 — **SUCCESS**  
**Security checklist (this step):** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` — **ISSUED + ON KB** (Chief Security; re-woven after QAQA bounce for missing file). Soft Soft CLOSE Soft HOLD SoR twin `docs/verification/` same filename pending Docs. Prior SD confirm PASS via PR #93.  
**Prior SD Security QA:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-qa-confirm.md` — **PASS** 10/10 (SoR twin PR #93 @ `0b40cab`)  
**Prior Dev Code QA:** `verification/2026-09-28__sd__verification__mvp-stage-c-a8-minimum-meters-budgets.md` — **PASS**  
**#66/#67 bind (mandatory; do NOT re-score):** Product QA PASS locked — thin Assistant + hardwall reports; metered path remains #67 wall-bound  
**CQ:** `cq:no-refactor`  
**DOC-FLOW:** `qa/2026-09-28__qa__qa-report__mvp-stage-c-a8-min-meters-budgets.md`  
**Constraints:** Scope **#68 ONLY**. Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (SoR publish). Soft Soft CLOSE Soft HOLD status:done until CBA. Soft HOLD **#69** SD LIFTED / Soft HOLD **#18** until #69 tip — **OUT** of this Story. Soft **#41** OUT via **#66+#67** (do **not** re-score). Gate **#26** backlog · **#27** HOLD. Soft Spec weave only for OTel/audit — **no 5th Story**. PoC **$0**; no MotorMarket. Live main may be ahead (docs Soft Soft CLOSE Soft HOLD SoR) — **verify against SD tip `c5485cf`**.

## Method

- GitHub contents / Checks at tip `c5485cf…` and PR head `c2768c1…` (no full clone)
- Soft gap: no live `dotnet test` → **CI + `StageCBudgetMeterTests.cs` equivalent** (**29 Facts**)
- Product QA Security checklist **ON KB** at `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` (Chief Security ISSUED; QAQA bounce for missing file — **re-woven** vs real file 21:27 ET)
- Soft Soft CLOSE Soft HOLD SoR Product QA checklist twin **CLEAR** PR **#101** merge `2762b95` / docs SHA `cc203fef` → `docs/verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md`

## Product acceptance criteria (Spec §8 / issue #68)

| AC | Verdict | Evidence (tip `c5485cf…` / merge PR #87) |
|----|---------|---------------------------------------------|
| 1 Per-Participant **meters** for MVP-metered Assistant/LLM — min counters for cutoff, not owner analytics | **MET** | `ParticipantBudget` keyed by `ParticipantSub` (limit/used); `BudgetService` + `IBudgetRepository`; `AuthEndpoints` `EnsureBudgetExistsAsync(sub, 1000)`; `Register_ProvisionsBudget`; `ParticipantBudget_Create_ScopedToParticipant`; `Budget_Status_ReturnsValidStatus`; DI `AddScoped<IBudgetService, BudgetService>` |
| 2 **Hard cutoff** server-side fail-closed: budget exhausted → further metered Assistant/tool invocations **DENY** (not soft-warn-only). Check **BEFORE** #67 gateway; record **AFTER** success. Bind in `AssistantService.InvokeAsync` vs optional `IBudgetService?` | **MET** (soft gap: optional `IBudgetService?`) | `AssistantService.InvokeAsync`: `CheckBudgetAsync` before `_gateway.InvokeToolAsync`; `RecordUsageAsync` after success only. `BudgetCheckResult.Exhausted` → `AssistantResult.BudgetExhausted()` (`BUDGET_EXHAUSTED`). Tests: `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit`; `ParticipantBudget_TryConsume_ReturnsFalse_WhenExhausted`; `BudgetCheckResult_Exhausted_HardCutoff_NotSoftWarnOnly`. **Soft gap:** ctor takes `IBudgetService? budgetService = null` and skips check/record when null — **bypass if DI omits**; tip DI **does** register `IBudgetService` so production path wired, but optional default is not fail-closed-by-construction |
| 3 Unauth / wrong-principal cannot burn another Participant's budget; cross-tenant meter misuse fail-closed | **MET** | `Assistant_CrossTenant_CannotBurnOthersBudget`; `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`; `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`; `Budget_Status_Unauthenticated_Returns401` / InvalidAuth / InvalidJwt; `BudgetService_CheckBudget_Unauthenticated_ReturnsUnauthenticated`. `BudgetEndpoints` own-sub only via `AuthHelper.GetAuthenticatedSub` — no other-sub lookup API |
| 4 Automated tests §8.1: under-budget allow; at/over deny; cross-tenant deny; unauth deny | **MET** (soft gap: HTTP at/over invoke) | `StageCBudgetMeterTests.cs` **29 Facts**; CI SUCCESS 36506810520 @ `c2768c1` + 36506996510 @ `c5485cf`. Under-budget HTTP: `Assistant_Invoke_UnderBudget_Returns200` / `_DecrementsUsage`. At/over: **domain** `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit` (+ entity TryConsume) — **no** HTTP `/assistant/invoke` at/over-budget deny Fact (register default **1000**). Cross-tenant + unauth Facts present |
| 5 Documented **A8-minimum** MVP — mature metering / owner cost UI → **V3**; no billing/escrow invent | **MET** | Spec `specs/…a8-minimum-meters-budgets.md` §6 OUT + Locked #8; PR #87 OUT table; no billing/escrow/settlement classes in #68 file set; `GET /budget/status` min status only |
| 6 Soft audit/OTel weave only if hooks on path — **no 5th Story** | **MET** | Spec §4 Soft weave; PR #87 = Budget domain/infra/API + Assistant bind + tests (no new OTel product Story); Gate #26/#27 held in constraints |

## Security checklist points 1–10 (Product QA evidence)

**Binding:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` — **ON KB** (Chief Security ISSUED; Product QA re-weave after QAQA bounce). Soft Soft CLOSE Soft HOLD SoR twin under `docs/verification/` same filename — pending Docs (404 OK to cite). Prior SD qa-confirm PASS via PR #93.

| # | Point | Verdict | Evidence |
|---|-------|---------|----------|
| 1 | Per-Participant meters | **EVIDENCED** | `ParticipantBudget_Create_ScopedToParticipant`; `BudgetService_*_CrossTenant_*`; register `EnsureBudgetExistsAsync(sub, 1000)`; status own-only |
| 2 | Hard cutoff fail-closed (not soft-warn) | **EVIDENCED** | Check-before / record-after in `AssistantService`; `BUDGET_EXHAUSTED`; domain exhaust Facts; soft note optional `IBudgetService?` |
| 3 | Cross-tenant / unauth cannot burn budget | **EVIDENCED** | `Assistant_CrossTenant_CannotBurnOthersBudget`; unauth CheckBudget; status 401 Facts |
| 4 | Metered path still wall-bound (#67; do not re-score #67) | **EVIDENCED** | Meter only inside tool path after `IsToolAvailable` / before `InvokeToolAsync`; `BudgetStatus_NoFieldClassLeakage`; `Budget_Status_NoPrivateFieldsInResponse`; do **not** re-score #67 |
| 5 | Authn fail-closed on meter APIs — GET `/budget/status` 401 / own-only 403 or 404; no FieldClass leak | **EVIDENCED** | `Budget_Status_Unauthenticated_Returns401`; InvalidAuth/InvalidJwt; own-only 200/200 (`Budget_Status_CrossTenant_Returns404` name vs own-only — API has **no** other-sub lookup; own-only OK) |
| 6 | OUT locked A8-min only (mature → V3) | **EVIDENCED** | Spec §6 OUT; PR OUT; counters-only status surface |
| 7 | No #69 owner-admin invent; budget status minimal only | **EVIDENCED** | `GET /budget/status` only; no owner-admin UI in #68 diff |
| 8 | No 5th Story; Gate #26 backlog; #27 HOLD; Soft #41 OUT via #66+#67 | **EVIDENCED** | Soft weave comments only; constraints Gate #26/#27; Soft #41 OUT via #66+#67 (not re-scored) |
| 9 | PoC $0; spend → COO → CEO | **EVIDENCED** | Local SQLite meters; no LLM SDK/provision in #68; PoC **$0** |
| 10 | Handshake close — no Product QA PASS until Security QA productqa-qa-confirm | **PASS** | Security QA `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-qa-confirm.md` **PASS 10/10**; Senior Security points-review **PASS 10/10**; Soft Soft CLOSE Soft HOLD Doc unlocked for Product QA PASS. |

## Soft gaps / non-blockers

- No live `dotnet` — CI + `StageCBudgetMeterTests.cs` ×**29** equivalent (accepted soft gap)
- **`IBudgetService?` optional** — `AssistantService` ctor default `null` skips check/record; tip DI registers service (wired) but omission would bypass cutoff — score honestly; prefer required bind for fail-closed-by-construction
- **HTTP at/over-budget `/assistant/invoke` deny** — missing Fact; at/over covered by **domain** `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit` (+ entity). Register default **1000** makes HTTP exhaust costly without a low-limit fixture. Endpoint maps unknown error codes (incl. `BUDGET_EXHAUSTED`) via default → **400 BadRequest** (still deny; not soft-warn)
- **`Budget_Status_CrossTenant_Returns404`** — name implies 404; body asserts both owner and stranger get **200** (each own budget). Own-only surface is OK; API has no other-sub lookup. Rename / assert documentation preferred
- **Strategy-only / no-tool invoke does not consume** — confirmed: meter check+record only when `ToolName` present; matches “metered Assistant/tool” lock (strategy-only / ready path unmetered)
- Soft Soft CLOSE Soft HOLD SoR Product QA checklist twin **CLEAR** PR **#101** merge `2762b95` / docs SHA `cc203fef`
- Tip `c5485cf` is impl merge; live main may be ahead with docs Soft Soft CLOSE Soft HOLD SoR — Product verify held to **`c5485cf`**
- CQ assessment may not be on main yet — does not block

## OUT / HOLD (verified)

- #66/#67 details — **bind only** (already Product QA PASSed; do not re-score)
- #69 UI/bot except min cutoff status surface — **OUT** (Soft HOLD #69 SD LIFTED / Soft HOLD #18 until #69 tip — not this Story)
- Soft #41 — OUT via #66+#67 (do not re-score)
- Mature metering / owner cost UI → V3; billing/escrow; MotorMarket; Cognito/DC4
- Gate #26 backlog · Gate #27 HOLD
- Soft Soft CLOSE Soft HOLD Doc — clearing via SoR publish after PASS
- Soft Soft CLOSE Soft HOLD status:done until CBA
- Soft Soft CLOSE Soft HOLD SoR Product QA checklist twin → Docs **CLEAR** (PR #101)
- 5th OTel/audit Story invent

## Disposition

**PASS** (re-sent after QAQA bounce; Security handshake closed) — AC 1–6 **MET** with soft gaps scored (optional `IBudgetService?`; domain-only at/over deny; own-only status naming). Security pts 1–10 **PASS** (Security QA productqa-qa-confirm + Senior Security points-review + QAQA). Soft Soft CLOSE Soft HOLD SoR checklist twin CLEAR (PR #101). Soft Soft CLOSE Soft HOLD Doc clearing via SoR `docs/qa/` publish. Soft Soft CLOSE Soft HOLD `status:done` until CBA. Soft **#41** OUT via **#66+#67**. Soft gaps remain non-blocking. Do not set GitHub `status:done` from this step alone. PoC **$0**.

### Done-list

- [x] Evidence at tip `c5485cf…` + PR head `c2768c1…` (CI SUCCESS both: 36506996510 / 36506810520)
- [x] AC 1–6 woven (Spec §8 / issue #68) — soft gaps scored honestly
- [x] Security pts 1–9 **re-woven** vs KB file `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-checklist.md` (QAQA bounce cleared; pt 10 HOLD)
- [x] QAQA confirm to Chief — **PASS** (meta after Security QA)
- [x] Senior Security → `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-points-review.md` — **PASS 10/10**
- [x] Security QA → `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-qa-confirm.md` — **PASS 10/10**
- [x] SoR publish under `docs/qa/` — PR **#103** OPEN; awaiting merge
- [x] Soft Soft CLOSE Soft HOLD SoR Product QA checklist twin under `docs/verification/` — PR **#101** MERGED merge `2762b95` / docs SHA `cc203fef`
