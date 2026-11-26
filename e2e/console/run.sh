#!/usr/bin/env bash
# Signed-in console shell Playwright run over HTTPS: S-B1 Stats at /admin/,
# the nav and the pages it leads to, S-D3, sign-out, and the session gate's
# way from a signed-in page to sign-in and back, on Chromium, Firefox and
# WebKit. Admin UI CI runs it with the other suites, through
# e2e/run-signed-out.sh.
#
#   ./e2e/console/run.sh
#   E2E_ENGINES="firefox" ./e2e/console/run.sh      one engine only
#
# The runner is the one the signed-out page suites share (the name is
# theirs; nothing in it is about being signed out):
# e2e/signed-out/harness.sh starts the app on 127.0.0.1:18448 behind a local
# CONNECT proxy on 127.0.0.1:18090, with a Turnstile verifier stand-in on
# 127.0.0.1:18091. This file names the ports and starts this suite's spec.
set -euo pipefail

SUITE=console
SUITE_ENV=CONSOLE_E2E
API_PORT=18448
PROXY_PORT=18090
VERIFY_PORT=18091

# shellcheck source=../signed-out/harness.sh
source "$(dirname "$0")/../signed-out/harness.sh"

start_app_behind_proxy

# Nothing is seeded here. Before every test the spec seeds the owner with a
# password and an enrolled authenticator through the sign-in suite's own
# seeding (e2e/sign-in/owner.ts and seed_owner.py), and signs in through the
# real pages. That module reads its settings under the sign-in suite's
# names, so the database file, a Python to write to it and the two run-time
# keys are handed over under those names. The password and the authenticator
# key are made up by the spec and never written down; the keys and the
# database are gone when this run ends.
export SIGN_IN_E2E_DB="$DB"
export SIGN_IN_E2E_PYTHON="$PY"
export SIGN_IN_E2E_OWNER="$OWNER"
export SIGN_IN_E2E_TOTP_KEY="$TOTP_KEY"
export SIGN_IN_E2E_RECOVERY_KEY="$RECOVERY_KEY"
run_playwright "$@"
