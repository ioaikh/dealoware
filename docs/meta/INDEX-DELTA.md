# Index without full re-scan (INDEX-DELTA)

**Locked procedure** for Doc Team hourly weekday rebuilds.  
**State file:** `/workspace/dealoware-kb/.doc-index-state.json`  
**Living index:** `/workspace/dealoware-kb/INDEX.md`  
**Flow:** [DOC-FLOW.md](DOC-FLOW.md)

No paid search/plugins unless Chief Docs + COO + DevOps propose → CEO confirms.

---

## 1. State-file schema

```json
{
  "version": 1,
  "updated_at": "ISO-8601 UTC timestamp",
  "kb_root": "/workspace/dealoware-kb",
  "files": {
    "<relative/path/from/kb_root>": {
      "sha256": "<hex>",
      "mtime": <unix_epoch_seconds>
    }
  }
}
```

- **Keys:** paths relative to KB root, POSIX-style, no leading `./` preferred (either consistent form is fine; stick to one — this KB uses no leading `./`).
- **sha256:** content hash of file bytes.
- **mtime:** filesystem modification time (seconds).
- **Omit** `.doc-index-state.json` itself from `files` (or ignore it when scanning).

---

## 2. What “unchanged” means

A path is **unchanged** when it exists in state **and** both current `mtime` **and** `sha256` match the stored values.

- mtime-only match is a fast filter; if mtime differs, recompute sha256.
- If sha256 matches after mtime change (touch without content change), treat as unchanged for INDEX content; optionally refresh mtime in state.
- **Do not open/re-read** body text of unchanged files when patching INDEX.

---

## 3. Hourly rebuild-from-delta steps

1. Walk only DOC-FLOW **stable paths** (area folders + root index/readme/stubs):  
   `product/`, `architecture/`, `specs/`, `plans/`, `qa/`, `verification/`, `ops/`, `meta/`, `index/`, plus root `INDEX.md`, `README.md`, and approved stubs.
2. Build a candidate set of paths (filenames; skip `.gitkeep` from INDEX listings if desired; still track in state optional).
3. Load `.doc-index-state.json`.
4. Compute deltas:
   - **Added:** path on disk, not in state → hash + mtime; queue INDEX section for that area.
   - **Modified:** path in state, mtime or sha256 differs → re-hash if needed; patch that INDEX entry only.
   - **Deleted:** path in state, missing on disk → remove from state and from INDEX.
   - **Unchanged:** skip content read.
5. Patch `INDEX.md` **only for delta paths** (area stub sections, seed links, “awaiting” lines). Do not regenerate the whole INDEX from a full content scan.
6. Write updated state file (`updated_at`, updated `files` map).
7. Done-list to Docs QA with counts: added / modified / deleted / skipped-unchanged.

---

## 4. How INDEX.md is patched from deltas only

| Delta | INDEX action |
|-------|----------------|
| Added under `architecture/` etc. | Append/link under that area’s section |
| Modified seed (e.g. PRODUCT-BRIEF) | Keep link; refresh one-line note only if title/summary line is maintained in INDEX (optional); never re-ingest full brief |
| Deleted | Remove link from that section |
| Unchanged | Leave INDEX lines as-is |

Area sections that still have no artifacts stay: `(empty — awaiting PM assigns)`.

---

## 5. Stable paths (cheap deltas)

Authors **must** land artifacts under DOC-FLOW folders with the naming pattern. Scattering files at KB root or renaming paths breaks delta locality and forces larger INDEX edits.

---

## 6. GitHub `docs/` mirror (later)

When mirroring to `github.com/ioaikh/dealoware` `docs/`:

- Use the **path map** in DOC-FLOW.
- Prefer **git range** (commits/paths since last sync) — not a full tree re-read — analogous to this delta procedure.
- Do not push from this indexing task unless CEO/COO explicitly orders a separate push task.

---

## 7. Cost note

Paid index plugins / hosted search: **needs-only list** to Chief+COO+DevOps → CEO. Default remains hash/mtime state file on the shared agent computer.
