# Architecture options — MVP Stage A: Field ACL registry + account list fail-closed (#31 + #32)

**Status:** Senior Architect options for Architecture QA (CEO named-slice unlock Stage A). **Amended:** Security answers §6 points 1–10.  
**Date:** 2026-09-21  
**Author:** Dealoware Senior Architect  
**Brief:** Chief Architect PRIORITY — Stories **#31** + **#32**  
**Binding proposal:** `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` (**CEO ACCEPTED Option A**; open-ended FieldClass)  
**Issues:** https://github.com/ioaikh/dealoware/issues/31 · https://github.com/ioaikh/dealoware/issues/32 · parent https://github.com/ioaikh/dealoware/issues/18  
**DOC-FLOW:** `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md`  
**Cost:** PoC/MVP **$0** · Host: **Amazon ECS Express Mode (Fargate)** sketch (`open`) · **App Runner** excluded (`existing-customers-only` + `no-new-features`) · No MotorMarket/DC4  
**HOLD (out of this Stage A deliverable):** Strategy ACL; ContactEmail ShareOutbound-after-Accept; **agent/tool hard wall implementation** (Stage B/C) · Gate **#24 / SA-REV-MVP-A** stays backlog until **after Stage A delivery** (do **not** open now)

## Sources

| Source | Role |
|--------|------|
| #31 / #32 AC | Binding acceptance |
| Option A proposal (CEO accepted) | FieldPolicy + dual wall architecture; open-ended FieldClass |
| PoC post-delivery review | Current `main` modular monolith + auth/party baselines |
| Templates | `architecture/templates/sa-architecture-options-brief-template.md` |
| Security SA checklist | `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md` |

---

## 1. Scope split (#31 vs #32)

| Story | IN (Stage A) | OUT |
|-------|--------------|-----|
| **#31** Field ACL registry + API projection | Extensible `FieldClass` registry; `IFieldPolicy` (or equivalent) on **API/DTO projection** (+ DB read shaping where projection happens); generic **any account-info** (not email-only); Option A **API-layer** half of dual enforcement | Agent/tool gateway impl; Strategy body Story; inventing FieldClasses beyond CEO examples unless labeled illustrative |
| **#32** Account list fail-closed | Negotiation / Offer / Artifact **list + get** fail-closed to **authorized** account/principal; unauthorized → fail-closed **no private-field leak** | Strategy lists; discovery/search product; Stage B/C |

**Authorization meaning for #32 (Spec must not guess):**

| Resource | Authorized viewer (Stage A) |
|----------|-----------------------------|
| **Artifact** list/get | Owning Participant (`OwnerParticipantId` == principal `sub`) — already PoC intent; harden fail-closed + tests |
| **Negotiation** list/get | Participant is **party** to that negotiation (A or B) — not “any owner of unrelated accounts” |
| **Offer** list/get | Participant is **party** to the parent negotiation (or otherwise product-authorized party rule already on PoC) |

Issue wording “owning account” = authenticated Participant’s **authorized account scope** (owner for artifacts; party for negotiations/offers). Do not invent multi-tenant admin views.

---

## 2. Options / tradeoffs + simplest maintainable pick

| Option | Pros | Cons | |
|--------|------|------|--|
| **A. Domain `FieldClass` registry + `IFieldPolicy.Evaluate` + API projection filters; repository party/owner constraints for #32** | Matches CEO Option A; one policy source; Spec-clear; Stage C can bind agent wall later to same Evaluate | Slight Domain API surface growth | **Recommended** |
| B. Ad hoc per-endpoint ifs only | Faster short term | Drift; fails open-ended FieldClass | **Reject** |
| C. Implement agent hard wall now with #31 | Completes dual wall early | Violates CEO Stage A named-slice HOLD | **Reject** |

**Pick: A** for both Stories, delivered as two cohesive SD slices (#31 then #32 or parallel if Spec allows) on existing modular monolith (`Dealoware.Domain` policy + `Dealoware.Api` / Application projection + Infrastructure query filters).

---

## 3. Recommended design (IN)

### 3a. #31 — FieldClass + IFieldPolicy (API layer)

1. **`FieldClass` registry (open-ended)** — not a forever-closed enum; new secret types later = new FieldClass + policy rows **without** rewriting the dual wall.  
2. **CEO example classes (starters, not exhaustive):** `LoginEmail`, `ContactEmail`, `DisplayName` (and other classes only if already required by Stage A projection of existing DTOs — label any extra as **illustrative** / derived from existing fields, do not invent product).  
3. **`IFieldPolicy.Evaluate(principal, fieldClass, action, resourceContext)`** — actions at least `Read` / `Write` / `List` (ShareOutbound exists in Option A but **Stage A does not implement contact share**; may stub Deny).  
4. **API/DTO projection** — serializers / response mappers omit fields denied for the caller.  
5. **LoginEmail:** User Read/Write; **OwnAgent Deny** (even if Stage C agent not built yet — policy rows must already encode this so later tools cannot “forget”).  
6. **ContactEmail:** User R/W; OwnAgent Read allowed by policy; ShareOutbound remains Deny until Stage B.  

**Stage A principal matrix (illustrative FieldClasses — bound to Option A FieldPolicy table; not exhaustive):**

| FieldClass | User | OwnAgent | Counterparty | Stranger / Unauth | Stage A note |
|------------|------|----------|--------------|-------------------|--------------|
| **LoginEmail** | Read/Write | **Deny** | Deny | Deny | CEO lock — User only |
| **ContactEmail** | Read/Write | **Read** | Deny | Deny | ShareOutbound Deny until Stage B; #7 stub untouched |
| **DisplayName** | Read/Write | Read | Read (if already exposed on existing DTOs) | Deny | **Illustrative** Option A example; no inventing public profile |

7. **No agent gateway code in #31** — architecture accepts dual wall; **implementation** of tool scrubbers = Stage C HOLD.

### 3b. #32 — List/get fail-closed

1. All list/get queries for Artifacts / Negotiations / Offers constrained in **repository / query plane** by owner or party — not UI-only filter.  
2. Wrong principal / unauthenticated → **401/403** (as appropriate) with **uniform deny body** — no private fields.  
3. IDOR on get-by-id → fail-closed (404 or 403 per Spec consistency with PoC; **no** leakage of existence via secret-bearing bodies).  
4. Automated tests: owner/party OK; stranger deny; cross-tenant IDOR deny; unauthenticated deny.

### 3c. Hosting / cost

Local/$0; ECS Express Mode sketch only; App Runner excluded. No AWS provision.

---

## 4. Explicit OUT / DEFER

| Item | Status |
|------|--------|
| Strategy ACL / StrategyBody enforcement Story | **HOLD Stage B** |
| ContactEmail ShareOutbound-after-Accept | **HOLD Stage B** |
| Agent/tool hard wall + response scrubbers | **HOLD Stage C** (architecture accepted; not impl now) |
| Gate #24 / SA-REV-MVP-A review issue open | **HOLD until after Stage A delivery** |
| Cognito/SSO, vault/KMS spend | **Out** unless CEO spend OK |
| MotorMarket/DC4 | **Out** |
| Expanding PoC #7 identity-seal stub | **Out** |

---

## 5. Proposed SA architecture-review moments

| Moment ID | Name | Trigger (when) | Scope under review | Next stage HOLD until |
|-----------|------|----------------|--------------------|------------------------|
| **SA-REV-MVP-A** (`gate:sa-arch-review` **#24**) | MVP Stage A — Field ACL + list fail-closed | **After Stage A delivery** of #31+#32 (or SA architecture for that slice closed + SD done) | FieldClass registry + IFieldPolicy API projection; negotiation/offer/artifact list/get fail-closed; open-ended FieldClass; LoginEmail deny-to-agent **policy**; dual wall **impl still Stage C** | CA PASS (Arch QA + Security) before Stage B Spec/eng |
| SA-REV-MVP-B | (unchanged) Stage B | After Stage B slice | Strategy ACL + contact share-after-Accept | CA PASS → Stage C |
| SA-REV-MVP-C | (unchanged) Stage C | After Stage C slice | Agent/tool hard wall impl | CA PASS → close path |
| SA-REV-MVP-CLOSE | MVP post-milestone | Milestone closed | Full MVP vs baselines | CA PASS → V1 |

**Now:** do **not** open #24. Hand moments table to CA for CPM; #24 remains backlog until Stage A delivery moment.

---

## 6. Security answers (Architecture always-critical handshake)

**Checklist:** `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md` (Chief Security, 2026-09-21).  
**Rule:** Architecture QA asks **Security QA** to confirm points **1–10** before PASS to Chief Architect.

| # | Security point | Architecture answer | Cite |
|---|----------------|---------------------|------|
| 1 | Stage A scope lock — API/DB only; agent wall HOLD Stage C | **MET.** Stage A = FieldClass + IFieldPolicy API/DB projection + account list fail-closed. Agent/tool hard wall **implementation** HOLD Stage C; Option A dual wall remains accepted end-state. | Header HOLD; §3a item 7; §4 OUT table |
| 2 | Open-ended FieldClass registry | **MET.** Registry extensible for any account-info class; LoginEmail/ContactEmail/DisplayName are examples not exhaustive; no invented FieldClasses beyond CEO examples unless illustrative. | §3a items 1–2; binding Option A |
| 3 | IFieldPolicy on API/DB projection; deny unknown | **MET.** Evaluate on Stage A projection paths; deny-by-default for unknown/unregistered classes (Spec must encode). | §3a items 3–4; Pick A |
| 4 | LoginEmail User-only on API (not OwnAgent) | **MET.** Policy: User R/W; OwnAgent Deny on API/DB projection even before Stage C tools exist. | §3a item 5 |
| 5 | ContactEmail OwnAgent vs counterparty (API); no share-after-Accept now | **MET.** ContactEmail OwnAgent Read allowed by policy; counterparty Deny; ShareOutbound Deny until Stage B; #7 stub unchanged. | §3a item 6; §4 OUT |
| 6 | Negotiation list/get fail-closed | **MET.** Party-scoped authorized viewer; unauthorized fail-closed no private leak. | §1 auth table; §3b |
| 7 | Offer list/get fail-closed | **MET.** Party-scoped via parent negotiation; same fail-closed. | §1; §3b |
| 8 | Artifact list/get fail-closed owner-scoped | **MET.** OwnerParticipantId == principal sub; IDOR harden + tests. | §1; §3b |
| 9 | No inventing / $0 / #7 preserved | **MET.** No Strategy ACL / agent wall impl / Cognito invent; no MM/DC4; $0; #7 as-is. | §4 OUT; header Cost |
| 10 | Traceability + handshake; #24 backlog | **MET.** Cites #31/#32 AC + Option A only; #24 / SA-REV-MVP-A after Stage A delivery — not opened now. Architecture QA obtains Security QA confirm before PASS. | §5 moments; Done-list |


## Done-list (Architecture QA)

- [ ] Path/name DOC-FLOW
- [ ] #31+#32 AC covered; Option A binding + open-ended FieldClass
- [ ] Party vs owner authorization clarified for Spec
- [ ] IN/OUT; Stage B/C HOLD explicit; #24 not opened now
- [ ] Review moments table present
- [ ] Hosting currency / $0 / no MM-DC4
- [ ] §6 Security answers map checklist points **1–10** with cites
- [ ] Ask **Security QA** to confirm points 1–10 before PASS to Chief Architect

**Next:** Architecture QA verifies §§1–6; ask Security QA confirm on 1–10; then confirm to Chief Architect only.
