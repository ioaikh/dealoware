# Doc step weave — Security checklist pts 1–10 · MVP Stage C parent #18 (Option A Spec unlock / Stage C remainder framing)

| Field | Value |
|-------|-------|
| **Status** | **ISSUED** — Soft HOLD treat #18 Doc as PASS until Doc handshake Soft HOLD SoR MERGED (Product QA SoR ≠ Doc). Soft HOLD status:done until CBA. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. |
| **Date** | 2026-09-28 |
| **Author** | Dealoware Senior Docs |
| **Story** | GitHub issue #18 · Participant secrets/ACL · Option A · Stage C Spec unlock framing |
| **Issue** | https://github.com/ioaikh/dealoware/issues/18 |
| **Framing PR** | https://github.com/ioaikh/dealoware/pull/111 (**MERGED**) @ `7e7731e` — `docs/verification/2026-09-28__sd__verification__mvp-stage-c-parent-18-framing.md` |
| **Checklist (binding, ISSUED)** | `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-doc-checklist.md` (Soft HOLD SoR twin PR **#129** @ `97f6cd3`) |
| **DOC-FLOW** | `ops/2026-09-28__docs__ops__mvp-stage-c-parent-18-doc-security-weave.md` |
| **Constraints** | Framing-only · BIND **#67** without Field-capture · do **not** re-score **#66/#67/#68/#69** · Soft **#41** CLOSED via **#66+#67** · Gate **#26** backlog · Gate **#27** HOLD · PoC **$0** · Soft HOLD multi-provider · no Cognito / MotorMarket / DC4 / vault / 5th Story invent |

## Primary surfaces Security will score
1. Framing PR #111 — Option A Spec unlock / Stage C remainder map on main `7e7731e` (Steps 1–9 PASS; framing/map only; no product code)
2. This Doc weave + living `INDEX.md` annotations for #18 Doc paths
3. Binding Doc checklist Soft HOLD SoR twin PR #129 @ `97f6cd3`

Supporting (not primary score surface; **not** overall Doc PASS): Product QA Security trio CLEAR (#125/#126/#128); SD Security Soft HOLD SoR #114; Spec / DevPlan Security PASS. Handshake points-review + qa-confirm **not invented**.

## Explicit separations (non-merge)
- **#66 thin Assistant** — named-slice Doc separate; bind only; do **not** re-score.
- **#67 hard wall** — BIND without Field-capture; map/cross-ref only; do **not** re-score #67 as this Story.
- **#68 A8-min meters** — named-slice Doc separate; do **not** re-score.
- **#69 X2 basic UI** — named-slice Doc separate; do **not** re-score.
- **Soft #41 Assistant OUT** — CLOSED via **#66+#67** only (do not re-open from #18).
- **Parent #18** — framing-only; does **not** Field-capture #66–#69.
- **Gate #26** — backlog. **Gate #27** — HOLD.
- **Soft HOLD multi-provider** — Spec/doc rewrite until BM multi-provider start.
- **PoC $0** — no IdP/vault/LLM provision / Cognito / MotorMarket / DC4 / 5th Story as delivered by #18 Docs.

## Point → how Doc artifacts satisfy

Mapped strictly to `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-doc-checklist.md` (pts 1–10). Handshake files indexed after Security PASS (not invented).

| # | Security point | Doc satisfaction (cites) |
|---|----------------|--------------------------|
| 1 | Option A dual wall end-state | Docs: API/DB wall (Stage A/B tip) + agent/tool wall (Stage C / #67) as dual defense for **all** FieldClasses; **reject** prompt-only soft guidance as sole control; framing/doc only — implementation on #67 (map; do **not** re-score #67). Framing PR #111 @ `7e7731e`. |
| 2 | FieldClass registry open-ended | Docs: LoginEmail / ContactEmail / DisplayName / StrategyBody as **examples**, not exhaustive; dual wall applies to all FieldClasses; no inventing new named FieldClasses unless CEO/Product locks them. |
| 3 | Stage C remainder map (not merge) | Docs map #18 AC remainder to named slices: **#67** hard wall + scrub; **#66** thin Assistant under wall; **#68** A8-min hard cutoff; **#69** X2 basic UI no bypass — complementary, not one merged surface; parent does **not** deliver sibling features. |
| 4 | Soft #41 Assistant OUT | Docs: soft #41 closes only via **#66 + #67** delivery under wall — not claimed as Stage B–delivered Assistant/tool runtime (do **not** re-open Soft #41). |
| 5 | No LoginEmail share / ShareOutbound Accept-gated | Docs preserve LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools); map/cross-ref only — BIND #67; no Field-capture. |
| 6 | Threat rows bound to #67 | Docs bind CEO prompt-injection / agent↔agent exfil posture to gateway + scrub (#67), not model trust; map/cross-ref only — BIND #67; no Field-capture / re-score. |
| 7 | Gate #26 / #27 HOLD | Docs: Gate **#26** remains backlog until Stage C delivery; Gate **#27** HOLD; parent Doc is framing-step, not post-delivery SA-REV unlock. |
| 8 | OUT locked / no invent | Docs: no Cognito/SSO/IdP, mature vault/KMS, MCP breadth, fuller Assistant as MVP, MotorMarket/DC4, or a **5th Story** for OTel/audit/idempotent (Soft weave on named slices only); Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start; keep #66/#67/#68/#69 separate Doc. |
| 9 | Cost / spend | Docs: PoC **$0**; any named LLM/API spend → **COO → CEO**. |
| 10 | Handshake close | **OPEN.** Docs QA must **not** PASS parent #18 until Security QA confirms these points via points-review + `…parent-18-doc-qa-confirm.md`. Named-slice Doc remain gated by their own Security QA. Parent does **not** Field-capture #66–#69. Soft HOLD Overall Doc PASS until handshake Soft HOLD SoR + INDEX (CPM). Handshake Soft HOLD SoR later after Senior Security + Security QA — **not invented**. |

## Indexed Product QA Security (Sec10 for Product QA — already CLEAR; not overall Doc PASS)
- `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-productqa-checklist.md` (Soft HOLD SoR PR **#125** @ `dcc503b`)
- `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-productqa-points-review.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-productqa-qa-confirm.md` (**PASS**; Soft HOLD SoR handshake PR **#126** @ `1501616`)
- Product QA report: `qa/2026-09-28__qa__qa-report__mvp-stage-c-participant-isolation-option-a-remainder.md` (Soft HOLD SoR PR **#128** @ `512663e`)

## Indexed SD Security (SD step — already CLEAR; not overall Doc PASS)
- `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-sd-checklist.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-sd-points-review.md`
- `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-sd-qa-confirm.md` (**PASS**; Soft HOLD SoR PR **#114** @ `32e97d2`)
- Framing evidence: `verification/2026-09-28__sd__verification__mvp-stage-c-parent-18-framing.md` (PR **#111** @ `7e7731e`)

## Indexed Doc Security handshake
- Binding checklist (**ISSUED**, Soft HOLD SoR CLEAR): `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-doc-checklist.md` (PR **#129** @ `97f6cd3`)
- Senior Security points-review: **not invented** — Soft HOLD SoR later after this weave + Senior Security + Security QA
- Security QA confirm: **not invented** — Soft HOLD SoR later (`…parent-18-doc-qa-confirm.md`)

## Close
**Soft HOLD Overall Doc PASS** / treat #18 Doc as PASS until handshake Soft HOLD SoR MERGED + INDEX. Soft HOLD status:done until CBA. Soft HOLD multi-provider. Do **not** CLOSE issue #18 from this weave. Do **not** flip status:done.
