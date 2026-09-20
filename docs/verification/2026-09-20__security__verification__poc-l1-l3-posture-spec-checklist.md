# Security checklist — Spec · PoC Standing license/repo/hosted posture L1–L3 (#8)

**Status:** Chief Security itemized points for Spec step (handshake per `ops/ORG-OPS.md`). Issue **before** Spec QA PASS.  
**Date:** 2026-09-20  
**Author:** Dealoware Chief Security  
**Story:** GitHub issue #8 · Standing L1–L3 chore (not a feature Story)  
**Issue:** https://github.com/ioaikh/dealoware/issues/8  
**Depends on:** None (parallel with #3–#7; no domain API dependency)  
**Hold:** Do **not** invent product features, marketing claims, license changes, or hosted-platform ownership changes  
**DOC-FLOW:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-checklist.md`

## Scope note
Spec-binding checklist for the **chore** only: Apache-2.0 LICENSE (L1), public-repo posture (L2), hosted non-goal AIKnowHow/Dealoware (L3), and MotorMarket/DC4 separation. **Do not invent AC** beyond issue #8. Real points — not N/A. PoC $0. No Cognito/SSO inventing. No MotorMarket/DC4 secrets or live-system coupling.

## Itemized security points (Spec must bind)

1. **L1 LICENSE correctness** — Spec binds Apache-2.0 LICENSE present/correct as the OSS license; no invented proprietary/commercial license claims for the public repo.

2. **L2 Public-repo posture** — Spec binds public GitHub `https://github.com/ioaikh/dealoware` as the canonical public posture in README/docs; no conflicting “private-only” or dual-repo inventing.

3. **Public-repo secrets hygiene** — Spec requires that public-repo docs/artifacts for this chore **never** commit secrets, API keys, cloud credentials, MotorMarket/DC4 test logins, SFTP credentials, or inventory dumps.

4. **L3 Hosted non-goal** — Spec documents that a free-hosted fork is **not** “the” Dealoware platform; hosted remains AIKnowHow / Dealoware — without inventing new hosted ownership or requiring AWS/IdP provision in PoC.

5. **No PoC hosted-platform inventing** — Spec must **not** schedule Cognito/SSO, production App Runner/ECS spend, or “official hosted SaaS” as #8 deliverables; PoC stays local/$0 for runtime.

6. **MotorMarket / DC4 separation** — Spec explicitly keeps MotorMarket/DC4 live systems, inventory, SFTP, and test logins **out** of Dealoware Spec/docs/code for this chore (and cites standing separation).

7. **No inventing product features** — Spec cites #8 AC + OUT only: no marketing claims; no new domain APIs; no Strategy/AI/settlement/identity-seal inventing under this chore.

8. **Cross-story non-merge** — Spec keeps L1–L3 posture separate from #3–#7 feature Specs except cross-refs; does not rewrite Artifact/auth/negotiation/identity-seal AC.

9. **Cost / spend guardrail** — Spec affirms PoC **$0** AWS/IdP for this chore; cost/critical → COO → CEO if any spend is proposed.

10. **Traceability + handshake** — Spec cites issue #8 AC + OUT only. Maps these Security points for Dev Plan/SD. Spec QA must **not** PASS until Security QA confirms.

## Handshake next
1. Senior Spec weaves/answers points in Spec (cite sections).
2. Senior Security reviews → done-list to Security QA.
3. Security QA PASS to Chief Security (or further instructions).
4. Chief Security PASS/HOLD to CPM + Chief Spec.

## Cost/critical
No AWS / IdP spend. Cost/critical → COO → CEO.
