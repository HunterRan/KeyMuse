# KeyMuse

**Windows 键鼠自动化工具**

KeyMuse 是一款轻量级、开源的 Windows 自动化工具。提供全局键盘/鼠标钩子、事件录制（支持分类管理）、多步骤工作流编排和自动连点功能——全部通过现代化的 WPF 桌面界面和可自定义的 HUD 悬浮窗实现。

---

## 功能特性

### 全局输入钩子
- 低级键盘和鼠标钩子（`SetWindowsHookEx` WH_KEYBOARD_LL / WH_MOUSE_LL）
- 系统级捕获所有输入事件，无需焦点
- 500ms 心跳健康监控，钩子失败自动重连
- 鼠标钩子在非录制/非按键捕获时自动卸载，避免游戏灵敏度受影响

### 录制 & 回放
- 录制键盘鼠标事件到 `.keymuse` 文件（ZIP：`session.json` + `events.bin`）
- **分类管理**——录制文件按命名分类组织
- **命名保存**——停止时提示输入名称，留空使用时间戳
- 三种回放循环模式：**单次** / **N 次** / **无限**
- 循环可配置**间隔延迟**（毫秒）
- 原始 timing 精确回放（1ms 定时器精度）
- 鼠标回放使用绝对坐标（`MOUSEEVENTF_ABSOLUTE`），绕过 Windows 鼠标加速，轨迹精确还原
- 录制时自动过滤快捷键事件（F6 不会被录进去）

### 工作流系统
- **多步骤工作流**——将多个录制串联为自动执行序列
- **每步设置**——配置每步的循环次数和间隔延迟（毫秒）
- **工作流重复模式**——单次 / N 次 / 无限循环
- 可视化步骤列表，支持上移/下移/添加/删除
- 录制浏览器对话框（分类树 + 文件列表）用于添加步骤

### 自动连点
- 可配置间隔的自动点击（≥ 100ms）
- 可配置触发键（任意虚拟键码或鼠标左键）
- 通过协调互斥锁并发运行

### 主题系统
- **3 套主题**：深色 / 浅色 / 灰色
- **亚克力玻璃效果**（Win32 DWM API，Windows 11 22H2+）
- 运行时即时切换，无需重启

### 现代化 WPF 桌面界面
- **主窗口**——自定义 WindowChrome 标题栏（最小化/最大化/关闭按钮）
- **页面**：录制页（分类 + 文件列表）、工作流页（步骤编辑器）、设置页（主题/存储/连点）
- **HUD 悬浮窗**——置顶状态叠加层，录制时显示最近 5 条事件，回放时显示当前 ±2 步；支持倒计时提示（间隔延迟）
- **系统托盘图标**——最小化到托盘，快捷菜单
- **自定义 MessageBox**——深色主题，支持图标（Info/Warning/Error/Question）和 YesNo/OK 按钮

### 配置文件系统
- 命名配置文件，存储在 `%APPDATA%\KeyMuse\profiles\`
- 保存主题偏好、连点键码、存储根路径
- 设置页支持完整的增删改查

### 存储管理
- 录制、工作流和配置默认存储在 `%APPDATA%\KeyMuse\`
- **可配置存储路径**——通过设置页面文件夹选择器更改
- 录制/工作流管理器使用统一的存储根路径
- 配置始终在 `%APPDATA%\KeyMuse\profiles\`，不受存储路径影响

---

## 下载

预编译二进制见 [Releases](https://github.com/HunterRan/KeyMuse/releases)。每个版本为单文件自包含可执行文件（~150MB），无需运行时依赖。

---

## 从源码构建

### 前置条件

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本
- Windows 10 / Windows 11（低级钩子需要 Windows）

### 克隆 & 构建

```bash
git clone https://github.com/HunterRan/KeyMuse.git
cd KeyMuse
dotnet restore
dotnet build --configuration Release
dotnet test
dotnet publish src/KeyMuse.Wpf/KeyMuse.Wpf.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

## 使用方法

1. 以**管理员身份**运行 `KeyMuse.Wpf.exe`（模拟输入操作需要管理员权限）
2. 应用出现在系统托盘并显示主窗口
3. **录制页**——选择或创建分类，点击"录制"开始捕获事件，"停止"后命名保存
4. **工作流页**——创建工作流，添加录制步骤并配置每步的循环次数和间隔
5. **设置页**——配置主题、存储路径、连点键
6. **HUD**——左下角悬浮窗显示实时状态（录制时跟踪事件，回放时显示步骤进度）

### 快捷键

| 键   | 功能                                                                 |
|------|----------------------------------------------------------------------|
| `F6` | 切换录制。录制页有选中分类则直接开始，否则弹窗选择。停止时弹窗命名。     |
| `F7` | 回放选中录制 / 停止回放。无选中文件则弹出录制浏览器选择。自动最小化窗口。 |
| `F8` | 执行选中工作流 / 停止执行。无选中工作流则弹出工作流选择器。自动最小化窗口。 |
| `F9` | 切换自动连点。                                                        |
| `F10`| 显示/隐藏主窗口。                                                     |

### 对话框特性
- 所有快捷键触发的对话框（分类选择、命名、录制浏览器）均为 `Topmost` 模式，在全屏游戏中也可正常显示
- 录制快捷键在对话框操作期间产生的键鼠事件不会混入录制

---

## 项目结构

```
KeyMuse/
├── src/
│   ├── KeyMuse.Core/              # 核心库（无 WPF 依赖）
│   │   ├── Models/                # 数据模型（InputEvent, RecordingSession 等）
│   │   ├── Services/              # 核心服务
│   │   └── Helpers/               # 辅助工具（KeyNames 键码映射）
│   └── KeyMuse.Wpf/               # WPF 桌面应用
│       ├── App.xaml(.cs)              # 入口、全局样式、热键注册、主题切换
│       ├── MainWindow.xaml(.cs)       # 自定义标题栏、标签导航
│       ├── HUDWindow.xaml(.cs)        # 置顶状态悬浮窗
│       ├── HotKeyManager.cs           # 全局热键注册（F6-F10）
│       ├── Pages/
│       │   ├── RecordingsPage         # 录制分类、文件列表、录制/回放控制
│       │   ├── WorkflowsPage          # 工作流步骤编辑器
│       │   ├── SettingsPage           # 主题、存储路径、连点配置
│       │   └── TextInputDialog        # 可复用的文本输入对话框
│       ├── Controls/
│       │   ├── DarkMessageBox         # 自定义消息框（图标、按钮）
│       │   ├── CategoryPickerDialog   # 分类选择对话框
│       │   ├── RecordingBrowserDialog # 录制文件浏览器（分类+文件）
│       │   └── WorkflowPickerDialog   # 工作流选择器
│       ├── Helpers/
│       │   └── AcrylicHelper          # Win32 亚克力/云母背景
│       └── Themes/                    # 主题资源字典（Dark/Light/Gray）
├── tests/
│   └── KeyMuse.Tests/             # xUnit 测试
├── assets/                        # 图标资源
├── KeyMuse.sln
├── README.md
└── LICENSE
```

---

## 架构决策

| 决策                | 选择                              | 理由                                     |
|---------------------|-----------------------------------|------------------------------------------|
| 钩子线程通信         | `ConcurrentQueue<T>`              | 解耦、线程安全、无共享状态                 |
| 输入冲突解决         | `SemaphoreSlim` 互斥锁             | 轻量、async 兼容的互斥                     |
| 录制文件格式         | ZIP（`session.json` + `events.bin`）| 可移植、元数据可读、事件紧凑二进制           |
| 录制存储             | `recordings/` 下的分类子目录        | 按用户分组组织、可扩展                     |
| 工作流存储           | `workflows/` 下的 JSON 文件         | 简单、可读、易备份                        |
| 配置存储             | `%APPDATA%/KeyMuse/profiles/`     | Windows 标准，用户作用域                   |
| 回放循环模式         | 单次 / N 次 / 无限                 | 通用自动化模式                            |
| 窗口修饰             | `shell:WindowChrome`              | 保持原生缩放/吸附行为 + 自定义标题栏        |
| 主题系统             | 运行时 ResourceDictionary 合并      | 即时切换，无需重启                         |
| 亚克力背景           | `DwmSetWindowAttribute`           | 现代化 Windows 视觉效果，低版本自动回退      |
| 钩子健康检查         | 500ms 心跳定时器                    | 及时检测和恢复钩子失败                     |
| 原子文件写入         | 临时文件 → 重命名完成                | 防止写入崩溃导致文件损坏                    |
| 鼠标回放             | `MOUSEEVENTF_ABSOLUTE` 绝对坐标     | 绕过 Windows 鼠标加速/速度设置，精确保真     |
| 鼠标钩子管理         | 按需安装/卸载                       | 非录制/非按键捕获时不安装钩子，避免系统延迟   |
| 定时器精度           | `timeBeginPeriod(1)`               | 1ms 分辨率，确保回放 timing 准确            |
| 快捷键对话框         | Topmost 窗口                       | 在全屏游戏中也正常显示                      |

---

## 许可证

本项目基于 MIT 许可证发布。详见 [LICENSE](LICENSE) 文件。

---

## 免责声明

KeyMuse 是通用自动化工具。请遵守目标软件的服务条款负责任地使用。作者不对滥用行为承担任何责任。
