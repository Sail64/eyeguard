using System.Windows;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Key = System.Windows.Input.Key;

namespace EyeGuard;

public partial class PasswordVerifyWindow : Window
{
    private readonly Config _config;
    public bool Verified { get; private set; }

    public PasswordVerifyWindow(Config config)
    {
        InitializeComponent();
        _config = config;
    }

    private void VerifyButton_Click(object sender, RoutedEventArgs e)
    {
        Verify();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Verify();
        }
    }

    private void Verify()
    {
        if (ConfigManager.VerifyPassword(_config, PasswordBox.Password))
        {
            Verified = true;
            DialogResult = true;
            Close();
        }
        else
        {
            ErrorText.Text = "密码错误，请重新输入";
            ErrorText.Visibility = Visibility.Visible;
            PasswordBox.Clear();
            PasswordBox.Focus();
        }
    }
}