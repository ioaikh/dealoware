# Spec — MVP Stage C: A8-minimum per-Participant meters + hard budgets (#68)

**Status:** Senior Spec draft; BA AC locked; Spec QA HOLD until Spec-step Security PASS + Chief clear  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage C Specs #67 → #66 → #68 → #69 → parent #18 (CEO Stage C unlock; CA PASS delta)  
**Issue:** https://github.com/ioaikh/dealoware/issues/68  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A · `stage:mvp` · Stage C · A8-minimum (Option 1)  
**DOC-FLOW:** `specs/2026-09-28__spec__spec__mvp-stage-c-a8-minimum-meters-budgets.md`  
**Constraints:** Separate Spec from #66 / #67 / #69 / parent #18 framing (cross-ref only). Primary metered consumer = sibling **#66**; metered path remains **#67** wall-bound. Hard cutoff fail-closed — **not** soft-warn-only. PoC **$0**. Gate **#26** backlog until Stage C delivery; Gate **#27** HOLD. Mature metering / owner cost UI → **V3**. No billing/escrow invent; Soft O7 align only if already on path (cite roadmap — no second product); no 5th Story for audit/OTel (Soft Spec weave only). **BA AC locked.** Cite CA PASS A8-min pick A. Spec QA must **not** PASS until Spec-step Security checklist delivered + Security QA confirms. **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears). Docs SoR PR soft — do **not** HOLD Spec content on docs merge. Any LLM spend → escalate COO → CEO; do not provision.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #68 AC + OUT | https://github.com/ioaikh/dealoware/issues/68 | Binding acceptance |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Stage C framing |
| CA PASS Stage C delta (A8-min pick A) | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | Per-Participant meters + hard cutoff; mature → V3 |
| BA note #68 | `plans/2026-09-28__ba__note__story-68-a8-minimum-meters-budgets.md` | Spec-ready refine; Soft audit/OTel; Soft O7 align-if-on-path |
| Spec Security checklist (#68) | `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-checklist.md` | Points **1–10** |
| SA Security PASS trio | `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md`, `...-sa-points-review.md`, `...-sa-qa-confirm.md` | Architecture-step Security PASS prior |
| Option A tip | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall context; consume tip |
| Sibling #66 / #67 / #69 Specs | `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md`, `...-agent-tool-hardwall-scrubber.md`, `...-basic-ui-first-party-bot-x2.md` | Cross-ref only — do not merge |
| Roadmap A8 Option 1 | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | A8-minimum MVP; mature → V3 |

**Product alignment:** CEO Stage C — **A8-minimum** Option 1 only. Mature metering / platform-owner cost UI → **V3**. Conflicts → escalate PM → Product → CEO. Cost/critical spend names → COO → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | SA A8-min pick A | Per-Participant meters + **hard cutoff** fail-closed (Option 1); mature UI deferred V3 |
| 1 | Per-Participant meters | Minimum viable counters for MVP-metered Assistant/LLM (primary consumer #66); counters scoped to Participant; do not invent extras beyond Product-named Stage C metered surfaces |
| 2 | Hard cutoff fail-closed | When budget exhausted, further metered Assistant/tool invocations **deny server-side** — **not** soft warn only |
| 3 | Cross-tenant / unauth | Unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed |
| 4 | Metered path wall-bound | Metered Assistant/tool path remains behind #67 hard wall; budget status must not become privilege escalation or field-leak channel |
| 5 | Authn on meter APIs | Unauth → **401**; wrong principal → **403**/**404**; uniform deny; no private leakage via meter/budget payloads |
| 6 | Soft audit/OTel | If meter/cutoff events touch SA-REV-MVP-C audit/OTel hooks — Spec notes touchpoints only; **no 5th Story** |
| 7 | Soft O7 align | Align Soft O7 MVP-light tags/budgets/kill-switch **only if already on path** — cite roadmap; do **not** invent a second product |
| 8 | Scope label | Documented **A8-minimum** MVP; mature metering / owner cost UI → **V3**; no full billing/settlement/escrow invent |
| 9 | Sibling #69 | May show budget status **minimally** to respect cutoff — **not** platform-owner admin |
| 10 | OUT / Gate / spend | Gate #26 backlog; #27 HOLD; PoC $0; any LLM/API spend → COO → CEO; do not provision |

---

## 1. Per-Participant meters (A8-minimum)

| Item | Spec lock |
|------|-----------|
| Scope | Counters scoped to Participant for MVP-metered Assistant / LLM usage |
| Primary consumer | Sibling **#66** thin Assistant (and Product-named Stage C metered surfaces without inventing extras) |
| Minimum viable | Sufficient for cutoff — **not** mature owner analytics |
| Units | Spec may define meter units / cutoff signal shape without inventing spend or billing product |

---

## 2. Hard budget / cutoff (fail-closed)

| Item | Spec lock |
|------|-----------|
| Enforcement | Server-side — when Participant budget exhausted, further metered Assistant/tool invocations **deny** |
| Soft-warn-only | **Rejected** as sole control |
| Wall bind | Cutoff path remains behind #67; deny must not leak private fields |

---

## 3. Authn / cross-tenant meter hygiene

| Item | Spec lock |
|------|-----------|
| Unauthenticated | Cannot burn any Participant budget; meter APIs → **401** |
| Wrong principal / cross-tenant | Fail-closed (**403**/**404**); cannot consume another’s budget |
| Error / status payloads | Uniform deny; **no** private-field leakage via meter/budget payloads |

---

## 4. Soft audit / OTel + Soft O7 (weave only)

| Touchpoint | Spec note (not a Story) |
|------------|-------------------------|
| Meter increment / cutoff deny | Soft: audit/OTel if SA-REV-MVP-C hooks already on path |
| Soft O7 | Align tags/budgets/kill-switch **only if already on path** — cite roadmap; no second product |
| Invent | **Do not** invent 5th Story, mature owner analytics, or Soft O7 as new product |

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-a8-min-spec-checklist.md`  
**Prior SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Rule:** Spec QA must **not** PASS until Spec-step Security checklist delivered + Security QA confirms.

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Per-Participant meters | Minimum viable meters for MVP-metered Assistant/LLM; counters scoped to Participant | Locked #1; §1 |
| 2 | Hard cutoff fail-closed | Budget exhausted → further metered invocations deny server-side — not soft-warn-only | Locked #2; §2 |
| 3 | Cross-tenant / unauth cannot burn budget | Unauth/wrong-principal cannot consume another’s budget; cross-tenant meter misuse fail-closed | Locked #3; §3 |
| 4 | Metered path still wall-bound (#67) | Metered path behind hard wall; budget status not privilege escalation or field-leak channel | Locked #4; §2; Sources siblings |
| 5 | Authn fail-closed on meter APIs | Unauth 401; wrong principal 403/404; uniform deny; no private leakage | Locked #5; §3 |
| 6 | OUT locked (A8-minimum only) | A8-minimum MVP; mature → V3; no billing/settlement/escrow invent; Soft O7 align-if-on-path only | Locked #7/#8; §6 OUT |
| 7 | Sibling surfaces | Does not invent #69 owner-admin; #69 may show budget status minimally | Locked #9; §6 OUT |
| 8 | No 5th Story / Gate HOLDs | Soft audit/OTel weave only; Gate #26 backlog; #27 HOLD; no Cognito/MM/DC4 | Locked #6/#10; §4; §6 OUT |
| 9 | Cost / spend | PoC $0; any named LLM/API spend → COO → CEO; Spec does not provision | Locked #10; §7 Host |
| 10 | Traceability + handshake | Cites #68 AC + roadmap A8 Option 1 + SA Security PASS; keep #66/#67/#69 separate; Spec QA PASS only after Security QA | Sources; this §5; Constraints |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Mature metering / platform-owner cost UI | A8 mature → **V3** |
| Escrow / settlement / checkout; full billing product | OUT |
| Thin Assistant runtime features (#66) | Sibling — primary consumer relationship only |
| Hard wall (#67) | Sibling — wall-bind only |
| UI/bot (#69) except minimum surface needed to respect cutoff | Sibling may show budget status minimally — not owner admin |
| Inventing a 5th Story for OTel/audit/idempotent | Soft weave only (§4) |
| Soft O7 as a second invent product | Align only if already on path |
| MotorMarket / DC4; Cognito/SSO inventing | OUT |
| Unlocking #26 early; #27 | #26 backlog; #27 HOLD |

---

## 7. Host / cost

Participant-scoped meter counters + server-side cutoff on O10 modular monolith (Assistant/#66 path). Local/$0; ECS Express sketch only; App Runner excluded. PoC **$0**. Any named LLM/API spend → escalate **COO → CEO** (do not provision).

---

## 8. Acceptance mapping (issue #68 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Per-Participant **meters** exist for MVP-metered Assistant / LLM (or equivalent metered) usage — minimum viable counters sufficient for cutoff (not mature owner analytics) | Locked #1; §1 |
| **Hard budget / cutoff** enforced server-side: when Participant budget exhausted, further metered Assistant/tool invocations **deny** (fail-closed) — not “soft warn only” | Locked #2; §2 |
| Unauthenticated / wrong-principal cannot consume another Participant’s budget; cross-tenant meter misuse fail-closed | Locked #3; §3 |
| Automated tests (or equivalent): under-budget allow; at/over budget deny; cross-tenant deny; unauth deny | §8.1 below; Spec QA + Dev Plan Done-lists |
| Documented as **A8-minimum** MVP — **mature metering / owner cost UI → V3**; no inventing full billing/settlement | Locked #8; §6 OUT |
| Soft Spec weave only (not a separate Story): if meter/cutoff events touch SA-REV-MVP-C **audit** (and/or OTel) hooks, Spec notes touchpoints — no new product surface invented | Locked #6; §4 |

### 8.1 Automated tests detail (binding)

| Case | Expect |
|------|--------|
| Under-budget | Metered Assistant/tool invocation **allow** (subject to #67 wall) |
| At / over budget | Further metered invocations **deny** server-side (fail-closed) |
| Cross-tenant meter misuse | Deny; cannot burn another Participant’s budget |
| Unauthenticated | Deny (**401**); cannot consume budget; no private leakage |

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only; CA PASS A8-min pick A + roadmap A8 Option 1 cited
- [ ] Locks per-Participant meters + hard cutoff fail-closed (not soft-warn-only)
- [ ] Cross-tenant/unauth cannot burn budget; meter APIs authn fail-closed
- [ ] Metered path wall-bound (#67); budget status not leak/escalation channel
- [ ] Soft audit/OTel weave only; Soft O7 align-if-on-path only — no 5th Story / second product
- [ ] **§8 Acceptance mapping complete** — all issue #68 AC bullets present as rows
- [ ] **Automated tests AC** in §8 map + §8.1 detail
- [ ] **Security 1–10** bound (§5) — ask Security QA before PASS
- [ ] Separate from #66/#67/#69/#18 framing; Gate #26 backlog; #27 HOLD; $0
- [ ] BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS (Chief)

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Implement per-Participant meters (minimum viable for Assistant/LLM cutoff)
- [ ] Enforce hard cutoff fail-closed server-side
- [ ] Fail-closed cross-tenant / unauth meter misuse
- [ ] Keep metered path behind #67; no field-leak via budget status
- [ ] Soft weave audit/OTel touchpoints where meters hit — no 5th Story
- [ ] **Automated tests** per §8 / §8.1
- [ ] Do not implement §6 OUT

**Next:** Spec QA verify → ask Security QA → confirm to Chief Spec only. Triad CLOSE after Spec Security QA PASS (Chief). Gate #26 backlog; PoC $0; spend → COO → CEO.
