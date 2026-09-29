# Security checklist — Dev Plan · MVP Stage C parent #18 (Option A Spec unlock / Stage C remainder framing)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS on parent #18 Dev Plan (if written).  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/18 · Participant secrets/ACL · Option A · Stage C Spec unlock framing  
**Named slices (separate Dev Plans + separate Security checklists):** #66 · #67 · #68 · #69  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (parent framing last)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-participant-isolation-option-a-remainder.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-qa-confirm.md` (Spec Security SoR **#71+#72**; tip `main` @ `f133e90` Senior PM — Spec Soft Soft CLOSE Soft HOLD SoR CLEAR / PR #73 cited by PM for Spec)  
**Sibling Spec Security PASS ×4:** `verification/2026-09-28__security__verification__mvp-stage-c-{hardwall,thin-assistant,a8-min,x2-ui-bot}-spec-qa-confirm.md`  
**Architecture:** Option A tip `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3b + Stage C delta `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md`  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. SD HOLD until Dev Plan QA + Security PASS. Eng dual-wall remainder implementation is **#67** — this parent checklist does **not** Field-capture #67. Framing/map only. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-devplan-checklist.md`

## Scope note
Parent #18 Dev Plan (if present) **frames/maps** Stage C unlock against CEO ACCEPTED Option A — dual wall end-state, §3b agent/tool hard wall remainder, FieldClass open-ended registry — and **maps** remainder work to named slices **#66–#69**. Real points — not N/A. Does **not** Field-capture #67. Do **not** invent a merged mega-plan that replaces #66–#69; do **not** invent FieldClasses beyond CEO examples; do **not** open Gate #26/#27.

## Itemized security points (Dev Plan must weave)

1. **Option A dual wall end-state map** — Plan restates API/DB wall (Stage A/B tip) + agent/tool wall (Stage C / #67) as dual defense for **all** FieldClasses; schedules **reject** of prompt-only soft guidance as sole control; verify framing only (implementation on #67).

2. **FieldClass registry open-ended** — Plan treats LoginEmail / ContactEmail / DisplayName / StrategyBody as **examples**, not exhaustive; dual wall applies to all FieldClasses; no inventing new named FieldClasses unless CEO/Product locks them.

3. **Stage C remainder map (not merge)** — Plan maps #18 AC remainder to named slices: **#67** hard wall + scrub; **#66** thin Assistant under wall; **#68** A8-min hard cutoff; **#69** X2 basic UI no bypass — complementary Dev Plans, not one merged surface; verify separate plans.

4. **Soft #41 Assistant OUT** — Plan states soft #41 closes only via **#66 + #67** delivery under wall — not claimed as Stage B–delivered Assistant/tool runtime; verify.

5. **No LoginEmail share / ShareOutbound Accept-gated** — Plan preserves LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools); verify framing cites tip + #67 (does not Field-capture #67).

6. **Threat rows bound to #67** — Plan keeps CEO prompt-injection / agent↔agent exfil posture bound to gateway + scrub (#67), not model trust; verify map/cross-ref only.

7. **Gate #26 / #27 HOLD** — Plan does **not** unlock Gate **#26** (backlog until Stage C delivery) or Gate **#27**; parent Dev Plan is framing-step, not post-delivery SA-REV; verify.

8. **OUT locked** — Plan does not invent Cognito/SSO/IdP, mature vault/KMS, MCP breadth, fuller Assistant as MVP, MotorMarket/DC4, or a **5th Story** for OTel/audit/idempotent (Soft weave on named slices only).

9. **Cost / spend** — PoC **$0**; any named LLM/API spend → **COO → CEO**; do not provision.

10. **Handshake close** — Dev Plan QA must **not** PASS parent #18 until Security QA confirms **this** checklist (and named-slice Dev Plans remain gated by their own Security QA). SD stays HOLD until then. Parent does **not** Field-capture #67.

## Handshake next
Senior Dev Planner weaves parent #18 (if any) → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner + Senior PM. Named slices #66–#69 continue on their own checklists in parallel (order preference #67→#66→#68→#69→#18).

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
