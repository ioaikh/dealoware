# Security checklist — SD · PoC Standing license/repo/hosted posture L1–L3 (#8)

**Status:** Chief Security itemized points for SD step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Code QA / Product QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #8 · Standing L1–L3 chore (not a feature Story)  
**Issue:** https://github.com/ioaikh/dealoware/issues/8  
**Plan:** `plans/2026-09-20__devplan__plan__poc-standing-l1-l3-posture.md`  
**Dev Plan Security PASS:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-devplan-qa-confirm.md`  
**Spec Security PASS:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-qa-confirm.md`  
**Hold:** Do **not** invent product features, Cognito/SSO, hosted SaaS, or MM/DC4 coupling  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-sd-checklist.md`

## Scope note
SD must implement Spec + Dev Plan Security gates for this **chore** only (LICENSE, public-repo docs, hosted non-goal, MM/DC4 separation). Real points — not N/A. PoC $0. No Cognito inventing. Keep separate from #3–#7 feature code except cross-refs.

## Itemized security points (SD must satisfy)

1. **L1 LICENSE** — Apache-2.0 `LICENSE` present/correct at repo root; no proprietary license inventing.

2. **L2 Public-repo posture** — README/docs state canonical `https://github.com/ioaikh/dealoware`; no private-only inventing.

3. **Public-repo secrets hygiene** — Deliverables contain **no** secrets, API keys, cloud credentials, MM/DC4 test logins, SFTP credentials, or inventory dumps.

4. **L3 Hosted non-goal** — Docs state free-hosted fork ≠ “the” platform; hosted remains AIKnowHow/Dealoware — no ownership inventing.

5. **No hosted-platform inventing** — No Cognito/SSO, prod App Runner/ECS spend, or “official hosted SaaS” modules as #8 deliverables; PoC remains local/$0.

6. **MotorMarket / DC4 separation** — No MM/DC4 live systems, inventory, SFTP, or test logins in Dealoware SD deliverables for this chore.

7. **No inventing product features** — No marketing claims as product AC; no new domain APIs; no Strategy/AI/settlement/identity-seal under this chore.

8. **Cross-story non-merge** — Does not rewrite #3–#7 feature Stories; L1–L3 posture only (+ cross-refs).

9. **Cost / spend** — No AWS/IdP provision for this chore; PoC **$0**.

10. **Evidence + handshake** — Done-list cites paths for 1–9; Code/Product QA must **not** PASS until Security QA confirms.

## Handshake next
Senior Developer → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Developer.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
