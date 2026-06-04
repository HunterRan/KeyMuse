# KeyMuse

**Windows 键盘鼠标自动化工具**

KeyMuse 是一款轻量级开源 Windows 自动化工具。提供全局键盘鼠标钩子、分类录制管理、多步骤工作流编排、连点等功能，配有现代化 WPF 桌面界面，支持主题切换和 HUD 状态叠加层。

---

## 功能

### 🎣 全局输入钩子
- 基于 `SetWindowsHookEx` 的低级键盘（WH_KEYBOARD_LL）和鼠标（WH_MOUSE_LL）钩子
- 全系统范围捕获输入事件，无需窗口焦点
- 500ms 心跳定时器自动监控钩子健康状态，断开时自动重连

### ⏺️ 录制与回放
- 录制键盘鼠标事件到 `.keymuse` 文件（ZIP 压缩包：`session.json` + `events.bin`）
- **分类管理** — 按命名分类组织录制文件
- **命名保存** — 停止录制时弹窗输入名称，留空使用时间戳默认名
- 三种回放循环模式：**单次** / **N 次** / **无限**
- 保持原始时间精度的回放

### 📋 工作流系统
- **多步骤工作流** — 将多个录制串联成自动化序列
- **每步设置** — 独立配置每步的重复次数和步骤间隔（ms）
- **工作流重复模式** — 单次 / N 次 / 无限循环
- 可视化步骤编辑：上移/下移排序、添加/移除步骤
- 选文件弹窗支持分类树 + 录制文件列表

### 🔁 连点器
- 可配置间隔（≥ 100ms）
- 可配置触发键（任意虚拟键码或鼠标左键）
- 通过协调互斥锁与录制/回放同时运行

### 🎨 主题系统
- **3 种主题**：深色 / 浅色 / 灰色
- **亚克力玻璃效果**：主窗口背景（Win32 DWM API，Windows 11 22H2+）
- 在线切换，无需重启

### 🖥️ 现代化 WPF 桌面界面
- **主窗口** — 自定义标题栏（WindowChrome）、标签页导航、原生窗口行为
- **页面**：录制管理（分类+文件列表）、工作流（步骤编辑器）、设置（主题/存储/连点）
- **HUD 叠加层** — 置顶左下角状态显示，录制模式显示最近 5 个事件，回放模式显示当前±2 步高亮
- **系统托盘图标** — 最小化到托盘，右键菜单
- **自定义消息框** — 深色主题，支持图标（信息/警告/错误/询问）和按钮（确定/是否）

### ⚙️ 配置系统
- 命名配置存储于 `%APPDATA%\KeyMuse\profiles\`
- 保存主题偏好、连点触发键、存储根路径
- 设置页面支持完整 CRUD 管理

### 🗂️ 存储管理
- 录制、工作流、配置文件默认存储在 `%APPDATA%\KeyMuse\`
- **存储路径可配置** — 设置页面通过文件夹浏览对话框更改
- 所有管理器（录制/工作流/配置）共享统一存储根目录
- 录制按分类目录组织

---

## 下载

预编译二进制文件在 [Releases](https://github.com/HunterRan/KeyMuse/releases) 页面。每个版本为单文件自包含可执行文件（~160MB），无需安装运行时。

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
dotnet publish src/KeyMuse.Wpf/KeyMuse.Wpf.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

## 使用说明

1. 启动 `KeyMuse.Wpf.exe`
2. 程序出现在系统托盘并显示主窗口
3. **录制页面** — 选择或创建分类，点击"录制"开始捕获，点击"停止"输入名称保存
4. **工作流页面** — 创建工作流，添加录制步骤并设置每步的重复次数和间隔
5. **设置页面** — 配置主题、存储路径、连点触发键
6. **HUD** — 左下角叠加层实时显示状态（录制时跟踪事件，回放时显示步骤进度）

### 全局快捷键

| 键 | 功能 | 说明 |
|----|------|------|
| **F6** | 录制 开始/停止 | 开始前选择分类，停止时弹窗命名 |
| **F7** | 回放选中录制 | 需先在录制页面选中文件，开始后自动最小化窗口 |
| **F8** | 执行选中工作流 | 需先在工作流页面选中工作流，开始后自动最小化窗口 |
| **F9** | 连点 开始/停止 | 开关连点器 |
| **F10** | 窗口 显示/隐藏 | 切换主面板显示状态 |
| **INS** | 连点触发键 | 可在设置页面自定义 |

---

## 项目结构

```
KeyMuse/
├── src/
│   ├── KeyMuse.Core/              # 核心库（无 WPF 依赖）
│   │   ├── Models/                # 数据模型（InputEvent, RecordingSession 等）
│   │   └── Services/              # 核心服务
│   │       ├── HookManager.cs         # 全局键盘鼠标钩子
│   │       ├── Recorder.cs            # 事件录制与 ZIP 序列化
│   │       ├── ReplayEngine.cs        # 事件回放（单次/N次/无限）
│   │       ├── AutoClicker.cs         # 间隔连点器
│   │       ├── InputCoordinator.cs    # 基于互斥锁的输入冲突协调
│   │       ├── ConfigManager.cs       # 配置管理（%APPDATA%/KeyMuse）
│   │       ├── RecordingManager.cs    # 基于分类的录制文件存储管理
│   │       ├── WorkflowManager.cs     # 工作流 CRUD 与步骤管理
│   │       ├── WorkflowExecutor.cs    # 多步骤工作流执行引擎
│   │       └── StatusMessageQueue.cs  # 线程安全状态消息队列
│   └── KeyMuse.Wpf/               # WPF 桌面应用程序
│       ├── App.xaml(.cs)              # 入口点、全局样式、快捷键、主题切换
│       ├── MainWindow.xaml(.cs)       # 自定义标题栏、标签页导航
│       ├── HUDWindow.xaml(.cs)        # 置顶状态叠加层
│       ├── HotKeyManager.cs           # 全局快捷键管理（F6-F10）
│       ├── Pages/
│       │   ├── RecordingsPage.xaml(.cs)# 录制分类、文件列表、录制/回放控制
│       │   ├── WorkflowsPage.xaml(.cs) # 工作流步骤编辑（排序、每步配置）
│       │   ├── SettingsPage.xaml(.cs)  # 主题、存储路径、连点配置
│       │   └── TextInputDialog.xaml(.cs)# 可复用的文本输入对话框
│       ├── Controls/
│       │   ├── DarkMessageBox.xaml(.cs)# 自定义主题消息框（图标、按钮）
│       │   ├── CategoryPickerDialog.xaml(.cs)# 分类选择弹窗（列表+新建）
│       │   └── RecordingBrowserDialog.xaml(.cs)# 分类树+录制文件选择器
│       ├── Helpers/
│       │   └── AcrylicHelper.cs        # Win32 亚克力/云母背景效果
│       └── Themes/                    # 主题资源字典
│           ├── Dark.xaml
│           ├── Light.xaml
│           └── Gray.xaml
├── tests/
│   └── KeyMuse.Tests/             # xUnit 单元测试
│       ├── RecorderTests.cs           # 序列化往返、保存/加载
│       ├── ReplayEngineTests.cs       # 循环模式行为
│       ├── ConfigManagerTests.cs      # 配置 CRUD
│       ├── AutoClickerTests.cs        # 开始/停止生命周期
│       ├── HookManagerTests.cs        # 生命周期与释放
│       └── InputCoordinatorTests.cs   # 互斥锁行为
├── assets/                        # 图标资源（SVG / ICO / PNG）
├── KeyMuse.sln
├── README.md
├── README.zh-CN.md
└── LICENSE
```

---

## 架构决策

| 决策 | 选择 | 理由 |
|------|------|------|
| 钩子线程通信 | `ConcurrentQueue<T>` | 解耦、线程安全、无共享状态 |
| 输入冲突解决 | `SemaphoreSlim` 互斥锁 | 轻量、异步兼容的互斥机制 |
| 录制文件格式 | ZIP（`session.json` + `events.bin`） | 可移植、元数据可读、事件数据紧凑 |
| 录制存储 | 分类子目录 `recordings/` | 按用户分组管理，可扩展 |
| 工作流存储 | `workflows/` 下 JSON 文件 | 简单、可读、易备份 |
| 配置存储 | `%APPDATA%/KeyMuse/profiles/` | Windows 标准，用户级作用域 |
| 回放循环模式 | 单次 / N次 / 无限 | 覆盖常见自动化场景 |
| 窗口边框 | `shell:WindowChrome` | 原生调整大小/吸附行为 + 自定义标题栏 |
| 主题系统 | 运行时合并 ResourceDictionary | 即时切换，无需重启 |
| 亚克力背景 | `DwmSetWindowAttribute` / `SetWindowCompositionAttribute` | 现代视觉效果，向下兼容 |
| 钩子健康检查 | 500ms 心跳定时器 | 早期发现断开并自动恢复 |
| 原子写入 | 临时文件 → 重命名完成 | 防止写入崩溃导致文件损坏 |
| 全局快捷键 | `RegisterHotKey` Win32 API + HwndSource | 系统级热键，后台焦点无关 |

---

## 常见问题

### 为什么需要管理员权限？
默认不需要。但如果目标程序以管理员权限运行，`SendInput` 会被 UIPI 阻止。此时可通过右键托盘图标菜单提权。

### .keymuse 文件是什么格式？
标准 ZIP 压缩包。可以直接用 WinRAR/7-Zip 打开查看 `session.json` 和 `events.bin`。

### 快捷键和其他程序冲突怎么办？
目前快捷键为固定 F6-F10，后续版本将支持自定义。

---

## 许可

本项目基于 MIT License 开源。详见 [LICENSE](LICENSE) 文件。

---

## 免责声明

KeyMuse 是通用自动化工具。请遵守您所操作软件的相关服务条款合理使用。作者不对滥用行为承担任何责任。
