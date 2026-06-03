using System.Windows;
using Forms = System.Windows.Forms;
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;

namespace KeyMuse.Wpf;

public partial class MainWindow : Window
{
    private readonly App _app;
    private string? _loadedRecording;

    public MainWindow(App app)
    {
        InitializeComponent();
        _app = app;
        RefreshProfileList();
        LoopModeCombo.SelectionChanged += (_, _) =>
            LoopCountBox.IsEnabled = LoopModeCombo.SelectedIndex == 1;
    }

    private void RefreshProfileList()
    {
        ProfileCombo.Items.Clear();
        var profiles = _app.ConfigManager.ListProfiles();
        foreach (var p in profiles) ProfileCombo.Items.Add(p);
        if (profiles.Length > 0) ProfileCombo.SelectedIndex = 0;
    }

    private void ProfileCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is string name)
        {
            _app.ConfigManager.LoadProfile(name);
            var config = _app.ConfigManager.Current;
            if (config != null)
            {
                ClickIntervalBox.Text = config.AutoClickIntervalMs.ToString();
            }
        }
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Controls.TextBox
        {
            Width = 200,
            Text = "配置" + (ProfileCombo.Items.Count + 1)
        };
        var win = new Window
        {
            Title = "新建配置",
            Content = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(10),
                Children =
                {
                    new System.Windows.Controls.Label { Content = "请输入配置名称：" },
                    dialog,
                    new System.Windows.Controls.Button
                    {
                        Content = "确定",
                        Margin = new Thickness(0, 10, 0, 0),
                        IsDefault = true
                    }
                }
            },
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };
        var btn = (System.Windows.Controls.Button)((System.Windows.Controls.StackPanel)win.Content).Children[2];
        btn.Click += (_, _) => { win.DialogResult = true; win.Close(); };
        win.ShowDialog();

        var name = dialog.Text;
        if (!string.IsNullOrWhiteSpace(name))
        {
            _app.ConfigManager.CreateProfile(name);
            RefreshProfileList();
            ProfileCombo.SelectedItem = name;
        }
    }

    private async void RecordBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_app.Recorder.IsRecording)
        {
            RecordBtn.IsEnabled = false;
            var path = await _app.Recorder.StopRecordingAsync();
            RecordBtn.Content = "● 录制";
            _loadedRecording = path;
            RecordingStatus.Text = path != null ? $"已保存: {System.IO.Path.GetFileName(path)}" : "录制已取消";
            RecordBtn.IsEnabled = true;
        }
        else
        {
            _app.Recorder.StartRecording();
            RecordBtn.Content = "● 停止";
            RecordingStatus.Text = "录制中...";
        }
    }

    private async void ReplayBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_app.ReplayEngine.IsPlaying)
        {
            _app.ReplayEngine.Stop();
            return;
        }

        if (_loadedRecording == null)
        {
            System.Windows.MessageBox.Show("请先录制或加载一个录制文件。", "提示");
            return;
        }

        var session = await _app.Recorder.LoadSessionAsync(_loadedRecording);
        if (session == null)
        {
            System.Windows.MessageBox.Show("无法加载录制文件。", "错误");
            return;
        }

        var mode = LoopModeCombo.SelectedIndex switch
        {
            1 => LoopMode.Count,
            2 => LoopMode.Infinite,
            _ => LoopMode.Single
        };
        var count = int.TryParse(LoopCountBox.Text, out var c) ? c : 1;
        var interval = int.TryParse(LoopIntervalBox.Text, out var iv) ? iv : 0;

        ReplayBtn.Content = "⏹ 回放中";
        await _app.ReplayEngine.PlayAsync(session, mode, count, interval);
        ReplayBtn.Content = "▶ 回放";
    }

    private void StopBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_app.ReplayEngine.IsPlaying)
            _app.ReplayEngine.Stop();
        if (_app.AutoClicker.IsRunning)
            _app.AutoClicker.Stop();
    }

    private async void LoadBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Forms.OpenFileDialog
        {
            Filter = "KeyMuse 录制文件 (*.keymuse)|*.keymuse|所有文件 (*.*)|*.*",
            Title = "选择录制文件"
        };
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            _loadedRecording = dialog.FileName;
            var session = await _app.Recorder.LoadSessionAsync(_loadedRecording);
            if (session != null)
            {
                RecordingStatus.Text = $"已加载: {System.IO.Path.GetFileName(dialog.FileName)} ({session.EventCount} 个事件)";
                StatusItem.Content = $"已加载: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
        }
    }

    private void ClickerBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_app.AutoClicker.IsRunning)
        {
            _app.AutoClicker.Stop();
            ClickerBtn.Content = "🔁 启动";
            ClickerStatus.Text = "已停止";
        }
        else
        {
            if (int.TryParse(ClickIntervalBox.Text, out var interval) && interval >= 100)
            {
                _app.AutoClicker.IntervalMs = interval;
                _app.AutoClicker.KeyCode = 0x2D;
                _app.AutoClicker.Start();
                ClickerBtn.Content = "🔁 停止";
                ClickerStatus.Text = "运行中";
            }
            else
            {
                System.Windows.MessageBox.Show("请设置有效的间隔时间（>= 100ms）。", "提示");
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _app.HookManager.Stop();
        base.OnClosed(e);
    }
}
