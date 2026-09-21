# Dev Plan — MVP Stage A Field ACL registry + API projection (#31)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-21  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner PRIORITY #31 ONLY  
**Issue:** https://github.com/ioaikh/dealoware/issues/31  
**DOC-FLOW:** `plans/2026-09-21__devplan__plan__mvp-stage-a-field-acl-registry.md`  
**Constraints:** #31 only; #32 separate cross-ref; API/DB projection only; agent hard-wall Stage C HOLD; #7 stub unchanged; Stage B/C + #24 HOLD; PoC $0; no Cognito/MM/DC4; cost → CPM.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | FieldClass + IFieldPolicy + API projection; Locked #0–#8; §7 AC + §7.1 tests; OUT; Security §5 |
| Spec QA (Spec-side PASS; Security handshake cleared) | `verification/2026-09-21__spec__verification__mvp-stage-a-field-acl-registry.md` | Spec-side bind (bounce amend v2 cleared) |
| Spec Security PASS | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan |
| Dev Plan Security checklist (must weave 1–10) | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-checklist.md` | Dev Plan-step handshake points **1–10** |
| SA Pick A (Stage A) | `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md` | Domain FieldClass + IFieldPolicy + API projection (Pick A) |
| Option A parent | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall; open-ended FieldClass; #18 |
| Participant Spec (#5) — consume authn | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Validated principal on protected projection paths; do **not** rewrite |
| Negotiation Spec (#6) — consume / leave party rules | `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md` | Party-only rules remain; Field ACL does not weaken them |
| Identity-seal Spec (#7) — leave stub unchanged | `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md` | **#7 stub unchanged** — no contact release inventing |
| Sibling #32 Spec — cross-ref only | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Row isolation complement; **do not implement in this plan** |
| Sibling #32 Dev Plan — cross-ref only | `plans/2026-09-21__devplan__plan__mvp-stage-a-account-list-fail-closed.md` | Separate Story/plan; format sibling |
| BA note #31 | `plans/2026-09-21__ba__note__story-31-field-acl-registry-api-projection.md` | Open-ended FieldClass; API/DB half only; #7 untouched |
| Issue #31 | https://github.com/ioaikh/dealoware/issues/31 | Binding AC + OUT |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A · Stage A |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO Stage A named slice — Field ACL registry + API projection (generic any account info). Complements #32 account list fail-closed (separate Story). Stage B/C HOLD. Gate #24 backlog until after Stage A delivery. Soft DisplayName **non-blocking**. Conflicts → PM → Product → CEO. Cost/critical → CPM.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend existing O10 modular monolith with an **extensible `FieldClass` registry** and **`IFieldPolicy.Evaluate(principal, fieldClass, action, resourceContext)`** (or equivalent) applied on **API/DB projection** paths only (serializers / response mappers / DB read shaping). Denied fields are **omitted** from DTOs (no placeholder leak). **Deny-by-default** for unknown/unregistered FieldClass. Encode Stage A policy rows: **LoginEmail** User R/W + **OwnAgent Deny** (all actions) on API/DB; **ContactEmail** User R/W + OwnAgent Read + counterparty Deny + **ShareOutbound Deny** (Stage B HOLD). Soft **DisplayName** illustrative / **non-blocking** if not projected. Require **#5** validated principal on protected projection paths; unauthenticated → **401/403**; error bodies omit private fields. Deliver automated tests per Spec §7.1. **Cross-ref #32** for row isolation — **do not implement #32** here. Leave **#7** identity-seal stub unchanged. **OUT:** Stage B/C, Strategy ACL, agent hard-wall impl, opening gate #24, Cognito/MM/DC4, inventing beyond CEO slice. PoC/MVP **$0**. No product code in this artifact — SD instructions only.

---

## 3. Locked decisions (Spec locks → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | SA Pick A | Domain `FieldClass` registry + `IFieldPolicy.Evaluate` + API/DTO projection filters | Steps 1–4 |
| 1 | FieldClass registry | Extensible for **any** account-info class (open-ended; not email-only) | Steps 2, 3 |
| 2 | IFieldPolicy | Evaluate on **API/DB projection** paths; omit denied fields from DTOs | Steps 3–5 |
| 3 | Dual wall | Option A end-state accepted; Stage A implements **API layer only**; agent/tool hard wall = Stage C HOLD | Steps 1, 9; Explicit OUT |
| 4 | Examples | `LoginEmail`, `ContactEmail` starters; `DisplayName` soft/illustrative — **non-blocking** if deferred | Steps 2, 4 |
| 5 | Deny-by-default | Unknown/unregistered FieldClass → **deny** projection | Steps 3, 6 |
| 6 | LoginEmail | User Read/Write; **OwnAgent Deny** on API/DB (even before Stage C tools) | Steps 4, 6 |
| 7 | ContactEmail | User R/W; OwnAgent Read; counterparty Deny; ShareOutbound Deny until Stage B; **#7 stub unchanged** | Steps 4, 8, 9 |
| 8 | OUT | Strategy ACL; ContactEmail share-after-Accept; agent hard-wall; Stage B/C; gate #24 open; Cognito/MM/DC4; inventing beyond CEO slice | Steps 9–10; Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 modular monolith — Spec §4).  
**Prerequisite:** PoC #5 Participant auth, #6 Negotiation/Offers, #7 identity-seal stub delivered (consume / leave stub — do **not** rewrite those Specs or merge Stories).  
**Sibling HOLD:** #32 Account list fail-closed — **cross-ref only**; separate plan/Story.  
**Hold:** Stage B/C; agent hard-wall impl; gate #24; Cognito/MM/DC4 — do **not** invent.

### Step 1 — Confirm host layout; Domain policy + Api/Application projection placement; keep health open

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**) if any host/DI touch is required for wiring.
- Place **`FieldClass` registry + `IFieldPolicy`** in **`Dealoware.Domain`** (policy source of truth).
- Place projection / omit-denied filters in **Api serializers / response mappers** and **Application** (and DB read shaping where projection happens) — Stage A **API/DB only**.
- Do **not** implement agent/tool gateway, tool scrubbers, or Stage C hard-wall code in this Story.
- Built-in ASP.NET Core DI only; continue Minimal APIs.
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS spend modules, or MM/DC4 refs.
- Do **not** open gate #24 or invent Stage B/C Stories.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none) if touched
- [ ] Policy lives in Domain; projection filters on API/DB paths (documented); **no** agent hard-wall impl
- [ ] No Cognito/IdP/AWS/MM provision tasks; gate #24 not opened

### Step 2 — Extensible FieldClass registry (open-ended; CEO examples as starters)

Per Spec §1 Registry + Locked #1/#4; SA Pick A §3a:

| Item | Plan lock |
|------|-----------|
| Shape | Extensible registry — **not** a forever-closed enum |
| Growth | New secret types later = new FieldClass + policy rows **without** rewriting the dual wall |
| Starters (CEO examples, **not** exhaustive) | `LoginEmail`, `ContactEmail` |
| Soft / illustrative | `DisplayName` — **non-blocking** if not projected yet; do not invent public profile product |
| Inventing | Do **not** invent additional named FieldClasses beyond CEO examples unless labeled **illustrative** / derived from existing Stage A DTO fields |

**Acceptance:**

- [ ] Registry supports registering new FieldClasses without rewriting dual-wall architecture
- [ ] `LoginEmail` and `ContactEmail` registered as starters
- [ ] `DisplayName` either soft-registered or explicitly deferred (**non-blocking**)
- [ ] No invented FieldClass product names beyond CEO examples / labeled illustrative

### Step 3 — IFieldPolicy.Evaluate on API/DB projection only; omit denied fields; deny-by-default

Per Spec §1 Policy evaluation + API/DB projection + Locked #2/#5:

| Item | Plan lock |
|------|-----------|
| Interface | `IFieldPolicy.Evaluate(principal, fieldClass, action, resourceContext)` (or equivalent) |
| Actions | At least `Read` / `Write` / `List`; `ShareOutbound` may stub **Deny** in Stage A |
| Context | Carries ownership/party/accept-grant signals as needed for evaluation |
| Where | Serializers / response mappers / DB read shaping where projection happens |
| Effect | Fields denied for the caller are **omitted** (not returned with placeholders that leak) |
| Deny-by-default | Unregistered / unknown FieldClass → **deny** projection (no silent allow) |
| Scope | **API/DB projection only** — no agent gateway code |

**Acceptance:**

- [ ] `IFieldPolicy.Evaluate` (or equivalent) exists and is invoked on Stage A projection paths
- [ ] Denied fields omitted from DTOs / responses
- [ ] Unknown/unregistered FieldClass → deny (covered by tests in Step 6)
- [ ] No agent/tool hard-wall implementation

### Step 4 — Encode Stage A policy rows (LoginEmail / ContactEmail; DisplayName soft)

Per Spec §2 example policy rows + Locked #6/#7:

| FieldClass | User | OwnAgent | Counterparty / stranger | ShareOutbound |
|------------|------|----------|-------------------------|---------------|
| LoginEmail | R/W | **Deny** (all actions) | Deny | Deny |
| ContactEmail | R/W | Read | Deny | **Deny** (Stage B HOLD) |
| DisplayName | Soft / illustrative — **non-blocking** if not projected | Soft | Soft | N/A |

**LoginEmail locks:**

- User Read/Write allowed on API projection for owner/User principal
- **OwnAgent Deny all** on API/DB projection (even before Stage C tools exist — so later tools cannot "forget")
- Counterparty / Stranger / Unauth Deny

**ContactEmail locks:**

- User Read/Write; OwnAgent **Read** allowed by policy
- Counterparty / Stranger Deny
- **ShareOutbound Deny** in Stage A (share-after-Accept = Stage B HOLD)
- Do **not** rewrite / expand PoC **#7** identity-seal stub

**Acceptance:**

- [ ] LoginEmail policy rows: User R/W; OwnAgent Deny; counterparty/stranger Deny
- [ ] ContactEmail policy rows: User R/W; OwnAgent Read; counterparty Deny; ShareOutbound Deny
- [ ] DisplayName soft / non-blocking if deferred
- [ ] #7 stub unchanged (no contact-release inventing)

### Step 5 — Authn fail-closed on protected projection paths (#5); omit private fields in errors

Per Spec §1 API/DB projection + Locked (authn); Spec Security point 6:

| Item | Plan lock |
|------|-----------|
| Authn | Validated **#5** principal required on protected projection paths that emit FieldClass-bearing DTOs |
| Unauthenticated / invalid | Fail closed **`401`/`403`** — bodies omit private fields / contact/PII / FieldClass secret values |
| Denied fields | Omitted from success DTOs; never echoed in error bodies |
| Health | `GET /health` remains Auth none |
| Do not invent | Cognito/SSO/password/cookie/IdP productization inside #31 |

**Verify tasks (required):**

- [ ] Unauthenticated calls to protected projection paths → 401/403; no private fields
- [ ] Invalid credential → fail closed
- [ ] Error bodies contain no LoginEmail / ContactEmail / other FieldClass secret values / contact/PII
- [ ] `GET /health` remains open

### Step 6 — Automated tests (binding Spec §7 / §7.1)

Schedule automated tests (or equivalent evidence):

| Case | Expect |
|------|--------|
| Owner / User | Allowed fields project OK under policy |
| OwnAgent | **Denied** LoginEmail (all actions) on API projection |
| Stranger / counterparty | Denied protected fields (LoginEmail, ContactEmail, etc.) |
| Unauthenticated | Deny (401/403); no private fields in errors |
| Deny-by-default | Unknown/unregistered FieldClass → deny projection |
| ContactEmail OwnAgent | OwnAgent **Read** allowed; ShareOutbound still Deny |
| Soft DisplayName | Non-blocking if not yet projected |

**Acceptance:**

- [ ] Test suite covers owner OK; OwnAgent denied LoginEmail; stranger/counterparty denied; unauth deny
- [ ] Deny-by-default for unknown FieldClass covered
- [ ] Failures assert status + **absence** of private/secret fields in deny / error bodies
- [ ] Soft DisplayName non-blocking (skip or soft-assert if deferred)

### Step 7 — Projection omit / error hygiene verify (Security points 3–6)

Explicit verify pass:

- [ ] Denied fields are **absent** from success DTOs (not null placeholders that leak shape + value)
- [ ] 401/403/deny error responses omit private fields, contact/PII, tokens, FieldClass secret values
- [ ] Unknown FieldClass never silently allows projection
- [ ] Logging does not dump raw tokens / private field values on deny paths

### Step 8 — Cross-ref #32 account list fail-closed (do not implement #32)

| Item | Plan lock |
|------|-----------|
| #31 responsibility | **Which fields** project on allowed rows (`FieldClass` / `IFieldPolicy` on API/DB) |
| #32 responsibility | **Which rows** appear on list/get (owner/party query-plane isolation) — **separate Story/plan** |
| Do not | Replace Field ACL with list isolation alone (or vice versa) |
| Do not | Implement Artifact/Negotiation/Offer list+get fail-closed filters in this #31 plan |
| Do not | Weaken #6 1:1 party-only rules |
| Do not | Merge #31 and #32 Stories or rewrite #32 Spec / plan |
| Cite | Sibling Spec `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md`; sibling plan `plans/2026-09-21__devplan__plan__mvp-stage-a-account-list-fail-closed.md`; issue https://github.com/ioaikh/dealoware/issues/32 |

**Acceptance:**

- [ ] Plan/handoff notes cross-ref #32 for row isolation
- [ ] No #32 list fail-closed implementation tasks executed under this Story
- [ ] #6 party rules remain intact; Field ACL does not weaken them

### Step 9 — Explicit OUT (do **not** implement Spec §6)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Strategy ACL / Strategy body | Stage B HOLD |
| ContactEmail ShareOutbound-after-Accept | Stage B HOLD |
| Agent / tool hard wall + response scrubbers | Stage C HOLD (architecture accepted; **not** impl now) |
| Opening gate **#24** / SA-REV-MVP-A | Backlog until **after** Stage A delivery — do **not** open now |
| Cognito / SSO / IdP / vault / KMS spend | Out |
| MotorMarket / DC4 | Zero |
| Expanding / rewriting PoC **#7** seal stub | **Unchanged** |
| Implementing **#32** list fail-closed in this Story | Cross-ref only — separate plan |
| Rewriting #5 / #6 / #7 Specs or merging Stories | Cross-ref / consume patterns only |
| Inventing FieldClasses beyond CEO examples (unless labeled illustrative) | Forbidden |
| Stage B/C inventing | Forbidden |

### Step 10 — Secrets / cost / zero MM-DC4; local $0

**Secrets (reuse #5 patterns):**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings
- [ ] Connection strings + signing material = env / placeholders only
- [ ] No tokens in query strings or projection bodies
- [ ] No logging of raw tokens / keys / credentials / private FieldClass values
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

- [ ] Extensible FieldClass registry + IFieldPolicy in Domain; projection omit on API/DB only
- [ ] LoginEmail: User R/W; OwnAgent Deny on API/DB verified
- [ ] ContactEmail: User R/W; OwnAgent Read; counterparty Deny; ShareOutbound Deny; #7 stub unchanged
- [ ] Deny-by-default for unknown FieldClass + tests
- [ ] Soft DisplayName non-blocking if deferred
- [ ] #5 principal required on protected projection paths; unauthenticated → 401/403; no private fields in errors
- [ ] Automated tests per Spec §7.1 (owner OK; OwnAgent denied LoginEmail; stranger/counterparty denied; unauth deny)
- [ ] Projection omit / error hygiene verified
- [ ] #32 cross-ref only — list fail-closed **not** implemented in this Story
- [ ] Agent hard-wall **not** implemented (Stage C HOLD); Strategy ACL **not** implemented (Stage B HOLD)
- [ ] `GET /health` still Auth none if host touched
- [ ] Nothing from Step 9 / Spec §6 OUT implemented; Stage B/C not invented; gate #24 not opened
- [ ] Secrets hygiene + zero MM/DC4 + local/$0; no Cognito
- [ ] #5–#7 consumed / left stub only (not rewritten); no invent Stories; Product conflicts escalated if any
- [ ] No product code left undocumented; handoff notes cite Spec + this plan

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec Locked #0 / SA Pick A | Domain FieldClass + IFieldPolicy + API projection | Steps 1–4 |
| Spec §1 Registry + Locked #1/#4 | Extensible open-ended FieldClass; CEO examples; DisplayName soft | Step 2 |
| Spec §1 Policy + Locked #2/#5 | Evaluate on API/DB; deny-by-default | Step 3 |
| Spec §1 API/DB projection | Omit denied fields; #5 authn; 401/403; no private in errors | Steps 3, 5, 7 |
| Spec §2 + Locked #6 | LoginEmail User R/W; OwnAgent Deny | Steps 4, 6 |
| Spec §2 + Locked #7 | ContactEmail OwnAgent Read; ShareOutbound Deny; #7 unchanged | Steps 4, 8, 9 |
| Spec §3 Cross-refs | Complement #32; keep #6/#7 | Step 8 |
| Spec §4 Host / cost | Extend O10 Domain + Api/Application; local/$0 | Steps 1, 10 |
| Spec §5 Security Spec 1–10 | Upstream Spec Security bind | §6 Security table |
| Spec §6 Explicit OUT | Stage B/C; agent wall; #24; Cognito; MM/DC4; #7 | Steps 9–10; Explicit OUT |
| Spec §7 / Issue AC: Extensible FieldClass registry | Open-ended any account-info | Step 2 |
| Spec §7 / Issue AC: IFieldPolicy.Evaluate + deny-by-default on API/DB | Projection + unknown deny | Steps 3, 6 |
| Spec §7 / Issue AC: CEO examples LoginEmail / ContactEmail / DisplayName soft | Starters; DisplayName non-blocking | Steps 2, 4 |
| Spec §7 / Issue AC: LoginEmail User R/W; OwnAgent Deny | Policy + tests | Steps 4, 6 |
| Spec §7 / Issue AC: ContactEmail rules; ShareOutbound Deny; #7 unchanged | Policy + OUT | Steps 4, 8, 9 |
| Spec §7 / Issue AC: Omit denied fields; no private in errors | Projection hygiene | Steps 3, 5, 7 |
| Spec §7 / Issue AC: Any-account-info API-layer; no agent gateway | Dual wall Stage A half | Steps 1, 9 |
| Spec §7.1 Automated tests | Owner OK; OwnAgent denied LoginEmail; stranger; unauth | Step 6 |
| #5 Spec consume | Validated principal | Step 5 |
| #6 Spec leave party rules | Do not weaken | Step 8 |
| #7 Spec leave stub | Unchanged | Steps 4, 9 |
| #32 Spec / plan cross-ref only | Row isolation separate | Step 8 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-qa-confirm.md` (points 1–10 MET)  
**Spec Security binding:** Spec §5 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **API/DB scope tasks** — Plan schedules FieldClass registry + `IFieldPolicy` on API/DB projection only; agent hard-wall **impl** remains Stage C HOLD | Steps 1–3 place Domain policy + Api/Application projection; Step 9 OUT forbids agent hard-wall; Locked #3 | Steps 1–3, 9, 11; Locked #3; Explicit OUT |
| 2 | **Open-ended FieldClass registry tasks** — Plan requires extensible registry (not email-only); LoginEmail/ContactEmail as examples | Step 2 extensible registry; CEO starters; DisplayName soft non-blocking; Locked #1/#4 | Steps 2, 4; Locked #1/#4 |
| 3 | **Deny-by-default tasks** — Plan includes unknown/unregistered FieldClass → deny projection; verify tests | Step 3 deny-by-default; Step 6 tests; Step 7 hygiene; Locked #5 | Steps 3, 6, 7, 11; Locked #5 |
| 4 | **LoginEmail User-only (API) tasks** — Plan schedules projection: User-only; OwnAgent Deny on API/DB; verify | Step 4 LoginEmail rows; Step 6 OwnAgent denied LoginEmail test; Locked #6 | Steps 4, 6, 11; Locked #6 |
| 5 | **ContactEmail rules (no share) tasks** — Plan schedules OwnAgent Read / counterparty Deny; ShareOutbound Deny until Stage B; do not rewrite #7 stub | Step 4 ContactEmail rows; Step 9 OUT share-after-Accept; #7 unchanged; Locked #7 | Steps 4, 8, 9, 11; Locked #7 |
| 6 | **Authn fail-closed on projection** — Plan requires principal on protected projection paths; 401/403; no private fields in errors | Step 5 #5 principal; 401/403; omit private in errors; Steps 6–7 verify | Steps 5, 6, 7, 11 |
| 7 | **No Stage B/C inventing** — No Strategy ACL, agent hard-wall, Cognito, or MM/DC4 tasks | Step 9 OUT table; Step 10 forbid Cognito/MM; Locked #8 | Steps 9–10; Explicit OUT; Locked #8 |
| 8 | **Cross-story non-merge** — Keep separate from #32 plan and PoC Stories except cross-refs / consume patterns | Header Constraints; Step 8 #32 cross-ref only; #5–#7 consume/leave stub; no Story merge | Steps 5, 8–9, 11; Sources |
| 9 | **Cost / spend** — PoC **$0**; no IdP/vault provision tasks | Step 10 local/$0; no IdP/vault; Cost/critical → CPM | Step 10; Cost/critical; Locked #8 |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS | Handshake note; Done-list Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Security QA confirms.**

---

## 7. Explicit OUT

Mirror Spec §6 / issue #31 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Strategy ACL / Strategy body | **HOLD Stage B** |
| ContactEmail ShareOutbound-after-Accept | **HOLD Stage B** |
| Agent / tool hard wall + response scrubbers | **HOLD Stage C** (architecture accepted; not impl now) |
| Opening gate **#24** / SA-REV-MVP-A | Backlog until after Stage A delivery — **do not open now** |
| Cognito / SSO / IdP / vault / KMS spend | Out |
| MotorMarket / DC4 | Zero |
| Expanding PoC **#7** identity-seal stub | **Unchanged** |
| Implementing **#32** account list fail-closed in this Story | Cross-ref only — separate plan |
| Rewriting #5 / #6 / #7 Specs | Consume / leave stub only |
| Inventing FieldClasses beyond CEO examples (unless labeled illustrative) | Forbidden |
| Stage B/C inventing | Forbidden |

---

## 8. Cost/critical

**#31 must not procure AWS / Cognito / IdP / vault spend.** Any paid AWS provision, Cognito/SSO/IdP spend, vault/KMS, or other spend proposal → escalate **CPM → COO → CEO**. Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, or vault tasks. ECS Express Mode remains **README sketch only** if present. Gate **#24** stays backlog — do not open. Cost/critical → **CPM**.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-21__devplan__plan__mvp-stage-a-field-acl-registry.md`
- [ ] Spec coverage §§1–6 + §7 AC / §7.1 + issue #31 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] #31 ONLY — #32 account list **cross-ref only** (not implemented); Stage B/C **not** invented; gate #24 backlog; agent hard-wall Stage C HOLD
- [ ] API/DB projection scope only; #7 stub unchanged; Soft DisplayName non-blocking
- [ ] Extensible FieldClass + IFieldPolicy + omit-denied + deny-by-default + LoginEmail OwnAgent Deny + ContactEmail ShareOutbound Deny + automated tests scheduled
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] No Cognito/SSO/IdP/vault/AWS spend instructions
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock (+ CPM per ops). Sibling #32 remains separate plan — this plan does not draft or implement #32.

---

## 10. Done-list for SD (after plan QA PASS + Chief confirm)

- [ ] Step 1: Confirm host; Domain policy + Api/Application projection placement; keep `GET /health` Auth none if touched; no agent hard-wall
- [ ] Step 2: Extensible FieldClass registry (LoginEmail/ContactEmail starters; DisplayName soft/non-blocking)
- [ ] Step 3: IFieldPolicy.Evaluate on API/DB projection; omit denied fields; deny-by-default
- [ ] Step 4: Encode LoginEmail (User R/W; OwnAgent Deny) + ContactEmail (OwnAgent Read; ShareOutbound Deny); #7 unchanged
- [ ] Step 5: #5 principal on protected projection paths; unauth → 401/403; no private fields in errors
- [ ] Step 6: Automated tests — owner OK; OwnAgent denied LoginEmail; stranger/counterparty denied; unauth deny; deny-by-default
- [ ] Step 7: Projection omit / error hygiene verify
- [ ] Step 8: Cross-ref #32 only — do **not** implement list fail-closed
- [ ] Step 9: Do not implement Spec §6 OUT; do not invent Stage B/C; do not open #24; do not implement agent hard-wall
- [ ] Step 10: Secrets hygiene; local/$0; zero MM/DC4; no Cognito
- [ ] Step 11: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
