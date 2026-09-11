# Security checklist — SD (Implementation) · PoC O10 scaffold

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-10  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #3 · capability **O10**  
**Issue:** https://github.com/ioaikh/dealoware/issues/3  
**Plan:** `plans/2026-09-10__devplan__plan__poc-o10-scaffold.md`  
**Spec:** `specs/2026-09-10__spec__spec__poc-o10-scaffold.md`  
**Dev Plan Security PASS:** `verification/2026-09-10__security__verification__poc-o10-devplan-qa-confirm.md`  
**DOC-FLOW:** `verification/2026-09-10__security__verification__poc-o10-sd-checklist.md`

## Scope note
Thin SD-binding checklist. Implementation must **satisfy** Spec + Dev Plan Security gates in code/docs — not invent new product scope. Real points — not N/A. No MotorMarket. No AWS spend. No invented Stories.

## Itemized security points (SD must satisfy)

1. **Local-only host** — Deliverable is a **local** runnable `.NET` host; do not implement/provision public exposure, prod TLS termination, or identity as part of O10 done.
2. **Health-only surface** — Implement **only** `GET /health` → `200` + `{ "status": "ok" }` with **Auth:None**; no other public routes (no auth/SSO/admin/Artifact/negotiate).
3. **Health zero external deps** — Health returns 200 **without** DB, Redis, AWS SDK calls, or outbound network.
4. **Local / $0 + host-shape docs** — README documents local run; AWS callout = **Amazon ECS Express Mode** sketch only; **App Runner** excluded; **no** AWS account/resource provision in this Story.
5. **Secrets hygiene** — No secrets, API keys, cloud credentials, or real connection strings in `appsettings*`, README, Dockerfile, launchSettings, comments; illustrative keys = placeholders/env-only.
6. **Zero MM/DC4** — No MotorMarket/DC4 project references, packages, config keys, schemas, feeds, SFTP, or shared DB pointers in the solution.
7. **Dockerfile (if present)** — Local multi-stage `docker build`/`run` sketch only; no prod IAM, Secrets Manager, ECR promo, or signing presented as delivered.
8. **Inert placeholders** — Domain/Application/Infrastructure (if present) have **no** PackageReferences for auth, LLM, payment, or other secret/exfil-risk SDKs.
9. **No scope creep** — Do not implement Strategy/AI, settlement, discovery/search, or domain Stories #4–#7 beyond empty placeholders; no AWS spend.
10. **Verify before handoff** — SD done-list cites evidence for points 1–9 (commands/paths); Dev Code QA must not PASS until Security QA confirms (ORG-OPS handshake).

## Handshake next
1. Senior Developer implements/weaves and answers points in done-list.
2. Senior Security reviews code/docs vs this checklist → done-list to Security QA.
3. Security QA confirms PASS to Chief Security (or further instructions).
4. Chief Security confirms PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS spend in O10. Cost/critical → COO → CEO.
