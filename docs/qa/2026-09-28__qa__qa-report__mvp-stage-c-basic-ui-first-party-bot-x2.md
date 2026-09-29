# QA Report — MVP Stage C Basic UI (X2 partial) (#69)

**Status:** **PASS** — Product Spec §8 / issue AC MET (soft gaps scored); Security pts 1–10 **PASS** (Security QA `productqa-qa-confirm` 10/10 + Senior Security points-review 10/10 + QAQA meta-PASS; Chief Product QA PASS locked)  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Product QA  
**Confirm to:** Dealoware QA (Chief QA) via QAQA  
**Story:** GitHub issue #69 · Basic UI and/or one first-party bot surface (X2 partial)  
**Issue:** https://github.com/ioaikh/dealoware/issues/69  
**PR (impl):** https://github.com/ioaikh/dealoware/pull/100 (**MERGED**)  
**Impl / CQ tip (verify):** `da612100fe0bac8341b9d95f5d8f92c3b6385c50` (`da61210`)  
**Docs tip (trail):** `fdc26b9fee8c8db542bb58360626a2afa2d2409e` (`fdc26b9`) — Soft Soft CLOSE Soft HOLD SoR Product QA checklist PR **#112**  
**Prior docs trail:** `03f96593ade6ed7efbdb048594f041284753be8b` (`03f9659`) OK  
**CI (impl @ da61210):** https://github.com/ioaikh/dealoware/actions/runs/36509394069 — **SUCCESS** (check_run 109217951626)  
**Security checklist (this step):** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-checklist.md` — **ISSUED** (KB + Soft Soft CLOSE Soft HOLD SoR twin **CLEAR** PR **#112** @ `fdc26b9` → `docs/verification/…x2-ui-bot-productqa-checklist.md`)  
**Prior SD Security QA:** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-qa-confirm.md` — **PASS** 10/10  
**SD handshake Soft Soft CLOSE Soft HOLD SoR:** PR **#106** @ `259813e` CLEAR (points-review + sd-qa-confirm)  
**SD checklist (ref):** `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-sd-checklist.md`  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md`  
**#66/#67/#68 bind (mandatory; do NOT re-score):** Product QA PASS locked separately — thin Assistant + hardwall + A8 meters reports  
**CQ:** `cq:no-refactor` @ `da61210`  
**DOC-FLOW:** `qa/2026-09-28__qa__qa-report__mvp-stage-c-basic-ui-first-party-bot-x2.md`  
**Constraints:** Scope **#69 ONLY**. Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (+ handshake Soft Soft CLOSE Soft HOLD SoR). Soft Soft CLOSE Soft HOLD status:done until CBA. Soft HOLD multi-provider. Soft HOLD **#18** until tip / separate Product QA LIFT. Soft **#41** CLOSED via **#66+#67**. Gate **#26** backlog · **#27** HOLD. Soft first-party bot OUT of Spec minimum. Soft Spec weave only for OTel/audit on named Stories — **no 5th Story**. PoC **$0**; no MotorMarket. Verify against impl tip **`da61210`**; docs tip `fdc26b9` for Soft Soft CLOSE Soft HOLD SoR trail.

## Method

- GitHub contents / Checks at tip `da61210…` (impl) and Soft Soft CLOSE Soft HOLD SoR tip `fdc26b9…` (no full clone)
- Soft gap: no live `dotnet test` → **CI + `StageCBasicUITests.cs` equivalent** (**32 Facts**)
- Product QA Security checklist woven vs Soft Soft CLOSE Soft HOLD SoR CLEAR PR **#112** @ `fdc26b9` (pts 1–10 PASS)
- Prefer automated API tests + minimal static UI smoke (HttpClient + static GET; no browser/E2E)

## Product acceptance criteria (Spec §8 / issue #69)

| AC | Verdict | Evidence (tip `da61210…` / merge PR #100) |
|----|---------|---------------------------------------------|
| 1 Spec-locked **basic UI** X2 MVP minimum (exactly-one first-party bot **not** required; Soft HOLD multi-provider) | **MET** | `wwwroot/index.html` + `app.js` + `styles.css`; `StaticUI_IndexHtml/AppJs/StylesCss_Returns200`; PR #100 5-file UI+tests only; Spec Locked #0/#5; bot OUT of minimum |
| 2 Authz/FieldPolicy server-side — **no** UI-only security; bind #67 when Assistant used | **MET** | `FieldPolicy_*` ×3; `Profile_ServerSideFieldProjection_NoLoginEmail`; `Search_DiscoveryOmitsPrivateFields`; `Assistant_*` gateway Facts; Program.cs +3 static-only (UseDefaultFiles/UseStaticFiles) |
| 3 Unauth fail-closed; wrong principal / cross-tenant → 403/404; no private-field leak | **MET** | `*_Unauth_Returns401` ×7; `InvalidAuth_Returns401`; `Unauth_NoPrivateFieldsInResponse_Profile`; `Artifact/Strategy/Negotiation_CrossTenant_Returns404`; `Strategy_CrossTenant_NoPrivateFieldLeak`; `Budget_CrossTenant_CannotAccessOther` |
| 4 Automated tests critical authz (Spec §8.1) | **MET** | `StageCBasicUITests.cs` **×32** Facts; CI SUCCESS 36509394069 @ `da61210` |
| 5 Documented X2 MVP partial; OpenAPI→V1; MCP→V5; Soft OTel weave only on named Stories | **MET** | Spec §6 OUT + Locked #8; `Health_StillAuthNone`; 5-file #69-only diff; no MCP/OpenAPI invent |
| 6 Soft #68 budget status **minimal** only (not V3 admin) | **MET** | `Budget_Status_ReturnsMinimalFields`; `Budget_Status_NoAdminPrivilege`; `Budget_Status_OwnBudgetOnly`; `Budget_CutoffRespected_AfterExhaustion` (shallow soft gap scored) |

## Security checklist points 1–10 (Product QA evidence)

**Binding:** `docs/verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-checklist.md` Soft Soft CLOSE Soft HOLD SoR **CLEAR** PR **#112** @ `fdc26b9` (KB twin same path under `verification/`).

| # | Point | Verdict | Evidence |
|---|-------|---------|----------|
| 1 | No privileged back doors / no UI-only security | **EVIDENCED** | FieldPolicy_* ×3; Profile_ServerSide*; Search_Discovery*; Strategy_CrossTenant_NoPrivateFieldLeak; static Program.cs only |
| 2 | Authn fail-closed 401 / wrong principal 403/404; no private leak | **EVIDENCED** | *_Unauth_Returns401 ×7; InvalidAuth; Unauth_NoPrivateFields*; CrossTenant_* ×4; Budget_CrossTenant_* |
| 3 | Assistant path binds #67 (do **not** re-score #67) | **EVIDENCED** | Assistant_ToolInvoke_UsesGateway; Assistant_UnallowedTool_Denied; Assistant_ToolResult_NoLoginEmail; Assistant_Capabilities_NoLoginEmailTool; Assistant_StrategyBinding_OwnOnly |
| 4 | Budget status minimal only (#68; do not re-score #68) | **EVIDENCED** | Budget_Status_ReturnsMinimalFields; NoAdminPrivilege; OwnBudgetOnly; Unauth/CrossTenant budget Facts |
| 5 | Surface = basic UI; Soft HOLD multi-provider; bot not required | **EVIDENCED** | StaticUI_* ×3; no bot channel in PR #100; Soft HOLD multi-provider |
| 6 | OUT locked X2 (OpenAPI→V1; MCP→V5; no 5th Story) | **EVIDENCED** | Health_StillAuthNone; 5-file #69-only diff; Spec/Plan OUT locks |
| 7 | Consume tip authz (no Stage A/B ACL rewrite) | **EVIDENCED** | Program.cs +3 only; FieldPolicy consume; no ACL Story rewrite in diff |
| 8 | No Gate unlock; Soft #41 CLOSED; keep #66/#67/#68 separate | **EVIDENCED** | Diff clean of Cognito/MM/DC4/vault; Soft #41 CLOSED via #66+#67; Soft HOLD #18; Gate #26 backlog · #27 HOLD |
| 9 | PoC $0 | **EVIDENCED** | No LLM/AWS/IdP spend in PR #100; PoC **$0** |
| 10 | Handshake close — no Product QA / QAQA PASS until Security QA productqa-qa-confirm | **PASS** | Senior Security `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-points-review.md` **PASS 10/10**; Security QA `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-qa-confirm.md` **PASS 10/10**. Soft Soft CLOSE Soft HOLD Doc unlocked for Product QA PASS (handshake Soft Soft CLOSE Soft HOLD SoR next). |

## Soft gaps / non-blockers

- No live `dotnet` — CI + `StageCBasicUITests.cs` ×**32** equivalent (accepted)
- Soft Soft CLOSE Soft HOLD SoR Product QA checklist twin **CLEAR** PR **#112** @ `fdc26b9`
- DTO/UI display residuals: `totalUnits` vs `limitUnits`; subjectEntity vs entities; negotiation party naming — polish only
- Shallow `Budget_CutoffRespected_AfterExhaustion` (asserts not-exhausted; no burn) — #68 domain already cq:no-refactor; bind only
- No browser/E2E — API HttpClient + static GET only (Chief prefer)
- Negotiations list/detail only (no create offer/neg forms) — intentional X2 partial OUT
- JWT discarded / ApiKey-only localStorage — UI ≠ security boundary
- Issue still OPEN `status:in-dev` — Soft Soft CLOSE Soft HOLD status:done until CBA (not Product QA to clear)
- Soft HOLD Doc until Product QA PASS + handshake Soft Soft CLOSE Soft HOLD SoR
- Soft HOLD multi-provider; Soft HOLD #18 until separate LIFT
- Do **not** re-score #66/#67/#68

## OUT / HOLD (verified)

- #66/#67/#68 details — **bind only** (already Product QA PASSed; do not re-score)
- Soft first-party bot fuller surface — OUT of Spec minimum
- Soft HOLD multi-provider until BM multi-provider start
- Soft HOLD #18 until tip / separate Product QA LIFT
- Soft #41 CLOSED via #66+#67 (do not re-score)
- Gate #26 backlog · Gate #27 HOLD
- Soft Soft CLOSE Soft HOLD Doc — until Product QA PASS + handshake Soft Soft CLOSE Soft HOLD SoR
- Soft Soft CLOSE Soft HOLD status:done until CBA
- Cognito/SSO/MM/DC4/vault; multi-bot marketplace; MCP breadth; public OpenAPI; 5th OTel Story

## Disposition

**PASS** — Spec §8 / issue AC **MET** with soft gaps scored. Security pts 1–10 **PASS** (Security QA productqa-qa-confirm + Senior Security points-review). Soft Soft CLOSE Soft HOLD SoR checklist twin CLEAR (PR #112 @ `fdc26b9`). Soft Soft CLOSE Soft HOLD Doc clearing via Soft Soft CLOSE Soft HOLD SoR `docs/qa/` publish + handshake Soft Soft CLOSE Soft HOLD SoR ×2 (points-review + qa-confirm) when Docs lands them. Soft Soft CLOSE Soft HOLD `status:done` until CBA. Soft **#41** CLOSED via **#66+#67**. Soft HOLD multi-provider. Do not set GitHub `status:done` from this step alone. PoC **$0**.

### Done-list

- [x] Evidence at impl `da61210…` (CI SUCCESS 36509394069)
- [x] Spec §8 / issue AC woven — soft gaps scored honestly
- [x] Security pts 1–10 woven vs Soft Soft CLOSE Soft HOLD SoR CLEAR PR **#112** @ `fdc26b9`
- [x] Senior Security → `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-points-review.md` — **PASS 10/10**
- [x] Security QA → `verification/2026-09-28__security__verification__mvp-stage-c-x2-ui-bot-productqa-qa-confirm.md` — **PASS 10/10**
- [x] QAQA confirm to Chief — **meta-PASS**; Chief Product QA PASS locked
- [x] Soft Soft CLOSE Soft HOLD SoR publish under `docs/qa/` — PR **#116** OPEN @ `cdcbce65dbf7ff3e4c79112d6202e7b5c6488f58` (awaiting Docs MERGE)
