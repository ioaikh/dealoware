# Security checklist — SD · MVP Stage C parent #18 (Option A Spec unlock / Stage C remainder framing)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS on parent #18 SD (if written).  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/18 · Participant secrets/ACL · Option A · Stage C Spec unlock framing  
**Named slices (separate SD + separate Security checklists):** #66 · #67 · #68 · #69  
**Order preference:** **#67 → #66 → #68 → #69 → #18** (parent framing last)  
**Spec:** `specs/2026-09-28__spec__spec__mvp-stage-c-participant-isolation-option-a-remainder.md`  
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-qa-confirm.md`  
**Plan:** `plans/2026-09-28__devplan__plan__mvp-stage-c-participant-isolation-option-a-remainder.md`  
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-devplan-qa-confirm.md`  
**Sibling Spec/Dev Plan Security PASS ×4:** `verification/2026-09-28__security__verification__mvp-stage-c-{hardwall,thin-assistant,a8-min,x2-ui-bot}-{spec,devplan}-qa-confirm.md`  
**Tip:** `main` @ `dc8ee46` (Dev Plan SoR PR **#76** MERGED; checklist **#74** CLEAR)  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. Soft Soft CLOSE Soft HOLD Dev Code QA until SD-step Security PASS. Soft **#41** → **#66+#67** under wall. Eng dual-wall remainder = **#67**. Parent framing-only — BIND #67; does **not** Field-capture #67. No Cognito/MM/DC4. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-sd-checklist.md`

## Scope note
Parent #18 SD (if present) **frames/maps** Stage C unlock against CEO ACCEPTED Option A — dual wall end-state, §3b agent/tool hard wall remainder, FieldClass open-ended registry — and **maps** remainder work to named slices **#66–#69**. Real points — not N/A. BIND **#67**; does **not** Field-capture #67. Do **not** implement sibling delivery inside parent SD; do **not** invent a merged mega-SD that replaces #66–#69; do **not** invent FieldClasses beyond CEO examples; do **not** open Gate #26/#27. Evidence via done-list (framing/map cites — not sibling code).

## Itemized security points (SD must satisfy)

1. **Option A dual wall end-state map** — Restate API/DB wall (Stage A/B tip) + agent/tool wall (Stage C / #67) as dual defense for **all** FieldClasses; **reject** prompt-only soft guidance as sole control; framing only — implementation on #67 (verified by map/done-list).

2. **FieldClass registry open-ended** — Treat LoginEmail / ContactEmail / DisplayName / StrategyBody as **examples**, not exhaustive; dual wall applies to all FieldClasses; no inventing new named FieldClasses unless CEO/Product locks them.

3. **Stage C remainder map (not merge)** — Map #18 AC remainder to named slices: **#67** hard wall + scrub; **#66** thin Assistant under wall; **#68** A8-min hard cutoff; **#69** X2 basic UI no bypass — complementary SD, not one merged surface; do **not** implement sibling delivery inside parent SD.

4. **Soft #41 Assistant OUT** — State soft #41 closes only via **#66 + #67** delivery under wall — not claimed as Stage B–delivered Assistant/tool runtime (verified in done-list).

5. **No LoginEmail share / ShareOutbound Accept-gated** — Preserve LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools); framing cites tip + #67 — does **not** Field-capture #67.

6. **Threat rows bound to #67** — Keep CEO prompt-injection / agent↔agent exfil posture bound to gateway + scrub (#67), not model trust; map/cross-ref only — BIND #67; no Field-capture.

7. **Gate #26 / #27 HOLD** — Do **not** unlock Gate **#26** (backlog until Stage C delivery) or Gate **#27**; parent SD is framing-step, not post-delivery SA-REV.

8. **OUT locked** — Do not invent Cognito/SSO/IdP, mature vault/KMS, MCP breadth, fuller Assistant as MVP, MotorMarket/DC4, or a **5th Story** for OTel/audit/idempotent (Soft weave on named slices only).

9. **Cost / spend** — PoC **$0**; any named LLM/API spend → **COO → CEO**; do not provision.

10. **Evidence + handshake** — Done-list cites framing/map paths for 1–9 (not sibling delivery code); Dev Code QA / Product QA must **not** PASS parent #18 until Security QA confirms **this** checklist (and named-slice SD remain gated by their own Security QA). Parent does **not** Field-capture #67.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer + Senior PM. Named slices #66–#69 continue on their own checklists in parallel (order preference #67→#66→#68→#69→#18).

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
