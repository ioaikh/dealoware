# Verification — Security points vs MVP Stage C #67 Agent/tool hard wall SD

**Author:** Dealoware Senior Security  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-checklist.md`  
**SoR checklist twin:** `docs/verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-checklist.md` (PR #77 @ `215a736`)  
**PR:** https://github.com/ioaikh/dealoware/pull/78 · OPEN · HEAD `b3bf40d2e8f97f272d07166f2c8e965efb1b8f6f`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md`  
**Tests:** `tests/Dealoware.Api.Tests/StageCAgentHardwallTests.cs` (39 new; CI Build & Test **success** on HEAD)  
**Issue:** https://github.com/ioaikh/dealoware/issues/67  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-points-review.md`  
**Constraints:** Soft #41 → #66+#67 under wall (not Stage B) · Gate **#26** backlog until Stage C delivery · Gate **#27** HOLD · Parent #18 framing does **not** Field-capture #67 · Soft Soft CLOSE Soft HOLD Dev Code QA until Security QA · Soft Soft CLOSE Soft HOLD SoR → Docs later (no handshake SoR invent) · PoC **$0** · no Cognito/MM/DC4 invent · keep #66/#68/#69 separate SD

## Scope note

SD implements Spec + Dev Plan Security for defense **#2**: agent runtime gateway + tool allowlist + server-side scrub via the **same** `IFieldPolicy.Evaluate` as API/DB (Stage A #31), AcceptGrant from Stage B #42. Scored on code/tests at PR #78 HEAD — not PR-body trust alone. Reject prompt-only soft wall; reject parallel agent ACL tables. Soft #41 Assistant OUT closes only via **#66+#67** under this wall.

## Checklist vs PR (official 1–10; not PR-body renumber)

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Agent runtime gateway — platform tools only; deny/fail-closed off-gateway | **MET** | `AgentGateway.InvokeToolAsync` routes only via `IToolAllowlist.GetTool` + `IToolExecutor` (platform stub); undeclared → `GatewayResult.ToolNotAllowed` / `TOOL_NOT_ALLOWED`; unauth → `UNAUTHENTICATED`; non-agent → `NOT_AGENT`. DI: `DependencyInjection.AddInfrastructure` registers `IAgentGateway`/`IToolAllowlist`/`IToolExecutor`=`StubToolExecutor`. Tests: `Gateway_UnallowedTool_ReturnsDeny`, `Gateway_UnauthenticatedPrincipal_ReturnsDeny`, `Gateway_NonAgentPrincipal_ReturnsDeny`, `Gateway_AllowedTool_ReturnsScrubbedResponse`. No raw-DB / arbitrary-HTTP path in `AgentGateway` tree. |
| 2 | Tool allowlist deny-by-default — FieldClass Read/ShareOutbound; undeclared denied | **MET** | `ToolAllowlist` dictionary registry; `GetTool`/`IsAllowed` null/false for unregistered; each `AgentTool` carries `FieldClassDeclaration` Read/ShareOutbound. MVP tools: `get_profile`, `get_negotiation`, `get_strategy`, `share_contact_email`, `list_artifacts`, `create_offer`, `accept_offer`. Tests: `Allowlist_UnregisteredTool_IsDenied`, `Allowlist_NullOrEmpty_IsDenied`, `GetTool_UnregisteredTool_ReturnsNull`, `GetProfile_DeclaresDisplayNameAndContactEmail_NotLoginEmail`, `ShareContactEmail_DeclaresShareOutboundOnly`, `ValidateToolDeclarations_RejectsUndeclaredFields`. |
| 3 | Server-side scrub before model — same IFieldPolicy.Evaluate; strip denied | **MET** | `AgentContextScrubber.Scrub` calls `_fieldPolicy.Evaluate` per field; denied excluded from `AllowedFields` / counted in `StrippedFieldCount` before any model context. Injected `IFieldPolicy` = same Domain `FieldPolicy` singleton as API/DB (`AddSingleton<IFieldPolicy, FieldPolicy>`). Tests: `Scrubber_UsesSameFieldPolicyAsApiDb`, `Scrubber_DeniedFields_Stripped`, `Scrubber_StripsAllForStranger`, `Scrubber_AllowedFields_PassThrough`. |
| 4 | No LoginEmail in agent context — User-only; OwnAgent Deny; no LoginEmail tool | **MET** | No tool in `ToolAllowlist.RegisterMvpTools` declares `LoginEmail`; `StubToolExecutor` never returns LoginEmail fields; `FieldPolicy.EvaluateLoginEmail` OwnAgent=false (Stage A held); scrubber strips accidental LoginEmail. Tests: `Allowlist_NoToolDeclaresLoginEmail`, `Allowlist_NoLoginEmailTool`, `Scrubber_StripsLoginEmail_EvenIfAccidentallyReturned`, `FieldPolicy_OwnAgent_DenyLoginEmail_Held`, `ShareOutbound_LoginEmail_AlwaysDeny`. |
| 5 | ShareOutbound Accept-gated — HasAcceptGrant server-side; prompt cannot grant | **MET** | Same `FieldPolicy.EvaluateShareOutbound`: ContactEmail only when `resourceContext.HasAcceptGrant && Counterparty`; LoginEmail always deny. Context factories from #42: `ForNegotiation` (pre) / `ForAcceptedNegotiation` (post). Scrubber enforces via Evaluate — prompt text never consulted. Tools `share_contact_email` / `accept_offer` declare ShareOutbound only. Tests: `PreAccept_ShareOutbound_Deny`, `PostAccept_ShareOutbound_Allow_ForCounterparty`, `PostAccept_ShareOutbound_Deny_ForStranger`, `Scrubber_ShareOutbound_RespectsAcceptGrant`, `ShareOutbound_LoginEmail_AlwaysDeny`. |
| 6 | Reject prompt-only / parallel ACL — no soft-wall-only; no parallel agent ACL tables | **MET** | Enforcement is gateway + allowlist + same `IFieldPolicy` scrub — no prompt-guidance control path; PR diff adds only `AgentGateway/*` + `StubToolExecutor` + DI + tests (no parallel agent ACL table/entity). Gateway fails closed before executor for undeclared tools. Tests: `Architecture_GatewayPlusEvaluate_NotPromptOnly`, `Scrubber_ServerSide_NotClientPromptBased`, `ValidateToolDeclarations_RejectsUndeclaredFields`, `ToolDeclarations_Enforced_NotPromptGuidance`. |
| 7 | Dual wall all FieldClasses; soft #41 path — open-ended; #66/#68/#69 separate | **MET** | Scrubber uses open-ended `IFieldPolicy` registry; unknown `FieldClass.Custom(...)` denied by default (`IsRegistered` false → Evaluate false). PR file set is **#67 only** (18 files: Domain AgentGateway + StubToolExecutor + DI + StageCAgentHardwallTests) — no Thin Assistant (#66), A8 meters (#68), or UI/bot (#69) product. Soft #41 closes only with **#66** under this wall (documented; not Stage B claim). Tests: `DualWall_AllRegisteredFieldClasses_EvaluatedBySamePolicyAsApiDb`, `DualWall_UnknownFieldClass_Denied`, `DualWall_Scrubber_UsesOpenEndedRegistry`. |
| 8 | Cross-agent mediated exfil posture — scrub if messaging; else document none | **MET** | `src/Dealoware.Domain/AgentGateway/README.md` explicit: **"MVP Status: No cross-agent messaging path exists"**; future path must go mediated+scrubbed. Stranger/cross-tenant scrub still verified for posture. Tests: `Stranger_DeniedAllProtectedFields`, `Scrubber_StripsAllForStranger`, `CrossTenant_NoPrivateFieldLeakage`. |
| 9 | OUT / Gate / spend — V3 vault; MCP OUT; #26 backlog; #27 HOLD; $0; no Cognito/MM/DC4 | **MET** | Domain csproj has **no** PackageReferences; Infra csproj = EF/Sqlite + JWT only (no Cognito/IdP/vault/LLM/MCP packages). Soft OTel = `StrippedFieldCount` + error sanitize only (no 5th Story). No #66/#68/#69 invent in diff. Gate #26 backlog / #27 HOLD held in constraints. PoC **$0**. Health untouched: `HealthEndpoint_NotAffectedByAgentGateway`. |
| 10 | Evidence + handshake — done-list; Soft HOLD Dev Code QA until Security QA; #18 not Field-capture | **MET** | This done-list cites paths/tests for 1–9; PR body Soft HOLD Dev Code QA until Security QA; parent #18 framing-only — does **not** Field-capture #67. Soft Soft CLOSE Soft HOLD SoR → Docs later (no handshake SoR invent here). CI Build & Test **success** @ HEAD `b3bf40d2…` (completed ~2026-09-28 20:30 ET). |

## Soft notes (non-blocking)

- Soft **#41 Assistant OUT** closes by design only with sibling **#66** under this wall — not a Stage B claim; #67 delivers the wall only.
- Soft OTel/audit — `ScrubbedToolResponse.StrippedFieldCount` + error sanitize are weave touchpoints only; no 5th Story.
- `Architecture_GatewayPlusEvaluate_NotPromptOnly` is a thin presence assert; stronger evidence is `Scrubber_ServerSide_NotClientPromptBased` + `ValidateToolDeclarations_RejectsUndeclaredFields` + fail-closed gateway codes — still MET.
- Soft Soft CLOSE Soft HOLD SoR → Docs later — do **not** invent handshake SoR in this file.
- StubToolExecutor is MVP platform stub (typed FieldClass-tagged responses); production would swap executor behind same gateway/scrub contracts.

## Gaps

**None.**

## Done-list (for Security QA)

- [x] DOC-FLOW filed; Spec + Dev Plan Security PASS cited
- [x] 10/10 with code/test cites at PR #78 HEAD `b3bf40d2e8f97f272d07166f2c8e965efb1b8f6f`
- [x] Soft #41 → #66+#67 under wall; Gate #26 backlog; #27 HOLD; parent #18 not Field-capture; PoC $0; no Cognito/MM/DC4
- [x] CI Build & Test **success** on HEAD
- [ ] Soft HOLD Dev Code QA / Product QA until Security QA confirms
- [ ] → Security QA confirm **PASS** → Chief Security
- [ ] Soft Soft CLOSE Soft HOLD SoR → Docs later (no invent)

## Cost/critical

None. PoC **$0**. Any named LLM/API/IdP spend → COO → CEO.
