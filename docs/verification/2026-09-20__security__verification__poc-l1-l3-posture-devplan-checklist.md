# Security checklist — Dev Plan · PoC Standing license/repo/hosted posture L1–L3 (#8)

**Status:** Chief Security itemized points for Dev Plan step (handshake per `ops/ORG-OPS.md`). Issue **before** Dev Plan QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #8 · Standing L1–L3 chore (not a feature Story)  
**Issue:** https://github.com/ioaikh/dealoware/issues/8  
**Spec:** `specs/2026-09-20__spec__spec__poc-standing-l1-l3-posture.md`  
**Spec Security PASS:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-qa-confirm.md`  
**Hold:** Do **not** invent product features, marketing claims, license changes, hosted-ownership changes, or Cognito/SSO  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-devplan-checklist.md`

## Scope note
Dev Plan must **schedule/gate** Spec Security constraints for SD on this **chore** only (L1 LICENSE, L2 public repo, L3 hosted non-goal, MM/DC4 separation). Real points — not N/A. PoC $0. No Cognito inventing. Keep separate from #3–#7 feature plans except cross-refs.

## Itemized security points (Dev Plan must weave)

1. **L1 LICENSE task** — Plan schedules verify/add Apache-2.0 `LICENSE` at repo root; forbids proprietary license inventing.

2. **L2 Public-repo docs task** — Plan schedules README/docs stating canonical `https://github.com/ioaikh/dealoware`; forbids private-only inventing.

3. **Public-repo secrets hygiene** — Plan requires chore deliverables never commit secrets, API keys, cloud creds, MM/DC4 test logins, SFTP credentials, or inventory dumps.

4. **L3 Hosted non-goal docs** — Plan schedules documentation that free-hosted fork ≠ “the” platform; hosted remains AIKnowHow/Dealoware — without ownership inventing or IdP provision tasks.

5. **No hosted-platform inventing** — Plan excludes Cognito/SSO, prod App Runner/ECS spend, and “official hosted SaaS” as #8 deliverables; PoC runtime stays local/$0.

6. **MotorMarket / DC4 separation** — Plan explicitly excludes MM/DC4 live systems, inventory, SFTP, and test logins from Dealoware chore work.

7. **No inventing product features** — Plan cites #8 AC+OUT only: no marketing claims; no new domain APIs; no Strategy/AI/settlement/identity-seal under this chore.

8. **Cross-story non-merge** — Plan keeps L1–L3 separate from #3–#7 feature plans except cross-refs; does not rewrite those Stories.

9. **Cost / spend guardrail** — Plan affirms PoC **$0** AWS/IdP for this chore; cost/critical → COO → CEO if spend proposed.

10. **Handshake close** — Dev Plan QA must **not** PASS until Security QA confirms these points.

## Handshake next
Senior Dev Planner weaves → Senior Security → Security QA → Chief Security PASS/HOLD to CPM + Chief Dev Planner.

## Cost/critical
No AWS/IdP spend. Cost/critical → COO → CEO.
