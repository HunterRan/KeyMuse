using System.Diagnostics;
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public enum LoopMode
{
    Single,
    Count,
    Infinite
}

public class ReplayEngine
{
    private readonly InputCoordinator _coordinator;
    private CancellationTokenSource? _cts;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;
    public int CurrentLoop { get; private set; }
    public int TotalLoops { get; private set; }
    public int CurrentEventIndex { get; private set; }
    public int TotalEvents { get; private set; }

    public event Action<StatusMessage>? OnStatusChanged;
    public event Action? OnCompleted;

    public ReplayEngine(InputCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public async Task PlayAsync(RecordingSession session, LoopMode mode = LoopMode.Single,
        int loopCount = 1, int loopIntervalMs = 0, CancellationToken externalToken = default)
    {
        if (_isPlaying) return;
        _isPlaying = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        var token = _cts.Token;
        CurrentLoop = 0;
        TotalEvents = session.EventCount;

        var loops = mode switch
        {
            LoopMode.Single => 1,
            LoopMode.Count => loopCount,
            LoopMode.Infinite => int.MaxValue,
            _ => 1
        };
        TotalLoops = loops;

        try
        {
            for (int loop = 0; loop < loops && !token.IsCancellationRequested; loop++)
            {
                CurrentLoop = loop + 1;
                OnStatusChanged?.Invoke(new StatusMessage
                {
                    Type = StatusMessageType.Replaying,
                    Text = $"回放中 - 第 {CurrentLoop}/{TotalLoops} 轮",
                    ProgressCurrent = 0,
                    ProgressTotal = TotalEvents
                });

                using (await _coordinator.AcquireAsync(token))
                {
                    await ReplayOnceAsync(session, token);
                }

                if (loop < loops - 1 && !token.IsCancellationRequested && loopIntervalMs > 0)
                {
                    await Task.Delay(loopIntervalMs, token);
                }
            }

            if (!token.IsCancellationRequested)
            {
                OnStatusChanged?.Invoke(new StatusMessage
                {
                    Type = StatusMessageType.Idle,
                    Text = "回放完成",
                    ProgressCurrent = TotalEvents,
                    ProgressTotal = TotalEvents
                });
                OnCompleted?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            OnStatusChanged?.Invoke(new StatusMessage
            {
                Type = StatusMessageType.Idle,
                Text = "回放已停止",
                ProgressCurrent = CurrentEventIndex,
                ProgressTotal = TotalEvents
            });
        }
        finally
        {
            _isPlaying = false;
        }
    }

    private async Task ReplayOnceAsync(RecordingSession session, CancellationToken token)
    {
        var sender = _coordinator.Sender;
        var sw = Stopwatch.StartNew();
        int accumulatedOriginal = 0;

        for (int i = 0; i < session.EventCount; i++)
        {
            token.ThrowIfCancellationRequested();
            CurrentEventIndex = i + 1;

            var evt = session.Events[i];

            if (i > 0 && evt.TimeOffsetMs > accumulatedOriginal)
            {
                var waitMs = evt.TimeOffsetMs - sw.ElapsedMilliseconds;
                if (waitMs > 0)
                {
                    await Task.Delay((int)Math.Max(0, waitMs), token);
                }
            }

            SendEvent(sender, evt);
            accumulatedOriginal = evt.TimeOffsetMs;

            OnStatusChanged?.Invoke(new StatusMessage
            {
                Type = StatusMessageType.Replaying,
                Text = $"回放中 - 第 {CurrentLoop}/{TotalLoops} 轮",
                ProgressCurrent = CurrentEventIndex,
                ProgressTotal = TotalEvents
            });
        }
    }

    private static void SendEvent(InputSender sender, InputEvent evt)
    {
        switch (evt.Type)
        {
            case InputEventType.KeyDown:
                sender.SendKeyDown(evt.VirtualKeyCode);
                break;
            case InputEventType.KeyUp:
                sender.SendKeyUp(evt.VirtualKeyCode);
                break;
            case InputEventType.MouseMove:
                sender.SendMouseMove(evt.X, evt.Y);
                break;
            case InputEventType.MouseDown:
                sender.SendMouseDown(evt.MouseData);
                break;
            case InputEventType.MouseUp:
                sender.SendMouseUp(evt.MouseData);
                break;
            case InputEventType.MouseWheel:
                sender.SendMouseWheel(evt.MouseData);
                break;
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}
