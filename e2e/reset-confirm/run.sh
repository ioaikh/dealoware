#!/usr/bin/env bash
# Confirm-page Playwright over HTTPS (Chief UI/UX N5 / P1 / P2 / P5).
# Not wired into Admin UI CI (deny-live would reject the admin host; that
# workflow's path filter is e2e/admin/**).
#
#   ./e2e/reset-confirm/run.sh
#
# Origin is the production constant https://admin.core.dealoware.com (no port).
# A high-port listen would send Origin :8443 and fail CSRF; no Testing/e2e
# config can change that without production-code edits. Listen on 443.
# If 443 is privileged, lower ip_unprivileged_port_start only after saving
# the old value, and restore it on every EXIT. Throwaway certs stay in mktemp.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
E2E="$ROOT/e2e"
WORKDIR="$(mktemp -d "${TMPDIR:-/tmp}/dw-reset-confirm.XXXXXX")"
UNPRIV_SYSCTL_KEY=net.ipv4.ip_unprivileged_port_start
UNPRIV_OLD=""
UNPRIV_CHANGED=0

restore_unprivileged_port_start() {
  if [[ "${UNPRIV_CHANGED}" -eq 1 ]]; then
    if ! sudo -n sysctl -w "${UNPRIV_SYSCTL_KEY}=${UNPRIV_OLD}" >/dev/null; then
      echo "e2e/reset-confirm: failed to restore ${UNPRIV_SYSCTL_KEY}=${UNPRIV_OLD}" >&2
    fi
    UNPRIV_CHANGED=0
  fi
}

cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
  fi
  rm -rf "$WORKDIR"
  restore_unprivileged_port_start
}
trap cleanup EXIT

can_bind_443() {
  python3 - <<'PY'
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
}

if ! can_bind_443; then
  UNPRIV_OLD="$(cat /proc/sys/net/ipv4/ip_unprivileged_port_start)"
  if [[ -z "${UNPRIV_OLD}" ]]; then
    echo "e2e/reset-confirm: could not read ${UNPRIV_SYSCTL_KEY}" >&2
    exit 1
  fi
  if ! sudo -n sysctl -w "${UNPRIV_SYSCTL_KEY}=0" >/dev/null; then
    echo "e2e/reset-confirm requires 127.0.0.1:443 so Origin is exactly https://admin.core.dealoware.com. Could not temporarily set ${UNPRIV_SYSCTL_KEY}=0. No high-port fallback." >&2
    exit 1
  fi
  UNPRIV_CHANGED=1
fi
if ! can_bind_443; then
  echo "e2e/reset-confirm requires 127.0.0.1:443 so Origin is exactly https://admin.core.dealoware.com. Port 443 is unavailable. No high-port fallback." >&2
  exit 1
fi

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
CURRENT_PASSWORD="radio station prior!"
TOKEN="$(openssl rand -base64 32 | tr '+/' '-_' | tr -d '=')"
SUCCESS_TOKEN="$(openssl rand -base64 32 | tr '+/' '-_' | tr -d '=')"
TOKEN_HASH="$(printf '%s' "$TOKEN" | openssl dgst -sha256 -hex | awk '{print toupper($NF)}')"
SUCCESS_HASH="$(printf '%s' "$SUCCESS_TOKEN" | openssl dgst -sha256 -hex | awk '{print toupper($NF)}')"
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
ACCOUNT_ID="$(python3 -c 'import uuid; print(str(uuid.uuid4()).upper())')"
LIVE_TOKEN_ID="$(python3 -c 'import uuid; print(str(uuid.uuid4()).upper())')"
SUCCESS_TOKEN_ID="$(python3 -c 'import uuid; print(str(uuid.uuid4()).upper())')"

SEED_OUT="$(python3 - "$TOTP_KEY" "$CURRENT_PASSWORD" <<'PY'
import base64, hashlib, os, sys
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

totp_key = base64.b64decode(sys.argv[1])
password = sys.argv[2].encode("utf-8")
salt = os.urandom(16)
dk = hashlib.pbkdf2_hmac("sha512", password, salt, 220000, dklen=32)
pw_hash = "pbkdf2-sha512$220000$%s$%s" % (
    base64.b64encode(salt).decode("ascii"),
    base64.b64encode(dk).decode("ascii"),
)
secret = os.urandom(20)
aes = AESGCM(totp_key)
nonce = os.urandom(12)
raw = aes.encrypt(nonce, secret, None)
ciphertext, tag = raw[:-16], raw[-16:]
packed = nonce + tag + ciphertext
print(pw_hash)
print(base64.b64encode(packed).decode("ascii"))
print(secret.hex())
PY
)"
PASSWORD_HASH="$(printf '%s\n' "$SEED_OUT" | sed -n '1p')"
TOTP_CIPHER="$(printf '%s\n' "$SEED_OUT" | sed -n '2p')"
TOTP_SECRET_HEX="$(printf '%s\n' "$SEED_OUT" | sed -n '3p')"

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="https://127.0.0.1:443"
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
  if curl -skf "https://127.0.0.1:443/health" >/dev/null 2>&1; then
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

python3 - "$DB" "$OWNER" "$PASSWORD_HASH" "$TOTP_CIPHER" "$NOW" "$EXPIRES" \
  "$ACCOUNT_ID" "$LIVE_TOKEN_ID" "$TOKEN_HASH" "$SUCCESS_TOKEN_ID" "$SUCCESS_HASH" <<'PY'
import sqlite3, sys
(db, owner, pw_hash, totp_cipher, now, expires,
 account_id, live_id, live_hash, success_id, success_hash) = sys.argv[1:]
con = sqlite3.connect(db)
cur = con.cursor()
cur.execute(
    "INSERT INTO AdminCredentials (Email, PasswordHash, PasswordUpdatedAt) VALUES (?, ?, ?)",
    (owner, pw_hash, now),
)
cur.execute(
    """INSERT INTO AdminCoreOwnerAccounts (
         Id, Email, PasswordHash, TotpSecretCipher, TotpEnrolledAt,
         RecoveryCodesIssued, FailedFactorAttempts, CreatedAt
       ) VALUES (?, ?, ?, ?, ?, 1, 0, ?)""",
    (account_id, owner, pw_hash, totp_cipher, now, now),
)
cur.execute(
    """INSERT INTO AdminPasswordResetTokens (
         Id, Email, TokenHash, CreatedAt, ExpiresAt, ConsumedAt, FailedCodeAttempts
       ) VALUES (?, ?, ?, ?, ?, NULL, 0)""",
    (live_id, owner, live_hash, now, expires),
)
cur.execute(
    """INSERT INTO AdminPasswordResetTokens (
         Id, Email, TokenHash, CreatedAt, ExpiresAt, ConsumedAt, FailedCodeAttempts
       ) VALUES (?, ?, ?, ?, ?, NULL, 0)""",
    (success_id, owner, success_hash, now, expires),
)
con.commit()
con.close()
PY

cd "$E2E"
if [[ ! -d node_modules/@playwright/test ]]; then
  npm ci
fi
npx playwright install chromium >/dev/null

export RESET_CONFIRM_BASE_URL="https://admin.core.dealoware.com"
export RESET_CONFIRM_TOKEN="$TOKEN"
export RESET_CONFIRM_SUCCESS_TOKEN="$SUCCESS_TOKEN"
export RESET_CONFIRM_TOTP_SECRET_HEX="$TOTP_SECRET_HEX"
if [[ -d /opt/cursor/artifacts ]]; then
  export RESET_CONFIRM_ARTIFACTS=/opt/cursor/artifacts
fi
npx playwright test -c reset-confirm/playwright.config.ts
