# Spec — PoC 1:1 Negotiation + Offers (D7–D10, P4)

**Status:** Senior Spec — Security-bound draft ready for Spec QA  
**Date:** 2026-09-20  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY #6 Negotiation + Offers (CEO unlock)  
**Issue:** https://github.com/ioaikh/dealoware/issues/6  
**IDs:** **D7 D8 D9 D10 P4** · `stage:poc` · strictly **1:1**  
**DOC-FLOW:** `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md`  
**Constraints:** No product code. No MotorMarket/DC4. No AWS spend / no invent Stories. Keep **separate** from #4/#5 Specs (build on; do not rewrite). PoC local/$0; ECS Express Mode sketch only. Cost/critical → COO → CEO. Spec QA must not PASS until Security QA confirms Spec-step points.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #6 AC + OUT | https://github.com/ioaikh/dealoware/issues/6 | Binding acceptance |
| Product Negotiation/Offers | `product/PRODUCT-BRIEF.md` | Complementary intents; Accept/Decline/Counter; Close cancels open offers; optional start/end |
| Architecture feasibility §1–2 | `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` | 1:1; offer lifecycle; concurrency minimal; Close cancels; idempotency optional PoC; accept does not release contact |
| Release roadmap | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | D7–D10, P4 |
| Host Spec (O10) | `specs/2026-09-10__spec__spec__poc-o10-scaffold.md` | Modular monolith host; local/$0; ECS Express sketch |
| Artifact Spec (#4) | `specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md` | Artifact aggregate to attach Negotiation |
| Participant Spec (#5) | `specs/2026-09-11__spec__spec__poc-participant-d6-auth.md` | Authn principal; protected Negotiation/Offer APIs |
| Spec Security checklist (binding) | `verification/2026-09-20__security__verification__poc-negotiation-spec-checklist.md` | Spec-step points **1–10** |

**Product alignment:** Negotiation around an Artifact between Participants with complementary intents; Offers Accept/Decline/Counter; Close cancels open offers; optional expiration. MVP strictly 1:1 (multi-party/multi-Artifact = V2). Claims lock: intermediary; no settlement. Contact on accept = MVP / #7 path — **not** this Spec. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 1 | D7 | 1:1 Negotiation + place Offer; complementary intents required |
| 2 | D8 | Accept / Decline / Counter on an open Offer |
| 3 | D9 | Close Negotiation **cancels all open offers** (binding) |
| 4 | D10 | Thin Negotiation expiration (optional `endsAt`; when past → Expired; cancel open offers) |
| 5 | P4 | Participant offer-cycle start: place / accept / decline / counter path |
| 6 | Cardinality | Strictly **1:1** — exactly two Participants; one Artifact; **no** multi-party / multi-Artifact |
| 7 | Concurrency (SA) | **One open Offer per side** (simplest maintainable; cite SA §1#3). No exclusive-negotiate flag required in PoC |
| 8 | Authn | Validated #5 principal required on all Negotiation/Offer APIs; fail closed `401`/`403` |
| 9 | Authz | Only the two Negotiation parties may read/act; non-party → `404` preferred (or `403`) without leaking |
| 10 | Identity on Accept | Accept does **not** release contact/PII in PoC (identity-seal / contact = #7 / MVP OUT) |
| 11 | Idempotency | Keys **optional** in PoC; **required MVP** (A14) — **not** PoC AC |
| 12 | Host | Extend O10; local/$0; ECS Express sketch; no AWS provision |
| 13 | OUT | Strategy; AI; contact exchange; multi-party/multi-Artifact; settlement; Cognito/SSO; #7–#8 scope; MM/DC4; AWS spend |

---

## 1. Domain model + state rules

### Negotiation

| Field | Notes |
|-------|-------|
| `id` | Server-generated |
| `artifactId` | Existing Artifact (#4) |
| `partyAParticipantId` | Initiator / first party (`#5.sub`) |
| `partyBParticipantId` | Counterparty — distinct from party A |
| `status` | `Open` \| `Closed` \| `Expired` |
| `startsAt` | Optional (thin) |
| `endsAt` | Optional — D10 expiration boundary |
| Complementary intents | At create: parties’ intents for this Negotiation must be **complementary** (Product examples: buy↔sell, provide↔consume, etc.). Spec does not invent a closed enum beyond Product examples; SD documents the PoC complementarity check used |

**Invariants**

1. Exactly **two** parties; exactly **one** Artifact.
2. Only parties may mutate or read Negotiation/Offer resources.
3. `Closed` and `Expired` are terminal for new Offers / Accept / Decline / Counter.

### Offer

| Field | Notes |
|-------|-------|
| `id` | Server-generated |
| `negotiationId` | Parent Negotiation |
| `fromParticipantId` | Must be a Negotiation party |
| `toParticipantId` | The other party |
| `status` | `Open` \| `Accepted` \| `Declined` \| `Superseded` \| `Cancelled` |
| Payload | Thin PoC terms sufficient for offer cycle (e.g. proposed value amount+currency and/or free-form terms string) — **no** Strategy/AI fields; **no** contact/PII |

### Lifecycle rules (binding)

| Action | Preconditions | Effects |
|--------|---------------|---------|
| Create Negotiation | Authn; Artifact exists; two distinct parties; complementary intents; 1:1 | `status=Open` |
| Place Offer | Negotiation `Open` (not expired); caller is party; caller has **no** other `Open` Offer on this Negotiation | New Offer `Open`; enforces **one open Offer per side** |
| Accept | Negotiation `Open`; Offer `Open`; caller is `toParticipantId` | Offer → `Accepted`; other `Open` Offers on Negotiation → `Cancelled`; Negotiation may remain `Open` or SD documents Accept→Close — **default:** Negotiation stays `Open` unless Close called (Product allows further offers until Close); **do not** release contact |
| Decline | Same as Accept preconditions | Offer → `Declined` |
| Counter | Same as Accept preconditions; caller has no other `Open` Offer (or Counter replaces caller’s prior open per one-open-per-side) | Prior Offer → `Superseded` (or `Declined` if SD prefers single terminal for declined path — **prefer `Superseded`** for Countered prior); new Offer `Open` from counterparty |
| Close | Negotiation `Open` or `Expired`-eligible; caller is party | Negotiation → `Closed`; **all** `Open` Offers → `Cancelled` (**D9 binding**) |
| Expire (D10 thin) | `endsAt` set and now ≥ `endsAt`; Negotiation still `Open` | Negotiation → `Expired`; **all** `Open` Offers → `Cancelled` (same cancel semantics as Close for opens) |

**Concurrency pick (SA cite):** SA feasibility §1#3 — “one open offer per side or exclusive negotiate flag”. Spec picks **one open Offer per side** as simplest maintainable for two-party lifecycle correctness.

---

## 2. API contracts

All endpoints below require **#5** validated principal (`Authorization` header). Unauthenticated → **401**. Non-party → **404** preferred (no leak) or **403**.

### `POST /negotiations` — create (D7)

| Item | Contract |
|------|----------|
| Body | `artifactId`, `counterpartyParticipantId`, optional `startsAt`/`endsAt`, intent fields as needed for complementarity check |
| Success | `201` + Negotiation representation |
| Failures | `400` validation / non-complementary / not 1:1; `401`; `404` Artifact |

### `GET /negotiations/{id}` — get

| Item | Contract |
|------|----------|
| Authz | Party only |
| Success | `200` + Negotiation (+ optional embedded offers list for parties) |
| Failures | `401`; `404` missing or non-party |

### `POST /negotiations/{id}/close` — close (D9)

| Item | Contract |
|------|----------|
| Authz | Party only |
| Success | `200` Negotiation `Closed`; all open Offers cancelled |
| Failures | `401`; `404`; `409` if already terminal (Closed) |

### `POST /negotiations/{id}/offers` — place Offer (D7 / P4)

| Item | Contract |
|------|----------|
| Authz | Party only |
| Body | Thin offer payload (no contact/PII; no secrets) |
| Success | `201` + Offer `Open` |
| Failures | `400` if caller already has `Open` Offer; `401`; `404`; `409` if Negotiation not `Open` |

### `POST /offers/{id}/accept` — Accept (D8 / P4)

| Item | Contract |
|------|----------|
| Authz | Must be Offer `toParticipantId` |
| Success | `200` Offer `Accepted`; other open Offers cancelled; **no contact fields** in response |
| Failures | `401`; `404`; `409` if Offer/Negotiation not actionable |

### `POST /offers/{id}/decline` — Decline (D8 / P4)

| Item | Contract |
|------|----------|
| Authz | Must be Offer `toParticipantId` |
| Success | `200` Offer `Declined` |
| Failures | `401`; `404`; `409` |

### `POST /offers/{id}/counter` — Counter (D8 / P4)

| Item | Contract |
|------|----------|
| Authz | Must be Offer `toParticipantId` |
| Body | New thin offer payload |
| Success | `201` (or `200`) with prior Offer `Superseded` + new Offer `Open` from counterparty |
| Failures | `401`; `404`; `409`; `400` if one-open-per-side violated |

### Expiration behavior (D10)

| Item | Contract |
|------|----------|
| Mechanism | Thin: evaluate `endsAt` on mutating reads/writes and/or a simple check before offer actions; optional lightweight sweep — Spec does not require a scheduler Story |
| Effect | Negotiation → `Expired`; cancel open Offers |
| API | Expired Negotiation rejects new place/accept/decline/counter with `409`; `GET` still allowed for parties |

### Idempotency

Optional PoC header (e.g. `Idempotency-Key`) on offer writes — **not** required for PoC AC; required at MVP (A14).

---

## 3. Authn / authz (cross-Spec)

| Concern | Owner Spec |
|---------|------------|
| Valid principal / token | **#5** |
| Artifact ownership | **#4** (Negotiation attaches to Artifact; create may require initiator relationship as SD documents — do not invent discovery) |
| Negotiation/Offer party authz | **#6** (this Spec) — both parties only |

Fail closed. Do not smuggle anonymous owners.

---

## 4. Host / runtime

| Item | Lock |
|------|------|
| Solution | Extend O10 modular monolith (`Dealoware.Api` + Domain/Application/Infrastructure) |
| Persistence | EF Core + relational; SQLite local OK (same as #4) |
| Local | PoC **local / $0** |
| AWS | ECS Express Mode sketch only; no App Runner; no AWS provision |
| Secrets | Env/placeholders only; never commit credentials; never log raw tokens |

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-20__security__verification__poc-negotiation-spec-checklist.md`  
Spec QA must not PASS until **Security QA** confirms these points. #7–#8 remain backlog — do not invent contact exchange.

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | Authn fail-closed | All Negotiation/Offer mutating + party-data reads require validated #5 principal; unauthenticated → 401/403; no anonymous negotiate/offer | §2 API preamble; §3 Authn; Locked #8 |
| 2 | Authorization — party-only | Only the two 1:1 parties may start/view/offer/accept/decline/counter/close; non-party → 404 preferred (or 403) without leaking | §2 Authz rows; §3; Locked #9 |
| 3 | Strictly 1:1 | Forbid multi-party / multi-Artifact; reject create/join adding a third Participant or second Artifact | §1 invariants; Locked #6; §6 OUT |
| 4 | Complementary intents | Require complementary intents at create; reject non-complementary without inventing new Product intent rules | §1 Negotiation; §2 create; Locked #1 |
| 5 | Offer state-machine integrity | Lock place/Accept/Decline/Counter + **one open Offer per side**; illegal/unauthorized transitions fail closed (no silent overwrite) | §1 Lifecycle; Locked #7; §2 offer endpoints |
| 6 | Close cancels open offers | Close cancels all open Offers (atomic/equivalent); Closed rejects further Accept/Counter on cancelled offers | §1 Close/Expire; §2 close; Locked #3 |
| 7 | Expiration (D10 thin) | After `endsAt`, Negotiation `Expired`; Accept/Counter/place/decline writes fail (`409`); no stale-client bypass | §1 Expire; §2 Expiration; Locked #4 |
| 8 | No contact / PII on Accept | Accept changes offer/negotiation state only — **no** contact exchange / identity reveal (#7 / MVP OUT) | Locked #10; §2 Accept; §6 OUT |
| 9 | Secrets / host / no spend | Reuse #5 Authorization-header hygiene; no tokens in query/body; no committed secrets; local/$0; ECS Express sketch; no Cognito/SSO/IdP; no settlement; zero MM/DC4 | §4 Host; §3; Locked #12/#13; §6 OUT |
| 10 | Traceability + handshake | Cites issue #6 AC+OUT only; maps these points; Spec QA PASS only after Security QA confirm | Sources; this §5; Done-list |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Strategy engine | PoC OUT |
| AI Assistant | PoC OUT |
| Contact exchange on Accept | #7 / MVP (P7/A9) |
| Multi-party / multi-Artifact | V2 (A13) |
| Settlement / checkout / escrow | Claims lock |
| Idempotency keys as PoC AC | Optional PoC; required MVP (A14) |
| Cognito / SSO IdP | No spend; O9 later |
| #7 identity-seal productization / #8+ | Separate Stories |
| MotorMarket / DC4 | Zero |
| AWS provision / App Runner / prod deploy | Local/$0; ECS Express sketch only |
| Rewriting #4/#5 Specs | Build on only |

---

## 7. Acceptance mapping (issue #6 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| D7 1:1 Negotiation + place Offer (complementary intents) | §1; §2 create + place |
| D8 Accept / Decline / Counter | §2 accept/decline/counter |
| D9 Close cancels all open offers | §1 Close row; §2 close |
| D10 thin expiration | §1 Expire; §2 Expiration |
| P4 offer-cycle start | §2 place/accept/decline/counter |
| Strictly 1:1 | Locked #6; §1 invariants |

---

## Done-list (Spec QA / Security QA / Dev Plan / SD)

### Spec QA (Security woven)

- [ ] DOC-FLOW: `specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md`
- [ ] Binding sources only; builds on #4/#5 without rewrite
- [ ] D7–D10 + P4 + 1:1 locked; Close cancels opens; one open Offer per side
- [ ] API contracts + #5 authn + party authz fail closed
- [ ] Accept does not release contact; idempotency not PoC AC
- [ ] **Security checklist points 1–10** bound with cites (§5)
- [ ] Explicit OUT + AC mapping; no MM/DC4; local/$0; #7–#8 not invented
- [ ] **Ask Security QA** confirm Spec-step 1–10 **before** PASS to Chief Spec

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Persist Negotiation + Offer aggregates; enforce 1:1 + complementarity + one-open-per-side
- [ ] Implement create/get/close + place/accept/decline/counter + thin expiration
- [ ] Wire #5 authn; party-only authz; Close/Expire cancel opens
- [ ] No contact release on Accept; do not implement §6 OUT

**Next:** Spec QA verifies → ask Security QA confirm Spec-step 1–10 → Spec QA confirm to **Chief Spec only** (never skip Chief).
