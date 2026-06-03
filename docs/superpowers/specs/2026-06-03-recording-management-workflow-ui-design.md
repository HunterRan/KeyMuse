# KeyMuse v0.1.0 功能设计文档

## 1. 概述

为 KeyMuse 增加三大功能：
1. 录制文件分类管理（文件系统映射）
2. 工作流系统（录制文件组合执行）
3. UI 卡片式布局重构与美化

## 2. 整体布局（卡片式 + Tab 栏）

主窗口尺寸：880x600（比当前 520x480 扩大）

```
┌──────────────────────────────────────────────────────────────┐
│ KeyMuse        [🎬录制] [⚡工作流] [⚙设置]     — ☐ ✕       │
├──────────────────────────────────────────────────────────────┤
│ Tab 内容区域                                                │
│ 左侧卡片 / 右侧卡片 / 全宽卡片                                  │
└──────────────────────────────────────────────────────────────┘
```

- **Tab 栏**：录制 / 工作流 / 设置，点击切换整个内容区域
- **所有 Tab 共享**：全局状态栏（HUD 窗口保持不变），热键保持不变
- **深色主题**：背景 #0f0c29，卡片 #1e1b3b，边框 #2a2550，强调色 #00d4ff

## 3. 录制 Tab

### 3.1 卡片布局

左卡片（录制控制） | 右卡片（录制库）
连点器（全宽底部卡片）

### 3.2 录制控制卡片

- 状态显示：空闲 / 录制中（带闪烁红点动画）
- 快捷键提示：F6 开始/停止录制
- 实时计数器：录制时长（mm:ss）、事件数量
- 按钮：■ 录制（切换录制/停止）、▶ 回放最后录制

### 3.3 录制库卡片

- 顶部分类筛选芯片：[全部] [游戏] [办公] [自定义...] [+]
- 搜索框：实时过滤文件名
- 录制文件列表：
  - 文件名（.keymuse 后缀隐藏）
  - 元数据：日期时间、时长、事件数
  - 操作按钮：▶ 回放、改名（✎）、删除（🗑）
  - 选中高亮，双击回放

### 3.4 连点器卡片（全宽）

- 键码选择：[Insert ▼]
- 间隔输入框：ms
- 启动/停止按钮
- 状态：已点击 N 次

### 3.5 分类管理

- 分类 = 文件系统文件夹：`%APPDATA%\KeyMuse\recordings\{分类名}\`
- 新建分类 → 创建文件夹
- 删除分类 → 删除空文件夹（非空提示）
- 移动录制文件 = 跨文件夹移动 .keymuse 文件
- 分类芯片点击即时筛选

### 3.6 录制文件存储

- 当前：`%TEMP%\KeyMuse\recording_*.keymuse`
- 改为：`%APPDATA%\KeyMuse\recordings\{分类名}\recording_yyyyMMdd-HHmmss.keymuse`
- 临时文件仍写 %TEMP%，停止录制后移到分类目录
- 默认分类："未分类"

## 4. 工作流 Tab

### 4.1 布局

左卡片（工作流列表） | 右卡片（工作流编辑 + 执行）

### 4.2 工作流模型

```json
{
  "name": "日常任务",
  "totalCount": 5,
  "steps": [
    { "recordingFile": "登录.keymuse", "count": 1 },
    { "recordingFile": "领取奖励.keymuse", "count": 1 },
    { "recordingFile": "下线.keymuse", "count": 1 }
  ]
}
```

### 4.3 工作流列表

- 工作流名称列表
- 当前选中高亮
- [+ 新建] [删除] 按钮
- 存储路径：`%APPDATA%\KeyMuse\workflows\{name}.json`

### 4.4 工作流编辑

- 工作流名称（可修改）
- 总执行次数输入
- 步骤列表（表格形式）：
  - 序号
  - 录制文件选择（从录制库选择，支持跨分类浏览）
  - 该步骤执行次数
  - 上移/下移/删除操作
- [+ 添加步骤] 按钮
- [保存] [另存为] 按钮

### 4.5 工作流执行

- 顺序执行每个步骤（每个步骤的 recording 播放 count 次）
- 整体执行 totalCount 次
- 执行时禁用编辑
- 进度条：当前步骤 / 总步骤，整体次数进度
- 每步骤状态：等待中 → 执行中 → 已完成
- 任一步骤失败 → 整体中止
- F9 急停 → 整体中止
- 执行日志输出到状态消息队列（HUD 窗口显示）

## 5. 设置 Tab

### 5.1 布局

单张全宽卡片

### 5.2 内容

- 配置管理：当前配置选择 [+新建] [删除]
- 连点设置：默认键码、默认间隔
- 快捷键帮助表（只读）
- 存储路径显示
- 配置导入/导出按钮
- 关于信息

## 6. Core 层新增

### 6.1 新模型

```csharp
// KeyMuse.Core/Models/WorkflowModel.cs
public class WorkflowStep
{
    public string RecordingFileName { get; set; } = "";
    public int Count { get; set; } = 1;
}

public class WorkflowModel
{
    public string Name { get; set; } = "";
    public int TotalCount { get; set; } = 1;
    public List<WorkflowStep> Steps { get; set; } = new();
}
```

### 6.2 新服务

```csharp
// KeyMuse.Core/Services/RecordingManager.cs
// - 分类 CRUD（创建/删除/重命名文件夹）
// - 录制文件 CRUD（列出/移动/删除/重命名）
// - 文件系统路径操作
// - 搜索/筛选

// KeyMuse.Core/Services/WorkflowManager.cs
// - 工作流 CRUD（JSON 存储）
// - 工作流执行引擎（顺序执行每个步骤）

// KeyMuse.Core/Services/WorkflowExecutor.cs
// - 工作流执行逻辑
// - 进度报告（整体 + 当前步骤）
// - 错误处理/中止
```

### 6.3 存储路径变更

```
%APPDATA%\KeyMuse\
├── recordings\
│   ├── 未分类\
│   │   ├── recording_20260603-143000.keymuse
│   │   └── ...
│   ├── 游戏\
│   │   └── boss_fight.keymuse
│   └── 办公\
│       └── data_entry.keymuse
├── profiles\
│   └── default\config.json
└── workflows\
    ├── 日常任务.json
    └── 刷副本流程.json
```

## 7. Recorder 变更

- `SaveRecordingAsync(string category)` → 保存到分类目录
- `LoadSessionAsync(string filePath)` → 不变（已支持任意路径）
- `ListRecordings(string category)` → 列出分类下所有 .keymuse 文件
- `MoveRecording(string filePath, string targetCategory)` → 移动文件

## 8. 不受影响的部分

- HookManager（无需变更）
- ReplayEngine（工作流直接调用 ReplayEngine.PlayAsync）
- AutoClicker（无需变更）
- InputCoordinator（工作流执行期间仍需协哨）
- HUDWindow（状态显示，无需变更）
- HotKeyManager（热键不变）
- 测试（现有测试不受影响，新增对应测试）
