using System.Windows;
using System.Windows.Threading;

namespace KeyMuse.Wpf;

public partial class HUDWindow : Window
{
    private readonly App _app;
    private bool _isDragging;
    private System.Windows.Point _dragStart;

    public HUDWindow(App app)
    {
        InitializeComponent();
        _app = app;

        Left = SystemParameters.WorkArea.Left + 10;
        Top = SystemParameters.WorkArea.Bottom - Height - 10;

        MouseDown += (_, e) =>
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                _isDragging = true;
                _dragStart = e.GetPosition(this);
                CaptureMouse();
            }
        };
        MouseMove += (_, e) =>
        {
            if (_isDragging)
            {
                var pos = e.GetPosition(this);
                Left += pos.X - _dragStart.X;
                Top += pos.Y - _dragStart.Y;
            }
        };
        MouseUp += (_, _) =>
        {
            _isDragging = false;
            ReleaseMouseCapture();
        };

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        timer.Tick += PollMessages;
        timer.Start();
    }

    private void PollMessages(object? sender, EventArgs e)
    {
        while (_app.MessageQueue.TryDequeue(out var msg))
        {
            StatusText.Text = msg.Text;

            if (msg.ProgressTotal > 0)
            {
                ProgressText.Text = $"{msg.ProgressCurrent} / {msg.ProgressTotal}";
            }
            else if (msg.ProgressCurrent > 0)
            {
                ProgressText.Text = $"{msg.ProgressCurrent} 次";
            }
            else
            {
                ProgressText.Text = "";
            }

            Background = msg.Type switch
            {
                Core.Models.StatusMessageType.Recording => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x22, 0x66, 0x22)),
                Core.Models.StatusMessageType.Replaying => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x22, 0x44, 0x66)),
                Core.Models.StatusMessageType.AutoClicking => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x66, 0x66, 0x22)),
                Core.Models.StatusMessageType.Error => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x66, 0x22, 0x22)),
                _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x22, 0x22, 0x22))
            };
        }
    }
}
