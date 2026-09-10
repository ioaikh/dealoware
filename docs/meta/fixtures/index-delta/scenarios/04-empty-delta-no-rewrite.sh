#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
name="04-empty-delta-no-rewrite"
kb="$(clone_kb "$name")"
rm -f "$kb/.doc-index-state.json"

# Init + stabilize
run_helper "$kb" >/dev/null 2>"$EVIDENCE/${name}-init.err"
# Second run should be empty delta
sleep 1  # ensure mtime would differ if rewritten
index_before=$(sha256sum "$kb/INDEX.md" | awk '{print $1}')
state_before=$(sha256sum "$kb/.doc-index-state.json" | awk '{print $1}')
index_mtime_before=$(stat -c %Y "$kb/INDEX.md")
state_mtime_before=$(stat -c %Y "$kb/.doc-index-state.json")
updated_at_before=$(jq -r .updated_at "$kb/.doc-index-state.json")

out="$EVIDENCE/${name}.out"
err="$EVIDENCE/${name}.err"
set +e
run_helper "$kb" >"$out" 2>"$err"
ec=$?
set -e
echo "exit_code=$ec" | tee "$EVIDENCE/${name}.exit"
echo "--- stdout ---"; cat "$out"

test "$ec" -eq 0
head -n1 "$out" | grep -q 'added=0'
head -n1 "$out" | grep -q 'modified=0'
head -n1 "$out" | grep -q 'deleted=0'

index_after=$(sha256sum "$kb/INDEX.md" | awk '{print $1}')
state_after=$(sha256sum "$kb/.doc-index-state.json" | awk '{print $1}')
index_mtime_after=$(stat -c %Y "$kb/INDEX.md")
state_mtime_after=$(stat -c %Y "$kb/.doc-index-state.json")
updated_at_after=$(jq -r .updated_at "$kb/.doc-index-state.json")

{
  echo "index_sha_before=$index_before"
  echo "index_sha_after=$index_after"
  echo "state_sha_before=$state_before"
  echo "state_sha_after=$state_after"
  echo "index_mtime_before=$index_mtime_before"
  echo "index_mtime_after=$index_mtime_after"
  echo "state_mtime_before=$state_mtime_before"
  echo "state_mtime_after=$state_mtime_after"
  echo "updated_at_before=$updated_at_before"
  echo "updated_at_after=$updated_at_after"
} | tee "$EVIDENCE/${name}.compare"

test "$index_before" = "$index_after"
test "$state_before" = "$state_after"
test "$index_mtime_before" = "$index_mtime_after"
test "$state_mtime_before" = "$state_mtime_after"
test "$updated_at_before" = "$updated_at_after"
echo "PASS $name (no rewrite)"
