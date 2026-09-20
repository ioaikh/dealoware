# Security checklist — Product QA · PoC Standing license/repo/hosted posture L1–L3 (#8)

**Status:** Chief Security itemized points for Product QA step (handshake per `ops/ORG-OPS.md`). Issue **before** Product QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #8 · Standing L1–L3 chore (not a feature Story)  
**Issue:** https://github.com/ioaikh/dealoware/issues/8  
**PR:** https://github.com/ioaikh/dealoware/pull/21 (MERGED)  
**SD Security PASS:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-sd-qa-confirm.md`  
**Hold:** Do **not** invent product features, Cognito/SSO, hosted SaaS, or MM/DC4 coupling  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-productqa-checklist.md`

## Scope note
Product QA must evidence that SD Security constraints for this **chore** hold on **main** (PR #21 merged). Real points — not N/A. PoC $0. Soft static/`gh` evidence OK if stated (same pattern as prior Stories).

## Itemized security points (Product QA must evidence)

1. **L1 LICENSE** — Apache-2.0 `LICENSE` present/correct at repo root on main.

2. **L2 Public-repo posture** — README/docs on main state canonical `https://github.com/ioaikh/dealoware`.

3. **Public-repo secrets hygiene** — No secrets, API keys, cloud credentials, MM/DC4 test logins, SFTP credentials, or inventory dumps in #8 deliverables on main.

4. **L3 Hosted non-goal** — Docs state free-hosted fork ≠ “the” platform; hosted remains AIKnowHow/Dealoware.

5. **No hosted-platform inventing** — No Cognito/SSO, prod App Runner/ECS spend, or official hosted SaaS delivered as #8; PoC local/$0.

6. **MotorMarket / DC4 separation** — No MM/DC4 live systems/inventory/SFTP/test logins in #8 deliverables; Out of Scope / separation documented.

7. **No inventing product features** — No marketing-as-AC; no new domain APIs; no Strategy/AI/settlement/identity-seal under this chore.

8. **Cross-story non-merge** — #3–#7 feature Stories not rewritten by #8.

9. **Cost / spend** — No AWS/IdP provision for this chore; PoC **$0**.

10. **Handshake close** — Product QA must **not** PASS until Security QA confirms these points.

## Handshake next
Product QA weaves → Senior Security / Security QA → Chief Security PASS/HOLD to CPM + Dealoware QA.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
