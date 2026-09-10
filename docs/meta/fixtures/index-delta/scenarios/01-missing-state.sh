#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
name="01-missing-state"
kb="$(clone_kb "$name")"
rm -f "$kb/.doc-index-state.json"
out="$EVIDENCE/${name}.out"
err="$EVIDENCE/${name}.err"
set +e
run_helper "$kb" >"$out" 2>"$err"
ec=$?
set -e
echo "exit_code=$ec" | tee "$EVIDENCE/${name}.exit"
echo "--- stdout ---"; cat "$out"
echo "--- stderr ---"; cat "$err"
# Assertions
grep -q 'WARNING: missing state file' "$err"
test -f "$kb/.doc-index-state.json"
jq -e '.version == 1 and (.files | type == "object")' "$kb/.doc-index-state.json" >/dev/null
# state must not contain itself
jq -e 'has(".doc-index-state.json") | not' "$kb/.doc-index-state.json" >/dev/null || \
  jq -e '.files | has(".doc-index-state.json") | not' "$kb/.doc-index-state.json" >/dev/null
echo "PASS $name"
