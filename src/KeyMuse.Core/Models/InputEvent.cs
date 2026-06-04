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

    public string Description => Type switch
    {
        InputEventType.KeyDown => $"按键 0x{VirtualKeyCode:X2} ↓",
        InputEventType.KeyUp => $"按键 0x{VirtualKeyCode:X2} ↑",
        InputEventType.MouseMove => $"鼠标移动 ({X}, {Y})",
        InputEventType.MouseDown => $"鼠标 {MouseButtonName} 按下",
        InputEventType.MouseUp => $"鼠标 {MouseButtonName} 释放",
        InputEventType.MouseWheel => MouseData > 0 ? "滚轮 ↑" : "滚轮 ↓",
        _ => "未知操作"
    };

    private string MouseButtonName => MouseData switch
    {
        0 => "左键",
        1 => "右键",
        2 => "中键",
        3 => "侧键1",
        4 => "侧键2",
        _ => $"按键{MouseData}"
    };
}
