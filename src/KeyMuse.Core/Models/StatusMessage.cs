namespace KeyMuse.Core.Models;

public enum StatusMessageType
{
    Idle,
    Recording,
    Replaying,
    AutoClicking,
    Error
}

public struct StatusMessage
{
    public StatusMessageType Type;
    public string Text;
    public string Detail;
    public string[]? RecentEvents;
    public int RecentEventIndex;
    public int ProgressCurrent;
    public int ProgressTotal;
}
