# Security checklist — Architecture (SA) · Step 2 AWS cloud architecture design (design-only Soft HOLD provision)

**Status:** Chief Security itemized points for Architecture step (handshake per `ops/ORG-OPS.md`). Issue **before** Architecture QA PASS. Architecture is **Security-critical**.
**Date:** 2026-10-02
**Author:** Dealoware Chief Security
**Issue:** https://github.com/ioaikh/dealoware/issues/142 · Spec Step 2 — AWS cloud architecture design (design-only Soft HOLD provision)
**Moment:** Product Step 2 — `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 2
**CEO unlock:** 2026-10-02 ~8:20am ET via CPM · status:in-dev
**Deliverable (Architecture design):** SA written AWS architecture design grounding for Spec #142 — services, tenancy/isolation, data store shape, secrets/PII boundary, observability, scale-out path from local/$0; KPIs **cost efficiency**, **security**, **performance** — **not** provision. Expected path under `architecture/` (DOC-FLOW SA architecture; Soft HOLD invent Stories).
**Sibling Spec Security checklist (already ISSUED):** `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md`
**Grounding (binding cite):**
- `product/2026-10-02__product__strategy__extended-next-steps-1y.md` Step 2
- `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` (ECS Express Mode sketch; App Runner excluded; PoC local)
- `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md` (no spend)
- Option A tip `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3b (dual wall)
- Gate **#27** CLOSED Soft HOLD SoR @ tip `4a97984` (do **not** reopen)
**Hold:** Soft HOLD Architecture QA PASS until Security QA confirms 1–10. Soft HOLD score until this checklist Soft HOLD SoR CLEAR. Soft HOLD handshake Soft HOLD SoR = **qa-confirm only** after Senior+QA+Chief PASS (Gate **#25/#26** pattern; **do not invent points-review Soft HOLD SoR**). Soft HOLD Spec content Soft HOLD until **CA design grounding PASS** (Chief Spec lock). Soft HOLD **AWS account / resource provision / spend**. Soft HOLD **invent product Stories / invent Spec/AC Steps 3–5**. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft **#41** CLOSED via **#66+#67** (do not re-open). App Runner excluded. No MotorMarket/Cognito invent as delivered / DC4. Gate **#27** CLOSED stay closed. PoC **$0**.
**DOC-FLOW:** `verification/2026-10-02__security__verification__mvp-sa-step-2-aws-arch-design-checklist.md`
**Tip context:** `4a97984`

## Scope note
Architecture Step 2 grounding turns the local/$0 + **Amazon ECS Express Mode (Fargate)** sketch into a **real AWS architecture design** (services + isolation + store + secrets/PII + observability + scale-out) with explicit cost/security/perf KPIs. Real points — not N/A. This design does **not** authorize AWS account creation, resource provision, deploy, paid observability spend, Cognito/SSO/IdP as delivered, mature vault/KMS as delivered, Soft HOLD multi-provider rewrite, invent Stories, invent Steps 3–5 Spec/AC, reopen Gate #27, App Runner greenfield, MotorMarket/DC4, or MCP marketplace.

## Itemized security points (Architecture design must answer)

1. **Design-only Soft HOLD provision / Soft HOLD spend** — Architecture review states Step 2 is **design grounding only**: **no** AWS account create, **no** cluster/service/resource provision, **no** paid plugins, **no** non-local deploy. Any later provision / spend → escalate **CA → CPM → COO → CEO**. PoC **$0** for provision. Cite Product Step 2 OUTs + DevOps ECS Express lock.

2. **Host baseline ECS Express Mode; App Runner OUT** — Architecture locks compute host shape to **Amazon ECS Express Mode (Fargate)** (status `open` per SA currency check). Architecture must **not** recommend App Runner for greenfield (excluded: `existing-customers-only` + `no-new-features`). Cite SA roadmap AWS host lock + DevOps lock; Soft HOLD invent alternate greenfield host.

3. **Tenancy / isolation inherits Option A dual-wall** — Architecture’s tenancy/isolation design for AWS **binds** CEO-accepted Option A **§3b**: Platform API/DB FieldPolicy wall **and** agent/tool hard wall (same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses). Reject prompt-only / soft-guidance-as-sole-control, parallel agent ACL tables that drift from FieldPolicy, or tenant shortcuts that weaken fail-closed list/discovery scrub. Cite Option A tip + Gate #27 CLOSED / Stage C #67 tip.

4. **Secrets / PII boundary Soft HOLD invent mature vault** — Architecture preserves identity-until-accept: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side. Mature PII vault / KMS / Cognito/SSO/IdP named only as **DEFER / Soft HOLD later maturity** — **not** as Step 2 delivered or provisioned. Soft HOLD invent Cognito as Step-2-delivered.

5. **Data store shape fail-closed under FieldPolicy** — Architecture’s store shape (local SQLite → designed PostgreSQL-when-AWS-shape path) must keep Field ACL / list fail-closed / discovery scrub / StrategyBody ACL integrity. No invent multi-tenant shared-DB-without-wall or cross-tenant read paths. Soft HOLD invent Stories that Field-capture store migration as in-scope delivery here.

6. **Observability Soft HOLD invent paid / Soft HOLD PII leak** — Architecture may design observability for cost/security/perf KPIs, but Soft HOLD invent **paid** observability beyond free tiers without COO→CEO, and Soft HOLD designs that dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces. Soft HOLD invent OTel/audit as a **5th Story**.

7. **Security KPI is real (not slogan)** — Architecture’s **security** KPI table must be measurable against dual-wall hold, fail-closed budget/cutoff where metered (#68), Accept-gated ShareOutbound, and Soft HOLD provision. Cost-efficiency / performance KPIs must **not** authorize security regressions (drop wall #2, share secrets across tenants, App Runner greenfield). Cite Product Step 2 KPI triad.

8. **Scale-out path Soft HOLD multi-provider / Soft HOLD Steps 3–5 invent** — Architecture’s scale-out path from local/$0 Soft HOLDs multi-provider Spec/doc rewrite until BM multi-provider start; Soft HOLDs invent Spec/AC for Steps **3–5**; Soft HOLDs invent product Stories beyond this design. Soft #41 stays CLOSED via #66+#67 — do not reopen as host gap.

9. **OUT / Soft HOLD locked pack** — Architecture OUT list must include: AWS provision/spend Soft HOLD; App Runner; Soft HOLD multi-provider rewrite; invent Stories Soft HOLD; invent Steps 3–5 Spec/AC Soft HOLD; Gate **#27** CLOSED stay closed; MotorMarket/DC4 OUT; Cognito/SSO/mature vault as delivered OUT; MCP marketplace OUT; PoC **$0** provision. Cost/critical → COO → CEO.

10. **Traceability + handshake Soft HOLD SoR pattern** — Architecture cites Product Step 2 + SA PoC feasibility AWS lock + DevOps ECS Express lock + Option A §3b + Gate #27 CLOSED tip `4a97984` + Soft #41 CLOSED via #66+#67. Architecture QA must **not** PASS until Security QA confirms these points via `…mvp-sa-step-2-aws-arch-design-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** (Gate #25/#26 pattern) — **do not invent points-review Soft HOLD SoR**. Soft HOLD CA formal content PASS Soft HOLD until checklist Soft HOLD SoR CLEAR + Senior answers § Security + Security QA → Chief PASS (then Spec Soft HOLD until CA design grounding PASS per Chief Spec).

## Handshake next
1. Soft HOLD SoR twin (this checklist) via Docs **now**; Soft HOLD score until Soft HOLD SoR CLEAR.
2. Senior Architect answers points in the Step 2 AWS architecture design under `architecture/` § Security (cite sections / evidence) — Soft HOLD invent Stories; Soft HOLD provision.
3. Senior Security may file points-review (content OK) — **handshake Soft HOLD SoR = qa-confirm only** (do **not** Soft HOLD SoR points-review).
4. Security QA PASS to Chief Security (or further instructions) → `…mvp-sa-step-2-aws-arch-design-qa-confirm.md`.
5. Chief Security PASS/HOLD to CPM + Chief Architect (+ Spec Soft HOLD until CA grounding PASS).
6. Later handshake Soft HOLD SoR **qa-confirm only** after Senior+QA+Chief PASS.
7. Sibling Spec checklist remains separate Soft HOLD SoR / Soft HOLD Spec QA track.

## Cost/critical
No AWS / IdP / LLM / paid observability spend without COO → CEO. Soft HOLD provision Soft HOLD stands. Soft HOLD multi-provider Soft HOLD stands. Soft HOLD invent Stories Soft HOLD stands. App Runner excluded. Gate #27 CLOSED. PoC **$0**.
