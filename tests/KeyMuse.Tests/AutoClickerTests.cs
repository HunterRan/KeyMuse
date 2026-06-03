using Xunit;
using KeyMuse.Core.Services;

namespace KeyMuse.Tests;

public class AutoClickerTests
{
    [Fact]
    public void StartStop_TogglesIsRunning()
    {
        var coordinator = new InputCoordinator();
        var clicker = new AutoClicker(coordinator)
        {
            IntervalMs = 50000,
            KeyCode = 0x2D
        };

        Assert.False(clicker.IsRunning);
        clicker.Start();
        Assert.True(clicker.IsRunning);
        clicker.Stop();
        Assert.False(clicker.IsRunning);
    }

    [Fact]
    public async Task StartStop_ResetsClickCount()
    {
        var coordinator = new InputCoordinator();
        var clicker = new AutoClicker(coordinator)
        {
            IntervalMs = 50000,
            KeyCode = 0x2D
        };

        clicker.Start();
        Assert.Equal(0, clicker.ClickCount);
        await Task.Delay(100);
        clicker.Stop();
    }

    [Fact]
    public void DoubleStart_DoesNotThrow()
    {
        var coordinator = new InputCoordinator();
        var clicker = new AutoClicker(coordinator)
        {
            IntervalMs = 50000,
            KeyCode = 0x2D
        };

        clicker.Start();
        clicker.Start();
        clicker.Stop();
    }

    [Fact]
    public void DoubleStop_DoesNotThrow()
    {
        var coordinator = new InputCoordinator();
        var clicker = new AutoClicker(coordinator)
        {
            IntervalMs = 50000,
            KeyCode = 0x2D
        };

        clicker.Stop();
        clicker.Stop();
    }
}
