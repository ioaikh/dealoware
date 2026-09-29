# Security checklist — Product QA · MVP Stage C parent #18 (Option A Spec unlock / Stage C remainder framing)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA / QAQA PASS on parent #18 framing.
**Date:** 2026-09-28
**Author:** Dealoware Chief Security
**Story:** https://github.com/ioaikh/dealoware/issues/18 · Participant secrets/ACL · Option A · Stage C Spec unlock framing
**Named slices (separate Product QA + separate Security):** #66 · #67 · #68 · #69 — **do NOT Field-capture or re-score**
**Order:** parent framing last after named-slice Product QA
**Binding SD checklist:** `docs/verification/2026-09-28__security__verification__mvp-stage-c-parent-18-sd-checklist.md` (also KB twin)
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-sd-qa-confirm.md`
**SD handshake Soft HOLD SoR:** PR **#114** @ `32e97d2` CLEAR
**Framing evidence:** PR **#111** @ `7e7731e` (docs framing; map isolation end-state)
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-qa-confirm.md`
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-devplan-qa-confirm.md`
**Hold:** Soft HOLD Product QA PASS until Senior Security → Security QA `…parent-18-productqa-qa-confirm.md` → QAQA. Soft HOLD Doc until Product QA PASS (+ handshake Soft HOLD SoR). Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft **#41** CLOSED via **#66+#67**. Gate **#26** backlog; Gate **#27** HOLD. BIND **#67** without Field-capture. PoC **$0**.
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-productqa-checklist.md`
**Tip:** `4a22434` / framing HEAD `7e7731e`

## Scope note
Product QA must **verify** parent #18 framing maps Option A dual-wall isolation **end-state** and maps remainder to named slices **#66–#69** — **map/verify only**. Real points — not N/A. Scope **#18 ONLY**. BIND **#67**; do **not** Field-capture or re-score #66/#67/#68/#69. Do **not** invent Cognito/MM/DC4/vault, Gate #26 unlock, multi-provider rewrite, or a 5th Story. Evidence via framing PR #111 + SD Security PASS + Spec/Dev Plan Security PASS (not sibling delivery re-test). Product QA Soft HOLD SoR ≠ Doc.

## Itemized security points (Product QA must verify)

1. **Option A dual wall end-state map** — Verify framing restates API/DB wall (Stage A/B tip) + agent/tool wall (Stage C / #67) as dual defense for **all** FieldClasses; **reject** prompt-only soft guidance as sole control; framing only — implementation verified on #67 (map/done-list; do **not** re-score #67).

2. **FieldClass registry open-ended** — Verify LoginEmail / ContactEmail / DisplayName / StrategyBody treated as **examples**, not exhaustive; dual wall applies to all FieldClasses; no inventing new named FieldClasses unless CEO/Product locks them.

3. **Stage C remainder map (not merge)** — Verify #18 AC remainder maps to named slices: **#67** hard wall + scrub; **#66** thin Assistant under wall; **#68** A8-min hard cutoff; **#69** X2 basic UI no bypass — complementary, not one merged surface; parent does **not** implement sibling delivery.

4. **Soft #41 Assistant OUT** — Verify soft #41 closes only via **#66 + #67** delivery under wall — not claimed as Stage B–delivered Assistant/tool runtime (do **not** re-open Soft #41).

5. **No LoginEmail share / ShareOutbound Accept-gated** — Verify framing preserves LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools); map/cross-ref only — BIND #67; no Field-capture.

6. **Threat rows bound to #67** — Verify CEO prompt-injection / agent↔agent exfil posture bound to gateway + scrub (#67), not model trust; map/cross-ref only — BIND #67; no Field-capture / re-score.

7. **Gate #26 / #27 HOLD** — Verify Gate **#26** remains backlog until Stage C delivery; Gate **#27** HOLD; parent Product QA is framing-step, not post-delivery SA-REV unlock.

8. **OUT locked / no invent** — Confirm no Cognito/SSO/IdP, mature vault/KMS, MCP breadth, fuller Assistant as MVP, MotorMarket/DC4, or a **5th Story** for OTel/audit/idempotent (Soft weave on named slices only); Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start; keep #66/#67/#68/#69 separate Product QA.

9. **Cost / spend** — Confirm PoC **$0**; any named LLM/API spend → **COO → CEO**; do not provision.

10. **Handshake close** — Product QA / QAQA must **not** PASS parent #18 until Security QA confirms **this** Product QA checklist via points-review + `…parent-18-productqa-qa-confirm.md`. Named-slice Product QA remain gated by their own Security QA. Parent does **not** Field-capture #66–#69. Soft HOLD Doc until Product QA PASS (+ handshake Soft HOLD SoR).

## Handshake next
Senior Product QA → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief QA + Senior PM.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
