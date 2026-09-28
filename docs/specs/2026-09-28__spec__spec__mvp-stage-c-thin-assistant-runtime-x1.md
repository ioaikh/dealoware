# Spec — MVP Stage C: Thin Strategy-driven AI Assistant runtime X1 (#66)

**Status:** Senior Spec draft; BA AC locked; Spec QA HOLD until Spec-step Security PASS + Chief clear  
**Date:** 2026-09-28  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage C Specs #67 → #66 → #68 → #69 → parent #18 (CEO Stage C unlock; CA PASS delta)  
**Issue:** https://github.com/ioaikh/dealoware/issues/66  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A (CEO ACCEPTED) · `stage:mvp` · Stage C · X1 thin  
**DOC-FLOW:** `specs/2026-09-28__spec__spec__mvp-stage-c-thin-assistant-runtime-x1.md`  
**Constraints:** Separate Spec from #67 / #68 / #69 / parent #18 framing (cross-ref only). **Mandatory bind #67** hard wall — do **not** invent prompt-only soft wall. Soft **#41 Assistant OUT** closes via **#66 + #67** under wall — not Stage B claim. Consume tip `main` @ `ca827a2`. PoC **$0**. Gate **#26** backlog until Stage C delivery; Gate **#27** HOLD. No MM/DC4; no Cognito invent; no fuller Assistant / free-form engine / A5 sandbox invent; no 5th Story for OTel/audit/idempotent (Soft Spec weave only). **BA AC locked.** Cite CA PASS thin Assistant pick A. Spec QA must **not** PASS until Spec-step Security checklist delivered + Security QA confirms. **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears). Docs SoR PR soft — do **not** HOLD Spec content on docs merge.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #66 AC + OUT | https://github.com/ioaikh/dealoware/issues/66 | Binding acceptance |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A; Stage C named slice |
| Soft #41 (Assistant OUT → here) | https://github.com/ioaikh/dealoware/issues/41 · `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md` | Stage B Strategy CRUD + StrategyBody ACL CLOSED; Assistant runtime was OUT |
| Option A architecture | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Stage C agent plane; OwnAgent StrategyBody; dual wall |
| CA PASS Stage C delta (thin Assistant pick A) | `architecture/2026-09-28__sa__architecture__mvp-stage-c-assistant-hardwall-budgets-ui.md` | OwnAgent-only thin Strategy-driven Assistant behind gateway |
| BA note #66 | `plans/2026-09-28__ba__note__story-66-thin-assistant-runtime.md` | Spec-ready refine; Soft OTel/audit/idempotent weave |
| Spec Security checklist (#66) | `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-checklist.md` | Points **1–10** |
| SA Security PASS trio | `verification/2026-09-28__security__verification__mvp-stage-c-sa-checklist.md`, `...-sa-points-review.md`, `...-sa-qa-confirm.md` | Architecture-step Security PASS prior |
| Sibling #67 Spec (mandatory bind) | `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md` | Hard wall + scrub — cross-ref; do not merge |
| Sibling #68 / #69 Specs | `specs/2026-09-28__spec__spec__mvp-stage-c-a8-minimum-meters-budgets.md`, `...-basic-ui-first-party-bot-x2.md` | Cross-ref only |
| Roadmap X1 / P6 | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | X1 thin partial; P6 communication with own AI only |

**Product alignment:** CEO Stage C — **X1** MVP **thin** OwnAgent Assistant. Fuller/stronger → **V1**; free-form Strategy engine → **V1**; A5 sandbox → **V4**. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | SA thin Assistant pick A | OwnAgent-only thin Strategy-driven Assistant behind #67 gateway; StrategyBody via Evaluate; no LoginEmail tool |
| 1 | OwnAgent-only 1:1 | Assistant acts only as **OwnAgent** for owning Participant (P6 spirit); never Counterparty / Stranger agent; no multi-party invent |
| 2 | StrategyBody via FieldPolicy | Consume/write StrategyBody only when `IFieldPolicy.Evaluate` allows OwnAgent R/W for owner; never another Participant’s StrategyBody |
| 3 | Mandatory bind #67 | Platform tools / gateway path **only**; no raw DB / arbitrary internal HTTP; **no** prompt-only soft wall invent |
| 4 | No LoginEmail in context | LoginEmail never in model / agent context packs or tool outputs; User-only (distinct from ContactEmail) |
| 5 | Authn / IDOR fail-closed | Unauth → **401**; wrong principal / cross-tenant → **403** or **404** (Spec consistency); uniform deny; no private-field leakage |
| 6 | Soft #41 OUT closed by design | Soft #41 Assistant OUT closes by **#66 + #67** delivery path — **not** claimed as Stage B–delivered |
| 7 | Soft OTel/audit/idempotent | Where Assistant path touches SA-REV-MVP-C hooks — Spec notes touchpoints only; **no 5th Story** |
| 8 | Scope label | Documented **X1** MVP **thin**; fuller → **V1**; free-form engine → **V1**; A5 → **V4**; BYO/multi-LLM → later |
| 9 | OUT / Gate / spend | #68/#69 not invented here; Gate #26 backlog; #27 HOLD; PoC $0; any LLM spend → COO → CEO |

---

## 1. Thin OwnAgent Assistant runtime (X1)

| Item | Spec lock |
|------|-----------|
| Boundary | Thin OwnAgent / Assistant runtime for owning Participant only — strictly **1:1** |
| Strategy | Reads/uses **own** minimal Strategy (StrategyBody via existing FieldPolicy OwnAgent R/W when acting for owner) |
| Identity | OwnAgent only — not Counterparty / Stranger agent invent |
| P6 | Communication with own AI only (roadmap spirit) |

---

## 2. Platform tools / gateway bind (#67)

| Item | Spec lock |
|------|-----------|
| Path | Runtime uses **platform tools / gateway path only** |
| FieldPolicy | Sibling **#67** binds FieldPolicy on tool I/O (allowlist + scrub) |
| Forbidden | Raw DB; arbitrary internal HTTP; prompt-only soft wall as sole control |
| Cross-ref | Do not re-specify #67 allowlist/scrub details here — bind and cite |

---

## 3. Authn / deny hygiene + no leakage

| Item | Spec lock |
|------|-----------|
| Unauthenticated | → **401** |
| Wrong principal / cross-tenant | → **403** or **404** (uniform deny; Spec may keep PoC/#32 consistency) |
| Model context / responses | **No** LoginEmail; **no** private Strategy of others; **no** denied fields |

---

## 4. Soft OTel / audit / idempotent-offers (weave only)

| Touchpoint | Spec note (not a Story) |
|------------|-------------------------|
| Assistant invoke | Soft: OTel/audit if SA-REV-MVP-C hooks already on path |
| Tool/offer writes via Assistant | Soft: idempotent-offers / audit weave where Assistant touches offer paths under #67 |
| Invent | **Do not** invent a 5th Story or observability product |

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-28__security__verification__mvp-stage-c-thin-assistant-spec-checklist.md`  
**Prior SA Security PASS:** `verification/2026-09-28__security__verification__mvp-stage-c-sa-qa-confirm.md`  
**Rule:** Spec QA must **not** PASS until Spec-step Security checklist delivered + Security QA confirms.

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | OwnAgent-only 1:1 | Thin Assistant = OwnAgent for owning Participant only; never Counterparty/Stranger; no multi-party | Locked #1; §1 |
| 2 | StrategyBody via FieldPolicy | Consume/write only when Evaluate allows OwnAgent R/W for owner; never others’ StrategyBody | Locked #2; §1 |
| 3 | Mandatory bind to #67 hard wall | Platform tools/gateway only; no prompt-only soft wall; cross-ref #67 | Locked #3; §2 |
| 4 | No LoginEmail in agent context | LoginEmail never in model/context packs/tool outputs; User-only | Locked #4; §3 |
| 5 | Authn / IDOR fail-closed | Unauth 401; wrong principal/cross-tenant 403/404; uniform deny; no private leakage | Locked #5; §3 |
| 6 | Soft #41 OUT closed by design only | Documents close via #66+#67 — not Stage B claim | Locked #6; Sources; §6 OUT |
| 7 | OUT locked (X1 thin) | X1 thin only; fuller → V1; free-form → V1; A5 → V4; BYO/multi-LLM later; no 5th Story | Locked #8/#9; §6 OUT |
| 8 | Sibling / Gate HOLDs | Does not invent #68/#69 into this Story; Gate #26 backlog; #27 HOLD; no Cognito/MM/DC4 | Locked #9; §6 OUT |
| 9 | Cost / spend | PoC $0; any named LLM/API spend → COO → CEO; do not provision | Locked #9; §7 Host |
| 10 | Traceability + handshake | Cites #66 AC + Option A Stage C + SA delta + SA Security PASS; keep #67/#68/#69 separate; Spec QA PASS only after Security QA | Sources; this §5; Constraints |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Fuller / stronger Assistant | X1 remainder → **V1** |
| Free-form Strategy evaluation engine | → **V1** |
| Strategy sandbox (A5) | → **V4** |
| Agent/tool hard wall implementation details (#67) | Sibling — mandatory bind; do not replace |
| A8 meters/budgets (#68) | Sibling — primary metered consumer relationship only |
| Basic UI / first-party bot (#69) | Sibling — surface only |
| Inventing a 5th Story for OTel/audit/idempotent | Soft weave only (§4) |
| MCP; public OpenAPI; multi-party; MotorMarket / DC4 | OUT |
| Cognito/SSO inventing; PoC/MVP spend | $0 until CEO spend OK |
| Unlocking #26 before Stage C delivery; #27 | #26 backlog; #27 HOLD |
| Claiming Stage B delivered Assistant runtime | Soft #41 OUT closes here + #67 |

---

## 7. Host / cost

Thin OwnAgent runtime behind #67 gateway on O10 modular monolith. Local/$0; ECS Express sketch only; App Runner excluded. PoC **$0**. Any named LLM/API spend → escalate **COO → CEO** (do not provision in Spec acceptance).

---

## 8. Acceptance mapping (issue #66 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Participant can run a **thin** OwnAgent / Assistant runtime that reads/uses **their own** minimal Strategy (StrategyBody via existing FieldPolicy OwnAgent R/W when acting for owner) — strictly **1:1**; no multi-party invent | Locked #1/#2; §1 |
| Assistant acts only as **OwnAgent** for the owning Participant (**P6** spirit: communication with own AI only) — not Counterparty / Stranger agent invent | Locked #1; §1 |
| Runtime uses **platform tools / gateway path** only (no raw DB / arbitrary internal HTTP) — hard-wall sibling **#67** binds FieldPolicy on tool I/O; this Story must not invent a prompt-only soft wall | Locked #3; §2 |
| Unauthenticated → **401**; wrong principal / cross-tenant misuse → fail-closed (**403** or **404** per Spec consistency); **uniform deny**; **no** LoginEmail / private Strategy of others / denied fields in model context or responses | Locked #4/#5; §3 |
| Automated tests (or equivalent): owner OwnAgent can use own Strategy; stranger/cross-tenant deny; unauth deny; no LoginEmail in agent context packs | §8.1 below; Spec QA + Dev Plan Done-lists |
| Documented as **X1** MVP **thin** — fuller / stronger Assistant → **V1**; free-form Strategy engine → **V1**; Strategy sandbox (**A5**) → **V4**; BYO / multi-LLM breadth → later stages | Locked #8; §6 OUT |
| Soft Spec weave only (not a separate Story): where Assistant path touches SA-REV-MVP-C **OTel / audit / idempotent-offers** hooks already on roadmap, Spec notes the touchpoints — no new product surface invented | Locked #7; §4 |

### 8.1 Automated tests detail (binding)

| Case | Expect |
|------|--------|
| Owner OwnAgent | Can use own Strategy (StrategyBody via FieldPolicy OwnAgent R/W) |
| Stranger / cross-tenant | Deny; no private Strategy leakage |
| Unauthenticated | Deny (**401**); no private fields in errors |
| LoginEmail | Never in agent context packs / tool outputs |
| Gateway bind | Runtime path uses platform tools / #67 gateway (not raw DB / arbitrary internal HTTP) |

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only; CA PASS thin Assistant pick A + Option A cited
- [ ] Locks OwnAgent-only 1:1; StrategyBody via FieldPolicy; mandatory #67 bind
- [ ] No prompt-only soft wall; no LoginEmail in context; authn/IDOR fail-closed
- [ ] Soft #41 OUT closed by #66+#67 design — not Stage B claim
- [ ] Soft OTel/audit/idempotent weave only — no 5th Story
- [ ] **§8 Acceptance mapping complete** — all issue #66 AC bullets present as rows
- [ ] **Automated tests AC** in §8 map + §8.1 detail
- [ ] **Security 1–10** bound (§5) — ask Security QA before PASS
- [ ] Separate from #67/#68/#69/#18 framing; Gate #26 backlog; #27 HOLD; $0
- [ ] BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS (Chief)

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Implement thin OwnAgent-only Assistant runtime (1:1)
- [ ] StrategyBody via existing FieldPolicy OwnAgent R/W for owner
- [ ] Bind platform tools / #67 gateway only — no prompt-only soft wall
- [ ] Enforce unauth 401; cross-tenant fail-closed; no LoginEmail in context
- [ ] Soft weave OTel/audit/idempotent touchpoints where path hits
- [ ] **Automated tests** per §8 / §8.1
- [ ] Do not implement §6 OUT

**Next:** Spec QA verify → ask Security QA → confirm to Chief Spec only. Triad CLOSE after Spec Security QA PASS (Chief). Soft #41 OUT closes with #67 under wall. Gate #26 backlog; PoC $0.
