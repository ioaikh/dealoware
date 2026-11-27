#!/usr/bin/env bash
# new-database-rehearsal.sh — H3 on a new, empty PostgreSQL database, in the order a fresh
# deployment runs it (T58 specification §3.4):
#
#   1. a new, empty database, owned by a master login WITHOUT superuser (as on RDS);
#   2. dbroles, then dbroles again (it must be idempotent);
#   3. migrate, as dealoware_migrate;
#   4. dbroles --verify, as dealoware_app;
#   5. the API starts on dealoware_app: its privilege check passes and /health answers 200;
#   6. the API refuses to start on the master login.
#
# Also: dbroles is refused in Development, and no output of dbroles or migrate holds a
# password, the host or a connection string.
#
# The commands run as in production: ASPNETCORE_ENVIRONMENT=Production, TLS VerifyFull
# against the server certificate's CA. Every password and key is a throwaway value made
# here with openssl and never printed. Used by the CI job "New database rehearsal".
#
# Needs: psql, openssl, curl, dotnet, a built Dealoware.Api (Release), and a PostgreSQL 16
# server with TLS whose certificate is valid for REHEARSAL_PG_HOST.
#
# Settings (environment):
#   REHEARSAL_PG_SUPERPASSWORD  required: the server's superuser password (throwaway)
#   REHEARSAL_CA                required: the CA file that signed the server's certificate
#   REHEARSAL_PG_HOST           default localhost
#   REHEARSAL_PG_PORT           default 5432
#   REHEARSAL_PG_SUPERUSER      default postgres
#   REHEARSAL_API_DIR           default src/Dealoware.Api/bin/Release/net10.0
#   REHEARSAL_API_PORT          default 18080
#
# WARNING: drops and re-creates the database "dealoware" and the logins dealoware,
# dealoware_app and dealoware_migrate on that server. Point it at a throwaway server only.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

PG_HOST="${REHEARSAL_PG_HOST:-localhost}"
PG_PORT="${REHEARSAL_PG_PORT:-5432}"
PG_SUPERUSER="${REHEARSAL_PG_SUPERUSER:-postgres}"
PG_SUPERPASSWORD="${REHEARSAL_PG_SUPERPASSWORD:?set REHEARSAL_PG_SUPERPASSWORD}"
CA="${REHEARSAL_CA:?set REHEARSAL_CA}"
API_DIR="${REHEARSAL_API_DIR:-$REPO_ROOT/src/Dealoware.Api/bin/Release/net10.0}"
API_PORT="${REHEARSAL_API_PORT:-18080}"
DB_NAME="dealoware"
MASTER="dealoware"

[ -f "$API_DIR/Dealoware.Api.dll" ] || { echo "FAIL: no Dealoware.Api.dll in REHEARSAL_API_DIR"; exit 1; }
[ -f "$CA" ] || { echo "FAIL: REHEARSAL_CA is not a file"; exit 1; }

WORK="$(mktemp -d)"
API_PID=""
cleanup() {
  if [ -n "$API_PID" ]; then kill "$API_PID" 2>/dev/null || true; wait "$API_PID" 2>/dev/null || true; fi
  rm -rf "$WORK"
}
trap cleanup EXIT

fail() { echo "FAIL: $*"; exit 1; }
step() { echo; echo "== $*"; }

# Throwaway values: hex, so printable ASCII. Never echoed.
MASTER_PW="$(openssl rand -hex 24)"
APP_PW="$(openssl rand -hex 24)"
MIGRATE_PW="$(openssl rand -hex 24)"

super_psql() {
  # Options first, the connection last (some psql builds stop reading options at the first argument).
  PGPASSWORD="$PG_SUPERPASSWORD" psql -X -q -v ON_ERROR_STOP=1 "$@" \
    "host=$PG_HOST port=$PG_PORT user=$PG_SUPERUSER dbname=postgres sslmode=verify-full sslrootcert=$CA"
}

# Runs the API binary as a one-shot command, with the settings the stack gives it.
# $1 = log file, $2 = DB_USERNAME, $3 = DB_PASSWORD, rest = arguments.
run_api_command() {
  local log="$1" user="$2" password="$3"; shift 3
  (
    cd "$API_DIR"
    env ASPNETCORE_ENVIRONMENT=Production \
        DB_HOST="$PG_HOST" DB_PORT="$PG_PORT" DB_NAME="$DB_NAME" \
        DB_USERNAME="$user" DB_PASSWORD="$password" \
        DEALOWARE_RDS_CERT_PATH="$CA" \
        DEALOWARE_DB_APP_PASSWORD="$APP_PW" DEALOWARE_DB_MIGRATE_PASSWORD="$MIGRATE_PW" \
        dotnet Dealoware.Api.dll "$@"
  ) >"$log" 2>&1
}

# Starts the long-lived API in the background with the Express service's settings.
start_api() {
  local log="$1" user="$2" password="$3"
  (
    cd "$API_DIR"
    exec env ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS="http://127.0.0.1:$API_PORT" \
        DB_HOST="$PG_HOST" DB_PORT="$PG_PORT" DB_NAME="$DB_NAME" \
        DB_USERNAME="$user" DB_PASSWORD="$password" \
        DEALOWARE_RDS_CERT_PATH="$CA" \
        DEALOWARE_JWT_SIGNING_KEY="$(openssl rand -hex 32)" \
        DEALOWARE_ADMIN_IP_HMAC_KEY="$(openssl rand -base64 32)" \
        DEALOWARE_ADMIN_RECOVERY_HMAC_KEY="$(openssl rand -base64 32)" \
        DEALOWARE_ADMIN_TOTP_KEY="$(openssl rand -base64 32)" \
        DEALOWARE_TURNSTILE_SECRET="$(openssl rand -hex 32)" \
        Admin__ForwardedHeaders__KnownNetworks="10.0.0.0/24" \
        dotnet Dealoware.Api.dll
  ) >"$log" 2>&1 &
  API_PID=$!
}

stop_api() {
  if [ -n "$API_PID" ]; then kill "$API_PID" 2>/dev/null || true; wait "$API_PID" 2>/dev/null || true; API_PID=""; fi
}

step "1. A new, empty database, owned by a master login without superuser (as on RDS)"
super_psql -c "DROP DATABASE IF EXISTS $DB_NAME WITH (FORCE)" \
           -c "DROP ROLE IF EXISTS dealoware_app" \
           -c "DROP ROLE IF EXISTS dealoware_migrate" \
           -c "DROP ROLE IF EXISTS $MASTER"
# The master password goes through psql's variable quoting, not into the command text.
super_psql -v pw="$MASTER_PW" <<SQL
CREATE ROLE $MASTER LOGIN CREATEROLE CREATEDB PASSWORD :'pw';
CREATE DATABASE $DB_NAME OWNER $MASTER;
SQL
echo "ok: database $DB_NAME, owner $MASTER (CREATEROLE CREATEDB, not superuser)"

step "dbroles is refused in Development"
set +e
( cd "$API_DIR" && env ASPNETCORE_ENVIRONMENT=Development dotnet Dealoware.Api.dll dbroles ) >"$WORK/dev.log" 2>&1
code=$?
set -e
cat "$WORK/dev.log"
[ "$code" -eq 2 ] || fail "dbroles in Development exited $code, expected 2"
grep -q "refused" "$WORK/dev.log" || fail "dbroles in Development did not say refused"

step "2. dbroles, as the master"
run_api_command "$WORK/dbroles-1.log" "$MASTER" "$MASTER_PW" dbroles || { cat "$WORK/dbroles-1.log"; fail "dbroles exited non-zero"; }
cat "$WORK/dbroles-1.log"

step "2. dbroles again: idempotent"
run_api_command "$WORK/dbroles-2.log" "$MASTER" "$MASTER_PW" dbroles || { cat "$WORK/dbroles-2.log"; fail "the second dbroles exited non-zero"; }
cat "$WORK/dbroles-2.log"

step "3. migrate, as dealoware_migrate"
run_api_command "$WORK/migrate.log" dealoware_migrate "$MIGRATE_PW" migrate || { cat "$WORK/migrate.log"; fail "migrate exited non-zero"; }
cat "$WORK/migrate.log"
grep -q "Database migrations applied." "$WORK/migrate.log" || fail "migrate did not apply the migrations"
grep -q "^migrate: every step ok$" "$WORK/migrate.log" || fail "migrate did not apply the H3 rights"

step "4. dbroles --verify, as dealoware_app"
run_api_command "$WORK/verify.log" "$MASTER" "$MASTER_PW" dbroles --verify || { cat "$WORK/verify.log"; fail "dbroles --verify exited non-zero"; }
cat "$WORK/verify.log"

step "No password, host or connection string in the output of dbroles and migrate"
# Case-insensitive by lower-casing both sides (grep -i -F aborts in some Git Bash builds).
# Any grep error counts as a failure: a scan that cannot run must not pass.
for f in dbroles-1 dbroles-2 migrate verify dev; do
  tr '[:upper:]' '[:lower:]' <"$WORK/$f.log" >"$WORK/$f.lower"
  for value in "$MASTER_PW" "$APP_PW" "$MIGRATE_PW" "$PG_HOST" "Host=" "Password=" "Username=" "SSL Mode"; do
    needle="$(printf '%s' "$value" | tr '[:upper:]' '[:lower:]')"
    set +e
    grep -qF -- "$needle" "$WORK/$f.lower"
    code=$?
    set -e
    [ "$code" -eq 1 ] || fail "$f output holds a password, the host or a connection-string key (grep exit $code)"
  done
done
echo "ok: none found in 5 outputs"

step "5. The API starts on dealoware_app; its privilege check passes; /health answers 200"
start_api "$WORK/api-app.log" dealoware_app "$APP_PW"
status=""
for _ in $(seq 1 60); do
  if ! kill -0 "$API_PID" 2>/dev/null; then cat "$WORK/api-app.log"; fail "the API stopped on the dealoware_app login"; fi
  status="$(curl -s -o "$WORK/health.json" -w '%{http_code}' "http://127.0.0.1:$API_PORT/health" || true)"
  [ "$status" = "200" ] && break
  sleep 1
done
[ "$status" = "200" ] || { cat "$WORK/api-app.log"; fail "/health did not answer 200 (last: $status)"; }
grep -q '"status":"ok"' "$WORK/health.json" || fail "/health body is not {\"status\":\"ok\"}"
if grep -q "Refusing to start" "$WORK/api-app.log"; then fail "the API logged a refusal on dealoware_app"; fi
echo "ok: /health 200 $(cat "$WORK/health.json")"
stop_api

step "6. The API refuses to start on the master login"
start_api "$WORK/api-master.log" "$MASTER" "$MASTER_PW"
for _ in $(seq 1 60); do
  kill -0 "$API_PID" 2>/dev/null || break
  sleep 1
done
if kill -0 "$API_PID" 2>/dev/null; then stop_api; fail "the API kept running on the master login"; fi
set +e
wait "$API_PID"
code=$?
set -e
API_PID=""
[ "$code" -ne 0 ] || fail "the API exited 0 on the master login"
grep -q "Refusing to start" "$WORK/api-master.log" || { cat "$WORK/api-master.log"; fail "no refusal message on the master login"; }
echo "ok: exited $code with the H3 refusal"

echo
echo "RESULT: PASS - new database rehearsal"
