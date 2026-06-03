using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Forms = System.Windows.Forms;
using KeyMuse.Core.Services;

namespace KeyMuse.Wpf;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private HUDWindow? _hudWindow;

    public HookManager HookManager { get; } = new();
    public InputCoordinator Coordinator { get; } = new();
    public Recorder Recorder { get; }
    public ReplayEngine ReplayEngine { get; }
    public AutoClicker AutoClicker { get; }
    public ConfigManager ConfigManager { get; } = new();
    public StatusMessageQueue MessageQueue { get; } = new();

    public App()
    {
        Recorder = new Recorder(HookManager);
        ReplayEngine = new ReplayEngine(Coordinator);
        AutoClicker = new AutoClicker(Coordinator);

        Recorder.OnStatusChanged += msg => MessageQueue.Enqueue(msg);
        ReplayEngine.OnStatusChanged += msg => MessageQueue.Enqueue(msg);
        AutoClicker.OnStatusChanged += msg => MessageQueue.Enqueue(msg);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetupTrayIcon();
        HookManager.Start();

        _hudWindow = new HUDWindow(this);
        _hudWindow.Show();

        _mainWindow = new MainWindow(this);
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "KeyMuse - 键鼠自动化",
            Visible = true
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("打开主面板", null, (_, _) => ShowMainWindow());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ShutdownApp());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null || !_mainWindow.IsVisible)
        {
            _mainWindow = new MainWindow(this);
        }
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private static System.Drawing.Icon LoadAppIcon()
    {
        var path = Environment.ProcessPath;
        if (path != null)
        {
            try
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon != null) return icon;
            }
            catch { }
        }
        return System.Drawing.Icon.ExtractAssociatedIcon("KeyMuse.Wpf.exe")!;
    }

    public static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void RestartAsAdmin(string? args = null)
    {
        var proc = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath,
            UseShellExecute = true,
            Verb = "runas",
            Arguments = args ?? ""
        };
        Process.Start(proc);
        Current.Shutdown();
    }

    private void ShutdownApp()
    {
        Recorder.StopRecordingAsync().ConfigureAwait(false);
        ReplayEngine.Stop();
        AutoClicker.Stop();
        HookManager.Stop();
        _trayIcon?.Dispose();
        Current.Shutdown();
    }
}
