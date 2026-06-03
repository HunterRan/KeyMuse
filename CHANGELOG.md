# Changelog

## 0.0.5 - 2026-06-03

### Fixes
- Fix HotKeyManager creating visible "HotKeySource" popup window by using HWND_MESSAGE
- Remove MainWindow creation at startup to prevent ghost popup window

## 0.0.4 - 2026-06-03

### Fixes
- Remove MainWindow creation at startup to prevent ghost popup window

## 0.0.3 - 2026-06-03

### Features
- Add RecordingInfo and WorkflowModel data models
- Add RecordingManager with category CRUD and recording file management
- Add WorkflowManager with JSON-based CRUD for workflow files
- Add WorkflowExecutor for sequential step-based workflow execution
- Add tab-based MainWindow with Recordings, Workflows, and Settings tabs
- Add RecordingsPage with category tree, recording list, and record/replay/import/rename/delete/export
- Add WorkflowsPage with step management, reorder, and one-click execution
- Add SettingsPage with profile management, autoclick key/interval/mode config, and storage path display

### Fixes
- Suppress nullable warning in RecordingManager.ListCategories

## 0.0.2 - 2026-06-03

### Features
- Redesign hotkey system: F6-F10 comprehensive shortcuts
- Add auto-clicker with configurable key and interval via F8
- Add global stop-all via F9
- Add window toggle via F10
- Add tray icon with context menu

## 0.0.1 - 2026-06-03

### Features
- Initial recording and replay of keyboard/mouse input
- HUD overlay showing recording/replay status
- Low-level Windows hooks (WH_KEYBOARD_LL, WH_MOUSE_LL)
- Input injection via SendInput
- File format: .keymuse (ZIP with session.json + events.bin)
- Auto-clicker
- Multi-profile configuration system
