#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
name="02-corrupt-state"
kb="$(clone_kb "$name")"
# First create a known INDEX checksum
index_before=$(sha256sum "$kb/INDEX.md" | awk '{print $1}')
# Write corrupt state
echo '{not valid json!!!' > "$kb/.doc-index-state.json"
state_before=$(sha256sum "$kb/.doc-index-state.json" | awk '{print $1}')
out="$EVIDENCE/${name}.out"
err="$EVIDENCE/${name}.err"
set +e
run_helper "$kb" >"$out" 2>"$err"
ec=$?
set -e
echo "exit_code=$ec" | tee "$EVIDENCE/${name}.exit"
echo "--- stdout ---"; cat "$out"
echo "--- stderr ---"; cat "$err"
test "$ec" -ne 0
grep -qiE 'ERROR:.*(corrupt|invalid)' "$err"
index_after=$(sha256sum "$kb/INDEX.md" | awk '{print $1}')
state_after=$(sha256sum "$kb/.doc-index-state.json" | awk '{print $1}')
test "$index_before" = "$index_after"
test "$state_before" = "$state_after"
echo "PASS $name (INDEX and state untouched)"
