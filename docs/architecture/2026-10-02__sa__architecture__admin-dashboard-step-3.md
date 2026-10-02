# Architecture options Soft HOLD SoR — Spec #148 / Product Step 3 platform-owner admin dashboard

**Status:** Senior Architect **proposal Soft HOLD SoR** for Spec #148 / Product Step 3. Soft Soft CLOSE Soft HOLD Architecture QA PASS until Chief Security checklist ISSUED + § Security answers amended + Security QA confirms via **qa-confirm Soft HOLD SoR only** (**no** invent points-review Soft HOLD SoR). Soft Soft CLOSE Soft HOLD invent Stories. Soft HOLD provision / Soft HOLD spend. Quiet. PoC **$0**.  
**Date:** 2026-10-02  
**Author:** Dealoware Senior Architect  
**Brief from:** Chief Architect → Senior Architect → Architecture QA (Architecture always Security-critical)  
**Issue:** https://github.com/ioaikh/dealoware/issues/148  
**Moment IDs:** **SA-REV-STEP3-ADMIN** (this deliverable) · Soft Soft CLOSE Soft HOLD next until CA PASS + Arch QA + Security Soft HOLD SoR CLEAR  
**DOC-FLOW:** `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md`  
**Security checklist:** Soft Soft CLOSE Soft HOLD **AWAIT** Chief Security ISSUED (do **not** invent checklist points)  
**Expected Security QA Soft HOLD SoR:** Soft HOLD path until checklist ISSUED — handshake Soft HOLD SoR = **qa-confirm only** (no invent points-review Soft HOLD SoR)

| Field | Value |
|-------|-------|
| Date | 2026-10-02 |
| Story / epic / phase | Spec #148 / Product Step 3 — platform-owner admin dashboard (O1–O3 + narrow related ops) |
| Binding Product sources | `product/2026-10-02__product__note__step3-admin-dashboard-spec-scope.md` · `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 3 · roadmap cite `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` O1–O3 (was V3; sequenced earlier as strategy only) |
| Host shape | Amazon **ECS Express Mode (Fargate)** Soft HOLD provision — inherit AWS Step 2 Option A Soft HOLD SoR; **App Runner OUT** |
| Cost | PoC **$0** · Soft HOLD provision · Soft HOLD spend |
| Tip | Soft Soft CLOSE Soft HOLD `c28361f` (AWS Soft HOLD SoR #142 tip context) |
| Status | Senior Architect proposal Soft HOLD SoR · § Security **AWAIT** checklist |
| Author | Dealoware Senior Architect |
| Brief from | CA |
| Security | Architecture always Security-critical · Soft Soft CLOSE Soft HOLD Arch QA PASS until checklist + answers + Security QA qa-confirm Soft HOLD SoR only |

## Sources

| Source | Role |
|--------|------|
| Issue #148 | Spec Step 3 — platform-owner admin dashboard; `status:in-dev` |
| Product Spec scope lock | `product/2026-10-02__product__note__step3-admin-dashboard-spec-scope.md` — binding Spec IN/OUT |
| Product Step 3 strategy | `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 3 |
| Roadmap O1–O3 cite | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` — O1–O3 was V3; sequenced earlier as strategy only |
| AWS design Soft HOLD SoR #142 | `architecture/2026-10-02__sa__architecture__aws-cloud-design-step-2.md` @ tip Soft Soft CLOSE Soft HOLD `c28361f` — Option A ECS Express Mode Fargate + managed Postgres Soft HOLD provision + Secrets Manager/task role + CloudWatch; Option A §3b dual wall; App Runner OUT — **cite; do not rewrite** |
| Option A secrets/ACL | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b dual wall + FieldPolicy — **cite; do not rewrite**; admin surfaces must use FieldPolicy dual wall; Soft HOLD invent parallel admin ACL that drifts |
| Gate #27 CLOSE review | `architecture/2026-10-01__sa__architecture__mvp-sa-rev-mvp-close-review.md` — Gate **#27 CLOSED**; Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67** |
| SA options brief template | `architecture/templates/sa-architecture-options-brief-template.md` |
| Tip Soft Soft CLOSE Soft HOLD `c28361f` | AWS Soft HOLD SoR #142 tip context; Soft HOLD multi-provider Soft HOLD |

**Locks (cite; do not change):** Gate **#27 CLOSED** — do not reopen · Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67** — do not reopen · Soft HOLD AWS provision/spend — no provision invent · Soft HOLD multi-provider Soft HOLD · Soft HOLD invent Stories · Soft HOLD invent AC beyond Product scope · Soft HOLD Step 4 until Step 3 CLOSED · Soft HOLD Step 5 until Step 4 · Soft HOLD O9 SSO/IdP · Soft HOLD A8 mature metering UI · Soft HOLD A9 mature PII vault · Soft HOLD O4/O6 and other O* not in IN · settlement/escrow/checkout OUT · MotorMarket/DC4 OUT · App Runner OUT · PoC **$0**.

---

## 1. Purpose

Architecture options Soft HOLD SoR for Spec #148 / Product Step 3 handshake: platform-owner **admin dashboard** covering **O1–O3** (registered users; Artifacts owner; negotiations/offers) plus **narrow related platform ops** that directly support (1)–(3) on existing MVP fabric entities — fail-closed.

This deliverable is **design Soft HOLD SoR only**. Soft HOLD invent Stories. Soft HOLD invent AC beyond Product scope. Soft HOLD provision. Soft HOLD spend. Soft Soft CLOSE Soft HOLD Architecture QA PASS until Chief Security checklist ISSUED, § Security answers amended, and Security QA confirms via **qa-confirm Soft HOLD SoR only** (**no** invent points-review Soft HOLD SoR).

Role boundary (binding Product): **platform-owner admin ≠ Participant UI (#69)**. Soft HOLD invent Participant Strategy/Assistant surfaces in this Spec.

---

## 2. Options + tradeoffs

| Option | Pros | Cons | Pick |
|--------|------|------|------|
| **A. Same modular-monolith host as MVP fabric; separate platform-owner admin route surface (API + thin admin UI) with distinct `PlatformOwner` principal / role claim; fail-closed admin FieldPolicy (or PlatformOwner principal class) that never weakens Participant dual wall** | Simplest maintainable; reuses Option A §3a/§3b FieldPolicy + AWS Step 2 Option A host Soft HOLD provision; one deployable Express Mode service Soft HOLD provision; clear role boundary vs #69; list/get admin queries remain FieldPolicy-aware for PII | Soft HOLD invent second cluster/mesh; Soft HOLD invent full RBAC matrix as Story invent (design shape only here) | **Recommended** |
| B. Reuse Participant UI (#69) as admin | Fast surface reuse | Collapses role boundary; Participant Strategy/Assistant invent risk; weakens dual wall clarity; Product OUT | **Reject** |
| C. Cognito / SSO as Step 3 delivered | Mature IdP narrative | O9 Soft HOLD / OUT of Product scope; invents AC beyond Product; Soft HOLD invent Cognito as delivered | **Reject** (Soft HOLD DEFER O9) |
| D. Separate admin microservice / admin mesh / multi-account admin | Isolation theater | Invents ops cost, Story surface, second cluster Soft HOLD provision invent; violates simplest maintainable; Soft HOLD multi-provider adjacency | **Reject** |
| E. App Runner admin host | Historically simple PaaS | App Runner OUT (currency + AWS Step 2 lock); Soft HOLD invent alternate greenfield host | **Reject** |

**Pick: Option A** — Soft HOLD SoR only. Soft HOLD invent Stories. Soft HOLD provision. Soft HOLD invent separate microservice/admin mesh. Soft HOLD invent Cognito/SSO as Step 3 delivered.

**Soft HOLD DEFER (not IN):** O9 SSO/IdP · A9 mature PII vault · A8 mature metering UI · O4/O6 and other O* not in Product IN.

---

## 3. Recommended architecture design (Option A)

### 3.1 Principal / role boundary

| Concern | Design shape Soft HOLD SoR | Soft Soft CLOSE Soft HOLD |
|---------|----------------------------|---------------------------|
| Principal class | Distinct **`PlatformOwner`** principal / role claim — **≠** Participant principal used by #69 | Soft HOLD invent Participant Strategy/Assistant as admin |
| Permissions/roles (O1 minimum) | Minimum platform-owner admin capability: role flag / claim sufficient for O1 list/view/manage registered users + O2/O3 admin views/management | Soft HOLD invent **full RBAC matrix** as Story invent — describe design shape only; Soft HOLD invent O9 SSO |
| Dual wall | Admin surfaces **must** use Option A §3a/§3b FieldPolicy dual wall; Soft HOLD invent parallel admin ACL that drifts from Domain `IFieldPolicy.Evaluate` | Soft HOLD invent admin bypass of Participant dual wall |
| Agent plane | If admin agents Soft HOLD invent later: Option A §3b dual wall holds for admin tool/agent plane; Soft HOLD invent admin agents in this Soft HOLD SoR | Soft HOLD invent admin agents as Step 3 delivered |

**Explicit:** PlatformOwner admin Read for LoginEmail (fail-closed, PlatformOwner-only) Soft HOLD invent dump LoginEmail to logs/metrics/traces. Soft HOLD invent LoginEmail exposure beyond fail-closed admin Read for PlatformOwner.

### 3.2 O1 — Registered users (list/view/manage)

| Surface | Design | Soft Soft CLOSE Soft HOLD |
|---------|--------|---------------------------|
| List / view / manage registered users | Admin API + thin admin UI under PlatformOwner principal; fail-closed list/get | Soft HOLD invent SSO; Soft HOLD invent Cognito as delivered |
| Permissions/roles minimum | PlatformOwner role flag Soft HOLD invent full RBAC Story matrix | Soft HOLD invent O9 IdP |
| PII (LoginEmail) | FieldPolicy-aware: LoginEmail User/PlatformOwner Read Soft HOLD invent dump to logs; Soft HOLD invent agent/model context for LoginEmail | Soft HOLD invent LoginEmail exposure beyond fail-closed admin Read for PlatformOwner |

### 3.3 O2 — Artifacts (owner): platform-owner views/management

| Surface | Design | Soft Soft CLOSE Soft HOLD |
|---------|--------|---------------------------|
| Artifacts owner views/management | Platform-owner admin queries/management on **existing MVP fabric Artifact entities**; FieldPolicy + resource scope; Soft HOLD invent parallel Artifact ACL | Soft HOLD invent settlement/escrow/checkout; Soft HOLD invent Artifact storage Stories beyond Product IN |

### 3.4 O3 — Negotiations / offers: platform-owner views/management

| Surface | Design | Soft Soft CLOSE Soft HOLD |
|---------|--------|---------------------------|
| Negotiations / offers views/management | Platform-owner admin views/management on existing negotiation/offer entities; FieldPolicy fail-closed | Soft HOLD invent cross-tenant Participant bypass of dual wall for agent path; Soft HOLD invent settlement |

### 3.5 Related platform ops (narrow)

| Surface | Design | Soft Soft CLOSE Soft HOLD |
|---------|--------|---------------------------|
| Narrow ops views | **Only** ops views that **directly support** O1–O3 on existing MVP fabric entities (e.g. health/status Soft HOLD invent O5 invent as full) — fail-closed; read/manage boundaries for Spec | Soft HOLD invent O4 global analytics · Soft HOLD invent O6 owner notifications · Soft HOLD invent other O* not in Product IN |

### 3.6 Host inherit (AWS Step 2 Option A Soft HOLD provision)

| Layer | Inherit Soft HOLD SoR | Soft Soft CLOSE Soft HOLD |
|-------|----------------------|---------------------------|
| Compute | Amazon **ECS Express Mode (Fargate)** — admin deploy **same** Express Mode service Soft HOLD provision | Soft HOLD invent second cluster Soft HOLD provision; Soft HOLD invent App Runner |
| Data | Managed Postgres Soft HOLD provision (AWS Step 2 Option A) — same EF domain model Soft HOLD invent CQRS | Soft HOLD invent store Stories as Step 3 provision delivery |
| Secrets | Secrets Manager / task role Soft HOLD provision | Soft HOLD invent mature vault as delivered |
| Observability | CloudWatch Soft HOLD paid backends Soft HOLD PII leak | Soft HOLD invent paid APM as capacity claim |
| Soft HOLD provision | Cite AWS Soft HOLD SoR #142 @ tip Soft Soft CLOSE Soft HOLD `c28361f` — **do not rewrite**; Soft HOLD AWS account/resource provision/spend | Soft HOLD invent provision as open now |

### 3.7 Security binding

- **Cite Option A §3a/§3b dual wall** (do not rewrite): API/DB FieldPolicy + agent/tool hard wall; same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses.
- Admin route surface: API/DB **fail-closed** for admin routes; Soft HOLD invent parallel admin ACL that drifts.
- Soft Soft CLOSE Soft HOLD weaken Participant dual wall for PlatformOwner convenience.
- Soft HOLD invent mature PII vault / Cognito / KMS as Step 3 delivered.
- Soft HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces.
- Gate **#27 CLOSED** / Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67** — do not reopen.

---

## 4. Explicit IN / OUT / Soft HOLD

### IN (this Soft HOLD SoR)

- Architecture options Soft HOLD SoR Option A for Spec #148 / Product Step 3 (O1–O3 + narrow related platform ops)
- Principal / role boundary: PlatformOwner ≠ Participant (#69)
- Host inherit: AWS Step 2 Option A Soft HOLD SoR #142 @ tip Soft Soft CLOSE Soft HOLD `c28361f` (ECS Express Mode Soft HOLD provision; App Runner OUT; Option A §3b dual wall)
- Binding cites: Product Spec scope lock + Product Step 3 strategy + roadmap O1–O3 cite + Option A §3a/§3b
- Proposed SA-REV-STEP3-ADMIN moment for CA → CPM
- § Security **AWAIT** Chief Security checklist (placeholder Soft HOLD SoR path note)

### OUT

- invent Stories Soft HOLD (Quiet on Stories)
- invent AC beyond Product scope
- AWS account / resource provision / spend Soft HOLD
- Marketing publish Soft HOLD
- Soft HOLD multi-provider Soft HOLD
- Step 4 until Step 3 CLOSED · Step 5 until Step 4
- **O9** SSO / IdP
- **A8** mature metering UI
- **A9** mature PII vault
- **O4** / **O6** and other O* not in Product IN
- settlement / escrow / checkout
- reopen Gate **#27** / Soft **#41**
- MotorMarket / DC4
- App Runner
- reuse Participant UI (#69) as admin
- Cognito / SSO as Step 3 delivered
- multi-account admin / separate admin microservice mesh Soft HOLD invent
- PoC **$0** claim as provisioned spend

### Soft HOLD

- Soft HOLD provision / Soft HOLD spend (escalate CA → CPM → COO → CEO)
- Soft HOLD invent Stories Soft HOLD
- Soft HOLD invent AC beyond Product scope Soft HOLD
- Soft HOLD multi-provider Soft HOLD until Step 3 CLOSED (then Step 4 track only per Product)
- Soft HOLD O9 SSO Soft HOLD
- Soft HOLD A8 mature UI Soft HOLD · Soft HOLD A9 mature vault Soft HOLD
- Soft HOLD invent admin agents Soft HOLD
- Soft HOLD invent full RBAC matrix as Story invent Soft HOLD (design shape only)
- Soft Soft CLOSE Soft HOLD Architecture QA PASS until Security checklist ISSUED + answers + Security QA **qa-confirm Soft HOLD SoR only** (**no** invent points-review Soft HOLD SoR)
- Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67** — do not reopen
- Gate **#27 CLOSED** — do not reopen
- Soft Soft CLOSE Soft HOLD next SA-REV until CA PASS + Arch QA + Security Soft HOLD SoR CLEAR

---

## 5. Proposed SA architecture-review moments (CA → CPM · `gate:sa-arch-review`)

| Moment ID | Name | Trigger (when) | Scope under review | Next stage HOLD until |
|-----------|------|----------------|--------------------|------------------------|
| **SA-REV-STEP3-ADMIN** | Spec #148 / Product Step 3 platform-owner admin Soft HOLD SoR | This deliverable — Soft HOLD SoR written | Option A admin route surface + PlatformOwner principal + O1–O3 + narrow ops + host inherit AWS Step 2 Option A Soft HOLD provision + dual wall binding + IN/OUT/Soft HOLD + Soft Soft CLOSE Soft HOLD § Security AWAIT | Soft Soft CLOSE Soft HOLD next until **CA PASS** + Architecture QA + Security Soft HOLD SoR CLEAR (**qa-confirm only**; **no** invent points-review Soft HOLD SoR) |
| Soft Soft CLOSE Soft HOLD next | Soft Soft CLOSE Soft HOLD until CA PASS path | Soft Soft CLOSE Soft HOLD invent as open now | Soft Soft CLOSE Soft HOLD invent Stories Soft HOLD | Soft Soft CLOSE Soft HOLD until CA PASS + Arch QA + Security Soft HOLD SoR CLEAR |

**Rules:** Do **not** unlock invent Stories / invent AC beyond Product / Soft HOLD provision Soft HOLD from SA alone. Soft Soft CLOSE Soft HOLD invent Stories Soft HOLD. Soft HOLD multi-provider Soft HOLD stands. Soft Soft CLOSE Soft HOLD Step 4 until Step 3 CLOSED.

---

## 6. § Security — Soft Soft CLOSE Soft HOLD AWAIT checklist

**Architecture always Security-critical.**

| Field | Value |
|-------|-------|
| Checklist | Soft Soft CLOSE Soft HOLD **AWAIT** Chief Security ISSUED — Soft Soft CLOSE Soft HOLD invent checklist points |
| Soft HOLD SoR path note | When Chief Security ISSUES checklist → amend this § Security with answers **1–N** citing sections/evidence in this Soft HOLD SoR |
| Architecture QA | Soft Soft CLOSE Soft HOLD Architecture QA done-list / PASS until checklist + answers + Security QA |
| Handshake Soft HOLD SoR | Soft HOLD SoR = **qa-confirm only** — **no invent points-review Soft HOLD SoR** |

Soft Soft CLOSE Soft HOLD Architecture QA PASS until Security Soft HOLD SoR CLEAR path.

---

## 7. Done-list → Architecture QA

- [ ] Options cover CA brief + Product Spec scope lock (O1–O3 + narrow related ops)
- [ ] Role boundary PlatformOwner ≠ Participant (#69) explicit
- [ ] Host inherit cites AWS Soft HOLD SoR #142 @ tip Soft Soft CLOSE Soft HOLD `c28361f` (Option A; Soft HOLD provision; App Runner OUT)
- [ ] Option A §3a/§3b dual wall cited — Soft HOLD invent parallel admin ACL that drifts
- [ ] Explicit IN / OUT / Soft HOLD match Product scope + CA brief
- [ ] Review moments table present for CA → CPM (`gate:sa-arch-review`)
- [ ] § Security Soft Soft CLOSE Soft HOLD AWAIT checklist — Soft Soft CLOSE Soft HOLD invent answers until ISSUED
- [ ] Soft Soft CLOSE Soft HOLD Architecture QA PASS until **Security QA** confirms via qa-confirm Soft HOLD SoR only (**no invent points-review Soft HOLD SoR**)
- [ ] Confirm to Chief only after Security Soft HOLD SoR CLEAR path Soft Soft CLOSE Soft HOLD invent Stories Soft HOLD

---

## 8. CEO questions

None.

---

## 9. Disposition

Soft HOLD SoR ready for Spec cite after **CA design grounding CLEAR** path. Soft Soft CLOSE Soft HOLD invent Stories Soft HOLD. Soft HOLD invent AC beyond Product scope Soft HOLD. Soft HOLD provision Soft HOLD. Soft Soft CLOSE Soft HOLD Architecture QA PASS until Security checklist ISSUED + answers + Security QA qa-confirm Soft HOLD SoR only. Soft HOLD multi-provider Soft HOLD. Gate **#27 CLOSED**. Soft Soft CLOSE Soft HOLD Soft **#41 CLOSED** via **#66+#67**. Tip Soft Soft CLOSE Soft HOLD `c28361f`. PoC **$0**. Quiet.
