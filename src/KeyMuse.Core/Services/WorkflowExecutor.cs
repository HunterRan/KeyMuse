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
                        var err = $"步骤 {stepIdx + 1}: 文件不存在 - {step.RecordingFilePath}";
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
                            var err = $"步骤 {stepIdx + 1}: 无法加载录制文件";
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
                ErrorMessage = "已中止"
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
                        X = binReader.ReadInt32(), Y = binReader.ReadInt32(),
                        RelX = binReader.ReadInt32(), RelY = binReader.ReadInt32(),
                        MouseData = binReader.ReadInt32(),
                        WindowHandle = (nint)binReader.ReadInt64(),
                        WindowLeft = binReader.ReadInt32(), WindowTop = binReader.ReadInt32(),
                        WindowWidth = binReader.ReadInt32(), WindowHeight = binReader.ReadInt32()
                    });
                }
            }
            return session;
        }
        catch { return null; }
    }
}
