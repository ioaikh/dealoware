# Dev Plan — PoC Artifact core model D1–D5 (create / get / list-own)

**Status:** Senior Dev Planner draft (Security Dev Plan-step points woven; ready for Dev Plan QA after Security QA confirm path)  
**Date:** 2026-09-11  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner — PRIORITY PoC #4 Artifact D1–D5  
**Issue:** https://github.com/ioaikh/dealoware/issues/4  
**IDs:** D1–D5 · Option A  
**DOC-FLOW:** `plans/2026-09-11__devplan__plan__poc-artifact-d1-d5.md`  
**Constraints:** No product code beyond SD instructions; no MM/DC4; no AWS spend; no invent Stories; **#5 is parallel** — do not invent auth productization inside #4; Product conflicts → PM → Product → CEO; cost/critical → CPM

---

## 1. Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md` | D1–D5 model, Option A API, owner-scope, interim/#5 principal, persistence, Security §4, OUT §5, AC §6 |
| Spec QA PASS | `verification/2026-09-11__spec__verification__poc-artifact-d1-d5.md` | Spec gate clear; Security handshake already PASS |
| Spec Security PASS | `verification/2026-09-11__security__verification__poc-artifact-spec-qa-confirm.md` | Spec-step points 1–10 MET — binding unlock for Dev Plan |
| Dev Plan Security checklist | `verification/2026-09-11__security__verification__poc-artifact-devplan-checklist.md` | Dev Plan-step handshake points **1–10** (must weave) |
| Host Spec (O10) | `specs/2026-09-10__spec__spec__poc-o10-scaffold.md` | Extend modular monolith; keep `GET /health`; local/$0; ECS Express Mode sketch |
| Product brief | `product/PRODUCT-BRIEF.md` | Core model Artifact (Subject / Intent / Value / Location / Time); Participant capability #1 CRUD own Artifacts — PoC = create/get/list-own |
| Option A note | `plans/2026-09-10__ba__note__story-4-artifact-api-depth.md` | Product lock: create + get + list-own; Update/Delete → MVP P1 |
| #5 Spec (cross-ref only) | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Parallel Story — consume principal / `sub` when wired; do **not** invent #5 inside #4 |
| Issue #4 | https://github.com/ioaikh/dealoware/issues/4 | Binding AC + OUT + Option A |

**Product alignment:** Core model Artifact (D1–D5). Participant capability #1 CRUD own Artifacts — PoC proves **create / get / list-own** only; full CRUD = **P1 @ MVP**. Claims lock unchanged (intermediary; no settlement). Conflicts → escalate PM → Product → CEO.

---

## 2. Restated understanding (short)

Executable plan for **SD only**: extend O10 modular monolith with Artifact aggregate **D1–D5** + `ownerParticipantId`; persist via **EF Core + SQLite** (local/$0; connection strings env/placeholders only); implement Option A APIs — `POST /artifacts`, `GET /artifacts/{id}` (owner-scoped; 404 preferred for non-owned/missing), `GET /artifacts` (list-own only); interim principal `X-PoC-Owner-Id` (or Spec-equivalent) until #5 JWT/`sub` is consumable; keep owner **authz** even when #5 **authn** is wired; enforce D3 currency uniqueness + Spec §4 input bounds; secrets hygiene; zero MM/DC4; no AWS provision. **OUT** as Spec §5. No product code in this plan artifact — instructions for SD only. **#5 remains a separate parallel Story** — consume/cross-ref only.

---

## 3. Locked decisions (Spec locks 1–10 → plan steps)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 1 | D1 Subject | Entities: name, description, properties (name/type/value), facts (strings); collection of entities OK | Steps 2–3, 6 |
| 2 | D2 Intent | e.g. buy, sell, rent, exchange, provide, consume, … | Steps 2–3, 6 |
| 3 | D3 Value | amount + currency; 0..n; **≤1 clear value per currency** (case-normalize) | Steps 2–3, 6, 12 |
| 4 | D4 Location | 0..n | Steps 2–3, 6 |
| 5 | D5 Time periods | start/end; 0..n | Steps 2–3, 6 |
| 6 | API Option A | **create + get + list-own** only; Update/Delete = **OUT** (MVP P1) | Steps 6–8, 11, 13 |
| 7 | Persistence | Required; owner-scoped list-own; EF Core + SQLite local | Steps 2, 5 |
| 8 | Owner / #5 | Store `ownerParticipantId`; interim `X-PoC-Owner-Id`; prefer #5 JWT/`sub` when #5 SD available; do **not** invent auth productization in #4 | Steps 4–5, 9 |
| 9 | Host | Extend O10; local/$0; ECS Express Mode callout only; no AWS provision; keep `GET /health` | Steps 1, 10 |
| 10 | OUT | Update/Delete; saved search/monitoring; Strategy; AI; discovery/search; Negotiation (#6); identity-seal (#7); inventing #5; MM/DC4; settlement | Steps 11, 13 |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (extend existing O10 solution — Spec §3 / Host Spec O10).  
**Prerequisite:** O10 scaffold present (`Dealoware.sln`, `src/Dealoware.Api` only runnable, Domain / Application / Infrastructure as delivered by #3).

### Step 1 — Extend O10 solution (host layout)

- Keep **`Dealoware.Api` as the only runnable** project; keep **`GET /health`** contract unchanged (Auth: none; no DB required for health).
- Extend **Domain / Application / Infrastructure** as needed for Artifact aggregate, validation, and EF Core persistence (fill previously empty placeholders where Spec requires).
- Do **not** add a second host / worker / BFF / gateway.
- Controllers not required; continue Minimal APIs in `Dealoware.Api`.
- Built-in ASP.NET Core DI only.

**Acceptance:**

- [ ] Solution still builds; `Dealoware.Api` only runnable
- [ ] `GET /health` → `200` + `{"status":"ok"}` unchanged
- [ ] No second host; no App Runner / AWS provision tasks

### Step 2 — Persist Artifact aggregate D1–D5 + `ownerParticipantId`

Implement persistence per Spec §1:

| Field | Notes |
|-------|-------|
| `id` | Server-generated stable Artifact id |
| `ownerParticipantId` | Set on create from principal; used for get/list-own authz |
| Subject (D1) | 1..n entities: `name`, `description`, `properties` `{name,type,value}`, `facts` (strings) |
| Intent (D2) | String (open set; examples per Spec) |
| Values (D3) | 0..n `{ amount, currency }`; **≤1 per currency** |
| Locations (D4) | 0..n location descriptor strings |
| TimePeriods (D5) | 0..n `{ start, end }` |

**Persistence locks:**

- Stack: **EF Core** + relational store
- Local PoC: **SQLite** OK
- Connection string: **env / placeholders only** — never commit real credentials
- Later AWS PostgreSQL shape: **not** provisioned in #4
- Implement entities/mappings in Domain + Infrastructure as SD structures; wire DbContext from Api/DI

**Acceptance:**

- [ ] Artifact aggregate with D1–D5 + `ownerParticipantId` persists and round-trips locally
- [ ] Connection string from env/placeholder only (no real secrets in `appsettings*` / source)
- [ ] No AWS DB provision

### Step 3 — Domain validation (D3 uniqueness + Spec §4 input bounds)

On create (and any validation path used by create):

1. **D3 currency uniqueness:** reject payloads with more than one Value sharing the same `currency` key; **case-normalize** currency string for the uniqueness check (as Spec/SD documents).
2. **Input bounds** (Spec §4 — abuse limits; not new Product Stories):

| Limit | Max (PoC) |
|-------|-----------|
| Entities per Subject | 50 |
| Properties per entity | 50 |
| Facts per entity | 100 |
| Fact / name / description / location string length | 4 KiB each |
| Property name/type/value string length | 1 KiB each |
| Values (D3) | 20 |
| Locations (D4) | 50 |
| Time periods (D5) | 50 |

3. **Unexpected body fields:** reject or ignore per SD — must **not** accept raw token/secret smuggling fields as first-class Artifact columns.
4. Subject must support at least one entity for a meaningful Subject (Spec §1 cardinality 1..n entities).

**Acceptance:**

- [ ] Duplicate currency (case-insensitive) → `400`
- [ ] Over-bound payloads → `400`
- [ ] Unexpected secret-smuggling fields not persisted as Artifact columns

### Step 4 — Interim principal (`X-PoC-Owner-Id`) — authz bridge only

Per Spec §2 Interim / #5 principal:

| Item | Plan lock |
|------|-----------|
| Interim | Request header **`X-PoC-Owner-Id`** (string Participant id) **or** Spec-equivalent PoC placeholder resolving to the same owner id |
| Create | Resolve header → set `ownerParticipantId` |
| Get / list-own | Authorize principal vs `ownerParticipantId` |
| Missing principal | **`401`** |
| **Do NOT invent in #4** | Password UX, SSO, OIDC IdP, Cognito, cookie sessions, social login, or other #5 productization |

**Acceptance:**

- [ ] Create/get/list-own require interim principal; missing → `401`
- [ ] Create sets `ownerParticipantId` from resolved principal
- [ ] No password/SSO/OIDC/Cognito implementation scheduled in this Story

### Step 5 — Prefer #5 JWT/`sub` when #5 SD available (consume only)

**Cross-ref only** — `#5` Spec: `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md`.

| Item | Plan lock |
|------|-----------|
| When #5 SD available | Prefer validated **API key / JWT** principal from #5; map OIDC-shaped **`sub`** (or equivalent) → `ownerParticipantId` |
| Until then | Keep interim `X-PoC-Owner-Id` bridge (Step 4) |
| Authn ≠ authz | Even when #5 authn is wired, **keep owner authz checks** on Artifact get/list-own (valid token alone is insufficient — Spec Locked #8 / #5 Spec §3) |
| Scope | Consume principal only — **do not** expand #4 into auth productization |

**Acceptance:**

- [ ] Plan/SD documents consume path for `#5.sub` → `ownerParticipantId` without implementing #5 inside #4
- [ ] Owner-scope authz remains on Artifact handlers regardless of authn mechanism
- [ ] Interim bridge remains until #5 is wired

### Step 6 — `POST /artifacts` (create)

| Item | Contract |
|------|----------|
| Authz | Interim principal (or #5 principal when wired) required; sets `ownerParticipantId` |
| Body | Subject entities (D1), Intent (D2), Values (D3), Locations (D4), TimePeriods (D5) — fields per Spec §1 only |
| Success | **`201`** + Artifact representation including `id`, `ownerParticipantId`, D1–D5 |
| Failures | **`400`** validation (incl. D3 currency uniqueness, Spec §4 input bounds); **`401`** if principal missing |

**Acceptance:**

- [ ] Happy path → 201 + Artifact with owner set
- [ ] Validation failures → 400
- [ ] Missing principal → 401

### Step 7 — `GET /artifacts/{id}` (get, owner-scoped)

| Item | Contract |
|------|----------|
| Authz | Principal required; **owner-scoped** |
| Success | **`200`** + Artifact if `ownerParticipantId` matches caller |
| Non-owned or missing | **`404` preferred** (do not leak existence of other owners’ Artifacts); `403` only if SD documents why — must **not** return other owners’ payloads |
| Failures | **`401`** if principal missing |

**Acceptance:**

- [ ] Owner get → 200
- [ ] Non-owned / missing → 404 (preferred) without cross-owner payload leak
- [ ] Missing principal → 401

### Step 8 — `GET /artifacts` (list-own only)

| Item | Contract |
|------|----------|
| Authz | Principal required |
| Behavior | Returns **only** Artifacts where `ownerParticipantId` == caller |
| Success | **`200`** + array (possibly empty) |
| Failures | **`401`** if principal missing |
| **Forbidden** | Global/public list, cross-owner list, search, discovery endpoints |

**Acceptance:**

- [ ] List returns only caller-owned Artifacts (empty array OK)
- [ ] Missing principal → 401
- [ ] No public/global/cross-owner list/search/discovery routes added

### Step 9 — Secrets hygiene gate

Before done:

- [ ] No committed secrets / API keys / cloud credentials / real connection strings in `appsettings*`, README, Dockerfile, launchSettings, source, comments
- [ ] Connection strings = env / placeholders only
- [ ] No tokens in **query strings** or **Artifact request/response bodies** as secret carriers
- [ ] No logging of **raw** keys / tokens / credentials
- [ ] Examples use placeholders only
- [ ] Artifact fields = domain data, **not** a secrets store (no requirement to persist API keys/passwords/cloud creds in Subject/Facts)

### Step 10 — Zero MM/DC4 verify

- [ ] Grep/search: no MotorMarket / DC4 project references, package names, shared libraries
- [ ] No MM/DC4 schemas, feeds, SFTP, shared DB, or config keys
- [ ] Document verify result in SD handoff notes

### Step 11 — README note (local/$0; ECS Express retained)

Update/extend README (do not remove O10 local-run steps):

- PoC remains **local / $0**
- Retain **Amazon ECS Express Mode** (Fargate) **sketch only** — not deployed by this Story
- **No** App Runner
- **No** AWS account / resource provision instructions that create spend
- If spend ever proposed → escalate **CPM → COO → CEO**

**Acceptance:**

- [ ] README states local/$0; ECS Express sketch; no App Runner; no AWS provision tasks for #4

### Step 12 — Explicit OUT (do **not** implement Spec §5)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Artifact Update / Delete / full CRUD productization | MVP **P1** |
| SA Option B stretch | Not required for PoC done |
| Saved search / market monitoring | Issue OUT |
| Discovery / instant search | MVP+ |
| Strategy / AI Assistant | PoC OUT |
| Negotiation (#6) / Offers | Later Story |
| Identity-seal (#7) | Later Story |
| Inventing #5 auth productization inside #4 (password/SSO/OIDC IdP) | **#5 is separate parallel Spec** — consume principal; interim until wired |
| Multi-party / multi-Artifact | V2 |
| Settlement / checkout / escrow | Claims lock |
| MotorMarket / DC4 | Zero deps |
| AWS provision / App Runner / prod deploy | Local/$0; ECS Express sketch only |

Also forbid: `PUT`/`PATCH`/`DELETE` on Artifacts; bulk mutate; search; saved search; Negotiation attach endpoints (#6).

### Step 13 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] O10 extended; Api only runnable; `GET /health` unchanged
- [ ] Artifact D1–D5 + `ownerParticipantId` persisted (EF Core + SQLite local; env/placeholders)
- [ ] D3 currency uniqueness (case-normalize) + Spec §4 input bounds enforced
- [ ] Unexpected secret-smuggling fields rejected/ignored (not first-class columns)
- [ ] Interim `X-PoC-Owner-Id` (or Spec-equivalent) resolves owner on create; 401 if missing
- [ ] Prefer/#5 consume path documented; owner authz kept even if #5 authn wired (authn ≠ authz)
- [ ] `POST /artifacts` → 201 + Artifact; 400 validation; 401 missing principal
- [ ] `GET /artifacts/{id}` owner-scoped; 404 preferred for non-owned/missing; 401 missing principal
- [ ] `GET /artifacts` list-own only; 200 array (possibly empty); 401 missing principal; no public/global/cross-owner list/search/discovery
- [ ] Secrets hygiene gate passed (Step 9)
- [ ] Zero MM/DC4 gate passed (Step 10)
- [ ] README local/$0 + ECS Express sketch; no App Runner; no AWS provision
- [ ] Nothing from Step 12 / Spec §5 OUT implemented
- [ ] No invent Stories; Product conflicts escalated if any

---

## 5. Spec §§1–6 + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec §1 | Domain model D1–D5 + persistence + D3 uniqueness | Steps 2–3 |
| Spec §2 | API Option A + interim/#5 principal + create/get/list-own | Steps 4–8 |
| Spec §3 | Host / runtime (extend O10; local/$0; ECS sketch; keep health) | Steps 1, 10–11 |
| Spec §4 | Security Spec checklist binding + input bounds | Steps 3, 9–10, 12–13; Security table §6 |
| Spec §5 | Explicit OUT | Step 12 |
| Spec §6 | Acceptance mapping (issue #4 AC) | This table + Steps 2–8 |
| Issue AC: D1 Subject (entities, properties, facts; collection OK) | Spec §1 D1 | Steps 2–3, 6 |
| Issue AC: D2 Intent | Spec §1 D2 | Steps 2–3, 6 |
| Issue AC: D3 Value (0..n; ≤1 per currency) | Spec §1 D3 + uniqueness | Steps 2–3, 6, 13 |
| Issue AC: D4 Location 0..n | Spec §1 D4 | Steps 2–3, 6 |
| Issue AC: D5 Time periods start/end 0..n | Spec §1 D5 | Steps 2–3, 6 |
| Issue AC: create / get / list-own persisted owner-scoped | Spec §2; §1 ownerParticipantId | Steps 2, 4–8 |
| Issue AC: Update/Delete out of PoC | Locked #6; Spec §5 OUT | Steps 11–13 |
| Option A note | create + get + list-own; Update/Delete → MVP P1 | Steps 6–8, 12 |
| #5 Spec cross-ref | Consume `sub`/principal; authn ≠ authz; keep Specs separate | Steps 4–5 |

---

## 6. Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-11__security__verification__poc-artifact-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-11__security__verification__poc-artifact-spec-qa-confirm.md` (points 1–10 MET)  
**Spec Security binding:** Spec §4 points 1–10

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **API surface tasks** — schedule only **create**, **get**, **list-own** (no public list/search/discovery; no Update/Delete tasks for PoC done) | Steps 6–8 implement only POST create, GET by id, GET list-own. Step 12 OUT forbids Update/Delete, search, discovery. Locked #6. | Steps 6–8, 12–13; Locked #6; Explicit OUT |
| 2 | **Owner-scope verify gates** — list-own returns only caller-owned; get non-owned/missing → **404** (preferred) or 403 without cross-owner leak | Step 7: 404 preferred for non-owned/missing; Step 8: list-own filter by `ownerParticipantId`; Step 13 self-verify both gates. | Steps 7–8, 13; Locked #8 |
| 3 | **Principal bridge / #5 consume** — interim principal **and** task to prefer #5 JWT/`sub` when #5 SD available — without expanding #4 into auth productization | Step 4: interim `X-PoC-Owner-Id`; Step 5: prefer #5 JWT/`sub` consume-only cross-ref; explicitly forbid password/SSO/OIDC/Cognito in #4. | Steps 4–5, 12; Locked #8; Sources #5 |
| 4 | **Authn vs authz** — keep owner authz checks in Artifact tasks even when #5 authn is wired (valid token ≠ owns Artifact) | Step 5 and Steps 7–8 require owner-scope checks independent of authn mechanism; cite #5 Spec §3 authz boundary. | Steps 5, 7–8, 13 |
| 5 | **Persistence local/$0** — local EF/SQLite (or Spec-locked store) only; connection strings via env/placeholders; **no** AWS DB provision; ECS Express Mode remains sketch | Step 2: EF Core + SQLite local; env/placeholders; Step 11: ECS sketch only; Cost/critical forbids AWS provision. | Steps 2, 11; Locked #7/#9; Cost/critical |
| 6 | **Input bounds + currency uniqueness** — validation tasks for Spec §4 bounds and D3 ≤1 value per currency; reject secret-smuggling unexpected fields | Step 3: D3 case-normalize uniqueness + full Spec §4 bounds table + unexpected-field reject/ignore. Step 6 create returns 400 on validation fail. | Steps 3, 6, 13 |
| 7 | **Secrets hygiene** — no committed secrets; no tokens in query strings/Artifact bodies; no logging raw keys; examples placeholders only | Step 9 full hygiene gate; Step 2 connection-string rule; Artifact body = domain data not secrets store. | Steps 2, 9, 13 |
| 8 | **Zero MM/DC4** — verify no MM/DC4 packages/config/refs | Step 10 verify gate; Step 12 OUT; Step 13 self-verify. | Steps 10, 12–13; Locked #10 |
| 9 | **No scope creep** — do not schedule Negotiation (#6), Strategy/AI, discovery, settlement, Update/Delete, or Cognito/SSO spend | Step 12 Explicit OUT mirrors Spec §5; Constraints header; Steps 4–5 forbid inventing #5 / Cognito/SSO. | Steps 4–5, 11–13; Locked #10; Explicit OUT |
| 10 | **Handshake close** — Dev Plan QA must not PASS until Security QA confirms these points (cite checklist + evidence) | See handshake note below. Done-list for Dev Plan QA requires Security QA confirm before PASS. | Handshake note; Done-list for Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Cite checklist `verification/2026-09-11__security__verification__poc-artifact-devplan-checklist.md`. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD.

---

## 7. Explicit OUT

Mirror Spec §5 / issue #4 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Artifact Update / Delete / full CRUD productization | MVP **P1** |
| SA Option B stretch | Not required for PoC done |
| Saved search / market monitoring | Issue OUT |
| Discovery / instant search | MVP+ |
| Strategy / AI Assistant | PoC OUT |
| Negotiation (#6) / Offers | Later Story |
| Identity-seal (#7) | Later Story |
| Inventing #5 auth productization inside #4 (password/SSO/OIDC IdP) | **#5 is separate parallel Spec** — consume its principal; interim until wired |
| Multi-party / multi-Artifact | V2 |
| Settlement / checkout / escrow | Claims lock |
| MotorMarket / DC4 | Zero deps |
| AWS provision / App Runner / prod deploy | Local/$0; ECS Express sketch only |

---

## 8. Cost/critical

**#4 must not procure AWS.** Any paid AWS provision, Cognito/IdP spend, or other spend proposal → escalate **CPM → COO → CEO**. PoC remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, DB provision, or App Runner tasks. ECS Express Mode remains **README sketch only**.

---

## 9. Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-11__devplan__plan__poc-artifact-d1-d5.md`
- [ ] Spec coverage §§1–6 + issue #4 AC + Option A mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] Product alignment Artifact D1–D5 / Option A (`PRODUCT-BRIEF.md` + BA Option A note); issue #4
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table §6)
- [ ] Input bounds + D3 currency uniqueness scheduled
- [ ] #5 = consume/cross-ref + interim bridge only — **no inventing #5 inside #4**
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] No AWS procure / spend instructions
- [ ] Explicit OUT respected (Spec §5)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock.

---

## 10. Done-list for SD (after plan QA PASS + Chief confirm)

- [ ] Step 1: Extend O10 solution (Domain/Application/Infrastructure as needed; Api only runnable; keep `GET /health`)
- [ ] Step 2: Persist Artifact aggregate D1–D5 + `ownerParticipantId` via EF Core + SQLite local; connection string env/placeholders only
- [ ] Step 3: Domain validation — D3 currency uniqueness (case-normalize) + Spec §4 input bounds; reject secret-smuggling unexpected fields
- [ ] Step 4: Interim principal `X-PoC-Owner-Id` (or Spec-equivalent) → `ownerParticipantId` on create; 401 if missing; do not invent #5 productization
- [ ] Step 5: Prefer #5 JWT/`sub` when #5 SD available (consume principal only); keep owner authz (authn ≠ authz)
- [ ] Step 6: `POST /artifacts` → 201 + Artifact; 400 validation; 401 missing principal
- [ ] Step 7: `GET /artifacts/{id}` owner-scoped; 404 preferred for non-owned/missing; 401 missing principal
- [ ] Step 8: `GET /artifacts` list-own only; 200 array (possibly empty); 401 missing principal; forbid public/global/cross-owner list/search/discovery
- [ ] Step 9: Secrets hygiene gate passed
- [ ] Step 10: Zero MM/DC4 verify passed
- [ ] Step 11: README local/$0; ECS Express Mode sketch retained; no App Runner; no AWS provision
- [ ] Step 12: Do not implement Spec §5 OUT
- [ ] Step 13: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
