namespace KeyMuse.Core.Models;

public class RecordingInfo
{
    public string FilePath { get; set; } = "";
    public string FileName => System.IO.Path.GetFileNameWithoutExtension(FilePath);
    public string Category { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int DurationMs { get; set; }
    public int EventCount { get; set; }
    public string DisplayDuration => DurationMs >= 60000
        ? $"{DurationMs / 60000}m{DurationMs % 60000 / 1000}s"
        : $"{DurationMs / 1000}s";
}
