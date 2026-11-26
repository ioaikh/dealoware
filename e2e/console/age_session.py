"""Moves the admin sessions of a browser-test run back in time.

Called by e2e/console/session-timer.spec.ts, with one JSON object on
standard input:

    db             the run's SQLite file
    lastActiveAgo  seconds: the last activity is set to that long ago (optional)
    createdAgo     seconds: the sign-in is set to that long ago, and the
                   8-hour end to 8 hours after it (optional)

The page's clock is moved with Playwright's clock; the server's clock is the
real one. So a test that lets 28 minutes pass in the page tells the server
the same thing by moving the session row back by as much. Nothing else is
changed: no row is added or removed.

It works on one kind of database only, as seed_owner.py does: the file e2e.db
of a browser-test run, in the folder the runner made and marked for it.
"""
import json
import sqlite3
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

# The run-folder check is seed_owner.py's own. Imported without leaving a
# __pycache__ folder in e2e/sign-in.
sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "sign-in"))
from seed_owner import a_runs_own_database  # noqa: E402

ABSOLUTE_LIFETIME = timedelta(hours=8)


def stored(value: datetime) -> str:
    """As the app stores a time in SQLite: sortable UTC, seven decimals."""
    return value.strftime("%Y-%m-%dT%H:%M:%S.%f") + "0Z"


def seconds(state: dict, key: str):
    value = state.get(key)
    if value is None:
        return None
    if not isinstance(value, (int, float)) or value < 0:
        sys.exit(f"{key} must be a number of seconds, 0 or more")
    return timedelta(seconds=value)


def main() -> None:
    state = json.load(sys.stdin)
    database = a_runs_own_database(state.get("db"))
    last_active_ago = seconds(state, "lastActiveAgo")
    created_ago = seconds(state, "createdAgo")
    now = datetime.now(timezone.utc)

    con = sqlite3.connect(database, timeout=15)
    try:
        with con:
            if last_active_ago is not None:
                con.execute("UPDATE AdminSessions SET LastActivityAt = ?", (stored(now - last_active_ago),))
            if created_ago is not None:
                created = now - created_ago
                con.execute(
                    "UPDATE AdminSessions SET CreatedAt = ?, AbsoluteExpiresAt = ?",
                    (stored(created), stored(created + ABSOLUTE_LIFETIME)),
                )
    finally:
        con.close()


if __name__ == "__main__":
    main()
