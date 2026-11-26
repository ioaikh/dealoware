"""Puts the reset rows of the reset-page browser tests into a known state.

Called by e2e/reset/reset.spec.ts before every test, after the owner has been
seeded (e2e/sign-in/owner.ts), with one JSON object on standard input:

    db           the run's SQLite file
    email        the configured owner
    tokenHashes  hashed reset-link tokens to store as live links (may be empty)

It clears the reset links, the address's reset budget and the stored
password, then stores the owner's password as a reset expects to find it (the
hash the owner row already holds) and the links, each good for one hour. So
every test starts from the same place whatever ran before it. The token
values themselves are made up by the caller; this script only writes rows.

Because it empties tables, it works on one kind of database only: the file
e2e.db of a browser-test run. The check is the sign-in suite's own
(e2e/sign-in/seed_owner.py), and it stops before anything is opened.
"""
import json
import sqlite3
import sys
import uuid
from datetime import datetime, timedelta, timezone
from pathlib import Path

# Borrowing the check must not leave a bytecode folder behind in the other suite.
sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "sign-in"))
from seed_owner import a_runs_own_database  # noqa: E402

LEFT_BY_A_RESET = ("AdminPasswordResetTokens", "AdminResetIpCounters", "AdminCredentials")


def stamp(moment: datetime) -> str:
    return moment.strftime("%Y-%m-%dT%H:%M:%S.%f") + "0Z"


def main() -> None:
    state = json.load(sys.stdin)
    database = a_runs_own_database(state.get("db"))
    now = datetime.now(timezone.utc)

    con = sqlite3.connect(database, timeout=15)
    try:
        with con:
            for table in LEFT_BY_A_RESET:
                con.execute(f"DELETE FROM {table}")
            owner = con.execute(
                "SELECT PasswordHash FROM AdminCoreOwnerAccounts WHERE Email = ?", (state["email"],)
            ).fetchone()
            if owner is None:
                sys.exit("seed_reset.py needs the owner row: seed the owner first (e2e/sign-in/owner.ts).")
            con.execute(
                "INSERT INTO AdminCredentials (Email, PasswordHash, PasswordUpdatedAt) VALUES (?, ?, ?)",
                (state["email"], owner[0], stamp(now)),
            )
            for token_hash in state["tokenHashes"]:
                con.execute(
                    """INSERT INTO AdminPasswordResetTokens (
                         Id, Email, TokenHash, CreatedAt, ExpiresAt, ConsumedAt, FailedCodeAttempts
                       ) VALUES (?, ?, ?, ?, ?, NULL, 0)""",
                    (str(uuid.uuid4()).upper(), state["email"], token_hash, stamp(now), stamp(now + timedelta(hours=1))),
                )
    finally:
        con.close()


if __name__ == "__main__":
    main()
