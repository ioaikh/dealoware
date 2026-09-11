# Spec — PoC Artifact core model D1–D5 (create / get / list-own)

**Status:** Senior Spec — Security-bound; CPM wording amend (#5 parallel unlocked); ready for Spec QA re-verify  
**Date:** 2026-09-11 (content from 2026-09-10 draft; DOC-FLOW date aligned)  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY PoC #4 Artifact Spec (D1–D5); Security handshake woven before Spec QA PASS  
**Issue:** https://github.com/ioaikh/dealoware/issues/4  
**IDs:** **D1 D2 D3 D4 D5** · `stage:poc` · API **Option A** (Product lock)  
**DOC-FLOW:** `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md`  
**Constraints:** No product code in this artifact. No MotorMarket/DC4. No AWS spend / no invent Stories. **#5 Participant auth is a separate parallel Story** (unlocked) — #4 binds owner-scope authz only; do not invent auth productization inside this Spec. Cost/critical → escalate via COO → CEO. Spec QA must not PASS until Security QA confirms Spec-step points.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #4 AC + OUT + Decisions | https://github.com/ioaikh/dealoware/issues/4 | Binding acceptance; Option A lock |
| Product Option A lock | `plans/2026-09-10__ba__note__story-4-artifact-api-depth.md` | create + get + list-own; Update/Delete → MVP P1 |
| Architecture feasibility §2 | `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` | Data model IN; prefer create/get/list-own; EF Core + relational (SQLite local OK) |
| Product Core model · Artifact | `product/PRODUCT-BRIEF.md` | Subject / Intent / Value / Location / Time |
| Host Spec (O10) | `specs/2026-09-10__spec__spec__poc-o10-scaffold.md` | Extends #3 modular monolith; local/$0; ECS Express Mode sketch |
| Host architecture (O10) | `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md` | Layout + security host baseline |
| Spec Security checklist (binding) | `verification/2026-09-11__security__verification__poc-artifact-spec-checklist.md` | Spec-step points **1–10** |
| #5 auth note (dependency only) | `plans/2026-09-10__ba__note__story-5-poc-auth.md` | Auth productization in #5 Spec (parallel) — not invented here |
| #5 Participant Spec (cross-ref) | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Separate Spec; Artifact consumes principal |

**Product alignment:** Core model Artifact (D1–D5). Participant capability #1 CRUD own Artifacts — PoC proves create/get/list-own only; full CRUD productization = **P1 @ MVP**. Claims lock unchanged (intermediary; no settlement). Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 1 | D1 Subject | Entities: name, description, properties (name/type/value), facts (strings); collection of entities OK |
| 2 | D2 Intent | e.g. buy, sell, rent, exchange, provide, consume, … |
| 3 | D3 Value | amount + currency; 0..n; **≤1 clear value per currency** |
| 4 | D4 Location | 0..n |
| 5 | D5 Time periods | start/end; 0..n |
| 6 | API Option A | **create + get + list-own** only; Update/Delete = **OUT** (MVP P1); SA Option B stretch **not** required |
| 7 | Persistence | Required; owner-scoped list-own |
| 8 | Owner / #5 | Store `ownerParticipantId`; create sets owner; get by id; list-own filters by owner. **#5 Spec is separate parallel Story** (unlocked): prefer #5 JWT/`sub` when wired; interim principal until then. #4 must not invent auth productization / SSO/OIDC |
| 9 | Host | Extend O10 modular monolith; PoC local/$0; ECS Express Mode callout only; no AWS provision |
| 10 | OUT | Update/Delete; saved search/monitoring; Strategy; AI; discovery/search; Negotiation (#6); identity-seal (#7); inventing #5 auth productization inside #4; MotorMarket/DC4; settlement |

---

## 1. Domain model D1–D5

### Artifact (aggregate root)

| Field | Type / notes | Cardinality |
|-------|--------------|-------------|
| `id` | Stable Artifact identifier (server-generated) | 1 |
| `ownerParticipantId` | Owner Participant id (set on create; used for list-own / get authz) | 1 |
| Subject (D1) | Collection of **entities** | 1..n entities (collection OK; at least one entity for a meaningful Subject) |
| Intent (D2) | String enum-like open set (examples: buy, sell, rent, exchange, provide, consume, …) | 1 |
| Values (D3) | List of `{ amount, currency }` | 0..n; **at most one clear value per currency** |
| Locations (D4) | List of location descriptors (geographical or universe-of-space strings per Product) | 0..n |
| TimePeriods (D5) | List of `{ start, end }` windows when negotiation can happen | 0..n |

### D1 Subject · Entity

| Field | Notes |
|-------|-------|
| `name` | Entity name |
| `description` | Entity description |
| `properties` | 0..n of `{ name, type, value }` |
| `facts` | 0..n strings (facts about the Artifact discovered during communication/negotiation — persisted as provided; no Negotiation Story in #4) |

### Value currency uniqueness (D3)

On create (and any future update at MVP): reject payloads with **more than one** Value sharing the same `currency` key (case-normalize currency string for uniqueness check as Spec/SD documents).

### Persistence notes

| Choice | Spec lock |
|--------|-----------|
| Stack | **EF Core** + one relational store (feasibility) |
| Local PoC | **SQLite** OK |
| Later AWS shape | PostgreSQL when AWS shape matters — **not** provisioned in #4 |
| Secrets | Connection strings via **env / placeholders only** — never commit real credentials |
| Host | Implement inside O10 solution (`Dealoware.Domain` / `Application` / `Infrastructure` as needed; API in `Dealoware.Api`) |

---

## 2. API contracts (Option A only)

Base: extend O10 Minimal APIs host. **No** public/global list, search, or discovery endpoints.

### Interim / #5 principal (authn separate Spec)

**#5 Participant auth is a separate Spec** (`specs/2026-09-11__spec__spec__poc-participant-d6-auth.md`) — do **not** invent #5 inside #4. Artifact Spec binds **owner-scope authz** only.

| Item | Spec lock |
|------|-----------|
| Preferred (when #5 wired) | Validated **API key / JWT** principal from #5; map OIDC-shaped `sub` (or equivalent) → `ownerParticipantId` |
| Interim (until #5 wired) | Request header **`X-PoC-Owner-Id`** (string Participant id) **or** equivalent PoC placeholder resolving to the same owner id |
| Scope | Does **not** claim password UX, SSO, or OIDC IdP delivered in #4 |
| Create | Sets `ownerParticipantId` from validated principal (or interim placeholder) |
| Get / list-own | Authorize principal vs `ownerParticipantId` (authn ≠ authz — valid token alone is insufficient) |
| Dependency | Production-grade / productized auth = **#5** Spec + MVP completion; keep Specs separate |

### `POST /artifacts` — create

| Item | Contract |
|------|----------|
| Authz | Interim principal required; sets `ownerParticipantId` |
| Body | Subject entities (D1), Intent (D2), Values (D3), Locations (D4), TimePeriods (D5) — fields per §1 only |
| Success | `201` + Artifact representation including `id`, `ownerParticipantId`, D1–D5 |
| Failures | `400` validation (incl. D3 currency uniqueness, §5 input bounds); `401` if principal missing |

### `GET /artifacts/{id}` — get

| Item | Contract |
|------|----------|
| Authz | Interim principal required; **owner-scoped** |
| Success | `200` + Artifact if `ownerParticipantId` matches caller |
| Non-owned or missing | **`404`** (preferred — do not leak existence of other owners’ Artifacts) or `403` if SD documents why; must not return other owners’ payloads |
| Failures | `401` if principal missing |

### `GET /artifacts` — list-own

| Item | Contract |
|------|----------|
| Authz | Interim principal required |
| Behavior | Returns **only** Artifacts where `ownerParticipantId` == caller |
| Success | `200` + array (possibly empty) |
| Failures | `401` if principal missing |
| Forbidden | Global/public list, cross-owner list, search, discovery |

### Explicitly not in PoC API

`PUT`/`PATCH`/`DELETE` on Artifacts; bulk mutate; search; saved search; Negotiation attach endpoints (#6).

---

## 3. Host / runtime constraints

| Item | Lock |
|------|------|
| Solution | Extend `Dealoware.sln` / O10 modular monolith — one runnable `Dealoware.Api` |
| Local | PoC remains **local / $0** |
| AWS | README may retain **Amazon ECS Express Mode** sketch only; **no** App Runner; **no** AWS account/resource provision in #4 |
| O10 health | Keep `GET /health` contract from O10 Spec unchanged |

---

## 4. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-11__security__verification__poc-artifact-spec-checklist.md`  
Spec QA must not PASS until **Security QA** confirms these points.

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Owner-scoped API only | PoC API = create / get / list-own only; no public/global list/search/discovery; get/list-own owner-scoped | §2 API; Locked #6; §6 OUT |
| 2 | Interim principal without inventing #5 | **#5 is parallel unlocked Spec** — prefer #5 API key/JWT `sub` when available; interim `X-PoC-Owner-Id` (or equivalent) until wired; do **not** invent auth productization / password/SSO/OIDC inside #4 | §2 Interim / #5 principal; Locked #8; Sources #5 Spec cross-ref |
| 3 | Authorization failure behavior | Non-owned/missing get → **404** (preferred) or 403 without leaking other owners’ data; list-own = caller-owned only | §2 GET contracts |
| 4 | No Update/Delete in PoC | Forbid Update/Delete (and bulk mutate) for PoC acceptance | Locked #6; §2 Explicitly not; §6 OUT |
| 5 | Persistence trust boundary | Local/dev persistence only; env/placeholders for connection strings; local/$0; ECS Express sketch only; no AWS provision | §1 Persistence; §3 Host |
| 6 | Input / data hygiene | Validation bounds on D1–D5 payloads (below); reject unexpected fields that smuggle auth/secrets where practical | §4 Input bounds; §1 model |
| 7 | Secrets not in Artifact body | Artifact fields = domain data, not secrets store; no requirement to persist API keys/passwords/cloud creds in Subject/Facts; examples use placeholders | §1 Domain; §4#7; Constraints |
| 8 | Zero MotorMarket / DC4 | Forbid MM/DC4 schemas, feeds, SFTP, shared DB, package refs | §6 OUT; Constraints |
| 9 | No scope creep | No Negotiation (#6), Strategy/AI, discovery/search, settlement, multi-party, or full CRUD; claims lock unchanged | §6 OUT; Locked #10 |
| 10 | Traceability + handshake | Cites issue #4, Product Option A, feasibility roadmap, this checklist; Spec QA PASS only after Security QA confirm | Sources; this §4; Done-list |

### Input bounds (Security point 6 — abuse limits; not new Product Stories)

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
| Unexpected body fields | Reject or ignore per SD — must not accept raw token/secret smuggling fields as first-class Artifact columns |

---

## 5. Explicit OUT

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

## 6. Acceptance mapping (issue #4 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| D1 Subject (entities, properties, facts; collection OK) | §1 D1 |
| D2 Intent | §1 D2 |
| D3 Value (0..n; ≤1 per currency) | §1 D3 + uniqueness |
| D4 Location 0..n | §1 D4 |
| D5 Time periods start/end 0..n | §1 D5 |
| create / get / list-own persisted owner-scoped | §2 API; §1 ownerParticipantId |
| Update/Delete out of PoC | Locked #6; §5 OUT |

---

## Done-list (Spec QA / Security QA / Dev Plan / SD)

### Spec QA (after Security weave — this draft)

- [ ] DOC-FLOW path: `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md`
- [ ] Binding sources only (issue #4, Option A note, SA §2, Product Artifact, O10 host, Spec Security checklist)
- [ ] D1–D5 model + Value currency uniqueness locked
- [ ] API Option A only: create / get / list-own; owner-scoped; #5 parallel Spec cross-ref + interim/`sub` documented
- [ ] Persistence: EF Core + relational; SQLite local OK; secrets env/placeholders
- [ ] **Security points 1–10** bound with section cites (§4)
- [ ] Explicit OUT + AC mapping
- [ ] No product code; no invented Stories; no MM/DC4; no AWS spend
- [ ] **Ask Security QA** to confirm Spec-step 1–10 with evidence **before** Spec QA PASS to Chief Spec

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec confirm)

- [ ] Persist Artifact aggregate D1–D5 + `ownerParticipantId` (EF Core; SQLite local)
- [ ] Implement `POST /artifacts`, `GET /artifacts/{id}`, `GET /artifacts` (list-own) per §2
- [ ] Wire interim PoC principal; enforce owner-scope + 404/403 behavior
- [ ] Enforce D3 currency uniqueness + §4 input bounds
- [ ] Do not implement §5 OUT (no Update/Delete, search, Negotiation, #5 IdP, AWS provision)

**Next:** Spec QA verifies → Security QA confirm → Spec QA confirm to **Chief Spec only**. Dev Plan after Chief Spec + CPM Security gate.
