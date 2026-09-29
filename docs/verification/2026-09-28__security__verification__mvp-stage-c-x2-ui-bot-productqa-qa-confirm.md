# Security QA — MVP Stage C #69 Basic UI (X2 partial) Product QA vs Chief Security Product QA checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 Product-QA-step Security points MET)  
**Asked by:** Senior Security / Senior Product QA / Dealoware QAQA / Chief Security (PRIORITY — Product QA HOLD PASS pts 1–9; pt 10 handshake; Soft HOLD score LIFTED after Senior PASS)  
**Product QA report:** `qa/2026-09-28__qa__qa-report__mvp-stage-c-basic-ui-first-party-bot-x2.md` (HOLD PASS; Sec pts 1–9 **EVIDENCED**; pt **10 HOLD** — correct process)  
**Chief checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-checklist.md` (10 points)  
**SoR checklist twin (GitHub):** Soft Soft CLOSE Soft HOLD SoR twin **CLEAR** PR **#112** @ `fdc26b9fee8c8db542bb58360626a2afa2d2409e` (short `fdc26b9`) → `docs/verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-checklist.md`  
**Senior Security Product QA points-review:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-points-review.md` (**PASS** 10/10) — cited; independent re-score **agrees**  
**Prior SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-qa-confirm.md` (PASS 10/10; SoR twin PR **#106** @ `259813e`) — supporting only; does **not** close Product-step pt 10  
**SD points-review (cross-cite):** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-points-review.md` (PASS 10/10)  
**Format ref:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-productqa-qa-confirm.md` · `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-qa-confirm.md` · `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-productqa-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/100 (**MERGED**)  
**Impl merge commit:** `da612100fe0bac8341b9d95f5d8f92c3b6385c50` (short `da61210`) — verified `get_pull_request` merged=true  
**CI (impl @ da61210):** https://github.com/ioaikh/dealoware/actions/runs/36509394069 — **SUCCESS**  
**Tests:** `tests/Dealoware.Api.Tests/StageCBasicUITests.cs` (×32 Facts; present @ `da61210`)  
**Issue:** https://github.com/ioaikh/dealoware/issues/69 · Basic UI and/or one first-party bot surface (X2 partial)  
**Parent:** #18 · Stage C · roadmap X2 MVP partial — framing-only; does **not** Field-capture #69  
**Surface pick:** Spec-locked **basic UI** only (exactly-one first-party bot **OUT** / not required for minimum)  
**Siblings:** #66 · #67 · #68 — keep separate Product QA; cross-ref only; bind #67 when UI invokes Assistant; #68 budget status minimal only — **do not re-score #66/#67/#68**  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-qa-confirm.md`  
**Tip:** Soft Soft CLOSE Soft HOLD SoR tip `fdc26b9` / impl `da61210`  
**Constraints:** Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (after this confirm + Soft Soft CLOSE Soft HOLD handshake SoR — do **not** invent handshake SoR). Soft Soft CLOSE Soft HOLD status:done until CBA. Soft Soft CLOSE Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft Soft CLOSE Soft HOLD SoR Product QA twin **CLEAR** PR **#112** @ `fdc26b9` (Product QA SoR ≠ Doc SoR). Soft Soft CLOSE Soft HOLD Dev Code QA / merge already lifted for #69 SD path is **separate**. Soft **#41** CLOSED via **#66+#67** (do not re-open). Gate **#26** backlog; Gate **#27** HOLD. PoC **$0**; no Cognito/MM/DC4/vault invent. Soft: no-live-dotnet OK (CI SUCCESS + StageCBasicUITests ×32). Path **x2-ui-bot**. Scope **#69 ONLY**.

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security Product QA checklist | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-checklist.md` | Binding 10 points — ISSUED on KB |
| SoR checklist twin | PR **#112** @ `fdc26b9` → `docs/verification/…x2-ui-bot-productqa-checklist.md` | **CLEAR** |
| Product QA report | `qa/2026-09-28__qa__qa-report__mvp-stage-c-basic-ui-first-party-bot-x2.md` | HOLD PASS pts 1–9 EVIDENCED; pt 10 HOLD |
| Senior Security Product QA points-review | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-points-review.md` | **PASS** 10/10 — cited; agrees |
| Prior SD Security PASS | `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-qa-confirm.md` | PASS 10/10 — supporting only; does **not** close Product pt 10 |
| Impl PR #100 | MERGED @ `da612100fe0bac8341b9d95f5d8f92c3b6385c50` | Verified `get_pull_request`; 5 files; +1624/−0 |
| CI impl | run 36509394069 @ `da61210` | **SUCCESS** |
| Tests | `StageCBasicUITests.cs` ×**32** @ `da61210` | Cited by Senior + Product QA + prior SD PASS |

## Soft gaps accepted (non-blocking)

| Soft gap | Disposition |
|----------|-------------|
| No live `dotnet test` on evidence box | **Accepted** — CI SUCCESS run 36509394069 @ `da61210` + `StageCBasicUITests.cs` ×32 + prior SD Security PASS |
| UI `budget.totalUnits` vs DTO `limitUnits` display naming | **Accepted** — display soft residual; not security invent (agrees Senior + prior SD Soft notes) |
| Shallow `Budget_CutoffRespected_AfterExhaustion` (initial IsExhausted only) | **Accepted** — non-blocking soft residual |
| No browser/E2E (API HttpClient + static GET only) | **Accepted** — Spec §8.1 matrix covered by StageCBasicUITests; UI ≠ security boundary |
| JWT discarded / ApiKey-only localStorage | **Accepted** — UI ≠ security boundary; authz server-side |
| Soft Soft CLOSE Soft HOLD SoR / Doc | **Accepted** — SoR twin CLEAR PR **#112** @ `fdc26b9`; this confirm does **not** invent handshake Soft Soft CLOSE Soft HOLD SoR / Docs unlock; Soft Soft CLOSE Soft HOLD Doc until Product QA PASS |
| Soft Soft CLOSE Soft HOLD status:done | **Accepted** — held until CBA; not cleared by this confirm |
| Soft Soft CLOSE Soft HOLD multi-provider Spec/doc rewrite | **Accepted** — Soft HOLD until BM multi-provider start |
| Soft Soft CLOSE Soft HOLD Dev Code QA lift on SD path is separate | **Accepted** — this is Product QA track |
| Soft **#41** CLOSED via **#66+#67** | **Accepted** — do **not** re-open / re-score |
| Do **not** re-score #66/#67/#68 | **Accepted** — bind/cross-ref only |
| Product QA SoR ≠ Doc SoR | **Accepted** — separate tracks |
| Gate #26 backlog; Gate #27 HOLD; parent #18 not Field-capture | **Accepted** |

## Independent re-score (Security QA)

Score vs **official Product QA checklist 1–10** only. Scope **#69 ONLY**. Bind #67 / #68 cite only — **do not re-score**. Agree Senior when evidence matches.

| # | Point | Senior | Security QA | Evidence |
|---|-------|--------|-------------|---------|
| 1 | No privileged back doors — basic UI does not bypass FieldPolicy / #67; no UI-only security | MET | **MET** | QA Sec #1 / AC2 **EVIDENCED** + SD PASS cross-cite @ `da61210`: `FieldPolicy_*` ×3; `Profile_ServerSideFieldProjection_NoLoginEmail`; `Search_DiscoveryOmitsPrivateFields`; `Strategy_CrossTenant_NoPrivateFieldLeak`; Program.cs static-only (`UseDefaultFiles`/`UseStaticFiles`); PR #100 5-file UI+tests only. Prior SD: `app.js` "UI is NOT a security boundary"; all protected flows via `apiCall(...)`. |
| 2 | Authn fail-closed 401 / wrong principal 403/404; no private-field leak via UI payloads | MET | **MET** | QA Sec #2 / AC3 **EVIDENCED**: `*_Unauth_Returns401` ×7; `InvalidAuth_Returns401`; `Unauth_NoPrivateFieldsInResponse_Profile`; `Artifact/Strategy/Negotiation_CrossTenant_Returns404`; `Strategy_CrossTenant_NoPrivateFieldLeak`; `Budget_CrossTenant_CannotAccessOther`. CI SUCCESS 36509394069 @ `da61210`. |
| 3 | Assistant path binds #67 — reject client prompt-only soft wall (do **not** re-score #67) | MET | **MET** | QA Sec #3 **EVIDENCED**: `Assistant_ToolInvoke_UsesGateway`; `Assistant_UnallowedTool_Denied`; `Assistant_ToolResult_NoLoginEmail`; `Assistant_Capabilities_NoLoginEmailTool`; `Assistant_StrategyBinding_OwnOnly`. Bind only — #67 Product QA already PASSed separately. |
| 4 | Budget status minimal only (#68) — not V3 admin / owner cost UI | MET | **MET** | QA Sec #4 / AC6 **EVIDENCED**: `Budget_Status_ReturnsMinimalFields`; `Budget_Status_NoAdminPrivilege`; `Budget_Status_OwnBudgetOnly`; `Budget_CutoffRespected_AfterExhaustion` (shallow soft gap scored); Unauth/CrossTenant budget Facts. Bind only — do not re-score #68. |
| 5 | Surface = basic UI; Soft HOLD multi-provider; bot not required | MET | **MET** | QA Sec #5 / AC1 **EVIDENCED**: `StaticUI_IndexHtml/AppJs/StylesCss_Returns200`; `wwwroot/index.html` + `app.js` + `styles.css`; no bot channel in PR #100; Soft HOLD multi-provider until BM multi-provider start; Spec Locked #0/#5. |
| 6 | OUT locked X2 — OpenAPI→V1; MCP→V5; no 5th Story | MET | **MET** | QA Sec #6 / AC5 **EVIDENCED**: `Health_StillAuthNone`; 5-file #69-only diff; Spec §6 OUT + Locked #8; no MCP/OpenAPI invent; Soft OTel/audit weave only on named #66/#67/#68. |
| 7 | Consume tip authz (#5 + FieldPolicy) — no Stage A/B ACL rewrite | MET | **MET** | QA Sec #7 **EVIDENCED**: Program.cs +3 only; FieldPolicy consume; no ACL Story rewrite in diff; CQ `cq:no-refactor` @ `da61210`. |
| 8 | No Gate unlock / no invent — #26 backlog; #27 HOLD; Soft #41 CLOSED; keep #66/#67/#68 separate | MET | **MET** | QA Sec #8 **EVIDENCED**: Diff clean of Cognito/MM/DC4/vault; Soft **#41** CLOSED via **#66+#67**; Soft HOLD #18 framing-only; Gate **#26** backlog · Gate **#27** HOLD; siblings Product QA kept separate. |
| 9 | Cost / spend — PoC $0; spend → COO→CEO | MET | **MET** | QA Sec #9 **EVIDENCED**: No LLM/AWS/IdP spend in PR #100; static SPA + local API only. PoC **$0**. Any named LLM/API spend → COO → CEO. |
| 10 | Handshake close — Product QA HOLD until Security QA; #18 not Field-capture | MET | **MET** | Product QA correctly HOLDs PASS / pt 10 until this Security QA `…x2-ui-bot-productqa-qa-confirm.md`. Prior SD Security PASS does **not** close Product-step pt 10. Soft Soft CLOSE Soft HOLD Dev Code QA lift on SD path is separate. Parent #18 framing-only — does **not** Field-capture #69. **This confirm closes Product QA Security gate.** Soft Soft CLOSE Soft HOLD Doc / handshake Soft Soft CLOSE Soft HOLD SoR **not invented**. Soft Soft CLOSE Soft HOLD status:done until CBA. Soft Soft CLOSE Soft HOLD multi-provider Soft HOLD stands. |

## Alignment with Senior Security done-list

Senior Security Product QA points-review scored all 10 **MET** (**PASS** 10/10) vs Product QA HOLD PASS report + MERGED PR #100 @ `da61210` + CI SUCCESS run 36509394069 + StageCBasicUITests ×32 + SoR twin CLEAR PR **#112** @ `fdc26b9` + prior #69 SD PASS. Independent Security QA re-score vs Chief Product QA checklist + Product QA report + Senior done-list + PR #100 merge verify — **all 10 MET**; **agrees** Senior; **no reopen**. Soft notes align (display naming; shallow cutoff Fact; no live `dotnet`; no browser/E2E; Soft Soft CLOSE Soft HOLD Doc / status:done / multi-provider; Soft #41 CLOSED; do not re-score #66/#67/#68; Gate #26 backlog / #27 HOLD; PoC $0). No contradiction.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| No UI-only security; FieldPolicy + #67 server-side | Held (FieldPolicy_* + Program.cs static-only + apiCall) |
| Authn fail-closed 401 / cross-tenant 403–404; no private leak | Held (Unauth_*/CrossTenant_* Facts + CI SUCCESS) |
| Assistant path binds #67; reject client prompt-only soft wall; do not re-score #67 | Held (Assistant_* Facts; bind only) |
| Budget status minimal only (#68); not V3 admin; do not re-score #68 | Held (Budget_Status_* Facts) |
| Surface = basic UI; Soft HOLD multi-provider; bot not required | Held (StaticUI_* + no bot channel) |
| OUT locked X2; no MCP/OpenAPI/5th Story | Held (5-file #69-only diff) |
| Consume tip authz; no Stage A/B ACL rewrite | Held (Program.cs +3; cq:no-refactor) |
| Soft #41 CLOSED via #66+#67; Gate #26 backlog; #27 HOLD; siblings separate | Held |
| PoC $0; no Cognito/MM/DC4/vault invent | Held |
| Soft Soft CLOSE Soft HOLD Doc / status:done / multi-provider; SoR twin CLEAR #112 — no invent handshake | Held |
| Evidence on impl `da61210` + CI SUCCESS 36509394069; path x2-ui-bot; scope #69 ONLY | Held |

## Gaps

**None.** Soft residuals (display naming; shallow cutoff Fact; no live `dotnet`; no browser/E2E) accepted non-blockers. Soft Soft CLOSE Soft HOLD handshake SoR not invented — not a GAP.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Next: Chief Security **PASS/HOLD** to CPM + Chief QA + Senior PM. Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (after this + Soft Soft CLOSE Soft HOLD handshake SoR — do **not** invent handshake SoR / claim Docs unlock from this file alone). Soft Soft CLOSE Soft HOLD status:done until CBA. Soft Soft CLOSE Soft HOLD multi-provider Spec/doc rewrite Soft HOLD stands until BM multi-provider start. Soft Soft CLOSE Soft HOLD Dev Code QA lift on SD path remains separate. Soft **#41** CLOSED via **#66+#67** — do not re-open. Gate **#26** backlog; Gate **#27** HOLD. Parent #18 does **not** Field-capture #69. Do **not** re-score #66/#67/#68. Product QA SoR ≠ Doc SoR. Cost/critical: none. PoC **$0**. Tip Soft Soft CLOSE Soft HOLD SoR `fdc26b9` / impl `da61210`.
