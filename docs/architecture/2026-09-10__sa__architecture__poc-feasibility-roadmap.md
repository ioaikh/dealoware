# Architecture options — PoC feasibility (roadmap consult)

**Status:** Senior Architect options for Architecture QA verification (PM feasibility consult; not a full Story).  
**Date:** 2026-09-10  
**Author:** Dealoware Senior Architect  
**Brief:** Chief Architect — Product-locked PoC thin slice + MVP add-ons feasibility vs V1  
**DOC-FLOW:** `architecture/` · `YYYY-MM-DD__sa__architecture__{slug}.md`

## Sources (cited; no invented requirements)

| Source | Path / note | Role |
|--------|-------------|------|
| CEO original | `product/CEO-ORIGINAL-BRIEF.md` | Verbatim intent + later CEO decisions appendix |
| Product summary | `product/PRODUCT-BRIEF.md` | Processed goals; Product QA PASS 2026-09-10 (`verification/2026-09-10__product__verification__product-brief-refresh.md`) |
| Product stage ack | On file with PM (Chief Product → CPM/Senior PM chat 2026-09-10); **not** a dated KB artifact under `product/` at write time | PoC / MVP / V1+ boundaries locked for this consult |
| DOC-FLOW | `meta/DOC-FLOW.md` | Path/naming |

**Capability IDs (D* / P* / O* / A*):** Formal `D*` / `P*` / `O*` IDs are **unknown in Product KB docs** (not defined in `CEO-ORIGINAL-BRIEF.md` or `PRODUCT-BRIEF.md`). Early platform additions in `PRODUCT-BRIEF.md` are numbered **1–14** (PM/Product chat sometimes labels these `A1`–`A14`). This doc cites those **PRODUCT-BRIEF § Agreed early platform additions** numbers only — does **not** invent new IDs.

**Product-locked scope for this consult (from CA brief / Product stage ack):**

**PoC IN:** Artifact Subject/Intent/Value/Location/Time; 1:1 Negotiation; Offer Accept / Decline / Counter / Close; minimal API; identity-seal **stub**; minimal Participant auth; prefer **.NET**; **AWS = target host shape** (not prod deploy).  
**PoC OUT:** Strategy engine; AI Assistant; saved-search / market monitoring.

**MVP add-ons (feasibility vs V1 only):** Artifact CRUD; instant search; initiate negotiate/offers; minimal Strategy CRUD; thin AI Assistant; identity-until-accept + contact on accept; idempotent offers + audit + OpenTelemetry hooks.

**Claims / non-goals (PRODUCT-BRIEF):** Intermediary only; no online settlement/checkout by default; MotorMarket/DC4 live systems out; Apache-2.0 public GitHub; hosted platform remains AIKnowHow / Dealoware; MVP strictly 1:1 (early addition **13** → multi-party / multi-Artifact = v2).

---

## 1. Is PoC thin slice feasible as a simple .NET core?

**Answer: YES** — with constraints below.

### Recommended shape (simplest maintainable)

| Choice | Recommendation | Why |
|--------|----------------|-----|
| Runtime | .NET 8+ ASP.NET Core | Matches CEO/Product prefer-.NET (original platform-owner #10; PRODUCT-BRIEF same) |
| API style | Minimal APIs or thin controllers + OpenAPI attributes later | Small surface; no need for full public OpenAPI package in PoC |
| Persistence | EF Core + one relational store (SQLite for local PoC OK; PostgreSQL when AWS shape matters) | One model, easy migrations; avoid premature CQRS/event-sourcing |
| Host shape | Runnable **locally** until spend approved; Dockerfile + optional **Amazon ECS Express Mode** (Fargate) **sketch** only — **do not** recommend App Runner | CEO lock via CA: target = ECS Express Mode; PoC stays local; no AWS account provision in PoC Stories; **not** prod deploy |
| Auth | Minimal registered Participant (password or API key / JWT) with **OIDC-compatible principal claim shape** | Enables later SSO (platform-owner #9) without rewrite |
| Identity seal | Stub: no contact/PII fields on public Participant DTO; opaque ids only | Aligns to identity-until-accept without building vault yet |


### AWS target host lock (CEO / Bot Manager — architecture note)

- **Named sketch/target:** **Amazon ECS Express Mode** (Fargate under the hood) — status `open`.
- **Do not** recommend **App Runner** for greenfield — status `existing-customers-only` + `no-new-features` (closed to new customers **2026-04-30**).
- **PoC remains local** until spend is approved; **no AWS account provision** and no AWS resources created in PoC Stories.

### Cloud hosting currency check (`ops/ORG-OPS.md` mandatory)

Checked **2026-09-10** against official AWS docs (ORG-OPS enums: `open` | `existing-customers-only` | `no-new-features` | `deprecated/sunset`).

| Service | Status labels | Greenfield Dealoware? | Official check |
|---------|---------------|----------------------|----------------|
| **Amazon ECS Express Mode** (Fargate) | `open` | **Yes** — named sketch/target | [ECS Express Mode overview](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/express-service-overview.html) — available in Regions where Amazon ECS and Fargate are supported; no closed-to-new-customers or no-new-features notice on that page (fetched 2026-09-10). |
| **AWS App Runner** | `existing-customers-only` + `no-new-features` | **No** — excluded from greenfield options | [App Runner availability change](https://docs.aws.amazon.com/apprunner/latest/dg/apprunner-availability-change.html); product [End of support notice](https://aws.amazon.com/apprunner/) — no new customers from **2026-04-30**; existing customers continue; no new features planned; AWS recommends ECS Express Mode. |

**Checklist sources:** `ops/ORG-OPS.md` § Hosting / cloud currency; supporting DevOps lock `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md` (CEO via CPM; no spend).

**PoC:** remains **local** until spend approved; no AWS account provision / no AWS resources in PoC Stories. Any provision → escalate CA → CPM → COO → CEO.

### Constraints (must hold)

1. **No Strategy eval / LLM runtime in PoC** — Product OUT list; keeps PoC a pure domain + API proof.
2. **1:1 Negotiation only** — PRODUCT-BRIEF early addition **13**; no multi-party graph.
3. **Offer lifecycle is the hard core** — Accept / Decline / Counter / Close must be correct and concurrent-safe enough for two parties (early addition **3** concurrency rules can stay minimal: one open offer per side or exclusive negotiate flag — Spec later).
4. **Close cancels open offers** — CEO original + PRODUCT-BRIEF Negotiation rules.
5. **Do not pull MotorMarket/DC4** schemas, feeds, or auth — CEO later decision #3 / PRODUCT-BRIEF relationship section.
6. **Intermediary posture** — no settlement/escrow tables in PoC (claims lock + early addition **12**).

### Tradeoffs

| Option | Pros | Cons | Pick |
|--------|------|------|------|
| **A. Modular monolith (.NET)** | One deployable; simplest; clear domain modules (Artifacts, Negotiations, Identity) | Less isolation than microservices | **Recommended** |
| B. Split API + worker early | Scales AI later | Overkill for PoC; invents ops cost | Defer |
| C. Non-.NET (Node/Go) | Faster prototype for some | Conflicts Product/CEO prefer-.NET | Reject unless CEO revises |

---

## 2. Recommended PoC boundaries — data model vs API surface

Feasibility only (not a Spec).

### 2a. Data model — IN vs DEFERRED

| Concept | PoC | Notes / Product anchor |
|---------|-----|------------------------|
| **Artifact** with Subject (name, description, properties name/type/value, facts strings), Intent, Value (amount + currency; 0..n, ≤1 clear per currency), Location 0..n, Time periods 0..n | **IN** | CEO original Artifact 1–5; PRODUCT-BRIEF Core model |
| **Participant** register + auth principal | **IN (minimal)** | CEO: must be registered |
| **Negotiation** 1:1, complementary intents, optional start/end | **IN** | CEO Negotiation; MVP 1:1 lock |
| **Offer** place + Accept / Decline / Counter / Close; open offers cancelled on Close | **IN** | CEO offer rules |
| **Identity-seal stub** (no public contact; sealed flag / placeholder) | **IN (stub)** | CEO #7 / PRODUCT-BRIEF AI Assistant identity rule — stub only in PoC |
| Contact exchange fields / PII vault | **DEFERRED → MVP+** | Contact on accept = MVP (Product stage ack); mature vault = later (early addition **9**) |
| Strategy entity / free-form rules | **DEFERRED → MVP (minimal) / V1 (fuller)** | PoC OUT |
| AI Assistant session / LLM meters | **DEFERRED → MVP thin** | PoC OUT; meters early addition **8** when Assistant appears |
| Saved search / market monitoring | **DEFERRED → V1** | PoC OUT; Participant capability #2 split per Product stage ack |
| Matching as first-class (early addition **1**) | **DEFERRED → V2** | Product stage ack / early addition **1** |
| Multi-party / multi-Artifact | **DEFERRED → V2** | Early addition **13** |
| Platform-owner admin, SSO, analytics, notifications breadth | **DEFERRED → V1+** | Original platform-owner list; not PoC |
| Settlement / escrow | **OUT (standing)** unless Product adds later | Claims lock + early addition **12** |

### 2b. API surface — IN vs DEFERRED

| Surface | PoC | Notes |
|---------|-----|-------|
| Auth: register / login (or issue token) | **IN** | Minimal |
| Artifact create/read (seed or owner-scoped) sufficient to attach a Negotiation | **IN** | Full CRUD can wait for MVP if create+get prove model — **prefer create/get/list-own in PoC** so MVP CRUD is extension not rewrite |
| Negotiation create (1:1), get, close | **IN** | |
| Offer create, accept, decline, counter | **IN** | Idempotency keys **optional in PoC**, **required MVP** (early addition **14**) |
| Identity: public DTOs omit contact; accept path does **not** release contact in PoC | **IN** | Stub behavior |
| Instant search | **DEFERRED → MVP** | |
| Strategy CRUD | **DEFERRED → MVP (minimal)** | |
| AI Assistant endpoints | **DEFERRED → MVP (thin)** | |
| Saved search | **DEFERRED → V1** | |
| Webhooks / published OpenAPI / MCP | **DEFERRED** — OpenAPI/webhooks V1; MCP later (early addition **7** + Product stage ack) | |
| Notifications fan-out | **DEFERRED → V1** | Participant capability #5 |
| Admin / SSO / multi-LLM | **DEFERRED → V1+** | |

### Tradeoff — Artifact API depth in PoC

| Option | Pros | Cons | Pick |
|--------|------|------|------|
| **A. Create + Get + List-own in PoC; full Update/Delete in MVP** | Proves S/I/V/L/T persistence; small surface | Slightly less than “CRUD” wording | Acceptable |
| **B. Full Artifact CRUD in PoC** | Matches MVP add-on early; less churn | Slightly larger PoC | **Recommended if cheap** (same aggregate) |

**Recommendation:** Prefer **B** if incremental cost is low (same aggregate root) — still no discovery/search in PoC.

---

## 3. MVP add-ons — hard architecture blockers before V1?

**No hard blockers that prevent starting V1 design**, if MVP stays thin and 1:1. Soft risks below must be designed for in MVP so V1 does not rewrite the core.

| MVP add-on | Hard blocker before V1? | Architecture guidance |
|------------|-------------------------|------------------------|
| Artifact CRUD | **No** | Same aggregate as PoC; complete REST verbs |
| Instant search | **No** (if scoped) | Start with DB filtered queries on Intent + key Subject fields; **do not** build search platform / saved-search monitors in MVP |
| Initiate negotiate/offers | **No** | Already PoC core; MVP = productized initiation UX/API |
| Minimal Strategy CRUD | **Soft risk, not hard blocker** | Persist structured/minimal strategy documents; **do not** implement full free-form NLP eval in MVP — defer richest free-form / sandbox (early addition **5**) |
| Thin AI Assistant | **Soft risk** | Feature-flag Assistant; call out-of-process LLM behind interface; enforce **hard budgets** when LLM appears (early addition **8**) — without budgets, cost risk escalates via COO → CEO |
| Identity-until-accept + contact on accept | **No** if seal boundary clear | PoC stub → MVP: release contact **only** on Accept; keep seal as domain invariant (early addition **9** light) |
| Idempotent offers + audit + OTel hooks | **No** | Add idempotency keys on offer writes (early addition **14**); append-only audit for offer decisions (early addition **2**); OTel spans on negotiate path (early addition **11**) — hooks in MVP, mature dashboards later |

### What would become a hard blocker (avoid)

1. Building **full free-form Strategy engine + sandbox** in MVP → delays V1; Product maps fuller Strategy to V1+.
2. Treating **saved-search / complementary matching** as MVP-complete → conflicts Product stage ack (saved-search V1; matching early addition **1** → V2).
3. **Multi-party** Negotiation schema in MVP → conflicts early addition **13**.
4. Coupling core to **MotorMarket/DC4** → CEO non-goal.
5. Shipping Assistant **without** meter/budget cutoffs → cost/critical path (COO/CEO), not a domain impossibility but an ops blocker.

---

## 4. Risks / deps that force stage moves

Cite only IDs known in Product docs; otherwise say unknown.

| Risk / dependency | Forces move? | Evidence | Stage note |
|-------------------|--------------|----------|------------|
| Full free-form Strategy + sandbox (early addition **5**) | Keep out of MVP | PRODUCT-BRIEF early additions; Product stage ack (minimal Strategy MVP; fuller V1; sandbox later) | Stay V1+ / later |
| Saved search / market monitoring | Not PoC/MVP-complete | Participant capability #2; Product stage ack → V1 | V1 |
| First-class complementary matching (early addition **1**) | Not MVP | PRODUCT-BRIEF early addition **1**; Product stage ack → V2 | V2 |
| Multi-party / multi-Artifact (early addition **13**) | Not MVP/PoC | PRODUCT-BRIEF “MVP is strictly 1:1; … v2” | V2 |
| Mature PII vault retention/erasure (early addition **9**) | Contact-on-accept can ship lighter in MVP | PRODUCT-BRIEF early addition **9**; Product stage ack (seal MVP / mature vault later) | Mature → post-MVP (often V3 in PM drafts — **V3 label not in Product KB**; treat as “post-MVP vault maturity”) |
| Webhooks + OpenAPI + MCP (early addition **7**) | Do not block MVP on MCP/public OpenAPI | PRODUCT-BRIEF early addition **7**; Product stage ack (OpenAPI/webhooks V1; MCP later) | V1 / later |
| SSO (platform-owner #9) | Not PoC | CEO/PRODUCT-BRIEF platform-owner list | V1+; keep OIDC-shaped principal in PoC |
| BYO model / multi-LLM (Participant #9 / platform-owner #8) | Not MVP-thin | PRODUCT-BRIEF | Later Vn — **exact Vn IDs unknown in Product KB** |
| AWS prod hardening / hosted platform ops | PoC = **local** + ECS Express Mode **sketch** only until spend approved; no account provision in PoC Stories | CEO lock via CA/Bot Manager; PRODUCT-BRIEF hosted remains AIKnowHow | Prod deploy ≠ PoC success criteria; App Runner not recommended |
| Formal `D*` / `P*` / `O*` capability catalog | **Unknown in Product KB** | Not present in CEO-ORIGINAL / PRODUCT-BRIEF files | Do not invent; if PM matrix needs IDs, Product triad must publish them |

### Escalation flags (no Product conflict found that blocks PoC YES)

- **None** between CA-stated PoC/MVP thin slice and PRODUCT-BRIEF core + early additions **13**, **2**, **11**, **14**, **9** (light), **8** (when Assistant appears).
- If PM roadmap assigns **saved-search** or **full Strategy sandbox** into MVP, flag to Chief Architect → PM → Product → CEO (stage boundary conflict).
- Cost: LLM Assistant without budgets → COO → CEO before spend.

---

## Recommended decision (summary)

1. **PoC as simple .NET modular monolith: YES.**
2. **PoC IN:** Artifact S/I/V/L/T (+ prefer full CRUD if cheap), Participant minimal auth, 1:1 Negotiation, Offer Accept/Decline/Counter/Close, identity-seal stub, minimal API; AWS shape only.  
   **PoC DEFER:** Strategy/AI, search/discovery, contact release, webhooks/OpenAPI/MCP, admin/SSO/analytics.
3. **MVP add-ons:** No hard architecture blockers before V1 if Strategy stays minimal, search stays instant-only, Assistant is thin + budgeted, seal→contact on Accept, and idempotent/audit/OTel hooks land.
4. **Stage-move risks:** Use PRODUCT-BRIEF early addition numbers **1, 5, 7, 9, 13** (+ platform-owner SSO/LLM) as cited above; **D*/P*/O* unknown in Product KB** — do not invent.

## Done-list (for Architecture QA)

- [ ] File path/name matches DOC-FLOW (`architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md`)
- [ ] §1 answers yes/no + constraints with .NET / AWS-shape evidence from Product sources
- [ ] §2 lists data model IN vs DEFERRED and API IN vs DEFERRED aligned to Product-locked PoC OUT list
- [ ] §3 states MVP hard blockers (none) vs soft risks with early-addition citations
- [ ] §4 risks cite only known Product-doc IDs / numbers; explicitly marks D*/P*/O* unknown
- [ ] No invented Stories, requirements, or capability IDs
- [ ] Product conflicts: none blocking; escalation path noted if PM expands MVP

**Next:** Architecture QA verifies vs CA brief + Product sources; confirm to Chief Architect only (never skip to PM).
