# Spec QA — Spec Step 2 AWS cloud architecture design (ECS Express Mode) #142 — Spec gate

**QA:** Dealoware Spec QA  
**Date:** 2026-10-02  
**Verdict:** **PASS (Spec gate)**  
**Spec:** `specs/2026-10-02__spec__spec__aws-cloud-architecture-design-ecs-express.md`  
**Issue:** https://github.com/ioaikh/dealoware/issues/142 · `status:in-dev`  
**DOC-FLOW:** `verification/2026-10-02__spec__verification__aws-cloud-architecture-design-ecs-express.md`  
**Security checklist (KB):** `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md`  
**Security QA confirm (KB):** `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-qa-confirm.md` — **PASS** 10/10 (Chief Security PASS; Senior PASS aligns)  
**SoR checklist twin:** `docs/verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md` — Soft HOLD SoR CLEAR PR **#143** @ `9654aff` / tip Soft HOLD SoR **`adbb310`**  
**CA PASS Option A:** `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md`  
**Arch QA PASS:** `verification/2026-10-02__sa__verification__mvp-sa-step-2-aws-arch-design-review.md`  
**DevOps assist ALIGN:** `ops/2026-10-02__devops__ops__spec-142-ecs-express-assist.md`  
**Host lock:** `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md`  
**Sibling SA Security Soft HOLD SoR:** PR **#145** @ tip delivery **`d3e8bb9`** (cite-only)  
**Tips:** Soft HOLD SoR **`adbb310`** / delivery **`d3e8bb9`**  
**Constraints:** Confirm to **Chief Spec only**. Soft HOLD formal CLOSE (triad) **pending** Soft HOLD SoR Spec qa-confirm CLEAR + Docs QA INDEX PASS — do **not** claim formal triad CLOSE from this file. Soft HOLD provision/spend · invent Stories · multi-provider · Steps 3–5 · Gate **#27 CLOSED** · Soft **#41 CLOSED** via **#66+#67** · App Runner OUT · PoC **$0**. Markdown Spec only — Spec QA did **not** edit the Spec. Handshake Soft HOLD SoR = **qa-confirm only** — do **not** invent points-review Soft HOLD SoR.

## DOC-FLOW

| Artifact | Path | Result |
|----------|------|--------|
| Spec | `specs/2026-10-02__spec__spec__aws-cloud-architecture-design-ecs-express.md` | **PASS** — DOC-FLOW header present; ~27KB; naming `YYYY-MM-DD__spec__spec__{slug}.md` |
| This evidence | `verification/2026-10-02__spec__verification__aws-cloud-architecture-design-ecs-express.md` | **PASS** — filed |
| Spec Security checklist | `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md` | **PASS** — binding 1–10; Soft HOLD SoR twin PR #143 @ `9654aff` CLEAR |
| Spec Security qa-confirm (KB) | `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-qa-confirm.md` | **PASS** — 10/10 MET; Security gate LIFTED by Chief Spec CLEAR |
| Soft HOLD SoR Spec qa-confirm (handshake) | Soft HOLD SoR `docs/verification/…mvp-spec-step-2-aws-arch-design-qa-confirm.md` | **Soft HOLD** — not yet MERGED as handshake Soft HOLD SoR (formal CLOSE pending) |
| CA Option A | `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md` | **PASS** — grounded |
| Arch QA | `verification/2026-10-02__sa__verification__mvp-sa-step-2-aws-arch-design-review.md` | **PASS** — cite |
| DevOps lock + assist | `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md` + `ops/2026-10-02__devops__ops__spec-142-ecs-express-assist.md` | **PASS** — ALIGN |

## Issue #142 design-SoR AC vs Spec §11 map

| #142 In-scope / Soft HOLD | Spec lock / section | Result |
|---------------------------|---------------------|--------|
| Services map | Option A ECS Express Mode single API + ALB/TLS + ECR + managed Postgres Soft HOLD + Secrets Manager + CloudWatch — §2 | **PASS** |
| Tenancy / isolation | Option A §3b dual wall; app + IAM task role; Soft HOLD multi-account — §3 | **PASS** |
| Data store shape | SQLite local → PostgreSQL-when-AWS; same EF model; fail-closed FieldPolicy — §4 | **PASS** |
| Secrets / PII boundary | Secrets Manager Soft HOLD; LoginEmail never agent; Accept-gated ShareOutbound; Soft HOLD Cognito/vault — §5 | **PASS** |
| Observability | CloudWatch + OTel Soft weave; Soft HOLD paid / PII leak / 5th Story — §6 | **PASS** |
| Scale-out from local/$0 | Phased Soft HOLD 0→4; Soft HOLD multi-provider; Soft HOLD Steps 3–5 invent — §7 | **PASS** |
| KPIs cost · security · performance | Mandatory KPI table §8; design ≠ SLA; cost/perf must not regress walls | **PASS** |
| Soft HOLD AWS provision / spend | Locked #0/#9; §9; PoC $0; escalate COO→CEO | **PASS** |
| App Runner OUT | Locked #1; §2; §9 OUT | **PASS** |
| Soft HOLD multi-provider | Locked #7; §7; §9 | **PASS** |
| Soft HOLD invent Stories / invent AC beyond design | Locked #10; §9 | **PASS** |
| Soft HOLD Steps 3–5 Spec | Locked #7; §9 | **PASS** |
| Gate #27 CLOSED | Locked #8; §9 OUT | **PASS** |
| Soft #41 CLOSED via #66+#67 | Locked #8; §7; §9 | **PASS** |
| MotorMarket / DC4 OUT | §9 OUT | **PASS** |
| PoC $0 provision | Locked #9; §1; §9 | **PASS** |

**All Issue #142 design-SoR In-scope + Soft HOLD/OUT bullets mapped.** No invent Stories / provision AC.

## CA Option A grounding (§§2–8 mirror)

| Spec section | CA Option A cite | Result |
|--------------|------------------|--------|
| §2 Services map | CA §3.1 services (ECS Express Mode + ALB/TLS + ECR + managed Postgres Soft HOLD + Secrets Manager + CloudWatch; App Runner / EKS / Lambda / early mesh Reject) | **PASS** |
| §3 Tenancy | CA §3.2 dual wall Option A §3b; app + IAM; Soft HOLD multi-account | **PASS** |
| §4 Data store | CA §3.3 SQLite→PostgreSQL; no CQRS; FieldPolicy integrity | **PASS** |
| §5 Secrets/PII | CA §3.4 LoginEmail / Accept-gate / Soft HOLD vault Cognito | **PASS** |
| §6 Observability | CA §3.5 CloudWatch + OTel Soft weave; Soft HOLD paid / 5th Story | **PASS** |
| §7 Scale-out | CA §3.6 phased Soft HOLD 0→4; Soft HOLD multi-provider / Steps 3–5 | **PASS** |
| §8 KPI triad | CA KPI table pattern; design ≠ SLA; no security regression via cost/perf | **PASS** |
| Locked #1/#2 Option A pick | CA §2 Option A Recommended | **PASS** |

DevOps assist ALIGN (compute / tenancy / data / secrets / obs / KPI Soft HOLDs) held. Host lock ECS Express Mode; App Runner OUT held.

## Sec10 weave vs Spec Security checklist 1–10

| # | Checklist point | Spec bind | Result |
|---|-----------------|-----------|--------|
| 1 | Design-only Soft HOLD provision / Soft HOLD spend | Locked #0/#9; §1; §9; §10#1 | **PASS** (Security SoR PASS) |
| 2 | Host ECS Express Mode; App Runner OUT | Locked #1/#2; §2; §9 OUT; §10#2 | **PASS** |
| 3 | Tenancy inherits Option A dual-wall | Locked #3; §3; §10#3 | **PASS** |
| 4 | Secrets/PII Soft HOLD invent mature vault | Locked #5; §5; §9 OUT; §10#4 | **PASS** |
| 5 | Store shape fail-closed under FieldPolicy | Locked #4; §4; §10#5 | **PASS** |
| 6 | Observability Soft HOLD paid / Soft HOLD PII leak | Locked #6; §6; §8; §10#6 | **PASS** |
| 7 | Security KPI real (not slogan) | Locked #3/#9; §8; §10#7 | **PASS** |
| 8 | Scale-out Soft HOLD multi-provider / Soft HOLD Steps 3–5 invent | Locked #7/#8/#10; §7; §9; §10#8 | **PASS** |
| 9 | OUT / Soft HOLD locked pack | Locked #0/#1/#8/#9/#10; §9; §10#9 | **PASS** |
| 10 | Traceability + handshake Soft HOLD SoR pattern | Sources; §10; tip `adbb310`; §10#10 | **PASS** |

Security QA Spec-step confirm **PASS 10/10** (`…mvp-spec-step-2-aws-arch-design-qa-confirm.md`). Spec §10 maps 1–10 with section cites — weave intact. Chief Spec CLEAR PASS — Spec Security gate LIFTED.

## Soft HOLDs / locks (confirm held)

| Constraint | Result | Evidence |
|------------|--------|----------|
| Soft HOLD provision / Soft HOLD spend | **PASS** | Locked #0/#9; §1; §9 Soft HOLD; PoC $0 |
| Soft HOLD invent Stories / invent AC beyond design SoR | **PASS** | Locked #10; §9 Soft HOLD; §11 no invent |
| Soft HOLD multi-provider | **PASS** | Locked #7; §7; §9 |
| Soft HOLD Steps 3–5 Spec/AC invent | **PASS** | Locked #7; §7; §9 |
| Gate #27 CLOSED (do not reopen) | **PASS** | Locked #8; §3; §9 OUT |
| Soft #41 CLOSED via #66+#67 (not host gap) | **PASS** | Locked #8; §7; §9 |
| App Runner OUT | **PASS** | Locked #1; §2 Reject; §9 OUT |
| PoC $0 | **PASS** | Locked #9; header; §1; §9 |
| MotorMarket / DC4 OUT | **PASS** | §9 OUT |
| No production SLA / hosted-at-scale claims | **PASS** | §1; §7; §8 explicit note |
| Spec unmodified by Spec QA | **PASS** | Evidence-only write |
| Handshake Soft HOLD SoR = qa-confirm only (no invent points-review Soft HOLD SoR) | **PASS** | §10#10; Constraints |

## Spec QA Done-list (content when cleared)

| Item | Result |
|------|--------|
| Design-SoR covers services / tenancy / store / secrets / obs / scale-out (#142 In-scope) | **PASS** |
| KPI table present (cost · security · performance) with design ≠ SLA note | **PASS** |
| Host ECS Express Mode locked; App Runner OUT | **PASS** |
| Explicit IN / OUT / Soft HOLD match #142 + Spec Security point 9 | **PASS** |
| §10 Security Spec checklist binding 1–10 with Spec section cites | **PASS** |
| No invent Stories / invent AC beyond design / invent Steps 3–5 Spec / provision AC | **PASS** |
| Tip `adbb310` + CA PASS + Arch QA + Security QA SA cites present | **PASS** |
| Gate #27 CLOSED / Soft #41 CLOSED via #66+#67 held; PoC $0; MotorMarket/DC4 OUT | **PASS** |
| Spec-step Security QA PASS + Chief Spec CLEAR after Security | **PASS** (gate LIFTED) |
| Soft HOLD SoR Spec qa-confirm MERGED (handshake) | **Soft HOLD** — formal CLOSE pending (separate) |

## Soft notes (non-blocking)

- Spec header Status still says Soft HOLD until Security QA + Soft HOLD SoR qa-confirm MERGED — content is Security-cleared; Spec QA Spec gate **PASS** under Chief Spec CLEAR. Soft HOLD formal CLOSE remains until Soft HOLD SoR Spec qa-confirm CLEAR + Docs QA INDEX PASS.
- Spec Soft HOLD SoR tip prefers **`adbb310`** over stale `4a97984` (Gate #27 CLOSED lineage) — accepted.
- Soft HOLD SoR checklist CLEAR PR **#143** @ `9654aff` ≠ handshake Soft HOLD SoR qa-confirm — do not invent.
- Sibling SA Soft HOLD SoR **#145** @ `d3e8bb9` cite-only — not re-scored.
- Managed DB names RDS PostgreSQL **or** Aurora Serverless v2 as design Soft HOLD provision — not a buy claim.
- Cognito/SSO / mature vault / KMS Soft HOLD later maturity — not Step 2 delivered.

## Gaps

**None** for Spec gate PASS.

## Handshake status

1. Spec-step Security QA **PASS** 10/10 (KB qa-confirm; Chief Security PASS; Senior PASS aligns). Soft HOLD SoR checklist twin PR **#143** @ `9654aff` / tip Soft HOLD SoR **`adbb310`** CLEAR.  
2. Spec QA Spec-side + Spec gate **PASS** (this artifact) — CA Option A grounded; #142 design-SoR AC mapped; locks held; §10 weave intact.  
3. Confirm to **Chief Spec only**.  
4. **Soft HOLD formal CLOSE** (triad) until Soft HOLD SoR Spec qa-confirm CLEAR + Docs QA INDEX PASS — do **not** claim Step 2 CLOSED / unlock provision / invent Stories / invent Steps 3–5 Spec/AC / reopen Gate #27 or Soft #41 from this file. Soft HOLD multi-provider Soft HOLD stands. PoC **$0**. Tips Soft HOLD SoR `adbb310` / delivery `d3e8bb9`. Quiet.
