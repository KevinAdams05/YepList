# Changelog

All notable changes to YepList will be documented in this file.

## [0.5.3] - 2026-05-13

### Fixed
- **Android**: Sync no longer silently skipped on cold app launch / widget process restart — dropped the `ConnectivityMonitor.isOnline` early-return that raced with `NetworkCallback.onAvailable` (sync now attempts the HTTP call directly and reports `ERROR` if it actually fails)
- **Haiku**: About window now opens from the Program menu (window forwards `B_ABOUT_REQUESTED` to the application)

### Changed
- **Haiku**: Replaced resizable split with a fixed-width 240px sidebar (matches the Windows client's layout)
- **Haiku**: Default window size reduced from 1400×700 to 1000×650
- **Haiku**: Delete key now deletes the selected task (menu accelerator)

### Added
- **Haiku**: Right-click context menu on tasks (Edit, Delete)
- **Haiku**: Sidebar header with YepList name and logo above the list of lists

## [0.5.2] - 2026-05-12

### Security
- **Backend**: Connection string and DB credentials removed from `appsettings.json` (placeholder only); real values live in `appsettings.Production.json` / `appsettings.Local.json` (now gitignored)
- **Backend**: `init.sql` no longer creates the application user with `ALL PRIVILEGES` and a wildcard host; DBA provisions `SELECT/INSERT/UPDATE/DELETE` on `@'localhost'` out-of-band
- **Backend**: Production config file locked down to `root:www-data 640` on the deploy server
- **Backend**: MySqlConnector connection string explicitly pins `AllowLoadLocalInfile=False`, `AllowUserVariables=False`, `Persist Security Info=False`
- **Backend**: `/api/debug/log` and `/api/debug/log/batch` now rate-limited (10 req/min/IP), sanitize CR/LF/control chars to prevent log forging + ANSI injection, use structured logging templates, write to a 10 MB rolling file, and can be disabled via `DebugLogging:Enabled`
- **Backend**: Kestrel hardened — `MaxRequestBodySize` 256 KB (was 28 MB default), `MaxConcurrentConnections` 200, `MaxRequestHeadersTotalSize` 16 KB
- **Backend**: Global rate limiter at 600 req/min/IP
- **Backend**: `UseExceptionHandler` + `AddProblemDetails` always on so unhandled errors return RFC 7807 JSON instead of leaking stack traces
- **Backend**: Removed wildcard CORS (`AllowAnyOrigin`) — no browser client uses the API
- **Backend**: Security response headers added: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`
- **Backend**: `ToggleCompleteRequest.IsCompleted` is now `[Required] bool?` — missing field returns 400 instead of silently toggling to `false`
- **Backend**: Item reparent (`PUT /api/items/{id}`) pre-validates the target list exists; returns 400 instead of a 500 from a raw MySQL FK violation

## [0.5.1] - 2026-05-12

### Added
- **All clients**: Completed tasks now sort to the bottom of the list (Linux, Haiku, Android — matches existing Windows behavior)
- **Android**: Sync on app open and every foreground resume
- **Android**: "+" button on widget header for quick-add new task
- **Android**: Widget background sync interval reduced from 15 min to ~5 min via self-rescheduling work chain
- **Android**: Tapping anywhere on a widget row toggles completion (no longer requires hitting the checkbox precisely)

### Fixed
- **Haiku**: Crash on PATCH requests caused by adding the reserved `Content-Type` header explicitly
- **Haiku**: Window can now be resized freely; default width increased to 1400px
- **Haiku**: Completed task text now legible (blended title color + strikethrough instead of near-invisible background tint)

## [0.5.0] - 2026-04-14

### Added
- **Android**: Widget deep-link — tapping a widget opens the app to that specific list
- **Android**: Edge-to-edge support for Android 16 (keyboard no longer covers input, toolbar no longer overlaps status bar)
- **Android**: Widget config screen now syncs with server before showing lists
- **Android**: Tabbed About dialog with Libraries and Changelog tabs
- **Linux**: Color picker with preset swatches in category manager
- **Linux**: Logo displayed in sidebar above lists
- **Linux**: Settings dialog for configuring server URL
- **Linux**: `.desktop` file for taskbar/launcher icon
- **Linux**: `.deb` package building via deploy script for easy installation on local network
- **Linux**: Tabbed About dialog with Libraries and Changelog tabs
- **Windows**: Settings dialog for configuring server URL
- **Windows**: Default list star indicator in sidebar
- **Windows**: Tabbed About dialog with Libraries and Changelog tabs
- **Windows**: Reusable `MarkdownRenderer` utility class for rich text formatting
- **Haiku**: Native C++ BeAPI client with BSplitView layout (sidebar + task list)
- **Haiku**: Full CRUD for lists, tasks, and categories via REST API
- **Haiku**: Custom BListItem drawing with system colors and font metrics
- **Haiku**: Right-click context menu for list management (rename, delete, set default)
- **Haiku**: Quick-add task bar, task completion toggle, category color swatches
- **Haiku**: 30-second BMessageRunner sync timer with incremental sync
- **Haiku**: Settings dialog for server URL, persisted via BMessage Flatten/Unflatten
- **Haiku**: Tabbed About dialog (About, Libraries, Changelog) with BTabView
- **Haiku**: BHttpSession + BJson networking with worker thread pattern
- **Backend**: Input validation on all API request DTOs (max lengths, required fields, regex patterns)
- **Backend**: `ModelState.IsValid` checks on all controller endpoints
- **Backend**: Debug log endpoint hardened with field length limits and batch size cap (100)
- **Docs**: SVG architecture diagrams in README and documentation
- **Docs**: Screenshots in README
- **Docs**: Warning banner about beta status and local network use
- **Docs**: MIT license
- **Docs**: Changelog file with full version history

### Fixed
- **Android**: Sync status bar no longer causes layout bounce (always reserves space)
- **Android**: Widget `actionStartActivity` import conflict resolved
- **Linux**: Sidebar no longer jumps to top list on refresh (preserves selection)
- **Windows**: Jagged fonts fixed with `HighDpiMode.PerMonitorV2` and explicit font rendering settings
- **Windows**: Bold text no longer bleeds into subsequent lines in changelog rendering

## [0.4.1] - 2026-04-12

### Added
- **Android**: Remote debug logging to server
- **Android**: Home screen widget enhancements (toggle complete, delete from widget)
- **Android**: Configurable sync interval in settings (15s, 30s, 1min, 5min, 15min)
- **All**: About dialogs with version info

### Fixed
- **Android**: Move-task between lists
- **Android**: Sync reliability improvements

## [0.4.0] - 2026-04-11

### Added
- **Android**: Full native client with MVVM architecture
- **Android**: Offline support with local-first Room database and pending operation queue
- **Android**: Home screen widget (Jetpack Glance) with list picker
- **Android**: Multi-select with bulk delete
- **Android**: Drag-and-drop task reordering
- **Android**: Quick-add task bar
- **Android**: Background sync via WorkManager (15-minute interval)
- **Android**: Swipe-to-delete with confirmation
- **Android**: Material Design 3 with dynamic colors

## [0.3.0] - 2026-04-10

### Added
- **Linux**: Multi-select delete, drag-drop reorder, quick-add bar
- **Linux**: Context menus for list management (rename, delete, set default)
- **Linux**: Default list setting stored in local settings
- **Linux**: Category color support
- **Linux**: Remote error logging
- **Windows**: Dark mode with auto-detection and theme restart
- **Windows**: Multi-select delete and drag-drop task reordering
- **Windows**: Quick-add task bar
- **Windows**: Context menus for list management
- **All**: Deploy script for API, Linux client, and Android APK

## [0.2.0] - 2026-04-09

### Added
- **Linux**: Native GTK4 + libadwaita client
- **Linux**: Split pane layout with sidebar and task list
- **Linux**: Category management, task editing, due dates
- **Linux**: Dark mode detection for XFCE via xfconf D-Bus query
- **Windows**: Modern flat UI redesign inspired by libadwaita

## [0.1.0] - 2026-04-08

### Added
- **Backend**: ASP.NET Core REST API with Dapper + MySQL
- **Backend**: CRUD endpoints for lists, categories, and items
- **Backend**: Sync endpoint with timestamp-based polling and deletion tracking
- **Windows**: Initial WinForms + Krypton Toolkit client
- **Windows**: List and task management, category support, 30-second sync
