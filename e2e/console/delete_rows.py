"""Changes, behind the page's back, what the delete dialog's browser tests need
changed (e2e/console/delete.spec.ts).

Called with one JSON object on standard input:

    db              the run's SQLite file
    expireTokens    optional, true: every delete confirm token runs out now
                    (the server's clock is the real one, so five minutes are
                    not waited for; the token is moved instead)
    addOfferTo      optional, a negotiation id: a new open offer in it, as a
                    party would make one while the dialog is open
    addNegotiationFor
                    optional, an artifact id: a new open negotiation on it,
                    between its owner and another participant

Nothing else is changed. It works on one kind of database only, as
seed_owner.py does: the file e2e.db of a browser-test run, in the folder the
runner made and marked for it.
"""
import json
import sqlite3
import sys
import uuid
from datetime import datetime, timedelta, timezone
from pathlib import Path

# The run-folder check is seed_owner.py's own. Imported without leaving a
# __pycache__ folder in e2e/sign-in.
sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "sign-in"))
from seed_owner import a_runs_own_database  # noqa: E402


def stored(value: datetime) -> str:
    """As the app stores a time in SQLite: sortable UTC, seven decimals."""
    return value.strftime("%Y-%m-%dT%H:%M:%S.%f") + "0Z"


def main() -> None:
    request = json.load(sys.stdin)
    database = a_runs_own_database(request.get("db"))
    now = datetime.now(timezone.utc)

    con = sqlite3.connect(database, timeout=15)
    try:
        with con:
            if request.get("expireTokens") is True:
                con.execute("UPDATE AdminDeleteConfirmTokens SET ExpiresAt = ?", (stored(now - timedelta(minutes=1)),))
            negotiation = request.get("addOfferTo")
            if negotiation is not None:
                row = con.execute(
                    "SELECT Id, PartyAParticipantId, PartyBParticipantId FROM Negotiations WHERE lower(Id) = lower(?)",
                    (str(negotiation),),
                ).fetchone()
                if row is None:
                    sys.exit("no such negotiation")
                con.execute(
                    """INSERT INTO Offers (Id, NegotiationId, FromParticipantId, ToParticipantId, Status, Amount,
                         Currency, Terms, CreatedAt, UpdatedAt, DeletedAt, Version)
                       VALUES (?, ?, ?, ?, 0, '77.0', 'USD', NULL, ?, NULL, NULL, 0)""",
                    (str(uuid.uuid4()).upper(), row[0], row[1], row[2], stored(now)),
                )
            artifact = request.get("addNegotiationFor")
            if artifact is not None:
                row = con.execute(
                    "SELECT Id, OwnerParticipantId FROM Artifacts WHERE lower(Id) = lower(?)", (str(artifact),)
                ).fetchone()
                if row is None:
                    sys.exit("no such artifact")
                other = con.execute(
                    "SELECT Sub FROM Participants WHERE Sub <> ? AND DeletedAt IS NULL ORDER BY Sub LIMIT 1", (row[1],)
                ).fetchone()
                con.execute(
                    """INSERT INTO Negotiations (Id, ArtifactId, PartyAParticipantId, PartyBParticipantId,
                         PartyAIntent, PartyBIntent, Status, StartsAt, EndsAt, CreatedAt, UpdatedAt, DeletedAt,
                         Version) VALUES (?, ?, ?, ?, 'sell', 'buy', 0, NULL, NULL, ?, NULL, NULL, 0)""",
                    (str(uuid.uuid4()).upper(), row[0], row[1], other[0], stored(now)),
                )
    finally:
        con.close()


if __name__ == "__main__":
    main()
