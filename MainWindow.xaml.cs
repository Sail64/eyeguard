using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace EyeGuard;

public partial class MainWindow : Window
{
    private NotifyIcon? _notifyIcon;
    private Config _config = null!;
    private TimerService? _timerService;
    private GlobalKeyboardHook? _keyboardHook;
    private List<LockScreenWindow> _lockScreenWindows = new();
    private WarningWindow? _warningWindow;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTrayIcon();
        LoadConfig();
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "EyeGuard - 正在运行",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        var statusItem = new ToolStripMenuItem("状态：初始化中...")
        {
            Enabled = false
        };
        contextMenu.Items.Add(statusItem);
        contextMenu.Items.Add("立即锁屏", null, OnForceBreakClick);
        contextMenu.Items.Add("设置", null, OnSettingsClick);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("退出", null, OnExitClick);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.MouseClick += OnTrayMouseClick;
    }

    private void LoadConfig()
    {
        _config = ConfigManager.Load();

        if (!ConfigManager.HasPassword(_config))
        {
            var settingsWindow = new SettingsWindow(_config);
            if (settingsWindow.ShowDialog() != true)
            {
                Application.Current.Shutdown();
                return;
            }
            _config = ConfigManager.Load();
        }

        InitializeTimer();
        ConfigManager.SetAutoStart(_config.AutoStart);
    }

    private void InitializeTimer()
    {
        _timerService = new TimerService(_config);
        _timerService.WarningStarted += OnWarningStarted;
        _timerService.BreakStarted += OnBreakStarted;
        _timerService.BreakEnded += OnBreakEnded;
        _timerService.Tick += OnTick;
        _timerService.Start();
    }

    private void OnTick(object? sender, int e)
    {
        var state = _timerService?.CurrentState ?? TimerState.Working;
        var minutes = e / 60;
        var seconds = e % 60;

        string tooltipText;
        string menuText;

        switch (state)
        {
            case TimerState.Working:
                tooltipText = $"EyeGuard - 剩余 {minutes:D2}:{seconds:D2}";
                menuText = $"使用中，剩余 {minutes:D2}:{seconds:D2}";
                break;
            case TimerState.Warning:
                tooltipText = $"EyeGuard - 即将锁屏（{e}秒）";
                menuText = $"即将锁屏（{e}秒）";
                break;
            case TimerState.Break:
                tooltipText = $"EyeGuard - 休息中，剩余 {minutes:D2}:{seconds:D2}";
                menuText = $"休息中，剩余 {minutes:D2}:{seconds:D2}";
                break;
            default:
                tooltipText = "EyeGuard - 运行中";
                menuText = "运行中";
                break;
        }

        if (_notifyIcon != null)
        {
            _notifyIcon.Text = tooltipText;
        }

        if (_notifyIcon?.ContextMenuStrip?.Items[0] is ToolStripMenuItem statusItem)
        {
            statusItem.Text = menuText;
        }
    }

    private void OnWarningStarted(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _warningWindow = new WarningWindow(_config.WarningSeconds);
            _warningWindow.Show();
        }));
    }

    private void OnBreakStarted(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // Close warning window if still open
            _warningWindow?.ForceClose();
            _warningWindow = null;

            // Install keyboard hook
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.IsActive = true;

            // Show lock screen on all monitors
            foreach (var screen in Screen.AllScreens)
            {
                var lockWindow = new LockScreenWindow(_config, _config.BreakDurationMinutes * 60);
                lockWindow.Unlocked += OnLockScreenUnlocked;
                lockWindow.EmergencyExitRequested += OnEmergencyExitRequested;
                lockWindow.Left = screen.Bounds.Left;
                lockWindow.Top = screen.Bounds.Top;
                lockWindow.Width = screen.Bounds.Width;
                lockWindow.Height = screen.Bounds.Height;
                lockWindow.Show();
                _lockScreenWindows.Add(lockWindow);
            }
        }));
    }

    private void OnBreakEnded(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            CloseLockScreens();
        }));
    }

    private void OnEmergencyExitRequested(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var dialog = new PasswordVerifyWindow(_config);
            if (dialog.ShowDialog() == true)
            {
                foreach (var window in _lockScreenWindows)
                {
                    window.AllowClose();
                }
                CloseLockScreens();
                _timerService?.ResetToWork();
            }
        }));
    }

    private void OnLockScreenUnlocked(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            CloseLockScreens();
            _timerService?.ResetToWork();
        }));
    }

    private void CloseLockScreens()
    {
        _keyboardHook?.Dispose();
        _keyboardHook = null;

        foreach (var window in _lockScreenWindows)
        {
            try
            {
                window.Close();
            }
            catch { }
        }
        _lockScreenWindows.Clear();
    }

    private void OnTrayMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            OpenSettings();
        }
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        OpenSettings();
    }

    private bool VerifyPassword()
    {
        var dialog = new PasswordVerifyWindow(_config);
        return dialog.ShowDialog() == true;
    }

    private void OpenSettings()
    {
        if (!VerifyPassword()) return;

        var settingsWindow = new SettingsWindow(_config);
        if (settingsWindow.ShowDialog() == true)
        {
            _config = ConfigManager.Load();
            _timerService?.Stop();
            InitializeTimer();
        }
    }

    private void OnForceBreakClick(object? sender, EventArgs e)
    {
        if (_timerService?.CurrentState == TimerState.Break)
        {
            return;
        }

        _timerService?.ForceBreak();
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        if (!VerifyPassword()) return;

        Cleanup();
        Application.Current.Shutdown();
    }

    private void Cleanup()
    {
        _timerService?.Stop();
        _keyboardHook?.Dispose();
        CloseLockScreens();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        base.OnClosing(e);
    }
}
