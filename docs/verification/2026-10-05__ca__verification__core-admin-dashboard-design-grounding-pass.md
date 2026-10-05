# CA design grounding PASS — Core admin dashboard (SA-REV-CORE-ADMIN)

| Field | Value |
|-------|--------|
| Author | Dealoware Chief Architect |
| Date | 2026-10-05 |
| Verdict | **PASS** — CA design grounding PASS |
| Moment | SA-REV-CORE-ADMIN |
| Disposition | Soft HOLD SoR CLEAR for Spec+SA cite / BM routing. Soft HOLD build / Stories / code / CDK / spend / provision / deploy. |
| Host | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** |
| PoC | **$0** |
| DOC-FLOW | `verification/2026-10-05__ca__verification__core-admin-dashboard-design-grounding-pass.md` |

**Not a build unlock.** Soft HOLD harden redeploy (H1) stands separately. No AWS account / CDK / bot-platform internals invented here.

## Option A pick (binding)

Same Core modular monolith; separate Core admin route surface at `admin.core.dealoware.com`; distinct **CoreOwner** principal; FieldPolicy dual wall (Option A §3a/§3b; cite, do not rewrite); no parallel admin ACL. O1 human users dropped. Platform-admin host replaced. Four must-covers bound: password CoreOwner auth + server-side session (8h/30m); modal confirm + API confirm token; append-only audit same-transaction; soft-delete `DeletedAt` + Spec cascade defaults (Artifact block-while-referenced; Participant cascades negotiations/offers, not Artifacts).

## Evidence Soft HOLD SoR pack

| Artifact | Path | Status |
|----------|------|--------|
| Binding Product | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | Binding |
| Binding Spec | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | Spec QA PASS |
| Spec QA | `verification/2026-10-05__spec__verification__core-admin-dashboard.md` | PASS |
| Spec Security qa-confirm | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` | PASS 10/10 |
| Architecture Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | Answers 1–10 MET |
| SA Security checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` | ISSUED |
| SA Security qa-confirm Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` | PASS 10/10 |
| Arch QA review (formal PASS) | `verification/2026-10-05__sa__verification__core-admin-dashboard-review.md` | Formal Arch QA PASS |

Arch QA formal PASS alone is not CA PASS; this file is the CA design grounding PASS Soft HOLD SoR.

## Locks that remain (do not lift)

- invent Stories
- invent build / implementation / code / CDK / spend / provision / deploy
- Soft HOLD until harden live QA PASS after app image bake, then a separate unlock
- inbound bot connector (comes after)
- platform admin hosts; human-user list on Core; Participant UI as admin
- SSO/IdP as delivered; App Runner; MotorMarket/DC4; settlement/escrow/checkout
- AWS account information / CDK details / bot-platform internals in public Soft HOLD SoR
- PoC **$0**

## Disposition

CA design grounding **PASS**. Soft HOLD SoR CLEAR for CPM → Bot Manager Spec+SA path send. Soft HOLD build. PoC **$0**. Not a build unlock.
