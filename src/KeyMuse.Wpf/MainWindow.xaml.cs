using System.Windows;
using KeyMuse.Core.Models;

namespace KeyMuse.Wpf;

public partial class MainWindow : Window
{
    private readonly App _app;

    public MainWindow(App app)
    {
        InitializeComponent();
        _app = app;
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        var profiles = _app.ConfigManager.ListProfiles();
        ProfileCombo.ItemsSource = profiles;
        if (profiles.Length > 0)
            ProfileCombo.SelectedIndex = 0;
    }

    private void ProfileCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is string name)
            _app.ConfigManager.LoadProfile(name);
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Pages.TextInputDialog("新建配置", "配置名称：");
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Answer))
        {
            _app.ConfigManager.CreateProfile(dlg.Answer.Trim());
            LoadProfiles();
        }
    }

    private void MinBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaxBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_app?.HookManager != null)
            _app.HookManager.Stop();
        base.OnClosed(e);
    }
}