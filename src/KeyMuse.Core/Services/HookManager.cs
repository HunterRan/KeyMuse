using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class HookManager : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;

    private nint _keyboardHookId = nint.Zero;
    private nint _mouseHookId = nint.Zero;
    private Thread? _hookThread;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public event Action<InputEvent>? OnInputEvent;
    public event Action<string>? OnError;

    private readonly ConcurrentQueue<InputEvent> _eventQueue = new();

    public bool IsRunning => _isRunning;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();
        _hookThread = new Thread(() => HookThreadProc(_cts.Token))
        {
            Name = "HookManager",
            IsBackground = true
        };
        _hookThread.SetApartmentState(ApartmentState.STA);
        _hookThread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        _hookThread?.Join(1000);

        if (_keyboardHookId != nint.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = nint.Zero;
        }
        if (_mouseHookId != nint.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = nint.Zero;
        }
    }

    private void HookThreadProc(CancellationToken token)
    {
        _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookCallback, nint.Zero, 0);
        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, MouseHookCallback, nint.Zero, 0);

        var heartbeatTimer = new System.Threading.Timer(_ =>
        {
            if (_keyboardHookId == nint.Zero && _mouseHookId == nint.Zero)
            {
                OnError?.Invoke("钩子已断开，正在重连...");
                ReconnectHooks();
            }
        }, null, 0, 500);

        while (!token.IsCancellationRequested)
        {
            if (NativeMethods.PeekMessage(out _, nint.Zero, 0, 0, 1))
            {
                NativeMethods.GetMessage(out _, nint.Zero, 0, 0);
            }
            else
            {
                Thread.Sleep(1);
            }

            while (_eventQueue.TryDequeue(out var evt))
            {
                OnInputEvent?.Invoke(evt);
            }
        }

        heartbeatTimer.Dispose();
    }

    private void ReconnectHooks()
    {
        if (_keyboardHookId == nint.Zero)
        {
            _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookCallback, nint.Zero, 0);
        }
        if (_mouseHookId == nint.Zero)
        {
            _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, MouseHookCallback, nint.Zero, 0);
        }
    }

    private nint KeyboardHookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var khs = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var type = wParam == WM_KEYDOWN ? InputEventType.KeyDown :
                       wParam == WM_KEYUP ? InputEventType.KeyUp : InputEventType.KeyDown;
            _eventQueue.Enqueue(new InputEvent
            {
                TimeOffsetMs = Environment.TickCount,
                Type = type,
                VirtualKeyCode = khs.vkCode
            });
        }
        return CallNextHookEx(nint.Zero, nCode, wParam, lParam);
    }

    private nint MouseHookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var mhs = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var type = wParam switch
            {
                WM_MOUSEMOVE => InputEventType.MouseMove,
                WM_LBUTTONDOWN => InputEventType.MouseDown,
                WM_LBUTTONUP => InputEventType.MouseUp,
                WM_RBUTTONDOWN => InputEventType.MouseDown,
                WM_RBUTTONUP => InputEventType.MouseUp,
                WM_MBUTTONDOWN => InputEventType.MouseDown,
                WM_MBUTTONUP => InputEventType.MouseUp,
                WM_MOUSEWHEEL => InputEventType.MouseWheel,
                _ => InputEventType.MouseMove
            };

            NativeMethods.GetWindowRect(GetForegroundWindow(), out var rect);
            _eventQueue.Enqueue(new InputEvent
            {
                TimeOffsetMs = Environment.TickCount,
                Type = type,
                X = mhs.pt.x,
                Y = mhs.pt.y,
                RelX = mhs.pt.x - rect.left,
                RelY = mhs.pt.y - rect.top,
                MouseData = (int)(wParam >> 16),
                WindowHandle = GetForegroundWindow(),
                WindowLeft = rect.left,
                WindowTop = rect.top,
                WindowWidth = rect.right - rect.left,
                WindowHeight = rect.bottom - rect.top
            });
        }
        return CallNextHookEx(nint.Zero, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    private delegate nint HookProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PeekMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
