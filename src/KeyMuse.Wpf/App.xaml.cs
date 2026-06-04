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
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {stage}: {ex.GetType().FullName}: {ex.Message}");
            var inner = ex.InnerException;
            while (inner != null)
            {
                sb.AppendLine($"  Inner: {inner.GetType().FullName}: {inner.Message}");
                sb.AppendLine($"  Inner Stack: {inner.StackTrace}");
                inner = inner.InnerException;
            }
            sb.AppendLine(ex.StackTrace);
            System.IO.File.AppendAllText(CrashLogPath, sb.ToString());
        }
        catch { }
    }
    private Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private HUDWindow? _hudWindow;
    private HotKeyManager? _hotKeyManager;
    private string? _lastRecordingPath;
    private string? _f6RecordingCategory;

    public string? SelectedRecordingPath { get; set; }
    public Core.Models.WorkflowModel? SelectedWorkflow { get; set; }

    public HookManager HookManager { get; } = new();
    public InputCoordinator Coordinator { get; } = new();
    public Recorder Recorder { get; }
    public ReplayEngine ReplayEngine { get; }
    public AutoClicker AutoClicker { get; }
    public ConfigManager ConfigManager { get; } = new();
    public StatusMessageQueue MessageQueue { get; } = new();
    public RecordingManager RecordingManager { get; } = new();
    public WorkflowManager WorkflowManager { get; } = new();
    public WorkflowExecutor WorkflowExecutor { get; }
    public string CurrentTheme { get; private set; } = "Dark";

    public App()
    {
        Recorder = new Recorder(HookManager);
        ReplayEngine = new ReplayEngine(Coordinator);
        AutoClicker = new AutoClicker(Coordinator);
        WorkflowExecutor = new WorkflowExecutor(ReplayEngine, Coordinator);

        Recorder.OnStatusChanged += msg => MessageQueue.Enqueue(msg);
        ReplayEngine.OnStatusChanged += msg => MessageQueue.Enqueue(msg);
        AutoClicker.OnStatusChanged += msg => MessageQueue.Enqueue(msg);
    }

    private void ApplyProfileSettings()
    {
        var profiles = ConfigManager.ListProfiles();
        if (profiles.Length > 0)
        {
            var firstProfile = ConfigManager.LoadProfile(profiles[0]);
            if (firstProfile != null)
            {
                if (!string.IsNullOrEmpty(firstProfile.StorageRoot))
                {
                    ConfigManager.SetStorageRoot(firstProfile.StorageRoot);
                    RecordingManager.SetStorageRoot(firstProfile.StorageRoot);
                    WorkflowManager.SetStorageRoot(firstProfile.StorageRoot);
                }
                SwitchTheme(firstProfile.Theme);
            }
        }
    }

    public void SwitchTheme(string themeName)
    {
        if (themeName == CurrentTheme) return;
        CurrentTheme = themeName;

        var uri = themeName switch
        {
            "Light" => new Uri("Themes/Light.xaml", UriKind.Relative),
            "Gray" => new Uri("Themes/Gray.xaml", UriKind.Relative),
            _ => new Uri("Themes/Dark.xaml", UriKind.Relative)
        };

        var dict = new ResourceDictionary { Source = uri };

        if (Resources.MergedDictionaries.Count > 0)
            Resources.MergedDictionaries[0] = dict;
        else
            Resources.MergedDictionaries.Add(dict);
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
            _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F7, OnF7ReplayRecording);
            _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F8, OnF8ReplayWorkflow);
            _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F9, OnF9AutoClick);
            _hotKeyManager.RegisterHotKey(System.Windows.Input.Key.F10, OnF10ToggleUI);

            _hudWindow = new HUDWindow(this);
            _hudWindow.Show();

            ApplyProfileSettings();

            ShowMainWindow();
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
            _mainWindow.SourceInitialized += (_, _) =>
                Helpers.AcrylicHelper.TryEnableAcrylic(_mainWindow);
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

    private async void OnF6Record()
    {
        if (Recorder.IsRecording)
        {
            var tempPath = await Recorder.StopRecordingAsync();
            if (tempPath != null)
            {
                var cat = _f6RecordingCategory ?? "未分类";
                var saved = RecordingManager.SaveRecording(tempPath, cat);
                var nameDlg = new Pages.TextInputDialog("保存录制", "录制名称（留空使用默认名称）：");
                if (nameDlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(nameDlg.Answer))
                {
                    try { RecordingManager.RenameRecording(saved, nameDlg.Answer.Trim()); }
                    catch { }
                }
                _lastRecordingPath = saved;
                _f6RecordingCategory = null;
            }
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Idle,
                Text = "F6: 录制已停止"
            });
        }
        else
        {
            var dlg = new Controls.CategoryPickerDialog();
            if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.SelectedCategory))
                return;
            _f6RecordingCategory = dlg.SelectedCategory;
            Recorder.StartRecording();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Recording,
                Text = $"F6: 开始录制 (分类: {_f6RecordingCategory})"
            });
        }
    }

    private async void OnF7ReplayRecording()
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

        var path = SelectedRecordingPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Controls.DarkMessageBox.Show("请在录制页面先选中一个录制文件", "提示", Controls.DarkMessageBoxIcon.Info);
            return;
        }

        var session = await Recorder.LoadSessionAsync(path!);
        if (session == null)
        {
            Controls.DarkMessageBox.Show("录制文件无效", "错误", Controls.DarkMessageBoxIcon.Error);
            return;
        }

        if (_mainWindow != null) _mainWindow.WindowState = System.Windows.WindowState.Minimized;
        _ = ReplayEngine.PlayAsync(session, LoopMode.Single);
        MessageQueue.Enqueue(new Core.Models.StatusMessage
        {
            Type = Core.Models.StatusMessageType.Replaying,
            Text = "F7: 开始回放"
        });
    }

    private async void OnF8ReplayWorkflow()
    {
        if (ReplayEngine.IsPlaying)
        {
            ReplayEngine.Stop();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.Idle,
                Text = "F8: 工作流已停止"
            });
            return;
        }

        var wf = SelectedWorkflow;
        if (wf == null)
        {
            Controls.DarkMessageBox.Show("请在工作流页面先选中一个工作流", "提示", Controls.DarkMessageBoxIcon.Info);
            return;
        }

        if (_mainWindow != null) _mainWindow.WindowState = System.Windows.WindowState.Minimized;
        await WorkflowExecutor.ExecuteAsync(wf);
        MessageQueue.Enqueue(new Core.Models.StatusMessage
        {
            Type = Core.Models.StatusMessageType.Idle,
            Text = "F8: 工作流执行完成"
        });
    }

    private void OnF9AutoClick()
    {
        if (AutoClicker.IsRunning)
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
            AutoClicker.KeyCode = -1;
            AutoClicker.Start();
            MessageQueue.Enqueue(new Core.Models.StatusMessage
            {
                Type = Core.Models.StatusMessageType.AutoClicking,
                Text = "F9: 开始连点"
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
