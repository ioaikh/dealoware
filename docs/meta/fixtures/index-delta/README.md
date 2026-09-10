# INDEX delta helper fixtures

Self-contained mini KB for QA of `meta/rebuild-index-delta.sh`.

## Quick run

```bash
SCRIPT=/workspace/dealoware-kb/meta/rebuild-index-delta.sh
FIX=/workspace/dealoware-kb/meta/fixtures/index-delta

# Full scenario suite (writes under scenarios/work/ only)
bash "$FIX/scenarios/run-all.sh"
```

Or individually:

```bash
bash "$FIX/scenarios/01-missing-state.sh"
bash "$FIX/scenarios/02-corrupt-state.sh"
bash "$FIX/scenarios/03-add-modify-delete-unchanged.sh"
bash "$FIX/scenarios/04-empty-delta-no-rewrite.sh"
bash "$FIX/scenarios/05-gitkeep-not-in-index.sh"
bash "$FIX/scenarios/06-add-preserves-annotated-siblings.sh"
bash "$FIX/scenarios/07-delete-annotated-preserves-siblings.sh"
```

Evidence lands in `evidence/`. Fixture runs set `DEALOWARE_KB_ROOT` to copies under `scenarios/work/` — they do **not** mutate the live KB.

Smoke (optional, dry-run only against live KB):

```bash
DEALOWARE_KB_ROOT=/workspace/dealoware-kb \
  /workspace/dealoware-kb/meta/rebuild-index-delta.sh --dry-run
```

## Notes

- Helper **skips** `.gitkeep` (not in INDEX, not in state).
- Helper **skips** walk of `meta/fixtures/**` so QA trees do not pollute live INDEX.
- Live KB: first non-dry-run may `deleted=` prior `.gitkeep` entries from an older state file (INDEX already omitted them).
- Evidence from last suite: `evidence/SUMMARY.md`
- Proposal cite: `ops/2026-09-10__docs__ops__index-delta-tooling-proposal.md` (DRAFT path is a stub).
