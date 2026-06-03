using KeyMuse.Core.Models;
using KeyMuse.Core.Services;
using Xunit;

namespace KeyMuse.Tests;

public class WorkflowManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly WorkflowManager _manager;

    public WorkflowManagerTests()
    {
        _testDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KMWfTest_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_testDir);
        _manager = new WorkflowManager(_testDir);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_testDir))
            System.IO.Directory.Delete(_testDir, true);
    }

    [Fact]
    public void ListWorkflowNames_Empty_ReturnsEmpty()
    {
        Assert.Empty(_manager.ListWorkflowNames());
    }

    [Fact]
    public void SaveAndLoadWorkflow()
    {
        var wf = new WorkflowModel
        {
            Name = "test_flow",
            TotalCount = 3,
            Steps = new List<WorkflowStep>
            {
                new() { RecordingFilePath = @"C:\a.keymuse", Count = 1 },
                new() { RecordingFilePath = @"C:\b.keymuse", Count = 2 }
            }
        };
        _manager.SaveWorkflow(wf);

        var loaded = _manager.LoadWorkflow("test_flow");
        Assert.NotNull(loaded);
        Assert.Equal("test_flow", loaded!.Name);
        Assert.Equal(3, loaded.TotalCount);
        Assert.Equal(2, loaded.Steps.Count);
        Assert.Equal(2, loaded.Steps[1].Count);
    }

    [Fact]
    public void LoadWorkflow_NotFound_ReturnsNull()
    {
        Assert.Null(_manager.LoadWorkflow("nonexistent"));
    }

    [Fact]
    public void DeleteWorkflow_RemovesFile()
    {
        var wf = new WorkflowModel { Name = "delete_me" };
        _manager.SaveWorkflow(wf);
        _manager.DeleteWorkflow("delete_me");
        Assert.Null(_manager.LoadWorkflow("delete_me"));
    }

    [Fact]
    public void ListWorkflowNames_AfterSave_ReturnsName()
    {
        _manager.SaveWorkflow(new WorkflowModel { Name = "flow_a" });
        _manager.SaveWorkflow(new WorkflowModel { Name = "flow_b" });
        var names = _manager.ListWorkflowNames();
        Assert.Contains("flow_a", names);
        Assert.Contains("flow_b", names);
    }
}
