using Xunit;
using KeyMuse.Core.Services;

namespace KeyMuse.Tests;

public class InputCoordinatorTests
{
    [Fact]
    public async Task AcquireAsync_ReturnsReleaser()
    {
        var coordinator = new InputCoordinator();
        using var releaser = await coordinator.AcquireAsync();
        Assert.NotNull(releaser);
    }

    [Fact]
    public async Task AcquireAsync_BlocksSecondCaller()
    {
        var coordinator = new InputCoordinator();
        var releaser = await coordinator.AcquireAsync();

        var blocked = true;
        var task = Task.Run(async () =>
        {
            using var r2 = await coordinator.AcquireAsync();
            blocked = false;
        });

        await Task.Delay(200);
        Assert.True(blocked);

        releaser.Dispose();
        await Task.Delay(100);
        Assert.False(blocked);
    }

    [Fact]
    public async Task AcquireAsync_MultipleRelease_DoesNotThrow()
    {
        var coordinator = new InputCoordinator();
        var releaser = await coordinator.AcquireAsync();
        releaser.Dispose();

        using var releaser2 = await coordinator.AcquireAsync();
        Assert.NotNull(releaser2);
    }

    [Fact]
    public void Sender_IsNotNull()
    {
        var coordinator = new InputCoordinator();
        Assert.NotNull(coordinator.Sender);
    }
}
