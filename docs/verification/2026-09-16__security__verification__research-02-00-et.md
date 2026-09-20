# Security research — 02:00 ET 2026-09-16

**Team:** Security (Chief)  
**Cadence:** Twice-daily mandatory research per `ops/ORG-OPS.md`  
**Window:** Delta since 2026-09-15 12:00 ET cycle  
**Outcome:** **No material actionable delta** — stay quiet (no filler to COO/CEO). Soft monitors unchanged.

Prior: `verification/2026-09-15__security__verification__research-12-00-et.md` (quiet).

---

## Delta summary

**No material delta** vs 2026-09-15 12:00 ET. Repo/pipeline quiet since 2026-09-11; hosting lock unchanged; no new Security handshake asks or FAIL confirms.

| Stream | Delta since 12:00 ET 2026-09-15 |
|--------|----------------------------------|
| GitHub `ioaikh/dealoware` | **None** — last commit 2026-09-11T02:49:15Z (PR #13 merge); PR #14 still OPEN (docs-only); #5 still `status:in-dev` |
| KB / verification | No new handshake/checklist/confirm files after 2026-09-11; only prior research notes |
| AWS hosting lock | **Unchanged** — ECS Express Mode (Fargate) open; App Runner existing-customers-only / no-new-features |
| Trusted web | Soft-monitor carry-forward only; no same-day gate |

---

## 1. Dealoware / agent feedback

- O10 / #4 Artifact / #5 Auth Security handshakes still **PASS** (no new files since 2026-09-11).
- PR [#14](https://github.com/ioaikh/dealoware/pull/14) — still OPEN, last update 2026-09-11T02:59:32Z; docs-only #5 KB mirror; **hygiene, not a security defect**.
- Issue [#5](https://github.com/ioaikh/dealoware/issues/5) — still `status:in-dev` after Doc Security PASS (Docs QA / BA-verify lag).
- Interim principal bridge still unnecessary (PR #13 merged). PoC remains **local/$0**.
- No new Stories; no MotorMarket scope creep in Dealoware paths.

**Standing soft monitors (unchanged, not same-day):**
1. No `.github/workflows` (ISSUE_TEMPLATE only under `.github/`).
2. JWT signing-key falls back to `DEVELOPMENT_PLACEHOLDER_KEY…` if `DEALOWARE_JWT_SIGNING_KEY` unset (`src/Dealoware.Api/Program.cs`).
3. Open `/auth/register` without enforced rate limit — re-check before public/ECS.
4. NuGet pin lag: EF Core **8.0.0**; `System.IdentityModel.Tokens.Jwt` / `Microsoft.IdentityModel.Tokens` **8.0.1** — OSV query this cycle: **0** vulns for both pins. Hygiene lag only.
5. PR #14 docs hygiene; #6 Negotiation full Security weave when unlocked.
6. **CVE-2026-50526** (`Microsoft.NET.Build.Containers`) — gate before first shared-builder / container publish.
7. **ALAS2023-2026-2149** / **ALAS2ECS-2026-137** (`ecs-service-connect-agent`, incl. CVE-2026-73553 and sibling Envoy CVEs) — only if Service Connect enabled on hosted ECS (Express Mode default path uses ALB/Fargate; SC not implied).
8. Actions SHA-pin / least-privilege / no unsafe `pull_request_target` — adopt **when CI lands** (Trivy-action / mutable-tag lessons reinforce existing guidance).

---

## 2. AWS hosting currency

| Service | Label | Notes |
|---------|-------|-------|
| Amazon ECS Express Mode (Fargate) | `open` | Docs still recommend for containerized apps; pay underlying resources only |
| AWS App Runner | `existing-customers-only` + `no-new-features` | Confirmed live docs 2026-09-16 |

**Dealoware lock:** Unchanged. No App Runner for greenfield. Any AWS provision/spend still escalate PM → COO → CEO. No spend proposed this cycle.

Sources: [ECS Express Mode overview](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/express-service-overview.html); [App Runner availability change](https://docs.aws.amazon.com/apprunner/latest/dg/apprunner-availability-change.html).

Out-of-scope while local/$0: EFS CSI CVE-2026-85781 (K8s CSI, non-default flag); ECS Agent Windows FSx CVE-2026-7461 (Windows EC2 agent; Fargate not affected).

---

## 3. Trusted-source web (auth / NuGet / container / Actions)

| Item | Status | Dealoware relevance | Same-day |
|------|--------|---------------------|----------|
| **CVE-2026-50526** | Unchanged (GHSA-55jh-fwmh-39m4) | Gate before first shared-builder / container publish | none (carry-forward) |
| IdentityModel JWT / EF Core pins | OSV **0** vulns at 8.0.1 / 8.0.0 | Hygiene lag only | none |
| Third-party Aikido JWT notes | Not official GHSA/OSV for current 8.x pins | Prefer official advisory before Story | none |
| Actions SHA-pin / trivy-action lessons | Carry-forward | When CI appears | none |
| Service Connect ALAS cluster | Unchanged since 2026-09-14 release | Soft monitor if SC later | none |

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

**Quiet.** Currency passes; no FAIL; no spend; no overdue handshake; no material change since 2026-09-15 12:00. Soft-monitor list continues.

**Copy:** `/workspace/security-research/2026-09-16-0200/research-brief.md`
