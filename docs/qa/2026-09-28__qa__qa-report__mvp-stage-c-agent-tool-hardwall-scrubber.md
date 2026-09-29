# QA Report — MVP Stage C Agent/tool hard wall + response scrubber (#67)

**Status:** **PASS** — Security QA confirm landed (pts 1–10 MET); QAQA confirmed Product QA PASS to Chief (no bounce)  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Product QA  
**Confirm to:** Dealoware QA (Chief QA) via QAQA **after** Security handshake  
**Story:** GitHub issue #67 · Agent/tool hard wall + response scrubber (dual-wall / #18 remainder)  
**Issue:** https://github.com/ioaikh/dealoware/issues/67  
**PR:** https://github.com/ioaikh/dealoware/pull/78 (**MERGED**)  
**Impl merge:** `dab5822f732cb67d1ccf38850794a6b34c15948b`  
**Tip (SoR handshake #80):** `8f3f6430c22098ecfc67fc71425000a4b1015610`  
**CI (impl @ dab5822):** https://github.com/ioaikh/dealoware/actions/runs/36503698040 — **SUCCESS**  
**CI (tip @ 8f3f643):** https://github.com/ioaikh/dealoware/actions/runs/36504493930 — **SUCCESS**  
**Security checklist (this step):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-checklist.md`  
**Security QA confirm (this step):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-qa-confirm.md` — **PASS** (pts 1–10 MET)  
**Prior SD Security QA:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md` — **PASS** 10/10 (SoR twin PR #80)  
**CQ:** `cq:no-refactor`  
**DOC-FLOW:** `qa/2026-09-28__qa__qa-report__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**Constraints:** Scope **#67 ONLY**. Soft Soft CLOSE Soft HOLD Doc clearing with Product QA PASS (SoR publish). Soft Soft CLOSE Soft HOLD status:done until CBA. Soft **#41** Assistant OUT closes only with **#66** under this wall (not Stage B). Gate **#26** backlog; **#27** HOLD. Parent **#18** framing-only (does not Field-capture #67). #66/#68/#69 OUT of this report. PoC **$0**; no MotorMarket.

## Method

- GitHub contents / Checks at tip `8f3f643…` and impl merge `dab5822…` (no clone)
- Soft gap: no live `dotnet test` → **CI + `StageCAgentHardwallTests.cs` equivalent** (39 Facts)
- Product QA Security checklist woven (pts 1–9); pt 10 HOLD for Security QA Product-step confirm

## Product acceptance criteria (Spec §9 / issue #67)

| AC | Verdict | Evidence (tip `8f3f643…` / merge `dab5822…`) |
|----|---------|---------------------------------------------|
| 1 Agents call platform tools only (separate runtime gateway) | **MET** | `AgentGateway.InvokeToolAsync` + `Gateway_AllowedTool_ReturnsScrubbedResponse`; `Gateway_UnallowedTool_ReturnsDeny` (`TOOL_NOT_ALLOWED`); `Gateway_UnauthenticatedPrincipal_ReturnsDeny`; `Gateway_NonAgentPrincipal_ReturnsDeny` |
| 2 Tool allowlist deny-by-default; FieldClass Read/ShareOutbound; undeclared denied | **MET** | `Allowlist_UnregisteredTool_IsDenied`; `Allowlist_NullOrEmpty_IsDenied`; `GetTool_UnregisteredTool_ReturnsNull`; `GetProfile_DeclaresDisplayNameAndContactEmail_NotLoginEmail`; `ShareContactEmail_DeclaresShareOutboundOnly`; `ValidateToolDeclarations_RejectsUndeclaredFields` |
| 3 Server-side scrub via same `IFieldPolicy.Evaluate`; denied stripped before model | **MET** | `Scrubber_UsesSameFieldPolicyAsApiDb`; `Scrubber_DeniedFields_Stripped`; `Scrubber_AllowedFields_PassThrough`; `Scrubber_StripsAllForStranger` |
| 4 No LoginEmail in agent context / tools; User-only; OwnAgent Deny | **MET** | `Allowlist_NoToolDeclaresLoginEmail`; `Allowlist_NoLoginEmailTool`; `Scrubber_StripsLoginEmail_EvenIfAccidentallyReturned`; `FieldPolicy_OwnAgent_DenyLoginEmail_Held`; `ShareOutbound_LoginEmail_AlwaysDeny` |
| 5 ShareOutbound ContactEmail only if Accept recorded; deny regardless of prompt | **MET** | `PreAccept_ShareOutbound_Deny`; `PostAccept_ShareOutbound_Allow_ForCounterparty`; `PostAccept_ShareOutbound_Deny_ForStranger`; `Scrubber_ShareOutbound_RespectsAcceptGrant` |
| 6 Cross-agent mediated if any; prompt cannot escalate | **MET** | README: MVP has **no** cross-agent messaging path; stranger/cross-tenant scrub: `Stranger_DeniedAllProtectedFields`; `CrossTenant_NoPrivateFieldLeakage`; prompt never consulted (`Scrubber_ServerSide_NotClientPromptBased`) |
| 7 Automated tests matrix (Spec §9.1) | **MET** | `StageCAgentHardwallTests.cs` (39 Facts); CI SUCCESS 36503698040 @ `dab5822` and 36504493930 @ `8f3f643` |
| 8 Documented as Stage C #18 remainder dual-wall; vault V3; MCP OUT | **MET** | PR #78 scoped to AgentGateway + StubToolExecutor + tests; no #66/#68/#69; SD Security PASS 10/10 |
| 9 Soft Spec weave only (OTel/audit/idempotent) — no 5th Story | **MET** | `ScrubbedToolResponse.StrippedFieldCount` + error sanitize only; no observability product surface |

## Security checklist points 1–10 (Product QA evidence)

Binding: `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-checklist.md`

| # | Point | Verdict | Evidence |
|---|-------|---------|----------|
| 1 | Agent runtime gateway — platform tools only; fail-closed off-gateway | **EVIDENCED** | `Gateway_AllowedTool_ReturnsScrubbedResponse`; `Gateway_UnallowedTool_ReturnsDeny`; `Gateway_UnauthenticatedPrincipal_ReturnsDeny`; `Gateway_NonAgentPrincipal_ReturnsDeny` |
| 2 | Tool allowlist deny-by-default | **EVIDENCED** | `Allowlist_UnregisteredTool_IsDenied`; `Allowlist_NullOrEmpty_IsDenied`; `GetTool_UnregisteredTool_ReturnsNull`; `ValidateToolDeclarations_RejectsUndeclaredFields` |
| 3 | Server-side scrub before model via same Evaluate | **EVIDENCED** | `Scrubber_UsesSameFieldPolicyAsApiDb`; `Scrubber_DeniedFields_Stripped`; `Scrubber_AllowedFields_PassThrough` |
| 4 | No LoginEmail in agent context | **EVIDENCED** | `Allowlist_NoToolDeclaresLoginEmail`; `Allowlist_NoLoginEmailTool`; `Scrubber_StripsLoginEmail_EvenIfAccidentallyReturned`; `FieldPolicy_OwnAgent_DenyLoginEmail_Held` |
| 5 | ShareOutbound Accept-gated server-side | **EVIDENCED** | `PreAccept_ShareOutbound_Deny`; `PostAccept_ShareOutbound_Allow_ForCounterparty`; `Scrubber_ShareOutbound_RespectsAcceptGrant`; `ShareOutbound_LoginEmail_AlwaysDeny` |
| 6 | Reject prompt-only / parallel ACL | **EVIDENCED** | `Architecture_GatewayPlusEvaluate_NotPromptOnly`; `Scrubber_ServerSide_NotClientPromptBased`; `ValidateToolDeclarations_RejectsUndeclaredFields`; PR #78 file set has no parallel ACL tables |
| 7 | Dual wall all FieldClasses; soft #41 → #66 under wall; siblings OUT | **EVIDENCED** | `DualWall_AllRegisteredFieldClasses_EvaluatedBySamePolicyAsApiDb`; `DualWall_UnknownFieldClass_Denied`; `DualWall_Scrubber_UsesOpenEndedRegistry`; PR #78 is #67-only (no #66/#68/#69) |
| 8 | Cross-agent mediated / documented none | **EVIDENCED** | `src/Dealoware.Domain/AgentGateway/README.md` MVP none; `Stranger_DeniedAllProtectedFields`; `CrossTenant_NoPrivateFieldLeakage` |
| 9 | OUT / Gate / spend | **EVIDENCED** | No Cognito/MM/DC4/vault/MCP invent in #67 file set; Gate #26 backlog / #27 HOLD held; PoC $0; soft OTel = `StrippedFieldCount` only |
| 10 | Handshake close | **PASS** | Security QA `…hardwall-productqa-qa-confirm.md` PASS (pts 1–10 MET); soft gaps accepted |

## Soft gaps / non-blockers

- No live `dotnet` — CI + `StageCAgentHardwallTests.cs` equivalent
- `Architecture_GatewayPlusEvaluate_NotPromptOnly` is a thin presence assert; stronger evidence is `Scrubber_ServerSide_NotClientPromptBased` + `ValidateToolDeclarations_RejectsUndeclaredFields` + fail-closed gateway codes
- StubToolExecutor is MVP platform stub; production swaps executor behind same gateway/scrub contracts
- Soft **#41 Assistant OUT** still held — closes only with sibling **#66** under this wall (not Stage B; #66 not in this PR is OK for #67 SCOPE)
- Soft multi-provider assistant API (CEO via Bot Manager) — no Executive suite rewrite until docs land; then API-contract + provider-stub swap cases only
- Tip `8f3f643` is handshake SoR after impl `dab5822` (docs-only PR #80); AgentGateway tree is the `dab5822` merge

## OUT / HOLD (verified)

- #66 Thin Assistant · #68 A8 meters · #69 UI/bot — **separate tracks** (not scored here)
- Parent #18 Field-capture — framing-only
- Gate #26 backlog · Gate #27 HOLD
- Soft Soft CLOSE Soft HOLD Doc until Product QA PASS
- Soft Soft CLOSE Soft HOLD status:done until CBA
- MotorMarket / Cognito / vault / MCP / 5th OTel Story

## Disposition

**PASS** — Security QA `…hardwall-productqa-qa-confirm.md` PASS (pts 1–10 MET); soft gaps accepted.  
QAQA confirmed Product QA PASS to Chief (no bounce).  
**Chief Product QA PASS locked 2026-09-28** by Dealoware QA (Chief QA). Soft Soft CLOSE Soft HOLD Doc may clear on this PASS. Soft Soft CLOSE Soft HOLD `status:done` until CBA BA-verify. Soft #41 OUT until #66 under wall. Scope #67 only. Do not set GitHub `status:done` from this step alone.

### Done-list

- [x] Evidence at impl `dab5822…` + tip `8f3f643…` (CI SUCCESS both)
- [x] AC 1–9 woven (Spec §9 / issue #67)
- [x] Product QA Security checklist woven (pts 1–9)
- [x] Security QA confirm PASS (pt 10)
- [x] QAQA confirm to Chief
- [x] SoR publish under `docs/qa/` — PR **#84** OPEN (learn from #31/#38/#59); awaiting merge
