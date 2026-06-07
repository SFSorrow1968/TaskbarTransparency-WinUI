using Microsoft.Win32;

namespace TaskbarTransparency.Services;

public sealed class StartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "OxygenTaskbar";

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
        {
            key.SetValue(ValueName, Quote(Environment.ProcessPath ?? AppContext.BaseDirectory));
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value && value.Length > 0;
    }

    private static string Quote(string path) => path.StartsWith('"') ? path : $"\"{path}\"";
}
