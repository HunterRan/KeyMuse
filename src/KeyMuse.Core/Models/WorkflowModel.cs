namespace KeyMuse.Core.Models;

public class WorkflowStep
{
    public string RecordingFilePath { get; set; } = "";
    public int Count { get; set; } = 1;
}

public class WorkflowModel
{
    public string Name { get; set; } = "";
    public int TotalCount { get; set; } = 1;
    public List<WorkflowStep> Steps { get; set; } = new();
}
