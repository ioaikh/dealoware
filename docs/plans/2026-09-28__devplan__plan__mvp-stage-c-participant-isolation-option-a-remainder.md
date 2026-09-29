# Dev Plan — MVP Stage C Participant isolation Option A remainder (#18 parent framing)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner PRIORITY **#18 parent framing ONLY** — Participant isolation Option A remainder (framing Spec, NOT Field capture of #67)  
**Issue:** https://github.com/ioaikh/dealoware/issues/18  
**Parent:** self · Option A (CEO ACCEPTED) · `stage:mvp` · Stage C Spec-step framing (not Field capture of #67)  
**DOC-FLOW:** `plans/2026-09-28__devplan__plan__mvp-stage-c-participant-isolation-option-a-remainder.md`  
**Constraints:** Parent isolation Dev Plan — **maps** #18 AC to (a) Stage A/B CLOSED tip vs (b) Stage C delta. **BIND #67** as eng dual-wall slice — do **not** Field-capture / merge #67 implementation into this plan; do **not** rewrite Option A; do **not** claim A/B delivered agent wall. Named slices **#66/#67/#68/#69** remain separate plans — cross-ref complementary only. Soft **#41** Assistant OUT closes only via **#66+#67** — not Stage B claim. Distinct from identity-seal **#7**. Soft OTel/audit/idempotent weave where isolation deny paths touch — **no 5th Story**. PoC **$0**. Gate **#26** backlog until Stage C **delivery**; Gate **#27** HOLD. No Cognito/vault/MCP/fuller Assistant invent; no MM/DC4. Spec+SpecQA SoR CLEAR: PR **#73** @ `f133e90`. SD HOLD until Dev Plan QA + Security QA PASS (Dev Plan-step checklist woven) + Chief unlock. Named-slice plans gated separately by their own Security QA.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-28__spec__spec__mvp-stage-c-participant-isolation-option-a-remainder.md` | Parent framing contracts; Locked #0–#10; §1 AC map; §2 invariants; §3 slice bind; §4 Soft weave; §5 Security Spec; §6 OUT; §7 Host; §8 AC + §8.1 tests |
| Spec QA (Spec gate PASS) | `verification/2026-09-28__spec__verification__mvp-stage-c-participant-isolation-option-a-remainder.md` | Spec-side bind; Sec10 weave intact; Spec gate PASS |
| Spec Security PASS (qa-confirm) | `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-qa-confirm.md` | Spec-step points 1–10 MET — Spec gate unlock prior (SoR PR #71+#72; tip `main` @ `f133e90` / PR **#73** Spec Soft Soft CLOSE Soft HOLD SoR CLEAR) |
| Spec Security points-review | `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-points-review.md` | Senior PASS 10/10 |
| Spec Security checklist (upstream) | `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-checklist.md` | Spec-step Security 1–10 |
| Dev Plan Security checklist (binding weave) | `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-devplan-checklist.md` | Dev Plan-step handshake points **1–10** — woven in §6 |
| SA Security PASS | `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md` | Architecture-step Security PASS prior |
| SA Security checklist / points-review | `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md`, `...-sa-points-review.md` | SA Security PASS trio |
| Option A architecture (§3b + Stage C) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall end-state; Platform hard wall; open-ended FieldClass; Story split |
| CA PASS Stage C delta | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | Hard wall pick A; thin Assistant; A8-min; X2; #18 remainder map |
| Stage A #31 Spec (CLOSED tip) | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | Field ACL registry + API projection — tip CLOSED |
| Stage A #32 Spec (CLOSED tip) | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Account list fail-closed — tip CLOSED |
| Stage B #40 Spec (CLOSED tip) | `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md` | Instant search discovery scrub — tip CLOSED |
| Stage B #41 Spec (CLOSED tip) | `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md` | Minimal Strategy CRUD + StrategyBody ACL; soft Assistant OUT — tip CLOSED |
| Stage B #42 Spec (CLOSED tip) | `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | ContactEmail ShareOutbound-after-Accept — tip CLOSED |
| Sibling #67 Dev Plan (already drafted — cite; do **not** merge) | `plans/2026-09-28__devplan__plan__mvp-stage-c-agent-tool-hardwall-scrubber.md` | Eng dual-wall BIND — complementary; do not Field-capture here |
| Sibling #67 Spec | `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` | Eng dual-wall Spec — complementary |
| Expected sibling #66 Dev Plan (cross-ref only) | `plans/2026-09-28__devplan__plan__mvp-stage-c-thin-assistant-runtime-x1.md` | Thin OwnAgent Assistant under wall — complementary |
| Expected sibling #68 Dev Plan (cross-ref only) | `plans/2026-09-28__devplan__plan__mvp-stage-c-a8-minimum-meters-budgets.md` | A8-min meters + hard cutoff — complementary |
| Expected sibling #69 Dev Plan (cross-ref only) | `plans/2026-09-28__devplan__plan__mvp-stage-c-basic-ui-first-party-bot-x2.md` | X2 basic UI no bypass — complementary |
| Named-slice Specs #66/#68/#69 | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md`, `...-a8-minimum-meters-budgets.md`, `...-basic-ui-first-party-bot-x2.md` | Cross-ref only — complementary; do not merge |
| Sibling Spec Security PASS ×4 | `verification/2026-09-28__security__verification__mvp-stage-c-{hardwall,thin-assistant,a8-min,x2-ui-bot}-spec-qa-confirm.md` | Named-slice Spec-step Security (separate) |
| Tip delivery | `main` @ `ca827a2` | Stage A/B CLOSED (#31+#32+#40+#41+#42); Gates #24/#25 CLOSED |
| Spec Soft Soft CLOSE Soft HOLD SoR CLEAR | PR **#73** @ `f133e90` | Spec+SpecQA SoR CLEAR (Senior PM) |
| Identity-seal #7 (distinct) | `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md` · issue https://github.com/ioaikh/dealoware/issues/7 | **Distinct** surface — do **not** merge/rewrite under #18 |
| Issue #18 | https://github.com/ioaikh/dealoware/issues/18 | Binding AC + OUT (authz / tenancy isolation) |
| Gate #26 (backlog) | https://github.com/ioaikh/dealoware/issues/26 | Do **not** open now — backlog until Stage C delivery |
| Gate #27 (HOLD) | https://github.com/ioaikh/dealoware/issues/27 | HOLD |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO ACCEPTED Option A dual wall. This Dev Plan frames Stage C unlock / remainder map — eng dual-wall remainder implementation is **#67**. Soft **#41** Assistant OUT closes only via **#66+#67**. Distinct from identity-seal **#7** / P7 / A9. Conflicts → PM → Product → CEO. Cost/critical → **COO → CEO** (LLM/API spend); PoC **$0**.

**Soft Spec weave (binding note — not a 5th Story):** Where isolation deny/scrub/authz paths touch SA-REV-MVP-C **OTel / audit / idempotent-offers** hooks already on roadmap, named-slice plans note touchpoints only — **no** observability product Story, **no** new product surface, **no** separate plan for Soft Spec on the parent. Cite Spec Locked #10 + Spec §4.

---

## 2. Restated understanding (short)

Executable plan for **SD framing / verification / map only** (parent #18): document and verify that **#18 AC** maps to (a) Stage A/B tip CLOSED on `main` @ `ca827a2` (#31 Field ACL; #32 list fail-closed; #40 discovery scrub; #41 StrategyBody ACL; #42 ShareOutbound-after-Accept) as the **API/DB wall**, and (b) Stage C delta remainder delivered via complementary named-slice plans **#67** (agent/tool hard wall + scrub), **#66** (thin OwnAgent Assistant under wall — closes soft #41 OUT), **#68** (A8-min meters + hard cutoff), **#69** (X2 UI no bypass). **BIND #67** as eng dual-wall slice — do **not** Field-capture / merge #67 implementation here; do **not** rewrite Option A; do **not** claim Stage A/B delivered agent wall. Confirm dual-wall end-state for **all** FieldClasses (open-ended registry; examples ≠ exhaustive); prompt-only soft guidance **rejected** as sole control. Preserve LoginEmail User-only; ContactEmail ShareOutbound Accept-gated (Stage B tip + #67 share tools). Threat rows (prompt-injection / agent↔agent exfil) bound to gateway + scrub (#67), not model trust. Soft OTel/audit/idempotent weave only — **no 5th Story**. Distinct from identity-seal **#7**. Gate **#26** backlog until Stage C delivery; Gate **#27** HOLD. PoC **$0**; LLM spend → COO → CEO (do not provision). No Cognito/vault/MCP/fuller Assistant invent; no MM/DC4. No product FieldClasses invent. Itemized steps are **framing/verification/map** — executable without inventing FieldClasses or re-implementing #66/#67/#68/#69. No product code in this artifact — SD framing instructions only. **#18 parent framing ONLY**.

---

## 3. Locked decisions (Spec locks → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | Option A dual wall end-state | API/DB wall (Stage A/B tip) + agent/tool wall (Stage C / #67) as dual defense for **all** FieldClasses; prompt-only soft guidance **rejected** as sole control | Steps 1–2, 8–9 |
| 1 | FieldClass registry open-ended | LoginEmail / ContactEmail / DisplayName / StrategyBody = **examples**, not exhaustive; dual wall applies to all FieldClasses; no inventing new named FieldClasses unless CEO/Product locks them | Steps 2, 8–9 |
| 2 | Stage A/B CLOSED map (tip) | #31/#32/#40/#41/#42 CLOSED on `main` @ `ca827a2`; do **not** claim they delivered agent wall | Steps 2–3, 9 |
| 3 | Stage C remainder map (not merge) | #18 AC remainder → **#67** hard wall + scrub; **#66** thin Assistant under wall; **#68** A8-min hard cutoff; **#69** X2 UI no bypass — complementary plans | Steps 1, 4–5, 8–9 |
| 4 | BIND #67 | Eng dual-wall slice; this parent plan does **not** Field-capture #67 implementation | Steps 1, 4, 8–9 |
| 5 | Soft #41 Assistant OUT | Closes only via **#66 + #67** delivery — not claimed as Stage B–delivered Assistant/tool runtime | Steps 5, 8–9 |
| 6 | Remaining isolation invariants | Strategy owner+OwnAgent only; list/search party-scoped; counterparty negotiation-scoped only; unauth/wrong-principal deny no leakage; IDOR tests | Steps 3, 6, 9 |
| 7 | Distinct from #7 | Identity-seal / contact-on-accept (#7 / P7 / A9) is a separate seal — this Story is authz/tenancy | Steps 8–9 |
| 8 | LoginEmail / ShareOutbound preserved | LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools) | Steps 3–4, 8–9 |
| 9 | Threat rows | Prompt-injection / agent↔agent exfil bound to gateway + scrub (#67), not model trust | Steps 4, 8–9 |
| 10 | Gate / OUT / spend / Soft weave | Gate #26 backlog until Stage C delivery; #27 HOLD; no Cognito/vault/MCP/fuller Assistant invent; no 5th Story (Soft OTel/audit/idempotent weave only); PoC $0; spend → COO → CEO | Steps 7–9; Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (framing / verification / map only — Spec §7 Host: no new host invent on parent).  
**Prerequisite:** Stage A/B tip CLOSED on `main` @ `ca827a2` (#31+#32+#40+#41+#42). Consume — do **not** rewrite those Specs or merge Stories. Soft #41 Assistant OUT closes only via **#66+#67** — do **not** claim Stage B delivered agent wall.  
**Sibling complementary (cross-ref only — do not merge / re-implement here):**  
- **#67** already drafted: `plans/2026-09-28__devplan__plan__mvp-stage-c-agent-tool-hardwall-scrubber.md` — BIND eng dual-wall; do **not** Field-capture.  
- **#66** expected: `plans/2026-09-28__devplan__plan__mvp-stage-c-thin-assistant-runtime-x1.md`  
- **#68** expected: `plans/2026-09-28__devplan__plan__mvp-stage-c-a8-minimum-meters-budgets.md`  
- **#69** expected: `plans/2026-09-28__devplan__plan__mvp-stage-c-basic-ui-first-party-bot-x2.md`  
**Distinct:** Identity-seal **#7** — separate Story/plan; do **not** merge or rewrite under #18.  
**Hold:** Gate #26 open; Gate #27; Field-capturing #67; Cognito/MM/DC4; MCP/fuller Assistant; vault/KMS; LLM provision; inventing FieldClasses / 5th Story — do **not** invent.

### Step 1 — Confirm Option A dual-wall map (API/DB tip + agent/tool #67); reject prompt-only sole control

Per Spec Locked #0/#4; Security Dev Plan point **1**:

| Item | Plan lock |
|------|-----------|
| Wall 1 (tip) | API/DB wall = Stage A/B tip CLOSED (#31+#32+#40+#41+#42 @ `ca827a2`) |
| Wall 2 (remainder) | Agent/tool wall = Stage C / **#67** eng dual-wall slice |
| Dual defense | Both walls defend **all** FieldClasses (open-ended registry) |
| Reject | Prompt-only soft guidance as **sole** control |
| Framing only | Verify map documented; **implementation** of wall 2 is on **#67** — do **not** Field-capture here |

**Acceptance:**

- [ ] Handoff notes restate dual wall: API/DB tip + agent/tool (#67)
- [ ] Prompt-only rejected as sole control (documented)
- [ ] No #67 gateway/allowlist/scrub Field implementation tasks executed under this Story
- [ ] Cite Spec Locked #0 + sibling #67 plan path

### Step 2 — Verify Stage A/B tip CLOSED claims; open-ended FieldClass registry; no agent-wall claim

Per Spec Locked #1/#2; Security Dev Plan points **1–2**:

| Tip Story | CLOSED claim (verify only) | Do **not** claim |
|-----------|----------------------------|------------------|
| #31 Field ACL registry | API/DB FieldPolicy + FieldClass projection | Delivered agent wall |
| #32 Account list fail-closed | Owner-scoped list/get | Delivered agent wall |
| #40 Instant search discovery | Discovery scrub omit secrets | Delivered agent wall |
| #41 Minimal Strategy CRUD | StrategyBody ACL; soft Assistant OUT still open until #66+#67 | Delivered Assistant/tool runtime |
| #42 Contact on accept | ContactEmail ShareOutbound-after-Accept | Delivered agent share tools |

| Item | Plan lock |
|------|-----------|
| Tip SHA | Confirm tip CLOSED on `main` @ `ca827a2` (Gates #24/#25 CLOSED) |
| Registry | LoginEmail / ContactEmail / DisplayName / StrategyBody = **examples**, not exhaustive |
| Invent forbid | Do **not** invent new named FieldClasses unless CEO/Product locks them |
| Agent wall | Remains Stage C / **#67** — tip did **not** deliver it |

**Acceptance:**

- [ ] Tip CLOSED Stories listed with cites; SHA `ca827a2` noted
- [ ] Explicit note: Stage A/B did **not** deliver agent/tool hard wall
- [ ] No new FieldClass invent tasks under this Story
- [ ] Open-ended registry restated in handoff notes

### Step 3 — Document remaining isolation invariants (parent) + tip locus map

Per Spec §1–§2 + Locked #6/#8; Security Dev Plan points **5–6**:

| Invariant | Tip locus (CLOSED) | Stage C locus (named slices) |
|-----------|--------------------|------------------------------|
| Strategy R/W owner + OwnAgent only | #41 Strategy CRUD + StrategyBody FieldPolicy | #66 under #67 wall closes soft #41 OUT |
| List/search party-/owner-scoped | #32 + #40 | #69 must not bypass |
| Counterparty negotiation-scoped only | #41 never StrategyBody to counterparty; #42 ShareOutbound-after-Accept; #31 | #67 share tools Accept-gated; scrub before agent context |
| Unauth / wrong-principal deny; no leakage | #31/#32/#40/#41/#42 fail-closed | Same on #67/#66/#68/#69 planes |
| IDOR automated tests | Stage A/B Spec §7.1 | Stage C named-slice Spec §7.1 / §8.1 / §9.1 loci |
| LoginEmail User-only; ShareOutbound Accept-gated | Stage B tip | + #67 share tools (cross-ref) |

**Acceptance:**

- [ ] Invariants table documented in handoff with tip vs Stage C locus
- [ ] LoginEmail User-only + ShareOutbound Accept-gated cited (tip + #67 cross-ref — not Field-captured)
- [ ] No product FieldClass / identity-seal invent

### Step 4 — BIND #67 as eng dual-wall slice; document remainder binds #66/#68/#69 (complementary only)

Per Spec §3 + Locked #3/#4/#9; Security Dev Plan points **3, 5–6**:

| Slice | Role | Plan / Spec DOC-FLOW (cross-ref only) |
|-------|------|---------------------------------------|
| **#67** | Eng dual-wall remainder — gateway + allowlist + same Evaluate scrub; threat rows bound here | `plans/2026-09-28__devplan__plan__mvp-stage-c-agent-tool-hardwall-scrubber.md` · Spec sibling |
| **#66** | Thin OwnAgent Assistant under wall (closes soft #41 OUT) | `plans/2026-09-28__devplan__plan__mvp-stage-c-thin-assistant-runtime-x1.md` |
| **#68** | A8-min per-Participant meters + hard cutoff | `plans/2026-09-28__devplan__plan__mvp-stage-c-a8-minimum-meters-budgets.md` |
| **#69** | X2 basic UI — no wall bypass | `plans/2026-09-28__devplan__plan__mvp-stage-c-basic-ui-first-party-bot-x2.md` |

| Item | Plan lock |
|------|-----------|
| BIND #67 | Cite sibling #67 Dev Plan already drafted — do **not** merge / rewrite / Field-capture |
| Threat rows | Prompt-injection / agent↔agent exfil → gateway + scrub (**#67**), not model trust — map/cross-ref only |
| Merge forbid | Do **not** invent one mega-plan that replaces #66–#69 |
| Implementation forbid | Do **not** re-implement gateway / allowlist / scrub / Assistant / meters / UI under this Story |

**Acceptance:**

- [ ] Remainder map table in handoff cites all four sibling plan paths
- [ ] #67 BIND explicit; no Field-capture tasks under #18
- [ ] Threat rows documented as bound to #67 (cross-ref only)
- [ ] Separate plans verified present / expected — not merged into this file

### Step 5 — Soft #41 Assistant OUT closes only via #66+#67 (not Stage B claim)

Per Spec Locked #5; Security Dev Plan point **4**:

| Item | Plan lock |
|------|-----------|
| Soft #41 OUT | Closes **only** when **#66** thin OwnAgent Assistant delivers **under** **#67** wall |
| Not Stage B | Do **not** claim Stage B #41 delivered Assistant/tool runtime |
| Framing | Document close condition; implementation remains on #66+#67 plans |

**Acceptance:**

- [ ] Handoff states soft #41 OUT → #66+#67 only
- [ ] No Stage B Assistant-delivery claim
- [ ] No #66/#67 product implementation under this Story

### Step 6 — Map #18 AC + §8.1 automated-tests locus (tip + Stage C); verify coverage plan without re-implementing slices

Per Spec §8 / §8.1 + Locked #6; Security Dev Plan points **1, 3, 10**:

| Issue AC / test case | Expect | Delivery locus |
|----------------------|--------|----------------|
| Strategy R/W owner (+ OwnAgent) only | Never counterparties / third | #41 tip + #66 under #67 |
| List/search authorized only | Party/owner | #32/#40 tip; #69 no bypass |
| Counterparty negotiation-scoped only | Not private Strategy / full account / unrelated | #41/#42 tip + #67 |
| Unauth / wrong-principal | Deny 401/403; no private-field leakage | All planes incl. #67/#66/#68/#69 |
| Owner Strategy OK | Owner R/W OK | #41 tip (+ #66) |
| Counterparty cannot read Strategy | StrategyBody denied on negotiation DTOs | #41 tip |
| Stranger cannot list another’s | Fail-closed list/search | #32+#40 tip |
| Cross-tenant IDOR | Fail | A/B tip + Stage C slices |
| Agent plane dual-wall remainder | Allowlist+scrub; no LoginEmail; pre-Accept share deny; prompt-only rejected | **#67** Spec §9.1 (bind — not Field-captured here) |
| Thin Assistant OwnAgent isolation | Owner OK; stranger/cross-tenant deny; no LoginEmail in context | **#66** Spec §8.1 |
| Meter isolation | Cross-tenant/unauth cannot burn budget | **#68** Spec §8.1 |
| UI authz | No UI-only security; fail-closed protected actions | **#69** Spec §8.1 |

**Acceptance:**

- [ ] #18 AC → tip vs Stage C locus map documented
- [ ] §8.1 test locus cites named-slice Specs — tests executed under those Stories, not reinvented here
- [ ] No inventing FieldClasses or product test harnesses beyond framing verification notes

### Step 7 — Soft OTel / audit / idempotent weave notes only (no 5th Story)

Per Spec §4 + Locked #10; Security Dev Plan point **8**:

| Touchpoint | Plan note (not a Story) |
|------------|-------------------------|
| Isolation deny / scrub / authz paths | Soft: where paths touch SA-REV-MVP-C OTel/audit/idempotent-offers hooks already on roadmap — named-slice plans note touchpoints |
| Invent forbid | Do **not** invent a 5th Story or observability product on the parent |
| Parent role | Document Soft weave expectation in handoff; do **not** schedule Soft product work here |

**Acceptance:**

- [ ] Soft weave note present in handoff
- [ ] No 5th Story / observability product tasks under this Story
- [ ] Named slices remain responsible for their Soft touchpoint notes

### Step 8 — Explicit OUT (do **not** implement Spec §6)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Field-capturing / merging #67 into this plan | #67 remains eng dual-wall slice plan — already drafted separately |
| Merging #66/#68/#69 into one mega-plan | Complementary plans only |
| Rewriting Option A tip | Cite-only |
| Claiming Stage A/B delivered agent wall | Soft #41 OUT closes via #66+#67 |
| Inventing new product / FieldClasses / spend | OUT |
| MotorMarket / DC4 | OUT |
| Unlocking #26 before Stage C delivery; #27 | #26 backlog; #27 HOLD |
| Cognito/SSO/IdP; mature vault/KMS; MCP breadth; fuller Assistant as MVP-delivered | OUT |
| Inventing a 5th Story for OTel/audit/idempotent | Soft weave on named slices only |
| PoC identity-seal stub rewrite (#7) | Distinct seal — do not expand here |
| Real contact release product rewrite | Stage B #42 tip + #67 share tools; not this plan’s Field work |
| Re-implementing #66/#67/#68/#69 under this Story | **Forbidden** — framing/map only |

**Acceptance:**

- [ ] OUT table mirrored in handoff
- [ ] No OUT items implemented under this Story
- [ ] Gate #26 not opened; #27 HOLD; #7 not merged

### Step 9 — Secrets / cost / zero MM-DC4; local $0; self-verify framing checklist

**Secrets / invent forbid:**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings
- [ ] No Cognito/SSO/IdP/vault SDK PackageReferences added by this Story
- [ ] No MCP marketplace / fuller Assistant invent
- [ ] No new named FieldClasses invented

**Host / spend:**

- [ ] Remains **local / $0** — no new host invent on parent framing
- [ ] ECS Express Mode **sketch only** if README already mentions — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource / IdP / vault / LLM provision that creates spend
- [ ] Any named **LLM/API spend** → escalate **COO → CEO** — do **not** provision in this Story
- [ ] Other spend proposals → escalate **CPM → COO → CEO** as applicable

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, packages, shared libs, schemas, feeds, SFTP, shared DB, or config keys introduced by this framing work
- [ ] Document verify result in SD handoff notes

**Self-verify (framing) before handoff:**

- [ ] Dual-wall map confirmed (API/DB tip + #67 agent wall); prompt-only rejected
- [ ] Tip CLOSED claims verified @ `ca827a2`; no agent-wall claim on A/B
- [ ] Remainder binds documented to #66/#67/#68/#69 complementary plans; #67 BIND without Field-capture
- [ ] Soft #41 OUT → #66+#67 only (not Stage B claim)
- [ ] Isolation invariants + #18 AC / §8.1 locus mapped; tests remain on tip + named-slice Stories
- [ ] Soft OTel/audit/idempotent weave only — no 5th Story
- [ ] Distinct from #7; LoginEmail/ShareOutbound preserved (tip + #67 cross-ref)
- [ ] Nothing from Step 8 / Spec §6 OUT implemented; gate #26 not opened; #27 HOLD
- [ ] Secrets hygiene + zero MM/DC4 + local/$0; no Cognito; no LLM provision (escalate COO → CEO)
- [ ] Spec+SpecQA SoR CLEAR cited (PR **#73** @ `f133e90`); Spec Security PASS + SA Security PASS cited
- [ ] #18 parent framing ONLY — no sibling Story Field merge; Product conflicts escalated if any
- [ ] No product code left undocumented; handoff notes cite Spec + this plan + sibling plan paths

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec Locked #0 / dual wall | API/DB tip + agent/tool (#67); prompt-only rejected | Steps 1–2, 8–9 |
| Spec Locked #1 / open-ended registry | Examples ≠ exhaustive; dual wall all FieldClasses; no invent | Steps 2, 8–9 |
| Spec Locked #2 / tip CLOSED map | #31/#32/#40/#41/#42 @ `ca827a2`; no agent-wall claim | Steps 2–3, 9 |
| Spec Locked #3/#4 / remainder map + BIND #67 | Map to #67/#66/#68/#69; do not Field-capture #67 | Steps 1, 4–5, 8–9 |
| Spec Locked #5 / soft #41 OUT | Closes via #66+#67 only — not Stage B claim | Steps 5, 8–9 |
| Spec Locked #6 / isolation invariants | Strategy / list / counterparty / deny / IDOR | Steps 3, 6, 9 |
| Spec Locked #7 / distinct from #7 | Identity-seal separate | Steps 8–9 |
| Spec Locked #8 / LoginEmail + ShareOutbound | User-only; Accept-gated (tip + #67) | Steps 3–4, 8–9 |
| Spec Locked #9 / threat rows | Bound to gateway + scrub (#67), not model trust | Steps 4, 8–9 |
| Spec Locked #10 + §4 Soft weave + §6 OUT | Gate #26/#27; no 5th Story; $0; OUT | Steps 7–9; Explicit OUT |
| Spec §1 AC map tip vs Stage C | Strategy / list / counterparty / deny / tests / #7 | Steps 2–6, 9 |
| Spec §2 Remaining invariants | Parent isolation invariants | Steps 3, 6, 9 |
| Spec §3 Named-slice bind | #67/#66/#68/#69 complementary | Steps 4–5, 8–9 |
| Spec §4 Soft OTel/audit/idempotent | Weave only — no 5th Story | Step 7 |
| Spec §5 Security Spec 1–10 | Upstream Spec Security bind (PASS) | §6 Security woven (Dev Plan-step) |
| Spec §7 Host / cost | No new host invent; local/$0; LLM → COO → CEO | Step 9 |
| Spec §8 / Issue AC row 1 | Strategy R/W owner + OwnAgent only | Steps 3, 5–6, 9 |
| Spec §8 / Issue AC row 2 | List/search authorized only | Steps 3, 6, 9 |
| Spec §8 / Issue AC row 3 | Counterparty negotiation-scoped only | Steps 3–4, 6, 9 |
| Spec §8 / Issue AC row 4 | Unauth/wrong-principal deny; no leakage | Steps 3, 6, 9 |
| Spec §8 / Issue AC row 5 + §8.1 | Automated tests tip + Stage C locus | Step 6 |
| Spec §8 / Issue AC row 6 | Distinct from #7 | Steps 8–9 |
| Sibling #67 Dev Plan (cite; do not merge) | Eng dual-wall BIND | Steps 1, 4, 8–9 |
| Sibling #66/#68/#69 plans (cross-ref) | Complementary OUT of this Field work | Steps 4–5, 8–9 |
| Spec QA + Spec Security PASS trio | Spec gate CLEAR (PR #73 @ `f133e90`; Spec Sec PASS) | Sources; §6 handshake |
| SA Security PASS | Architecture-step prior | Sources |

---

## 6. Security Dev Plan-step binding

**Status:** Security woven (Chief Security Dev Plan checklist on disk).  

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-qa-confirm.md` (points 1–10 MET; Spec Security SoR **#71+#72**; tip `main` @ `f133e90` / PR **#73** Spec Soft Soft CLOSE Soft HOLD SoR CLEAR)  
**SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Spec Security binding:** Spec §5 points 1–10 (Spec-step — **not** a substitute for Dev Plan-step checklist)  
**Named-slice Dev Plans:** gated by their own Security QA (order preference #67→#66→#68→#69→#18)

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Option A dual wall end-state map** — Plan restates API/DB wall (Stage A/B tip) + agent/tool wall (Stage C / #67) as dual defense for **all** FieldClasses; schedules **reject** of prompt-only soft guidance as sole control; verify framing only (implementation on #67) | Step 1 dual-wall map + reject prompt-only; Step 2 tip vs agent-wall claim; Locked #0; implementation remains on #67 | Steps 1–2, 8–9; Locked #0 |
| 2 | **FieldClass registry open-ended** — Plan treats LoginEmail / ContactEmail / DisplayName / StrategyBody as **examples**, not exhaustive; dual wall applies to all FieldClasses; no inventing new named FieldClasses unless CEO/Product locks them | Step 2 open-ended registry + invent forbid; Step 9 no FieldClass invent; Locked #1 | Steps 2, 8–9; Locked #1 |
| 3 | **Stage C remainder map (not merge)** — Plan maps #18 AC remainder to named slices: **#67** hard wall + scrub; **#66** thin Assistant under wall; **#68** A8-min hard cutoff; **#69** X2 basic UI no bypass — complementary Dev Plans, not one merged surface; verify separate plans | Steps 1, 4 remainder bind table + sibling plan paths; Step 8 OUT forbid mega-merge; Locked #3/#4 | Steps 1, 4–5, 8–9; Locked #3/#4 |
| 4 | **Soft #41 Assistant OUT** — Plan states soft #41 closes only via **#66 + #67** delivery under wall — not claimed as Stage B–delivered Assistant/tool runtime; verify | Step 5 soft #41 close condition; Step 2 tip note soft OUT still open until #66+#67; Locked #5 | Steps 2, 5, 8–9; Locked #5 |
| 5 | **No LoginEmail share / ShareOutbound Accept-gated** — Plan preserves LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools); verify framing cites tip + #67 (does not Field-capture #67) | Step 3 invariants + LoginEmail/ShareOutbound tip locus; Step 4 #67 share-tools cross-ref only; Locked #8 | Steps 3–4, 8–9; Locked #8 |
| 6 | **Threat rows bound to #67** — Plan keeps CEO prompt-injection / agent↔agent exfil posture bound to gateway + scrub (#67), not model trust; verify map/cross-ref only | Step 4 threat rows → #67 map/cross-ref; Step 8 OUT no Field-capture; Locked #9 | Steps 4, 8–9; Locked #9 |
| 7 | **Gate #26 / #27 HOLD** — Plan does **not** unlock Gate **#26** (backlog until Stage C delivery) or Gate **#27**; parent Dev Plan is framing-step, not post-delivery SA-REV; verify | Step 8 OUT Gate rows; Step 9 self-verify gates; Constraints; Locked #10 | Steps 8–9; Explicit OUT; Locked #10 |
| 8 | **OUT locked** — Plan does not invent Cognito/SSO/IdP, mature vault/KMS, MCP breadth, fuller Assistant as MVP, MotorMarket/DC4, or a **5th Story** for OTel/audit/idempotent (Soft weave on named slices only) | Step 7 Soft weave only; Step 8 OUT table; Step 9 no Cognito/MM/MCP/FieldClass invent; Locked #10 | Steps 7–9; Explicit OUT; Locked #10 |
| 9 | **Cost / spend** — PoC **$0**; any named LLM/API spend → **COO → CEO**; do not provision | Step 9 local/$0; LLM → COO → CEO; no provision; Cost/critical §8 | Step 9; Cost/critical; Locked #10 |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS parent #18 until Security QA confirms **this** checklist (and named-slice Dev Plans remain gated by their own Security QA). SD stays HOLD until then. Parent does **not** Field-capture #67 | See handshake note below. Done-list requires Security QA confirm before PASS; #67 BIND without Field-capture throughout | Handshake note; Done-list Dev Plan QA; Steps 1, 4, 8–9 |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Senior Security → Security QA confirms.** **SD stays HOLD** until Dev Plan QA + Security QA PASS + Chief Dev Planner unlock (+ CPM per ops). Named-slice Dev Plans (#66/#67/#68/#69) remain gated by their own Security QA. Parent does **not** Field-capture #67.

---

## 7. Explicit OUT

Mirror Spec §6 / issue #18 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Field-capturing / merging #67 into this plan | #67 remains eng dual-wall slice — `plans/2026-09-28__devplan__plan__mvp-stage-c-agent-tool-hardwall-scrubber.md` |
| Merging #66/#68/#69 into one mega-plan | Complementary plans only |
| Rewriting Option A tip | Cite-only |
| Claiming Stage A/B delivered agent wall | Soft #41 OUT closes via **#66+#67** |
| Inventing new product / FieldClasses / spend | OUT |
| MotorMarket / DC4 | OUT |
| Unlocking #26 before Stage C delivery; #27 | #26 backlog; #27 HOLD |
| Cognito/SSO/IdP; mature vault/KMS; MCP breadth; fuller Assistant as MVP-delivered | OUT |
| Inventing a 5th Story for OTel/audit/idempotent | Soft weave on named slices only (Spec §4 / Step 7) |
| PoC identity-seal stub rewrite (#7) | Distinct seal — do not expand here |
| Real contact release product rewrite | Stage B #42 tip + #67 share tools; not this plan’s Field work |
| Drafting or re-implementing #66 / #67 / #68 / #69 Field work in this plan | **Forbidden** — #18 parent framing ONLY |
| Prompt-only soft wall as sole control | Rejected architecture — not a deliverable |

---

## 8. Cost/critical

**#18 parent framing must not procure AWS / Cognito / IdP / vault / KMS / LLM spend.** Any named **LLM/API spend** → escalate **COO → CEO** (do **not** provision). Other paid AWS provision, Cognito/SSO/IdP, vault/KMS, MCP marketplace, or spend proposal → escalate **CPM → COO → CEO** as applicable. Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, vault, MCP marketplace, LLM provider, or FieldClass invent tasks. ECS Express Mode remains **README sketch only** if present. Gate **#26** stays backlog until Stage C delivery — do not open. Gate **#27** HOLD. Do **not** Field-capture #67. Soft #41 Assistant OUT closes via **#66+#67** — not Stage B.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-28__devplan__plan__mvp-stage-c-participant-isolation-option-a-remainder.md`
- [ ] Spec coverage Locked #0–#10 + §§1–4 + §6 OUT + §7 Host + §8 AC + §8.1 + issue #18 AC mapped to plan steps
- [ ] Itemized steps executable by SD as **framing/verification/map** without inventing FieldClasses or re-implementing #66/#67/#68/#69
- [ ] **#18 parent framing ONLY** — #66/#67/#68/#69 **cross-ref only** (not Field-captured/merged); identity-seal #7 distinct; gate #26 backlog; #27 HOLD
- [ ] Maps #18 AC to Stage A/B CLOSED tip @ `ca827a2` vs Stage C delta; **BIND #67** without Field-capture; soft #41 OUT → #66+#67 only (not Stage B claim)
- [ ] Soft Spec weave OTel/audit/idempotent touchpoints only — **no 5th Story** (explicit Soft weave note in body)
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6) — checklist `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-devplan-checklist.md`
- [ ] No Cognito/SSO/IdP/vault/AWS/LLM provision instructions (LLM → COO → CEO)
- [ ] No invented Stories / requirements / FieldClasses
- [ ] No MotorMarket / DC4; no MCP/public marketplace
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD framing instructions only)
- [ ] Spec+SpecQA SoR CLEAR cited (PR **#73** @ `f133e90`); Spec Security PASS trio + SA Security PASS cited; sibling #67 plan path cited
- [ ] **Security QA confirm required** on Dev Plan-step points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner (HOLD until Senior Security → Security QA)
- [ ] **SD HOLD** until Dev Plan QA + Security QA PASS + Chief unlock
- [ ] Named-slice Dev Plans remain gated by their own Security QA

**Next:** Senior Security review → Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock (+ CPM per ops). Named slices #66–#69 continue on their own Dev Plan Security QA in parallel — **this plan does not Field-capture them**.

---

## 10. Done-list for SD (after plan QA PASS + Security QA confirm + Chief unlock)

- [ ] Step 1: Confirm dual-wall map (API/DB tip + #67 agent wall); reject prompt-only sole control; no #67 Field-capture
- [ ] Step 2: Verify tip CLOSED @ `ca827a2`; open-ended FieldClass registry; no A/B agent-wall claim
- [ ] Step 3: Document remaining isolation invariants + tip vs Stage C locus; LoginEmail/ShareOutbound preserved
- [ ] Step 4: BIND #67; document remainder binds to #66/#68/#69 complementary plans; threat rows → #67 cross-ref only
- [ ] Step 5: Soft #41 OUT → #66+#67 only (not Stage B claim)
- [ ] Step 6: Map #18 AC + §8.1 automated-tests locus (tip + Stage C); do not re-implement slice tests here
- [ ] Step 7: Soft OTel/audit/idempotent weave notes only — no 5th Story
- [ ] Step 8: Do not implement Spec §6 OUT; do not Field-capture #67; do not open #26; do not merge #7
- [ ] Step 9: Secrets hygiene; local/$0; zero MM/DC4; no Cognito; LLM → COO → CEO; complete self-verify framing checklist
- [ ] Hand off to CQ gate (never skip CQ) — framing evidence only; product delivery remains on named-slice Stories
