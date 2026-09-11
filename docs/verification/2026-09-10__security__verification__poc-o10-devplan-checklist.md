# Security checklist — Dev Plan · PoC O10 scaffold

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-10  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #3 · capability **O10**  
**Issue:** https://github.com/ioaikh/dealoware/issues/3  
**Spec (binding):** `specs/2026-09-10__spec__spec__poc-o10-scaffold.md`  
**Spec Security PASS:** `verification/2026-09-10__security__verification__poc-o10-spec-qa-confirm.md`  
**DOC-FLOW:** `verification/2026-09-10__security__verification__poc-o10-devplan-checklist.md`

## Scope note
Thin Dev Plan-binding checklist. Plan must **schedule and gate** Spec Security constraints for SD — not invent new product scope. Real points — not N/A. No MotorMarket. No AWS spend. No invented Stories.

## Itemized security points (Dev Plan must weave)

1. **Local-only delivery** — Plan milestones deliver a **local** runnable host only; no step that provisions public exposure, prod TLS, or identity as O10 done criteria.
2. **Health-only surface** — Plan includes implement + verify `GET /health` Auth:None as **sole** O10 route; explicitly **excludes** auth/SSO/admin/other public routes from this Story’s plan.
3. **Health zero external deps** — Plan includes a verify step: health returns 200 **without** DB, Redis, AWS, or outbound network.
4. **Local / $0 + host-shape docs** — Plan includes README (or equivalent) steps: **ECS Express Mode** sketch callout; **App Runner** excluded; **no** AWS account/resource provision tasks; escalate path if spend ever proposed.
5. **Secrets hygiene gate** — Plan includes check before done: no secrets/API keys/cloud credentials/real connection strings in `appsettings*`, README, Dockerfile, launchSettings, comments; placeholders/env-only only.
6. **Zero MM/DC4 gate** — Plan includes verify: no MM/DC4 project refs, packages, config keys, schemas, feeds, SFTP, shared DB.
7. **Dockerfile optional/local** — If Dockerfile is planned, mark as local `docker build`/`run` sketch only; **out:** prod IAM, Secrets Manager, ECR promo, signing.
8. **Inert placeholders** — Plan forbids adding auth/LLM/payment (or other secret/exfil-risk) PackageReferences to Domain/Application/Infrastructure in O10 tasks.
9. **No security-scope creep** — Plan does not schedule AWS provision, MotorMarket integration, or new security Stories; cites Spec Security PASS as binding input.
10. **Security handshake close** — Plan notes Dev Plan QA must not PASS until Security QA confirms these points (or Chief Security marks HOLD with evidence).

## Handshake next
1. Senior Dev Planner weaves points into the Dev Plan (cite sections/tasks).
2. Senior Security reviews plan vs this checklist → done-list to Security QA.
3. Security QA confirms PASS to Chief Security (or further instructions).
4. Chief Security confirms PASS to CPM + Chief Dev Planner (or HOLD ping to CPM).

## Cost/critical
No AWS spend in O10. Cost/critical → COO → CEO.
