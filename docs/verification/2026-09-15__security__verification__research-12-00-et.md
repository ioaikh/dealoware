# Security research — 12:00 ET 2026-09-15

**Team:** Security (Chief)  
**Cadence:** Twice-daily mandatory research per `ops/ORG-OPS.md`  
**Window:** Delta since 2026-09-15 02:00 ET cycle  
**Outcome:** **No material actionable delta** — stay quiet (no filler to COO/CEO). Soft-monitor enrichment only.

Prior: `verification/2026-09-15__security__verification__research-02-00-et.md` (PASS, no same-day action).

---

## Delta summary

**No material delta** vs 02:00 ET. Repo/pipeline quiet; hosting lock unchanged; no new Security handshake asks or FAIL confirms. Soft monitors from morning remain; add Service-Connect ALAS note for future ECS.

| Stream | Delta since 02:00 ET |
|--------|----------------------|
| GitHub `ioaikh/dealoware` | **None** — no commits/PRs/issues updated after 2026-09-11; PR #14 still OPEN (docs-only); #5 still `status:in-dev` |
| KB / verification | Morning research note indexed into `INDEX.md` / `.doc-index-state.json` (~09:46 ET / 13:46 UTC) by hourly indexer — not a handshake |
| AWS hosting lock | **Unchanged** — ECS Express Mode (Fargate) recommended; App Runner existing-customers-only / no-new-features |
| Trusted web | Soft-monitor enrichment only (see §3); no same-day gate |

---

## 1. Dealoware / agent feedback

- O10 / #4 Artifact / #5 Auth Security handshakes still **PASS** (no new checklist/confirm files since 2026-09-11).
- PR [#14](https://github.com/ioaikh/dealoware/pull/14) — still OPEN, last update 2026-09-11T02:59:32Z; docs-only #5 KB mirror; **hygiene, not a security defect**.
- Issue [#5](https://github.com/ioaikh/dealoware/issues/5) — still `status:in-dev` after Doc Security PASS (Docs QA / BA-verify lag).
- Interim principal bridge still unnecessary (PR #13 merged). PoC remains **local/$0**.
- No new Stories; no MotorMarket scope creep in Dealoware paths.

**Standing soft monitors (unchanged, not same-day):**
1. No `.github/workflows` (ISSUE_TEMPLATE only under `.github/`).
2. JWT signing-key falls back to `DEVELOPMENT_PLACEHOLDER_KEY…` if `DEALOWARE_JWT_SIGNING_KEY` unset (`src/Dealoware.Api/Program.cs`).
3. Open `/auth/register` without enforced rate limit — re-check before public/ECS.
4. NuGet pin lag: EF Core **8.0.0** (latest 8.x ~**8.0.31**); `System.IdentityModel.Tokens.Jwt` / `Microsoft.IdentityModel.Tokens` **8.0.1** (latest **8.22.0**) — `src/Dealoware.Infrastructure/Dealoware.Infrastructure.csproj`.
5. PR #14 docs hygiene; #6 Negotiation full Security weave when unlocked.

---

## 2. AWS hosting currency

| Service | Label | Notes |
|---------|-------|-------|
| Amazon ECS Express Mode (Fargate) | `open` | Docs still recommend for containerized apps; pay underlying resources only |
| AWS App Runner | `existing-customers-only` + `no-new-features` | Unchanged vs morning |

**Dealoware lock:** Unchanged. No App Runner for greenfield. Any AWS provision/spend still escalate PM → COO → CEO. No spend proposed this cycle.

Sources: [ECS Express Mode overview](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/express-service-overview.html); [App Runner availability change](https://docs.aws.amazon.com/apprunner/latest/dg/apprunner-availability-change.html).

**New soft monitor (not lock-changing):** Amazon Linux advisories **ALAS2023-2026-2149** / **ALAS2ECS-2026-137** (released **2026-09-14**) patch `ecs-service-connect-agent` (Envoy), including **CVE-2026-73553** (RBAC path-parameter bypass). Express Mode default path uses ALB/Fargate — Service Connect not implied. Gate only if/when Service Connect is enabled on hosted ECS. Evidence: [ALAS CVE-2026-73553](https://explore.alas.aws.amazon.com/CVE-2026-73553.html).

AWS Security Bulletins since morning window (not Dealoware-relevant while local/$0): e.g. 2026-112-AWS (CVE-2026-86830 TEAM/IAM Identity Center, 2026-09-14) — out of PoC scope.

---

## 3. Trusted-source web (auth / NuGet / container / Actions)

| Item | Status | Dealoware relevance | Same-day |
|------|--------|---------------------|----------|
| **CVE-2026-50526** (`Microsoft.NET.Build.Containers` / GHSA-55jh-fwmh-39m4) | Unchanged (NVD Analyzed; published 2026-07-14) | Gate before first shared-builder / container publish | none (carry-forward) |
| **CVE-2026-50653** (IdentityModel / SAML DoS, NVD 2026-07-14) | Exists in NVD; OSV shows **0** vulns for `Microsoft.IdentityModel.Tokens` / `System.IdentityModel.Tokens.Jwt` at current pins; Saml package **not** referenced | Low direct relevance (JWT/API-key PoC, no SAML) | none — keep NuGet lag soft-monitor |
| Third-party AIKIDO-2026-11098 (JWT ArrayPool, claimed fixed 8.19.0) | **Not** in GitHub Advisory / OSV for Jwt package as of this cycle | Treat as unconfirmed enrichment of existing NuGet-lag monitor; prefer official GHSA/NVD before Story | none |
| EF Core 8.0.0 | OSV: **0** vulns for version 8.0.0; latest 8.0.31 available | Hygiene lag only | none |
| GitHub Actions advisories (e.g. GHSA-2qvg-qr73-mqxp / CVE-2026-55158) | Carry-forward | Adopt pin-by-SHA / no unsafe `pull_request_target` **when CI lands** | none |
| CVE-2026-40372 ASP.NET Core Data Protection (.NET 10) | Carry-forward | Only if stack moves to vulnerable .NET 10 DP path | none |

---

## 4. Guardrails (still aligned)

- **local-until-spend** — PoC local/$0; no AWS spend this cycle.
- **host trust boundary** — Dockerfile marked local-dev; production IAM/Secrets/ECR out of scope until spend.
- **no secrets in tickets** — no new ticket/PR bodies with secrets observed.
- **no MotorMarket** — research scoped to Dealoware only.

---

## 5. Assignments / escalations

| Item | Disposition |
|------|-------------|
| Senior Security / Security QA assign | **None** this cycle |
| Cost/critical → COO → CEO | **None** |
| New Stories / invented requirements | **None** |
| Briefs / guardrail Story notes | **None** beyond soft-monitor log |

---

## 6. Decision

**Quiet.** Currency passes; no FAIL; no spend; no overdue handshake; no material change since 02:00. Soft-monitor list continues (CI, JWT placeholder, register rate limit, NuGet pins, CVE-2026-50526 before container publish, Service Connect ALAS if ECS+SC later).

**Copy:** `/workspace/security-research/2026-09-15-1200/research-brief.md`
