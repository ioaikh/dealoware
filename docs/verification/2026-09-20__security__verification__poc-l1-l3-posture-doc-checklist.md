# Security checklist — Doc · PoC Standing license/repo/hosted posture L1–L3 (#8)

**Status:** Chief Security itemized points for Doc step (handshake per `ops/ORG-OPS.md`). Issue **before** Docs QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #8 · Standing L1–L3 chore (not a feature Story)  
**Issue:** https://github.com/ioaikh/dealoware/issues/8  
**PR:** https://github.com/ioaikh/dealoware/pull/21 (MERGED)  
**Product QA Security PASS:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-productqa-qa-confirm.md`  
**Hold:** Do **not** invent product features, Cognito/SSO, hosted SaaS, or MM/DC4 coupling as delivered  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-doc-checklist.md`

## Scope note
Doc must accurately describe the L1–L3 **chore** posture (Apache-2.0, public repo, hosted non-goal, MM/DC4 separation) and must **not** invent product features or hosted-platform ownership changes. Real points — not N/A. PoC $0. Keep separate from #3–#7 Doc Security except cross-refs.

## Itemized security points (Doc must satisfy)

1. **L1 LICENSE** — Docs state Apache-2.0 LICENSE present/correct; no proprietary inventing.

2. **L2 Public-repo posture** — Docs state canonical `https://github.com/ioaikh/dealoware` as public posture.

3. **Public-repo secrets hygiene** — Docs/examples use placeholders only; no real secrets, MM/DC4 logins, SFTP, or inventory dumps.

4. **L3 Hosted non-goal** — Docs state free-hosted fork ≠ “the” platform; hosted remains AIKnowHow/Dealoware.

5. **No hosted-platform inventing** — Docs do not claim Cognito/SSO, prod App Runner/ECS spend, or official hosted SaaS as #8 delivered; PoC local/$0.

6. **MotorMarket / DC4 separation** — Docs keep MM/DC4 live systems/inventory/SFTP/test logins out; Out of Scope / separation documented.

7. **No inventing product features** — Docs do not invent marketing-as-AC or new domain APIs under this chore.

8. **Cross-story non-merge** — Docs keep L1–L3 separate from #3–#7 feature Docs except cross-refs.

9. **Cost / spend** — Docs affirm PoC **$0** / no IdP provision for this chore.

10. **Handshake close** — Docs QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Docs weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Chief Docs.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
