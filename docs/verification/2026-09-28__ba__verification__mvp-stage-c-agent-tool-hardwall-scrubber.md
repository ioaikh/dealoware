# BA business verification — Story #67 Agent/tool hard wall + response scrubber (dual-wall / #18 remainder)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-28 (BA verify executed ~8:55 PM ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/67  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/78 (**MERGED** @ main `dab5822f732cb67d1ccf38850794a6b34c15948b`; MergedAt 2026-09-28 8:33:37 PM ET)  
**CI (impl @ dab5822):** https://github.com/ioaikh/dealoware/actions/runs/36503698040 — **SUCCESS**  
**CI (SoR tip @ 8f3f643):** https://github.com/ioaikh/dealoware/actions/runs/36504493930 — **SUCCESS**  
**Tip (main @ Soft Soft CLOSE Soft HOLD SoR chain):** `70433141d725b8fbf0177d81caf0f7436b684d20` (PR #86 docs; AgentGateway tree from #78 @ `dab5822…`)  
**Method:** Business-intent AC check from Story #67 body + merged PR #78 evidence on **main** (`gh` remote reads of AgentGateway / ToolAllowlist / AgentContextScrubber / StageCAgentHardwallTests / README; no clone) + KB Product QA / Spec / DevPlan / SD verifies + Security ProductQA/SD confirms (not code review). No invented requirements. Stage C named slice only. Soft **#41** Assistant OUT via **#66+#67** — this Story is the **wall** only; do **not** invent Assistant product UX from #66. Parent **#18** Spec HOLD (framing-only; does not Field-capture #67). Gate **#26** backlog · **#27** HOLD. Soft Soft CLOSE Soft HOLD SoR CLEAR (#77/#80/#81/#82/#84/#85). PoC **$0**. No MotorMarket/DC4. Do **not** CLOSE issue or flip `status:done` from this step.

## Binding AC (Story #67 — do not invent)

1. **Agents call platform tools only** (separate agent runtime gateway) — not raw DB, not arbitrary HTTP to internal APIs
2. **Tool allowlist** (deny-by-default): each tool declares FieldClasses it may Read / ShareOutbound; undeclared tools denied
3. **Server-side scrub:** every tool response / context pack runs `IFieldPolicy.Evaluate` (or equivalent); denied fields stripped **before** model context
4. **No LoginEmail** in agent context packs / tools — LoginEmail remains User-only (not even OwnAgent)
5. **ShareOutbound** tool allows ContactEmail share **only if Accept recorded** on that offer/negotiation; else deny regardless of prompt text
6. Cross-agent messaging (if any at MVP) is **mediated**; payloads cannot include denied FieldClasses; prompt text cannot escalate rights
7. Automated tests (or equivalent): allowlisted tool OK with scrub; denied field stripped; LoginEmail never in context; pre-Accept share deny; stranger/cross-tenant deny; unauth deny; **reject prompt-only soft guidance as sole control**
8. Documented as Stage C **#18 remainder** dual-wall bind — mature vault → **V3**; MCP/public tool marketplace invent → out
9. Soft Spec weave only (not a separate Story): where hard-wall deny/scrub or offer-touching tool paths hit SA-REV-MVP-C **OTel / audit / idempotent-offers** hooks, Spec notes touchpoints — no new product surface invented

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | Agents call platform tools only (separate runtime gateway) | **PASS** | PR #78 adds Domain `AgentGateway` + Infra `StubToolExecutor` + DI; Facts: `Gateway_AllowedTool_ReturnsScrubbedResponse`, `Gateway_UnallowedTool_ReturnsDeny` (`TOOL_NOT_ALLOWED`), `Gateway_UnauthenticatedPrincipal_ReturnsDeny`, `Gateway_NonAgentPrincipal_ReturnsDeny`. Product QA AC1 MET; Security ProductQA/SD pt1 MET. README: platform tools only, no raw DB / arbitrary internal HTTP. |
| 2 | Tool allowlist deny-by-default; FieldClass Read/ShareOutbound; undeclared denied | **PASS** | `ToolAllowlist` + `FieldClassDeclaration`; Facts: `Allowlist_UnregisteredTool_IsDenied`, `Allowlist_NullOrEmpty_IsDenied`, `GetTool_UnregisteredTool_ReturnsNull`, `GetProfile_DeclaresDisplayNameAndContactEmail_NotLoginEmail`, `ShareContactEmail_DeclaresShareOutboundOnly`, `ValidateToolDeclarations_RejectsUndeclaredFields`. Product QA AC2 MET; Security pts 2 MET. |
| 3 | Server-side scrub via same `IFieldPolicy.Evaluate`; denied stripped before model | **PASS** | `AgentContextScrubber` → Evaluate; Facts: `Scrubber_UsesSameFieldPolicyAsApiDb`, `Scrubber_DeniedFields_Stripped`, `Scrubber_AllowedFields_PassThrough`, `Scrubber_StripsAllForStranger`. Product QA AC3 MET; Security pts 3 MET. |
| 4 | No LoginEmail in agent context / tools; User-only; OwnAgent Deny | **PASS** | Facts: `Allowlist_NoToolDeclaresLoginEmail`, `Allowlist_NoLoginEmailTool`, `Scrubber_StripsLoginEmail_EvenIfAccidentallyReturned`, `FieldPolicy_OwnAgent_DenyLoginEmail_Held`, `ShareOutbound_LoginEmail_AlwaysDeny`. Product QA AC4 MET; Security pts 4 MET. |
| 5 | ShareOutbound ContactEmail only if Accept recorded; deny regardless of prompt | **PASS** | Facts: `PreAccept_ShareOutbound_Deny`, `PostAccept_ShareOutbound_Allow_ForCounterparty`, `PostAccept_ShareOutbound_Deny_ForStranger`, `Scrubber_ShareOutbound_RespectsAcceptGrant`. Product QA AC5 MET; Security pts 5 MET. Consumes Stage B #42 AcceptGrant patterns (not rewrite). |
| 6 | Cross-agent mediated if any; prompt cannot escalate | **PASS** | AgentGateway README: **"MVP Status: No cross-agent messaging path exists"**; future must be mediated+scrubbed. Facts: `Stranger_DeniedAllProtectedFields`, `CrossTenant_NoPrivateFieldLeakage`, `Scrubber_ServerSide_NotClientPromptBased`. Product QA AC6 MET; Security pts 6/8 MET. |
| 7 | Automated tests matrix | **PASS** | `tests/Dealoware.Api.Tests/StageCAgentHardwallTests.cs` (39 Facts) on merge `dab5822…`; CI SUCCESS runs 36503698040 @ `dab5822` and 36504493930 @ `8f3f643`. Includes allowlist+scrub, strip, LoginEmail never, pre-Accept deny, stranger/cross-tenant, unauth, reject prompt-only (`Architecture_GatewayPlusEvaluate_NotPromptOnly`, `Scrubber_ServerSide_NotClientPromptBased`, `ToolDeclarations_Enforced_NotPromptGuidance`). Product QA AC7 MET. Soft: no live `dotnet` — CI + inventory equivalent. |
| 8 | Documented Stage C #18 remainder dual-wall; vault V3; MCP OUT | **PASS** | PR #78 scoped to AgentGateway + StubToolExecutor + DI + tests — no #66/#68/#69 product; README dual-wall bind; vault/MCP invent OUT per Story OOS + Product QA AC8 + Security pts 7/9. Parent #18 framing-only (does not Field-capture #67). |
| 9 | Soft Spec weave OTel/audit/idempotent only — no 5th Story | **PASS** | `ScrubbedToolResponse.StrippedFieldCount` + error sanitize touchpoints only; README Soft OTel section; no observability product surface. Product QA AC9 MET; Security pt9 soft OTel = touchpoint only. |

## Out of scope held

| OOS item (Story #67 + locks) | Held? | Evidence |
|------------------------------|-------|----------|
| Thin Assistant product UX (**#66**) — invent Assistant from #66 | **Yes** | PR #78 has no #66 surface; Soft #41 OUT via #66+#67 — this Story = wall only; Product QA / Security: siblings OUT |
| A8 meters/budgets (**#68**) | **Yes** | Not in PR; Product QA OUT; Security pt7 |
| Basic UI / bot surface (**#69**) | **Yes** | Not in PR; Product QA OUT; Security pt7 |
| Inventing a **5th Story** for OTel/audit/idempotent | **Yes** | Soft weave only (`StrippedFieldCount`); Product QA AC9; Security pt9 |
| Mature PII vault retention/erasure (**A9** → **V3**) | **Yes** | Story OOS; Security pt9; Product QA AC8 |
| MCP breadth; public OpenAPI; spend / Cognito / MotorMarket / DC4 | **Yes** | No Cognito/MM/DC4/MCP invent in #67 file set; PoC $0; Security pt9 |
| Unlocking gate **#26** before Stage C delivery | **Yes** | #26 stays backlog; CPM comments + Soft locks; Security pt9 |
| Gate **#27** HOLD | **Yes** | Held; Soft locks; Security pt9 |
| Rewriting parent **#18** Spec / Field-capture #67 | **Yes** | Parent framing-only; Spec HOLD; Product QA / Security: does not Field-capture #67 |
| Soft **#41** Assistant OUT claimed as Stage B delivery | **Yes** | Soft #41 closes only with **#66** under this wall — not Stage B; wall-only delivery OK for #67 SCOPE |

## Soft gaps (non-blocking for BA business verify)

- No live `dotnet restore|build|test` on evidence box — accepted; CI SUCCESS runs 36503698040 + 36504493930 + `StageCAgentHardwallTests` ×39 inventory via `gh`.
- `Architecture_GatewayPlusEvaluate_NotPromptOnly` is a thin presence assert — accepted; stronger evidence `Scrubber_ServerSide_NotClientPromptBased` + `ValidateToolDeclarations_RejectsUndeclaredFields` + fail-closed gateway codes (Product QA / Security soft note).
- StubToolExecutor is MVP platform stub — accepted; production swaps executor behind same gateway/scrub contracts.
- Soft **#41 Assistant OUT** still held — closes only with sibling **#66** under this wall (not Stage B; #66 not in this PR is OK for #67 SCOPE).
- Soft Soft CLOSE Soft HOLD SoR chain CLEAR (#77 @ `215a736`, #80 @ `8f3f643`, #81 @ `0d77963`, #82 @ `63c0bc2`, #84 @ `7828faa`, #85 @ `22e84a1`); tip main @ `7043314` (PR #86 #66 docs). Eng `status:done` HOLD until CBA confirm after BAQA. GH issue state CLOSED ≠ BA-verify / ≠ `status:done`.

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Spec QA | PASS | `verification/2026-09-28__spec__verification__mvp-stage-c-agent-tool-hardwall-scrubber.md` + Spec Security confirm 10/10 |
| Dev Plan QA | PASS | `verification/2026-09-28__devplan__verification__mvp-stage-c-agent-tool-hardwall-scrubber.md` + DevPlan Security confirm 10/10 |
| SD / Dev Code QA | PASS | `verification/2026-09-28__sd__verification__mvp-stage-c-agent-tool-hardwall-scrubber.md` |
| SD Security | PASS (1–10 MET) | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-sd-qa-confirm.md` |
| CQ | `cq:no-refactor` | Issue labels + CQ assessment; Chief CQ confirm |
| Product QA | PASS | `qa/2026-09-28__qa__qa-report__mvp-stage-c-agent-tool-hardwall-scrubber.md` |
| Product QA Security | PASS (1–10 MET) | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-productqa-qa-confirm.md` |
| Soft Soft CLOSE Soft HOLD Doc | PASS | Chief Docs PASS; CPM `status:ready-for-ba-verify` |
| Soft Soft CLOSE Soft HOLD SoR | CLEAR | PRs #77/#80/#81/#82/#84/#85 MERGED; tip `7043314` |

## Recommendation to CBA

**PASS** — deliverable meets Story #67 business AC (separate agent runtime gateway + deny-by-default tool allowlist + server-side scrub via same `IFieldPolicy.Evaluate`; LoginEmail never in agent context; ShareOutbound ContactEmail Accept-gated regardless of prompt; cross-agent MVP none documented with mediated/scrub posture held; StageCAgentHardwallTests ×39 + CI SUCCESS; Stage C #18 remainder dual-wall documented with vault→V3 / MCP OUT; Soft OTel/audit/idempotent weave only — no 5th Story), and OOS held (#66 Assistant UX invent OUT; #68/#69 separate; no 5th Story; vault V3; MCP/Cognito/MotorMarket/DC4 OUT; #26 backlog; #27 HOLD; parent #18 Spec HOLD / not Field-captured; Soft #41 OUT until #66 under wall; PoC $0). Soft gaps non-blocking. Hand to BAQA for verify-QA; eng `status:done` HOLD until CBA final confirm after BAQA. Do **not** CLOSE issue #67 from this step (GH CLOSED ≠ BA-verify), do **not** flip `status:done`, do **not** unlock #26/#27/#18, do **not** invent #66 Assistant product UX, do **not** merge sibling tracks from this step. Stage C named slice only. PoC $0.
