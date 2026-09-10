#!/usr/bin/env bash
# Scenario: delete one annotated link → only that path gone; others unchanged including notes.
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
name="07-delete-annotated-preserves-siblings"
kb="$(clone_kb "$name")"
rm -f "$kb/.doc-index-state.json"

# Three annotated ops links (two stay, one deleted)
python3 - "$kb/INDEX.md" <<'PY'
from pathlib import Path
import sys
p = Path(sys.argv[1])
text = p.read_text(encoding="utf-8")
old = """## ops/

- [ORG-OPS.md](ops/ORG-OPS.md)
- [README.md](ops/README.md)
"""
new = """## ops/

- [ORG-OPS.md](ops/ORG-OPS.md) — agent ops charter (stable; fixture annotation)
- [README.md](ops/README.md) — area readme note (fixture annotation)
- [TO-DELETE.md](ops/TO-DELETE.md) — doomed annotated sibling (fixture)
"""
if old not in text:
    raise SystemExit("expected bare ops section not found in fixture INDEX")
p.write_text(text.replace(old, new, 1), encoding="utf-8")
PY

# Create the third ops file so state tracks it
echo '# Fixture doomed ops doc' > "$kb/ops/TO-DELETE.md"

# Baseline state with all three present
run_helper "$kb" >"$EVIDENCE/${name}-init.out" 2>"$EVIDENCE/${name}-init.err"

ops_before="$EVIDENCE/${name}.ops-before"
python3 - "$kb/INDEX.md" "$ops_before" <<'PY'
from pathlib import Path
import sys
lines = Path(sys.argv[1]).read_text(encoding="utf-8").splitlines()
start = lines.index("## ops/")
end = next(i for i in range(start + 1, len(lines)) if lines[i].startswith("## "))
Path(sys.argv[2]).write_text("\n".join(lines[start:end]) + "\n", encoding="utf-8")
PY

# DELETE the annotated TO-DELETE path from disk
rm -f "$kb/ops/TO-DELETE.md"

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
head -n1 "$out" | grep -q 'deleted=1'
grep -q 'deleted: ops/TO-DELETE.md' "$out"

ops_after="$EVIDENCE/${name}.ops-after"
python3 - "$kb/INDEX.md" "$ops_after" <<'PY'
from pathlib import Path
import sys
lines = Path(sys.argv[1]).read_text(encoding="utf-8").splitlines()
start = lines.index("## ops/")
end = next(i for i in range(start + 1, len(lines)) if lines[i].startswith("## "))
Path(sys.argv[2]).write_text("\n".join(lines[start:end]) + "\n", encoding="utf-8")
PY
cp "$kb/INDEX.md" "$EVIDENCE/${name}.INDEX.md"

# Deleted path gone (link + its annotation)
! grep -q 'TO-DELETE.md' "$ops_after"
! grep -q 'doomed annotated sibling' "$ops_after"

# Remaining annotated siblings unchanged (exact lines)
grep -Fqx -- \
  '- [ORG-OPS.md](ops/ORG-OPS.md) — agent ops charter (stable; fixture annotation)' \
  "$ops_after"
grep -Fqx -- \
  '- [README.md](ops/README.md) — area readme note (fixture annotation)' \
  "$ops_after"

echo "PASS $name"
