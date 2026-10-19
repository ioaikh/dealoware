#!/usr/bin/env bash
# Confirm-page Playwright over HTTPS (Chief UI/UX N5).
# Not wired into Admin UI CI (deny-live would reject the admin host; that
# workflow's path filter is e2e/admin/**).
#
#   ./e2e/reset-confirm/run.sh
#
# Throwaway cert and keys stay in mktemp. Nothing is written into the repo.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
E2E="$ROOT/e2e"
WORKDIR="$(mktemp -d "${TMPDIR:-/tmp}/dw-reset-confirm.XXXXXX")"
cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
  fi
  rm -rf "$WORKDIR"
}
trap cleanup EXIT

umask 077
PFX_PASS="$(openssl rand -base64 24)"
openssl req -x509 -newkey rsa:2048 \
  -keyout "$WORKDIR/key.pem" -out "$WORKDIR/cert.pem" \
  -days 1 -nodes \
  -subj "/CN=admin.core.dealoware.com" \
  -addext "subjectAltName=DNS:admin.core.dealoware.com,IP:127.0.0.1" \
  >/dev/null 2>&1
openssl pkcs12 -export \
  -out "$WORKDIR/cert.pfx" \
  -inkey "$WORKDIR/key.pem" \
  -in "$WORKDIR/cert.pem" \
  -passout "pass:${PFX_PASS}" \
  >/dev/null 2>&1
rm -f "$WORKDIR/key.pem" "$WORKDIR/cert.pem"

HMAC_KEY="$(openssl rand -base64 32)"
TOTP_KEY="$(openssl rand -base64 32)"
RECOVERY_KEY="$(openssl rand -base64 32)"
DB="$WORKDIR/e2e.db"
OWNER="alex.doe@example.test"
TOKEN="$(openssl rand -base64 32 | tr '+/' '-_' | tr -d '=')"
TOKEN_HASH="$(printf '%s' "$TOKEN" | openssl dgst -sha256 -hex | awk '{print toupper($NF)}')"
NOW="$(python3 - <<'PY'
from datetime import datetime, timezone
print(datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%S.%f') + '0Z')
PY
)"
EXPIRES="$(python3 - <<'PY'
from datetime import datetime, timezone, timedelta
print((datetime.now(timezone.utc) + timedelta(hours=1)).strftime('%Y-%m-%dT%H:%M:%S.%f') + '0Z')
PY
)"
DUMMY_HASH='pbkdf2-sha512$220000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA='
ACCOUNT_ID="$(python3 - <<'PY'
import uuid
print(str(uuid.uuid4()))
PY
)"

PORT=8443
if python3 - <<'PY'
import socket, sys
s = socket.socket()
try:
    s.bind(("127.0.0.1", 443))
    sys.exit(0)
except OSError:
    sys.exit(1)
finally:
    s.close()
PY
then
  PORT=443
fi

if [[ "$PORT" -eq 443 ]]; then
  BASE_URL="https://admin.core.dealoware.com"
  LISTEN="https://127.0.0.1:443"
else
  BASE_URL="https://admin.core.dealoware.com:${PORT}"
  LISTEN="https://127.0.0.1:${PORT}"
fi

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="$LISTEN"
export ASPNETCORE_Kestrel__Certificates__Default__Path="$WORKDIR/cert.pfx"
export ASPNETCORE_Kestrel__Certificates__Default__Password="$PFX_PASS"
export ConnectionStrings__DefaultConnection="Data Source=${DB}"
export CoreOwner__Email="$OWNER"
export DEALOWARE_ADMIN_IP_HMAC_KEY="$HMAC_KEY"
export DEALOWARE_ADMIN_TOTP_KEY="$TOTP_KEY"
export DEALOWARE_ADMIN_RECOVERY_HMAC_KEY="$RECOVERY_KEY"
unset DB_HOST DB_PORT DB_NAME DB_USERNAME DB_PASSWORD || true

dotnet run --project "$ROOT/src/Dealoware.Api/Dealoware.Api.csproj" --no-launch-profile \
  >"$WORKDIR/api.log" 2>&1 &
API_PID=$!

ready=0
for _ in $(seq 1 90); do
  if curl -skf "https://127.0.0.1:${PORT}/health" >/dev/null 2>&1; then
    ready=1
    break
  fi
  if ! kill -0 "$API_PID" 2>/dev/null; then
    echo "API exited before health check" >&2
    cat "$WORKDIR/api.log" >&2 || true
    exit 1
  fi
  sleep 0.5
done
if [[ "$ready" -ne 1 ]]; then
  echo "API did not become healthy" >&2
  cat "$WORKDIR/api.log" >&2 || true
  exit 1
fi

python3 - "$DB" "$OWNER" "$TOKEN_HASH" "$NOW" "$EXPIRES" "$DUMMY_HASH" "$ACCOUNT_ID" <<'PY'
import sqlite3, sys
db, owner, token_hash, now, expires, dummy_hash, account_id = sys.argv[1:]
con = sqlite3.connect(db)
cur = con.cursor()
cur.execute(
    "INSERT INTO AdminCredentials (Email, PasswordHash, PasswordUpdatedAt) VALUES (?, ?, ?)",
    (owner, dummy_hash, now),
)
cur.execute(
    """INSERT INTO AdminCoreOwnerAccounts (
         Id, Email, PasswordHash, TotpSecretCipher, TotpEnrolledAt,
         RecoveryCodesIssued, FailedFactorAttempts, CreatedAt
       ) VALUES (?, ?, ?, ?, ?, 1, 0, ?)""",
    (account_id, owner, dummy_hash, "e2e-not-a-secret", now, now),
)
cur.execute(
    """INSERT INTO AdminPasswordResetTokens (
         Id, Email, TokenHash, CreatedAt, ExpiresAt, ConsumedAt, FailedCodeAttempts
       ) VALUES (?, ?, ?, ?, ?, NULL, 0)""",
    (account_id, owner, token_hash, now, expires),
)
con.commit()
con.close()
PY

cd "$E2E"
if [[ ! -d node_modules/@playwright/test ]]; then
  npm ci
fi
npx playwright install chromium >/dev/null

export RESET_CONFIRM_BASE_URL="$BASE_URL"
export RESET_CONFIRM_TOKEN="$TOKEN"
if [[ -d /opt/cursor/artifacts ]]; then
  export RESET_CONFIRM_ARTIFACTS=/opt/cursor/artifacts
fi
npx playwright test -c reset-confirm/playwright.config.ts
