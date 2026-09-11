# Spec — PoC O10 .NET scaffold + minimal API host

**Status:** Senior Spec — Security retro amend (Spec-step handshake points 1–9); ready for Spec QA re-verify  
**Date:** 2026-09-10 (amended same day — Security Spec checklist)  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY PoC O10 Spec (#3); Security retro handshake  
**Issue:** https://github.com/ioaikh/dealoware/issues/3  
**Capability:** **O10** (platform-owner #10 — simple high-performance .NET core on AWS shape)  
**DOC-FLOW:** `specs/2026-09-10__spec__spec__poc-o10-scaffold.md`  
**Constraints:** No product code in this artifact. No MotorMarket/DC4. No AWS spend / no invent Stories / no SSO-admin-auth expansion. Cost/critical → escalate via COO → CEO (O10 must not procure). Dev Plan HOLD until Security QA PASS on Spec step.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Architecture (amended; §7 Security) | `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md` | Binding layout, health, local run, AWS shape, Security answers |
| SA Security checklist | `verification/2026-09-10__security__verification__poc-o10-sa-checklist.md` | Architecture handshake points 1–8 |
| SA Security QA PASS | `verification/2026-09-10__security__verification__poc-o10-sa-qa-confirm.md` | Points 1–8 MET (prior) |
| Spec Security checklist (binding this amend) | `verification/2026-09-10__security__verification__poc-o10-spec-checklist.md` | Spec-step handshake points **1–9** |
| SA amend verify PASS | `verification/2026-09-10__sa__verification__poc-o10-security-amend.md` | SA Security handshake cleared |
| Issue #3 AC + OUT | https://github.com/ioaikh/dealoware/issues/3 | Binding acceptance + out of scope |
| Product summary | `product/PRODUCT-BRIEF.md` | O10 / platform-owner #10 alignment only |

**Product alignment:** Platform-owner capability **#10** — simple, high-performance core; prefer **.NET**, hosted on **AWS** (`PRODUCT-BRIEF.md`). This Story delivers the **host scaffold only** (O10). Domain/Strategy/AI/platform-owner suite remain later Stories. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions (from SA §6 — Spec binds these)

| # | Decision | Spec lock |
|---|----------|-----------|
| 1 | Solution layout | `Dealoware.sln` + `src/Dealoware.Api` (**only runnable**) + `Dealoware.Domain` + optional empty `Application` / `Infrastructure` (**Option A**) |
| 2 | API | .NET 8+ **Minimal APIs**; **only** `GET /health` → `200` + `{ "status": "ok" }` (Auth: **none**; no DB/Redis/AWS/network deps) |
| 3 | Docs | README local run: SDK 8+, restore, `dotnet run --project src/Dealoware.Api`, curl health, expect 200 |
| 4 | AWS | README callout = **Amazon ECS Express Mode** (Fargate) sketch; PoC **local / $0** until spend; **no** App Runner; **no** AWS provision; optional local Dockerfile sketch only |
| 5 | Security binding | No secrets/API keys/cloud creds/real connection strings in `appsettings*` / README / Dockerfile / launchSettings / comments; placeholders/env-only; zero MM/DC4; placeholder libs inert (no auth/LLM/payment SDKs) |
| 6 | OUT | Prod AWS; Strategy; AI; platform-owner suite; multi-party; settlement; discovery/search; domain Stories #4–#7 (empty libs only) |

---

## 1. Solution layout + project rules

### Target tree

```text
src/
  Dealoware.Api/                 # ASP.NET Core host (Minimal APIs) — ONLY runnable project for O10
  Dealoware.Domain/              # class lib placeholder (empty or marker) — no domain logic required in O10
  Dealoware.Application/         # class lib placeholder (optional for O10)
  Dealoware.Infrastructure/      # class lib placeholder (optional for O10)
tests/
  Dealoware.Api.Tests/           # optional smoke: host starts + health returns 200
Dealoware.sln
Dockerfile                       # optional sketch — multi-stage; NOT prod deploy
README.md                        # local run steps (AC)
```

Place `src/` under repo root (`ioaikh/dealoware`) unless an existing convention is documented by Spec/SD — do not scatter projects at unrelated paths.

### Project rules (binding)

1. **One deployable** for PoC: `Dealoware.Api` is the **only** runnable project. It may reference Domain (and Application/Infrastructure when present).
2. **No second host** in O10 (no worker, BFF, or gateway).
3. **No MotorMarket / DC4** project references, package names, connection strings, or shared libraries.
4. **Placeholder libs stay inert** — Domain / Application / Infrastructure must **not** add PackageReferences for auth, LLM, payment, or other secret/exfil-risk SDKs in O10.
5. Controllers not required; do not mix Minimal APIs and controllers in the scaffold. Built-in DI only (no extra IoC).
6. Config: `appsettings.json` + env vars; illustrative keys = **placeholders / env-only** (Security §5).

### Fallback

Option B (single Api project only) is acceptable only if SD documents fewer projects; Spec **recommends Option A** per SA.

---

## 2. Health contract (canonical)

| Item | Contract |
|------|----------|
| Method / path | **`GET /health`** (canonical — do not also invent a separate metrics/readiness split in O10) |
| Success | **`200`** + JSON body **`{ "status": "ok" }`** |
| Auth | **None** |
| Dependencies | Must **not** require DB, Redis, AWS, or external/outbound network to return 200 |
| Alias | `/ping` may alias `/health` only if SD adds it; Spec does not require both |
| Failure | Process down → connection failure (no degraded health states required) |

**O10 endpoint surface = health only.** No Artifact, auth, negotiate, search, Strategy, or admin routes.

---

## 3. Local run acceptance steps (README must document)

Minimum README steps (Dev Plan / SD may refine ports/flags; these must exist):

1. Prerequisites: **.NET SDK 8+** installed.
2. `dotnet restore` on the solution (`Dealoware.sln`).
3. `dotnet run --project src/Dealoware.Api`
4. `curl` (or browser) to the health URL (port from launchSettings or console output).
5. Expected: **HTTP 200** + `{ "status": "ok" }`.

Optional: `dotnet test` if `Dealoware.Api.Tests` exists.

---

## 4. AWS-shape callout (ECS Express Mode; local / $0)

| In O10 | Out of O10 |
|--------|------------|
| README section: target host shape = **Amazon ECS Express Mode** (Fargate); **not** deployed by this Story. PoC stays **local / $0** until spend approved — **no** AWS account provision / **no** AWS resources. | Real AWS accounts, IaC apply, ALB, prod IAM, multi-env pipelines |
| **Do not** recommend **App Runner** | App Runner |
| Optional `Dockerfile` multi-stage as **local** `docker build` / `docker run` sketch | Prod image signing, ECR promo, Secrets Manager wiring, autoscaling |

**Cost/critical:** Any paid AWS provision → escalate Chief Spec / pipeline → CPM → COO → CEO. **O10 must not procure.**

---

## 5. Security Spec checklist binding (points 1–9)

**Binding Spec-step checklist:** `verification/2026-09-10__security__verification__poc-o10-spec-checklist.md`  
**Prior SA Security QA PASS:** `verification/2026-09-10__security__verification__poc-o10-sa-qa-confirm.md` (points 1–8 MET)  
**SA Security checklist (upstream):** `verification/2026-09-10__security__verification__poc-o10-sa-checklist.md`

Spec binds SA Security answers into implementable requirements for Dev Plan/SD. Spec consumers must keep these true. **No new security Stories. Do not expand O10 into SSO / admin / auth.**

| # | Security point | Spec requirement for SD | Spec section cites |
|---|----------------|-------------------------|--------------------|
| 1 | Trust boundary in AC | O10 deliverable is a **local-only** runnable host. Prod TLS termination, public exposure, and identity are **not** acceptance criteria and must not be implied as delivered. | §3 Local run; §4 Out of O10; §5#1; §6 OUT (Prod AWS); §7 AC mapping |
| 2 | Health-only unauthenticated surface | Lock `GET /health` Auth:**None** as **liveness-only**; **no** other public routes in O10 AC; auth/identity deferred (#5+) — not smuggled into scaffold. | §2 Health contract; Locked decisions #2; §5#2; §6 OUT (platform-owner / auth) |
| 3 | Health zero external deps | Health `200` **without** DB, Redis, AWS, or outbound network. | §2 Dependencies row; Locked decisions #2; §5#3 |
| 4 | Local / $0 + host-shape | No AWS account/resources/provision; README target = **Amazon ECS Express Mode** sketch only; **App Runner** excluded; any provision → escalate pipeline → CPM → COO → CEO. | §4 AWS-shape; Locked decisions #4; §5#4; §6 OUT |
| 5 | Secrets hygiene binding | No secrets, API keys, cloud credentials, or real connection strings in `appsettings*`, README, Dockerfile, launchSettings, comments; illustrative keys = placeholders/env-only. | §1 project rule 6; Locked decisions #5; §5#5 |
| 6 | Zero MM/DC4 | Project rules + OUT forbid MM/DC4 refs, packages, config keys, schemas, feeds, SFTP, shared DB. | §1 project rule 3; §5#6; §6 OUT; §7 AC row |
| 7 | Dockerfile sketch scope | If Dockerfile is present: **local** build/run only; prod IAM / Secrets Manager / ECR promo / signing **not** delivered. | §1 tree (Dockerfile); §4 AWS table; §5#7 |
| 8 | Inert placeholders | Forbid Domain/Application/Infrastructure PackageReferences for auth, LLM, payment, or other secret/exfil-risk SDKs in O10. | §1 project rule 4; Locked decisions #5; §5#8; §6 OUT |
| 9 | Traceability | This Spec cites Spec-step Security checklist + SA Security QA PASS (Sources table + this §5). Does **not** invent new security Stories or expand O10 into SSO/admin/auth. | Sources table; this §5; §6 OUT (platform-owner suite / auth); Constraints header |

**AC trust-boundary lock (point 1):** Acceptance criteria for O10 are local run (§3) + health (§2) + layout (§1) + AWS **shape callout only** (§4). They do **not** include prod TLS, public exposure, or identity.

---

## 6. Explicit OUT (match issue #3 + SA §5)

Do **not** deliver in O10:

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

## 7. Acceptance mapping (issue #3 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| Runnable .NET modular-monolith layout | §1 Solution layout |
| Health / ping endpoint | §2 Health contract (`GET /health`) |
| Documented local run steps | §3 Local run |
| No MotorMarket / DC4 live-system deps | §1 rule 3; §5 points 5–6; §6 OUT |
| AWS = ECS Express Mode; local/$0; optional Dockerfile not prod | §4 AWS-shape; §5 points 4/7 |
| Local-only host (no prod TLS/public/identity AC) | §5 point 1; §3; §6 OUT |

---

## Done-list (Spec QA / SD consumers)

### Spec QA (verify vs Chief Spec Security retro brief)

- [ ] Path/name DOC-FLOW: `specs/2026-09-10__spec__spec__poc-o10-scaffold.md`
- [ ] Sources cite Spec-step Security checklist + SA Security QA PASS + SA arch / issue #3 / Product O10/#10
- [ ] Locks SA §6 decisions 1–6 unchanged (layout Option A, health JSON, README run, ECS Express, security binding, OUT)
- [ ] Health contract canonical: `GET /health` → `200` + `{ "status": "ok" }`; Auth none; no external deps
- [ ] **Security Spec checklist points 1–9** bound with Spec section cites (§5 table)
- [ ] Point 9 Traceability: cites Spec checklist + SA Security QA PASS; no invented security Stories; no SSO/admin/auth expansion
- [ ] Explicit OUT matches issue + SA (incl. #4–#7 domain deferred; zero MM/DC4)
- [ ] No product code; no invented Stories; Product conflicts path noted
- [ ] Cost/critical: O10 must not procure AWS
- [ ] Ask **Security QA** to confirm Spec-step points 1–9 with evidence before Chief Spec unlocks Dev Plan

### SD / Dev Plan consumers (after Spec QA PASS + Security QA PASS + Chief Spec confirm)

- [ ] Create `Dealoware.sln` + projects per §1 (Api only runnable)
- [ ] Implement Minimal API `GET /health` per §2
- [ ] README local run steps per §3
- [ ] README AWS callout per §4 (ECS Express Mode; local/$0; no App Runner)
- [ ] Optional Dockerfile = local sketch only (§4 / §5#7)
- [ ] Secrets + MM/DC4 + inert placeholders per §5
- [ ] Do not implement anything in §6 OUT

**Next:** Spec QA verifies with evidence → ask Security QA confirm Spec-step 1–9 → Spec QA confirm to **Chief Spec only** (never skip Chief). Dev Plan remains HOLD until Security QA PASS.
