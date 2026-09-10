#!/usr/bin/env bash
set -euo pipefail
DIR="$(cd "$(dirname "$0")" && pwd)"
source "$DIR/_lib.sh"
echo "=== INDEX delta fixture suite ==="
echo "SCRIPT=$SCRIPT"
fail=0
for s in 01-missing-state.sh 02-corrupt-state.sh 02b-invalid-schema.sh \
         03-add-modify-delete-unchanged.sh 04-empty-delta-no-rewrite.sh \
         05-gitkeep-not-in-index.sh \
         06-add-preserves-annotated-siblings.sh \
         07-delete-annotated-preserves-siblings.sh; do
  echo ""
  echo ">>> Running $s"
  if bash "$DIR/$s"; then
    echo "OK $s"
  else
    echo "FAIL $s" >&2
    fail=1
  fi
done
echo ""
if test "$fail" -eq 0; then
  echo "ALL FIXTURES PASSED"
  echo "Evidence: $EVIDENCE"
  exit 0
else
  echo "SOME FIXTURES FAILED" >&2
  exit 1
fi
