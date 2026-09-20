# Proposed SA architecture-review moments — MVP (pre-#18)

**Date:** 2026-09-20  
**From:** Chief Architect  
**To:** CPM (schedule GitHub `gate:sa-arch-review` issues)  
**Status:** **SCHEDULED** 2026-09-20 (CEO unlocked scheduling). GitHub: #24 A · #25 B · #26 C · #27 CLOSE. **#18 product pipeline still CEO unlock** (separate from these gates).  
**ORG-OPS:** § SA architecture reviews · templates under `architecture/templates/`  
**PoC $0**

## Floor already closed
| Moment | Status |
|--------|--------|
| PoC #3–#8 post-delivery review | **PASS** 2026-09-20 — `architecture/2026-09-20__sa__architecture__poc-post-delivery-review.md` |

## Proposed MVP stage-split (additive; schedule when #18 unlocks / MVP SA phase starts)
Each moment uses deliverable shape: intent / deviations / fit / plan-or-escalate + Security handshake. **Next stage HOLD** until CA PASS.

| Moment ID | Name | Trigger | Scope under review | HOLD until CA PASS |
|-----------|------|---------|--------------------|--------------------|
| SA-REV-MVP-A | MVP Stage A — Core domain harden | After MVP Stories for Artifact Update/Delete + full registration/auth land (or SA architecture for that slice closes) | Artifact CRUD complete; auth registration/password-or-productized path per Product lock; host still ECS Express sketch / $0 | Stage B Spec/eng |
| SA-REV-MVP-B | MVP Stage B — Discovery + Strategy + contact-on-accept | After MVP architecture/delivery for instant search + minimal Strategy CRUD + identity contact-on-accept | Discovery surface; Strategy CRUD min; seal→contact on Accept; no MM/DC4 | Stage C Spec/eng |
| SA-REV-MVP-C | MVP Stage C — Thin Assistant + budgets + UI/bot | After MVP architecture/delivery for thin AI Assistant + A8-min meters/hard budgets + basic UI or first-party bot | Assistant boundary + budgets; UI/bot surface; OTel/audit/idempotent offers hooks | MVP milestone close |
| SA-REV-MVP-CLOSE | MVP post-milestone architecture review | CPM declares MVP milestone closed (all in-scope Stories done) or CEO names | Full MVP vs SA baselines + feasibility § MVP add-ons; fit to V1+ | V1 phase unlock |

## Notes
- Stage-split required because MVP is large/long vs PoC (ORG-OPS).
- Host currency: ECS Express Mode `open`; App Runner excluded.
- Do not invent Stories; PM owns Story filing after CEO #18 unlock.
- First GitHub issue per moment when CPM/PM open MVP SA phase — label `gate:sa-arch-review`.

## Hand-off
**Done:** Bot Manager filed gate issues on CEO unlock of scheduling:
| Moment | Issue |
|--------|-------|
| SA-REV-MVP-A | https://github.com/ioaikh/dealoware/issues/24 |
| SA-REV-MVP-B | https://github.com/ioaikh/dealoware/issues/25 |
| SA-REV-MVP-C | https://github.com/ioaikh/dealoware/issues/26 |
| SA-REV-MVP-CLOSE | https://github.com/ioaikh/dealoware/issues/27 |

CPM: EOD miss-check applies; arm 15m watch when each gate enters pipeline (not while backlog). Do **not** start #18 Spec/SD until CEO unlocks #18.
