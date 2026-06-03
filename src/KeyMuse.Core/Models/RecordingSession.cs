namespace KeyMuse.Core.Models;

public class RecordingSession
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int EventCount => Events.Count;
    public int DurationMs { get; set; }
    public int TargetWindowWidth { get; set; }
    public int TargetWindowHeight { get; set; }
    public string? TargetWindowTitle { get; set; }
    public List<InputEvent> Events { get; set; } = new();
}
