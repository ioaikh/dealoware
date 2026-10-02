# Security checklist — Architecture (SA) · Gate #27 SA-REV-MVP-CLOSE (MVP post-milestone)

**Status:** Chief Security itemized points for Architecture step (handshake per `ops/ORG-OPS.md`). Issue **before** Architecture QA PASS.
**Date:** 2026-10-01
**Author:** Dealoware Chief Security
**Gate:** https://github.com/ioaikh/dealoware/issues/27 · **SA-REV-MVP-CLOSE** full MVP post-milestone review
**Moment:** SA-REV-MVP-CLOSE — `architecture/2026-09-20__sa__architecture__mvp-proposed-arch-review-moments.md`
**CEO unlock:** Named 2026-10-01 via Bot Manager · status:in-dev
**Deliverable:** `architecture/2026-10-01__sa__architecture__mvp-sa-rev-mvp-close-review.md` (intent / deviations / fit / plan-or-escalate + Security §)
**Prior Gate PASSes (binding cite):**
- Gate **#24** SA-REV-MVP-A — Stage A Security PASS (qa-confirm under `verification/2026-09-21__security__verification__mvp-stage-a-sa-rev-mvp-a-qa-confirm.md`)
- Gate **#25** SA-REV-MVP-B — Stage B Security PASS (`verification/2026-09-22__security__verification__mvp-stage-b-sa-rev-mvp-b-qa-confirm.md`; SoR cycle closed)
- Gate **#26** SA-REV-MVP-C — Stage C Security PASS (`verification/2026-09-28__security__verification__mvp-stage-c-sa-rev-mvp-c-qa-confirm.md`; SoR #134+#135 @ tip `eea9cfe`; Gate #26 CLOSED status:done)
**Binding tip:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` — CEO ACCEPTED Option A (**§3b** Platform hard wall + dual wall for **all** FieldClasses)
**Stage C SA delta (consumed):** `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md`
**Delivery evidence (impl tips):** #67 PR **#78** @ `dab5822` · #66 PR **#79** @ `199125a` · #68 PR **#87** @ `c5485cf` · #69 PR **#100** @ `da61210` · #18 framing PR **#111** @ `7e7731e` · Doc Overall / Gate #26 tip Soft HOLD SoR @ `eea9cfe`
**Hold:** Soft HOLD Architecture QA PASS until Security QA confirms 1–10. Soft HOLD Gate **#27** status:done until CA PASS + Arch QA + Security Soft HOLD SoR CLEAR (**qa-confirm SoR only** — Gate #25/#26 pattern; **do not invent points-review Soft HOLD SoR**). Soft HOLD **V1 phase unlock** until CA PASS. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft **#41** CLOSED via **#66+#67** (do not re-open). App Runner excluded. No MotorMarket/Cognito/DC4 invent. PoC **$0**.
**DOC-FLOW:** `verification/2026-10-01__security__verification__mvp-sa-rev-mvp-close-checklist.md`
**Tip:** `eea9cfe`

## Scope note
Architecture post-milestone review must score **full MVP tip** security posture (`eea9cfe`) against Option A **§3b** dual-wall + Gates **#24/#25/#26** Security PASS + Stage A/B/C delivery evidence, and map **fit to V1+** (residual gaps → plan-or-escalate; **not** claim as MVP-delivered). Real points — not N/A. This gate does **not** unlock multi-provider rewrite, Cognito/SSO/IdP, mature vault/KMS, App Runner greenfield, MotorMarket/DC4, MCP marketplace, or invent a new Story. Soft HOLD multi-provider Soft HOLD stands. Soft HOLD V1 unlock Soft HOLD until CA PASS. BIND named Gate PASSes as evidence (cite; do **not** re-score each Stage as a new Story).

## Itemized security points (Architecture review must answer)

1. **Option A dual-wall held on tip (full MVP)** — Review confirms tip `eea9cfe` still implements CEO-accepted Option A **§3b**: Platform API/DB FieldPolicy wall **and** agent/tool hard wall (defense #2) using the **same** Domain `IFieldPolicy.Evaluate` for **all** FieldClasses (open-ended registry; CEO examples ≠ exhaustive). Prompt-only / soft-guidance-as-sole-control **rejected**. Parallel agent ACL tables that drift from FieldPolicy **rejected**. Cite Option A tip + Gate #26 PASS / Stage C #67 tip `dab5822`.

2. **Stage A foundations held (#24 PASS)** — Review confirms Artifact CRUD + auth/registration posture from Gate **#24** Security PASS remains on tip: Field ACL / list fail-closed / discovery scrub where Stage A locked them; no regression that weakens Participant secrets or tenant isolation. Cite Gate #24 qa-confirm + Stage A SoR. Do **not** invent Cognito/SSO as MVP-delivered.

3. **Stage B discovery + Strategy + contact-on-Accept held (#25 PASS)** — Review confirms Gate **#25** Security PASS posture remains: StrategyBody ACL, ContactEmail ShareOutbound-after-Accept, discovery surfaces under FieldPolicy, seal→contact on Accept without MM/DC4 invent. Soft #41 was **OUT** at Stage B by design; closure is via Stage C (#66+#67) — do not re-open as Stage B gap. Cite Gate #25 qa-confirm.

4. **Stage C agent/tool hard wall + Soft #41 CLOSED (#26 PASS / #67+#66)** — Review confirms separate agent runtime gateway + per-Agent tool allowlist (deny-by-default) + **server-side** scrub of every tool response / context pack via same Evaluate; thin Strategy-driven Assistant (X1 MVP partial) OwnAgent-only **behind** the wall; Soft **#41** CLOSED via **#66+#67** delivery under wall — **not** claimed as Stage B–delivered Assistant/tool runtime. Fuller free-form / multi-party → **DEFER V1** (fit row). Cite Gate #26 PASS + tips `dab5822` + `199125a`.

5. **LoginEmail never in agent context; ShareOutbound Accept-gated** — Review confirms no LoginEmail tool and LoginEmail never enters model / agent context packs (LoginEmail User-only; OwnAgent Deny held from Stage A; distinct from ContactEmail). Share tools (e.g. ContactEmail ShareOutbound) require Accept grant **server-side**; deny regardless of prompt text; prompt injection cannot grant FieldClasses Evaluate denies. Cite #67 Security PASS / Gate #26.

6. **A8-minimum meters + hard cutoff fail-closed (#68)** — Review confirms per-Participant meters for billable/LLM-touching Assistant (and Product-named Stage C metered surfaces without inventing extras) plus **hard cutoff**: further spend-path calls **fail closed** at budget (no soft warn-only). Mature metering / owner cost UI → **DEFER V3**. Metered path remains wall-bound (#67). Named LLM/API spend → **COO → CEO**; do not provision. PoC **$0**. Cite #68 tip `c5485cf` + Gate #26.

7. **X2 bot/UI must not bypass walls (#69)** — Review confirms Spec-locked **basic UI** (X2 MVP partial) exercises Authenticated Participant flows under existing auth + FieldPolicy and **must not** bypass the API wall or the agent/tool hard wall (no privileged back doors; no UI-only security). Soft HOLD multi-provider Soft HOLD stands. OpenAPI/webhooks → **DEFER V1**; MCP → **DEFER V5**. Cite #69 tip `da61210` + Gate #26.

8. **Cross-agent / mediated exfil + parent #18 remainder mapped (not merge)** — Review confirms cross-agent messaging (if any in MVP scope) is mediated so payloads cannot include denied FieldClasses; agent↔agent exfil and prompt-injection threat rows addressed by gateway + scrub, **not** by trust in model behavior. Parent **#18** framing maps remainder to complementary named slices **#67/#66/#68/#69** (not one merged mega-surface); cite framing PR **#111** @ `7e7731e` + #18 Doc/Product QA Security PASS. Do **not** Field-capture / invent a 5th Story.

9. **No inventing / Soft HOLD multi-provider / App Runner excluded / Soft HOLD V1 unlock** — Review must **not** claim Cognito/SSO/IdP, mature vault/KMS, App Runner greenfield, MotorMarket/DC4, fuller Assistant as MVP-delivered, multi-provider rewrite as delivered, MCP marketplace, or V1 phase unlocked. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start Soft HOLD stands. Soft HOLD **V1 phase unlock** Soft HOLD until **CA PASS** on this gate. Hosting stay sketch-only (ECS Express `open`; App Runner excluded). Cost/critical → COO → CEO. PoC **$0**.

10. **Traceability + fit to V1+ + handshake** — Review cites Option A tip (**§3b**) + Gates **#24/#25/#26** Security PASS + Stage C SA delta + delivery tips (#67/#66/#68/#69/#18) + Soft #41 CLOSED via #66+#67 + tip `eea9cfe`. Explicit **fit / plan-or-escalate** rows map residual gaps to Soft HOLD multi-provider Soft HOLD, Soft HOLD V1 unlock Soft HOLD (until CA PASS), DEFER V1/V3/V5 buckets, or CEO escalation (no guessing). Architecture QA must **not** PASS until Security QA confirms these points via `…mvp-sa-rev-mvp-close-qa-confirm.md` (Gate #25/#26 pattern: **qa-confirm Soft HOLD SoR only** — **do not invent points-review Soft HOLD SoR**). Soft HOLD Gate **#27** status:done until CA PASS + Arch QA + Security Soft HOLD SoR CLEAR.

## Handshake next
1. Senior Architect answers points in `architecture/2026-10-01__sa__architecture__mvp-sa-rev-mvp-close-review.md` § Security (cite sections / evidence).
2. Senior Security may file points-review (content OK) — **handshake Soft HOLD SoR = qa-confirm only** (Gate #25/#26 pattern; do **not** Soft HOLD SoR points-review).
3. Security QA PASS to Chief Security (or further instructions) → `…mvp-sa-rev-mvp-close-qa-confirm.md`.
4. Chief Security PASS/HOLD to CPM + Chief Architect.
5. Soft HOLD SoR twin (this checklist) via Docs now; later handshake Soft HOLD SoR **qa-confirm only** after Senior+QA+Chief PASS. Soft HOLD score until checklist Soft HOLD SoR CLEAR.

## Cost/critical
No AWS / IdP / LLM spend without COO → CEO. PoC **$0**. Soft HOLD multi-provider Soft HOLD stands. Soft HOLD V1 unlock Soft HOLD until CA PASS. App Runner excluded.
