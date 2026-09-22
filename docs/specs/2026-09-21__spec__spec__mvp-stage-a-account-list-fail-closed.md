# Spec — MVP Stage A: Account list fail-closed (#32)

**Status:** Senior Spec — Security-bound; SA Arch QA PASS + BA AC locked; triad CLOSE HOLD for Spec Security QA only  
**Date:** 2026-09-21  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY Stage A Specs #31 + #32 (CEO unlock)  
**Issue:** https://github.com/ioaikh/dealoware/issues/32  
**Parent:** https://github.com/ioaikh/dealoware/issues/18 · Option A · `stage:mvp` · Stage A  
**DOC-FLOW:** `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md`  
**Constraints:** Separate Spec from #31 (cross-ref only). Extend PoC #4–#7; do not invent Stage B/C. PoC **$0**. Gate **#24** not opened by Spec. **BA AC locked (CBA PASS). SA Arch QA PASS** — cite Pick A `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md` (repository/query party/owner fail-closed lists). Spec QA may verify + ask Security QA. **Triad CLOSE / Dev Plan unlock ping HOLD** until Spec Security QA PASS (Chief clears).

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #32 AC + OUT | https://github.com/ioaikh/dealoware/issues/32 | Binding acceptance |
| Parent #18 / Option A | https://github.com/ioaikh/dealoware/issues/18 · `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall; Stage A list fail-closed |
| Stage A SA options (**Pick A** PASS) | `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md` | Repository/query owner+party fail-closed lists |
| Spec Security checklist | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-checklist.md` | Points **1–10** |
| PoC #4 / #5 / #6 / #7 | Artifact / auth / negotiation / identity-seal Specs | Baselines to harden |
| Sibling #31 Spec | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | Field projection complement |

**Product alignment:** CEO Stage A named slice — account-scoped list/get fail-closed for negotiations / offers / artifacts. Stage B/C HOLD. Conflicts → PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 0 | SA Pick A | Repository / query-plane owner/party constraints for Artifact/Negotiation/Offer list+get (cite Stage A SA arch) |
| 1 | Artifact list/get | Fail-closed to **owning** Participant (`OwnerParticipantId` == principal `sub`) |
| 2 | Negotiation list/get | Fail-closed to **party** (A or B) — authorized account scope |
| 3 | Offer list/get | Fail-closed to **party** of parent negotiation (PoC party rule) |
| 4 | Unauthorized | Fail-closed **no leakage** — 404 preferred or 403; uniform deny; no private fields |
| 5 | Query plane | Constraints in repository/query — not UI-only filter |
| 6 | Authn | Validated #5 principal required; unauthenticated → 401/403 |
| 7 | Complement #31 | Field ACL projects fields; list isolation does **not** replace Field ACL or weaken #6 party rules |
| 8 | OUT | Strategy lists; discovery/search product; Stage B/C; agent wall; gate #24 open; Cognito/MM/DC4 |

**Issue wording “owning account”** = authenticated Participant’s **authorized account scope** (owner for artifacts; party for negotiations/offers). Do not invent multi-tenant admin views.

---

## 1. Fail-closed contracts

### Authn

All list/get paths below require validated principal. Unauthenticated → **401/403** with bodies that omit private fields.

### Artifact (`GET` list + get-by-id)

| Rule | Spec lock |
|------|-----------|
| Authorized | `OwnerParticipantId` == caller `sub` |
| Unauthorized / IDOR | Fail-closed (404 preferred or 403); no secret-bearing body |
| Align | Harden PoC #4 list-own / get owner-scope |

### Negotiation (`GET` list + get-by-id)

| Rule | Spec lock |
|------|-----------|
| Authorized | Caller is party A or B |
| Unauthorized | Fail-closed; no leak of other accounts’ negotiations |
| Align | Harden PoC #6 party-only get |

### Offer (`GET` list + get-by-id)

| Rule | Spec lock |
|------|-----------|
| Authorized | Caller is party to parent negotiation |
| Unauthorized | Fail-closed; no cross-account offer leak |

### Deny hygiene

| Rule | Spec lock |
|------|-----------|
| Error bodies | No private fields; no contact/PII; no FieldClass secret values |
| Empty lists | OK for authorized empty scope; must not smuggle other tenants’ data |
| Existence | Prefer 404 for non-authorized get-by-id (consistent with PoC) |

---

## 2. Tests (binding)

Automated tests required:

- Owner/party OK for each resource type
- Stranger deny
- Cross-tenant IDOR deny on get-by-id
- Unauthenticated deny

---

## 3. Cross-refs (non-merge)

| Spec | Relationship |
|------|--------------|
| #31 | Field projection for allowed rows; list isolation selects **which rows** |
| #6 | 1:1 party-only rules remain |
| #7 | Seal stub unchanged |

---

## 4. Host / cost

Extend O10 modular monolith query/repository filters. Local/$0; ECS Express sketch only; no AWS/IdP provision.

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-checklist.md`

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Fail-closed authn | Validated principal; 401/403; no private-field leakage | §1 Authn; Locked #6 |
| 2 | Negotiation isolation | Party-scoped list/get; unauthorized fail-closed | §1 Negotiation; Locked #2 |
| 3 | Offer isolation | Party-scoped via parent negotiation | §1 Offer; Locked #3 |
| 4 | Artifact isolation | Owner-scoped; IDOR harden | §1 Artifact; Locked #1 |
| 5 | No private-field leakage in denies | Uniform deny; no other Participants’ private fields | §1 Deny hygiene |
| 6 | Complement #31; don’t weaken #6 | Cross-ref Field ACL; keep party-only | §3; Locked #7 |
| 7 | No Stage B/C inventing | No Strategy list ACL / agent wall / Cognito / MM/DC4 | Locked #8; §6 OUT |
| 8 | Cross-story non-merge | No rewrite #31/#7/PoC | §3; Constraints |
| 9 | Cost / spend | PoC $0 | §4 |
| 10 | Traceability + handshake | Cites #32 AC + Option A/#18; Spec QA PASS only after Security QA | Sources; this §5 |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Strategy list ACL | Stage B HOLD |
| Discovery/search product | Out |
| Agent/tool hard wall | Stage C HOLD |
| Contact share-after-Accept | Stage B HOLD |
| Gate #24 open | After Stage A delivery |
| Cognito/SSO / vault spend | Out |
| MotorMarket/DC4 | Out |
| Rewriting #31 / #7 / PoC | Cross-ref only |

---

## 7. Acceptance mapping (issue #32 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Negotiation list/get fail-closed to owning account | §1 Negotiation (party = authorized scope) |
| Offer list/get fail-closed | §1 Offer |
| Artifact list/get fail-closed | §1 Artifact |
| Unauthorized fail-closed no leakage | §1 Deny hygiene; Locked #4 |

---

## Done-list

### Spec QA

- [ ] DOC-FLOW path; binding sources only
- [ ] Owner vs party authorization locked; query-plane fail-closed
- [ ] Tests requirement present
- [ ] **Security 1–10** bound (§5) — ask Security QA before PASS
- [ ] Separate from #31; Stage B/C OUT; $0
- [ ] SA Arch QA PASS + BA AC locked cleared
- [ ] **HOLD triad CLOSE / Dev Plan ping** until Spec Security QA PASS; Spec QA must ask Security QA before PASS

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec + SA Arch QA PASS gate)

- [ ] Repository/query constraints for Artifact/Negotiation/Offer list+get
- [ ] Automated IDOR/stranger/unauth tests
- [ ] Do not implement §6 OUT

**Next:** Spec QA verify → ask Security QA → confirm to Chief Spec only. Triad CLOSE / Dev Plan unlock after Spec Security QA PASS (Chief).
