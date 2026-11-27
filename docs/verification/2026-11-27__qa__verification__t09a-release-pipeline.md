# T09a: release pipeline, the engine part (offline steps 1 to 5)

| Field | Value |
|-------|-------|
| Scope | T09a of the T09 implementation plan, steps 1 to 5: `/health/ready` and the version, the `GIT_SHA` build argument, the database-change check, the CI image build, and the CI upgrade rehearsal |
| Branch | `claude/t09a-release-pipeline`, from `main` at `f4c85dd`; pull request #84 (draft) |
| The other part | The deployment side (the migration step, the release and smoke-test scripts, their offline tests and the release log) is in the private infrastructure repository, with its own record |
| Date | 2026-11-27 |
| Result | **Built and tested.** Not yet reviewed by an independent reviewer. |

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

## Not done

- The upgrade rehearsal has not run end to end: there is no `release-*` tag yet, and Docker on this machine stopped before a local run could be made. Its first real run is the first pull request after `release-1`. Its pieces were tried separately (the seed and read script above; the migrate command and the API's Production start-up are the ones the release uses).
- No independent review yet.
