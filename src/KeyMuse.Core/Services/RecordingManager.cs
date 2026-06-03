using System.Text.Json;
using System.IO.Compression;
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class RecordingManager
{
    private readonly string _baseDir;

    public string BaseDir => _baseDir;

    public RecordingManager()
    {
        _baseDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMuse", "recordings");
        System.IO.Directory.CreateDirectory(_baseDir);
    }

    public RecordingManager(string customBaseDir)
    {
        _baseDir = customBaseDir;
        System.IO.Directory.CreateDirectory(_baseDir);
    }

    public string[] ListCategories()
    {
        var dirs = System.IO.Directory.GetDirectories(_baseDir)
            .Select(p => System.IO.Path.GetFileName(p)!)
            .ToArray();
        return dirs.OrderBy(x => x == "\u672a\u5206\u7c7b" ? 0 : 1)
                   .ThenBy(x => x)
                   .ToArray();
    }

    public void CreateCategory(string name)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_baseDir, name));
    }

    public void DeleteCategory(string name)
    {
        var dir = System.IO.Path.Combine(_baseDir, name);
        if (!System.IO.Directory.Exists(dir)) return;
        if (System.IO.Directory.GetFiles(dir, "*.keymuse").Length > 0)
            throw new InvalidOperationException($"\u5206\u7c7b '{name}' \u4e0d\u4e3a\u7a7a\uff0c\u65e0\u6cd5\u5220\u9664");
        System.IO.Directory.Delete(dir);
    }

    public string EnsureCategory(string name)
    {
        var dir = System.IO.Path.Combine(_baseDir, name);
        System.IO.Directory.CreateDirectory(dir);
        return dir;
    }

    public RecordingInfo[] ListRecordings(string category)
    {
        var dir = System.IO.Path.Combine(_baseDir, category);
        if (!System.IO.Directory.Exists(dir))
            return Array.Empty<RecordingInfo>();

        return System.IO.Directory.GetFiles(dir, "*.keymuse")
            .Select(LoadRecordingInfo)
            .Where(r => r != null)
            .Select(r => r!)
            .OrderByDescending(r => r.CreatedAt)
            .ToArray();
    }

    public RecordingInfo[] ListAllRecordings()
    {
        if (!System.IO.Directory.Exists(_baseDir))
            return Array.Empty<RecordingInfo>();

        return System.IO.Directory.GetDirectories(_baseDir)
            .SelectMany(dir => System.IO.Directory.GetFiles(dir, "*.keymuse"))
            .Select(LoadRecordingInfo)
            .Where(r => r != null)
            .Select(r => r!)
            .OrderByDescending(r => r.CreatedAt)
            .ToArray();
    }

    private RecordingInfo? LoadRecordingInfo(string filePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);
            var entry = archive.GetEntry("session.json");
            if (entry == null) return null;

            using var reader = new StreamReader(entry.Open());
            var json = reader.ReadToEnd();
            var session = JsonSerializer.Deserialize<RecordingSession>(json);
            if (session == null) return null;

            var dirName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(filePath)) ?? "";
            return new RecordingInfo
            {
                FilePath = filePath,
                Category = dirName,
                CreatedAt = session.CreatedAt,
                DurationMs = session.DurationMs,
                EventCount = session.EventCount
            };
        }
        catch
        {
            return null;
        }
    }

    public void DeleteRecording(string filePath)
    {
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
    }

    public string SaveRecording(string tempFilePath, string category)
    {
        var catDir = EnsureCategory(category);
        var fileName = System.IO.Path.GetFileName(tempFilePath);
        var destPath = System.IO.Path.Combine(catDir, fileName);
        System.IO.File.Move(tempFilePath, destPath);
        return destPath;
    }

    public string MoveRecording(string filePath, string targetCategory)
    {
        var catDir = EnsureCategory(targetCategory);
        var destPath = System.IO.Path.Combine(catDir, System.IO.Path.GetFileName(filePath));
        System.IO.File.Move(filePath, destPath);
        return destPath;
    }

    public string RenameRecording(string filePath, string newName)
    {
        var dir = System.IO.Path.GetDirectoryName(filePath) ?? _baseDir;
        var destPath = System.IO.Path.Combine(dir, newName + ".keymuse");
        System.IO.File.Move(filePath, destPath);
        return destPath;
    }
}
