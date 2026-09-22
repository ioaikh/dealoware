# Dev Plan — MVP Stage A Account list fail-closed (#32)

**Status:** Senior Dev Planner draft (Security woven)  
**Date:** 2026-09-21  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner PRIORITY #32 ONLY  
**Issue:** https://github.com/ioaikh/dealoware/issues/32  
**DOC-FLOW:** `plans/2026-09-21__devplan__plan__mvp-stage-a-account-list-fail-closed.md`  
**Constraints:** #32 only; #31 Field ACL cross-ref separate; extend PoC #4–#7; no Stage B/C inventing; gate #24 backlog; PoC $0; no Cognito/MM/DC4; cost → CPM.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md` | Fail-closed list+get contracts; Locked #0–#8; tests; OUT; Security §5 |
| Spec QA (Spec-side PASS; Security handshake cleared) | `verification/2026-09-21__spec__verification__mvp-stage-a-account-list-fail-closed.md` | Spec-side bind |
| Spec Security PASS | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan |
| Dev Plan Security checklist (must weave 1–10) | `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-checklist.md` | Dev Plan-step handshake points **1–10** |
| SA Pick A (Stage A) | `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md` | Repository/query owner+party fail-closed lists |
| Option A parent | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` | Dual wall; Stage A list fail-closed; #18 |
| Artifact Spec (#4) — extend / harden | `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md` | Owner-scoped list-own / get; do **not** rewrite Story |
| Participant Spec (#5) — consume authn | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Validated principal; do **not** rewrite |
| Negotiation Spec (#6) — extend / harden | `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md` | Party-only get; Offer party via parent; do **not** rewrite Story |
| Identity-seal Spec (#7) — consume / leave stub | `specs/2026-09-20__spec__spec__poc-identity-seal-stub.md` | Seal stub unchanged; no contact leak |
| Sibling #31 Spec — cross-ref only | `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md` | Field projection complement; **do not implement in this plan** |
| BA note #32 | `plans/2026-09-21__ba__note__story-32-account-list-failclosed.md` | Owner vs party locked |
| Issue #32 | https://github.com/ioaikh/dealoware/issues/32 | Binding AC + OUT |
| Parent #18 | https://github.com/ioaikh/dealoware/issues/18 | Option A · Stage A |
| DOC-FLOW | `meta/DOC-FLOW.md` | Naming / path |

**Product alignment:** CEO Stage A named slice — account-scoped list/get fail-closed for negotiations / offers / artifacts. Complements #31 Field ACL (separate Story). Stage B/C HOLD. Gate #24 backlog until after Stage A delivery. Conflicts → PM → Product → CEO. Cost/critical → CPM.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: harden existing O10 modular monolith **repository / query-plane** filters so Artifact / Negotiation / Offer **list + get** are fail-closed to the authenticated Participant’s **authorized account scope** (owner for Artifacts; party A/B for Negotiations; party via parent Negotiation for Offers). Require **#5** validated principal on all those paths; unauthenticated → **401/403**; unauthorized / IDOR → **404 preferred** (or 403) with **uniform deny bodies** that leak **no** private fields / contact / FieldClass secrets. Deliver automated tests: owner/party OK, stranger deny, IDOR deny, unauth deny. **Cross-ref #31** Field ACL for field projection on allowed rows — **do not implement #31** here. Extend PoC #4–#7 patterns; keep `GET /health` open if touched. **OUT:** Stage B/C, Strategy lists, discovery, agent wall, opening gate #24, Cognito/MM/DC4, rewriting #31/#7/PoC Stories. PoC/MVP **$0**. No product code in this artifact — SD instructions only.

---

## 3. Locked decisions (Spec locks → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 0 | SA Pick A | Repository / query-plane owner/party constraints for Artifact/Negotiation/Offer list+get | Steps 1–5 |
| 1 | Artifact list/get | Fail-closed to owning Participant (`OwnerParticipantId` == principal `sub`) | Steps 2, 6 |
| 2 | Negotiation list/get | Fail-closed to party (A or B) | Steps 3, 6 |
| 3 | Offer list/get | Fail-closed to party of parent Negotiation | Steps 4, 6 |
| 4 | Unauthorized | Fail-closed no leakage — 404 preferred or 403; uniform deny; no private fields | Steps 5–7 |
| 5 | Query plane | Constraints in repository/query — not UI-only filter | Steps 1–4 |
| 6 | Authn | Validated #5 principal required; unauthenticated → 401/403 | Steps 5, 6 |
| 7 | Complement #31 | Field ACL projects fields; list isolation selects **which rows**; do not weaken #6 party rules | Step 8 |
| 8 | OUT | Strategy lists; discovery; Stage B/C; agent wall; gate #24 open; Cognito/MM/DC4 | Steps 9–10; Explicit OUT |

**Issue wording “owning account”** = authenticated Participant’s **authorized account scope** (owner for artifacts; party for negotiations/offers). Do **not** invent multi-tenant admin views.

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 modular monolith — Spec §4).  
**Prerequisite:** PoC #4 Artifact, #5 Participant auth, #6 Negotiation/Offers, #7 identity-seal stub delivered (consume / harden — do **not** rewrite those Specs or merge Stories).  
**Sibling HOLD:** #31 Field ACL — **cross-ref only**; separate plan/Story.  
**Hold:** Stage B/C; gate #24; Cognito/MM/DC4 — do **not** invent.

### Step 1 — Confirm host layout; query-plane placement (no UI-only); keep health open

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**) if any host/DI touch is required for wiring.
- Place owner/party constraints in **Infrastructure repository / query** (and Application query services as needed) — **not** controller-only / UI-only post-fetch filters.
- Extend Domain/Application/Infrastructure as needed for fail-closed list+get filters; continue Minimal APIs.
- Built-in ASP.NET Core DI only.
- Do **not** add Cognito/IdP SDK PackageReferences, vault/KMS spend modules, or MM/DC4 refs.
- Do **not** open gate #24 or invent Stage B/C Stories.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none) if touched
- [ ] Filter placement documented as repository/query-plane (not UI-only)
- [ ] No Cognito/IdP/AWS/MM provision tasks; gate #24 not opened

### Step 2 — Artifact list+get: owner-scoped fail-closed (harden #4)

Per Spec §1 Artifact + Locked #1; SA Pick A §3b; align PoC #4 list-own / get owner-scope:

| Item | Plan lock |
|------|-----------|
| Authorized | `OwnerParticipantId` == caller `#5.sub` |
| List | Query returns **only** Artifacts owned by principal — constraint in repository/query |
| Get-by-id | Owner OK → `200`; non-owner / missing → **404 preferred** (or 403); **no** secret-bearing body |
| IDOR | Guess/swap Artifact id of another owner → fail-closed deny |
| Authn | Validated #5 principal required; unauth → 401/403 |

**Harden (do not rewrite #4 Story):** Ensure any existing list-own / get paths enforce owner filter in the **query plane** (not only in-memory after fetch-all). Add/fix filters where PoC gaps remain.

**Endpoints in scope (existing #4 surface):** `GET /artifacts` (list-own), `GET /artifacts/{id}` — no new public/global/search/discovery list.

**Acceptance:**

- [ ] Owner list returns only own Artifacts
- [ ] Owner get → 200
- [ ] Stranger / wrong principal get → 404 preferred (or 403); no payload leak
- [ ] Unauthenticated list/get → 401/403; no private fields in body
- [ ] No UI-only filter; repository/query constraint present

### Step 3 — Negotiation list+get: party-scoped fail-closed (harden #6)

Per Spec §1 Negotiation + Locked #2; SA Pick A; harden PoC #6 party-only get:

| Item | Plan lock |
|------|-----------|
| Authorized | Caller is **party A or B** on that Negotiation |
| List | Query returns **only** Negotiations where principal is party A or B — repository/query constraint |
| Get-by-id | Party OK → `200`; non-party / missing → **404 preferred** (or 403); no leak of other accounts’ negotiations |
| IDOR | Cross-tenant get-by-id → fail-closed |
| Authn | #5 principal required |

**List surface:** Ensure a party-scoped list exists or is hardened (e.g. `GET /negotiations` list-own/party). If PoC only had get-by-id, add **party-scoped list** query constrained in repository — do **not** invent discovery/search product. Do **not** invent multi-tenant admin list.

**Acceptance:**

- [ ] Party list returns only Negotiations where caller is A or B
- [ ] Party get → 200
- [ ] Stranger / non-party get → 404 preferred (or 403); no cross-account leak
- [ ] Unauthenticated → 401/403
- [ ] Query-plane party filter (not UI-only)

### Step 4 — Offer list+get: party via parent Negotiation (harden #6)

Per Spec §1 Offer + Locked #3:

| Item | Plan lock |
|------|-----------|
| Authorized | Caller is **party to the parent Negotiation** (PoC party rule) |
| List | Query returns only Offers whose parent Negotiation includes the principal as party — repository/query constraint (join/filter on parent; not “all offers then filter in UI”) |
| Get-by-id | Party-to-parent OK → `200`; unauthorized → **404 preferred** (or 403); no cross-account offer leak |
| IDOR | Offer id belonging to another negotiation’s parties → fail-closed |
| Authn | #5 principal required |

**Acceptance:**

- [ ] Party list returns only Offers authorized via parent Negotiation party membership
- [ ] Party get → 200
- [ ] Stranger / non-party get → 404 preferred (or 403); no leak
- [ ] Unauthenticated → 401/403
- [ ] Query-plane parent-party filter present

### Step 5 — Authn fail-closed (#5 required); deny hygiene

Per Spec §1 Authn + Deny hygiene + Locked #4/#6:

| Item | Plan lock |
|------|-----------|
| Authn | All Artifact / Negotiation / Offer **list+get** paths require validated **#5** principal (`Authorization` header per #5) |
| Unauthenticated / invalid | Fail closed **`401`/`403`** — bodies omit private fields / contact/PII / FieldClass secret values |
| Unauthorized (authn OK, scope fail) | **404 preferred** (PoC consistency) or **403**; **uniform deny**; no existence leak via secret-bearing bodies |
| Empty lists | OK for authorized empty scope; must **not** smuggle other tenants’ data |
| Health | `GET /health` remains Auth none |
| Do not invent | Cognito/SSO/password/cookie/IdP productization inside #32 |

**Verify tasks (required):**

- [ ] Unauthenticated Artifact/Negotiation/Offer list+get → 401/403; no private fields
- [ ] Invalid credential → fail closed
- [ ] Deny bodies contain no contact/PII / FieldClass secrets / other Participants’ private fields
- [ ] Empty authorized list does not include foreign rows
- [ ] `GET /health` remains open

### Step 6 — Automated tests (binding Spec §2)

Schedule automated tests covering **all three** resource types:

| Case | Expected |
|------|----------|
| Owner/party OK — Artifact | Owner list+get succeed |
| Owner/party OK — Negotiation | Party list+get succeed |
| Owner/party OK — Offer | Party-via-parent list+get succeed |
| Stranger deny | Authenticated non-owner/non-party list excludes foreign rows; get → 404 preferred / 403 |
| Cross-tenant IDOR deny | Get-by-id with another tenant’s resource id → fail-closed; no payload leak |
| Unauthenticated deny | No token / invalid → 401/403; no private leak |

**Acceptance:**

- [ ] Test suite (or equivalent automated evidence) covers owner/party OK, stranger, IDOR, unauth for Artifact + Negotiation + Offer
- [ ] Failures assert status + **absence** of private/secret fields in deny bodies
- [ ] Tests exercise repository/query behavior (not mocked-away authz that always returns empty)

### Step 7 — Deny-body / empty-list hygiene verify (Security point 5)

Explicit verify pass:

- [ ] Error responses for 401/403/404 on list+get omit private fields, contact/PII, tokens, FieldClass secret values
- [ ] Empty list for authorized principal is `[]` (or equivalent) with **no** other-tenant rows
- [ ] Prefer 404 for non-authorized get-by-id (consistent with PoC #4/#6)
- [ ] Logging does not dump raw tokens / private payloads on deny paths

### Step 8 — Complement #31 Field ACL (cross-ref only — do not implement #31)

| Item | Plan lock |
|------|-----------|
| #32 responsibility | **Which rows** appear on list/get (owner/party query-plane isolation) |
| #31 responsibility | **Which fields** project on allowed rows (`IFieldPolicy` / FieldClass) — **separate Story/plan** |
| Do not | Replace party/owner list rules with Field ACL alone |
| Do not | Implement FieldClass registry, `IFieldPolicy`, or DTO field scrubbers in this #32 plan |
| Do not | Weaken #6 1:1 party-only rules |
| Do not | Merge #31 and #32 Stories or rewrite #31 Spec |
| Cite | Sibling Spec `specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md`; issue https://github.com/ioaikh/dealoware/issues/31 |

**Acceptance:**

- [ ] Plan/handoff notes cross-ref #31 for field projection
- [ ] No #31 Field ACL implementation tasks executed under this Story
- [ ] #6 party rules remain intact

### Step 9 — Explicit OUT (do **not** implement Spec §6)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Strategy list ACL / Strategy body | Stage B HOLD |
| Discovery / search product | Out |
| Agent / tool hard wall | Stage C HOLD |
| ContactEmail ShareOutbound-after-Accept | Stage B HOLD |
| Opening gate **#24** / SA-REV-MVP-A | Backlog until **after** Stage A delivery — do **not** open now |
| Cognito / SSO / IdP / vault / KMS spend | Out |
| MotorMarket / DC4 | Zero |
| Expanding / rewriting PoC **#7** seal stub | Unchanged |
| Rewriting #31 / #4 / #5 / #6 Specs or merging Stories | Cross-ref / extend patterns only |
| Multi-tenant admin views | Do not invent |
| Stage B/C inventing | Forbidden |

### Step 10 — Secrets / cost / zero MM-DC4; local $0

**Secrets (reuse #5 patterns):**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings
- [ ] Connection strings + signing material = env / placeholders only
- [ ] No tokens in query strings or list/get bodies
- [ ] No logging of raw tokens / keys / credentials
- [ ] No Cognito/SSO/IdP/vault SDK PackageReferences added by this Story

**Host / spend:**

- [ ] Remains **local / $0**
- [ ] ECS Express Mode **sketch only** if README already mentions — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource provision that creates spend
- [ ] If spend ever proposed → escalate **CPM → COO → CEO**

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, packages, shared libs, schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 11 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] Query-plane owner/party filters for Artifact / Negotiation / Offer list+get (not UI-only)
- [ ] Artifact owner-scoped; Negotiation party-scoped; Offer party-via-parent
- [ ] #5 principal required; unauthenticated → 401/403 fail closed
- [ ] Unauthorized → 404 preferred / 403; uniform deny; no private leak
- [ ] Automated tests: owner/party OK, stranger deny, IDOR deny, unauth deny — all three resources
- [ ] Deny-body / empty-list hygiene verified
- [ ] #31 cross-ref only — Field ACL **not** implemented in this Story
- [ ] #6 party rules not weakened; #7 stub unchanged
- [ ] `GET /health` still Auth none if host touched
- [ ] Nothing from Step 9 / Spec §6 OUT implemented; Stage B/C not invented; gate #24 not opened
- [ ] Secrets hygiene + zero MM/DC4 + local/$0; no Cognito
- [ ] #4–#7 consumed/hardened only (not rewritten); no invent Stories; Product conflicts escalated if any
- [ ] No product code left undocumented; handoff notes cite Spec + this plan

---

## 5. Spec + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec Locked #0 / SA Pick A | Repository/query-plane fail-closed | Steps 1–5 |
| Spec §1 Artifact + Locked #1 | Owner-scoped list+get | Step 2 |
| Spec §1 Negotiation + Locked #2 | Party-scoped list+get | Step 3 |
| Spec §1 Offer + Locked #3 | Party via parent Negotiation | Step 4 |
| Spec §1 Authn + Deny hygiene + Locked #4/#6 | #5 principal; 401/403; 404 preferred; no private leak | Steps 5, 7 |
| Spec §2 Tests | Owner/party OK; stranger; IDOR; unauth | Step 6 |
| Spec §3 Cross-refs | Complement #31; keep #6/#7 | Step 8 |
| Spec §4 Host / cost | Extend O10; local/$0; ECS sketch | Steps 1, 10 |
| Spec §5 Security Spec 1–10 | Upstream Spec Security bind | §6 Security table |
| Spec §6 Explicit OUT | Stage B/C; #24; Cognito; MM/DC4; #7 | Steps 9–10; Explicit OUT |
| Spec §7 / Issue AC: Negotiation list/get fail-closed | Party = authorized scope | Step 3 |
| Issue AC: Offer list/get fail-closed | Party via parent | Step 4 |
| Issue AC: Artifact list/get fail-closed | Owner-scoped query plane | Step 2 |
| Issue AC: Unauthorized fail-closed no leakage | Deny hygiene | Steps 5, 7 |
| #4 Spec extend | Owner list-own / get harden | Step 2 |
| #5 Spec consume | Validated principal | Step 5 |
| #6 Spec extend | Party get + Offer party rule harden | Steps 3–4 |
| #7 Spec leave stub | Unchanged; no contact leak | Steps 7–9 |
| #31 Spec cross-ref only | Field projection separate | Step 8 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md` (points 1–10 MET)  
**Spec Security binding:** Spec §5 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Authn fail-closed tasks** — Plan requires #5 principal on Neg/Offer/Artifact list+get; verify unauthenticated → 401/403 with no private leak | Step 5 requires validated #5 principal on all list+get paths; verify unauth → 401/403; health stays open; Locked #6 | Steps 5, 6, 11; Locked #6 |
| 2 | **Negotiation list/get isolation tasks** — Plan schedules owner/party filters + verify unauthorized fail-closed (404 preferred) without cross-account leak | Step 3 party-scoped repository/query list+get; stranger/IDOR → 404 preferred; Locked #2 | Steps 3, 6, 11; Locked #2 |
| 3 | **Offer list/get isolation tasks** — Same for offers (party via parent negotiation) | Step 4 party-via-parent query-plane filters; IDOR/stranger deny; Locked #3 | Steps 4, 6, 11; Locked #3 |
| 4 | **Artifact list/get isolation tasks** — Same for artifacts (owner-scoped; align #4) | Step 2 owner-scoped harden of #4 list-own/get; IDOR deny; Locked #1 | Steps 2, 6, 11; Locked #1 |
| 5 | **Deny-body / empty-list hygiene** — Plan includes verify: error bodies and empty lists do not leak other Participants’ private fields | Step 7 dedicated hygiene verify; Step 5 deny rules; Locked #4 | Steps 5, 7, 11; Locked #4 |
| 6 | **Complement #31 Field ACL** — Plan cross-refs #31 for field projection; does not replace party/owner list rules with Field ACL alone | Step 8 cross-ref only; no #31 impl; do not weaken #6 party rules; Locked #7 | Step 8; Locked #7; Explicit OUT |
| 7 | **No Stage B/C inventing** — No Strategy list ACL, agent hard-wall, Cognito, or MM/DC4 tasks | Step 9 OUT table; Step 10 forbid Cognito/MM; Locked #8 | Steps 9–10; Explicit OUT; Locked #8 |
| 8 | **Cross-story non-merge** — Keep separate from #31 plan and PoC Stories except consume patterns | Header Constraints; Steps 2–4 extend #4/#6; Step 8 #31 cross-ref; #7 unchanged; no Story merge | Steps 2–4, 8–9, 11; Sources |
| 9 | **Cost / spend** — PoC **$0**; no IdP/vault provision tasks | Step 10 local/$0; no IdP/vault; Cost/critical → CPM | Step 10; Cost/critical; Locked #8 |
| 10 | **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS | Handshake note; Done-list Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Security QA confirms.**

---

## 7. Explicit OUT

Mirror Spec §6 / issue #32 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Strategy list ACL / Strategy body | **HOLD Stage B** |
| Discovery / search product | Out |
| Agent / tool hard wall | **HOLD Stage C** |
| ContactEmail ShareOutbound-after-Accept | **HOLD Stage B** |
| Opening gate **#24** / SA-REV-MVP-A | Backlog until after Stage A delivery — **do not open now** |
| Cognito / SSO / IdP / vault / KMS spend | Out |
| MotorMarket / DC4 | Zero |
| Expanding PoC **#7** identity-seal stub | Unchanged |
| Implementing **#31** Field ACL in this Story | Cross-ref only — separate plan |
| Rewriting #4 / #5 / #6 / #7 Specs | Extend / consume patterns only |
| Multi-tenant admin views | Do not invent |
| Stage B/C inventing | Forbidden |

---

## 8. Cost/critical

**#32 must not procure AWS / Cognito / IdP / vault spend.** Any paid AWS provision, Cognito/SSO/IdP spend, vault/KMS, or other spend proposal → escalate **CPM → COO → CEO**. Remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, or vault tasks. ECS Express Mode remains **README sketch only** if present. Gate **#24** stays backlog — do not open.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-21__devplan__plan__mvp-stage-a-account-list-fail-closed.md`
- [ ] Spec coverage §§1–6 + issue #32 AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] #32 ONLY — #31 Field ACL **cross-ref only** (not implemented); Stage B/C **not** invented; gate #24 backlog
- [ ] Extends PoC #4–#7 (consume/harden) — **no rewrite** / Story merge
- [ ] Fail-closed list+get for Artifact (owner) / Negotiation (party) / Offer (party via parent) + automated tests scheduled
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] No Cognito/SSO/IdP/vault/AWS spend instructions
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock (+ CPM per ops). Parent may start #31 separately — this plan does not draft #31.

---

## 10. Done-list for SD (after plan QA PASS + Chief confirm)

- [ ] Step 1: Confirm host; query-plane placement; keep `GET /health` Auth none if touched
- [ ] Step 2: Artifact list+get owner-scoped fail-closed (harden #4) in repository/query
- [ ] Step 3: Negotiation list+get party-scoped fail-closed (harden #6) in repository/query
- [ ] Step 4: Offer list+get party-via-parent fail-closed in repository/query
- [ ] Step 5: #5 principal on all list+get; unauth → 401/403; deny hygiene
- [ ] Step 6: Automated tests — owner/party OK, stranger deny, IDOR deny, unauth deny (all three resources)
- [ ] Step 7: Deny-body / empty-list hygiene verify
- [ ] Step 8: Cross-ref #31 only — do **not** implement Field ACL
- [ ] Step 9: Do not implement Spec §6 OUT; do not invent Stage B/C; do not open #24
- [ ] Step 10: Secrets hygiene; local/$0; zero MM/DC4; no Cognito
- [ ] Step 11: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
