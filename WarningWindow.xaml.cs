using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using System.Windows.Threading;

namespace EyeGuard;

public partial class WarningWindow : Window
{
    private int _remainingSeconds;
    private readonly int _totalSeconds;
    private readonly DispatcherTimer _timer;
    private bool _allowClose;

    private static readonly SolidColorBrush BlueBrush = new(Color.FromRgb(0x19, 0x76, 0xD2));
    private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(0xF5, 0x7C, 0x00));
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xD3, 0x2F, 0x2F));

    public WarningWindow(int seconds)
    {
        InitializeComponent();
        _remainingSeconds = seconds;
        _totalSeconds = seconds;
        _allowClose = false;
        UpdateDisplay();

        Loaded += PositionWindow;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void PositionWindow(object? sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Width - Width - 20;
        Top = SystemParameters.WorkArea.Height - Height - 20;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            _allowClose = true;
            Close();
        }
        else
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        var minutes = _remainingSeconds / 60;
        var seconds = _remainingSeconds % 60;
        CountdownText.Text = $"{minutes:D2}:{seconds:D2}";

        var ratio = (double)_remainingSeconds / _totalSeconds;

        if (ratio > 0.6)
            CardBorder.Background = BlueBrush;
        else if (ratio > 0.3)
            CardBorder.Background = OrangeBrush;
        else
            CardBorder.Background = RedBrush;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
        }
        base.OnClosing(e);
    }

    public void ForceClose()
    {
        _timer.Stop();
        _allowClose = true;
        Close();
    }
}