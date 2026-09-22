# Dev Plan — MVP Stage B: Minimal Strategy create/edit (#41)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-22  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner PRIORITY #41 ONLY  
**Issue:** https://github.com/ioaikh/dealoware/issues/41  
**DOC-FLOW:** `plans/2026-09-22__devplan__plan__mvp-stage-b-minimal-strategy-crud.md`  
**Constraints:** #41 ONLY — do not draft/implement #40 / #42; P3 minimal owner CRUD; StrategyBody User+OwnAgent only; Counterparty Deny; OwnAgent = API policy ≠ Assistant; OUT engine/Stage C/#40/#42; ECS Express sketch; PoC $0; no Cognito/MM/DC4; gate #25 backlog; do not unlock #18 whole; cost → CPM.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md` | Minimal Strategy CRUD + StrategyBody ACL; Locked #0–#9; §7 AC + §7.1 tests; OUT; Security §5 |
| Spec QA (Spec gate PASS) | `verification/2026-09-22__spec__verification__mvp-stage-b-minimal-strategy-crud.md` | Spec-side PASS; Security handshake cleared |
| Spec Security PASS | `verification/2026-09-22__security__verification__mvp-stage-b-strategy-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan (SoR PR #44) |
| Dev Plan Security checklist (must weave 1–10) | `verification/2026-09-22__security__verification__mvp-stage-b-strategy-devplan-checklist.md` | Dev Plan-step handshake points **1–10** |
| Option A architecture (Stage B) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | StrategyBody User + OwnAgent only; Counterparty Deny; Stage B Story split |
| Stage A #31 Spec (CLOSED) — consume | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | FieldClass + IFieldPolicy baseline; add StrategyBody rows — do **not** rewrite |
| Stage A #31 Dev Plan — consume patterns | `plans/2026-09-21__devplan__plan__mvp-stage-a-field-acl-registry.md` | Format sibling; Field ACL placement patterns |
| Stage A #32 Spec (CLOSED) — spirit | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Owner-scoped list/get spirit; do **not** rewrite |
| BA note #41 | `plans/2026-09-22__ba__note__story-41-minimal-strategy-crud.md` | Spec-ready refine; P3 minimal; OwnAgent ≠ Assistant |
| Sibling #40 Spec — cross-ref only | `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md` | Instant search — **do not implement in this plan** |
| Sibling #42 Spec — cross-ref only | `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | Contact ShareOutbound — **do not implement in this plan** |
| Issue #41 | https://github.com/ioaikh/dealoware/issues/41 | Binding AC + OUT |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A · Stage B named slice; #18 Spec/SD HOLD |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO Stage B named slice — **P3** MVP **minimal** Strategy CRUD. Fuller free-form → **V1**; A5 sandbox → **V4**; thin Assistant → Stage C / **X1**. Stage C HOLD. Gate **#25** backlog. Do **not** unlock #18 Spec/SD as a whole. Conflicts → PM → Product → CEO. Cost/critical → CPM.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend existing O10 modular monolith with **owner-scoped minimal Strategy CRUD** (create / edit / get / list-own) bound to owning Participant (`OwnerParticipantId` == principal `sub`, or equivalent) on the **query plane** — never UI-only filter. Encode **StrategyBody** FieldClass via Stage A `#31` `IFieldPolicy`: **User** R/W; **OwnAgent** R/W when acting for owner (**API policy row only** — **not** Assistant / tool runtime); **Counterparty / Stranger / Unauth Deny**. Shared 1:1 Negotiation DTOs **never** expose StrategyBody / private Strategy fields. Cross-tenant / IDOR fail-closed; unauth → **401**; wrong principal → **403** or **404** (PoC/#32 consistency OK); uniform deny; no private-field leakage. Minimal document may be opaque/text StrategyBody — do **not** invent free-form condition types / evaluation engine / A5 sandbox. Deliver automated tests per Spec §7.1. Consume `#31` registry (add StrategyBody rows) — do **not** rewrite `#31` / `#32`. **Cross-ref #40 / #42** — do **not** implement. **OUT:** fuller engine (V1); A5 (V4); thin Assistant runtime (Stage C / X1); gate #25 open; #18 Spec/SD unlock; Cognito/MM/DC4; vault/KMS. PoC/MVP **$0**. No product code in this artifact — SD instructions only.

---

## 3. Locked decisions (Spec locks → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | SA Stage B Strategy ACL | StrategyBody Owner + OwnAgent only; never Counterparty | Steps 2–4, 7 |
| 1 | Owner-scoped CRUD | Create/edit/get/list-own bound to owning Participant; query plane — never UI-only | Steps 3, 5, 6 |
| 2 | StrategyBody FieldClass | User R/W; OwnAgent R/W for owner; Counterparty/Stranger/Unauth Deny via IFieldPolicy | Steps 2, 4, 6 |
| 3 | Never to counterparty | Negotiation DTOs never expose StrategyBody / private Strategy | Steps 4, 7 |
| 4 | Fail-closed IDOR | Cross-tenant list/get + IDOR fail-closed; unauth 401; wrong principal 403/404; uniform deny | Steps 5, 6, 8 |
| 5 | OwnAgent = API policy only | OwnAgent StrategyBody ACL = API policy — **not** Assistant / tool runtime | Steps 2, 4, 9 |
| 6 | Consume #31 | Add StrategyBody rows on #31 registry; do not rewrite #31/#32 | Steps 1–2, 9 |
| 7 | Minimal representation | Opaque/text StrategyBody OK; no free-form engine inventing | Steps 3, 9 |
| 8 | Scope label | P3 MVP minimal; fuller → V1; A5 → V4; thin Assistant → Stage C/X1 | Steps 9–10; Explicit OUT |
| 9 | OUT | Engine; Assistant; #40; #42; gate #25; #18 unlock; Cognito/MM/DC4; vault/KMS | Steps 9–10; Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 modular monolith — Spec §4).  
**Prerequisite:** Stage A #31 Field ACL CLOSED + #32 account list fail-closed CLOSED; gate #24 CA PASS (FieldPolicy registry exists before StrategyBody rows). Consume #5 Participant auth patterns.  
**Sibling HOLD:** #40 Instant search · #42 Contact on accept — **cross-ref only**; separate plans/Stories.  
**Hold:** Stage C Assistant (#26) + #27; gate #25; #18 Spec/SD whole unlock; Cognito/MM/DC4 — do **not** invent.

### Step 1 — Confirm host layout; Domain StrategyBody FieldClass + Api/Application CRUD placement; keep health open

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**) if any host/DI touch is required for wiring.
- Place **StrategyBody FieldClass policy rows** on existing `#31` registry in **`Dealoware.Domain`** (policy source of truth — **consume**, do not rewrite #31 AC).
- Place owner-scoped Strategy CRUD (create/edit/get/list-own) in **Api / Application** (+ repository query constraints) — Spec §4.
- Do **not** implement Assistant / tool runtime, agent hard-wall (#26), free-form evaluation engine, or A5 sandbox.
- Built-in ASP.NET Core DI only; continue Minimal APIs.
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS spend modules, or MM/DC4 refs.
- Do **not** open gate #25 or unlock #18 Spec/SD as a whole.
- Do **not** implement #40 / #42 in this Story.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none) if touched
- [ ] StrategyBody policy rows live on #31 Domain registry; CRUD on Api/Application; **no** Assistant runtime
- [ ] No Cognito/IdP/AWS/MM provision tasks; gate #25 not opened; #18 Spec/SD not unlocked; #40/#42 not implemented

### Step 2 — Register StrategyBody FieldClass on #31 IFieldPolicy (consume, don’t rewrite)

Per Spec §2 + Locked #2/#5/#6; SA StrategyBody Stage B row:

| Item | Plan lock |
|------|-----------|
| FieldClass | `StrategyBody` (CEO example) registered on existing `#31` extensible registry |
| Interface | Enforce via `IFieldPolicy.Evaluate(principal, StrategyBody, action, resourceContext)` (or equivalent) on Strategy API/DB projection paths |
| Deny-by-default | Unknown classes remain #31 baseline — do not weaken |
| Do not | Rewrite #31 FieldClass AC, #32 AC, or dual-wall architecture |
| Do not | Invent additional Strategy FieldClasses beyond StrategyBody unless labeled illustrative |

**Acceptance:**

- [ ] `StrategyBody` registered on #31 FieldClass registry
- [ ] Evaluate invoked on Strategy create/edit/get/list-own + projection paths
- [ ] #31 / #32 Specs / ACs **not** rewritten
- [ ] No invented Strategy FieldClass product beyond StrategyBody / labeled illustrative

### Step 3 — Owner-scoped minimal Strategy CRUD (query plane)

Per Spec §1 Query plane + Locked #1/#7:

| Item | Plan lock |
|------|-----------|
| Operations | **Create**, **edit**, **get**, **list-own** a minimal Strategy document bound to owning Participant |
| Ownership | `OwnerParticipantId` == principal `sub` (or equivalent) on repository/query constraints |
| Query plane | Owner-scoped at DB/query layer — **never** “filter in UI only” |
| Document shape | Minimal opaque/text StrategyBody (or equivalent) — do **not** invent free-form condition types / evaluation runtime / A5 |
| Cross-tenant / IDOR | Fail-closed (no StrategyBody leakage) |

**Acceptance:**

- [ ] Owner can create / edit / get / list-own own Strategy
- [ ] List/get constrained at repository/query — not UI-only
- [ ] Minimal document only (opaque/text OK); no free-form engine
- [ ] Cross-tenant / other-owner IDOR attempts fail-closed (covered by Steps 5–6)

### Step 4 — Encode StrategyBody ACL rows (User + OwnAgent; Counterparty Deny)

Per Spec §2 + Locked #0/#2/#3/#5:

| Principal | StrategyBody | Plan lock |
|-----------|--------------|-----------|
| User (owner) | Read / Write | Allowed |
| OwnAgent (acting for owner) | Read / Write | Allowed as **API policy row only** — **not** Assistant runtime |
| Counterparty | Deny | Negotiation DTOs never carry StrategyBody |
| Stranger / Unauth | Deny | Fail-closed |

**OwnAgent lock:** OwnAgent StrategyBody R/W is **API policy** evidence only. Do **not** schedule Assistant / tool runtime delivery, thin Assistant, or Stage C / X1 tasks under this Story.

**Acceptance:**

- [ ] StrategyBody policy: User R/W; OwnAgent R/W for owner; Counterparty/Stranger/Unauth Deny
- [ ] OwnAgent path is API policy evaluation — no Assistant/tool runtime code
- [ ] Negotiation-scoped DTOs omit StrategyBody / private Strategy fields (verify Step 7)

### Step 5 — Authn fail-closed; uniform deny; no private-field leakage

Per Spec §1 Authn / deny hygiene + Locked #4:

| Item | Plan lock |
|------|-----------|
| Unauthenticated | → **401** |
| Wrong principal (other owner / stranger) | → **403** or **404** (PoC/#32 consistency OK) |
| Error bodies | Uniform deny; **no** StrategyBody / private Strategy fields / PII leakage |
| Authn | Validated #5 principal required on protected Strategy CRUD paths |
| Health | `GET /health` remains Auth none |
| Do not invent | Cognito/SSO/password/cookie/IdP productization inside #41 |

**Verify tasks (required):**

- [ ] Unauthenticated Strategy create/edit/get/list → **401**; no private fields
- [ ] Wrong principal / other-owner IDOR → **403** or **404**; uniform deny
- [ ] Error bodies contain no StrategyBody / private Strategy / contact/PII
- [ ] `GET /health` remains open

### Step 6 — Automated tests (binding Spec §7 / §7.1)

Schedule automated tests (or equivalent evidence):

| Case | Expect |
|------|--------|
| Owner | OK create / edit / get / list-own |
| OwnAgent (acting for owner) | Allowed StrategyBody Read/Write via **API policy row** — not Assistant runtime |
| Counterparty | StrategyBody / private Strategy **denied** on negotiation DTOs |
| Stranger | Strategy list/get **deny** |
| Unauthenticated | Deny (**401**); no private fields in errors |
| Cross-tenant / IDOR | Fail-closed; no StrategyBody leakage |

**Acceptance:**

- [ ] Test suite covers owner OK; OwnAgent API-policy allow; counterparty deny; stranger deny; unauth 401
- [ ] Failures assert status + **absence** of StrategyBody / private fields in deny / error bodies
- [ ] OwnAgent test proves API policy row — does **not** invent Assistant runtime harness

### Step 7 — Never-to-counterparty + negotiation DTO hygiene verify

Per Spec §3 + Locked #3; Security Dev Plan point 3:

Explicit verify pass:

- [ ] Shared 1:1 Negotiation / Offer views expose **negotiation-scoped party fields only**
- [ ] StrategyBody / private Strategy fields **absent** from counterparty DTOs (not null placeholders that leak)
- [ ] Counterparty automated case (Step 6) asserts absence
- [ ] Do **not** weaken #6 1:1 party-only rules
- [ ] Logging does not dump StrategyBody / private Strategy on deny / counterparty paths

### Step 8 — Owner query-plane / IDOR hygiene verify (Security points 1, 4)

Explicit verify pass:

- [ ] Create/edit/get/list-own enforced at repository/query — not UI-only filter
- [ ] Cross-tenant Strategy list/get fail-closed
- [ ] IDOR (guess other owner's Strategy id) → 403/404; no StrategyBody in body
- [ ] Unauth → 401; wrong principal → 403/404; uniform deny bodies

### Step 9 — Explicit OUT (do **not** implement Spec §6)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Fuller free-form Strategy conditions / evaluation engine | P3 fuller → **V1** |
| Strategy sandbox (A5) | → **V4** |
| Thin Assistant runtime as deliverable | Stage C / **X1** — OwnAgent ACL ≠ runtime |
| Instant search (#40) | Sibling — cross-ref only |
| ContactEmail ShareOutbound-after-Accept (#42) | Sibling — cross-ref only |
| Stage C agent/tool hard wall (#26); #27 | HOLD |
| Gate #25 open / unlock before Stage B delivery | Backlog |
| Unlocking #18 Spec/SD as a whole | HOLD |
| Cognito/SSO / vault/KMS spend | Out |
| MotorMarket / DC4 | Out |
| Rewriting #31 / #32 | Cross-ref / consume only |
| Inventing beyond Stage B named slice | Forbidden |

### Step 10 — Secrets / cost / zero MM-DC4; local $0; ECS Express sketch only

**Secrets (reuse #5 patterns):**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings
- [ ] Connection strings + signing material = env / placeholders only
- [ ] No tokens in query strings or Strategy bodies
- [ ] No logging of raw tokens / keys / credentials / StrategyBody private values
- [ ] No Cognito/SSO/IdP/vault SDK PackageReferences added by this Story

**Host / spend:**

- [ ] Remains **local / $0**
- [ ] ECS Express Mode **sketch only** if README already mentions — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource provision that creates spend
- [ ] If spend ever proposed → escalate **CPM → COO → CEO**

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, packages, shared libs, schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 11 — Cross-ref siblings #40 / #42 (do not implement)

| Item | Plan lock |
|------|-----------|
| #41 responsibility | Minimal owner Strategy CRUD + StrategyBody FieldClass ACL |
| #40 responsibility | Instant search / discovery — **separate Story/plan**; must omit StrategyBody (sibling Spec) |
| #42 responsibility | ContactEmail ShareOutbound-after-Accept — **separate Story/plan** |
| Do not | Implement search discovery or contact-share-on-accept under this Story |
| Do not | Merge #40 / #41 / #42 Stories or rewrite sibling Specs / plans |
| Cite | Sibling Specs `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md`, `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md`; issues https://github.com/ioaikh/dealoware/issues/40 · https://github.com/ioaikh/dealoware/issues/42 |

**Acceptance:**

- [ ] Plan/handoff notes cross-ref #40 / #42
- [ ] No #40 / #42 implementation tasks executed under this Story
- [ ] #31/#32 consumed only (not rewritten)

### Step 12 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] Owner-scoped create/edit/get/list-own at query plane (not UI-only)
- [ ] StrategyBody on #31 IFieldPolicy: User R/W; OwnAgent R/W for owner (API policy only); Counterparty/Stranger/Unauth Deny
- [ ] Negotiation DTOs never carry StrategyBody
- [ ] Unauth → 401; wrong principal → 403/404; uniform deny; no private-field leakage
- [ ] Automated tests per Spec §7.1 (owner OK; OwnAgent API policy; counterparty deny; stranger deny; unauth deny)
- [ ] OwnAgent ≠ Assistant runtime (no thin/full Assistant / tool code)
- [ ] #31/#32 not rewritten; #40/#42 not implemented
- [ ] Minimal document only — no free-form engine / A5 sandbox
- [ ] `GET /health` still Auth none if host touched
- [ ] Nothing from Step 9 / Spec §6 OUT implemented; Stage C not invented; gate #25 not opened; #18 Spec/SD not unlocked
- [ ] Secrets hygiene + zero MM/DC4 + local/$0; no Cognito; ECS Express sketch only
- [ ] No invent Stories; Product conflicts escalated if any
- [ ] No product code left undocumented; handoff notes cite Spec + this plan

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec Locked #0 / SA Stage B | StrategyBody Owner + OwnAgent only; never Counterparty | Steps 2–4, 7 |
| Spec §1 Query plane + Locked #1 | Owner-scoped create/edit/get/list-own; never UI-only | Steps 3, 5, 8 |
| Spec §1 Minimal document + Locked #7 | Opaque/text OK; no free-form engine | Steps 3, 9 |
| Spec §1 Authn + Locked #4 | Unauth 401; wrong principal 403/404; uniform deny | Steps 5, 6, 8 |
| Spec §2 + Locked #2 | StrategyBody User R/W; OwnAgent R/W; Counterparty/Stranger/Unauth Deny | Steps 2, 4, 6 |
| Spec Locked #5 | OwnAgent = API policy only (≠ Assistant) | Steps 2, 4, 6, 9 |
| Spec §3 + Locked #3 | Negotiation DTOs never expose StrategyBody | Steps 4, 7 |
| Spec Locked #6 | Consume #31; don’t rewrite #31/#32 | Steps 1–2, 11 |
| Spec §3 Cross-refs | #40 / #42 separate; Stage C HOLD | Steps 9, 11 |
| Spec §4 Host / cost | Extend O10 Domain + Api/Application; local/$0; ECS Express sketch | Steps 1, 10 |
| Spec §5 Security Spec 1–10 | Upstream Spec Security bind | §6 Security table |
| Spec §6 Explicit OUT | Engine; Assistant; #40/#42; #25; #18; Cognito; MM/DC4 | Steps 9–10; Explicit OUT |
| Spec §7 / Issue AC: Owner CRUD minimal | Create/edit/get/list-own; query plane | Steps 3, 6, 8 |
| Spec §7 / Issue AC: StrategyBody via IFieldPolicy | User R/W; OwnAgent R/W; Counterparty Deny; Stranger/Unauth Deny | Steps 2, 4, 6 |
| Spec §7 / Issue AC: Never to counterparty | Negotiation DTOs omit StrategyBody | Steps 4, 7 |
| Spec §7 / Issue AC: IDOR fail-closed; 401/403/404; uniform deny | Authn + IDOR hygiene | Steps 5, 6, 8 |
| Spec §7 / Issue AC: Automated tests | Owner OK; OwnAgent API policy; counterparty; stranger; unauth | Step 6 |
| Spec §7 / Issue AC: P3 minimal; V1/V4; Stage C/X1 OUT | Scope + OUT | Steps 9–10; Explicit OUT |
| Spec §7.1 Automated tests detail | Binding cases | Step 6 |
| #31 Spec consume | Add StrategyBody rows | Steps 1–2 |
| #32 Spec spirit | Owner-scoped list complement | Steps 3, 8 |
| #40 / #42 Spec cross-ref only | Separate Stories | Step 11 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-spec-qa-confirm.md` (points 1–10 MET; SoR PR #44)  
**Spec Security binding:** Spec §5 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Owner-scoped query-plane tasks** — Plan schedules create/edit/get/list-own with owner query plane; IDOR fail-closed tasks | Step 3 owner-scoped CRUD at repository/query; Steps 5/8 IDOR + authn verify; Locked #1/#4 | Steps 3, 5, 6, 8, 12; Locked #1/#4 |
| 2 | **StrategyBody FieldClass ACL tasks** — Plan schedules IFieldPolicy rows: User R/W; OwnAgent R/W for owner; Counterparty/Stranger/Unauth Deny; verify | Steps 2+4 register + encode StrategyBody rows; Step 6 tests; Locked #2 | Steps 2, 4, 6, 12; Locked #2 |
| 3 | **Never-to-counterparty tasks** — Plan requires negotiation DTOs never expose StrategyBody; verify | Step 4 Counterparty Deny; Step 7 DTO hygiene verify; Step 6 counterparty test; Locked #3 | Steps 4, 6, 7, 12; Locked #3 |
| 4 | **Authn fail-closed tasks** — Unauth 401; wrong principal 403/404; uniform deny bodies; verify | Step 5 authn + deny hygiene; Steps 6/8 verify; Locked #4 | Steps 5, 6, 8, 12; Locked #4 |
| 5 | **OwnAgent = API policy only** — Plan allows OwnAgent StrategyBody as API policy — **no** Assistant/tool runtime tasks (Stage C out) | Steps 2/4 OwnAgent R/W as API policy; Step 6 OwnAgent API-policy test; Step 9 OUT forbids Assistant; Locked #5 | Steps 2, 4, 6, 9, 12; Locked #5; Explicit OUT |
| 6 | **Consume #31, don’t rewrite** — Plan adds StrategyBody enforcement on #31 registry; keep #40/#42 separate | Steps 1–2 consume #31; Step 11 #40/#42 cross-ref only; Locked #6 | Steps 1–2, 9, 11, 12; Locked #6; Sources |
| 7 | **No Stage C / Assistant inventing** — No thin/full Assistant, #26 hard wall, Cognito, MM/DC4 tasks | Step 9 OUT table; Step 10 forbid Cognito/MM; Locked #9 | Steps 9–10; Explicit OUT; Locked #8/#9 |
| 8 | **OUT locked** — P3 minimal; free-form → V1; A5 sandbox → V4; X1 Assistant → Stage C; gate #25 backlog | Step 9 OUT; Step 3 minimal document; gate #25 not opened; Locked #8/#9 | Steps 3, 9–10, 12; Explicit OUT; Locked #8/#9 |
| 9 | **Cost / spend** — PoC **$0**; no IdP/vault provision tasks | Step 10 local/$0; ECS Express sketch only; no IdP/vault; Cost/critical → CPM | Step 10; Cost/critical; Locked #9 |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS; SD HOLD | Handshake note; Done-list Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-22__security__verification__mvp-stage-b-strategy-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Security QA confirms.** **SD stays HOLD** until Dev Plan QA + Security PASS + CPM unlock.

---

## 7. Explicit OUT

Mirror Spec §6 / issue #41 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Fuller free-form Strategy conditions / evaluation engine | P3 fuller → **V1** |
| Strategy sandbox (A5) | → **V4** |
| Thin Assistant runtime as deliverable | Stage C / **X1** — OwnAgent ACL ≠ runtime |
| Instant search (#40) | Sibling — cross-ref only |
| ContactEmail ShareOutbound-after-Accept (#42) | Sibling — cross-ref only |
| Stage C agent/tool hard wall (#26); #27 | HOLD |
| Gate **#25** open / unlock before Stage B delivery | Backlog — **do not open now** |
| Unlocking #18 Spec/SD as a whole | HOLD |
| Cognito / SSO / IdP / vault / KMS spend | Out |
| MotorMarket / DC4 | Zero |
| Rewriting #31 / #32 | Cross-ref / consume only |
| Inventing beyond Stage B named slice | Forbidden |

---

## 8. Cost/critical

**#41 must not procure AWS / Cognito / IdP / vault spend.** Any paid AWS provision, Cognito/SSO/IdP spend, vault/KMS, or other spend proposal → escalate **CPM → COO → CEO**. Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, or vault tasks. ECS Express Mode remains **README sketch only** if present. Gate **#25** stays backlog — do not open. Do **not** unlock #18 Spec/SD as a whole. Cost/critical → **CPM**.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-22__devplan__plan__mvp-stage-b-minimal-strategy-crud.md`
- [ ] Spec coverage §§1–6 + §7 AC / §7.1 + issue #41 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] **#41 ONLY** — #40 / #42 **cross-ref only** (not implemented); Stage C **not** invented; gate #25 backlog; #18 Spec/SD HOLD
- [ ] P3 minimal owner CRUD; StrategyBody User+OwnAgent (API policy only); Counterparty Deny; OwnAgent ≠ Assistant
- [ ] Owner query plane + IDOR fail-closed + authn 401/403|404 + negotiation DTO omit + automated tests scheduled
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] No Cognito/SSO/IdP/vault/AWS spend instructions
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner
- [ ] **SD HOLD** until Dev Plan QA + Security PASS + CPM unlock

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock (+ CPM per ops). Siblings #40 / #42 remain separate plans — this plan does not draft or implement #40 / #42.

---

## 10. Done-list for SD (after plan QA PASS + Chief confirm)

- [ ] Step 1: Confirm host; Domain StrategyBody on #31 + Api/Application CRUD placement; keep `GET /health` Auth none if touched; no Assistant runtime
- [ ] Step 2: Register StrategyBody FieldClass on #31 IFieldPolicy (consume, don’t rewrite)
- [ ] Step 3: Owner-scoped minimal Strategy create/edit/get/list-own (query plane; opaque/text OK)
- [ ] Step 4: Encode StrategyBody ACL (User R/W; OwnAgent R/W API policy only; Counterparty/Stranger/Unauth Deny)
- [ ] Step 5: Authn fail-closed; unauth → 401; wrong principal → 403/404; uniform deny; no private fields
- [ ] Step 6: Automated tests — owner OK; OwnAgent API policy; counterparty deny; stranger deny; unauth deny; IDOR fail-closed
- [ ] Step 7: Never-to-counterparty + negotiation DTO hygiene verify
- [ ] Step 8: Owner query-plane / IDOR hygiene verify
- [ ] Step 9: Do not implement Spec §6 OUT; do not invent Stage C / Assistant; do not open #25; do not unlock #18; do not implement #40/#42
- [ ] Step 10: Secrets hygiene; local/$0; ECS Express sketch only; zero MM/DC4; no Cognito
- [ ] Step 11: Cross-ref #40 / #42 only — do **not** implement
- [ ] Step 12: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
