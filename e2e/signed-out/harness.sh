#!/usr/bin/env bash
# Shared runner for the browser tests of the signed-out admin pages. It is
# sourced by a suite's run.sh (e2e/bootstrap/run.sh, e2e/sign-in/run.sh,
# e2e/enrol/run.sh) and does nothing by itself. Admin UI CI runs every suite
# that sources this file, through e2e/run-signed-out.sh.
#
# The suite's run.sh sets these, then sources this file:
#
#   SUITE        the suite's folder under e2e/                  (bootstrap)
#   SUITE_ENV    prefix of the variables its spec reads         (BOOTSTRAP_E2E)
#   API_PORT     loopback port the app listens on
#   PROXY_PORT   loopback port of the CONNECT proxy
#   VERIFY_PORT  loopback port of the Turnstile verifier stand-in
#
# and then calls, in this order:
#
#   start_app_behind_proxy   certificate, keys, verifier stand-in, the app, the proxy
#   run_playwright "$@"      installs the suite's packages if needed and runs its
#                            spec, once on each browser engine
#
# A suite whose tests use up what they are given (a setup link, a throttle
# budget) defines a function seed_suite before run_playwright. It is called
# before each engine, and must put the rows back as a first run needs them,
# written to "$DB" with "$PY".
#
# Engines: Chromium, Firefox and WebKit, one after the other, each a separate
# Playwright run. To run fewer, name them:
#
#   E2E_ENGINES="firefox" ./e2e/<suite>/run.sh
#   E2E_ENGINES="chromium webkit" ./e2e/<suite>/run.sh
#
# Screenshots and axe reports land in the suite's test-results folder: those
# of Chromium directly in it, those of the other two in a folder each.
#
# With E2E_REPORT_DIR set to a folder, each engine's run also leaves
# Playwright's own report there, as <engine>.json. e2e/run-signed-out.sh reads
# them to tell a run that executed tests from one that skipped them all.
#
# Needs bash, openssl, curl, Python 3, the .NET SDK and Node. Works on Linux,
# macOS and Git Bash on Windows.
#
# Origin is the production constant https://admin.core.dealoware.com (no port).
# Kestrel listens on 127.0.0.1:$API_PORT. A local CONNECT proxy on
# 127.0.0.1:$PROXY_PORT forwards every browser connection to that listener, so
# Playwright keeps the Origin exact without privileged ports, sysctl, or
# host-file edits. Nothing the browser asks for leaves this machine: the specs
# answer the requests for Cloudflare's widget script themselves.
#
# Turnstile: the app runs as Development with Admin:Turnstile:VerifyUrl
# pointed at a stand-in verifier on 127.0.0.1:$VERIFY_PORT, which accepts one
# run-time token and rejects everything else. Outside Development the app
# refuses to start with any verify URL but Cloudflare's
# (TurnstileOptionsValidator), so this set-up cannot exist in a deployment.

: "${SUITE:?the suite run.sh must set SUITE}"
: "${SUITE_ENV:?the suite run.sh must set SUITE_ENV}"
: "${API_PORT:?the suite run.sh must set API_PORT}"
: "${PROXY_PORT:?the suite run.sh must set PROXY_PORT}"
: "${VERIFY_PORT:?the suite run.sh must set VERIFY_PORT}"

# Python 3 is "python3" on Linux and macOS. On Windows that name is often a
# Store shortcut that runs nothing, and the interpreter is "python".
PY=""
for candidate in python3 python; do
  if "$candidate" -c 'import sys; sys.exit(0 if sys.version_info[0] == 3 else 1)' >/dev/null 2>&1; then
    PY="$candidate"
    break
  fi
done
if [[ -z "$PY" ]]; then
  echo "Python 3 is required (tried python3 and python)" >&2
  exit 1
fi

# Git Bash: give dotnet, openssl and Python a path they understand, and stop
# the shell from rewriting "/CN=..." and URL-like arguments into file paths.
native_path() {
  if command -v cygpath >/dev/null 2>&1; then
    cygpath -m "$1"
  else
    printf '%s' "$1"
  fi
}
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

ROOT="$(native_path "$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)")"
E2E="$ROOT/e2e"
SUITE_DIR="$E2E/$SUITE"
WORKDIR="$(native_path "$(mktemp -d "${TMPDIR:-/tmp}/dw-${SUITE}.XXXXXX")")"
DB="$WORKDIR/e2e.db"
OWNER="alex.doe@example.test"

# Marks this folder as a browser-test run's own. A script that empties tables
# (e2e/sign-in/seed_owner.py) refuses any database that does not sit next to
# this file with these words in it.
RUN_MARKER=".dealoware-e2e-run"
printf 'dealoware e2e run\n' >"$WORKDIR/$RUN_MARKER"

ENGINES="${E2E_ENGINES:-chromium firefox webkit}"
# A value of only spaces would run no engine and still pass: refuse it.
read -ra named_engines <<<"$ENGINES"
if [[ ${#named_engines[@]} -eq 0 ]]; then
  echo "E2E_ENGINES names no engine" >&2
  rm -rf "$WORKDIR"
  exit 2
fi
for engine in $ENGINES; do
  case "$engine" in
    chromium|firefox|webkit) ;;
    *)
      echo "E2E_ENGINES may name chromium, firefox and webkit, not \"$engine\"" >&2
      rm -rf "$WORKDIR"
      exit 2
      ;;
  esac
done

# Stops a background process. "dotnet run" starts the app as a child process,
# and ending "dotnet run" alone can leave the app running. So on Windows the
# whole tree is ended by Windows process id, and elsewhere the children are
# told to stop before the process itself.
stop_process() {
  local pid="$1"
  if [[ -z "$pid" ]] || ! kill -0 "$pid" 2>/dev/null; then
    return 0
  fi
  if [[ -r "/proc/$pid/winpid" ]] && command -v taskkill >/dev/null 2>&1; then
    taskkill /PID "$(cat "/proc/$pid/winpid")" /T /F >/dev/null 2>&1 || true
  else
    if command -v pkill >/dev/null 2>&1; then
      pkill -TERM -P "$pid" 2>/dev/null || true
    fi
    kill "$pid" 2>/dev/null || true
  fi
  wait "$pid" 2>/dev/null || true
}

cleanup() {
  stop_process "${PROXY_PID:-}"
  stop_process "${VERIFY_PID:-}"
  stop_process "${API_PID:-}"
  rm -rf "$WORKDIR" 2>/dev/null || true
}
trap cleanup EXIT

new_token() {
  openssl rand -hex 32
}

sha256_upper() {
  printf '%s' "$1" | openssl dgst -sha256 -hex | awk '{print toupper($NF)}'
}

# True when something listens on the loopback port.
port_answers() {
  "$PY" - "$1" <<'PY'
import socket, sys
s = socket.socket()
try:
    s.settimeout(0.2)
    s.connect(("127.0.0.1", int(sys.argv[1])))
    sys.exit(0)
except OSError:
    sys.exit(1)
finally:
    s.close()
PY
}

wait_for_port() {
  local port="$1" pid="$2" name="$3"
  for _ in $(seq 1 60); do
    # Ours only if our process is alive too: a port that answers while our
    # process has died belongs to someone else.
    if port_answers "$port" && kill -0 "$pid" 2>/dev/null; then
      return 0
    fi
    if ! kill -0 "$pid" 2>/dev/null; then
      echo "$name exited before listen" >&2
      exit 1
    fi
    sleep 0.1
  done
  echo "$name did not listen on 127.0.0.1:${port}" >&2
  exit 1
}

# Starts the Turnstile verifier stand-in, the app over HTTPS and the CONNECT
# proxy, and returns once all three answer. The app has created its schema by
# then, so the suite can write its rows to "$DB".
start_app_behind_proxy() {
  # A port that already answers belongs to another run of this suite, or to
  # an app one left behind. That app would answer this run's browser, out of
  # its own database, and the tests would fail for no reason of theirs.
  local port
  for port in "$API_PORT" "$PROXY_PORT" "$VERIFY_PORT"; do
    if port_answers "$port"; then
      echo "127.0.0.1:${port} is already in use: is another run of ${SUITE} going?" >&2
      exit 1
    fi
  done

  umask 077
  local pfx_pass
  pfx_pass="$(openssl rand -hex 24)"
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
    -passout "pass:${pfx_pass}" \
    >/dev/null 2>&1
  rm -f "$WORKDIR/key.pem" "$WORKDIR/cert.pem"

  # Made up for this run and gone with it. A suite that seeds rows the app
  # must be able to read (an encrypted authenticator key, hashed recovery
  # codes) uses TOTP_KEY and RECOVERY_KEY.
  HMAC_KEY="$(openssl rand -base64 32)"
  TOTP_KEY="$(openssl rand -base64 32)"
  RECOVERY_KEY="$(openssl rand -base64 32)"
  TURNSTILE_SECRET="$(openssl rand -hex 24)"
  TURNSTILE_PASS_TOKEN="$(openssl rand -hex 24)"

  export ASPNETCORE_ENVIRONMENT=Development
  export ASPNETCORE_URLS="https://127.0.0.1:${API_PORT}"
  export ASPNETCORE_Kestrel__Certificates__Default__Path="$WORKDIR/cert.pfx"
  export ASPNETCORE_Kestrel__Certificates__Default__Password="$pfx_pass"
  export ConnectionStrings__DefaultConnection="Data Source=${DB}"
  export CoreOwner__Email="$OWNER"
  export DEALOWARE_ADMIN_IP_HMAC_KEY="$HMAC_KEY"
  export DEALOWARE_ADMIN_TOTP_KEY="$TOTP_KEY"
  export DEALOWARE_ADMIN_RECOVERY_HMAC_KEY="$RECOVERY_KEY"
  export DEALOWARE_TURNSTILE_SECRET="$TURNSTILE_SECRET"
  export Admin__Turnstile__SiteKey="e2e-site-key"
  export Admin__Turnstile__VerifyUrl="http://127.0.0.1:${VERIFY_PORT}/siteverify"
  unset DB_HOST DB_PORT DB_NAME DB_USERNAME DB_PASSWORD || true

  # Stand-in for Cloudflare siteverify. Loopback only. Success needs the
  # run-time secret and the run-time pass token; anything else is a failure.
  "$PY" - "$VERIFY_PORT" "$TURNSTILE_SECRET" "$TURNSTILE_PASS_TOKEN" <<'PY' &
import json, sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs

port, secret, pass_token = int(sys.argv[1]), sys.argv[2], sys.argv[3]

class Handler(BaseHTTPRequestHandler):
    def do_POST(self):
        length = int(self.headers.get("Content-Length") or 0)
        fields = parse_qs(self.rfile.read(length).decode("utf-8"))
        ok = (
            self.path == "/siteverify"
            and fields.get("secret") == [secret]
            and fields.get("response") == [pass_token]
        )
        body = {"success": True, "hostname": "admin.core.dealoware.com"} if ok else {
            "success": False,
            "error-codes": ["invalid-input-response"],
        }
        payload = json.dumps(body).encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def log_message(self, *args):
        pass

ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
PY
  VERIFY_PID=$!

  dotnet run --project "$ROOT/src/Dealoware.Api/Dealoware.Api.csproj" --no-launch-profile \
    >"$WORKDIR/api.log" 2>&1 &
  API_PID=$!

  local ready=0
  for _ in $(seq 1 240); do
    if curl -skf "https://127.0.0.1:${API_PORT}/health" >/dev/null 2>&1; then
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

  "$PY" - "$PROXY_PORT" "$API_PORT" <<'PY' &
import select, socket, sys, threading

listen_port = int(sys.argv[1])
target_port = int(sys.argv[2])
listen = ("127.0.0.1", listen_port)
target = ("127.0.0.1", target_port)

def handle(conn: socket.socket) -> None:
    try:
        data = b""
        while b"\r\n\r\n" not in data:
            chunk = conn.recv(4096)
            if not chunk:
                return
            data += chunk
        first = data.split(b"\r\n", 1)[0].decode("latin1", "replace")
        if not first.upper().startswith("CONNECT "):
            conn.sendall(b"HTTP/1.1 405 Method Not Allowed\r\nConnection: close\r\n\r\n")
            return
        dest = socket.create_connection(target)
        conn.sendall(b"HTTP/1.1 200 Connection Established\r\n\r\n")
        sockets = [conn, dest]
        while True:
            readable, _, _ = select.select(sockets, [], [])
            for sock in readable:
                other = dest if sock is conn else conn
                buf = sock.recv(65536)
                if not buf:
                    dest.close()
                    return
                other.sendall(buf)
    except OSError:
        return
    finally:
        conn.close()

server = socket.socket()
server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
server.bind(listen)
server.listen(50)
while True:
    client, _ = server.accept()
    threading.Thread(target=handle, args=(client,), daemon=True).start()
PY
  PROXY_PID=$!

  wait_for_port "$PROXY_PORT" "$PROXY_PID" "proxy"
  wait_for_port "$VERIFY_PORT" "$VERIFY_PID" "verifier stand-in"
}

# Runs the suite's spec on each engine in turn, against the one running app.
# The suite folder has its own package.json (Playwright at the version
# e2e/package.json pins, plus axe); Admin UI CI installs the browsers from
# that one pin. Anything the suite exported under its own prefix before this
# call reaches the spec as well. Fails if any engine's run failed, after all
# of them have run.
run_playwright() {
  local arg
  for arg in "$@"; do
    if [[ "$arg" == --project* ]]; then
      echo "Pick engines with E2E_ENGINES (for example E2E_ENGINES=firefox), not with --project" >&2
      exit 2
    fi
  done

  cd "$SUITE_DIR"
  if [[ ! -d node_modules/@playwright/test || ! -d node_modules/@axe-core/playwright ]]; then
    npm ci
  fi
  # shellcheck disable=SC2086
  npx playwright install $ENGINES >/dev/null

  export "${SUITE_ENV}_BASE_URL=https://admin.core.dealoware.com"
  export "${SUITE_ENV}_PROXY=http://127.0.0.1:${PROXY_PORT}"
  export "${SUITE_ENV}_TURNSTILE_TOKEN=${TURNSTILE_PASS_TOKEN}"

  # What an earlier run saved must not be mistaken for this run's.
  rm -rf "$SUITE_DIR/test-results"

  # Asked for a report of each engine's run: the list on the screen as
  # always, and Playwright's JSON report in a file.
  if [[ -n "${E2E_REPORT_DIR:-}" ]]; then
    mkdir -p "$E2E_REPORT_DIR"
    set -- --reporter=list,json "$@"
  fi

  local engine artifacts failed=""
  for engine in $ENGINES; do
    if declare -F seed_suite >/dev/null; then
      seed_suite
    fi
    artifacts="$SUITE_DIR/test-results"
    if [[ "$engine" != chromium ]]; then
      artifacts="$artifacts/$engine"
    fi
    mkdir -p "$artifacts"
    export "${SUITE_ENV}_ARTIFACTS=${artifacts}"
    if [[ -n "${E2E_REPORT_DIR:-}" ]]; then
      export PLAYWRIGHT_JSON_OUTPUT_FILE="${E2E_REPORT_DIR}/${engine}.json"
    fi
    echo "== ${SUITE} on ${engine} =="
    if ! npx playwright test -c playwright.config.cjs --project="$engine" "$@"; then
      failed="$failed $engine"
    fi
  done

  if [[ -n "$failed" ]]; then
    echo "${SUITE}: failed on${failed}" >&2
    return 1
  fi
  echo "${SUITE}: passed on ${ENGINES}"
}
