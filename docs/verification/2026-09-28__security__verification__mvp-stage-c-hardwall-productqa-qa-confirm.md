# Security QA — MVP Stage C #67 Agent/tool hard wall + response scrubber Product QA vs Chief Security Product QA checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 Product-QA-step Security points MET)  
**Asked by:** Senior Product QA / Dealoware QAQA / Chief Security (PRIORITY — Product QA HOLD PASS pts 1–9; pt 10 handshake; QAQA meta-PASS to Chief after this)  
**Product QA report:** `qa/2026-09-28__qa__qa-report__mvp-stage-c-agent-tool-hardwall-scrubber.md` (HOLD PASS pending this confirm; pts 1–9 EVIDENCED on **main**; pt 10 handshake open)  
**Chief checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-checklist.md` (10 points)  
**Senior Security Product QA points-review:** **ABSENT** at score time — scored independently vs checklist + Product QA report (same Gate pattern as Stage B discovery when Senior late)  
**Prior SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md` (PASS 10/10)  
**Format ref:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-productqa-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/78 (**MERGED**)  
**Impl merge commit:** `dab5822f732cb67d1ccf38850794a6b34c15948b`  
**Tip (SoR handshake #80):** `8f3f6430c22098ecfc67fc71425000a4b1015610`  
**CI (impl @ dab5822):** https://github.com/ioaikh/dealoware/actions/runs/36503698040 — **SUCCESS** (verified `get_workflow_run`)  
**CI (tip @ 8f3f643):** https://github.com/ioaikh/dealoware/actions/runs/36504493930 — **SUCCESS** (verified `get_workflow_run`)  
**Tests:** `tests/Dealoware.Api.Tests/StageCAgentHardwallTests.cs` (39 Facts; present on tip `8f3f643`)  
**CQ:** `cq:no-refactor`  
**Issue:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**Parent:** #18 · Option A · Stage C · eng dual-wall remainder = **#67** — framing-only; does **not** Field-capture #67  
**Siblings:** #66 · #68 · #69 — **not scored / not confirmed here**  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-qa-confirm.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD Doc until Product QA PASS. Soft Soft CLOSE Soft HOLD status:done until CBA BA-verify. Soft Soft CLOSE Soft HOLD SoR → Docs later (no invent Docs SoR unlock from this file alone). Soft **#41** → **#66+#67** under wall (not Stage B; this confirm = wall only; Soft #41 OUT until #66 MERGED/delivered). Gate **#26** backlog; Gate **#27** HOLD. PoC **$0**; no Cognito/MM/DC4. Soft: no-live-dotnet OK (CI SUCCESS + StageCAgentHardwallTests).

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security Product QA checklist | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-checklist.md` | Binding 10 points |
| Product QA report | `qa/2026-09-28__qa__qa-report__mvp-stage-c-agent-tool-hardwall-scrubber.md` | HOLD PASS pts 1–9 EVIDENCED; pt 10 HOLD |
| SD Security PASS | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md` | PASS 10/10 |
| Senior Security Product QA points-review | `verification/*hardwall-productqa-points-review*` | **ABSENT** — independent score (Gate pattern) |
| Impl PR #78 | MERGED @ `dab5822f732cb67d1ccf38850794a6b34c15948b` | Verified `get_pull_request` merged=true |
| Tip SoR PR #80 | `8f3f6430c22098ecfc67fc71425000a4b1015610` | Soft Soft CLOSE Soft HOLD SoR |
| CI impl | run 36503698040 @ `dab5822…` | **SUCCESS** |
| CI tip | run 36504493930 @ `8f3f643…` | **SUCCESS** |
| Tests on tip | `StageCAgentHardwallTests.cs` (39 Facts) | Present via `get_file_contents` @ `8f3f643` |
| AgentGateway tree on tip | `src/Dealoware.Domain/AgentGateway/*` | Present (gateway / allowlist / scrubber / README) |

## Soft gaps accepted (non-blocking)

| Soft gap | Disposition |
|----------|-------------|
| No live `dotnet test` on evidence box | **Accepted** — CI SUCCESS runs 36503698040 + 36504493930 + `StageCAgentHardwallTests.cs` ×39 + prior SD Security PASS (Stage B Product QA pattern) |
| Soft Soft CLOSE Soft HOLD SoR / Doc | **Accepted** — tip SoR PR #80 @ `8f3f643` noted; this confirm does **not** invent Docs SoR unlock; Soft Soft CLOSE Soft HOLD Doc until Product QA PASS (Product QA may clear after this) |
| Soft Soft CLOSE Soft HOLD status:done | **Accepted** — held until CBA BA-verify; not cleared by this confirm |
| Soft **#41** Assistant OUT | **Accepted** — path requires **#66** delivery under this wall; this #67 Product QA confirms wall only; Soft #41 remains OUT until #66 MERGED/delivered (matches Senior Product QA soft note) |
| `Architecture_GatewayPlusEvaluate_NotPromptOnly` thin presence assert | **Accepted** — stronger evidence `Scrubber_ServerSide_NotClientPromptBased` + `ValidateToolDeclarations_RejectsUndeclaredFields` + fail-closed gateway codes; still MET |
| StubToolExecutor MVP stub | **Accepted** — production swaps executor behind same gateway/scrub contracts |

## Independent re-score (Security QA)

Score vs **official Product QA checklist 1–10** only.

| # | Point | Product QA | Security QA | Evidence |
|---|-------|------------|-------------|----------|
| 1 | Agent runtime gateway — platform tools only; fail-closed off-gateway | EVIDENCED | **MET** | `AgentGateway.InvokeToolAsync` + `Gateway_AllowedTool_ReturnsScrubbedResponse`; `Gateway_UnallowedTool_ReturnsDeny` (`TOOL_NOT_ALLOWED`); `Gateway_UnauthenticatedPrincipal_ReturnsDeny`; `Gateway_NonAgentPrincipal_ReturnsDeny`; tip `8f3f643…` + CI 36503698040 / 36504493930 |
| 2 | Tool allowlist deny-by-default; FieldClass Read/ShareOutbound; undeclared denied | EVIDENCED | **MET** | `Allowlist_UnregisteredTool_IsDenied`; `Allowlist_NullOrEmpty_IsDenied`; `GetTool_UnregisteredTool_ReturnsNull`; `GetProfile_DeclaresDisplayNameAndContactEmail_NotLoginEmail`; `ShareContactEmail_DeclaresShareOutboundOnly`; `ValidateToolDeclarations_RejectsUndeclaredFields` |
| 3 | Server-side scrub before model via same `IFieldPolicy.Evaluate` | EVIDENCED | **MET** | `Scrubber_UsesSameFieldPolicyAsApiDb`; `Scrubber_DeniedFields_Stripped`; `Scrubber_AllowedFields_PassThrough`; `Scrubber_StripsAllForStranger` |
| 4 | No LoginEmail in agent context — User-only; OwnAgent Deny | EVIDENCED | **MET** | `Allowlist_NoToolDeclaresLoginEmail`; `Allowlist_NoLoginEmailTool`; `Scrubber_StripsLoginEmail_EvenIfAccidentallyReturned`; `FieldPolicy_OwnAgent_DenyLoginEmail_Held`; `ShareOutbound_LoginEmail_AlwaysDeny` |
| 5 | ShareOutbound Accept-gated server-side; prompt cannot escalate | EVIDENCED | **MET** | `PreAccept_ShareOutbound_Deny`; `PostAccept_ShareOutbound_Allow_ForCounterparty`; `PostAccept_ShareOutbound_Deny_ForStranger`; `Scrubber_ShareOutbound_RespectsAcceptGrant` |
| 6 | Reject prompt-only / parallel ACL | EVIDENCED | **MET** | `Architecture_GatewayPlusEvaluate_NotPromptOnly`; `Scrubber_ServerSide_NotClientPromptBased`; `ValidateToolDeclarations_RejectsUndeclaredFields`; PR #78 file set has no parallel ACL tables |
| 7 | Dual wall all FieldClasses; soft #41 → #66 under wall; #66/#68/#69 OUT | EVIDENCED | **MET** | `DualWall_AllRegisteredFieldClasses_EvaluatedBySamePolicyAsApiDb`; `DualWall_UnknownFieldClass_Denied`; `DualWall_Scrubber_UsesOpenEndedRegistry`; PR #78 is #67-only; soft #41 OUT until #66 under wall (not Stage B) |
| 8 | Cross-agent mediated / documented none | EVIDENCED | **MET** | `src/Dealoware.Domain/AgentGateway/README.md` MVP none; `Stranger_DeniedAllProtectedFields`; `CrossTenant_NoPrivateFieldLeakage` |
| 9 | OUT / Gate / spend | EVIDENCED | **MET** | No Cognito/MM/DC4/vault/MCP invent in #67; Gate #26 backlog / #27 HOLD; PoC $0; soft OTel = `StrippedFieldCount` only; CQ `cq:no-refactor` |
| 10 | Handshake close | HOLD (confirm missing) | **MET** | This confirm closes Product QA Security gate; Product QA / QAQA may clear HOLD → PASS |

## Alignment with Senior review

Senior Security Product QA points-review **absent** at score time (`*hardwall-productqa-points-review*` not found). Independent Security QA re-score vs Chief Product QA checklist + Product QA report (impl `dab5822…` + tip `8f3f643…` + CI SUCCESS both runs + StageCAgentHardwallTests ×39) + prior SD Security PASS — **all 10 MET**; soft notes align with checklist, Product QA report, and Senior Product QA (soft #41 OUT until #66; Soft Soft CLOSE Soft HOLD Doc / status:done / SoR).

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Agent runtime gateway; platform tools only; fail-closed off-gateway | Held (`AgentGateway` + `TOOL_NOT_ALLOWED` / `UNAUTHENTICATED` / `NOT_AGENT`) |
| Tool allowlist deny-by-default; FieldClass Read/ShareOutbound | Held (`ToolAllowlist` + Allowlist_* / ValidateToolDeclarations_*) |
| Server-side scrub via same `IFieldPolicy.Evaluate`; strip before model | Held (`AgentContextScrubber` + Scrubber_*) |
| No LoginEmail tool / context; OwnAgent Deny held | Held (Allowlist_No* + Scrubber_StripsLoginEmail + FieldPolicy_OwnAgent_DenyLoginEmail_Held) |
| ShareOutbound Accept-gated server-side; prompt cannot escalate | Held (PreAccept_/PostAccept_/Scrubber_ShareOutbound_*) |
| Reject prompt-only / parallel ACL tables | Held (architecture + no parallel ACL in PR #78) |
| Dual wall open-ended registry; soft #41 → #66 under wall; siblings OUT | Held (DualWall_*; #67-only scope) |
| Cross-agent: MVP documents none; stranger scrub posture held | Held (README + Stranger_/CrossTenant_) |
| Gate #26 backlog; #27 HOLD; PoC $0; no Cognito/MM/DC4 | Held |
| Soft Soft CLOSE Soft HOLD Doc until Product QA PASS; status:done until CBA; SoR no invent unlock | Held |
| Evidence on main `dab5822…` / tip `8f3f643…` + CI SUCCESS both | Held |

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are non-blocking.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Product QA / QAQA may clear HOLD and PASS to Chief. Soft Soft CLOSE Soft HOLD Doc may clear on Product QA PASS after this confirm. Soft Soft CLOSE Soft HOLD status:done remains until CBA BA-verify. Soft Soft CLOSE Soft HOLD SoR → Docs later — do **not** invent Docs SoR unlock from this file alone. Cost/critical: none. PoC **$0**. Do **not** open Gate #26. Do **not** treat this as #66 / #68 / #69 / parent #18 confirm. Soft #41 Assistant OUT remains until **#66** MERGED/delivered under this wall. Gate **#27** HOLD. Scope **#67 ONLY**.
