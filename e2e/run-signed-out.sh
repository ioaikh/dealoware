#!/usr/bin/env bash
# Runs every browser suite of the admin pages that is built on the shared
# runner, one after the other. This is what Admin UI CI runs
# (.github/workflows/admin-ui-ci.yml), and it runs the same way by hand:
#
#   ./e2e/run-signed-out.sh                           every suite, three engines
#   E2E_ENGINES="chromium" ./e2e/run-signed-out.sh    every suite, one engine
#
# A suite is a folder e2e/<suite>/ whose run.sh sources
# e2e/signed-out/harness.sh. None is named here: a new suite is picked up the
# moment its run.sh exists.
#
# The run fails if
#
#   - no suite is found;
#   - a suite's run.sh fails: a test failed, no test was collected, or the app
#     did not start;
#   - a suite ran no test on one of the engines. A run in which every test
#     was skipped is not a pass. This is read from Playwright's own report of
#     each run (e2e/admin/ci/assert-executed.js), not from what was printed.
#
# A failure does not stop the suites after it; the summary at the end names
# each suite's result.
#
# Arguments go to every suite's run.sh, and from there to Playwright:
#
#   ./e2e/run-signed-out.sh --forbid-only
#
# Needs what the suites need (see e2e/signed-out/harness.sh). Works on Linux,
# macOS and Git Bash on Windows.
set -euo pipefail

# Git Bash: give Node a path it understands.
native_path() {
  if command -v cygpath >/dev/null 2>&1; then
    cygpath -m "$1"
  else
    printf '%s' "$1"
  fi
}

E2E="$(native_path "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)")"
ASSERT_EXECUTED="$E2E/admin/ci/assert-executed.js"

# Named here as well, so that this script knows which reports to expect.
export E2E_ENGINES="${E2E_ENGINES:-chromium firefox webkit}"
# A value of only spaces would run no engine and still pass: refuse it.
read -ra named_engines <<<"$E2E_ENGINES"
if [[ ${#named_engines[@]} -eq 0 ]]; then
  echo "E2E_ENGINES names no engine" >&2
  exit 1
fi

SUITES=()
for run in "$E2E"/*/run.sh; do
  [[ -f "$run" ]] || continue
  if grep -Eq '^[[:space:]]*(source|\.)[[:space:]].*/signed-out/harness\.sh' "$run"; then
    SUITES+=("$(basename "$(dirname "$run")")")
  fi
done

if [[ "${#SUITES[@]}" -eq 0 ]]; then
  echo "No suite found: no e2e/<suite>/run.sh sources e2e/signed-out/harness.sh" >&2
  exit 1
fi
echo "Suites: ${SUITES[*]}"
echo "Engines: ${E2E_ENGINES}"

REPORTS="$(native_path "$(mktemp -d "${TMPDIR:-/tmp}/dw-e2e-reports.XXXXXX")")"
trap 'rm -rf "$REPORTS"' EXIT

SUMMARY=()
FAILED=0
for suite in "${SUITES[@]}"; do
  echo
  echo "==== ${suite} ===="
  why=""
  if ! E2E_REPORT_DIR="$REPORTS/$suite" bash "$E2E/$suite/run.sh" "$@"; then
    why="its run.sh failed"
  fi

  # Counts only: the reports themselves are never printed.
  for engine in $E2E_ENGINES; do
    if counts="$(node "$ASSERT_EXECUTED" "$REPORTS/$suite/$engine.json")"; then
      echo "${suite} on ${engine}: ${counts}"
    else
      echo "${suite} on ${engine}: no test ran (${counts})" >&2
      why="${why:+$why; }no test ran on ${engine}"
    fi
  done

  if [[ -n "$why" ]]; then
    FAILED=1
    SUMMARY+=("${suite}: FAILED (${why})")
  else
    SUMMARY+=("${suite}: passed")
  fi
done

echo
echo "==== summary (${E2E_ENGINES}) ===="
printf '%s\n' "${SUMMARY[@]}"
if [[ "$FAILED" -ne 0 ]]; then
  echo "Browser suites failed" >&2
  exit 1
fi
echo "All ${#SUITES[@]} browser suites passed"
