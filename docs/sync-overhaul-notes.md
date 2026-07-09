# Sync Overhaul — Working Notes

> Living document. Started 2026-06-23. Captures investigation, decisions, and plan
> for fixing sync reliability + adding diagnostics/logging + soft-delete with provenance.

## Reported problems

1. **Widgets + main app not auto-refreshing on Taylor's Phone** (Galaxy S23, OneUI 8.5,
   Android 16). Kevin's phone (Galaxy S22+, OneUI 5.1, Android 13) does not show this.
2. **Stale-overwrite data loss**: Kevin's phone had the most recent list; Taylor's phone
   had a stale version. Taylor's phone synced and *overwrote the correct list with her
   stale version*. Classic last-write-wins with no conflict detection.
3. Want **server-side diagnostics/logging** to see what is refreshed and when.
4. Want a **server-side log viewer**.
5. Want **soft-delete**: keep deleted items in DB, flag as deleted, record which device
   deleted them + timestamp.

## Working hypotheses

- **#1 (widgets)** is a *client-side Android background-execution* problem. Android 16 /
  OneUI 8.5 is far more aggressive at killing background work (WorkManager / AlarmManager /
  FCM / battery optimization) than Android 13. Likely independent of the data-overwrite bug.
- **#2 (overwrite)** is the dangerous one — needs per-item versioning/timestamps so the
  server can reject or merge stale writes instead of blindly accepting them.
- **#5 (soft-delete)** also fixes a latent sync bug: hard-deletes can't propagate, so a
  delete on one device can be "resurrected" by another device that still holds the item.

## Open questions for Kevin

- Same-item conflict: newest-timestamp-wins, or surface a conflict to the user?
- Log viewer: authenticated web page served by the API, or just tailable log files?

## Investigation findings

### Sync mechanism
- **Endpoint**: `GET /api/sync?since=<ISO8601>` → `SyncResponseDto { ServerTime, Lists,
  Categories, Items, DeletedListIds, DeletedItemIds, DeletedCategoryIds }`.
  - `SyncController.cs`; repos expose `GetModifiedSinceAsync` (`WHERE modified_date > @Since`).
- **Pull**: server is source of truth. Client `SyncRepository.pullFromServer()` **upserts
  everything returned** and deletes the returned deleted-IDs. Stores `lastSyncTime = ServerTime`.
- **Push**: client keeps a `pending_operations` FIFO queue (create/update/delete/toggle/
  reorder) and replays it against the normal mutation endpoints. Temp IDs are negative,
  remapped to server IDs on create.
- **Conflict resolution**: last-write-wins via `modified_date`, which MySQL auto-stamps with
  `ON UPDATE CURRENT_TIMESTAMP` — i.e. **at server receipt time, not at user edit time.**

### >>> Root cause of the stale-overwrite (issue #2)
`modified_date` is stamped when the **server processes** the write, not when the user made
the edit. So a stale edit that is pushed *later* gets a *newer* timestamp and wins:
1. Kevin edits at 10:00 → server stamps 10:00.
2. Taylor's phone (holding stale data / queued stale ops) syncs at 11:00 → server stamps
   11:00 → Taylor's stale content is now "newest" → wins.
3. Kevin's next pull overwrites his good data with Taylor's stale version.
The server cannot distinguish a stale edit from a fresh one. **Fix requires the client's
real edit time and/or a version baseline** (see Plan Phase 1).

### Soft-delete — already partially exists
- Deletes are **hard deletes** of the main row + a tombstone row inserted into
  `deleted_entity (entity_type, entity_id, deleted_date)`. No device, no retained data.
- User wants the **row kept**, flagged deleted, with **deleting device + timestamp**.

### Logging — already partially exists
- `POST /api/debug/log` + `/batch` → text file `./debug.log` (10 MB rotate, 1 backup),
  stricter rate-limit policy "debug" (10/min). Android `RemoteLogger` batches logs and
  already includes `Build.MANUFACTURER`/`MODEL`.
- **Missing**: DB-backed/queryable log, a viewer, and any *sync-specific* audit
  ("what refreshed, when, from which device").

### Cross-cutting facts that shape the design
- **No authentication anywhere.** LAN-only; protected by rate limiting + Kestrel caps.
  → A log viewer needs its own auth decision (can't inherit any).
- **No migration runner.** `init.sql` is `CREATE TABLE IF NOT EXISTS`, applied manually.
  → Schema changes need `ALTER` statements; consider a tiny ordered-`.sql` runner at startup.
- **No device identity** sent on requests today.
- `DeletedEntityRepository.PurgeOlderThanAsync(days)` exists but **nothing schedules it.**

### Client plumbing facts (for the all-together rollout)
- **Android**: offline-first, SQLite/Room, pending-ops queue, temp negative IDs. Local
  entities carry `modifiedDate` (string). Must stamp `clientModifiedDate` = real local edit
  time when queuing edits, and send `X-Device-Id`/`X-Device-Name` on sync+mutation calls.
- **Windows**: **in-memory only, online-first**, no offline queue, writes **directly** to the
  API at edit time, 30 s timer sync + manual + on-show full refresh. So it can send
  `clientModifiedDate = DateTime.UtcNow` at the moment of the call, and device headers from
  `Environment.MachineName` + a stable GUID stored in `settings.json`.
- Both clients already send device info to `/api/debug/log` via RemoteLogger, but **not** to
  the sync/mutation endpoints.

### Constraint: app DB user has no DDL
`init.sql` grants the app user only SELECT/INSERT/UPDATE/DELETE. ⇒ no startup auto-migrator;
schema changes ship as **admin-applied migration `.sql`** + updated `init.sql`.

### Android background / widget refresh (issue #1)
- Widgets are **Glance**, refreshed **on-demand only** at the end of `SyncManager.sync()`.
- Background sync = a **self-rescheduling OneTimeWorkRequest chain** (5 min) + a 30 s
  foreground loop. No FCM, no PeriodicWorkRequest, no AlarmManager.
- **Hypothesis**: on Android 16 / OneUI 8.5 the self-chain + foreground loop get killed by
  App Standby buckets / Doze / OneUI "Sleeping apps" once the app isn't actively used, so
  neither the app nor widgets refresh. Kevin's Android 13 is more lenient. Partly a device
  *settings* issue (Sleeping apps / battery optimization), partly needs a durable scheduler.

## Plan (phased)

> Ordering: server-side data-integrity first (stops data loss), then observability, then the
> Android background-refresh rework. KISS/YAGNI per coding standards.

**Phase 0 — Migrations.** Add `ALTER`-based migration `.sql` files + a minimal startup
runner (or documented manual apply) so existing DBs get new columns/tables. Update `init.sql`.

**Phase 1 — Item tracking + conflict safety (fixes data loss).** Add `version INT` and a
client-supplied edit timestamp to item/list/category. Strategy = DECISION (see below).
Stop using receipt-time `modified_date` as the conflict arbiter.

**Phase 2 — Soft-delete with provenance.** Add `is_deleted`, `deleted_date`,
`deleted_by_device` to the three tables; change deletes to `UPDATE` not `DELETE`; derive
sync deleted-IDs from flagged rows; retire `deleted_entity` writes.

**Phase 3 — Device identity.** Clients send `X-Device-Id` (stable UUID) + `X-Device-Name`
("Taylor's Phone"). Server `device` table (id, name, platform, first_seen, last_seen),
upserted via middleware. Feeds provenance + audit log.

**Phase 4 — Sync audit log.** `sync_log` table (timestamp, device, action, entity_type,
entity_id, since, returned counts, detail). SyncController logs pulls; mutation endpoints
log writes. Answers "what refreshed, when, by whom."

**Phase 5 — Server-side log viewer.** Minimal HTML page served by the API over `sync_log`
+ `debug.log`, with filters (device/action/time). Auth = DECISION (see below).

**Phase 6 — Retention / purge job.** `IHostedService` daily timer purging `sync_log` and
soft-deleted rows older than `Retention:Days` (default **90**), gated by `Retention:Enabled`.
Wire up the existing `PurgeOlderThanAsync`.

**Phase 7 — Android durable background + widget refresh (issue #1).** Add a real
`PeriodicWorkRequest` baseline; drive widget updates from it so they refresh without the app
open; add a battery-optimization-exemption prompt; document OneUI "Sleeping apps" exclusion
for Taylor's Phone. (Real-time FCM push = deferred / optional later.)

## Decisions made (2026-06-23)
1. **Conflict strategy = Newest edit-time wins.** Clients send the real user edit timestamp
   (`clientModifiedDate`). Server applies a write only if `@clientModifiedDate >= modified_date`
   (0 rows affected ⇒ stale ⇒ ignored). No conflict UI. Fixes the overwrite bug.
2. **Log-viewer auth = Localhost-only.** Viewer + its data endpoint reject non-loopback
   remote IPs. View via the server box or an SSH tunnel. No secret to manage.
3. **Sequencing = All together.** Backend + Android + Windows in one push, Phases 0–7.
   (FCM real-time push still deferred unless requested.)

## Progress

### Backend — DONE (Phases 0–6), builds clean (.NET 10, 0 warnings)
- **Phase 0**: `init.sql` updated; `Schema/Migrations/001_sync_overhaul.sql` + Migrations
  `README.md` added. ⚠️ **Operational step**: apply the migration as a DB admin
  (`mysql -u root -p yeplist < 001_sync_overhaul.sql`) — the app user has no DDL grant.
- **Phase 1**: `version` + `client_modified_date` on all three tables; repo updates are
  guarded `WHERE @ClientModifiedDate >= client_modified_date` → returns
  `(WriteOutcome, entity)`; controllers return server state on stale and 404 on notfound.
  `modified_date` stays the pull cursor (unchanged).
- **Phase 2**: `is_deleted/deleted_date/deleted_by_device`; deletes are now `UPDATE`s
  stamped with `X-Device-Name`; deleted-IDs derived from flags; `deleted_entity` no longer
  written (kept for legacy). `GetById`/`GetAll` exclude deleted.
- **Phase 3**: `DeviceTrackingMiddleware` reads `X-Device-Id/Name/Platform`, upserts the
  `device` table, stashes identity on `HttpContext` (`GetDeviceId/GetDeviceName`).
- **Phase 4**: `sync_log` table + `SyncLogRepository`; pulls and every write are audited
  (device, action, entity, result=applied/stale/notfound, pull counts). Best-effort (never
  fails the request).
- **Phase 5**: `LogViewerController` at `/admin/logs` (HTML + `/data` + `/devices` +
  `/debug`), **loopback-only** (404 otherwise). Single-page viewer with device/action/time
  filters, stale rows highlighted, debug.log tail.
- **Phase 6**: `RetentionService` (daily) purges sync_log + soft-deleted rows + legacy
  tombstones older than `Retention:Days` (default 90), gated by `Retention:Enabled`. Config
  added to `appsettings.json`.

### Windows client — DONE (type-checks with EnableWindowsTargeting on Linux)
- `AppSettings.DeviceId` (generated GUID, persisted) + `EnsureDeviceId()`.
- `TodoApiClient` ctor sends `X-Device-Id/Name/Platform` headers; all create/update/toggle
  bodies carry `clientModifiedDate = DateTime.UtcNow`.
- Models gained `Version` + `ClientModifiedDate`.

### Android client — DONE (now compiles on Linux — see build env below; `YepList.apk` built)
- Conflict: `clientModifiedDate` added to the 5 request DTOs; repos stamp it with the real
  local edit time (the same `now` used for the entity's modifiedDate). **Room entities left
  unchanged on purpose** — avoids a destructive Room migration; Gson ignores the extra
  `version`/`clientModifiedDate` the server now returns.
- Device: `DeviceHeaderInterceptor` + `AppContainer.deviceId` (UUID in prefs) / `deviceName`
  (user's device name e.g. "Taylor's Phone", else manufacturer+model). Wired through
  `NetworkModule.createApiService`.
- Phase 7: `SyncScheduler.schedulePeriodicBackstop()` adds a durable 15-min PeriodicWorkRequest
  (KEEP) alongside the 5-min chain; `schedulePeriodicSync()` now starts both.
  `BatteryOptimizationHelper` + one-time prompt in `MainActivity`; manifest permission +
  strings added.

## Operational steps required (Kevin)
1. **Apply the DB migration** as an admin on the server:
   `mysql -u root -p yeplist < backend/src/ToDoList.Data/Schema/Migrations/001_sync_overhaul.sql`
2. **Build/deploy the backend** (builds clean on the dev server, .NET 10).
3. **Build the Android app on Windows** (Android SDK there) — couldn't compile on this Linux box.
4. **On Taylor's S23**: tap *Allow* on the new battery prompt AND exclude YepList from
   Settings → Battery → *Sleeping apps* / *Deep sleeping apps* (device setting, not code).
5. **Log viewer** is localhost-only: browse `http://localhost:5000/admin/logs` on the server
   box, or via SSH tunnel `ssh -L 5000:localhost:5000 kevin@192.168.74.122`.

## Where we left off (2026-06-23) — STOPPING POINT

**Code complete; all three components build. Not yet deployed or device-tested.**
- Backend (.NET 10): builds clean (0 warnings).
- Windows client: type-checks (`-p:EnableWindowsTargeting=true`).
- Android client: builds on Linux → `app/build/outputs/apk/debug/YepList.apk`.
- Linux build env set up (userspace JDK 21 + Android SDK); `clients/android/build-android.sh`
  added. Windows→Linux fixes committed: `gradle.properties` (removed `org.gradle.java.home`),
  `gradlew` (DEFAULT_JVM_OPTS quoting), `local.properties` (sdk.dir).

**Next time, to finish the rollout** (see "Operational steps required" above):
1. Apply `Schema/Migrations/001_sync_overhaul.sql` as a DB admin.
2. Deploy the backend; build/install the Android APK; rebuild Windows on a Windows box (or
   keep type-checking here — it won't run on Linux).
3. On Taylor's S23: accept the battery prompt + exclude from "Sleeping apps".
4. Reproduce the original overwrite scenario and confirm the stale edit now loses; watch
   `/admin/logs` for `stale`-result rows as proof.

**UI fix (2026-06-24):** header overlap on Taylor's S23 — the status-bar inset was padded
onto the fixed-height `MaterialToolbar`, clipping the title/overflow. Moved the inset padding
to the `AppBarLayout` (gave it `@+id/appBar`); toolbar keeps full `actionBarSize`. Rebuilt OK.

**Not done / deferred:** FCM real-time push (still polling-based); no automated tests added.

## Decisions log

- 2026-06-23: Treat the report as distinct workstreams (data integrity, soft-delete,
  device identity, observability, retention, Android background refresh) — not one "sync" fix.
- 2026-06-23: Retention default **90 days**, configurable (per user). Single `Retention:Days`
  + `Retention:Enabled` rather than per-type knobs (KISS) unless asked otherwise.
- 2026-07-09: This work ships as **0.5.4**. Version bumped across all components (backend
  csprojs, Linux `meson.build` + About dialog, Android `build.gradle.kts` versionCode 5→6 /
  versionName, Haiku `.rdef` + About window, Windows csproj) and the `CHANGELOG.md` copies.
  Marked **Unreleased** in the changelog — more changes may land before 0.5.4 is cut, and it
  is intentionally **not committed or released** yet.
