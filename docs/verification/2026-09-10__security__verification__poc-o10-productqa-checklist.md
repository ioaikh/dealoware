# Security checklist — Product QA · PoC O10 scaffold

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-10  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #3 · capability **O10**  
**Issue:** https://github.com/ioaikh/dealoware/issues/3  
**PR:** https://github.com/ioaikh/dealoware/pull/9  
**SD Security PASS:** `verification/2026-09-10__security__verification__poc-o10-sd-qa-confirm.md`  
**CQ:** no-refactor  
**DOC-FLOW:** `verification/2026-09-10__security__verification__poc-o10-productqa-checklist.md`

## Scope note
Thin Product QA-binding checklist. Product QA must **verify with evidence** that SD Security constraints still hold in the deliverable — not invent new Stories. Real points — not N/A. No MotorMarket. PoC $0. CQ = no-refactor.

## Itemized security points (Product QA must evidence)

1. **Local-only** — Verify product acceptance does **not** require/claim public exposure, prod TLS, or identity as delivered by O10.
2. **Health contract** — Verify `GET /health` → `200` + `{ "status": "ok" }` with **Auth:None**; no other public routes present in the PR/runnable host.
3. **Health zero external deps** — Verify health succeeds **without** DB, Redis, AWS, or outbound network (runtime evidence or equivalent).
4. **Local / $0 + host-shape docs** — Verify README (or docs in PR) states ECS Express Mode sketch only, App Runner excluded, no AWS provision; PoC local/$0.
5. **Secrets hygiene** — Spot-check PR artifacts: no secrets/API keys/cloud credentials/real connection strings in `appsettings*`, README, Dockerfile, launchSettings, comments.
6. **Zero MM/DC4** — Verify no MotorMarket/DC4 refs, packages, or config keys in solution.
7. **Dockerfile (if present)** — Verify marked/used as local sketch only; no prod IAM/Secrets Manager/ECR/signing as delivered.
8. **Inert placeholders** — Verify Domain/Application/Infrastructure (if present) lack auth/LLM/payment (secret/exfil-risk) PackageReferences.
9. **No scope creep** — Verify OUT still holds: no Strategy/AI, settlement, search, domain #4–#7 beyond placeholders; CQ no-refactor respected.
10. **Handshake close** — Product QA must not PASS until Security QA confirms these points (cite this checklist + evidence).

## Handshake next
1. Senior Product QA / Dealoware QA weaves checks into Product QA evidence.
2. Senior Security reviews Product QA evidence vs this checklist → done-list to Security QA (or Security QA evidence-checks on step-QA ask).
3. Security QA confirms PASS to Chief Security (or further instructions).
4. Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS spend. Cost/critical → COO → CEO.
