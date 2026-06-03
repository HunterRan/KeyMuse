# KeyMuse

**Keyboard & Mouse Automation Tool for Windows**

KeyMuse is a lightweight, open-source automation utility designed for Windows. It provides global keyboard/mouse hooking, event recording and replay, and auto-clicking — all through a clean WPF desktop interface with a minimal HUD overlay.

---

## Features

### 🎣 Global Input Hooking
- Low-level keyboard and mouse hook using `SetWindowsHookEx` (WH_KEYBOARD_LL / WH_MOUSE_LL)
- Captures all input events system-wide without focus requirement
- Automatic health monitoring via 500ms heartbeat timer
- Auto-reconnect on hook disconnection

### ⏺️ Recording & Replay
- Record all keyboard and mouse events to a `.keymuse` file (ZIP archive containing `session.json` + `events.bin`)
- Replay recorded sessions with three loop modes:
  - **Single** — play once
  - **Count** — play N times with configurable interval
  - **Infinite** — loop until manually stopped
- Event-accurate replay with original timing preserved

### 🔁 Auto Clicker
- Configurable interval-based auto-clicking (≥ 100ms)
- Triggered via VK_INSERT by default
- Runs concurrently with recording/replay via coordinated mutex

### 🖥️ WPF Desktop UI
- **Main Window** — profile management, record/replay controls, auto-clicker config, recording load/open
- **HUD Overlay** — always-on-top bottom-left status display, shows messages from the status queue
- **System Tray Icon** — minimize to tray, quick menu for show/exit

### ⚙️ Profile System
- Named profiles stored at `%APPDATA%\KeyMuse\profiles\`
- Each profile stores auto-clicker interval and per-user preferences
- Built-in CRUD management from the main window

---

## Download

Pre-built binaries are available under [Releases](https://github.com/HunterRan/KeyMuse/releases). Each release is a single-file self-contained executable (~150MB) — no runtime dependencies required.

---

## Build from Source

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Windows 10 / Windows 11 (low-level hooks require Windows)

### Clone & Build

```bash
git clone https://github.com/HunterRan/KeyMuse.git
cd KeyMuse
dotnet restore
dotnet build --configuration Release
dotnet test
dotnet publish src/KeyMuse.Wpf/KeyMuse.Wpf.csproj -c Release -o ./publish
```

---

## Usage

1. Launch `KeyMuse.Wpf.exe`
2. The app appears in the system tray and shows the main control window
3. **Profile** — select or create a named profile for your settings
4. **Record** — click "Record" to start capturing events, click "Stop" to save
5. **Replay** — select a recorded `.keymuse` file (via Load or from a recent recording), choose loop mode, click "Play"
6. **Auto Clicker** — set interval (ms), click "Start" to begin (VK_INSERT trigger)
7. **HUD** — the overlay in the bottom-left corner shows real-time status
8. **Stop** — stop replay or auto-clicker at any time

### Keyboard Shortcuts

| Key  | Action                     |
|------|----------------------------|
| `F6` | Toggle recording           |
| `F7` | Play last recording / stop replay |
| `F8` | Toggle auto-clicker        |
| `F9` | Emergency stop all tasks   |
| `F10`| Show/hide main window      |
| `INS`| Auto-clicker trigger (configurable) |

---

## Project Structure

```
KeyMuse/
├── src/
│   ├── KeyMuse.Core/           # Core library (no WPF dependency)
│   │   ├── Models/             # Data models (InputEvent, RecordingSession, etc.)
│   │   └── Services/           # Core services
│   │       ├── HookManager.cs      # Global keyboard/mouse hook
│   │       ├── Recorder.cs         # Event recording & ZIP serialization
│   │       ├── ReplayEngine.cs     # Event replay with loop modes
│   │       ├── AutoClicker.cs      # Interval-based auto-clicker
│   │       ├── InputCoordinator.cs # Mutex-based input conflict resolution
│   │       ├── ConfigManager.cs    # Profile CRUD (%APPDATA%/KeyMuse)
│   │       └── StatusMessageQueue.cs # Thread-safe status message queue
│   └── KeyMuse.Wpf/            # WPF desktop application
│       ├── App.xaml.cs             # Entry point, tray icon, window management
│       ├── MainWindow.xaml(.cs)    # Control panel UI
│       ├── HUDWindow.xaml(.cs)     # Always-on-top status overlay
│       └── HotKeyManager.cs        # Global hotkey registration (F6-F10)
├── tests/
│   └── KeyMuse.Tests/          # xUnit tests (26 tests)
│       ├── RecorderTests.cs        # Serialization roundtrip, save/load
│       ├── ReplayEngineTests.cs    # Loop mode behavior
│       ├── ConfigManagerTests.cs   # Profile CRUD operations
│       ├── AutoClickerTests.cs     # Start/stop lifecycle
│       ├── HookManagerTests.cs     # Lifecycle & disposal
│       └── InputCoordinatorTests.cs # Mutex behavior
├── assets/                     # Icon assets (SVG, ICO, PNG)
├── KeyMuse.sln
├── README.md
├── README.zh-CN.md
└── LICENSE
```

---

## Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Hook thread communication | `ConcurrentQueue<T>` (message queue) | Decoupled, thread-safe, no shared state |
| Input conflict resolution | `SemaphoreSlim` mutex | Lightweight, async-compatible mutual exclusion |
| Recording file format | ZIP (`session.json` + `events.bin`) | Portable, human-readable metadata, compact binary events |
| Profile storage | `%APPDATA%/KeyMuse/profiles/` | Standard Windows convention, user-scoped |
| Replay loop modes | Single / Count / Infinite | Common automation patterns |
| Hook health check | 500ms heartbeat timer | Early detection and auto-recovery of hook failure |
| Atomic file write | Temp file → rename on completion | Prevents corruption from crash during write |

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

## Disclaimer

KeyMuse is a general-purpose automation tool. Use responsibly in accordance with applicable terms of service of the software you interact with. The authors assume no liability for misuse.
