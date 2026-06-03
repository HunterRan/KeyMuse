using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using Forms = System.Windows.Forms;
using KeyMuse.Core.Services;

namespace KeyMuse.Wpf;

public partial class App : System.Windows.Application
{
    private static readonly string CrashLogPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KeyMuse-crash.log");

    private static void LogCrash(string stage, Exception ex)
    {
        try
        {
            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {stage}: {ex.GetType().FullName}: {ex.Message}\r\n{ex.StackTrace}\r\n";
            System.IO.File.AppendAllText(CrashLogPath, msg);
        }
        catch { }
    }
    private Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private HUDWindow? _hudWindow;
    private HotKeyManager? _hotKeyManager;
    private string? _lastRecordingPath;

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
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogCrash("AppDomain", (Exception)args.ExceptionObject);
        DispatcherUnhandledException += (_, args) =>
        {
            LogCrash("Dispatcher", args.Exception);
            args.Handled = true;
        };
        try
        {
            base.OnStartup(e);
            SetupTrayIcon();
            HookManager.Start();

            _hotKeyManager = new HotKeyManager();
            _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F6, OnF6Record);
            _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F7, OnF7Replay);
            _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F8, OnF8AutoClick);
            _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F9, OnF9StopAll);
            _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F10, OnF10ToggleUI);

            _hudWindow = new HUDWindow(this);
            _hudWindow.Show();

            _mainWindow = new MainWindow(this);
        }
        catch (Exception ex)
        {
            LogCrash("OnStartup", ex);
            Current.Shutdown();
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "KeyMuse - 键鼠自动化\nF6录制  F7回放  F8连点  F9急停  F10窗口",
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

    private void OnF6Record()
    {
        if (Recorder.IsRecording)
        {
            _ = Recorder.StopRecordingAsync().ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null) _lastRecordingPath = t.Result;
            });
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Idle,
                Text = "F6: 录制已停止"
            });
        }
        else
        {
            Recorder.StartRecording();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Recording,
                Text = "F6: 开始录制"
            });
        }
    }

    private async void OnF7Replay()
    {
        if (ReplayEngine.IsPlaying)
        {
            ReplayEngine.Stop();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Idle,
                Text = "F7: 回放已停止"
            });
            return;
        }

        var path = _lastRecordingPath;
        if (path == null || !File.Exists(path))
        {
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Error,
                Text = "F7: 无可用录制文件"
            });
            return;
        }

        var session = await Recorder.LoadSessionAsync(path!);
        if (session == null)
        {
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Error,
                Text = "F7: 录制文件无效"
            });
            return;
        }

        _ = ReplayEngine.PlayAsync(session, LoopMode.Single);
        MessageQueue.Enqueue(new Core.Models.StatusMessage
        {
            Type = Core.Models.StatusMessageType.Replaying,
            Text = "F7: 开始回放"
        });
    }

    private void OnF8AutoClick()
    {
        if (AutoClicker.IsRunning)
        {
            AutoClicker.Stop();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Idle,
                Text = "F8: 连点已停止"
            });
        }
        else
        {
            AutoClicker.KeyCode = 0x2D;
            AutoClicker.Start();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.AutoClicking,
                Text = "F8: 开始连点"
            });
        }
    }

    private void OnF9StopAll()
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
                Text = "F9: 已停止所有任务"
            });
        }
    }

    private void OnF10ToggleUI()
    {
        if (_mainWindow != null && _mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            ShowMainWindow();
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
