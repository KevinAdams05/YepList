# Changelog

All notable changes to YepList will be documented in this file.

## [0.5.0] - 2026-04-14

### Added
- **Android**: Widget deep-link — tapping a widget opens the app to that specific list
- **Android**: Edge-to-edge support for Android 16 (keyboard no longer covers input, toolbar no longer overlaps status bar)
- **Android**: Widget config screen now syncs with server before showing lists
- **Linux**: Color picker with preset swatches in category manager
- **Linux**: Logo displayed in sidebar above lists
- **Linux**: Settings dialog for configuring server URL
- **Linux**: `.desktop` file for taskbar/launcher icon
- **Linux**: `.deb` package building via deploy script for easy installation on local network
- **Windows**: Settings dialog for configuring server URL
- **Windows**: Default list star indicator in sidebar
- **Docs**: SVG architecture diagrams in README and documentation
- **Docs**: Screenshots in README
- **Docs**: Warning banner about beta status and local network use
- **Docs**: MIT license

### Fixed
- **Android**: Sync status bar no longer causes layout bounce (always reserves space)
- **Android**: Widget `actionStartActivity` import conflict resolved
- **Linux**: Sidebar no longer jumps to top list on refresh (preserves selection)
- **Windows**: Jagged fonts fixed with `HighDpiMode.PerMonitorV2` and explicit font rendering settings

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
