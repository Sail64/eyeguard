using System.Windows;
using System.Windows.Threading;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Shapes.Rectangle;
using WpfLGB = System.Windows.Media.LinearGradientBrush;
using WpfGS = System.Windows.Media.GradientStop;

namespace EyeGuard;

public partial class LockScreenWindow : Window
{
    private readonly Config _config;
    private int _remainingSeconds;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _animTimer;
    private bool _allowClose;
    private double _time;

    private readonly GradientAnimLayer[] _layers;

    public event EventHandler? Unlocked;
    public event EventHandler? EmergencyExitRequested;

    private static readonly WpfColor[] Palette =
    {
        WpfColor.FromRgb(0x0A, 0x3A, 0x6E),
        WpfColor.FromRgb(0x10, 0x42, 0x77),
        WpfColor.FromRgb(0x16, 0x4B, 0x80),
        WpfColor.FromRgb(0x1C, 0x53, 0x89),
        WpfColor.FromRgb(0x22, 0x5B, 0x92),
        WpfColor.FromRgb(0x28, 0x64, 0x9B),
        WpfColor.FromRgb(0x2E, 0x6C, 0xA5),
        WpfColor.FromRgb(0x34, 0x74, 0xAE),
        WpfColor.FromRgb(0x3A, 0x7D, 0xB7),
        WpfColor.FromRgb(0x40, 0x85, 0xC0),
        WpfColor.FromRgb(0x46, 0x8D, 0xC9),
        WpfColor.FromRgb(0x4C, 0x95, 0xD2),
        WpfColor.FromRgb(0x52, 0x9E, 0xDC),
        WpfColor.FromRgb(0x58, 0xA6, 0xE5),
        WpfColor.FromRgb(0x5E, 0xAE, 0xEE),
        WpfColor.FromRgb(0x64, 0xB5, 0xF6),
    };

    private static readonly WpfColor[] AccentPalette =
    {
        WpfColor.FromRgb(0x19, 0x76, 0xD2),
        WpfColor.FromRgb(0x21, 0x7C, 0xD5),
        WpfColor.FromRgb(0x29, 0x82, 0xD7),
        WpfColor.FromRgb(0x31, 0x87, 0xDA),
        WpfColor.FromRgb(0x39, 0x8D, 0xDD),
        WpfColor.FromRgb(0x41, 0x93, 0xE0),
        WpfColor.FromRgb(0x49, 0x98, 0xE2),
        WpfColor.FromRgb(0x51, 0x9E, 0xE5),
        WpfColor.FromRgb(0x59, 0xA4, 0xE8),
        WpfColor.FromRgb(0x61, 0xA9, 0xEA),
        WpfColor.FromRgb(0x69, 0xAF, 0xED),
        WpfColor.FromRgb(0x71, 0xB5, 0xF0),
        WpfColor.FromRgb(0x79, 0xBB, 0xF2),
        WpfColor.FromRgb(0x81, 0xC0, 0xF5),
        WpfColor.FromRgb(0x89, 0xC6, 0xF7),
        WpfColor.FromRgb(0x90, 0xCA, 0xF9),
    };

    public LockScreenWindow(Config config, int totalSeconds)
    {
        InitializeComponent();
        _config = config;
        _remainingSeconds = totalSeconds;
        _allowClose = false;
        _time = 0;
        UpdateCountdownText();

        _layers = new GradientAnimLayer[]
        {
            new(Gradient1, Palette,
                1.0, 0, 0,
                new WpfPoint(0, -0.1), new WpfPoint(0, 1.1),
                0, 0, 0, 0,
                0.6, 0.1),
            new(Gradient2, AccentPalette,
                0.20, 0.10, 0.8,
                new WpfPoint(-0.1, -0.1), new WpfPoint(1.1, 1.1),
                0, 0, 0, 0,
                1.0, 0.15),
            new(Gradient3, Palette,
                0.15, 0.08, 1.2,
                new WpfPoint(1.1, -0.1), new WpfPoint(-0.1, 1.1),
                0, 0, 0, 0,
                1.5, 0.10),
            new(Gradient4, AccentPalette,
                0.10, 0.05, 1.6,
                new WpfPoint(-0.1, 0), new WpfPoint(1.1, 0),
                0, 0, 0, 0,
                2.0, 0.12),
        };

        foreach (var layer in _layers)
            layer.Initialize();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;

        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _animTimer.Tick += AnimTimer_Tick;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _timer.Start();
        _animTimer.Start();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
            e.Cancel = true;
        else
            _animTimer.Stop();
    }

    private void AnimTimer_Tick(object? sender, EventArgs e)
    {
        _time += 0.09;
        foreach (var layer in _layers)
            layer.Animate(_time);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        UpdateCountdownText();
        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            Unlock();
        }
    }

    private void UpdateCountdownText()
    {
        CountdownText.Text = $"{_remainingSeconds / 60:D2}:{_remainingSeconds % 60:D2}";
    }

    private void EmergencyExit_Click(object sender, RoutedEventArgs e)
    {
        EmergencyExitRequested?.Invoke(this, EventArgs.Empty);
    }

    public void AllowClose()
    {
        _timer.Stop();
        _allowClose = true;
    }

    private void Unlock()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Unlocked?.Invoke(this, EventArgs.Empty);
            _allowClose = true;
            Close();
        }));
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TOOLWINDOW);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private class GradientAnimLayer
    {
        private readonly WpfRect _rect;
        private readonly WpfColor[] _palette;
        private readonly double _baseOpacity;
        private readonly double _opacityAmp;
        private readonly double _opacitySpeed;
        private readonly WpfPoint _baseStart;
        private readonly WpfPoint _baseEnd;
        private readonly double _scrollSpeed;
        private readonly double _scrollAmp;
        private WpfLGB? _brush;

        public GradientAnimLayer(WpfRect rect, WpfColor[] palette,
            double baseOpacity, double opacityAmp, double opacitySpeed,
            WpfPoint baseStart, WpfPoint baseEnd,
            double flowSpeed, double flowAmp,
            double waveSpeed, double waveAmp,
            double scrollSpeed, double scrollAmp)
        {
            _rect = rect;
            _palette = palette;
            _baseOpacity = baseOpacity;
            _opacityAmp = opacityAmp;
            _opacitySpeed = opacitySpeed;
            _baseStart = baseStart;
            _baseEnd = baseEnd;
            _scrollSpeed = scrollSpeed;
            _scrollAmp = scrollAmp;
        }

        public void Initialize()
        {
            _brush = new WpfLGB
            {
                StartPoint = _baseStart,
                EndPoint = _baseEnd
            };
            var count = _palette.Length;
            for (int i = 0; i < count; i++)
                _brush.GradientStops.Add(new WpfGS(_palette[i], (double)i / (count - 1)));
            _rect.Fill = _brush;
            _rect.Opacity = _baseOpacity;
        }

        public void Animate(double time)
        {
            var opacity = _baseOpacity + _opacityAmp * Math.Sin(time * _opacitySpeed);
            _rect.Opacity = Math.Clamp(opacity, 0.01, 1.0);

            var scroll = _scrollAmp * Math.Sin(time * _scrollSpeed);
            var dx = _baseEnd.X - _baseStart.X;
            var dy = _baseEnd.Y - _baseStart.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            var nx = len > 0 ? dx / len : 0;
            var ny = len > 0 ? dy / len : 0;
            _brush!.StartPoint = new WpfPoint(_baseStart.X + nx * scroll, _baseStart.Y + ny * scroll);
            _brush.EndPoint = new WpfPoint(_baseEnd.X + nx * scroll, _baseEnd.Y + ny * scroll);
        }

        private static WpfColor Lerp(WpfColor a, WpfColor b, double t)
        {
            return WpfColor.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }
    }
}