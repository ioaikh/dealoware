# BA business verification — Story #69 Basic UI and/or one first-party bot surface (X2 partial)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-28 (BA verify executed ~10:13 PM ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/69  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/100 (**MERGED** @ main `da612100fe0bac8341b9d95f5d8f92c3b6385c50`; MergedAt 2026-09-28 9:45:24 PM ET)  
**CI (impl @ da61210):** https://github.com/ioaikh/dealoware/actions/runs/36509394069 — **SUCCESS**  
**Tip (main @ Overall Doc PASS handshake + INDEX):** `4a22434f6a86d00bb5cb57c7f8dc16e9a1cc97d7` (PR #123 INDEX Overall Doc PASS; Doc handshake PR #122 @ `a72777b`; UI tree from #100 @ `da61210…`)  
**Method:** Business-intent AC check from Story #69 body + merged PR #100 evidence on **main** (`gh` remote reads of wwwroot SPA / Program.cs static host / StageCBasicUITests; no clone) + KB Product QA / Spec / DevPlan / SD verifies + Security ProductQA/SD/Doc confirms + Doc weave (not code review). No invented requirements. Stage C named slice only — **X2 MVP partial** Spec-locked **basic UI** (exactly-one first-party bot **OUT** of Spec minimum; SA and/or allows either). Soft HOLD invent beyond X2 partial: **no** MCP, **no** public OpenAPI, **no** multi-channel marketplace. Soft Spec ≠ 5th Story. Soft **#41** CLOSED via **#66+#67** (do **not** re-open). Soft HOLD multi-provider. Gate **#26** backlog · **#27** HOLD. Parent **#18** Soft HOLD / framing-only (does not Field-capture #69). Soft Soft CLOSE Soft HOLD SoR CLEAR (#106/#112/#115/#117/#119/#121/#122/#123). PoC **$0**. No MotorMarket/DC4. Do **not** CLOSE issue or flip `status:done` from this step.

## Binding AC (Story #69 — do not invent)

1. Deliver **basic UI** and/or **exactly one** first-party bot channel sufficient to exercise MVP Participant flows already on path (auth, artifacts, discovery, negotiation/offers, Strategy, thin Assistant as siblings land) — Spec picks UI and/or bot without inventing a multi-channel marketplace
2. Surface respects existing authz / FieldPolicy — **no** UI-only filtering as security; agent/bot path binds hard-wall sibling **#67** when Assistant is used
3. **No** MCP; **no** public OpenAPI package required for this Story
4. Unauthenticated protected actions fail-closed; no private-field leakage via UI/bot payloads
5. Automated tests or equivalent evidence for critical authz paths on the chosen surface
6. Documented as **X2** MVP **partial** — OpenAPI/webhooks → **V1**; MCP breadth → **V5**
7. Soft: budget status (**#68**) may appear **minimally** if needed to respect cutoff — **not** platform-owner admin / mature cost UI

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | Deliver **basic UI** and/or **exactly one** first-party bot — Spec picks without inventing multi-channel marketplace | **PASS** | Spec Locked #0 = **basic UI** minimum (bot OUT of Spec minimum — allowed by and/or). PR #100: `wwwroot/index.html` + `app.js` + `styles.css` + `Program.cs` `UseDefaultFiles`/`UseStaticFiles` only (5 files, +1624/−0). Facts: `StaticUI_IndexHtml/AppJs/StylesCss_Returns200`. Nav exercises Profile/Artifacts/Discovery/Negotiations/Strategies/Assistant/Budget. No bot channel / multi-bot invent in #100. Product QA AC1 MET; Spec QA AC map PASS; Security ProductQA/SD/Doc pts 5 MET. |
| 2 | Authz/FieldPolicy server-side — **no** UI-only security; bind **#67** when Assistant used | **PASS** | app.js: "UI is NOT a security boundary"; all actions via API. Facts: `FieldPolicy_*` ×3; `Profile_ServerSideFieldProjection_NoLoginEmail`; `Search_DiscoveryOmitsPrivateFields`; `Assistant_ToolInvoke_UsesGateway`; `Assistant_UnallowedTool_Denied`; `Assistant_ToolResult_NoLoginEmail`; `Assistant_Capabilities_NoLoginEmailTool`; `Assistant_StrategyBinding_OwnOnly`. Program.cs +3 static-only (no authz rewrite). #67 wall already BA PASS — **bind only**, do not re-score. Product QA AC2 MET; Security pts 1/3 MET. |
| 3 | **No** MCP; **no** public OpenAPI package | **PASS** | Spec §6 OUT + Locked #5/#8; PR #100 5-file #69-only diff (wwwroot + Program static + StageCBasicUITests); `Health_StillAuthNone`; no MCP/OpenAPI/Swagger invent. Product QA AC5 MET; Security pts 6 MET. |
| 4 | Unauth fail-closed; wrong principal / cross-tenant → 403/404; no private-field leak via UI/bot payloads | **PASS** | Facts: `*_Unauth_Returns401` ×7; `InvalidAuth_Returns401`; `Unauth_NoPrivateFieldsInResponse_Profile`; `Artifact/Strategy/Negotiation_CrossTenant_Returns404`; `Strategy_CrossTenant_NoPrivateFieldLeak`; `Budget_CrossTenant_CannotAccessOther`. Product QA AC3 MET; Security pts 2 MET. |
| 5 | Automated tests (or equivalent) for critical authz paths on chosen surface | **PASS** | `tests/Dealoware.Api.Tests/StageCBasicUITests.cs` **×32 Facts** on merge `da61210…`; CI SUCCESS run 36509394069 @ `da61210`. Spec §8.1 matrix covered. Product QA AC4 MET. Soft: no live `dotnet` — CI + inventory equivalent; no browser/E2E (API HttpClient + static GET — accepted). |
| 6 | Documented **X2** MVP **partial** — OpenAPI/webhooks → **V1**; MCP → **V5** | **PASS** | Spec `specs/…basic-ui-first-party-bot-x2.md` §6 OUT + Locked #8; PR #100 OUT; Overall Doc PASS (triad SoR #117/#119/#122 + INDEX #123 @ `4a22434`); Doc weave `ops/…x2-ui-bot-doc-security-weave.md`. Product QA AC5 MET; Security Doc pts 6 MET. Soft OTel/audit weave only on named #66/#67/#68 — **no 5th Story**. |
| 7 | Soft: **#68** budget status **minimal** only — not V3 admin / mature cost UI | **PASS** | Facts: `Budget_Status_ReturnsMinimalFields`; `Budget_Status_NoAdminPrivilege`; `Budget_Status_OwnBudgetOnly`; `Budget_CutoffRespected_AfterExhaustion` (shallow soft gap scored). UI `loadBudgetStatus` remaining/exhausted only. #68 already BA PASS / Product QA PASS — **bind only**, do not re-score. Product QA AC6 MET; Security pts 4 MET. |

## Out of scope held

| OOS item (Story #69 + locks) | Held? | Evidence |
|------------------------------|-------|----------|
| Public OpenAPI / webhooks package (**A7** / **X2** remainder → **V1**) | **Yes** | Spec §6 OUT; PR #100 no OpenAPI; Product QA / Security pts 6 |
| MCP breadth → **V5** | **Yes** | Spec §6 OUT; no MCP invent in #100; Security pts 6 |
| Platform-owner admin suite; mature cost UI → **V3** | **Yes** | Budget status minimal only; Product QA AC6; Security pts 4 |
| Inventing multi-bot / multi-channel marketplace; fuller first-party bot beyond Spec minimum | **Yes** | Spec Locked #0 = basic UI only; bot OUT of Spec minimum; PR #100 no bot channel; Soft HOLD multi-provider |
| MotorMarket / DC4; spend; Cognito/SSO/vault invent | **Yes** | Diff clean; PoC $0; Security pts 8–9 |
| Inventing a **5th Story** for OTel/audit/idempotent | **Yes** | Soft Spec weave only on named #66/#67/#68; Product QA / Security pts 6/8 |
| Soft Spec ≠ 5th Story; inventing beyond X2 partial named slice | **Yes** | X2 basic UI only; Spec/DevPlan/SD/Product QA/Doc HOLD invent |
| Soft **#41** Assistant OUT claimed as Stage B / re-opened from #69 | **Yes** | Soft #41 CLOSED via **#66+#67** only — do not re-open from #69 |
| Re-scoring / implementing #66/#67/#68 here | **Yes** | Bind only (Assistant path → #67; budget min → #68); siblings already BA PASS / Product QA PASS |
| Unlocking gate **#26** before Stage C delivery; gate **#27** HOLD | **Yes** | #26 stays backlog; #27 HOLD; CPM comments + Soft locks; Security pts 8 |
| Parent **#18** Soft HOLD / Field-capture #69; requesting Spec unlock invent | **Yes** | Parent framing-only; Soft HOLD; does not Field-capture #69 |
| Marketing eng Story | **Yes** | Marketing research-only; Spec OUT |

## Soft gaps (non-blocking for BA business verify)

- No live `dotnet restore|build|test` on evidence box — accepted; CI SUCCESS run 36509394069 @ `da61210` + `StageCBasicUITests` ×32 inventory via `gh`.
- No browser/E2E — API HttpClient + static GET only (Chief prefer; UI ≠ security boundary) — accepted.
- DTO/UI display residuals: `totalUnits` vs `limitUnits`; subjectEntity vs entities; negotiation party naming — polish only — accepted.
- Shallow `Budget_CutoffRespected_AfterExhaustion` (asserts not-exhausted; no burn) — #68 domain already cq:no-refactor; bind only — accepted.
- Negotiations list/detail only (no create offer/neg forms) — intentional X2 partial OUT — accepted.
- JWT discarded / ApiKey-only localStorage — UI ≠ security boundary; authz server-side — accepted.
- Soft HOLD multi-provider until BM multi-provider start — accepted.
- Soft Soft CLOSE Soft HOLD SoR chain CLEAR (#106 @ `259813e`, #112 @ `fdc26b9`, #115 @ `3b7b323`, #117 @ `95e04e4`, #119 @ `bdc828e`, #121 @ `f49e686`, #122 @ `a72777b`, #123 @ `4a22434`); tip main @ `4a22434`. Eng `status:done` HOLD until CBA confirm after BAQA. Do **not** CLOSE issue or flip `status:done` from this step.

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Spec QA | PASS | `verification/2026-09-28__spec__verification__mvp-stage-c-basic-ui-first-party-bot-x2.md` + Spec Security confirm 10/10 (PR #71/#72) |
| Dev Plan QA | PASS | `verification/2026-09-28__devplan__verification__mvp-stage-c-basic-ui-first-party-bot-x2.md` + DevPlan Security confirm 10/10 |
| SD / Dev Code QA | PASS | `verification/2026-09-28__sd__verification__mvp-stage-c-basic-ui-first-party-bot-x2.md` |
| SD Security | PASS (1–10 MET) | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-qa-confirm.md` (SoR PR #106 @ `259813e`) |
| CQ | `cq:no-refactor` | Issue labels + `specs/2026-09-28__cq__assessment__mvp-stage-c-basic-ui-no-refactor.md` |
| Product QA | PASS | `qa/2026-09-28__qa__qa-report__mvp-stage-c-basic-ui-first-party-bot-x2.md` (SoR PR #121 @ `f49e686`) |
| Product QA Security | PASS (1–10 MET) | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-qa-confirm.md` (handshake SoR PR #115 @ `3b7b323`; checklist SoR PR #112 @ `fdc26b9`) |
| Overall Doc | PASS | Doc triad SoR #117 @ `95e04e4` · #119 @ `bdc828e` · #122 @ `a72777b` · INDEX #123 @ `4a22434`; Doc Security Senior 10/10 + Security QA 10/10 |
| Doc weave | ISSUED → Overall Doc PASS | `ops/2026-09-28__docs__ops__mvp-stage-c-x2-ui-bot-doc-security-weave.md` |
| Soft Soft CLOSE Soft HOLD SoR | CLEAR | PRs #106/#112/#115/#117/#119/#121/#122/#123 MERGED; tip `4a22434` |
| Sibling #66 / #67 / #68 | BA PASS / Product QA PASS | Bind only — do not re-score; Soft #41 OUT via #66+#67 |

## Recommendation to CBA

**PASS** — deliverable meets Story #69 business AC (Spec-locked basic UI X2 MVP partial exercising auth/artifacts/discovery/negotiations/Strategy/thin Assistant/budget-min surfaces; authz/FieldPolicy server-side with #67 bind when Assistant used — no UI-only security; no MCP / no public OpenAPI; unauth fail-closed + no private-field leak; StageCBasicUITests ×32 + CI SUCCESS; documented X2 partial with OpenAPI→V1 / MCP→V5; Soft #68 budget status minimal only — not V3 admin), and OOS held (OpenAPI/webhooks → V1; MCP → V5; multi-bot marketplace OUT; bot OUT of Spec minimum; mature cost UI → V3; no 5th Story; Soft #41 CLOSED via #66+#67 — do not re-open; #66/#67/#68 bind-only; Cognito/MotorMarket/DC4/multi-provider OUT; #26 backlog; #27 HOLD; parent #18 Soft HOLD / not Field-captured; PoC $0). Soft gaps non-blocking. Soft HOLD invent beyond X2 partial. Hand to BAQA for verify-QA; eng `status:done` HOLD until CBA final confirm after BAQA. Do **not** CLOSE issue #69 from this step, do **not** flip `status:done`, do **not** unlock #26/#27/#18, do **not** invent MCP / public OpenAPI / multi-channel marketplace / 5th Story / multi-provider, do **not** merge sibling tracks from this step. Stage C named slice only. PoC $0.
