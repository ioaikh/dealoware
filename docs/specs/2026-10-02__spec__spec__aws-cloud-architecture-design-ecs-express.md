# Spec — Spec Step 2: AWS cloud architecture design (ECS Express Mode) — #142

**Status:** Senior Spec — Security-bound design-SoR; Spec QA Soft HOLD until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED  
**Date:** 2026-10-02  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec CLEAR WRITE #142 (CA PASS unlocked; Spec Security checklist ISSUED)  
**Issue:** https://github.com/ioaikh/dealoware/issues/142 · `status:in-dev` · Spec Step 2 — AWS cloud architecture design (design-only Soft HOLD provision)  
**DOC-FLOW:** `specs/2026-10-02__spec__spec__aws-cloud-architecture-design-ecs-express.md`  
**Constraints:** **Design-SoR Spec only.** Soft HOLD AWS account/resource provision · Soft HOLD spend · Soft HOLD invent Stories · Soft HOLD invent AC beyond design SoR · Soft HOLD multi-provider · Soft HOLD Steps 3–5 Spec · **App Runner OUT** · Gate **#27 CLOSED** (do not reopen) · Soft **#41 CLOSED** via **#66+#67** (do not reopen as host gap) · PoC **$0** · MotorMarket/DC4 OUT · no production SLA / hosted-at-scale claims · any provision/spend → **COO → CEO** (and CA/CPM path as locked). Tip **`adbb310`** (prefer over stale `4a97984`). Spec QA Soft HOLD until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED. Handshake Soft HOLD SoR = **qa-confirm only** — do **not** invent points-review Soft HOLD SoR.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #142 In-scope + Soft HOLD/OUT | https://github.com/ioaikh/dealoware/issues/142 | Binding design-SoR acceptance framing |
| Product Step 2 | `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 2 | KPI triad cost efficiency · security · performance; design-only; OUTs |
| CA PASS Option A | `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md` | Binding architecture design SoR (services / tenancy / store / secrets / obs / scale-out + KPI + currency) |
| Arch QA PASS | `verification/2026-10-02__sa__verification__mvp-sa-step-2-aws-arch-design-review.md` | Architecture QA PASS (Soft HOLD provision) |
| Security QA SA track | `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-qa-confirm.md` | SA-step Security QA PASS (1–10 MET; qa-confirm only) |
| Spec Security checklist | `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md` | Spec-step points **1–10** (this Spec must bind) |
| DevOps host lock | `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md` | ECS Express Mode; no spend |
| DevOps assist ALIGN | `ops/2026-10-02__devops__ops__spec-142-ecs-express-assist.md` | Design-only infra guidance (compute / tenancy / data / secrets / obs / KPI Soft HOLDs) |
| Option A secrets/ACL (§3b) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall cite — **do not rewrite** |
| Feasibility AWS host (O10) | `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` | ECS Express sketch; App Runner OUT; Postgres-when-AWS |
| Tip | **`adbb310`** | Soft HOLD SoR checklist CLEAR PRs context (prefer over stale `4a97984`) |

**Product alignment:** Product Step 2 success = written AWS architecture design Spec/SoR with KPI table + OUT list; SA/DevOps aligned; still **$0 provision**. Conflicts → escalate PM → Product → CEO. Cost/critical → COO → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | Design-SoR only | This Spec is **architecture design Spec/SoR only** — no AWS account create, no cluster/service/resource provision, no paid plugins, no non-local deploy claimed delivered |
| 1 | Host | **Amazon ECS Express Mode (Fargate)** sole greenfield compute host; status `open` (currency 2026-10-02); **App Runner OUT** |
| 2 | Option A pick | Single modular-monolith API on ECS Express Mode + managed Postgres (design Soft HOLD provision) + Secrets Manager / task role + CloudWatch — cite CA PASS §2/§3 |
| 3 | Tenancy | Single-tenant platform DB + Participant-scoped authz via Option A **§3b** dual wall (API/DB FieldPolicy **and** agent/tool hard wall; same `IFieldPolicy.Evaluate` for **all** FieldClasses). Isolation at app + IAM task role. Soft HOLD multi-account invent |
| 4 | Data store | EF Core + SQLite (local/$0) → EF Core + **PostgreSQL** when AWS shape (same domain model); no CQRS invent; Field ACL / list fail-closed / discovery scrub / StrategyBody ACL integrity must hold |
| 5 | Secrets/PII | Secrets Manager + task-role injection Soft HOLD buy; secrets **not** in image; LoginEmail never agent/model; ContactEmail ShareOutbound Accept-gated; Soft HOLD Cognito/SSO/mature vault/KMS as Step-2-delivered |
| 6 | Observability | CloudWatch default + OTel Soft weave; Soft HOLD paid APM/backends without COO→CEO; Soft HOLD PII/denied FieldClasses in logs/metrics/traces; Soft HOLD invent 5th Story |
| 7 | Scale-out | Phased 0→4 Soft HOLD (local $0 → design → CEO spend unlock → CI/ECR → task scale); Soft HOLD multi-provider; Soft HOLD Steps 3–5 Spec/AC invent; Soft HOLD live capacity / SLA claims |
| 8 | Soft #41 / Gate #27 | Soft **#41 CLOSED** via **#66+#67** — not a host gap; Gate **#27 CLOSED** — do not reopen |
| 9 | Spend path | Any later provision/spend → escalate **COO → CEO** (and CA → CPM path as locked); PoC **$0** for provision |
| 10 | Scope label | Documented as Spec Step 2 **design-SoR**; Soft HOLD invent Stories / invent AC beyond design SoR |

---

## 1. Purpose

Turn the local/$0 runnable posture plus the locked **Amazon ECS Express Mode (Fargate)** sketch into a **real AWS architecture design Spec/SoR** for Issue #142 / Product Step 2: services map, tenancy/isolation, data store shape, secrets/PII boundary, observability, and scale-out path from local/$0, with mandatory KPIs **cost efficiency · security · performance**.

This Spec is **design-only**. Soft HOLD provision. Soft HOLD spend. Soft HOLD invent Stories. Soft HOLD invent AC beyond design SoR. Soft HOLD Spec work for Steps 3–5. Soft HOLD multi-provider rewrite. Do **not** claim production SLAs or hosted-at-scale capacity.

---

## 2. Services map (Option A)

| Layer | Design choice | Soft HOLD notes |
|-------|---------------|-----------------|
| Compute | **Amazon ECS Express Mode (Fargate)** — single service running containerized `Dealoware.Api` (.NET modular monolith) | Express Mode = container image + task execution role + infrastructure role; orchestrates Fargate service, ALB+TLS, autoscaling, monitoring, networking. Soft HOLD live capacity / task count claims. Soft HOLD provision. |
| Load balancing / TLS | ALB as Express Mode provisions | Soft HOLD invent second public entry or custom mesh |
| Image registry | Amazon ECR | CI push Soft HOLD until spend unlock + pipeline unlock |
| Data | Managed **PostgreSQL** (RDS PostgreSQL **or** Aurora Serverless v2 as design choice Soft HOLD provision) | Soft HOLD buy; Soft HOLD invent capacity SLAs; same EF Core model as local SQLite |
| Secrets | AWS Secrets Manager + task-role injection to env/config | Secrets **not** in image; Soft HOLD buy until spend unlock |
| IAM | Express Mode exec role + infra role + least-privilege task role | Soft HOLD deploy; private egress defaults as design intent |
| Object storage | Optional S3 for future artifacts | Soft HOLD; Soft HOLD invent Artifact storage Stories here |
| Identity / SSO | OIDC-shaped JWT already on MVP Participant auth path stays | **Cognito / SSO Soft HOLD** until separate unlock — Soft HOLD invent Cognito as Step-2-delivered |
| Observability | CloudWatch logs/metrics as Express Mode default; OTel Soft weave; optional X-Ray as design Soft HOLD | Soft HOLD paid APM / paid backends without COO→CEO |

**Reject / OUT (cite CA PASS §2):** App Runner greenfield (**OUT**); EKS / self-managed K8s as Step 2 target (**Reject**); Lambda-only rewrite (**Reject**); early multi-service mesh invent (**Soft HOLD defer** until separately unlocked load justifies).

---

## 3. Tenancy / isolation

- **Single-tenant platform DB** with **Participant-scoped authz** via Option A dual wall (API/DB FieldPolicy + agent/tool hard wall; same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses). Cite Option A §3a/§3b — **do not rewrite**.
- Isolation boundary at **app authz + IAM task role** (DevOps assist). Soft HOLD invent multi-account / physical per-tenant isolation as delivered.
- Soft HOLD invent multi-tenant shared-DB-without-wall or cross-tenant read paths.
- VPC / private data plane Soft HOLD until provision unlock; **fail-closed defaults** as design intent.
- Reject prompt-only / soft-guidance-as-sole-control; reject parallel agent ACL tables that drift from FieldPolicy (Gate #27 CLOSED / Stage C #67).

---

## 4. Data store shape

| Today (local/$0) | AWS design shape | Rules |
|------------------|------------------|-------|
| EF Core + **SQLite** | EF Core + **PostgreSQL** (same domain model) | One relational model; **no CQRS**; Soft HOLD invent event-sourcing |
| Local migrations | Same EF migrations path → Postgres | Soft HOLD invent Stories that Field-capture store migration as in-scope delivery **in this Step** |
| Backups | Design intent: automated backups / PITR as Soft HOLD provision choice | Soft HOLD buy; Soft HOLD invent capacity SLAs |

Field ACL / list fail-closed / discovery scrub / StrategyBody ACL integrity **must** hold on the AWS store shape (Stages A/B/C delivered walls — Gate #27 CLOSED).

---

## 5. Secrets / PII boundary

- **Cite Option A §3a/§3b dual wall** (do not rewrite): API/DB authorize via FieldPolicy + resource scope; agent/tool hard wall with allowlist + server-side scrub; dual wall for all FieldClasses.
- **LoginEmail** never to agent / model context (User-only).
- **ContactEmail** OwnAgent Read OK; **ShareOutbound only after Accept** (server-side grant).
- Identity-until-accept: no contact/PII on public Participant DTOs.
- Runtime secrets (DB connection, signing keys, LLM API keys if later unlocked) live in **Secrets Manager** / env from **task role** — not baked into image.
- Mature PII vault / KMS / Cognito Soft HOLD later maturity — **not** Step 2 delivered. Soft HOLD invent vault as Product/Security SoR beyond Option A cite.
- Soft HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces (design rule).

---

## 6. Observability

- CloudWatch logs/metrics as Express Mode default path.
- Design for **structured logs + request ids**; reuse MVP Soft weave touchpoints (OTel/audit/idempotent-offers hooks already Soft-woven — Soft HOLD invent OTel/audit as a **5th Story**).
- Soft HOLD paid observability / APM / X-Ray backends beyond free tiers without COO→CEO.
- Soft HOLD invent new product Stories for observability surfaces in this design.

---

## 7. Scale-out path from local/$0

| Phase | What | Soft HOLD |
|-------|------|-----------|
| **0** | Local runnable · PoC **$0** | Current valid posture |
| **1** | **This design Spec/SoR** (SA-REV-AWS-DESIGN → Spec #142) | Soft HOLD next until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED + Spec QA PASS |
| **2** | CEO spend unlock → account + min Express Mode service + managed Postgres Soft HOLD | Soft HOLD until COO→CEO; Soft HOLD invent as open now |
| **3** | CI push ECR → Express Mode deploy | Soft HOLD until spend + pipeline unlock |
| **4** | Scale tasks / shared ALB as Express Mode autoscaling allows | Soft HOLD live capacity / SLA claims; Fargate sizing classes = design knobs only |
| Later Soft HOLD | Multi-AZ / read replicas / Soft HOLD multi-provider rewrite | Soft HOLD until separately named unlock; Soft HOLD invent Spec/AC Steps 3–5 |

Do **not** claim provisioned capacity or production SLAs. Soft HOLD multi-provider until BM multi-provider start. Soft **#41 CLOSED** via **#66+#67** — not a host gap.

---

## 8. Mandatory KPI table

**Explicit:** Design KPIs ≠ live SLAs / provisioned capacity claims. Cost/perf KPIs must **not** authorize security regressions (drop wall #2, share secrets across tenants, App Runner greenfield).

| Design choice | Cost efficiency | Security | Performance |
|---------------|-----------------|----------|-------------|
| **Host — ECS Express Mode (Fargate), single service** | Pay only underlying resources; no App Runner dead-end; Soft HOLD buy until spend OK; tagging / Budgets Soft HOLD buy (DevOps Cost KPI) | `open` greenfield; currency-checked; least-privilege task roles Soft HOLD deploy; App Runner OUT | Fargate sizing classes as design knobs Soft HOLD live capacity; Express Mode autoscaling Soft HOLD claim |
| **DB — managed PostgreSQL (RDS or Aurora Serverless v2 design option)** | Soft HOLD provision; prefer simplest until spend unlock chooses; Soft HOLD invent capacity | Same FieldPolicy wall on store; Soft HOLD shared-DB-without-wall | Same EF model; Soft HOLD invent CQRS; Soft HOLD SLA |
| **Secrets — Secrets Manager + task role** | Soft HOLD buy; no secrets in image reduces blast/rework cost | Dual wall Option A holds; LoginEmail never agent; Accept-gated ShareOutbound; Soft HOLD invent Cognito/vault as delivered | Role injection Soft HOLD deploy latency invent |
| **Observability — CloudWatch default + OTel Soft weave Soft HOLD paid** | Soft HOLD paid backends without COO→CEO | Soft HOLD PII/denied FieldClasses in logs/metrics/traces; Soft HOLD invent 5th Story | Soft HOLD invent APM as capacity claim |
| **Tenancy — app + IAM task role; Option A dual wall** | No multi-account invent cost | Dual wall (API/DB + agent/tool) same `IFieldPolicy`; reject prompt-only; Gate #27 CLOSED / #67 | Single-service path Soft HOLD invent mesh latency |
| **Scale path — phased Soft HOLD 0→4** | Soft HOLD spend until CEO; LLM hard-stops remain app (#68) Soft HOLD buy | Soft HOLD multi-provider rewrite; Soft HOLD invent Steps 3–5 Spec/AC; Soft HOLD invent Stories; Soft #41 CLOSED held | Soft HOLD multi-AZ / replicas later; Soft HOLD live capacity |

---

## 9. Explicit IN / OUT / Soft HOLD

### IN (this design Spec/SoR)

- Written AWS architecture design Option A (services, tenancy, store, secrets/PII, observability, scale-out)
- KPI table: cost efficiency · security · performance (design only)
- Host lock: Amazon ECS Express Mode (Fargate); App Runner OUT; currency check cited via CA PASS §6 (2026-10-02)
- Binding cites: Product Step 2, CA PASS Option A, Arch QA PASS, Security QA SA qa-confirm, Spec Security checklist, DevOps lock + assist, Option A §3b, tip **`adbb310`**
- Spec Security answers 1–10 MET against ISSUED Spec-step checklist (§10)

### OUT

- AWS account / resource provision / spend / paid plugins / non-local deploy as delivered by this Step
- App Runner greenfield
- MotorMarket / DC4
- Cognito / SSO / mature vault / KMS as **delivered** by Step 2
- MCP marketplace
- Claiming hosted-at-scale / production SLAs / live capacity
- Invent product Stories / invent AC beyond design SoR
- Invent Spec/AC for Steps 3–5
- Reopen Gate **#27** (CLOSED)
- Reopen Soft **#41** as host gap (CLOSED via **#66+#67**)

### Soft HOLD

- Soft HOLD provision / Soft HOLD spend (escalate CA → CPM → COO → CEO)
- Soft HOLD multi-provider SoR rewrite until BM multi-provider start
- Soft HOLD invent Spec/AC for Steps 3–5
- Soft HOLD invent product Stories beyond this design SoR
- Soft HOLD invent AC beyond design SoR
- Soft HOLD SA-REV-AWS-PROVISION until CEO spend unlock (**not** open now)
- Soft HOLD multi-account physical isolation / multi-AZ / read replicas / paid APM
- Soft HOLD Cognito/SSO until separate unlock
- Soft HOLD Spec QA PASS until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED
- Soft HOLD Spec Step 2 formal CLOSE Soft HOLD until checklist Soft HOLD SoR CLEAR + content Chief PASS + Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX PASS (per CPM Soft HOLD SoR rules)

---

## 10. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md`  
**Prior SA Security PASS:** `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-qa-confirm.md` (1–10 MET; qa-confirm only)  
**CA PASS arch:** `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md`  
**Rule:** Spec QA must **not** PASS until Spec-step Security QA confirms these points via Soft HOLD SoR `…mvp-spec-step-2-aws-arch-design-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** — **do not invent points-review Soft HOLD SoR**.

| # | Security point | Spec requirement / answer | Spec section cites |
|---|----------------|---------------------------|--------------------|
| 1 | Design-only Soft HOLD provision / Soft HOLD spend | **MET.** Step 2 is **architecture design Spec/SoR only**: no AWS account create, no cluster/service/resource provision, no paid plugins, no non-local deploy claimed delivered. Any later provision/spend → escalate **COO → CEO** (and CA/CPM path as locked). PoC **$0** for provision. Cite Product Step 2 OUTs + DevOps ECS Express lock. | Locked #0/#9; §1 Purpose; §9 OUT/Soft HOLD; Sources (Product Step 2, DevOps lock) |
| 2 | Host ECS Express Mode; App Runner OUT | **MET.** Compute host locked to **Amazon ECS Express Mode (Fargate)** as design baseline (status `open` per CA PASS currency 2026-10-02). Spec does **not** recommend or Spec App Runner for greenfield (`existing-customers-only` + `no-new-features`). Soft HOLD invent alternate greenfield host. | Locked #1/#2; §2 Services map; §9 OUT; Sources (CA PASS, DevOps lock + assist) |
| 3 | Tenancy inherits Option A dual-wall | **MET.** AWS tenancy/isolation binds CEO-accepted Option A **§3b**: Platform API/DB FieldPolicy wall **and** agent/tool hard wall (same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses). Reject prompt-only / soft-guidance-as-sole-control; reject parallel agent ACL tables that drift; Soft HOLD tenant shortcuts that weaken fail-closed list/discovery scrub. Isolation at app + IAM task role; Soft HOLD multi-account invent. | Locked #3; §3 Tenancy; Sources (Option A §3b, Gate #27 / Stage C #67) |
| 4 | Secrets/PII Soft HOLD invent mature vault | **MET.** Identity-until-accept preserved: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side. Secrets Manager / task role pattern Soft HOLD buy. Mature PII vault / KMS / Cognito/SSO/IdP = Soft HOLD later maturity — **not** Step 2 delivered. Soft HOLD invent Cognito as Step-2-delivered. | Locked #5; §5 Secrets/PII; §9 OUT |
| 5 | Store shape fail-closed under FieldPolicy | **MET.** Local SQLite → designed PostgreSQL-when-AWS-shape path keeps Field ACL / list fail-closed / discovery scrub / StrategyBody ACL integrity. Soft HOLD invent multi-tenant shared-DB-without-wall or cross-tenant read paths. Soft HOLD invent Stories that Field-capture store migration as in-scope delivery **here**. No CQRS invent. | Locked #4; §4 Data store; Sources (feasibility, Gate #27 CLOSED walls) |
| 6 | Observability Soft HOLD paid / Soft HOLD PII leak | **MET.** Observability designed for KPI triad via CloudWatch default + OTel Soft weave; Soft HOLD invent **paid** observability beyond free tiers without COO→CEO; Soft HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces; Soft HOLD invent OTel/audit as a **5th Story**. | Locked #6; §6 Observability; §8 KPI obs row |
| 7 | Security KPI real vs dual-wall / budgets / Accept-gate / Soft HOLD provision | **MET.** Security KPI measurable against dual-wall hold, fail-closed budget/cutoff where metered (#68), Accept-gated ShareOutbound, and Soft HOLD provision. Cost-efficiency / performance KPIs explicitly **must not** authorize security regressions (drop wall #2, share secrets across tenants, App Runner greenfield). | Locked #3/#9; §8 KPI table + explicit note; Sources (Product Step 2 KPI triad) |
| 8 | Scale-out Soft HOLD multi-provider / Soft HOLD Steps 3–5 invent | **MET.** Scale-out path Soft HOLDs multi-provider Spec/doc rewrite until BM multi-provider start; Soft HOLDs invent Spec/AC for Steps **3–5**; Soft HOLDs invent product Stories / invent AC beyond this design SoR. Soft **#41 CLOSED** via **#66+#67** — do not reopen as host gap. | Locked #7/#8/#10; §7 Scale-out; §9 Soft HOLD |
| 9 | OUT / Soft HOLD locked pack | **MET.** OUT/Soft HOLD pack includes: AWS provision/spend Soft HOLD; App Runner; Soft HOLD multi-provider rewrite; invent Stories Soft HOLD; invent Steps 3–5 Spec/AC Soft HOLD; Gate **#27** CLOSED stay closed; MotorMarket/DC4 OUT; Cognito/SSO/mature vault as delivered OUT; MCP marketplace OUT; PoC **$0** provision. Cost/critical → COO → CEO. | Locked #0/#1/#8/#9/#10; §9 IN/OUT/Soft HOLD; header Constraints |
| 10 | Traceability + handshake Soft HOLD SoR pattern | **MET.** Spec cites Product Step 2 + CA PASS Option A + Arch QA PASS + Security QA SA qa-confirm + Spec Security checklist + DevOps ECS Express lock + DevOps assist ALIGN + Option A §3b + Gate #27 CLOSED + Soft #41 CLOSED via #66+#67 + tip **`adbb310`**. Spec QA Soft HOLD until Security QA confirms via Soft HOLD SoR `…mvp-spec-step-2-aws-arch-design-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** (Gate #25/#26 pattern) — **do not invent points-review Soft HOLD SoR**. Soft HOLD Spec Step 2 formal CLOSE Soft HOLD until checklist Soft HOLD SoR CLEAR + content Chief PASS + Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX PASS. Soft HOLD invent Stories / invent AC beyond design SoR. | This §10; Sources; §9 Soft HOLD; header Status/Constraints |

---

## 11. Acceptance mapping — Issue #142 design-SoR AC

**Binding:** Issue #142 In-scope bullets + Soft HOLD/OUT locks. Design only — do **not** invent product Stories or provision AC.

| #142 In-scope / Soft HOLD | Spec lock | Spec section |
|---------------------------|-----------|--------------|
| Services map | Option A services: ECS Express Mode single API + ALB/TLS + ECR + managed Postgres Soft HOLD + Secrets Manager + CloudWatch | §2 |
| Tenancy / isolation | Option A §3b dual wall; app + IAM task role; Soft HOLD multi-account | §3 |
| Data store shape | SQLite local → PostgreSQL-when-AWS; same EF model; fail-closed FieldPolicy | §4 |
| Secrets / PII boundary | Secrets Manager Soft HOLD; Option A LoginEmail / Accept-gated ShareOutbound; Soft HOLD Cognito/vault | §5 |
| Observability | CloudWatch + OTel Soft weave; Soft HOLD paid / Soft HOLD PII leak / Soft HOLD 5th Story | §6 |
| Scale-out from local/$0 | Phased Soft HOLD 0→4; Soft HOLD multi-provider; Soft HOLD Steps 3–5 invent | §7 |
| KPIs cost · security · performance | Mandatory KPI table; design ≠ SLA; cost/perf must not regress walls | §8 |
| Soft HOLD AWS provision / spend | Explicit Soft HOLD + PoC $0; escalate COO→CEO | Locked #0/#9; §9 |
| App Runner OUT | Locked OUT; reject greenfield App Runner | Locked #1; §2; §9 OUT |
| Soft HOLD multi-provider | Soft HOLD until BM multi-provider start | Locked #7; §7; §9 |
| Soft HOLD invent Stories / invent AC beyond design | Soft HOLD; design-SoR only | Locked #10; §9 |
| Soft HOLD Steps 3–5 Spec | Soft HOLD invent Spec/AC for Steps 3–5 | Locked #7; §9 |
| Gate #27 CLOSED | Do not reopen | Locked #8; §9 OUT |
| MotorMarket / DC4 OUT | OUT | §9 OUT |
| PoC $0 provision | Locked | Locked #9; §1; §9 |

**OUT of this Spec (not AC):** Provision/deploy Stories; invent Steps 3–5 Spec/AC; invent meter/billing product; invent Cognito as delivered; reopen #27; claim hosted-at-scale / production SLAs.

---

## 12. Verification evidence (design SoR only)

Design Spec names **architecture / Security handshake evidence** only — do **not** invent SD product tests beyond design.

| Evidence | Path / ref | Role |
|----------|------------|------|
| CA PASS architecture Option A | `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md` | Binding design SoR |
| Architecture QA PASS | `verification/2026-10-02__sa__verification__mvp-sa-step-2-aws-arch-design-review.md` | Arch QA PASS Soft HOLD provision |
| Security QA SA track PASS | `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-qa-confirm.md` | SA-step 1–10 MET; qa-confirm only |
| Spec Security checklist | `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md` | Spec-step points 1–10 (this Spec binds) |
| Soft HOLD Spec-step Security QA | Soft HOLD SoR `…mvp-spec-step-2-aws-arch-design-qa-confirm.md` (expected; Soft HOLD until ISSUED + MERGED) | Spec QA Soft HOLD until this PASS + Soft HOLD SoR MERGED |
| Tip | **`adbb310`** | Soft HOLD SoR checklist CLEAR context |

No SD product automated tests are invented by this design Spec. Provision/deploy verification Soft HOLD until CEO spend unlock + separate SA-REV-AWS-PROVISION.

---

## 13. Spec QA Done-list Soft HOLD

Spec QA Soft HOLD until **all** of:

1. Spec-step Security QA PASS on points 1–10 (Soft HOLD SoR `…mvp-spec-step-2-aws-arch-design-qa-confirm.md`)
2. Soft HOLD SoR qa-confirm **MERGED** (handshake Soft HOLD SoR = qa-confirm only — do **not** invent points-review Soft HOLD SoR)
3. Chief Spec clear after Security PASS

**Done-list content (for Spec QA when cleared):**

- [ ] Design-SoR covers services / tenancy / store / secrets / obs / scale-out (Issue #142 In-scope)
- [ ] KPI table present (cost efficiency · security · performance) with design ≠ SLA note
- [ ] Host ECS Express Mode locked; App Runner OUT
- [ ] Explicit IN / OUT / Soft HOLD match Issue #142 + Spec Security point 9
- [ ] §10 Security Spec checklist binding 1–10 with Spec section cites
- [ ] No invent Stories / invent AC beyond design / invent Steps 3–5 Spec / provision AC
- [ ] Tip **`adbb310`** + CA PASS + Arch QA + Security QA SA cites present
- [ ] Gate #27 CLOSED / Soft #41 CLOSED via #66+#67 held; PoC $0; MotorMarket/DC4 OUT

---

## 14. Done-list → Chief Spec

- [x] Design Spec written at DOC-FLOW path
- [x] Option A services / tenancy / store / secrets / obs / scale-out bound from CA PASS
- [x] KPI table present
- [x] Soft HOLD / OUT pack locked (provision, spend, Stories, Steps 3–5, multi-provider, App Runner, #27, PoC $0, MotorMarket/DC4, no SLA claims)
- [x] Spec Security checklist points 1–10 answered with Spec section cites
- [x] Acceptance mapping for #142 design-SoR AC (no invent Stories/provision AC)
- [x] Tip **`adbb310`** + all CA PASS paths cited
- [ ] Spec QA Soft HOLD until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED

**Confirm to Chief Spec only** when draft ready for Spec QA (after Security PASS gate). Soft HOLD provision Soft HOLD stands. PoC **$0**. Quiet.
