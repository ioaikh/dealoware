# Security QA — Spec #142 / Product Step 2 AWS architecture design Spec vs Chief Security Spec checklist

**Author:** Dealoware Security QA  
**Date:** 2026-10-02  
**Verdict:** **PASS** (all 10 points MET)  
**Asked by:** Senior Security — Soft HOLD Security QA LIFTED (Senior PASS); Soft HOLD Spec QA PASS until this confirm  
**Chief checklist (binding):** `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md` (10 points)  
**SoR checklist twin (GitHub):** `docs/verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md` (PR **#143** @ `9654aff7622f4eb132db62a8c6738b774f2e82de` / `9654aff`) **CLEAR** (HTTP 200; tip Soft HOLD SoR `adbb310`; INDEX bare row MATCH)  
**Senior Security done-list:** `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-points-review.md` (**PASS** 10/10 — cited; independently re-scored; agrees)  
**Spec deliverable:** `specs/2026-10-02__spec__spec__aws-cloud-architecture-design-ecs-express.md` (§10 Security Spec checklist binding 1–10 MET + §§1–9 supporting)  
**Sibling SA track (cite-only — do NOT re-score Arch):** `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-qa-confirm.md` Soft HOLD SoR **#145** @ `d3e8bb9`  
**Issue:** https://github.com/ioaikh/dealoware/issues/142 · Spec Step 2 — AWS cloud architecture design (design-only Soft HOLD provision)  
**DOC-FLOW:** `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-qa-confirm.md`  
**Tip (Soft HOLD SoR):** `adbb310`  
**Tip (delivery / SA qa-confirm Soft HOLD SoR):** `d3e8bb9`  
**Constraints:** Soft HOLD Spec QA PASS until this confirm (lifted by PASS). Soft HOLD Spec Step 2 formal CLOSE until Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX PASS (per checklist Soft HOLD SoR rules). Soft HOLD **AWS account / resource provision / spend**. Soft HOLD invent product Stories / invent AC beyond design SoR. Soft HOLD invent Spec/AC Steps **3–5**. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft **#41** CLOSED via **#66+#67** (do not re-open). App Runner excluded. No MotorMarket/Cognito invent as delivered / DC4 / mature vault as delivered. Gate **#27** CLOSED stay closed. Handshake Soft HOLD SoR = **qa-confirm only** — **do not invent points-review Soft HOLD SoR**. Soft HOLD SoR checklist CLEAR PR **#143** @ `9654aff` ≠ handshake Soft HOLD SoR. Sibling SA Security QA PASS cite-only — do **not** re-score Arch. PoC **$0**.

## Soft notes accepted (non-blocking)

| Soft note | Disposition |
|-----------|-------------|
| Soft HOLD Spec QA PASS until this confirm | **Accepted** — lifted by PASS |
| Soft HOLD Spec Step 2 formal CLOSE until Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX | **Accepted** — do **not** invent handshake Soft HOLD SoR / claim Step 2 CLOSED |
| Soft HOLD SoR checklist CLEAR PR **#143** @ `9654aff` / tip Soft HOLD SoR `adbb310` | **Accepted** — Soft HOLD SoR twin only; ≠ handshake Soft HOLD SoR |
| Handshake Soft HOLD SoR = **qa-confirm only** (no points-review twin) | **Accepted** — Gate #25/#26/#27 pattern |
| Soft HOLD AWS provision / Soft HOLD invent Stories / Soft HOLD invent AC beyond design / Soft HOLD Steps 3–5 invent / Soft HOLD multi-provider | **Accepted** — Soft HOLDs stand |
| Soft **#41** CLOSED via **#66+#67**; Gate **#27** CLOSED stay closed | **Accepted** — do not reopen |
| Sibling SA Security QA PASS Soft HOLD SoR **#145** @ `d3e8bb9` cite-only | **Accepted** — Arch track CLEARED; not re-scored here |
| App Runner OUT; Cognito/SSO/mature vault Soft HOLD later maturity | **Accepted** — not Step 2 delivered |
| Soft OTel/audit Soft weave only (no 5th Story) | **Accepted** |

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security Spec checklist | `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md` | Binding 10 points |
| SoR checklist twin (GitHub) | `docs/verification/…mvp-spec-step-2-aws-arch-design-checklist.md` (PR #143 @ `9654aff`) | CLEAR — Soft HOLD SoR twin only (≠ handshake SoR); verified `gh pr view` MERGED; raw HTTP 200 |
| Senior Security points-review | `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-points-review.md` | **PASS** 10/10 — present; aligned |
| Spec deliverable (§10 + §§1–9) | `specs/2026-10-02__spec__spec__aws-cloud-architecture-design-ecs-express.md` | Security answers 1–10 **MET** with cites |
| Sibling SA Security QA PASS | `…mvp-sa-step-2-aws-arch-design-qa-confirm.md` Soft HOLD SoR #145 @ `d3e8bb9` | Cite-only; do not re-score Arch |
| Tip Soft HOLD SoR | `adbb310` | Spec checklist Soft HOLD SoR PR #143 (+ SA checklist #144) |
| Delivery / SA Soft HOLD SoR tip | `d3e8bb9` | SA qa-confirm Soft HOLD SoR PR #145 |

## Independent re-score (Security QA)

Score vs **official Spec Step 2 AWS architecture checklist 1–10** only. Surfaces: Senior PASS + Soft HOLD SoR checklist CLEAR PR **#143** @ `9654aff` + Spec §10 + §§1–9 + tip Soft HOLD SoR `adbb310`. Sibling SA PASS cite-only — do **not** re-score Arch.

| # | Point | Senior | Security QA | Evidence |
|---|-------|--------|-------------|---------|
| 1 | Design-only Soft HOLD provision / Soft HOLD spend | MET | **MET** | Spec §10#1 + Locked #0/#9 + Senior: design Spec/SoR only; no AWS account create / cluster/service/resource provision / paid plugins / non-local deploy claimed delivered. Later provision/spend → **COO → CEO** (CA/CPM path as locked). PoC **$0**. Soft HOLD SoR #143 @ `9654aff`. |
| 2 | Host ECS Express Mode; App Runner OUT | MET | **MET** | Spec §10#2 + Locked #1/#2 + Senior: **Amazon ECS Express Mode (Fargate)** design baseline (status `open` currency 2026-10-02). App Runner OUT. Soft HOLD invent alternate greenfield host. |
| 3 | Tenancy inherits Option A dual-wall | MET | **MET** | Spec §10#3 + Locked #3 + Senior: Option A **§3b** Platform API/DB FieldPolicy wall **and** agent/tool hard wall via same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses. Prompt-only sole control **rejected**; parallel agent ACL tables **rejected**. Soft HOLD tenant shortcuts. Cite Option A + Gate #27 CLOSED / Stage C #67. |
| 4 | Secrets / PII Soft HOLD invent mature vault | MET | **MET** | Spec §10#4 + Locked #5 + Senior: identity-until-accept; LoginEmail never in agent/model context; ContactEmail ShareOutbound Accept-gated server-side. Mature vault / KMS / Cognito/SSO/IdP Soft HOLD later maturity — **not** Step 2 delivered. Soft HOLD invent Cognito as Step-2-delivered. |
| 5 | Store shape fail-closed under FieldPolicy | MET | **MET** | Spec §10#5 + Locked #4 + Senior: SQLite → PostgreSQL-when-AWS-shape keeps Field ACL / list fail-closed / discovery scrub / StrategyBody ACL. Soft HOLD invent shared-DB-without-wall / cross-tenant reads / Field-capture migration Stories here. No CQRS invent. |
| 6 | Observability Soft HOLD paid / Soft HOLD PII leak | MET | **MET** | Spec §10#6 + Locked #6 + Senior: CloudWatch default + OTel Soft weave. Soft HOLD invent paid observability beyond free tiers without COO→CEO. Soft HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces. Soft HOLD invent OTel/audit as a **5th Story**. |
| 7 | Security KPI is real (not slogan) | MET | **MET** | Spec §10#7 + Locked #3/#9 + Senior: security KPI measurable vs dual-wall hold, fail-closed budget/cutoff (#68), Accept-gated ShareOutbound, Soft HOLD provision. Cost/perf KPIs **must not** authorize drop wall #2, share secrets across tenants, or App Runner greenfield. Cite Product Step 2 KPI triad. |
| 8 | Scale-out Soft HOLD multi-provider / Soft HOLD Steps 3–5 invent | MET | **MET** | Spec §10#8 + Locked #7/#8/#10 + Senior: Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start; Soft HOLD invent Spec/AC Steps **3–5**; Soft HOLD invent product Stories / invent AC beyond design SoR. Soft **#41 CLOSED** via **#66+#67** — not reopened as host gap. |
| 9 | OUT / Soft HOLD locked pack | MET | **MET** | Spec §10#9 + Locked #0/#1/#8/#9/#10 + Senior: AWS provision/spend Soft HOLD; App Runner; Soft HOLD multi-provider rewrite; invent Stories Soft HOLD; invent Steps 3–5 Spec/AC Soft HOLD; Gate **#27** CLOSED stay closed; MotorMarket/DC4 OUT; Cognito/SSO/mature vault as delivered OUT; MCP marketplace OUT; PoC **$0** provision. Cost/critical → COO → CEO. |
| 10 | Traceability + handshake Soft HOLD SoR pattern | MET | **MET** | Spec §10#10 + Senior: cites Product Step 2 + CA PASS Option A + Arch QA PASS + SA Security QA qa-confirm + Spec Security checklist + DevOps ECS Express lock + DevOps assist ALIGN + Option A §3b + Gate #27 CLOSED + Soft #41 CLOSED via #66+#67 + tip Soft HOLD SoR `adbb310` + Soft HOLD SoR checklist CLEAR PR **#143** @ `9654aff`. Spec QA Soft HOLD until this confirm. Soft HOLD Spec Step 2 formal CLOSE until Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX. Handshake Soft HOLD SoR = **qa-confirm only** — **do not invent points-review Soft HOLD SoR**. Soft HOLD SoR twin ≠ handshake Soft HOLD SoR. Sibling SA Soft HOLD SoR **#145** @ `d3e8bb9` cite-only. |

## Alignment with Senior Security done-list

Senior Spec points-review scored pts **1–10 MET** on Spec §10 + Soft HOLD SoR checklist CLEAR PR **#143** @ `9654aff` / tip Soft HOLD SoR `adbb310` + sibling SA Soft HOLD SoR **#145** @ `d3e8bb9` cite-only, with soft notes on Soft HOLD Spec QA until Security QA, Soft HOLD Spec Step 2 formal CLOSE until Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX, Soft HOLD provision / invent Stories / multi-provider / Steps 3–5, Gate #27 CLOSED, Soft #41 CLOSED via #66+#67, and handshake Soft HOLD SoR = qa-confirm only. Independent Security QA re-score **agrees** on all 10; soft notes **accepted**. No bounce. No gaps vs Senior. Soft HOLD Security QA LIFTED (Senior PASS + Soft HOLD SoR CLEAR).

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Design-only; Soft HOLD provision / Soft HOLD spend; PoC $0 | Held (§10#1) |
| ECS Express Mode `open`; App Runner OUT | Held (§10#2) |
| Option A §3b dual wall inherited; prompt-only sole control rejected | Held (§10#3) |
| LoginEmail never in agent context; ShareOutbound Accept-gated; Soft HOLD mature vault/Cognito as delivered | Held (§10#4) |
| Store path fail-closed under FieldPolicy; Soft HOLD invent migration Stories | Held (§10#5) |
| Observability Soft HOLD paid / Soft HOLD PII leak; no 5th Story | Held (§10#6) |
| Security KPI real; cost/perf must not regress walls | Held (§10#7) |
| Soft HOLD multi-provider; Soft HOLD Steps 3–5 invent; Soft #41 CLOSED | Held (§10#8) |
| OUT pack + Gate #27 CLOSED stay closed | Held (§10#9) |
| Soft HOLD Spec Step 2 formal CLOSE; handshake Soft HOLD SoR = qa-confirm only; Soft HOLD SoR twin ≠ handshake | Held (§10#10) |

## Gaps

**None.** Soft notes non-blocking. Soft HOLD handshake Soft HOLD SoR not invented. Soft HOLD points-review Soft HOLD SoR not invented. Soft HOLD provision not unlocked.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Spec QA may lift Soft HOLD Security gate on this Spec Soft HOLD SoR. Soft HOLD Spec Step 2 formal CLOSE until Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX PASS — do **not** invent points-review Soft HOLD SoR / claim Step 2 CLOSED / unlock provision / invent Stories / invent Steps 3–5 Spec/AC / reopen Gate #27 / Soft #41 from this file. Soft HOLD multi-provider Soft HOLD stands. Soft #41 CLOSED via **#66+#67**. Gate **#27** CLOSED stay closed. Soft HOLD SoR checklist CLEAR PR **#143** @ `9654aff` ≠ handshake Soft HOLD SoR. Sibling SA Soft HOLD SoR **#145** @ `d3e8bb9` cite-only. Cost/critical: none from this design Spec. PoC **$0**. Tip Soft HOLD SoR `adbb310` / delivery `d3e8bb9`.
