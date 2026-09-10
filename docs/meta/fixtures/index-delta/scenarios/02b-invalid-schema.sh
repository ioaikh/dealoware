#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
name="02b-invalid-schema"
kb="$(clone_kb "$name")"
index_before=$(sha256sum "$kb/INDEX.md" | awk '{print $1}')
# Valid JSON but wrong schema (missing version)
printf '%s\n' '{"files":{},"kb_root":"x"}' > "$kb/.doc-index-state.json"
state_before=$(sha256sum "$kb/.doc-index-state.json" | awk '{print $1}')
out="$EVIDENCE/${name}.out"
err="$EVIDENCE/${name}.err"
set +e
run_helper "$kb" >"$out" 2>"$err"
ec=$?
set -e
echo "exit_code=$ec" | tee "$EVIDENCE/${name}.exit"
echo "--- stderr ---"; cat "$err"
test "$ec" -ne 0
grep -qiE 'ERROR:.*invalid state' "$err"
test "$(sha256sum "$kb/INDEX.md" | awk '{print $1}')" = "$index_before"
test "$(sha256sum "$kb/.doc-index-state.json" | awk '{print $1}')" = "$state_before"
echo "PASS $name"
