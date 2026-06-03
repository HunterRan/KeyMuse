# KeyMuse

**Windows 键盘鼠标自动化工具**

KeyMuse 是一款轻量级开源 Windows 自动化工具。提供全局键盘鼠标钩子、事件录制回放、连点等功能，配有简洁的 WPF 桌面界面和 HUD 状态叠加层。

---

## 功能

### 全局输入钩子
- 基于 `SetWindowsHookEx` 的低级键盘（WH_KEYBOARD_LL）和鼠标（WH_MOUSE_LL）钩子
- 全系统范围捕获输入事件，无需窗口焦点
- 500ms 心跳定时器自动监控钩子健康状态
- 钩子断开时自动重连

### 录制与回放
- 录制键盘鼠标事件到 `.keymuse` 文件（ZIP 压缩包，内含 `session.json` + `events.bin`）
- 三种回放循环模式：
  - **单次** — 播放一次
  - **N 次** — 播放指定次数，可配置间隔
  - **无限** — 循环直到手动停止
- 保持原始时间精度的事件回放

### 连点器
- 可配置间隔的自动按键（≥ 100ms）
- 默认触发键：VK_INSERT
- 通过协调互斥锁与录制/回放同时运行

### WPF 桌面界面
- **主窗口** — 配置管理、录制/回放控制、连点设置、录制文件加载
- **HUD 叠加层** — 置顶左下角状态显示，实时读取消息队列
- **系统托盘图标** — 最小化到托盘，右键菜单快速显示/退出
- **托盘悬浮提示** — 显示快捷键参考

### 全局快捷键

| 键 | 功能 | 说明 |
|----|------|------|
| **F6** | 录制 | 空闲→开始录制，录制中→停止并保存 |
| **F7** | 回放 | 有录制文件→播放，回放中→停止 |
| **F8** | 连点 | 开关连点器 |
| **F9** | 急停 | 停止所有任务（录制+回放+连点） |
| **F10** | 窗口 | 显示/隐藏主面板 |

### 配置系统
- 配置文件存储在 `%APPDATA%\KeyMuse\profiles\`
- 每个配置保存连点器间隔、快捷键等偏好设置
- 支持内置 CRUD 管理（主窗口操作）
- 支持导入/导出（`.keymuse-profile` 格式）

---

## 下载

预编译二进制文件在 [Releases](https://github.com/HunterRan/KeyMuse/releases) 页面。每个版本为单文件自包含可执行文件（~155MB），无需安装运行时。

---

## 从源码构建

### 前置要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本
- Windows 10 / Windows 11（低级钩子需要 Windows）

### 克隆与构建

```bash
git clone https://github.com/HunterRan/KeyMuse.git
cd KeyMuse
dotnet restore
dotnet build --configuration Release
dotnet test
dotnet publish src/KeyMuse.Wpf/KeyMuse.Wpf.csproj -c Release -o ./publish
```

---

## 使用说明

1. 启动 `KeyMuse.Wpf.exe`
2. 程序出现在系统托盘并显示主控制窗口
3. **配置** — 选择或创建命名配置
4. **录制** — 点击"录制"开始捕获事件，点击"停止"保存（或按 **F6**）
5. **回放** — 通过 F7 快速回放上次录制，或在主窗口加载 `.keymuse` 文件详细设置
6. **连点** — 设置间隔（ms），点击"启动"（或按 **F8**）
7. **HUD** — 左下角叠加层实时显示状态
8. **急停** — 随时按 **F9** 停止所有任务
9. **窗口** — 按 **F10** 切换主面板显示/隐藏

---

## 项目结构

```
KeyMuse/
├── src/
│   ├── KeyMuse.Core/           # 核心库（无 WPF 依赖）
│   │   ├── Models/             # 数据模型（InputEvent, RecordingSession 等）
│   │   └── Services/           # 核心服务
│   │       ├── HookManager.cs      # 全局键盘鼠标钩子
│   │       ├── Recorder.cs         # 事件录制与 ZIP 序列化
│   │       ├── ReplayEngine.cs     # 事件回放（单次/N次/无限）
│   │       ├── AutoClicker.cs      # 间隔连点器
│   │       ├── InputCoordinator.cs # 基于互斥锁的输入冲突协调
│   │       ├── ConfigManager.cs    # 配置管理（%APPDATA%/KeyMuse）
│   │       └── StatusMessageQueue.cs # 线程安全状态消息队列
│   └── KeyMuse.Wpf/            # WPF 桌面应用程序
│       ├── App.xaml.cs             # 入口点、托盘图标、窗口管理
│       ├── MainWindow.xaml(.cs)    # 控制面板 UI
│       ├── HUDWindow.xaml(.cs)     # 置顶状态叠加层
│       └── HotKeyManager.cs        # 全局快捷键管理
├── tests/
│   └── KeyMuse.Tests/          # xUnit 单元测试（26 个）
├── assets/                     # 图标资源（SVG / ICO / PNG）
├── KeyMuse.sln
├── README.md
└── LICENSE
```

---

## 架构决策

| 决策 | 选择 | 理由 |
|------|------|------|
| 钩子线程通信 | `ConcurrentQueue<T>` 消息队列 | 解耦、线程安全、无共享状态 |
| 输入冲突解决 | `SemaphoreSlim` 互斥锁 | 轻量、异步兼容的互斥机制 |
| 录制文件格式 | ZIP（`session.json` + `events.bin`） | 可移植、元数据可读、事件数据紧凑 |
| 配置存储 | `%APPDATA%/KeyMuse/profiles/` | Windows 标准约定，用户级作用域 |
| 回放循环模式 | 单次 / N次 / 无限 | 覆盖常见自动化场景 |
| 钩子健康检查 | 500ms 心跳定时器 | 早期发现钩子断开并自动恢复 |
| 原子写入 | 临时文件 → 重命名完成 | 防止写入崩溃导致文件损坏 |
| 全局快捷键 | `RegisterHotKey` Win32 API + HwndSource | 系统级热键，后台无焦点也可用 |

---

## 常见问题

### 为什么需要管理员权限？
默认不需要。但如果目标程序以管理员权限运行，`SendInput` 会被 UIPI 阻止。此时可通过右键托盘图标菜单或代码中调用 `RestartAsAdmin()` 提权。

### .keymuse 文件是什么格式？
标准 ZIP 压缩包。可以直接用 WinRAR/7-Zip 打开查看内部文件。

### 快捷键和其他程序冲突怎么办？
目前快捷键为固定 F6-F10，后续版本将支持自定义。

---

## 许可

本项目基于 MIT License 开源。详见 [LICENSE](LICENSE) 文件。

---

## 免责声明

KeyMuse 是通用自动化工具。请遵守您所操作软件的相关服务条款合理使用。作者不对滥用行为承担任何责任。
