# Security QA — Spec #142 / Product Step 2 AWS cloud architecture design vs Chief Security SA checklist

**Author:** Dealoware Security QA  
**Date:** 2026-10-02  
**Verdict:** **PASS** (all 10 points MET)  
**Asked by:** Dealoware Architecture QA (interim Architecture content PASS; Soft HOLD full Arch QA PASS until this confirm)  
**Chief checklist (binding):** `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-checklist.md` (10 points)  
**Senior Security done-list:** *not present* — no `verification/*mvp-sa-step-2-aws-arch-design*points*` file found; Security QA scored checklist directly against architecture § Security + supporting §§ (Gate #25 pattern; handshake Soft HOLD SoR = **qa-confirm only** — **do not invent points-review Soft HOLD SoR**)  
**Architecture:** `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md` (§8 Security answers 1–10 MET)  
**SA verification (interim):** `verification/2026-10-02__sa__verification__mvp-sa-step-2-aws-arch-design-review.md` (HOLD — Architecture content PASS; Soft HOLD final on Security QA)  
**Issue:** https://github.com/ioaikh/dealoware/issues/142 · Spec Step 2 — AWS cloud architecture design (design-only Soft HOLD provision)  
**Grounding:** Product Step 2 · feasibility AWS host · DevOps ECS Express lock · Option A §3b · Gate **#27** CLOSED tip `4a97984` · DevOps assist ALIGN  
**DOC-FLOW:** `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-qa-confirm.md`  
**Tip context:** `4a97984`  
**Constraints:** Soft HOLD Architecture QA PASS until this confirm (lifted by PASS). Soft HOLD **AWS account / resource provision / spend**. Soft HOLD invent product Stories / invent Spec/AC Steps 3–5. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft **#41** CLOSED via **#66+#67** (do not re-open). App Runner excluded. No MotorMarket/Cognito invent as delivered / DC4 / mature vault as delivered. Gate **#27** CLOSED stay closed. Handshake Soft HOLD SoR = **qa-confirm only** — **do not invent points-review Soft HOLD SoR**. Soft HOLD score until checklist Soft HOLD SoR CLEAR (Docs PR **#143** OPEN — Soft note only; Gate #25 pattern scored LIVE Architecture QA ask before Soft CLOSE twin). PoC **$0**. Architecture QA does **not** unlock provision or claim Step 2 Spec CLEAR.

## Soft notes accepted (non-blocking)

| Soft note | Disposition |
|-----------|-------------|
| Soft HOLD provision / Soft HOLD spend | **Accepted** — design-only; any later provision → CA → CPM → COO → CEO |
| Checklist Soft HOLD SoR twin PR **#143** OPEN (raw on `main` 404) | **Accepted** — Soft HOLD score Soft note; does **not** Soft HOLD this LIVE Architecture QA ask (Gate #25 pattern). Soft HOLD SoR twin ≠ handshake Soft HOLD SoR |
| Handshake Soft HOLD SoR = **qa-confirm only** (no points-review twin) | **Accepted** — do **not** invent points-review Soft HOLD SoR |
| Senior points-review absent | **Accepted** — content OK later; handshake Soft HOLD SoR remains qa-confirm only |
| Soft HOLD multi-provider Spec/doc rewrite | **Accepted** — Soft HOLD stands; not claimed delivered |
| Soft HOLD invent Stories / invent Steps 3–5 Spec/AC | **Accepted** — Soft HOLD stands |
| Soft **#41** CLOSED via **#66+#67** | **Accepted** — not reopened as host gap |
| Gate **#27** CLOSED tip `4a97984` | **Accepted** — stay closed; do not reopen |
| Cognito/SSO / mature vault / KMS = Soft HOLD later maturity | **Accepted** — not Step 2 delivered |
| Soft OTel/audit = Soft weave only (no 5th Story) | **Accepted** |
| App Runner excluded; ECS Express sketch `open` | **Accepted** — §6 currency 2026-10-02 |
| Sibling Spec Security checklist remains a separate Soft HOLD SoR / Spec QA track | **Accepted** — not scored here |

## Sources checked

| Source | Path / ref | Result |
|--------|------------|--------|
| Chief Security SA checklist | `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-checklist.md` | Binding 10 points |
| Step 2 AWS architecture (§8 + supporting) | `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md` | Security answers 1–10 **MET** with section cites |
| Architecture QA interim | `verification/2026-10-02__sa__verification__mvp-sa-step-2-aws-arch-design-review.md` | Architecture content PASS; HOLD final on Security QA |
| Senior Security points-review | *(absent)* | No bounce — QA independent score only |
| SoR checklist twin | GitHub PR **#143** OPEN; raw on `main` 404 | Soft HOLD SoR twin **not CLEAR** — Soft note only |
| Gate #27 CLOSED | tip `4a97984` cited in architecture | Stay closed |
| Soft #41 | CLOSED via #66+#67 cited | Held |

## Independent re-score (Security QA)

Score vs **official Step 2 AWS architecture checklist 1–10** only. Surfaces: architecture §8 answers + supporting §§ + Arch QA interim HOLD. Design-only — do **not** treat as provision authorization.

| # | Point | Architecture §8 | Security QA | Evidence |
|---|-------|-----------------|-------------|----------|
| 1 | Design-only Soft HOLD provision / Soft HOLD spend | MET | **MET** | §8#1 + §1 + §5: no AWS account create, no cluster/service/resource provision, no paid plugins, no non-local deploy claimed delivered. Later provision/spend → **CA → CPM → COO → CEO**. PoC **$0**. Cite Product Step 2 OUTs + DevOps ECS Express lock. |
| 2 | Host ECS Express Mode; App Runner OUT | MET | **MET** | §8#2 + §3.1 + §6: compute locked to **Amazon ECS Express Mode (Fargate)** status `open` (currency **2026-10-02**). App Runner excluded (`existing-customers-only` + `no-new-features`). Soft HOLD invent alternate greenfield host. Options reject C. |
| 3 | Tenancy inherits Option A dual-wall | MET | **MET** | §8#3 + §3.2: binds Option A **§3b** Platform API/DB FieldPolicy wall **and** agent/tool hard wall via same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses. Prompt-only sole control **rejected**; parallel agent ACL tables **rejected**; Soft HOLD tenant shortcuts that weaken fail-closed list/discovery scrub. Cite Option A tip + Gate #27 CLOSED / Stage C #67. |
| 4 | Secrets / PII Soft HOLD invent mature vault | MET | **MET** | §8#4 + §3.4: identity-until-accept; no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side. Mature vault / KMS / Cognito/SSO/IdP = Soft HOLD later maturity — **not** Step 2 delivered. Soft HOLD invent Cognito as Step-2-delivered. |
| 5 | Data store shape fail-closed under FieldPolicy | MET | **MET** | §8#5 + §3.3: local SQLite → designed PostgreSQL-when-AWS-shape keeps Field ACL / list fail-closed / discovery scrub / StrategyBody ACL. Soft HOLD invent multi-tenant shared-DB-without-wall or cross-tenant reads. Soft HOLD invent Stories that Field-capture store migration as in-scope delivery here. |
| 6 | Observability Soft HOLD paid / Soft HOLD PII leak | MET | **MET** | §8#6 + §3.5: CloudWatch default + OTel Soft weave for KPI triad. Soft HOLD invent **paid** observability beyond free tiers without COO→CEO. Soft HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces. Soft HOLD invent OTel/audit as a **5th Story**. |
| 7 | Security KPI is real (not slogan) | MET | **MET** | §8#7 + §4 KPI table: security KPI measurable against dual-wall hold, fail-closed budget/cutoff where metered (#68), Accept-gated ShareOutbound, and Soft HOLD provision. Cost/perf KPIs **must not** authorize drop wall #2, share secrets across tenants, or App Runner greenfield. Cite Product Step 2 KPI triad. |
| 8 | Scale-out Soft HOLD multi-provider / Soft HOLD Steps 3–5 invent | MET | **MET** | §8#8 + §3.6 + §5: scale-out Soft HOLDs multi-provider Spec/doc rewrite until BM multi-provider start; Soft HOLDs invent Spec/AC Steps **3–5**; Soft HOLDs invent product Stories beyond this design. Soft **#41 CLOSED** via **#66+#67** — not reopened as host gap. |
| 9 | OUT / Soft HOLD locked pack | MET | **MET** | §8#9 + §5 + header: AWS provision/spend Soft HOLD; App Runner; Soft HOLD multi-provider rewrite; invent Stories Soft HOLD; invent Steps 3–5 Spec/AC Soft HOLD; Gate **#27** CLOSED stay closed; MotorMarket/DC4 OUT; Cognito/SSO/mature vault as delivered OUT; MCP marketplace OUT; PoC **$0** provision. Cost/critical → COO → CEO. |
| 10 | Traceability + handshake Soft HOLD SoR pattern | MET | **MET** | §8#10: cites Product Step 2 + SA feasibility AWS lock + DevOps ECS Express lock + DevOps assist ALIGN + Option A §3b + Gate #27 CLOSED tip `4a97984` + Soft #41 CLOSED via #66+#67. Architecture QA Soft HOLD until this confirm. Handshake Soft HOLD SoR = **qa-confirm only** — **do not invent points-review Soft HOLD SoR**. Soft HOLD Spec invent of provision Stories until CA design grounding PASS path. Soft HOLD checklist Soft HOLD SoR CLEAR (PR #143 OPEN) is Soft note — ≠ handshake Soft HOLD SoR. |

## Alignment with Senior Security done-list

Senior points-review **absent** (expected: handshake Soft HOLD SoR = qa-confirm only). Independent Security QA re-score vs checklist + architecture §8: **all 10 MET**. Soft notes **accepted**. No bounce. No gaps.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Design-only; Soft HOLD provision / Soft HOLD spend; PoC $0 | Held (§8#1) |
| ECS Express Mode `open`; App Runner OUT | Held (§8#2) |
| Option A §3b dual wall inherited; prompt-only sole control rejected | Held (§8#3) |
| LoginEmail never in agent context; ShareOutbound Accept-gated; Soft HOLD mature vault/Cognito as delivered | Held (§8#4) |
| Store path fail-closed under FieldPolicy; Soft HOLD invent migration Stories | Held (§8#5) |
| Observability Soft HOLD paid / Soft HOLD PII leak; no 5th Story | Held (§8#6) |
| Security KPI real; cost/perf must not regress walls | Held (§8#7) |
| Soft HOLD multi-provider; Soft HOLD Steps 3–5 invent; Soft #41 CLOSED | Held (§8#8) |
| OUT pack + Gate #27 CLOSED stay closed | Held (§8#9) |
| Handshake Soft HOLD SoR = qa-confirm only; points-review Soft HOLD SoR not invented | Held (§8#10) |

## Gaps

**None.** Soft notes non-blocking. Soft HOLD handshake Soft HOLD SoR not invented. Soft HOLD points-review Soft HOLD SoR not invented. Soft HOLD provision not unlocked.

## Handshake status

Security QA → **PASS** confirm to Chief Security. Architecture QA may lift Security HOLD and PASS to Chief Architect on the Security gate for this design (confirm to **Chief Architect only**). Soft HOLD provision / Soft HOLD spend Soft HOLD stands. Soft HOLD invent Stories / invent Steps 3–5 Spec/AC Soft HOLD stands. Soft HOLD multi-provider Soft HOLD stands. Soft #41 CLOSED via **#66+#67** — do not re-open. Gate **#27** CLOSED stay closed. Soft HOLD checklist Soft HOLD SoR CLEAR until Docs PR **#143** merges — Soft HOLD SoR twin ≠ handshake Soft HOLD SoR. Handshake Soft HOLD SoR = **qa-confirm only**. Do **not** unlock provision / claim Step 2 Spec CLEAR / transition GitHub issue states from this file. Cost/critical: none from this design. PoC **$0**. Tip context `4a97984`.
