using System.Collections.Concurrent;
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class StatusMessageQueue : IDisposable
{
    private readonly ConcurrentQueue<StatusMessage> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void Enqueue(StatusMessage message)
    {
        _queue.Enqueue(message);
        _signal.Release();
    }

    public bool TryDequeue(out StatusMessage message)
    {
        return _queue.TryDequeue(out message);
    }

    public async Task<StatusMessage> WaitAsync(CancellationToken token = default)
    {
        await _signal.WaitAsync(token);
        _queue.TryDequeue(out var message);
        return message;
    }

    public void Dispose()
    {
        _signal.Dispose();
        GC.SuppressFinalize(this);
    }
}
