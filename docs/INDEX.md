# Dealoware Documentation Knowledge Base — INDEX

**Purpose:** Living, discoverable index of Dealoware agent-ops documentation. Canonical root: `/workspace/dealoware-kb/` on the shared agent computer.

**Product alignment rule:** Two distinct briefs — original `product/CEO-ORIGINAL-BRIEF.md` + summary `product/PRODUCT-BRIEF.md` (do not collapse). Conflicts → escalate **PM → Product → CEO**. Do not invent Stories or requirements.

**Ownership:** Doc Team owns structure and this index. Hourly weekday rebuild from **hash/mtime deltas only** (see [meta/INDEX-DELTA.md](meta/INDEX-DELTA.md)). Full flow: [meta/DOC-FLOW.md](meta/DOC-FLOW.md).

**State file:** [`.doc-index-state.json`](.doc-index-state.json) — path → `{sha256, mtime}`; do not re-read unchanged docs.

---

## Area folders

| Area | Path | Owners (triad) | README |
|------|------|----------------|--------|
| Product | [product/](product/) | Product Team | [product/README.md](product/README.md) |
| Architecture | [architecture/](architecture/) | SA Team | [architecture/README.md](architecture/README.md) |
| Specs | [specs/](specs/) | Spec Team | [specs/README.md](specs/README.md) |
| Plans | [plans/](plans/) | Dev Plan Team | [plans/README.md](plans/README.md) |
| QA | [qa/](qa/) | QA Team | [qa/README.md](qa/README.md) |
| Verification | [verification/](verification/) | Security / DevOps / etc. | [verification/README.md](verification/README.md) |
| Ops | [ops/](ops/) | Business / COO (+ CEO critical) | [ops/README.md](ops/README.md) |
| Meta (Doc process) | [meta/](meta/) | Doc Team | [meta/README.md](meta/README.md) |
| Index fragments | [index/](index/) | Doc Team | [index/README.md](index/README.md) |

---

## Key seed docs

- [product/CEO-ORIGINAL-BRIEF.md](product/CEO-ORIGINAL-BRIEF.md) — **original** verbatim CEO specification (do not overwrite with summary)
- [product/PRODUCT-BRIEF.md](product/PRODUCT-BRIEF.md) — **processed summary** (Product may refresh; goals, domain, claims lock, licensing)
- [ops/ORG-OPS.md](ops/ORG-OPS.md) — agent ops charter, pipeline, triads, doc ownership
- [meta/DOC-FLOW.md](meta/DOC-FLOW.md) — all-team publish flow (naming, handoff, mirror map)
- [meta/INDEX-DELTA.md](meta/INDEX-DELTA.md) — hourly delta index procedure (no full re-scan)

Root stubs (redirect only): [PRODUCT-BRIEF.md](PRODUCT-BRIEF.md) → product/; [ORG-OPS.md](ORG-OPS.md) → ops/

---

## product/

- [CEO-ORIGINAL-BRIEF.md](product/CEO-ORIGINAL-BRIEF.md) — **original** (verbatim CEO; already mirrored to GitHub `docs/product/`)
- [PRODUCT-BRIEF.md](product/PRODUCT-BRIEF.md) — **processed summary** (Product-owned refresh; GitHub `docs/product/PRODUCT-BRIEF.md`; must stay distinct from original)
- [2026-09-20__product__note__participant-data-isolation-mvp.md](product/2026-09-20__product__note__participant-data-isolation-mvp.md)
- [2026-09-20__product__guide__poc-negotiation-scenarios.md](product/2026-09-20__product__guide__poc-negotiation-scenarios.md)

---

## architecture/

- [2026-09-10__sa__architecture__poc-feasibility-roadmap.md](architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md) — PoC feasibility roadmap (SA)
- [2026-09-10__sa__architecture__poc-o10-scaffold.md](architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md) — PoC O10 scaffold (SA)
- [2026-09-20__sa__architecture__poc-post-delivery-review-brief.md](architecture/2026-09-20__sa__architecture__poc-post-delivery-review-brief.md)
- [2026-09-20__sa__architecture__poc-post-delivery-review.md](architecture/2026-09-20__sa__architecture__poc-post-delivery-review.md) — SA post-PoC delivery review — PASS
- [2026-09-20__sa__architecture__mvp-proposed-arch-review-moments.md](architecture/2026-09-20__sa__architecture__mvp-proposed-arch-review-moments.md)
- [2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md](architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md)
- [2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md](architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md) — SA architecture Stage A Field ACL (#31) + account list fail-closed (#32) options (cross-ref; #32 OUT of #31 Doc weave)
- [sa-architecture-options-brief-template.md](architecture/templates/sa-architecture-options-brief-template.md)
- [sa-architecture-stage-review-template.md](architecture/templates/sa-architecture-stage-review-template.md)

## specs/


- [2026-09-10__spec__spec__poc-o10-scaffold.md](specs/2026-09-10__spec__spec__poc-o10-scaffold.md) — Spec #3 O10 scaffold
- [2026-09-10__spec__spec__poc-artifact-d1-d5.md](specs/2026-09-10__spec__spec__poc-artifact-d1-d5.md) — Spec #4 Artifact D1–D5 (dated 09-10 copy)
- [2026-09-11__spec__spec__poc-artifact-d1-d5.md](specs/2026-09-11__spec__spec__poc-artifact-d1-d5.md) — Spec #4 Artifact D1–D5
- [2026-09-11__spec__spec__poc-participant-d6-auth.md](specs/2026-09-11__spec__spec__poc-participant-d6-auth.md) — Spec #5 Participant/Auth D6
- [2026-09-11__cq__assessment__poc-artifact-d1-d5-no-refactor.md](specs/2026-09-11__cq__assessment__poc-artifact-d1-d5-no-refactor.md)
- [2026-09-11__cq__assessment__poc-participant-d6-auth-no-refactor.md](specs/2026-09-11__cq__assessment__poc-participant-d6-auth-no-refactor.md)
- [2026-09-20__cq__assessment__poc-negotiation-d7-d10-no-refactor.md](specs/2026-09-20__cq__assessment__poc-negotiation-d7-d10-no-refactor.md) — CQ no-refactor #6 Negotiation D7–D10
- [2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md](specs/2026-09-20__spec__spec__poc-negotiation-offers-d7-d10.md) — Spec #6 Negotiation + Offers D7–D10/P4
- [2026-09-20__cq__assessment__poc-identity-seal-stub-no-refactor.md](specs/2026-09-20__cq__assessment__poc-identity-seal-stub-no-refactor.md) — CQ no-refactor #7 Identity-seal stub
- [2026-09-20__spec__spec__poc-identity-seal-stub.md](specs/2026-09-20__spec__spec__poc-identity-seal-stub.md) — Spec #7 Identity-seal stub (opaque ids; Accept=state-only)
- [2026-09-20__cq__assessment__poc-standing-l1-l3-posture-no-refactor.md](specs/2026-09-20__cq__assessment__poc-standing-l1-l3-posture-no-refactor.md) — CQ no-refactor #8 Standing L1–L3 posture (chore)
- [2026-09-20__spec__spec__poc-standing-l1-l3-posture.md](specs/2026-09-20__spec__spec__poc-standing-l1-l3-posture.md) — Spec #8 Standing L1–L3 posture (LICENSE / public repo / hosted non-goal)
- [2026-09-21__cq__assessment__mvp-stage-a-account-list-fail-closed-no-refactor.md](specs/2026-09-21__cq__assessment__mvp-stage-a-account-list-fail-closed-no-refactor.md) — CQ no-refactor assessment Account list fail-closed #32
- [2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md](specs/2026-09-21__spec__spec__mvp-stage-a-account-list-fail-closed.md) — Spec Account list fail-closed #32 (Stage A Neg/Offer/Artifact list+get)
- [2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md](specs/2026-09-21__spec__spec__mvp-stage-a-field-acl-registry.md) — Spec Field ACL registry #31 (Doc catch-up INDEX; separate from #32; overall Doc HOLD)
- [2026-09-21__cq__assessment__mvp-stage-a-field-acl-registry-no-refactor.md](specs/2026-09-21__cq__assessment__mvp-stage-a-field-acl-registry-no-refactor.md) — CQ no-refactor assessment Field ACL registry #31 (cq:no-refactor)
---

## plans/

- [2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md](plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md) — general release roadmap PoC→MVP→V1–V5 (accepted; indexed 2026-09-10)
- [2026-09-10__ba__note__story-4-artifact-api-depth.md](plans/2026-09-10__ba__note__story-4-artifact-api-depth.md)
- [2026-09-10__ba__note__story-5-poc-auth.md](plans/2026-09-10__ba__note__story-5-poc-auth.md)
- [2026-09-10__pm__plan__poc-github-backlog.md](plans/2026-09-10__pm__plan__poc-github-backlog.md)
- [2026-09-10__devplan__plan__poc-o10-scaffold.md](plans/2026-09-10__devplan__plan__poc-o10-scaffold.md) — Dev Plan #3 O10 scaffold
- [2026-09-11__devplan__plan__poc-artifact-d1-d5.md](plans/2026-09-11__devplan__plan__poc-artifact-d1-d5.md) — Dev Plan #4 Artifact D1–D5
- [2026-09-11__devplan__plan__poc-participant-d6-auth.md](plans/2026-09-11__devplan__plan__poc-participant-d6-auth.md) — Dev Plan #5 Participant/Auth D6
- [BRIEF-1-SENIOR-MARKETING-PACKAGE.md](plans/marketing/BRIEF-1-SENIOR-MARKETING-PACKAGE.md)
- [2026-09-20__devplan__plan__poc-negotiation-offers-d7-d10.md](plans/2026-09-20__devplan__plan__poc-negotiation-offers-d7-d10.md) — Dev Plan #6 Negotiation + Offers D7–D10/P4
- [2026-09-20__devplan__plan__poc-identity-seal-stub.md](plans/2026-09-20__devplan__plan__poc-identity-seal-stub.md) — Dev Plan #7 Identity-seal stub
- [2026-09-20__devplan__plan__poc-standing-l1-l3-posture.md](plans/2026-09-20__devplan__plan__poc-standing-l1-l3-posture.md) — Dev Plan #8 Standing L1–L3 posture (chore)
- [2026-09-21__ba__note__story-31-field-acl-registry-api-projection.md](plans/2026-09-21__ba__note__story-31-field-acl-registry-api-projection.md) — BA note Story #31 Field ACL (Doc catch-up INDEX)
- [2026-09-21__ba__note__story-32-account-list-failclosed.md](plans/2026-09-21__ba__note__story-32-account-list-failclosed.md) — BA note Story #32 Account list fail-closed
- [2026-09-21__devplan__plan__mvp-stage-a-account-list-fail-closed.md](plans/2026-09-21__devplan__plan__mvp-stage-a-account-list-fail-closed.md) — Dev Plan Account list fail-closed #32
- [2026-09-21__devplan__plan__mvp-stage-a-field-acl-registry.md](plans/2026-09-21__devplan__plan__mvp-stage-a-field-acl-registry.md) — Dev Plan Field ACL registry #31 (on main docs/; Doc catch-up INDEX — overall Doc HOLD)
## qa/


- [2026-09-10__qa__qa-report__poc-o10-scaffold.md](qa/2026-09-10__qa__qa-report__poc-o10-scaffold.md) — Product QA report for O10 scaffold (**PASS**; Sec10 cleared via productqa-qa-confirm)
- [2026-09-11__qa__qa-report__poc-artifact-d1-d5.md](qa/2026-09-11__qa__qa-report__poc-artifact-d1-d5.md) — Product QA report for Artifact D1–D5 (**PASS**; Sec10 closed via productqa-qa-confirm)
- [2026-09-11__qa__qa-report__poc-participant-d6-auth.md](qa/2026-09-11__qa__qa-report__poc-participant-d6-auth.md) — Product QA report for Participant D6 auth (**PASS**; Sec10 closed via productqa-qa-confirm)
- [2026-09-20__qa__qa-report__poc-negotiation-offers-d7-d10.md](qa/2026-09-20__qa__qa-report__poc-negotiation-offers-d7-d10.md) — Product QA report for Negotiation D7–D10 (**PASS**; Sec10 closed via productqa-qa-confirm)
- [2026-09-20__qa__qa-report__poc-identity-seal-stub.md](qa/2026-09-20__qa__qa-report__poc-identity-seal-stub.md) — Product QA report for Identity-seal stub (**PASS**; Sec10 closed via productqa-qa-confirm; overall Doc step #7 PASS after Security handshake)
- [2026-09-20__qa__qa-report__poc-standing-l1-l3-posture.md](qa/2026-09-20__qa__qa-report__poc-standing-l1-l3-posture.md) — Product QA report for Standing L1–L3 posture (#8 chore) (**PASS**; Sec10 closed via productqa-qa-confirm; overall Doc step #8 PASS after Security handshake)
- [2026-09-20__qa__qa-report__poc-nightly-autotest-inventory-plan.md](qa/2026-09-20__qa__qa-report__poc-nightly-autotest-inventory-plan.md)
- [2026-09-20__qa__qa-report__poc-scenario-autotest-suite.md](qa/2026-09-20__qa__qa-report__poc-scenario-autotest-suite.md)
- [2026-09-21__qa__qa-report__mvp-stage-a-account-list-fail-closed.md](qa/2026-09-21__qa__qa-report__mvp-stage-a-account-list-fail-closed.md) — Product QA report Account list fail-closed #32 (**PASS**; Sec10 closed via productqa-qa-confirm; overall Doc HOLD — not Doc PASS)
- [2026-09-21__qa__qa-report__poc-nightly-autotest-run.md](qa/2026-09-21__qa__qa-report__poc-nightly-autotest-run.md)
- [2026-09-21__qa__qa-report__poc-nightly-chief-analysis.md](qa/2026-09-21__qa__qa-report__poc-nightly-chief-analysis.md)
- [2026-09-21__qa__qa-report__poc-security-hygiene-soft-tests.md](qa/2026-09-21__qa__qa-report__poc-security-hygiene-soft-tests.md)
- [2026-09-21__qa__trx__poc-nightly-autotest.trx](qa/results/2026-09-21__qa__trx__poc-nightly-autotest.trx)
- [2026-09-21__qa__qa-report__mvp-stage-a-field-acl-registry.md](qa/2026-09-21__qa__qa-report__mvp-stage-a-field-acl-registry.md) — Product QA report Field ACL registry #31 (**PASS**; Sec10 closed via productqa-qa-confirm; overall Doc HOLD — not Doc PASS)
---

## verification/

- [2026-09-10__devops__verification__index-delta-helper.md](verification/2026-09-10__devops__verification__index-delta-helper.md)
- [2026-09-10__product__verification__product-brief-refresh.md](verification/2026-09-10__product__verification__product-brief-refresh.md)
- [2026-09-10__sa__verification__poc-feasibility-roadmap.md](verification/2026-09-10__sa__verification__poc-feasibility-roadmap.md) — SA verification of PoC feasibility roadmap
- [2026-09-10__devops__verification__index-delta-additive-patch.md](verification/2026-09-10__devops__verification__index-delta-additive-patch.md) — DevOps QA evidence: additive-only INDEX delta patch
- [2026-09-10__sa__verification__poc-o10-scaffold.md](verification/2026-09-10__sa__verification__poc-o10-scaffold.md) — SA verification of PoC O10 scaffold
- [2026-09-10__sa__verification__host-shape-ecs-express-patch.md](verification/2026-09-10__sa__verification__host-shape-ecs-express-patch.md) — SA verification: host-shape ECS Express patch
- [2026-09-10__security__verification__poc-o10-sa-checklist.md](verification/2026-09-10__security__verification__poc-o10-sa-checklist.md) — Security points on #3 O10 SA checklist
- [2026-09-10__sa__verification__poc-o10-security-amend.md](verification/2026-09-10__sa__verification__poc-o10-security-amend.md) — SA amend for Security O10 points
- [2026-09-10__security__verification__poc-o10-sa-points-review.md](verification/2026-09-10__security__verification__poc-o10-sa-points-review.md) — Security points review on #3 O10
- [2026-09-10__security__verification__poc-o10-sa-qa-confirm.md](verification/2026-09-10__security__verification__poc-o10-sa-qa-confirm.md) — Security QA confirm on #3 O10
- [2026-09-10__security__verification__poc-o10-spec-checklist.md](verification/2026-09-10__security__verification__poc-o10-spec-checklist.md) — Security checklist on Spec #3 O10
- [2026-09-10__spec__verification__poc-o10-scaffold.md](verification/2026-09-10__spec__verification__poc-o10-scaffold.md) — Spec verification of O10 scaffold
- [2026-09-10__security__verification__poc-o10-spec-points-review.md](verification/2026-09-10__security__verification__poc-o10-spec-points-review.md) — Security points review on Spec #3 O10
- [2026-09-10__security__verification__poc-o10-spec-qa-confirm.md](verification/2026-09-10__security__verification__poc-o10-spec-qa-confirm.md) — Security QA confirm on Spec #3 O10
- [2026-09-10__security__verification__poc-o10-devplan-checklist.md](verification/2026-09-10__security__verification__poc-o10-devplan-checklist.md) — Security checklist on Dev Plan #3 O10
- [2026-09-10__spec__verification__poc-o10-security-amend.md](verification/2026-09-10__spec__verification__poc-o10-security-amend.md) — Spec verification of O10 Security amend
- [2026-09-10__security__verification__poc-o10-devplan-points-review.md](verification/2026-09-10__security__verification__poc-o10-devplan-points-review.md) — Security points review on Dev Plan #3 O10
- [2026-09-10__security__verification__poc-o10-devplan-qa-confirm.md](verification/2026-09-10__security__verification__poc-o10-devplan-qa-confirm.md) — Security QA confirm on Dev Plan #3 O10
- [2026-09-10__devplan__verification__poc-o10-scaffold.md](verification/2026-09-10__devplan__verification__poc-o10-scaffold.md) — Dev Plan QA verify of O10 scaffold
- [2026-09-10__security__verification__poc-o10-sd-checklist.md](verification/2026-09-10__security__verification__poc-o10-sd-checklist.md) — Security checklist on SD #3 O10
- [2026-09-10__security__verification__poc-o10-sd-points-review.md](verification/2026-09-10__security__verification__poc-o10-sd-points-review.md) — Security points review on SD #3 O10
- [2026-09-10__security__verification__poc-o10-sd-qa-confirm.md](verification/2026-09-10__security__verification__poc-o10-sd-qa-confirm.md) — Security QA confirm on SD #3 O10
- [2026-09-10__sd__verification__poc-o10-scaffold.md](verification/2026-09-10__sd__verification__poc-o10-scaffold.md) — SD verification of O10 scaffold
- [2026-09-10__security__verification__poc-o10-productqa-checklist.md](verification/2026-09-10__security__verification__poc-o10-productqa-checklist.md) — Security checklist on Product QA #3 O10
- [2026-09-10__security__verification__poc-o10-productqa-qa-confirm.md](verification/2026-09-10__security__verification__poc-o10-productqa-qa-confirm.md) — Security QA confirm on Product QA #3 O10
- [2026-09-10__security__verification__poc-o10-productqa-points-review.md](verification/2026-09-10__security__verification__poc-o10-productqa-points-review.md) — Security points review on Product QA #3 O10
- [2026-09-10__security__verification__poc-o10-doc-checklist.md](verification/2026-09-10__security__verification__poc-o10-doc-checklist.md) — Security checklist on Doc #3 O10
- [2026-09-10__security__verification__poc-o10-doc-points-review.md](verification/2026-09-10__security__verification__poc-o10-doc-points-review.md) — Security points review on Doc #3 O10
- [2026-09-10__security__verification__poc-o10-doc-qa-confirm.md](verification/2026-09-10__security__verification__poc-o10-doc-qa-confirm.md) — Security QA confirm on Doc #3 O10
- [2026-09-10__ba__verification__poc-o10-scaffold.md](verification/2026-09-10__ba__verification__poc-o10-scaffold.md) — BA verification of O10 scaffold
- [2026-09-11__security__verification__poc-artifact-spec-checklist.md](verification/2026-09-11__security__verification__poc-artifact-spec-checklist.md) — Security checklist on Spec #4 Artifact
- [2026-09-11__security__verification__poc-artifact-spec-points-review.md](verification/2026-09-11__security__verification__poc-artifact-spec-points-review.md) — Security points review on Spec #4 Artifact
- [2026-09-11__security__verification__poc-artifact-spec-qa-confirm.md](verification/2026-09-11__security__verification__poc-artifact-spec-qa-confirm.md) — Security QA confirm on Spec #4 Artifact
- [2026-09-11__security__verification__poc-auth-spec-checklist.md](verification/2026-09-11__security__verification__poc-auth-spec-checklist.md) — Security checklist on Spec #5 Auth
- [2026-09-11__spec__verification__poc-artifact-d1-d5.md](verification/2026-09-11__spec__verification__poc-artifact-d1-d5.md) — Spec verification of Artifact D1–D5
- [2026-09-11__spec__verification__poc-participant-d6-auth.md](verification/2026-09-11__spec__verification__poc-participant-d6-auth.md) — Spec verification of Participant/Auth D6
- [2026-09-11__security__verification__poc-artifact-devplan-checklist.md](verification/2026-09-11__security__verification__poc-artifact-devplan-checklist.md) — Security checklist on Dev Plan #4 Artifact
- [2026-09-11__security__verification__poc-auth-spec-points-review.md](verification/2026-09-11__security__verification__poc-auth-spec-points-review.md) — Security points review on Spec #5 Auth
- [2026-09-11__security__verification__poc-auth-spec-qa-confirm.md](verification/2026-09-11__security__verification__poc-auth-spec-qa-confirm.md) — Security QA confirm on Spec #5 Auth
- [2026-09-11__security__verification__poc-auth-devplan-checklist.md](verification/2026-09-11__security__verification__poc-auth-devplan-checklist.md) — Security checklist on Dev Plan #5 Auth
- [2026-09-11__devplan__verification__poc-artifact-d1-d5.md](verification/2026-09-11__devplan__verification__poc-artifact-d1-d5.md) — Dev Plan QA verify of Artifact D1–D5
- [2026-09-11__security__verification__poc-artifact-devplan-points-review.md](verification/2026-09-11__security__verification__poc-artifact-devplan-points-review.md) — Security points review on Dev Plan #4 Artifact
- [2026-09-11__security__verification__poc-artifact-devplan-qa-confirm.md](verification/2026-09-11__security__verification__poc-artifact-devplan-qa-confirm.md) — Security QA confirm on Dev Plan #4 Artifact
- [2026-09-11__security__verification__poc-artifact-sd-checklist.md](verification/2026-09-11__security__verification__poc-artifact-sd-checklist.md) — Security checklist on SD #4 Artifact
- [2026-09-11__security__verification__poc-auth-devplan-qa-confirm.md](verification/2026-09-11__security__verification__poc-auth-devplan-qa-confirm.md) — Security QA confirm on Dev Plan #5 Auth
- [2026-09-10__marketing__verification__brief-1-senior-package.md](verification/2026-09-10__marketing__verification__brief-1-senior-package.md)
- [2026-09-11__devplan__verification__poc-participant-d6-auth.md](verification/2026-09-11__devplan__verification__poc-participant-d6-auth.md) — Dev Plan QA verify of Participant/Auth D6
- [2026-09-11__security__verification__poc-auth-devplan-points-review.md](verification/2026-09-11__security__verification__poc-auth-devplan-points-review.md) — Security points review on Dev Plan #5 Auth
- [2026-09-11__security__verification__poc-auth-sd-checklist.md](verification/2026-09-11__security__verification__poc-auth-sd-checklist.md) — Security checklist on SD #5 Auth
- [2026-09-11__security__verification__poc-artifact-sd-points-review.md](verification/2026-09-11__security__verification__poc-artifact-sd-points-review.md) — Security points review on SD #4 Artifact
- [2026-09-11__security__verification__poc-artifact-sd-qa-confirm.md](verification/2026-09-11__security__verification__poc-artifact-sd-qa-confirm.md) — Security QA confirm on SD #4 Artifact
- [2026-09-11__sd__verification__poc-artifact-d1-d5.md](verification/2026-09-11__sd__verification__poc-artifact-d1-d5.md) — SD verification of Artifact D1–D5
- [2026-09-11__security__verification__poc-artifact-productqa-checklist.md](verification/2026-09-11__security__verification__poc-artifact-productqa-checklist.md) — Security checklist on Product QA #4 Artifact
- [2026-09-11__security__verification__poc-artifact-productqa-qa-confirm.md](verification/2026-09-11__security__verification__poc-artifact-productqa-qa-confirm.md) — Security QA confirm on Product QA #4 Artifact
- [2026-09-11__security__verification__poc-artifact-productqa-points-review.md](verification/2026-09-11__security__verification__poc-artifact-productqa-points-review.md) — Security points review on Product QA #4 Artifact
- [2026-09-11__security__verification__poc-artifact-doc-checklist.md](verification/2026-09-11__security__verification__poc-artifact-doc-checklist.md) — Security checklist on Doc #4 Artifact
- [2026-09-11__security__verification__poc-artifact-doc-points-review.md](verification/2026-09-11__security__verification__poc-artifact-doc-points-review.md) — Security points review on Doc #4 Artifact
- [2026-09-11__security__verification__poc-artifact-doc-qa-confirm.md](verification/2026-09-11__security__verification__poc-artifact-doc-qa-confirm.md) — Security QA confirm on Doc #4 Artifact
- [2026-09-11__ba__verification__poc-artifact-d1-d5.md](verification/2026-09-11__ba__verification__poc-artifact-d1-d5.md) — BA business verification of Artifact D1–D5 (Senior BA PASS; pending BAQA)
- [2026-09-11__sd__verification__poc-participant-d6-auth.md](verification/2026-09-11__sd__verification__poc-participant-d6-auth.md) — SD verification of Participant/Auth D6
- [2026-09-11__security__verification__poc-auth-sd-points-review.md](verification/2026-09-11__security__verification__poc-auth-sd-points-review.md) — Security points review on SD #5 Auth
- [2026-09-11__security__verification__poc-auth-sd-qa-confirm.md](verification/2026-09-11__security__verification__poc-auth-sd-qa-confirm.md) — Security QA PASS (10/10) for PoC Participant auth SD (#5)
- [2026-09-11__security__verification__poc-auth-productqa-checklist.md](verification/2026-09-11__security__verification__poc-auth-productqa-checklist.md) — Security checklist for Product QA · PoC Participant auth D6 (#5)
- [2026-09-11__security__verification__poc-auth-doc-checklist.md](verification/2026-09-11__security__verification__poc-auth-doc-checklist.md) — Security checklist on Doc #5 Auth
- [2026-09-11__security__verification__poc-auth-productqa-qa-confirm.md](verification/2026-09-11__security__verification__poc-auth-productqa-qa-confirm.md) — Security QA confirm on Product QA #5 Auth
- [2026-09-11__security__verification__poc-auth-doc-points-review.md](verification/2026-09-11__security__verification__poc-auth-doc-points-review.md) — Security points review on Doc #5 Auth
- [2026-09-11__security__verification__poc-auth-doc-qa-confirm.md](verification/2026-09-11__security__verification__poc-auth-doc-qa-confirm.md) — Security QA confirm on Doc #5 Auth
- [2026-09-11__security__verification__poc-auth-productqa-points-review.md](verification/2026-09-11__security__verification__poc-auth-productqa-points-review.md)
- [2026-09-15__security__verification__research-02-00-et.md](verification/2026-09-15__security__verification__research-02-00-et.md)
- [2026-09-15__security__verification__research-12-00-et.md](verification/2026-09-15__security__verification__research-12-00-et.md)
- [2026-09-11__ba__verification__poc-participant-d6-auth.md](verification/2026-09-11__ba__verification__poc-participant-d6-auth.md) — BA business verification of Participant D6 auth (#5) — PASS (BAQA/CBA closed; issue done)
- [2026-09-16__security__verification__research-02-00-et.md](verification/2026-09-16__security__verification__research-02-00-et.md)
- [2026-09-17__security__verification__research-02-00-et.md](verification/2026-09-17__security__verification__research-02-00-et.md)
- [2026-09-17__security__verification__research-12-00-et.md](verification/2026-09-17__security__verification__research-12-00-et.md)
- [2026-09-18__security__verification__research-02-00-et.md](verification/2026-09-18__security__verification__research-02-00-et.md)
- [2026-09-18__security__verification__research-12-00-et.md](verification/2026-09-18__security__verification__research-12-00-et.md)
- [2026-09-19__security__verification__research-02-00-et.md](verification/2026-09-19__security__verification__research-02-00-et.md)
- [2026-09-19__security__verification__research-12-00-et.md](verification/2026-09-19__security__verification__research-12-00-et.md)
- [2026-09-20__security__verification__research-02-00-et.md](verification/2026-09-20__security__verification__research-02-00-et.md)
- [2026-09-20__devplan__verification__poc-negotiation-offers-d7-d10.md](verification/2026-09-20__devplan__verification__poc-negotiation-offers-d7-d10.md) — Dev Plan QA verify of Negotiation D7–D10
- [2026-09-20__sd__verification__poc-negotiation-offers-d7-d10.md](verification/2026-09-20__sd__verification__poc-negotiation-offers-d7-d10.md) — SD verification of Negotiation D7–D10
- [2026-09-20__security__verification__poc-negotiation-devplan-checklist.md](verification/2026-09-20__security__verification__poc-negotiation-devplan-checklist.md) — Security checklist on Dev Plan #6 Negotiation
- [2026-09-20__security__verification__poc-negotiation-devplan-points-review.md](verification/2026-09-20__security__verification__poc-negotiation-devplan-points-review.md) — Security points review on Dev Plan #6 Negotiation
- [2026-09-20__security__verification__poc-negotiation-devplan-qa-confirm.md](verification/2026-09-20__security__verification__poc-negotiation-devplan-qa-confirm.md) — Security QA confirm on Dev Plan #6 Negotiation
- [2026-09-20__security__verification__poc-negotiation-doc-checklist.md](verification/2026-09-20__security__verification__poc-negotiation-doc-checklist.md) — Security checklist on Doc #6 Negotiation (**overall Doc step #6 PASS**; Security QA cleared via doc-qa-confirm)
- [2026-09-20__security__verification__poc-negotiation-productqa-checklist.md](verification/2026-09-20__security__verification__poc-negotiation-productqa-checklist.md) — Security checklist on Product QA #6 Negotiation
- [2026-09-20__security__verification__poc-negotiation-productqa-qa-confirm.md](verification/2026-09-20__security__verification__poc-negotiation-productqa-qa-confirm.md) — Security QA confirm on Product QA #6 Negotiation (PASS; Sec10 closed)
- [2026-09-20__security__verification__poc-negotiation-sd-checklist.md](verification/2026-09-20__security__verification__poc-negotiation-sd-checklist.md) — Security checklist on SD #6 Negotiation
- [2026-09-20__security__verification__poc-negotiation-sd-points-review.md](verification/2026-09-20__security__verification__poc-negotiation-sd-points-review.md) — Security points review on SD #6 Negotiation
- [2026-09-20__security__verification__poc-negotiation-sd-qa-confirm.md](verification/2026-09-20__security__verification__poc-negotiation-sd-qa-confirm.md) — Security QA confirm on SD #6 Negotiation
- [2026-09-20__security__verification__poc-negotiation-spec-checklist.md](verification/2026-09-20__security__verification__poc-negotiation-spec-checklist.md) — Security checklist on Spec #6 Negotiation
- [2026-09-20__security__verification__poc-negotiation-spec-points-review.md](verification/2026-09-20__security__verification__poc-negotiation-spec-points-review.md) — Security points review on Spec #6 Negotiation
- [2026-09-20__security__verification__poc-negotiation-spec-qa-confirm.md](verification/2026-09-20__security__verification__poc-negotiation-spec-qa-confirm.md) — Security QA confirm on Spec #6 Negotiation
- [2026-09-20__spec__verification__poc-negotiation-offers-d7-d10.md](verification/2026-09-20__spec__verification__poc-negotiation-offers-d7-d10.md) — Spec verification of Negotiation D7–D10
- [2026-09-20__security__verification__poc-negotiation-doc-points-review.md](verification/2026-09-20__security__verification__poc-negotiation-doc-points-review.md) — Security points-review Doc #6 — PASS 10/10
- [2026-09-20__security__verification__poc-negotiation-doc-qa-confirm.md](verification/2026-09-20__security__verification__poc-negotiation-doc-qa-confirm.md) — Security QA Doc #6 — PASS 10/10 (clears overall Doc HOLD)
- [2026-09-20__security__verification__poc-negotiation-productqa-points-review.md](verification/2026-09-20__security__verification__poc-negotiation-productqa-points-review.md) — Product QA Security points-review #6 — PASS (catch-up)
- [2026-09-20__ba__verification__poc-negotiation-offers-d7-d10.md](verification/2026-09-20__ba__verification__poc-negotiation-offers-d7-d10.md) — BA business verification of Negotiation D7–D10 (#6) — PASS (BAQA/CBA closed; issue done)
- [2026-09-20__devplan__verification__poc-identity-seal-stub.md](verification/2026-09-20__devplan__verification__poc-identity-seal-stub.md) — Dev Plan QA verify of Identity-seal stub #7
- [2026-09-20__sd__verification__poc-identity-seal-stub.md](verification/2026-09-20__sd__verification__poc-identity-seal-stub.md) — SD verification of Identity-seal stub #7
- [2026-09-20__security__verification__poc-identity-seal-devplan-checklist.md](verification/2026-09-20__security__verification__poc-identity-seal-devplan-checklist.md) — Security checklist on Dev Plan #7 Identity-seal
- [2026-09-20__security__verification__poc-identity-seal-devplan-points-review.md](verification/2026-09-20__security__verification__poc-identity-seal-devplan-points-review.md) — Security points review on Dev Plan #7 Identity-seal
- [2026-09-20__security__verification__poc-identity-seal-devplan-qa-confirm.md](verification/2026-09-20__security__verification__poc-identity-seal-devplan-qa-confirm.md) — Security QA confirm on Dev Plan #7 Identity-seal
- [2026-09-20__security__verification__poc-identity-seal-doc-checklist.md](verification/2026-09-20__security__verification__poc-identity-seal-doc-checklist.md) — Security checklist on Doc #7 Identity-seal (**overall Doc step #7 PASS**; Security QA cleared via doc-qa-confirm)
- [2026-09-20__security__verification__poc-identity-seal-productqa-checklist.md](verification/2026-09-20__security__verification__poc-identity-seal-productqa-checklist.md) — Security checklist on Product QA #7 Identity-seal
- [2026-09-20__security__verification__poc-identity-seal-productqa-points-review.md](verification/2026-09-20__security__verification__poc-identity-seal-productqa-points-review.md) — Product QA Security points-review #7 — present on disk (indexed; not invented)
- [2026-09-20__security__verification__poc-identity-seal-productqa-qa-confirm.md](verification/2026-09-20__security__verification__poc-identity-seal-productqa-qa-confirm.md) — Security QA confirm on Product QA #7 Identity-seal (PASS; Sec10 closed)
- [2026-09-20__security__verification__poc-identity-seal-sd-checklist.md](verification/2026-09-20__security__verification__poc-identity-seal-sd-checklist.md) — Security checklist on SD #7 Identity-seal
- [2026-09-20__security__verification__poc-identity-seal-sd-points-review.md](verification/2026-09-20__security__verification__poc-identity-seal-sd-points-review.md) — Security points review on SD #7 Identity-seal
- [2026-09-20__security__verification__poc-identity-seal-sd-qa-confirm.md](verification/2026-09-20__security__verification__poc-identity-seal-sd-qa-confirm.md) — Security QA confirm on SD #7 Identity-seal
- [2026-09-20__security__verification__poc-identity-seal-spec-checklist.md](verification/2026-09-20__security__verification__poc-identity-seal-spec-checklist.md) — Security checklist on Spec #7 Identity-seal
- [2026-09-20__security__verification__poc-identity-seal-spec-points-review.md](verification/2026-09-20__security__verification__poc-identity-seal-spec-points-review.md) — Security points review on Spec #7 Identity-seal
- [2026-09-20__security__verification__poc-identity-seal-spec-qa-confirm.md](verification/2026-09-20__security__verification__poc-identity-seal-spec-qa-confirm.md) — Security QA confirm on Spec #7 Identity-seal
- [2026-09-20__security__verification__research-12-00-et.md](verification/2026-09-20__security__verification__research-12-00-et.md)
- [2026-09-20__spec__verification__poc-identity-seal-stub.md](verification/2026-09-20__spec__verification__poc-identity-seal-stub.md) — Spec verification of Identity-seal stub #7
- [2026-09-20__security__verification__poc-identity-seal-doc-points-review.md](verification/2026-09-20__security__verification__poc-identity-seal-doc-points-review.md) — Security points-review Doc #7 — PASS 10/10
- [2026-09-20__security__verification__poc-identity-seal-doc-qa-confirm.md](verification/2026-09-20__security__verification__poc-identity-seal-doc-qa-confirm.md) — Security QA Doc #7 — PASS 10/10 (clears overall Doc HOLD)
- [2026-09-20__ba__verification__poc-identity-seal-stub.md](verification/2026-09-20__ba__verification__poc-identity-seal-stub.md)
- [2026-09-20__devplan__verification__poc-standing-l1-l3-posture.md](verification/2026-09-20__devplan__verification__poc-standing-l1-l3-posture.md) — Dev Plan QA verify of Standing L1–L3 posture #8
- [2026-09-20__sd__verification__poc-standing-l1-l3-posture.md](verification/2026-09-20__sd__verification__poc-standing-l1-l3-posture.md) — SD verification of Standing L1–L3 posture #8
- [2026-09-20__security__verification__poc-l1-l3-posture-devplan-checklist.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-devplan-checklist.md) — Security checklist on Dev Plan #8 Standing L1–L3
- [2026-09-20__security__verification__poc-l1-l3-posture-devplan-points-review.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-devplan-points-review.md) — Security points review on Dev Plan #8 Standing L1–L3
- [2026-09-20__security__verification__poc-l1-l3-posture-devplan-qa-confirm.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-devplan-qa-confirm.md) — Security QA confirm on Dev Plan #8 Standing L1–L3
- [2026-09-20__security__verification__poc-l1-l3-posture-doc-checklist.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-doc-checklist.md) — Security checklist on Doc #8 Standing L1–L3 (**overall Doc step #8 PASS**; Security QA cleared via doc-qa-confirm)
- [2026-09-20__security__verification__poc-l1-l3-posture-productqa-checklist.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-productqa-checklist.md) — Security checklist on Product QA #8 Standing L1–L3
- [2026-09-20__security__verification__poc-l1-l3-posture-productqa-points-review.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-productqa-points-review.md) — Product QA Security points-review #8 — present on disk (indexed; not invented)
- [2026-09-20__security__verification__poc-l1-l3-posture-productqa-qa-confirm.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-productqa-qa-confirm.md) — Security QA confirm on Product QA #8 Standing L1–L3 (PASS; Sec10 closed)
- [2026-09-20__security__verification__poc-l1-l3-posture-sd-checklist.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-sd-checklist.md) — Security checklist on SD #8 Standing L1–L3
- [2026-09-20__security__verification__poc-l1-l3-posture-sd-points-review.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-sd-points-review.md) — Security points review on SD #8 Standing L1–L3
- [2026-09-20__security__verification__poc-l1-l3-posture-sd-qa-confirm.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-sd-qa-confirm.md) — Security QA confirm on SD #8 Standing L1–L3
- [2026-09-20__security__verification__poc-l1-l3-posture-spec-checklist.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-checklist.md) — Security checklist on Spec #8 Standing L1–L3
- [2026-09-20__security__verification__poc-l1-l3-posture-spec-points-review.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-points-review.md) — Security points review on Spec #8 Standing L1–L3
- [2026-09-20__security__verification__poc-l1-l3-posture-spec-qa-confirm.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-qa-confirm.md) — Security QA confirm on Spec #8 Standing L1–L3
- [2026-09-20__spec__verification__poc-standing-l1-l3-posture.md](verification/2026-09-20__spec__verification__poc-standing-l1-l3-posture.md) — Spec verification of Standing L1–L3 posture #8
- [2026-09-20__security__verification__poc-l1-l3-posture-doc-points-review.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-doc-points-review.md) — Security points-review Doc #8 — PASS 10/10
- [2026-09-20__security__verification__poc-l1-l3-posture-doc-qa-confirm.md](verification/2026-09-20__security__verification__poc-l1-l3-posture-doc-qa-confirm.md) — Security QA Doc #8 — PASS 10/10 (clears overall Doc HOLD)
- [2026-09-20__ba__verification__poc-standing-l1-l3-posture.md](verification/2026-09-20__ba__verification__poc-standing-l1-l3-posture.md)
- [2026-09-20__sa__verification__poc-post-delivery-review.md](verification/2026-09-20__sa__verification__poc-post-delivery-review.md) — SA verify post-PoC delivery review — PASS
- [2026-09-20__security__verification__poc-post-delivery-sa-checklist.md](verification/2026-09-20__security__verification__poc-post-delivery-sa-checklist.md) — Security checklist for SA post-PoC review
- [2026-09-20__security__verification__poc-post-delivery-sa-qa-confirm.md](verification/2026-09-20__security__verification__poc-post-delivery-sa-qa-confirm.md) — Security QA confirm SA post-PoC review — PASS
- [2026-09-20__security__verification__poc-post-delivery-sa-points-review.md](verification/2026-09-20__security__verification__poc-post-delivery-sa-points-review.md)
- [2026-09-21__devplan__verification__mvp-stage-a-account-list-fail-closed.md](verification/2026-09-21__devplan__verification__mvp-stage-a-account-list-fail-closed.md) — Dev Plan verify Account list fail-closed #32
- [2026-09-21__devplan__verification__mvp-stage-a-field-acl-registry.md](verification/2026-09-21__devplan__verification__mvp-stage-a-field-acl-registry.md) — Dev Plan verify Field ACL #31 (Doc catch-up INDEX)
- [2026-09-21__sa__verification__mvp-participant-secrets-acl-integration-ceo-option-a-accept.md](verification/2026-09-21__sa__verification__mvp-participant-secrets-acl-integration-ceo-option-a-accept.md)
- [2026-09-21__sa__verification__mvp-participant-secrets-acl-integration.md](verification/2026-09-21__sa__verification__mvp-participant-secrets-acl-integration.md)
- [2026-09-21__sa__verification__mvp-stage-a-field-acl-list-failclosed.md](verification/2026-09-21__sa__verification__mvp-stage-a-field-acl-list-failclosed.md) — SA/Arch QA verify Field ACL + list-failclosed (#31+#32 cross-ref; #32 OUT of #31 Doc weave; PASS)
- [2026-09-21__sd__verification__mvp-stage-a-account-list-fail-closed.md](verification/2026-09-21__sd__verification__mvp-stage-a-account-list-fail-closed.md) — SD verify Account list fail-closed #32 (PR #34 MERGED)
- [2026-09-21__security__verification__mvp-participant-secrets-acl-sa-checklist.md](verification/2026-09-21__security__verification__mvp-participant-secrets-acl-sa-checklist.md)
- [2026-09-21__security__verification__mvp-participant-secrets-acl-sa-points-review.md](verification/2026-09-21__security__verification__mvp-participant-secrets-acl-sa-points-review.md)
- [2026-09-21__security__verification__mvp-participant-secrets-acl-sa-qa-confirm.md](verification/2026-09-21__security__verification__mvp-participant-secrets-acl-sa-qa-confirm.md)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-checklist.md) — Security checklist Dev Plan #31 Field ACL (Doc catch-up INDEX)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-points-review.md) — Security points-review Dev Plan #31 (Doc catch-up INDEX)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-devplan-qa-confirm.md) — Security QA confirm Dev Plan #31 (Doc catch-up INDEX)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md) — Security checklist SA #31+#32 Stage A (cross-ref; #32 OUT of #31 Doc weave)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-points-review.md) — Security points-review SA #31+#32 (cross-ref; #32 OUT of #31 Doc weave)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-qa-confirm.md) — Security QA confirm SA #31+#32 (cross-ref; #32 OUT of #31 Doc weave)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-sd-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-checklist.md) — Security checklist SD #31 Field ACL (Doc catch-up INDEX; #31 SD track separate from #32)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-spec-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-checklist.md) — Security checklist Spec #31 Field ACL (Doc catch-up INDEX)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-spec-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-points-review.md) — Security points-review Spec #31 (Doc catch-up INDEX)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-spec-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-spec-qa-confirm.md) — Security QA confirm Spec #31 (Doc catch-up INDEX)
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-checklist.md) — Security checklist Dev Plan #32 list-failclosed
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-points-review.md) — Security points-review Dev Plan #32 list-failclosed
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-devplan-qa-confirm.md) — Security QA confirm Dev Plan #32 (PASS 10/10)
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-checklist.md) — Doc Security checklist #32 — ISSUED; cleared by doc-qa-confirm (overall Doc PASS)
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-checklist.md) — Security checklist Product QA #32 list-failclosed
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-points-review.md) — Security points-review Product QA #32 (PASS 10/10; present on disk — not invented)
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-productqa-qa-confirm.md) — Security QA confirm Product QA #32 (PASS; Sec10 closed)
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-checklist.md) — Security checklist SD #32 list-failclosed
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-points-review.md) — Security points-review SD #32 list-failclosed
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-sd-qa-confirm.md) — Security QA confirm SD #32 (PASS 10/10)
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-checklist.md) — Security checklist Spec #32 list-failclosed
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-points-review.md) — Security points-review Spec #32 list-failclosed
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-spec-qa-confirm.md) — Security QA confirm Spec #32 (PASS 10/10)
- [2026-09-21__security__verification__research-02-00-et.md](verification/2026-09-21__security__verification__research-02-00-et.md)
- [2026-09-21__security__verification__research-12-00-et.md](verification/2026-09-21__security__verification__research-12-00-et.md)
- [2026-09-21__spec__verification__mvp-stage-a-account-list-fail-closed.md](verification/2026-09-21__spec__verification__mvp-stage-a-account-list-fail-closed.md) — Spec verify Account list fail-closed #32
- [2026-09-21__spec__verification__mvp-stage-a-field-acl-registry.md](verification/2026-09-21__spec__verification__mvp-stage-a-field-acl-registry.md) — Spec verify Field ACL registry #31 (Doc catch-up INDEX)
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-points-review.md) — Senior Security Doc #32 points-review — PASS 10/10
- [2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-list-failclosed-doc-qa-confirm.md) — Security QA Doc #32 qa-confirm — PASS 10/10 (clears overall Doc HOLD)
- [2026-09-21__ba__verification__mvp-stage-a-account-list-fail-closed.md](verification/2026-09-21__ba__verification__mvp-stage-a-account-list-fail-closed.md) — BA verify Account list fail-closed #32 (additive INDEX; #32 CLOSED — not #31 Doc weave)
- [2026-09-21__sd__verification__mvp-stage-a-field-acl-registry.md](verification/2026-09-21__sd__verification__mvp-stage-a-field-acl-registry.md) — SD / Dev Code QA verify Field ACL #31 (PASS @ PR #37 HEAD; Sec SD closed)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-doc-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-doc-checklist.md) — Doc Security checklist #31 Field ACL — **ISSUED**; HOLD until doc-qa-confirm (overall Doc HOLD)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-checklist.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-checklist.md) — Security checklist Product QA #31 Field ACL
- [2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-points-review.md) — Security points-review Product QA #31 (PASS 10/10; present on disk — not invented)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-productqa-qa-confirm.md) — Security QA confirm Product QA #31 (PASS; Sec10 closed — not overall Doc PASS)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-sd-points-review.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-points-review.md) — Security points-review SD #31 Field ACL (PASS 10/10; present on disk — not invented)
- [2026-09-21__security__verification__mvp-stage-a-field-acl-sd-qa-confirm.md](verification/2026-09-21__security__verification__mvp-stage-a-field-acl-sd-qa-confirm.md) — Security QA confirm SD #31 (PASS 10/10 — SD Sec only)
## ops/

- [ORG-OPS.md](ops/ORG-OPS.md) — agent ops charter (stable; do not replace with dated notes)
- [2026-09-10__pm__ops__github-issue-tracking.md](ops/2026-09-10__pm__ops__github-issue-tracking.md) — GitHub issue-tracking ops (pre-PoC; mirror PR [#1](https://github.com/ioaikh/dealoware/pull/1) → `docs/ops/2026-09-10__pm__ops__github-issue-tracking.md`)
- [2026-09-10__docs__ops__index-delta-tooling-proposal.md](ops/2026-09-10__docs__ops__index-delta-tooling-proposal.md) — INDEX delta shell helper (CEO APPROVED 2026-09-10; paid plugins blocked; state file off GitHub mirror)
- [2026-09-10__devops__ops__index-delta-helper-brief.md](ops/2026-09-10__devops__ops__index-delta-helper-brief.md) — Chief DevOps implement brief
- [DRAFT-INDEX-DELTA-TOOLING-PROPOSAL.md](ops/DRAFT-INDEX-DELTA-TOOLING-PROPOSAL.md) — stub → dated tooling proposal
- [2026-09-10__devops__ops__index-delta-additive-patch-brief.md](ops/2026-09-10__devops__ops__index-delta-additive-patch-brief.md) — DevOps brief: additive-only delta patcher
- [2026-09-10__devops__ops__index-delta-sibling-wipe-incident.md](ops/2026-09-10__devops__ops__index-delta-sibling-wipe-incident.md) — sibling INDEX wipe incident (restored)
- [2026-09-10__devops__ops__ecs-express-host-shape-lock.md](ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md)
- [2026-09-10__docs__ops__poc-o10-doc-security-weave.md](ops/2026-09-10__docs__ops__poc-o10-doc-security-weave.md) — Doc Security weave pts 1–10 (**overall Doc step #3 PASS**; Security QA cleared)
- [2026-09-11__docs__ops__poc-artifact-doc-security-weave.md](ops/2026-09-11__docs__ops__poc-artifact-doc-security-weave.md) — Doc Security weave pts 1–10 for Artifact #4 (**overall Doc step #4 PASS**; Security QA cleared)
- [2026-09-11__docs__ops__poc-auth-doc-security-weave.md](ops/2026-09-11__docs__ops__poc-auth-doc-security-weave.md) — Doc Security weave pts 1–10 for Auth #5 (**overall Doc step #5 PASS**; Security QA cleared)
- [2026-09-20__docs__ops__poc-negotiation-doc-security-weave.md](ops/2026-09-20__docs__ops__poc-negotiation-doc-security-weave.md) — Doc Security weave pts 1–10 for Negotiation #6 (**overall Doc step #6 PASS**; Security QA cleared HOLD)
- [RECONSTRUCT-TEAM-FOR-NEW-PROJECT.md](ops/RECONSTRUCT-TEAM-FOR-NEW-PROJECT.md)
- [2026-09-19__ops__done-list__brief-1-baseline.md](ops/reports/2026-09-19__ops__done-list__brief-1-baseline.md)
- [2026-09-19__ops__ops-report__brief-1-baseline.md](ops/reports/2026-09-19__ops__ops-report__brief-1-baseline.md)
- [2026-09-19__ops__verification__brief-1-baseline.md](ops/reports/2026-09-19__ops__verification__brief-1-baseline.md)
- [2026-09-20__ops__done-list__brief-2-performance.md](ops/reports/2026-09-20__ops__done-list__brief-2-performance.md)
- [2026-09-20__ops__done-list__brief-3-6h-noon.md](ops/reports/2026-09-20__ops__done-list__brief-3-6h-noon.md)
- [2026-09-20__ops__ops-note__pm-pipeline-visibility-and-cpm-15m-watch.md](ops/reports/2026-09-20__ops__ops-note__pm-pipeline-visibility-and-cpm-15m-watch.md)
- [2026-09-20__ops__ops-note__team-performance-check-model.md](ops/reports/2026-09-20__ops__ops-note__team-performance-check-model.md)
- [2026-09-20__ops__ops-report__brief-2-performance.md](ops/reports/2026-09-20__ops__ops-report__brief-2-performance.md)
- [2026-09-20__ops__ops-report__brief-3-6h-noon.md](ops/reports/2026-09-20__ops__ops-report__brief-3-6h-noon.md)
- [2026-09-20__ops__verification__brief-2-performance.md](ops/reports/2026-09-20__ops__verification__brief-2-performance.md)
- [2026-09-20__docs__ops__poc-identity-seal-doc-security-weave.md](ops/2026-09-20__docs__ops__poc-identity-seal-doc-security-weave.md) — Doc Security weave pts 1–10 for Identity-seal #7 (**overall Doc step #7 PASS**; Security QA cleared HOLD)
- [2026-09-20__docs__ops__poc-l1-l3-posture-doc-security-weave.md](ops/2026-09-20__docs__ops__poc-l1-l3-posture-doc-security-weave.md) — Doc Security weave pts 1–10 for Standing L1–L3 #8 (**overall Doc step #8 PASS**; Security QA cleared HOLD)
- [2026-09-20__ops__ops-note__post-milestone-sa-review-gate.md](ops/reports/2026-09-20__ops__ops-note__post-milestone-sa-review-gate.md)
- [2026-09-20__ops__done-list__brief-4-6h-evening.md](ops/reports/2026-09-20__ops__done-list__brief-4-6h-evening.md)
- [2026-09-20__ops__ops-note__poc-nightly-autotest-chief-loop.md](ops/reports/2026-09-20__ops__ops-note__poc-nightly-autotest-chief-loop.md)
- [2026-09-20__ops__ops-note__sa-arch-review-process-gaps-lock.md](ops/reports/2026-09-20__ops__ops-note__sa-arch-review-process-gaps-lock.md)
- [2026-09-20__ops__ops-report__brief-4-6h-evening.md](ops/reports/2026-09-20__ops__ops-report__brief-4-6h-evening.md)
- [2026-09-20__ops__verification__brief-4-6h-evening.md](ops/reports/2026-09-20__ops__verification__brief-4-6h-evening.md)
- [2026-09-21__ops__done-list__brief-5-6h-overnight.md](ops/reports/2026-09-21__ops__done-list__brief-5-6h-overnight.md)
- [2026-09-21__ops__done-list__brief-6-6h-dawn.md](ops/reports/2026-09-21__ops__done-list__brief-6-6h-dawn.md)
- [2026-09-21__ops__ops-report__brief-5-6h-overnight.md](ops/reports/2026-09-21__ops__ops-report__brief-5-6h-overnight.md)
- [2026-09-21__ops__ops-report__brief-6-6h-dawn.md](ops/reports/2026-09-21__ops__ops-report__brief-6-6h-dawn.md)
- [2026-09-21__ops__verification__brief-5-6h-overnight.md](ops/reports/2026-09-21__ops__verification__brief-5-6h-overnight.md)
- [2026-09-21__ops__verification__brief-6-6h-dawn.md](ops/reports/2026-09-21__ops__verification__brief-6-6h-dawn.md)
- [2026-09-21__docs__ops__mvp-stage-a-list-failclosed-doc-security-weave.md](ops/2026-09-21__docs__ops__mvp-stage-a-list-failclosed-doc-security-weave.md) — Doc Security weave #32 — Overall Doc step #32 PASS (Security QA cleared HOLD)
- [2026-09-21__docs__ops__mvp-stage-a-field-acl-doc-security-weave.md](ops/2026-09-21__docs__ops__mvp-stage-a-field-acl-doc-security-weave.md) — Doc Security weave #31 Field ACL — **HOLD** overall Doc PASS until Security QA doc-qa-confirm (#32 OUT)
## meta/

- [DOC-FLOW.md](meta/DOC-FLOW.md)
- [INDEX-DELTA.md](meta/INDEX-DELTA.md)
- [rebuild-index-delta.sh](meta/rebuild-index-delta.sh)

## index/

(empty — awaiting PM assigns) — optional fragments; this root INDEX is primary.

---

## Cadence & mirror notes

- Doc Team: hourly weekday work-hours review; patch INDEX from deltas; update `.doc-index-state.json`.
- GitHub mirror (map only unless Product/PM already pushed): `github.com/ioaikh/dealoware` `docs/` ↔ KB areas (see DOC-FLOW). **Both** `docs/product/CEO-ORIGINAL-BRIEF.md` and `docs/product/PRODUCT-BRIEF.md` map; original already pushed — keep distinct. Later sync via **git range**, not full tree re-read.
- Cost/plugins: needs-only → Chief Docs + COO + DevOps → CEO. Do not install/buy without confirmation.
