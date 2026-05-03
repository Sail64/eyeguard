using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace EyeGuard;

public static class ConfigManager
{
    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public static Config Load()
    {
        if (!File.Exists(ConfigPath))
        {
            return new Config();
        }

        var json = File.ReadAllText(ConfigPath);
        var config = JsonSerializer.Deserialize<Config>(json);
        return config ?? new Config();
    }

    public static void Save(Config config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigPath, json);
    }

    public static bool HasPassword(Config config)
    {
        return !string.IsNullOrWhiteSpace(config.PasswordHash);
    }

    public static string EncryptPassword(string plainPassword)
    {
        var bytes = Encoding.UTF8.GetBytes(plainPassword);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public static bool VerifyPassword(Config config, string inputPassword)
    {
        if (string.IsNullOrWhiteSpace(config.PasswordHash))
            return false;

        try
        {
            var encrypted = Convert.FromBase64String(config.PasswordHash);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var original = Encoding.UTF8.GetString(decrypted);
            return original == inputPassword;
        }
        catch
        {
            return false;
        }
    }

    public static void SetAutoStart(bool enabled)
    {
        var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        if (key == null) return;

        if (enabled)
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + '\\' +
                          AppDomain.CurrentDomain.FriendlyName;
            if (!exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                exePath += ".exe";
            key.SetValue("EyeGuard", exePath);
        }
        else
        {
            key.DeleteValue("EyeGuard", false);
        }

        key.Close();
    }
}
