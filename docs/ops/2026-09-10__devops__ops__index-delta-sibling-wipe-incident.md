# Ops incident — INDEX delta helper sibling wipe (efficiency/risk, no spend)

**Opened via:** COO → CPM ask to Chief DevOps  
**Date:** 2026-09-10  
**Status:** Triaged; Senior DevOps patching; DevOps QA standing by  
**Do not reopen:** GitHub issue-tracking Doc close (separate)

---

## 1. Evidence paths (`dealoware-kb`)

| Path | Role |
|------|------|
| `meta/rebuild-index-delta.sh` | Defective helper (`patch_index` / `section_link_paths`) |
| `ops/2026-09-10__devops__ops__index-delta-additive-patch-brief.md` | Chief brief for additive-only fix |
| `ops/2026-09-10__devops__ops__index-delta-helper-brief.md` | Original implement brief |
| `verification/2026-09-10__devops__verification__index-delta-helper.md` | Prior QA PASS (pre-defect report) |
| `verification/2026-09-10__devops__verification__index-delta-additive-patch.md` | Target verification after fix (pending) |
| `INDEX.md` § `## ops/` | Live annotated sibling links (Doc restored manually after wipe) |
| `meta/INDEX-DELTA.md` | Locked delta procedure |
| `meta/fixtures/index-delta/` | Fixture suite (to be extended for annotated siblings) |

Doc report: Chief Docs / Senior Docs — brief wipe of sibling `ops/` INDEX lines on add; manual restore.

---

## 2. Root cause

`section_link_paths()` matched only lines that are **exactly** `- [text](path)` end-of-line.

Live INDEX entries often include trailing annotations, e.g.:

`- [ORG-OPS.md](ops/ORG-OPS.md) — agent ops charter...`

Those lines failed the regex → were omitted from `keep` → `patch_index()` rewrote the area section from the incomplete path set → **sibling annotated links wiped**; only bare new `link_line()` entries remained.

Efficiency/risk: Doc hourly index can silently lose discoverability links; no spend involved.

---

## 3. Additive-only patch plan + non-regression

### Plan
1. Parse links allowing trailing description after `)`.
2. **Preserve original full line** for kept paths (annotations intact).
3. **Adds:** append new link only if path absent (bare link OK for new).
4. **Deletes:** remove only line(s) for that path; leave all other lines intact.
5. Never rebuild an area section from a path-only set that drops annotations.
6. Extend fixtures: annotated siblings survive add; delete removes only target.
7. Senior → DevOps QA → confirm to Chief → notify Doc/CPM/COO/Bot Manager.

### Non-regression argument
- Prior locked behaviors retained: missing-state init+warning; corrupt fail-closed; empty-delta no rewrite; no `.gitkeep` in INDEX; delta-only (no full content re-scan); no paid plugins.
- New fixtures encode the annotated-sibling invariant so future INDEX notes cannot regress the wipe.
- Doc continues verifying full `ops/` section until additive QA PASS.

**No spend. No GitHub issue-tracking Doc reopen.**


---

## Disposition (2026-09-10)

- **DevOps QA:** PASS — `verification/2026-09-10__devops__verification__index-delta-additive-patch.md`
- **Hourly INDEX:** trust restored from DevOps side (Doc sibling re-check pending)
- **No spend**
