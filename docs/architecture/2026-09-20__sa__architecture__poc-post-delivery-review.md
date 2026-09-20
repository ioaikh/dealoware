# Architecture review — PoC post-delivery (#3–#8)

**Status:** Senior Architect review for Architecture QA (CEO 2026-09-20 post-milestone gate). **Amended:** Security answers §5 points 1–10.  
**Date:** 2026-09-20  
**Author:** Dealoware Senior Architect  
**Brief:** `architecture/2026-09-20__sa__architecture__poc-post-delivery-review-brief.md`  
**DOC-FLOW:** `architecture/2026-09-20__sa__architecture__poc-post-delivery-review.md`  
**HOLD:** MVP / GitHub #18 unlock until Chief Architect PASS (Architecture QA) or CEO answers escalations  
**Constraints:** No product invention; no MotorMarket; PoC $0; host shape = **Amazon ECS Express Mode (Fargate)** (`open`); **App Runner** excluded (`existing-customers-only` + `no-new-features`)

## Sources

| Source | Path / link | Role |
|--------|-------------|------|
| CA / CEO brief | `architecture/2026-09-20__sa__architecture__poc-post-delivery-review-brief.md` | Binding ask |
| Feasibility baseline | `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` | PoC thin-slice intent |
| O10 scaffold baseline | `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md` | Host/layout/health/secrets |
| Product | `product/CEO-ORIGINAL-BRIEF.md`, `product/PRODUCT-BRIEF.md` | Prefer .NET / AWS; claims; early additions |
| Actual | `https://github.com/ioaikh/dealoware` `main` (fetched 2026-09-20) | `src/Dealoware.{Api,Application,Domain,Infrastructure}`, README, Dockerfile, LICENSE, tests, Postman |
| Security SA checklist | `verification/2026-09-20__security__verification__poc-post-delivery-sa-checklist.md` | Architecture always-critical points **1–10** |

**Stories CLOSED (brief):** #3 O10 · #4 Artifact D1–D5 · #5 Auth D6 · #6 Negotiation/Offers D7–D10 · #7 Identity-seal · #8 L1–L3 posture.

---

## 1. Intent check

| Area | Verdict | Baseline cite | Evidence on `main` |
|------|---------|---------------|-------------------|
| **Host / O10** | **PASS** | O10 §1–§4, §6; feasibility §1 modular monolith .NET 8+; ECS Express sketch; local-until-spend | `Dealoware.sln` + `src/Dealoware.{Api,Application,Domain,Infrastructure}`; `TargetFramework` net8.0; `Program.cs` `MapGet("/health", … Results.Ok(new { status = "ok" }))` with **no auth** and no DB call on that path; README Local Run + ECS Express Mode target / App Runner NOT target; `Dockerfile` local multi-stage comment (no prod IAM/Secrets Manager/ECR/signing) |
| **Artifact** | **PASS** (soft note: no Update/Delete yet) | Feasibility §2a Artifact S/I/V/L/T IN; §2b prefer Create/Get/List-own or full CRUD | `Domain/Artifacts/Artifact.cs` — Subject entities, Intent, Values, Locations, TimePeriods + `OwnerParticipantId`; API `POST/GET /artifacts`, `GET /artifacts/{id}` owner-scoped (`ArtifactEndpoints.cs`); auth required on Artifact routes |
| **Auth / Participant** | **PASS** | Feasibility §1 Auth OIDC-compatible principal; Participant minimal register/auth | `Participant.Sub` OIDC-shaped (`participant:{guid}`); `/auth/register`, `/token`, `/revoke`, `/rotate-key` (`AuthEndpoints.cs`); JWT via env/`Jwt:SigningKey` placeholder (`Program.cs`); Authorization header patterns documented in README |
| **Negotiation / Offers** | **PASS** | Feasibility PoC IN: 1:1 Negotiation; Offer Accept/Decline/Counter/Close; Close cancels open offers; MVP 1:1 | `NegotiationEndpoints` create/get/place offer/close; `OfferEndpoints` accept/decline/counter; complementary intents documented in README; Postman `postman/Dealoware-PoC-Negotiations.postman_collection.json`; tests under `tests/Dealoware.Api.Tests/` |
| **Identity-seal** | **PASS** | Feasibility identity-seal **stub**; contact deferred MVP+ | README Identity Seal stub; responses use opaque `participant:{uuid}` + `identitySealed: true`; `IdentitySealTests` assert no contact PII on create/get/offer/accept paths; no contact release on Accept |
| **L1–L3 posture** | **PASS** | PRODUCT-BRIEF / CEO later decisions: Apache-2.0; public GitHub; hosted remains AIKnowHow; MM/DC4 out | Root `LICENSE` Apache 2.0; README Apache + AIKnowHow hosted posture; README explicitly out MotorMarket/DC4; public repo `ioaikh/dealoware` |

**Overall intent:** PoC thin-slice architecture intent from Sep-10 baselines is **met** on `main` for CLOSED Stories #3–#8.

---

## 2. Deviations

| Deviation | Original | Actual | Why (evidence) | Root cause | Required adjustments (just-delivered SD) |
|-----------|----------|--------|----------------|------------|------------------------------------------|
| Artifact verbs stop at Create/Get/List-own | Feasibility §2b Option B “full CRUD if cheap”; Option A Create+Get+List acceptable | No `PUT`/`PATCH`/`DELETE` on `/artifacts` | `ArtifactEndpoints.cs` maps only POST `/`, GET `/{id}`, GET `/` | Story #4 scoped to D1–D5 core + owner CRUD-lite; Update/Delete deferred | **Docs/tests:** keep AC honest (no claim of full CRUD). **Follow-up Story (MVP Artifact CRUD):** add Update/Delete when Product/PM schedule — do **not** invent now |
| Infrastructure libs are no longer “inert placeholders” | O10 §1 rule 5 / Security §8: Domain/App/Infra placeholders must not pull auth/LLM/payment SDKs **in O10** | Infra has EF Core Sqlite + JWT packages; Application/Domain have real models | Expected after #4–#7 landed on same modular monolith | Sequential Story delivery into one host — correct evolution past O10-only | **None for SD rollback.** Architecture note: O10 scaffold doc remains historical for #3; current state = full PoC modular monolith |
| Host exposes many routes beyond `/health` | O10 §2: O10 endpoint surface health-only | `Program.cs` maps Auth, Artifact, Negotiation, Offer endpoints | Cumulative #4–#8 delivery | Same as above | **None** — health remains unauthenticated liveness; other routes auth-gated (except register/token as designed) |
| Persistence default SQLite file + `EnsureCreated` | Feasibility: SQLite OK local; EF Core | `DefaultConnection` / `dealoware.db`; `EnsureCreatedAsync` at startup | PoC local $0 | Intentional PoC simplicity | **Keep for PoC.** MVP+: migrations + managed DB only after spend OK (escalate) — no provision now |
| JWT signing key code default placeholder string | O10 secrets binding: placeholders/env-only | `DEVELOPMENT_PLACEHOLDER_KEY_CHANGE_IN_PRODUCTION_32CHARS` fallback in `Program.cs`; README warns env for real keys | Dev ergonomics | Acceptable if never a real secret | **Keep** placeholder labeling; SD must not commit real keys (tests/README already warn) |

No MotorMarket/DC4 coupling found in reviewed csproj/README/Dockerfile paths.

---

## 3. Fit to future architecture (MVP+ / Vn)

Against feasibility roadmap PoC OUT / MVP add-ons and PRODUCT-BRIEF early additions (no invented IDs beyond Product numbers):

| Action | Item | Rationale |
|--------|------|-----------|
| **Keep** | Modular monolith .NET 8 (`Api` + Domain + Application + Infrastructure) | Matches feasibility recommended Option A; Spec/SD already invested |
| **Keep** | `GET /health` liveness-only, no external deps | O10 Security binding; do not couple health to DB |
| **Keep** | OIDC-shaped `sub`, Authorization-header credentials | Enables later SSO without principal rewrite |
| **Keep** | Identity-seal stub (`identitySealed`, opaque ids, no contact on accept) | Correct PoC; MVP contact-on-accept extends without rewriting DTOs if contact stays off public types until Accept |
| **Keep** | ECS Express Mode as named host sketch; App Runner excluded; local until spend | ORG-OPS / CEO lock; PoC $0 |
| **Keep** | 1:1 Negotiation + complementary intents | Early addition **13**; multi-party stays V2 |
| **Add** (MVP, when unlocked — not now) | Artifact Update/Delete; instant search; minimal Strategy CRUD; thin AI Assistant + budgets (early addition **8**); contact-on-accept; idempotent offers + audit + OTel hooks | Feasibility §3 MVP add-ons — **HOLD #18 / MVP** until CA PASS |
| **Add** (V1+) | Saved search; fuller Strategy; notifications; published OpenAPI/webhooks | Feasibility §4 / Product stage ack |
| **Change** (docs only now) | Treat O10 scaffold doc as **#3 historical baseline**; this review + feasibility as living PoC architecture posture | Avoid readers treating “health-only surface” as current whole-system constraint |
| **Remove / do not introduce** | App Runner sketches; MotorMarket/DC4 deps; settlement/escrow; multi-party in MVP; Strategy/AI in remaining PoC | Claims lock + PoC OUT list |
| **Remove / do not introduce** | Prod AWS provision in PoC Stories | PoC $0 until CEO spend OK |

---

## 4. Disposition

**Choice: Update plans (documentation / architecture index) — no CEO escalations required for intent PASS.**

### Patches / file list (KB canonical; GitHub `docs/` mirror via Doc/PM)

| File | Action |
|------|--------|
| `architecture/2026-09-20__sa__architecture__poc-post-delivery-review.md` | **This deliverable** — current PoC posture after #3–#8 |
| `architecture/README.md` | Add link to this review as post-PoC-milestone architecture status (Doc triad may index) |
| `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md` | **No rewrite of history** — optional one-line header note “Superseded for *current* endpoint surface by 2026-09-20 post-delivery review; remains #3 intent baseline” (can be applied in follow-up Doc/SA micro-patch after QA PASS) |
| `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` | **Keep** as PoC/MVP feasibility baseline; no product invent |
| GitHub `docs/architecture/` mirror | After Architecture QA + CA PASS, Doc/PM sync this review to `docs/architecture/` on `main` |

**Not patching now:** Product briefs, MVP Story unlock (#18), AWS provision.

**CEO escalations:** **None** from this review — intent PASS; soft Artifact Update/Delete deferred is Story-scope, not a CEO product conflict.

---

## 5. Security answers (Architecture always-critical handshake)

**Checklist:** `verification/2026-09-20__security__verification__poc-post-delivery-sa-checklist.md` (Chief Security, 2026-09-20).  
**Rule:** Architecture QA asks **Security QA** to confirm points **1–10** with evidence before PASS to Chief Architect. No product invention; PoC $0; #18 HOLD.

| # | Security point | Architecture answer | Cite |
|---|----------------|---------------------|------|
| 1 | Host/O10 trust boundary — local/$0; ECS sketch; health liveness-only | **MET.** PoC remains local/$0; ECS Express Mode is README **sketch/target** only (App Runner excluded). `GET /health` is unauthenticated liveness `{status:ok}` with **no** DB/AWS/outbound on that path. Production TLS/IdP/public exposure **not** claimed delivered. | §1 Host/O10 PASS; `Program.cs` health MapGet; README AWS / Local Run; Dockerfile local-only comment |
| 2 | Secrets hygiene — no committed secrets/MM-DC4 logins | **MET.** `appsettings.json` uses local SQLite path only; JWT uses env / config with **development placeholder** fallback string (not a real secret); README warns never commit real signing keys. No MM/DC4 logins/SFTP/inventory credentials in reviewed artifacts. | §1–§2; `Program.cs` JwtSettings; `appsettings.json`; README Authentication env table |
| 3 | Zero MM/DC4 coupling + L1–L3 | **MET.** No MM/DC4 project refs/packages/config in csproj tree reviewed; README lists MotorMarket/DC4 as out; L1–L3 Apache/public/hosted-non-goal held. | §1 L1–L3 PASS; README out-of-scope / LICENSE Apache-2.0 |
| 4 | Artifact owner-scoped fail-closed | **MET.** Artifact endpoints require Authorization; owner-scoped via `OwnerParticipantId` ↔ principal `sub`; no anonymous Artifact CRUD architecture. Soft gap Update/Delete not present — does not weaken fail-closed on existing verbs. | §1 Artifact PASS; `ArtifactEndpoints.cs`; `Artifact.OwnerParticipantId` |
| 5 | Participant authn fail-closed; no Cognito/SSO delivered | **MET.** Protected routes use Authorization header (JWT / ApiKey); register/token are bootstrap; revoke/rotate present. **No** Cognito/SSO as delivered — OIDC-**shaped** `sub` only for future SSO. | §1 Auth PASS; `AuthEndpoints.cs`; `Participant.Sub`; README auth |
| 6 | Negotiation 1:1 + party-only; Close/expiry fail-closed | **MET.** Architecture is 1:1 Negotiation with complementary intents; party-only authz on negotiate/offer paths; Accept/Decline/Counter/Close state machine; Close cancels open offers (README/tests). Thin expiry per Story intent — fail-closed on unauthorized party actions. | §1 Negotiation PASS; Negotiation/Offer endpoints; README PoC negotiate section; Postman |
| 7 | Identity-seal stub — opaque ids; contact deferred MVP | **MET.** Responses use opaque `participant:{uuid}` + `identitySealed: true`; Accept returns state only — **no** contact release. Real contact-on-accept deferred MVP (P7/A9 in Product early-addition language) — **not** claimed delivered. | §1 Identity-seal PASS; README Identity Seal; `IdentitySealTests` |
| 8 | L1–L3 public posture | **MET.** Apache-2.0 `LICENSE`; public `ioaikh/dealoware`; hosted remains AIKnowHow/Dealoware (free fork ≠ platform) in README. | §1 L1–L3; LICENSE; README header |
| 9 | No PoC→MVP inventing / spend | **MET.** This review does **not** invent Cognito/SSO, prod ECS spend, App Runner, settlement, Strategy/AI, or #18 tenancy as PoC-delivered. MVP items listed under §3 **Add (when unlocked)** only. PoC **$0**; cost/critical → COO → CEO. | §3 Fit-to-future; §4 Disposition; HOLD #18 |
| 10 | Traceability + handshake | **MET.** §1 maps each area to baseline § + `main` evidence; §2 maps gaps to SD/doc adjustments (no CEO guess); Security answers cite checklist + evidence. Architecture QA must obtain Security QA confirm before PASS. | §§1–4; this §5; Done-list |


## Done-list (Architecture QA)

- [ ] Path/name DOC-FLOW: `architecture/2026-09-20__sa__architecture__poc-post-delivery-review.md`
- [ ] §1 Intent check covers all six areas with baseline § + `main` evidence
- [ ] §2 Deviations table complete (or empty with explicit none)
- [ ] §3 Fit-to-future change/remove/add/keep
- [ ] §4 Disposition: update-plans list **or** numbered CEO questions
- [ ] §5 Security answers map checklist points **1–10** with cites (`verification/2026-09-20__security__verification__poc-post-delivery-sa-checklist.md`)
- [ ] Ask **Security QA** to confirm points 1–10 with evidence **before** PASS to Chief Architect
- [ ] No product invention; MVP/#18 still HOLD; PoC $0; ECS Express / App Runner currency respected

**Next:** Architecture QA verifies §§1–5 vs brief + Security checklist; ask Security QA confirm on 1–10; then confirm to Chief Architect only.
