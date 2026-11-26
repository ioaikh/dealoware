"""Puts made-up participants, artifacts, negotiations and offers into the run's
database, for the list and detail pages of the console browser tests.

Called by e2e/console/entities.ts with one JSON object on standard input:

    db      the run's SQLite file
    empty   optional: names of tables to leave empty after seeding
            (for the "No {type} yet." state), from Participants, Artifacts,
            Negotiations, Offers

Every run starts from the same rows: the four tables (and the subject
entities of artifacts) are emptied, then filled. It prints one JSON object
with the ids the tests look for. Nothing here is real: names are invented,
addresses are on example.test.

Because it empties tables, it works on one kind of database only: the file
e2e.db of a browser-test run, in the temporary folder the runner made and
marked for it (e2e/signed-out/harness.sh). Given any other path it stops
before it opens anything.
"""
import json
import sqlite3
import sys
import uuid
from datetime import datetime, timedelta, timezone
from decimal import Decimal
from pathlib import Path

RUN_DATABASE = "e2e.db"
RUN_MARKER = ".dealoware-e2e-run"
RUN_MARKER_TEXT = "dealoware e2e run"

# Children first.
TABLES = ("Offers", "Negotiations", "SubjectEntities", "Artifacts", "Participants")
MAY_BE_EMPTIED = {"Participants", "Artifacts", "Negotiations", "Offers"}

# Status numbers as the app stores them.
NEGOTIATION = {"Open": 0, "Closed": 1, "Expired": 2}
OFFER = {"Open": 0, "Accepted": 1, "Declined": 2, "Superseded": 3, "Cancelled": 4}

NAMED_PEOPLE = [
    # display name, active
    ("Alex Rivera", True),
    ("Blair Chen", True),
    ("Casey Novak", False),
    ("Dana Okafor", True),
    ("Eli Moreau", True),
]
DELETED_PERSON = "Robin Hart"

NAMED_THINGS = [
    # name, owner, description
    ("Red bicycle", "Alex Rivera", "A red city bicycle.\nNew tyres, one owner."),
    ("Oak desk", "Blair Chen", "Solid oak, 140 by 70 cm."),
    ("Vintage camera", "Dana Okafor", "35 mm film camera with its case."),
    ("Garden tools", "Eli Moreau", "Spade, rake and shears."),
]
DELETED_THING = ("Broken lamp", "Alex Rivera", "Does not switch on.")

BULK = 200
OFFERS = 1234
BASE = datetime(2026, 11, 20, 15, 0, 0, tzinfo=timezone.utc)


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
            "seed_entities.py empties tables, so it only works on the database of a browser-test run "
            f"({RUN_DATABASE} in a folder the runner marked with {RUN_MARKER}). Start it with e2e/console/run.sh."
        )
    return database


def stamp(at: datetime | None) -> str | None:
    """As the app stores a time on SQLite: UTC, seven fractional digits, Z."""
    if at is None:
        return None
    return at.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f") + "0Z"


def new_id() -> str:
    return str(uuid.uuid4()).upper()


def amount(value: Decimal) -> str:
    """As the app stores a decimal on SQLite: invariant, at least one decimal place."""
    text = format(value.normalize(), "f")
    return text if "." in text else text + ".0"


def main() -> None:
    request = json.load(sys.stdin)
    database = a_runs_own_database(request.get("db"))
    empty = set(request.get("empty") or [])
    if not empty <= MAY_BE_EMPTIED:
        sys.exit("empty may name Participants, Artifacts, Negotiations and Offers only")

    people = {}  # name -> (id, sub)
    things = {}  # name -> id
    out = {"participants": {}, "artifacts": {}, "negotiations": {}, "offers": {}}

    con = sqlite3.connect(database, timeout=15)
    try:
        with con:
            for table in TABLES:
                con.execute(f"DELETE FROM {table}")

            def person(name, active, created, updated=None, deleted=None, mail=False):
                pid = str(uuid.uuid4())
                sub = f"participant:{pid}"
                slug = name.lower().replace(" ", ".")
                con.execute(
                    """INSERT INTO Participants (Id, Sub, DisplayName, LoginEmail, ContactEmail, CreatedAt,
                         UpdatedAt, IsActive, DeletedAt, Version) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, 0)""",
                    (pid.upper(), sub, name,
                     f"{slug}@example.test" if mail else None,
                     f"{slug}.contact@example.test" if mail else None,
                     stamp(created), stamp(updated), 1 if active else 0, stamp(deleted)),
                )
                people[name] = (pid.upper(), sub)
                return sub

            def thing(name, owner_sub, description, created, updated=None, deleted=None):
                aid = new_id()
                con.execute(
                    """INSERT INTO Artifacts (Id, OwnerParticipantId, Intent, CreatedAt, UpdatedAt, DeletedAt,
                         Locations, Version) VALUES (?, ?, 'sell', ?, ?, ?, '[]', 0)""",
                    (aid, owner_sub, stamp(created), stamp(updated), stamp(deleted)),
                )
                con.execute(
                    """INSERT INTO SubjectEntities (Id, ArtifactId, Name, Description, Facts)
                       VALUES (?, ?, ?, ?, '[]')""",
                    (new_id(), aid, name, description),
                )
                things[name] = aid
                return aid

            def negotiation(artifact, a_sub, b_sub, status, created, updated=None, deleted=None, ends=None):
                nid = new_id()
                con.execute(
                    """INSERT INTO Negotiations (Id, ArtifactId, PartyAParticipantId, PartyBParticipantId,
                         PartyAIntent, PartyBIntent, Status, StartsAt, EndsAt, CreatedAt, UpdatedAt, DeletedAt,
                         Version) VALUES (?, ?, ?, ?, 'sell', 'buy', ?, NULL, ?, ?, ?, ?, 0)""",
                    (nid, artifact, a_sub, b_sub, NEGOTIATION[status], stamp(ends), stamp(created),
                     stamp(updated), stamp(deleted)),
                )
                return nid

            def offer(neg, from_sub, to_sub, status, value, currency, terms, created, updated=None, deleted=None):
                oid = new_id()
                con.execute(
                    """INSERT INTO Offers (Id, NegotiationId, FromParticipantId, ToParticipantId, Status, Amount,
                         Currency, Terms, CreatedAt, UpdatedAt, DeletedAt, Version)
                       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 0)""",
                    (oid, neg, from_sub, to_sub, OFFER[status], amount(value), currency, terms,
                     stamp(created), stamp(updated), stamp(deleted)),
                )
                return oid

            # Participants: five named ones (one suspended), one soft-deleted, and 200 more.
            for i, (name, active) in enumerate(NAMED_PEOPLE):
                person(name, active, BASE - timedelta(days=30 + i), updated=BASE - timedelta(hours=i), mail=True)
            person(DELETED_PERSON, True, BASE - timedelta(days=40), deleted=BASE - timedelta(days=2), mail=True)
            for i in range(1, BULK + 1):
                person(f"Seed participant {i:03d}", i % 10 != 0, BASE - timedelta(days=60, minutes=i))
            sub = {name: value[1] for name, value in people.items()}

            # Artifacts: four named ones, one soft-deleted, and 200 more.
            for i, (name, owner, description) in enumerate(NAMED_THINGS):
                thing(name, sub[owner], description, BASE - timedelta(days=20 + i), updated=BASE - timedelta(hours=2 + i))
            thing(DELETED_THING[0], sub[DELETED_THING[1]], DELETED_THING[2], BASE - timedelta(days=25),
                  deleted=BASE - timedelta(days=1))
            for i in range(1, BULK + 1):
                thing(f"Seed artifact {i:03d}", sub[f"Seed participant {i:03d}"], "Made up for the browser tests.",
                      BASE - timedelta(days=50, minutes=i))

            # Negotiations: named ones in every status, one in summer time and one in winter time, one soft-deleted.
            n = out["negotiations"]
            n["bicycle"] = negotiation(things["Red bicycle"], sub["Alex Rivera"], sub["Blair Chen"], "Open",
                                       BASE - timedelta(days=10), updated=BASE - timedelta(minutes=5))
            n["desk"] = negotiation(things["Oak desk"], sub["Blair Chen"], sub["Dana Okafor"], "Open",
                                    BASE - timedelta(days=9), updated=BASE - timedelta(minutes=10))
            n["camera"] = negotiation(things["Vintage camera"], sub["Dana Okafor"], sub["Eli Moreau"], "Closed",
                                      datetime(2026, 7, 1, 16, 30, tzinfo=timezone.utc))
            n["tools"] = negotiation(things["Garden tools"], sub["Eli Moreau"], sub["Alex Rivera"], "Expired",
                                     datetime(2026, 1, 15, 17, 30, tzinfo=timezone.utc),
                                     ends=datetime(2026, 2, 1, 17, 0, tzinfo=timezone.utc))
            n["second-bicycle"] = negotiation(things["Red bicycle"], sub["Alex Rivera"], sub["Casey Novak"], "Open",
                                              BASE - timedelta(days=8))
            n["deleted"] = negotiation(things["Oak desk"], sub["Blair Chen"], sub["Alex Rivera"], "Open",
                                       BASE - timedelta(days=7), deleted=BASE - timedelta(days=1))
            bulk_negotiations = []
            cycle = ["Open", "Closed", "Expired"]
            for i in range(1, 25):
                bulk_negotiations.append(negotiation(
                    things[f"Seed artifact {i:03d}"], sub[f"Seed participant {i:03d}"],
                    sub[f"Seed participant {i + 100:03d}"], cycle[i % 3], BASE - timedelta(days=12, minutes=i)))

            # Offers: 1,234 not deleted, in five statuses (none Withdrawn: nothing writes it yet), and one deleted.
            o = out["offers"]
            o["terms"] = offer(n["bicycle"], sub["Alex Rivera"], sub["Blair Chen"], "Open", Decimal("1250.5"), "USD",
                               "Pick-up in person.\nCash or bank transfer.", BASE - timedelta(days=5),
                               updated=BASE - timedelta(minutes=1))
            o["accepted"] = offer(n["desk"], sub["Dana Okafor"], sub["Blair Chen"], "Accepted", Decimal("300"), "EUR",
                                  None, BASE - timedelta(days=4), updated=BASE - timedelta(minutes=2))
            o["superseded"] = offer(n["bicycle"], sub["Blair Chen"], sub["Alex Rivera"], "Superseded", Decimal("1100"),
                                    "USD", None, BASE - timedelta(days=6), updated=BASE - timedelta(minutes=3))
            o["deleted"] = offer(n["bicycle"], sub["Alex Rivera"], sub["Blair Chen"], "Open", Decimal("999"), "USD", None,
                                 BASE - timedelta(days=6), deleted=BASE - timedelta(days=1))
            statuses = list(OFFER)
            targets = [n["bicycle"], n["desk"], n["second-bicycle"]] + bulk_negotiations
            parties = {
                n["bicycle"]: (sub["Alex Rivera"], sub["Blair Chen"]),
                n["desk"]: (sub["Blair Chen"], sub["Dana Okafor"]),
                n["second-bicycle"]: (sub["Alex Rivera"], sub["Casey Novak"]),
            }
            for i, neg in enumerate(bulk_negotiations, start=1):
                parties[neg] = (sub[f"Seed participant {i:03d}"], sub[f"Seed participant {i + 100:03d}"])
            for i in range(OFFERS - 3):
                neg = targets[i % len(targets)]
                a, b = parties[neg]
                from_sub, to_sub = (a, b) if i % 2 == 0 else (b, a)
                offer(neg, from_sub, to_sub, statuses[i % len(statuses)], Decimal(10) + Decimal(i) * Decimal("1.25"),
                      "USD" if i % 3 else "EUR", None, BASE - timedelta(days=3, minutes=i))

            for table in ("Offers", "Negotiations", "Artifacts", "Participants"):
                if table in empty:
                    if table == "Artifacts":
                        con.execute("DELETE FROM SubjectEntities")
                    con.execute(f"DELETE FROM {table}")
    finally:
        con.close()

    out["participants"] = {name: {"id": value[0].lower(), "sub": value[1]} for name, value in people.items()
                           if not name.startswith("Seed ")}
    out["artifacts"] = {name: value.lower() for name, value in things.items() if not name.startswith("Seed ")}
    out["negotiations"] = {key: value.lower() for key, value in out["negotiations"].items()}
    out["offers"] = {key: value.lower() for key, value in out["offers"].items()}
    out["counts"] = {"participants": len(NAMED_PEOPLE) + BULK, "offers": OFFERS}
    json.dump(out, sys.stdout)


if __name__ == "__main__":
    main()
