#!/usr/bin/env bash
# First-time setup page (S-A4, /admin/bootstrap) Playwright run over HTTPS,
# on Chromium, Firefox and WebKit. Admin UI CI runs it with the other suites,
# through e2e/run-signed-out.sh.
#
#   ./e2e/bootstrap/run.sh
#   E2E_ENGINES="firefox" ./e2e/bootstrap/run.sh      one engine only
#
# The runner itself is shared with the other signed-out page suites:
# e2e/signed-out/harness.sh starts the app on 127.0.0.1:18444 behind a local
# CONNECT proxy on 127.0.0.1:18082, with a Turnstile verifier stand-in on
# 127.0.0.1:18083. This file names the ports, seeds what this suite needs
# and starts its spec.
set -euo pipefail

SUITE=bootstrap
SUITE_ENV=BOOTSTRAP_E2E
API_PORT=18444
PROXY_PORT=18082
VERIFY_PORT=18083

# shellcheck source=../signed-out/harness.sh
source "$(dirname "$0")/../signed-out/harness.sh"

start_app_behind_proxy

# A live setup link, and one whose 24 hours ran out an hour ago.
LIVE_TOKEN="$(new_token)"
EXPIRED_TOKEN="$(new_token)"

# This suite uses up what it is given: its last tests set the owner's
# password, spend the live link and run the address's budget out. So before
# each engine the run is put back to where a first run starts: an owner with
# no password, the two setup links unused, and no budget spent.
seed_suite() {
  "$PY" - "$DB" "$OWNER" "$(sha256_upper "$LIVE_TOKEN")" "$(sha256_upper "$EXPIRED_TOKEN")" <<'PY'
import sqlite3, sys, uuid
from datetime import datetime, timedelta, timezone

db, owner, live_hash, expired_hash = sys.argv[1:]

def stamp(moment):
    return moment.strftime("%Y-%m-%dT%H:%M:%S.%f") + "0Z"

now = datetime.now(timezone.utc)
rows = [
    (live_hash, now, now + timedelta(hours=24)),
    (expired_hash, now - timedelta(hours=25), now - timedelta(hours=1)),
]
con = sqlite3.connect(db, timeout=15)
with con:
    for table in ("AdminBootstrapTokens", "AdminCoreOwnerAccounts", "AdminCredentials", "AdminResetIpCounters"):
        con.execute(f"DELETE FROM {table}")
    for token_hash, created, expires in rows:
        con.execute(
            """INSERT INTO AdminBootstrapTokens (
                 Id, Email, TokenHash, CreatedAt, ExpiresAt, ConsumedAt
               ) VALUES (?, ?, ?, ?, ?, NULL)""",
            (str(uuid.uuid4()).upper(), owner, token_hash, stamp(created), stamp(expires)),
        )
con.close()
PY
}

export BOOTSTRAP_E2E_TOKEN="$LIVE_TOKEN"
export BOOTSTRAP_E2E_EXPIRED_TOKEN="$EXPIRED_TOKEN"
run_playwright "$@"
