# Fixture evidence summary — 2026-09-10T17:05:04Z

## 01-missing-state
exit: exit_code=0
stdout:
added=16 modified=0 deleted=0 unchanged=0
  added: INDEX.md
  added: ORG-OPS.md
  added: PRODUCT-BRIEF.md
  added: README.md
  added: architecture/README.md
  added: index/README.md
  added: meta/DOC-FLOW.md
  added: meta/README.md
  added: ops/ORG-OPS.md
  added: ops/README.md
  added: plans/README.md
  added: product/PRODUCT-BRIEF.md
  added: product/README.md
  added: qa/README.md
  added: specs/README.md
  added: verification/README.md
stderr:
WARNING: missing state file /workspace/dealoware-kb/meta/fixtures/index-delta/scenarios/work/01-missing-state/.doc-index-state.json; initializing empty v1

## 02-corrupt-state
exit: exit_code=1
stdout:
stderr:
ERROR: corrupt state JSON at /workspace/dealoware-kb/meta/fixtures/index-delta/scenarios/work/02-corrupt-state/.doc-index-state.json: Expecting property name enclosed in double quotes: line 1 column 2 (char 1)

## 02b-invalid-schema
exit: exit_code=1
stdout:
stderr:
ERROR: invalid state schema at /workspace/dealoware-kb/meta/fixtures/index-delta/scenarios/work/02b-invalid-schema/.doc-index-state.json: missing version

## 03-add-modify-delete-unchanged
exit: exit_code=0
stdout:
added=1 modified=1 deleted=1 unchanged=14
  added: architecture/2026-09-10__sa__architecture__fixture-delta.md
  modified: product/PRODUCT-BRIEF.md
  deleted: meta/README.md
stderr:
counts:
counts_line=added=1 modified=1 deleted=1 unchanged=14

## 04-empty-delta-no-rewrite
exit: exit_code=0
stdout:
added=0 modified=0 deleted=0 unchanged=16
stderr:
compare:
index_sha_before=f0d32416d337510018b08d856cff520fc5f77236786abe4e27aad7699f0ef41a
index_sha_after=f0d32416d337510018b08d856cff520fc5f77236786abe4e27aad7699f0ef41a
state_sha_before=fffdbc595309702b75892d113243b18301c3cd2e990e96a42f94beb0a5a4e95f
state_sha_after=fffdbc595309702b75892d113243b18301c3cd2e990e96a42f94beb0a5a4e95f
index_mtime_before=1789059896
index_mtime_after=1789059896
state_mtime_before=1789059896
state_mtime_after=1789059896
updated_at_before=2026-09-10T17:04:56Z
updated_at_after=2026-09-10T17:04:56Z

## 05-gitkeep-not-in-index
exit: exit_code=0
stdout:
added=16 modified=0 deleted=0 unchanged=0
  added: INDEX.md
  added: ORG-OPS.md
  added: PRODUCT-BRIEF.md
  added: README.md
  added: architecture/README.md
  added: index/README.md
  added: meta/DOC-FLOW.md
  added: meta/README.md
  added: ops/ORG-OPS.md
  added: ops/README.md
  added: plans/README.md
  added: product/PRODUCT-BRIEF.md
  added: product/README.md
  added: qa/README.md
  added: specs/README.md
  added: verification/README.md
stderr:
WARNING: missing state file /workspace/dealoware-kb/meta/fixtures/index-delta/scenarios/work/05-gitkeep-not-in-index/.doc-index-state.json; initializing empty v1

## live-kb-dry-run
exit_code=0
stdout:
added=1 modified=0 deleted=6 unchanged=20
  added: meta/rebuild-index-delta.sh
  deleted: architecture/.gitkeep
  deleted: index/.gitkeep
  deleted: plans/.gitkeep
  deleted: qa/.gitkeep
  deleted: specs/.gitkeep
  deleted: verification/.gitkeep
stderr:
dry-run: no writes
