#!/usr/bin/env bash
# Shared fixture helpers
FIX_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCRIPT="${SCRIPT:-/workspace/dealoware-kb/meta/rebuild-index-delta.sh}"
KB_CLEAN="$FIX_ROOT/kb-clean"
WORK_ROOT="$FIX_ROOT/scenarios/work"
EVIDENCE="$FIX_ROOT/evidence"

mkdir -p "$WORK_ROOT" "$EVIDENCE"

clone_kb() {
  local name="$1"
  local dest="$WORK_ROOT/$name"
  rm -rf "$dest"
  mkdir -p "$dest"
  # Prefer cp -a; fall back
  cp -a "$KB_CLEAN/." "$dest/"
  # Ensure no leftover state unless scenario wants one
  echo "$dest"
}

run_helper() {
  local kb="$1"
  shift
  DEALOWARE_KB_ROOT="$kb" "$SCRIPT" "$@"
}
