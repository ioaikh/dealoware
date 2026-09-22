# Dev Plan — MVP Stage B Instant search / discovery (#40)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-22  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner PRIORITY **#40 ONLY** — Instant search / discovery (P2)  
**Issue:** https://github.com/ioaikh/dealoware/issues/40  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A (CEO ACCEPTED) · `stage:mvp` · Stage B  
**DOC-FLOW:** `plans/2026-09-22__devplan__plan__mvp-stage-b-instant-search-discovery.md`  
**Constraints:** **#40 ONLY** — do **not** draft #41/#42. Keep siblings separate (cross-ref only). Consume Stage A #31+#32 CLOSED; do not invent Stage C / Assistant / saved-search / A1. PoC **$0**. Gate **#25** stays **backlog** until Stage B delivery — do **not** open/unlock now. Do **not** unlock #18 Spec/SD as a whole. No Cognito/MM/DC4; no vault/KMS inventing. ECS Express sketch only. Cost/critical → CPM. SD HOLD until Dev Plan QA + Security QA PASS + Chief unlock.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md` | Instant search contracts; Locked #0–#8; §7 AC + §7.1 tests; OUT; Security §5 |
| Spec QA (Spec gate PASS) | `verification/2026-09-22__spec__verification__mvp-stage-b-instant-search-discovery.md` | Spec-side bind; Sec10 weave intact |
| Spec Security PASS | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan (SoR PR #44) |
| Spec Security points-review | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-points-review.md` | Senior PASS 10/10 |
| Dev Plan Security checklist (must weave 1–10) | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-devplan-checklist.md` | Dev Plan-step handshake points **1–10** |
| Spec Security checklist (upstream) | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-checklist.md` | Spec-step Security 1–10 |
| Option A architecture (Stage B) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Stage B discovery must not leak secrets; discovery ≠ owner inventory |
| BA note #40 | `plans/2026-09-22__ba__note__story-40-instant-search-discovery.md` | Spec-ready refine; P2 instant only |
| Stage A #31 Spec (consume) | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | IFieldPolicy / FieldClass omit on search DTOs — do **not** rewrite |
| Stage A #32 Spec (separate surface) | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Owner inventory remains separate — discovery must **not** dump |
| PoC Artifact (#4) | `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md` | Discoverable Artifact fields already on path |
| PoC Participant (#5) | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Authn baseline — validated principal |
| Sibling #41 Spec — cross-ref only | `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md` | Strategy ACL out — **do not implement** |
| Sibling #42 Spec — cross-ref only | `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | ContactEmail ShareOutbound out — **do not implement** |
| Issue #40 | https://github.com/ioaikh/dealoware/issues/40 | Binding AC + OUT |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A · Stage B named slice; Spec/SD HOLD |
| Gate #25 (backlog) | https://github.com/ioaikh/dealoware/issues/25 | Do **not** open now |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO Stage B named slice — **P2** MVP **instant** search only. Saved-search / market monitoring → **V1**; complementary-intent matching (**A1**) → **V2**. Stage C HOLD. Gate #25 backlog. Conflicts → PM → Product → CEO. Cost/critical → CPM.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend the O10 modular monolith with an **authenticated instant-search / discovery** surface over **discoverable Artifact fields already on path** (PoC Artifact D1–D5 / MVP Artifact surface — e.g. Subject / Intent / Value / Location / Time class fields). Search results **must omit** denied FieldClasses via Stage A **#31** `IFieldPolicy` / serializer projection — explicitly **absent**: **StrategyBody**, **LoginEmail**, **ContactEmail**, private account lists / Strategy inventory, auth secrets. Discovery is a **separate surface** from **#32** owner inventory list/get — must **not** dump another Participant’s private inventory or secrets, and must **not** be implemented as “list all Artifacts then filter in UI.” Require **#5** validated principal; unauthenticated → **401**; wrong-principal / stranger misuse → fail-closed with **uniform deny bodies** and **no** private-field leakage. Deliver automated tests per Spec §7.1. **OUT:** saved-search, A1, #41 Strategy, #42 ContactEmail share, Stage C Assistant, opening gate **#25**, unlocking #18 Spec/SD, Cognito/MM/DC4, new Artifact schema, rewriting #31/#32. PoC/MVP **$0**. No product code in this artifact — SD instructions only. **#40 ONLY** — do not draft or implement #41/#42.

---

## 3. Locked decisions (Spec locks → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | SA Stage B discovery | Authenticated instant search; search payloads must **not** leak secrets; discovery ≠ Artifact owner inventory | Steps 1–4, 6–7 |
| 1 | Authn | Validated #5 principal; unauthenticated → **401**; no private-field leakage in errors | Steps 2, 5, 7 |
| 2 | Discoverable fields | Artifact fields already on path / allowed for discovery; **no new Artifact schema** for search | Steps 3–4 |
| 3 | Omit secrets | Serializers omit denied FieldClasses; absent: StrategyBody, LoginEmail, ContactEmail, private lists / Strategy inventory, auth secrets | Steps 4, 6–7 |
| 4 | Separate surface | Discovery ≠ #32 owner inventory; must **not** dump another Participant’s private inventory/secrets | Steps 1, 3, 6–7 |
| 5 | Uniform deny | Wrong-principal / stranger → fail-closed; uniform deny bodies; no private-field leakage | Steps 5, 7 |
| 6 | Consume #31 | Use Stage A `IFieldPolicy` / FieldClass for projection omit; do **not** rewrite #31 registry AC | Steps 4, 8 |
| 7 | Scope label | **P2** MVP **instant** only; saved-search → **V1**; A1 → **V2** | Steps 9–10; Explicit OUT |
| 8 | OUT | Saved-search; A1; #41; #42; Stage C; gate #25 open; #18 Spec/SD unlock; Cognito/MM/DC4; vault/KMS | Steps 9–10; Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 modular monolith — Spec §4).  
**Prerequisite:** Stage A #31 Field ACL + #32 account list fail-closed **CLOSED**; PoC Artifact (#4) + Participant auth (#5) delivered. Consume — do **not** rewrite those Specs or merge Stories.  
**Sibling HOLD:** #41 Minimal Strategy CRUD; #42 Contact on accept — **cross-ref only**; separate plans/Stories. **Do not draft or implement here.**  
**Hold:** Stage C; gate #25; unlocking #18 Spec/SD as a whole; Cognito/MM/DC4 — do **not** invent.

### Step 1 — Confirm host layout; discovery surface placement; keep health open

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**) if any host/DI touch is required for wiring.
- Place instant-search in **Application/Api search path** + Domain FieldPolicy projection (Spec §4) — **separate** from #32 owner inventory list/get endpoints.
- Do **not** implement discovery as “list all Artifacts then filter in UI/controller.”
- Continue Minimal APIs; built-in ASP.NET Core DI only.
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS spend modules, or MM/DC4 refs.
- Do **not** open gate #25, unlock #18 Spec/SD, or invent Stage C / #41 / #42 Stories.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none) if touched
- [ ] Discovery path documented as **separate surface** from #32 owner inventory
- [ ] No Cognito/IdP/AWS/MM provision tasks; gate #25 not opened; #18 Spec/SD not unlocked

### Step 2 — Authn fail-closed on instant-search paths (#5 required)

Per Spec §1 Authn + Locked #1; Security Dev Plan point **1**:

| Item | Plan lock |
|------|-----------|
| Principal | Validated **#5** (or successor) principal required on all instant-search paths |
| Unauthenticated / invalid | Fail closed **`401`**; error bodies omit private fields / contact/PII / FieldClass secrets / auth secrets |
| Auth header | Reuse #5 `Authorization` patterns — do **not** invent Cognito/SSO/password/cookie productization |
| Health | `GET /health` remains Auth none |

**Verify tasks (required):**

- [ ] Unauthenticated instant search → **401**; no private fields in body
- [ ] Invalid credential → fail closed **401**; no private leak
- [ ] Auth’d principal reaches search path (happy path wired)
- [ ] `GET /health` remains open

### Step 3 — Instant search over discoverable Artifact fields only (no new schema)

Per Spec §1 Discoverable fields + Locked #2; Security Dev Plan point **4**:

| Item | Plan lock |
|------|-----------|
| In | Artifact fields **already on path** and **allowed for discovery** (PoC Artifact D1–D5 / MVP Artifact CRUD surface — e.g. Subject / Intent / Value / Location / Time class fields already delivered or in-scope) |
| Out of invent | Do **not** invent new Artifact schema, product indexes as requirements, or matching semantics beyond **instant** search over those fields |
| Query | Search operates over discoverable fields of others’ Artifacts — **not** a dump of owner inventory |
| Separate from #32 | Do **not** call or reuse #32 owner list/get as the discovery implementation |

**Endpoints (plan intent — SD names to match host conventions):** authenticated instant-search route(s) under Api — **new discovery surface**, not an expansion of `GET /artifacts` owner list.

**Acceptance:**

- [ ] Auth’d Participant can run instant search over discoverable Artifact fields already on path
- [ ] No new Artifact schema / entity invented for search
- [ ] Discovery is **not** implemented as owner-inventory list-all + UI filter
- [ ] Search surface is **separate** from #32 `GET /artifacts` (list-own) / get-by-id owner paths

### Step 4 — Search payload omit secrets via #31 IFieldPolicy (consume, don’t rewrite)

Per Spec §1 Search payload omit + Locked #3/#6; Security Dev Plan points **3** and **6**:

| Must be **absent** from search payloads | Note |
|-----------------------------------------|------|
| **StrategyBody** | FieldClass — never in discovery results |
| **LoginEmail** | FieldClass — never |
| **ContactEmail** | FieldClass — never (share path is **#42** — out) |
| Private account lists / Strategy inventory | Not discovery |
| Auth secrets | Never in API bodies |

| Item | Plan lock |
|------|-----------|
| Mechanism | Consume Stage A **#31** `IFieldPolicy` / FieldClass for **serializer projection omit** on search DTOs |
| Strip | Denied classes stripped; do **not** return placeholders that leak |
| Do not | Rewrite #31 registry AC, FieldClass enum invent beyond consume, or merge #31 Story |
| Do not | Implement Strategy ACL (#41) or ContactEmail ShareOutbound (#42) under this Story |

**Acceptance:**

- [ ] Search result serializers project only discovery-allowed Artifact / negotiation-scoped fields
- [ ] Automated/manual inspect: **no** StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets in search payloads
- [ ] Omit via #31 `IFieldPolicy` — #31 Spec **not** rewritten
- [ ] No #41 / #42 implementation tasks executed under this Story

### Step 5 — Uniform deny / stranger misuse fail-closed (no leak)

Per Spec §1 Authn + Locked #5; Security Dev Plan point **5**:

| Item | Plan lock |
|------|-----------|
| Wrong-principal / stranger misuse | Fail-closed with **uniform deny bodies** |
| Leakage | **No** private fields / FieldClass secrets / other Participants’ inventory in errors |
| Align | #18 / #31 / #32 spirit — uniform deny; no existence leak via secret-bearing bodies |
| Do not invent | Multi-tenant admin discovery; Cognito productization |

**Verify tasks (required):**

- [ ] Stranger / wrong-principal misuse of discovery → fail-closed; uniform deny
- [ ] Deny bodies contain no StrategyBody / LoginEmail / ContactEmail / private lists / auth secrets / other Participants’ private fields
- [ ] Logging does not dump raw tokens / private payloads on deny paths

### Step 6 — Discovery ≠ #32 owner inventory (binding separation)

Per Spec §2 + Locked #4; Security Dev Plan point **2**:

| Surface | Plan lock |
|---------|-----------|
| #32 Artifact list/get | Owner-scoped inventory (`OwnerParticipantId` == principal) — **unchanged**; do **not** weaken |
| #40 Instant search | Separate authenticated discovery over **discoverable** Artifact fields of others |
| Cross-use forbid | Must **not** dump another Participant’s private account inventory or secrets |
| Implementation forbid | Must **not** be “list all Artifacts then filter in UI” |

**Acceptance:**

- [ ] #32 owner list/get behavior unchanged (fail-closed owner scope intact)
- [ ] Discovery responses do **not** return another Participant’s private owner inventory dump
- [ ] Discovery responses do **not** include secrets (ties Step 4 omit)
- [ ] Handoff notes document discovery ≠ inventory with cites to Spec §2 / #32

### Step 7 — Automated tests (binding Spec §7 / §7.1)

Schedule automated tests (or equivalent evidence) covering:

| Case | Expected |
|------|----------|
| Auth’d Participant OK | Instant search OK over discoverable Artifact fields already on path |
| Search payload omit | **No** StrategyBody / LoginEmail / ContactEmail / private account lists / Strategy inventory / auth secrets |
| Unauthenticated deny | No token / invalid → **401**; no private fields in errors |
| Inventory / secrets dump | Discovery does **not** return another Participant’s private owner inventory or secrets |
| Uniform deny (stranger misuse) | Wrong-principal / stranger → fail-closed; uniform deny; no private leak |

**Acceptance:**

- [ ] Test suite covers all Spec §7.1 cases (+ stranger uniform-deny)
- [ ] Failures assert status + **absence** of secret/private fields in payloads and deny bodies
- [ ] Tests exercise real projection/authz paths (not mocked-away omit that always strips everything)

### Step 8 — Consume #31 Field ACL; keep #32 separate; siblings cross-ref only

| Item | Plan lock |
|------|-----------|
| #31 | **Consume** `IFieldPolicy` for search DTO projection omit — do **not** rewrite #31 registry AC or merge Story |
| #32 | Owner inventory remains **separate** — do **not** weaken list fail-closed; do **not** implement discovery as inventory list |
| #41 | Strategy ACL / StrategyBody / Strategy CRUD — **OUT**; cross-ref only |
| #42 | ContactEmail ShareOutbound-after-Accept — **OUT**; cross-ref only |
| #5 / PoC Artifact | Authn + discoverable field baselines — consume; do **not** rewrite |
| #18 | Parent Spec/SD remains **HOLD** — do **not** unlock as a whole |

**Acceptance:**

- [ ] Plan/handoff cites #31 consume + #32 separate surface
- [ ] No #31 rewrite / #32 weaken / #41 / #42 / #18-unlock tasks under this Story

### Step 9 — Explicit OUT (do **not** implement Spec §6)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Saved search / market monitoring | Rest of P2 → **V1** |
| First-class complementary-intent matching (A1) | → **V2** |
| Strategy ACL / Strategy CRUD | **#41** — separate Story |
| ContactEmail ShareOutbound-after-Accept | **#42** — separate Story |
| Stage C agent/tool hard wall; thin Assistant runtime | HOLD |
| Gate **#25** open / unlock before Stage B delivery | Backlog — **do not open now** |
| Unlocking #18 Spec/SD as a whole; #26–#27 | HOLD |
| New Artifact schema for search | Out |
| Cognito/SSO / vault/KMS spend | Out |
| MotorMarket / DC4 | Zero |
| Rewriting #31 / #32 | Cross-ref / consume only |

### Step 10 — Secrets / cost / zero MM-DC4; local $0

**Secrets (reuse #5 / #31 patterns):**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings
- [ ] Connection strings + signing material = env / placeholders only
- [ ] No tokens in query strings or search bodies
- [ ] No logging of raw tokens / keys / credentials / secret FieldClass values
- [ ] No Cognito/SSO/IdP/vault SDK PackageReferences added by this Story

**Host / spend:**

- [ ] Remains **local / $0**
- [ ] ECS Express Mode **sketch only** if README already mentions — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource provision that creates spend
- [ ] If spend ever proposed → escalate **CPM → COO → CEO**

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, packages, shared libs, schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 11 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] Auth’d instant search over discoverable Artifact fields already on path (no new schema)
- [ ] #5 principal required; unauthenticated → **401** fail closed
- [ ] Search payloads omit StrategyBody / LoginEmail / ContactEmail / private lists / Strategy inventory / auth secrets via #31 IFieldPolicy
- [ ] Discovery surface **separate** from #32 owner inventory; no inventory/secrets dump
- [ ] Wrong-principal / stranger → uniform deny; no private leak
- [ ] Automated tests per Spec §7 / §7.1 (+ stranger deny)
- [ ] #31 consumed not rewritten; #32 not weakened; #41/#42 not implemented
- [ ] `GET /health` still Auth none if host touched
- [ ] Nothing from Step 9 / Spec §6 OUT implemented; Stage C not invented; gate #25 not opened; #18 Spec/SD not unlocked
- [ ] Secrets hygiene + zero MM/DC4 + local/$0; no Cognito
- [ ] #40 ONLY — no sibling Story drafts/impl; Product conflicts escalated if any
- [ ] No product code left undocumented; handoff notes cite Spec + this plan

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec Locked #0 / SA Stage B | Auth’d instant search; no secrets leak; discovery ≠ inventory | Steps 1–4, 6–7 |
| Spec §1 Authn + Locked #1 | #5 principal; unauth → 401; no private leak | Steps 2, 5, 7 |
| Spec §1 Discoverable fields + Locked #2 | Fields already on path; no new schema | Steps 3–4 |
| Spec §1 Search payload omit + Locked #3 | Absent StrategyBody / LoginEmail / ContactEmail / private lists / auth secrets | Steps 4, 7 |
| Spec §2 + Locked #4 | Discovery ≠ #32 owner inventory | Steps 1, 3, 6–7 |
| Spec Locked #5 | Uniform deny; stranger fail-closed | Steps 5, 7 |
| Spec §3 + Locked #6 | Consume #31 IFieldPolicy; don’t rewrite | Steps 4, 8 |
| Spec Locked #7/#8 + §6 OUT | P2 instant; V1/V2; #25 backlog; siblings out | Steps 9–10; Explicit OUT |
| Spec §4 Host / cost | Extend O10; local/$0; ECS sketch | Steps 1, 10 |
| Spec §5 Security Spec 1–10 | Upstream Spec Security bind | §6 Security table |
| Spec §7 row 1 / Issue AC | Auth’d instant search; no new schema | Steps 2–3, 7 |
| Spec §7 row 2 / Issue AC | Omit denied classes from search payloads | Steps 4, 7 |
| Spec §7 row 3 / Issue AC | Separate surface from #32; no dump | Steps 3, 6–7 |
| Spec §7 row 4 / Issue AC | Unauth 401; uniform deny; no leak | Steps 2, 5, 7 |
| Spec §7 row 5 / §7.1 / Issue AC | Automated tests | Step 7 |
| Spec §7 row 6 / Issue AC | P2 instant only; saved-search → V1; A1 → V2 | Steps 9–10; Explicit OUT |
| #31 Spec consume | IFieldPolicy projection omit | Steps 4, 8 |
| #32 Spec separate | Owner inventory unchanged | Steps 6, 8 |
| #5 Spec consume | Validated principal | Step 2 |
| #41 / #42 Specs cross-ref only | OUT — do not implement | Steps 8–9 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-qa-confirm.md` (points 1–10 MET; SoR PR #44)  
**Spec Security binding:** Spec §5 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Authn fail-closed tasks** — Plan schedules authenticated search only; unauth → 401; verify tests | Step 2 requires validated #5 principal on all instant-search paths; verify unauth → **401**; health stays open; Locked #1 | Steps 2, 7, 11; Locked #1 |
| 2 | **Discovery ≠ inventory tasks** — Plan keeps discovery surface separate from #32 owner list/get; no private inventory dump tasks | Steps 1, 3, 6 keep discovery separate; forbid list-all+UI-filter; #32 unchanged; Locked #4 | Steps 1, 3, 6, 11; Locked #4 |
| 3 | **Search payload omit-secrets tasks** — Plan schedules serializers omit StrategyBody / LoginEmail / ContactEmail / private lists / auth secrets | Step 4 omit table + #31 IFieldPolicy projection; Step 7 payload asserts; Locked #3 | Steps 4, 7, 11; Locked #3 |
| 4 | **Discoverable-fields-only tasks** — Plan limits search/result fields to allowed Artifact fields; no new Artifact schema | Step 3 limits to fields already on path; no schema invent; Locked #2 | Steps 3–4, 11; Locked #2 |
| 5 | **Uniform deny / no-leak tasks** — Plan includes stranger misuse fail-closed + uniform deny bodies; verify | Step 5 uniform deny + no private fields; Step 7 stranger case; Locked #5 | Steps 5, 7, 11; Locked #5 |
| 6 | **Consume #31 Field ACL** — Plan consumes IFieldPolicy for projection omit; does not rewrite #31 | Step 4 consume IFieldPolicy; Step 8 no rewrite; Locked #6 | Steps 4, 8, 11; Locked #6 |
| 7 | **No Stage C / #18 inventing** — No Assistant hard wall, Cognito, MM/DC4, or #18 Spec/SD tasks | Step 9 OUT; Step 10 forbid Cognito/MM; #18 HOLD; Locked #8 | Steps 9–10; Explicit OUT; Locked #8 |
| 8 | **OUT locked** — P2 instant only; saved-search → V1; A1 → V2; gate #25 backlog | Step 9 OUT table; Locked #7/#8; siblings #41/#42 cross-ref only | Steps 8–9; Explicit OUT; Locked #7/#8 |
| 9 | **Cost / spend** — PoC **$0**; no IdP/vault provision tasks | Step 10 local/$0; no IdP/vault; Cost/critical → CPM | Step 10; Cost/critical; Locked #8 |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS | Handshake note; Done-list Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-22__security__verification__mvp-stage-b-discovery-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Security QA confirms.** **SD stays HOLD** until Dev Plan QA + Security QA PASS + Chief Dev Planner unlock (+ CPM per ops).

---

## 7. Explicit OUT

Mirror Spec §6 / issue #40 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Saved search / market monitoring | Rest of **P2** → **V1** |
| First-class complementary-intent matching (A1) | → **V2** |
| Strategy ACL / Strategy CRUD | **#41** — separate Story/plan |
| ContactEmail ShareOutbound-after-Accept | **#42** — separate Story/plan |
| Stage C agent/tool hard wall; thin Assistant runtime | **HOLD** |
| Opening gate **#25** / SA-REV-MVP-B | Backlog until after Stage B delivery — **do not open now** |
| Unlocking #18 Spec/SD as a whole; #26–#27 | **HOLD** |
| New Artifact schema for search | Out |
| Cognito / SSO / IdP / vault / KMS spend | Out |
| MotorMarket / DC4 | Zero |
| Rewriting #31 / #32 | Consume / separate surface only |
| Drafting or implementing #41 / #42 in this plan | **Forbidden** — #40 ONLY |

---

## 8. Cost/critical

**#40 must not procure AWS / Cognito / IdP / vault spend.** Any paid AWS provision, Cognito/SSO/IdP spend, vault/KMS, or other spend proposal → escalate **CPM → COO → CEO**. Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, or vault tasks. ECS Express Mode remains **README sketch only** if present. Gate **#25** stays backlog — do not open. Do **not** unlock #18 Spec/SD as a whole.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-22__devplan__plan__mvp-stage-b-instant-search-discovery.md`
- [ ] Spec coverage §§1–6 + §7 AC + §7.1 + issue #40 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] **#40 ONLY** — #41/#42 **cross-ref only** (not drafted/implemented); Stage C **not** invented; gate #25 backlog; #18 Spec/SD HOLD
- [ ] Consumes #31 Field ACL (omit) + keeps #32 owner inventory separate — **no rewrite** / Story merge
- [ ] Auth’d instant search + omit-secrets + unauth 401 + uniform deny + automated tests scheduled
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] No Cognito/SSO/IdP/vault/AWS spend instructions
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner
- [ ] **SD HOLD** until Dev Plan QA + Security QA PASS + Chief unlock

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock (+ CPM per ops). Parent may run #41/#42 separately — **this plan does not draft them**.

---

## 10. Done-list for SD (after plan QA PASS + Security QA confirm + Chief unlock)

- [ ] Step 1: Confirm host; discovery surface separate from #32; keep `GET /health` Auth none if touched
- [ ] Step 2: #5 principal on instant-search; unauth → **401**; deny hygiene
- [ ] Step 3: Instant search over discoverable Artifact fields already on path; no new schema
- [ ] Step 4: Serializer omit via #31 IFieldPolicy — StrategyBody / LoginEmail / ContactEmail / private lists / auth secrets **absent**
- [ ] Step 5: Stranger / wrong-principal → uniform deny; no private leak
- [ ] Step 6: Discovery ≠ #32 owner inventory; no inventory/secrets dump; #32 unchanged
- [ ] Step 7: Automated tests per Spec §7 / §7.1 (+ stranger deny)
- [ ] Step 8: Consume #31 only; keep #32 separate; do **not** implement #41/#42; do **not** unlock #18
- [ ] Step 9: Do not implement Spec §6 OUT; do not invent Stage C; do not open #25
- [ ] Step 10: Secrets hygiene; local/$0; zero MM/DC4; no Cognito; ECS sketch only
- [ ] Step 11: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
