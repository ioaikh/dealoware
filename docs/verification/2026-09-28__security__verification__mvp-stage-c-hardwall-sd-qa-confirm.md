# Security QA — MVP Stage C #67 Agent/tool hard wall + response scrubber SD vs Chief Security SD checklist

**Author:** Dealoware Security QA  
**Date:** 2026-09-28  
**Verdict:** **PASS** (all 10 SD-step Security points MET)  
**Asked by:** Senior Security / Chief Security — SD review handshake (Gate #24/#25 pattern; PRIORITY)  
**Chief checklist (binding):** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-checklist.md` (10 points) — **SD checklist only** (not PR-body renumber; not Dev Plan checklist)  
**SoR checklist twin (GitHub):** `docs/verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-checklist.md` (PR **#77** @ `215a736`) — Soft Soft CLOSE Soft HOLD SoR → Docs later; do **not** claim Docs SoR unlock  
**Senior Security done-list:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-points-review.md` (**PASS** 10/10; appeared during run — cited; independent score agrees)  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md`  
**Format ref:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-sd-qa-confirm.md` · `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-devplan-qa-confirm.md`  
**PR:** https://github.com/ioaikh/dealoware/pull/78 · OPEN  
**HEAD:** `b3bf40d2e8f97f272d07166f2c8e965efb1b8f6f` (verified via `get_pull_request` / `list_check_runs_for_ref`)  
**Base tip main:** `215a736` (SD checklist SoR CLEAR PR #77)  
**CI:** Build & Test **SUCCESS** (completed ~2026-09-28 20:30 ET) · `mergeable_state` **clean** / MERGEABLE  
**Tests:** `tests/Dealoware.Api.Tests/StageCAgentHardwallTests.cs` (39 Facts; CI green — no live `dotnet test` on this box; static `gh` review @ HEAD)  
**Issue:** https://github.com/ioaikh/dealoware/issues/67 · Agent/tool hard wall + response scrubber  
**Parent:** #18 · Option A · Stage C · #18 dual-wall remainder (eng-facing slice) — framing-only; does **not** Field-capture #67  
**Siblings:** #66 · #68 · #69 — **not scored / not confirmed here** (keep separate)  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md`  
**Constraints:** Soft Soft CLOSE Soft HOLD Dev Code QA until this Security QA PASS. Soft Soft CLOSE Soft HOLD SoR → Docs later (no invent Docs SoR unlock). Soft **#41** → **#66+#67** under wall (not Stage B claim; #66 not in this PR is OK for #67 SCOPE). Gate **#26** backlog until Stage C delivery; Gate **#27** HOLD. PoC **$0**; no Cognito/MM/DC4 invent. Parent #18 does **not** Field-capture #67.

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security SD checklist (KB) | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-checklist.md` | Binding 10 points |
| SoR checklist twin (GitHub) | `docs/verification/…-hardwall-sd-checklist.md` (PR #77 @ `215a736`) | Present — Soft Soft CLOSE Soft HOLD SoR → Docs later |
| Senior Security points-review | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-points-review.md` | **PASS** 10/10 — cited (Gate #24/#25 late-arrival pattern) |
| PR #78 | `ioaikh/dealoware` @ `b3bf40d2…` | 18 files; AgentGateway/* + StubToolExecutor + DI + StageCAgentHardwallTests |
| CI | `list_check_runs_for_ref` @ HEAD | Build & Test **SUCCESS**; mergeable **clean** |
| Spec / Dev Plan Security PASS | `…hardwall-spec-qa-confirm.md` / `…hardwall-devplan-qa-confirm.md` | Upstream unlock context |

## Independent re-score (Security QA)

Score vs **official SD checklist 1–10** (not PR-body renumber). Static `gh` review of HEAD files/tests — no live `dotnet test` on this box.

| # | Point | Result | Evidence |
|---|-------|--------|----------|
| 1 | Agent runtime gateway — platform tools only; deny/fail-closed off-gateway | **MET** | `AgentGateway.InvokeToolAsync` routes only via `IToolAllowlist.GetTool` + `IToolExecutor` (platform `StubToolExecutor`); undeclared → `TOOL_NOT_ALLOWED`; unauth → `UNAUTHENTICATED`; non-agent → `NOT_AGENT`. DI registers `IAgentGateway` / `IToolAllowlist` / `IToolExecutor`=`StubToolExecutor` / `IAgentContextScrubber`. No raw-DB / arbitrary-HTTP path in AgentGateway tree. Tests: `Gateway_UnallowedTool_ReturnsDeny`, `Gateway_UnauthenticatedPrincipal_ReturnsDeny`, `Gateway_NonAgentPrincipal_ReturnsDeny`, `Gateway_AllowedTool_ReturnsScrubbedResponse`. |
| 2 | Tool allowlist deny-by-default — FieldClass Read/ShareOutbound; undeclared denied | **MET** | `ToolAllowlist` deny-by-default dictionary; `GetTool`/`IsAllowed` null/false for unregistered; each `AgentTool` carries `FieldClassDeclaration` Read/ShareOutbound. MVP tools: `get_profile`, `get_negotiation`, `get_strategy`, `share_contact_email`, `list_artifacts`, `create_offer`, `accept_offer`. Tests: `Allowlist_UnregisteredTool_IsDenied`, `Allowlist_NullOrEmpty_IsDenied`, `GetTool_UnregisteredTool_ReturnsNull`, `GetProfile_DeclaresDisplayNameAndContactEmail_NotLoginEmail`, `ShareContactEmail_DeclaresShareOutboundOnly`, `ValidateToolDeclarations_RejectsUndeclaredFields`. |
| 3 | Server-side scrub before model — IFieldPolicy.Evaluate; strip denied before model context | **MET** | `AgentContextScrubber.Scrub` calls `_fieldPolicy.Evaluate` per field; denied excluded from `AllowedFields` / counted in `StrippedFieldCount` before model context. Same Domain `FieldPolicy` singleton as API/DB (`AddSingleton<IFieldPolicy, FieldPolicy>`). Tests: `Scrubber_UsesSameFieldPolicyAsApiDb`, `Scrubber_DeniedFields_Stripped`, `Scrubber_StripsAllForStranger`, `Scrubber_AllowedFields_PassThrough`. |
| 4 | No LoginEmail in agent context — User-only; OwnAgent Deny; no LoginEmail tool | **MET** | No tool in `ToolAllowlist.RegisterMvpTools` declares `LoginEmail`; `StubToolExecutor` never returns LoginEmail; OwnAgent Deny held via `FieldPolicy`; scrubber strips accidental LoginEmail. Tests: `Allowlist_NoToolDeclaresLoginEmail`, `Allowlist_NoLoginEmailTool`, `Scrubber_StripsLoginEmail_EvenIfAccidentallyReturned`, `FieldPolicy_OwnAgent_DenyLoginEmail_Held`, `ShareOutbound_LoginEmail_AlwaysDeny`. |
| 5 | ShareOutbound Accept-gated — HasAcceptGrant server-side | **MET** | Same `FieldPolicy.EvaluateShareOutbound`: ContactEmail only when `HasAcceptGrant && Counterparty`; LoginEmail always deny. Context factories from #42 (`ForNegotiation` / `ForAcceptedNegotiation`). Scrubber enforces via Evaluate — prompt never consulted. Tools `share_contact_email` / `accept_offer` declare ShareOutbound. Tests: `PreAccept_ShareOutbound_Deny`, `PostAccept_ShareOutbound_Allow_ForCounterparty`, `PostAccept_ShareOutbound_Deny_ForStranger`, `Scrubber_ShareOutbound_RespectsAcceptGrant`, `ShareOutbound_LoginEmail_AlwaysDeny`. |
| 6 | Reject prompt-only / parallel ACL — no soft-wall-only; no parallel agent ACL tables | **MET** | Enforcement = gateway + allowlist + same `IFieldPolicy` scrub; PR adds only `AgentGateway/*` + `StubToolExecutor` + DI + tests — no parallel agent ACL table/entity. Fail-closed before executor for undeclared tools. Tests: `Architecture_GatewayPlusEvaluate_NotPromptOnly`, `Scrubber_ServerSide_NotClientPromptBased`, `ValidateToolDeclarations_RejectsUndeclaredFields`, `ToolDeclarations_Enforced_NotPromptGuidance`. |
| 7 | Dual wall all FieldClasses; soft #41 path — #66/#68/#69 separate; soft #41 via #66+#67 under wall | **MET** | Open-ended `IFieldPolicy` registry; unknown `FieldClass.Custom(...)` denied by default. File set is **#67 only** (18 files) — no Thin Assistant (#66), A8 (#68), or UI/bot (#69). Soft #41 closes only with **#66** under this wall (not Stage B; #66 absent from PR is OK for #67 SCOPE). Tests: `DualWall_AllRegisteredFieldClasses_EvaluatedBySamePolicyAsApiDb`, `DualWall_UnknownFieldClass_Denied`, `DualWall_Scrubber_UsesOpenEndedRegistry`. |
| 8 | Cross-agent mediated exfil posture — if MVP path enforce scrub; else document none | **MET** | `src/Dealoware.Domain/AgentGateway/README.md`: **"MVP Status: No cross-agent messaging path exists"**; future path must be mediated+scrubbed. Stranger/cross-tenant scrub posture verified. Tests: `Stranger_DeniedAllProtectedFields`, `Scrubber_StripsAllForStranger`, `CrossTenant_NoPrivateFieldLeakage`. |
| 9 | OUT / Gate / spend — vault V3; MCP OUT; #26 backlog; #27 HOLD; $0; no Cognito/MM/DC4 | **MET** | No Cognito/MM/DC4/vault/LLM/MCP packages in AgentGateway path (code-search empty); soft OTel = `StrippedFieldCount` + error sanitize only (no 5th Story); no #66/#68/#69 invent. Gate #26 backlog / #27 HOLD held. PoC **$0**. Test: `HealthEndpoint_NotAffectedByAgentGateway`. |
| 10 | Evidence + handshake — done-list; Soft HOLD Dev Code QA until Security QA; #18 not Field-capture | **MET** | This confirm + Senior done-list cite paths/tests for 1–9. **Soft Soft CLOSE Soft HOLD Dev Code QA / Product QA until this Security QA PASS.** Parent #18 framing-only — does **not** Field-capture #67. Soft Soft CLOSE Soft HOLD SoR → Docs later (no invent Docs SoR unlock). CI SUCCESS @ HEAD verified. |

## Soft notes (non-blocking)

- **Senior SD-step points-review present** (`…hardwall-sd-points-review.md` — **PASS** 10/10). Arrived during Gate #24/#25 independent-score window; Security QA cites and **agrees** on all 10 MET.
- **CI SUCCESS** — Build & Test completed success on HEAD `b3bf40d2…` (~2026-09-28 20:30 ET); `mergeable_state` clean. Reported accurately (not CONFLICTING/empty).
- Soft **#41 Assistant OUT** closes by design only with sibling **#66** under this wall — not a Stage B claim; #67 delivers the wall only; #66 not in this PR is OK for #67 SCOPE.
- Soft OTel/audit — `ScrubbedToolResponse.StrippedFieldCount` + error sanitize are weave touchpoints only; no 5th Story.
- `Architecture_GatewayPlusEvaluate_NotPromptOnly` is a thin presence assert; stronger evidence is `Scrubber_ServerSide_NotClientPromptBased` + `ValidateToolDeclarations_RejectsUndeclaredFields` + fail-closed gateway codes — still MET (aligns Senior soft note).
- StubToolExecutor is MVP platform stub (typed FieldClass-tagged responses); production swaps executor behind same gateway/scrub contracts.
- Soft Soft CLOSE Soft HOLD SoR → Docs later — tip checklist SoR PR **#77** @ `215a736` noted; do **not** claim Docs SoR unlock.
- No live `dotnet test` on this box — static `gh` review @ HEAD `b3bf40d2…` (CI SUCCESS is the suite evidence).
- **#66 / #68 / #69 / parent #18** not reviewed / not confirmed in this document.
- Gate **#26** backlog until Stage C delivery; Gate **#27** HOLD; PoC **$0**.

## Alignment with Senior review

Senior Security done-list (`verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-points-review.md`) scored all 10 **MET** with matching PR #78 / AgentGateway / ToolAllowlist / AgentContextScrubber / StubToolExecutor / DI / StageCAgentHardwallTests / README cites at HEAD `b3bf40d2…`. Independent Security QA re-score **agrees** — no gaps; soft notes complement (CI SUCCESS accurate; soft #41 path; thin Architecture_ assert; StubToolExecutor MVP stub; Soft Soft CLOSE Soft HOLD SoR → Docs later; #66/#68/#69/#18 unscored). No contradiction.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Agent runtime gateway; platform tools only; fail-closed off-gateway | Held (`AgentGateway` + `TOOL_NOT_ALLOWED` / `UNAUTHENTICATED` / `NOT_AGENT` tests) |
| Tool allowlist deny-by-default; FieldClass Read/ShareOutbound declarations | Held (`ToolAllowlist` + Allowlist_* / ValidateToolDeclarations_* tests) |
| Server-side scrub via same `IFieldPolicy.Evaluate`; strip before model | Held (`AgentContextScrubber` + Scrubber_* tests) |
| No LoginEmail tool / context; OwnAgent Deny held | Held (Allowlist_No* + Scrubber_StripsLoginEmail + FieldPolicy_OwnAgent_DenyLoginEmail_Held) |
| ShareOutbound Accept-gated server-side; prompt cannot escalate | Held (PreAccept_/PostAccept_/Scrubber_ShareOutbound_* tests) |
| Reject prompt-only / parallel ACL tables | Held (architecture + no parallel ACL in PR diff) |
| Dual wall open-ended registry; soft #41 → #66 under wall; siblings separate | Held (DualWall_* tests; #67-only file set) |
| Cross-agent: MVP documents none; stranger scrub posture held | Held (README + Stranger_/CrossTenant_ tests) |
| Gate #26 backlog; #27 HOLD; PoC $0; no Cognito/MM/DC4 | Held |
| Soft Soft CLOSE Soft HOLD Dev Code QA until this PASS; #18 not Field-capture | Held |

## Gaps

**None.** All checklist points 1–10 **MET**. Soft notes are non-blocking.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Soft Soft CLOSE Soft HOLD Dev Code QA / Product QA may PASS on Security gate **after** this confirm. Soft Soft CLOSE Soft HOLD SoR → Docs later — do **not** invent Docs SoR unlock. Cost/critical: none. PoC **$0**. Do **not** open Gate #26. Do **not** treat this as #66 / #68 / #69 / parent #18 confirm. Gate **#27** HOLD. Soft #41 Assistant OUT closes only via **#66+#67** under wall — not Stage B claim.
