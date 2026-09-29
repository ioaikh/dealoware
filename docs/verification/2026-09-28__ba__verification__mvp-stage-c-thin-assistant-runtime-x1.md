# BA business verification — Story #66 Thin Strategy-driven AI Assistant runtime (X1 thin)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-28 (BA verify executed ~9:24 PM ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/66  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/79 (**MERGED** @ main `199125afe537bb71945f080ad178f275c73153ef`; MergedAt 2026-09-28 8:52:12 PM ET)  
**CI (impl @ 199125a):** https://github.com/ioaikh/dealoware/actions/runs/36505175931 — **SUCCESS**  
**CI (SD tip @ 7043314):** https://github.com/ioaikh/dealoware/actions/runs/36505260417 — **SUCCESS**  
**Tip (main @ Overall Doc PASS handshake):** `46ad38731db5fea7d6dc862e05811a441eb5d671` (PR #98 Doc handshake; Assistant tree from #79 @ `199125a…`)  
**Method:** Business-intent AC check from Story #66 body + merged PR #79 evidence on **main** (`gh` remote reads of AssistantService / AssistantEndpoints / StageCThinAssistantTests; no clone) + KB Product QA / Spec / DevPlan / SD verifies + Security ProductQA/SD/Doc confirms + Doc weave (not code review). No invented requirements. Stage C named slice only. Soft **#41** Assistant OUT **owned by this Story** under **#67** wall (already BA PASS / `status:done`) — verify OwnAgent-only thin runtime; do **not** invent fuller Assistant / MCP / OpenAPI / #66 UX beyond X1 thin AC. Parent **#18** Spec HOLD (framing-only; does not Field-capture #66). Gate **#26** backlog · **#27** HOLD. Soft Soft CLOSE Soft HOLD SoR CLEAR (#83/#86/#88/#89/#91/#94/#96/#98). PoC **$0**. No MotorMarket/DC4. Do **not** CLOSE issue or flip `status:done` from this step.

## Binding AC (Story #66 — do not invent)

1. Participant can run a **thin** OwnAgent / Assistant runtime that reads/uses **their own** minimal Strategy (StrategyBody via existing FieldPolicy OwnAgent R/W when acting for owner) — strictly **1:1**; no multi-party invent
2. Assistant acts only as **OwnAgent** for the owning Participant (**P6** spirit: communication with own AI only) — not Counterparty / Stranger agent invent
3. Runtime uses **platform tools / gateway path** only (no raw DB / arbitrary internal HTTP) — hard-wall sibling **#67** binds FieldPolicy on tool I/O; this Story must not invent a prompt-only soft wall
4. Unauthenticated → **401**; wrong principal / cross-tenant misuse → fail-closed (**403** or **404** per Spec consistency); **uniform deny**; **no** LoginEmail / private Strategy of others / denied fields in model context or responses
5. Automated tests (or equivalent): owner OwnAgent can use own Strategy; stranger/cross-tenant deny; unauth deny; no LoginEmail in agent context packs
6. Documented as **X1** MVP **thin** — fuller / stronger Assistant → **V1**; free-form Strategy engine → **V1**; Strategy sandbox (**A5**) → **V4**; BYO / multi-LLM breadth → later stages
7. Soft Spec weave only (not a separate Story): where Assistant path touches SA-REV-MVP-C **OTel / audit / idempotent-offers** hooks already on roadmap, Spec notes the touchpoints — no new product surface invented

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | Thin OwnAgent reads/uses **own** minimal Strategy (StrategyBody via FieldPolicy OwnAgent R/W) — strictly **1:1**; no multi-party | **PASS** | PR #79 `AssistantService.InvokeAsync` → `FieldPrincipal.Agent(ownerSub)` + `GetByIdForOwnerAsync` + `_fieldPolicy.Evaluate(…, StrategyBody, Read, …)`; Facts: `Assistant_Invoke_WithOwnStrategy_Returns200WithStrategyContext`, `FieldPolicy_OwnAgent_AllowStrategyBody_ForOwner`, `FieldPrincipal_Agent_IsOwnAgentType`; no Counterparty/Stranger principal path in PR #79 (13 files). Product QA AC1 MET; Security ProductQA/SD/Doc pts 1–2 MET. |
| 2 | Acts only as **OwnAgent** for owning Participant (P6) — not Counterparty/Stranger invent | **PASS** | Facts: `FieldPolicy_OwnAgent_DenyStrategyBody_ForStranger`, `Assistant_Invoke_StrangerStrategyDeny_Returns404NoPrivateLeak`, `Assistant_Invoke_CrossTenantDeny_NoPrivateLeak`, `Assistant_Capabilities_OwnStrategiesOnly_NoOtherUserStrategies`, `Assistant_Invoke_OwnerOK_Returns200`. Product QA AC2 MET; Security pts 1 MET. |
| 3 | Platform tools / **#67 gateway** only — no raw DB / arbitrary HTTP; no prompt-only soft wall | **PASS** | `AssistantService` ctor requires `IAgentGateway`; tools only via `_gateway.IsToolAvailable` + `InvokeToolAsync`; Facts: `AssistantService_RequiresGateway`, `AssistantService_UsesGatewayForToolInvocation`, `AssistantService_RejectsUnallowedTool` (`TOOL_NOT_ALLOWED`), `Assistant_Invoke_UnallowedTool_ReturnsBadRequest`, `Gateway_DenyByDefault_UnknownTool`, `Gateway_AllowlistToolsOnly`. #67 wall already BA PASS / Product QA PASS — **bind only**, do not re-score. Product QA AC3 MET; Security pts 3 MET. |
| 4 | Unauth → **401**; wrong principal / cross-tenant → fail-closed (403/404); uniform deny; **no** LoginEmail / others’ Strategy / denied fields | **PASS** | `AssistantEndpoints`: empty sub → `Results.Unauthorized()`; `STRATEGY_NOT_FOUND`→404; `NOT_AGENT`/`ACCESS_DENIED`→403. Facts: `Assistant_Invoke_UnauthDeny_Returns401`, `Assistant_Capabilities_UnauthDeny_Returns401`, `Assistant_Invoke_InvalidAuth_Returns401`, `Assistant_Invoke_InvalidJwt_Returns401`, `Assistant_UnauthError_NoPrivateFieldsInBody`, stranger/cross-tenant 404 no-leak, LoginEmail deny Facts. Product QA AC4 MET; Security pts 4–5 MET. |
| 5 | Automated tests: owner OK; stranger/cross-tenant deny; unauth deny; no LoginEmail in agent context packs | **PASS** | `tests/Dealoware.Api.Tests/StageCThinAssistantTests.cs` **28 Facts** (Spec §8.1 matrix) on merge `199125a…`; CI SUCCESS runs 36505175931 @ `199125a` and 36505260417 @ `7043314`. Product QA AC5 MET. Soft: no live `dotnet` — CI + inventory equivalent. |
| 6 | Documented **X1 thin** — fuller → V1; free-form engine → V1; A5 sandbox → V4; BYO/multi-LLM → later | **PASS** | Spec `specs/…thin-assistant-runtime-x1.md` §6 OUT + Locked #8; `AssistantService` / `AssistantEndpoints` / `IAssistantService` OUT comments; PR #79 OUT table; no LLM SDK packages in #66 file set; provider-neutral `/assistant/invoke` + `/assistant/capabilities` only. Product QA AC6 MET; Security pts 7 MET. Overall Doc PASS (triad SoR #94/#96/#98 @ `46ad387`). |
| 7 | Soft Spec weave only for OTel/audit/idempotent — **no 5th Story**; do not implement #67/#68/#69 here (bind wall only) | **PASS** | Spec §4 Soft weave only; PR #79 file set = Domain/Application/Api Assistant + DI + tests only (no #67 re-impl, no #68 meters, no #69 UI); soft OTel = scrub/`StrippedFieldCount` from #67 bind. Product QA AC7 MET; Security pts 8–9 MET. |

## Out of scope held

| OOS item (Story #66 + locks) | Held? | Evidence |
|------------------------------|-------|----------|
| Fuller / stronger Assistant (**X1** remainder → **V1**) | **Yes** | Spec §6 OUT; PR OUT comments; no LLM SDK; Product QA AC6; Security pts 7 |
| Free-form Strategy evaluation engine; **A5** sandbox → **V4** | **Yes** | Spec §6 OUT; PR OUT; Product QA / Security OUT locked |
| Agent/tool hard wall implementation details (**#67**) — bind only | **Yes** | Consumes `IAgentGateway`; #67 already BA PASS / Product QA PASS; do not re-score; Product QA AC3 |
| A8 meters/budgets (**#68**); basic UI / first-party bot (**#69**) | **Yes** | Not in PR #79; Product QA OUT; Security pts 8 |
| Inventing a **5th Story** for OTel/audit/idempotent | **Yes** | Soft Spec weave only; Product QA AC7; Security pts 8–9 |
| MCP; public OpenAPI package; multi-party; MotorMarket / DC4; PoC/MVP spend; Cognito/SSO inventing | **Yes** | No Cognito/MM/DC4/MCP/OpenAPI invent in #66 file set; PoC $0; Security pts 8–9 |
| Unlocking gate **#26** before Stage C delivery; gate **#27** HOLD | **Yes** | #26 stays backlog; #27 HOLD; CPM comments + Soft locks; Security pts 8 |
| Parent **#18** Spec HOLD / Field-capture #66; requesting `ready-for-dev` / Spec unlock invent | **Yes** | Parent framing-only; Spec HOLD; Product QA / Security: does not Field-capture #66 |
| Soft **#41** Assistant OUT claimed as Stage B delivery | **Yes** | Soft #41 closes by **#66+#67 under wall** — not Stage B; #66 MERGED under #67 wall → Soft #41 OUT closes by delivery path |
| Inventing fuller Assistant / MCP / OpenAPI / UX beyond X1 thin AC | **Yes** | Thin string I/O only; OwnAgent-only; #67 bind; Product QA / Security / Doc HOLD fuller invent |

## Soft gaps (non-blocking for BA business verify)

- No live `dotnet restore|build|test` on evidence box — accepted; CI SUCCESS runs 36505175931 + 36505260417 + `StageCThinAssistantTests` ×28 inventory via `gh`.
- `AssistantService_RequiresGateway` is a construction presence assert — accepted; stronger evidence `UsesGatewayForToolInvocation` + `RejectsUnallowedTool` + allowlist deny-by-default (Product QA / Security soft note).
- Thin X1 consumes StrategyBody via Evaluate **Read** + owner-scoped repo; StrategyBody **write** through Assistant is OUT (fuller → V1) — FieldPolicy OwnAgent R/W gate held by unit tests (accepted).
- Soft OTel/audit — rely on #67 scrub/`StrippedFieldCount` weave; no 5th Story in this PR (accepted).
- Soft multi-provider = interchangeable clients of **one** Dealoware API — no provider-specific invent in #66 (accepted).
- Soft Soft CLOSE Soft HOLD SoR chain CLEAR (#83 @ `32014a6`, #86 @ `7043314`, #88 @ `bccdd6e`, #89 @ `460a648`, #91 @ `3400480`, #94 @ `08f8ddf`, #96 @ `1dfc2cd`, #98 @ `46ad387`); tip main @ `46ad387`. Eng `status:done` HOLD until CBA confirm after BAQA. GH issue state CLOSED ≠ BA-verify / ≠ `status:done`.

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Spec QA | PASS | `verification/2026-09-28__spec__verification__mvp-stage-c-thin-assistant-runtime-x1.md` + Spec Security confirm 10/10 |
| Dev Plan QA | PASS | `verification/2026-09-28__devplan__verification__mvp-stage-c-thin-assistant-runtime-x1.md` + DevPlan Security confirm 10/10 |
| SD / Dev Code QA | PASS | `verification/2026-09-28__sd__verification__mvp-stage-c-thin-assistant-runtime-x1.md` |
| SD Security | PASS (1–10 MET) | `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-sd-qa-confirm.md` |
| CQ | `cq:no-refactor` | Issue labels + CQ assessment; Chief CQ confirm |
| Product QA | PASS | `qa/2026-09-28__qa__qa-report__mvp-stage-c-thin-assistant-runtime-x1.md` |
| Product QA Security | PASS (1–10 MET) | `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-productqa-qa-confirm.md` |
| Product QA handshake / report SoR | CLEAR | PR #91 @ `3400480` · PR #89 @ `460a648` |
| Overall Doc | PASS | Doc triad SoR #94/#96/#98 @ `46ad387`; Doc Security Senior 10/10 + Security QA 10/10 |
| Doc weave | ISSUED → Overall Doc PASS | `ops/2026-09-28__docs__ops__mvp-stage-c-thin-assistant-doc-security-weave.md` |
| Soft Soft CLOSE Soft HOLD SoR | CLEAR | PRs #83/#86/#88/#89/#91/#94/#96/#98 MERGED; tip `46ad387` |
| Sibling #67 wall | BA PASS / `status:done` | `verification/2026-09-28__ba__verification__mvp-stage-c-agent-tool-hardwall-scrubber.md` — **bind only** |

## Recommendation to CBA

**PASS** — deliverable meets Story #66 business AC (thin OwnAgent-only Strategy-driven Assistant runtime; StrategyBody via FieldPolicy OwnAgent R/W for owner — 1:1; P6 OwnAgent only; mandatory #67 gateway bind — no prompt-only soft wall; unauth 401 / wrong-principal fail-closed 403–404 / no LoginEmail leak; StageCThinAssistantTests ×28 + CI SUCCESS; documented X1 thin with fuller→V1 / free-form→V1 / A5→V4; Soft OTel/audit/idempotent weave only — no 5th Story), and OOS held (fuller Assistant / free-form / A5 / BYO OUT; #67 bind-only / #68/#69 separate; no 5th Story; MCP/OpenAPI/Cognito/MotorMarket/DC4 OUT; #26 backlog; #27 HOLD; parent #18 Spec HOLD / not Field-captured; Soft #41 OUT closes via #66+#67 under wall — not Stage B; PoC $0). Soft gaps non-blocking. Soft **#41** Assistant OUT owned here under #67 wall (already BA PASS). Hand to BAQA for verify-QA; eng `status:done` HOLD until CBA final confirm after BAQA. Do **not** CLOSE issue #66 from this step (GH CLOSED ≠ BA-verify), do **not** flip `status:done`, do **not** unlock #26/#27/#18, do **not** invent fuller Assistant / MCP / OpenAPI / UX beyond X1 thin AC, do **not** merge sibling tracks from this step. Stage C named slice only. PoC $0.
