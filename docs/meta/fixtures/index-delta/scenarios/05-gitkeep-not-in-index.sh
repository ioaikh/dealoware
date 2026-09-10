#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
name="05-gitkeep-not-in-index"
kb="$(clone_kb "$name")"
rm -f "$kb/.doc-index-state.json"
# Ensure .gitkeep exists in empty-ish areas
touch "$kb/architecture/.gitkeep"
touch "$kb/plans/.gitkeep"

out="$EVIDENCE/${name}.out"
err="$EVIDENCE/${name}.err"
run_helper "$kb" >"$out" 2>"$err"
echo "exit_code=$?" | tee "$EVIDENCE/${name}.exit"

# INDEX must not contain .gitkeep links
if grep -n '\.gitkeep' "$kb/INDEX.md"; then
  echo "FAIL: .gitkeep found in INDEX" >&2
  exit 1
fi
# State should also not track .gitkeep (helper skips them)
if jq -r '.files | keys[]' "$kb/.doc-index-state.json" | grep -q '\.gitkeep'; then
  echo "NOTE: .gitkeep present in state (optional); INDEX clean is required" | tee -a "$EVIDENCE/${name}.out"
  # Prefer skip from state too — fail if present so policy is clear
  echo "FAIL: .gitkeep tracked in state (helper prefers skip)" >&2
  exit 1
fi
cp "$kb/INDEX.md" "$EVIDENCE/${name}.INDEX.md"
echo "PASS $name"
