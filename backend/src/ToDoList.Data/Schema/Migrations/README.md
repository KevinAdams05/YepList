# Database migrations

The application database user is granted only `SELECT/INSERT/UPDATE/DELETE`
(no DDL — see `../init.sql`). The API therefore does **not** run migrations at
startup. Schema changes ship here as numbered scripts and are applied by an
admin (a user with `ALTER`/`CREATE`/`INDEX` privileges).

## Applying

Apply migrations in numeric order, once each, against the target database:

```sh
mysql -u root -p yeplist < 001_sync_overhaul.sql
```

Fresh installs run `../init.sql` instead, which already contains every change
up to the latest migration — so a brand-new database does **not** need the
migration scripts.

## Conventions

- Files are `NNN_short_description.sql`, zero-padded, applied in order.
- Each script must be **idempotent** (guarded `IF NOT EXISTS` / conditional
  `UPDATE`s) so a re-run is a safe no-op.
- Requires MySQL 8.0+ (`ADD COLUMN IF NOT EXISTS` / `ADD INDEX IF NOT EXISTS`).

## History

| Script | Summary |
|--------|---------|
| `001_sync_overhaul.sql` | Conflict tracking (`client_modified_date`, `version`), soft-delete provenance (`is_deleted`, `deleted_date`, `deleted_by_device`), `device` registry, `sync_log` audit trail. |
