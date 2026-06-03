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
    private HotKeyManager? _hotKeyManager;

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

        _hotKeyManager = new HotKeyManager();
        _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F9, OnF9Pressed);
        _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F10, OnF10Pressed);

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

    private void OnF9Pressed()
    {
        if (Recorder.IsRecording)
        {
            _ = Recorder.StopRecordingAsync();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Idle,
                Text = "F9: 录制已停止"
            });
        }
        else if (ReplayEngine.IsPlaying)
        {
            ReplayEngine.Stop();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Idle,
                Text = "F9: 回放已停止"
            });
        }
        else if (AutoClicker.IsRunning)
        {
            AutoClicker.Stop();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Idle,
                Text = "F9: 连点已停止"
            });
        }
        else
        {
            Recorder.StartRecording();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Recording,
                Text = "F9: 开始录制"
            });
        }
    }

    private void OnF10Pressed()
    {
        var stopped = false;
        if (Recorder.IsRecording)
        {
            _ = Recorder.StopRecordingAsync();
            stopped = true;
        }
        if (ReplayEngine.IsPlaying)
        {
            ReplayEngine.Stop();
            stopped = true;
        }
        if (AutoClicker.IsRunning)
        {
            AutoClicker.Stop();
            stopped = true;
        }
        if (stopped)
        {
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Idle,
                Text = "F10: 已停止所有任务"
            });
        }
    }

    private void ShutdownApp()
    {
        _hotKeyManager?.Dispose();
        Recorder.StopRecordingAsync().ConfigureAwait(false);
        ReplayEngine.Stop();
        AutoClicker.Stop();
        HookManager.Stop();
        _trayIcon?.Dispose();
        Current.Shutdown();
    }
}
