"""Puts made-up entries into the audit log of the run's database, for the audit
viewer's browser tests (audit.spec.ts).

Called with one JSON object on standard input:

    db            the run's SQLite file
    participant   id of a participant that exists (an entry's Entity ID links to it)
    deleted       id of a soft-deleted participant (its Entity ID opens nothing)
    offer         id of an offer that exists
    mode          "seed"   (default) the made-up entries, in place of any this
                           script wrote before
                  "empty"  no entry at all, the app's own included: the
                           "No audit entries yet." state
                  "remove" this script's entries gone, the app's own left

It prints one JSON object with the ids the tests look for. Nothing here is
real: names are invented, addresses are on example.test, and what stands in
for a keyed hash of an IP address is fixed text that hashes nothing.

Every made-up entry is dated in 2025, before anything the app itself writes
during a run, so the app's own entries are always the newest. The app never
changes or deletes an audit entry, and on PostgreSQL the table refuses both.
This script deletes rows, so it works on one kind of database only: the file
e2e.db of a browser-test run, in the temporary folder the runner made and
marked for it (e2e/signed-out/harness.sh). Given any other path it stops
before it opens anything.
"""
import json
import sqlite3
import sys
import uuid
from datetime import datetime, timedelta, timezone
from pathlib import Path

RUN_DATABASE = "e2e.db"
RUN_MARKER = ".dealoware-e2e-run"
RUN_MARKER_TEXT = "dealoware e2e run"

# What the viewer shows of the stored value: its first twelve characters. The
# rest must never reach a page. Every row this script writes starts with them.
IP_PREFIX = "c2VlZGVkLWlw"
IP_REST = "REST-OF-THE-STORED-VALUE-NEVER-SHOWN="

ADMIN = "sam.owner@example.test"
TYPED = "mallory.typed@example.test"

# The bulk entries: one ET day of their own, for paging.
BULK = 130
BULK_DAY = datetime(2025, 2, 10, 15, 0, 0, tzinfo=timezone.utc)


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
            "seed_audit.py deletes audit entries, so it only works on the database of a browser-test run "
            f"({RUN_DATABASE} in a folder the runner marked with {RUN_MARKER}). Start it with e2e/console/run.sh."
        )
    return database


def stamp(at: datetime) -> str:
    """As the app stores a time on SQLite: UTC, seven fractional digits, Z."""
    return at.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f") + "0Z"


def an_id(value: object, name: str) -> str:
    """A record id as the app stores it on SQLite, or the script stops."""
    try:
        return str(uuid.UUID(str(value))).upper()
    except ValueError:
        sys.exit(f"{name} must be the id of a seeded record")


def main() -> None:
    request = json.load(sys.stdin)
    database = a_runs_own_database(request.get("db"))
    mode = request.get("mode") or "seed"
    if mode not in ("seed", "empty", "remove"):
        sys.exit("mode may be seed, empty or remove")

    # Checked before anything is deleted: a seed that cannot be written removes nothing.
    participant = deleted = offer = None
    if mode == "seed":
        participant = an_id(request.get("participant"), "participant")
        deleted = an_id(request.get("deleted"), "deleted")
        offer = an_id(request.get("offer"), "offer")

    out = {"entries": {}, "prefix": IP_PREFIX, "rest": IP_REST, "admin": ADMIN, "typed": TYPED, "bulk": BULK}
    con = sqlite3.connect(database, timeout=15)
    try:
        with con:
            if mode == "empty":
                con.execute("DELETE FROM AdminAuditLog")
            else:
                con.execute("DELETE FROM AdminAuditLog WHERE IpHmac LIKE ?", (IP_PREFIX + "%",))
            if mode != "seed":
                json.dump(out, sys.stdout)
                return

            def entry(key, at, action, actor, reason=None, kind=None, record=None, before=None, after=None,
                      correlation=None):
                eid = str(uuid.uuid4()).upper()
                con.execute(
                    """INSERT INTO AdminAuditLog (Id, Timestamp, Action, ActorEmail, IpHmac, EntityType, EntityId,
                         ReasonClass, CorrelationId, BeforeSnapshot, AfterSnapshot)
                       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                    (eid, stamp(at), action, actor, IP_PREFIX + IP_REST, kind, record, reason, correlation,
                     None if before is None else json.dumps(before),
                     None if after is None else json.dumps(after)),
                )
                if key:
                    out["entries"][key] = eid.lower()

            # Early March 2025 (winter time): a failed sign-in with a typed address, the lock that
            # followed, a sign-in, an edit, a delete, another edit, a sign-out.
            day = datetime(2025, 3, 2, 15, 0, 0, tzinfo=timezone.utc)
            entry("failed", day, "login_failure", TYPED, reason="bad_password")
            entry("locked", day + timedelta(minutes=1), "login_failure", TYPED, reason="locked")
            entry("signedIn", day + timedelta(minutes=5), "login_success", ADMIN)
            # An edit whose stored snapshots hold, beside what may be read, a field the field policy
            # denies the owner (StrategyBody) and a key that looks like a credential: neither may show.
            entry("edit", datetime(2025, 3, 3, 16, 30, 0, tzinfo=timezone.utc), "entity_edit", ADMIN,
                  kind="Participant", record=participant,
                  before={"DisplayName": "Alex Rivers", "ContactEmail": "alex.rivera.contact@example.test",
                          "ParticipantActive": False, "StrategyBody": "DENIED-FIELD-BEFORE",
                          "passwordHash": "CREDENTIAL-LOOKING-BEFORE"},
                  after={"DisplayName": "Alex Rivera", "ContactEmail": "alex.rivera.contact@example.test",
                         "ParticipantActive": True, "StrategyBody": "DENIED-FIELD-AFTER",
                         "passwordHash": "CREDENTIAL-LOOKING-AFTER"})
            entry("delete", datetime(2025, 3, 4, 16, 30, 0, tzinfo=timezone.utc), "entity_delete", ADMIN,
                  kind="Participant", record=deleted, correlation=str(uuid.uuid4()).upper(),
                  before={"DisplayName": "Robin Hart", "ParticipantActive": True})
            entry("offerEdit", datetime(2025, 3, 5, 16, 30, 0, tzinfo=timezone.utc), "entity_edit", ADMIN,
                  kind="Offer", record=offer,
                  before={"OfferTerms": "Pick-up in person."},
                  after={"OfferTerms": "Pick-up in person.\nCash or bank transfer."})
            entry("signedOut", datetime(2025, 3, 5, 20, 0, 0, tzinfo=timezone.utc), "sign_out", ADMIN)
            # One in summer time.
            entry("summer", datetime(2025, 7, 1, 16, 30, 0, tzinfo=timezone.utc), "account.password_changed", ADMIN)
            # And a day of its own with enough entries to page through.
            for i in range(BULK):
                entry(None, BULK_DAY + timedelta(seconds=i), "sign_out" if i % 2 else "login_success", ADMIN)
    finally:
        con.close()

    json.dump(out, sys.stdout)


if __name__ == "__main__":
    main()
