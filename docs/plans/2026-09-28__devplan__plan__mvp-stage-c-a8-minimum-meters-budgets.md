# Dev Plan — MVP Stage C A8-minimum per-Participant meters + hard budgets (#68)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner PRIORITY **#68 ONLY** — A8-minimum per-Participant meters + hard budgets (cutoff)  
**Issue:** https://github.com/ioaikh/dealoware/issues/68  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A · `stage:mvp` · Stage C · A8-minimum (Option 1)  
**DOC-FLOW:** `plans/2026-09-28__devplan__plan__mvp-stage-c-a8-minimum-meters-budgets.md`  
**Constraints:** **#68 ONLY** — do **not** draft #66/#67/#69. Keep siblings separate (cross-ref only). Primary metered consumer = sibling **#66**; metered path remains **#67** wall-bound. Hard cutoff fail-closed — **not** soft-warn-only. Budget status must **not** become privilege escalation or field-leak channel. Soft audit/OTel weave only; Soft O7 align **only if already on path** — no second product; **no 5th Story**. Mature metering / owner cost UI → **V3**; no billing/escrow invent. **#69** may show budget status **minimally** — **not** owner admin (do **not** implement #69). Gate **#26** backlog until Stage C delivery; Gate **#27** HOLD. PoC **$0**. Any LLM/API spend → **COO → CEO**; do not provision. No Cognito/MM/DC4. Distinct from PoC **#7** (identity-seal stub — do not merge/rewrite). Do **not** unlock #18 Spec/SD as a whole. SD HOLD until Dev Plan QA + Security QA PASS + Chief unlock.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-28__spec__spec__mvp-stage-c-a8-minimum-meters-budgets.md` | A8-min meters + hard cutoff contracts; Locked #0–#10; §8 AC + §8.1 tests; OUT; Security §5 |
| Spec QA (Spec gate PASS) | `verification/2026-09-28__spec__verification__mvp-stage-c-a8-minimum-meters-budgets.md` | Spec-side bind; Sec10 weave intact; Spec gate **PASS** |
| Spec Security PASS (qa-confirm) | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-qa-confirm.md` | Spec-step points 1–10 **MET** — binding unlock for Dev Plan (SoR PR **#72** @ `32d2e0bc`) |
| Spec Security points-review | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-points-review.md` | Senior **PASS** 10/10 |
| Spec Security checklist (upstream) | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-checklist.md` | Spec-step Security 1–10 (PR **#71** @ `f64a3d11`) |
| Dev Plan Security checklist (binding) | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-devplan-checklist.md` | Dev Plan-step handshake points **1–10** — woven in §6 |
| SA Security PASS (prior) | `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md` | Architecture-step Security PASS prior |
| CA PASS Stage C delta (A8-min pick A) | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | Per-Participant meters + hard cutoff; mature → V3 |
| BA note #68 | `plans/2026-09-28__ba__note__story-68-a8-minimum-meters-budgets.md` | Spec-ready refine; Soft audit/OTel; Soft O7 align-if-on-path |
| Option A tip | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall context; consume tip |
| Roadmap A8 Option 1 | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | A8-minimum MVP; mature → V3; Soft O7 MVP-light |
| Sibling #66 Spec — cross-ref only | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md` | Primary metered consumer — **do not implement** |
| Sibling #67 Spec — cross-ref only | `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` | Metered path wall-bind — **do not implement** |
| Sibling #69 Spec — cross-ref only | `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md` | May show budget status minimally — **do not implement** |
| Issue #68 | https://github.com/ioaikh/dealoware/issues/68 | Binding AC + OUT |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A · Stage C named slice |
| Gate #26 (backlog) | https://github.com/ioaikh/dealoware/issues/26 | Do **not** open now |
| Gate #27 (HOLD) | https://github.com/ioaikh/dealoware/issues/27 | **HOLD** |
| PoC #7 (distinct) | https://github.com/ioaikh/dealoware/issues/7 | Identity-seal stub — **distinct**; do not merge/rewrite |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO Stage C — **A8-minimum** Option 1 only. Mature metering / platform-owner cost UI → **V3**. Soft O7 MVP-light tags/budgets/kill-switch — align **only if already on path** (cite roadmap; no second product). Conflicts → PM → Product → CEO. Cost/critical / LLM spend → **COO → CEO**.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend the O10 modular monolith with **A8-minimum** **per-Participant meters** + **hard budget cutoff** (fail-closed) for MVP-metered Assistant / LLM (and Product-named Stage C metered surfaces without inventing extras). **Primary consumer** = sibling **#66** thin Assistant; metered Assistant/tool path remains behind sibling **#67** hard wall. When Participant budget is exhausted, further metered Assistant/tool invocations **deny server-side** — **not** soft-warn-only. Unauthenticated / wrong-principal cannot burn another Participant’s budget; meter APIs fail-closed (**401** / **403**/**404**); uniform deny; **no** private-field leakage via meter/budget payloads; budget status must **not** be a privilege-escalation or field-leak channel. Soft: if meter/cutoff events touch SA-REV-MVP-C audit/OTel hooks already on path — note touchpoints only (**no 5th Story**). Soft O7 align **only if already on path**. Deliver automated tests per Spec §8.1. **OUT:** mature metering / owner cost UI (→ **V3**); billing/settlement/escrow; #66/#67/#69 features (except #69 may *later* show status minimally — not in this Story); Soft O7 as second product; Cognito/MM/DC4; opening gate **#26**; unlocking #18 Spec/SD; rewriting PoC **#7**. PoC/MVP **$0**. No product code in this artifact — SD instructions only. **#68 ONLY** — do not draft or implement #66/#67/#69.

---

## 3. Locked decisions (Spec locks → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | SA A8-min pick A | Per-Participant meters + **hard cutoff** fail-closed (Option 1); mature UI deferred V3 | Steps 1–4, 7 |
| 1 | Per-Participant meters | Minimum viable counters for MVP-metered Assistant/LLM; scoped to Participant; primary consumer #66; no invent extras | Steps 3, 7–8 |
| 2 | Hard cutoff fail-closed | Budget exhausted → further metered invocations **deny server-side** — not soft-warn-only | Steps 4, 7 |
| 3 | Cross-tenant / unauth | Unauth/wrong-principal cannot consume another’s budget; cross-tenant meter misuse fail-closed | Steps 2, 5, 7 |
| 4 | Metered path wall-bound | Metered path remains behind #67; budget status ≠ privilege escalation / field-leak channel | Steps 1, 4, 6–7 |
| 5 | Authn on meter APIs | Unauth → **401**; wrong principal → **403**/**404**; uniform deny; no private leak via meter/budget payloads | Steps 2, 5, 7 |
| 6 | Soft audit/OTel | Touchpoints only if SA-REV-MVP-C hooks already on path; **no 5th Story** | Step 8; Soft weave |
| 7 | Soft O7 align | Align tags/budgets/kill-switch **only if already on path** — cite roadmap; no second product | Step 8; Explicit OUT |
| 8 | Scope label | **A8-minimum** MVP; mature metering / owner cost UI → **V3**; no billing/settlement/escrow invent | Steps 9–10; Explicit OUT |
| 9 | Sibling #69 | May show budget status **minimally** to respect cutoff — **not** platform-owner admin; **do not implement #69 here** | Steps 6, 8–9 |
| 10 | OUT / Gate / spend | Gate #26 backlog; #27 HOLD; PoC $0; LLM/API spend → COO → CEO; no Cognito/MM/DC4 | Steps 9–10; Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 modular monolith — Spec §7 Host).  
**Prerequisite:** Spec gate PASS + Spec Security PASS (cited Sources); Stage A/B tip on `main` @ `ca827a2`. Sibling **#66** is primary metered consumer; sibling **#67** wall-binds the metered path — consume relationships via cross-ref only; do **not** merge Stories or rewrite those Specs.  
**Sibling HOLD:** #66 thin Assistant; #67 agent/tool hard wall; #69 X2 UI/bot — **cross-ref only**; separate plans/Stories. **Do not draft or implement here.**  
**Hold:** Gate #26; Gate #27; unlocking #18 Spec/SD as a whole; Cognito/MM/DC4; mature metering/billing; Soft O7 as second product; 5th Story for audit/OTel — do **not** invent.

### Step 1 — Confirm host layout; meter surface placement; keep health open; wall-bind note

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**) if any host/DI touch is required for wiring.
- Place per-Participant meters + cutoff enforcement on the **Application/Api metered Assistant/tool path** (Spec §7 Host; primary consumer **#66**) — **behind** the **#67** hard-wall binding (cross-ref only; do not implement #67 under this Story).
- Continue Minimal APIs; built-in ASP.NET Core DI only.
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS spend modules, billing/escrow modules, or MM/DC4 refs.
- Do **not** open gate #26, unlock #18 Spec/SD, invent Soft O7 as a second product, invent a 5th Story, or merge #66/#67/#69/#7.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none) if touched
- [ ] Meter/cutoff path documented as Participant-scoped and **#67 wall-bound** (cross-ref)
- [ ] No Cognito/IdP/AWS/MM provision tasks; gate #26 not opened; #18 Spec/SD not unlocked; #66/#67/#69/#7 not merged

### Step 2 — Authn fail-closed on meter / budget / cutoff paths (#5 required)

Per Spec §3 Authn + Locked #5; Security Dev Plan point **5**:

| Item | Plan lock |
|------|-----------|
| Principal | Validated **#5** (or successor) principal required on meter/budget/cutoff paths |
| Unauthenticated / invalid | Fail closed **`401`**; cannot burn any Participant budget; error bodies omit private fields / PII / FieldClass secrets / auth secrets / other Participants’ meter details |
| Wrong principal | Fail closed **`403`**/**`404`**; uniform deny; cannot consume another’s budget |
| Auth header | Reuse #5 `Authorization` patterns — do **not** invent Cognito/SSO/password/cookie productization |
| Health | `GET /health` remains Auth none |

**Verify tasks (required):**

- [ ] Unauthenticated meter/budget/cutoff path → **401**; no private leak; no budget burn
- [ ] Invalid credential → fail closed **401**; no private leak
- [ ] Auth’d principal reaches meter path (happy path wired)
- [ ] `GET /health` remains open

### Step 3 — Per-Participant minimum meters (A8-minimum; primary consumer #66)

Per Spec §1 + Locked #1; Security Dev Plan point **1**:

| Item | Plan lock |
|------|-----------|
| Scope | Counters **scoped to Participant** for MVP-metered Assistant / LLM usage |
| Primary consumer | Sibling **#66** thin Assistant (and Product-named Stage C metered surfaces **without inventing extras**) |
| Minimum viable | Sufficient for cutoff — **not** mature owner analytics |
| Units / signal | Spec may define meter units / cutoff signal shape without inventing spend or billing product |
| Do not | Invent billing product, escrow, settlement, owner cost UI, or extras beyond Product-named Stage C metered surfaces |

**Acceptance:**

- [ ] Per-Participant meters exist for MVP-metered Assistant/LLM (minimum viable for cutoff)
- [ ] Counters scoped to Participant (no shared/global burn across Participants)
- [ ] Primary consumer relationship to #66 documented (cross-ref only — #66 not implemented here)
- [ ] No mature analytics / billing schema invented

### Step 4 — Hard budget / cutoff fail-closed (server-side deny)

Per Spec §2 + Locked #2/#4; Security Dev Plan points **2** and **4**:

| Item | Plan lock |
|------|-----------|
| Enforcement | **Server-side** — when Participant budget exhausted, further metered Assistant/tool invocations **deny** |
| Soft-warn-only | **Rejected** as sole control |
| Wall bind | Cutoff path remains behind **#67**; deny must **not** leak private fields |
| Budget status | Must **not** become privilege escalation or field-leak channel (no private FieldClass / StrategyBody / LoginEmail / ContactEmail / other Participants’ secrets via status payloads) |

**Acceptance:**

- [ ] At/over budget → further metered invocations **deny** server-side (fail-closed)
- [ ] Soft-warn-only is **not** the sole control
- [ ] Cutoff deny bodies omit private fields / secrets
- [ ] Budget status responses cannot escalate privileges or leak FieldClass-denied data
- [ ] Handoff notes cite #67 wall-bind (cross-ref only)

### Step 5 — Cross-tenant / wrong-principal meter misuse fail-closed

Per Spec §3 + Locked #3; Security Dev Plan point **3**:

| Item | Plan lock |
|------|-----------|
| Cross-tenant | Wrong principal **cannot** consume / burn another Participant’s budget |
| Deny | Fail-closed (**403**/**404**); uniform deny bodies |
| Leakage | **No** private fields / other Participants’ meter internals / FieldClass secrets in errors or status |

**Verify tasks (required):**

- [ ] Cross-tenant meter misuse → deny; cannot burn another’s budget
- [ ] Deny/status bodies contain no private FieldClass values / auth secrets / other Participants’ private meter details beyond uniform deny
- [ ] Logging does not dump raw tokens / private payloads / secret FieldClass values on deny paths

### Step 6 — Sibling surfaces: #66 consumer / #67 wall / #69 minimal status only (do not implement siblings)

Per Spec Locked #4/#9 + §6 OUT; Security Dev Plan points **4** and **7**:

| Surface | Plan lock |
|---------|-----------|
| #66 thin Assistant | **Primary metered consumer** — cross-ref only; do **not** implement Assistant runtime under #68 |
| #67 hard wall | Metered path **still wall-bound** — cross-ref only; do **not** implement wall/scrubber under #68 |
| #69 X2 UI/bot | May show budget status **minimally** to respect cutoff — **not** platform-owner admin; do **not** implement #69 here |
| #18 parent | Spec/SD remains framed separately — do **not** unlock as a whole |
| #7 identity-seal | **Distinct** — do not merge/rewrite PoC seal stub |

**Acceptance:**

- [ ] Plan/handoff cites #66 primary consumer + #67 wall-bind + #69 minimal-status-only (cross-ref)
- [ ] No #66 / #67 / #69 / #7 / #18-unlock implementation tasks under this Story
- [ ] No owner-admin cost UI invented for #69 or otherwise

### Step 7 — Automated tests (binding Spec §8 / §8.1)

Schedule automated tests (or equivalent evidence) covering:

| Case | Expected |
|------|----------|
| Under-budget allow | Metered Assistant/tool invocation **allow** (subject to #67 wall) |
| At / over budget deny | Further metered invocations **deny** server-side (fail-closed) — not soft-warn-only |
| Cross-tenant meter misuse | Deny; cannot burn another Participant’s budget |
| Unauthenticated deny | Deny (**401**); cannot consume budget; no private leakage |

**Acceptance:**

- [ ] Test suite covers all Spec §8.1 cases
- [ ] Failures assert status + **absence** of private/secret fields in payloads and deny bodies
- [ ] Tests exercise real meter/cutoff/authz paths (not mocked-away always-allow / always-deny that skips Participant scoping)
- [ ] Soft-warn-only is **not** accepted as sole evidence of cutoff

### Step 8 — Soft audit/OTel weave + Soft O7 align-if-on-path only (no 5th Story / no second product)

Per Spec §4 + Locked #6/#7; Security Dev Plan point **8**:

| Touchpoint | Plan lock |
|------------|-----------|
| Meter increment / cutoff deny | Soft: if SA-REV-MVP-C audit/OTel hooks **already on path**, note touchpoints in handoff — weave only |
| Soft O7 | Align MVP-light tags/budgets/kill-switch **only if already on path** — cite roadmap; do **not** invent a second product |
| Invent forbid | **Do not** invent 5th Story, mature owner analytics, Soft O7 product surface, or billing |

**Acceptance:**

- [ ] Handoff notes Soft audit/OTel touchpoints **only if** hooks already on path (or explicitly “none on path — no invent”)
- [ ] No 5th Story / Soft O7 second product / mature analytics tasks executed
- [ ] Soft O7 align documented as cite-roadmap-only when applicable

### Step 9 — Explicit OUT (do **not** implement Spec §6)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Mature metering / platform-owner cost UI | A8 mature → **V3** |
| Escrow / settlement / checkout; full billing product | OUT |
| Thin Assistant runtime features (#66) | Sibling — primary consumer relationship only |
| Hard wall (#67) | Sibling — wall-bind only |
| UI/bot (#69) except minimum surface needed to respect cutoff | Sibling may show budget status minimally — **not** owner admin; **do not implement #69 here** |
| Inventing a 5th Story for OTel/audit/idempotent | Soft weave only (Step 8) |
| Soft O7 as a second invent product | Align only if already on path |
| MotorMarket / DC4; Cognito/SSO inventing | OUT |
| Unlocking #26 early; #27 | #26 backlog; #27 HOLD |
| Unlocking #18 Spec/SD as a whole | HOLD |
| Rewriting / merging PoC #7 identity-seal | Distinct — out |
| Drafting or implementing #66 / #67 / #69 in this plan | **Forbidden** — #68 ONLY |

### Step 10 — Secrets / cost / zero MM-DC4; local $0

**Secrets (reuse #5 patterns):**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings / LLM keys
- [ ] Connection strings + signing material = env / placeholders only
- [ ] No tokens in query strings or meter bodies
- [ ] No logging of raw tokens / keys / credentials / secret FieldClass values / full budget internals that leak cross-tenant
- [ ] No Cognito/SSO/IdP/vault/billing SDK PackageReferences added by this Story

**Host / spend:**

- [ ] Remains **local / $0**
- [ ] ECS Express Mode **sketch only** if README already mentions — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource provision that creates spend
- [ ] **No** LLM/API provision by this Story — if spend ever proposed → escalate **COO → CEO**

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, packages, shared libs, schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 11 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] Per-Participant meters exist (minimum viable for Assistant/LLM cutoff); scoped to Participant
- [ ] Hard cutoff fail-closed server-side; soft-warn-only **not** sole control
- [ ] Unauth → **401**; wrong principal / cross-tenant → fail-closed; cannot burn another’s budget
- [ ] Metered path documented #67 wall-bound; budget status not escalation/leak channel
- [ ] Automated tests per Spec §8 / §8.1
- [ ] Soft audit/OTel weave only (no 5th Story); Soft O7 align-if-on-path only (no second product)
- [ ] #66/#67/#69 not implemented; #7 not rewritten; #18 Spec/SD not unlocked
- [ ] `GET /health` still Auth none if host touched
- [ ] Nothing from Step 9 / Spec §6 OUT implemented; gate #26 not opened; #27 not unlocked
- [ ] Secrets hygiene + zero MM/DC4 + local/$0; no Cognito; no LLM provision without COO → CEO
- [ ] #68 ONLY — no sibling Story drafts/impl; Product conflicts escalated if any
- [ ] No product code left undocumented; handoff notes cite Spec + this plan
- [ ] **Security Dev Plan-step weave PASS** + Security QA confirm completed before treating plan as unlocked (see §6)

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec Locked #0 / SA A8-min pick A | Per-Participant meters + hard cutoff; mature → V3 | Steps 1–4, 7, 9 |
| Spec §1 + Locked #1 | Per-Participant minimum meters; primary consumer #66 | Steps 3, 7–8 |
| Spec §2 + Locked #2 | Hard cutoff fail-closed — not soft-warn-only | Steps 4, 7 |
| Spec Locked #3 + §3 | Cross-tenant / unauth cannot burn budget | Steps 2, 5, 7 |
| Spec Locked #4 | Metered path #67 wall-bound; budget status not leak/escalation | Steps 1, 4, 6–7 |
| Spec Locked #5 + §3 | Authn fail-closed on meter APIs; uniform deny; no private leak | Steps 2, 5, 7 |
| Spec §4 + Locked #6/#7 | Soft audit/OTel weave; Soft O7 align-if-on-path | Step 8; Explicit OUT |
| Spec Locked #8/#9/#10 + §6 OUT | A8-minimum; V3; #69 minimal; Gate #26/#27; $0 | Steps 6, 9–10; Explicit OUT |
| Spec §7 Host / cost | Extend O10; local/$0; ECS sketch; spend → COO → CEO | Steps 1, 10 |
| Spec §5 Security Spec 1–10 | Upstream Spec Security bind | §6 Security Dev Plan-step woven 1–10 |
| Spec §8 row 1 / Issue AC | Per-Participant meters — minimum viable for cutoff | Steps 3, 7 |
| Spec §8 row 2 / Issue AC | Hard budget/cutoff server-side deny | Steps 4, 7 |
| Spec §8 row 3 / Issue AC | Unauth/wrong-principal cannot burn budget | Steps 2, 5, 7 |
| Spec §8 row 4 / §8.1 / Issue AC | Automated tests | Step 7 |
| Spec §8 row 5 / Issue AC | A8-minimum MVP; mature → V3; no billing invent | Steps 9–10; Explicit OUT |
| Spec §8 row 6 / Issue AC | Soft audit/OTel weave only — no 5th Story | Step 8 |
| #66 / #67 / #69 Specs cross-ref only | OUT — do not implement | Steps 6, 8–9 |
| #7 distinct | Identity-seal — do not merge | Steps 1, 9 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-qa-confirm.md` (points 1–10 MET; SoR PR **#72** @ `32d2e0bc`)  
**Spec Security points-review:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-points-review.md` (PASS 10/10)  
**Spec Security checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-checklist.md` (PR **#71**)  
**Prior SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Spec Security binding:** Spec §5 points 1–10  
**Spec SoR CLEAR:** tip `main` @ `f133e90` / PR **#73**

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Per-Participant meters tasks** — Plan schedules minimum viable meters for MVP-metered Assistant/LLM (and Product-named Stage C metered surfaces without inventing extras); counters scoped to Participant; verify | Step 3 meters table + #66 primary consumer cross-ref; Participant-scoped; no invent extras; Step 7 under-budget allow; Locked #1 | Steps 3, 7, 11; Locked #1 |
| 2 | **Hard cutoff fail-closed tasks** — Plan requires when budget exhausted, further metered Assistant/tool invocations **deny server-side** (fail-closed) — **not** soft warn only; verify | Step 4 cutoff lock; soft-warn-only rejected as sole control; Step 7 at/over-budget deny; Locked #2 | Steps 4, 7, 11; Locked #2 |
| 3 | **Cross-tenant / unauth cannot burn budget** — Plan schedules unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed; verify | Steps 2, 5 verify tasks + Step 7 cross-tenant / unauth cases; Locked #3 | Steps 2, 5, 7, 11; Locked #3 |
| 4 | **Metered path still wall-bound (#67)** — Plan requires metered Assistant/tool path remains behind agent/tool hard wall; budget status must not become privilege escalation or field-leak channel; verify | Steps 1, 4, 6 wall-bind + budget status hygiene (no FieldClass/StrategyBody/LoginEmail leak); Locked #4 | Steps 1, 4, 6–7, 11; Locked #4 |
| 5 | **Authn fail-closed on meter APIs** — Plan schedules unauthenticated → **401**; wrong principal → **403**/**404**; uniform deny; no private leakage via meter/budget payloads; verify | Step 2 authn table + Step 5 deny hygiene; Step 7 unauth case; Locked #5 | Steps 2, 5, 7, 11; Locked #5 |
| 6 | **OUT locked (A8-minimum only)** — Plan keeps **A8-minimum** MVP; mature metering / platform-owner cost UI → **V3**; no inventing full billing/settlement/escrow; Soft O7 align only if already on path (no second product) | Step 8 Soft O7 align-if-on-path only; Step 9 OUT: A8-min; mature → V3; no billing/escrow; Locked #7/#8 | Steps 8–9; Explicit OUT; Locked #7/#8 |
| 7 | **Sibling surfaces** — Plan does not invent #69 owner-admin suite; #69 may show budget status **minimally** to respect cutoff only; keep #66/#67/#69 separate plans | Step 6 sibling table: #66 consumer / #67 wall / #69 minimal-status only; Locked #9 | Steps 6, 8–9; Locked #9; Explicit OUT |
| 8 | **No 5th Story / Gate HOLDs** — Soft OTel/audit/idempotent = weave only on named plans if meters touch those hooks (no 5th Story); Gate **#26** backlog until delivery; Gate **#27** HOLD; no Cognito/MM/DC4/vault invent | Step 8 Soft audit/OTel weave only; Step 9 OUT; Gate #26 backlog; #27 HOLD; no Cognito/MM/DC4; Locked #6/#10 | Steps 8–10; Explicit OUT; Locked #6/#10 |
| 9 | **Cost / spend** — PoC **$0**; any named LLM/API spend → **COO → CEO**; Plan does not provision spend without that path | Step 10 local/$0; no LLM provision; Cost/critical → COO → CEO; Locked #10 | Step 10; Cost/critical; Locked #10 |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then | See handshake note below. Done-list requires Security QA confirm before PASS; SD HOLD | Handshake note; Done-list Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Senior Security → Security QA confirms.** **SD stays HOLD** until Dev Plan QA + Security QA PASS + Chief Dev Planner unlock (+ CPM per ops).

---

## 7. Explicit OUT

Mirror Spec §6 / issue #68 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Mature metering / platform-owner cost UI | A8 mature → **V3** |
| Escrow / settlement / checkout; full billing product | OUT |
| Thin Assistant runtime features (#66) | Sibling — primary consumer relationship only |
| Hard wall (#67) | Sibling — wall-bind only |
| UI/bot (#69) except minimum surface needed to respect cutoff | Sibling may show budget status minimally — **not** owner admin; **do not implement #69** |
| Inventing a 5th Story for OTel/audit/idempotent | Soft weave only (§4 Spec / Step 8) |
| Soft O7 as a second invent product | Align only if already on path |
| MotorMarket / DC4; Cognito / SSO inventing | OUT |
| Unlocking #26 early; #27 | #26 backlog; #27 HOLD |
| Unlocking #18 Spec/SD as a whole | **HOLD** |
| Rewriting / merging PoC #7 identity-seal | Distinct — out |
| Drafting or implementing #66 / #67 / #69 in this plan | **Forbidden** — #68 ONLY |

---

## 8. Cost/critical

**#68 must not procure AWS / Cognito / IdP / vault / LLM spend.** Any paid AWS provision, Cognito/SSO/IdP spend, vault/KMS, named LLM/API provision, or other spend proposal → escalate **COO → CEO**. Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, LLM key provision, or vault/billing tasks. ECS Express Mode remains **README sketch only** if present. Gate **#26** stays backlog — do not open. Gate **#27** HOLD. Do **not** unlock #18 Spec/SD as a whole.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-28__devplan__plan__mvp-stage-c-a8-minimum-meters-budgets.md`
- [ ] Spec coverage §§1–7 + §8 AC + §8.1 + issue #68 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] **#68 ONLY** — #66/#67/#69 **cross-ref only** (not drafted/implemented); #7 distinct; gate #26 backlog; #27 HOLD; #18 Spec/SD HOLD
- [ ] Primary consumer #66 + metered path #67 wall-bound cited; hard cutoff fail-closed (not soft-warn-only)
- [ ] Budget status not privilege escalation / field-leak channel; authn fail-closed; cross-tenant deny; automated tests scheduled
- [ ] Soft audit/OTel weave only; Soft O7 align-if-on-path only — no 5th Story / second product / billing invent
- [x] **Security Dev Plan-step points 1–10 all woven** with cites (table §6) — **HOLD PASS** until Senior Security → Security QA confirms
- [ ] No Cognito/SSO/IdP/vault/AWS/LLM spend instructions
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **HOLD PASS** — **Security QA confirm required** on Dev Plan-step points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner
- [ ] **SD HOLD** until Dev Plan QA + Security QA PASS + Chief unlock

**Next:** Dev Plan QA verifies with evidence → ask Senior Security → Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). **Dev Plan QA must NOT PASS until Security QA confirms.** SD starts only after Chief Dev Planner unlock (+ CPM per ops). Parent may run #66/#67/#69 separately — **this plan does not draft them**.

---

## 10. Done-list for SD (after plan QA PASS + Security QA confirm + Chief unlock)

- [ ] Step 1: Confirm host; meter path #67 wall-bound (cross-ref); keep `GET /health` Auth none if touched
- [ ] Step 2: #5 principal on meter/budget/cutoff paths; unauth → **401**; deny hygiene
- [ ] Step 3: Per-Participant minimum meters; primary consumer #66 relationship (cross-ref); no billing invent
- [ ] Step 4: Hard cutoff fail-closed server-side; soft-warn-only not sole control; budget status not leak/escalation
- [ ] Step 5: Cross-tenant / wrong-principal → fail-closed; cannot burn another’s budget
- [ ] Step 6: Do **not** implement #66/#67/#69/#7; #69 minimal-status note only
- [ ] Step 7: Automated tests per Spec §8 / §8.1
- [ ] Step 8: Soft audit/OTel weave only if hooks on path; Soft O7 align-if-on-path only; no 5th Story
- [ ] Step 9: Do not implement Spec §6 OUT; do not open #26; do not unlock #27/#18
- [ ] Step 10: Secrets hygiene; local/$0; zero MM/DC4; no Cognito; no LLM provision without COO → CEO; ECS sketch only
- [ ] Step 11: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
