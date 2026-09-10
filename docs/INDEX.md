# Dealoware Documentation Knowledge Base — INDEX

**Purpose:** Living, discoverable index of Dealoware agent-ops documentation. Canonical root: `/workspace/dealoware-kb/` on the shared agent computer.

**Product alignment rule:** Product goals/facts (`product/PRODUCT-BRIEF.md`) are source of truth. Conflicts → escalate **PM → Product → CEO**. Do not invent Stories or requirements.

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

- [product/PRODUCT-BRIEF.md](product/PRODUCT-BRIEF.md) — goals, domain model, claims lock, early platform additions, licensing (Apache 2.0 / public GitHub)
- [ops/ORG-OPS.md](ops/ORG-OPS.md) — agent ops charter, pipeline, triads, doc ownership
- [meta/DOC-FLOW.md](meta/DOC-FLOW.md) — all-team publish flow (naming, handoff, mirror map)
- [meta/INDEX-DELTA.md](meta/INDEX-DELTA.md) — hourly delta index procedure (no full re-scan)

Root stubs (redirect only): [PRODUCT-BRIEF.md](PRODUCT-BRIEF.md) → product/; [ORG-OPS.md](ORG-OPS.md) → ops/

---

## product/

- [PRODUCT-BRIEF.md](product/PRODUCT-BRIEF.md)

---

## architecture/

(empty — awaiting PM assigns)

---

## specs/

(empty — awaiting PM assigns)

---

## plans/

(empty — awaiting PM assigns)

---

## qa/

(empty — awaiting PM assigns)

---

## verification/

(empty — awaiting PM assigns)

---

## ops/

- [ORG-OPS.md](ops/ORG-OPS.md)

---

## meta/

- [DOC-FLOW.md](meta/DOC-FLOW.md)
- [INDEX-DELTA.md](meta/INDEX-DELTA.md)

---

## index/

(empty — awaiting PM assigns) — optional fragments; this root INDEX is primary.

---

## Cadence & mirror notes

- Doc Team: hourly weekday work-hours review; patch INDEX from deltas; update `.doc-index-state.json`.
- GitHub mirror (map only, no push in structure tasks): `github.com/ioaikh/dealoware` `docs/` ↔ KB areas (see DOC-FLOW). Later sync via **git range**, not full tree re-read.
- Cost/plugins: needs-only → Chief Docs + COO + DevOps → CEO. Do not install/buy without confirmation.
