# Security checklist — Architecture (SA) · PoC O10 scaffold

**Status:** Chief Security itemized points for Architecture step (handshake per `ops/ORG-OPS.md`).  
**Date:** 2026-09-10  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #3 · capability **O10**  
**Issue:** https://github.com/ioaikh/dealoware/issues/3  
**Architecture:** `architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md`  
**DOC-FLOW:** `verification/2026-09-10__security__verification__poc-o10-sa-checklist.md`

## Scope note
Thin O10 checklist (scaffold / host / health / local-until-spend). Real points — not N/A. No MotorMarket. No AWS spend.

## Itemized security points (Architecture must answer)

1. **Host trust boundary** — Architecture states O10 is a **local-only** host for PoC; production trust (TLS termination, public exposure, identity) is **out of scope** and must not be implied as delivered.
2. **Unauthenticated health surface** — `GET /health` (or chosen ping) with **no auth** is acceptable for O10 **only** if the doc states: (a) endpoint is liveness-only, (b) **no** other public routes in O10, (c) auth/identity is deferred to later Stories and must not be smuggled into this scaffold.
3. **No external dependency on health path** — Health/ping must not require DB, Redis, AWS, or outbound network to return 200 (already in arch — Security requires this remain binding).
4. **Local-until-spend / no cloud provision** — Explicit: no AWS account provision, no AWS resources, no paid cloud in O10. Target host shape = **Amazon ECS Express Mode (Fargate)** as **sketch only**. **App Runner** excluded (closed to new customers / no-new-features). Any provision → escalate CA → CPM → COO → CEO.
5. **Secrets hygiene** — No secrets, API keys, connection strings to MotorMarket/DC4, or cloud credentials in repo artifacts (`appsettings*`, README, Dockerfile, launchSettings, comments). Use placeholders/env-only patterns if any config keys are shown.
6. **Zero MotorMarket / DC4 coupling** — No project references, package names, config keys, schemas, feeds, SFTP, or shared DB pointers to MM/DC4.
7. **Dockerfile / image sketch (if present)** — Local build/run sketch only; no prod IAM roles, Secrets Manager wiring, ECR promo, or signing claims presented as delivered.
8. **Placeholder libs stay inert** — Domain/Application/Infrastructure placeholders must not pull auth, LLM, payment, or third-party SDKs that introduce secret or data-exfil risk in O10.

## Handshake next
1. Senior Architect amends architecture to answer each point (cite section).
2. Architecture QA asks **Security QA** to confirm with evidence.
3. Security QA → PASS confirm or further instructions (bounce Senior Architect and/or Chief Architect).
4. Spec unlock only after Security QA confirm (PM owns gate).

## Cost/critical
No AWS spend in O10. Cost/critical findings → COO → CEO.
