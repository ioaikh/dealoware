# Joint tooling proposal: INDEX delta shell helper (CEO APPROVED)

**Status:** CEO APPROVED 2026-09-10 — implement shell helper; paid plugins still blocked; state file off GitHub  
**Authors:** Chief DevOps (tooling section) + Chief Docs (Doc needs) + COO (package → CEO)  
**Aligns to:** `meta/INDEX-DELTA.md`, `meta/DOC-FLOW.md`  
**Date:** 2026-09-10

---

## 1. Goal

Provide a **small shell-only helper** so Doc Team’s weekday-hourly INDEX rebuild uses **hash/mtime deltas** (no full content re-scan), matching the locked CEO constraint.

## 2. Non-goals (locked)

- No paid search/plugins/hosted index.
- No package install or spend until joint proposal → COO → CEO confirm.
- Do not push to GitHub from this helper; do not publish `.doc-index-state.json` to `docs/` mirror (local/state-only preferred).
- Doc Team still owns INDEX content/structure; DevOps owns helper design + maintenance brief.

## 3. DevOps tooling section (proposed)

### 3.1 Deliverable

One POSIX-friendly shell script, e.g.:

`/workspace/dealoware-kb/meta/rebuild-index-delta.sh`

(or `ops/` if Doc prefers meta process-only docs — Doc chooses final path).

### 3.2 Behavior (must)

1. **KB root:** default `/workspace/dealoware-kb` (override via env `DEALOWARE_KB_ROOT` if needed).
2. **Walk stable paths only** (INDEX-DELTA §3 / DOC-FLOW):  
   `product/`, `architecture/`, `specs/`, `plans/`, `qa/`, `verification/`, `ops/`, `meta/`, `index/`, plus root `INDEX.md`, `README.md`, approved stubs.  
   Skip `.doc-index-state.json` from the `files` map (or ignore when scanning).
3. **Load state:** `.doc-index-state.json` (schema version 1 per INDEX-DELTA).
4. **Delta classify:**
   - **Added:** on disk, not in state → hash + mtime; queue INDEX patch for that area.
   - **Modified:** in state; mtime differs → recompute sha256; if hash differs, patch INDEX entry; if hash same, optionally refresh mtime only (INDEX unchanged).
   - **Deleted:** in state, missing on disk → drop from state + remove INDEX link.
   - **Unchanged:** mtime + sha256 match → **do not open/re-read body**.
5. **Patch `INDEX.md` only for delta paths** (area sections / seed links / awaiting lines). Never regenerate whole INDEX from a full content scan.
6. **Write state** with updated `updated_at` (ISO-8601 UTC) and `files` map.
7. **Emit done-list counts** to stdout (and exit 0 on success):  
   `added=N modified=N deleted=N unchanged=N` (plus optional list of delta paths).
8. **Idempotent:** second run with no FS changes → all unchanged, INDEX/state content stable (mtime/`updated_at` may bump only if we choose “touch state”; prefer **no rewrite** when delta empty).
9. **Fail loud on corrupt state:**
   - Missing/unreadable state on first run → initialize empty `files` **or** exit non-zero with clear message (Doc chooses; DevOps recommends: **init empty v1 with warning** on first run only).
   - Invalid JSON / missing `version` / wrong schema → **exit non-zero**, print path + parse error, **do not** overwrite INDEX or state.
   - Unwritable INDEX or state → exit non-zero, leave prior files intact where possible.

### 3.3 Implementation notes (shell)

- Prefer `sha256sum` / `stat` / `find` / `jq` **if already present** on the shared agent computer; if `jq` absent, use Python stdlib JSON **already on box** — still no new installs without CEO OK.
- Fast path: compare mtime first; hash only when mtime differs or path is new.
- Relative path keys, POSIX-style, no leading `./` (match current KB convention).
- Optional: `--dry-run` prints deltas without writing.

### 3.4 Cadence / ops

- Intended for Doc Team **weekday hourly** runs (routine or manual).
- DevOps: assist script on Doc ask; Senior DevOps implements from Chief brief → DevOps QA verifies vs brief → confirm to Chief.
- Verification artifact (when executed):  
  `verification/YYYY-MM-DD__devops__verification__index-delta-helper.md`

### 3.5 Cost / plugins

| Item | Now | Later |
|------|-----|-------|
| Shell helper | Default | — |
| Paid index / search plugin | **Out** | Needs-only list → Chief Docs + COO + DevOps → CEO |
| GitHub `docs/` mirror sync | Map only (DOC-FLOW) | Separate task; prefer git-range delta; no push from indexer |

### 3.6 Success criteria (for CEO confirm + later QA)

- [ ] Delta-only: unchanged files never content-read.
- [ ] Corrupt state fails closed (non-zero, no silent INDEX clobber).
- [ ] Empty-delta run is no-op (or state-only timestamp — Doc preference documented).
- [ ] Done-list counts match actual FS deltas on a fixture test.
- [ ] State file stays local (not required in GitHub `docs/`).

## 4. Ask of CEO (via COO)

1. **Approve** shell-only INDEX delta helper as described (no spend).
2. **Confirm** paid plugins remain blocked until a later needs-only proposal.
3. **Confirm** `.doc-index-state.json` stays off public `docs/` mirror.

## 5. Open points — resolved by Chief Docs (see §6)

1. First-run missing state → init empty v1 + warning; corrupt → fail closed.
2. Empty-delta → no rewrite (no `updated_at` bump).
3. Script path → `meta/rebuild-index-delta.sh`.
4. Fixtures → `meta/fixtures/index-delta/`; verification report → `verification/` per DOC-FLOW.

## 6. Chief Docs — Doc needs (locked 2026-09-10)

1. **First-run / missing state:** If `.doc-index-state.json` is **absent**, initialize empty v1 (`files: {}`) with a clear **warning** on stderr, then proceed (first-run only). If state is **present but corrupt/invalid** (bad JSON, missing `version`, wrong schema) → **fail closed** (non-zero); do not overwrite INDEX or state.
2. **Empty-delta rewrite:** Prefer **no rewrite** of INDEX or state when counts are all unchanged (no `updated_at` bump). Keeps hourly quiet and idempotent.
3. **Script path:** `/workspace/dealoware-kb/meta/rebuild-index-delta.sh` (process tooling beside `INDEX-DELTA.md`).
4. **Fixtures:** Tiny fixture tree under `meta/fixtures/index-delta/`. When the helper is implemented/verified, land the verification report at `verification/YYYY-MM-DD__devops__verification__index-delta-helper.md` per DOC-FLOW.
5. **INDEX listings:** Track `.gitkeep` in state optional; **do not** list `.gitkeep` in INDEX.md area sections.
6. **Ownership:** Doc owns INDEX content/structure and hourly cadence; DevOps owns helper design/maintenance; Docs QA verifies INDEX/DOC-FLOW outcomes; DevOps QA verifies helper vs brief.
7. **Needs-only (later, not this proposal):** CI lint/link check; optional search index — still proposal-only, no spend now.

Doc Team **supports** asking CEO to approve this shell-only helper (no spend), keep paid plugins blocked, and keep `.doc-index-state.json` off the public `docs/` mirror.


---

**Next:** COO packages joint proposal → CEO confirm → only then implement (DevOps triad).
