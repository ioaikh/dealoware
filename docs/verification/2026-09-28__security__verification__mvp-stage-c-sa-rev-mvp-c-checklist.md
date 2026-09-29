# Security checklist — Architecture (SA) · Gate #26 SA-REV-MVP-C (Stage C #67+#66+#68+#69+#18 post-delivery)

**Status:** Chief Security itemized points for Architecture step (handshake per `ops/ORG-OPS.md`). Issue **before** Architecture QA PASS.
**Date:** 2026-09-28
**Author:** Dealoware Chief Security
**Gate:** https://github.com/ioaikh/dealoware/issues/26 · **SA-REV-MVP-C** post-delivery review
**Stories reviewed:** https://github.com/ioaikh/dealoware/issues/67 · https://github.com/ioaikh/dealoware/issues/66 · https://github.com/ioaikh/dealoware/issues/68 · https://github.com/ioaikh/dealoware/issues/69 · https://github.com/ioaikh/dealoware/issues/18
**Parent:** #18 · Option A · Stage C remainder (as delivered pack)
**Deliverable:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-sa-rev-mvp-c-review.md`
**Prior Stage B Gate #25 PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-sa-rev-mvp-b-qa-confirm.md`
**Prior Stage C SA unlock Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md` (SoR checklist twin PR **#70** @ `83c15a3`)
**Binding tip:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` — CEO ACCEPTED Option A (**§3b** Platform hard wall + dual wall for **all** FieldClasses)
**Stage C SA delta brief:** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md`
**Moments:** `architecture/2026-09-20__sa__architecture__mvp-proposed-arch-review-moments.md` · SA-REV-MVP-C
**Delivery evidence (impl tips):** #67 PR **#78** @ `dab5822` · #66 PR **#79** @ `199125a` · #68 PR **#87** @ `c5485cf` · #69 PR **#100** @ `da61210` · #18 framing PR **#111** @ `7e7731e`
**Doc Overall PASS tip:** Soft HOLD SoR INDEX PR **#132** @ `5435de8` (CPM Soft HOLD Overall Doc PASS LOCKED; Soft HOLD BA-verify OPEN)
**Hold:** Soft HOLD Architecture QA PASS until Security QA confirms 1–10. Soft HOLD Gate **#26** status:done until CA PASS + Arch QA + Security Soft HOLD SoR CLEAR. Soft HOLD Gate **#27** HOLD. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft **#41** CLOSED via **#66+#67** (do not re-open). App Runner excluded. No MotorMarket/Cognito/DC4 invent. PoC **$0**.
**DOC-FLOW:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-rev-mvp-c-checklist.md`
**Tip:** `5435de8`

## Scope note
Architecture post-delivery review must score **delivered** Stage C security posture on tip for pack **#67+#66+#68+#69+#18** against Option A **§3b** + Stage C SA delta brief + Stage C SA unlock Security PASS (SoR #70 @ `83c15a3`). Real points — not N/A. This gate reviews Stage C **delivery** — it does **not** unlock Gate **#27** / V1 / invent multi-provider rewrite / Cognito / vault / App Runner greenfield / MotorMarket/DC4 / a 5th Story. Soft HOLD multi-provider Soft HOLD stands. BIND named-slice Security PASSes as evidence (cite; do **not** re-score each slice as a new Story).

## Itemized security points (Architecture review must answer)

1. **Agent/tool hard wall delivered (#67) = defense #2 on same FieldPolicy (§3b)** — Review confirms separate agent runtime gateway + per-Agent tool allowlist (deny-by-default) + **server-side** scrub of every tool response / context pack via the **same** Domain `IFieldPolicy.Evaluate` as the API/DB wall. Prompt-only / system-prompt soft guidance is **rejected** as sole control. Parallel agent ACL tables that drift from FieldPolicy are **rejected**. Dual wall applies to **all** FieldClasses (open-ended registry; CEO examples ≠ exhaustive). Cite #67 Security PASS / tip `dab5822`.

2. **No LoginEmail in agent context (#67)** — Review confirms no LoginEmail tool and LoginEmail never enters model / agent context packs; LoginEmail remains User-only (OwnAgent Deny held from Stage A); distinct from ContactEmail. Cite #67 Security PASS.

3. **ShareOutbound tools Accept-gated; prompt cannot escalate (#67)** — Review confirms share tools (e.g. ContactEmail ShareOutbound) require Accept grant **server-side**; deny regardless of prompt text. Prompt injection cannot grant FieldClasses Evaluate denies. Cite #67 Security PASS.

4. **Cross-agent / mediated exfil posture (#67)** — Review confirms cross-agent messaging (if any in Stage C scope) is mediated so payloads cannot include denied FieldClasses; agent↔agent exfil and prompt-injection threat rows addressed by gateway + scrub, **not** by trust in model behavior. Cite #67 Security PASS + Option A threat rows.

5. **Thin Assistant OwnAgent-only under the wall (#66) — Soft #41 CLOSED** — Review confirms thin Strategy-driven Assistant (X1 MVP partial) is OwnAgent-only; consumes StrategyBody only when Evaluate allows OwnAgent R/W for owner; runs **behind** the §3b / #67 gateway. Soft **#41 Assistant OUT** closes via **#66 + #67** delivery under wall — **not** claimed as Stage B–delivered Assistant/tool runtime (do **not** re-open Soft #41). Fuller free-form / multi-party → **DEFER V1**. Cite #66+#67 Security PASS / tips `199125a` + `dab5822`.

6. **A8-minimum meters + hard cutoff fail-closed (#68)** — Review confirms per-Participant meters for billable/LLM-touching Assistant (and Product-named Stage C metered surfaces without inventing extras) plus **hard cutoff**: further spend-path calls **fail closed** at budget (no soft warn-only). Mature metering / owner cost UI → **DEFER V3**. Metered path remains wall-bound (#67). Named LLM/API spend → **COO → CEO**; do not provision. PoC **$0**. Cite #68 Security PASS / tip `c5485cf`.

7. **X2 bot/UI must not bypass walls (#69)** — Review confirms Spec-locked **basic UI** (X2 MVP partial; exactly-one first-party bot **not** required for this minimum) exercises Authenticated Participant flows under existing auth + FieldPolicy and **must not** bypass the API wall or the agent/tool hard wall (no privileged back doors; no UI-only security). Soft HOLD multi-provider Soft HOLD stands. OpenAPI/webhooks → **DEFER V1**; MCP → **DEFER V5**. Cite #69 Security PASS / tip `da61210`.

8. **Consume Stage A/B tip + Option A; #18 maps remainder (not merge)** — Review confirms Stage C consumes Field ACL + list fail-closed + discovery scrub + StrategyBody ACL + ContactEmail ShareOutbound-after-Accept + Gates **#24/#25** PASS + Stage B Gate #25 PASS; binds Stage C delta to Option A **§3b** without rewriting accepted dual-wall tip. Parent **#18** framing maps remainder to complementary named slices **#67/#66/#68/#69** (not one merged mega-surface); cite framing PR **#111** @ `7e7731e` + #18 Doc/Product QA Security PASS. Do **not** Field-capture / re-score siblings as this gate invents new Stories.

9. **No inventing / Soft HOLD multi-provider / App Runner excluded / Gate #27 HOLD** — Review must **not** claim Cognito/SSO/IdP, mature vault/KMS, App Runner greenfield, MotorMarket/DC4, fuller Assistant as MVP-delivered, multi-provider rewrite as delivered, Gate **#27** unlock, or a **5th Story** for OTel/audit/idempotent (Soft weave on named slices only). Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start Soft HOLD stands. Hosting stay sketch-only (ECS Express `open`; App Runner excluded). Cost/critical → COO → CEO. PoC **$0**.

10. **Traceability + handshake** — Review cites Option A tip (**§3b** + Stage C row) + Stage C SA delta brief + Stage C SA unlock Security PASS (SoR #70 @ `83c15a3`) + Gate #25 PASS + delivery tips (#67/#66/#68/#69/#18) + Soft #41 CLOSED via #66+#67. Maps residual gaps to Gate **#27** HOLD, Soft HOLD multi-provider Soft HOLD, or CEO escalation (no guessing). Architecture QA must **not** PASS until Security QA confirms these points via points-review + `…sa-rev-mvp-c-qa-confirm.md`. Soft HOLD Gate **#26** status:done until CA PASS + Arch QA + Security Soft HOLD SoR CLEAR.

## Handshake next
1. Senior Architect answers points in `architecture/2026-09-28__sa__architecture__mvp-stage-c-sa-rev-mvp-c-review.md` § Security (cite sections / evidence).
2. Senior Security reviews → done-list to Security QA.
3. Security QA PASS to Chief Security (or further instructions).
4. Chief Security PASS/HOLD to CPM + Chief Architect.
5. Soft HOLD SoR twin (checklist) + later handshake Soft HOLD SoR ×2 (points-review + qa-confirm) via Docs — do **not** invent handshake Soft HOLD SoR until Senior+QA+Chief PASS.

## Cost/critical
No AWS / IdP / LLM spend without COO → CEO. PoC **$0**. Soft HOLD multi-provider Soft HOLD stands. App Runner excluded.
