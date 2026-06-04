using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace KeyMuse.Wpf.Controls;

public enum DarkMessageBoxButton
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

public enum DarkMessageBoxIcon
{
    None,
    Info,
    Question,
    Warning,
    Error
}

public partial class DarkMessageBox : Window
{
    public bool? Result { get; private set; }

    private DarkMessageBox(string message, string title, DarkMessageBoxButton buttons, DarkMessageBoxIcon icon)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        TitleText.Text = title;
        MessageText.Text = message;
        SetupIcon(icon);
        SetupButtons(buttons);
    }

    private void SetupIcon(DarkMessageBoxIcon icon)
    {
        if (icon == DarkMessageBoxIcon.None)
        {
            IconBorder.Visibility = Visibility.Collapsed;
            return;
        }
        IconBorder.Visibility = Visibility.Visible;
        IconBorder.Background = icon switch
        {
            DarkMessageBoxIcon.Info => new SolidColorBrush(Color.FromRgb(10, 132, 255)),
            DarkMessageBoxIcon.Question => new SolidColorBrush(Color.FromRgb(90, 200, 250)),
            DarkMessageBoxIcon.Warning => new SolidColorBrush(Color.FromRgb(255, 159, 10)),
            DarkMessageBoxIcon.Error => new SolidColorBrush(Color.FromRgb(255, 69, 58)),
            _ => new SolidColorBrush(Color.FromRgb(10, 132, 255))
        };
        var iconText = icon switch
        {
            DarkMessageBoxIcon.Info => "i",
            DarkMessageBoxIcon.Question => "?",
            DarkMessageBoxIcon.Warning => "!",
            DarkMessageBoxIcon.Error => "✕",
            _ => ""
        };
        IconBorder.Child = new TextBlock
        {
            Text = iconText,
            Foreground = Brushes.White,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private void SetupButtons(DarkMessageBoxButton buttons)
    {
        switch (buttons)
        {
            case DarkMessageBoxButton.OK:
                AddButton("确定", true, null);
                break;
            case DarkMessageBoxButton.OKCancel:
                AddButton("取消", false, null);
                AddButton("确定", true, true);
                break;
            case DarkMessageBoxButton.YesNo:
                AddButton("否", false, null);
                AddButton("是", true, true);
                break;
            case DarkMessageBoxButton.YesNoCancel:
                AddButton("取消", null, null);
                AddButton("否", false, null);
                AddButton("是", true, true);
                break;
        }
    }

    private void AddButton(string text, bool? result, bool? isDefault)
    {
        var btn = new System.Windows.Controls.Button
        {
            Content = text,
            Margin = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(16, 8, 16, 8),
            MinWidth = 72,
            IsDefault = isDefault == true,
            IsCancel = result == null
        };
        if (isDefault == true && result == true)
            btn.Background = (Brush)FindResource("AccentBrush");
        btn.Click += (_, _) => { Result = result; DialogResult = result ?? false; Close(); };
        ButtonPanel.Children.Add(btn);
    }

    public static bool? Show(string message, string title = "KeyMuse",
        DarkMessageBoxButton buttons = DarkMessageBoxButton.OK,
        DarkMessageBoxIcon icon = DarkMessageBoxIcon.None)
    {
        var dlg = new DarkMessageBox(message, title, buttons, icon);
        dlg.ShowDialog();
        return dlg.Result;
    }

    public static bool? Show(string message, string title, DarkMessageBoxIcon icon)
    {
        return Show(message, title, DarkMessageBoxButton.OK, icon);
    }
}