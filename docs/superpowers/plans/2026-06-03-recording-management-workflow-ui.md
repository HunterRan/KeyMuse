# Recording Management + Workflow + UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add category-based recording management, workflow system (compose recordings), and card-style tabbed UI

**Architecture:** Core layer gains RecordingManager (file-system folder mapping), WorkflowManager (JSON CRUD), and WorkflowExecutor (sequential step execution). WPF layer gains 3 tab pages (RecordingsPage, WorkflowsPage, SettingsPage) with card layout, replacing MainWindow's inline controls. Recorder saves to category folders under `%APPDATA%\KeyMuse\recordings\`.

**Tech Stack:** C# .NET 8 WPF, xUnit, ImageMagick (icons), Win32 interop

---

### Task 1: Core Model — RecordingInfo + WorkflowModel

**Files:**
- Create: `src/KeyMuse.Core/Models/RecordingInfo.cs`
- Create: `src/KeyMuse.Core/Models/WorkflowModel.cs`

- [ ] **Step 1: Create RecordingInfo model**

```csharp
namespace KeyMuse.Core.Models;

public class RecordingInfo
{
    public string FilePath { get; set; } = "";
    public string FileName => System.IO.Path.GetFileNameWithoutExtension(FilePath);
    public string Category { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int DurationMs { get; set; }
    public int EventCount { get; set; }
    public string DisplayDuration => DurationMs >= 60000
        ? $"{DurationMs / 60000}m{DurationMs % 60000 / 1000}s"
        : $"{DurationMs / 1000}s";
}
```

- [ ] **Step 2: Create WorkflowModel**

```csharp
namespace KeyMuse.Core.Models;

public class WorkflowStep
{
    public string RecordingFilePath { get; set; } = "";
    public int Count { get; set; } = 1;
}

public class WorkflowModel
{
    public string Name { get; set; } = "";
    public int TotalCount { get; set; } = 1;
    public List<WorkflowStep> Steps { get; set; } = new();
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/KeyMuse.Core/KeyMuse.Core.csproj --configuration Release`
Expected: Build succeeds with 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/KeyMuse.Core/Models/RecordingInfo.cs src/KeyMuse.Core/Models/WorkflowModel.cs
git commit -m "feat: add RecordingInfo and WorkflowModel models"
```

---

### Task 2: Core Service — RecordingManager

**Files:**
- Create: `src/KeyMuse.Core/Services/RecordingManager.cs`
- Test: `tests/KeyMuse.Tests/RecordingManagerTests.cs`

- [ ] **Step 1: Create RecordingManager**

```csharp
using System.Text.Json;
using System.IO.Compression;
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class RecordingManager
{
    private readonly string _baseDir;

    public string BaseDir => _baseDir;

    public RecordingManager()
    {
        _baseDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMuse", "recordings");
        System.IO.Directory.CreateDirectory(_baseDir);
    }

    public RecordingManager(string customBaseDir)
    {
        _baseDir = customBaseDir;
        System.IO.Directory.CreateDirectory(_baseDir);
    }

    public string[] ListCategories()
    {
        var dirs = System.IO.Directory.GetDirectories(_baseDir)
            .Select(System.IO.Path.GetFileName)
            .ToArray();
        return dirs.OrderBy(x => x == "\u672a\u5206\u7c7b" ? 0 : 1)
                   .ThenBy(x => x)
                   .ToArray();
    }

    public void CreateCategory(string name)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_baseDir, name));
    }

    public void DeleteCategory(string name)
    {
        var dir = System.IO.Path.Combine(_baseDir, name);
        if (!System.IO.Directory.Exists(dir)) return;
        if (System.IO.Directory.GetFiles(dir, "*.keymuse").Length > 0)
            throw new InvalidOperationException($"\u5206\u7c7b '{name}' \u4e0d\u4e3a\u7a7a\uff0c\u65e0\u6cd5\u5220\u9664");
        System.IO.Directory.Delete(dir);
    }

    public string EnsureCategory(string name)
    {
        var dir = System.IO.Path.Combine(_baseDir, name);
        System.IO.Directory.CreateDirectory(dir);
        return dir;
    }

    public RecordingInfo[] ListRecordings(string category)
    {
        var dir = System.IO.Path.Combine(_baseDir, category);
        if (!System.IO.Directory.Exists(dir))
            return Array.Empty<RecordingInfo>();

        return System.IO.Directory.GetFiles(dir, "*.keymuse")
            .Select(LoadRecordingInfo)
            .Where(r => r != null)
            .Select(r => r!)
            .OrderByDescending(r => r.CreatedAt)
            .ToArray();
    }

    public RecordingInfo[] ListAllRecordings()
    {
        if (!System.IO.Directory.Exists(_baseDir))
            return Array.Empty<RecordingInfo>();

        return System.IO.Directory.GetDirectories(_baseDir)
            .SelectMany(dir => System.IO.Directory.GetFiles(dir, "*.keymuse"))
            .Select(LoadRecordingInfo)
            .Where(r => r != null)
            .Select(r => r!)
            .OrderByDescending(r => r.CreatedAt)
            .ToArray();
    }

    private RecordingInfo? LoadRecordingInfo(string filePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);
            var entry = archive.GetEntry("session.json");
            if (entry == null) return null;

            using var reader = new StreamReader(entry.Open());
            var json = reader.ReadToEnd();
            var session = JsonSerializer.Deserialize<RecordingSession>(json);
            if (session == null) return null;

            var dirName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(filePath)) ?? "";
            return new RecordingInfo
            {
                FilePath = filePath,
                Category = dirName,
                CreatedAt = session.CreatedAt,
                DurationMs = session.DurationMs,
                EventCount = session.EventCount
            };
        }
        catch
        {
            return null;
        }
    }

    public void DeleteRecording(string filePath)
    {
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
    }

    public string SaveRecording(string tempFilePath, string category)
    {
        var catDir = EnsureCategory(category);
        var fileName = System.IO.Path.GetFileName(tempFilePath);
        var destPath = System.IO.Path.Combine(catDir, fileName);
        System.IO.File.Move(tempFilePath, destPath);
        return destPath;
    }

    public string MoveRecording(string filePath, string targetCategory)
    {
        var catDir = EnsureCategory(targetCategory);
        var destPath = System.IO.Path.Combine(catDir, System.IO.Path.GetFileName(filePath));
        System.IO.File.Move(filePath, destPath);
        return destPath;
    }

    public string RenameRecording(string filePath, string newName)
    {
        var dir = System.IO.Path.GetDirectoryName(filePath) ?? _baseDir;
        var destPath = System.IO.Path.Combine(dir, newName + ".keymuse");
        System.IO.File.Move(filePath, destPath);
        return destPath;
    }
}
```

- [ ] **Step 2: Write RecordingManager tests**

```csharp
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;
using System.IO.Compression;
using System.Text.Json;

namespace KeyMuse.Tests;

public class RecordingManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly RecordingManager _manager;

    public RecordingManagerTests()
    {
        _testDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KMTest_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_testDir);
        _manager = new RecordingManager(_testDir);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_testDir))
            System.IO.Directory.Delete(_testDir, true);
    }

    private string CreateTestRecording(string category, string name, int events = 10)
    {
        var catDir = System.IO.Path.Combine(_testDir, category);
        System.IO.Directory.CreateDirectory(catDir);
        var filePath = System.IO.Path.Combine(catDir, name + ".keymuse");

        var session = new RecordingSession
        {
            CreatedAt = DateTime.Now,
            DurationMs = 5000,
            Events = Enumerable.Range(0, events).Select(i => new InputEvent
            {
                TimeOffsetMs = i * 100,
                Type = InputEventType.KeyDown,
                VirtualKeyCode = 0x41
            }).ToList()
        };

        using var stream = new FileStream(filePath, FileMode.Create);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        var jsonEntry = archive.CreateEntry("session.json");
        using (var writer = new StreamWriter(jsonEntry.Open()))
        {
            writer.Write(JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true }));
        }
        var eventsEntry = archive.CreateEntry("events.bin");
        using (var binWriter = new BinaryWriter(eventsEntry.Open()))
        {
            binWriter.Write(session.Events.Count);
            foreach (var e in session.Events)
            {
                binWriter.Write(e.TimeOffsetMs);
                binWriter.Write((int)e.Type);
                binWriter.Write(e.VirtualKeyCode);
                binWriter.Write(e.X); binWriter.Write(e.Y);
                binWriter.Write(e.RelX); binWriter.Write(e.RelY);
                binWriter.Write(e.MouseData);
                binWriter.Write(e.WindowHandle.ToInt64());
                binWriter.Write(e.WindowLeft); binWriter.Write(e.WindowTop);
                binWriter.Write(e.WindowWidth); binWriter.Write(e.WindowHeight);
            }
        }
        return filePath;
    }

    [Fact]
    public void ListCategories_Empty_ReturnsEmpty()
    {
        var cats = _manager.ListCategories();
        Assert.Empty(cats);
    }

    [Fact]
    public void CreateCategory_CreatesDirectory()
    {
        _manager.CreateCategory("test_cat");
        Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(_testDir, "test_cat")));
    }

    [Fact]
    public void ListCategories_ReturnsCreated()
    {
        _manager.CreateCategory("game");
        _manager.CreateCategory("work");
        var cats = _manager.ListCategories();
        Assert.Contains("game", cats);
        Assert.Contains("work", cats);
    }

    [Fact]
    public void ListRecordings_ReturnsRecordingInfo()
    {
        _manager.CreateCategory("test");
        CreateTestRecording("test", "rec1", 5);
        var list = _manager.ListRecordings("test");
        Assert.Single(list);
        Assert.Equal(5, list[0].EventCount);
        Assert.Equal("rec1", list[0].FileName);
    }

    [Fact]
    public void ListAllRecordings_AggregatesAll()
    {
        _manager.CreateCategory("a");
        _manager.CreateCategory("b");
        CreateTestRecording("a", "r1");
        CreateTestRecording("b", "r2");
        CreateTestRecording("b", "r3");
        Assert.Equal(3, _manager.ListAllRecordings().Length);
    }

    [Fact]
    public void DeleteRecording_RemovesFile()
    {
        _manager.CreateCategory("test");
        var path = CreateTestRecording("test", "del_me");
        _manager.DeleteRecording(path);
        Assert.False(System.IO.File.Exists(path));
    }

    [Fact]
    public void SaveRecording_MovesToCategory()
    {
        _manager.CreateCategory("target");
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_save.keymuse");
        CreateTestRecording(System.IO.Path.GetTempPath(), "test_save", 1);
        // the helper creates at GetTempPath\category, but we want a standalone file
        System.IO.File.Copy(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_save", "test_save.keymuse"),
            tempFile);
        System.IO.Directory.Delete(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_save"), true);

        var saved = _manager.SaveRecording(tempFile, "target");
        Assert.True(System.IO.File.Exists(saved));
        Assert.Contains("target", saved);
    }

    [Fact]
    public void RenameRecording_ChangesName()
    {
        _manager.CreateCategory("test");
        var path = CreateTestRecording("test", "old_name");
        var renamed = _manager.RenameRecording(path, "new_name");
        Assert.False(System.IO.File.Exists(path));
        Assert.True(System.IO.File.Exists(renamed));
        Assert.Contains("new_name", renamed);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail (no RecordingManager yet)**

Run: `dotnet test tests/KeyMuse.Tests/KeyMuse.Tests.csproj --configuration Release`
Expected: Compilation error (RecordingManager not found)

- [ ] **Step 4: Add RecordingManager to project, then run tests**

```bash
# RecordingManager already created in Step 1, just build and test
dotnet build --configuration Release
dotnet test tests/KeyMuse.Tests/KeyMuse.Tests.csproj --configuration Release --no-build
```
Expected: All RecordingManager tests pass

- [ ] **Step 5: Commit**

```bash
git add src/KeyMuse.Core/Services/RecordingManager.cs tests/KeyMuse.Tests/RecordingManagerTests.cs
git commit -m "feat: add RecordingManager with category CRUD and recording listing"
```

---

### Task 3: Update Recorder for category-based save

**Files:**
- Modify: `src/KeyMuse.Core/Services/Recorder.cs`

- [ ] **Step 1: Add SaveRecordingAsync overload with category**

Add to Recorder class:
```csharp
public async Task<string?> StopRecordingAsync(string category)
{
    var tempPath = await StopRecordingAsync();
    if (tempPath == null) return null;

    var recordingDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeyMuse", "recordings", category);
    System.IO.Directory.CreateDirectory(recordingDir);

    var fileName = System.IO.Path.GetFileName(tempPath);
    var destPath = System.IO.Path.Combine(recordingDir, fileName);
    System.IO.File.Move(tempPath, destPath);
    return destPath;
}
```

Also add a `GetTempPath()` overload:
```csharp
// Modify StopRecordingAsync() line ~101:
// Change finalFile path from TEMP to a recordings dir
// Actually, keep StopRecordingAsync() saving to TEMP as before for backward compat.
// The new StopRecordingAsync(string category) moves the file to the category dir.
```

Wait - there's a subtle issue. `StopRecordingAsync()` saves to TEMP, then `StopRecordingAsync(string category)` moves it. But `StopRecordingAsync()` already does `File.Move(tempFile, finalFile)`. 

Let me refactor: split the save logic from the move logic.

Actually, the cleaner approach: keep `StopRecordingAsync()` saving to TEMP. The caller (RecordingManager) handles moving to the correct category folder. So no changes needed to Recorder. The App.xaml.cs will use RecordingManager to save.

Let me skip this task since we don't actually need to change Recorder. The RecordingManager.SaveRecording() handles moving from TEMP to the category folder.

- [ ] **Step 1: No changes needed — RecordingManager.SaveRecording handles category save after Recorder.StopRecordingAsync() returns the temp path**

Verified: App flow will be:
1. `Recorder.StartRecording()`
2. `var tempPath = await Recorder.StopRecordingAsync()` → saves to TEMP
3. `RecordingManager.SaveRecording(tempPath, selectedCategory)` → moves to category dir

- [ ] **Step 2: Commit (no code changes)**

---

### Task 4: Core Service — WorkflowManager

**Files:**
- Create: `src/KeyMuse.Core/Services/WorkflowManager.cs`
- Test: `tests/KeyMuse.Tests/WorkflowManagerTests.cs`

- [ ] **Step 1: Create WorkflowManager**

```csharp
using System.Text.Json;
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class WorkflowManager
{
    private readonly string _baseDir;

    public string BaseDir => _baseDir;

    public WorkflowManager()
    {
        _baseDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMuse", "workflows");
        System.IO.Directory.CreateDirectory(_baseDir);
    }

    public WorkflowManager(string customBaseDir)
    {
        _baseDir = customBaseDir;
        System.IO.Directory.CreateDirectory(_baseDir);
    }

    public string[] ListWorkflowNames()
    {
        return System.IO.Directory.GetFiles(_baseDir, "*.json")
            .Select(System.IO.Path.GetFileNameWithoutExtension)
            .OrderBy(x => x)
            .ToArray();
    }

    public WorkflowModel? LoadWorkflow(string name)
    {
        var path = System.IO.Path.Combine(_baseDir, name + ".json");
        if (!System.IO.File.Exists(path)) return null;
        var json = System.IO.File.ReadAllText(path);
        return JsonSerializer.Deserialize<WorkflowModel>(json);
    }

    public void SaveWorkflow(WorkflowModel workflow)
    {
        var path = System.IO.Path.Combine(_baseDir, workflow.Name + ".json");
        var json = JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(path, json);
    }

    public void DeleteWorkflow(string name)
    {
        var path = System.IO.Path.Combine(_baseDir, name + ".json");
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    public bool WorkflowExists(string name)
    {
        return System.IO.File.Exists(System.IO.Path.Combine(_baseDir, name + ".json"));
    }
}
```

- [ ] **Step 2: Write WorkflowManager tests**

```csharp
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;

namespace KeyMuse.Tests;

public class WorkflowManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly WorkflowManager _manager;

    public WorkflowManagerTests()
    {
        _testDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KMWfTest_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_testDir);
        _manager = new WorkflowManager(_testDir);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_testDir))
            System.IO.Directory.Delete(_testDir, true);
    }

    [Fact]
    public void ListWorkflowNames_Empty_ReturnsEmpty()
    {
        Assert.Empty(_manager.ListWorkflowNames());
    }

    [Fact]
    public void SaveAndLoadWorkflow()
    {
        var wf = new WorkflowModel
        {
            Name = "test_flow",
            TotalCount = 3,
            Steps = new List<WorkflowStep>
            {
                new() { RecordingFilePath = @"C:\a.keymuse", Count = 1 },
                new() { RecordingFilePath = @"C:\b.keymuse", Count = 2 }
            }
        };
        _manager.SaveWorkflow(wf);

        var loaded = _manager.LoadWorkflow("test_flow");
        Assert.NotNull(loaded);
        Assert.Equal("test_flow", loaded!.Name);
        Assert.Equal(3, loaded.TotalCount);
        Assert.Equal(2, loaded.Steps.Count);
        Assert.Equal(2, loaded.Steps[1].Count);
    }

    [Fact]
    public void LoadWorkflow_NotFound_ReturnsNull()
    {
        Assert.Null(_manager.LoadWorkflow("nonexistent"));
    }

    [Fact]
    public void DeleteWorkflow_RemovesFile()
    {
        var wf = new WorkflowModel { Name = "delete_me" };
        _manager.SaveWorkflow(wf);
        _manager.DeleteWorkflow("delete_me");
        Assert.Null(_manager.LoadWorkflow("delete_me"));
    }

    [Fact]
    public void ListWorkflowNames_AfterSave_ReturnsName()
    {
        _manager.SaveWorkflow(new WorkflowModel { Name = "flow_a" });
        _manager.SaveWorkflow(new WorkflowModel { Name = "flow_b" });
        var names = _manager.ListWorkflowNames();
        Assert.Contains("flow_a", names);
        Assert.Contains("flow_b", names);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet build --configuration Release && dotnet test tests/KeyMuse.Tests/KeyMuse.Tests.csproj --configuration Release --no-build`
Expected: All WorkflowManager tests pass

- [ ] **Step 4: Commit**

```bash
git add src/KeyMuse.Core/Services/WorkflowManager.cs tests/KeyMuse.Tests/WorkflowManagerTests.cs
git commit -m "feat: add WorkflowManager with JSON CRUD"
```

---

### Task 5: Core Service — WorkflowExecutor

**Files:**
- Create: `src/KeyMuse.Core/Services/WorkflowExecutor.cs`
- Test: `tests/KeyMuse.Tests/WorkflowExecutorTests.cs`

- [ ] **Step 1: Create WorkflowExecutor**

```csharp
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class WorkflowProgress
{
    public int CurrentStepIndex { get; set; }
    public int TotalSteps { get; set; }
    public int CurrentOverallCount { get; set; }
    public int TotalOverallCount { get; set; }
    public string? CurrentStepName { get; set; }
    public bool IsRunning { get; set; }
    public bool IsCompleted { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WorkflowExecutor
{
    private readonly ReplayEngine _replayEngine;
    private readonly InputCoordinator _coordinator;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public event Action<WorkflowProgress>? OnProgress;
    public event Action<string>? OnError;

    public WorkflowExecutor(ReplayEngine replayEngine, InputCoordinator coordinator)
    {
        _replayEngine = replayEngine;
        _coordinator = coordinator;
    }

    public async Task ExecuteAsync(WorkflowModel workflow, CancellationToken token = default)
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        try
        {
            var progress = new WorkflowProgress
            {
                TotalSteps = workflow.Steps.Count,
                TotalOverallCount = workflow.TotalCount,
                IsRunning = true
            };

            for (int overall = 0; overall < workflow.TotalCount; overall++)
            {
                progress.CurrentOverallCount = overall + 1;
                if (_cts.IsCancellationRequested) break;

                for (int stepIdx = 0; stepIdx < workflow.Steps.Count; stepIdx++)
                {
                    if (_cts.IsCancellationRequested) break;

                    var step = workflow.Steps[stepIdx];
                    progress.CurrentStepIndex = stepIdx;
                    progress.CurrentStepName = System.IO.Path.GetFileNameWithoutExtension(step.RecordingFilePath);

                    OnProgress?.Invoke(progress);

                    if (!System.IO.File.Exists(step.RecordingFilePath))
                    {
                        var err = $"\u6b65\u9aa4 {stepIdx + 1}: \u6587\u4ef6\u4e0d\u5b58\u5728 - {step.RecordingFilePath}";
                        progress.ErrorMessage = err;
                        OnError?.Invoke(err);
                        OnProgress?.Invoke(progress);
                        return;
                    }

                    for (int c = 0; c < step.Count; c++)
                    {
                        if (_cts.IsCancellationRequested) break;

                        var session = await LoadSessionAsync(step.RecordingFilePath);
                        if (session == null)
                        {
                            var err = $"\u6b65\u9aa4 {stepIdx + 1}: \u65e0\u6cd5\u52a0\u8f7d\u5f55\u5236\u6587\u4ef6";
                            progress.ErrorMessage = err;
                            OnError?.Invoke(err);
                            OnProgress?.Invoke(progress);
                            return;
                        }

                        await _replayEngine.PlayAsync(session, LoopMode.Single, 1, 0);
                    }
                }
            }

            progress.IsCompleted = true;
            progress.IsRunning = false;
            OnProgress?.Invoke(progress);
        }
        catch (OperationCanceledException)
        {
            OnProgress?.Invoke(new WorkflowProgress
            {
                IsRunning = false,
                IsCompleted = false,
                ErrorMessage = "\u5df2\u4e2d\u6b62"
            });
        }
        finally
        {
            _isRunning = false;
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _replayEngine.Stop();
    }

    private static async Task<RecordingSession?> LoadSessionAsync(string filePath)
    {
        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
            var entry = archive.GetEntry("session.json");
            if (entry == null) return null;
            using var reader = new StreamReader(entry.Open());
            var json = await reader.ReadToEndAsync();
            var session = System.Text.Json.JsonSerializer.Deserialize<RecordingSession>(json);
            if (session == null) return null;

            var eventsEntry = archive.GetEntry("events.bin");
            if (eventsEntry != null)
            {
                using var binReader = new BinaryReader(eventsEntry.Open());
                var count = binReader.ReadInt32();
                session.Events = new List<InputEvent>(count);
                for (int i = 0; i < count; i++)
                {
                    session.Events.Add(new InputEvent
                    {
                        TimeOffsetMs = binReader.ReadInt32(),
                        Type = (InputEventType)binReader.ReadInt32(),
                        VirtualKeyCode = binReader.ReadInt32(),
                        X = binReader.ReadInt32(),
                        Y = binReader.ReadInt32(),
                        RelX = binReader.ReadInt32(),
                        RelY = binReader.ReadInt32(),
                        MouseData = binReader.ReadInt32(),
                        WindowHandle = (nint)binReader.ReadInt64(),
                        WindowLeft = binReader.ReadInt32(),
                        WindowTop = binReader.ReadInt32(),
                        WindowWidth = binReader.ReadInt32(),
                        WindowHeight = binReader.ReadInt32()
                    });
                }
            }
            return session;
        }
        catch { return null; }
    }
}
```

- [ ] **Step 2: Write WorkflowExecutor tests** (mocked ReplayEngine)

```csharp
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;

namespace KeyMuse.Tests;

public class WorkflowExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_NoSteps_Completes()
    {
        var coord = new InputCoordinator();
        var engine = new ReplayEngine(coord);
        var executor = new WorkflowExecutor(engine, coord);

        var wf = new WorkflowModel
        {
            Name = "empty",
            TotalCount = 1,
            Steps = new List<WorkflowStep>()
        };

        WorkflowProgress? finalProgress = null;
        executor.OnProgress += p => finalProgress = p;

        await executor.ExecuteAsync(wf);

        Assert.NotNull(finalProgress);
        Assert.True(finalProgress!.IsCompleted);
        Assert.False(finalProgress.IsRunning);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingFile_ReportsError()
    {
        var coord = new InputCoordinator();
        var engine = new ReplayEngine(coord);
        var executor = new WorkflowExecutor(engine, coord);

        var wf = new WorkflowModel
        {
            Name = "missing",
            TotalCount = 1,
            Steps = new List<WorkflowStep>
            {
                new() { RecordingFilePath = @"Z:\nonexistent\file.keymuse", Count = 1 }
            }
        };

        string? error = null;
        executor.OnError += e => error = e;

        await executor.ExecuteAsync(wf);

        Assert.NotNull(error);
        Assert.Contains("\u4e0d\u5b58\u5728", error);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet build --configuration Release && dotnet test tests/KeyMuse.Tests/KeyMuse.Tests.csproj --configuration Release --no-build`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add src/KeyMuse.Core/Services/WorkflowExecutor.cs tests/KeyMuse.Tests/WorkflowExecutorTests.cs
git commit -m "feat: add WorkflowExecutor with sequential step execution"
```

---

### Task 6: MainWindow — Tab system + Card Layout

**Files:**
- Create: `src/KeyMuse.Wpf/Pages/RecordingsPage.xaml`
- Create: `src/KeyMuse.Wpf/Pages/RecordingsPage.xaml.cs`
- Create: `src/KeyMuse.Wpf/Pages/WorkflowsPage.xaml`
- Create: `src/KeyMuse.Wpf/Pages/WorkflowsPage.xaml.cs`
- Create: `src/KeyMuse.Wpf/Pages/SettingsPage.xaml`
- Create: `src/KeyMuse.Wpf/Pages/SettingsPage.xaml.cs`
- Modify: `src/KeyMuse.Wpf/MainWindow.xaml`
- Modify: `src/KeyMuse.Wpf/MainWindow.xaml.cs`

- [ ] **Step 1: Redesign MainWindow.xaml with Tab bar + content area**

```xml
<Window x:Class="KeyMuse.Wpf.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="KeyMuse" Height="680" Width="920"
        WindowStartupLocation="CenterScreen"
        Background="#0f0c29">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Title Bar -->
        <Border Grid.Row="0" Background="#1a1535" Padding="12 8">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0" Text="KeyMuse" FontSize="18" FontWeight="700"
                           Foreground="#00d4ff" VerticalAlignment="Center"/>
                <TextBlock Grid.Column="1" x:Name="GlobalStatusText" Text="空闲"
                           Foreground="#888" FontSize="12" VerticalAlignment="Center"
                           Margin="20 0 0 0"/>
            </Grid>
        </Border>

        <!-- Tab Bar -->
        <Border Grid.Row="1" Background="#1a1535" Padding="0" Height="36">
            <StackPanel Orientation="Horizontal">
                <RadioButton x:Name="TabRecordings" GroupName="Tabs"
                             Style="{StaticResource TabButtonStyle}" IsChecked="True"
                             Content="  录制" FontSize="13" Tag="Recordings"/>
                <RadioButton x:Name="TabWorkflows" GroupName="Tabs"
                             Style="{StaticResource TabButtonStyle}"
                             Content="  工作流" FontSize="13" Tag="Workflows"/>
                <RadioButton x:Name="TabSettings" GroupName="Tabs"
                             Style="{StaticResource TabButtonStyle}"
                             Content="  设置" FontSize="13" Tag="Settings"/>
            </StackPanel>
        </Border>

        <!-- Tab Content -->
        <ContentControl Grid.Row="2" x:Name="TabContent" Margin="8">
            <!-- Pages are loaded into here programmatically -->
        </ContentControl>

        <!-- Status Bar -->
        <Border Grid.Row="3" Background="#1a1535" Padding="10 4">
            <TextBlock x:Name="FooterStatus" Text="就绪" Foreground="#666" FontSize="11"/>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 2: Add Tab button style to App.xaml**

Add to App.xaml `<Application.Resources>`:
```xml
<Application.Resources>
    <ResourceDictionary>
        <Style x:Key="TabButtonStyle" TargetType="RadioButton">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Foreground" Value="#666"/>
            <Setter Property="Padding" Value="16 6"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="RadioButton">
                        <Border Name="Border" Background="{TemplateBinding Background}"
                                BorderThickness="0 0 0 2" BorderBrush="Transparent"
                                Padding="{TemplateBinding Padding}">
                            <ContentPresenter/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsChecked" Value="True">
                                <Setter TargetName="Border" Property="BorderBrush" Value="#00d4ff"/>
                                <Setter Property="Foreground" Value="#00d4ff"/>
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter Property="Background" Value="#2a2550"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </ResourceDictionary>
</Application.Resources>
```

- [ ] **Step 3: Rewrite MainWindow.xaml.cs with tab switching**

```csharp
using System.Windows;
using System.Windows.Controls;
using KeyMuse.Wpf.Pages;

namespace KeyMuse.Wpf;

public partial class MainWindow : Window
{
    private readonly App _app;

    private RecordingsPage? _recordingsPage;
    private WorkflowsPage? _workflowsPage;
    private SettingsPage? _settingsPage;

    public MainWindow(App app)
    {
        InitializeComponent();
        _app = app;

        TabRecordings.Checked += (_, _) => SwitchTab("Recordings");
        TabWorkflows.Checked += (_, _) => SwitchTab("Workflows");
        TabSettings.Checked += (_, _) => SwitchTab("Settings");

        SwitchTab("Recordings");
    }

    private void SwitchTab(string tab)
    {
        switch (tab)
        {
            case "Recordings":
                _recordingsPage ??= new RecordingsPage(_app);
                TabContent.Content = _recordingsPage;
                break;
            case "Workflows":
                _workflowsPage ??= new WorkflowsPage(_app);
                TabContent.Content = _workflowsPage;
                break;
            case "Settings":
                _settingsPage ??= new SettingsPage(_app);
                TabContent.Content = _settingsPage;
                break;
        }
    }

    public void UpdateGlobalStatus(string text)
    {
        GlobalStatusText.Text = text;
    }

    protected override void OnClosed(EventArgs e)
    {
        _app.HookManager.Stop();
        base.OnClosed(e);
    }
}
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build src/KeyMuse.Wpf/KeyMuse.Wpf.csproj --configuration Release`
Expected: Build succeeds (may have warnings about empty pages, but no errors)

- [ ] **Step 5: Commit**

```bash
git add src/KeyMuse.Wpf/MainWindow.xaml src/KeyMuse.Wpf/MainWindow.xaml.cs src/KeyMuse.Wpf/App.xaml
git commit -m "feat: redesign MainWindow with tab bar and card layout"
```

---

### Task 7: RecordingsPage — Recording Tab

**Files:**
- Modify: `src/KeyMuse.Wpf/Pages/RecordingsPage.xaml`
- Modify: `src/KeyMuse.Wpf/Pages/RecordingsPage.xaml.cs`

- [ ] **Step 1: Create RecordingsPage.xaml**

```xml
<UserControl x:Class="KeyMuse.Wpf.Pages.RecordingsPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="280"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Left: Record Controls Card -->
        <Border Grid.Column="0" Grid.Row="0" Grid.RowSpan="2"
                Background="#1e1b3b" BorderBrush="#2a2550" BorderThickness="1"
                CornerRadius="12" Margin="0 0 8 8" Padding="16">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <TextBlock Grid.Row="0" Text="录制控制" FontSize="16" FontWeight="700"
                           Foreground="#00d4ff" Margin="0 0 0 12"/>

                <Border Grid.Row="1" Background="#2a2550" CornerRadius="6" Padding="10">
                    <StackPanel>
                        <TextBlock x:Name="RecStatusText" Text="空闲" FontSize="13" Foreground="#888"/>
                        <TextBlock x:Name="RecDetailsText" Text="F6 开始录制" FontSize="11" Foreground="#555" Margin="0 4 0 0"/>
                    </StackPanel>
                </Border>

                <Button Grid.Row="2" x:Name="RecordBtn"
                        Content="录制" FontSize="14" Height="36" Margin="0 12 0 0"
                        Background="#226622" Foreground="#fff" BorderThickness="0"
                        CornerRadius="6" Click="RecordBtn_Click"/>

                <Separator Grid.Row="3" Background="#2a2550" Margin="0 12"/>

                <TextBlock Grid.Row="4" Text="文件分类" FontSize="12" Foreground="#888" Margin="0 0 0 6"/>

                <TextBox Grid.Row="5" x:Name="CategoryInput" FontSize="12" Height="28"
                         Background="#2a2550" Foreground="#ccc" BorderThickness="0"
                         Padding="8 4"/>
                <Button Grid.Row="6" Content="+ 新建分类" FontSize="11" Height="24" Margin="0 4 0 0"
                        Background="#333" Foreground="#888" BorderThickness="0"
                        CornerRadius="4" Click="NewCategory_Click"/>
            </Grid>
        </Border>

        <!-- Right: Recording Library Card -->
        <Border Grid.Column="1" Grid.Row="0" Grid.RowSpan="3"
                Background="#1e1b3b" BorderBrush="#2a2550" BorderThickness="1"
                CornerRadius="12" Margin="8 0 0 8" Padding="16">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <TextBlock Grid.Row="0" Text="录制库" FontSize="16" FontWeight="700"
                           Foreground="#00d4ff" Margin="0 0 0 8"/>

                <!-- Category chips -->
                <ScrollViewer Grid.Row="1" HorizontalScrollBarVisibility="Auto"
                              VerticalScrollBarVisibility="Disabled" Margin="0 0 0 8">
                    <StackPanel x:Name="CategoryChips" Orientation="Horizontal"/>
                </ScrollViewer>

                <!-- Search -->
                <Border Grid.Row="2" Background="#2a2550" CornerRadius="6" Margin="0 0 0 8" Height="28">
                    <TextBox x:Name="SearchBox" FontSize="12" Background="Transparent"
                             Foreground="#ccc" BorderThickness="0" Padding="8 4"
                             TextChanged="SearchBox_TextChanged"/>
                </Border>

                <!-- Recording list -->
                <ScrollViewer Grid.Row="3">
                    <ItemsControl x:Name="RecordingList">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background="#222" BorderBrush="#2a2550" BorderThickness="1"
                                        CornerRadius="8" Padding="10" Margin="0 0 0 4">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>
                                        <StackPanel Grid.Column="0">
                                            <TextBlock Text="{Binding FileName}" FontSize="13"
                                                       Foreground="#eee" FontWeight="600"/>
                                            <StackPanel Orientation="Horizontal" Margin="0 2 0 0">
                                                <TextBlock Text="{Binding DisplayDuration}" FontSize="11" Foreground="#666" Margin="0 0 12 0"/>
                                                <TextBlock Text="{Binding EventCount, StringFormat={}{0} events}" FontSize="11" Foreground="#666"/>
                                            </StackPanel>
                                        </StackPanel>
                                        <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                                            <Button Content="回放" FontSize="11" Padding="8 2" Margin="0 0 4 0"
                                                    Background="#224466" Foreground="#fff" BorderThickness="0"
                                                    CornerRadius="4" Tag="{Binding FilePath}" Click="ReplayBtn_Click"/>
                                            <Button Content="删" FontSize="11" Padding="6 2"
                                                    Background="#442222" Foreground="#c88" BorderThickness="0"
                                                    CornerRadius="4" Tag="{Binding FilePath}" Click="DeleteBtn_Click"/>
                                        </StackPanel>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </ScrollViewer>
            </Grid>
        </Border>

        <!-- Bottom: AutoClicker Card -->
        <Border Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="2"
                Background="#1e1b3b" BorderBrush="#2a2550" BorderThickness="1"
                CornerRadius="12" Margin="0 8 0 0" Padding="12 8">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0" Text="连点" FontSize="13" Foreground="#ccc" VerticalAlignment="Center" Margin="0 0 12 0"/>
                <TextBlock Grid.Column="1" Text="键:" FontSize="12" Foreground="#888" VerticalAlignment="Center"/>
                <ComboBox Grid.Column="2" x:Name="ClickerKeyCombo" FontSize="12" Width="80"
                          Background="#2a2550" Foreground="#ccc" BorderThickness="0" Margin="4 0"/>
                <WrapPanel Grid.Column="3" VerticalAlignment="Center" Margin="12 0">
                    <TextBlock Text="间隔:" FontSize="12" Foreground="#888" VerticalAlignment="Center"/>
                    <TextBox x:Name="ClickerIntervalBox" FontSize="12" Width="60" Height="24"
                             Background="#2a2550" Foreground="#ccc" BorderThickness="0"
                             Padding="4 1" Margin="4 0" Text="1000"/>
                    <TextBlock Text="ms" FontSize="12" Foreground="#888" VerticalAlignment="Center"/>
                </WrapPanel>
                <Button Grid.Column="4" x:Name="ClickerBtn" Content="启动" FontSize="12" Padding="12 4"
                        Background="#666622" Foreground="#fff" BorderThickness="0"
                        CornerRadius="4" Click="ClickerBtn_Click"/>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create RecordingsPage.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Controls;
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;

namespace KeyMuse.Wpf.Pages;

public partial class RecordingsPage : UserControl
{
    private readonly App _app;
    private string _selectedCategory = "\u5168\u90e8";
    private string? _lastRecordedFile;

    public RecordingsPage(App app)
    {
        InitializeComponent();
        _app = app;

        ClickerKeyCombo.Items.Add("Insert");
        ClickerKeyCombo.Items.Add("F1");
        ClickerKeyCombo.Items.Add("F2");
        ClickerKeyCombo.Items.Add("Space");
        ClickerKeyCombo.SelectedIndex = 0;

        RefreshCategories();
        RefreshRecordings();
    }

    private void RefreshCategories()
    {
        CategoryChips.Children.Clear();
        var allChip = new Button
        {
            Content = "\u5168\u90e8",
            FontSize = 12,
            Height = 26,
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = "\u5168\u90e8"
        };
        allChip.Click += CategoryChip_Click;
        CategoryChips.Children.Add(allChip);

        foreach (var cat in _app.RecordingManager.ListCategories())
        {
            var chip = new Button
            {
                Content = cat,
                FontSize = 12,
                Height = 26,
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = cat
            };
            chip.Click += CategoryChip_Click;
            CategoryChips.Children.Add(chip);
        }
    }

    private void CategoryChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            _selectedCategory = btn.Tag?.ToString() ?? "\u5168\u90e8";
            RefreshRecordings();
        }
    }

    private void RefreshRecordings()
    {
        RecordingInfo[] list;
        if (_selectedCategory == "\u5168\u90e8")
            list = _app.RecordingManager.ListAllRecordings();
        else
            list = _app.RecordingManager.ListRecordings(_selectedCategory);

        var search = SearchBox.Text?.Trim().ToLower() ?? "";
        if (!string.IsNullOrEmpty(search))
            list = list.Where(r => r.FileName.ToLower().Contains(search)).ToArray();

        RecordingList.ItemsSource = list;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshRecordings();
    }

    private async void RecordBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_app.Recorder.IsRecording)
        {
            RecordBtn.IsEnabled = false;
            var tempPath = await _app.Recorder.StopRecordingAsync();
            if (tempPath != null)
            {
                var category = string.IsNullOrWhiteSpace(CategoryInput.Text) ? "\u672a\u5206\u7c7b" : CategoryInput.Text;
                _lastRecordedFile = _app.RecordingManager.SaveRecording(tempPath, category);
                RecStatusText.Text = $"\u5df2\u4fdd\u5b58: {System.IO.Path.GetFileName(_lastRecordedFile)}";
                RefreshCategories();
                RefreshRecordings();
            }
            RecordBtn.Content = "录制";
            RecordBtn.IsEnabled = true;
            RecordBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x22, 0x66, 0x22));
        }
        else
        {
            _app.Recorder.StartRecording();
            RecordBtn.Content = "停止录制";
            RecordBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0x22, 0x22));
            RecStatusText.Text = "录制中...";
        }
    }

    private async void ReplayBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path && System.IO.File.Exists(path))
        {
            var session = await _app.Recorder.LoadSessionAsync(path);
            if (session != null)
            {
                await _app.ReplayEngine.PlayAsync(session, LoopMode.Single);
            }
        }
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            var result = System.Windows.MessageBox.Show(
                $"\u786e\u5b9a\u5220\u9664 '{System.IO.Path.GetFileName(path)}'?",
                "\u786e\u8ba4\u5220\u9664", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _app.RecordingManager.DeleteRecording(path);
                RefreshRecordings();
            }
        }
    }

    private void NewCategory_Click(object sender, RoutedEventArgs e)
    {
        var name = CategoryInput.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            _app.RecordingManager.CreateCategory(name);
            CategoryInput.Text = "";
            RefreshCategories();
            _selectedCategory = name;
            RefreshRecordings();
        }
    }

    private void ClickerBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_app.AutoClicker.IsRunning)
        {
            _app.AutoClicker.Stop();
            ClickerBtn.Content = "启动";
            ClickerBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x22));
        }
        else
        {
            if (int.TryParse(ClickerIntervalBox.Text, out var ms) && ms >= 100)
            {
                _app.AutoClicker.IntervalMs = ms;
                _app.AutoClicker.KeyCode = 0x2D;
                _app.AutoClicker.Start();
                ClickerBtn.Content = "停止";
                ClickerBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0x22, 0x22));
            }
        }
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/KeyMuse.Wpf/KeyMuse.Wpf.csproj --configuration Release`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/KeyMuse.Wpf/Pages/RecordingsPage.xaml src/KeyMuse.Wpf/Pages/RecordingsPage.xaml.cs
git commit -m "feat: add RecordingsPage with categories, library, and auto-clicker"
```

---

### Task 8: WorkflowsPage

**Files:**
- Modify: `src/KeyMuse.Wpf/Pages/WorkflowsPage.xaml`
- Modify: `src/KeyMuse.Wpf/Pages/WorkflowsPage.xaml.cs`

- [ ] **Step 1: Create WorkflowsPage.xaml**

```xml
<UserControl x:Class="KeyMuse.Wpf.Pages.WorkflowsPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="200"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- Left: Workflow list -->
        <Border Grid.Column="0" Background="#1e1b3b" BorderBrush="#2a2550" BorderThickness="1"
                CornerRadius="12" Padding="12" Margin="0 0 8 0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="工作流" FontSize="16" FontWeight="700"
                           Foreground="#00d4ff" Margin="0 0 0 12"/>
                <ScrollViewer Grid.Row="1">
                    <ItemsControl x:Name="WorkflowList">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background="#222" CornerRadius="6" Padding="8 6" Margin="0 0 0 4"
                                        Cursor="Hand" Tag="{Binding}">
                                    <TextBlock Text="{Binding}" FontSize="13" Foreground="#ccc"/>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </ScrollViewer>
                <StackPanel Grid.Row="2">
                    <Button Content="+ 新建" FontSize="12" Height="28" Margin="0 4 0 0"
                            Background="#226644" Foreground="#fff" BorderThickness="0"
                            CornerRadius="4" Click="NewWorkflow_Click"/>
                    <Button x:Name="DeleteWorkflowBtn" Content="删除" FontSize="12" Height="28" Margin="0 4 0 0"
                            Background="#442222" Foreground="#c88" BorderThickness="0"
                            CornerRadius="4" Click="DeleteWorkflow_Click"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Right: Workflow editor -->
        <Border Grid.Column="1" Background="#1e1b3b" BorderBrush="#2a2550" BorderThickness="1"
                CornerRadius="12" Padding="16">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <Grid Grid.Row="0" Margin="0 0 0 8">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="编辑:" FontSize="13" Foreground="#888" VerticalAlignment="Center"/>
                    <TextBox Grid.Column="1" x:Name="WorkflowNameBox" FontSize="14" FontWeight="600"
                             Background="#2a2550" Foreground="#eee" BorderThickness="0"
                             Padding="8 4" Margin="8 0"/>
                    <Button Grid.Column="2" Content="保存" FontSize="12" Padding="12 4"
                            Background="#2266aa" Foreground="#fff" BorderThickness="0"
                            CornerRadius="4" Click="SaveWorkflow_Click"/>
                </Grid>

                <Grid Grid.Row="1" Margin="0 0 0 8">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="总执行次数:" FontSize="12" Foreground="#888" VerticalAlignment="Center"/>
                    <TextBox Grid.Column="1" x:Name="TotalCountBox" FontSize="12" Width="50"
                             Background="#2a2550" Foreground="#eee" BorderThickness="0"
                             Padding="4 2" Margin="4 0" Text="1"/>
                    <TextBlock Grid.Column="2" Text="每步可单独设置次数" FontSize="11" Foreground="#555" VerticalAlignment="Center" Margin="8 0 0 0"/>
                </Grid>

                <TextBlock Grid.Row="2" Text="步骤" FontSize="12" Foreground="#888" Margin="0 0 0 4"/>

                <ScrollViewer Grid.Row="3">
                    <ItemsControl x:Name="StepsList">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background="#222" BorderBrush="#2a2550" BorderThickness="1"
                                        CornerRadius="6" Padding="8" Margin="0 0 0 4">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="20"/>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="60"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>
                                        <TextBlock Grid.Column="0" Text="{Binding Index}" FontSize="12" Foreground="#888" VerticalAlignment="Center"/>
                                        <TextBlock Grid.Column="1" Text="{Binding FileName}" FontSize="12" Foreground="#ccc" VerticalAlignment="Center" Margin="0 0 8 0"/>
                                        <TextBox Grid.Column="2" Text="{Binding Count}" FontSize="12" Width="50"
                                                 Background="#2a2550" Foreground="#eee" BorderThickness="0"
                                                 Padding="4 2" VerticalAlignment="Center"/>
                                        <Button Grid.Column="3" Content="×" FontSize="11" Padding="4 0"
                                                Background="#442222" Foreground="#c88" BorderThickness="0"
                                                CornerRadius="3" Tag="{Binding}" Click="RemoveStep_Click"/>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </ScrollViewer>

                <Button Grid.Row="4" Content="+ 添加步骤" FontSize="12" Height="28" Margin="0 8"
                        Background="#333" Foreground="#888" BorderThickness="0"
                        CornerRadius="4" Click="AddStep_Click"/>

                <Border Grid.Row="5" Background="#2a2550" CornerRadius="8" Padding="12 8">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <StackPanel Grid.Column="0" VerticalAlignment="Center">
                            <TextBlock x:Name="WfProgressText" Text="就绪" FontSize="12" Foreground="#888"/>
                            <ProgressBar x:Name="WfProgressBar" Height="6" Margin="0 4 0 0"
                                         Foreground="#00d4ff" Background="#333" BorderThickness="0"/>
                        </StackPanel>
                        <StackPanel Grid.Column="1" Orientation="Horizontal">
                            <Button x:Name="ExecuteWorkflowBtn" Content="执行工作流" FontSize="12" Padding="12 4"
                                    Background="#226622" Foreground="#fff" BorderThickness="0"
                                    CornerRadius="4" Margin="0 0 4 0" Click="ExecuteWorkflow_Click"/>
                            <Button x:Name="StopWorkflowBtn" Content="停止" FontSize="12" Padding="12 4"
                                    Background="#662222" Foreground="#c88" BorderThickness="0"
                                    CornerRadius="4" IsEnabled="False" Click="StopWorkflow_Click"/>
                        </StackPanel>
                    </Grid>
                </Border>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create WorkflowsPage.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Controls;
using KeyMuse.Core.Models;

namespace KeyMuse.Wpf.Pages;

public partial class WorkflowsPage : UserControl
{
    private readonly App _app;
    private WorkflowModel? _currentWorkflow;
    private string? _selectedName;

    public WorkflowsPage(App app)
    {
        InitializeComponent();
        _app = app;
        RefreshList();
    }

    private void RefreshList()
    {
        var names = _app.WorkflowManager.ListWorkflowNames();
        WorkflowList.ItemsSource = names;
    }

    private void LoadWorkflow(string name)
    {
        _selectedName = name;
        _currentWorkflow = _app.WorkflowManager.LoadWorkflow(name);
        if (_currentWorkflow != null)
        {
            WorkflowNameBox.Text = _currentWorkflow.Name;
            TotalCountBox.Text = _currentWorkflow.TotalCount.ToString();
            RefreshSteps();
        }
    }

    private void RefreshSteps()
    {
        if (_currentWorkflow == null) return;
        var items = _currentWorkflow.Steps.Select((s, i) => new
        {
            Index = i + 1,
            FileName = System.IO.Path.GetFileNameWithoutExtension(s.RecordingFilePath),
            FilePath = s.RecordingFilePath,
            Count = s.Count
        }).ToList();
        StepsList.ItemsSource = items;
    }

    private void NewWorkflow_Click(object sender, RoutedEventArgs e)
    {
        var baseName = "\u65b0\u5de5\u4f5c\u6d41";
        var name = baseName;
        int i = 1;
        while (_app.WorkflowManager.WorkflowExists(name))
            name = $"{baseName}{i++}";

        _currentWorkflow = new WorkflowModel { Name = name };
        _app.WorkflowManager.SaveWorkflow(_currentWorkflow);
        RefreshList();
        LoadWorkflow(name);
    }

    private void DeleteWorkflow_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedName == null) return;
        var result = System.Windows.MessageBox.Show(
            $"\u786e\u5b9a\u5220\u9664\u5de5\u4f5c\u6d41 '{_selectedName}'?",
            "\u786e\u8ba4\u5220\u9664", MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes)
        {
            _app.WorkflowManager.DeleteWorkflow(_selectedName);
            _currentWorkflow = null;
            _selectedName = null;
            WorkflowNameBox.Text = "";
            StepsList.ItemsSource = null;
            RefreshList();
        }
    }

    private void SaveWorkflow_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) return;
        _currentWorkflow.Name = WorkflowNameBox.Text.Trim();
        if (int.TryParse(TotalCountBox.Text, out var total))
            _currentWorkflow.TotalCount = total;
        _app.WorkflowManager.SaveWorkflow(_currentWorkflow);
        RefreshList();
    }

    private void AddStep_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) return;

        var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "KeyMuse \u5f55\u5236\u6587\u4ef6 (*.keymuse)|*.keymuse|" +
                     "\u6240\u6709\u6587\u4ef6 (*.*)|*.*",
            Title = "\u9009\u62e9\u5f55\u5236\u6587\u4ef6"
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _currentWorkflow.Steps.Add(new WorkflowStep
            {
                RecordingFilePath = dialog.FileName,
                Count = 1
            });
            RefreshSteps();
        }
    }

    private void RemoveStep_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null || sender is not Button btn) return;
        var item = btn.Tag;

        var step = _currentWorkflow.Steps.FirstOrDefault(s =>
            System.IO.Path.GetFileNameWithoutExtension(s.RecordingFilePath) ==
            item?.GetType().GetProperty("FileName")?.GetValue(item)?.ToString());

        if (step != null)
        {
            _currentWorkflow.Steps.Remove(step);
            RefreshSteps();
        }
    }

    private async void ExecuteWorkflow_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) return;
        SaveWorkflow_Click(sender, e);

        var executor = new KeyMuse.Core.Services.WorkflowExecutor(_app.ReplayEngine, _app.Coordinator);
        executor.OnProgress += p =>
        {
            Dispatcher.Invoke(() =>
            {
                if (p.IsCompleted)
                {
                    WfProgressText.Text = $"\u5df2\u5b8c\u6210!";
                    WfProgressBar.Value = 1;
                    ExecuteWorkflowBtn.IsEnabled = true;
                    StopWorkflowBtn.IsEnabled = false;
                }
                else if (p.ErrorMessage != null)
                {
                    WfProgressText.Text = $"\u9519\u8bef: {p.ErrorMessage}";
                    ExecuteWorkflowBtn.IsEnabled = true;
                    StopWorkflowBtn.IsEnabled = false;
                }
                else
                {
                    WfProgressText.Text = $"\u6b65\u9aa4 {p.CurrentStepIndex + 1}/{p.TotalSteps} - " +
                                          $"\u5f53\u524d: {p.CurrentOverallCount}/{p.TotalOverallCount}";
                    WfProgressBar.Value = p.TotalOverallCount > 0
                        ? (double)p.CurrentOverallCount / p.TotalOverallCount
                        : 0;
                }
            });
        };
        executor.OnError += msg =>
        {
            Dispatcher.Invoke(() =>
            {
                WfProgressText.Text = $"\u9519\u8bef: {msg}";
            });
        };

        ExecuteWorkflowBtn.IsEnabled = false;
        StopWorkflowBtn.IsEnabled = true;
        await executor.ExecuteAsync(_currentWorkflow);
    }

    private void StopWorkflow_Click(object sender, RoutedEventArgs e)
    {
        _app.ReplayEngine.Stop();
        ExecuteWorkflowBtn.IsEnabled = true;
        StopWorkflowBtn.IsEnabled = false;
        WfProgressText.Text = "\u5df2\u4e2d\u6b62";
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/KeyMuse.Wpf/KeyMuse.Wpf.csproj --configuration Release`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/KeyMuse.Wpf/Pages/WorkflowsPage.xaml src/KeyMuse.Wpf/Pages/WorkflowsPage.xaml.cs
git commit -m "feat: add WorkflowsPage with editor and executor"
```

---

### Task 9: SettingsPage

**Files:**
- Modify: `src/KeyMuse.Wpf/Pages/SettingsPage.xaml`
- Modify: `src/KeyMuse.Wpf/Pages/SettingsPage.xaml.cs`

- [ ] **Step 1: Create SettingsPage.xaml**

```xml
<UserControl x:Class="KeyMuse.Wpf.Pages.SettingsPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="#1e1b3b" BorderBrush="#2a2550" BorderThickness="1"
            CornerRadius="12" Padding="24">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <TextBlock Grid.Row="0" Text="设置" FontSize="18" FontWeight="700"
                       Foreground="#00d4ff" Margin="0 0 0 20"/>

            <!-- Profile config -->
            <Border Grid.Row="1" Background="#222" CornerRadius="8" Padding="16" Margin="0 0 0 12">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid Grid.Row="0" Margin="0 0 0 8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="配置管理" FontSize="14" FontWeight="600"
                                   Foreground="#ddd" VerticalAlignment="Center"/>
                        <ComboBox Grid.Column="1" x:Name="ProfileCombo" FontSize="12" Width="120"
                                  Background="#2a2550" Foreground="#ccc" BorderThickness="0"
                                  Margin="12 0" SelectionChanged="Profile_SelectionChanged"/>
                        <Button Grid.Column="2" Content="+ 新建" FontSize="11" Padding="8 2"
                                Background="#226644" Foreground="#fff" BorderThickness="0"
                                CornerRadius="4" Margin="0 0 4 0" Click="NewProfile_Click"/>
                        <Button Grid.Column="3" Content="删除" FontSize="11" Padding="8 2"
                                Background="#442222" Foreground="#c88" BorderThickness="0"
                                CornerRadius="4" Click="DeleteProfile_Click"/>
                    </Grid>
                    <Grid Grid.Row="1">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="连点间隔:" FontSize="12" Foreground="#888" VerticalAlignment="Center"/>
                        <TextBox Grid.Column="1" x:Name="ClickIntervalBox" FontSize="12" Width="60"
                                 Background="#2a2550" Foreground="#eee" BorderThickness="0"
                                 Padding="4 2" Margin="8 0" Text="1000"/>
                    </Grid>
                </Grid>
            </Border>

            <!-- Hotkeys -->
            <Border Grid.Row="2" Background="#222" CornerRadius="8" Padding="16" Margin="0 0 0 12">
                <StackPanel>
                    <TextBlock Text="快捷键" FontSize="14" FontWeight="600" Foreground="#ddd" Margin="0 0 0 8"/>
                    <TextBlock FontSize="12" Foreground="#888">
                        F6 = 录制 | F7 = 回放 | F8 = 连点 | F9 = 急停 | F10 = 窗口
                    </TextBlock>
                </StackPanel>
            </Border>

            <!-- Storage -->
            <Border Grid.Row="3" Background="#222" CornerRadius="8" Padding="16" Margin="0 0 0 12">
                <StackPanel>
                    <TextBlock Text="存储位置" FontSize="14" FontWeight="600" Foreground="#ddd" Margin="0 0 0 8"/>
                    <TextBlock x:Name="StoragePathText" FontSize="11" Foreground="#666"/>
                </StackPanel>
            </Border>

            <!-- Buttons -->
            <StackPanel Grid.Row="4" Orientation="Horizontal" Margin="0 0 0 12">
                <Button Content="导出配置" FontSize="12" Padding="12 6" Margin="0 0 8 0"
                        Background="#333" Foreground="#ccc" BorderThickness="0"
                        CornerRadius="4" Click="ExportConfig_Click"/>
                <Button Content="导入配置" FontSize="12" Padding="12 6"
                        Background="#333" Foreground="#ccc" BorderThickness="0"
                        CornerRadius="4" Click="ImportConfig_Click"/>
            </StackPanel>

            <!-- About -->
            <Border Grid.Row="5" Background="#222" CornerRadius="8" Padding="16">
                <TextBlock FontSize="11" Foreground="#555">
                    KeyMuse v0.1.0 | Keyboard &amp; Mouse Automation Tool
                </TextBlock>
            </Border>
        </Grid>
    </Border>
</UserControl>
```

- [ ] **Step 2: Create SettingsPage.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Controls;

namespace KeyMuse.Wpf.Pages;

public partial class SettingsPage : UserControl
{
    private readonly App _app;

    public SettingsPage(App app)
    {
        InitializeComponent();
        _app = app;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        StoragePathText.Text = $"\u5f55\u5236: {appData}\\KeyMuse\\recordings\n" +
                               $"\u914d\u7f6e: {appData}\\KeyMuse\\profiles\n" +
                               $"\u5de5\u4f5c\u6d41: {appData}\\KeyMuse\\workflows";

        RefreshProfiles();
    }

    private void RefreshProfiles()
    {
        ProfileCombo.Items.Clear();
        var profiles = _app.ConfigManager.ListProfiles();
        foreach (var p in profiles) ProfileCombo.Items.Add(p);
        if (profiles.Length > 0) ProfileCombo.SelectedIndex = 0;
    }

    private void Profile_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is string name)
        {
            _app.ConfigManager.LoadProfile(name);
            var config = _app.ConfigManager.Current;
            if (config != null)
                ClickIntervalBox.Text = config.AutoClickIntervalMs.ToString();
        }
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new TextBox { Width = 200 };
        var win = new Window
        {
            Title = "\u65b0\u5efa\u914d\u7f6e",
            Content = new StackPanel
            {
                Margin = new Thickness(10),
                Children =
                {
                    new Label { Content = "\u8bf7\u8f93\u5165\u914d\u7f6e\u540d\u79f0\uff1a" },
                    dialog,
                    new Button { Content = "\u786e\u5b9a", Margin = new Thickness(0, 10, 0, 0), IsDefault = true }
                }
            },
            Width = 300, Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        };
        var btn = (Button)((StackPanel)win.Content).Children[2];
        btn.Click += (_, _) => { win.DialogResult = true; win.Close(); };
        if (win.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Text))
        {
            _app.ConfigManager.CreateProfile(dialog.Text);
            RefreshProfiles();
            ProfileCombo.SelectedItem = dialog.Text;
        }
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is string name)
        {
            var result = System.Windows.MessageBox.Show(
                $"\u786e\u5b9a\u5220\u9664\u914d\u7f6e '{name}'?", "\u786e\u8ba4\u5220\u9664",
                MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _app.ConfigManager.DeleteProfile(name);
                RefreshProfiles();
            }
        }
    }

    private void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.SaveFileDialog
        {
            Filter = "KeyMuse Profile (*.keymuse-profile)|*.keymuse-profile",
            Title = "\u5bfc\u51fa\u914d\u7f6e"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _app.ConfigManager.ExportProfile(dialog.FileName, ProfileCombo.SelectedItem?.ToString());
        }
    }

    private void ImportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "KeyMuse Profile (*.keymuse-profile)|*.keymuse-profile|\u6240\u6709\u6587\u4ef6 (*.*)|*.*",
            Title = "\u5bfc\u5165\u914d\u7f6e"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _app.ConfigManager.ImportProfile(dialog.FileName);
            RefreshProfiles();
        }
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/KeyMuse.Wpf/KeyMuse.Wpf.csproj --configuration Release`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/KeyMuse.Wpf/Pages/SettingsPage.xaml src/KeyMuse.Wpf/Pages/SettingsPage.xaml.cs
git commit -m "feat: add SettingsPage with profile, hotkeys, storage info"
```

---

### Task 10: Wire up App.xaml.cs — register new services

**Files:**
- Modify: `src/KeyMuse.Wpf/App.xaml.cs`

- [ ] **Step 1: Add RecordingManager and WorkflowManager to App**

Add properties:
```csharp
public RecordingManager RecordingManager { get; } = new();
public WorkflowManager WorkflowManager { get; } = new();
```

Insert after line with ConfigManager:
```csharp
public ConfigManager ConfigManager { get; } = new();
public RecordingManager RecordingManager { get; } = new();
public WorkflowManager WorkflowManager { get; } = new();
public StatusMessageQueue MessageQueue { get; } = new();
```

- [ ] **Step 2: Fully rebuild and test**

Run:
```bash
dotnet build --configuration Release
dotnet test tests/KeyMuse.Tests/KeyMuse.Tests.csproj --configuration Release --no-build
```
Expected: All 26+ existing tests pass + new tests

- [ ] **Step 3: Commit**

```bash
git add src/KeyMuse.Wpf/App.xaml.cs
git commit -m "feat: register RecordingManager and WorkflowManager in App"
```

---

### Task 11: Full integration test and publish

**Files:**
- Modify: `src/KeyMuse.Wpf/KeyMuse.Wpf.csproj` (if Pages need namespace registration)

- [ ] **Step 1: Full build + test**

Run: `dotnet build --configuration Release && dotnet test --configuration Release --no-build`
Expected: All tests pass

- [ ] **Step 2: Single-file publish**

Run:
```bash
dotnet publish src/KeyMuse.Wpf/KeyMuse.Wpf.csproj --configuration Release --output publish/v0.0.2 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded
```

- [ ] **Step 3: Local smoke test (start and verify no crash)**

Run the published exe, wait 5s, check crash log.

- [ ] **Step 4: Commit all remaining changes**

```bash
git add --all
git commit -m "feat: recording management, workflow system, and card UI redesign"
git push origin main
```

- [ ] **Step 5: Upload to GitHub Release**

Upload `publish/v0.0.2/KeyMuse.Wpf.exe` to the v0.0.2 release.
