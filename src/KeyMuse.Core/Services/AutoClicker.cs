using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class AutoClicker
{
    private readonly InputCoordinator _coordinator;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public int ClickCount { get; private set; }
    public int IntervalMs { get; set; } = 1000;
    public int KeyCode { get; set; }
    public bool ToggleMode { get; set; } = true;

    public event Action<StatusMessage>? OnStatusChanged;

    public AutoClicker(InputCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        ClickCount = 0;
        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);

        OnStatusChanged?.Invoke(new StatusMessage
        {
            Type = StatusMessageType.AutoClicking,
            Text = $"连点中 - 间隔 {IntervalMs}ms",
            ProgressCurrent = 0,
            ProgressTotal = 0
        });
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();

        OnStatusChanged?.Invoke(new StatusMessage
        {
            Type = StatusMessageType.Idle,
            Text = $"连点已停止 - 共点击 {ClickCount} 次",
            ProgressCurrent = ClickCount,
            ProgressTotal = 0
        });
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            var lastFailCount = _coordinator.Sender.FailCount;
            while (!token.IsCancellationRequested)
            {
                using (await _coordinator.AcquireAsync(token))
                {
                    var sender = _coordinator.Sender;
                    if (KeyCode < 0)
                    {
                        sender.SendMouseDown(0);
                        await Task.Delay(50, token);
                        sender.SendMouseUp(0);
                    }
                    else
                    {
                        sender.SendKeyDown(KeyCode);
                        await Task.Delay(50, token);
                        sender.SendKeyUp(KeyCode);
                    }
                }

                ClickCount++;
                var currentFailCount = _coordinator.Sender.FailCount;
                if (currentFailCount > lastFailCount)
                {
                    lastFailCount = currentFailCount;
                    OnStatusChanged?.Invoke(new StatusMessage
                    {
                        Type = StatusMessageType.Warning,
                        Text = "模拟操作被拦截！请以管理员身份运行 KeyMuse 或检查安全软件",
                        ProgressCurrent = ClickCount,
                        ProgressTotal = 0
                    });
                }
                else
                {
                    OnStatusChanged?.Invoke(new StatusMessage
                    {
                        Type = StatusMessageType.AutoClicking,
                        Text = $"连点中 - 间隔 {IntervalMs}ms",
                        ProgressCurrent = ClickCount,
                        ProgressTotal = 0
                    });
                }

                await Task.Delay(IntervalMs, token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isRunning = false;
        }
    }
}
