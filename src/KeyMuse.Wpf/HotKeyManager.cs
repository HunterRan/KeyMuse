using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace KeyMuse.Wpf;

public sealed class HotKeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId;

    private static readonly IntPtr HWND_MESSAGE = new(-3);

    public HotKeyManager()
    {
        var ps = new HwndSourceParameters("HotKeySource")
        {
            ParentWindow = HWND_MESSAGE,
            WindowStyle = 0,
            Width = 0,
            Height = 0,
        };
        _source = new HwndSource(ps);
        _source.AddHook(WndProc);
    }

    public bool RegisterHotKey(System.Windows.Input.Key key, Action handler)
    {
        var vk = KeyToVirtualKey(key);
        if (vk == 0) return false;

        var id = ++_nextId;
        if (!NativeMethods.RegisterHotKey(_source.Handle, id, 0, (uint)vk))
            return false;

        _handlers[id] = handler;
        return true;
    }

    public void UnregisterAll()
    {
        foreach (var id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_source.Handle, id);
        _handlers.Clear();
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_handlers.TryGetValue(id, out var handler))
            {
                handler();
                handled = true;
            }
        }
        return nint.Zero;
    }

    private static uint KeyToVirtualKey(System.Windows.Input.Key key) => (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);

    public void Dispose()
    {
        UnregisterAll();
        _source.Dispose();
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(nint hWnd, int id);
    }
}
