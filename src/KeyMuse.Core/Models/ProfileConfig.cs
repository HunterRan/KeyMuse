namespace KeyMuse.Core.Models;

public class ProfileConfig
{
    public string Name { get; set; } = "Default";
    public int AutoClickIntervalMs { get; set; } = 1000;
    public int AutoClickKeyCode { get; set; } = 0;
    public bool AutoClickToggleMode { get; set; } = true;
    public string? LastRecordingFile { get; set; }
}
