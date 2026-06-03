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
    public int ProgressCurrent;
    public int ProgressTotal;
}
