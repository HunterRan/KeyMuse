using KeyMuse.Core.Models;
using KeyMuse.Core.Services;
using Xunit;

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
        Assert.Contains("不存在", error);
    }
}
