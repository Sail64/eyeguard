using System.Windows;
using System.Windows.Threading;

namespace EyeGuard;

public partial class SettingsWindow : Window
{
    private readonly Config _config;
    private readonly bool _isFirstSetup;
    public bool Saved { get; private set; }

    public SettingsWindow(Config config) : this(config, !ConfigManager.HasPassword(config))
    {
    }

    public SettingsWindow(Config config, bool isFirstSetup)
    {
        InitializeComponent();
        _config = config;
        _isFirstSetup = isFirstSetup;

        WorkMinutesBox.Text = config.WorkDurationMinutes.ToString();
        BreakMinutesBox.Text = config.BreakDurationMinutes.ToString();
        WarningSecondsBox.Text = config.WarningSeconds.ToString();
        AutoStartCheckBox.IsChecked = config.AutoStart;

        SubtitleText.Text = _isFirstSetup ? "初始化设置" : "修改设置";
        PasswordHintText.Text = _isFirstSetup
            ? "设置解锁密码，用于锁屏解锁和修改设置时验证身份"
            : "留空则保留原密码不变，填写则更新密码";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var password = PasswordBox.Password;
        var confirm = ConfirmPasswordBox.Password;

        if (_isFirstSetup && string.IsNullOrWhiteSpace(password))
        {
            ShowError("密码不能为空");
            return;
        }

        if (!string.IsNullOrWhiteSpace(password) && password != confirm)
        {
            ShowError("两次输入的密码不一致");
            return;
        }

        if (!int.TryParse(WorkMinutesBox.Text, out var workMin) || workMin < 1)
        {
            ShowError("使用时长必须为正整数");
            return;
        }

        if (!int.TryParse(BreakMinutesBox.Text, out var breakMin) || breakMin < 1)
        {
            ShowError("休息时长必须为正整数");
            return;
        }

        if (!int.TryParse(WarningSecondsBox.Text, out var warnSec) || warnSec < 1)
        {
            ShowError("预警时长必须为正整数");
            return;
        }

        _config.WorkDurationMinutes = workMin;
        _config.BreakDurationMinutes = breakMin;
        _config.WarningSeconds = warnSec;

        if (!string.IsNullOrWhiteSpace(password))
        {
            _config.PasswordHash = ConfigManager.EncryptPassword(password);
        }

        _config.AutoStart = AutoStartCheckBox.IsChecked ?? true;

        ConfigManager.Save(_config);
        ConfigManager.SetAutoStart(_config.AutoStart);
        Saved = true;

        SaveButton.IsEnabled = false;
        SuccessBanner.Visibility = Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;

        var closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        closeTimer.Tick += (s, ev) =>
        {
            closeTimer.Stop();
            DialogResult = true;
            Close();
        };
        closeTimer.Start();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}