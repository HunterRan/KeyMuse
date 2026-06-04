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

            UpdateEventList(msg.RecentEvents, msg.RecentEventIndex);

            Background = msg.Type switch
            {
                Core.Models.StatusMessageType.Recording => new SolidColorBrush(Color.FromArgb(0xCC, 0x22, 0x66, 0x22)),
                Core.Models.StatusMessageType.Replaying => new SolidColorBrush(Color.FromArgb(0xCC, 0x22, 0x44, 0x66)),
                Core.Models.StatusMessageType.AutoClicking => new SolidColorBrush(Color.FromArgb(0xCC, 0x66, 0x66, 0x22)),
                Core.Models.StatusMessageType.Error => new SolidColorBrush(Color.FromArgb(0xCC, 0x66, 0x22, 0x22)),
                _ => new SolidColorBrush(Color.FromArgb(0xCC, 0x22, 0x22, 0x22))
            };
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
