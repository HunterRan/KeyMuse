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
                LoadProfileSettings();
        }
    }

    private void LoadProfileSettings()
    {
        if (_currentProfile == null) return;
        ClickIntervalBox.Text = _currentProfile.AutoClickIntervalMs.ToString();
        ClickKeyBox.Text = _currentProfile.AutoClickKeyCode < 0 ? "鼠标左键" : $"0x{_currentProfile.AutoClickKeyCode:X2}";

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
        _currentProfile.AutoClickToggleMode = true;

        var keyText = ClickKeyBox.Text.Trim();
        if (keyText == "鼠标左键")
            _currentProfile.AutoClickKeyCode = -1;
        else if (keyText.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
            _currentProfile.AutoClickKeyCode = System.Convert.ToInt32(keyText, 16);
        else if (int.TryParse(keyText, out var keyCode))
            _currentProfile.AutoClickKeyCode = keyCode;

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
        _app.ConfigManager.SaveProfile(_currentProfile);
        LoadProfileSettings();
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
        _app.ConfigManager.SetStorageRoot(root);
        _app.RecordingManager.SetStorageRoot(root);
        _app.WorkflowManager.SetStorageRoot(root);
    }
}
