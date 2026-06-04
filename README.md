# KeyMuse

**Keyboard & Mouse Automation Tool for Windows**

KeyMuse is a lightweight, open-source automation utility for Windows. It provides global keyboard/mouse hooking, event recording with category management, multi-step workflow orchestration, and auto-clicking — all through a modern WPF desktop interface with a customizable HUD overlay.

---

## Features

### 🎣 Global Input Hooking
- Low-level keyboard and mouse hooks (`SetWindowsHookEx` WH_KEYBOARD_LL / WH_MOUSE_LL)
- Captures all input events system-wide without focus requirement
- 500ms heartbeat health monitoring with auto-reconnect on hook failure

### ⏺️ Recording & Replay
- Record keyboard and mouse events to `.keymuse` files (ZIP: `session.json` + `events.bin`)
- **Category-based recording management** — organize recordings into named categories
- **Named saving** — prompt for recording name on stop, default to timestamp if empty
- Three replay loop modes: **Single** / **Count** / **Infinite**
- Event-accurate replay with original timing preserved

### 📋 Workflow System
- **Multi-step workflows** — chain multiple recordings into a single automated sequence
- **Per-step settings** — configure repeat count and interval delay (ms) for each step
- **Workflow repeat modes** — single execution, N times, or infinite loop
- Visual step list with reorder (move up/down), add/remove operations
- Recording browser dialog for step addition (category tree + file list)

### 🔁 Auto Clicker
- Configurable interval-based clicking (≥ 100ms)
- Configurable trigger key (any virtual key code or mouse left button)
- Runs concurrently via coordinated mutex

### 🎨 Theme System
- **3 themes**: Dark / Light / Gray
- **Acrylic glass effect** on Main Window (Win32 DWM API, Windows 11 22H2+)
- Online theme switching without restart

### 🖥️ Modern WPF Desktop UI
- **Main Window** — custom chrome (WindowChrome), title bar buttons, tabbed navigation
- **Pages**: Recordings (category + file list), Workflows (step editor), Settings (theme, storage, auto-clicker)
- **HUD Overlay** — always-on-top status overlay with dynamic content (last 5 recording events / current ±2 replay steps)
- **System Tray Icon** — minimize to tray, quick menu
- **Custom MessageBox** — dark-themed, icon support (Info/Warning/Error/Question), YesNo/OK button modes

### ⚙️ Profile System
- Named profiles at `%APPDATA%\KeyMuse\profiles\`
- Stores theme preference, auto-clicker key code, storage root path
- Full CRUD management from the Settings page

### 🗂️ Storage Management
- Recordings, workflows, and profiles stored at `%APPDATA%\KeyMuse\` by default
- **Configurable storage root** — change via Settings page with folder browser
- All managers (Recording, Workflow, Config) use the same storage root
- Category-based recording organization

---

## Download

Pre-built binaries are available under [Releases](https://github.com/HunterRan/KeyMuse/releases). Each release is a single-file self-contained executable (~160MB) — no runtime dependencies required.

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
dotnet publish src/KeyMuse.Wpf/KeyMuse.Wpf.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

## Usage

1. Launch `KeyMuse.Wpf.exe`
2. The app appears in the system tray and shows the main window
3. **Recordings Page** — select or create a category, click "Record" to capture events, "Stop" to name and save
4. **Workflows Page** — create a workflow, add recorded steps with per-step repeat count and interval
5. **Settings Page** — configure theme, storage path, auto-clicker key code
6. **HUD** — bottom-left overlay shows real-time status (tracking events during recording, step progress during replay)

### Keyboard Shortcuts

| Key  | Action                        |
|------|-------------------------------|
| `F6` | Toggle recording (pick category on start, name on stop) |
| `F7` | Replay selected recording / stop replay (minimizes window on start) |
| `F8` | Execute selected workflow / stop execution (minimizes window on start) |
| `F9` | Toggle auto-clicker           |
| `F10`| Show/hide main window         |
| `INS`| Auto-clicker trigger (configurable in Settings) |

---

## Project Structure

```
KeyMuse/
├── src/
│   ├── KeyMuse.Core/              # Core library (no WPF dependency)
│   │   ├── Models/                # Data models (InputEvent, RecordingSession, etc.)
│   │   └── Services/              # Core services
│   │       ├── HookManager.cs         # Global keyboard/mouse hook
│   │       ├── Recorder.cs            # Event recording & ZIP serialization
│   │       ├── ReplayEngine.cs        # Event replay with loop modes
│   │       ├── AutoClicker.cs         # Interval-based auto-clicker
│   │       ├── InputCoordinator.cs    # Mutex-based input conflict resolution
│   │       ├── ConfigManager.cs       # Profile CRUD (%APPDATA%/KeyMuse)
│   │       ├── RecordingManager.cs    # Category-based recording storage
│   │       ├── WorkflowManager.cs     # Workflow CRUD & step management
│   │       ├── WorkflowExecutor.cs    # Multi-step workflow execution
│   │       └── StatusMessageQueue.cs  # Thread-safe status message queue
│   └── KeyMuse.Wpf/               # WPF desktop application
│       ├── App.xaml(.cs)              # Entry point, global styles, hotkeys, theme
│       ├── MainWindow.xaml(.cs)       # Custom chrome, tabbed navigation, title bar
│       ├── HUDWindow.xaml(.cs)        # Always-on-top status overlay
│       ├── HotKeyManager.cs           # Global hotkey registration (F6-F10)
│       ├── Pages/
│       │   ├── RecordingsPage.xaml(.cs)# Recording categories, file list, record/replay controls
│       │   ├── WorkflowsPage.xaml(.cs) # Workflow step editor with ordering & per-step config
│       │   ├── SettingsPage.xaml(.cs)  # Theme, storage root, auto-clicker config
│       │   └── TextInputDialog.xaml(.cs)# Reusable text input dialog
│       ├── Controls/
│       │   ├── DarkMessageBox.xaml(.cs)# Custom themed MessageBox (icons, buttons)
│       │   ├── CategoryPickerDialog.xaml(.cs)# Category selection dialog (list + new)
│       │   └── RecordingBrowserDialog.xaml(.cs)# Category + recording file picker
│       ├── Helpers/
│       │   └── AcrylicHelper.cs        # Win32 acrylic/mica backdrop for MainWindow
│       └── Themes/                    # Theme resource dictionaries
│           ├── Dark.xaml
│           ├── Light.xaml
│           └── Gray.xaml
├── tests/
│   └── KeyMuse.Tests/             # xUnit tests
│       ├── RecorderTests.cs           # Serialization roundtrip, save/load
│       ├── ReplayEngineTests.cs       # Loop mode behavior
│       ├── ConfigManagerTests.cs      # Profile CRUD operations
│       ├── AutoClickerTests.cs        # Start/stop lifecycle
│       ├── HookManagerTests.cs        # Lifecycle & disposal
│       └── InputCoordinatorTests.cs   # Mutex behavior
├── assets/                        # Icon assets (SVG, ICO, PNG)
├── KeyMuse.sln
├── README.md
├── README.zh-CN.md
└── LICENSE
```

---

## Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Hook thread communication | `ConcurrentQueue<T>` | Decoupled, thread-safe, no shared state |
| Input conflict resolution | `SemaphoreSlim` mutex | Lightweight, async-compatible mutual exclusion |
| Recording file format | ZIP (`session.json` + `events.bin`) | Portable, human-readable metadata, compact binary events |
| Recording storage | Category subdirectories under `recordings/` | Organized by user-defined groups, scalable |
| Workflow storage | JSON files under `workflows/` | Simple, human-readable, easy to backup |
| Profile storage | `%APPDATA%/KeyMuse/profiles/` | Windows standard, user-scoped |
| Replay loop modes | Single / Count / Infinite | Common automation patterns |
| Window chrome | `shell:WindowChrome` | Native resize/snap behavior while custom title bar |
| Theme system | Runtime ResourceDictionary merge | Instant switching, no restart needed |
| Acrylic backdrop | `DwmSetWindowAttribute` / `SetWindowCompositionAttribute` | Modern Windows visual effect with fallback |
| Hook health check | 500ms heartbeat timer | Early detection and auto-recovery of hook failure |
| Atomic file write | Temp file → rename on completion | Prevents corruption from crash during write |

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

## Disclaimer

KeyMuse is a general-purpose automation tool. Use responsibly in accordance with applicable terms of service of the software you interact with. The authors assume no liability for misuse.
