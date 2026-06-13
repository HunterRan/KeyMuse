using System.Windows;
using Application = System.Windows.Application;
using KeyMuse.Core.Models;

namespace KeyMuse.Wpf.Controls;

public partial class WorkflowPickerDialog : Window
{
    private readonly App _app;
    private string[] _workflowNames = [];

    public WorkflowModel? SelectedWorkflow { get; private set; }

    public WorkflowPickerDialog()
    {
        InitializeComponent();
        _app = (App)Application.Current;
        _workflowNames = _app.WorkflowManager.ListWorkflowNames();
        WorkflowList.ItemsSource = _workflowNames;
        if (_workflowNames.Length > 0)
            WorkflowList.SelectedIndex = 0;
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
        if (WorkflowList.SelectedItem is string name)
        {
            SelectedWorkflow = _app.WorkflowManager.LoadWorkflow(name);
            if (SelectedWorkflow != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                DarkMessageBox.Show("无法加载工作流文件", "错误", DarkMessageBoxIcon.Error);
            }
        }
        else
        {
            DarkMessageBox.Show("请先选择一个工作流", "提示", DarkMessageBoxIcon.Info);
        }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
