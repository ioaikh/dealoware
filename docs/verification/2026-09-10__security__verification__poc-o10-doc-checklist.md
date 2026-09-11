# Security checklist — Doc · PoC O10 scaffold

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Doc QA PASS.  
**Date:** 2026-09-10  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #3 · capability **O10**  
**Issue:** https://github.com/ioaikh/dealoware/issues/3  
**PR:** https://github.com/ioaikh/dealoware/pull/9  
**Product QA Security PASS:** `verification/2026-09-10__security__verification__poc-o10-productqa-qa-confirm.md`  
**DOC-FLOW:** `verification/2026-09-10__security__verification__poc-o10-doc-checklist.md`

## Scope note
Thin Doc-binding checklist. Documentation must **not** invent prod security claims, leak secrets, or imply AWS spend/MotorMarket coupling. Real points — not N/A. PoC $0.

## Itemized security points (Doc must satisfy)

1. **No prod trust claims** — Docs do not claim O10 delivers public exposure, prod TLS termination, or identity/auth as complete.
2. **Health documented accurately** — Docs describe `GET /health` as liveness-only, Auth:None, sole O10 route (or match Spec); do not invent metrics/readiness/auth surfaces.
3. **Local / $0 + host-shape** — Docs state PoC is local/$0; target host = **ECS Express Mode** sketch; **App Runner** excluded; no AWS provision instructions presented as done.
4. **Secrets hygiene** — Docs/examples use placeholders/env-only; no real secrets, API keys, cloud credentials, or connection strings.
5. **Zero MM/DC4** — Docs do not introduce MotorMarket/DC4 integration steps, credentials, feeds, or shared-DB guidance.
6. **Dockerfile sketch scope** — If documented, Dockerfile is local sketch only; no prod IAM/Secrets Manager/ECR/signing as delivered.
7. **No scope-creep docs** — Docs do not schedule/describe Strategy/AI, settlement, search, or domain #4–#7 as part of O10 delivery.
8. **DOC-FLOW / index** — Security-related verification artifacts for #3 remain under `verification/` with correct naming; no scatter of secrets at KB root.
9. **Traceability** — Doc cites Spec/SD/Product QA Security PASS paths where relevant; does not invent new security Stories.
10. **Handshake close** — Doc QA must not PASS until Security QA confirms these points.

## Handshake next
1. Senior Docs weaves points into Doc deliverables.
2. Senior Security reviews → done-list to Security QA (or Security QA evidence-checks on step-QA ask).
3. Security QA PASS to Chief Security.
4. Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS spend. Cost/critical → COO → CEO.
