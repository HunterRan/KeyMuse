using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using KeyMuse.Core.Models;
using KeyMuse.Core.Services;

namespace KeyMuse.Wpf.Pages;

public partial class WorkflowsPage : System.Windows.Controls.UserControl
{
    private readonly App _app;
    private WorkflowModel? _currentWorkflow;
    private ObservableCollection<StepViewModel> _steps = new();

    public WorkflowsPage()
    {
        InitializeComponent();
        _app = (App)System.Windows.Application.Current;
        StepList.ItemsSource = _steps;
        LoadWorkflows();
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
            RefreshSteps();
            RepeatCountBox.Text = wf.TotalCount.ToString();
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
                Count = step.Count
            });
        }
    }

    private void SaveCurrentWorkflow()
    {
        if (_currentWorkflow == null) return;
        _currentWorkflow.Steps.Clear();
        foreach (var vm in _steps)
        {
            _currentWorkflow.Steps.Add(new WorkflowStep
            {
                RecordingFilePath = vm.FilePath,
                Count = vm.Count
            });
        }
        _currentWorkflow.TotalCount = int.TryParse(RepeatCountBox.Text, out var n) ? n : 1;
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
        if (System.Windows.MessageBox.Show($"确定删除工作流「{_currentWorkflow.Name}」？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _app.WorkflowManager.DeleteWorkflow(_currentWorkflow.Name);
            _currentWorkflow = null;
            _steps.Clear();
            LoadWorkflows();
        }
    }

    private void AddStep_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "KeyMuse录制 (*.keymuse)|*.keymuse",
            Title = "选择录制文件"
        };
        if (dlg.ShowDialog() == true)
        {
            _steps.Add(new StepViewModel
            {
                Index = _steps.Count + 1,
                FilePath = dlg.FileName,
                Count = 1
            });
            SaveCurrentWorkflow();
            RefreshSteps();
        }
    }

    private void RemoveStep_Click(object sender, RoutedEventArgs e)
    {
        if (StepList.SelectedItem is StepViewModel vm)
        {
            _steps.Remove(vm);
            SaveCurrentWorkflow();
            RefreshSteps();
        }
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
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
        if (_currentWorkflow == null) return;
        SaveCurrentWorkflow();

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
                ExecStatus.Text = $"步骤 {progress.CurrentStepIndex + 1}/{progress.TotalSteps} · 总进度 {progress.CurrentOverallCount}/{progress.TotalOverallCount}";
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
        _app.WorkflowExecutor.Stop();
        ExecStatus.Text = "已停止";
    }
}

public class StepViewModel
{
    public int Index { get; set; }
    public string FilePath { get; set; } = "";
    public int Count { get; set; }
}
