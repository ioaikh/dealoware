# Dev Plan — MVP Stage C Thin Strategy-driven AI Assistant runtime X1 (#66)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner PRIORITY **#66 ONLY** — Thin Strategy-driven AI Assistant runtime (X1 thin)  
**Issue:** https://github.com/ioaikh/dealoware/issues/66  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A (CEO ACCEPTED) · `stage:mvp` · Stage C · X1 thin  
**DOC-FLOW:** `plans/2026-09-28__devplan__plan__mvp-stage-c-thin-assistant-runtime-x1.md`  
**Constraints:** **#66 ONLY** — do **not** draft #67/#68/#69. Keep siblings separate (cross-ref only). **MANDATORY BIND #67** — platform tools/gateway path only; **NO** prompt-only soft wall invent. Soft **#41 Assistant OUT** closes via **#66 + #67** under #67 wall — **not** Stage B claim. Soft OTel/audit/idempotent touchpoints only — **no 5th Story**. Consume tip / Spec SoR CLEAR PR **#73** @ `f133e90`. PoC **$0**. Gate **#26** stays **backlog** until Stage C delivery — do **not** open/unlock now. Gate **#27** HOLD. Do **not** unlock #18 Spec/SD as a whole. No Cognito/MM/DC4; distinct from identity-seal **#7**. X1 thin only; fuller → V1; free-form → V1; A5 → V4. Any LLM/API spend → **COO → CEO** — do **not** provision. SD HOLD until Dev Plan QA + Security QA PASS + Chief unlock.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md` | Thin OwnAgent Assistant contracts; Locked #0–#9; §8 AC + §8.1 tests; OUT; Security §5 |
| Spec QA (Spec gate PASS) | `verification/2026-09-28__spec__verification__mvp-stage-c-thin-assistant-runtime-x1.md` | Spec-side bind; Sec10 weave intact; Spec SoR CLEAR |
| Spec Security PASS | `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan |
| Spec Security points-review | `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-points-review.md` | Senior PASS 10/10 |
| Spec Security checklist (upstream) | `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-checklist.md` | Spec-step Security 1–10 |
| Dev Plan Security checklist (binding) | `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-devplan-checklist.md` | Dev Plan-step handshake points **1–10** — woven in §6 |
| SA Security PASS | `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md` | Architecture-step Security PASS prior |
| Option A architecture (Stage C) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Stage C agent plane; OwnAgent StrategyBody; dual wall |
| CA PASS Stage C delta (thin Assistant pick A) | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | OwnAgent-only thin Strategy-driven Assistant behind gateway |
| BA note #66 | `plans/2026-09-28__ba__note__story-66-thin-assistant-runtime.md` | Spec-ready refine; Soft OTel/audit/idempotent weave |
| Soft #41 Spec (Assistant OUT → here) | `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md` | Stage B Strategy CRUD + StrategyBody ACL CLOSED; Assistant runtime was OUT |
| Stage A #31 Spec (consume) | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | IFieldPolicy / FieldClass — OwnAgent R/W for StrategyBody when owner |
| PoC Participant (#5) | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Authn baseline — validated principal |
| Sibling #67 Spec — mandatory bind / cross-ref only | `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` | Hard wall + scrub — **bind**; do **not** implement wall details here |
| Sibling #68 Spec — cross-ref only | `specs/2026-09-28__spec__spec__mvp-stage-c-a8-minimum-meters-budgets.md` | A8 meters — **OUT**; do not implement |
| Sibling #69 Spec — cross-ref only | `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md` | X2 UI/bot — **OUT**; do not implement |
| Issue #66 | https://github.com/ioaikh/dealoware/issues/66 | Binding AC + OUT |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A · Stage C named slice; Spec/SD HOLD as whole |
| Soft #41 | https://github.com/ioaikh/dealoware/issues/41 | Assistant OUT → closes via #66+#67 under wall |
| Gate #26 (backlog) | https://github.com/ioaikh/dealoware/issues/26 | Do **not** open now |
| Gate #27 (HOLD) | https://github.com/ioaikh/dealoware/issues/27 | HOLD |
| Spec SoR CLEAR | PR **#73** @ `f133e90` | Spec + Spec QA SoR MERGED CLEAR |
| Roadmap X1 / P6 | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | X1 thin partial; P6 communication with own AI only |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO Stage C named slice — **X1** MVP **thin** OwnAgent Assistant only. Fuller/stronger → **V1**; free-form Strategy engine → **V1**; A5 sandbox → **V4**; BYO/multi-LLM → later. Soft #41 Assistant OUT closes via **#66 + #67** under wall — **not** Stage B claim. Gate #26 backlog; #27 HOLD. Distinct from identity-seal **#7**. Conflicts → PM → Product → CEO. Cost/critical / LLM spend → **COO → CEO** (do not provision).

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend the O10 modular monolith with a **thin OwnAgent-only Strategy-driven AI Assistant runtime (X1)** for the owning Participant — strictly **1:1** (P6 spirit: communication with own AI only). Runtime reads/uses **own** minimal Strategy (**StrategyBody** only when existing Stage A/B `IFieldPolicy.Evaluate` allows **OwnAgent R/W** for owner); never another Participant’s StrategyBody; never Counterparty / Stranger agent invent. **MANDATORY BIND #67**: all tool I/O goes through **platform tools / gateway path only** — no raw DB, no arbitrary internal HTTP, **no** prompt-only soft wall as sole control. Do **not** re-implement #67 allowlist/scrub details here — bind and cite. Authn: validated **#5** principal; unauthenticated → **401**; wrong principal / cross-tenant → fail-closed **403** or **404** (Spec consistency); uniform deny; **no** LoginEmail / others’ private Strategy / denied fields in model context packs, tool outputs, or responses. Soft OTel/audit/idempotent-offers: note touchpoints where Assistant path hits SA-REV-MVP-C hooks already on roadmap — **no 5th Story**. Deliver automated tests per Spec §8.1. **OUT:** fuller Assistant (→ V1); free-form engine (→ V1); A5 (→ V4); #67 wall implementation details; #68 meters; #69 UI; Gate #26 open; #27 unlock; #18 Spec/SD unlock as whole; Cognito/MM/DC4; identity-seal #7 rewrite; LLM provision without COO→CEO. PoC/MVP **$0**. No product code in this artifact — SD instructions only. **#66 ONLY** — do not draft or implement #67/#68/#69.

---

## 3. Locked decisions (Spec locks → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | SA thin Assistant pick A | OwnAgent-only thin Strategy-driven Assistant behind #67 gateway; StrategyBody via Evaluate; no LoginEmail tool | Steps 1–4, 6–7 |
| 1 | OwnAgent-only 1:1 | Assistant acts only as OwnAgent for owning Participant; never Counterparty/Stranger; no multi-party | Steps 1–3, 7 |
| 2 | StrategyBody via FieldPolicy | Consume/write StrategyBody only when Evaluate allows OwnAgent R/W for owner; never others’ | Steps 3–4, 7–8 |
| 3 | Mandatory bind #67 | Platform tools / gateway path **only**; no raw DB / arbitrary internal HTTP; **no** prompt-only soft wall | Steps 1, 4, 7–9 |
| 4 | No LoginEmail in context | LoginEmail never in model / agent context packs or tool outputs; User-only (≠ ContactEmail) | Steps 4–5, 7 |
| 5 | Authn / IDOR fail-closed | Unauth → **401**; wrong principal / cross-tenant → **403**/**404**; uniform deny; no private leak | Steps 2, 5, 7 |
| 6 | Soft #41 OUT closed by design | Soft #41 Assistant OUT closes by **#66 + #67** delivery — **not** Stage B–delivered claim | Steps 8–9; Explicit OUT |
| 7 | Soft OTel/audit/idempotent | Touchpoints only where Assistant path hits SA-REV-MVP-C hooks; **no 5th Story** | Step 6; Explicit OUT |
| 8 | Scope label | **X1** MVP **thin**; fuller → V1; free-form → V1; A5 → V4; BYO/multi-LLM later | Steps 9–10; Explicit OUT |
| 9 | OUT / Gate / spend | #67/#68/#69 not implemented here; Gate #26 backlog; #27 HOLD; PoC $0; LLM → COO → CEO; distinct from #7 | Steps 8–10; Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 modular monolith — Spec §7 Host).  
**Prerequisite:** Stage A #31 Field ACL + #32 account list fail-closed **CLOSED**; Stage B #40+#41+#42 **CLOSED** (Strategy CRUD + StrategyBody ACL delivered; Assistant runtime was **OUT**); PoC Participant auth (#5) delivered. Consume — do **not** rewrite those Specs or merge Stories. Soft #41 Assistant OUT closes **here + #67** under wall.  
**Sibling HOLD:** #67 agent/tool hard wall (mandatory **bind** only); #68 A8 meters; #69 X2 UI/bot — **cross-ref only**; separate plans/Stories. **Do not draft or implement wall details / meters / UI here.**  
**Hold:** Gate #26 open; Gate #27; unlocking #18 Spec/SD as a whole; Cognito/MM/DC4; identity-seal #7 rewrite; LLM provision — do **not** invent.

### Step 1 — Confirm host layout; thin Assistant runtime placement; bind #67 gateway path; keep health open

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**) if any host/DI touch is required for wiring.
- Place thin OwnAgent Assistant runtime in **Application/Api agent path** behind **#67 platform tools / gateway** (Spec §1–§2) — OwnAgent-only 1:1 for owning Participant.
- Runtime must call **platform tools / gateway only** — do **not** open raw DB connections or arbitrary internal HTTP from the Assistant path.
- Do **not** invent a prompt-only soft wall as sole control; do **not** re-specify or implement #67 allowlist/scrub details under this Story.
- Continue Minimal APIs; built-in ASP.NET Core DI only.
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS spend modules, MM/DC4 refs, or LLM provider packages that create spend without COO→CEO.
- Do **not** open gate #26, unlock #27, unlock #18 Spec/SD as a whole, or invent #67/#68/#69 implementation Stories here.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none) if touched
- [ ] Assistant path documented as OwnAgent-only thin runtime **behind #67 gateway**
- [ ] No raw DB / arbitrary internal HTTP from Assistant path; no prompt-only soft wall invent
- [ ] No Cognito/IdP/AWS/MM provision tasks; gate #26 not opened; #27 not unlocked; #18 Spec/SD not unlocked as whole

### Step 2 — Authn fail-closed on Assistant invoke paths (#5 required)

Per Spec §3 Authn + Locked #5; Security Dev Plan point **5**:

| Item | Plan lock |
|------|-----------|
| Principal | Validated **#5** (or successor) principal required on all Assistant invoke paths |
| Unauthenticated / invalid | Fail closed **`401`**; error bodies omit private fields / StrategyBody of others / LoginEmail / FieldClass secrets / auth secrets |
| Auth header | Reuse #5 `Authorization` patterns — do **not** invent Cognito/SSO/password/cookie productization |
| Health | `GET /health` remains Auth none |

**Verify tasks (required):**

- [ ] Unauthenticated Assistant invoke → **401**; no private fields in body
- [ ] Invalid credential → fail closed **401**; no private leak
- [ ] Auth’d principal reaches OwnAgent Assistant path (happy path wired)
- [ ] `GET /health` remains open

### Step 3 — Thin OwnAgent-only 1:1 runtime; own Strategy via FieldPolicy

Per Spec §1 + Locked #1/#2; Security Dev Plan points **1** and **2**:

| Item | Plan lock |
|------|-----------|
| Identity | Assistant acts only as **OwnAgent** for owning Participant (P6); never Counterparty / Stranger agent; no multi-party invent |
| Strategy | Reads/uses **own** minimal Strategy only |
| StrategyBody | Consume/write only when `IFieldPolicy.Evaluate` allows **OwnAgent R/W** for owner context |
| Forbidden | Never another Participant’s StrategyBody; never dump owner inventory of others |
| Do not | Rewrite #31/#41 registry AC; invent free-form Strategy engine; invent A5 sandbox |

**Endpoints (plan intent — SD names to match host conventions):** authenticated thin Assistant invoke route(s) under Api — OwnAgent-only; not a Counterparty/Stranger agent surface; not #69 UI.

**Acceptance:**

- [ ] Auth’d owning Participant can run thin OwnAgent Assistant that uses **own** StrategyBody via FieldPolicy OwnAgent R/W
- [ ] Runtime refuses Counterparty / Stranger agent identity invent
- [ ] No multi-party agent invent; no free-form engine; no A5 sandbox
- [ ] #31/#41 FieldPolicy consumed — not rewritten

### Step 4 — Mandatory #67 gateway bind; no LoginEmail; no denied fields in context

Per Spec §2/#3 + Locked #3/#4; Security Dev Plan points **3** and **4**:

| Must be **absent** from model/context packs / tool outputs / responses | Note |
|-----------------------------------------------------------------------|------|
| **LoginEmail** | User-only — never in agent context (distinct from ContactEmail) |
| Other Participants’ **StrategyBody** / private Strategy | Never |
| Denied FieldClasses | Never enter context packs or tool outputs |
| Auth secrets / raw tokens | Never |

| Item | Plan lock |
|------|-----------|
| Path | All tool I/O via **#67 platform tools / gateway** only |
| Forbidden | Raw DB; arbitrary internal HTTP; prompt-only soft wall as sole control |
| Cross-ref | Cite #67 Spec for allowlist + scrub — do **not** re-implement wall details under #66 |
| LoginEmail | No LoginEmail tool; never in context packs / tool outputs |
| Do not | Invent parallel agent ACL tables; invent #68 meters or #69 UI under this Story |

**Acceptance:**

- [ ] Assistant runtime path uses platform tools / #67 gateway exclusively
- [ ] Automated/manual inspect: **no** LoginEmail / others’ StrategyBody / denied fields / auth secrets in context packs or tool outputs
- [ ] No prompt-only soft wall as sole control; no raw DB / arbitrary internal HTTP
- [ ] No #67 wall-detail / #68 / #69 implementation tasks executed under this Story

### Step 5 — Uniform deny / stranger / cross-tenant fail-closed (no leak)

Per Spec §3 + Locked #5; Security Dev Plan point **5**:

| Item | Plan lock |
|------|-----------|
| Wrong principal / cross-tenant | Fail-closed **`403`** or **`404`** (Spec consistency with PoC/#32) |
| Leakage | **No** LoginEmail / others’ StrategyBody / denied fields / private inventory in errors or context |
| Align | #18 / #31 / #32 / #67 spirit — uniform deny; no existence leak via secret-bearing bodies |
| Do not invent | Multi-tenant admin Assistant; Cognito productization; Counterparty agent |

**Verify tasks (required):**

- [ ] Stranger / wrong-principal / cross-tenant misuse of Assistant → fail-closed; uniform deny
- [ ] Deny bodies contain no LoginEmail / others’ StrategyBody / denied fields / auth secrets
- [ ] Logging does not dump raw tokens / private Strategy / LoginEmail on deny paths

### Step 6 — Soft OTel / audit / idempotent-offers weave only (no 5th Story)

Per Spec §4 + Locked #7:

| Touchpoint | Plan lock |
|------------|-----------|
| Assistant invoke | Soft: if SA-REV-MVP-C OTel/audit hooks already on path, wire touchpoints — do **not** invent observability product |
| Tool/offer writes via Assistant | Soft: if Assistant touches offer paths under #67, align idempotent-offers / audit weave where hooks already exist |
| Invent forbid | Do **not** invent a **5th Story** or new product surface for OTel/audit/idempotent |

**Acceptance:**

- [ ] Handoff notes document soft touchpoints only (or “none on path yet”) with Spec §4 cite
- [ ] No 5th Story / observability product invented under this Story

### Step 7 — Automated tests (binding Spec §8 / §8.1)

Schedule automated tests (or equivalent evidence) covering:

| Case | Expected |
|------|----------|
| Owner OwnAgent OK | Thin Assistant can use own Strategy (StrategyBody via FieldPolicy OwnAgent R/W) |
| Stranger / cross-tenant | Deny; no private Strategy leakage |
| Unauthenticated | Deny (**401**); no private fields in errors |
| LoginEmail | Never in agent context packs / tool outputs |
| Gateway bind | Runtime path uses platform tools / #67 gateway (not raw DB / arbitrary internal HTTP) |

**Acceptance:**

- [ ] Test suite covers all Spec §8.1 cases
- [ ] Failures assert status + **absence** of LoginEmail / others’ Strategy / denied fields in payloads, context packs, and deny bodies
- [ ] Tests exercise real FieldPolicy / gateway bind paths (not mocked-away omit that always strips everything; not a prompt-only soft wall stub)

### Step 8 — Soft #41 OUT close by design; consume #31/#41; siblings cross-ref only; distinct from #7

| Item | Plan lock |
|------|-----------|
| Soft #41 Assistant OUT | Closes by **#66 + #67** delivery under wall — do **not** claim Stage B delivered Assistant/tool runtime |
| #31 / #41 FieldPolicy | **Consume** OwnAgent R/W Evaluate for owner StrategyBody — do **not** rewrite registry AC or merge Stories |
| #67 | Mandatory **bind** only — do **not** implement allowlist/scrub/wall details here |
| #68 | A8 meters/budgets — **OUT**; cross-ref only (primary metered consumer relationship noted; not implemented) |
| #69 | X2 UI/bot — **OUT**; cross-ref only |
| #5 | Authn baseline — consume; do **not** rewrite |
| #18 | Parent Spec/SD remains **HOLD** as a whole — do **not** unlock |
| #7 | Distinct from identity-seal / contact-on-accept **#7** — do **not** merge seals or rewrite #7 |

**Acceptance:**

- [ ] Plan/handoff cites Soft #41 → #66+#67 under wall (not Stage B claim)
- [ ] No #31/#41 rewrite; no #67 wall-detail / #68 / #69 / #18-unlock / #7-merge tasks under this Story

### Step 9 — Explicit OUT (do **not** implement Spec §6)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Fuller / stronger Assistant | X1 remainder → **V1** |
| Free-form Strategy evaluation engine | → **V1** |
| Strategy sandbox (A5) | → **V4** |
| Agent/tool hard wall implementation details | **#67** — bind only; separate Story |
| A8 meters/budgets | **#68** — separate Story |
| Basic UI / first-party bot | **#69** — separate Story |
| Inventing a 5th Story for OTel/audit/idempotent | Soft weave only (Step 6) |
| MCP; public OpenAPI; multi-party | OUT |
| Gate **#26** open / unlock before Stage C delivery | Backlog — **do not open now** |
| Gate **#27** unlock | **HOLD** |
| Unlocking #18 Spec/SD as a whole | HOLD |
| Claiming Stage B delivered Assistant runtime | Soft #41 OUT closes here + #67 |
| Identity-seal / #7 rewrite | Distinct from #7 |
| Cognito/SSO / vault/KMS / LLM provision without COO→CEO | Out / escalate |
| MotorMarket / DC4 | Zero |

### Step 10 — Secrets / cost / zero MM-DC4; local $0; no LLM provision

**Secrets (reuse #5 / #31 patterns):**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings / LLM provider keys
- [ ] Connection strings + signing material = env / placeholders only
- [ ] No tokens in query strings or Assistant bodies
- [ ] No logging of raw tokens / keys / credentials / LoginEmail / secret FieldClass values / others’ StrategyBody
- [ ] No Cognito/SSO/IdP/vault SDK PackageReferences added by this Story that create spend

**Host / spend:**

- [ ] Remains **local / $0**
- [ ] ECS Express Mode **sketch only** if README already mentions — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource provision that creates spend
- [ ] **Any named LLM/API spend** → escalate **COO → CEO** — **do not provision** in this Story acceptance
- [ ] If other spend ever proposed → escalate **CPM → COO → CEO**

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, packages, shared libs, schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 11 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] Thin OwnAgent-only 1:1 Assistant runtime; own StrategyBody via FieldPolicy OwnAgent R/W
- [ ] #5 principal required; unauthenticated → **401** fail closed
- [ ] Platform tools / #67 gateway only — no raw DB / arbitrary internal HTTP; no prompt-only soft wall
- [ ] No LoginEmail / others’ Strategy / denied fields in context packs, tool outputs, or responses
- [ ] Wrong-principal / cross-tenant → uniform deny (**403**/**404**); no private leak
- [ ] Soft OTel/audit/idempotent touchpoints only — no 5th Story
- [ ] Automated tests per Spec §8 / §8.1
- [ ] Soft #41 OUT closed by #66+#67 design — not Stage B claim; #31/#41 consumed not rewritten
- [ ] #67/#68/#69 not implemented here; #18 Spec/SD not unlocked as whole; distinct from #7
- [ ] `GET /health` still Auth none if host touched
- [ ] Nothing from Step 9 / Spec §6 OUT implemented; gate #26 not opened; #27 not unlocked
- [ ] Secrets hygiene + zero MM/DC4 + local/$0; no Cognito; no LLM provision without COO→CEO
- [ ] #66 ONLY — no sibling Story drafts/impl; Product conflicts escalated if any
- [ ] No product code left undocumented; handoff notes cite Spec + this plan

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec Locked #0 / SA thin pick A | OwnAgent-only thin Assistant behind #67; StrategyBody via Evaluate | Steps 1–4, 6–7 |
| Spec §1 + Locked #1 | OwnAgent-only 1:1; P6; no Counterparty/Stranger | Steps 1–3, 7 |
| Spec §1 + Locked #2 | StrategyBody via FieldPolicy OwnAgent R/W for owner | Steps 3–4, 7–8 |
| Spec §2 + Locked #3 | Platform tools / #67 gateway only; no prompt-only soft wall | Steps 1, 4, 7–9 |
| Spec §3 + Locked #4 | No LoginEmail in context packs / tool outputs | Steps 4–5, 7 |
| Spec §3 + Locked #5 | Unauth 401; cross-tenant 403/404; uniform deny; no leak | Steps 2, 5, 7 |
| Spec Locked #6 | Soft #41 OUT via #66+#67 — not Stage B claim | Steps 8–9; Explicit OUT |
| Spec §4 + Locked #7 | Soft OTel/audit/idempotent weave; no 5th Story | Step 6; Explicit OUT |
| Spec Locked #8/#9 + §6 OUT | X1 thin; V1/V4; siblings/Gate/spend OUT | Steps 8–10; Explicit OUT |
| Spec §7 Host / cost | Extend O10; local/$0; LLM → COO → CEO | Steps 1, 10 |
| Spec §5 Security Spec 1–10 | Upstream Spec Security bind | §6 Security Dev Plan-step woven 1–10 |
| Spec §8 row 1 / Issue AC | Thin OwnAgent uses own Strategy 1:1 | Steps 1–3, 7 |
| Spec §8 row 2 / Issue AC | OwnAgent only (P6); not Counterparty/Stranger | Steps 1, 3, 7 |
| Spec §8 row 3 / Issue AC | Platform tools / #67 gateway; no soft wall | Steps 1, 4, 7 |
| Spec §8 row 4 / Issue AC | Unauth 401; fail-closed; no LoginEmail/others’ Strategy | Steps 2, 4–5, 7 |
| Spec §8 row 5 / §8.1 / Issue AC | Automated tests | Step 7 |
| Spec §8 row 6 / Issue AC | X1 thin; fuller→V1; free-form→V1; A5→V4 | Steps 9–10; Explicit OUT |
| Spec §8 row 7 / Issue AC | Soft OTel/audit/idempotent weave; no 5th Story | Step 6 |
| #31 / #41 Specs consume | FieldPolicy OwnAgent R/W for owner StrategyBody | Steps 3–4, 8 |
| #5 Spec consume | Validated principal | Step 2 |
| #67 Spec mandatory bind | Gateway / allowlist / scrub — bind; do not implement details | Steps 1, 4, 8–9 |
| #68 / #69 Specs cross-ref only | OUT — do not implement | Steps 8–9 |
| Soft #41 | Assistant OUT closes via #66+#67 | Steps 8–9 |
| Identity-seal #7 | Distinct — do not merge | Steps 8–9 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-qa-confirm.md` (points 1–10 MET)  
**Spec Security points-review:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-points-review.md` (PASS 10/10)  
**Spec Security checklist (upstream):** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-checklist.md`  
**Prior SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Spec Security binding:** Spec §5 points 1–10  
**Spec SoR CLEAR:** PR **#73** @ `f133e90`

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **OwnAgent-only 1:1 tasks** — Plan schedules thin Assistant as **OwnAgent** for the owning Participant only (P6 spirit); never Counterparty / Stranger agent identity; no multi-party invent; verify | Steps 1, 3 lock OwnAgent-only 1:1; P6; refuse Counterparty/Stranger; Step 7 owner OK + stranger deny tests; Locked #1 | Steps 1, 3, 7, 11; Locked #1 |
| 2 | **StrategyBody via FieldPolicy tasks** — Plan schedules StrategyBody consume/write only when `IFieldPolicy.Evaluate` allows OwnAgent R/W for owner; never expose another Participant’s StrategyBody; verify | Steps 3–4 consume Evaluate OwnAgent R/W for owner; never others’; Step 7 tests; Step 8 consume #31/#41; Locked #2 | Steps 3–4, 7–8, 11; Locked #2 |
| 3 | **Mandatory bind to #67 hard wall** — Plan requires runtime uses **platform tools / gateway path only**; **rejects** prompt-only soft wall as sole control; cross-ref #67 for allowlist + scrub; verify | Steps 1, 4 bind platform tools/gateway only; forbid raw DB / arbitrary HTTP / soft wall; Step 7 gateway-bind test; wall details not implemented here; Locked #3 | Steps 1, 4, 7–9, 11; Locked #3; Explicit OUT |
| 4 | **No LoginEmail in agent context tasks** — Plan requires LoginEmail never enters model / agent context packs or tool outputs; LoginEmail remains User-only (≠ ContactEmail); verify | Step 4 omit LoginEmail from context packs/tool outputs; Step 5 deny hygiene; Step 7 LoginEmail case; Locked #4 | Steps 4–5, 7, 11; Locked #4 |
| 5 | **Authn / IDOR fail-closed tasks** — Plan schedules unauthenticated → **401**; wrong principal / cross-tenant → **403** or **404**; uniform deny; **no** private-field leakage in responses or model context; verify | Steps 2, 5: unauth 401; cross-tenant 403/404; uniform deny; no leak; Step 7 tests; Locked #5 | Steps 2, 5, 7, 11; Locked #5 |
| 6 | **Soft #41 OUT closed by delivery path only** — Plan documents soft #41 Assistant OUT closed by **#66 + #67** delivery under wall — does **not** claim Assistant/tool runtime was Stage B–delivered; verify no Stage B Assistant claim tasks | Step 8 Soft #41 → #66+#67 under wall (not Stage B claim); Step 9 OUT forbids Stage B Assistant claim; Locked #6 | Steps 8–9; Explicit OUT; Locked #6 |
| 7 | **OUT locked (X1 thin)** — Plan keeps **X1 MVP thin** only; fuller / stronger → **V1**; free-form → **V1**; A5 → **V4**; BYO / multi-LLM later; Soft OTel/audit/idempotent = weave only (no 5th Story) | Step 6 Soft OTel weave only — no 5th Story; Step 9 OUT: X1 thin; fuller→V1; free-form→V1; A5→V4; Locked #7/#8 | Steps 6, 9–10; Explicit OUT; Locked #7/#8 |
| 8 | **Sibling / Gate HOLDs** — Plan does not invent #68/#69 surfaces into this Story; Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/SSO/MM/DC4/vault invent; keep siblings separate plans | Steps 8–9: #67/#68/#69 cross-ref only; Gate #26 backlog; #27 HOLD; distinct from #7; no Cognito/MM/DC4; Locked #9 | Steps 8–10; Explicit OUT; Locked #9 |
| 9 | **Cost / spend** — PoC **$0**; any named LLM/API spend → escalate **COO → CEO**; do not provision spend tasks without that path | Step 10 local/$0; no LLM/IdP/vault provision; Cost → COO → CEO; Locked #9 | Step 10; Cost/critical; Locked #9 |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then | See handshake note below. Done-list requires Security QA confirm before PASS; SD HOLD | Handshake note; Done-list Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Senior Security → Security QA confirms.** **SD stays HOLD** until Dev Plan QA + Security QA PASS + Chief Dev Planner unlock (+ CPM per ops).

---

## 7. Explicit OUT

Mirror Spec §6 / issue #66 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Fuller / stronger Assistant | X1 remainder → **V1** |
| Free-form Strategy evaluation engine | → **V1** |
| Strategy sandbox (A5) | → **V4** |
| Agent/tool hard wall implementation details (#67) | Sibling — mandatory bind; do not replace / implement details here |
| A8 meters/budgets (#68) | Sibling — primary metered consumer relationship only |
| Basic UI / first-party bot (#69) | Sibling — surface only |
| Inventing a 5th Story for OTel/audit/idempotent | Soft weave only (§4 / Step 6) |
| MCP; public OpenAPI; multi-party; MotorMarket / DC4 | OUT |
| Cognito / SSO / IdP / vault / KMS / LLM provision without COO→CEO | Out / escalate |
| Opening gate **#26** / SA-REV-MVP-C post-delivery | Backlog until after Stage C delivery — **do not open now** |
| Unlocking gate **#27** | **HOLD** |
| Unlocking #18 Spec/SD as a whole | **HOLD** |
| Claiming Stage B delivered Assistant runtime | Soft #41 OUT closes here + #67 under wall |
| Identity-seal / contact-on-accept rewrite (#7) | Distinct from #7 — do not merge |
| Drafting or implementing #67 / #68 / #69 in this plan | **Forbidden** — #66 ONLY |

---

## 8. Cost/critical

**#66 must not procure AWS / Cognito / IdP / vault / LLM spend.** Any paid AWS provision, Cognito/SSO/IdP spend, vault/KMS, **named LLM/API spend**, or other spend proposal → escalate **COO → CEO** (do **not** provision in Story acceptance). Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, LLM provider account, or vault tasks. ECS Express Mode remains **README sketch only** if present. Gate **#26** stays backlog — do not open. Gate **#27** HOLD. Do **not** unlock #18 Spec/SD as a whole.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-28__devplan__plan__mvp-stage-c-thin-assistant-runtime-x1.md`
- [ ] Spec coverage §§1–7 + §8 AC + §8.1 + issue #66 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] **#66 ONLY** — #67/#68/#69 **cross-ref only** (not drafted/implemented); Gate #26 backlog; #27 HOLD; #18 Spec/SD HOLD as whole
- [ ] **MANDATORY BIND #67** — platform tools/gateway only; **NO** prompt-only soft wall; wall details not implemented here
- [ ] OwnAgent-only 1:1; StrategyBody via FieldPolicy OwnAgent R/W; no LoginEmail in context; authn/IDOR fail-closed scheduled
- [ ] Soft #41 OUT closes via #66+#67 under wall — **not** Stage B claim
- [ ] Soft OTel/audit/idempotent weave only — **no 5th Story**
- [ ] Automated tests per Spec §8 / §8.1 scheduled
- [ ] Distinct from identity-seal **#7**; no Cognito/SSO/IdP/vault/LLM provision; no MM/DC4
- [ ] X1 thin only; fuller→V1; free-form→V1; A5→V4; PoC $0; Spec SoR CLEAR PR #73 @ `f133e90` cited
- [x] **Security Dev Plan-step points 1–10 all woven** with cites (table §6) — **HOLD PASS** until Senior Security → Security QA confirms
- [ ] No invented Stories / requirements; Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner
- [ ] **SD HOLD** until Dev Plan QA + Security QA PASS + Chief unlock

**Next:** Dev Plan QA verifies with evidence → ask Senior Security → Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). **Dev Plan QA must NOT PASS until Security QA confirms.** SD starts only after Chief Dev Planner unlock (+ CPM per ops). Parent may run #67/#68/#69 separately — **this plan does not draft them**.

---

## 10. Done-list for SD (after plan QA PASS + Security QA confirm + Chief unlock)

- [ ] Step 1: Confirm host; thin OwnAgent Assistant behind #67 gateway; keep `GET /health` Auth none if touched
- [ ] Step 2: #5 principal on Assistant invoke; unauth → **401**; deny hygiene
- [ ] Step 3: OwnAgent-only 1:1; own StrategyBody via FieldPolicy OwnAgent R/W; no multi-party
- [ ] Step 4: #67 platform tools/gateway only; no LoginEmail / denied fields in context; no soft wall / raw DB / arbitrary HTTP
- [ ] Step 5: Stranger / cross-tenant → uniform deny (**403**/**404**); no private leak
- [ ] Step 6: Soft OTel/audit/idempotent touchpoints only — no 5th Story
- [ ] Step 7: Automated tests per Spec §8 / §8.1
- [ ] Step 8: Soft #41 → #66+#67 design close; consume #31/#41; do **not** implement #67 details/#68/#69; do **not** unlock #18; distinct from #7
- [ ] Step 9: Do not implement Spec §6 OUT; do not open #26; do not unlock #27
- [ ] Step 10: Secrets hygiene; local/$0; zero MM/DC4; no Cognito; no LLM provision without COO→CEO; ECS sketch only
- [ ] Step 11: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
