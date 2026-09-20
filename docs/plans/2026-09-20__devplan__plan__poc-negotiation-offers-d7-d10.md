# Dev Plan — PoC 1:1 Negotiation + Offers (D7–D10, P4)

**Status:** Senior Dev Planner draft (Security Dev Plan-step points woven; ready for Dev Plan QA after Security QA confirm path)  
**Date:** 2026-09-20  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner — PRIORITY PoC #6 Negotiation/Offers  
**Issue:** https://github.com/ioaikh/dealoware/issues/6  
**IDs:** **D7 D8 D9 D10 P4** · strictly **1:1**  
**DOC-FLOW:** `plans/2026-09-20__devplan__plan__poc-negotiation-offers-d7-d10.md`  
**Constraints:** No product code beyond SD instructions; build on #4/#5 (do not rewrite); #7–#8 backlog (do not invent); no MM/DC4; no Cognito/SSO/settlement/AWS spend; Product conflicts → PM → Product → CEO; cost/critical → CPM

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md` | D7–D10 + P4 domain, lifecycle, APIs, authn/authz, host, Security §5, OUT §6, AC §7 |
| Spec QA (Spec-side PASS; Security handshake cleared) | `verification/2026-09-20__spec__verification__poc-negotiation-offers-d7-d10.md` | Spec-side bind; gate unlocked after Security QA |
| Spec Security PASS | `verification/2026-09-20__security__verification__poc-negotiation-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan |
| Dev Plan Security checklist (must weave 1–10) | `verification/2026-09-20__security__verification__poc-negotiation-devplan-checklist.md` | Dev Plan-step handshake points **1–10** |
| Artifact Spec (#4) — consume only | `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md` | Artifact aggregate to attach Negotiation; do **not** rewrite |
| Participant Spec (#5) — consume only | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Authn principal on all Negotiation/Offer APIs; do **not** rewrite |
| #4 Dev Plan (consume patterns; do not merge Stories) | `plans/2026-09-11__devplan__plan__poc-artifact-d1-d5.md` | Persistence / host patterns already delivered |
| #5 Dev Plan (consume auth patterns; do not merge Stories) | `plans/2026-09-11__devplan__plan__poc-participant-d6-auth.md` | Authorization-header / secrets hygiene patterns |
| Host Spec (O10) | `specs/2026-09-10__spec__spec__poc-o10-scaffold.md` | Extend modular monolith; keep `GET /health` Auth none; local/$0; ECS Express Mode sketch |
| Issue #6 | https://github.com/ioaikh/dealoware/issues/6 | Binding AC + OUT + CPM unlock |

**Product alignment:** Negotiation around an Artifact between Participants with complementary intents; Offers Accept/Decline/Counter; Close cancels open offers; optional expiration. MVP strictly 1:1. Accept does **not** release contact (#7 / MVP OUT). Claims lock: intermediary; no settlement. Conflicts → escalate PM → Product → CEO. **#7–#8 remain backlog — do not invent.**

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend O10 modular monolith with **Negotiation + Offer** aggregates (D7–D10, P4); persist via **EF Core + SQLite** (local/$0; connection strings env/placeholders only); create Negotiation with exactly **two distinct parties** + **one Artifact** + **complementary-intent gate**; reject third party / second Artifact; require **#5 validated principal** on **all** Negotiation/Offer APIs (fail closed `401`/`403`); **party-only authz** (non-party → `404` preferred or `403`, no cross-negotiation leak); place Offer with **one open Offer per side**; Accept / Decline / Counter with illegal-transition fail-closed tests (Counter → prior `Superseded`; Accept does **not** release contact/PII); Close cancels **all** open Offers (D9) and rejects post-Close Accept/Counter; thin D10 `endsAt` → `Expired` + cancel opens; P4 offer-cycle path (place/accept/decline/counter); reuse #5 secrets/header hygiene; **no** Cognito/SSO/settlement/MM modules; **OUT** #7–#8, Strategy/AI, multi-party, contact on Accept. Build on #4/#5 — do **not** rewrite them. No product code in this plan artifact — instructions for SD only.

---

## 3. Locked decisions (Spec locks 1–13 → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 1 | D7 | 1:1 Negotiation + place Offer; complementary intents required | Steps 3–4, 7–8 |
| 2 | D8 | Accept / Decline / Counter on an open Offer | Steps 9–11 |
| 3 | D9 | Close Negotiation **cancels all open offers** | Step 12 |
| 4 | D10 | Thin Negotiation expiration (optional `endsAt`; past → Expired; cancel opens) | Step 13 |
| 5 | P4 | Participant offer-cycle start: place / accept / decline / counter path | Steps 8–11, 14 |
| 6 | Cardinality | Strictly **1:1** — exactly two Participants; one Artifact; **no** multi-party / multi-Artifact | Steps 3–4 |
| 7 | Concurrency (SA) | **One open Offer per side** (simplest maintainable; SA §1#3) | Steps 8, 11, 15 |
| 8 | Authn | Validated #5 principal required on all Negotiation/Offer APIs; fail closed `401`/`403` | Steps 5–6 |
| 9 | Authz | Only the two Negotiation parties may read/act; non-party → `404` preferred (or `403`) without leaking | Step 6 |
| 10 | Identity on Accept | Accept does **not** release contact/PII in PoC (#7 / MVP OUT) | Steps 9, 16 |
| 11 | Idempotency | Keys **optional** in PoC; **required MVP** (A14) — **not** PoC AC | Step 16 |
| 12 | Host | Extend O10; local/$0; ECS Express sketch; no AWS provision | Steps 1, 17–18 |
| 13 | OUT | Strategy; AI; contact exchange; multi-party/multi-Artifact; settlement; Cognito/SSO; #7–#8; MM/DC4; AWS spend | Steps 16–17, Explicit OUT |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 solution — Spec §4 / Host Spec O10).  
**Prerequisite:** O10 scaffold present; **#4 Artifact** and **#5 Participant auth** delivered (consume only — do **not** rewrite #4/#5 Specs or merge Stories).  
**Hold:** #7 identity-seal / #8+ remain backlog — do **not** invent.

### Step 1 — Extend O10 solution (host layout; keep health open)

- Keep **`Dealoware.Api` as the only runnable** project.
- Keep **`GET /health`** contract unchanged (**Auth: none**; no DB required for health — O10 Spec §2).
- Extend **Domain / Application / Infrastructure** as needed for Negotiation + Offer aggregates, lifecycle rules, EF Core persistence, and party authz.
- Do **not** add a second host / worker / BFF / gateway.
- Controllers not required; continue Minimal APIs in `Dealoware.Api`.
- Built-in ASP.NET Core DI only.
- Do **not** add Cognito/IdP SDK PackageReferences, settlement/payment SDKs, or MM/DC4 refs.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged (Auth none)
- [ ] No second host; no Cognito/IdP/AWS/settlement/MM provision tasks

### Step 2 — Persist Negotiation + Offer aggregates (EF Core + SQLite local)

Implement persistence per Spec §1:

**Negotiation**

| Field | Notes |
|-------|-------|
| `id` | Server-generated |
| `artifactId` | Existing Artifact (#4) — exactly one |
| `partyAParticipantId` | Initiator / first party (`#5.sub`) |
| `partyBParticipantId` | Counterparty — distinct from party A |
| `status` | `Open` \| `Closed` \| `Expired` |
| `startsAt` | Optional (thin) |
| `endsAt` | Optional — D10 expiration boundary |
| Complementary intents | Stored/checked at create as SD documents (Product examples: buy↔sell, provide↔consume, etc. — no invented closed enum) |

**Offer**

| Field | Notes |
|-------|-------|
| `id` | Server-generated |
| `negotiationId` | Parent Negotiation |
| `fromParticipantId` | Must be a Negotiation party |
| `toParticipantId` | The other party |
| `status` | `Open` \| `Accepted` \| `Declined` \| `Superseded` \| `Cancelled` |
| Payload | Thin PoC terms (e.g. proposed value amount+currency and/or free-form terms string) — **no** Strategy/AI fields; **no** contact/PII |

**Persistence locks:**

- Stack: **EF Core** + relational store (same as #4)
- Local PoC: **SQLite** OK
- Connection string: **env / placeholders only** — never commit real credentials
- Later AWS PostgreSQL shape: **not** provisioned in #6
- Wire DbContext from Api/DI; reuse #4/#5 Infrastructure patterns where present — do **not** rewrite #4/#5 domain

**Acceptance:**

- [ ] Negotiation + Offer aggregates persist and round-trip locally
- [ ] Connection string from env/placeholder only (no real secrets in `appsettings*` / source)
- [ ] No AWS DB provision; no contact/PII columns on Offer/Accept path

### Step 3 — Create Negotiation invariants (exactly two parties + one Artifact; 1:1 reject)

On `POST /negotiations` (create):

1. Require **exactly two distinct** parties (`partyAParticipantId` ≠ `partyBParticipantId`).
2. Require **exactly one** existing Artifact (`artifactId` must resolve via #4).
3. **Reject** any create/join that would add a **third Participant** or a **second Artifact** on one Negotiation (`400`).
4. Do **not** schedule multi-party / multi-Artifact endpoints (V2 / A13 OUT).

**Acceptance:**

- [ ] Create with two distinct parties + one Artifact → allowed (subject to complementary gate Step 4)
- [ ] Third party / second Artifact → `400` rejected
- [ ] Missing Artifact → `404`
- [ ] No multi-party / multi-Artifact routes added

### Step 4 — Complementary-intent gate at create

Per Spec §1 Negotiation + Locked #1 / #4:

| Item | Plan lock |
|------|-----------|
| At create | Parties’ intents for this Negotiation must be **complementary** (Product examples: buy↔sell, provide↔consume, etc.) |
| Reject | Non-complementary → `400` |
| Enum | Spec does **not** invent a closed Product intent enum beyond Product examples; **SD documents** the PoC complementarity check used |
| Do not invent | New Product intent rules beyond Spec/Product examples |

**Acceptance:**

- [ ] Complementary intents → create succeeds (`201`)
- [ ] Non-complementary → `400`
- [ ] PoC complementarity check documented by SD (no invented Product enum)

### Step 5 — Authn: #5 principal required on ALL Negotiation/Offer APIs (fail closed)

Per Spec §2 preamble + §3 + Locked #8; consume **#5** only:

| Endpoint class | Authn required |
|----------------|----------------|
| `POST /negotiations`, `GET /negotiations/{id}`, `POST /negotiations/{id}/close` | **Yes** — validated #5 principal |
| `POST /negotiations/{id}/offers` | **Yes** |
| `POST /offers/{id}/accept`, `decline`, `counter` | **Yes** |
| `GET /health` (O10) | **No** (unchanged) |

| Item | Plan lock |
|------|-----------|
| Mechanism | Reuse #5 validated principal (`Authorization` header — Bearer JWT and/or documented API-key scheme per #5) |
| Unauthenticated / invalid | Fail closed **`401`** / **`403`** — **no** anonymous negotiate/offer |
| Transport | `#5` header hygiene — **no** tokens in query strings or Negotiation/Offer bodies |
| Scope | Consume #5 — **do not** invent Cognito/SSO/password/cookie/IdP inside #6 |

**Verify tasks (required):**

- [ ] Unauthenticated create/get/close/place/accept/decline/counter → `401`/`403`
- [ ] Invalid credential → fail closed (no silent anonymous party)
- [ ] `GET /health` remains open

### Step 6 — Authz: party-only; non-party → 404 preferred (no leak)

Per Spec §2 Authz rows + Locked #9:

| Item | Plan lock |
|------|-----------|
| Allowed | Only the two Negotiation parties may read/act (start/view/offer/accept/decline/counter/close) |
| Non-party | **`404` preferred** (or `403`) — **without** leaking cross-negotiation existence or payloads |
| Offer actions | Accept/Decline/Counter: caller must be Offer `toParticipantId` (and a Negotiation party) |
| Authn ≠ authz | Valid #5 token alone is insufficient — must also be a party |

**Verify tasks (required):**

- [ ] Non-party `GET /negotiations/{id}` → `404` (preferred) without payload leak
- [ ] Non-party mutate (place/accept/decline/counter/close) → `404`/`403` without cross-negotiation leak
- [ ] Party happy paths still succeed

### Step 7 — `POST /negotiations` + `GET /negotiations/{id}` (D7 create/get)

| Item | Contract |
|------|----------|
| Body (create) | `artifactId`, `counterpartyParticipantId`, optional `startsAt`/`endsAt`, intent fields as needed for complementarity check |
| Success create | **`201`** + Negotiation representation (`status=Open`) |
| Failures create | **`400`** validation / non-complementary / not 1:1; **`401`**; **`404`** Artifact |
| Get authz | Party only |
| Success get | **`200`** + Negotiation (+ optional embedded offers list for parties) |
| Failures get | **`401`**; **`404`** missing or non-party |

**Acceptance:**

- [ ] Happy create → 201 Open Negotiation with two parties + one Artifact
- [ ] Party get → 200
- [ ] Validation / non-complementary / not 1:1 → 400
- [ ] Missing Artifact → 404; missing principal → 401; non-party get → 404 preferred

### Step 8 — `POST /negotiations/{id}/offers` — place Offer (D7 / P4); one open per side

| Item | Contract |
|------|----------|
| Preconditions | Negotiation `Open` (not expired); caller is party; caller has **no** other `Open` Offer on this Negotiation |
| Body | Thin offer payload (no contact/PII; no secrets) |
| Success | **`201`** + Offer `Open` |
| Failures | **`400`** if caller already has `Open` Offer; **`401`**; **`404`**; **`409`** if Negotiation not `Open` |

**Concurrency lock (SA cite):** **one open Offer per side** — enforce on place (and Counter Step 11).

**Acceptance:**

- [ ] First open Offer per side → 201
- [ ] Second concurrent open Offer same side → `400`
- [ ] Place on Closed/Expired Negotiation → `409`
- [ ] Payload has no contact/PII/secret fields persisted

### Step 9 — `POST /offers/{id}/accept` (D8 / P4); no contact/PII release

| Item | Contract |
|------|----------|
| Authz | Must be Offer `toParticipantId` |
| Preconditions | Negotiation `Open`; Offer `Open` |
| Success | **`200`** Offer → `Accepted`; **other** `Open` Offers on Negotiation → `Cancelled`; Negotiation **stays `Open`** unless Close called (Spec default) |
| Response | **No contact fields** / no identity reveal / no PII |
| Failures | **`401`**; **`404`**; **`409`** if Offer/Negotiation not actionable |

**Explicit:** Accept = **state change only**. Do **not** schedule contact exchange / identity-seal tasks (#7 backlog).

**Acceptance:**

- [ ] Accept happy path → Offer Accepted; other opens Cancelled; Negotiation remains Open
- [ ] Response contains **no** contact/PII fields
- [ ] Illegal Accept (wrong caller / not Open / Closed/Expired) → fail closed `404`/`409` (no silent overwrite)

### Step 10 — `POST /offers/{id}/decline` (D8 / P4)

| Item | Contract |
|------|----------|
| Authz | Must be Offer `toParticipantId` |
| Preconditions | Same as Accept |
| Success | **`200`** Offer → `Declined` |
| Failures | **`401`**; **`404`**; **`409`** |

**Acceptance:**

- [ ] Decline happy path → Declined
- [ ] Illegal Decline → fail closed `404`/`409`

### Step 11 — `POST /offers/{id}/counter` (D8 / P4); prior → Superseded

| Item | Contract |
|------|----------|
| Authz | Must be Offer `toParticipantId` |
| Preconditions | Same as Accept; caller has no other `Open` Offer (or Counter replaces caller’s prior open per one-open-per-side) |
| Body | New thin offer payload |
| Success | **`201`** (or `200`) with prior Offer → **`Superseded`** (prefer Superseded for Countered prior) + new Offer `Open` from counterparty |
| Failures | **`401`**; **`404`**; **`409`**; **`400`** if one-open-per-side violated |

**Illegal-transition fail-closed tests (required):**

- [ ] Counter on non-Open Offer / Closed or Expired Negotiation → `409` (or `404` as Spec)
- [ ] Prior Offer status becomes `Superseded` (not silent overwrite)
- [ ] One-open-per-side still holds after Counter
- [ ] Unauthorized caller → `404`/`403`

### Step 12 — `POST /negotiations/{id}/close` (D9); cancels ALL open Offers; post-Close reject

| Item | Contract |
|------|----------|
| Authz | Party only |
| Success | **`200`** Negotiation → `Closed`; **all** `Open` Offers → `Cancelled` (**D9 binding**; atomic/equivalent) |
| Failures | **`401`**; **`404`**; **`409`** if already terminal (Closed) |

**Verify tasks (required):**

- [ ] Close with multiple open Offers → all opens `Cancelled`
- [ ] Post-Close Accept / Counter / place / decline → rejected (`409` or Spec-equivalent fail closed)
- [ ] Already Closed → `409`

### Step 13 — D10 thin `endsAt` expiration → Expired + cancel opens

| Item | Contract |
|------|----------|
| Mechanism | Thin: evaluate `endsAt` on mutating reads/writes and/or a simple check before offer actions; optional lightweight sweep — Spec does **not** require a scheduler Story |
| Effect | When `endsAt` set and now ≥ `endsAt` and Negotiation still `Open` → Negotiation → `Expired`; **all** `Open` Offers → `Cancelled` (same cancel semantics as Close for opens) |
| API | Expired Negotiation rejects new place/accept/decline/counter with **`409`**; `GET` still allowed for parties |

**Verify tasks (required):**

- [ ] Past `endsAt` → status `Expired`; opens cancelled
- [ ] Accept / Counter / place / decline after expiry → fail (`409`) — no stale-client bypass
- [ ] Party `GET` still allowed on Expired

### Step 14 — P4 offer-cycle path (place / accept / decline / counter)

Deliver and self-verify an end-to-end **participant offer-cycle** path:

1. Two authenticated parties + Artifact (#4/#5 consume)
2. Create Negotiation (complementary intents)
3. Place Offer
4. Counter (prior Superseded) **or** Decline path
5. Place / Accept path (no contact release)
6. Optional Close (cancels remaining opens)

**Acceptance:**

- [ ] Documented/verifiable place → accept path
- [ ] Documented/verifiable place → decline path
- [ ] Documented/verifiable place → counter path
- [ ] No contact/PII on Accept response throughout cycle

### Step 15 — Offer state-machine integrity + concurrency gates (fail-closed tests)

Schedule illegal-transition / concurrency tests covering Spec §1 Lifecycle:

| Gate | Expected |
|------|----------|
| Place when caller already has Open Offer | `400` |
| Accept/Decline/Counter when Offer not Open | `409` |
| Accept/Decline/Counter when Negotiation Closed/Expired | `409` |
| Counter sets prior to `Superseded` | Assert status |
| Accept cancels other opens | Assert Cancelled |
| Close/Expire cancel all opens | Assert Cancelled |
| No silent overwrite of terminal Offer states | Fail closed |

**Acceptance:**

- [ ] All gates above covered by SD verify tests/notes
- [ ] One-open-per-side enforced on place + counter

### Step 16 — Explicit OUT (do **not** implement Spec §6 / #7–#8)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Strategy engine | PoC OUT |
| AI Assistant | PoC OUT |
| Contact exchange on Accept | **#7** / MVP (P7/A9) — **backlog; do not invent** |
| Identity-seal productization / #8+ | Separate Stories — **backlog; do not invent** |
| Multi-party / multi-Artifact | V2 (A13) |
| Settlement / checkout / escrow | Claims lock |
| Idempotency keys as PoC AC | Optional PoC; required MVP (A14) — not required for done |
| Cognito / SSO IdP | No spend; O9 later |
| MotorMarket / DC4 | Zero |
| AWS provision / App Runner / prod deploy | Local/$0; ECS Express sketch only |
| Rewriting #4/#5 Specs or merging Stories | Build on / consume only |

Also forbid: contact/PII fields on Accept response; Cognito/SSO modules; settlement modules; MM/DC4 packages.

### Step 17 — Secrets hygiene reuse #5 patterns; local/$0; zero MM/DC4

**Secrets (reuse #5):**

- [ ] No committed secrets / API keys / cloud credentials / real connection strings
- [ ] Connection strings + any signing material = env / placeholders only
- [ ] No tokens in **query strings** or Negotiation/Offer **bodies**
- [ ] No logging of **raw** tokens / keys / credentials
- [ ] Examples use placeholders only
- [ ] No Cognito/SSO/IdP/settlement SDK PackageReferences added by this Story

**Host / spend:**

- [ ] PoC remains **local / $0**
- [ ] README retains **Amazon ECS Express Mode** (Fargate) **sketch only** — not deployed by this Story
- [ ] **No** App Runner; **no** AWS account / resource provision instructions that create spend
- [ ] If spend ever proposed → escalate **CPM → COO → CEO**

**Zero MM/DC4:**

- [ ] Grep/search: no MotorMarket / DC4 project references, package names, shared libraries
- [ ] No MM/DC4 schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 18 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] O10 extended; Api only runnable; `GET /health` unchanged (Auth none)
- [ ] Negotiation + Offer aggregates persisted (EF Core + SQLite local; env/placeholders)
- [ ] Create: exactly two distinct parties + one Artifact; third party / second Artifact rejected
- [ ] Complementary-intent gate at create; non-complementary → 400; check documented
- [ ] #5 principal required on all Negotiation/Offer APIs; unauthenticated → 401/403 fail closed
- [ ] Party-only authz; non-party → 404 preferred (or 403) without cross-negotiation leak
- [ ] Place Offer: one open Offer per side; Negotiation Open not expired
- [ ] Accept / Decline / Counter illegal-transition fail-closed; Counter → prior Superseded
- [ ] Accept does **not** release contact/PII; other opens Cancelled on Accept
- [ ] Close cancels ALL open Offers; post-Close Accept/Counter rejected
- [ ] D10 thin endsAt → Expired + cancel opens; Accept/Counter/place/decline fail after expiry
- [ ] P4 offer-cycle path verified (place/accept/decline/counter)
- [ ] Secrets hygiene + zero MM/DC4 + local/$0 + ECS Express sketch; no Cognito/SSO/settlement
- [ ] Nothing from Step 16 / Spec §6 OUT implemented; #7–#8 not invented
- [ ] #4/#5 consumed only (not rewritten); no invent Stories; Product conflicts escalated if any

---

## 5. Spec §§1–6 + AC mapping (state-machine + concurrency called out)

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec §1 Domain + lifecycle | Negotiation/Offer model; invariants; place/Accept/Decline/Counter/Close/Expire | Steps 2–4, 8–13, 15 |
| Spec §1 **state-machine** | Offer statuses Open/Accepted/Declined/Superseded/Cancelled; Negotiation Open/Closed/Expired | Steps 9–13, 15 |
| Spec §1 **concurrency** | **One open Offer per side** (SA §1#3) | Steps 8, 11, 15 |
| Spec §2 API contracts | create/get/close + place/accept/decline/counter + expiration behavior | Steps 5–14 |
| Spec §3 Authn / authz | #5 principal; party-only; fail closed | Steps 5–6 |
| Spec §4 Host / runtime | Extend O10; EF/SQLite; local/$0; ECS sketch; secrets | Steps 1–2, 17 |
| Spec §5 Security Spec checklist 1–10 | Upstream Spec Security bind | Security table §6; all steps |
| Spec §6 Explicit OUT | Strategy/AI/contact/#7–#8/multi-party/settlement/Cognito/MM/AWS | Step 16; Explicit OUT |
| Spec §7 / Issue AC: **D7** 1:1 Negotiation + place Offer (complementary intents) | Spec §1; §2 create + place | Steps 3–4, 7–8 |
| Issue AC: **D8** Accept / Decline / Counter | Spec §2 accept/decline/counter | Steps 9–11 |
| Issue AC: **D9** Close cancels all open offers | Spec §1 Close; §2 close | Step 12 |
| Issue AC: **D10** thin expiration | Spec §1 Expire; §2 Expiration | Step 13 |
| Issue AC: **P4** offer-cycle start | Spec §2 place/accept/decline/counter | Steps 8–11, 14 |
| Issue AC: Strictly **1:1** | Locked #6; §1 invariants | Steps 3–4 |
| #4 Spec consume | Artifact exists for attach | Steps 2–3, 7 |
| #5 Spec consume | Validated principal; Authorization header | Steps 5–6, 17 |
| O10 Host Spec | Extend monolith; keep health Auth none | Steps 1, 17 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-20__security__verification__poc-negotiation-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-20__security__verification__poc-negotiation-spec-qa-confirm.md` (points 1–10 MET)  
**Spec Security binding:** Spec §5 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Authn on all Negotiation/Offer APIs** — Plan tasks require #5 principal on create/get/list (as Spec), place/accept/decline/counter/close; verify unauthenticated fail closed | Step 5 requires validated #5 principal on every Negotiation/Offer endpoint; verify unauthenticated → 401/403; health stays open; Locked #8 | Steps 5, 7–13, 18; Locked #8 |
| 2 | **Party-only authz verify** — non-party cannot view/mutate; 404 preferred (or 403) without cross-negotiation leak | Step 6 party-only authz + verify tasks for non-party get/mutate without leak; authn ≠ authz | Steps 6–7, 18; Locked #9 |
| 3 | **1:1 enforcement tasks** — reject third Participant or second Artifact on one Negotiation | Steps 3–4 create invariants reject third party / second Artifact (`400`); no multi-party routes | Steps 3–4, 7, 16; Locked #6 |
| 4 | **Complementary-intent gate** — start-Negotiation check; reject otherwise | Step 4 complementary gate at create; non-complementary → 400; SD documents PoC check (no invented Product enum) | Steps 4, 7, 18; Locked #1 |
| 5 | **Offer state-machine tasks** — place/Accept/Decline/Counter with illegal-transition fail-closed tests (incl. one-open-per-side) | Steps 8–11 implement lifecycle; Step 15 schedules illegal-transition + one-open-per-side concurrency tests; Counter → Superseded | Steps 8–11, 15, 18; Locked #7 |
| 6 | **Close cancels opens** — Close cancels all open offers + verify post-Close Accept/Counter rejected | Step 12 Close → Cancelled all opens; verify post-Close Accept/Counter/place/decline rejected | Steps 12, 15, 18; Locked #3 |
| 7 | **D10 expiration tasks** — thin expiry + verify Accept/Counter (Spec-named writes) fail after expiry | Step 13 endsAt → Expired + cancel opens; verify Accept/Counter/place/decline → 409 after expiry; GET still allowed | Steps 13, 15, 18; Locked #4 |
| 8 | **No contact/PII on Accept** — exclude contact exchange / identity reveal (#7 backlog); Accept = state change only | Step 9 Accept response forbids contact/PII; Step 16 OUT #7/#8 contact/identity-seal; payload rules exclude contact | Steps 9, 14, 16, 18; Locked #10 |
| 9 | **Host / secrets / OUT** — Local/$0; reuse #5 header/secret hygiene; no Cognito/SSO/settlement/MM modules in plan tasks | Step 17 secrets reuse #5 + local/$0 + ECS sketch + zero MM/DC4; Step 1/16 forbid Cognito/SSO/settlement/MM; Cost/critical | Steps 1, 2, 5, 16–18; Locked #12/#13; Cost/critical |
| 10 | **Handshake close** — Dev Plan QA must not PASS until Security QA confirms these points | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS | Handshake note; Done-list for Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-20__security__verification__poc-negotiation-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD. **Dev Plan QA must not PASS until Security QA confirms.**

---

## 7. Explicit OUT

Mirror Spec §6 / issue #6 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Strategy engine | PoC OUT |
| AI Assistant | PoC OUT |
| Contact exchange on Accept | **#7** / MVP (P7/A9) — **backlog; do not invent** |
| Identity-seal productization / #8+ | Separate Stories — **backlog; do not invent** |
| Multi-party / multi-Artifact | V2 (A13) |
| Settlement / checkout / escrow | Claims lock |
| Idempotency keys as PoC AC | Optional PoC; required MVP (A14) |
| Cognito / SSO IdP | No spend; O9 later |
| MotorMarket / DC4 | Zero |
| AWS provision / App Runner / prod deploy | Local/$0; ECS Express sketch only |
| Rewriting #4/#5 Specs | Build on / consume only |

---

## 8. Cost/critical

**#6 must not procure AWS / Cognito / IdP / settlement spend.** Any paid AWS provision, Cognito/SSO/IdP spend, or other spend proposal → escalate **CPM → COO → CEO**. PoC remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, Cognito user-pool, App Runner, or settlement tasks. ECS Express Mode remains **README sketch only**.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-20__devplan__plan__poc-negotiation-offers-d7-d10.md`
- [ ] Spec coverage §§1–6 + issue #6 AC (D7–D10 + P4 + 1:1) mapped to plan steps
- [ ] State-machine + concurrency (one-open-per-side) called out in §5 mapping + Steps 8/11/15
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] Builds on #4/#5 consume-only — **no rewrite**; #7–#8 backlog — **not invented**
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] No Cognito/SSO/settlement/AWS spend instructions
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock (+ CPM per ops).

---

## 10. Done-list for SD (after plan QA PASS + Chief confirm)

- [ ] Step 1: Extend O10 (Domain/Application/Infrastructure as needed; Api only runnable; keep `GET /health` Auth none)
- [ ] Step 2: Persist Negotiation + Offer aggregates via EF Core + SQLite local; connection string env/placeholders only
- [ ] Step 3: Create invariants — exactly two distinct parties + one Artifact; reject third party / second Artifact
- [ ] Step 4: Complementary-intent gate at create; non-complementary → 400; document PoC check
- [ ] Step 5: #5 principal on ALL Negotiation/Offer APIs; unauthenticated → 401/403 fail closed
- [ ] Step 6: Party-only authz; non-party → 404 preferred (or 403) without cross-negotiation leak
- [ ] Step 7: `POST /negotiations` + `GET /negotiations/{id}` per Spec contracts
- [ ] Step 8: Place Offer — one open Offer per side; Negotiation Open not expired
- [ ] Step 9: Accept — state change only; other opens Cancelled; **no** contact/PII in response
- [ ] Step 10: Decline per Spec
- [ ] Step 11: Counter — prior → Superseded; one-open-per-side; illegal transitions fail closed
- [ ] Step 12: Close cancels ALL open Offers; post-Close Accept/Counter rejected
- [ ] Step 13: D10 thin endsAt → Expired + cancel opens; Accept/Counter fail after expiry
- [ ] Step 14: P4 offer-cycle path verified (place/accept/decline/counter)
- [ ] Step 15: State-machine + concurrency fail-closed tests complete
- [ ] Step 16: Do not implement Spec §6 OUT; do not invent #7–#8
- [ ] Step 17: Secrets hygiene (#5 patterns); local/$0; ECS Express sketch; zero MM/DC4; no Cognito/SSO/settlement
- [ ] Step 18: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
