using System.Text.Json;
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class WorkflowManager
{
    private readonly string _baseDir;

    public string BaseDir => _baseDir;

    public WorkflowManager()
    {
        _baseDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMuse", "workflows");
        System.IO.Directory.CreateDirectory(_baseDir);
    }

    public WorkflowManager(string customBaseDir)
    {
        _baseDir = customBaseDir;
        System.IO.Directory.CreateDirectory(_baseDir);
    }

    public string[] ListWorkflowNames()
    {
        return System.IO.Directory.GetFiles(_baseDir, "*.json")
            .Select(p => System.IO.Path.GetFileNameWithoutExtension(p)!)
            .OrderBy(x => x)
            .ToArray();
    }

    public WorkflowModel? LoadWorkflow(string name)
    {
        var path = System.IO.Path.Combine(_baseDir, name + ".json");
        if (!System.IO.File.Exists(path)) return null;
        var json = System.IO.File.ReadAllText(path);
        return JsonSerializer.Deserialize<WorkflowModel>(json);
    }

    public void SaveWorkflow(WorkflowModel workflow)
    {
        var path = System.IO.Path.Combine(_baseDir, workflow.Name + ".json");
        var json = JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(path, json);
    }

    public void DeleteWorkflow(string name)
    {
        var path = System.IO.Path.Combine(_baseDir, name + ".json");
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    public bool WorkflowExists(string name)
    {
        return System.IO.File.Exists(System.IO.Path.Combine(_baseDir, name + ".json"));
    }
}
