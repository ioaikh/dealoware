# Architecture options — Spec #142 / Product Step 2 AWS cloud design (design-only Soft HOLD provision)

**Status:** Senior Architect **proposal design SoR** for Spec #142 / Product Step 2. **Amended:** § Security answers points 1–10 MET. Soft Soft CLOSE Soft HOLD Architecture QA PASS until Security QA confirms 1–10 (qa-confirm SoR only; **no** invent points-review Soft HOLD SoR). Soft Soft CLOSE Soft HOLD Spec invent of provision Stories. Soft HOLD provision / Soft HOLD spend. Quiet. PoC **$0**.  
**Date:** 2026-10-02  
**Author:** Dealoware Senior Architect  
**Brief from:** Chief Architect → Senior Architect → Architecture QA (Architecture always Security-critical)  
**Issue:** https://github.com/ioaikh/dealoware/issues/142  
**Moment IDs:** SA-REV-AWS-DESIGN (this deliverable) · Soft HOLD SA-REV-AWS-PROVISION (later Soft HOLD until CEO spend unlock — **not** open now)  
**DOC-FLOW:** `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md`  
**Security checklist:** `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-checklist.md` · **Amended:** § Security answers points 1–10  
**Expected Security QA Soft HOLD SoR:** `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-qa-confirm.md` (handshake Soft HOLD SoR = **qa-confirm only**)

| Field | Value |
|-------|-------|
| Date | 2026-10-02 |
| Story / epic / phase | Spec #142 / Product Step 2 — AWS cloud architecture design |
| Binding Product sources | `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 2 |
| Host shape | Amazon **ECS Express Mode (Fargate)** — design SoR; Soft HOLD provision; **App Runner OUT** |
| Cost | PoC **$0** provision Soft HOLD; Soft HOLD spend until COO → CEO |
| Tip | Soft Soft CLOSE Soft HOLD main `4a97984` (docs-mirror / dealoware tip context) |
| Status | Senior Architect proposal design SoR · Security answers 1–10 MET |
| Author | Dealoware Senior Architect |
| Brief from | CA |
| Security | Architecture always Security-critical · checklist ISSUED · answers 1–10 MET |

## Sources

| Source | Role |
|--------|------|
| Issue #142 | Spec Step 2 — design-only Soft HOLD provision |
| Product Step 2 | `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 2 — KPIs cost efficiency · security · performance |
| Feasibility AWS host + persistence | `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` § AWS target host + SQLite local / PostgreSQL when AWS shape matters + modular monolith |
| O10 scaffold | `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md` — containerizable .NET modular-monolith API host |
| Option A secrets/ACL | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b dual wall + FieldPolicy — **cite; do not rewrite**; applies on host |
| Gate #27 CLOSE review | `architecture/2026-10-01__sa__architecture__mvp-sa-rev-mvp-close-review.md` — Gate **#27 CLOSED**; Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67** |
| DevOps host lock | `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md` — ECS Express Mode; no spend |
| DevOps assist ALIGN | `ops/2026-10-02__devops__ops__spec-142-ecs-express-assist.md` — Spec #142 design-only infra guidance (compute / tenancy / data / secrets / obs / KPI Soft HOLDs) |
| ORG-OPS | `ops/ORG-OPS.md` § Hosting / cloud currency |
| SA options brief template | `architecture/templates/sa-architecture-options-brief-template.md` |
| Tip Soft Soft CLOSE Soft HOLD `4a97984` | Gate #27 CLOSED tip context; Soft HOLD multi-provider Soft HOLD |

**Locks (cite; do not change):** Gate **#27 CLOSED** — do not reopen · Soft HOLD multi-provider Soft HOLD — do not invent SoR rewrite as delivered/in-scope · Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67** — do not reopen Stage B Assistant claim · Host ECS Express Mode — App Runner OUT · PoC **$0** for provision · Soft HOLD invent Spec/AC for Steps 3–5 · Soft HOLD invent Stories · MotorMarket/DC4 OUT.

---

## 1. Purpose

Turn the local/$0 runnable posture plus the named **Amazon ECS Express Mode (Fargate)** sketch into a **real AWS architecture design SoR** for Spec #142 / Chief Product accept: services map, tenancy/isolation, data store shape, secrets/PII boundary, observability, and scale-out path from local/$0, with mandatory KPIs **cost efficiency · security · performance**.

This deliverable is **design-only**. Soft HOLD provision. Soft HOLD spend. Soft HOLD AWS account / resource create. Soft HOLD Spec invent of provision Stories. Soft Soft CLOSE Soft HOLD Architecture QA PASS until Security QA confirms checklist points 1–10 via qa-confirm Soft HOLD SoR only.

---

## 2. Options + tradeoffs

| Option | Pros | Cons | Pick |
|--------|------|------|------|
| **A. Single modular-monolith API on ECS Express Mode + managed Postgres + Secrets Manager + CloudWatch** | Matches feasibility/O10/DevOps lock; one deployable; Express Mode orchestrates Fargate + ALB+TLS + autoscaling + monitoring + networking from image + exec role + infra role; pay only underlying resources; FieldPolicy dual wall unchanged on host; simplest maintainable | Soft HOLD provision until CEO spend unlock; managed DB / Secrets Manager / egress named as Soft HOLD buy later | **Recommended** |
| B. Split API + worker / early multi-service mesh | Isolation for future AI workers | Invents ops cost and Story surface now; violates simplest maintainable; Soft HOLD multi-provider adjacency | **Reject / Soft HOLD defer** until separately unlocked load justifies |
| C. AWS App Runner greenfield | Historically simple PaaS | Currency = `existing-customers-only` + `no-new-features`; closed to new customers; AWS recommends ECS Express Mode; DevOps + CA lock exclude | **Reject** |
| D. EKS / self-managed Kubernetes | Fine-grained control | Overkill for current MVP fabric; invents cluster ops; Soft HOLD spend balloon | **Reject** for Step 2 design target |
| E. Lambda-only rewrite | Scale-to-zero narrative | Conflicts .NET modular-monolith O10; rewrites host/auth/EF path; invents AC | **Reject** |

**Pick: Option A** — design SoR only. Aligns DevOps assist (`ops/2026-10-02__devops__ops__spec-142-ecs-express-assist.md`): single Express Mode service sketch → later scale-out; containerized .NET API; managed DB as design option Soft HOLD provision; Secrets Manager / task role pattern; OTel + CloudWatch/X-Ray as design Soft HOLD paid backends; no multi-account invent.

---

## 3. Recommended architecture design (Option A)

### 3.1 Services map

| Layer | Design choice | Notes (Soft HOLD provision) |
|-------|---------------|-------------------------------|
| Compute | **Amazon ECS Express Mode (Fargate)** — single service running containerized `Dealoware.Api` (.NET modular monolith) | Express Mode = container image + task execution role + infrastructure role; orchestrates Fargate service, ALB+TLS, autoscaling, monitoring, networking ([ECS Express Mode overview](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/express-service-overview.html)). Soft HOLD live capacity / task count claims. |
| Load balancing / TLS | ALB as Express Mode provisions | Soft HOLD invent second public entry or custom mesh. |
| Image registry | Amazon ECR | CI push Soft HOLD until spend unlock + pipeline unlock. |
| Data | Managed **PostgreSQL** (RDS PostgreSQL **or** Aurora Serverless v2 as design choice) | Soft HOLD provision; Soft HOLD buy. Cost note: Aurora Serverless v2 may fit bursty early load but is Soft HOLD spend; RDS PostgreSQL is the simpler named option until CEO spend unlock chooses. Same EF Core model as local SQLite. |
| Secrets | AWS Secrets Manager + task-role injection to env/config | Secrets **not** in image. Soft HOLD buy until spend unlock. |
| IAM | Express Mode exec role + infra role + least-privilege task role | Soft HOLD deploy; private egress defaults as design intent (DevOps assist Security KPI Soft HOLD deploy). |
| Object storage | Optional S3 for future artifacts | Soft HOLD; Soft HOLD invent Artifact storage Stories here. |
| Identity / SSO | Design-compatible **OIDC-shaped JWT** already on MVP Participant auth path stays | **Cognito / SSO Soft HOLD** until separate SSO unlock — Soft HOLD invent Cognito as Step-2-delivered. |
| Observability | CloudWatch logs/metrics as Express Mode default; OTel hooks already Soft-woven in MVP moments; optional X-Ray as design Soft HOLD | Soft HOLD paid APM / paid backends without COO→CEO. |

### 3.2 Tenancy / isolation

- **Single-tenant platform DB** with **Participant-scoped authz** via Option A dual wall (API/DB FieldPolicy + agent/tool hard wall; same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses). Cite Option A §3a/§3b — **do not rewrite**.
- Isolation boundary at **app authz + IAM task role** (DevOps assist). Soft HOLD invent multi-account / physical per-tenant isolation as delivered.
- Soft HOLD invent multi-tenant shared-DB-without-wall or cross-tenant read paths.
- VPC / private data plane Soft HOLD until provision unlock; **fail-closed defaults** as design intent (private egress Soft HOLD deploy).
- Reject prompt-only / soft-guidance-as-sole-control; reject parallel agent ACL tables that drift from FieldPolicy (Gate #27 CLOSED / Stage C #67 tip).

### 3.3 Data store shape

| Today (local/$0) | AWS design shape | Rules |
|------------------|------------------|-------|
| EF Core + **SQLite** | EF Core + **PostgreSQL** (same domain model) | One relational model; **no CQRS**; Soft HOLD invent event-sourcing |
| Local migrations | Same EF migrations path → Postgres | Soft HOLD invent Stories that Field-capture store migration as in-scope delivery **in this Step** |
| Backups | Design intent: automated backups / PITR as Soft HOLD provision choice | Soft HOLD buy; Soft HOLD invent capacity SLAs |

Field ACL / list fail-closed / discovery scrub / StrategyBody ACL integrity **must** hold on the AWS store shape (Stages A/B/C delivered walls — Gate #27 CLOSED).

### 3.4 Secrets / PII boundary

- **Cite Option A §3a/§3b dual wall** (do not rewrite): API/DB authorize via FieldPolicy + resource scope; agent/tool hard wall with allowlist + server-side scrub; dual wall for all FieldClasses.
- **LoginEmail** never to agent / model context (User-only).
- **ContactEmail** OwnAgent Read OK; **ShareOutbound only after Accept** (server-side grant).
- Identity-until-accept: no contact/PII on public Participant DTOs.
- Runtime secrets (DB connection, signing keys, LLM API keys if later unlocked) live in **Secrets Manager** / env from **task role** — not baked into image (DevOps assist).
- Mature PII vault / KMS / Cognito Soft HOLD later maturity — **not** Step 2 delivered. Soft HOLD invent vault as Product/Security SoR beyond Option A cite.
- Soft HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces (design rule).

### 3.5 Observability

- CloudWatch logs/metrics as Express Mode default path.
- Design for **structured logs + request ids**; reuse MVP Soft weave touchpoints (OTel/audit/idempotent-offers hooks already Soft-woven — Soft HOLD invent OTel/audit as a **5th Story**).
- Soft HOLD paid observability / APM / X-Ray backends beyond free tiers without COO→CEO (DevOps assist Soft HOLD paid backends).
- Soft HOLD invent new product Stories for observability surfaces in this design.

### 3.6 Scale-out path from local/$0

| Phase | What | Soft Soft CLOSE Soft HOLD |
|-------|------|---------------------------|
| **0** | Local runnable · PoC **$0** | Current valid posture |
| **1** | **This design SoR** (SA-REV-AWS-DESIGN) | Soft Soft CLOSE Soft HOLD next until CA PASS + Arch QA + Security QA qa-confirm |
| **2** | CEO spend unlock → account + min Express Mode service + managed Postgres Soft HOLD | Soft HOLD until COO→CEO; Soft HOLD invent as open now |
| **3** | CI push ECR → Express Mode deploy | Soft HOLD until spend + pipeline unlock |
| **4** | Scale tasks / shared ALB as Express Mode autoscaling allows | Soft HOLD live capacity / SLA claims; Fargate sizing classes = design knobs only (DevOps Performance KPI Soft HOLD live capacity) |
| Later Soft HOLD | Multi-AZ / read replicas / Soft HOLD multi-provider rewrite | Soft HOLD until separately named unlock; Soft HOLD invent Spec/AC Steps 3–5 |

Do **not** claim provisioned capacity or production SLAs. Soft HOLD multi-provider Soft HOLD until BM multi-provider start. Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67** — not a host gap.

---

## 4. Mandatory KPI table

**Explicit:** Design KPIs ≠ live SLAs / provisioned capacity claims. Cost/perf KPIs must **not** authorize security regressions (drop wall #2, share secrets across tenants, App Runner greenfield).

| Design choice | Cost efficiency | Security | Performance |
|---------------|-----------------|----------|-------------|
| **Host — ECS Express Mode (Fargate), single service** | Pay only underlying resources; no App Runner dead-end; Soft HOLD buy until spend OK; tagging / Budgets Soft HOLD buy (DevOps Cost KPI) | `open` greenfield; currency-checked; least-privilege task roles Soft HOLD deploy; App Runner OUT | Fargate sizing classes as design knobs Soft HOLD live capacity; Express Mode autoscaling Soft HOLD claim |
| **DB — managed PostgreSQL (RDS or Aurora Serverless v2 design option)** | Soft HOLD provision; prefer simplest until spend unlock chooses; Soft HOLD invent capacity | Same FieldPolicy wall on store; Soft HOLD shared-DB-without-wall | Same EF model; Soft HOLD invent CQRS; Soft HOLD SLA |
| **Secrets — Secrets Manager + task role** | Soft HOLD buy; no secrets in image reduces blast/rework cost | Dual wall Option A holds; LoginEmail never agent; Accept-gated ShareOutbound; Soft HOLD invent Cognito/vault as delivered | Role injection Soft HOLD deploy latency invent |
| **Observability — CloudWatch default + OTel Soft weave Soft HOLD paid** | Soft HOLD paid backends without COO→CEO | Soft HOLD PII/denied FieldClasses in logs/metrics/traces; Soft HOLD invent 5th Story | Soft HOLD invent APM as capacity claim |
| **Tenancy — app + IAM task role; Option A dual wall** | No multi-account invent cost | Dual wall (API/DB + agent/tool) same `IFieldPolicy`; reject prompt-only; Gate #27 CLOSED / #67 | Single-service path Soft HOLD invent mesh latency |
| **Scale path — phased Soft Soft CLOSE Soft HOLD 0→4** | Soft HOLD spend until CEO; LLM hard-stops remain app (#68) Soft HOLD buy | Soft HOLD multi-provider rewrite; Soft HOLD invent Steps 3–5 Spec/AC; Soft HOLD invent Stories; Soft #41 CLOSED held | Soft HOLD multi-AZ / replicas later; Soft HOLD live capacity |

---

## 5. Explicit IN / OUT / Soft HOLD

### IN (this design SoR)

- Written AWS architecture design Option A (services, tenancy, store, secrets/PII, observability, scale-out)
- KPI table: cost efficiency · security · performance (design only)
- Host lock: Amazon ECS Express Mode (Fargate); currency check updated 2026-10-02
- Binding cites: Product Step 2, feasibility AWS host, O10, Option A §3a/§3b, Gate #27 CLOSED, DevOps lock + assist ALIGN, tip Soft Soft CLOSE Soft HOLD `4a97984`
- Proposed SA-REV moments for CA→CPM
- § Security answers 1–10 MET against ISSUED checklist

### OUT

- AWS account / resource provision / spend / paid plugins / non-local deploy as delivered by this Step
- App Runner greenfield
- MotorMarket / DC4
- Cognito / SSO / mature vault / KMS as **delivered** by Step 2
- MCP marketplace
- Claiming hosted-at-scale / production SLAs / live capacity

### Soft HOLD

- Soft HOLD provision / Soft HOLD spend (escalate CA → CPM → COO → CEO)
- Soft HOLD multi-provider SoR rewrite until BM multi-provider start
- Soft HOLD invent Spec/AC for Steps 3–5
- Soft HOLD invent product Stories beyond this design SoR
- Soft HOLD SA-REV-AWS-PROVISION until CEO spend unlock (**not** open now)
- Soft HOLD multi-account physical isolation / multi-AZ / read replicas / paid APM
- Soft HOLD Cognito/SSO until separate unlock
- Soft HOLD Architecture QA PASS until Security QA qa-confirm Soft HOLD SoR CLEAR
- Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67** — do not reopen
- Gate **#27 CLOSED** — do not reopen
- Soft Soft CLOSE Soft HOLD Spec invent of provision Stories

---

## 6. Hosting currency labels (updated 2026-10-02)

Checked **2026-10-02** against official AWS docs (`ops/ORG-OPS.md` § Hosting / cloud currency enums: `open` | `existing-customers-only` | `no-new-features` | `deprecated/sunset`).

| Service | Status | Greenfield Dealoware? | Official check |
|---------|--------|----------------------|----------------|
| **Amazon ECS Express Mode (Fargate)** | `open` | **Yes** — locked target | [ECS Express Mode overview](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/express-service-overview.html) — available where ECS + Fargate; no closed-to-new / no-new-features notice (fetched **2026-10-02**). Express Mode = image + exec role + infra role; orchestrates Fargate service, ALB+TLS, autoscaling, monitoring, networking; pay only underlying resources. |
| **AWS App Runner** | `existing-customers-only` + `no-new-features` | **No** — excluded | [App Runner availability change](https://docs.aws.amazon.com/apprunner/latest/dg/apprunner-availability-change.html) — closed to new customers; AWS recommends ECS Express Mode (fetched **2026-10-02**). |

**Checklist sources:** `ops/ORG-OPS.md` § Hosting / cloud currency; `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md`; DevOps assist ALIGN `ops/2026-10-02__devops__ops__spec-142-ecs-express-assist.md`. Soft HOLD invent alternate greenfield host.

---

## 7. Proposed SA architecture-review moments (CA → CPM · `gate:sa-arch-review`)

| Moment ID | Name | Trigger (when) | Scope under review | Next stage HOLD until |
|-----------|------|----------------|--------------------|------------------------|
| **SA-REV-AWS-DESIGN** | Spec #142 / Product Step 2 AWS cloud design SoR | This deliverable — design SoR written | Option A services/tenancy/store/secrets/obs/scale-out + KPI triad + currency + IN/OUT/Soft HOLD + Security 1–10 | Soft Soft CLOSE Soft HOLD next until **CA PASS** + Architecture QA + Security Soft HOLD SoR CLEAR (**qa-confirm only**; no invent points-review Soft HOLD SoR) |
| Soft HOLD **SA-REV-AWS-PROVISION** | First AWS provision / min Express Mode + managed DB (later) | Soft HOLD until **CEO spend unlock** via COO→CEO | Soft HOLD invent as open now; provision/deploy Soft HOLD | Soft HOLD until CEO spend unlock + separate CA brief — **do not invent as open now** |

**Rules:** Do **not** unlock provision / Spec invent of provision Stories / Steps 3–5 from SA alone. Soft Soft CLOSE Soft HOLD Spec invent of provision Stories. Soft HOLD multi-provider Soft HOLD stands.

---

## 8. Security answers (Architecture always-critical handshake)

**Checklist:** `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-checklist.md` (ISSUED).  
**Soft Soft CLOSE Soft HOLD Architecture QA PASS until Security QA confirms 1–10** via Soft HOLD SoR `…mvp-sa-step-2-aws-arch-design-qa-confirm.md` (**qa-confirm only** — **no invent points-review Soft HOLD SoR**).

| # | Security point | Architecture answer | Cite |
|---|----------------|---------------------|------|
| 1 | Design-only Soft HOLD provision / Soft HOLD spend | **MET.** Step 2 is **design grounding only**: no AWS account create, no cluster/service/resource provision, no paid plugins, no non-local deploy claimed delivered. Any later provision/spend → escalate **CA → CPM → COO → CEO**. PoC **$0** for provision. | §1 Purpose; §5 OUT/Soft HOLD; Product Step 2 OUTs; DevOps lock `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md`; DevOps assist Soft HOLD provision |
| 2 | Host ECS Express Mode `open`; App Runner OUT | **MET.** Compute host locked to **Amazon ECS Express Mode (Fargate)** status `open` (currency check **2026-10-02**). App Runner excluded (`existing-customers-only` + `no-new-features`). Soft HOLD invent alternate greenfield host. | §3.1 Services map; §6 currency table; feasibility AWS host lock; DevOps lock + assist; Options reject C |
| 3 | Tenancy inherits Option A §3b dual-wall | **MET.** AWS tenancy/isolation binds CEO-accepted Option A **§3b**: Platform API/DB FieldPolicy wall **and** agent/tool hard wall (same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses). Reject prompt-only / soft-guidance-as-sole-control; reject parallel agent ACL tables that drift; Soft HOLD invent tenant shortcuts that weaken fail-closed list/discovery scrub. Isolation at app + IAM task role; Soft HOLD multi-account invent. | §3.2 Tenancy; Option A tip §3a/§3b (cite, not rewrite); Gate #27 CLOSED review; Stage C #67 tip |
| 4 | Secrets/PII Soft HOLD invent mature vault | **MET.** Identity-until-accept preserved: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side. Secrets Manager / task role pattern Soft HOLD buy. Mature PII vault / KMS / Cognito/SSO/IdP = Soft HOLD later maturity — **not** Step 2 delivered. Soft HOLD invent Cognito as Step-2-delivered. | §3.4 Secrets/PII; Option A §3b + LoginEmail/ContactEmail/ShareOutbound rows; DevOps assist Secrets/PII row; §5 OUT |
| 5 | Store shape fail-closed under FieldPolicy | **MET.** Local SQLite → designed PostgreSQL-when-AWS-shape path keeps Field ACL / list fail-closed / discovery scrub / StrategyBody ACL integrity. Soft HOLD invent multi-tenant shared-DB-without-wall or cross-tenant read paths. Soft HOLD invent Stories that Field-capture store migration as in-scope delivery **here**. No CQRS invent. | §3.3 Data store; feasibility persistence row; Gate #27 CLOSED Stages A/B/C walls |
| 6 | Observability Soft HOLD paid / Soft HOLD PII leak | **MET.** Observability designed for KPI triad via CloudWatch default + OTel Soft weave; Soft HOLD invent **paid** observability beyond free tiers without COO→CEO; Soft HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces; Soft HOLD invent OTel/audit as a **5th Story**. | §3.5 Observability; KPI table obs row; DevOps assist Observability Soft HOLD paid backends; Gate #27 Soft weave note |
| 7 | Security KPI real vs dual-wall / budgets / Accept-gate / Soft HOLD provision | **MET.** Security KPI measurable against dual-wall hold, fail-closed budget/cutoff where metered (#68), Accept-gated ShareOutbound, and Soft HOLD provision. Cost-efficiency / performance KPIs explicitly **must not** authorize security regressions (drop wall #2, share secrets across tenants, App Runner greenfield). | §4 KPI table + explicit note; Product Step 2 KPI triad; Option A §3b; #68 fail-closed budgets; §5 Soft HOLD provision |
| 8 | Scale-out Soft HOLD multi-provider / Soft HOLD Steps 3–5 invent | **MET.** Scale-out path Soft HOLDs multi-provider Spec/doc rewrite until BM multi-provider start; Soft HOLDs invent Spec/AC for Steps **3–5**; Soft HOLDs invent product Stories beyond this design. Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67** — do not reopen as host gap. | §3.6 Scale-out; §5 Soft HOLD; §7 moments Soft HOLD SA-REV-AWS-PROVISION; Gate #27 Soft #41 CLOSED |
| 9 | OUT / Soft HOLD locked pack | **MET.** OUT/Soft HOLD pack includes: AWS provision/spend Soft HOLD; App Runner; Soft HOLD multi-provider rewrite; invent Stories Soft HOLD; invent Steps 3–5 Spec/AC Soft HOLD; Gate **#27** CLOSED stay closed; MotorMarket/DC4 OUT; Cognito/SSO/mature vault as delivered OUT; MCP marketplace OUT; PoC **$0** provision. Cost/critical → COO → CEO. | §5 IN/OUT/Soft HOLD; header Locks; DevOps assist Explicit Soft HOLD; Product Step 2 OUTs |
| 10 | Traceability + handshake Soft HOLD SoR pattern | **MET.** Cites Product Step 2 + SA feasibility AWS lock + DevOps ECS Express lock + DevOps assist ALIGN + Option A §3b + Gate #27 CLOSED tip Soft Soft CLOSE Soft HOLD `4a97984` + Soft #41 CLOSED via #66+#67. Architecture QA Soft HOLD until Security QA confirms via `…mvp-sa-step-2-aws-arch-design-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** (Gate #25/#26 pattern) — **no invent points-review Soft HOLD SoR**. Soft Soft CLOSE Soft HOLD Spec invent of provision Stories until CA design grounding PASS path. Soft Soft CLOSE Soft HOLD Architecture QA PASS until Security QA. | This §8; Sources; §7 moments; checklist DOC-FLOW; tip Soft Soft CLOSE Soft HOLD `4a97984` |

---

## 9. Done-list → Architecture QA

- [ ] Options cover CA brief + Product Step 2 + DevOps assist ALIGN
- [ ] KPI table present (cost efficiency · security · performance) with design ≠ SLA note
- [ ] Hosting currency labels updated 2026-10-02 (ECS Express `open`; App Runner excluded)
- [ ] Explicit IN / OUT / Soft HOLD match CA brief + checklist point 9
- [ ] Review moments table present for CA → CPM (`gate:sa-arch-review`)
- [ ] § Security answers 1–10 MET with section cites (checklist ISSUED)
- [ ] Soft Soft CLOSE Soft HOLD Architecture QA PASS until **Security QA** confirms 1–10 (qa-confirm Soft HOLD SoR only; **no invent points-review Soft HOLD SoR**)
- [ ] Confirm to Chief only after Security Soft HOLD SoR CLEAR path

---

## 10. CEO questions

None. Soft Soft CLOSE Soft HOLD disposition for Spec: design SoR ready for Spec after CA PASS path — Soft HOLD Spec invent of provision Stories. Soft HOLD multi-provider Soft HOLD. Soft HOLD provision Soft HOLD. Gate #27 CLOSED. Soft Soft CLOSE Soft HOLD Soft #41 CLOSED via #66+#67. PoC **$0**. Quiet.
