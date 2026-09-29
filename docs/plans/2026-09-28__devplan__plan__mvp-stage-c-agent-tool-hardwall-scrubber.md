# Dev Plan — MVP Stage C Agent/tool hard wall + response scrubber (#67)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner PRIORITY **#67 ONLY** — Agent/tool hard wall + response scrubber (Stage C · #18 dual-wall remainder eng-facing)  
**Issue:** https://github.com/ioaikh/dealoware/issues/67  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A (CEO ACCEPTED) · `stage:mvp` · Stage C · #18 dual-wall remainder (eng-facing slice)  
**DOC-FLOW:** `plans/2026-09-28__devplan__plan__mvp-stage-c-agent-tool-hardwall-scrubber.md`  
**Constraints:** **#67 ONLY** — do **not** draft #66/#68/#69 or parent #18 framing Spec/SD. Keep siblings separate (cross-ref only). Consume Stage A/B tip on `main` @ `ca827a2` (#31+#32+#40+#41+#42); do **not** rewrite Option A; do **not** claim Stage B delivered agent wall. Soft **#41** Assistant OUT closes only with sibling **#66** under **this** wall — **not** a Stage B claim. Distinct from identity-seal **#7**. PoC **$0**. Gate **#26** stays **backlog** until Stage C **delivery** — do **not** open/unlock now. Gate **#27** HOLD. Do **not** unlock #18 Spec/SD as a Field-capture whole. No Cognito/MM/DC4; no vault/KMS inventing; no MCP/public tool marketplace; no 5th Story for OTel/audit/idempotent (Soft Spec weave only). Any named LLM/API spend → escalate **COO → CEO** (do **not** provision). Spec+SpecQA SoR CLEAR: PR **#73** @ `f133e90`. SD HOLD until Dev Plan QA + Security QA PASS (after Dev Plan-step checklist woven) + Chief unlock.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` | Hard wall contracts; Locked #0–#10; §§1–5; §7 OUT; §8 Host; §9 AC + §9.1 tests; Security Spec §6 |
| Spec QA (Spec gate PASS) | `verification/2026-09-28__spec__verification__mvp-stage-c-agent-tool-hardwall-scrubber.md` | Spec-side bind; Sec10 weave intact; Spec+SpecQA SoR CLEAR PR **#73** @ `f133e90` |
| Spec Security PASS (qa-confirm) | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md` | Spec-step points 1–10 MET — Spec gate unlock prior |
| Spec Security points-review | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-points-review.md` | Senior PASS 10/10 |
| Spec Security checklist (upstream) | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-checklist.md` | Spec-step Security 1–10 |
| Dev Plan Security checklist (binding) | `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-devplan-checklist.md` | Dev Plan-step handshake points **1–10** — woven in §6 |
| SA Security PASS | `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md` | Architecture-step Security PASS prior (cite with SA checklist / points-review trio) |
| SA Security checklist / points-review | `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md`, `...-sa-points-review.md` | SA Security PASS trio |
| Option A architecture (§3b + Stage C) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Platform hard wall; Story split; dual wall all FieldClasses |
| CA PASS Stage C delta (hard wall pick A) | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | Separate gateway + allowlist + same Evaluate scrub; reject prompt-only / parallel ACL |
| BA note #67 | `plans/2026-09-28__ba__note__story-67-agent-tool-hardwall-scrubber.md` | Spec-ready refine; Soft OTel/audit/idempotent weave |
| Stage A #31 Spec (consume) | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | FieldClass + `IFieldPolicy` API/DB wall (CLOSED) — same Evaluate for agent scrub |
| Stage B #41 / #42 Specs (consume tip) | `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md`, `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | StrategyBody ACL; ShareOutbound-after-Accept / AcceptGrant (CLOSED) |
| Stage A/B tip (consume) | `main` @ `ca827a2` (#31+#32+#40+#41+#42) | CLOSED tip — do **not** rewrite; do **not** claim Stage B delivered agent wall |
| Sibling #66 / #68 / #69 Specs | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md`, `...-a8-minimum-meters-budgets.md`, `...-basic-ui-first-party-bot-x2.md` | Cross-ref only — do **not** merge / draft / implement |
| Parent #18 Spec (framing) | `specs/2026-09-28__spec__spec__mvp-stage-c-participant-isolation-option-a-remainder.md` | Framing Spec; does **not** Field-capture this slice |
| Identity-seal #7 (distinct) | `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md` · issue https://github.com/ioaikh/dealoware/issues/7 | **Distinct** surface — do **not** merge/rewrite under #67 |
| Issue #67 | https://github.com/ioaikh/dealoware/issues/67 | Binding AC + OUT |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A · Stage C named slice; Spec/SD HOLD as whole Field capture |
| Gate #26 (backlog) | https://github.com/ioaikh/dealoware/issues/26 | Do **not** open now — backlog until Stage C delivery |
| Gate #27 (HOLD) | https://github.com/ioaikh/dealoware/issues/27 | HOLD |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO Stage C named slice — eng-facing **#18 dual-wall remainder** (defense #2 agent/tool plane). Soft **#41** Assistant OUT closes with **#66** under this wall — **not** Stage B claim. Mature vault → **V3**. Conflicts → PM → Product → CEO. Cost/critical → **COO → CEO** (LLM/API spend); PoC **$0**.

**Soft Spec weave (binding note — not a 5th Story):** Where hard-wall deny/scrub or offer-touching tool paths hit SA-REV-MVP-C **OTel / audit / idempotent-offers** hooks already on path, SD notes/emits touchpoints only — **no** observability product Story, **no** new product surface, **no** separate plan for Soft Spec. Cite Spec Locked #9 + Spec §5.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend the O10 modular monolith with a **separate agent runtime gateway** so agents call **platform tools only** (not raw DB, not arbitrary HTTP to internal APIs). Encode a **deny-by-default tool allowlist** where each tool declares FieldClasses it may `Read` / `ShareOutbound`; undeclared tools denied. Run **server-side scrub** on every tool response / context pack via the **same** Domain `IFieldPolicy.Evaluate` as the API/DB wall — denied fields stripped **before** model context; **reject** prompt-only soft guidance as sole control and **reject** parallel agent ACL tables. **No LoginEmail** tool; LoginEmail never in agent context packs (User-only; OwnAgent Deny held). **ShareOutbound** tools (e.g. `share_contact_email` or equivalent) Allow **only if** AcceptGrant / `HasAcceptGrant` recorded **server-side** — deny regardless of prompt text. Any cross-agent messaging at MVP is **mediated**; payloads scrubbed; prompt cannot escalate. Dual wall binds **all** FieldClasses (open-ended registry; CEO examples ≠ exhaustive). Soft weave OTel/audit/idempotent touchpoints only — **no 5th Story**. Deliver automated tests per Spec §9 / §9.1. **OUT:** #66/#68/#69 product; inventing Soft observability Story; mature vault → V3; MCP/marketplace; Cognito/MM/DC4; opening gate **#26**; #27; unlocking #18 as Field capture; claiming Stage B delivered agent wall; merging identity-seal **#7**. PoC/MVP **$0**; LLM spend → COO → CEO (do not provision). No product code in this artifact — SD instructions only. **#67 ONLY**.

---

## 3. Locked decisions (Spec locks → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | SA hard wall pick A | Separate **agent runtime gateway** + tool allowlist + server-side scrub calling **same** Domain `IFieldPolicy.Evaluate` as API/DB wall | Steps 1–4, 9 |
| 1 | Platform tools only | Agents call **platform tools only** — not raw DB, not arbitrary HTTP to internal APIs | Steps 1–2, 9 |
| 2 | Tool allowlist deny-by-default | Each tool declares FieldClasses it may `Read` / `ShareOutbound`; undeclared tools **denied** | Steps 3, 9 |
| 3 | Server-side scrub | Every tool response / context pack → `IFieldPolicy.Evaluate`; denied fields stripped **before** model context; same policy as API/DB | Steps 4, 9 |
| 4 | No LoginEmail | No LoginEmail tool; LoginEmail never in agent context packs; User-only; OwnAgent Deny held | Steps 5, 9 |
| 5 | ShareOutbound Accept-gated | Share tools require `HasAcceptGrant` / Stage B AcceptGrant **server-side**; deny regardless of prompt text | Steps 6, 9 |
| 6 | Cross-agent mediated | If any cross-agent messaging at MVP: mediated; payloads cannot include denied FieldClasses; prompt cannot escalate | Steps 7, 9 |
| 7 | Reject prompt-only / parallel ACL | Reject system-prompt soft guidance as sole control; reject separate agent ACL tables that drift from FieldPolicy | Steps 2, 4, 7, 9 |
| 8 | Dual wall all FieldClasses | Open-ended registry; dual wall for **all** FieldClasses via same Evaluate; consume A/B tip | Steps 3–4, 8, 10 |
| 9 | Soft OTel/audit/idempotent | Touchpoints only where deny/scrub/offer paths hit SA-REV-MVP-C hooks; **no 5th Story** | Step 8; Soft weave note |
| 10 | Scope label / OUT | Stage C **#18 remainder** dual-wall; mature vault → **V3**; MCP/marketplace OUT; Gate #26 backlog; #27 HOLD; $0; soft #41 → #66 under this wall (not Stage B) | Steps 10–11; Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 modular monolith — Spec §8).  
**Prerequisite:** Stage A/B tip CLOSED on `main` @ `ca827a2` (#31+#32+#40+#41+#42). Consume — do **not** rewrite those Specs or merge Stories. Soft #41 Assistant OUT closes with **#66** under **this** wall — do **not** claim Stage B delivered agent wall.  
**Sibling HOLD:** #66 Thin Assistant; #68 A8 meters/budgets; #69 Basic UI / bot — **cross-ref only**; separate plans/Stories. **Do not draft or implement here.** Parent #18 framing Spec — cross-ref only; do **not** Field-capture as a whole here.  
**Distinct:** Identity-seal **#7** — separate Story/plan; do **not** merge or rewrite under #67.  
**Hold:** Gate #26 open; Gate #27; unlocking #18 Spec/SD as Field capture; Cognito/MM/DC4; MCP/marketplace; vault/KMS; LLM provision — do **not** invent.

### Step 1 — Confirm host layout; agent runtime gateway placement; keep health open

- Keep **`Dealoware.Api` as the only runnable** project (unless Spec/host already documents a separate runnable for the gateway — still no Cognito/IdP/AWS provision).
- Keep **`GET /health`** contract unchanged (**Auth: none**) if any host/DI touch is required for wiring.
- Place **separate agent runtime gateway** in Application/Api + Domain FieldPolicy path (Spec §1 / Locked #0/#1) — agents invoke **platform tools only**.
- Do **not** give agents raw DB access, arbitrary HTTP to internal APIs, or privileged back doors that skip Evaluate.
- Continue Minimal APIs; built-in ASP.NET Core DI only (or host patterns already on tip).
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS spend modules, MM/DC4 refs, MCP marketplace, or LLM provider provision tasks.
- Do **not** open gate #26, unlock #27, unlock #18 Spec/SD as Field capture, or invent #66/#68/#69 Stories.

**Acceptance:**

- [ ] Solution still builds; host layout documents separate agent runtime gateway
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none) if touched
- [ ] Gateway path documented as **platform tools only** — no raw DB / arbitrary internal HTTP
- [ ] No Cognito/IdP/AWS/MM/MCP/LLM provision tasks; gate #26 not opened; #27 HOLD; #18 Spec/SD not Field-unlocked

### Step 2 — Separate agent runtime gateway (platform tools only; reject prompt-only sole control)

Per Spec §1 + Locked #0/#1/#7:

| Item | Plan lock |
|------|-----------|
| Boundary | Separate **agent runtime gateway** — agents invoke **platform tools only** |
| Forbidden | Raw DB access; arbitrary HTTP to internal APIs; privileged back doors that skip Evaluate |
| Bind | Same Domain FieldPolicy as API/DB wall (defense #2) |
| Reject | System-prompt soft guidance as **sole** control; parallel agent ACL tables |

**Verify tasks (required):**

- [ ] Tool invocation path goes through gateway (not direct DB / internal HTTP)
- [ ] Control architecture asserts gateway + Evaluate — **not** prompt-only soft wall
- [ ] No parallel agent ACL table invent that drifts from FieldPolicy

**Acceptance:**

- [ ] Agents can only reach registered platform tools via gateway
- [ ] Tests / design notes reject prompt-only as sole control (ties Step 9 case)
- [ ] Same Domain `IFieldPolicy` cited for agent plane — no second ACL product

### Step 3 — Tool allowlist deny-by-default (FieldClass Read / ShareOutbound declarations)

Per Spec §2 + Locked #2/#8:

| Item | Plan lock |
|------|-----------|
| Declaration | Each allowlisted tool declares FieldClasses it may `Read` and/or `ShareOutbound` |
| Undeclared | Tools not on allowlist → **deny** |
| Examples | May use locked FieldClass **examples** (LoginEmail, ContactEmail, DisplayName, StrategyBody, etc.) — examples ≠ exhaustive; do **not** invent new FieldClasses |
| Actions | Align Option A actions: `Read` · `Write` · `List` · `ShareOutbound` as applicable to tool I/O |
| Dual wall | Allowlist + Evaluate cover **all** FieldClasses via same registry — not CEO-example-only wall |

**Acceptance:**

- [ ] Allowlist encoded deny-by-default; undeclared tool → deny
- [ ] Each allowlisted tool declares Read and/or ShareOutbound FieldClasses
- [ ] No new FieldClass invent beyond consume of #31 registry
- [ ] Dual-wall posture documented for all FieldClasses (open-ended registry)

### Step 4 — Server-side scrub before model context (same IFieldPolicy.Evaluate)

Per Spec §3 + Locked #3/#7/#8:

| Item | Plan lock |
|------|-----------|
| Path | Every tool response / context pack runs `IFieldPolicy.Evaluate(principal, fieldClass, action, resourceContext)` (or equivalent) |
| Effect | Denied fields **stripped before** model / agent context |
| Same policy | Must call the **same** `IFieldPolicy` as API/DB wall — **no** parallel scrub table |
| Reject | Prompt-only scrub; separate agent ACL / scrub tables |

**Acceptance:**

- [ ] Every tool response / context pack passes Evaluate (or equivalent) before model context
- [ ] Denied fields absent from model-bound context packs
- [ ] Scrub uses same Domain `IFieldPolicy` as #31 API/DB wall — no parallel table
- [ ] Consume #31 — do **not** rewrite Field ACL Spec / Story

### Step 5 — No LoginEmail tool / never in agent context

Per Spec §4 + Locked #4:

| Rule | Plan lock |
|------|-----------|
| LoginEmail | User-only; **no** LoginEmail tool; **never** in agent context packs; OwnAgent Deny held from Stage A |
| Do not | Invent LoginEmail agent tool, OwnAgent exception, or LoginEmail in scrubbed “placeholders that leak” |

**Acceptance:**

- [ ] No LoginEmail tool registered on allowlist
- [ ] Automated/manual inspect: LoginEmail **never** present in agent context packs / tool outputs
- [ ] OwnAgent Deny held — no #31 rewrite to weaken

### Step 6 — ShareOutbound Accept-gated server-side; prompt cannot escalate

Per Spec §4 + Locked #5:

| Item | Plan lock |
|------|-----------|
| Share tools | e.g. `share_contact_email` or equivalent — Allow ContactEmail ShareOutbound **only if** Accept recorded (`HasAcceptGrant` / Stage B AcceptGrant) **server-side** |
| Else | Deny regardless of prompt text / injection |
| Escalation | Prompt text **cannot** grant FieldClasses Evaluate denies |
| Consume | Stage B #42 AcceptGrant patterns — do **not** rewrite #42 Spec; do **not** implement #42 product under this Story beyond wall gating |

**Verify tasks (required):**

- [ ] Pre-Accept share tool → **deny** regardless of prompt text
- [ ] Post-Accept (AcceptGrant present) → Allow only per Evaluate + allowlist declaration
- [ ] Injection / prompt escalation cannot bypass Evaluate deny

**Acceptance:**

- [ ] ShareOutbound gated on AcceptGrant server-side
- [ ] Prompt cannot escalate FieldClass rights
- [ ] #42 Spec not rewritten; #67 only implements wall gating for share tools

### Step 7 — Cross-agent mediated (if any); reject prompt-only / parallel ACL

Per Spec §4 + Locked #6/#7:

| Item | Plan lock |
|------|-----------|
| Cross-agent (if any at MVP) | Mediated gateway path; payloads scrubbed; no denied FieldClasses |
| Trust | Gateway + scrub — **not** model trust |
| Reject | Prompt-only sole control; separate agent ACL tables |
| If none | Document “no cross-agent messaging at MVP” — do **not** invent a messaging product under #67 |

**Acceptance:**

- [ ] If cross-agent path exists: mediated + scrubbed; denied FieldClasses absent
- [ ] If none: explicit non-delivery noted — no invented messaging product
- [ ] Architecture/tests reject prompt-only sole control and parallel ACL tables

### Step 8 — Soft OTel / audit / idempotent-offers weave only (no 5th Story)

Per Spec §5 + Locked #9:

| Touchpoint | Plan lock (Soft Spec weave — **not** a Story) |
|------------|-----------------------------------------------|
| Hard-wall deny | Soft: emit audit/OTel signal **if** SA-REV-MVP-C hooks already on path |
| Scrub strip | Soft: observability touchpoint when denied fields stripped |
| Offer-touch tools | Soft: idempotent-offers / audit weave where share/offer-write tools touch AcceptGrant paths |
| Invent forbid | **Do not** invent a 5th Story, observability product surface, Soft Spec plan, or sibling under #67 |

**Soft Spec weave note (binding):** Soft Spec OTel/audit/idempotent touchpoints **ONLY** — where hard-wall deny/scrub or offer-touching tool paths hit SA-REV-MVP-C hooks already on path. **NO** 5th Story / observability product. Cite Spec Locked #9 + §5 + §7 OUT row “Inventing a 5th Story…”.

**Acceptance:**

- [ ] Where deny/scrub/offer paths hit existing SA-REV-MVP-C hooks, soft touchpoints noted/wired
- [ ] No 5th Story / new observability product / Soft Spec sibling plan created
- [ ] Handoff cites Soft weave-only (not product AC)

### Step 9 — Automated tests (binding Spec §9 / §9.1)

Schedule automated tests (or equivalent evidence) covering:

| Case | Expected |
|------|----------|
| Allowlisted tool + scrub | OK; response/context passes Evaluate; allowed fields only |
| Denied field in tool response | Stripped **before** model context |
| LoginEmail | Never present in agent context packs / tool outputs |
| Pre-Accept ShareOutbound | Deny regardless of prompt text |
| Stranger / cross-tenant | Deny; no private-field leakage |
| Unauthenticated | Deny; no private fields in errors |
| Prompt-only soft guidance as sole control | **Rejected** as architecture/control (tests assert gateway+Evaluate path, not prompt-only) |

**Acceptance:**

- [ ] Test suite covers all Spec §9.1 cases
- [ ] Failures assert status + **absence** of denied/private fields in context packs and deny bodies
- [ ] Tests exercise real gateway + Evaluate paths (not mocked-away scrub that always strips everything / prompt-only fake)

### Step 10 — Explicit OUT (do **not** implement Spec §7)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Thin Assistant product UX / strategy interpretation | **#66** — sibling; wall is mandatory binding for that runtime — do **not** implement Assistant here |
| A8 meters/budgets | **#68** — sibling |
| Basic UI / bot surface | **#69** — sibling |
| Inventing a 5th Story for OTel/audit/idempotent | Soft Spec weave only (Step 8 / Spec §5) |
| Mature PII vault retention/erasure (A9 mature) | → **V3** |
| MCP breadth; public OpenAPI; public tool marketplace | OUT |
| Inventing spend / Cognito / MotorMarket / DC4 | OUT |
| Unlocking #26 before Stage C delivery; #27 | #26 backlog; #27 HOLD |
| Rewriting parent #18 Spec as Field capture; rewriting Option A | Framing Spec separate; Option A cite-only |
| Claiming Stage B delivered agent wall | Soft #41 OUT closes with **#66** under **this** wall — not Stage B |
| Identity-seal #7 merge/rewrite | Distinct Story — out |
| Drafting or implementing #66 / #68 / #69 in this plan | **Forbidden** — #67 ONLY |

### Step 11 — Secrets / cost / zero MM-DC4; local $0; LLM → COO → CEO

**Secrets (reuse #5 / #31 patterns):**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings / LLM keys
- [ ] Connection strings + signing material = env / placeholders only
- [ ] No tokens in query strings or agent context dumps
- [ ] No logging of raw tokens / keys / credentials / secret FieldClass values / full model prompts with secrets
- [ ] No Cognito/SSO/IdP/vault/MCP/LLM SDK PackageReferences that imply provision/spend by this Story

**Host / spend:**

- [ ] Remains **local / $0**
- [ ] ECS Express Mode **sketch only** if README already mentions — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource / IdP / vault / LLM provision that creates spend
- [ ] Any named **LLM/API spend** → escalate **COO → CEO** — do **not** provision in this Story
- [ ] Other spend proposals → escalate **CPM → COO → CEO** as applicable

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, packages, shared libs, schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 12 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] Separate agent runtime gateway; platform tools only; no raw DB / arbitrary internal HTTP
- [ ] Tool allowlist deny-by-default with FieldClass Read/ShareOutbound declarations; undeclared denied
- [ ] Server-side scrub via same `IFieldPolicy.Evaluate`; denied stripped before model context
- [ ] No LoginEmail tool/context; OwnAgent Deny held
- [ ] ShareOutbound Accept-gated server-side; prompt cannot escalate
- [ ] Cross-agent mediated if any; reject prompt-only / parallel ACL
- [ ] Soft OTel/audit/idempotent touchpoints only — no 5th Story
- [ ] Automated tests per Spec §9 / §9.1
- [ ] #31 consumed not rewritten; #41/#42 tip consumed; soft #41 OUT closes with #66 under this wall (not Stage B claim)
- [ ] `GET /health` still Auth none if host touched
- [ ] Nothing from Step 10 / Spec §7 OUT implemented; gate #26 not opened; #27 HOLD; #18 Spec/SD not Field-unlocked; #7 not merged
- [ ] Secrets hygiene + zero MM/DC4 + local/$0; no Cognito; no LLM provision (escalate COO → CEO)
- [ ] #67 ONLY — no sibling Story drafts/impl; Product conflicts escalated if any
- [ ] No product code left undocumented; handoff notes cite Spec + this plan

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec Locked #0 / SA pick A | Separate gateway + allowlist + same-Evaluate scrub | Steps 1–4, 9 |
| Spec §1 + Locked #1 | Platform tools only; not raw DB / arbitrary internal HTTP | Steps 1–2, 9 |
| Spec §2 + Locked #2 | Tool allowlist deny-by-default; FieldClass declarations | Steps 3, 9 |
| Spec §3 + Locked #3 | Server-side scrub before model context; same IFieldPolicy | Steps 4, 9 |
| Spec §4 + Locked #4 | No LoginEmail tool / never in agent context | Steps 5, 9 |
| Spec §4 + Locked #5 | ShareOutbound Accept-gated; prompt cannot escalate | Steps 6, 9 |
| Spec §4 + Locked #6 | Cross-agent mediated; payloads scrubbed | Steps 7, 9 |
| Spec Locked #7 | Reject prompt-only / parallel ACL | Steps 2, 4, 7, 9 |
| Spec Locked #8 | Dual wall all FieldClasses; consume tip | Steps 3–4, 10–12 |
| Spec §5 + Locked #9 | Soft OTel/audit/idempotent weave only — no 5th Story | Step 8; Soft weave note |
| Spec Locked #10 + §7 OUT | Scope; vault → V3; MCP OUT; Gate #26/#27; $0; soft #41 → #66 | Steps 10–11; Explicit OUT |
| Spec §8 Host / cost | Extend O10; local/$0; ECS sketch; LLM → COO → CEO | Steps 1, 11 |
| Spec §6 Security Spec 1–10 | Upstream Spec Security bind (PASS) | §6 Security Dev Plan-step woven 1–10 |
| Spec §9 row 1 / Issue AC | Platform tools only; separate gateway | Steps 1–2, 9 |
| Spec §9 row 2 / Issue AC | Tool allowlist deny-by-default | Steps 3, 9 |
| Spec §9 row 3 / Issue AC | Server-side scrub before model | Steps 4, 9 |
| Spec §9 row 4 / Issue AC | No LoginEmail in tools/context | Steps 5, 9 |
| Spec §9 row 5 / Issue AC | ShareOutbound Accept-gated | Steps 6, 9 |
| Spec §9 row 6 / Issue AC | Cross-agent mediated; prompt cannot escalate | Steps 7, 9 |
| Spec §9 row 7 / §9.1 / Issue AC | Automated tests incl. reject prompt-only | Step 9 |
| Spec §9 row 8 / Issue AC | #18 remainder dual-wall; vault → V3; MCP OUT | Steps 10–11; Explicit OUT |
| Spec §9 row 9 / Issue AC | Soft Spec weave only — no 5th Story | Step 8 |
| #31 Spec consume | Same IFieldPolicy Evaluate for scrub | Steps 4, 12 |
| #41/#42 Spec tip consume | StrategyBody ACL; AcceptGrant ShareOutbound | Steps 5–6, 12 |
| #66/#68/#69 Specs cross-ref only | OUT — do not implement | Steps 10, 12 |
| #7 identity-seal distinct | OUT — do not merge | Step 10; Explicit OUT |
| Spec QA + Spec Security PASS trio | Spec gate CLEAR (PR #73 @ `f133e90`; Spec Sec PASS) | Sources; §6 handshake |
| SA Security PASS | Architecture-step prior | Sources |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-spec-qa-confirm.md` (points 1–10 MET; with points-review + Spec checklist trio)  
**SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Spec Security binding:** Spec §6 points 1–10 (Spec-step — **not** a substitute for Dev Plan-step checklist)  
**Spec SoR CLEAR:** PR **#73** @ `f133e90`

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Agent runtime gateway tasks** — Plan schedules agents on **platform tools only** (not raw DB, not arbitrary internal HTTP); verify fail-closed off-gateway paths | Steps 1–2 place separate agent runtime gateway; platform tools only; forbid raw DB / arbitrary internal HTTP / privileged back doors; Step 9 tests; Locked #0/#1 | Steps 1–2, 9, 12; Locked #0/#1 |
| 2 | **Tool allowlist deny-by-default tasks** — Plan schedules each tool declaring FieldClasses it may `Read` / `ShareOutbound`; undeclared tools denied; verify | Step 3 allowlist deny-by-default with FieldClass Read/ShareOutbound declarations; undeclared denied; Step 9 tests; Locked #2/#8 | Steps 3, 9, 12; Locked #2/#8 |
| 3 | **Server-side scrub-before-model tasks** — Plan requires every tool response / context pack runs `IFieldPolicy.Evaluate`; denied fields stripped **before** model context; verify | Step 4 scrub via same Domain `IFieldPolicy.Evaluate`; denied stripped before model; no parallel scrub table; Step 9 tests; Locked #3/#7/#8 | Steps 4, 9, 12; Locked #3/#7/#8 |
| 4 | **No LoginEmail in agent context tasks** — Plan schedules LoginEmail User-only; OwnAgent Deny held; no LoginEmail tool / no LoginEmail in context packs; verify | Step 5 no LoginEmail tool/context; OwnAgent Deny held; Step 9 LoginEmail case; Locked #4 | Steps 5, 9, 12; Locked #4 |
| 5 | **ShareOutbound Accept-gated tasks** — Plan requires share tools check `HasAcceptGrant` / AcceptGrant **server-side**; deny regardless of prompt text; prompt injection cannot grant Evaluate denies; verify | Step 6 ShareOutbound Accept-gated server-side; prompt cannot escalate; pre-/post-Accept verify; Step 9 cases; Locked #5 | Steps 6, 9, 12; Locked #5 |
| 6 | **Reject prompt-only / parallel ACL** — Plan explicitly **rejects** system-prompt soft guidance as sole control and **rejects** separate agent ACL tables; verify no soft-wall-only or parallel-table tasks land | Steps 2, 4, 7 reject prompt-only sole control + parallel agent ACL; Step 9 architecture reject case; Locked #7 | Steps 2, 4, 7, 9, 12; Locked #7; Explicit OUT |
| 7 | **Dual wall all FieldClasses; soft #41 path** — Plan schedules dual wall for open-ended FieldClass registry; soft #41 OUT closes only with **#66** under this wall (not Stage B claim); keep #66/#68/#69 separate plans | Steps 3–4 dual wall all FieldClasses via same Evaluate; Step 10 soft #41 → #66 under this wall (not Stage B); siblings cross-ref only; Locked #8/#10 | Steps 3–4, 8, 10–12; Explicit OUT; Locked #8/#10 |
| 8 | **Cross-agent mediated exfil posture** — Plan schedules (if MVP includes cross-agent messaging) payloads cannot include denied FieldClasses; threats addressed by gateway + scrub, not model trust; verify | Step 7 mediated + scrubbed if any; else explicit non-delivery; gateway+scrub trust (not model); Step 9 stranger/cross-tenant cases; Locked #6/#7 | Steps 7, 9, 12; Locked #6/#7 |
| 9 | **OUT / Gate / spend** — Mature vault → **V3**; MCP/public marketplace OUT; Gate **#26** backlog; Gate **#27** HOLD; no Cognito/MM/DC4/vault invent; Soft OTel/audit/idempotent = weave only (no 5th Story); PoC **$0** | Step 8 Soft weave only — no 5th Story; Step 10 OUT table; Step 11 local/$0; LLM → COO → CEO; Gate #26 backlog; #27 HOLD; Locked #9/#10 | Steps 8, 10–11; Explicit OUT; Cost/critical; Locked #9/#10 |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms. SD stays HOLD until then. Parent #18 Dev Plan does **not** Field-capture this wall | See handshake note below. Done-list requires Security QA confirm before PASS; SD HOLD; #18 framing not Field-captured here | Handshake note; Done-list Dev Plan QA; Explicit OUT |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-28__security__verification__mvp-stage-c-hardwall-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Senior Security → Security QA confirms.** **SD stays HOLD** until Dev Plan QA + Security QA PASS + Chief Dev Planner unlock (+ CPM per ops). Parent #18 Dev Plan does **not** Field-capture this wall.

---

## 7. Explicit OUT

Mirror Spec §7 / issue #67 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Thin Assistant product UX / strategy interpretation (#66) | Sibling — wall is mandatory binding for that runtime |
| A8 meters/budgets (#68) | Sibling |
| Basic UI / bot surface (#69) | Sibling |
| Inventing a 5th Story for OTel/audit/idempotent | Soft Spec weave only (Spec §5 / Step 8) |
| Mature PII vault retention/erasure (A9 mature) | → **V3** |
| MCP breadth; public OpenAPI; public tool marketplace | OUT |
| Inventing spend / Cognito / MotorMarket / DC4 | OUT |
| Unlocking #26 before Stage C delivery; #27 | #26 backlog; #27 HOLD |
| Rewriting parent #18 Spec as Field capture; rewriting Option A | Framing Spec separate; Option A cite-only |
| Claiming Stage B delivered agent wall | Soft #41 OUT closes with **#66** under **this** wall |
| Identity-seal #7 merge/rewrite | Distinct — out |
| Drafting or implementing #66 / #68 / #69 in this plan | **Forbidden** — #67 ONLY |
| Prompt-only soft wall as sole control; parallel agent ACL tables | Rejected architecture — not deliverables |

---

## 8. Cost/critical

**#67 must not procure AWS / Cognito / IdP / vault / KMS / LLM spend.** Any named **LLM/API spend** → escalate **COO → CEO** (do **not** provision). Other paid AWS provision, Cognito/SSO/IdP, vault/KMS, MCP marketplace, or spend proposal → escalate **CPM → COO → CEO** as applicable. Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, vault, MCP marketplace, or LLM provider tasks. ECS Express Mode remains **README sketch only** if present. Gate **#26** stays backlog until Stage C delivery — do not open. Gate **#27** HOLD. Do **not** unlock #18 Spec/SD as Field capture. Soft #41 Assistant OUT closes with **#66** under this wall — not Stage B.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-28__devplan__plan__mvp-stage-c-agent-tool-hardwall-scrubber.md`
- [ ] Spec coverage Locked #0–#10 + §§1–5 + §7 OUT + §8 Host + §9 AC + §9.1 + issue #67 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] **#67 ONLY** — #66/#68/#69 + parent #18 framing **cross-ref only** (not drafted/implemented); identity-seal #7 distinct; gate #26 backlog; #27 HOLD
- [ ] Consumes Stage A/B tip @ `ca827a2` (#31+#32+#40+#41+#42) — **no rewrite**; soft #41 OUT closes with #66 under this wall (not Stage B claim)
- [ ] Gateway + allowlist deny-by-default + same-Evaluate scrub + no LoginEmail + Accept-gated ShareOutbound + mediated cross-agent + reject prompt-only/parallel ACL + automated tests scheduled
- [ ] Soft Spec weave OTel/audit/idempotent touchpoints only — **no 5th Story** (explicit Soft weave note in body)
- [x] **Security Dev Plan-step points 1–10 all woven** with cites (table §6) — **HOLD PASS** until Senior Security → Security QA confirms
- [ ] No Cognito/SSO/IdP/vault/AWS/LLM provision instructions (LLM → COO → CEO)
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4; no MCP/public marketplace
- [ ] Explicit OUT respected (Spec §7)
- [ ] No product code in this artifact (SD instructions only)
- [ ] Spec+SpecQA SoR CLEAR cited (PR **#73** @ `f133e90`); Spec Security PASS trio + SA Security PASS cited
- [ ] **Security QA confirm required** on Dev Plan-step points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner
- [ ] **SD HOLD** until Dev Plan QA + Security QA PASS + Chief unlock

**Next:** Dev Plan QA verifies with evidence → ask Senior Security → Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). **Dev Plan QA must NOT PASS until Security QA confirms.** SD starts only after Chief Dev Planner unlock (+ CPM per ops). Parent/siblings may run separately — **this plan does not draft them**.

---

## 10. Done-list for SD (after plan QA PASS + Security QA confirm + Chief unlock)

- [ ] Step 1: Confirm host; separate agent runtime gateway placement; keep `GET /health` Auth none if touched
- [ ] Step 2: Platform tools only via gateway; reject prompt-only sole control / parallel ACL
- [ ] Step 3: Tool allowlist deny-by-default with FieldClass Read/ShareOutbound declarations
- [ ] Step 4: Server-side scrub every tool response/context via same `IFieldPolicy.Evaluate` before model
- [ ] Step 5: No LoginEmail tool/context; OwnAgent Deny held
- [ ] Step 6: ShareOutbound Accept-gated server-side; prompt cannot escalate
- [ ] Step 7: Cross-agent mediated if any; reject prompt-only / parallel ACL
- [ ] Step 8: Soft OTel/audit/idempotent touchpoints only — no 5th Story
- [ ] Step 9: Automated tests per Spec §9 / §9.1
- [ ] Step 10: Do not implement Spec §7 OUT; do not draft #66/#68/#69; do not open #26; do not claim Stage B agent wall
- [ ] Step 11: Secrets hygiene; local/$0; zero MM/DC4; no Cognito; no LLM provision (COO → CEO)
- [ ] Step 12: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
