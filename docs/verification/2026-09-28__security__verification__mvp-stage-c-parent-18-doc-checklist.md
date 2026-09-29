# Security checklist — Doc · MVP Stage C parent #18 (Option A Spec unlock / Stage C remainder framing)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.
**Date:** 2026-09-28
**Author:** Dealoware Chief Security
**Story:** https://github.com/ioaikh/dealoware/issues/18 · Participant secrets/ACL · Option A · Stage C Spec unlock framing
**Named slices (separate Doc + separate Security):** #66 · #67 · #68 · #69 — **do NOT Field-capture or re-score**
**Order:** parent Doc last after named-slice Doc
**Framing evidence:** PR **#111** @ `7e7731e` — `docs/verification/2026-09-28__sd__verification__mvp-stage-c-parent-18-framing.md`
**Product QA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-productqa-qa-confirm.md`
**Product QA handshake Soft HOLD SoR:** PR **#126** @ `1501616` CLEAR
**Product QA report Soft HOLD SoR:** PR **#128** @ `512663e` CLEAR (`docs/qa/…participant-isolation-option-a-remainder.md` + INDEX)
**Product QA checklist Soft HOLD SoR:** PR **#125** @ `dcc503b` CLEAR
**SD Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-sd-qa-confirm.md`
**SD handshake Soft HOLD SoR:** PR **#114** @ `32e97d2` CLEAR
**Spec Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-qa-confirm.md`
**Dev Plan Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-devplan-qa-confirm.md`
**Hold:** Soft HOLD Overall Doc PASS until Doc handshake Soft HOLD SoR + INDEX (CPM). Treat #18 Doc as PASS until handshake Soft HOLD SoR MERGED + INDEX. Soft HOLD `status:done` until CBA. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft **#41** CLOSED via **#66+#67**. Gate **#26** backlog; Gate **#27** HOLD. BIND **#67** without Field-capture. Parent framing-only. PoC **$0**; no MotorMarket/Cognito/DC4.
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-doc-checklist.md`
**Tip:** `512663e`

## Scope note
Doc must accurately describe parent #18 Option A dual-wall isolation **end-state** and map Stage C remainder to named slices **#66–#69** — **map/document only**. Real points — not N/A. Scope **#18 ONLY**. Product QA SoR ≠ Doc — this is a separate Doc-step checklist. BIND **#67**; do **not** Field-capture or re-score #66/#67/#68/#69. Do **not** invent Cognito/MM/DC4/vault, Gate #26 unlock, multi-provider rewrite, or a 5th Story. Evidence via framing PR #111 + Product QA / SD / Spec / Dev Plan Security PASS (not sibling delivery re-doc). Keep #66/#67/#68/#69 separate Doc.

## Itemized security points (Doc must satisfy)

1. **Option A dual wall end-state** — Docs state API/DB wall (Stage A/B tip) + agent/tool wall (Stage C / #67) as dual defense for **all** FieldClasses; **reject** prompt-only soft guidance as sole control; framing/doc only — implementation on #67 (map; do **not** re-score #67).

2. **FieldClass registry open-ended** — Docs treat LoginEmail / ContactEmail / DisplayName / StrategyBody as **examples**, not exhaustive; dual wall applies to all FieldClasses; no inventing new named FieldClasses unless CEO/Product locks them.

3. **Stage C remainder map (not merge)** — Docs map #18 AC remainder to named slices: **#67** hard wall + scrub; **#66** thin Assistant under wall; **#68** A8-min hard cutoff; **#69** X2 basic UI no bypass — complementary, not one merged surface; parent does **not** deliver sibling features.

4. **Soft #41 Assistant OUT** — Docs state soft #41 closes only via **#66 + #67** delivery under wall — not claimed as Stage B–delivered Assistant/tool runtime (do **not** re-open Soft #41).

5. **No LoginEmail share / ShareOutbound Accept-gated** — Docs preserve LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools); map/cross-ref only — BIND #67; no Field-capture.

6. **Threat rows bound to #67** — Docs bind CEO prompt-injection / agent↔agent exfil posture to gateway + scrub (#67), not model trust; map/cross-ref only — BIND #67; no Field-capture / re-score.

7. **Gate #26 / #27 HOLD** — Docs state Gate **#26** remains backlog until Stage C delivery; Gate **#27** HOLD; parent Doc is framing-step, not post-delivery SA-REV unlock.

8. **OUT locked / no invent** — Docs confirm no Cognito/SSO/IdP, mature vault/KMS, MCP breadth, fuller Assistant as MVP, MotorMarket/DC4, or a **5th Story** for OTel/audit/idempotent (Soft weave on named slices only); Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start; keep #66/#67/#68/#69 separate Doc.

9. **Cost / spend** — Docs state PoC **$0**; any named LLM/API spend → **COO → CEO**.

10. **Handshake close** — Docs QA must **not** PASS parent #18 until Security QA confirms these points via points-review + `…parent-18-doc-qa-confirm.md`. Named-slice Doc remain gated by their own Security QA. Parent does **not** Field-capture #66–#69. Soft HOLD Overall Doc PASS until handshake Soft HOLD SoR + INDEX (CPM).

## Handshake next
Senior Docs weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP/LLM spend without COO → CEO. PoC **$0**.
