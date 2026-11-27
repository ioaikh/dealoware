#!/usr/bin/env bash
# Upgrade rehearsal (T09 spec §2.3), run by CI on PostgreSQL 16. It stands in for a test copy
# until there is one: the previous release must keep working on the schema of this commit.
#
#   scripts/upgrade-rehearsal/run.sh <previous release tag, or any commit to treat as one>
#
# A commit that is not a release can stand in for one (CI's manual run, "previous_ref"), so the
# rehearsal can be exercised before release-1. It must have /health/ready (T09a or later).
#
# 1. Build the previous release (the tag) and this commit.
# 2. Run the previous release's migrations.
# 3. Load a small fixed data set through the previous release's own API (records.mjs).
# 4. Run this commit's migrations.
# 5. Start the previous release again on the result.
# 6. /health/ready must answer 200, and each seeded record must read back unchanged.
# 7. The previous release writes one record of each kind on the new schema, and reads it back.
#
# The database is a throwaway PostgreSQL container with TLS, so both releases run as in
# production: Production environment, SSL Mode=VerifyFull, migrations under one login and
# the API under a least-privilege login. Every key is made up for this run.
set -euo pipefail

prev_tag="${1:?usage: run.sh <previous release tag or commit>}"
if ! git rev-parse --verify --quiet "$prev_tag^{commit}" >/dev/null; then
  echo "not a tag or commit here: $prev_tag" >&2; exit 2
fi

repo="$(git rev-parse --show-toplevel)"
here="$repo/scripts/upgrade-rehearsal"
work="${RUNNER_TEMP:-/tmp}/upgrade-rehearsal"
rm -rf "$work"; mkdir -p "$work"
pg_name="rehearsal-pg"
pg_port="${REHEARSAL_PG_PORT:-55432}"
api_port="${REHEARSAL_API_PORT:-18080}"
api_pid=""

cleanup() {
  if [ -n "$api_pid" ]; then kill "$api_pid" 2>/dev/null || true; fi
  docker rm -f "$pg_name" >/dev/null 2>&1 || true
  git -C "$repo" worktree remove --force "$work/prev-src" >/dev/null 2>&1 || true
  git -C "$repo" worktree prune >/dev/null 2>&1 || true
}
trap cleanup EXIT

say() { echo "== $*"; }
rand_b64() { openssl rand -base64 32; }
rand_hex() { openssl rand -hex "$1"; }

# ---- 1. Both builds -------------------------------------------------------------------------
say "building $prev_tag and this commit ($(git -C "$repo" rev-parse --short HEAD))"
git -C "$repo" worktree add --detach "$work/prev-src" "$prev_tag" >/dev/null
dotnet publish "$work/prev-src/src/Dealoware.Api/Dealoware.Api.csproj" -c Release -o "$work/prev" --nologo -v quiet
dotnet publish "$repo/src/Dealoware.Api/Dealoware.Api.csproj" -c Release -o "$work/new" --nologo -v quiet

# ---- The database: PostgreSQL 16 with TLS -------------------------------------------------
say "starting PostgreSQL 16 with TLS"
openssl req -x509 -newkey rsa:2048 -nodes -days 1 -subj "/CN=rehearsal-ca" \
  -keyout "$work/ca.key" -out "$work/ca.crt" 2>/dev/null
openssl req -newkey rsa:2048 -nodes -subj "/CN=localhost" \
  -keyout "$work/server.key" -out "$work/server.csr" 2>/dev/null
printf 'subjectAltName=DNS:localhost\n' > "$work/san.ext"
openssl x509 -req -in "$work/server.csr" -CA "$work/ca.crt" -CAkey "$work/ca.key" -CAcreateserial \
  -days 1 -extfile "$work/san.ext" -out "$work/server.crt" 2>/dev/null

migrate_pw="$(rand_hex 16)"
app_pw="$(rand_hex 16)"
docker run --detach --name "$pg_name" --publish "127.0.0.1:$pg_port:5432" \
  --env POSTGRES_USER=dealoware_migrate --env POSTGRES_PASSWORD="$migrate_pw" --env POSTGRES_DB=dealoware \
  postgres:16 >/dev/null
for _ in $(seq 1 60); do
  docker exec "$pg_name" pg_isready -U dealoware_migrate -d dealoware >/dev/null 2>&1 && break
  sleep 1
done
docker cp "$work/server.crt" "$pg_name:/var/lib/postgresql/server.crt"
docker cp "$work/server.key" "$pg_name:/var/lib/postgresql/server.key"
docker exec -u root "$pg_name" sh -c 'chown postgres:postgres /var/lib/postgresql/server.* && chmod 600 /var/lib/postgresql/server.key'
psql_admin() { docker exec -i "$pg_name" psql -v ON_ERROR_STOP=1 -q -U dealoware_migrate -d dealoware "$@"; }
psql_admin <<SQL
ALTER SYSTEM SET ssl = on;
ALTER SYSTEM SET ssl_cert_file = '/var/lib/postgresql/server.crt';
ALTER SYSTEM SET ssl_key_file = '/var/lib/postgresql/server.key';
SELECT pg_reload_conf();
-- The API's own login: data only, as in production. Tables the migrations create later
-- are granted to it by default.
CREATE ROLE dealoware_app LOGIN PASSWORD '$app_pw';
GRANT CONNECT ON DATABASE dealoware TO dealoware_app;
GRANT USAGE ON SCHEMA public TO dealoware_app;
ALTER DEFAULT PRIVILEGES FOR ROLE dealoware_migrate IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO dealoware_app;
ALTER DEFAULT PRIVILEGES FOR ROLE dealoware_migrate IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO dealoware_app;
SQL

common_env=(
  "DB_HOST=localhost" "DB_PORT=$pg_port" "DB_NAME=dealoware"
  "DEALOWARE_RDS_CERT_PATH=$work/ca.crt"
)

migrate() { # <build dir>
  env "${common_env[@]}" DB_USERNAME=dealoware_migrate DB_PASSWORD="$migrate_pw" \
    dotnet "$1/Dealoware.Api.dll" migrate
}

jwt_key="$(rand_hex 32)"
ip_key="$(rand_b64)"; totp_key="$(rand_b64)"; recovery_key="$(rand_b64)"
start_api() { # <build dir> <log>
  env "${common_env[@]}" DB_USERNAME=dealoware_app DB_PASSWORD="$app_pw" \
    ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS="http://127.0.0.1:$api_port" \
    DEALOWARE_JWT_SIGNING_KEY="$jwt_key" DEALOWARE_ADMIN_IP_HMAC_KEY="$ip_key" \
    DEALOWARE_ADMIN_TOTP_KEY="$totp_key" DEALOWARE_ADMIN_RECOVERY_HMAC_KEY="$recovery_key" \
    Admin__ForwardedHeaders__KnownNetworks=127.0.0.1/32 \
    dotnet "$1/Dealoware.Api.dll" > "$2" 2>&1 &
  api_pid=$!
  for _ in $(seq 1 60); do
    if curl --silent --output /dev/null http://127.0.0.1:$api_port/health; then return 0; fi
    if ! kill -0 "$api_pid" 2>/dev/null; then echo "the API stopped:"; cat "$2"; return 1; fi
    sleep 1
  done
  echo "the API did not start:"; cat "$2"; return 1
}
stop_api() { kill "$api_pid"; wait "$api_pid" 2>/dev/null || true; api_pid=""; }

ready_is_200() {
  local code
  code="$(curl --silent --output "$work/ready.json" --write-out '%{http_code}' http://127.0.0.1:$api_port/health/ready)"
  cat "$work/ready.json"; echo
  [ "$code" = "200" ]
}

# ---- 2. and 3. The previous release, with its data --------------------------------------
say "$prev_tag: migrations"
migrate "$work/prev"
say "$prev_tag: the fixed data set, through its own API"
start_api "$work/prev" "$work/prev-1.log"
ready_is_200
node "$here/records.mjs" seed "http://127.0.0.1:$api_port" "$work/state.json"
node "$here/records.mjs" read "http://127.0.0.1:$api_port" "$work/state.json" "$work/before.json"
stop_api

# ---- 4. This commit's migrations -----------------------------------------------------------
say "this commit: migrations"
migrate "$work/new"

# ---- 5. and 6. The previous release on the new schema -------------------------------------
say "$prev_tag again, on the new schema"
start_api "$work/prev" "$work/prev-2.log"
if ! ready_is_200; then
  echo "FAIL: $prev_tag does not answer /health/ready 200 on this commit's schema"; cat "$work/prev-2.log"; exit 1
fi
node "$here/records.mjs" read "http://127.0.0.1:$api_port" "$work/state.json" "$work/after.json"
if ! diff -u "$work/before.json" "$work/after.json"; then
  echo "FAIL: a record read by $prev_tag changed after this commit's migrations"; exit 1
fi
say "$prev_tag writes on the new schema"
if ! node "$here/records.mjs" write "http://127.0.0.1:$api_port" "$work/state.json"; then
  echo "FAIL: $prev_tag cannot write on this commit's schema"; cat "$work/prev-2.log"; exit 1
fi
stop_api
say "PASS: $prev_tag runs on this commit's schema; every seeded record reads back unchanged, and it writes every kind"
