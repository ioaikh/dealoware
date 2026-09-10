# Chief DevOps brief — INDEX patcher additive fix (defect)

**To:** Senior DevOps  
**QA:** DevOps QA  
**Intake:** Chief Docs (Senior Docs restored sibling `ops/` INDEX lines manually)  
**Date:** 2026-09-10  
**No spend.**

---

## Defect

`meta/rebuild-index-delta.sh` → `patch_index()` can **drop sibling links** in an area section when adding a file.

**Root cause (triage):** `section_link_paths()` only matches lines that are **exactly** `- [text](path)` end-of-line. Live INDEX lines often have trailing notes, e.g.:

`- [ORG-OPS.md](ops/ORG-OPS.md) — agent ops charter...`

Those lines are **not** extracted → `keep` misses siblings → section rewrite wipes them → only new bare `link_line()` entries remain.

## Required fix (locked)

1. **Additive area-section updates only** — never wipe sibling links.
2. When keeping an existing path: **preserve the original full line** (including trailing `— notes`).
3. Parse markdown links allowing trailing description after `)`.
4. Adds: append new link line only if path not already present (bare `link_line` OK for new).
5. Deletes: remove only the line(s) for that path; leave all other lines intact.
6. Do **not** rebuild the section from a path-only set if that drops annotations.
7. Empty placeholder handling stays; still never list `.gitkeep`.

## Fixtures

Extend `meta/fixtures/index-delta/`:
- Scenario: area section with annotated sibling links; add one file → siblings + annotations remain; new link present.
- Scenario: delete one annotated link → only that path gone; others unchanged including notes.

## Done-list → DevOps QA

- [ ] Patch in `meta/rebuild-index-delta.sh`
- [ ] New/updated fixtures green via `scenarios/run-all.sh`
- [ ] Evidence paths
- Verification report: `verification/2026-09-10__devops__verification__index-delta-additive-patch.md`

## Out of scope

Paid plugins, GitHub push, inventing Stories.
