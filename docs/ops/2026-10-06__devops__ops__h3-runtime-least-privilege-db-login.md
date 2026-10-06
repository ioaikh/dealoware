# H3 — Least-privilege runtime DB login

## Deploy order (required)

An image that contains this guard must **not** be baked or pinned live until the runtime DB secret has been cut over from the master login to the least-privilege app login. Pinning the guarded image while the long-lived API still uses the master (or any privileged) login will fail closed at startup.

Required order:

1. **Create roles** — migrations login (owns schema objects) and least-privilege app login (DML only). Set default privileges so objects the migrations login creates are usable by the app login.
2. **Run `migrate`** — one-shot (`dotnet Dealoware.Api.dll migrate` or `dotnet run --project src/Dealoware.Api -- migrate`) with the migrations login injected into `DB_*` / the connection string for that process only.
3. **Cut the API secret** — point the long-lived API at the app login (same `DB_*` / connection-string keys; different credentials).
4. **Pin the guarded image** — only after the runtime secret is the app login.

Alternatively, cut over the API secret and pin the guarded image in **one step**, with a rollback plan (revert the image pin and/or restore the previous runtime secret).

Any image that also includes A7 PR #20 additionally requires `DEALOWARE_ADMIN_IP_HMAC_KEY` to be set in the same deploy. Missing that key fails the admin path closed; it is independent of this DB-login guard.

## Runtime vs migrations login

| Process | Credentials | Privileges |
|---------|-------------|------------|
| Long-lived API | App login | `CONNECT` on the database; `USAGE` on the app schema; `SELECT` / `INSERT` / `UPDATE` / `DELETE` on tables; `USAGE` / `SELECT` on sequences. Must **not** own tables and must **not** be superuser, `CREATEROLE`, `CREATEDB`, or a member of `rds_superuser`. |
| `migrate` one-shot | Migrations login | Owns the schema objects. Same image as the API; run as a one-shot task with migrations credentials injected only into that task. Never inject this login into the long-lived API. |

Default privileges on the app schema must `GRANT` the runtime login access to tables (and sequences) the migrations login creates. Otherwise DML fails after cutover even when the privilege guard passes.

This repository does not run DDL from the long-lived API outside Development (`DatabaseSchemaBootstrap.ApplyStartupSchemaAsync` no-ops). Schema changes go through `migrate` only.

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
