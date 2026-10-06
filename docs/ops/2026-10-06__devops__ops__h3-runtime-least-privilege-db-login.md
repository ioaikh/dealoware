# H3 — Least-privilege runtime DB login

## Deploy order (required)

An image that contains this guard must **not** be baked or pinned live until the runtime DB secret has been cut over from the master login to the least-privilege app login. Pinning the guarded image while the long-lived API still uses the master (or any privileged) login will fail closed at startup.

Required order:

1. **Create roles** — migrations login `dealoware_migrate` (owns schema objects; may CREATE / INSERT `__EFMigrationsHistory`) and least-privilege app login `dealoware_app` (DML only; **SELECT-only** on `__EFMigrationsHistory`). Set default privileges so objects the migrations login creates are usable by the app login.
2. **Baseline existing databases** — deployed DBs were created with Development `EnsureCreated` / schema bootstrap and have **no** `__EFMigrationsHistory` row (there were no EF migrations on main). The first real incremental migration (A7 PR #20 `20261006000100_AddAdminTablesAndSoftDelete`, open / not modified here) assumes that schema already exists. The `migrate` one-shot detects a **complete** baseline table set with no history and **records** `20261005000000_Baseline` as applied instead of re-running `CREATE TABLE`, then applies any newer migrations. A fresh empty database applies the baseline for real. A partial or unknown schema fails closed. This never runs at API startup and never writes history as `dealoware_app`.
3. **Run `migrate`** — one-shot (`dotnet Dealoware.Api.dll migrate` or `dotnet run --project src/Dealoware.Api -- migrate`) as `dealoware_migrate` only (same `DB_*` / connection-string keys; migrations credentials injected into that process only).
4. **Cut the API secret** — point the long-lived API at `dealoware_app` (same `DB_*` / connection-string keys; different credentials).
5. **Pin the guarded image** — only after the runtime secret is the app login.

Alternatively, cut over the API secret and pin the guarded image in **one step**, with a rollback plan (revert the image pin and/or restore the previous runtime secret).

Any image that also includes A7 PR #20 additionally requires `DEALOWARE_ADMIN_IP_HMAC_KEY` to be set in the same deploy. Missing that key fails the admin path closed; it is independent of this DB-login guard.

## Runtime vs migrations login

| Process | Credentials | Privileges |
|---------|-------------|------------|
| Long-lived API | `dealoware_app` | `CONNECT` on the database; `USAGE` on the app schema; `SELECT` / `INSERT` / `UPDATE` / `DELETE` on tables; `USAGE` / `SELECT` on sequences; **SELECT-only** on `__EFMigrationsHistory`. Must **not** own tables, must **not** need DDL, and must **not** be superuser, `CREATEROLE`, `CREATEDB`, or a member of `rds_superuser`. |
| `migrate` one-shot | `dealoware_migrate` | Owns the schema objects. May `CREATE` / `INSERT` `__EFMigrationsHistory` (baseline stamp + later migrations). Same image as the API; run as a one-shot task with migrations credentials injected only into that task. Never inject this login into the long-lived API. |

Default privileges on the app schema must `GRANT` the runtime login access to tables (and sequences) the migrations login creates. Otherwise DML fails after cutover even when the privilege guard passes.

This repository does not run DDL from the long-lived API outside Development (`DatabaseSchemaBootstrap.ApplyStartupSchemaAsync` no-ops). Schema changes and any `CREATE` / `INSERT` on `__EFMigrationsHistory` go through `migrate` only. The API runtime path does not stamp the baseline, does not call `MigrateAsync`, and does not need DDL.

## Startup guard (long-lived API only)

Active when the provider is PostgreSQL **and** the environment is not Development. Not invoked by `DatabaseMigrateCommand`.

After the `DbContext` is available and before `app.Run`, the API runs a read-only catalog query on the current login and **fails closed** if any of the following is true:

- `rolsuper`
- `rolcreaterole`
- `rolcreatedb`
- member of role `rds_superuser` (if that role does not exist, membership is treated as false)
- `current_user` owns any table in the app schema (`pg_tables.tableowner = current_user`)

Failure logs a safe message (no hosts, users, passwords, or connection strings) and `Environment.Exit(1)` when the entry assembly is `Dealoware.Api`.

## Break-glass

`Database:AllowPrivilegedRuntimeLogin` defaults to **false**. Set it to `true` only as explicit break-glass (for example a documented emergency). Nothing is skipped silently: when the flag is on in a PostgreSQL non-Development process, the API logs that the check was skipped.

Do not use break-glass as the deploy path. Follow **Deploy order (required)** above.
