#!/usr/bin/env bash
# Password-reset pages Playwright run over HTTPS: S-A8 /admin/reset and S-A9
# /admin/reset/sent, with S-A5 /admin/link-expired, S-A10
# /admin/reset/confirm and the two sentences S-A1 can open with, on Chromium,
# Firefox and WebKit. Not wired into Admin UI CI.
#
#   ./e2e/reset/run.sh
#   E2E_ENGINES="firefox" ./e2e/reset/run.sh      one engine only
#
# The runner itself is shared with the other signed-out page suites:
# e2e/signed-out/harness.sh starts the app on 127.0.0.1:18447 behind a local
# CONNECT proxy on 127.0.0.1:18088, with a Turnstile verifier stand-in on
# 127.0.0.1:18089. This file names the ports and starts this suite's spec.
set -euo pipefail

SUITE=reset
SUITE_ENV=RESET_E2E
API_PORT=18447
PROXY_PORT=18088
VERIFY_PORT=18089

# shellcheck source=../signed-out/harness.sh
source "$(dirname "$0")/../signed-out/harness.sh"

start_app_behind_proxy

# Nothing is seeded here. Before every test the spec puts the owner into the
# state a reset starts from: a password and an enrolled authenticator, with
# the sign-in suite's own seeding (e2e/sign-in/owner.ts and seed_owner.py),
# which reads its settings under that suite's names. So the database file, a
# Python to write to it and the two run-time keys are handed over under those
# names. The reset rows (the stored password, the links, an unspent budget)
# are then written by this suite's seed_reset.py. The password, the
# authenticator key and the link tokens are made up by the spec and never
# written down; the keys and the database are gone when this run ends.
export SIGN_IN_E2E_DB="$DB"
export SIGN_IN_E2E_PYTHON="$PY"
export SIGN_IN_E2E_OWNER="$OWNER"
export SIGN_IN_E2E_TOTP_KEY="$TOTP_KEY"
export SIGN_IN_E2E_RECOVERY_KEY="$RECOVERY_KEY"
run_playwright "$@"
