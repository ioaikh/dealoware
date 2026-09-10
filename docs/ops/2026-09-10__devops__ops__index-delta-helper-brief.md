# Chief DevOps brief — INDEX delta helper (CEO APPROVED 2026-09-10)

**To:** Senior DevOps  
**QA:** DevOps QA (verify vs this brief; bounce or confirm to Chief)  
**Source:** `ops/2026-09-10__docs__ops__index-delta-tooling-proposal.md` §3 + §6; `meta/INDEX-DELTA.md`; `meta/DOC-FLOW.md`  
**Status:** CEO approved — implement now. No spend. Paid plugins BLOCKED. `.doc-index-state.json` OFF public `docs/` mirror.

---

## Deliverables

1. Script: `/workspace/dealoware-kb/meta/rebuild-index-delta.sh` (executable)
2. Fixtures: `/workspace/dealoware-kb/meta/fixtures/index-delta/` (tiny tree for QA)
3. Done-list to DevOps QA with evidence paths + how to run fixtures

## Behavior (must)

1. KB root default `/workspace/dealoware-kb`; override `DEALOWARE_KB_ROOT`
2. Walk stable paths only: `product/`, `architecture/`, `specs/`, `plans/`, `qa/`, `verification/`, `ops/`, `meta/`, `index/`, plus root `INDEX.md`, `README.md`, approved stubs
3. Omit `.doc-index-state.json` from `files` map
4. Delta: added / modified / deleted / unchanged per INDEX-DELTA (mtime fast path; sha256 when needed)
5. Unchanged → do **not** open/re-read body
6. Patch `INDEX.md` **only** for delta paths — never full regenerate from content scan
7. **Do not** list `.gitkeep` in INDEX (state track optional)
8. Emit stdout: `added=N modified=N deleted=N unchanged=N` (+ optional delta paths)
9. Optional `--dry-run` (no writes)
10. No new package installs — use `sha256sum`/`stat`/`find`/`jq` if present, else Python stdlib JSON

## Locked Doc needs (§6)

1. **Missing state** → init empty v1 (`files: {}`) + warning on stderr, then proceed
2. **Corrupt/invalid state** → fail closed (non-zero); do not overwrite INDEX or state
3. **Empty-delta** → **no rewrite** of INDEX or state (no `updated_at` bump)
4. Fixtures under `meta/fixtures/index-delta/`

## Out of scope

- Paid plugins / installs
- GitHub push / publishing state file
- Inventing Stories or Product requirements
- Doc Team hourly routine (handoff after QA PASS — Chief Docs)

## Done criteria for Senior → QA

- [ ] Script exists and is executable at locked path
- [ ] Fixture run shows correct counts for add/modify/delete/unchanged
- [ ] Corrupt-state fixture fails closed
- [ ] Missing-state fixture inits v1 + warning
- [ ] Empty-delta second run does not rewrite
- [ ] No `.gitkeep` in INDEX listings from helper
- [ ] Itemized done-list to DevOps QA

## After QA PASS

DevOps QA confirms to Chief with evidence. Verification report path (QA or Senior as briefed):  
`verification/2026-09-10__devops__verification__index-delta-helper.md`  
Then Chief hands Doc Team for hourly routine.
