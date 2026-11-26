#!/usr/bin/env bash
# Authenticator set-up pages Playwright run over HTTPS: S-A6
# /admin/setup/authenticator and S-A7 /admin/setup/recovery-codes, reached the
# way an owner reaches them, through a first sign-in. On Chromium, Firefox and
# WebKit. Admin UI CI runs it with the other suites, through
# e2e/run-signed-out.sh.
#
#   ./e2e/enrol/run.sh
#   E2E_ENGINES="firefox" ./e2e/enrol/run.sh      one engine only
#
# The runner itself is shared with the other signed-out page suites:
# e2e/signed-out/harness.sh starts the app on 127.0.0.1:18446 behind a local
# CONNECT proxy on 127.0.0.1:18086, with a Turnstile verifier stand-in on
# 127.0.0.1:18087. This file names the ports and starts this suite's spec.
set -euo pipefail

SUITE=enrol
SUITE_ENV=ENROL_E2E
API_PORT=18446
PROXY_PORT=18086
VERIFY_PORT=18087

# shellcheck source=../signed-out/harness.sh
source "$(dirname "$0")/../signed-out/harness.sh"

start_app_behind_proxy

# Nothing is seeded here. Before every test the spec puts the owner into the
# state a first sign-in starts from: a password and no authenticator. It does
# that with the sign-in suite's own seeding (e2e/sign-in/owner.ts and
# seed_owner.py), which reads its settings under that suite's names; so the
# database file, a Python to write to it and the two run-time keys are handed
# over under those names. The password is made up by the seeding and never
# written down. The authenticator key and the recovery codes are made up by
# the app itself, and the spec reads them off the pages as an owner would.
# The keys and the database are gone when this run ends.
export SIGN_IN_E2E_DB="$DB"
export SIGN_IN_E2E_PYTHON="$PY"
export SIGN_IN_E2E_OWNER="$OWNER"
export SIGN_IN_E2E_TOTP_KEY="$TOTP_KEY"
export SIGN_IN_E2E_RECOVERY_KEY="$RECOVERY_KEY"
run_playwright "$@"
