using System.IO;
using System.Windows;
using System.Windows.Controls;
using KeyMuse.Core.Models;
using KeyMuse.Wpf.Controls;
using Forms = System.Windows.Forms;

namespace KeyMuse.Wpf.Pages;

public partial class SettingsPage : System.Windows.Controls.UserControl
{
    private readonly App _app;
    private ProfileConfig? _currentProfile;
    private int _capturedKeyCode = 0x2D;

    public SettingsPage()
    {
        _app = (App)System.Windows.Application.Current;
        InitializeComponent();
        LoadProfiles();
        ShowStoragePaths();
    }

    private void LoadProfiles()
    {
        var profiles = _app.ConfigManager.ListProfiles();
        ProfileCombo.ItemsSource = profiles;
        if (profiles.Length > 0)
            ProfileCombo.SelectedIndex = 0;
    }

    private void ShowStoragePaths()
    {
        StorageRootBox.Text = _currentProfile?.StorageRoot ?? _app.ConfigManager.ProfilesDir;
        RecordingDirText.Text = _app.RecordingManager.BaseDir;
        WorkflowDirText.Text = _app.WorkflowManager.BaseDir;
        ConfigDirText.Text = _app.ConfigManager.ProfilesDir;
    }

    private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is string name)
        {
            _currentProfile = _app.ConfigManager.LoadProfile(name);
            if (_currentProfile != null)
            {
                LoadProfileSettings();
                _app.AutoClicker.IntervalMs = _currentProfile.AutoClickIntervalMs;
                _app.AutoClicker.KeyCode = _currentProfile.AutoClickKeyCode;
            }
        }
    }

    private void LoadProfileSettings()
    {
        if (_currentProfile == null) return;
        ClickIntervalBox.Text = _currentProfile.AutoClickIntervalMs.ToString();
        _capturedKeyCode = _currentProfile.AutoClickKeyCode;
        KeyNameText.Text = KeyMuse.Core.Helpers.KeyNames.GetName(_capturedKeyCode);

        StorageRootBox.Text = _currentProfile.StorageRoot ?? _app.ConfigManager.ProfilesDir;

        ThemeCombo.SelectedIndex = _currentProfile.Theme switch
        {
            "Light" => 1,
            "Gray" => 2,
            _ => 0
        };
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new TextInputDialog("新建配置", "配置名称：");
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Answer))
        {
            _app.ConfigManager.CreateProfile(dlg.Answer.Trim());
            LoadProfiles();
        }
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile == null) return;

        _currentProfile.AutoClickIntervalMs = int.TryParse(ClickIntervalBox.Text, out var interval) ? interval : 1000;
        _currentProfile.AutoClickKeyCode = _capturedKeyCode;

        _currentProfile.Theme = ThemeCombo.SelectedIndex switch
        {
            1 => "Light",
            2 => "Gray",
            _ => "Dark"
        };

        var newRoot = StorageRootBox.Text.Trim();
        if (!string.IsNullOrEmpty(newRoot) && Directory.Exists(newRoot))
        {
            _currentProfile.StorageRoot = newRoot;
            ApplyStorageRoot(newRoot);
        }

        _app.AutoClicker.IntervalMs = _currentProfile.AutoClickIntervalMs;
        _app.AutoClicker.KeyCode = _currentProfile.AutoClickKeyCode;

        _app.ConfigManager.SaveProfile(_currentProfile);
        _app.SwitchTheme(_currentProfile.Theme);
        ShowStoragePaths();
        DarkMessageBox.Show("设置已保存", "KeyMuse", DarkMessageBoxIcon.Info);
    }

    private void ResetBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile == null) return;
        _currentProfile.AutoClickIntervalMs = 1000;
        _currentProfile.AutoClickKeyCode = -1;
        _capturedKeyCode = -1;
        KeyNameText.Text = "鼠标左键";
        _app.AutoClicker.IntervalMs = 1000;
        _app.AutoClicker.KeyCode = -1;
        _app.ConfigManager.SaveProfile(_currentProfile);
        DarkMessageBox.Show("已恢复默认设置", "KeyMuse", DarkMessageBoxIcon.Info);
    }

    private void BrowseStorageRoot_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new Forms.FolderBrowserDialog();
        dlg.Description = "选择 KeyMuse 数据存储根目录";
        dlg.ShowNewFolderButton = true;

        var current = StorageRootBox.Text.Trim();
        if (!string.IsNullOrEmpty(current) && Directory.Exists(current))
            dlg.SelectedPath = current;

        if (dlg.ShowDialog() == Forms.DialogResult.OK)
        {
            StorageRootBox.Text = dlg.SelectedPath;
        }
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var theme = ThemeCombo.SelectedIndex switch
        {
            1 => "Light",
            2 => "Gray",
            _ => "Dark"
        };
        _app.SwitchTheme(theme);
    }

    private void ApplyStorageRoot(string root)
    {
        _app.RecordingManager.SetStorageRoot(root);
        _app.WorkflowManager.SetStorageRoot(root);
    }

    private async void CaptureKeyBtn_Click(object sender, RoutedEventArgs e)
    {
        CaptureKeyBtn.IsEnabled = false;
        KeyNameText.Text = "按任意键...";
        KeyCaptureBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x33, 0x5B, 0xC0, 0xEB));

        var (vkCode, name) = await _app.HookManager.CaptureNextKeyAsync();

        _capturedKeyCode = vkCode;
        KeyNameText.Text = name;
        KeyCaptureBorder.Background = (System.Windows.Media.Brush)FindResource("InputBgBrush");
        CaptureKeyBtn.IsEnabled = true;
    }
}
