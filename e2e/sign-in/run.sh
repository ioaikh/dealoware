#!/usr/bin/env bash
# Sign-in pages Playwright run over HTTPS: S-A1 /admin/sign-in, S-A2
# /admin/sign-in/code and S-A3 /admin/sign-in/recovery, on Chromium, Firefox
# and WebKit. Admin UI CI runs it with the other suites, through
# e2e/run-signed-out.sh.
#
#   ./e2e/sign-in/run.sh
#   E2E_ENGINES="firefox" ./e2e/sign-in/run.sh      one engine only
#
# The runner itself is shared with the other signed-out page suites:
# e2e/signed-out/harness.sh starts the app on 127.0.0.1:18445 behind a local
# CONNECT proxy on 127.0.0.1:18084, with a Turnstile verifier stand-in on
# 127.0.0.1:18085. This file names the ports and starts this suite's spec.
set -euo pipefail

SUITE=sign-in
SUITE_ENV=SIGN_IN_E2E
API_PORT=18445
PROXY_PORT=18084
VERIFY_PORT=18085

# shellcheck source=../signed-out/harness.sh
source "$(dirname "$0")/../signed-out/harness.sh"

start_app_behind_proxy

# Nothing is seeded here. The spec puts the owner into the state each test
# needs, before every test (e2e/sign-in/owner.ts): with a password and an
# enrolled authenticator, or with a password only. For that it gets the
# database file, a Python to write to it, and the two run-time keys the app
# reads those rows with. The password, the authenticator key and the recovery
# codes are made up by the spec and never written down; the keys and the
# database are gone when this run ends.
export SIGN_IN_E2E_DB="$DB"
export SIGN_IN_E2E_PYTHON="$PY"
export SIGN_IN_E2E_OWNER="$OWNER"
export SIGN_IN_E2E_TOTP_KEY="$TOTP_KEY"
export SIGN_IN_E2E_RECOVERY_KEY="$RECOVERY_KEY"
run_playwright "$@"
