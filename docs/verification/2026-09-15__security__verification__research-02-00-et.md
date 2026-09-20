# Security research — 02:00 ET 2026-09-15

**Team:** Security (Chief)  
**Cadence:** Twice-daily mandatory research per `ops/ORG-OPS.md`  
**Window:** Feedback since ~2026-09-11 + trusted-source web (~90 days)  
**Outcome:** **Nothing actionable same day** — stay quiet (no filler to COO/CEO).

Prior routine runs (02:00 / 12:00) failed on `usage_limit` only; this is the first successful research cycle since routines were armed.

---

## 1. AWS currency check (ORG-OPS mandatory)

| Service | Label | Notes |
|---------|-------|-------|
| Amazon ECS Express Mode (Fargate) | `open` | Official ECS Express Mode docs; pay underlying resources only |
| AWS Fargate (under Express Mode) | `open` | Same |
| AWS App Runner | `existing-customers-only` + `no-new-features` | Closed to new customers from 2026-04-30; AWS points new workloads to ECS Express Mode |

**Dealoware lock:** Unchanged and current. No App Runner for greenfield. Any AWS provision/spend still escalate PM → COO → CEO. No spend proposed this cycle.

Sources: [ECS Express Mode overview](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/express-service-overview.html); [App Runner availability change](https://docs.aws.amazon.com/apprunner/latest/dg/apprunner-availability-change.html); [AWS Service Availability Updates 2026-03-31](https://aws.amazon.com/about-aws/whats-new/2026/03/aws-service-availability/).

---

## 2. Dealoware feedback / pipeline

**Closed / PASS (no action):** O10 (#3), #4 Artifact, #5 Auth — Spec→DevPlan→SD→ProductQA→Doc Security handshakes PASS (verification `*poc-o10-*`, `*poc-artifact-*`, `*poc-auth-*-qa-confirm.md`). Host lock + local/$0 + Dockerfile local-only still aligned.

**Monitor only (not same-day):**
1. No `.github/workflows` yet — soft gap while PoC local/$0.
2. JWT signing-key development placeholder if `DEALOWARE_JWT_SIGNING_KEY` unset — guardrail before non-local host.
3. Open `/auth/register` without enforced rate limit — re-check before public/ECS.
4. NuGet pin lag (EF Core 8.0.0; IdentityModel JWT ~8.0.1 vs newer 8.x) — monitoring.
5. PR #14 open (docs-only #5 KB mirror) — Docs/PM hygiene.
6. Issue #5 still `status:in-dev` after Doc Security PASS — Docs QA / BA-verify lag, not Security FAIL.
7. #6 Negotiation when unlocked — full Security weave required before Spec QA.
8. INDEX sibling-wipe — DevOps track.

KB quiet since ~2026-09-11 03:00Z for new Stories needing Security handshake.

---

## 3. Trusted-source web (carry-forward hygiene, not same-day)

- **CVE-2026-50526** (`Microsoft.NET.Build.Containers`) — patch before first shared-builder image publish ([GHSA-55jh-fwmh-39m4](https://github.com/advisories/GHSA-55jh-fwmh-39m4), 2026-07-14).
- GitHub npm/Actions supply-chain hardening (2026-07-28 blog) — adopt when CI appears (pin Actions by SHA, Dependabot cooldown, avoid unsafe `pull_request_target`).
- **CVE-2026-40372** ASP.NET Core Data Protection (.NET 10.0.0–10.0.6) — relevant only if stack moves to vulnerable .NET 10 DataProtection path; prefer fail-closed JWT/API-key for #5-style auth.
- OWASP AI Agent Security Cheat Sheet / CISA agentic AI guidance — reinforces existing local-until-spend, HITL/spend escalate, secret hygiene, fail-closed charter. No new product work.

---

## 4. Assignments / escalations

| Item | Disposition |
|------|-------------|
| Senior Security / Security QA assign | **None** this cycle |
| Cost/critical → COO → CEO | **None** |
| New Stories / invented requirements | **None** |
| MotorMarket | **Out of scope** |

---

## 5. Decision

**Quiet.** Currency passes; no FAIL; no spend; no overdue handshake. Standing soft gaps remain on monitor list for host/spend and #6 unlock.
