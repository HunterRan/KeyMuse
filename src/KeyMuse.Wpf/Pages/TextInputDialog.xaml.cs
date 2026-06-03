using System.Windows;

namespace KeyMuse.Wpf.Pages;

public partial class TextInputDialog : Window
{
    public string? Answer { get; private set; }

    public TextInputDialog(string title, string prompt, string? defaultValue = null)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = defaultValue ?? "";
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
        Answer = InputBox.Text;
        DialogResult = true;
    }
}
