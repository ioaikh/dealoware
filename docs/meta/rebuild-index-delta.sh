#!/usr/bin/env bash
# rebuild-index-delta.sh — Dealoware KB INDEX delta helper (CEO-approved 2026-09-10)
# See: meta/INDEX-DELTA.md, ops/2026-09-10__docs__ops__index-delta-tooling-proposal.md §3+§6
set -euo pipefail

DRY_RUN=0
for arg in "$@"; do
  case "$arg" in
    --dry-run) DRY_RUN=1 ;;
    -h|--help)
      cat <<'USAGE'
Usage: rebuild-index-delta.sh [--dry-run]

  DEALOWARE_KB_ROOT  KB root (default: /workspace/dealoware-kb)

Walks stable DOC-FLOW paths, classifies added/modified/deleted/unchanged via
mtime+sha256 state (.doc-index-state.json), patches INDEX.md for delta paths
only, writes state when non-empty delta. Empty delta is a no-op (no rewrite).
USAGE
      exit 0
      ;;
    *)
      echo "ERROR: unknown argument: $arg" >&2
      exit 2
      ;;
  esac
done

export DEALOWARE_KB_ROOT="${DEALOWARE_KB_ROOT:-/workspace/dealoware-kb}"
export REBUILD_INDEX_DELTA_DRY_RUN="$DRY_RUN"

# Core logic in Python stdlib (jq also available; no new installs).
exec python3 - "$DEALOWARE_KB_ROOT" "$DRY_RUN" <<'PY'
#!/usr/bin/env python3
"""INDEX delta helper — state schema v1, patch INDEX.md for delta paths only."""
from __future__ import annotations

import hashlib
import json
import os
import re
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

AREA_DIRS = (
    "product",
    "architecture",
    "specs",
    "plans",
    "qa",
    "verification",
    "ops",
    "meta",
    "index",
)
ROOT_FILES = ("INDEX.md", "README.md", "PRODUCT-BRIEF.md", "ORG-OPS.md")
STATE_NAME = ".doc-index-state.json"
EMPTY_PLACEHOLDER = "(empty — awaiting PM assigns)"
SKIP_INDEX_NAMES = {".gitkeep"}  # never list in INDEX; also skip from state walk


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def rel_key(path: Path, root: Path) -> str:
    return path.relative_to(root).as_posix()


def file_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def file_mtime(path: Path) -> int:
    return int(path.stat().st_mtime)


def collect_candidates(root: Path) -> dict[str, Path]:
    """Stable paths only; omit state file and .gitkeep from candidate set."""
    out: dict[str, Path] = {}
    for name in ROOT_FILES:
        p = root / name
        if p.is_file():
            out[name] = p
    for area in AREA_DIRS:
        d = root / area
        if not d.is_dir():
            continue
        for p in sorted(d.rglob("*")):
            if not p.is_file():
                continue
            if p.name == STATE_NAME:
                continue
            if p.name in SKIP_INDEX_NAMES:
                continue
            key = rel_key(p, root)
            # Fixture/QA trees under meta/fixtures/ are not Doc artifacts
            if key == "meta/fixtures" or key.startswith("meta/fixtures/"):
                continue
            out[key] = p
    # Never track state file
    out.pop(STATE_NAME, None)
    return out


def validate_state(data: Any) -> str | None:
    """Return error message if invalid; None if OK."""
    if not isinstance(data, dict):
        return "state root must be a JSON object"
    if "version" not in data:
        return "missing version"
    if data.get("version") != 1:
        return f"unsupported version: {data.get('version')!r} (need 1)"
    if "files" not in data:
        return "missing files"
    if not isinstance(data["files"], dict):
        return "files must be an object"
    for k, v in data["files"].items():
        if not isinstance(k, str):
            return "files keys must be strings"
        if not isinstance(v, dict):
            return f"files[{k!r}] must be an object"
        if "sha256" not in v or "mtime" not in v:
            return f"files[{k!r}] missing sha256 or mtime"
        if not isinstance(v["sha256"], str):
            return f"files[{k!r}].sha256 must be string"
        if not isinstance(v["mtime"], (int, float)):
            return f"files[{k!r}].mtime must be number"
    return None


def load_or_init_state(state_path: Path, kb_root: str) -> tuple[dict, bool]:
    """
    Returns (state, missing_initialized).
    Missing → empty v1 + warning (caller prints).
    Corrupt → raises SystemExit with non-zero.
    """
    if not state_path.exists():
        state = {
            "version": 1,
            "updated_at": utc_now_iso(),
            "kb_root": kb_root,
            "files": {},
        }
        return state, True
    try:
        text = state_path.read_text(encoding="utf-8")
        data = json.loads(text)
    except json.JSONDecodeError as e:
        print(
            f"ERROR: corrupt state JSON at {state_path}: {e}",
            file=sys.stderr,
        )
        sys.exit(1)
    except OSError as e:
        print(f"ERROR: cannot read state at {state_path}: {e}", file=sys.stderr)
        sys.exit(1)
    err = validate_state(data)
    if err:
        print(
            f"ERROR: invalid state schema at {state_path}: {err}",
            file=sys.stderr,
        )
        sys.exit(1)
    # Drop self-reference if present
    data["files"].pop(STATE_NAME, None)
    return data, False


def classify(
    candidates: dict[str, Path], state_files: dict[str, Any]
) -> tuple[list[str], list[str], list[str], list[str], dict[str, dict]]:
    """
    Returns added, modified, deleted, unchanged, and new_files map.
    Fast path: mtime match → treat as unchanged without re-hash when sha also
    would match; if mtime differs, recompute sha256.
    Unchanged: do not open body for INDEX (hash may still be read only when
    mtime differs — content not used for INDEX text).
    """
    added: list[str] = []
    modified: list[str] = []
    deleted: list[str] = []
    unchanged: list[str] = []
    new_files: dict[str, dict] = {}

    cand_keys = set(candidates)
    state_keys = set(state_files)

    for key in sorted(cand_keys - state_keys):
        p = candidates[key]
        sha = file_sha256(p)
        mt = file_mtime(p)
        new_files[key] = {"sha256": sha, "mtime": mt}
        added.append(key)

    for key in sorted(cand_keys & state_keys):
        p = candidates[key]
        prev = state_files[key]
        prev_mtime = int(prev["mtime"])
        prev_sha = prev["sha256"]
        mt = file_mtime(p)
        if mt == prev_mtime:
            # Fast path: assume content unchanged; keep prior sha
            new_files[key] = {"sha256": prev_sha, "mtime": mt}
            unchanged.append(key)
            continue
        # mtime differs → recompute hash
        sha = file_sha256(p)
        new_files[key] = {"sha256": sha, "mtime": mt}
        if sha == prev_sha:
            # touch without content change → unchanged for INDEX; refresh mtime in state
            unchanged.append(key)
        else:
            modified.append(key)

    for key in sorted(state_keys - cand_keys):
        # Skip .gitkeep leftovers from older state if any
        if Path(key).name in SKIP_INDEX_NAMES:
            deleted.append(key)
            continue
        deleted.append(key)

    return added, modified, deleted, unchanged, new_files


def area_for(relpath: str) -> str | None:
    """Return area dir name if under an area folder; None for root files."""
    parts = relpath.split("/")
    if len(parts) >= 2 and parts[0] in AREA_DIRS:
        return parts[0]
    return None


def link_line(relpath: str) -> str:
    name = Path(relpath).name
    return f"- [{name}]({relpath})"


def find_section_span(lines: list[str], heading: str) -> tuple[int, int] | None:
    """
    Find ## heading line index and end (exclusive) before next ## or EOF.
    heading like '## product/'
    """
    start = None
    for i, line in enumerate(lines):
        if line.strip() == heading:
            start = i
            break
    if start is None:
        return None
    end = len(lines)
    for j in range(start + 1, len(lines)):
        if lines[j].startswith("## "):
            end = j
            break
    return start, end


# Markdown list link; trailing description after ) is allowed (annotations).
LINK_LINE_RE = re.compile(r"^\s*-\s+\[([^\]]+)\]\(([^)]+)\)(.*)$")


def link_path_from_line(line: str) -> str | None:
    """Return markdown link target if line is a list link; else None.

    Trailing text after ')' (e.g. ' — notes') is allowed and ignored for path.
    """
    m = LINK_LINE_RE.match(line)
    return m.group(2) if m else None


def is_empty_placeholder_line(line: str) -> bool:
    s = line.strip()
    return s == EMPTY_PLACEHOLDER or s.startswith(EMPTY_PLACEHOLDER)


def section_link_paths(section_lines: list[str]) -> list[str]:
    """Extract markdown link targets from - [text](path) lines (trailing notes OK)."""
    paths: list[str] = []
    for line in section_lines:
        p = link_path_from_line(line)
        if p is not None:
            paths.append(p)
    return paths


def patch_index(index_text: str, added: list[str], deleted: list[str]) -> str:
    """
    Additive area-section updates only. Modify: keep link (no body re-read).
    Preserve original full lines (including trailing annotations) for kept paths.
    Skip .gitkeep. Root files: no inventing new structure.
    """
    lines = index_text.splitlines(keepends=True)

    # Work on lines with endings stripped for logic
    bare = [ln.rstrip("\n") for ln in lines]

    # Group ops by area
    adds_by_area: dict[str, list[str]] = {a: [] for a in AREA_DIRS}
    dels_by_area: dict[str, list[str]] = {a: [] for a in AREA_DIRS}

    for p in added:
        if Path(p).name in SKIP_INDEX_NAMES:
            continue
        area = area_for(p)
        if area:
            adds_by_area[area].append(p)

    for p in deleted:
        if Path(p).name in SKIP_INDEX_NAMES:
            continue
        area = area_for(p)
        if area:
            dels_by_area[area].append(p)

    # For each area with adds/dels: surgically edit section body (never rebuild
    # from a path-only set — that would drop annotations on sibling links).

    for area in AREA_DIRS:
        a_list = adds_by_area[area]
        d_list = dels_by_area[area]
        if not a_list and not d_list:
            continue
        heading = f"## {area}/"
        span = find_section_span(bare, heading)
        if span is None:
            # Do not invent new sections beyond existing pattern
            continue
        start, end = span
        body = bare[start + 1 : end]
        del_set = set(d_list)

        existing_paths = section_link_paths(body)
        paths_after_delete = [p for p in existing_paths if p not in del_set]
        present = set(paths_after_delete)
        new_adds = [
            p
            for p in a_list
            if p not in present and Path(p).name not in SKIP_INDEX_NAMES
        ]
        # Dedupe while preserving order
        seen_add: set[str] = set()
        unique_adds: list[str] = []
        for p in new_adds:
            if p not in seen_add:
                seen_add.add(p)
                unique_adds.append(p)
        will_have_links = bool(paths_after_delete) or bool(unique_adds)

        rebuilt: list[str] = []
        for line in body:
            p = link_path_from_line(line)
            if p is not None:
                if p in del_set:
                    continue  # remove only this path's line(s)
                # Keep original full line including trailing notes
                rebuilt.append(line)
                continue
            if is_empty_placeholder_line(line):
                if will_have_links:
                    # Drop placeholder; keep trailing note if combined line
                    # e.g. "(empty — awaiting PM assigns) — optional fragments..."
                    s = line.strip()
                    if s != EMPTY_PLACEHOLDER and s.startswith(EMPTY_PLACEHOLDER):
                        rest = s[len(EMPTY_PLACEHOLDER) :].strip()
                        if rest:
                            rebuilt.append(rest)
                    continue
                rebuilt.append(line)
                continue
            # blanks, standalone notes, etc. — leave intact
            rebuilt.append(line)

        if unique_adds:
            # Append new bare link lines immediately after the last existing link
            # (before trailing blanks / horizontal rules that sit in the section).
            last_link_i = None
            for i, line in enumerate(rebuilt):
                if link_path_from_line(line) is not None:
                    last_link_i = i
            if last_link_i is not None:
                insert_at = last_link_i + 1
            else:
                # Empty→links: after leading blanks, before notes / --- / trailers
                insert_at = 0
                while insert_at < len(rebuilt) and rebuilt[insert_at].strip() == "":
                    insert_at += 1
                if insert_at == 0:
                    rebuilt.insert(0, "")
                    insert_at = 1
            for p in sorted(unique_adds):
                rebuilt.insert(insert_at, link_line(p))
                insert_at += 1

        has_link = any(link_path_from_line(ln) is not None for ln in rebuilt)
        has_ph = any(is_empty_placeholder_line(ln) for ln in rebuilt)
        if not has_link and not has_ph:
            insert_at = 0
            while insert_at < len(rebuilt) and rebuilt[insert_at].strip() == "":
                insert_at += 1
            if insert_at == 0:
                rebuilt.insert(0, "")
                insert_at = 1
            rebuilt.insert(insert_at, EMPTY_PLACEHOLDER)

        # Keep a trailing blank before the next ## section when body had one
        if not rebuilt or rebuilt[-1].strip() != "":
            # Preserve prior trailing blank convention if next section follows
            if end < len(bare) and bare[end].startswith("## "):
                rebuilt.append("")
            elif end == len(bare):
                rebuilt.append("")

        bare = bare[: start + 1] + rebuilt + bare[end:]

    # Ensure trailing newline
    text = "\n".join(bare)
    if not text.endswith("\n"):
        text += "\n"
    return text


def atomic_write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, tmp = tempfile.mkstemp(dir=str(path.parent), prefix=f".{path.name}.", suffix=".tmp")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            f.write(content)
            f.flush()
            os.fsync(f.fileno())
        os.replace(tmp, path)
    except Exception:
        try:
            os.unlink(tmp)
        except OSError:
            pass
        raise


def main() -> int:
    if len(sys.argv) < 3:
        print("Usage: rebuild-index-delta.sh [--dry-run]", file=sys.stderr)
        return 2
    root = Path(sys.argv[1]).resolve()
    dry_run = sys.argv[2] == "1"

    if not root.is_dir():
        print(f"ERROR: KB root not a directory: {root}", file=sys.stderr)
        return 1

    state_path = root / STATE_NAME
    index_path = root / "INDEX.md"

    state, missing = load_or_init_state(state_path, str(root))
    if missing:
        print(
            f"WARNING: missing state file {state_path}; initializing empty v1",
            file=sys.stderr,
        )

    candidates = collect_candidates(root)
    # Also drop any .gitkeep entries lingering in loaded state from classification
    # perspective: treat them as not-in-candidates so they become deleted from state
    state_files = dict(state.get("files") or {})
    # Remove state self if somehow present
    state_files.pop(STATE_NAME, None)

    added, modified, deleted, unchanged, new_files = classify(candidates, state_files)

    # Filter .gitkeep from reported deleted if we never wanted them — still OK to clean
    # Emit counts
    print(
        f"added={len(added)} modified={len(modified)} "
        f"deleted={len(deleted)} unchanged={len(unchanged)}"
    )
    for label, items in (
        ("added", added),
        ("modified", modified),
        ("deleted", deleted),
    ):
        for p in items:
            print(f"  {label}: {p}")

    delta_n = len(added) + len(modified) + len(deleted)
    if delta_n == 0:
        # Empty delta → no rewrite of INDEX or state
        return 0

    # Patch INDEX only for add/delete (modify keeps link)
    index_changed = bool(added or deleted)
    new_index: str | None = None
    if index_changed:
        if not index_path.is_file():
            print(f"ERROR: INDEX.md missing at {index_path}", file=sys.stderr)
            return 1
        old = index_path.read_text(encoding="utf-8")
        new_index = patch_index(old, added, deleted)

    new_state = {
        "version": 1,
        "updated_at": utc_now_iso(),
        "kb_root": str(root),
        "files": new_files,
    }
    # Ensure state never contains itself
    new_state["files"].pop(STATE_NAME, None)

    if dry_run:
        print("dry-run: no writes", file=sys.stderr)
        return 0

    try:
        if new_index is not None:
            atomic_write(index_path, new_index)
            # Re-hash INDEX after patch so next run does not false-positive "modified"
            if index_path.is_file() and "INDEX.md" in new_files:
                new_files["INDEX.md"] = {
                    "sha256": file_sha256(index_path),
                    "mtime": file_mtime(index_path),
                }
            elif index_path.is_file():
                new_files["INDEX.md"] = {
                    "sha256": file_sha256(index_path),
                    "mtime": file_mtime(index_path),
                }
            new_state["files"] = new_files
            new_state["files"].pop(STATE_NAME, None)
        atomic_write(state_path, json.dumps(new_state, indent=2, sort_keys=False) + "\n")
    except OSError as e:
        print(f"ERROR: write failed: {e}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
PY
