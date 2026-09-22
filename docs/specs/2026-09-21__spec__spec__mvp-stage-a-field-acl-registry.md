# Spec — MVP Stage A: Field ACL registry + API projection (#31)

**Status:** Senior Spec — Security-bound; bounce amend v2 (§7 full AC map + §7.1 tests); Soft DisplayName non-blocking; triad CLOSE HOLD for Spec Security QA only  
**Date:** 2026-09-21  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage A Specs #31 + #32 (CEO unlock)  
**Issue:** https://github.com/ioaikh/dealoware/issues/31  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A (CEO ACCEPTED) · `stage:mvp` · Stage A  
**DOC-FLOW:** `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md`  
**Constraints:** Separate Spec from #32 (cross-ref only). Extend PoC #5–#7; do not invent Stage B/C. PoC **$0**. Gate **#24** not opened by Spec. **BA AC locked (CBA PASS). SA Arch QA PASS** — cite Pick A `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md` (Domain FieldClass + IFieldPolicy + API projection). Soft DisplayName **non-blocking**. Spec QA may verify + ask Security QA. **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears).

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #31 AC + OUT | https://github.com/ioaikh/dealoware/issues/31 | Binding acceptance |
| Parent #18 CEO locks | https://github.com/ioaikh/dealoware/issues/18 | Option A dual wall; open-ended FieldClass |
| Option A architecture | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | FieldPolicy + dual wall |
| Stage A SA options (**Pick A** PASS) | `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md` | Domain FieldClass + IFieldPolicy + API projection |
| Spec Security checklist | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-checklist.md` | Points **1–10** |
| PoC #5 / #6 / #7 | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md`, `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md`, `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md` | Authn / party / seal baselines |
| Sibling #32 Spec | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Cross-ref only |

**Product alignment:** CEO named Stage A slice only — Field ACL registry + API projection (generic any account info). Stage B/C HOLD. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | SA Pick A | Domain `FieldClass` registry + `IFieldPolicy.Evaluate` + API/DTO projection filters (cite Stage A SA arch) |
| 1 | FieldClass registry | Extensible for **any** account-info class (open-ended; not email-only) |
| 2 | IFieldPolicy | Evaluate on **API/DB projection** paths; omit denied fields from DTOs |
| 3 | Dual wall | Option A accepted end-state; Stage A implements **API layer only**; agent/tool hard wall = Stage C HOLD |
| 4 | Examples | `LoginEmail`, `ContactEmail` starters; `DisplayName` soft/illustrative — **non-blocking** if deferred |
| 5 | Deny-by-default | Unknown/unregistered FieldClass → **deny** projection |
| 6 | LoginEmail | User Read/Write; **OwnAgent Deny** on API/DB (even before Stage C tools) |
| 7 | ContactEmail | User R/W; OwnAgent Read allowed by policy; counterparty Deny; ShareOutbound Deny until Stage B; **#7 stub unchanged** |
| 8 | OUT | Strategy ACL; ContactEmail share-after-Accept; agent hard-wall impl; Stage B/C; gate #24 open; Cognito/MM/DC4; inventing beyond CEO slice |

---

## 1. FieldClass registry + IFieldPolicy

### Registry

| Rule | Spec lock |
|------|-----------|
| Shape | Extensible registry (not a forever-closed enum) |
| Growth | New secret types later = new FieldClass + policy rows **without** rewriting the dual wall |
| Examples | LoginEmail / ContactEmail — starters; DisplayName soft/illustrative (**non-blocking**) |
| Inventing | Do not invent additional named FieldClasses beyond CEO examples unless labeled illustrative / derived from existing DTO fields |

### Policy evaluation

| Item | Spec lock |
|------|-----------|
| Interface | `IFieldPolicy.Evaluate(principal, fieldClass, action, resourceContext)` (or equivalent) |
| Actions | At least `Read` / `Write` / `List`; `ShareOutbound` may stub **Deny** in Stage A |
| Context | Carries ownership/party/accept-grant signals as needed for evaluation |
| Deny-by-default | Unregistered / unknown FieldClass → deny |

### API/DB projection

| Item | Spec lock |
|------|-----------|
| Where | Serializers / response mappers / DB read shaping where projection happens |
| Effect | Fields denied for the caller are **omitted** (not returned with placeholders that leak) |
| Authn | Validated #5 principal required on protected projection paths; unauthenticated → 401/403; errors omit private fields |

---

## 2. Example policy rows (Stage A)

| FieldClass | User | OwnAgent | Counterparty / stranger | ShareOutbound |
|------------|------|----------|-------------------------|---------------|
| LoginEmail | R/W | **Deny** | Deny | Deny |
| ContactEmail | R/W | Read | Deny | **Deny** (Stage B HOLD) |
| DisplayName | Soft / illustrative — **non-blocking** for Spec/SD PASS if not projected yet | Soft | Soft | N/A |

---

## 3. Cross-refs (non-merge)

| Spec | Relationship |
|------|--------------|
| #32 | List/get fail-closed complements field projection; does **not** replace Field ACL |
| #6 | Party-only negotiation rules remain; Field ACL does not weaken them |
| #7 | Identity-seal stub unchanged — no contact release inventing |

---

## 4. Host / cost

Extend O10 modular monolith (`Dealoware.Domain` policy + Api/Application projection). Local/$0; ECS Express sketch only; App Runner excluded; no AWS/IdP provision.

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-checklist.md`

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Stage A API/DB scope | FieldClass + IFieldPolicy on API/DB projection only; agent wall impl Stage C HOLD | Locked #3; §1; §6 OUT |
| 2 | Open-ended FieldClass | Extensible any account-info class; examples ≠ exhaustive | Locked #1/#4; §1 Registry |
| 3 | Deny-by-default | Unknown/unregistered → deny | Locked #5; §1 Policy |
| 4 | LoginEmail User-only (API) | Not projected to counterparty/stranger/OwnAgent via API | Locked #6; §2 |
| 5 | ContactEmail rules (no share) | OwnAgent Read / counterparty Deny; no share-after-Accept; #7 unchanged | Locked #7; §2; §3 |
| 6 | Fail-closed authn | Validated principal; 401/403; no private fields in errors | §1 API/DB projection |
| 7 | No Stage B/C inventing | No Strategy ACL / agent wall / Cognito / MM/DC4 | Locked #8; §6 OUT |
| 8 | Cross-story non-merge | No rewrite of #32/#7/PoC AC | §3; Constraints |
| 9 | Cost / spend | PoC $0; no IdP/vault required | §4 |
| 10 | Traceability + handshake | Cites #31 AC + Option A/#18; Spec QA PASS only after Security QA | Sources; this §5 |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Strategy ACL | Stage B HOLD |
| ContactEmail share-after-Accept | Stage B HOLD |
| Agent/tool hard wall implementation | Stage C HOLD |
| Gate #24 / SA-REV-MVP-A open | After Stage A delivery — not by Spec |
| Cognito/SSO / vault spend | Out |
| MotorMarket/DC4 | Out |
| Rewriting #32 / #7 / PoC Specs | Cross-ref only |

---

## 7. Acceptance mapping (issue #31 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Extensible `FieldClass` registry (open-ended; any account-info class) | §1 Registry; Locked #0/#1 |
| `IFieldPolicy.Evaluate(principal, fieldClass, action, resourceContext)` (or equivalent) on Stage A **API/DB projection**; deny-by-default for unknown/unregistered | §1 Policy evaluation; Locked #2/#5 |
| CEO example FieldClasses as starters (**not** exhaustive): **LoginEmail**, **ContactEmail**, **DisplayName** — do not invent additional named FieldClasses unless labeled illustrative / derived from existing Stage A DTO fields | Locked #4; §1 Examples; §2 (DisplayName soft/**non-blocking**) |
| **LoginEmail:** User Read/Write; **OwnAgent Deny all** on API projection; Counterparty / Stranger / Unauth Deny | Locked #6; §2 LoginEmail row |
| **ContactEmail:** User Read/Write; OwnAgent **Read** allowed; Counterparty / Stranger Deny; **ShareOutbound Deny** in Stage A (share-after-Accept = Stage B HOLD; do not rewrite PoC #7) | Locked #7; §2 ContactEmail row; §3 |
| API/DTO serializers / response mappers **omit** fields denied for the caller; **no private fields in error bodies** | §1 API/DB projection |
| Generic **any-account-info** scope (not email-only); Option A **API-layer** half of dual enforcement — **no** agent gateway / tool scrubber code in this Story | Locked #1/#3; §6 OUT |
| **Automated tests** (or equivalent evidence): owner/User OK for allowed fields; OwnAgent denied LoginEmail; stranger/counterparty denied protected fields; unauthenticated deny | §7.1 below; Spec QA + Dev Plan/SD Done-lists |

### 7.1 Automated tests detail (binding)

| Case | Expect |
|------|--------|
| Owner / User | Allowed fields project OK under policy |
| OwnAgent | **Denied** LoginEmail (all actions) on API projection |
| Stranger / counterparty | Denied protected fields (LoginEmail, ContactEmail, etc.) |
| Unauthenticated | Deny (401/403); no private fields in errors |

Soft DisplayName remains **non-blocking** if not yet projected.

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only; Pick A arch cited
- [ ] Locks FieldClass open-ended + IFieldPolicy API projection + deny-by-default
- [ ] CEO example FieldClasses (LoginEmail / ContactEmail; DisplayName soft non-blocking); #7 unchanged
- [ ] LoginEmail OwnAgent Deny + ContactEmail ShareOutbound Deny rows
- [ ] Omit-denied / error hygiene (DTO + errors)
- [ ] **§7 Acceptance mapping complete** — all issue #31 AC bullets present as rows
- [ ] **Automated tests AC** in §7 map + §7.1 detail (owner OK; OwnAgent denied LoginEmail; stranger/counterparty denied; unauth deny)
- [ ] **Security 1–10** bound (§5) — ask Security QA before PASS
- [ ] Separate from #32; Stage B/C OUT; $0
- [ ] SA Arch QA PASS + BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Implement FieldClass registry + IFieldPolicy + API/DTO projection
- [ ] Encode example policies incl. LoginEmail OwnAgent Deny; ContactEmail ShareOutbound Deny
- [ ] **Automated tests** per §7 / §7.1 (owner OK; OwnAgent denied LoginEmail; stranger/counterparty denied; unauth deny)
- [ ] Do not implement §6 OUT

**Next:** Spec QA re-verify bounce fixes → ask Security QA → confirm to Chief Spec only. Triad CLOSE after Spec Security QA PASS (Chief).
