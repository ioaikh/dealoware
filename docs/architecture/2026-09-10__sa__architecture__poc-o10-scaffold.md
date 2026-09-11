# Architecture options — PoC O10 .NET scaffold + minimal API host

**Status:** Senior Architect options for Architecture QA (Story work — real, not dry-run). **Amended 2026-09-10:** Security answers §7 (always-critical handshake).  
**Date:** 2026-09-10  
**Author:** Dealoware Senior Architect  
**Brief:** Chief Architect — GitHub issue #3 · capability **O10** only  
**Issue:** https://github.com/ioaikh/dealoware/issues/3  
**DOC-FLOW:** `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md`

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #3 AC + OUT | https://github.com/ioaikh/dealoware/issues/3 | Binding acceptance + out of scope |
| CEO original | `product/CEO-ORIGINAL-BRIEF.md` | Platform-owner #10 — prefer .NET, AWS host |
| Product summary | `product/PRODUCT-BRIEF.md` | Same #10; MotorMarket/DC4 separation; claims lock |
| Prior SA feasibility | `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` | PoC .NET modular monolith **YES** |
| Prior Architecture QA | `verification/2026-09-10__sa__verification__poc-feasibility-roadmap.md` | **PASS** |
| Release roadmap (stage only) | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | **O10** primary at PoC; continues all stages |
| Security SA checklist (Chief Security) | `verification/2026-09-10__security__verification__poc-o10-sa-checklist.md` | Architecture always-critical handshake — points 1–8 |

**Scope of this Story:** **O10 only** — solution scaffold + minimal API host + health/ping + local run docs + AWS-shape callout. Domain Stories (#4–#7 Artifact, auth, Negotiation, identity-seal) are **out** of this deliverable (issue: first PoC Story; unblocks them).

---

## 0. Prior feasibility — still valid for O10?

**YES — no deltas that invalidate the prior PASS.**

| Prior recommendation | Still valid for #3 / O10? | Note |
|----------------------|---------------------------|------|
| Modular monolith (.NET 8+) | **Yes** | Issue AC: modular-monolith shape |
| Minimal APIs (or thin controllers) | **Yes** | Prefer Minimal APIs for scaffold (see §2) |
| AWS = target host shape, not prod | **Yes** (named: **ECS Express Mode**; local until spend) | Issue AC + OUT; CEO lock — no App Runner; no AWS provision in PoC Stories |
| No MotorMarket/DC4 deps | **Yes** | Issue AC |
| Persistence / domain modules | **Defer wiring** | O10 = host + layout; EF/domain land with later Stories — keep **empty module folders or class-lib placeholders** only if Spec/SD need seams; do not invent domain endpoints |

**Delta vs feasibility doc:** Feasibility covered full PoC thin slice. This Story is **host-only**. Do not implement Artifact/Negotiation/Offer/auth APIs here.

---

## 1. Recommended solution layout (projects / modules)

**Pick: single solution, modular-monolith folders, one runnable host.**

```text
src/
  Dealoware.Api/                 # ASP.NET Core host (Minimal APIs) — ONLY runnable project for O10
  Dealoware.Domain/              # class lib placeholder (empty or marker) — no domain logic required in O10
  Dealoware.Application/         # class lib placeholder (optional for O10; can add when #4+ land)
  Dealoware.Infrastructure/      # class lib placeholder (persistence/AWS adapters later)
tests/
  Dealoware.Api.Tests/           # smoke: host starts + health returns 200 (optional but recommended)
Dealoware.sln
Dockerfile                       # optional sketch — multi-stage build; NOT prod deploy
README.md                        # local run steps (AC)
```

### Layout rules

1. **One deployable** for PoC: `Dealoware.Api` references Domain (and Application/Infrastructure when non-empty).
2. **No second host** (no worker, no BFF, no gateway) in O10.
3. **No MotorMarket / DC4** project references, package names, connection strings, or shared libraries.
4. Repo root may already be `ioaikh/dealoware` — place `src/` under repo root unless Spec/SD document an existing convention; do not scatter projects at unrelated paths.
5. **Placeholder libs stay inert** — Domain/Application/Infrastructure must not add PackageReferences for auth, LLM, payment, or other secret/exfil-risk SDKs in O10 (Security §8).

### Tradeoffs — layout

| Option | Pros | Cons | Pick |
|--------|------|------|------|
| **A. Api + Domain (+ empty App/Infra libs)** | Clear seams for #4–#7; matches prior modular-monolith YES | Slight empty-project noise | **Recommended** |
| B. Single Api project only | Absolute minimum files | Refactors when domain lands | Acceptable fallback if SD wants fewer projects |
| C. Microservices / multiple hosts | Isolation | Violates simplest maintainable + issue modular-monolith | **Reject** |

---

## 2. Host / API shape

| Choice | Recommendation | Rationale |
|--------|----------------|-----------|
| Runtime | **.NET 8** (LTS) or newer LTS available at implement time | CEO/Product platform-owner #10; prior SA |
| Style | **Minimal APIs** in `Program.cs` (or `Endpoints/` static classes if Spec prefers) | Smallest host; OpenAPI package not required for O10 |
| Controllers | Not required for O10 | Thin controllers OK later if team prefers; do not mix both styles in scaffold |
| DI | Built-in ASP.NET Core container | No extra IoC |
| Config | `appsettings.json` + env vars. **Binding (Security §5):** O10 artifacts (`appsettings*`, README, Dockerfile, launchSettings, comments) must contain **no** secrets, API keys, cloud credentials, or real connection strings; any illustrative config keys use **placeholders / env-only** patterns. Also no MM/DC4 connection strings. | Local-first |
| HTTPS | `dotnet run` defaults / launchSettings — fine for local | Prod TLS = later / DevOps |

**O10 endpoint surface:** health/ping **only** (plus implicit framework endpoints if any). No Artifact, auth, negotiate, search, Strategy, or admin routes.

**Secrets hygiene (binding):** O10 artifacts (`appsettings*`, README, Dockerfile, launchSettings, comments) must contain **no** secrets, API keys, cloud credentials, or real connection strings; any illustrative config keys use **placeholders / env-only** patterns.

---

## 3. Health / ping contract

**Purpose:** Prove the host is runnable (issue AC). Not platform-owner full health suite (**O5** in roadmap is MVP+ mature boards — do not expand O10 into O5).

| Item | Contract |
|------|----------|
| Method / path | `GET /health` **or** `GET /ping` — **pick one canonical**; document it in README |
| Success | `200` with a small JSON body, e.g. `{ "status": "ok" }` (or plain `ok` text — Spec picks one; keep stable) |
| Auth | **None** for this endpoint in O10 |
| Dependencies | Must **not** require DB, Redis, AWS, or external network to return 200 |
| Failure | Process down → connection failure (no fancy degraded states required in O10) |

**Recommendation:** `GET /health` → `200` + `{ "status": "ok" }`. If both `/health` and `/ping` are desired, make `/ping` an alias only — do not invent metrics/readiness splits in O10.

### Tradeoffs — health depth

| Option | Pros | Cons | Pick |
|--------|------|------|------|
| **A. Liveness-only JSON** | Meets AC; no DB coupling | Not “deep” health | **Recommended for O10** |
| B. AspNetCore.HealthChecks + DB | Closer to prod | Invents persistence before #4+; fails AC spirit if DB required | **Defer** |
| C. Full O5 dashboards | — | Out of issue OUT / wrong capability | **Reject** |

---

## 4. Local run + AWS-shape callout (not prod)

### Local run (AC — must document in repo README)

Minimum documented steps (Spec/SD may refine commands; architecture requires these exist):

1. Prerequisites: .NET SDK 8+ installed.
2. `dotnet restore` on the solution.
3. `dotnet run --project src/Dealoware.Api`
4. `curl` / browser hit to the health/ping URL (include port from launchSettings or console output).
5. Expected: HTTP 200 + documented body.

Optional: `dotnet test` if smoke test project exists.

### AWS host shape (target only — NOT prod deploy)

| In O10 | Out of O10 |
|--------|------------|
| Comment or short README section: “Target host shape = **Amazon ECS Express Mode** (Fargate); **not** deployed by this Story. PoC stays **local** until spend approved — no AWS account provision.” **Do not** recommend App Runner. | Real AWS accounts, IaC apply, ALB, prod IAM, multi-env pipelines, App Runner |
| Optional `Dockerfile` multi-stage build that produces a runnable image **locally** (`docker build` / `docker run`) as a **sketch** | Prod image signing, ECR promo, secrets manager wiring, autoscaling |

Aligns to issue AC, prior SA, DevOps consult ack in release roadmap (PoC AWS = shape not prod), and **CEO lock via Bot Manager/CA:** named target = **Amazon ECS Express Mode** (Fargate) — `open`. **Do not** recommend App Runner (`existing-customers-only` + `no-new-features`).

**PoC remains local** until spend is approved; O10 (and PoC Stories) must **not** provision AWS accounts or create AWS resources.

**Cost/critical:** Any paid AWS provision → escalate Chief Architect → CPM → COO → CEO. O10 must not procure.
**Dockerfile / local-run secrets hygiene (binding):** O10 artifacts (`appsettings*`, README, Dockerfile, launchSettings, comments) must contain **no** secrets, API keys, cloud credentials, or real connection strings; any illustrative config keys use **placeholders / env-only** patterns. Optional Dockerfile remains a local sketch only (no prod IAM/Secrets Manager/ECR/signing as delivered).


### Cloud hosting currency check (`ops/ORG-OPS.md` mandatory)

Checked **2026-09-10** against official AWS docs (ORG-OPS enums: `open` | `existing-customers-only` | `no-new-features` | `deprecated/sunset`).

| Service | Status labels | Greenfield Dealoware? | Official check |
|---------|---------------|----------------------|----------------|
| **Amazon ECS Express Mode** (Fargate) | `open` | **Yes** — named sketch/target | [ECS Express Mode overview](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/express-service-overview.html) — available in Regions where Amazon ECS and Fargate are supported; no closed-to-new-customers or no-new-features notice on that page (fetched 2026-09-10). |
| **AWS App Runner** | `existing-customers-only` + `no-new-features` | **No** — excluded from greenfield options | [App Runner availability change](https://docs.aws.amazon.com/apprunner/latest/dg/apprunner-availability-change.html); product [End of support notice](https://aws.amazon.com/apprunner/) — no new customers from **2026-04-30**; existing customers continue; no new features planned; AWS recommends ECS Express Mode. |

**Checklist sources:** `ops/ORG-OPS.md` § Hosting / cloud currency; supporting DevOps lock `ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md` (CEO via CPM; no spend).

**PoC:** remains **local** until spend approved; no AWS account provision / no AWS resources in PoC Stories. Any provision → escalate CA → CPM → COO → CEO.


---

## 5. Explicit OUT / non-goals (match issue AC)

Do **not** deliver in O10:

| OUT (issue) | Architecture note |
|-------------|-------------------|
| Prod AWS deploy | Shape/sketch only |
| Strategy engine | No projects/packages for strategy eval |
| AI Assistant | No LLM SDK required for O10 |
| Platform-owner suite | No admin/users/roles/SSO surfaces |
| Multi-party / multi-Artifact | No domain model yet; when added later stay 1:1 until Product says otherwise |
| Settlement / checkout / escrow | Claims lock — no payment modules |
| Discovery / search | No search endpoints or indexes |

Also OUT for this Story (issue dependencies): Artifact CRUD, Participant auth beyond host, Negotiation/Offers, identity-seal — those are later PoC Stories; scaffold may reserve empty class libs only.

**MotorMarket / DC4:** Zero dependencies (schemas, feeds, auth, SFTP, shared DB). Evidence for QA: no project references or config keys pointing at MM/DC4.

---

## 6. Recommended decision (simplest maintainable)

1. **Solution:** `Dealoware.sln` with `Dealoware.Api` host + `Dealoware.Domain` (and optional empty Application/Infrastructure) — Option A.
2. **API:** Minimal APIs; **only** `GET /health` (JSON `{ "status": "ok" }`).
3. **Docs:** README local run steps meeting AC.
4. **AWS:** README callout = **ECS Express Mode** sketch + optional Dockerfile; PoC **local until spend**; **no** prod deploy / no AWS provision; **no** App Runner.
5. **Prior feasibility:** Still valid; O10 is the host slice of that YES.

---

## 7. Security answers (Architecture always-critical handshake)

**Checklist:** `verification/2026-09-10__security__verification__poc-o10-sa-checklist.md` (Chief Security).  
**Rule:** Architecture QA asks Security QA to confirm each point before PASS to Chief Architect. No invented product requirements; no AWS spend.

| # | Security point | Architecture answer | Cite |
|---|----------------|---------------------|------|
| 1 | Host trust boundary — local-only PoC; prod trust not delivered | O10 is a **local-only** runnable host (`dotnet run` / optional local Docker). Production trust (TLS termination, public exposure, identity) is **out of scope** and **must not** be implied as delivered by this Story. | §4 Local run; §4 AWS Out of O10; §5 OUT (Prod AWS deploy) |
| 2 | Unauthenticated health — liveness-only; no other public routes; no smuggled identity | `GET /health` has **Auth: None** and is **liveness-only** (Option A). **O10 endpoint surface = health/ping only** — no Artifact, auth, negotiate, search, Strategy, or admin routes. Auth/identity deferred to later Stories (#5+); must not be smuggled into this scaffold. | §2 endpoint surface; §3 Health contract (Auth / Recommendation); §5 OUT auth |
| 3 | Health has no external deps for 200 | Binding: health **must not** require DB, Redis, AWS, or outbound network to return 200. HealthChecks+DB and O5 dashboards are **Defer/Reject**. | §3 Dependencies row; §3 tradeoffs B/C |
| 4 | Local-until-spend / no provision; ECS Express sketch; App Runner excluded | PoC **local until spend**; **no** AWS account provision / resources in O10. Target sketch = **Amazon ECS Express Mode** (Fargate) status `open`. **App Runner** `existing-customers-only` + `no-new-features` — **excluded** from greenfield. Any provision → escalate CA → CPM → COO → CEO. | §4 AWS table + currency check; §6 decision #4 |
| 5 | Secrets hygiene | **Binding:** O10 artifacts (`appsettings*`, README, Dockerfile, launchSettings, comments) contain **no** secrets, API keys, cloud credentials, or real connection strings; any illustrative config keys use **placeholders / env-only**. Also no MM/DC4 connection strings. | §2 Config row; §1 layout rule 3; §5 MM/DC4; binding sentence below |
| 6 | Zero MM/DC4 coupling | **No** project references, package names, config keys, schemas, feeds, SFTP, or shared DB pointers to MotorMarket/DC4. | §1 layout rule 3; §5 MotorMarket / DC4; issue AC |
| 7 | Dockerfile sketch (if any) — local only | Optional `Dockerfile` is a **local** multi-stage build/run sketch only. **Out:** prod IAM roles, Secrets Manager wiring, ECR promo, image signing as delivered. | §1 Dockerfile comment; §4 AWS Out column |
| 8 | Placeholder libs inert | Domain / Application / Infrastructure placeholders must **not** pull auth, LLM, payment, or other secret/exfil-risk third-party SDKs in O10. Empty/marker class libs only until later Stories. | §1 layout + rule 5; §5 OUT Strategy/AI/settlement; §0 defer wiring |


**Binding (Security point 5):** O10 artifacts (`appsettings*`, README, Dockerfile, launchSettings, comments) must contain **no** secrets, API keys, cloud credentials, or real connection strings; any illustrative config keys use **placeholders / env-only** patterns.

## Done-list (Architecture QA)

- [ ] Path/name DOC-FLOW: `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md`
- [ ] Covers all issue #3 AC (runnable layout, health/ping, local run docs, no MM/DC4, AWS shape not prod)
- [ ] Explicit OUT matches issue Out of scope
- [ ] Tradeoffs with simplest maintainable pick
- [ ] Confirms prior feasibility PASS still valid (or documented deltas)
- [ ] O10-only — no invented domain/Strategy/AI/search requirements
- [ ] Spec can consume layout + health contract without guessing
- [ ] **§7 Security answers** map checklist points **1–8** with cites (source: `verification/2026-09-10__security__verification__poc-o10-sa-checklist.md`)
- [ ] Ask **Security QA** to confirm points 1–8 with evidence **before** PASS to Chief Architect

**Next:** Architecture QA verifies vs CA amend brief + issue AC + Security checklist; ask Security QA confirm; then confirm to Chief Architect only (never skip Chief).
