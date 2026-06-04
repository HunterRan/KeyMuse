using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class Recorder : IDisposable
{
    private readonly HookManager _hookManager;
    private readonly ConcurrentQueue<InputEvent> _buffer = new();
    private CancellationTokenSource? _cts;
    private Task? _flushTask;
    private int _startTick;
    private bool _isRecording;
    private readonly List<string> _recentEventDescs = new(capacity: 5);
    private readonly string _tempDir;

    public bool IsRecording => _isRecording;
    public int EventCount => _buffer.Count;

    public event Action<StatusMessage>? OnStatusChanged;

    public Recorder(HookManager hookManager)
    {
        _hookManager = hookManager;
        _tempDir = Path.Combine(Path.GetTempPath(), "KeyMuse");
        Directory.CreateDirectory(_tempDir);
    }

    public void StartRecording()
    {
        if (_isRecording) return;
        _isRecording = true;
        _startTick = Environment.TickCount;
        _cts = new CancellationTokenSource();
        _buffer.Clear();
        _hookManager.OnInputEvent += OnInputEventHandler;
        _flushTask = Task.Run(() => FlushLoop(_cts.Token));

        OnStatusChanged?.Invoke(new StatusMessage
        {
            Type = StatusMessageType.Recording,
            Text = "录制中...",
            ProgressCurrent = 0,
            ProgressTotal = 0
        });
    }

    private void OnInputEventHandler(InputEvent evt)
    {
        if (!_isRecording) return;
        evt.TimeOffsetMs = Environment.TickCount - _startTick;
        _buffer.Enqueue(evt);

        lock (_recentEventDescs)
        {
            _recentEventDescs.Add(evt.Description);
            if (_recentEventDescs.Count > 5)
                _recentEventDescs.RemoveAt(0);
        }
    }

    private async Task FlushLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(100, token);
            string[]? snapshot;
            lock (_recentEventDescs)
            {
                snapshot = _recentEventDescs.Count > 0 ? _recentEventDescs.ToArray() : null;
            }
            OnStatusChanged?.Invoke(new StatusMessage
            {
                Type = StatusMessageType.Recording,
                Text = $"录制中 - 已捕获 {_buffer.Count} 个事件",
                Detail = snapshot?.LastOrDefault() ?? "",
                RecentEvents = snapshot,
                RecentEventIndex = (snapshot?.Length ?? 1) - 1,
                ProgressCurrent = _buffer.Count,
                ProgressTotal = 0
            });
        }
    }

    public async Task<string?> StopRecordingAsync()
    {
        if (!_isRecording) return null;
        _isRecording = false;
        _hookManager.OnInputEvent -= OnInputEventHandler;
        _cts?.Cancel();

        if (_flushTask != null)
        {
            try { await _flushTask; } catch (OperationCanceledException) { }
        }

        var session = new RecordingSession
        {
            CreatedAt = DateTime.Now,
            DurationMs = Environment.TickCount - _startTick,
            Events = _buffer.ToList()
        };

        if (session.Events.Count > 0)
        {
            var first = session.Events[0];
            session.TargetWindowWidth = first.WindowWidth;
            session.TargetWindowHeight = first.WindowHeight;
            session.TargetWindowTitle = "Auto-detected";
        }

        var tempFile = Path.Combine(_tempDir, $"recording_{Guid.NewGuid():N}.tmp");
        await SaveSessionAsync(session, tempFile);

        var finalFile = Path.Combine(_tempDir, $"recording_{DateTime.Now:yyyyMMdd-HHmmss}.keymuse");
        File.Move(tempFile, finalFile);

        OnStatusChanged?.Invoke(new StatusMessage
        {
            Type = StatusMessageType.Idle,
            Text = $"录制完成 - {session.EventCount} 个事件",
            ProgressCurrent = session.EventCount,
            ProgressTotal = 0
        });

        return finalFile;
    }

    public async Task<RecordingSession?> LoadSessionAsync(string filePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);
            var entry = archive.GetEntry("session.json");
            if (entry == null) return null;

            using var reader = new StreamReader(entry.Open());
            var json = await reader.ReadToEndAsync();
            var session = JsonSerializer.Deserialize<RecordingSession>(json);
            if (session == null) return null;

            var eventsEntry = archive.GetEntry("events.bin");
            if (eventsEntry != null)
            {
                using var binReader = new BinaryReader(eventsEntry.Open());
                session.Events = ReadEvents(binReader);
            }

            return session;
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveSessionAsync(RecordingSession session, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        var jsonEntry = archive.CreateEntry("session.json");
        using (var writer = new StreamWriter(jsonEntry.Open()))
        {
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            await writer.WriteAsync(json);
        }

        var eventsEntry = archive.CreateEntry("events.bin");
        using (var binWriter = new BinaryWriter(eventsEntry.Open()))
        {
            WriteEvents(binWriter, session.Events);
        }
    }

    private static void WriteEvents(BinaryWriter writer, List<InputEvent> events)
    {
        writer.Write(events.Count);
        foreach (var e in events)
        {
            writer.Write(e.TimeOffsetMs);
            writer.Write((int)e.Type);
            writer.Write(e.VirtualKeyCode);
            writer.Write(e.X);
            writer.Write(e.Y);
            writer.Write(e.RelX);
            writer.Write(e.RelY);
            writer.Write(e.MouseData);
            writer.Write(e.WindowHandle.ToInt64());
            writer.Write(e.WindowLeft);
            writer.Write(e.WindowTop);
            writer.Write(e.WindowWidth);
            writer.Write(e.WindowHeight);
        }
    }

    private static List<InputEvent> ReadEvents(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        var events = new List<InputEvent>(count);
        for (int i = 0; i < count; i++)
        {
            events.Add(new InputEvent
            {
                TimeOffsetMs = reader.ReadInt32(),
                Type = (InputEventType)reader.ReadInt32(),
                VirtualKeyCode = reader.ReadInt32(),
                X = reader.ReadInt32(),
                Y = reader.ReadInt32(),
                RelX = reader.ReadInt32(),
                RelY = reader.ReadInt32(),
                MouseData = reader.ReadInt32(),
                WindowHandle = (nint)reader.ReadInt64(),
                WindowLeft = reader.ReadInt32(),
                WindowTop = reader.ReadInt32(),
                WindowWidth = reader.ReadInt32(),
                WindowHeight = reader.ReadInt32()
            });
        }
        return events;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        GC.SuppressFinalize(this);
    }
}
