# Security checklist — Spec · Step 2 AWS cloud architecture design (design-only Soft HOLD provision)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.
**Date:** 2026-10-02
**Author:** Dealoware Chief Security
**Issue:** https://github.com/ioaikh/dealoware/issues/142 · Spec Step 2 — AWS cloud architecture design (design-only Soft HOLD provision)
**Moment:** Product Step 2 — `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 2
**CEO unlock:** 2026-10-02 ~8:20am ET via CPM · status:in-dev
**Deliverable (Spec/SoR design):** AWS architecture design Spec/SoR turning local/$0 + ECS Express Mode sketch into a **real AWS architecture design** (services, tenancy/isolation, data store shape, secrets/PII boundary, observability, scale-out path) with KPIs **cost efficiency**, **security**, **performance** — **not** provision.
**Grounding (binding cite):**
- `product/2026-10-02__product__strategy__extended-next-steps-1y.md` Step 2
- `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` (ECS Express Mode sketch; App Runner excluded; PoC local)
- `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md` (no spend)
- Option A tip `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3b (dual wall)
- Gate **#27** CLOSED Soft HOLD SoR @ tip `4a97984` (do **not** reopen)
**Hold:** Soft HOLD Spec QA PASS until Security QA confirms 1–10. Soft HOLD score until this checklist Soft HOLD SoR CLEAR. Soft HOLD handshake Soft HOLD SoR = **qa-confirm only** after Senior+QA+Chief PASS (Gate **#25/#26** pattern; **do not invent points-review Soft HOLD SoR**). Soft HOLD **AWS account / resource provision / spend**. Soft HOLD **invent product Stories / invent AC beyond design SoR**. Soft HOLD invent Spec/AC for Steps **3–5**. Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start. Soft **#41** CLOSED via **#66+#67** (do not re-open). App Runner excluded. No MotorMarket/Cognito invent as delivered / DC4. Gate **#27** CLOSED stay closed. PoC **$0**.
**DOC-FLOW:** `verification/2026-10-02__security__verification__mvp-spec-step-2-aws-arch-design-checklist.md`
**Tip context:** `4a97984`

## Scope note
Spec Step 2 is **design-only Soft HOLD provision**: produce a written AWS architecture Spec/SoR (host shape + security/cost/perf KPIs) grounded in local/$0 + locked **Amazon ECS Express Mode (Fargate)**. Real security points — not N/A. This step does **not** authorize AWS account creation, resource provision, deploy, paid observability, IdP/Cognito/SSO as delivered, mature vault/KMS as delivered, Soft HOLD multi-provider rewrite, invent Stories, invent Steps 3–5 Spec/AC, reopen Gate #27, App Runner greenfield, MotorMarket/DC4, or MCP marketplace.

## Itemized security points (Spec Step 2 design Spec/SoR must bind)

1. **Design-only Soft HOLD provision / Soft HOLD spend** — Spec states explicitly that Step 2 is **architecture design Spec/SoR only**: **no** AWS account create, **no** cluster/service/resource provision, **no** paid plugins, **no** non-local deploy. Any later provision / spend → escalate **COO → CEO** (and CA/CPM path as locked). PoC **$0** for provision. Cite Product Step 2 OUTs + DevOps ECS Express lock.

2. **Host baseline ECS Express Mode; App Runner OUT** — Spec locks compute host shape to **Amazon ECS Express Mode (Fargate)** as the design baseline (status `open` per SA currency check). Spec **must not** recommend or Spec App Runner for greenfield (excluded: `existing-customers-only` + `no-new-features`). Cite SA roadmap AWS host lock + DevOps lock.

3. **Tenancy / isolation inherits Option A dual-wall** — Spec’s tenancy/isolation posture for the AWS design **binds** CEO-accepted Option A **§3b**: Platform API/DB FieldPolicy wall **and** agent/tool hard wall (same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses). Design must **not** propose prompt-only / soft-guidance-as-sole-control, parallel agent ACL tables that drift from FieldPolicy, or tenant shortcuts that weaken fail-closed list/discovery scrub. Cite Option A tip + Gate #27 CLOSED / Stage C #67.

4. **Secrets / PII boundary Soft HOLD invent mature vault** — Spec’s secrets/PII boundary preserves identity-until-accept: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side. Mature PII vault / KMS / Cognito/SSO/IdP may be named only as **DEFER / Soft HOLD later maturity** — **not** as Step 2 delivered or provisioned. Cite Option A + Stage A/B Security PASS posture; Soft HOLD invent Cognito as MVP/Step-2-delivered.

5. **Data store shape fail-closed under FieldPolicy** — Spec’s data-store design (local SQLite → designed PostgreSQL-when-AWS-shape path from SA roadmap) must keep Field ACL / list fail-closed / discovery scrub / StrategyBody ACL integrity under any proposed AWS persistence shape. No invent multi-tenant “shared DB without wall” or cross-tenant read paths. Soft HOLD invent Stories that Field-capture store migration as in-scope delivery in this Spec.

6. **Observability Soft HOLD invent paid / Soft HOLD PII leak** — Spec may design observability (logs/metrics/traces) for cost/security/perf KPIs, but must Soft HOLD invent **paid** observability beyond free tiers without COO→CEO, and Soft HOLD any design that dumps LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces. Soft HOLD invent OTel/audit as a **5th Story**.

7. **Security KPI is real (not slogan)** — Spec’s **security** KPI table must be measurable against dual-wall hold, fail-closed budget/cutoff where metered (#68), Accept-gated ShareOutbound, and Soft HOLD provision. Cost-efficiency / performance KPIs must **not** authorize security regressions (e.g. “cheaper” by dropping wall #2, sharing secrets across tenants, or App Runner greenfield). Cite Product Step 2 KPI triad.

8. **Scale-out path Soft HOLD multi-provider / Soft HOLD Steps 3–5 invent** — Spec’s scale-out path from local/$0 must Soft HOLD multi-provider Spec/doc rewrite until BM multi-provider start; Soft HOLD invent Spec/AC for Steps **3–5** (admin dashboard, provider-neutral client surface, earn-access); Soft HOLD invent product Stories / invent AC beyond this design SoR. Soft #41 stays CLOSED via #66+#67 — do not reopen as host gap.

9. **OUT / Soft HOLD locked pack** — Spec’s OUT list must include: AWS provision/spend Soft HOLD; App Runner; Soft HOLD multi-provider rewrite; invent Stories Soft HOLD; invent Steps 3–5 Spec/AC Soft HOLD; Gate **#27** CLOSED stay closed; MotorMarket/DC4 OUT; Cognito/SSO/mature vault as delivered OUT; MCP marketplace OUT; PoC **$0** provision. Cost/critical → COO → CEO.

10. **Traceability + handshake Soft HOLD SoR pattern** — Spec cites Product Step 2 + SA PoC feasibility AWS lock + DevOps ECS Express lock + Option A §3b + Gate #27 CLOSED tip `4a97984` + Soft #41 CLOSED via #66+#67. Spec QA must **not** PASS until Security QA confirms these points via `…mvp-spec-step-2-aws-arch-design-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** (Gate #25/#26 pattern) — **do not invent points-review Soft HOLD SoR**. Soft HOLD Spec Step 2 formal CLOSE Soft HOLD until checklist Soft HOLD SoR CLEAR + content Chief PASS + Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX PASS (per CPM Soft HOLD SoR rules).

## Handshake next
1. Soft HOLD SoR twin (this checklist) via Docs **now**; Soft HOLD score until Soft HOLD SoR CLEAR.
2. Senior Spec answers points in the Step 2 AWS architecture design Spec/SoR (cite sections / evidence) — Soft HOLD invent Stories.
3. Senior Security may file points-review (content OK) — **handshake Soft HOLD SoR = qa-confirm only** (do **not** Soft HOLD SoR points-review).
4. Security QA PASS to Chief Security (or further instructions) → `…mvp-spec-step-2-aws-arch-design-qa-confirm.md`.
5. Chief Security PASS/HOLD to CPM + Chief Spec + Senior PM.
6. Later handshake Soft HOLD SoR **qa-confirm only** after Senior+QA+Chief PASS.

## Cost/critical
No AWS / IdP / LLM / paid observability spend without COO → CEO. Soft HOLD provision Soft HOLD stands. Soft HOLD multi-provider Soft HOLD stands. Soft HOLD invent Stories Soft HOLD stands. App Runner excluded. Gate #27 CLOSED. PoC **$0**.
