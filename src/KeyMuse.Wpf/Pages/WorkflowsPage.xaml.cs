using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;
using KeyMuse.Wpf.Controls;

namespace KeyMuse.Wpf.Pages;

public partial class WorkflowsPage : System.Windows.Controls.UserControl
{
    private readonly App _app;
    private WorkflowModel? _currentWorkflow;
    private ObservableCollection<StepViewModel> _steps = new();
    private int _repeatModeIndex;
    private FileSystemWatcher? _watcher;
    private DateTime _lastRefresh = DateTime.MinValue;

    public WorkflowsPage()
    {
        InitializeComponent();
        _app = (App)System.Windows.Application.Current;
        StepList.ItemsSource = _steps;
        RepeatModeCombo.SelectionChanged += (_, _) =>
        {
            _repeatModeIndex = RepeatModeCombo.SelectedIndex;
            var isCount = _repeatModeIndex == 1;
            RepeatCountLabel.Visibility = isCount ? Visibility.Visible : Visibility.Collapsed;
            RepeatCountBox.Visibility = isCount ? Visibility.Visible : Visibility.Collapsed;
            RepeatCountSuffix.Visibility = isCount ? Visibility.Visible : Visibility.Collapsed;
        };
        LoadWorkflows();
        SetupWatcher();
    }

    private void SetupWatcher()
    {
        try
        {
            var dir = _app.WorkflowManager.BaseDir;
            if (!Directory.Exists(dir)) return;
            _watcher = new FileSystemWatcher(dir, "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };
            _watcher.Created += OnDiskChanged;
            _watcher.Deleted += OnDiskChanged;
            _watcher.Renamed += OnDiskChanged;
            _watcher.Changed += OnDiskChanged;
            _watcher.EnableRaisingEvents = true;
        }
        catch { }
    }

    private void OnDiskChanged(object sender, FileSystemEventArgs e)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastRefresh).TotalMilliseconds < 500) return;
        _lastRefresh = now;
        Dispatcher.Invoke(LoadWorkflows);
    }

    private void LoadWorkflows()
    {
        var names = _app.WorkflowManager.ListWorkflowNames();
        WorkflowList.ItemsSource = names.Select(n => _app.WorkflowManager.LoadWorkflow(n)).Where(w => w != null).ToList();
    }

    private void WorkflowList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WorkflowList.SelectedItem is WorkflowModel wf)
        {
            _currentWorkflow = wf;
            _app.SelectedWorkflow = wf;
            RefreshSteps();
            if (wf.TotalCount <= 0)
            {
                _repeatModeIndex = 2;
                RepeatModeCombo.SelectedIndex = 2;
            }
            else if (wf.TotalCount == 1)
            {
                _repeatModeIndex = 0;
                RepeatModeCombo.SelectedIndex = 0;
            }
            else
            {
                _repeatModeIndex = 1;
                RepeatModeCombo.SelectedIndex = 1;
                RepeatCountBox.Text = wf.TotalCount.ToString();
            }
        }
        else
        {
            _app.SelectedWorkflow = null;
        }
    }

    private void RefreshSteps()
    {
        _steps.Clear();
        if (_currentWorkflow == null) return;
        for (int i = 0; i < _currentWorkflow.Steps.Count; i++)
        {
            var step = _currentWorkflow.Steps[i];
            _steps.Add(new StepViewModel
            {
                Index = i + 1,
                FilePath = step.RecordingFilePath,
                Source = step
            });
        }
    }

    private void SaveCurrentWorkflow()
    {
        if (_currentWorkflow == null) return;

        while (_currentWorkflow.Steps.Count > _steps.Count)
            _currentWorkflow.Steps.RemoveAt(_currentWorkflow.Steps.Count - 1);
        while (_currentWorkflow.Steps.Count < _steps.Count)
            _currentWorkflow.Steps.Add(new WorkflowStep());

        for (int i = 0; i < _steps.Count; i++)
        {
            var step = _currentWorkflow.Steps[i];
            var vm = _steps[i];
            step.RecordingFilePath = vm.FilePath;
            step.Count = vm.Count;
            step.IntervalMs = vm.IntervalMs;
        }

        _currentWorkflow.TotalCount = _repeatModeIndex switch
        {
            2 => -1,
            1 => int.TryParse(RepeatCountBox.Text, out var n) ? n : 1,
            _ => 1
        };
        _app.WorkflowManager.SaveWorkflow(_currentWorkflow);
    }

    private void NewWorkflow_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new TextInputDialog("新建工作流", "工作流名称：");
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Answer))
        {
            var wf = new WorkflowModel
            {
                Name = dlg.Answer.Trim(),
                TotalCount = 1,
                Steps = new List<WorkflowStep>()
            };
            _app.WorkflowManager.SaveWorkflow(wf);
            LoadWorkflows();
        }
    }

    private void DeleteWorkflow_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) return;
        if (DarkMessageBox.Show($"确定删除工作流「{_currentWorkflow.Name}」？", "确认", DarkMessageBoxButton.YesNo, DarkMessageBoxIcon.Warning) == true)
        {
            _app.WorkflowManager.DeleteWorkflow(_currentWorkflow.Name);
            _currentWorkflow = null;
            _steps.Clear();
            LoadWorkflows();
        }
    }

    private void AddStep_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) { DarkMessageBox.Show("请先选择一个工作流", "提示", DarkMessageBoxIcon.Info); return; }
        var dlg = new RecordingBrowserDialog();
        if (dlg.ShowDialog() == true && dlg.SelectedFilePath != null)
        {
            _steps.Add(new StepViewModel
            {
                Index = _steps.Count + 1,
                FilePath = dlg.SelectedFilePath,
                Count = 1
            });
            SaveCurrentWorkflow();
            RefreshSteps();
        }
    }

    private void RemoveStep_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) { DarkMessageBox.Show("请先选择一个工作流", "提示", DarkMessageBoxIcon.Info); return; }
        if (StepList.SelectedItem is StepViewModel vm)
        {
            _steps.Remove(vm);
            SaveCurrentWorkflow();
            RefreshSteps();
        }
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) { DarkMessageBox.Show("请先选择一个工作流", "提示", DarkMessageBoxIcon.Info); return; }
        if (StepList.SelectedItem is StepViewModel vm)
        {
            var idx = _steps.IndexOf(vm);
            if (idx > 0)
            {
                _steps.Move(idx, idx - 1);
                SaveCurrentWorkflow();
                RefreshSteps();
            }
        }
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) { DarkMessageBox.Show("请先选择一个工作流", "提示", DarkMessageBoxIcon.Info); return; }
        if (StepList.SelectedItem is StepViewModel vm)
        {
            var idx = _steps.IndexOf(vm);
            if (idx < _steps.Count - 1)
            {
                _steps.Move(idx, idx + 1);
                SaveCurrentWorkflow();
                RefreshSteps();
            }
        }
    }

    private async void RunBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) { DarkMessageBox.Show("请先选择一个工作流", "提示", DarkMessageBoxIcon.Info); return; }
        SaveCurrentWorkflow();
        System.Diagnostics.Debug.WriteLine($"[WorkflowsPage] Run: totalCount={_currentWorkflow.TotalCount} steps={_currentWorkflow.Steps.Count}");
        for (int i = 0; i < _currentWorkflow.Steps.Count; i++)
            System.Diagnostics.Debug.WriteLine($"  step[{i}]: count={_currentWorkflow.Steps[i].Count} intervalMs={_currentWorkflow.Steps[i].IntervalMs}");

        RunBtn.IsEnabled = false;
        ExecStatus.Text = "执行中...";

        _app.WorkflowExecutor.OnProgress += OnExecProgress;
        _app.WorkflowExecutor.OnError += OnExecError;

        try
        {
            await _app.WorkflowExecutor.ExecuteAsync(_currentWorkflow);
        }
        finally
        {
            _app.WorkflowExecutor.OnProgress -= OnExecProgress;
            _app.WorkflowExecutor.OnError -= OnExecError;
            Dispatcher.Invoke(() =>
            {
                RunBtn.IsEnabled = true;
                ExecStatus.Text = "";
            });
        }
    }

    private void OnExecProgress(WorkflowProgress progress)
    {
        Dispatcher.Invoke(() =>
        {
            if (progress.IsCompleted)
                ExecStatus.Text = "✓ 执行完成";
            else if (progress.IsRunning)
            {
                var totalDisplay = progress.TotalOverallCount <= 0 ? "∞" : progress.TotalOverallCount.ToString();
                ExecStatus.Text = $"步骤 {progress.CurrentStepIndex + 1}/{progress.TotalSteps} · 总进度 {progress.CurrentOverallCount}/{totalDisplay}";
            }
            else if (!string.IsNullOrEmpty(progress.ErrorMessage))
                ExecStatus.Text = "✗ " + progress.ErrorMessage;
        });
    }

    private void OnExecError(string message)
    {
        Dispatcher.Invoke(() => ExecStatus.Text = "✗ " + message);
    }

    private void StopExecBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) { DarkMessageBox.Show("请先选择一个工作流", "提示", DarkMessageBoxIcon.Info); return; }
        _app.WorkflowExecutor.Stop();
        ExecStatus.Text = "已停止";
    }
}

public class StepViewModel : System.ComponentModel.INotifyPropertyChanged
{
    public int Index { get; set; }
    public string FilePath { get; set; } = "";
    public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(FilePath);

    public WorkflowStep? Source { get; set; }

    private int _count = 1;
    public int Count
    {
        get => Source?.Count ?? _count;
        set
        {
            if (Source != null) Source.Count = value;
            _count = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
        }
    }

    private int _intervalMs;
    public int IntervalMs
    {
        get => Source?.IntervalMs ?? _intervalMs;
        set
        {
            if (Source != null) Source.IntervalMs = value;
            _intervalMs = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IntervalMs)));
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
