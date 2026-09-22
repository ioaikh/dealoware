# Spec — MVP Stage B: Minimal Strategy create/edit (#41)

**Status:** Senior Spec — Security-bound; BA AC locked; triad CLOSE HOLD for Spec Security QA only  
**Date:** 2026-09-22  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage B Specs #40 + #41 + #42 (CEO Stage B unlock)  
**Issue:** https://github.com/ioaikh/dealoware/issues/41  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A (CEO ACCEPTED) · `stage:mvp` · Stage B  
**DOC-FLOW:** `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md`  
**Constraints:** Separate Spec from #40 / #42 (cross-ref only). Consume Stage A #31 Field ACL CLOSED; do not invent Stage C / Assistant runtime / free-form engine. PoC **$0**. Gate **#25** stays **backlog** until Stage B delivery — do **not** open/unlock now. Do **not** unlock #18 Spec/SD as a whole. **BA AC locked.** Cite SA Option A StrategyBody Stage B row. Spec QA must **not** PASS until Security QA confirms. **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears). Stage C HOLD. No MM/DC4; no Cognito/vault/KMS inventing.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #41 AC + OUT | https://github.com/ioaikh/dealoware/issues/41 | Binding acceptance |
| Parent #18 CEO locks | https://github.com/ioaikh/dealoware/issues/18 | Option A dual wall; Stage B named slice |
| Option A architecture (Stage B) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | StrategyBody User + OwnAgent only; Counterparty Deny; Stage B Story split |
| BA note #41 | `plans/2026-09-22__ba__note__story-41-minimal-strategy-crud.md` | Spec-ready refine; P3 minimal; OwnAgent ≠ Assistant |
| Spec Security checklist | `verification/2026-09-22__security__verification__mvp-stage-b-strategy-spec-checklist.md` | Points **1–10** |
| Stage A #31 Spec | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | FieldClass + IFieldPolicy baseline (CLOSED) |
| Stage A #32 Spec | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Owner-scoped list/get spirit |
| Sibling #40 / #42 Specs | `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md`, `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | Cross-ref only — do not merge |

**Product alignment:** CEO Stage B named slice — **P3** MVP **minimal** Strategy CRUD. Fuller free-form → V1; A5 sandbox → V4; thin Assistant → Stage C / X1. Stage C HOLD. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | SA Stage B Strategy ACL | StrategyBody Owner + OwnAgent only; never Counterparty (cite SA StrategyBody row + Story split) |
| 1 | Owner-scoped CRUD | Owning Participant can **create**, **edit**, **get**, and **list own** a minimal Strategy; query plane owner-scoped — never “filter in UI only” |
| 2 | StrategyBody FieldClass | Via Stage A `IFieldPolicy`: **User** R/W; **OwnAgent** R/W when acting for owner; **Counterparty Deny**; **Stranger / Unauth Deny** |
| 3 | Never to counterparty | Shared 1:1 Negotiation DTOs **never** expose StrategyBody / private Strategy fields |
| 4 | Fail-closed IDOR | Cross-tenant Strategy list/get and IDOR → fail-closed; unauth → **401**; wrong principal → **403** or **404** (PoC/#32 consistency OK); uniform deny; no private-field leakage |
| 5 | OwnAgent = API policy only | OwnAgent StrategyBody ACL rows are **API policy** — **not** Assistant / tool runtime delivery (Stage C / X1 out) |
| 6 | Consume #31 | Add StrategyBody policy rows / enforcement on #31 registry; do **not** rewrite #31 or #32 AC |
| 7 | Minimal representation | Spec may use opaque/text StrategyBody document without inventing free-form condition types / evaluation engine |
| 8 | Scope label | Documented **P3** MVP **minimal**; fuller free-form → **V1**; A5 → **V4**; thin Assistant → Stage C / **X1** |
| 9 | OUT | Fuller engine; Assistant runtime; discovery (#40); contact ShareOutbound (#42); gate #25 open; #18 Spec/SD unlock; Cognito/MM/DC4; vault/KMS |

---

## 1. Minimal Strategy CRUD (owner-scoped)

### Query plane

| Item | Spec lock |
|------|-----------|
| Create / edit / get / list-own | Bound to owning Participant (`OwnerParticipantId` == principal `sub`, or equivalent) |
| List/get | Repository/query constraints — **not** UI-only filter |
| Cross-tenant / IDOR | Fail-closed; no StrategyBody leakage |

### Minimal document

| Item | Spec lock |
|------|-----------|
| Shape | Minimal Strategy document bound to owner account (e.g. opaque/text StrategyBody) |
| Inventing | Do **not** invent free-form condition types, evaluation runtime, or sandbox (A5) |

### Authn / deny hygiene

| Item | Spec lock |
|------|-----------|
| Unauthenticated | → **401** |
| Wrong principal | → **403** or **404** (Spec may keep PoC/#32 consistency) |
| Error bodies | Uniform deny; **no** private fields / StrategyBody |

---

## 2. StrategyBody FieldClass ACL (Stage B)

| Principal | StrategyBody | Spec lock |
|-----------|--------------|-----------|
| User (owner) | Read / Write | Allowed |
| OwnAgent (acting for owner) | Read / Write | Allowed as **API policy row only** — not Assistant runtime |
| Counterparty | Deny | Negotiation DTOs never carry StrategyBody |
| Stranger / Unauth | Deny | Fail-closed |

Enforcement via Stage A `IFieldPolicy.Evaluate(principal, StrategyBody, action, resourceContext)` (or equivalent) on Strategy API/DB projection paths. Deny-by-default for unknown classes remains #31 baseline.

---

## 3. Counterparty negotiation DTOs + cross-refs

| Spec / rule | Relationship |
|-------------|--------------|
| Shared 1:1 Negotiation / Offer views | Negotiation-scoped party fields only; StrategyBody **never** to counterparty |
| #31 | Baseline FieldClass + IFieldPolicy; this Story adds StrategyBody rows — do not rewrite #31 |
| #32 | Owner-scoped list spirit complement; do not rewrite |
| #40 | Instant search must omit StrategyBody (sibling) — separate Story |
| #42 | Contact ShareOutbound separate Story |
| Stage C / X1 | Assistant runtime HOLD — OwnAgent row ≠ runtime inventing |

---

## 4. Host / cost

Extend O10 modular monolith (`Dealoware.Domain` StrategyBody FieldClass + Api/Application owner-scoped CRUD). Local/$0; ECS Express sketch only; App Runner excluded; no AWS/IdP/vault provision. PoC **$0**.

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-22__security__verification__mvp-stage-b-strategy-spec-checklist.md`

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Owner-scoped query plane | Create/edit/get/list-own owner-scoped; never UI-only filter; IDOR fail-closed | Locked #1/#4; §1 Query plane |
| 2 | StrategyBody FieldClass ACL | User R/W; OwnAgent R/W for owner; Counterparty / Stranger / Unauth Deny | Locked #2; §2 |
| 3 | Never to counterparty | Negotiation DTOs never expose StrategyBody / private Strategy | Locked #3; §3 |
| 4 | Authn fail-closed | Unauth → 401; wrong principal → 403/404; uniform deny; no private-field leakage | Locked #4; §1 Authn |
| 5 | OwnAgent = API policy row only | Not Assistant / tool runtime (Stage C / X1 out) | Locked #5; §2; §3 |
| 6 | Consume #31, don’t rewrite | Add StrategyBody rows on #31 registry; no rewrite #31/#32 | Locked #6; §3 |
| 7 | No Stage C / Assistant inventing | No thin/full Assistant, agent hard wall (#26), Cognito/SSO, MM/DC4 | Locked #9; §6 OUT; Constraints |
| 8 | OUT locked | P3 minimal; fuller → V1; A5 → V4; thin Assistant → Stage C/X1; gate #25 backlog | Locked #8/#9; §6 OUT |
| 9 | Cost / spend | PoC $0; no IdP/vault required to accept Spec | §4 |
| 10 | Traceability + handshake | Cites #41 AC + #18 Option A Stage B; Spec QA PASS only after Security QA; keep #40/#42 separate | Sources; this §5; Constraints |

---

## 6. Explicit OUT

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

---

## 7. Acceptance mapping (issue #41 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Owning Participant can **create**, **edit**, **get**, and **list own** a **minimal** Strategy document bound to their account (query plane: owner-scoped — never “filter in UI only”) | Locked #1/#7; §1 Query plane; §1 Minimal document |
| **StrategyBody** (CEO example FieldClass) enforceable via Stage A `IFieldPolicy` (or equivalent): **User** Read/Write; **OwnAgent** Read/Write when acting for owner; **Counterparty Deny**; **Stranger / Unauth Deny** | Locked #2; §2 |
| Counterparty views of a shared 1:1 Negotiation **never** expose StrategyBody / private Strategy fields (negotiation-scoped DTOs only — align #18 AC) | Locked #3; §3 |
| Cross-tenant Strategy list/get and IDOR attempts fail-closed; unauthenticated → **401**; wrong principal → **403** (or **404** if Spec keeps PoC consistency); **uniform deny bodies** with **no** private-field leakage | Locked #4; §1 Authn / deny hygiene |
| Automated tests (or equivalent evidence): owner OK create/edit/get/list-own; OwnAgent allowed StrategyBody when acting for owner (API policy row — **not** Assistant runtime); counterparty Strategy deny; stranger list/get deny; unauthenticated deny | §7.1 below; Spec QA + Dev Plan/SD Done-lists |
| Documented as **P3** MVP **minimal** CRUD — full free-form Strategy conditions / evaluation engine → **V1**; Strategy sandbox (**A5**) → **V4**; thin Assistant runtime → Stage C / **X1** (out of this Story) | Locked #8/#9; §6 OUT |

### 7.1 Automated tests detail (binding)

| Case | Expect |
|------|--------|
| Owner | OK create / edit / get / list-own |
| OwnAgent (acting for owner) | Allowed StrategyBody Read/Write via **API policy row** — not Assistant runtime |
| Counterparty | StrategyBody / private Strategy **denied** on negotiation DTOs |
| Stranger | Strategy list/get **deny** |
| Unauthenticated | Deny (**401**); no private fields in errors |

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only; SA StrategyBody Stage B row cited
- [ ] Locks owner-scoped create/edit/get/list-own; query plane fail-closed
- [ ] StrategyBody User R/W + OwnAgent R/W (API policy only) + Counterparty/Stranger/Unauth Deny
- [ ] Never expose StrategyBody on counterparty negotiation DTOs
- [ ] Consume #31; do not rewrite #31/#32; OwnAgent ≠ Assistant runtime
- [ ] **§7 Acceptance mapping complete** — all issue #41 AC bullets present as rows
- [ ] **Automated tests AC** in §7 map + §7.1 detail (owner OK; OwnAgent API policy; counterparty deny; stranger deny; unauth deny)
- [ ] **Security 1–10** bound (§5) — ask Security QA before PASS
- [ ] Separate from #40/#42; Stage C OUT; gate #25 backlog; #18 Spec/SD HOLD; $0
- [ ] BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS (Chief)

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Implement owner-scoped minimal Strategy create/edit/get/list-own
- [ ] Encode StrategyBody FieldClass via IFieldPolicy (User R/W; OwnAgent R/W for owner; Counterparty/Stranger/Unauth Deny)
- [ ] Ensure negotiation DTOs never carry StrategyBody
- [ ] **Automated tests** per §7 / §7.1
- [ ] Do not implement §6 OUT

**Next:** Spec QA verify → ask Security QA → confirm to Chief Spec only. Triad CLOSE after Spec Security QA PASS (Chief). Gate #25 backlog; do not unlock #18 Spec/SD; Stage C HOLD; PoC $0.
