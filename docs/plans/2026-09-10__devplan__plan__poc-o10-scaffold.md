# Dev Plan — PoC O10 .NET scaffold + minimal API host

**Status:** Senior Dev Planner draft (Security Dev Plan-step points woven; ready for Dev Plan QA after Security QA confirm path)  
**Date:** 2026-09-10  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner — PRIORITY PoC #3 O10  
**Issue:** https://github.com/ioaikh/dealoware/issues/3  
**Capability:** O10  
**DOC-FLOW path:** `plans/2026-09-10__devplan__plan__poc-o10-scaffold.md`  
**Constraints:** No product code in this artifact beyond SD instructions; no MM/DC4; no AWS spend; no invent Stories; Product conflicts → PM → Product → CEO; cost/critical → CPM

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Spec (binding) | `specs/2026-09-10__spec__spec__poc-o10-scaffold.md` | Layout, health, README, AWS shape, Security §5, OUT §6, AC mapping |
| Spec Security QA PASS | `verification/2026-09-10__security__verification__poc-o10-spec-qa-confirm.md` | Spec-step points 1–9 MET — binding unlock for Dev Plan |
| Architecture | `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md` | Option A layout, Minimal APIs, ECS Express Mode sketch |
| Dev Plan Security checklist | `verification/2026-09-10__security__verification__poc-o10-devplan-checklist.md` | Dev Plan-step handshake points **1–10** (must weave) |
| Product brief | `product/PRODUCT-BRIEF.md` | O10 / platform-owner #10 alignment only |
| Issue #3 | https://github.com/ioaikh/dealoware/issues/3 | Binding AC + OUT |

**Product alignment:** Platform-owner capability **#10** — simple, high-performance .NET core on AWS shape (`PRODUCT-BRIEF.md`). This Story = **host scaffold only** (O10). Conflicts → escalate PM → Product → CEO.

---

## Restated understanding (short)

Executable plan for **SD only**: Option A layout (`Dealoware.sln` + `src/Dealoware.Api` only runnable + `Dealoware.Domain` + optional empty Application/Infrastructure); Minimal APIs **`GET /health` only** → `200` + `{"status":"ok"}`; README local run; README **Amazon ECS Express Mode** sketch / local **$0**; secrets hygiene + zero MM/DC4 + inert placeholders. **OUT** as Spec §6. No product code in this plan artifact — instructions for SD only.

---

## Locked decisions (Spec locks 1–6)

| # | Decision | Spec lock (binding) | Plan steps |
|---|----------|---------------------|------------|
| 1 | Solution layout | `Dealoware.sln` + `src/Dealoware.Api` (**only runnable**) + `Dealoware.Domain` + optional empty `Application` / `Infrastructure` (**Option A**) | Steps 1–2 |
| 2 | API | .NET 8+ **Minimal APIs**; **only** `GET /health` → `200` + `{ "status": "ok" }` (Auth: **none**; no DB/Redis/AWS/network deps) | Step 3 |
| 3 | Docs | README local run: SDK 8+, restore, `dotnet run --project src/Dealoware.Api`, curl health, expect 200 | Step 5 |
| 4 | AWS | README callout = **Amazon ECS Express Mode** (Fargate) sketch; PoC **local / $0**; **no** App Runner; **no** AWS provision; optional local Dockerfile sketch only | Steps 6–7 |
| 5 | Security binding | No secrets/API keys/cloud creds/real connection strings in `appsettings*` / README / Dockerfile / launchSettings / comments; placeholders/env-only; zero MM/DC4; placeholder libs inert (no auth/LLM/payment SDKs) | Steps 2, 4, 8–9 |
| 6 | OUT | Prod AWS; Strategy; AI; platform-owner suite; multi-party; settlement; discovery/search; domain Stories #4–#7 (empty libs only) | Step 8 |

---

## Itemized SD execution steps (numbered, runnable)

**Repo root:** `ioaikh/dealoware` (place `src/` under repo root unless existing convention is already documented — Spec §1).

### Step 1 — Repo layout (Option A)

Create solution and projects under repo root:

```text
Dealoware.sln
src/
  Dealoware.Api/                 # ASP.NET Core Minimal APIs — ONLY runnable
  Dealoware.Domain/              # class lib placeholder (empty or marker)
  Dealoware.Application/         # optional empty class lib
  Dealoware.Infrastructure/      # optional empty class lib
tests/
  Dealoware.Api.Tests/           # optional smoke tests
README.md
```

**Commands (illustrative; SD may use equivalent `dotnet` CLI):**

1. `dotnet new sln -n Dealoware -o .` (or create `Dealoware.sln` if not present)
2. `dotnet new web -n Dealoware.Api -o src/Dealoware.Api --framework net8.0` (Minimal APIs template; strip default WeatherForecast / controllers if present)
3. `dotnet new classlib -n Dealoware.Domain -o src/Dealoware.Domain --framework net8.0`
4. Optional: `dotnet new classlib -n Dealoware.Application -o src/Dealoware.Application --framework net8.0`
5. Optional: `dotnet new classlib -n Dealoware.Infrastructure -o src/Dealoware.Infrastructure --framework net8.0`
6. Optional: `dotnet new xunit -n Dealoware.Api.Tests -o tests/Dealoware.Api.Tests --framework net8.0`
7. `dotnet sln Dealoware.sln add src/Dealoware.Api/Dealoware.Api.csproj src/Dealoware.Domain/Dealoware.Domain.csproj` (+ optional Application/Infrastructure/Tests)

**Acceptance:**

- [ ] `Dealoware.sln` exists at repo root
- [ ] `src/Dealoware.Api` is the **only** runnable project (no second host / worker / BFF / gateway)
- [ ] `src/Dealoware.Domain` exists (empty or marker)
- [ ] Application / Infrastructure present only as empty placeholders if created
- [ ] No MotorMarket / DC4 project names or paths

### Step 2 — Project references / PackageReference rules

**References (allowed):**

- `Dealoware.Api` → `Dealoware.Domain` (required when Domain exists)
- `Dealoware.Api` → `Dealoware.Application` / `Dealoware.Infrastructure` only if those projects exist
- Tests → `Dealoware.Api` (if tests project exists)
- Domain / Application / Infrastructure: **no** cross-refs that pull secret/exfil-risk SDKs

**Forbidden in O10 (all projects):**

- Any MotorMarket / DC4 project references, package names, or shared libraries
- PackageReferences for **auth**, **LLM**, **payment**, or other secret/exfil-risk SDKs on Domain / Application / Infrastructure
- Extra IoC containers; use built-in ASP.NET Core DI only
- Mixing controllers with Minimal APIs in the scaffold

**Acceptance:**

- [ ] `dotnet restore` succeeds on `Dealoware.sln`
- [ ] Grep/search: no MM/DC4 package or project refs
- [ ] Placeholder libs have **no** auth/LLM/payment PackageReferences (inert)

### Step 3 — Implement Minimal API `GET /health`

In `src/Dealoware.Api` only:

- Use .NET 8+ **Minimal APIs** (e.g. in `Program.cs`)
- Map **`GET /health`** → HTTP **`200`** + JSON body **`{ "status": "ok" }`**
- **Auth: none** on this endpoint
- Must **not** require DB, Redis, AWS, or outbound network to return 200
- O10 endpoint surface = **health only** — do **not** add Artifact, auth, negotiate, search, Strategy, admin, SSO, or other public routes
- `/ping` may alias `/health` only if SD chooses; Spec does **not** require both — do not invent metrics/readiness splits

**Acceptance:**

- [ ] `curl` to `/health` returns `200` and `{"status":"ok"}` (or equivalent JSON with those fields)
- [ ] No auth middleware required for health
- [ ] Health works with no DB/Redis/AWS/network configured
- [ ] No other application routes beyond health (and optional `/ping` alias)

### Step 4 — Config: appsettings + launchSettings

- Keep `appsettings.json` / `appsettings.Development.json` and `Properties/launchSettings.json`
- Illustrative keys = **placeholders / env-only** patterns only
- **Must contain no:** secrets, API keys, cloud credentials, real connection strings (in appsettings*, README, Dockerfile, launchSettings, comments)
- **Must contain no:** MotorMarket / DC4 connection strings, schemas, feeds, SFTP, or shared-DB keys

**Acceptance:**

- [ ] Secrets hygiene scan of `appsettings*`, launchSettings, comments — clean
- [ ] No MM/DC4 config keys

### Step 5 — README local run steps

Document in repo `README.md` (minimum; Spec §3):

1. Prerequisites: **.NET SDK 8+** installed
2. `dotnet restore` on `Dealoware.sln`
3. `dotnet run --project src/Dealoware.Api`
4. `curl` (or browser) to the health URL (port from launchSettings or console output)
5. Expected: **HTTP 200** + `{ "status": "ok" }`

Optional: `dotnet test` if `tests/Dealoware.Api.Tests` exists.

**Acceptance:**

- [ ] README contains all five steps above
- [ ] Following README on a clean machine with SDK 8+ yields health 200

### Step 6 — README AWS callout (shape only; no provision)

Add a short README section:

- Target host shape = **Amazon ECS Express Mode** (Fargate)
- **Not** deployed by this Story
- PoC stays **local / $0** until spend approved
- **Do not** recommend **App Runner**
- **No** AWS account provision / **no** AWS resource create tasks in this Story
- If spend ever proposed → escalate **CPM → COO → CEO** (O10 must not procure)

**Acceptance:**

- [ ] README names **ECS Express Mode**; excludes App Runner
- [ ] README states local / $0; no provision instructions that create spend

### Step 7 — Optional Dockerfile (local sketch only)

If SD adds `Dockerfile` at repo root:

- Multi-stage build for **local** `docker build` / `docker run` sketch only
- **Out of O10 delivery:** prod IAM roles, Secrets Manager wiring, ECR promo, image signing
- Same secrets hygiene as Step 4 (no secrets/creds in Dockerfile)

**Acceptance:**

- [ ] If present: documented as local sketch only; not a prod deploy path
- [ ] If absent: OK (optional)

### Step 8 — Explicit OUT checklist (do **not** implement Spec §6)

SD must **not** deliver any of:

| OUT | Note |
|-----|------|
| Prod AWS deploy | Shape/sketch only |
| App Runner | Excluded from greenfield |
| AWS account / resource provision | Local / $0 until spend escalate |
| Strategy engine | No strategy packages/projects |
| AI Assistant / LLM SDKs | Forbidden in placeholder libs |
| Platform-owner suite | No admin/users/roles/SSO surfaces |
| Multi-party / multi-Artifact | Later |
| Settlement / checkout / escrow | No payment modules |
| Discovery / search | No search endpoints or indexes |
| Domain Stories #4–#7 | Empty libs only |
| MotorMarket / DC4 | Zero dependencies |

### Step 9 — Self-verify checklist for SD before handoff

Before marking Story ready for CQ / handoff, SD verifies:

- [ ] Option A layout; Api only runnable
- [ ] `GET /health` → 200 + `{"status":"ok"}`; Auth none; no external deps
- [ ] README local run works end-to-end
- [ ] README ECS Express Mode sketch; no App Runner; no AWS provision steps
- [ ] Optional Dockerfile = local only (or omitted)
- [ ] Secrets hygiene gate passed (Step 4)
- [ ] Zero MM/DC4 gate passed (Steps 1–2, 4)
- [ ] Placeholder libs inert (no auth/LLM/payment PackageReferences)
- [ ] Nothing from Step 8 / Spec §6 implemented
- [ ] No invent Stories; Product conflicts escalated if any

---

## Spec §§1–6 + AC mapping

| Spec / AC source | Content | Plan step(s) |
|------------------|---------|--------------|
| Spec §1 | Solution layout + project rules | Steps 1–2 |
| Spec §2 | Health contract | Step 3 |
| Spec §3 | Local run README | Step 5 |
| Spec §4 | AWS-shape callout + optional Dockerfile | Steps 6–7 |
| Spec §5 | Security Spec checklist binding (1–9) | Steps 2–9; Security table below |
| Spec §6 | Explicit OUT | Step 8 |
| Issue AC: Runnable .NET modular-monolith layout | Spec §1 | Steps 1–2 |
| Issue AC: Health / ping endpoint | Spec §2 | Step 3 |
| Issue AC: Documented local run steps | Spec §3 | Step 5 |
| Issue AC: No MotorMarket / DC4 live-system deps | Spec §1 rule 3; §5#6; §6 | Steps 1–2, 4, 8–9 |
| Issue AC: AWS = ECS Express Mode; local/$0; optional Dockerfile not prod | Spec §4; §5#4/#7 | Steps 6–7 |
| Spec §7 AC: Local-only host (no prod TLS/public/identity AC) | Spec §5#1 | Steps 1, 3, 5–6, 8–9 |

---

## Security Dev Plan-step binding

**Binding checklist:** `verification/2026-09-10__security__verification__poc-o10-devplan-checklist.md` (points **1–10**)  
**Upstream Spec Security QA PASS:** `verification/2026-09-10__security__verification__poc-o10-spec-qa-confirm.md` (points 1–9 MET)

| # | Security point | How plan addresses it | Plan section / SD step |
|---|----------------|----------------------|------------------------|
| 1 | **Local-only delivery** — milestones deliver a **local** runnable host only; no step that provisions public exposure, prod TLS, or identity as O10 done criteria | Steps deliver `dotnet run` + README local run only. Done criteria = local health 200. Prod TLS / public exposure / identity explicitly OUT (Step 8) and excluded from acceptance. | Steps 3, 5, 8–9; Locked #2/#6; Explicit OUT |
| 2 | **Health-only surface** — implement + verify `GET /health` Auth:None as **sole** O10 route; exclude auth/SSO/admin/other public routes | Step 3 implements only `GET /health` (Auth none); forbids Artifact/auth/negotiate/search/Strategy/admin/SSO routes. Step 8 OUT lists platform-owner suite / auth. | Steps 3, 8–9; Locked #2 |
| 3 | **Health zero external deps** — verify health returns 200 **without** DB, Redis, AWS, or outbound network | Step 3 acceptance: health works with no DB/Redis/AWS/network. Step 9 self-verify repeats this gate. | Steps 3, 9; Locked #2 |
| 4 | **Local / $0 + host-shape docs** — README: **ECS Express Mode** sketch; **App Runner** excluded; **no** AWS account/resource provision tasks; escalate if spend proposed | Step 6 README callout binds ECS Express Mode, excludes App Runner, forbids provision, documents escalate **CPM → COO → CEO**. Cost/critical section below. | Steps 6–7; Locked #4; Cost/critical |
| 5 | **Secrets hygiene gate** — before done: no secrets/API keys/cloud credentials/real connection strings in `appsettings*`, README, Dockerfile, launchSettings, comments; placeholders/env-only | Step 4 config rules + acceptance scan; Step 7 Dockerfile hygiene; Step 9 self-verify secrets gate. | Steps 4, 7, 9; Locked #5 |
| 6 | **Zero MM/DC4 gate** — verify no MM/DC4 project refs, packages, config keys, schemas, feeds, SFTP, shared DB | Steps 1–2 forbid MM/DC4 refs/packages; Step 4 forbids MM/DC4 config; Step 8 OUT; Step 9 verify gate. | Steps 1–2, 4, 8–9; Locked #5/#6 |
| 7 | **Dockerfile optional/local** — if planned: local `docker build`/`run` sketch only; **out:** prod IAM, Secrets Manager, ECR promo, signing | Step 7 marks Dockerfile optional local sketch; lists prod IAM/Secrets Manager/ECR/signing as out. | Step 7; Locked #4 |
| 8 | **Inert placeholders** — forbid auth/LLM/payment (or other secret/exfil-risk) PackageReferences on Domain/Application/Infrastructure | Step 2 PackageReference rules + acceptance; Step 8 OUT AI/settlement; Step 9 self-verify inert libs. | Steps 2, 8–9; Locked #5 |
| 9 | **No security-scope creep** — do not schedule AWS provision, MotorMarket integration, or new security Stories; cite Spec Security PASS as binding input | Sources table cites Spec Security QA PASS; Steps 6–8 schedule no AWS provision / no MM / no new security Stories; Constraints header. | Sources; Steps 6–8; Constraints; Explicit OUT |
| 10 | **Security handshake close** — Dev Plan QA must not PASS until Security QA confirms these points (or Chief Security marks HOLD with evidence) | See handshake note below. | Handshake note; Done-list for Dev Plan QA |

### Handshake note (point 10)

**Dev Plan QA must ask Security QA to confirm Dev Plan-step points 1–10** (this table) with evidence **before** PASS to Chief Dev Planner. Do not skip Chief. If Security QA HOLDs, stop — do not unlock SD.

---

## Explicit OUT

Mirror Spec §6 / issue #3 Out of scope — SD must not implement:

| OUT | Note |
|-----|------|
| Prod AWS deploy | Shape/sketch only |
| App Runner | Excluded from greenfield |
| AWS account / resource provision | Local / $0 until spend escalate |
| Strategy engine | No strategy packages/projects |
| AI Assistant / LLM SDKs | Not required; forbidden in placeholder libs |
| Platform-owner suite | No admin/users/roles/SSO surfaces |
| Multi-party / multi-Artifact | Later; MVP 1:1 when domain lands |
| Settlement / checkout / escrow | Claims lock — no payment modules |
| Discovery / search | No search endpoints or indexes |
| Domain Stories #4–#7 | Artifact CRUD, Participant auth, Negotiation/Offers, identity-seal — empty libs only |
| MotorMarket / DC4 | Zero dependencies |

---

## Cost/critical

**O10 must not procure AWS.** Any paid AWS provision or spend proposal → escalate **CPM → COO → CEO**. PoC remains **local / $0** until that path clears. This plan schedules **no** AWS account create, IaC apply, or resource provision tasks.

---

## Done-list for Dev Plan QA

- [ ] Path/name DOC-FLOW: `plans/2026-09-10__devplan__plan__poc-o10-scaffold.md`
- [ ] Spec coverage §§1–6 + issue AC mapped to plan steps
- [ ] Itemized steps executable by SD without inventing requirements
- [ ] Product alignment O10 / platform-owner #10 (`PRODUCT-BRIEF.md`); issue #3
- [ ] **Security Dev Plan-step points 1–10 all woven** with cites (table above)
- [ ] No invented Stories / requirements
- [ ] No MotorMarket / DC4
- [ ] No AWS procure / spend instructions
- [ ] Explicit OUT respected (Spec §6)
- [ ] No product code in this artifact (SD instructions only)
- [ ] **Security QA confirm required** on points 1–10 **before** Dev Plan QA PASS to Chief Dev Planner

**Next:** Dev Plan QA verifies with evidence → ask Security QA confirm Dev Plan-step 1–10 → Dev Plan QA confirm to **Chief Dev Planner only** (never skip Chief). SD starts only after Chief Dev Planner unlock.

---

## Done-list for SD (after plan QA PASS + Chief confirm)

- [ ] Step 1: Create `Dealoware.sln` + Option A projects (`src/Dealoware.Api` only runnable + Domain + optional empty App/Infra + optional tests)
- [ ] Step 2: Wire project references; enforce PackageReference rules (inert placeholders; no auth/LLM/payment SDKs; no MM/DC4)
- [ ] Step 3: Implement Minimal API `GET /health` → `200` + `{"status":"ok"}`; Auth none; no external deps; no other routes
- [ ] Step 4: Config placeholders/env-only; secrets hygiene; no MM/DC4 keys
- [ ] Step 5: README local run (SDK 8+, restore, run Api, curl health, expect 200)
- [ ] Step 6: README AWS callout — ECS Express Mode sketch; local/$0; no App Runner; no provision
- [ ] Step 7: Optional Dockerfile = local sketch only (or omit)
- [ ] Step 8: Do not implement Spec §6 OUT
- [ ] Step 9: Complete self-verify checklist before handoff
- [ ] Hand off to CQ gate (never skip CQ)
