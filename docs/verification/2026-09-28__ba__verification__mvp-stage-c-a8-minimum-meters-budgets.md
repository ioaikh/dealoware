# BA business verification — Story #68 A8-minimum per-Participant meters + hard budgets (cutoff)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-28 (BA verify executed ~9:52 PM ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/68  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/87 (**MERGED** @ main `c5485cfdd1c1f35a0af32c792fb588935f606ddf`; MergedAt 2026-09-28 9:14:59 PM ET)  
**CI (PR head @ c2768c1):** https://github.com/ioaikh/dealoware/actions/runs/36506810520 — **SUCCESS**  
**CI (impl tip @ c5485cf):** https://github.com/ioaikh/dealoware/actions/runs/36506996510 — **SUCCESS**  
**Tip (main @ Overall Doc PASS handshake):** `11e64d407852331c020df905c5252908e1299c2d` (PR #109 Doc handshake; Budget tree from #87 @ `c5485cf…`)  
**Method:** Business-intent AC check from Story #68 body + merged PR #87 evidence on **main** (`gh` remote reads of ParticipantBudget / BudgetService / BudgetEndpoints / AssistantService meter bind / StageCBudgetMeterTests; no clone) + KB Product QA / Spec / DevPlan / SD verifies + Security ProductQA/SD/Doc confirms + Doc weave (not code review). No invented requirements. Stage C named slice only — **A8-minimum Option 1** hard cutoff. Soft **#41** Assistant OUT already via **#66+#67** (do **not** re-open). Soft Spec weave ≠ 5th Story. Soft HOLD **#69** until CPM routes (OUT of this Story except min status surface). Parent **#18** Soft HOLD / framing-only (does not Field-capture #68). Gate **#26** backlog · **#27** HOLD. Soft HOLD multi-provider invent. Soft Soft CLOSE Soft HOLD SoR CLEAR (#93/#101/#103/#105/#107/#108/#109). PoC **$0**. No MotorMarket/DC4. Do **not** CLOSE issue or flip `status:done` from this step.

## Binding AC (Story #68 — do not invent)

1. Per-Participant **meters** exist for MVP-metered Assistant / LLM (or equivalent metered) usage — minimum viable counters sufficient for cutoff (not mature owner analytics)
2. **Hard budget / cutoff** enforced server-side: when Participant budget exhausted, further metered Assistant/tool invocations **deny** (fail-closed) — not “soft warn only”
3. Unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed
4. Automated tests (or equivalent): under-budget allow; at/over budget deny; cross-tenant deny; unauth deny
5. Documented as **A8-minimum** MVP — **mature metering / owner cost UI → V3**; no inventing full billing/settlement
6. Soft Spec weave only (not a separate Story): if meter/cutoff events touch SA-REV-MVP-C **audit** (and/or OTel) hooks, Spec notes touchpoints — no new product surface invented

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | Per-Participant **meters** for MVP-metered Assistant/LLM — min counters for cutoff, not owner analytics | **PASS** | PR #87: `ParticipantBudget` keyed by `ParticipantSub` (limit/used); `BudgetService` + `IBudgetRepository`; `AuthEndpoints` `EnsureBudgetExistsAsync(sub, 1000)`; DI `AddScoped<IBudgetService, BudgetService>`. Facts: `ParticipantBudget_Create_ScopedToParticipant`, `Register_ProvisionsBudget`, `Budget_Status_ReturnsValidStatus`, `Assistant_Invoke_UnderBudget_DecrementsUsage`. Product QA AC1 MET; Security ProductQA/SD/Doc pts 1 MET. |
| 2 | **Hard cutoff** server-side fail-closed — budget exhausted → further metered Assistant/tool invocations **DENY** (not soft-warn-only). Check **BEFORE** #67 gateway; record **AFTER** success | **PASS** | `AssistantService.InvokeAsync`: `CheckBudgetAsync` before `_gateway.InvokeToolAsync`; `RecordUsageAsync` after success only; `BudgetCheckResult.Exhausted` → `AssistantResult.BudgetExhausted()` (`BUDGET_EXHAUSTED`). Facts: `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit`, `ParticipantBudget_TryConsume_ReturnsFalse_WhenExhausted`, `BudgetCheckResult_Exhausted_HardCutoff_NotSoftWarnOnly`. Product QA AC2 MET; Security pts 2 MET. Soft gap: optional `IBudgetService?` (tip DI registers — accepted). |
| 3 | Unauth / wrong-principal cannot burn another Participant’s budget; cross-tenant meter misuse fail-closed | **PASS** | Facts: `Assistant_CrossTenant_CannotBurnOthersBudget`, `BudgetService_CheckBudget_CrossTenant_CannotBurnOthersBudget`, `BudgetService_GetBudgetStatus_CrossTenant_ReturnsOnlyOwnBudget`, `Budget_Status_Unauthenticated_Returns401` / InvalidAuth / InvalidJwt, `BudgetService_CheckBudget_Unauthenticated_ReturnsUnauthenticated`. `BudgetEndpoints` own-sub only via `AuthHelper.GetAuthenticatedSub` — no other-sub lookup API. Product QA AC3 MET; Security pts 3/5 MET. |
| 4 | Automated tests: under-budget allow; at/over deny; cross-tenant deny; unauth deny | **PASS** | `tests/Dealoware.Api.Tests/StageCBudgetMeterTests.cs` **29 Facts** on merge `c5485cf…`; CI SUCCESS runs 36506810520 @ `c2768c1` + 36506996510 @ `c5485cf`. Under-budget HTTP: `Assistant_Invoke_UnderBudget_Returns200` / `_DecrementsUsage`. At/over: **domain** `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit` (+ entity TryConsume). Cross-tenant + unauth Facts present. Product QA AC4 MET. Soft: no live `dotnet` — CI + inventory equivalent; HTTP at/over invoke Fact missing (domain covers — accepted). |
| 5 | Documented **A8-minimum** MVP — mature metering / owner cost UI → **V3**; no billing/escrow invent | **PASS** | Spec `specs/…a8-minimum-meters-budgets.md` §6 OUT + Locked #8; PR #87 OUT table / Budget domain comments; no billing/escrow/settlement classes in #68 file set (18 files counters-only); `GET /budget/status` min status only (Limit/Used/Remaining/IsExhausted/UsagePercentage). Product QA AC5 MET; Security pts 6–7 MET. Overall Doc PASS (triad SoR #107/#108/#109 @ `11e64d4`). |
| 6 | Soft Spec weave only for audit/OTel — **no 5th Story**; do not implement #66/#67/#69 here (bind #66 consumer + #67 wall only) | **PASS** | Spec §4 Soft weave only; PR #87 = Budget domain/infra/API + Assistant meter bind + Auth provision + tests only (no new OTel product Story; no #69 owner-admin; #67 wall bind via existing `IAgentGateway` — do not re-score). Product QA AC6 MET; Security pts 8–9 MET. Gate #26 backlog · #27 HOLD held. |

## Out of scope held

| OOS item (Story #68 + locks) | Held? | Evidence |
|------------------------------|-------|----------|
| Mature metering / platform-owner cost UI (**A8** mature → **V3**) | **Yes** | Spec §6 OUT; PR OUT; Product QA AC5; Security pts 6 |
| Escrow / settlement / checkout; full billing product | **Yes** | No billing/escrow classes in #68 file set; Product QA / Security OUT |
| Thin Assistant runtime features (**#66**) — invent beyond meter bind | **Yes** | Meter hook only in `AssistantService`; #66 already BA PASS / Product QA PASS — bind only |
| Hard wall (**#67**) — invent / re-score wall | **Yes** | Consumes existing `IAgentGateway`; do not re-score #67; Product QA Sec #4; Security pts 4 |
| UI/bot (**#69**) except minimum surface needed to respect cutoff | **Yes** | Only `GET /budget/status` min DTO; no owner-admin; Soft HOLD #69 separate; Product QA Sec #7 |
| Inventing a **5th Story** for OTel/audit/idempotent | **Yes** | Soft Spec weave only; Product QA AC6; Security pts 8 |
| Soft O7 as a second invent product | **Yes** | Align-if-on-path only; Spec Locked; no second product in #68 |
| Soft **#41** Assistant OUT claimed as Stage B / re-opened from #68 | **Yes** | Soft #41 OUT via **#66+#67** only — do not re-open from #68 |
| MotorMarket / DC4; inventing Cognito/SSO; multi-provider invent | **Yes** | No Cognito/MM/DC4/multi-provider invent in #68 file set; PoC $0; Security pts 8–9 |
| Unlocking gate **#26** before Stage C delivery; gate **#27** HOLD | **Yes** | #26 stays backlog; #27 HOLD; CPM comments + Soft locks; Security pts 8 |
| Parent **#18** Soft HOLD / Field-capture #68; verifying **#69** | **Yes** | Parent framing-only; Spec HOLD; #69 OUT of this Story |
| Soft Spec weave ≠ 5th Story; inventing beyond A8-minimum named slice | **Yes** | A8 Option 1 counters + hard cutoff only; Product QA / Security / Doc HOLD invent |

## Soft gaps (non-blocking for BA business verify)

- No live `dotnet restore|build|test` on evidence box — accepted; CI SUCCESS runs 36506810520 + 36506996510 + `StageCBudgetMeterTests` ×29 inventory via `gh`.
- **`IBudgetService?` optional** — `AssistantService` ctor default `null` skips check/record when omitted; tip DI **does** register `IBudgetService` so production path wired. Prefer required bind for fail-closed-by-construction later (Product QA / Security soft note).
- **HTTP at/over-budget `/assistant/invoke` deny** — missing Fact; at/over covered by **domain** `BudgetService_CheckBudget_ReturnsExhausted_WhenAtLimit` (+ entity). Register default **1000** makes HTTP exhaust costly without low-limit fixture. Endpoint maps `BUDGET_EXHAUSTED` → **400 BadRequest** (still deny; not soft-warn) — accepted.
- **`Budget_Status_CrossTenant_Returns404` naming** — name implies 404; body asserts both owner and stranger get **200** (each own budget). Own-only surface OK; API has no other-sub lookup. Rename preferred; isolation still MET — accepted.
- Strategy-only / no-tool invoke does not consume — meter check+record only when `ToolName` present; matches “metered Assistant/tool” lock — accepted.
- Soft Soft CLOSE Soft HOLD SoR chain CLEAR (#93 @ `0b40cab`, #101 @ `2762b95`, #103 @ `961b815`, #105 @ `d3c59a9`, #107 @ `3c5063c`, #108 @ `605c28c`, #109 @ `11e64d4`); tip main @ `11e64d4`. Eng `status:done` HOLD until CBA confirm after BAQA. Do **not** CLOSE issue or flip `status:done` from this step.

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Spec QA | PASS | `verification/2026-09-28__spec__verification__mvp-stage-c-a8-minimum-meters-budgets.md` + Spec Security confirm 10/10 |
| Dev Plan QA | PASS | `verification/2026-09-28__devplan__verification__mvp-stage-c-a8-minimum-meters-budgets.md` + DevPlan Security confirm 10/10 |
| SD / Dev Code QA | PASS | `verification/2026-09-28__sd__verification__mvp-stage-c-a8-minimum-meters-budgets.md` |
| SD Security | PASS (1–10 MET) | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-sd-qa-confirm.md` (SoR PR #93 @ `0b40cab`) |
| CQ | `cq:no-refactor` | Issue labels + CQ assessment; Chief CQ confirm |
| Product QA | PASS | `qa/2026-09-28__qa__qa-report__mvp-stage-c-a8-min-meters-budgets.md` (SoR PR #103 @ `961b815`) |
| Product QA Security | PASS (1–10 MET) | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-qa-confirm.md` (handshake SoR PR #105 @ `d3c59a9`; checklist SoR PR #101 @ `2762b95`) |
| Overall Doc | PASS | Doc triad SoR #107 @ `3c5063c` · #108 @ `605c28c` · #109 @ `11e64d4`; Doc Security Senior 10/10 + Security QA 10/10; INDEX Overall Doc PASS row |
| Doc weave | ISSUED → Overall Doc PASS | `ops/2026-09-28__docs__ops__mvp-stage-c-a8-min-doc-security-weave.md` |
| Soft Soft CLOSE Soft HOLD SoR | CLEAR | PRs #93/#101/#103/#105/#107/#108/#109 MERGED; tip `11e64d4` |
| Sibling #66 / #67 | BA PASS / Product QA PASS | Bind only — do not re-score; Soft #41 OUT via #66+#67 |

## Recommendation to CBA

**PASS** — deliverable meets Story #68 business AC (per-Participant meters keyed by ParticipantSub with Register provision 1000; hard cutoff fail-closed before #67 gateway with `BUDGET_EXHAUSTED` — not soft-warn-only; unauth/cross-tenant cannot burn another’s budget; StageCBudgetMeterTests ×29 + CI SUCCESS; documented A8-minimum with mature→V3 / no billing invent; Soft OTel/audit weave only — no 5th Story), and OOS held (mature owner cost UI → V3; billing/escrow OUT; #66/#67 bind-only / #69 OUT except min status; no 5th Story; Soft #41 OUT via #66+#67 — do not re-open; Cognito/MotorMarket/DC4/multi-provider OUT; #26 backlog; #27 HOLD; parent #18 Soft HOLD / not Field-captured; #69 not verified here; PoC $0). Soft gaps non-blocking. Hand to BAQA for verify-QA; eng `status:done` HOLD until CBA final confirm after BAQA. Do **not** CLOSE issue #68 from this step, do **not** flip `status:done`, do **not** unlock #26/#27/#18, do **not** invent beyond A8-minimum named slice, do **not** invent a 5th Story, do **not** verify/unlock #69 from this step, do **not** merge sibling tracks from this step. Stage C named slice only. PoC $0.
