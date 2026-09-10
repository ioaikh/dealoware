#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
name="03-add-modify-delete-unchanged"
kb="$(clone_kb "$name")"
rm -f "$kb/.doc-index-state.json"

# Baseline init
run_helper "$kb" >"$EVIDENCE/${name}-init.out" 2>"$EVIDENCE/${name}-init.err"

# Mutations:
# ADD: new architecture doc
echo '# New arch doc' > "$kb/architecture/2026-09-10__sa__architecture__fixture-delta.md"
# MODIFY: change product brief content
echo '# PRODUCT-BRIEF fixture MODIFIED' > "$kb/product/PRODUCT-BRIEF.md"
# DELETE: remove meta README from disk (was tracked)
rm -f "$kb/meta/README.md"
# UNCHANGED: leave ops/ORG-OPS.md alone

out="$EVIDENCE/${name}.out"
err="$EVIDENCE/${name}.err"
set +e
run_helper "$kb" >"$out" 2>"$err"
ec=$?
set -e
echo "exit_code=$ec" | tee "$EVIDENCE/${name}.exit"
echo "--- stdout ---"; cat "$out"
echo "--- stderr ---"; cat "$err"
test "$ec" -eq 0

# Parse counts from first line
counts=$(head -n1 "$out")
echo "counts_line=$counts" | tee "$EVIDENCE/${name}.counts"
echo "$counts" | grep -q 'added=1'
echo "$counts" | grep -q 'modified=1'
echo "$counts" | grep -q 'deleted=1'
# unchanged should be > 0
unchanged=$(echo "$counts" | sed -n 's/.*unchanged=\([0-9]*\).*/\1/p')
test "$unchanged" -gt 0

grep -q 'added: architecture/2026-09-10__sa__architecture__fixture-delta.md' "$out"
grep -q 'modified: product/PRODUCT-BRIEF.md' "$out"
grep -q 'deleted: meta/README.md' "$out"

# INDEX should list new arch file, not list .gitkeep, and not list deleted meta README
grep -q 'architecture/2026-09-10__sa__architecture__fixture-delta.md' "$kb/INDEX.md"
! grep -q '\.gitkeep' "$kb/INDEX.md"
! grep -q 'meta/README.md' "$kb/INDEX.md"

echo "PASS $name counts=$counts"
