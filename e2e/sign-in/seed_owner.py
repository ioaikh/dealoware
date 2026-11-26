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
"""
import json
import sqlite3
import sys
import uuid
from datetime import datetime, timezone

LEFT_BY_A_SIGN_IN = (
    "AdminRecoveryCodes",
    "AdminPendingAuths",
    "AdminSessions",
    "AdminAuthTokens",
    "AdminAuthFailureEvents",
    "AdminAuthLockouts",
    "AdminCoreOwnerAccounts",
)


def main() -> None:
    state = json.load(sys.stdin)
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f") + "0Z"
    enrolled = state["totpCipher"] is not None
    account_id = str(uuid.uuid4()).upper()

    con = sqlite3.connect(state["db"], timeout=15)
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
