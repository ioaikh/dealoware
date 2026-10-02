# Spec — Spec Step 3: Platform-owner admin dashboard (O1–O3 + narrow related ops) — #148

**Status:** Senior Spec — Security-bound design-SoR; Spec QA Soft HOLD until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED  
**Date:** 2026-10-02  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec CLEAR WRITE #148 (CA PASS Option A Soft HOLD SoR unlocked; Spec Security checklist ISSUED)  
**Issue:** https://github.com/ioaikh/dealoware/issues/148 · `status:in-dev` · Spec Step 3 — Platform-owner admin dashboard (users, offers, negotiations, related ops)  
**DOC-FLOW:** `specs/2026-10-02__spec__spec__platform-owner-admin-dashboard.md`  
**Constraints:** **Design-SoR Spec only** for platform-owner admin dashboard Step 3. Soft HOLD invent Stories · Soft HOLD invent AC beyond Product scope · Soft HOLD AWS provision/spend · Soft HOLD multi-provider · Soft HOLD Marketing publish · Soft HOLD invent Spec/AC Steps 4–5 · Gate **#27 CLOSED** (do not reopen) · Soft **#41 CLOSED** via **#66+#67** (do not reopen) · **App Runner OUT** · MotorMarket/DC4 OUT · O9 OUT · A8-mature OUT · A9-mature OUT · O4/O6 OUT · settlement/escrow OUT · no parallel admin ACL (use Option A §3a/§3b dual wall) · platform-owner admin **≠** Participant UI (#69) · PoC **$0** · any provision/spend → **COO → CEO** (and CA/CPM path as locked). Tips: architecture Soft HOLD SoR **`d63c55f`** · SA Security Soft HOLD SoR CLEAR **`610a623`** (PRs **#151+#153**) · AWS inherit Soft HOLD provision **`c28361f`**. Spec QA Soft HOLD until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED. Handshake Soft HOLD SoR = **qa-confirm only** — do **not** invent points-review Soft HOLD SoR. Done-list to **Chief Spec** (not Spec QA yet).

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #148 In-scope + Soft HOLD/OUT | https://github.com/ioaikh/dealoware/issues/148 | Binding Spec Step 3 acceptance framing |
| Product Spec scope lock | `product/2026-10-02__product__note__step3-admin-dashboard-spec-scope.md` | Binding Spec IN/OUT (O1–O3 + narrow related ops; role boundary) |
| Product Step 3 strategy | `product/2026-10-02__product__strategy__extended-next-steps-1y.md` § Step 3 | Strategy/SoR framing; KPI / OUT posture; earlier-than-V3 sequencing |
| Roadmap O1–O3 cite | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | O1–O3 was V3; sequenced earlier as strategy only |
| CA PASS Option A Soft HOLD SoR | `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` @ tip **`d63c55f`** | Binding architecture design SoR (Option A pick; PlatformOwner ≠ #69; dual wall; host inherit Soft HOLD provision) |
| Arch QA PASS | `verification/2026-10-02__sa__verification__mvp-sa-step-3-admin-dashboard-review.md` | Architecture QA PASS (Soft HOLD Spec Soft HOLD until CA PASS path; Soft HOLD invent Stories) |
| SA Security Soft HOLD SoR CLEAR | `verification/2026-10-02__security__verification__mvp-sa-step-3-admin-dashboard-qa-confirm.md` @ tip **`610a623`** (PRs **#151+#153**) | SA-step Security QA PASS (1–10 MET; qa-confirm only) |
| Spec Security checklist | `verification/2026-10-02__security__verification__mvp-spec-step-3-admin-dashboard-checklist.md` | Spec-step points **1–10** (this Spec must bind / weave) |
| AWS Step 2 design Soft HOLD SoR #142 | `specs/2026-10-02__spec__spec__aws-cloud-architecture-design-ecs-express.md` @ tip **`c28361f`** | Design constraints inherit Soft HOLD provision — **cite; do not invent provision Stories** |
| Option A secrets/ACL (§3a/§3b) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall cite — **do not rewrite**; Soft HOLD invent parallel admin ACL that drifts |
| Participant UI #69 CLOSED Soft HOLD SoR | `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md` | Role boundary: platform-owner admin ≠ Participant UI |
| Gate #27 CLOSED Soft HOLD SoR | Gate **#27 CLOSED** Soft HOLD SoR (cite; do not reopen) | Identity/ACL gate CLOSED |
| Soft #41 CLOSED | Soft **#41 CLOSED** via **#66+#67** | Do not reopen as host/admin gap |

**Product alignment:** Product Step 3 success = written platform-owner admin Spec/SoR covering O1–O3 + narrow related ops with role boundary platform-owner ≠ Participant, OUT list explicit (SSO/mature vault/etc.), claims-safe language; still **$0 provision**. Conflicts → escalate PM → Product → CEO. Cost/critical → COO → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | Design-SoR only | This Spec is **platform-owner admin dashboard design Spec/SoR only** — Soft HOLD invent Stories · Soft HOLD invent AC beyond Product scope · Soft HOLD AWS account/resource provision/spend · Soft HOLD claim hosted admin as delivered |
| 1 | Architecture pick | **Option A** — PlatformOwner admin route (API + thin admin UI) on **same** modular-monolith host as MVP fabric; distinct `PlatformOwner` principal / role claim; fail-closed admin FieldPolicy; Soft HOLD invent separate admin microservice / admin mesh |
| 2 | Role boundary | **platform-owner admin ≠ Participant UI (#69)** — Soft HOLD invent Participant Strategy/Assistant as admin; Soft HOLD grant Participant roles platform-owner admin powers |
| 3 | Dual wall | Admin surfaces **must** use Option A **§3a/§3b** FieldPolicy dual wall (API/DB FieldPolicy **and** agent/tool hard wall; same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses). Soft HOLD invent **parallel admin ACL** that drifts. Soft HOLD weaken Participant dual wall for PlatformOwner convenience |
| 4 | Scope IN | **O1** registered users list/view/manage (roles **minimum**, Soft HOLD SSO) · **O2** Artifacts owner views/manage · **O3** negotiations/offers views/manage · **narrow related platform ops** that directly support O1–O3 on existing MVP fabric entities — fail-closed |
| 5 | Host inherit | Cite AWS Step 2 Option A Soft HOLD SoR #142 @ tip **`c28361f`** as **design constraints only** (ECS Express Mode Fargate Soft HOLD provision; managed Postgres Soft HOLD provision; Secrets Manager/task role; CloudWatch Soft HOLD paid; **App Runner OUT**; Option A §3b dual wall). Soft HOLD invent provision Stories · Soft HOLD invent second cluster Soft HOLD invent App Runner |
| 6 | Soft HOLD mature identity / vault / metering | Soft HOLD invent **O9** SSO/IdP / Cognito as Step 3 delivered · Soft HOLD invent **A8** mature metering UI · Soft HOLD invent **A9** mature PII vault/KMS · Soft HOLD invent paid observability beyond free tiers without COO→CEO |
| 7 | Soft HOLD Steps 4–5 / multi-provider / Marketing | Soft HOLD invent Spec/AC Steps **4–5** until Step 3 CLOSED (+ CEO confirm invent before Step 5) · Soft HOLD multi-provider Spec/doc rewrite until Step 3 CLOSED (then Step 4 track only) · Soft HOLD Marketing publish |
| 8 | Gate #27 / Soft #41 | Gate **#27 CLOSED** — do not reopen · Soft **#41 CLOSED** via **#66+#67** — do not reopen |
| 9 | Spend path | Any later provision/spend → escalate **COO → CEO** (and CA → CPM path as locked); PoC **$0** for provision |
| 10 | Scope label | Documented as Spec Step 3 **design-SoR** (platform-owner admin); Soft HOLD invent Stories / invent AC beyond Product scope |

---

## 1. Purpose

Turn Product Step 3 scope lock + CA PASS Option A Soft HOLD SoR into a **Security-bound platform-owner admin dashboard Spec/SoR** for Issue #148: distinct PlatformOwner admin route surface covering **O1** registered users (list/view/manage; roles minimum), **O2** Artifacts owner views/management, **O3** negotiations/offers views/management, plus **narrow related platform ops** that directly support (1)–(3) on existing MVP fabric entities — fail-closed.

This Spec is **design-SoR only**. Soft HOLD invent Stories. Soft HOLD invent AC beyond Product scope. Soft HOLD AWS provision/spend. Soft HOLD Spec work for Steps 4–5. Soft HOLD multi-provider rewrite. Soft HOLD Marketing publish. Soft HOLD invent Cognito/SSO / mature vault / mature metering as Step 3 delivered. Soft HOLD invent parallel admin ACL. Soft HOLD reuse Participant UI (#69) as admin. Soft HOLD invent settlement/escrow/checkout. PoC **$0**.

Role boundary (binding Product + CA PASS): **platform-owner admin ≠ Participant UI (#69)**.

---

## 2. Architecture pick — Option A

| Option | Spec disposition |
|--------|------------------|
| **A. Same modular-monolith host; separate PlatformOwner admin route (API + thin admin UI); distinct `PlatformOwner` principal / role claim; fail-closed FieldPolicy dual wall; no parallel admin ACL** | **PICK** — Soft HOLD SoR only; Soft HOLD invent Stories Soft HOLD provision |
| B. Reuse Participant UI (#69) as admin | **OUT / Reject** — collapses role boundary; Product OUT |
| C. Cognito / SSO as Step 3 delivered | **OUT / Reject** — O9 Soft HOLD; Soft HOLD invent Cognito as delivered |
| D. Separate admin microservice / admin mesh / multi-account admin | **OUT / Reject** — invents ops cost / Story surface / second cluster Soft HOLD provision invent |
| E. App Runner admin host | **OUT / Reject** — App Runner OUT (currency + AWS Step 2 lock) |

**Pick: Option A** — Soft HOLD SoR only. Soft HOLD invent Stories. Soft HOLD provision. Soft HOLD invent separate microservice/admin mesh. Soft HOLD invent Cognito/SSO as Step 3 delivered.

**Soft HOLD DEFER (not IN):** O9 SSO/IdP · A9 mature PII vault · A8 mature metering UI · O4/O6 and other O* not in Product IN.

---

## 3. Principal / role boundary (PlatformOwner ≠ #69)

| Concern | Spec lock | Soft HOLD |
|---------|-----------|-----------|
| Principal class | Distinct **`PlatformOwner`** principal / role claim — **≠** Participant principal used by #69 | Soft HOLD invent Participant Strategy/Assistant as admin |
| Permissions/roles (O1 minimum) | Minimum platform-owner admin capability: role flag / claim sufficient for O1 list/view/manage registered users + O2/O3 admin views/management | Soft HOLD invent **full RBAC matrix** as Story invent — design shape only; Soft HOLD invent O9 SSO |
| Dual wall | Admin surfaces **must** use Option A §3a/§3b FieldPolicy dual wall; Soft HOLD invent parallel admin ACL that drifts from Domain `IFieldPolicy.Evaluate` | Soft HOLD invent admin bypass of Participant dual wall |
| Agent plane | If admin agents Soft HOLD invent later: Option A §3b dual wall holds for admin tool/agent plane | Soft HOLD invent admin agents as Step 3 delivered |
| LoginEmail | PlatformOwner admin Read for LoginEmail (fail-closed, PlatformOwner-only) Soft HOLD invent dump LoginEmail to logs/metrics/traces | Soft HOLD invent LoginEmail exposure beyond fail-closed admin Read for PlatformOwner |

**Explicit:** Soft HOLD invent reuse Participant UI (#69) as admin. Soft HOLD grant Participant roles platform-owner admin powers. Soft HOLD invent Participant Strategy/Assistant surfaces in this Spec.

---

## 4. O1 — Registered users (list/view/manage)

| Surface | Spec lock | Soft HOLD |
|---------|-----------|-----------|
| List / view / manage registered users | Admin API + thin admin UI under PlatformOwner principal; fail-closed list/get | Soft HOLD invent SSO; Soft HOLD invent Cognito as delivered |
| Permissions/roles minimum | PlatformOwner role flag Soft HOLD invent full RBAC Story matrix | Soft HOLD invent O9 IdP |
| PII (LoginEmail) | FieldPolicy-aware: LoginEmail User/PlatformOwner Read Soft HOLD invent dump to logs; Soft HOLD invent agent/model context for LoginEmail | Soft HOLD invent LoginEmail exposure beyond fail-closed admin Read for PlatformOwner |

---

## 5. O2 — Artifacts (owner): platform-owner views/management

| Surface | Spec lock | Soft HOLD |
|---------|-----------|-----------|
| Artifacts owner views/management | Platform-owner admin queries/management on **existing MVP fabric Artifact entities**; FieldPolicy + resource scope; Soft HOLD invent parallel Artifact ACL | Soft HOLD invent settlement/escrow/checkout; Soft HOLD invent Artifact storage Stories beyond Product IN; Soft HOLD invent Field-capture store migration as in-scope delivery **here** |

---

## 6. O3 — Negotiations / offers: platform-owner views/management

| Surface | Spec lock | Soft HOLD |
|---------|-----------|-----------|
| Negotiations / offers views/management | Platform-owner admin views/management on existing negotiation/offer entities; FieldPolicy fail-closed | Soft HOLD invent cross-tenant Participant bypass of dual wall for agent path; Soft HOLD invent settlement |
| Identity-until-accept | Preserve: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side | Soft HOLD leak LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into admin UI, logs, or exports without explicit Accept-gated / FieldPolicy allow |

---

## 7. Related platform ops (narrow) + host inherit

### 7.1 Narrow related ops

| Surface | Spec lock | Soft HOLD |
|---------|-----------|-----------|
| Narrow ops views | **Only** ops views that **directly support** O1–O3 on existing MVP fabric entities (e.g. health/status Soft HOLD invent O5 invent as full) — fail-closed; read/manage boundaries for Spec | Soft HOLD invent O4 global analytics · Soft HOLD invent O6 owner notifications · Soft HOLD invent other O* not in Product IN · Soft HOLD invent broad ops/admin surface · Soft HOLD invent Stories for ops scope expansion |

### 7.2 Host inherit (AWS Step 2 Option A Soft HOLD provision @ tip `c28361f`)

| Layer | Inherit Soft HOLD SoR | Soft HOLD |
|-------|----------------------|-----------|
| Compute | Amazon **ECS Express Mode (Fargate)** — admin deploy **same** Express Mode service Soft HOLD provision | Soft HOLD invent second cluster Soft HOLD provision; Soft HOLD invent App Runner |
| Data | Managed Postgres Soft HOLD provision (AWS Step 2 Option A) — same EF domain model Soft HOLD invent CQRS | Soft HOLD invent store Stories as Step 3 provision delivery |
| Secrets | Secrets Manager / task role Soft HOLD provision | Soft HOLD invent mature vault as delivered |
| Observability | CloudWatch Soft HOLD paid backends Soft HOLD PII leak | Soft HOLD invent paid APM as capacity claim; Soft HOLD invent OTel/audit as a new Story here |
| Soft HOLD provision | Cite AWS Soft HOLD SoR #142 @ tip **`c28361f`** — **design constraints only; do not rewrite**; Soft HOLD AWS account/resource provision/spend | Soft HOLD invent provision as open now |

---

## 8. Security binding (dual wall + PII)

- **Cite Option A §3a/§3b dual wall** (do not rewrite): API/DB FieldPolicy + agent/tool hard wall; same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses.
- Admin route surface: API/DB **fail-closed** for admin routes; Soft HOLD invent parallel admin ACL that drifts.
- Soft HOLD weaken Participant dual wall for PlatformOwner convenience.
- Soft HOLD invent mature PII vault / Cognito / KMS as Step 3 delivered.
- Soft HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces.
- Soft HOLD invent admin agents as Step 3 delivered.
- Gate **#27 CLOSED** / Soft **#41 CLOSED** via **#66+#67** — do not reopen.
- Claims hygiene: intermediary · identity-until-accept · no settlement/escrow invent · no traction claims · PoC **$0**.

---

## 9. Explicit IN / OUT / Soft HOLD

### IN (this design Spec/SoR)

- Platform-owner admin dashboard design Spec/SoR Option A for Spec #148 / Product Step 3 (O1–O3 + narrow related platform ops)
- Principal / role boundary: PlatformOwner ≠ Participant (#69)
- Dual wall: Option A §3a/§3b FieldPolicy — Soft HOLD invent parallel admin ACL
- Host inherit: AWS Step 2 Option A Soft HOLD SoR #142 @ tip **`c28361f`** (ECS Express Mode Soft HOLD provision; App Runner OUT; Option A §3b dual wall) — **design constraints only**
- Binding cites: Product Spec scope lock + Product Step 3 strategy + roadmap O1–O3 cite + CA PASS Option A @ tip **`d63c55f`** + Arch QA PASS + SA Security Soft HOLD SoR CLEAR @ tip **`610a623`** (PRs **#151+#153**) + Spec Security checklist
- Spec Security answers 1–10 MET against ISSUED Spec-step checklist (§10)

### OUT

- invent Stories Soft HOLD (Quiet on Stories)
- invent AC beyond Product scope
- AWS account / resource provision / spend Soft HOLD
- Marketing publish Soft HOLD
- Soft HOLD multi-provider Soft HOLD until Step 3 CLOSED (then Step 4 track only)
- invent Spec/AC Steps **4–5** until Step 3 CLOSED (+ CEO confirm invent before Step 5 Spec)
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
- MCP marketplace
- multi-account admin / separate admin microservice mesh Soft HOLD invent
- parallel admin ACL that drifts from FieldPolicy
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
- Soft HOLD Spec QA PASS until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED
- Soft HOLD Spec Step 3 formal CLOSE Soft HOLD until checklist Soft HOLD SoR CLEAR + content Chief PASS + Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX PASS (per CPM Soft HOLD SoR rules)
- Gate **#27 CLOSED** — do not reopen
- Soft **#41 CLOSED** via **#66+#67** — do not reopen

---

## 10. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-10-02__security__verification__mvp-spec-step-3-admin-dashboard-checklist.md`  
**Prior SA Security PASS:** `verification/2026-10-02__security__verification__mvp-sa-step-3-admin-dashboard-qa-confirm.md` @ tip **`610a623`** (PRs **#151+#153**; 1–10 MET; qa-confirm only)  
**CA PASS arch Soft HOLD SoR:** `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` @ tip **`d63c55f`**  
**Arch QA PASS:** `verification/2026-10-02__sa__verification__mvp-sa-step-3-admin-dashboard-review.md`  
**Rule:** Spec QA must **not** PASS until Spec-step Security QA confirms these points via Soft HOLD SoR `…mvp-spec-step-3-admin-dashboard-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** — **do not invent points-review Soft HOLD SoR**.

| # | Security point | Spec requirement / answer | Spec section cites |
|---|----------------|---------------------------|--------------------|
| 1 | Role boundary: platform-owner admin ≠ Participant UI (#69) | **MET.** Spec states explicitly that platform-owner admin dashboard is a **distinct** surface (API + thin admin UI) with distinct **`PlatformOwner`** principal / role claim — **≠** Participant UI (#69). Soft HOLD invent Participant Strategy/Assistant as admin. Soft HOLD conflate Participant capabilities with platform-owner admin or grant Participant roles platform-owner admin powers. Cite #69 CLOSED Soft HOLD SoR + Product Spec scope lock. | Locked #2; §2 Option A pick / reject B; §3 Principal / role boundary; Sources (#69 Spec, Product scope lock) |
| 2 | O1 users — permissions/roles minimum; Soft HOLD O9 SSO/IdP | **MET.** Spec covers registered users list/view/manage under **minimum** PlatformOwner role flag Soft HOLD invent full RBAC Story matrix Soft HOLD invent Cognito/SSO / O9 as Step 3 delivered. Soft HOLD invent mature identity beyond min admin role model. Cite Product Spec scope OUTs. | Locked #4/#6; §4 O1; §2 Soft HOLD DEFER O9; §9 OUT O9 / Cognito |
| 3 | Fail-closed dual-wall inherits Option A §3b | **MET.** Admin views/management of users, Artifacts, negotiations/offers, and related ops **bind** CEO-accepted Option A **§3b**: Platform API/DB FieldPolicy wall **and** agent/tool hard wall (same Domain `IFieldPolicy.Evaluate` for **all** FieldClasses). Soft HOLD invent prompt-only controls, parallel ACL tables that drift from FieldPolicy, tenant shortcuts that weaken fail-closed list/discovery scrub, or admin bypass that dumps denied FieldClasses. Soft HOLD weaken Participant dual wall for PlatformOwner convenience. Cite Option A tip + Gate #27 CLOSED / Stage C #67. | Locked #3; §3 Dual wall; §8 Security binding; Sources (Option A §3a/§3b, Gate #27 CLOSED) |
| 4 | O2 Artifacts Soft HOLD invent Field-capture Stories | **MET.** Spec defines platform-owner Artifact views/management on **existing MVP fabric Artifact entities** under FieldPolicy fail-closed + resource scope. Soft HOLD invent product Stories / Field-capture store migration as in-scope delivery **here**. Soft HOLD invent AC beyond Product scope Soft HOLD invent parallel Artifact ACL Soft HOLD invent settlement/escrow/checkout. Cite Product scope + Soft HOLD invent Stories. | Locked #4; §5 O2; §9 OUT / Soft HOLD |
| 5 | O3 Soft HOLD invent ShareOutbound regressions | **MET.** Spec's platform-owner negotiations/offers views/management preserve identity-until-accept: no contact/PII on public Participant DTOs; **LoginEmail** never in agent/model context; **ContactEmail** ShareOutbound **Accept-gated** server-side. Soft HOLD leak LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into admin UI, logs, or exports without explicit Accept-gated / FieldPolicy allow. Soft HOLD invent cross-tenant Participant bypass of dual wall for agent path. Cite Option A + Stage A/B Security PASS posture + Gate #27 CLOSED. | Locked #3/#4; §6 O3; §8 Security binding; Sources (Option A §3b, Gate #27 CLOSED) |
| 6 | Related platform ops narrow + fail-closed | **MET.** Spec includes only ops views that **directly** support O1–O3 on existing MVP fabric entities — fail-closed. Soft HOLD invent broad ops/admin surface Soft HOLD invent settlement/escrow/checkout Soft HOLD invent O4/O6 and other O* not in Product IN Soft HOLD invent Stories for ops scope expansion. | Locked #4; §7.1 Related platform ops; §9 OUT O4/O6 / settlement |
| 7 | Soft HOLD AWS provision; inherit Step 2 design constraints only | **MET.** Spec cites Step 2 AWS architecture design Soft HOLD SoR #142 @ tip **`c28361f`** as **design constraints only** (ECS Express Mode Fargate Soft HOLD provision; managed Postgres Soft HOLD provision; Secrets Manager/task role; CloudWatch Soft HOLD paid; App Runner OUT; Option A §3b dual wall). Soft HOLD AWS account create / resource provision / spend / paid plugins / non-local deploy. Any later provision/spend → escalate **COO → CEO** (and CA/CPM path as locked). PoC **$0** for provision. Soft HOLD invent second cluster Soft HOLD invent App Runner Soft HOLD invent store Stories as Step 3 provision delivery. Cite #142 Soft HOLD SoR + DevOps ECS Express lock Soft HOLD spend. | Locked #5/#9; §7.2 Host inherit; §9 Soft HOLD provision; Sources (#142 Spec @ `c28361f`) |
| 8 | Soft HOLD mature A8 metering UI / A9 PII vault / paid observability | **MET.** Spec Soft HOLD invent A8 mature metering UI Soft HOLD invent A9 mature PII vault/KMS Soft HOLD invent Cognito/SSO as delivered Soft HOLD invent paid observability beyond free tiers without COO→CEO Soft HOLD dump LoginEmail / ContactEmail / StrategyBody / denied FieldClasses into logs/metrics/traces Soft HOLD invent OTel/audit as a new Story in this Spec. CloudWatch Soft HOLD paid backends Soft HOLD PII leak (inherit Step 2 obs Soft HOLD). Soft HOLD invent admin agents. | Locked #6; §7.2 Observability Soft HOLD; §8 Security binding; §9 Soft HOLD A8/A9 Soft HOLD / OUT |
| 9 | OUT / Soft HOLD locked pack | **MET.** Spec's OUT / Soft HOLD pack includes: Soft HOLD invent Stories; Soft HOLD invent AC beyond Product scope; Soft HOLD AWS provision/spend; Soft HOLD Marketing publish; Soft HOLD multi-provider Spec/doc rewrite until Step 3 CLOSED; Soft HOLD invent Spec/AC Steps **4–5** until Step 3 CLOSED (+ CEO confirm invent before Step 5); **O9** SSO/IdP; **A8** mature metering UI; **A9** mature PII vault; settlement/escrow/checkout; Gate **#27** CLOSED stay closed; Soft **#41 CLOSED** via **#66+#67**; App Runner; MotorMarket/DC4; Cognito as delivered; MCP marketplace; PoC **$0**. Cost/critical → COO → CEO. Soft HOLD invent reuse Participant UI (#69) as admin Soft HOLD invent multi-account admin / separate admin microservice mesh Soft HOLD invent parallel admin ACL. | Locked #0/#7/#8/#9/#10; §9 IN/OUT/Soft HOLD; header Constraints |
| 10 | Traceability + handshake Soft HOLD SoR pattern | **MET.** Spec cites Product Spec scope lock + strategy Step 3 + Option A §3b + Step 2 #142 tip **`c28361f`** + #69 Participant boundary + Gate #27 CLOSED + Soft #41 CLOSED via #66+#67 + CA PASS Option A tip **`d63c55f`** + Arch QA PASS + SA Security Soft HOLD SoR CLEAR tip **`610a623`** (PRs **#151+#153**) + Spec Security checklist. Spec QA Soft HOLD until Security QA confirms via Soft HOLD SoR `…mvp-spec-step-3-admin-dashboard-qa-confirm.md`. Handshake Soft HOLD SoR = **qa-confirm only** (Gate #25/#26/#27 pattern) — **do not invent points-review Soft HOLD SoR**. Soft HOLD Spec Step 3 formal CLOSE Soft HOLD until checklist Soft HOLD SoR CLEAR + content Chief PASS + Soft HOLD SoR qa-confirm CLEAR + Docs QA INDEX PASS. Soft HOLD invent Stories Soft HOLD Soft HOLD invent AC beyond Product Soft HOLD. Done-list to **Chief Spec** (not Spec QA yet). | This §10; Sources; §9 Soft HOLD; header Status/Constraints; §14 Done-list → Chief Spec |

---

## 11. Acceptance mapping — Product scope / Issue #148 AC

**Binding:** Product Spec scope lock IN/OUT + Issue #148 In-scope bullets + Soft HOLD/OUT locks. Design only — do **not** invent product Stories or invent AC beyond Product scope.

| Product / Issue #148 requirement | Spec lock | Spec section |
|----------------------------------|-----------|--------------|
| **O1** — registered users: list/view/manage; permissions/roles **minimum** (not full SSO) | PlatformOwner admin API + thin UI; min role flag Soft HOLD invent full RBAC Soft HOLD invent O9/Cognito | Locked #4/#6; §4 |
| **O2** — artifacts (owner): platform-owner views/management of Artifacts | Existing MVP fabric Artifact entities; FieldPolicy + resource scope Soft HOLD invent Field-capture Stories Soft HOLD invent parallel Artifact ACL Soft HOLD invent settlement | Locked #4; §5 |
| **O3** — negotiations / offers: platform-owner views/management | Existing negotiation/offer entities; FieldPolicy fail-closed; identity-until-accept; Soft HOLD ShareOutbound regressions | Locked #4; §6 |
| **Related platform ops (narrow):** only ops that directly support (1)–(3) on existing MVP fabric — fail-closed | Narrow ops only Soft HOLD invent O4/O6 Soft HOLD invent broad ops Soft HOLD invent Stories for ops expansion | Locked #4; §7.1 |
| Role boundary: platform-owner admin ≠ Participant UI (#69) | Distinct PlatformOwner principal Soft HOLD invent Participant conflation Soft HOLD invent #69 reuse as admin | Locked #2; §2/#3 |
| Inherit AWS design Soft HOLD (design constraints only; **no provision**) | Cite #142 @ tip **`c28361f`** Soft HOLD provision Soft HOLD invent provision Stories Soft HOLD invent App Runner Soft HOLD invent second cluster | Locked #5/#9; §7.2 |
| Claims hygiene: intermediary · identity-until-accept · no settlement/escrow invent · no traction claims · PoC $0 | Explicit in §8 + §9 OUT | §8; §9 OUT |
| Soft HOLD invent Stories Soft HOLD | Explicit Soft HOLD | Locked #0/#10; §9 Soft HOLD |
| Soft HOLD invent AC beyond Product scope Soft HOLD | Explicit Soft HOLD — AC map = Product IN only | Locked #0/#10; this §11; §9 Soft HOLD |
| Soft HOLD AWS provision/spend Soft HOLD | Explicit Soft HOLD; escalate COO→CEO; PoC $0 | Locked #5/#9; §7.2; §9 |
| Soft HOLD Marketing publish Soft HOLD | Explicit Soft HOLD / OUT | §9 OUT / Soft HOLD |
| Soft HOLD multi-provider Soft HOLD until Step 3 CLOSED | Explicit Soft HOLD | Locked #7; §9 Soft HOLD |
| Soft HOLD invent Spec/AC Steps 4–5 Soft HOLD | Explicit Soft HOLD until Step 3 CLOSED (+ CEO confirm invent before Step 5) | Locked #7; §9 Soft HOLD |
| O9 SSO / IdP OUT | Explicit OUT | Locked #6; §9 OUT |
| A8 mature metering UI OUT | Explicit OUT | Locked #6; §9 OUT |
| A9 mature PII vault OUT | Explicit OUT | Locked #6; §9 OUT |
| O4 / O6 and other O* not in IN OUT | Explicit OUT | §7.1; §9 OUT |
| Settlement / escrow / checkout OUT | Explicit OUT | §5/#6/#8; §9 OUT |
| Gate #27 CLOSED stay closed | Explicit OUT reopen | Locked #8; §9 OUT |
| Soft #41 CLOSED via #66+#67 | Explicit OUT reopen | Locked #8; §9 OUT |
| MotorMarket / DC4 OUT | Explicit OUT | §9 OUT |
| App Runner OUT | Explicit OUT | Locked #5; §2 reject E; §7.2; §9 OUT |
| PoC $0 | Locked | Locked #9; §1; §9 |
| Option A pick (same modular-monolith; PlatformOwner route; dual wall; no parallel admin ACL) | Locked Option A Soft HOLD SoR | Locked #1/#3; §2 |
| Soft HOLD invent Cognito as delivered | Explicit OUT | §2 reject C; §9 OUT |
| Soft HOLD invent multi-account admin / separate admin microservice mesh | Explicit OUT / Reject D | §2 reject D; §9 OUT |

**OUT of this Spec (not AC):** invent Stories; invent AC beyond Product; provision/deploy Stories; invent Spec/AC Steps 4–5; invent Cognito/SSO as delivered; invent mature A8/A9; invent O4/O6; invent settlement/escrow; invent parallel admin ACL; reopen #27 / Soft #41; claim hosted admin as delivered; Marketing publish; multi-provider rewrite.

**Product AC mapping gaps:** **None.** Every Product Spec scope lock IN item and Issue #148 In-scope / Soft HOLD/OUT bullet maps to a Spec row above. Soft HOLD inventing AC beyond Product — no gaps invented to fill.

---

## 12. Verification evidence (design SoR only)

Design Spec names **architecture / Security handshake evidence** only — do **not** invent SD product tests / Stories beyond design SoR.

| Evidence | Path / ref | Role |
|----------|------------|------|
| CA PASS architecture Option A Soft HOLD SoR | `architecture/2026-10-02__sa__architecture__admin-dashboard-step-3.md` @ tip **`d63c55f`** | Binding design SoR |
| Architecture QA PASS | `verification/2026-10-02__sa__verification__mvp-sa-step-3-admin-dashboard-review.md` | Arch QA PASS Soft HOLD invent Stories Soft HOLD Spec Soft HOLD until CA PASS path |
| SA Security Soft HOLD SoR CLEAR | `verification/2026-10-02__security__verification__mvp-sa-step-3-admin-dashboard-qa-confirm.md` @ tip **`610a623`** (PRs **#151+#153**) | SA-step 1–10 MET; qa-confirm only |
| Spec Security checklist | `verification/2026-10-02__security__verification__mvp-spec-step-3-admin-dashboard-checklist.md` | Spec-step points 1–10 (this Spec binds) |
| Soft HOLD Spec-step Security QA | Soft HOLD SoR `…mvp-spec-step-3-admin-dashboard-qa-confirm.md` (expected; Soft HOLD until ISSUED + MERGED) | Spec QA Soft HOLD until this PASS + Soft HOLD SoR MERGED |
| AWS Step 2 Soft HOLD SoR inherit | `specs/2026-10-02__spec__spec__aws-cloud-architecture-design-ecs-express.md` @ tip **`c28361f`** | Design constraints Soft HOLD provision |
| Tip architecture Soft HOLD SoR | **`d63c55f`** | CA PASS Option A Soft HOLD SoR CLEAR (PR **#152**) |
| Tip SA Security Soft HOLD SoR | **`610a623`** | SA Security Soft HOLD SoR CLEAR (PRs **#151+#153**) |
| Tip AWS inherit | **`c28361f`** | Step 2 design Soft HOLD SoR Soft HOLD provision |

No SD product automated tests / Stories are invented by this design Spec. Provision/deploy verification Soft HOLD until CEO spend unlock + separate unlock. Soft HOLD invent Stories Soft HOLD.

---

## 13. Spec QA Done-list Soft HOLD

Spec QA Soft HOLD until **all** of:

1. Spec-step Security QA PASS on points 1–10 (Soft HOLD SoR `…mvp-spec-step-3-admin-dashboard-qa-confirm.md`)
2. Soft HOLD SoR qa-confirm **MERGED** (handshake Soft HOLD SoR = qa-confirm only — do **not** invent points-review Soft HOLD SoR)
3. Chief Spec clear after Security PASS

**Done-list content (for Spec QA when cleared):**

- [ ] Design-SoR covers O1–O3 + narrow related ops (Issue #148 / Product Spec scope lock IN)
- [ ] Option A pick locked; PlatformOwner ≠ #69; dual wall §3a/§3b; no parallel admin ACL
- [ ] Host inherit cites AWS Soft HOLD SoR #142 @ tip **`c28361f`** (design-only Soft HOLD provision; App Runner OUT)
- [ ] Explicit IN / OUT / Soft HOLD match Product scope + Spec Security point 9
- [ ] §10 Security Spec checklist binding 1–10 with Spec section cites
- [ ] §11 Acceptance mapping complete — all Product IN / Issue #148 Soft HOLD/OUT bullets present as rows; Soft HOLD invent AC beyond Product
- [ ] No invent Stories / invent AC beyond Product / invent Spec/AC Steps 4–5 / provision AC / Cognito delivered / parallel admin ACL
- [ ] Tips **`d63c55f`** + **`610a623`** + **`c28361f`** + CA PASS + Arch QA + SA Security cites present
- [ ] Gate #27 CLOSED / Soft #41 CLOSED via #66+#67 held; PoC $0; MotorMarket/DC4 OUT; O9/A8-mature/A9-mature/O4/O6/settlement OUT

---

## 14. Done-list → Chief Spec

- [x] Design Spec written at DOC-FLOW path
- [x] Option A PlatformOwner admin route bound from CA PASS Soft HOLD SoR @ tip **`d63c55f`**
- [x] Role boundary PlatformOwner ≠ Participant (#69) explicit
- [x] Dual wall Option A §3a/§3b cited — Soft HOLD invent parallel admin ACL
- [x] O1–O3 + narrow related ops Soft HOLD SoR surfaces bound from Product Spec scope lock
- [x] Soft HOLD / OUT pack locked (invent Stories, invent AC beyond Product, AWS provision/spend, multi-provider, Marketing, Steps 4–5 Spec/AC, O9, A8-mature, A9-mature, O4/O6, settlement/escrow, Gate #27, Soft #41, App Runner, MotorMarket/DC4, Cognito delivered, parallel admin ACL, PoC $0)
- [x] Spec Security checklist points 1–10 answered with Spec section cites (§10)
- [x] Acceptance mapping for Product / Issue #148 AC (no invent Stories / invent AC beyond Product; **no Product AC mapping gaps**)
- [x] Tips **`d63c55f`** + **`610a623`** (PRs **#151+#153**) + **`c28361f`** + all CA PASS / Arch QA / SA Security paths cited
- [ ] Spec QA Soft HOLD until Spec-step Security QA PASS + Soft HOLD SoR qa-confirm MERGED

**Confirm to Chief Spec only** when draft ready for Spec QA (after Spec-step Security PASS gate). Soft HOLD Spec QA until then. Soft HOLD invent Stories Soft HOLD. Soft HOLD provision Soft HOLD stands. PoC **$0**. Quiet.
