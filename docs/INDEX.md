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

---

## architecture/

- [2026-09-10__sa__architecture__poc-feasibility-roadmap.md](architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md) — PoC feasibility roadmap (SA)
- [2026-09-10__sa__architecture__poc-o10-scaffold.md](architecture/2026-09-10__sa__architecture__poc-o10-scaffold.md) — PoC O10 scaffold (SA)

## specs/

(empty — awaiting PM assigns)

---

## plans/

- [2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md](plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md) — general release roadmap PoC→MVP→V1–V5 (accepted; indexed 2026-09-10)
- [2026-09-10__ba__note__story-4-artifact-api-depth.md](plans/2026-09-10__ba__note__story-4-artifact-api-depth.md)
- [2026-09-10__ba__note__story-5-poc-auth.md](plans/2026-09-10__ba__note__story-5-poc-auth.md)
- [2026-09-10__pm__plan__poc-github-backlog.md](plans/2026-09-10__pm__plan__poc-github-backlog.md)

## qa/

(empty — awaiting PM assigns)

---

## verification/

- [2026-09-10__devops__verification__index-delta-helper.md](verification/2026-09-10__devops__verification__index-delta-helper.md)
- [2026-09-10__product__verification__product-brief-refresh.md](verification/2026-09-10__product__verification__product-brief-refresh.md)
- [2026-09-10__sa__verification__poc-feasibility-roadmap.md](verification/2026-09-10__sa__verification__poc-feasibility-roadmap.md) — SA verification of PoC feasibility roadmap
- [2026-09-10__devops__verification__index-delta-additive-patch.md](verification/2026-09-10__devops__verification__index-delta-additive-patch.md) — DevOps QA evidence: additive-only INDEX delta patch
- [2026-09-10__sa__verification__poc-o10-scaffold.md](verification/2026-09-10__sa__verification__poc-o10-scaffold.md) — SA verification of PoC O10 scaffold
- [2026-09-10__sa__verification__host-shape-ecs-express-patch.md](verification/2026-09-10__sa__verification__host-shape-ecs-express-patch.md) — SA verification: host-shape ECS Express patch

## ops/

- [ORG-OPS.md](ops/ORG-OPS.md) — agent ops charter (stable; do not replace with dated notes)
- [2026-09-10__pm__ops__github-issue-tracking.md](ops/2026-09-10__pm__ops__github-issue-tracking.md) — GitHub issue-tracking ops (pre-PoC; mirror PR [#1](https://github.com/ioaikh/dealoware/pull/1) → `docs/ops/2026-09-10__pm__ops__github-issue-tracking.md`)
- [2026-09-10__docs__ops__index-delta-tooling-proposal.md](ops/2026-09-10__docs__ops__index-delta-tooling-proposal.md) — INDEX delta shell helper (CEO APPROVED 2026-09-10; paid plugins blocked; state file off GitHub mirror)
- [2026-09-10__devops__ops__index-delta-helper-brief.md](ops/2026-09-10__devops__ops__index-delta-helper-brief.md) — Chief DevOps implement brief
- [DRAFT-INDEX-DELTA-TOOLING-PROPOSAL.md](ops/DRAFT-INDEX-DELTA-TOOLING-PROPOSAL.md) — stub → dated tooling proposal
- [2026-09-10__devops__ops__index-delta-additive-patch-brief.md](ops/2026-09-10__devops__ops__index-delta-additive-patch-brief.md) — DevOps brief: additive-only delta patcher
- [2026-09-10__devops__ops__index-delta-sibling-wipe-incident.md](ops/2026-09-10__devops__ops__index-delta-sibling-wipe-incident.md) — sibling INDEX wipe incident (restored)
- [2026-09-10__devops__ops__ecs-express-host-shape-lock.md](ops/2026-09-10__devops__ops__ecs-express-host-shape-lock.md)


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
