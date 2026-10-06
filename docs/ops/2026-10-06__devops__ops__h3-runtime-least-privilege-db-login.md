# H3 — Least-privilege runtime DB login

## Deploy order (required)

An image that contains this guard must **not** be baked or pinned live until the runtime DB secret has been cut over from the master login to the least-privilege app login. Pinning the guarded image while the long-lived API still uses the master (or any privileged) login will fail closed at startup.

The H3 role-split design **§7 steps 2–7 are the required runbook**. Do not skip them. Names only (roles and the `dealoware` database / `public` schema). No secret values, secret names, or account IDs.

Required order:

1. **Create roles** — as `dealoware`, create `dealoware_migrate` (owns schema objects; may CREATE / INSERT `__EFMigrationsHistory`) and `dealoware_app` (DML only; **SELECT-only** on `__EFMigrationsHistory`). Both: `NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS`.
2. **B1 SET grant (design §7 step 2)** — immediately: `GRANT dealoware_migrate TO dealoware WITH INHERIT FALSE, SET TRUE;` so `dealoware` can `SET ROLE dealoware_migrate` and `OWNER TO` without inheriting migrate privileges.
3. **PUBLIC lockdown (design §7 step 3)** — `REVOKE ALL ON DATABASE dealoware FROM PUBLIC;` and `REVOKE CREATE ON SCHEMA public FROM PUBLIC;`. Then `GRANT CONNECT` on the database to `dealoware_migrate` and `dealoware_app`; `GRANT USAGE, CREATE ON SCHEMA public` to `dealoware_migrate`; `GRANT USAGE ON SCHEMA public` to `dealoware_app`; `REVOKE CREATE ON SCHEMA public FROM dealoware_app`.
4. **Transfer ownership (design §7 step 4)** — as `dealoware` (after B1), transfer ownership of every existing table and sequence in `public` (including `__EFMigrationsHistory` if present) to `dealoware_migrate`. Mandatory before claiming cutover; migrate cannot ALTER tables it does not own.
5. **Backfill grants (design §7 step 5)** — under `SET ROLE dealoware_migrate;` (or a `dealoware_migrate` session): `GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO dealoware_app;` then `REVOKE INSERT, UPDATE, DELETE ON TABLE public."__EFMigrationsHistory" FROM dealoware_app;` and `GRANT SELECT` only; `GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO dealoware_app;`. Set `ALTER DEFAULT PRIVILEGES FOR ROLE dealoware_migrate` so later tables/sequences get the same DML. `REVOKE TRUNCATE, REFERENCES, TRIGGER` on all tables from `dealoware_app`. `RESET ROLE;`
6. **Baseline existing databases** — deployed DBs were created with Development `EnsureCreated` / schema bootstrap and have **no** `__EFMigrationsHistory` row. The first real incremental migration (A7 PR #20 `20261006000100_AddAdminTablesAndSoftDelete`, open / not modified here) assumes that schema already exists. The `migrate` one-shot stamps `20261005000000_Baseline` only when the 13 baseline tables match the frozen model (column name, type, nullability, and primary keys) and there is no extra table besides `__EFMigrationsHistory`. Partial, unknown, or mismatched schemas fail closed. Fresh empty databases apply the baseline for real. Never at API startup; never as `dealoware_app`.
7. **Run `migrate` (design §7 step 6)** — one-shot (`dotnet Dealoware.Api.dll migrate` or `dotnet run --project src/Dealoware.Api -- migrate`) as `dealoware_migrate` only (same `DB_*` / connection-string keys; migrations credentials injected into that process only).
8. **REVOKE history writes after every migrate (design §7 step 6b)** — as `dealoware_migrate` (`SET ROLE dealoware_migrate` or migrate login): `REVOKE INSERT, UPDATE, DELETE ON TABLE public."__EFMigrationsHistory" FROM dealoware_app;` and `GRANT SELECT ON TABLE public."__EFMigrationsHistory" TO dealoware_app;`. Default privileges would otherwise re-grant writes when the history table is first created. Re-run this after every future migrate.
9. **App-login negative test (design §7 step 7)** — as `dealoware_app`: `CREATE TABLE` must be denied; `INSERT` into `__EFMigrationsHistory` must be denied; `SELECT` / `INSERT` on a business table must succeed. Do this before cutting the API secret over.
10. **Cut the API secret** — point the long-lived API at `dealoware_app` (same `DB_*` / connection-string keys; different credentials).
11. **Pin the guarded image** — only after the runtime secret is the app login.

Alternatively, cut over the API secret and pin the guarded image in **one step**, with a rollback plan (revert the image pin and/or restore the previous runtime secret).

Any image that also includes A7 PR #20 additionally requires `DEALOWARE_ADMIN_IP_HMAC_KEY` to be set in the same deploy. Missing that key fails the admin path closed; it is independent of this DB-login guard.

## Runtime vs migrations login

| Process | Credentials | Privileges |
|---------|-------------|------------|
| Long-lived API | `dealoware_app` | `CONNECT` on the database; `USAGE` on the app schema; `SELECT` / `INSERT` / `UPDATE` / `DELETE` on tables; `USAGE` / `SELECT` on sequences; **SELECT-only** on `__EFMigrationsHistory`. Must **not** own tables (including via membership in `dealoware_migrate` or `dealoware`), must **not** need DDL, and must **not** be superuser, `CREATEROLE`, `CREATEDB`, `BYPASSRLS`, or a member of `rds_superuser`. |
| `migrate` one-shot | `dealoware_migrate` | Owns the schema objects. May `CREATE` / `INSERT` `__EFMigrationsHistory` (baseline stamp + later migrations). Same image as the API; run as a one-shot task with migrations credentials injected only into that task. Never inject this login into the long-lived API. |

Default privileges on the app schema must `GRANT` the runtime login access to tables (and sequences) the migrations login creates. Otherwise DML fails after cutover even when the privilege guard passes.

This repository does not run DDL from the long-lived API outside Development (`DatabaseSchemaBootstrap.ApplyStartupSchemaAsync` no-ops). Schema changes and any `CREATE` / `INSERT` on `__EFMigrationsHistory` go through `migrate` only. The API runtime path does not stamp the baseline, does not call `MigrateAsync`, and does not need DDL.

## Startup guard (long-lived API only)

Active when the provider is PostgreSQL **and** the environment is not Development. Not invoked by `DatabaseMigrateCommand`.

After the `DbContext` is available and before `app.Run`, the API runs a read-only catalog query on the current login and **fails closed** if any of the following is true:

- `rolsuper`
- `rolcreaterole`
- `rolcreatedb`
- `rolbypassrls`
- inherited or SET-only membership of `rds_superuser` via `pg_has_role(..., 'MEMBER')` (if that role does not exist, membership is treated as false)
- membership in any role that owns a table in the app schema (`pg_has_role(current_user, tableowner, 'MEMBER')`, including `dealoware_migrate` or `dealoware`)

Failure logs a safe message (no hosts, users, passwords, or connection strings) and `Environment.Exit(1)` when the entry assembly is `Dealoware.Api`.

## Break-glass

`Database:AllowPrivilegedRuntimeLogin` defaults to **false**. Set it to `true` only as explicit break-glass (for example a documented emergency). Nothing is skipped silently: when the flag is on in a PostgreSQL non-Development process, the API logs that the check was skipped.

Do not use break-glass as the deploy path. Follow **Deploy order (required)** above.
