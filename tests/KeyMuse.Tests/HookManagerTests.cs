using Xunit;
using KeyMuse.Core.Services;

namespace KeyMuse.Tests;

public class HookManagerTests
{
    [Fact]
    public void StartStop_DoesNotThrow()
    {
        using var hook = new HookManager();
        hook.Start();
        Assert.True(hook.IsRunning);
        hook.Stop();
        Assert.False(hook.IsRunning);
    }

    [Fact]
    public void DoubleStart_DoesNotThrow()
    {
        using var hook = new HookManager();
        hook.Start();
        hook.Start();
        hook.Stop();
    }

    [Fact]
    public void DoubleStop_DoesNotThrow()
    {
        using var hook = new HookManager();
        hook.Stop();
        hook.Stop();
    }

    [Fact]
    public void Dispose_StopsHook()
    {
        var hook = new HookManager();
        hook.Start();
        hook.Dispose();
        Assert.False(hook.IsRunning);
    }
}
