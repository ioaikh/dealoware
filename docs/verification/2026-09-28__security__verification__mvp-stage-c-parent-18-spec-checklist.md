# Security checklist — Spec · MVP Stage C parent #18 (Option A Spec unlock / Stage C remainder framing)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS on parent #18 Spec (if written).  
**Date:** 2026-09-28  
**Author:** Dealoware Chief Security  
**Story:** https://github.com/ioaikh/dealoware/issues/18 · Participant secrets/ACL · Option A · Stage C Spec unlock framing  
**Named slices (separate Specs + separate Security checklists):** #66 · #67 · #68 · #69  
**Architecture:** Option A tip `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3b + Stage C delta `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md`  
**Prior Security PASS (SA step):** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Sibling Spec Security (already ISSUED):** `verification/2026-09-28__security__verification__mvp-stage-c-{thin-assistant,hardwall,a8-min,x2-ui-bot}-spec-checklist.md`  
**Hold:** Gate **#26** backlog until Stage C **delivery**. Gate **#27** HOLD. Eng dual-wall remainder implementation Spec is **#67** — this parent checklist does **not** Field-capture #67. No MotorMarket. PoC **$0**.  
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-checklist.md`

## Scope note
Parent #18 Spec (if present) frames Stage C unlock against CEO ACCEPTED Option A — dual wall end-state, §3b agent/tool hard wall remainder, FieldClass open-ended registry — and **maps** remainder work to named slices **#66–#69**. Real points — not N/A. Do **not** invent a merged mega-Spec that replaces #66–#69; do **not** invent FieldClasses beyond CEO examples; do **not** open Gate #26.

## Itemized security points (Parent #18 Spec must bind)

1. **Option A dual wall end-state** — Spec restates API/DB wall (Stage A/B tip) + agent/tool wall (Stage C / #67) as dual defense for **all** FieldClasses; prompt-only soft guidance **rejected** as sole control.

2. **FieldClass registry open-ended** — Spec treats LoginEmail / ContactEmail / DisplayName / StrategyBody as **examples**, not exhaustive; dual wall applies to all FieldClasses; no inventing new named FieldClasses unless CEO/Product locks them.

3. **Stage C remainder map (not merge)** — Spec maps #18 AC remainder to named slices: **#67** hard wall + scrub; **#66** thin Assistant under wall; **#68** A8-min hard cutoff; **#69** X2 UI/bot no bypass — complementary Specs, not one merged surface.

4. **Soft #41 Assistant OUT** — Spec states soft #41 closes only via **#66 + #67** delivery — not claimed as Stage B–delivered Assistant/tool runtime.

5. **No LoginEmail share / ShareOutbound Accept-gated** — Spec preserves LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools).

6. **Threat rows bound** — Spec keeps CEO prompt-injection / agent↔agent exfil posture bound to gateway + scrub (#67), not model trust.

7. **Gate #26 / #27 HOLD** — Spec does **not** unlock Gate **#26** (backlog until Stage C delivery) or Gate **#27**; parent Spec is Spec-step framing, not post-delivery SA-REV.

8. **OUT locked** — Spec does not invent Cognito/SSO/IdP, mature vault/KMS, MCP breadth, fuller Assistant as MVP, MotorMarket/DC4, or a **5th Story** for OTel/audit/idempotent (Soft Spec weave on named slices only).

9. **Cost / spend** — PoC **$0**; any named LLM/API spend → **COO → CEO**; do not provision.

10. **Traceability + handshake** — Spec cites Option A tip + Stage C SA delta + SA Security PASS + sibling Spec Security checklists ×4; Spec QA must **not** PASS parent #18 until Security QA confirms **this** checklist (and named-slice Specs remain gated by their own Security QA).

## Handshake next
Senior Spec weaves parent #18 (if any) → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Spec + Senior PM. Named slices #66–#69 continue on their own checklists in parallel.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
