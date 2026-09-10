#!/usr/bin/env bash
# Scenario: area section has annotated sibling links; add one file →
# siblings + annotations remain; new link present.
set -euo pipefail
source "$(dirname "$0")/_lib.sh"
name="06-add-preserves-annotated-siblings"
kb="$(clone_kb "$name")"
rm -f "$kb/.doc-index-state.json"

# Annotate existing ops sibling links (matches live INDEX style)
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
"""
if old not in text:
    raise SystemExit("expected bare ops section not found in fixture INDEX")
p.write_text(text.replace(old, new, 1), encoding="utf-8")
PY

# Baseline state after annotated INDEX is in place
run_helper "$kb" >"$EVIDENCE/${name}-init.out" 2>"$EVIDENCE/${name}-init.err"

# Snapshot ops section lines we must preserve exactly
ops_before="$EVIDENCE/${name}.ops-before"
python3 - "$kb/INDEX.md" "$ops_before" <<'PY'
from pathlib import Path
import sys
lines = Path(sys.argv[1]).read_text(encoding="utf-8").splitlines()
start = lines.index("## ops/")
end = next(i for i in range(start + 1, len(lines)) if lines[i].startswith("## "))
Path(sys.argv[2]).write_text("\n".join(lines[start:end]) + "\n", encoding="utf-8")
PY

# ADD one ops file
echo '# Fixture additive ops doc' > \
  "$kb/ops/2026-09-10__devops__ops__fixture-annotated-add.md"

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
head -n1 "$out" | grep -q 'added=1'
grep -q 'added: ops/2026-09-10__devops__ops__fixture-annotated-add.md' "$out"

# Capture ops section after
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

# Exact annotated sibling lines must remain
grep -Fqx -- \
  '- [ORG-OPS.md](ops/ORG-OPS.md) — agent ops charter (stable; fixture annotation)' \
  "$ops_after"
grep -Fqx -- \
  '- [README.md](ops/README.md) — area readme note (fixture annotation)' \
  "$ops_after"
# New bare link present
grep -Fqx -- \
  '- [2026-09-10__devops__ops__fixture-annotated-add.md](ops/2026-09-10__devops__ops__fixture-annotated-add.md)' \
  "$ops_after"
# New link must sit with sibling links (before section ---), not after ---
python3 - "$ops_after" <<'PY2'
from pathlib import Path
import sys
lines = Path(sys.argv[1]).read_text(encoding="utf-8").splitlines()
new = "- [2026-09-10__devops__ops__fixture-annotated-add.md](ops/2026-09-10__devops__ops__fixture-annotated-add.md)"
ni = lines.index(new)
# last annotated sibling
ri = lines.index("- [README.md](ops/README.md) — area readme note (fixture annotation)")
assert ni == ri + 1, f"new link index {ni} should be right after README at {ri}: {lines}"
if "---" in lines:
    assert ni < lines.index("---"), "new link must appear before section ---"
print("ordering OK")
PY2
# Must not have stripped annotations to bare forms only
! grep -Fqx -- '- [ORG-OPS.md](ops/ORG-OPS.md)' "$ops_after"
! grep -Fqx -- '- [README.md](ops/README.md)' "$ops_after"

echo "PASS $name"
