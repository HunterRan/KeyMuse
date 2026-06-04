using System.Runtime.InteropServices;
using System.Windows;

namespace KeyMuse.Wpf.Helpers;

public static class AcrylicHelper
{
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWA_MICA = 2;
    private const int DWMWA_ACRYLIC = 3;

    private const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;
    private const int WCA_ACCENT_POLICY = 19;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    public static void TryEnableAcrylic(Window window)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int backdropType = DWMWA_ACRYLIC;
        var result = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

        if (result != 0)
        {
            TrySetWindowCompositionAcrylic(hwnd);
        }
    }

    private static void TrySetWindowCompositionAcrylic(IntPtr hwnd)
    {
        var accent = new AccentPolicy
        {
            AccentState = ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = 0x20,
            GradientColor = unchecked((int)0xCC000000)
        };

        var data = new WindowCompositionAttributeData
        {
            Attribute = WCA_ACCENT_POLICY,
            SizeOfData = Marshal.SizeOf(accent),
            Data = Marshal.AllocHGlobal(Marshal.SizeOf(accent))
        };

        try
        {
            Marshal.StructureToPtr(accent, data.Data, false);
            SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(data.Data);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }
}
