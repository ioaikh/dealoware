# Spec — MVP Stage C: Participant isolation Option A remainder (#18 parent framing)

**Status:** Senior Spec draft; BA AC locked; Spec QA HOLD until Spec-step Security PASS + Chief clear  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage C Specs #67 → #66 → #68 → #69 → parent #18 (CEO Stage C + #18 Spec unlock; CA PASS delta)  
**Issue:** https://github.com/ioaikh/dealoware/issues/18  
**Parent:** self · Option A (CEO ACCEPTED 2026-09-21) · `stage:mvp` · Stage C Spec-step framing (not Field capture of #67)  
**DOC-FLOW:** `specs/2026-09-28__spec__spec__mvp-stage-c-participant-isolation-option-a-remainder.md`  
**Constraints:** Parent isolation Spec — **maps** #18 AC to (a) Stage A/B CLOSED on tip vs (b) Stage C delta. **BIND #67** as eng dual-wall slice — do **not** Field-capture / merge #67 into this Spec; do **not** rewrite Option A; do **not** claim A/B delivered agent wall. Named slices **#66–#69** remain separate Specs + separate Security checklists. Distinct from identity-seal **#7**. Soft OTel/audit/idempotent weave where isolation deny paths touch — **no 5th Story**. PoC **$0**. Gate **#26** backlog until Stage C **delivery**; Gate **#27** HOLD. #26 stays backlog; do not invent product / MotorMarket / FieldClasses / spend. **BA AC locked** (issue #18). Cite Option A §3b + CA PASS delta + parent Spec-step Security checklist. Spec QA must **not** PASS until Spec-step Security checklist delivered + Security QA confirms (**this** checklist; named-slice Specs remain gated by their own Security QA). **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears). Docs SoR PR soft — do **not** HOLD Spec content on docs merge.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #18 AC + OUT | https://github.com/ioaikh/dealoware/issues/18 | Binding acceptance (authz / tenancy isolation) |
| Option A architecture (§3b + Stage C row) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall end-state; Platform hard wall; open-ended FieldClass; Story split |
| CA PASS Stage C delta | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | Hard wall pick A; thin Assistant; A8-min; X2; #18 remainder map |
| Parent Spec Security checklist (#18) | `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-checklist.md` | Points **1–10** (Spec-step framing — does **not** Field-capture #67) |
| Sibling Spec Security checklists ×4 | `verification/2026-09-28__security__verification__mvp-stage-c-{hardwall,thin-assistant,a8-min,x2-ui-bot}-spec-checklist.md` | Named-slice Spec-step Security (separate) |
| SA Security PASS trio | `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md`, `...-sa-points-review.md`, `...-sa-qa-confirm.md` | Architecture-step Security PASS prior |
| Stage A #31 Spec (CLOSED) | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | Field ACL registry + API projection |
| Stage A #32 Spec (CLOSED) | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Account list fail-closed |
| Stage B #40 Spec (CLOSED) | `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md` | Instant search discovery scrub |
| Stage B #41 Spec (CLOSED) | `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md` | Minimal Strategy CRUD + StrategyBody ACL; soft Assistant OUT |
| Stage B #42 Spec (CLOSED) | `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | ContactEmail ShareOutbound-after-Accept |
| Eng dual-wall slice #67 Spec | `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` | BIND — complementary Spec; do not merge |
| Named slices #66 / #68 / #69 Specs | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md`, `...-a8-minimum-meters-budgets.md`, `...-basic-ui-first-party-bot-x2.md` | Cross-ref only — complementary; do not merge |
| Tip delivery | `main` @ `ca827a2` | Stage A/B CLOSED; Gates #24/#25 CLOSED |

**Product alignment:** CEO ACCEPTED Option A dual wall. This Spec frames Stage C unlock / remainder map — eng dual-wall remainder implementation is **#67**. Distinct from identity-seal **#7** / P7 / A9. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | Option A dual wall end-state | API/DB wall (Stage A/B tip) + agent/tool wall (Stage C / #67) as dual defense for **all** FieldClasses; prompt-only soft guidance **rejected** as sole control |
| 1 | FieldClass registry open-ended | LoginEmail / ContactEmail / DisplayName / StrategyBody = **examples**, not exhaustive; dual wall applies to all FieldClasses; no inventing new named FieldClasses unless CEO/Product locks them |
| 2 | Stage A/B CLOSED map (tip) | #31 Field ACL; #32 list fail-closed; #40 discovery scrub; #41 StrategyBody ACL; #42 ShareOutbound-after-Accept — CLOSED on `main` @ `ca827a2`; do **not** claim they delivered agent wall |
| 3 | Stage C remainder map (not merge) | #18 AC remainder → **#67** hard wall + scrub; **#66** thin Assistant under wall; **#68** A8-min hard cutoff; **#69** X2 UI no bypass — complementary Specs |
| 4 | BIND #67 | Eng dual-wall slice; this parent Spec does **not** Field-capture #67 implementation |
| 5 | Soft #41 Assistant OUT | Closes only via **#66 + #67** delivery — not claimed as Stage B–delivered Assistant/tool runtime |
| 6 | Remaining isolation invariants | Strategy owner+OwnAgent only; list/search party-scoped; counterparty negotiation-scoped only; unauth/wrong-principal deny no leakage; IDOR tests |
| 7 | Distinct from #7 | Identity-seal / contact-on-accept (#7 / P7 / A9) is a separate seal — this Story is authz/tenancy |
| 8 | LoginEmail / ShareOutbound preserved | LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools) |
| 9 | Threat rows | Prompt-injection / agent↔agent exfil bound to gateway + scrub (#67), not model trust |
| 10 | Gate / OUT / spend | Gate #26 backlog until Stage C delivery; #27 HOLD; no Cognito/vault/MCP/fuller Assistant invent; no 5th Story; PoC $0; spend → COO → CEO |

---

## 1. #18 AC map — Stage A/B CLOSED vs Stage C delta

| #18 AC theme | Stage A/B tip (CLOSED @ ca827a2) | Stage C delta (this unlock) |
|--------------|----------------------------------|-----------------------------|
| Strategy readable/writable only by owning Participant (+ OwnAgent acting for them) | #41 Strategy CRUD + StrategyBody FieldPolicy (User + OwnAgent API policy); soft Assistant runtime was OUT | #66 thin OwnAgent Assistant under #67 wall closes soft #41 OUT — not Stage B claim |
| List/search party-scoped / owner-scoped | #32 account list fail-closed; #40 discovery scrub | Remains tip; Stage C surfaces (#69) must not bypass |
| Counterparty negotiation-scoped only | #41 never StrategyBody to counterparty; #42 ShareOutbound-after-Accept; #31 Field ACL | #67 share tools Accept-gated; scrub before agent context |
| Unauth / wrong-principal deny; no leakage | #31/#32/#40/#41/#42 fail-closed patterns | Same invariants on agent plane (#67) + Assistant (#66) + meters (#68) + UI (#69) |
| Automated IDOR / isolation tests | Covered per Stage A/B Spec §7.1 | Stage C named-slice Specs add agent/Assistant/meter/UI cases |
| Distinct from identity-seal #7 | Documented in Stage A/B Specs | Restated here — do not merge seals |

**Do not claim:** Stage A/B delivered agent/tool hard wall. Dual-wall remainder eng slice = **#67**.

---

## 2. Remaining isolation invariants (parent)

| Invariant | Spec lock |
|-----------|-----------|
| Strategy | Readable/writable **only** by owning Participant and their OwnAgent acting for them — never counterparties or third Participants |
| List/search | Returns **only** records the Participant is authorized to see (party to, or owner) |
| Counterparty 1:1 view | Negotiation-scoped fields allowed by product rules only — not other party’s private Strategy, full account history, or unrelated negotiations/offers |
| Unauth / wrong-principal | Deny (401/403) with **no** private-field leakage in error bodies |
| IDOR | Automated tests: owner OK; counterparty cannot read Strategy; stranger cannot list another’s negotiations/offers; cross-tenant IDOR fail |
| Distinct from #7 | Identity-seal / contact-on-accept is separate threat model |

---

## 3. Stage C named-slice bind (complementary Specs)

| Slice | Role | Spec DOC-FLOW |
|-------|------|---------------|
| **#67** | Eng dual-wall remainder — gateway + allowlist + same Evaluate scrub | `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` |
| **#66** | Thin OwnAgent Assistant under wall (closes soft #41 OUT) | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md` |
| **#68** | A8-min per-Participant meters + hard cutoff | `specs/2026-09-28__spec__spec__mvp-stage-c-a8-minimum-meters-budgets.md` |
| **#69** | X2 basic UI — no wall bypass | `specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md` |

Parent Spec **maps** remainder — does **not** merge Stories or Field-capture #67.

---

## 4. Soft OTel / audit / idempotent (weave only)

| Touchpoint | Spec note (not a Story) |
|------------|-------------------------|
| Isolation deny paths | Soft: where deny/scrub/authz paths touch SA-REV-MVP-C OTel/audit/idempotent-offers hooks already on roadmap — named-slice Specs note touchpoints |
| Invent | **Do not** invent a 5th Story or observability product on the parent |

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-parent-18-spec-checklist.md`  
**Prior SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Sibling Spec Security (separate):** hardwall / thin-assistant / a8-min / x2-ui-bot Spec checklists  
**Rule:** Spec QA must **not** PASS parent #18 until Security QA confirms **this** checklist (named-slice Specs remain gated by their own Security QA).

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Option A dual wall end-state | Restates API/DB wall (A/B tip) + agent/tool wall (Stage C / #67) as dual defense for all FieldClasses; prompt-only rejected as sole control | Locked #0; §1; §3 |
| 2 | FieldClass registry open-ended | Examples ≠ exhaustive; dual wall all FieldClasses; no inventing new named FieldClasses | Locked #1; Sources Option A |
| 3 | Stage C remainder map (not merge) | Maps remainder to #67/#66/#68/#69 complementary Specs — not one merged surface; does not Field-capture #67 | Locked #3/#4; §3 |
| 4 | Soft #41 Assistant OUT | Closes only via #66+#67 — not Stage B claim | Locked #5; §1 |
| 5 | No LoginEmail share / ShareOutbound Accept-gated | LoginEmail User-only; ContactEmail ShareOutbound only with Accept grant (Stage B tip + #67 share tools) | Locked #8; §1/#2 |
| 6 | Threat rows bound | Prompt-injection / agent↔agent exfil → gateway + scrub (#67), not model trust | Locked #9; §3 #67 bind |
| 7 | Gate #26 / #27 HOLD | Does not unlock Gate #26 (backlog until delivery) or #27; Spec-step framing ≠ post-delivery SA-REV | Locked #10; §6 OUT |
| 8 | OUT locked | No Cognito/SSO/IdP, mature vault/KMS, MCP breadth, fuller Assistant as MVP, MM/DC4, or 5th Story invent | Locked #10; §6 OUT |
| 9 | Cost / spend | PoC $0; any named LLM/API spend → COO → CEO; do not provision | Locked #10; §7 Host |
| 10 | Traceability + handshake | Cites Option A tip + Stage C SA delta + SA Security PASS + sibling Spec Security ×4; Spec QA PASS only after Security QA on **this** checklist | Sources; this §5; Constraints |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Field-capturing / merging #67 into this Spec | #67 remains eng dual-wall slice Spec |
| Merging #66/#68/#69 into one mega-Spec | Complementary Specs only |
| Rewriting Option A tip | Cite-only |
| Claiming Stage A/B delivered agent wall | Soft #41 OUT closes via #66+#67 |
| Inventing new product / FieldClasses / spend | OUT |
| MotorMarket / DC4 | OUT |
| Unlocking #26 before Stage C delivery; #27 | #26 backlog; #27 HOLD |
| Cognito/SSO/IdP; mature vault/KMS; MCP breadth; fuller Assistant as MVP-delivered | OUT |
| Inventing a 5th Story for OTel/audit/idempotent | Soft weave on named slices only |
| PoC identity-seal stub rewrite (#7) | Distinct seal — do not expand here |
| Real contact release product rewrite | Stage B #42 tip + #67 share tools; not this Spec’s Field work |

---

## 7. Host / cost

No new host invent on parent framing. Named slices extend O10 modular monolith under Option A. Local/$0; ECS Express sketch only; App Runner excluded. PoC **$0**. Any named LLM/API spend → escalate **COO → CEO**.

---

## 8. Acceptance mapping (issue #18 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Strategy documents (when Strategy CRUD exists) are readable/writable **only** by the owning Participant (and their own AI Assistant acting for them) — never by counterparties or third Participants | Locked #6; §1 (#41 tip + #66 under #67); §2 Strategy invariant |
| List/search of a Participant's negotiations, offers, and account-scoped history returns **only** records that Participant is authorized to see (party to, or owner) | Locked #6; §1 (#32/#40 tip); §2 List/search |
| Counterparty views of a shared 1:1 Negotiation expose **only** negotiation-scoped fields allowed by product rules — not the other party's private Strategy, full account history, or unrelated negotiations/offers | Locked #6; §1 (#41/#42 tip + #67); §2 Counterparty |
| Unauthenticated and wrong-principal callers receive deny (401/403) with **no** private-field leakage in error bodies | Locked #6; §2 Unauth / wrong-principal; Stage C slices inherit |
| Automated tests (or equivalent evidence) cover: owner OK; counterparty cannot read Strategy; stranger cannot list another Participant's negotiations/offers; cross-tenant IDOR attempts fail | §8.1 below; Stage A/B §7.1 CLOSED + Stage C named-slice §7.1 |
| Documented as distinct from identity-seal / contact-on-accept (**#7** / **P7** / **A9**) | Locked #7; §2 Distinct from #7; §6 OUT |

### 8.1 Automated tests detail (binding)

| Case | Expect | Delivery locus |
|------|--------|----------------|
| Owner Strategy OK | Owner can read/write own Strategy | Stage B #41 tip (+ #66 OwnAgent under wall) |
| Counterparty cannot read Strategy | StrategyBody / private Strategy denied on negotiation DTOs | Stage B #41 tip |
| Stranger cannot list another’s negotiations/offers | Fail-closed list/search | Stage A #32 + Stage B #40 tip |
| Cross-tenant IDOR | Fail | Stage A/B tip + Stage C slices |
| Unauth / wrong-principal | Deny 401/403; no private-field leakage | All planes including #67 agent wall |
| Agent plane dual-wall (remainder) | Allowlist+scrub; no LoginEmail; pre-Accept share deny; prompt-only rejected | **#67** Spec §9.1 (bind — not Field-captured here) |
| Thin Assistant OwnAgent isolation | Owner OK; stranger/cross-tenant deny; no LoginEmail in context | **#66** Spec §8.1 |
| Meter isolation | Cross-tenant/unauth cannot burn budget | **#68** Spec §8.1 |
| UI authz | No UI-only security; fail-closed protected actions | **#69** Spec §8.1 |

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only; Option A §3b + CA PASS delta + parent Spec-step checklist cited
- [ ] Maps #18 AC to Stage A/B CLOSED vs Stage C delta; does **not** Field-capture #67
- [ ] Binds #67 as eng dual-wall slice; maps #66/#68/#69 complementary — no merge
- [ ] Soft #41 OUT closes via #66+#67 — not Stage B claim; distinct from #7
- [ ] Dual wall all FieldClasses; open-ended registry; prompt-only rejected; LoginEmail/ShareOutbound preserved
- [ ] Soft OTel/audit/idempotent weave only — no 5th Story
- [ ] **§8 Acceptance mapping complete** — all issue #18 AC bullets present as rows
- [ ] **Automated tests AC** in §8 map + §8.1 detail (tip + Stage C locus)
- [ ] **Security 1–10** bound (§5 parent checklist) — ask Security QA before PASS; named slices keep own Security QA
- [ ] Gate #26 backlog; #27 HOLD; $0; no MM/DC4 invent
- [ ] BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS (Chief)

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Parent framing does **not** replace Dev Plan for #66–#69 — implement via named-slice Specs
- [ ] Ensure isolation invariants hold across tip + Stage C planes
- [ ] **Automated tests** per §8 / §8.1 (tip coverage + Stage C locus)
- [ ] Do not implement §6 OUT; do not Field-capture #67 here

**Next:** Spec QA verify → ask Security QA (parent checklist) → confirm to Chief Spec only. Named slices #66–#69 continue on their own Spec Security QA in parallel. Triad CLOSE after Spec Security QA PASS (Chief). Gate #26 backlog until Stage C delivery; PoC $0.
