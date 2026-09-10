# Dealoware Doc Flow — All-Team Publish Guide

**Canonical KB root:** `/workspace/dealoware-kb/` (shared agent computer)  
**Owners:** Doc Team triad — Chief Docs → Senior Docs → Docs QA (QA confirms to Chief only)  
**Product alignment:** Keep **two distinct** product docs — original `product/CEO-ORIGINAL-BRIEF.md` (verbatim CEO) and summary `product/PRODUCT-BRIEF.md` (Product-maintained). Do not invent Stories or requirements. Do **not** merge/overwrite original with summary.

Related: [INDEX-DELTA.md](INDEX-DELTA.md) · [../INDEX.md](../INDEX.md) · [../ops/ORG-OPS.md](../ops/ORG-OPS.md)

---

## A. Purpose & ownership

1. **Doc Team triad**
   - **Chief Docs** — intake from PM; restate understanding; brief Senior Docs or Docs QA; receive QA confirm (never skip Chief to PM).
   - **Senior Docs** — execute structure, index rebuilds, flow updates; done-list to Docs QA.
   - **Docs QA** — verify with evidence; bounce Senior or confirm to Chief only.
2. **Canonical working copy** for agent ops: box path `/workspace/dealoware-kb/`.
3. **Product goals/facts** constrain all artifacts: **original** = `product/CEO-ORIGINAL-BRIEF.md`; **summary** = `product/PRODUCT-BRIEF.md` (Product may refresh summary; never collapse into/overwrite original). Conflicts → note in doc + escalate **PM → Product → CEO**.
4. Doc Team owns KB structure, living `INDEX.md`, and `.doc-index-state.json` delta index (see INDEX-DELTA).

---

## B. Artifact types

| Artifact area | Who writes | Lands under | Naming pattern | When Doc Team indexes | Handoff |
|---------------|------------|-------------|----------------|----------------------|---------|
| Product original (CEO verbatim) | CEO (intent); Doc indexes | `product/CEO-ORIGINAL-BRIEF.md` | Stable name `CEO-ORIGINAL-BRIEF.md` | On create/change | Doc triad; **never** overwrite with summary |
| Product summary / domain / claims | Product Team | `product/PRODUCT-BRIEF.md` | Stable name `PRODUCT-BRIEF.md`; dated updates use pattern below | On create/change (hourly delta) | PM assign → Chief Docs brief → Senior Docs → Docs QA |
| Org / ops charters | Business / COO (+ CEO for critical) | `ops/` | Stable names for charters (`ORG-OPS.md`); dated updates use pattern | On create/change | Same handoff |
| Architecture | SA Team | `architecture/` | `YYYY-MM-DD__sa__architecture__{slug}.md` | Hourly weekday delta rebuild | Same handoff |
| Specs | Spec Team | `specs/` | `YYYY-MM-DD__spec__spec__{slug}.md` | Hourly weekday delta rebuild | Same handoff |
| Refactor specs | CQ Team | `specs/` | `YYYY-MM-DD__cq__spec__refactor-{slug}.md` | Hourly weekday delta rebuild | PM → Chief Docs after CQ QA |
| Dev plans | Dev Plan Team | `plans/` | `YYYY-MM-DD__devplan__plan__{slug}.md` | Hourly weekday delta rebuild | Same handoff |
| QA reports | QA Team | `qa/` | `YYYY-MM-DD__qa__qa-report__{slug}.md` | Hourly weekday delta rebuild | Same handoff |
| Verification reports | Security / DevOps / etc. | `verification/` | `YYYY-MM-DD__{team}__verification__{slug}.md` | Hourly weekday delta rebuild | Same handoff |
| Doc process | Doc Team | `meta/` | Stable process names (`DOC-FLOW.md`, `INDEX-DELTA.md`) | On change | Internal Doc triad |

**Handoff (all teams):** PM assign → Chief Docs brief → Senior Docs places/indexes → Docs QA confirms to Chief.

**Stable paths rule:** Put architecture / specs / plans / QA / verification **only** under the folders above with the naming pattern. Do not scatter dated artifacts at KB root. Stable paths keep hash/mtime deltas cheap (INDEX-DELTA).

---

## C. Naming convention

**Pattern:** `YYYY-MM-DD__{team}__{artifact-type}__{slug}.md`

**Examples:**
- `2026-09-10__sa__architecture__artifact-negotiation-v1.md`
- `2026-09-10__spec__spec__offer-state-machine.md`
- `2026-09-10__devplan__plan__mvp-core-api.md`
- `2026-09-10__qa__qa-report__mvp-smoke.md`
- `2026-09-10__security__verification__pii-vault-review.md`

### Allowed `{team}` enum
`sa` | `spec` | `cq` | `devplan` | `sd` | `qa` | `security` | `devops` | `ba` | `pm` | `product` | `business` | `docs` | `core` | `agents`

### Allowed `{artifact-type}` enum
`architecture` | `spec` | `plan` | `qa-report` | `verification` | `ops` | `product` | `meta` | `note`

**Slug:** lowercase kebab-case, no spaces (e.g. `offer-state-machine`).

Seed/charter files may keep stable undated names (`CEO-ORIGINAL-BRIEF.md`, `PRODUCT-BRIEF.md`, `ORG-OPS.md`, `DOC-FLOW.md`).

---

## D. Mirror guidance for GitHub

| Agent ops (canonical) | Public/repo mirror |
|-----------------------|--------------------|
| `/workspace/dealoware-kb/` | `github.com/ioaikh/dealoware` → path `docs/` |

### Path map
| KB path | Repo path |
|---------|-----------|
| `dealoware-kb/product/` | `docs/product/` |
| `dealoware-kb/architecture/` | `docs/architecture/` |
| `dealoware-kb/specs/` | `docs/specs/` |
| `dealoware-kb/plans/` | `docs/plans/` |
| `dealoware-kb/qa/` | `docs/qa/` |
| `dealoware-kb/verification/` | `docs/verification/` |
| `dealoware-kb/ops/` | `docs/ops/` |
| `dealoware-kb/meta/` | `docs/meta/` |
| `dealoware-kb/index/` | `docs/index/` |
| `dealoware-kb/INDEX.md` | `docs/INDEX.md` |
| `dealoware-kb/README.md` | `docs/README.md` |
| `dealoware-kb/.doc-index-state.json` | optional / local-only (prefer not publish state file) |


### Product docs on GitHub `docs/product/`
| KB | Repo mirror |
|----|-------------|
| `dealoware-kb/product/CEO-ORIGINAL-BRIEF.md` | `docs/product/CEO-ORIGINAL-BRIEF.md` |
| `dealoware-kb/product/PRODUCT-BRIEF.md` | `docs/product/PRODUCT-BRIEF.md` |

Both files map. **Original already pushed** to GitHub `docs/product/` — keep it distinct from the summary on mirror too (do not replace original with summary in repo).

**Do not push or install in this task** — map only.

**Later mirror efficiency:** sync via **git range** (changed paths since last sync), not full tree re-read — same delta spirit as INDEX-DELTA.

### Cost / plugins (needs only — do not install/buy)
- Possible future needs: search indexing plugin, link checker, markdown lint in CI.
- **Chief Docs + COO + DevOps propose → CEO confirms** before any cost action. No paid plugin until then.

---

## E. Conflict / escalation

1. Docs **must not invent** Product Stories or requirements beyond the Product briefs (`CEO-ORIGINAL-BRIEF.md` + `PRODUCT-BRIEF.md`). Never merge/overwrite original with summary.
2. Product conflicts: note in the artifact + escalate **PM → Product → CEO**.
3. Cost or critical changes: via **COO → CEO**.
4. No MotorMarket live inventory, logins, SFTP, or vehicle-only framing as Dealoware scope.

---

## F. Indexing cadence

1. Doc Team reviews changes **hourly during weekday work hours** and rebuilds `INDEX.md` from **deltas only**.
2. Procedure: [INDEX-DELTA.md](INDEX-DELTA.md) — compare `.doc-index-state.json` hash/mtime; update/add/remove those entries only; **do not re-read unchanged docs**.
3. Support files: root `INDEX.md` + `.doc-index-state.json` (path → `{sha256, mtime}`).
4. Teams write under DOC-FLOW stable paths so deltas stay small.

---

## Quick checklist for authors

- [ ] Artifact under correct folder (`architecture/`, `specs/`, …)
- [ ] Filename matches naming pattern (or approved stable seed name)
- [ ] Aligns to Product briefs (original + summary kept distinct); no invented Stories
- [ ] Conflict noted + escalated if any
- [ ] PM → Doc triad handoff for indexing
