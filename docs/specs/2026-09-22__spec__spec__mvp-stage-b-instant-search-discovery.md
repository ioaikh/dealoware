# Spec — MVP Stage B: Instant search / discovery (#40)

**Status:** Senior Spec — Security-bound; BA AC locked; triad CLOSE HOLD for Spec Security QA only  
**Date:** 2026-09-22  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage B Specs #40 + #41 + #42 (CEO Stage B unlock)  
**Issue:** https://github.com/ioaikh/dealoware/issues/40  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A (CEO ACCEPTED) · `stage:mvp` · Stage B  
**DOC-FLOW:** `specs/2026-09-22__spec__spec__mvp-stage-b-instant-search-discovery.md`  
**Constraints:** Separate Spec from #41 / #42 (cross-ref only). Consume Stage A #31+#32 CLOSED; do not invent Stage C / Assistant / saved-search / A1. PoC **$0**. Gate **#25** stays **backlog** until Stage B delivery — do **not** open/unlock now. Do **not** unlock #18 Spec/SD as a whole. **BA AC locked.** Cite SA Option A Stage B discovery row. Spec QA must **not** PASS until Security QA confirms. **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears). Stage C HOLD. No MM/DC4; no Cognito/vault/KMS inventing.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #40 AC + OUT | https://github.com/ioaikh/dealoware/issues/40 | Binding acceptance |
| Parent #18 CEO locks | https://github.com/ioaikh/dealoware/issues/18 | Option A dual wall; Stage B named slice |
| Option A architecture (Stage B) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Stage B discovery must not leak secrets; discovery ≠ owner inventory |
| BA note #40 | `plans/2026-09-22__ba__note__story-40-instant-search-discovery.md` | Spec-ready refine; P2 instant only |
| Spec Security checklist | `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-checklist.md` | Points **1–10** |
| Stage A #31 / #32 Specs | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md`, `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Field ACL consume; owner inventory separate |
| PoC Artifact / #5 auth | `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md`, `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Discoverable Artifact fields + authn baseline |
| Sibling #41 / #42 Specs | `specs/2026-09-22__spec__spec__mvp-stage-b-minimal-strategy-crud.md`, `specs/2026-09-22__spec__spec__mvp-stage-b-contact-on-accept.md` | Cross-ref only — do not merge |

**Product alignment:** CEO Stage B named slice — **P2** MVP **instant** search only. Saved-search → V1; A1 matching → V2. Stage C HOLD. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | SA Stage B discovery | Authenticated instant search; search payloads must **not** leak secrets; discovery ≠ Artifact owner inventory (cite SA Stage B row) |
| 1 | Authn | Instant search requires validated #5 principal; unauthenticated → **401**; no private-field leakage in errors |
| 2 | Discoverable fields | Search/result fields limited to Artifact / negotiation-scoped fields **already on path** and **allowed for discovery** (PoC Artifact D1–D5 / MVP Artifact surface). **No new Artifact schema** for search |
| 3 | Omit secrets | Serializers omit denied FieldClasses from search results. Explicitly **absent**: **StrategyBody**, **LoginEmail**, **ContactEmail**, private account lists / Strategy inventory, auth secrets |
| 4 | Separate surface | Discovery is a **separate surface** from #32 owner inventory list/get; must **not** dump another Participant’s private account inventory or secrets |
| 5 | Uniform deny | Wrong-principal / stranger misuse → fail-closed with **uniform deny bodies**; no private-field leakage (align #18 / #31 / #32) |
| 6 | Consume #31 | Use Stage A `IFieldPolicy` / FieldClass for projection omit; do **not** rewrite #31 registry AC |
| 7 | Scope label | Documented **P2** MVP **instant** only; saved-search / market monitoring → **V1**; complementary-intent matching (**A1**) → **V2** |
| 8 | OUT | Saved-search; A1; Strategy ACL (#41); contact ShareOutbound (#42); Stage C Assistant; gate #25 open; #18 Spec/SD unlock; Cognito/MM/DC4; vault/KMS |

---

## 1. Instant search surface

### Authn

| Item | Spec lock |
|------|-----------|
| Principal | Validated #5 (or successor) principal required on instant-search paths |
| Unauthenticated | → **401**; error bodies omit private fields |
| Wrong principal / stranger misuse | Fail-closed; uniform deny bodies; no secret fields |

### Discoverable fields (no schema invent)

| Item | Spec lock |
|------|-----------|
| In | Artifact fields already on path and allowed for discovery (PoC Artifact D1–D5 / MVP Artifact CRUD surface — e.g. Subject / Intent / Value / Location / Time class fields already delivered or in-scope) |
| Out of invent | Spec does **not** invent new Artifact schema, indexes as product requirements, or matching semantics beyond instant search over those fields |
| Projection | Results return only Artifact / negotiation-scoped fields allowed for discovery |

### Search payload omit (binding)

| Must be absent from search payloads | Note |
|-------------------------------------|------|
| StrategyBody | FieldClass — never in discovery results |
| LoginEmail | FieldClass — never |
| ContactEmail | FieldClass — never (share path is #42) |
| Private account lists / Strategy inventory | Not discovery |
| Auth secrets | Never in API bodies |

Omit via Stage A `IFieldPolicy` / serializer projection — denied classes stripped; do not return placeholders that leak.

---

## 2. Discovery ≠ owner inventory (#32)

| Surface | Spec lock |
|---------|-----------|
| #32 Artifact list/get | Owner-scoped inventory (`OwnerParticipantId` == principal) — fail-closed |
| #40 Instant search | Separate authenticated discovery surface over **discoverable** Artifact fields of others — must **not** dump another Participant’s private inventory or secrets |
| Cross-use | Discovery must not be implemented as “list all Artifacts then filter in UI”; secrets omission is mandatory |

---

## 3. Cross-refs (non-merge)

| Spec | Relationship |
|------|--------------|
| #31 | Consumed for FieldClass omit on search DTOs; do not rewrite |
| #32 | Owner inventory remains separate; discovery does not replace or weaken list fail-closed |
| #41 | Strategy ACL / StrategyBody out of this Story |
| #42 | ContactEmail ShareOutbound out of this Story |
| #5 / PoC Artifact | Authn + discoverable field baselines |

---

## 4. Host / cost

Extend O10 modular monolith (Application/Api search path + Domain FieldPolicy projection). Local/$0; ECS Express sketch only; App Runner excluded; no AWS/IdP/vault provision. PoC **$0**.

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-22__security__verification__mvp-stage-b-discovery-spec-checklist.md`

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Authn fail-closed | Validated #5 principal; unauthenticated → **401**; no private-field leakage | Locked #1; §1 Authn |
| 2 | Discovery ≠ owner inventory | Separate surface from #32; no dump of another Participant’s private inventory/secrets | Locked #4; §2 |
| 3 | Search payload omit secrets | Absent: StrategyBody, LoginEmail, ContactEmail, private lists / Strategy inventory, auth secrets | Locked #3; §1 Search payload omit |
| 4 | Discoverable fields only | Artifact fields already on path / allowed for discovery; no new Artifact schema | Locked #2; §1 Discoverable fields |
| 5 | Uniform deny / no leak | Wrong-principal / stranger → uniform deny; no private fields in errors | Locked #5; §1 Authn |
| 6 | Consume Field ACL, don’t rewrite #31 | IFieldPolicy projection omit; no rewrite of #31 AC | Locked #6; §3 |
| 7 | No Stage C / #18 inventing | No Assistant hard wall, Cognito/SSO, MM/DC4; #18 Spec/SD HOLD | Locked #8; §6 OUT; Constraints |
| 8 | OUT locked | P2 instant only; saved-search → V1; A1 → V2; gate #25 backlog | Locked #7/#8; §6 OUT |
| 9 | Cost / spend | PoC $0; no IdP/vault required to accept Spec | §4 |
| 10 | Traceability + handshake | Cites #40 AC + #18 Option A Stage B; Spec QA PASS only after Security QA; keep #41/#42 separate | Sources; this §5; Constraints |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Saved search / market monitoring | Rest of P2 → **V1** |
| First-class complementary-intent matching (A1) | → **V2** |
| Strategy ACL / Strategy CRUD | **#41** |
| ContactEmail ShareOutbound-after-Accept | **#42** |
| Stage C agent/tool hard wall; thin Assistant runtime | HOLD |
| Gate #25 open / unlock before Stage B delivery | Backlog |
| Unlocking #18 Spec/SD as a whole; #26–#27 | HOLD |
| New Artifact schema for search | Out |
| Cognito/SSO / vault/KMS spend | Out |
| MotorMarket / DC4 | Out |
| Rewriting #31 / #32 | Cross-ref / consume only |

---

## 7. Acceptance mapping (issue #40 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Authenticated Participant can run **instant search** over **discoverable Artifact fields already on path** (PoC Artifact model / MVP Artifact CRUD surface — e.g. Subject / Intent / Value / Location / Time class fields already delivered or in-scope for MVP Artifact). Spec does **not** invent new Artifact schema for search | Locked #1/#2; §1 Authn; §1 Discoverable fields |
| Search results return only Artifact / negotiation-scoped fields **allowed for discovery** — serializers omit denied classes. Explicitly **absent** from search payloads: **StrategyBody**, **LoginEmail**, **ContactEmail**, private account lists / Strategy inventory, auth secrets | Locked #3; §1 Search payload omit; §5 points 3–4 |
| Discovery is a **separate surface** from Artifact **owner inventory** list/get (#32 owner-scoped). Instant search must **not** dump another Participant’s private account inventory or secrets (architecture: *must not dump secrets*) | Locked #4; §2 |
| Unauthenticated callers → fail-closed (**401**); wrong-principal / stranger misuse of discovery → fail-closed with **uniform deny bodies** and **no** private-field leakage (align #18 / #31 / #32 spirit) | Locked #1/#5; §1 Authn |
| Automated tests (or equivalent evidence): auth’d search OK on discoverable fields; search payload has no StrategyBody / LoginEmail / ContactEmail / private lists; unauthenticated deny; no inventory/secrets dump | §7.1 below; Spec QA + Dev Plan/SD Done-lists |
| Documented as **P2** MVP **instant** search only — **saved-search / market monitoring explicitly out → V1**; first-class complementary-intent matching (**A1**) → V2 (not this Story) | Locked #7/#8; §6 OUT |

### 7.1 Automated tests detail (binding)

| Case | Expect |
|------|--------|
| Auth’d Participant | Instant search OK over discoverable Artifact fields already on path |
| Search payload | **No** StrategyBody / LoginEmail / ContactEmail / private account lists / Strategy inventory / auth secrets |
| Unauthenticated | Deny (**401**); no private fields in errors |
| Inventory / secrets dump | Discovery does **not** return another Participant’s private owner inventory or secrets |

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only; SA Stage B discovery row cited
- [ ] Locks auth’d instant search; discoverable Artifact fields only; no new Artifact schema
- [ ] Omit StrategyBody / LoginEmail / ContactEmail / private lists / auth secrets from search payloads
- [ ] Discovery ≠ #32 owner inventory; uniform deny; unauth 401
- [ ] Consume #31 Field ACL; do not rewrite #31/#32
- [ ] **§7 Acceptance mapping complete** — all issue #40 AC bullets present as rows
- [ ] **Automated tests AC** in §7 map + §7.1 detail (auth’d OK; omit secrets; unauth deny; no inventory dump)
- [ ] **Security 1–10** bound (§5) — ask Security QA before PASS
- [ ] Separate from #41/#42; Stage C OUT; gate #25 backlog; #18 Spec/SD HOLD; $0
- [ ] BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS (Chief)

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Implement auth’d instant search over discoverable Artifact fields already on path
- [ ] Enforce serializer omit of denied classes (StrategyBody, LoginEmail, ContactEmail, private lists, auth secrets)
- [ ] Keep discovery surface separate from #32 owner inventory
- [ ] **Automated tests** per §7 / §7.1
- [ ] Do not implement §6 OUT

**Next:** Spec QA verify → ask Security QA → confirm to Chief Spec only. Triad CLOSE after Spec Security QA PASS (Chief). Gate #25 backlog; do not unlock #18 Spec/SD; Stage C HOLD; PoC $0.
