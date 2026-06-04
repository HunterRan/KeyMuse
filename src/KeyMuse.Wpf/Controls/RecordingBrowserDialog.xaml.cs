using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Application = System.Windows.Application;
using KeyMuse.Core.Models;

namespace KeyMuse.Wpf.Controls;

public partial class RecordingBrowserDialog : Window
{
    private readonly App _app;
    private readonly ObservableCollection<RecordingInfo> _recordings = new();

    public string? SelectedFilePath { get; private set; }

    public RecordingBrowserDialog()
    {
        InitializeComponent();
        _app = (App)Application.Current;
        RecordingList.ItemsSource = _recordings;
        LoadCategories();
    }

    private void LoadCategories()
    {
        var categories = _app.RecordingManager.ListCategories();
        CategoryList.ItemsSource = categories;
        if (categories.Length > 0)
            CategoryList.SelectedIndex = 0;
    }

    private void CategoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _recordings.Clear();
        if (CategoryList.SelectedItem is string cat)
        {
            var recordings = _app.RecordingManager.ListRecordings(cat);
            foreach (var r in recordings)
                _recordings.Add(r);
        }
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
        if (RecordingList.SelectedItem is RecordingInfo info)
        {
            SelectedFilePath = info.FilePath;
            DialogResult = true;
            Close();
        }
        else
        {
            DarkMessageBox.Show("请先选择一个录制文件", "提示", DarkMessageBoxIcon.Info);
        }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
