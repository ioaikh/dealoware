# T09c: backups and the restore test, the engine part (offline step 1)

| Field | Value |
|-------|-------|
| Scope | T09c of the T09 implementation plan, step 1: the read-only `dbcheck` command (T09 spec section 5.2) and its tests |
| Branch | `claude/t09c-backups`, from `main` at `ac883aa` |
| The other part | The backup settings, the `dealoware-dbcheck` task, `restore-test.ps1` and its tests, the restore-test log, the 35-day rule in `release.ps1` and the README's "Recovering the database" are in the private infrastructure repository, with their own record |
| Date | 2026-11-27 |
| Hosting | **None touched.** Nothing here calls a hosting provider. Shipping `dbcheck` in a release and the first restore test are separate steps that wait for Ivan's recorded OK. |
| Result | **Built and tested.** Independent review: fix first, no blockers; the engine item (false failures once the admin console is live) is fixed in `8893b4a`, below. |

## What was built

| What | Where |
|------|-------|
| `dotnet Dealoware.Api.dll dbcheck --cutoff <UTC time>`: a one-shot command like `migrate`; it does not start the web host. The time must be ISO 8601 with `Z` or an offset; anything else is refused (exit 2) before connecting. It connects as the API does (`DB_*`, and outside Development `VerifyFull` with the root certificate); PostgreSQL only. It prints one JSON line (exit 0), or one line `dbcheck: failed (<reason>)` (exit 1) whose reason is an error code or a type, never the driver's message, a host or a password. | `src/Dealoware.Api/DatabaseCheckCommand.cs`, `Program.cs` |
| The check: one transaction, `REPEATABLE READ` and `READ ONLY` (checked with `current_setting('transaction_read_only')`), with fixed output settings (UTC, ISO dates, full float digits, hex bytes) so the same row gives the same text in any session; it is rolled back. For each table of the model: the rows created at or before the cutoff, whether the engine hard-deletes from it, and the newest migration. The known records: up to five of the newest rows created at or before the cutoff that have not changed since it, each as a SHA-256 of its primary key and a SHA-256 of all its stored columns (`t::text`). | `src/Dealoware.Infrastructure/Persistence/DatabaseCheck.cs` |
| The creation column of each table (`CreatedAt`, `Timestamp`, `OccurredAt`, `RevokedAt`), or none: `ArtifactValues`, `EntityProperties`, `SubjectEntities`, `TimePeriods`, `AdminAuthLockouts`, `AdminCredentials`, `AdminResetIpCounters` and `AdminSessionStepUps` have no creation time, so all their rows are counted. A test fails when a table is added to the model without being listed. | `DatabaseCheck.CreatedColumns` |
| The change-time columns of each table (`UpdatedAt`, `DeletedAt`, `ConsumedAt`, `UsedAt`, `RevokedAt`, `LastActivityAt`, `PasswordUpdatedAt`, the lockout and step-up times): a row whose change time is after the cutoff is left out of the known records, so a row changed on the live database after the cutoff cannot make a good backup look bad. A test fails when a table has `UpdatedAt` or `DeletedAt` that is not listed. | `DatabaseCheck.ChangedColumns` |
| The tables the engine hard-deletes from: `AdminAuthFailureEvents`, `AdminDeleteConfirmTokens`, `AdminRecoveryCodes`, `AdminSessionRecoveryNotices`, `AdminSessions`, `RevokedTokens`. A test scans the source for `ExecuteDelete`, `Remove` and `RemoveRange` and fails when the list and the code disagree. | `DatabaseCheck.HardDeleteTables` |

## Tests run

| Test | Where | Result |
|------|-------|--------|
| `DatabaseCheckPostgresTests` (6): a write inside the check's transaction is refused (`INSERT`, `UPDATE`, `DELETE`, `CREATE TABLE`, each SQLSTATE 25006, nothing changed); two databases with equal data give equal output (a row created after the cutoff in one of them does not change it); one changed field (a boolean, and a `numeric` amount) changes that table's row hash and nothing else; a row changed on the live side after the cutoff is left out of its known records, and what it keeps matches the copy record by record; a hard delete lowers only that table's count; no column value appears in the output (IDs, email, name, subject, IP hash, amount, timestamps), and every string in it is a fixed word, a table or column name, the cutoff, the migration ID or a SHA-256. The model has `numeric` columns but no `jsonb` or array columns. | Windows, a throwaway PostgreSQL 16 container | 6 passed |
| `DatabaseRolesPostgresTests.DbCheck_AsTheAppLogin_ReadsEveryTable_AndChangesNothing`: after `dbroles` and `migrate`, `dealoware_app` can run the check on every table | Same | passed (the class: 10 passed) |
| `DatabaseCheckCommandTests` (23): the command name; the cutoff format; bad arguments refused before connecting; no PostgreSQL refused; an unreachable database fails in one line with no host or password; every model table listed, each listed creation and change column a time of the model; the hard-delete list against the source; the JSON's exact shape | Windows, no database | 23 passed |
| The new-database rehearsal with the new step "4b. dbcheck, as dealoware_app": `Production`, TLS `VerifyFull`, one JSON line, exit 0, no password, host or connection string in its output | Windows, Docker, PostgreSQL 16 with TLS | Step 4b passed. Step 5 (the API start) stopped locally because the shell had no admin keys set; CI sets them. |
| The full suite (before the review fix) | Windows, SQLite, no filter | 2528 passed, 24 skipped, 6 failed: the five known local failures (`AdminTotpTests.SecSc6_NewServiceInstanceKeepsFactorLock` and four `PostmanCollectionHygieneTests.RunNewmanScript_*`), and `AdminAuditPostgresAppendOnlyTests.SecR2_...`, which needs PostgreSQL and which CI's SQLite job leaves out by its filter. None touches this work. |

CI: the PostgreSQL job's filter now runs `DatabaseCheckPostgresTests` as well; the "New database rehearsal" job runs `dbcheck`.

## Decisions taken here, for the reviewer

- **The known record's ID is printed as a hash.** Spec 5.2 says "its ID and a SHA-256 of its stored columns" and "it prints no column value". An ID is a column value (for `AdminCredentials` the key is an email address), so the output carries a SHA-256 of the primary key instead. The comparison works the same way.
- **Tables without a creation time are counted whole.** Their count cannot be cut at the cutoff, so the restore test notes a difference there rather than failing on it; their known records are still compared.
- **Change times, not a perfect diff.** A row changed without a change time (a counter, say) still shows as a difference if it is among the known records. The restore-test runbook asks for no admin activity during the test.

## Re-review fix (`7422c8f`)

The re-review found that the four tables with no creation time of their own (`ArtifactValues`, `SubjectEntities`, `TimePeriods`, `EntityProperties`: the core negotiation data) could lose every row on the copy and still pass, since their counts were only notes. Now:

- They are cut through their artifact: `ArtifactId` to `Artifacts`, and `EntityProperties` through `SubjectEntityId` to `SubjectEntities` and on to `Artifacts` (`DatabaseCheck.ParentCuts`). Their rows are written only when the artifact is created (checked in the domain code: an admin edit renames a subject entity but stamps the artifact's `UpdatedAt`), so the artifact's `CreatedAt` is their creation time and the artifact's change times are theirs. A row with no artifact is counted apart as `uncutRows`.
- `rows` is null for the four tables that still cannot be cut (`AdminAuthLockouts`, `AdminCredentials`, `AdminResetIpCounters`, `AdminSessionStepUps`); their count is `uncutRows`. A test pins that list, and checks each parent-cut step against the model's foreign keys.
- The hard-delete scan now follows cascading foreign keys: it found `AdminSessionStepUps` (deleted with its session), now in the list.

Tests: `DatabaseCheckPostgresTests` 7 (new: the children are cut through their artifact, a later artifact and an edit after the cutoff change nothing, and a copy without the children counts 0); `DatabaseCheckCommandTests` 23; with `DatabaseRolesPostgresTests` and the table-grant tests, 43 passed on a throwaway PostgreSQL 16.
