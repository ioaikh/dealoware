#!/usr/bin/env bash
# First-time setup page (S-A4, /admin/bootstrap) Playwright run over HTTPS.
# Not wired into Admin UI CI.
#
#   ./e2e/bootstrap/run.sh
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

# The owner has no password yet: only the two setup links are seeded.
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
con = sqlite3.connect(db)
for token_hash, created, expires in rows:
    con.execute(
        """INSERT INTO AdminBootstrapTokens (
             Id, Email, TokenHash, CreatedAt, ExpiresAt, ConsumedAt
           ) VALUES (?, ?, ?, ?, ?, NULL)""",
        (str(uuid.uuid4()).upper(), owner, token_hash, stamp(created), stamp(expires)),
    )
con.commit()
con.close()
PY

export BOOTSTRAP_E2E_TOKEN="$LIVE_TOKEN"
export BOOTSTRAP_E2E_EXPIRED_TOKEN="$EXPIRED_TOKEN"
run_playwright "$@"
