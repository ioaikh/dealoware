"""Puts the Core admin owner of the sign-in browser tests into a known state.

Called by e2e/sign-in/owner.ts before every test, with one JSON object on
standard input:

    db              the run's SQLite file
    email           the configured owner
    passwordHash    as the app stores it
    totpCipher      the encrypted authenticator key, or null for an owner who
                    has a password but has not enrolled an authenticator
    recoveryHashes  hashed recovery codes (empty when not enrolled)

It first clears everything a sign-in leaves behind: pending sign-ins,
sessions, failure counts and the locks they lead to. So every test starts
from the same place whatever ran before it. The values themselves are worked
out by the caller; this script only writes rows.

Because it empties tables, it works on one kind of database only: the file
e2e.db of a browser-test run, in the temporary folder the runner made and
marked for it (e2e/signed-out/harness.sh). Given any other path it stops
before it opens anything.
"""
import json
import sqlite3
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path

RUN_DATABASE = "e2e.db"
RUN_MARKER = ".dealoware-e2e-run"
RUN_MARKER_TEXT = "dealoware e2e run"

LEFT_BY_A_SIGN_IN = (
    "AdminRecoveryCodes",
    "AdminPendingAuths",
    "AdminSessions",
    "AdminAuthTokens",
    "AdminAuthFailureEvents",
    "AdminAuthLockouts",
    "AdminCoreOwnerAccounts",
)


def a_runs_own_database(path: object) -> Path:
    """The path, if it is the database of a browser-test run. Otherwise the script stops here."""
    database = Path(path) if isinstance(path, str) and path else None
    marked = False
    if database is not None and database.name == RUN_DATABASE and database.is_file():
        try:
            marked = (database.parent / RUN_MARKER).read_text(encoding="utf-8").strip() == RUN_MARKER_TEXT
        except OSError:
            marked = False
    if database is None or not marked:
        sys.exit(
            "seed_owner.py empties tables, so it only works on the database of a browser-test run "
            f"({RUN_DATABASE} in a folder the runner marked with {RUN_MARKER}). Start it with e2e/sign-in/run.sh."
        )
    return database


def main() -> None:
    state = json.load(sys.stdin)
    database = a_runs_own_database(state.get("db"))
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f") + "0Z"
    enrolled = state["totpCipher"] is not None
    account_id = str(uuid.uuid4()).upper()

    con = sqlite3.connect(database, timeout=15)
    try:
        with con:
            for table in LEFT_BY_A_SIGN_IN:
                con.execute(f"DELETE FROM {table}")
            con.execute(
                """INSERT INTO AdminCoreOwnerAccounts (
                     Id, Email, PasswordHash, TotpSecretCipher, TotpEnrolledAt,
                     RecoveryCodesIssued, LastUsedTotpTimestep, FailedFactorAttempts,
                     FactorAttemptWindowStartedAt, FactorLockedUntil,
                     PendingTotpSecretCipher, RecoveryCodesRevealCipher, CreatedAt
                   ) VALUES (?, ?, ?, ?, ?, ?, NULL, 0, NULL, NULL, NULL, NULL, ?)""",
                (
                    account_id,
                    state["email"],
                    state["passwordHash"],
                    state["totpCipher"],
                    now if enrolled else None,
                    1 if enrolled else 0,
                    now,
                ),
            )
            for code_hash in state["recoveryHashes"]:
                con.execute(
                    """INSERT INTO AdminRecoveryCodes (Id, AccountId, CodeHash, UsedAt, CreatedAt)
                       VALUES (?, ?, ?, NULL, ?)""",
                    (str(uuid.uuid4()).upper(), account_id, code_hash, now),
                )
    finally:
        con.close()


if __name__ == "__main__":
    main()
