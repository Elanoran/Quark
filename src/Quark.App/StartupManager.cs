using Microsoft.Win32;
using System.Diagnostics;

namespace Quark.App;

public static class StartupManager
{
    private const string AppName = "Quark";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Apply(bool enabled)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            key.SetValue(AppName, Quote(Application.ExecutablePath));
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        string? value = key?.GetValue(AppName) as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string Quote(string path)
    {
        return "\"" + path + "\"";
    }
}
