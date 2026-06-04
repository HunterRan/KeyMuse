namespace KeyMuse.Core.Models;

public class ProfileConfig
{
    public string Name { get; set; } = "Default";
    public int AutoClickIntervalMs { get; set; } = 1000;
    public int AutoClickKeyCode { get; set; } = -1;
    public bool AutoClickToggleMode { get; set; } = true;
    public string? LastRecordingFile { get; set; }
    public string? StorageRoot { get; set; }
    public string Theme { get; set; } = "Dark";
}
