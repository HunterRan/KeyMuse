using System.Runtime.InteropServices;

namespace KeyMuse.Core.Services;

public class InputSender
{
    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public int FailCount { get; private set; }

    public bool SendKeyDown(int virtualKeyCode)
    {
        var input = CreateKeyboardInput((ushort)virtualKeyCode, KEYEVENTF_KEYDOWN);
        var result = SendInput(1, [input], Marshal.SizeOf<INPUT>());
        if (result == 0) FailCount++;
        return result > 0;
    }

    public bool SendKeyUp(int virtualKeyCode)
    {
        var input = CreateKeyboardInput((ushort)virtualKeyCode, KEYEVENTF_KEYUP);
        var result = SendInput(1, [input], Marshal.SizeOf<INPUT>());
        if (result == 0) FailCount++;
        return result > 0;
    }

    public bool SendMouseMove(int x, int y)
    {
        var input = CreateMouseInput(MOUSEEVENTF_MOVE, x, y, 0);
        var result = SendInput(1, [input], Marshal.SizeOf<INPUT>());
        if (result == 0) FailCount++;
        return result > 0;
    }

    public bool SendMouseDown(int button)
    {
        var flag = button switch
        {
            0 => MOUSEEVENTF_LEFTDOWN,
            1 => MOUSEEVENTF_RIGHTDOWN,
            _ => MOUSEEVENTF_MIDDLEDOWN
        };
        var input = CreateMouseInput(flag, 0, 0, 0);
        var result = SendInput(1, [input], Marshal.SizeOf<INPUT>());
        if (result == 0) FailCount++;
        return result > 0;
    }

    public bool SendMouseUp(int button)
    {
        var flag = button switch
        {
            0 => MOUSEEVENTF_LEFTUP,
            1 => MOUSEEVENTF_RIGHTUP,
            _ => MOUSEEVENTF_MIDDLEUP
        };
        var input = CreateMouseInput(flag, 0, 0, 0);
        var result = SendInput(1, [input], Marshal.SizeOf<INPUT>());
        if (result == 0) FailCount++;
        return result > 0;
    }

    public bool SendMouseWheel(int delta)
    {
        var input = CreateMouseInput(MOUSEEVENTF_WHEEL, 0, 0, delta);
        var result = SendInput(1, [input], Marshal.SizeOf<INPUT>());
        if (result == 0) FailCount++;
        return result > 0;
    }

    private static INPUT CreateKeyboardInput(ushort vk, uint flags)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = nint.Zero
                }
            }
        };
    }

    private static INPUT CreateMouseInput(uint flags, int x, int y, int data)
    {
        return new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = x,
                    dy = y,
                    mouseData = (uint)data,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = nint.Zero
                }
            }
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }
}
