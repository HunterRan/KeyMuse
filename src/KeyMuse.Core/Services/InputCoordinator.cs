namespace KeyMuse.Core.Services;

public class InputCoordinator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly InputSender _sender = new();

    public InputSender Sender => _sender;

    public async Task<IDisposable> AcquireAsync(CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token);
        return new Releaser(_semaphore);
    }

    private class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _sem;
        public Releaser(SemaphoreSlim sem) => _sem = sem;
        public void Dispose() => _sem.Release();
    }
}
