namespace KeyMuse.Core.Models;

public enum InputEventType
{
    KeyDown,
    KeyUp,
    MouseMove,
    MouseDown,
    MouseUp,
    MouseWheel
}

public struct InputEvent
{
    public int TimeOffsetMs;
    public InputEventType Type;
    public int VirtualKeyCode;
    public int X;
    public int Y;
    public int RelX;
    public int RelY;
    public int MouseData;
    public nint WindowHandle;
    public int WindowLeft;
    public int WindowTop;
    public int WindowWidth;
    public int WindowHeight;
}
