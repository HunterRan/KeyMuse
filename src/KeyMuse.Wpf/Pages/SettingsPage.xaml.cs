using System.Windows;
using System.Windows.Controls;
using KeyMuse.Core.Models;
using KeyMuse.Wpf.Controls;

namespace KeyMuse.Wpf.Pages;

public partial class SettingsPage : System.Windows.Controls.UserControl
{
    private readonly App _app;
    private ProfileConfig? _currentProfile;

    public SettingsPage()
    {
        InitializeComponent();
        _app = (App)System.Windows.Application.Current;
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
        ClickModeCombo.SelectedIndex = _currentProfile.AutoClickToggleMode ? 1 : 0;

        var keyStr = $"0x{_currentProfile.AutoClickKeyCode:X2}";
        for (int i = 0; i < ClickKeyCombo.Items.Count; i++)
        {
            if (ClickKeyCombo.Items[i] is ComboBoxItem item && (item.Tag as string) == keyStr)
            {
                ClickKeyCombo.SelectedIndex = i;
                break;
            }
        }
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
        _currentProfile.AutoClickToggleMode = ClickModeCombo.SelectedIndex == 1;

        if (ClickKeyCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            _currentProfile.AutoClickKeyCode = System.Convert.ToInt32(tag, 16);

        _app.ConfigManager.SaveProfile(_currentProfile);
        DarkMessageBox.Show("设置已保存", "KeyMuse", DarkMessageBoxIcon.Info);
    }

    private void ResetBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile == null) return;
        _currentProfile.AutoClickIntervalMs = 1000;
        _currentProfile.AutoClickKeyCode = 0x2D;
        _currentProfile.AutoClickToggleMode = true;
        _app.ConfigManager.SaveProfile(_currentProfile);
        LoadProfileSettings();
        DarkMessageBox.Show("已恢复默认设置", "KeyMuse", DarkMessageBoxIcon.Info);
    }
}
