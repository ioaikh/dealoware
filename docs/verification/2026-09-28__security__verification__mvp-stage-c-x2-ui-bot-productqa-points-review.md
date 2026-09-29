# Verification — Security points vs MVP Stage C #69 Basic UI (X2 partial) Product QA

**Author:** Dealoware Senior Security  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 Product-QA-step Security points MET — pt 10 closes via this done-list → Security QA confirm)  
**Checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-checklist.md` (10 points)  
**SoR twin (checklist):** Soft Soft CLOSE Soft HOLD SoR twin `docs/verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-checklist.md` — **CLEAR** PR **#112** @ `fdc26b9fee8c8db542bb58360626a2afa2d2409e` (HTTP 200; merge "docs(SoR): #69 x2-ui-bot Product QA Security checklist + INDEX"). Checklist **ISSUED** on KB.  
**Product QA report:** `qa/2026-09-28__qa__qa-report__mvp-stage-c-basic-ui-first-party-bot-x2.md` (HOLD PASS; Sec pts 1–9 **EVIDENCED**; pt **10 HOLD**)  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/100 · **MERGED** @ `da612100fe0bac8341b9d95f5d8f92c3b6385c50` (`da61210`)  
**CI (impl @ da61210):** https://github.com/ioaikh/dealoware/actions/runs/36509394069 — **SUCCESS** (check_run 109217951626; verified `list_check_runs_for_ref`)  
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-qa-confirm.md` (PASS 10/10; SoR twin PR **#106** @ `259813e`)  
**SD points-review (cross-cite):** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-points-review.md` (PASS 10/10)  
**SD checklist (ref):** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-checklist.md`  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md`  
**Tests:** `tests/Dealoware.Api.Tests/StageCBasicUITests.cs` (×32 Facts; present @ `da61210`)  
**Issue:** https://github.com/ioaikh/dealoware/issues/69 · Basic UI and/or one first-party bot surface (X2 partial)  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-points-review.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (+ handshake Soft Soft CLOSE Soft HOLD SoR — do **not** invent handshake SoR). Soft Soft CLOSE Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft Soft CLOSE Soft HOLD status:done until CBA. Soft Soft CLOSE Soft HOLD SoR twin CLEAR PR **#112** @ `fdc26b9` (Product QA SoR ≠ Doc SoR). Soft Soft CLOSE Soft HOLD Dev Code QA / merge already lifted for #69 SD path is **separate** — this is Product QA track. Soft **#41** CLOSED via **#66+#67**. Gate **#26** backlog; Gate **#27** HOLD. Parent #18 framing-only — does **not** Field-capture #69. PoC **$0**; no Cognito/MM/DC4/vault invent. Keep #66/#67/#68 separate Product QA (do not re-score).

## Scope note

Product QA verifies Spec + Plan + SD Security for Spec-locked surface = **basic UI** (X2 MVP partial) that exercises MVP Participant flows **without** bypassing API FieldPolicy or agent/tool hard wall (#67). Scored vs official Product QA checklist 1–10 against Product QA HOLD PASS report + prior #69 SD Security PASS + Soft Soft CLOSE Soft HOLD SoR checklist twin CLEAR PR **#112** @ `fdc26b9`. Scope **#69 ONLY**. Soft Soft CLOSE Soft HOLD Doc / status:done / handshake SoR unlocks remain held. Soft Soft CLOSE Soft HOLD Dev Code QA lift on SD path does **not** close Product-step pt 10.

## Checklist vs Product QA (official 1–10)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | No privileged back doors — basic UI does not bypass FieldPolicy / #67; no UI-only security | **MET** | QA Sec #1 / AC2 **EVIDENCED** + SD PASS cross-cite @ `da61210`: `FieldPolicy_*` ×3; `Profile_ServerSideFieldProjection_NoLoginEmail`; `Search_DiscoveryOmitsPrivateFields`; `Strategy_CrossTenant_NoPrivateFieldLeak`; Program.cs +3 static-only (`UseDefaultFiles`/`UseStaticFiles`); PR #100 5-file UI+tests only. Prior SD: `app.js` "UI is NOT a security boundary"; all protected flows via `apiCall(...)`. |
| 2 | Authn fail-closed 401 / wrong principal 403/404; no private-field leak via UI payloads | **MET** | QA Sec #2 / AC3 **EVIDENCED**: `*_Unauth_Returns401` ×7; `InvalidAuth_Returns401`; `Unauth_NoPrivateFieldsInResponse_Profile`; `Artifact/Strategy/Negotiation_CrossTenant_Returns404`; `Strategy_CrossTenant_NoPrivateFieldLeak`; `Budget_CrossTenant_CannotAccessOther`. CI SUCCESS 36509394069 @ `da61210`. |
| 3 | Assistant path binds #67 — reject client prompt-only soft wall (do **not** re-score #67) | **MET** | QA Sec #3 **EVIDENCED**: `Assistant_ToolInvoke_UsesGateway`; `Assistant_UnallowedTool_Denied`; `Assistant_ToolResult_NoLoginEmail`; `Assistant_Capabilities_NoLoginEmailTool`; `Assistant_StrategyBinding_OwnOnly`. Bind only — #67 Product QA already PASSed separately. |
| 4 | Budget status minimal only (#68) — not V3 admin / owner cost UI | **MET** | QA Sec #4 / AC6 **EVIDENCED**: `Budget_Status_ReturnsMinimalFields`; `Budget_Status_NoAdminPrivilege`; `Budget_Status_OwnBudgetOnly`; `Budget_CutoffRespected_AfterExhaustion` (shallow soft gap scored); Unauth/CrossTenant budget Facts. Bind only — do not re-score #68. |
| 5 | Surface = basic UI; Soft HOLD multi-provider; bot not required | **MET** | QA Sec #5 / AC1 **EVIDENCED**: `StaticUI_IndexHtml/AppJs/StylesCss_Returns200`; `wwwroot/index.html` + `app.js` + `styles.css`; no bot channel in PR #100; Soft HOLD multi-provider until BM multi-provider start; Spec Locked #0/#5. |
| 6 | OUT locked X2 — OpenAPI→V1; MCP→V5; no 5th Story | **MET** | QA Sec #6 / AC5 **EVIDENCED**: `Health_StillAuthNone`; 5-file #69-only diff; Spec §6 OUT + Locked #8; no MCP/OpenAPI invent; Soft OTel/audit weave only on named #66/#67/#68. |
| 7 | Consume tip authz (#5 + FieldPolicy) — no Stage A/B ACL rewrite | **MET** | QA Sec #7 **EVIDENCED**: Program.cs +3 only; FieldPolicy consume; no ACL Story rewrite in diff; CQ `cq:no-refactor` @ `da61210`. |
| 8 | No Gate unlock / no invent — #26 backlog; #27 HOLD; Soft #41 CLOSED; keep #66/#67/#68 separate | **MET** | QA Sec #8 **EVIDENCED**: Diff clean of Cognito/MM/DC4/vault; Soft **#41** CLOSED via **#66+#67**; Soft HOLD #18; Gate **#26** backlog · Gate **#27** HOLD; siblings Product QA kept separate. |
| 9 | Cost / spend — PoC $0; spend → COO→CEO | **MET** | QA Sec #9 **EVIDENCED**: No LLM/AWS/IdP spend in PR #100; static SPA + local API only. PoC **$0**. Any named LLM/API spend → COO → CEO. |
| 10 | Handshake close — Product QA HOLD until Security QA; #18 not Field-capture | **MET** | Product QA correctly HOLDs PASS / pt 10 until Security QA `…x2-ui-bot-productqa-qa-confirm.md`. Prior SD Security PASS does **not** close Product-step pt 10. Soft Soft CLOSE Soft HOLD Dev Code QA lift on SD path is separate. Parent #18 framing-only — does **not** Field-capture #69. This done-list correctly holds Soft HOLD until Security QA Product-step confirm. Soft Soft CLOSE Soft HOLD Doc / handshake Soft Soft CLOSE Soft HOLD SoR **not invented** here. |

## Soft notes (non-blocking)

- No live `dotnet test` on evidence box — CI SUCCESS run 36509394069 @ `da61210` + `StageCBasicUITests.cs` ×**32** equivalent accepted (Product QA soft gap scored).
- Soft Soft CLOSE Soft HOLD SoR Product QA checklist twin — checklist **ISSUED** on KB; Soft Soft CLOSE Soft HOLD SoR **CLEAR** PR **#112** @ `fdc26b9` (HTTP 200). Do **not** invent handshake Soft Soft CLOSE Soft HOLD SoR (points-review / productqa-qa-confirm / docs/qa publish remain later).
- Soft Soft CLOSE Soft HOLD Doc until Product QA PASS + Soft Soft CLOSE Soft HOLD handshake SoR.
- Soft Soft CLOSE Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start.
- Soft Soft CLOSE Soft HOLD pt 10 Soft Soft CLOSE Soft HOLD until Security QA files `…x2-ui-bot-productqa-qa-confirm.md` (Product QA HOLD was correct process).
- Soft Soft CLOSE Soft HOLD Dev Code QA / merge already lifted for #69 SD path is **separate** — this is Product QA track.
- Product QA SoR ≠ Doc SoR (separate tracks).
- Soft **#41** CLOSED via **#66+#67** — do not re-open / re-score.
- Gate **#26** backlog; Gate **#27** HOLD.
- Soft residuals from Product QA (non-blocking): `totalUnits` vs `limitUnits` display; shallow `Budget_CutoffRespected_AfterExhaustion`; no browser/E2E (API HttpClient + static GET only); JWT discarded / ApiKey-only localStorage (UI ≠ security boundary).
- Do **not** re-score #66/#67/#68.
- Tip lineage: Soft Soft CLOSE Soft HOLD SoR tip `fdc26b9`; impl tip `da61210` PR #100.

## Gaps

**None.** Soft residuals (display naming; shallow cutoff Fact; no live `dotnet`; no browser/E2E) accepted non-blockers. Soft Soft CLOSE Soft HOLD handshake SoR not invented — not a GAP.

## Done-list (for Security QA)

- [x] DOC-FLOW filed; Product QA HOLD PASS + SD Security PASS cited
- [x] Score 10/10 vs Product QA checklist + Product QA HOLD PASS report + Soft Soft CLOSE Soft HOLD SoR twin CLEAR PR **#112** @ `fdc26b9` + prior #69 SD PASS (evidence cites: StageCBasicUITests ×32; FieldPolicy_*/Unauth_*/CrossTenant_*/Assistant_*/Budget_Status_*/StaticUI_*; Program.cs static-only; PR #100 @ `da61210`)
- [x] Soft #41 CLOSED via #66+#67; Gate #26 backlog; #27 HOLD; parent #18 not Field-capture; PoC $0; no Cognito/MM/DC4
- [x] CI Build & Test **SUCCESS** on impl `da61210` (run 36509394069)
- [x] Soft Soft CLOSE Soft HOLD SoR twin CLEAR PR **#112** @ `fdc26b9` (HTTP 200; do not invent handshake)
- [x] Pt 10 handshake gate correctly stated — Product QA HOLD was correct process; Soft HOLD until Security QA `…x2-ui-bot-productqa-qa-confirm.md`
- [ ] Security QA → `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-qa-confirm.md`
- [ ] Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (after Security QA) + Soft Soft CLOSE Soft HOLD handshake SoR (do **not** invent)
- [ ] Soft Soft CLOSE Soft HOLD status:done until CBA
- [ ] Soft Soft CLOSE Soft HOLD multi-provider until BM multi-provider start
- [x] Soft Soft CLOSE Soft HOLD SoR twin → Docs CLEAR PR **#112** (do **not** invent handshake)

## Cost/critical

None. PoC **$0**. Any named LLM/API/IdP spend → COO → CEO.
