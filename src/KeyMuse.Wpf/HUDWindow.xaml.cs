using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
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

    private static readonly Brush RecColor = new SolidColorBrush(Color.FromArgb(0xCC, 0x22, 0x66, 0x22));
    private static readonly Brush PlayColor = new SolidColorBrush(Color.FromArgb(0xCC, 0x22, 0x44, 0x66));
    private static readonly Brush ClickColor = new SolidColorBrush(Color.FromArgb(0xCC, 0x66, 0x66, 0x22));
    private static readonly Brush ErrorColor = new SolidColorBrush(Color.FromArgb(0xCC, 0x66, 0x22, 0x22));
    private static readonly Brush WarningColor = new SolidColorBrush(Color.FromArgb(0xCC, 0x88, 0x66, 0x11));
    private static readonly Brush IdleColor = new SolidColorBrush(Color.FromArgb(0xCC, 0x22, 0x22, 0x22));

    private static readonly Brush RecIcon = new SolidColorBrush(Color.FromRgb(0xE0, 0x44, 0x44));
    private static readonly Brush PlayIcon = new SolidColorBrush(Color.FromRgb(0x5B, 0xC0, 0xEB));
    private static readonly Brush ClickIcon = new SolidColorBrush(Color.FromRgb(0xEB, 0xCB, 0x5B));
    private static readonly Brush ErrorIcon = new SolidColorBrush(Color.FromRgb(0xEB, 0x5B, 0x5B));
    private static readonly Brush WarningIcon = new SolidColorBrush(Color.FromRgb(0xEB, 0xA5, 0x3B));
    private static readonly Brush IdleIcon = new SolidColorBrush(Color.FromRgb(0x5B, 0xC0, 0xEB));

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

            if (msg.CountdownMs > 0)
            {
                CountdownText.Text = $"间隔 {msg.CountdownMs / 1000.0:F1}s";
            }
            else
            {
                CountdownText.Text = "";
            }

            UpdateEventList(msg.RecentEvents, msg.RecentEventIndex);

            var (bg, icon) = msg.Type switch
            {
                Core.Models.StatusMessageType.Recording => (RecColor, RecIcon),
                Core.Models.StatusMessageType.Replaying => (PlayColor, PlayIcon),
                Core.Models.StatusMessageType.AutoClicking => (ClickColor, ClickIcon),
                Core.Models.StatusMessageType.Warning => (WarningColor, WarningIcon),
                Core.Models.StatusMessageType.Error => (ErrorColor, ErrorIcon),
                _ => (IdleColor, IdleIcon)
            };
            Background = bg;
            StatusIcon.Foreground = icon;
        }
    }

    private static readonly Brush HighlightFg = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly Brush DimFg = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
    private static readonly Brush CurrentMarker = new SolidColorBrush(Color.FromRgb(0x5B, 0xC0, 0xEB));

    private void UpdateEventList(string[]? events, int highlightIndex)
    {
        EventListText.Inlines.Clear();

        if (events == null || events.Length == 0)
            return;

        for (int i = 0; i < events.Length; i++)
        {
            bool isHighlight = i == highlightIndex;
            var prefix = isHighlight ? "▶ " : "  ";
            var line = events[i];

            if (line == "—")
            {
                EventListText.Inlines.Add(new Run("\n"));
                continue;
            }

            EventListText.Inlines.Add(new Run(prefix + line + "\n")
            {
                Foreground = isHighlight ? HighlightFg : DimFg,
                FontWeight = isHighlight ? FontWeights.Bold : FontWeights.Normal
            });
        }
    }
}
