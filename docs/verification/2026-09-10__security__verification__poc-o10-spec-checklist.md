# Security checklist — Spec · PoC O10 scaffold

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Retro — Spec QA PASS landed without Security woven; treat as gate before Dev Plan unlock.  
**Date:** 2026-09-10  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #3 · capability **O10**  
**Issue:** https://github.com/ioaikh/dealoware/issues/3  
**Spec:** `specs/2026-09-10__spec__spec__poc-o10-scaffold.md`  
**Prior SA Security PASS:** `verification/2026-09-10__security__verification__poc-o10-sa-qa-confirm.md`  
**DOC-FLOW:** `verification/2026-09-10__security__verification__poc-o10-spec-checklist.md`

## Scope note
Thin Spec-step checklist. Spec must **bind** SA Security answers into implementable requirements for Dev Plan/SD — not re-litigate architecture. Real points — not N/A. No MotorMarket. No AWS spend. No invented Stories.

## Itemized security points (Spec must answer / bind)

1. **Trust boundary in AC** — Spec states O10 deliverable is **local-only** runnable host; does **not** make prod TLS termination, public exposure, or identity acceptance criteria.
2. **Health-only unauthenticated surface** — Spec locks `GET /health` Auth:None as **liveness-only**; **no** other public routes in O10 AC; auth/identity deferred — not smuggled into scaffold requirements.
3. **Health zero external deps** — Spec binds health `200` **without** DB, Redis, AWS, or outbound network.
4. **Local / $0 + host-shape** — Spec binds: no AWS account/resources/provision; README target = **Amazon ECS Express Mode** sketch only; **App Runner** excluded; any provision → escalate pipeline → CPM → COO → CEO.
5. **Secrets hygiene binding** — Spec requires SD: no secrets, API keys, cloud credentials, or real connection strings in `appsettings*`, README, Dockerfile, launchSettings, comments; illustrative keys = placeholders/env-only.
6. **Zero MM/DC4** — Spec project rules + OUT forbid MM/DC4 refs, packages, config keys, schemas, feeds, SFTP, shared DB.
7. **Dockerfile sketch scope** — If Dockerfile is in scope, Spec marks it local build/run only; prod IAM / Secrets Manager / ECR promo / signing **not** delivered.
8. **Inert placeholders** — Spec forbids Domain/Application/Infrastructure PackageReferences for auth, LLM, payment, or other secret/exfil-risk SDKs in O10.
9. **Traceability** — Spec cites SA Security checklist + Security QA PASS; does not invent new security Stories or expand O10 into SSO/admin/auth.

## Handshake next
1. Senior Spec amends Spec if any point not bound (cite sections).
2. Spec QA (or step QA) asks **Security QA** to confirm with evidence.
3. Security QA → PASS to Chief Security or further instructions.
4. Chief Security pings CPM to unlock Dev Plan after Security QA PASS.

## Cost/critical
No AWS spend in O10. Cost/critical → COO → CEO.
