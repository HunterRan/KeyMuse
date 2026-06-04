using System.Windows;
using System.Windows.Controls;
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;
using KeyMuse.Wpf.Controls;

namespace KeyMuse.Wpf.Pages;

public partial class RecordingsPage : System.Windows.Controls.UserControl
{
    private readonly App _app;
    private RecordingInfo? _selectedRecording;
    private string? _currentCategory;

    public RecordingsPage()
    {
        InitializeComponent();
        _app = (App)System.Windows.Application.Current;
        LoopModeCombo.SelectionChanged += (_, _) =>
            LoopCountBox.IsEnabled = LoopModeCombo.SelectedIndex == 1;
        LoadCategories();
    }

    private void LoadCategories()
    {
        CategoryList.ItemsSource = null;
        var categories = _app.RecordingManager.ListCategories();
        CategoryList.ItemsSource = categories;
    }

    private void LoadRecordings(string category)
    {
        _currentCategory = category;
        var recordings = _app.RecordingManager.ListRecordings(category);
        RecordingList.ItemsSource = recordings;
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is string cat)
            LoadRecordings(cat);
    }

    private void RecordingList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedRecording = RecordingList.SelectedItem as RecordingInfo;
    }

    private void NewCategory_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new TextInputDialog("新建分类", "分类名称：");
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Answer))
        {
            _app.RecordingManager.CreateCategory(dlg.Answer.Trim());
            LoadCategories();
        }
    }

    private void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        var cat = CategoryList.SelectedItem as string;
        if (cat == null) return;
        if (DarkMessageBox.Show($"确定删除分类「{cat}」及其所有录制？", "确认", DarkMessageBoxButton.YesNo, DarkMessageBoxIcon.Warning) == true)
        {
            _app.RecordingManager.DeleteCategory(cat);
            LoadCategories();
            RecordingList.ItemsSource = null;
        }
    }

    private async void RecordBtn_Click(object sender, RoutedEventArgs e)
    {
        var cat = CategoryList.SelectedItem as string;
        if (cat == null)
        {
            DarkMessageBox.Show("请先选择一个分类", "提示", DarkMessageBoxIcon.Info);
            return;
        }

        if (!_app.Recorder.IsRecording)
        {
            _app.Recorder.StartRecording();
            RecordBtn.Content = "■ 停止";
        }
        else
        {
            RecordBtn.Content = "● 录制";
            var path = await _app.Recorder.StopRecordingAsync();
            if (path != null)
            {
                _app.RecordingManager.SaveRecording(path, cat);
                LoadRecordings(cat);
            }
        }
    }

    private async void ReplayBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRecording == null)
        {
            DarkMessageBox.Show("请先选择一个录制文件", "提示", DarkMessageBoxIcon.Info);
            return;
        }

        if (!System.IO.File.Exists(_selectedRecording.FilePath))
        {
            DarkMessageBox.Show("录制文件不存在", "错误", DarkMessageBoxIcon.Error);
            return;
        }

        var session = await _app.Recorder.LoadSessionAsync(_selectedRecording.FilePath);
        if (session == null)
        {
            DarkMessageBox.Show("无法加载录制文件", "错误", DarkMessageBoxIcon.Error);
            return;
        }

        var mode = LoopMode.Single;
        int count = 1;

        switch (LoopModeCombo.SelectedIndex)
        {
            case 1:
                mode = LoopMode.Count;
                count = int.TryParse(LoopCountBox.Text, out var n) ? n : 3;
                break;
            case 2:
                mode = LoopMode.Infinite;
                break;
        }

        await _app.ReplayEngine.PlayAsync(session, mode, count, 0);
    }

    private void StopBtn_Click(object sender, RoutedEventArgs e)
    {
        _app.ReplayEngine.Stop();
    }

    private void ImportBtn_Click(object sender, RoutedEventArgs e)
    {
        var cat = CategoryList.SelectedItem as string;
        if (cat == null)
        {
            DarkMessageBox.Show("请先选择一个分类", "提示", DarkMessageBoxIcon.Info);
            return;
        }

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "KeyMuse录制 (*.keymuse)|*.keymuse",
            Multiselect = true
        };

        if (dlg.ShowDialog() == true)
        {
            foreach (var file in dlg.FileNames)
            {
                _app.RecordingManager.SaveRecording(file, cat);
            }
            LoadRecordings(cat);
        }
    }

    private void RenameBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRecording == null) return;
        var dlg = new TextInputDialog("重命名", "新名称：", _selectedRecording.FileName);
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Answer))
        {
            _app.RecordingManager.RenameRecording(_selectedRecording.FilePath, dlg.Answer.Trim());
            if (_currentCategory != null) LoadRecordings(_currentCategory);
        }
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRecording == null) return;
        if (DarkMessageBox.Show($"确定删除「{_selectedRecording.FileName}」？", "确认", DarkMessageBoxButton.YesNo, DarkMessageBoxIcon.Warning) == true)
        {
            _app.RecordingManager.DeleteRecording(_selectedRecording.FilePath);
            if (_currentCategory != null) LoadRecordings(_currentCategory);
        }
    }

    private void ExportBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRecording == null) return;
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "KeyMuse录制 (*.keymuse)|*.keymuse",
            FileName = _selectedRecording.FileName
        };
        if (dlg.ShowDialog() == true)
        {
            System.IO.File.Copy(_selectedRecording.FilePath, dlg.FileName, true);
        }
    }
}
