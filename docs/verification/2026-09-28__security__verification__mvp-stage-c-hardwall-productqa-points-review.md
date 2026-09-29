# Verification — Security points vs MVP Stage C #67 Agent/tool hard wall Product QA

**Author:** Dealoware Senior Security  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 Product-QA-step Security points MET — pt 10 closes via this done-list → Security QA confirm)  
**Checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-checklist.md` (10 points)  
**SoR twin (checklist):** Soft Soft CLOSE Soft HOLD checklist SoR PR **#82** CLEAR · tip Soft Soft CLOSE Soft HOLD SoR PR **#80** @ `8f3f643`  
**Product QA report:** `qa/2026-09-28__qa__qa-report__mvp-stage-c-agent-tool-hardwall-scrubber.md` (HOLD PASS; Sec 1–9 EVIDENCED; pt 10 HOLD)  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/78 · **MERGED** @ `dab5822f732cb67d1ccf38850794a6b34c15948b`  
**Tip Soft Soft CLOSE Soft HOLD SoR:** PR **#80** @ `8f3f6430c22098ecfc67fc71425000a4b1015610`  
**CI (impl @ dab5822):** https://github.com/ioaikh/dealoware/actions/runs/36503698040 — **SUCCESS** (verified `list_check_runs_for_ref`)  
**CI (tip @ 8f3f643):** https://github.com/ioaikh/dealoware/actions/runs/36504493930 — **SUCCESS** (verified `list_check_runs_for_ref`)  
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md` (PASS 10/10; SoR twin PR #80)  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-devplan-qa-confirm.md`  
**SD checklist (ref):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-checklist.md`  
**Tests:** `tests/Dealoware.Api.Tests/StageCAgentHardwallTests.cs` (39 Facts; present @ `dab5822`)  
**Issue:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-points-review.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (after Security QA). Soft Soft CLOSE Soft HOLD status:done until CBA. Soft Soft CLOSE Soft HOLD SoR → Docs later (do **not** invent). Soft **#41** → **#66+#67** under wall (not Stage B). Gate **#26** backlog; Gate **#27** HOLD. Parent #18 framing-only — does **not** Field-capture #67. #66/#68/#69 OUT of this Story. PoC **$0**; no Cognito/MM/DC4.

## Scope note

Product QA verifies Spec + Plan + SD Security for defense **#2**: agent runtime gateway + tool allowlist + server-side scrub via the **same** `IFieldPolicy.Evaluate` as API/DB. Scored vs official Product QA checklist 1–10 against Product QA HOLD PASS report **and** independent re-spot of code/tests at MERGED impl `dab5822…` (not report trust alone). Soft Soft CLOSE Soft HOLD Doc / status:done / SoR unlocks remain held.

## Checklist vs Product QA (official 1–10)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Agent runtime gateway — platform tools only; fail-closed off-gateway | **MET** | QA Sec #1 / AC1 + re-spot `AgentGateway.InvokeToolAsync` @ `dab5822…`: allowlist lookup → `IToolExecutor` only; undeclared → `TOOL_NOT_ALLOWED`; unauth → `UNAUTHENTICATED`; non-agent → `NOT_AGENT`. Tests: `Gateway_AllowedTool_ReturnsScrubbedResponse`, `Gateway_UnallowedTool_ReturnsDeny`, `Gateway_UnauthenticatedPrincipal_ReturnsDeny`, `Gateway_NonAgentPrincipal_ReturnsDeny`. No raw-DB / arbitrary-HTTP path in AgentGateway tree. |
| 2 | Tool allowlist deny-by-default — FieldClass Read/ShareOutbound; undeclared denied | **MET** | QA Sec #2 + re-spot `ToolAllowlist.RegisterMvpTools` @ `dab5822…`: dictionary registry; `GetTool`/`IsAllowed` null/false for unregistered; MVP tools declare Read/ShareOutbound only (no LoginEmail). Tests: `Allowlist_UnregisteredTool_IsDenied`, `Allowlist_NullOrEmpty_IsDenied`, `GetTool_UnregisteredTool_ReturnsNull`, `GetProfile_DeclaresDisplayNameAndContactEmail_NotLoginEmail`, `ShareContactEmail_DeclaresShareOutboundOnly`, `ValidateToolDeclarations_RejectsUndeclaredFields`. |
| 3 | Server-side scrub before model — same IFieldPolicy.Evaluate; strip denied | **MET** | QA Sec #3 + re-spot `AgentContextScrubber.Scrub` @ `dab5822…`: per-field `_fieldPolicy.Evaluate`; denied excluded from `AllowedFields` / counted in `StrippedFieldCount` before model context. Same Domain `IFieldPolicy` as API/DB. Tests: `Scrubber_UsesSameFieldPolicyAsApiDb`, `Scrubber_DeniedFields_Stripped`, `Scrubber_AllowedFields_PassThrough`, `Scrubber_StripsAllForStranger`. |
| 4 | No LoginEmail in agent context — User-only; OwnAgent Deny; no LoginEmail tool | **MET** | QA Sec #4 + re-spot: no tool in `RegisterMvpTools` declares LoginEmail; scrubber strips accidental LoginEmail; OwnAgent Deny held. Tests: `Allowlist_NoToolDeclaresLoginEmail`, `Allowlist_NoLoginEmailTool`, `Scrubber_StripsLoginEmail_EvenIfAccidentallyReturned`, `FieldPolicy_OwnAgent_DenyLoginEmail_Held`, `ShareOutbound_LoginEmail_AlwaysDeny`. |
| 5 | ShareOutbound Accept-gated — HasAcceptGrant server-side; prompt cannot grant | **MET** | QA Sec #5 + re-spot: scrub via Evaluate with `ForNegotiation` (pre) / `ForAcceptedNegotiation` (post); prompt never consulted. Tests: `PreAccept_ShareOutbound_Deny`, `PostAccept_ShareOutbound_Allow_ForCounterparty`, `PostAccept_ShareOutbound_Deny_ForStranger`, `Scrubber_ShareOutbound_RespectsAcceptGrant`, `ShareOutbound_LoginEmail_AlwaysDeny`. |
| 6 | Reject prompt-only / parallel ACL — no soft-wall-only; no parallel agent ACL tables | **MET** | QA Sec #6 + re-spot: enforcement is gateway + allowlist + same `IFieldPolicy` scrub; PR #78 file set = AgentGateway/* + StubToolExecutor + DI + tests — no parallel agent ACL table/entity. Tests: `Architecture_GatewayPlusEvaluate_NotPromptOnly`, `Scrubber_ServerSide_NotClientPromptBased`, `ValidateToolDeclarations_RejectsUndeclaredFields`, `ToolDeclarations_Enforced_NotPromptGuidance`. |
| 7 | Dual wall all FieldClasses; soft #41 path — #66 under wall; siblings OUT | **MET** | QA Sec #7 + re-spot: scrubber uses open-ended `IFieldPolicy`; unknown `FieldClass.Custom(...)` denied. PR #78 / merge `dab5822…` is **#67 only** — no Thin Assistant (#66), A8 meters (#68), or UI/bot (#69). Soft #41 closes only with **#66** under this wall (documented; not Stage B claim; #66 absence from this PR is OK for #67 SCOPE). Tests: `DualWall_AllRegisteredFieldClasses_EvaluatedBySamePolicyAsApiDb`, `DualWall_UnknownFieldClass_Denied`, `DualWall_Scrubber_UsesOpenEndedRegistry`. |
| 8 | Cross-agent mediated exfil posture — scrub if messaging; else document none | **MET** | QA Sec #8 + re-spot `src/Dealoware.Domain/AgentGateway/README.md` @ `dab5822…`: **"MVP Status: No cross-agent messaging path exists"**; future path must go mediated+scrubbed. Stranger/cross-tenant scrub verified. Tests: `Stranger_DeniedAllProtectedFields`, `Scrubber_StripsAllForStranger`, `CrossTenant_NoPrivateFieldLeakage`. |
| 9 | OUT / Gate / spend — V3 vault; MCP OUT; #26 backlog; #27 HOLD; $0; no Cognito/MM/DC4 | **MET** | QA Sec #9 + prior SD Security PASS + re-spot scope: #67 file set has no Cognito/MM/DC4/vault/MCP invent; soft OTel = `StrippedFieldCount` + error sanitize only (no 5th Story). Gate #26 backlog / #27 HOLD held. PoC **$0**. Test: `HealthEndpoint_NotAffectedByAgentGateway`. CI SUCCESS both SHAs. |
| 10 | Handshake close — Product QA/QAQA must not PASS until Security QA; #18 not Field-capture | **MET** | Product QA correctly HOLDs PASS / pt 10 until Security QA `…hardwall-productqa-qa-confirm.md`. Parent #18 framing-only — does **not** Field-capture #67. This done-list → Security QA Product-step confirm. Soft Soft CLOSE Soft HOLD Doc / status:done / SoR remain held. |

## Catch-up note

Filed 2026-09-28 after Security QA independent Product-step PASS (Senior score was in-flight / briefly ABSENT to peers at Chief PASS time). Aligns Security QA confirm `…hardwall-productqa-qa-confirm.md` + Chief PASS. Soft Soft CLOSE Soft HOLD checklist SoR **#82** CLEAR. Soft Soft CLOSE Soft HOLD Doc may PASS. Soft Soft CLOSE Soft HOLD handshake SoR ×2 may land. Cite-align catch-up; content PASS **stands** (no rescore). Soft #41 → #66+#67 under wall; PoC $0.

## Soft notes (non-blocking; align Product QA + SD)

- No live `dotnet test` on evidence box — CI SUCCESS runs 36503698040 @ `dab5822` and 36504493930 @ `8f3f643` + `StageCAgentHardwallTests.cs` ×39 equivalent accepted.
- Soft **#41 Assistant OUT** closes only with sibling **#66** under this wall — not a Stage B claim; #67 delivers the wall only; #66 not in this PR is OK for #67 SCOPE.
- Soft OTel/audit — `ScrubbedToolResponse.StrippedFieldCount` + error sanitize are weave touchpoints only; no 5th Story.
- `Architecture_GatewayPlusEvaluate_NotPromptOnly` is a thin presence assert; stronger evidence is `Scrubber_ServerSide_NotClientPromptBased` + `ValidateToolDeclarations_RejectsUndeclaredFields` + fail-closed gateway codes — still MET.
- StubToolExecutor is MVP platform stub; production swaps executor behind same gateway/scrub contracts.
- Tip `8f3f643` is Soft Soft CLOSE Soft HOLD SoR after impl `dab5822` (docs-only PR #80); AgentGateway tree is the `dab5822` merge.
- Soft Soft CLOSE Soft HOLD SoR → Docs later — do **not** invent Docs SoR unlock here.

## Gaps

**None.**

## Done-list (for Security QA)

- [x] DOC-FLOW filed; Spec + Dev Plan + SD Security PASS cited
- [x] Score 10/10 vs Product QA checklist + Product QA HOLD PASS report + MERGED PR #78 @ `dab5822…` (code/test re-spot)
- [x] Soft #41 → #66+#67 under wall; Gate #26 backlog; #27 HOLD; parent #18 not Field-capture; #66/#68/#69 OUT; PoC $0; no Cognito/MM/DC4
- [x] CI Build & Test **SUCCESS** on impl `dab5822` and tip `8f3f643`
- [x] Pt 10 handshake gate correctly stated — Product QA HOLD was correct process
- [x] Security QA confirm **PASS** (`…hardwall-productqa-qa-confirm.md`) + Chief PASS (independent Gate; Senior catch-up align)
- [x] Soft Soft CLOSE Soft HOLD checklist SoR **#82** CLEAR
- [ ] Soft Soft CLOSE Soft HOLD Doc may PASS (Chief unlocked path)
- [ ] Soft Soft CLOSE Soft HOLD status:done until CBA
- [ ] Soft Soft CLOSE Soft HOLD handshake SoR ×2 → Docs (no invent here)

## Cost/critical

None. PoC **$0**. Any named LLM/API/IdP spend → COO → CEO.
