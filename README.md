# YepList

> [!WARNING]
> This is beta code, use at your own risk. Data is not encrypted in the database and SSL is not forced. Intended for local network traffic only. Don't store any top secret data in this app.

A selfhosted cross-platform To-Do list app with a local server back-end. The backend consists of MySQL and C#/ASP.NET Core. Clients are available for Linux, Windows, Haiku, and Android. 

The Android app has local caching, so if you add items when you are away from your network they will get synced once you are connected. 


## Screenshots
**Linux**

![linux screenshot](resources/linuxscreenshot.png)


**Windows**

![windows screenshot](resources/windowsscreenshot.png)

**Android**

<img src="resources/AndroidScreenshot.jpg" alt="android screenshot" width="300">

**Haiku**

<img src="resources/HaikuScreenshot.png" alt="Haiku screenshot" width="600">

## Architecture

YepList uses a shared REST API backend with four native clients that sync via timestamp-based polling.

<p align="center">
  <img src="resources/architecture.svg" alt="YepList Architecture" width="640">
</p>

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Backend API | ASP.NET Core 10 + Dapper |
| Database | MySQL 8.0 |
| Windows Client | C# WinForms + Krypton Toolkit |
| Linux Client | Vala + GTK4 + libadwaita |
| Haiku Client | C++ + Haiku Interface Kit (BeAPI) |
| Android Client | Kotlin + Retrofit + Room + Jetpack Glance |

## Features

- Create and manage multiple task lists
- Organize tasks with color-coded categories
- Set due dates on tasks
- Drag-and-drop task reordering
- Multi-select and bulk delete
- Quick-add task bar
- Cross-platform sync (changes appear on all clients within seconds)
- Android home screen widget with tap-to-complete and delete
- Offline support on Android (local-first with sync queue)
- Dark mode support on all platforms
- Default list setting per client

## Getting Started

### Prerequisites

- **Backend**: .NET 10 SDK, MySQL 8.0
- **Windows Client**: .NET 10 SDK (Windows)
- **Linux Client**: Vala compiler, GTK4, libadwaita, libsoup-3.0, json-glib-1.0, Meson, Ninja
- **Haiku Client**: Haiku R1/beta5 with the development tools (or a Haiku cross-build toolchain)
- **Android Client**: Android Studio with JDK 21+

### Server Setup

1. Install MySQL 8.0 and .NET 10 runtime on your Linux server
2. Run the database schema:
   ```bash
   mysql -u root -p < backend/src/ToDoList.Data/Schema/init.sql
   ```
3. Update the connection string in `backend/src/ToDoList.Api/appsettings.json`
4. Publish and deploy the API:
   ```bash
   cd backend
   dotnet publish src/ToDoList.Api -c Release -o publish
   ```
5. The API listens on `http://0.0.0.0:5000` by default

### Building the Clients

**Windows:**
```bash
cd clients/windows
dotnet build src/ToDoList.Windows
```

**Linux:**
```bash
cd clients/linux
meson setup builddir
cd builddir
ninja
./yep-list --server http://your-server:5000
```

**Haiku:** (build natively on Haiku)
```bash
cd clients/haiku
make
# Or produce a standalone .hpkg: package/build-hpkg.sh
```

**Android:**
```bash
cd clients/android
./gradlew assembleDebug
# APK at app/build/outputs/apk/debug/app-debug.apk
```

## Documentation

See the [full documentation](docs/GUIDE.md) for detailed information on the data model, API reference, sync strategy, client architecture, deployment, and more.

## Project Structure

```
ToDoList/
  backend/
    ToDoList.sln
    src/
      ToDoList.Core/       # Models, DTOs, interfaces
      ToDoList.Data/       # Dapper repositories, DB schema
      ToDoList.Api/        # ASP.NET Core Web API
  clients/
    windows/               # C# WinForms + Krypton
    linux/                 # Vala + GTK4 + libadwaita
    haiku/                 # C++ + Haiku Interface Kit (BeAPI)
    android/               # Kotlin + Retrofit + Room
  resources/               # Logos and icons
  docs/                    # Documentation
```

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a full list of changes by version.

## License

This project is licensed under the [MIT License](LICENSE).
