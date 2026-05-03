namespace EyeGuard;

public class Config
{
    public int WorkDurationMinutes { get; set; } = 40;
    public int BreakDurationMinutes { get; set; } = 5;
    public int WarningSeconds { get; set; } = 30;
    public string PasswordHash { get; set; } = string.Empty;
    public bool AutoStart { get; set; } = true;
}
