# T09a: release pipeline, the engine part (offline steps 1 to 5)

| Field | Value |
|-------|-------|
| Scope | T09a of the T09 implementation plan, steps 1 to 5: `/health/ready` and the version, the `GIT_SHA` build argument, the database-change check, the CI image build, and the CI upgrade rehearsal |
| Branch | `claude/t09a-release-pipeline`, from `main` at `f4c85dd`; pull request #84 (draft) |
| The other part | The deployment side (the migration step, the release and smoke-test scripts, their offline tests and the release log) is in the private infrastructure repository, with its own record |
| Date | 2026-11-27 |
| Result | **Built and tested.** Independent review: merge after fixes; every item fixed, re-review pending. The rehearsal found a defect outside T09a, since fixed by T58a; the rehearsal now passes end to end (below). |

## What was built

| Plan step | What | Where |
|-----------|------|-------|
| 1 | `/health` keeps answering without touching the database. `/health/ready` adds `SELECT 1` within 2 seconds: 200 `{"status":"ok","version":...}`, or 503 `{"status":"unavailable","version":...}`. No other detail; `Cache-Control: no-store` on every answer. Both answer signed out on the engine's address. The admin address keeps its own list: `/health` only, so `/health/ready` there is 404. No exemption was added to the admin session gate, which only looks at `/admin` paths. The 2-second limit holds twice: the query is cancelled at the limit, and the answer does not wait for a driver that ignores the cancellation. Only the kind of fault is logged, never its message. | `src/Dealoware.Api/Endpoints/HealthEndpoints.cs`, `Program.cs` |
| 1 | The version: the commit's first 7 characters, built into the assembly (`DealowareGitSha` property, assembly metadata). Only a hex hash of 7 to 40 characters is accepted; anything else, and any build without it, answers `dev`. | `src/Dealoware.Api/ReleaseVersion.cs`, `Dealoware.Api.csproj` |
| 2 | `GIT_SHA` build argument, default `dev`, declared after the restore layer; the build refuses anything but `dev` or a lower-case hex hash; the image is labelled `org.opencontainers.image.revision`. | `Dockerfile` |
| 3 | The database-change check (spec section 2.2): every migration added since the last `release-*` tag, read as EF's own operations for SQLite and PostgreSQL. Refused: any drop (table, column, key, constraint, index, sequence, schema), a table or column rename, a type change, a column made required without a default (also a new required column without a default on an existing table), a shorter length or smaller precision, a check constraint added to an existing table, raw SQL with `DROP`, `RENAME` or `ALTER ... TYPE`. A table made in the same release may still change. A `*_Contract` migration passes only with the comment `// Code stopped using this in release-<n>` naming a tagged release. A migration that was in the last release must not change. | `tests/Dealoware.Api.Tests/ReleaseMigrationRules.cs`, `ReleaseMigrationRulesTests.cs`; CI job "Database changes since the last release" |
| 4 | CI image build: `docker build` with the commit as `GIT_SHA` on every pull request and on `main`; nothing is pushed. The image is started once (Development, its own SQLite file, throwaway keys made per run) and both health answers must give the commit's first 7 characters. On failure the job prints the container's state and log. | `.github/workflows/ci.yml`, job "Image build" |
| 5 | CI upgrade rehearsal (spec section 2.3): builds the last `release-*` tag and this commit; on PostgreSQL 16 with TLS runs the release's migrations, loads a fixed data set through the release's own API (two participants, an artifact, a negotiation, an offer, a strategy), runs this commit's migrations, starts the release again and requires `/health/ready` 200 and every seeded record to read back byte for byte. Both run as Production: `VerifyFull`, migrations under one login, the API under a least-privilege login, throwaway keys. Skipped, with a note in the job summary, while no `release-*` tag exists. | `scripts/upgrade-rehearsal/run.sh`, `records.mjs`; job "Upgrade rehearsal from the last release" |

## Tests run

| Test | Where | Result |
|------|-------|--------|
| `HealthEndpointTests` (26: 200 with the database, 503 when it refuses, 503 at the limit when it never answers, back to 200, version in both and nothing else, `no-store` on all, no sign-in needed, the admin address keeps `/health` only, the version rule) | Windows, SQLite | 26 passed |
| The same, on PostgreSQL 16 (their own database, `dealoware_ci_health`) | Windows, a throwaway PostgreSQL 16 container | 26 passed |
| `ReleaseMigrationRulesTests` (29: each rule on migrations written for the test; the contract cases; the check on this repository) | Windows | 29 passed; the check said "No release-* tag yet ... Nothing to check" |
| The check refuses a real drop (plan step 3) | A scratch clone with a local `release-1` tag and an added migration `DemoDropColumn` dropping `Offers.Terms`, run with `DEALOWARE_RELEASE_MIGRATION_CHECK=required` | Failed as it should, with exactly one finding: `drop: DropColumn Offers.Terms`. After the migration was removed: passed, "Migrations added since: none". The scratch clone and its tag were deleted; nothing of it is in this branch. |
| `records.mjs` seed and two reads | Windows, the published API in Development on SQLite | The two reads were byte-identical |
| Image build with `GIT_SHA` | Docker on Windows, from `git archive` of the commit | Built; label equal to the full hash. A bad value (`bad;rm`) was refused by the build. |
| All CI jobs | GitHub, pull request #84 | Build & Test, PostgreSQL, the database-change check and the upgrade rehearsal (skipped, no tag) passed. The image build failed at first: even Development refuses to start without the three admin keys. Fixed in `9610c38`; its run is the coordinator's to see. |

The local Windows baseline has five known failures outside this work (`AdminTotpTests.SecSc6_NewServiceInstanceKeepsFactorLock` and four `PostmanCollectionHygieneTests.RunNewmanScript_*`); the full suite was left to CI.

## Found on the way

- On Windows, the check first reported two released migrations as changed: git's output was read in the console code page, which changed the dashes in their comments. It now reads UTF-8 (`714e44f`).
- A Windows checkout with `core.autocrlf=true` cannot build the image: the `Dockerfile`'s checksum of `certs/rds-global-bundle.pem` fails on CRLF line endings. CI (Linux) is not affected. The release script builds from a worktree made with `core.autocrlf=false`.

## Independent review: merge after fixes

The review found, for the engine part, three medium items. Each is fixed, with tests.

| Item | Finding | Fix | Commit |
|------|---------|-----|--------|
| M4 | A table dropped and made again under the same name counted as new, so its drop passed. | A table counts as new only if it is not in the last release's model (the tables its migrations made). | `9a3a453` |
| M5 | A unique index, a unique constraint or a foreign key added to an existing table, a column made required with a default, and raw SQL with `SET NOT NULL`, `ADD CONSTRAINT`, `TRUNCATE` or `DELETE` all passed. | Each is now refused. | `9a3a453` |
| M6 | The rehearsal had never run end to end, and only read after the upgrade. | `run.sh` takes a release tag or any commit to treat as one; CI's manual run takes it as `previous_ref`. After the upgrade the previous release now writes one record of each kind (participant, profile, artifact, negotiation, offer and its decline, strategy and its update) and reads each back. | `cd41fa5`, `73eed36` |

`ReleaseMigrationRulesTests`: 38 passed (was 29).

### The rehearsal, run end to end

It was run on Windows with Docker (PostgreSQL 16 with TLS, Production, `VerifyFull`, a least-privilege API login), from `21dfc16` (the first commit with `/health/ready`) to this branch. The run fixed three things in `run.sh` (`73eed36`): the API now starts from its build folder, as in the image, so it reads `appsettings.json`; the forwarded-headers setting is a private range, because loopback is refused; and the readiness wait works with Git for Windows' `curl`.

Then it found a real defect, **not in T09a's code**:

- **On a fresh PostgreSQL database made by `migrate`, the API cannot register a participant** (HTTP 500: `column "IsActive" is of type integer but expression is of type boolean`). The baseline migration's model file, `20261005000000_Baseline.Designer.cs`, was scaffolded against SQLite and carries SQLite store types (6 columns `INTEGER`, 71 `TEXT`), and EF uses them for PostgreSQL too. The tests do not see it: they make their PostgreSQL schema with `EnsureCreated`, not with the migrations. A database made before the migrations and then stamped does not see it either. A first release onto an empty database would.
- With those store types removed from the baseline model, in a scratch copy only, the rehearsal passed every step: migrations, the data set, the new migrations, `/health/ready` 200, every record read back byte for byte, and one write of each kind read back.
- The fix (remove the SQLite store types from the baseline's model file, and run a migration test on PostgreSQL in CI) is outside T09a and changes the database layer. It waits for a decision.

## After T58a was merged into this branch (`021d15c`)

- T58a brought the fix for the baseline migration's database types (approved by the owner), the `dbroles` command, and a sixth CI job, "New database rehearsal". `ci.yml` keeps all six jobs; the PostgreSQL job's filter runs both `HealthEndpointTests` and `DatabaseRolesPostgresTests`.
- The upgrade rehearsal now sets the database up the T58a way (`b05993f`): the database belongs to a master login that is not a superuser, the release's own `dbroles` makes the two limited logins, migrations run as `dealoware_migrate` and the API as `dealoware_app`, with the engine's own grants.
- **The rehearsal passed end to end** on Windows with Docker (PostgreSQL 16, TLS, Production, `VerifyFull`), from `021d15c` as the previous release to this branch: `dbroles`, the previous release's migrations (every grant step `ok`), the fixed data set, this commit's migrations, `/health/ready` 200, every record read back byte for byte, and one write of each kind read back. The defect it found earlier is gone with the real fix, not the scratch one.
- `ReleaseMigrationRulesTests` on the merged branch: 38 passed; the check says there is no release tag yet.
- The release script on the infrastructure side now requires the six jobs by name.

## Not done

- The rehearsal's first run against a real `release-*` tag waits for `release-1`. Until then CI's manual run can point it at a commit.
- No re-review yet.
