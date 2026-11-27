# T58a — the database logins (engine): verification

| Field | Value |
|-------|-------|
| Scope | `dbroles` and `dbroles --verify`; the post-migration rights in `migrate`; the list of tables that may be changed and its test; the CI job "New database rehearsal" |
| Branch | `claude/t58a-database-logins` (draft pull request #85) |
| Rules | T58 specification §3.2 to §3.4; T58 implementation plan, "Order of work in T58a", steps 1 to 3; H3 design §7 steps 1 to 7; H3 ops note (`docs/ops/2026-10-06__devops__ops__h3-runtime-least-privilege-db-login.md`) |
| Date | 2026-11-27 |
| AWS | None. No AWS call was made, and no secret's value was read. |
| Result | **Built and tested. Blocked by one defect that predates T58a:** on a new PostgreSQL database, `migrate` builds the 13 original tables with SQLite column types. `dbroles --verify` finds it, so its CI checks fail until Ivan decides on the fix (below). |

## What was built

1. **`dotnet Dealoware.Api.dll dbroles`** (`DatabaseRolesCommand`, `DatabaseRoles.ApplyAsync`). It runs as the master login, in one transaction, H3 design §7 steps 1 to 5:
   - it creates `dealoware_migrate` and `dealoware_app` if missing (`LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS NOREPLICATION`);
   - it sets their passwords on every run from `DEALOWARE_DB_MIGRATE_PASSWORD` and `DEALOWARE_DB_APP_PASSWORD`;
   - it checks that neither login has a special right or belongs to a role;
   - `GRANT dealoware_migrate TO CURRENT_USER WITH INHERIT FALSE, SET TRUE`;
   - it locks PUBLIC out of the database, grants `CONNECT` and schema `USAGE` (and `CREATE` to `dealoware_migrate`);
   - it gives every existing table and sequence in `public` to `dealoware_migrate`;
   - under `SET LOCAL ROLE dealoware_migrate`, it sets default rights on new tables to `SELECT, INSERT` and on new sequences to `USAGE, SELECT` for `dealoware_app` (Senior Security G1).
2. **`dbroles --verify`** (`DatabaseRoles.VerifyAsync`). It logs in as `dealoware_app` and passes only if all of these hold:
   - the login is `dealoware_app`;
   - the API's own H3 query (`RuntimeLoginPrivilegeGuard.LoadFactsAsync`, now public) finds it not privileged;
   - `CREATE TABLE`, an insert into `__EFMigrationsHistory`, and `UPDATE`, `DELETE` and `TRUNCATE` on `AdminAuditLog` are refused with SQLSTATE 42501, not by the trigger;
   - reading and adding work on `AdminAuditLog` and on `Participants`, through the EF model as the API does;
   - changing and removing work on `Participants`.

   Everything after the privilege check runs in one transaction that is rolled back.
3. **`migrate`** (`DatabaseRoles.ApplyAppGrantsAsync`, called by `DatabaseMigrateCommand` after the migrations, on PostgreSQL). As the owner of the tables, it gives `dealoware_app` exactly its rights:
   - `REVOKE ALL` then `SELECT, INSERT, UPDATE, DELETE` on `DatabaseTableGrants.MayChange`;
   - `SELECT, INSERT` on `ReadAndAddOnly` (`AdminAuditLog`);
   - `SELECT` on `__EFMigrationsHistory`;
   - `USAGE, SELECT` on the sequences.

   It exits 1 if `dealoware_app` does not exist ("run dbroles first"), or if the database holds a table in neither list, or lacks a listed one.
4. **The list of tables that may be changed** (`DatabaseTableGrants`, next to the model). It holds 27 tables in `MayChange` and 1 in `ReadAndAddOnly`.
   - The engine changes or removes rows in all of them except the audit log (repositories, admin edit and delete, token consumption, lockout counters, and `SELECT ... FOR UPDATE`, which needs `UPDATE`).
   - So only `AdminAuditLog` is add-only. Putting a table there wrongly would break the API at run time.
   - `DatabaseTableGrantsTests` fails when a model table is in neither list or in both, or a listed table is not in the model.
5. **CI job "New database rehearsal"** (`.github/workflows/ci.yml`, `scripts/new-database-rehearsal.sh`). It runs on PostgreSQL 16 with TLS, VerifyFull against a throwaway CA, as in production, and with a master login that is a `CREATEROLE CREATEDB` non-superuser that owns the database, as on RDS. In order:
   - `dbroles`, then `dbroles` again;
   - `migrate` as `dealoware_migrate`;
   - `dbroles --verify`;
   - the API starts on `dealoware_app` and `/health` answers 200;
   - the API refuses the master login (exit 1, "Refusing to start");
   - `dbroles` is refused in Development (exit 2);
   - no output of dbroles or migrate holds any of the three passwords, the host, `Host=`, `Password=`, `Username=` or `SSL Mode`.

   The PostgreSQL job also runs the new `DatabaseRolesPostgresTests`.

Choices worth a reviewer's look:

- **Passwords are sent as SCRAM-SHA-256 verifiers** (`PostgresScramVerifier`, checked against the RFC 7677 example), not as plain text. The plain password never reaches the server or its log.
- **What is printed.** The commands print step names, `ok` / `failed`, and at most a SQLSTATE or a fixed reason. They never print an exception message: driver messages can name the host or the login.
- **When `dbroles` refuses** (exit 2, before connecting):
  - in Development, by `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT`;
  - on unknown arguments, or missing DB settings or certificate;
  - on a password that is not 16 to 1024 printable ASCII characters;
  - on reused passwords;
  - when `DB_USERNAME` is one of the two logins it creates.
- **It refuses servers older than PostgreSQL 16** (the `SET` option needs 16; RDS runs 16.9).

## Tests and results

| What | Where | Result |
|------|-------|--------|
| Whole .NET suite | Local, Windows | 2426 passed, 16 skipped (PostgreSQL-only), **5 failed: exactly the known baseline** (`AdminTotpTests.SecSc6_NewServiceInstanceKeepsFactorLock`, four `PostmanCollectionHygieneTests.RunNewmanScript_*`) |
| `DatabaseRolesCommandTests`, `DatabaseTableGrantsTests`, `PostgresScramVerifierTests` (38) | Local | All pass |
| `DatabaseRolesPostgresTests` (6) | Local PostgreSQL 16.9 | 5 pass. **`Verify_OnANewDatabase_PassesEveryCheck_AndLeavesNothingBehind` fails**: "read and add on Participants work: failed (PostgreSQL error 42804)" |
| Rehearsal script | Local PostgreSQL 16.9 with TLS | Every step passes except the same `--verify` step (42804). Steps 5 and 6 pass: `/health` 200 on `dealoware_app`; master refused with exit 1. The secrets scan passes. |
| CI run 46012210209 on PR #85 | GitHub | Build & Test: pass. PostgreSQL Sec Tests: 1 failure, the same test. New database rehearsal: fails at the same step. The duplicate-key exceptions in that log also appear on the last green `main` run (`PK_AdminResetIpCounters`, `IX_AdminCoreOwnerAccounts_Email`); they come from the existing parallel-burst tests. |

## The defect `--verify` found (predates T58a)

- **What.** `20261005000000_Baseline.Designer.cs`, the target model of the first migration, carries SQLite column types: 71 `HasColumnType("TEXT")` and 6 `HasColumnType("INTEGER")`.
  - On PostgreSQL, the migration takes those types. On a **new** PostgreSQL database, `migrate` creates the 13 original tables (Participants, Artifacts, Negotiations, Offers and the rest) with `text` where the model has `uuid` and `timestamp with time zone`, and `integer` where it has `boolean`.
  - The later migrations have no designer, so the admin tables are typed correctly.
  - Until now no PostgreSQL database was ever built by the migrations. The October database came from `EnsureCreated` and was only stamped.
- **Effect.** The API's own writes and typed reads on those tables fail. For example, the server log for the `--verify` insert says: `column "IsActive" is of type integer but expression is of type boolean`. A uuid lookup on a `text` column also fails (42883).
  - `/health` does not touch the database, so a deployment would look healthy.
  - Registration and every engine endpoint would fail.
- **Why not fixed in T58a.** It changes a migration, which is outside the T58a steps and needs Ivan's OK. The check stays as the specification wrote it (one business table). Rewriting the probe to fit the wrong types would hide the defect.
- **A fix, ready and not merged.** Local branch `claude/t58a-baseline-pg-types`, one commit (`292a4d8`) on top of this branch, not pushed. It removes the 77 SQLite type annotations from the Baseline designer. Without them, each provider takes its own types for the same CLR types:
  - SQLite still gets `TEXT` / `INTEGER`, so its schema does not change;
  - PostgreSQL gets `uuid`, `timestamp with time zone`, `boolean`, `bigint`, `character varying(n)`;
  - those are the types the existing baseline-stamp check already expects of an `EnsureCreated` PostgreSQL database.
- **Results with the fix** (local):
  - the rehearsal passes end to end, `dbroles --verify: every step ok` included;
  - `DatabaseRolesPostgresTests` 6/6;
  - every `DatabaseMigrationBaseline`, migration and `AdminAuditPostgres` test passes on PostgreSQL 16.9 (58 tests);
  - the whole suite on SQLite: 2426 passed, 16 skipped, and the same 5 known failures as without the fix.

## Not done here

- Nothing in AWS. The stack change, the README and `run-migrate-task.ps1` are in the infrastructure repository (T58a steps 4 to 6), with their own record.
- `docs/ops/2026-10-06__devops__ops__h3-runtime-least-privilege-db-login.md` still describes the October hand-run order. The infrastructure README's "H3 on a new stack" is the procedure for a new stack. Updating the ops note can follow the merge.
